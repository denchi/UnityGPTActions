from starlette.applications import Starlette
from starlette.responses import JSONResponse
from starlette.routing import Route
from starlette.requests import Request
from sentence_transformers import SentenceTransformer
import argparse
import faiss
import numpy as np
import json
import uvicorn

# ------------------------
# Configuration
index_file = "./data/embeddings/faiss.index"
metadata_file = "./data/embeddings/chunks_metadata.json"
embedding_model_name = "all-MiniLM-L6-v2"
# ------------------------

# ------------------------
# Load Embeddings + Metadata
index = faiss.read_index(index_file)

with open(metadata_file, 'r', encoding='utf-8') as f:
    metadata = json.load(f)

model = SentenceTransformer(embedding_model_name)
# ------------------------

# ------------------------
# API Setup
async def ping(_: Request):
    return JSONResponse({"message": "UnityGPT Search API is running!"})

async def search(request: Request):
    body = await request.json()
    query = body.get("query", "")
    top_k = int(body.get("top_k", 5))

    embedding = model.encode([query])
    embedding = np.array(embedding).astype('float32')
    D, I = index.search(embedding, top_k)

    results = []
    for idx in I[0]:
        if idx < len(metadata):
            item = metadata[idx]
            results.append({
                "file": item["file"],
                "type": item["type"],
                "name": item["name"],
                "className": item.get("class", ""),
                "content": item["content"],
            })

    return JSONResponse(results)

app = Starlette(routes=[
    Route("/ping", ping, methods=["GET"]),
    Route("/search", search, methods=["POST"]),
])

# -MCP-----------------------

# class ExecuteRequest(BaseModel):
#     name: str
#     parameters: dict

# with open("./UnityGptActions.json") as f:
#     unity_actions = json.load(f)

# @app.get("/actions/describe")
# def describe_actions():
#     return unity_actions
# 
# @app.post("/actions/execute")
# async def execute_action(req: ExecuteRequest):
#     result = run_unity_action(req.name, req.parameters)
#     return {"result": result}

# ------------------------

# Start the server
if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()
    uvicorn.run(app, host=args.host, port=args.port)
# ------------------------
