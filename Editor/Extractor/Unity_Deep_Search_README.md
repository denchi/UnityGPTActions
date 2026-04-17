# Unity Deep Search and MCP Runtime Notes

This package runs two local Python services from Unity Project Settings:

- `Start Search Server` (Deep Search API)
- `Start MCP Services` (Unity MCP Bridge + MCP protocol server)

For standard setup, use `Project Settings > Chat Settings`. Do not use the old `http.server` command; the Search API entrypoint is `search_api.py`.

## Manual Commands (Optional)

Run these only when debugging outside the UI.

### Search API

```bash
python Packages/com.deathbygravitystudio.gptactions/Editor/Extractor/search_api.py --host 127.0.0.1 --port 8000
```

Health check:

```bash
curl http://127.0.0.1:8000/ping
```

### MCP Server

```bash
python Packages/com.deathbygravitystudio.gptactions/Editor/Mcp/mcp_server.py \
  --unity-url http://127.0.0.1:7071 \
  --host 127.0.0.1 \
  --port 7072
```

Health check:

```bash
curl http://127.0.0.1:7072/mcp/health
```

## Python Requirement

- MCP requires Python >= 3.10.
- If setup fails, install Python 3.10+ and set `Python Path` / `Python Fallback` in `Project Settings > Chat Settings`.
