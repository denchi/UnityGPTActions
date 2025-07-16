import os
from extractors.csharp_extractor import extract_csharp_chunks
#from extractors.prefab_scene_extractor import extract_yaml_objects
#from extractors.docs_extractor import extract_markdown_chunks
from utils.path_utils import make_unity_relative
#from build_tree_sitter import build_tree_sitter

SUPPORTED_EXTENSIONS = {
    '.cs': extract_csharp_chunks,
    # '.unity': extract_yaml_objects,
    # '.prefab': extract_yaml_objects,
    # '.md': extract_markdown_chunks,
    # '.txt': extract_markdown_chunks
}


def extract_all(project_root: str):
    all_chunks = []

    for root, dirs, files in os.walk(project_root):
        for file_name in files:
            file_path = os.path.join(root, file_name)
            _, ext = os.path.splitext(file_name.lower())

            extractor = SUPPORTED_EXTENSIONS.get(ext)
            if extractor:
                print(f"Extracting from {file_path}...")
                try:
                    chunks = extractor(file_path)
                    for chunk in chunks:
                        # Ensure the file path starts with Assets/
                        chunk["file"] = make_unity_relative(file_path)
                    all_chunks.extend(chunks)
                except Exception as e:
                    print(f"Error extracting from {file_path}: {e}")

    return all_chunks


if __name__ == "__main__":
    import argparse
    import json

    parser = argparse.ArgumentParser(description="Extract project data for deep search.")
    parser.add_argument("--project", required=True, help="Path to the Unity project Assets folder.")
    parser.add_argument("--out", default="./data/processed/chunks.json", help="Output JSON path for chunks.")

    args = parser.parse_args()

    #build_tree_sitter()  # Ensure tree-sitter is built
    
    chunks = extract_all(args.project)

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, 'w', encoding='utf-8') as f:
        json.dump(chunks, f, indent=2, ensure_ascii=False)

    print(f"Extraction complete. {len(chunks)} chunks saved to {args.out}")
