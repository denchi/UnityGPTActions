
# 🧠 Unity Deep Search Integration (LLM + Embeddings)

This repository/project contains two tightly integrated parts:
1. A **Python-based deep search server** (using embeddings and FAISS)
2. A **Unity C# integration** to query the server via GPTActions

## 📂 Directory Structure Overview

```
UnityProject/
├── Packages/
│   └── com.deathbygravitystudio.gptactions/
│       ├── Editor/
│       │   ├── Assistant/
│       │   │   ├── DeepSearchClient.cs
│       │   │   ├── QueryDeepSearchAction.cs
│       │   │   ├── QueryAssetsAction.cs  # Example reference
│       │   └── Extractor/
│       │       ├── extract_all.py
│       │       ├── extractors/
│       │       └── ...
│       └── package.json
├── DeepSearchIndexer/   # Python tooling (OUTSIDE Unity)
│   ├── Library/py/mcp/
│   ├── extract_all.py
│   ├── index_all.py
│   ├── search_api.py
│   ├── data/
│   ├── embeddings/
│   ├── requirements.txt
└── ...
```

## 🚀 Python Deep Search Indexer (Outside Unity Project)

### ✅ Setup
1. Create and activate a virtual environment:
```bash
cd DeepSearchIndexer
python3 -m venv Library/py/mcp
source Library/py/mcp/bin/activate  # Or Library\py\mcp\Scripts\activate on Windows
```

2. Install dependencies:
```bash
pip install -r requirements.txt
```

### ✅ Run Extraction & Indexing
Extract chunks from Unity Assets and create a FAISS index.

```bash
python extract_all.py --project ./path/to/UnityProject/Assets
python index_all.py
```

This will generate:
```
./data/embeddings/faiss.index
./data/embeddings/chunks_metadata.json
```

### ✅ Start the Search API (Manually)
```bash
python search_api.py
```

Test it:
```bash
curl http://127.0.0.1:8000/docs
```

## 🔗 Unity Integration

### ✅ DeepSearchClient.cs Responsibilities
- Start the Python API server if it's not running
- Stop the Python API server on request
- Query the API for semantic search results

### 🔧 Starting and Stopping Server (From Unity)
```csharp
DeepSearchClient.StartSearchServerAsync();
DeepSearchClient.StopSearchServer();
```

These commands are available to:
- Automatically start when `SearchAsync()` detects no server.
- Be wired into Unity Editor menus for manual control.

### 🔍 Querying the Server
```csharp
var results = await DeepSearchClient.SearchAsync("player shooting", 5);
```
Results include:
- Asset file paths
- Asset types
- Code/content snippets

## ✨ GPT Action: QueryDeepSearchAction.cs
A GPTAction designed to allow LLM to run:
```csharp
[GPTAction("Performs a deep semantic search over indexed Unity project files and returns paths and summaries of matching assets or code.")]
```

## 🛠️ Recommended Unity Menu Items (Optional)
```csharp
[MenuItem("Tools/Unity Assistant/Start Deep Search Server")]
public static void StartServerMenu() => DeepSearchClient.StartSearchServerAsync();

[MenuItem("Tools/Unity Assistant/Stop Deep Search Server")]
public static void StopServerMenu() => DeepSearchClient.StopSearchServer();
```

## ✅ How to Confirm It's Working
1. Run `SearchAsync` from GPTAction or manually in Unity.
2. Confirm:
    - The server starts automatically if needed.
    - Results show valid `Assets/...` paths and snippets.
3. You can kill the server manually via:
```bash
lsof -i :8000
kill <PID>
```
Or via `DeepSearchClient.StopSearchServer()`.

## ⚠️ Notes
- Keep `DeepSearchIndexer` OUTSIDE your Unity `Packages/` to avoid import issues.
- Avoid putting `Library/py/` inside Unity packages.
- Unity expects paths starting with `Assets/` for AssetDatabase access.

## 📝 Summary Responsibilities
| Component             | Role                                 |
|------------------------|--------------------------------------|
| `extract_all.py`       | Extracts Unity project chunks         |
| `index_all.py`         | Embeds & builds FAISS index           |
| `search_api.py`        | FastAPI endpoint for search           |
| `DeepSearchClient.cs`  | Starts/stops server, performs search   |
| `QueryDeepSearchAction.cs` | Exposes search to GPT tools      |
