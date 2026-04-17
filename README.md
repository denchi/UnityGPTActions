# GPT Actions for Unity

[![Unity 2021.2.3f1](https://github.com/denchi/UnityGPTActions/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/denchi/UnityGPTActions/actions/workflows/tests.yml)

Unity Editor tooling for AI-assisted project edits, deep search, and Unity MCP integration.

## Requirements

- Unity **2021.2.3f1** or newer
- Python **3.10+** for MCP features
- OpenAI API key (for chat/features that call OpenAI)

## Installation

Add the package to `Packages/manifest.json`.

If the package repo root is the package itself:

```json
"com.deathbygravitystudio.gptactions": "https://github.com/denchi/UnityGPTActions.git"
```

If the repo is a monorepo:

```json
"com.deathbygravitystudio.gptactions": "https://github.com/denchi/UnityGPTActions.git?path=/Packages/com.deathbygravitystudio.gptactions"
```

Create `Assets/StreamingAssets/.env`:

```dotenv
OPENAI_API_KEY=<your_openai_key>
SERP_API_KEY=<optional_serpapi_key>
GOOGLE_CSE_API_KEY=<optional_google_api_key>
GOOGLE_CSE_CX=<optional_google_search_engine_id>
```

## First Run (New Machine)

After Unity imports the package:

1. Open `Project Settings > Chat Settings`.
2. Verify Python settings in **Common Configs**.
3. Click `Start Search Server` in the **Search** section.
4. Click `Start MCP Services` in the **MCP** section.
5. Confirm both status indicators become `Online`.

Notes:
- Search server setup installs indexing/search dependencies into your configured venv.
- MCP setup now also ensures MCP dependencies even if the venv already existed.
- `MCP Autostart` starts bridge + MCP server on editor launch when env is ready.

## Which Server Does What?

| Button | Service | Default URL | Needed for |
| --- | --- | --- | --- |
| `Start Search Server` | Deep Search API | `http://127.0.0.1:8000` | semantic search/index queries |
| `Start MCP Services` | Unity MCP Bridge + MCP Python Server | bridge `http://127.0.0.1:7071`, server `http://127.0.0.1:7072/mcp` | MCP clients (Codex, external MCP tools) |

If you only use local in-editor chat and do not use MCP clients, MCP server is optional.

## Python Compatibility

MCP requires Python >= 3.10. The setup flow tries common interpreter names and paths (`python3`, `python`, `py`, `python3.10+`) and validates the version before creating/updating the env.

If no valid interpreter is found, install one and retry:

- macOS (Homebrew): `brew install python@3.12`
- Ubuntu/Debian: `sudo apt install python3 python3-venv python3-pip`
- Windows (winget): `winget install Python.Python.3.12`

## What To Commit / Push

When adding this package to a Unity project, commit:

- `Packages/manifest.json`
- `Packages/packages-lock.json`

Optionally commit if you want team-shared defaults:

- `ProjectSettings/ChatSettings.asset`

Never commit secrets/local runtime state:

- `Assets/StreamingAssets/.env` (already ignored)
- `Library/`, generated venvs, local machine-specific cache

## Usage

Open **AI Chat** from `Window > AI Chat`.

The assistant can execute editor actions (assets, hierarchy, scripts, settings, search). For the full action catalog, see:

- `Packages/com.deathbygravitystudio.gptactions/Editor/Actions/`

## Health Checks

- Search API: `GET http://127.0.0.1:8000/ping`
- MCP server: `GET http://127.0.0.1:7072/mcp/health`
- MCP bridge: `GET http://127.0.0.1:7071/health`
