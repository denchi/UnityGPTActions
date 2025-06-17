# GPT Actions for Unity

[![Tests](https://github.com/denchi/UnityGPTActions/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/denchi/UnityGPTActions/actions/workflows/tests.yml)

**GPT Actions** is a Unity Editor extension that adds an AI powered chat window capable of executing editor actions through OpenAI's GPT models. The package exposes a collection of tools that let the assistant create and modify assets, generate scripts, query project information and automate common Unity workflows.

## Installation

Add the package to your project's `manifest.json` using the Git URL:

```json
"com.deathbygravitystudio.gptactions": "https://github.com/denchi/UnityGPTActions.git"
```

All required dependencies, including `com.deathbygravitystudio.environment`, will be fetched automatically.

## Setup API Keys

Create a file named `.env` inside your project's `Assets/StreamingAssets` folder and define your API keys:

```
OPENAI_API_KEY=<your_openai_key>
SERP_API_KEY=<optional_serpapi_key>
```

The OpenAI key is required for chatting with the assistant. The optional SerpAPI key enables actions that search the internet.

## Usage

1. Open the **AI Chat** window from the Unity menu: `Window > AI Chat`.
2. Type a prompt or choose one of the suggested templates. The assistant will respond and may execute tool calls to modify your project.
3. Actions include creating prefabs, generating scripts and shaders, adjusting serialized fields, querying assets, managing packages and more.

Chat and tool call history is stored between sessions so you can continue a conversation after closing the editor.

## License

This project is licensed under the [MIT License](LICENSE).
