import os
import json
import numpy as np
from sentence_transformers import SentenceTransformer
import faiss

# --------------------------
# Configuration
chunks_file = "./data/processed/chunks.json"
index_file = "./data/embeddings/faiss.index"
metadata_file = "./data/embeddings/chunks_metadata.json"
embedding_model_name = "all-MiniLM-L6-v2"
# --------------------------

# --------------------------
# Load Chunks
with open(chunks_file, 'r', encoding='utf-8') as f:
    chunks = json.load(f)

texts = [chunk["content"] for chunk in chunks]
print(f"Loaded {len(texts)} chunks for embedding.")
# --------------------------

# --------------------------
# Create Embeddings
model = SentenceTransformer(embedding_model_name)
embeddings = model.encode(texts, show_progress_bar=True)
embeddings = np.array(embeddings).astype('float32')
# --------------------------

# --------------------------
# Create FAISS Index
dim = embeddings.shape[1]
index = faiss.IndexFlatL2(dim)
index.add(embeddings)

# Save Index
os.makedirs(os.path.dirname(index_file), exist_ok=True)
faiss.write_index(index, index_file)
print(f"FAISS index saved to: {index_file}")
# --------------------------

# --------------------------
# Save Metadata
with open(metadata_file, 'w', encoding='utf-8') as f:
    json.dump(chunks, f, indent=2, ensure_ascii=False)
print(f"Metadata saved to: {metadata_file}")
# --------------------------
