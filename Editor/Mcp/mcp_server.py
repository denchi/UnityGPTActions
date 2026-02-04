#!/usr/bin/env python3
import argparse
import json
import urllib.error
import urllib.request

import asyncio
import contextlib
import uvicorn
from starlette.applications import Starlette
from starlette.responses import JSONResponse
from starlette.routing import Mount, Route

try:
    from mcp.server.lowlevel import Server
except Exception:
    from mcp.server import Server
from mcp.server.sse import SseServerTransport
from mcp.server.streamable_http_manager import StreamableHTTPSessionManager
from mcp.types import Resource, Tool, TextContent

_resource_cache = []
_tool_cache = []


def _http_get_json(url):
    req = urllib.request.Request(url, method="GET")
    with urllib.request.urlopen(req, timeout=5) as resp:
        data = resp.read().decode("utf-8")
        return json.loads(data)


def _http_post_json(url, payload):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=data, method="POST")
    req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req, timeout=30) as resp:
        text = resp.read().decode("utf-8")
        return json.loads(text)


def create_server(unity_url):
    server = Server("UnityAssistant")

    def refresh_tool_cache():
        nonlocal unity_url
        try:
            tools_url = unity_url.rstrip("/") + "/tools"
            tools = _http_get_json(tools_url)
            return tools if isinstance(tools, list) else []
        except Exception:
            return []

    def rebuild_resource_cache(tools):
        resources = []
        for tool in tools:
            name = tool.get("name")
            if not name:
                continue
            resources.append(
                Resource(
                    name=name,
                    uri=f"tool://{name}",
                    description=tool.get("description") or "",
                    mimeType="application/json",
                    _meta={"source": "unity-tools"},
                )
            )
        return resources

    global _tool_cache, _resource_cache
    _tool_cache = refresh_tool_cache()
    _resource_cache = rebuild_resource_cache(_tool_cache)

    @server.list_tools()
    async def list_tools():
        tools = _tool_cache
        results = []
        if isinstance(tools, list):
            for tool in tools:
                name = tool.get("name")
                if not name:
                    continue
                results.append(
                    Tool(
                        name=name,
                        description=tool.get("description") or "",
                        inputSchema=tool.get("inputSchema")
                        or {"type": "object", "properties": {}},
                    )
                )
        return results

    @server.list_resources()
    async def list_resources():
        return _resource_cache

    @server.read_resource()
    async def read_resource(uri):
        if not uri.startswith("tool://"):
            return "Resource not found"
        name = uri.replace("tool://", "", 1)
        for tool in _tool_cache:
            if tool.get("name") == name:
                return json.dumps(tool)
        return "Resource not found"

    @server.call_tool()
    async def call_tool(name, arguments):
        call_url = unity_url.rstrip("/") + "/tools/call"
        payload = {"name": name, "arguments": arguments or {}}
        response = _http_post_json(call_url, payload)
        text = response.get("content", "")
        return [TextContent(type="text", text=text)]

    return server


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--unity-url", required=True, help="Unity MCP Bridge base URL")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=7072)
    parser.add_argument("--log-level", default="info")
    args = parser.parse_args()

    server = create_server(args.unity_url)
    sse = SseServerTransport("/mcp/messages")
    streamable_http = StreamableHTTPSessionManager(
        app=server,
        event_store=None,
        json_response=True,
        stateless=True,
    )

    async def handle_sse(request):
        async with sse.connect_sse(request.scope, request.receive, request._send) as streams:
            await server.run(
                streams[0], streams[1], server.create_initialization_options()
            )

    class StreamableASGI:
        def __init__(self, manager):
            self.manager = manager

        async def __call__(self, scope, receive, send):
            await self.manager.handle_request(scope, receive, send)

    streamable_asgi = StreamableASGI(streamable_http)

    @contextlib.asynccontextmanager
    async def lifespan(app):
        async with streamable_http.run():
            yield

    async def handle_shutdown(request):
        server = getattr(request.app.state, "server", None)
        if server is not None:
            server.should_exit = True
        return JSONResponse({"ok": True})

    async def handle_health(request):
        return JSONResponse({"ok": True})

    app = Starlette(
        lifespan=lifespan,
        routes=[
            Route("/mcp/sse", endpoint=handle_sse),
            Mount("/mcp/messages", app=sse.handle_post_message),
            Route("/mcp/shutdown", endpoint=handle_shutdown, methods=["POST"]),
            Route("/mcp/health", endpoint=handle_health),
            Route("/mcp", endpoint=streamable_asgi, methods=["POST"]),
            Route("/mcp/", endpoint=streamable_asgi, methods=["POST"]),
        ],
    )
    app.router.redirect_slashes = False

    async def run_server():
        config = uvicorn.Config(app, host=args.host, port=args.port, log_level=args.log_level)
        server = uvicorn.Server(config)
        app.state.server = server
        await server.serve()

    asyncio.run(run_server())


if __name__ == "__main__":
    main()
