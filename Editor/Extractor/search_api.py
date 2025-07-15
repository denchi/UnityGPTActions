from fastapi import FastAPI
from pydantic import BaseModel
from typing import List
from sentence_transformers import SentenceTransformer
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
app = FastAPI()

class SearchRequest(BaseModel):
    query: str
    top_k: int = 5

class SearchResult(BaseModel):
    file: str
    type: str
    name: str
    content: str

# simple get request returning true
@app.get("/ping", tags=["Health Check"])
def read_root():
    return {"message": "UnityGPT Search API is running!"}

@app.post("/search", response_model=List[SearchResult])
def search(request: SearchRequest):
    embedding = model.encode([request.query])
    embedding = np.array(embedding).astype('float32')
    D, I = index.search(embedding, request.top_k)

    results = []
    for idx in I[0]:
        if idx < len(metadata):
            item = metadata[idx]
            results.append(SearchResult(
                file=item["file"],
                type=item["type"],
                name=item["name"],
                content=item["content"]
            ))

    return results

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
    uvicorn.run(app, host="127.0.0.1", port=8000)
# ------------------------
