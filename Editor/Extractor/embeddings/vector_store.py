import faiss
import numpy as np
import json
import os

def save_faiss_index(embeddings, index_file):
    dim = embeddings.shape[1]
    index = faiss.IndexFlatL2(dim)
    index.add(embeddings)
    faiss.write_index(index, index_file)

def load_faiss_index(index_file):
    return faiss.read_index(index_file)

def save_metadata(chunks, metadata_file):
    with open(metadata_file, 'w', encoding='utf-8') as f:
        json.dump(chunks, f, indent=2, ensure_ascii=False)
