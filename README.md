# GPT Actions for Unity

[![Unity 2021.2.3f1](https://github.com/denchi/UnityGPTActions/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/denchi/UnityGPTActions/actions/workflows/tests.yml)

## Supported Unity Versions

This package is tested with **Unity 2021.2.3f1** and newer.

## Content
- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Actions](#actions)
- [Indexing](#indexing)
- [Deep Search](#deep-search)
- [Folders & Config](#folders--config)
- [cURL Test](#curl-test)

## Features

- AI-powered chat window that can execute editor commands
- Tools to create or modify assets, scripts and shaders
- Retrieve project information like tags, layers and packages
- Automate common Unity workflows directly from the chat
- Utilizes the new OpenAI `/responses` endpoint for improved results
- Implementation in `Runtime/Api/OpenAIResponsesApiService.cs`

## Installation

Add the package to your `manifest.json`:

```json
"com.deathbygravitystudio.gptactions": "https://github.com/denchi/UnityGPTActions.git"
```

Create `Assets/StreamingAssets/.env` and define:

```
OPENAI_API_KEY=<your_openai_key>
SERP_API_KEY=<optional_serpapi_key>
```

## Usage

1. Open **AI Chat** from `Window > AI Chat`.
2. Enter a prompt or choose a template and let the assistant modify your project.
3. Actions include spawning objects, adjusting serialized fields, generating code and more.

## Actions

The extension exposes the following actions:
- [AddPackageAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/AddPackageAction.cs)
- [AdjustRectTransformAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/AdjustRectTransformAction.cs)
- [AdjustSerializedFieldAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/AdjustSerializedFieldAction.cs)
- [AdjustUnityObjectFieldAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/AdjustUnityObjectFieldAction.cs)
- [ApplyMaterialAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/ApplyMaterialAction.cs)
- [AssignLayerAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/AssignLayerAction.cs)
- [AssignPrimitiveMeshAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/AssignPrimitiveMeshAction.cs)
- [AttachRemoveComponentAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/AttachRemoveComponentAction.cs)
- [CreateAnimatorControllerAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreateAnimatorControllerAction.cs)
- [CreateCustomShaderFunction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreateCustomShaderFunction.cs)
- [CreateGameObjectAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreateGameObjectAction.cs)
- [CreateLayerAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreateLayerAction.cs)
- [CreateMonoBehaviourAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreateMonoBehaviourAction.cs)
- [CreatePrefabAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreatePrefabAction.cs)
- [CreateScriptableObjectAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreateScriptableObjectAction.cs)
- [CreateTagAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreateTagAction.cs)
- [CreateUnityAssetAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreateUnityAssetAction.cs)
- [CreatesServiceScriptAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/CreatesServiceScriptAction.cs)
- [DescribeCSharpScriptAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/DescribeCSharpScriptAction.cs)
- [DumpProjectSettingPropertiesAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/DumpProjectSettingPropertiesAction.cs)
- [GenerateCSharpScriptAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GenerateCSharpScriptAction.cs)
- [GenerateMaterialAssetAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GenerateMaterialAssetAction.cs)
- [GenerateMeshAssetAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GenerateMeshAssetAction.cs)
- [GenerateRigidbodyAndColliderAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GenerateRigidbodyAndColliderAction.cs)
- [GenerateScriptableMenuAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GenerateScriptableMenuAction.cs)
- [GenerateShaderAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GenerateShaderAction.cs)
- [GenerateSvgImageIgptAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GenerateSvgImageIgptAction.cs)
- [GenerateTextureAssetAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GenerateTextureAssetAction.cs)
- [GetObjectBoundsAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GetObjectBoundsAction.cs)
- [GetRectTransformPropertiesAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/GetRectTransformPropertiesAction.cs)
- [ListAllProjectSettingsAssetsAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/ListAllProjectSettingsAssetsAction.cs)
- [ModifyProjectSettingAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/ModifyProjectSettingAction.cs)
- [ParentGameObjectAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/ParentGameObjectAction.cs)
- [QueryAssetsAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/QueryAssetsAction.cs)
- [QueryCSharpClassesByParentTypeAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/QueryCSharpClassesByParentTypeAction.cs)
- [QueryInternetAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/QueryInternetAction.cs)
- [QueryOpenedSceneAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/QueryOpenedSceneAction.cs)
- [RemovePackageAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/RemovePackageAction.cs)
- [RetrieveLayersAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/RetrieveLayersAction.cs)
- [RetrievePackagesAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/RetrievePackagesAction.cs)
- [RetrieveProjectInfoAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/RetrieveProjectInfoAction.cs)
- [RetrieveProjectSettingAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/RetrieveProjectSettingAction.cs)
- [RetrieveTagsAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/RetrieveTagsAction.cs)
- [RunPythonCode](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/RunPythonCode.cs)
- [SelectAssetsAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/SelectAssetsAction.cs)
- [SpawnGameObjectAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/SpawnGameObjectAction.cs)
- [ToggleGameObjectActiveStateAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/ToggleGameObjectActiveStateAction.cs)
- [TransformGameObjectAction](https://github.com/denchi/UnityGPTActions/blob/main/Editor/Actions/TransformGameObjectAction.cs)

## Indexing

The package can index your project's assets and scripts to enable fast searching and context-aware actions. Indexing is performed automatically on startup or can be triggered manually from the AI Chat window. Indexed data includes file paths, script classes, asset types, and metadata.

- **Manual Indexing:** Use the "Reindex Project" button in the AI Chat window.
- **Automatic Indexing:** Occurs on package initialization or after major asset changes.

## Deep Search

Deep Search allows you to query across all indexed files, folders, and scripts using natural language or keywords. This feature supports advanced filtering and can locate code, assets, or configuration files based on your prompt.

- **Usage:** Enter search queries in the AI Chat window.
- **Supported Filters:** File type, folder, class name, asset type, and more.

Testing:
``` bash
curl -X POST http://127.0.0.1:8000/search -H "Content-Type: application/json" -d '{"query": "bullet"}'
```

Kill:
``` bash
kill -9 $(lsof -t -i :8000)
```

Start:
``` bash
python3 -m http.server 8000
```