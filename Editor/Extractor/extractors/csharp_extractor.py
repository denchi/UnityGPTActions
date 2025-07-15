import os
import re

def extract_csharp_chunks(file_path):
    chunks = []
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Extract classes and methods
    class_matches = re.findall(r'(public|private|protected)?\s*class\s+(\w+)[^{]*{([^}]*)}', content, re.DOTALL)
    for match in class_matches:
        visibility, class_name, body = match
        chunks.append({
            "type": "class",
            "name": class_name,
            "content": f"class {class_name} {{ {body.strip()} }}",
            "file": file_path
        })

    # Optional: extract top-level comments
    comments = re.findall(r'///(.*)', content)
    if comments:
        chunks.append({
            "type": "doc",
            "name": "comments",
            "content": '\n'.join(comments).strip(),
            "file": file_path
        })

    return chunks

def make_unity_relative(path, project_assets_root):
    abs_root = os.path.abspath(project_assets_root)
    abs_path = os.path.abspath(path)
    rel_path = os.path.relpath(abs_path, abs_root)
    return os.path.join("Assets", rel_path.replace(os.sep, "/"))