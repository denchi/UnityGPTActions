#!/usr/bin/env python3
import argparse
import json
import urllib.request
import urllib.error

import asyncio
import uvicorn
from starlette.applications import Starlette
from starlette.responses import JSONResponse
from starlette.routing import Route, Mount

from mcp.server import Server
from mcp.server.sse import SseServerTransport
from mcp.types import Tool, TextContent


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

    @server.list_tools()
    async def list_tools():
        tools_url = unity_url.rstrip("/") + "/tools"
        tools = _http_get_json(tools_url)
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

    async def handle_sse(request):
        async with sse.connect_sse(request.scope, request.receive, request._send) as streams:
            await server.run(
                streams[0], streams[1], server.create_initialization_options()
            )

    async def handle_shutdown(request):
        server = getattr(request.app.state, "server", None)
        if server is not None:
            server.should_exit = True
        return JSONResponse({"ok": True})

    async def handle_health(request):
        return JSONResponse({"ok": True})

    app = Starlette(
        routes=[
            Route("/mcp/sse", endpoint=handle_sse),
            Mount("/mcp/messages", app=sse.handle_post_message),
            Route("/mcp/shutdown", endpoint=handle_shutdown, methods=["POST"]),
            Route("/mcp/health", endpoint=handle_health),
        ],
    )

    async def run_server():
        config = uvicorn.Config(app, host=args.host, port=args.port, log_level=args.log_level)
        server = uvicorn.Server(config)
        app.state.server = server
        await server.serve()

    asyncio.run(run_server())


if __name__ == "__main__":
    main()
