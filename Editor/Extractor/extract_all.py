import os
import argparse
import json
from extractors.csharp_extractor import extract_csharp_chunks
from utils.path_utils import make_unity_relative


def extract_all(project_root: str, data_path: str):
    all_chunks = []

    for root, _, files in os.walk(project_root):
        for file_name in files:
            file_path = os.path.join(root, file_name)
            _, ext = os.path.splitext(file_name.lower())

            if ext == ".cs":
                print(f"Extracting from {file_path}...")
                try:
                    chunks = extract_csharp_chunks(file_path, data_path)
                    for chunk in chunks:
                        chunk["file"] = make_unity_relative(file_path)
                    all_chunks.extend(chunks)
                except Exception as e:
                    print(f"Error extracting from {file_path}: {e}")

    return all_chunks


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Extract project data for deep search.")
    parser.add_argument("--project", required=True, help="Path to the Unity project Assets folder.")
    parser.add_argument("--out", default="./data/processed/chunks.json", help="Output JSON path for chunks.")
    parser.add_argument("--data", required=True, help="Path to the data directory for tree-sitter .so")

    args = parser.parse_args()
    chunks = extract_all(args.project, args.data)

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, 'w', encoding='utf-8') as f:
        json.dump(chunks, f, indent=2, ensure_ascii=False)

    print(f"Extraction complete. {len(chunks)} chunks saved to {args.out}")
