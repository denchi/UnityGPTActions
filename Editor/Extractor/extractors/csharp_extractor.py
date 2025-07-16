import os
from tree_sitter import Language, Parser

root = os.path.dirname(os.path.abspath(__file__))
so_path = os.path.join(root, 'build', 'my-languages.so')

if not os.path.exists(so_path):
    raise RuntimeError(f"Missing {so_path}. Did you run build_tree_sitter.py?")

CSHARP_LANGUAGE = Language(so_path, 'c_sharp')
parser = Parser()
parser.set_language(CSHARP_LANGUAGE)


def extract_csharp_chunks(file_path):
    chunks = []

    with open(file_path, 'rb') as f:
        source_code = f.read()

    tree = parser.parse(source_code)
    root = tree.root_node
    source_code_str = source_code.decode('utf-8')

    def extract_text(node):
        return source_code[node.start_byte:node.end_byte].decode('utf-8')

    def extract_methods(node, class_name):
        found_chunks = []
        if node.type == "method_declaration":
            method_name = None
            for child in node.children:
                if child.type == "identifier":
                    method_name = extract_text(child)
                    break
            method_content = extract_text(node).strip()
            method_chunks = split_large_method(method_content)
            for idx, chunk_content in enumerate(method_chunks):
                found_chunks.append({
                    "type": "method",
                    "class": class_name,
                    "name": f"{method_name}" if len(method_chunks) == 1 else f"{method_name} (part {idx+1})",
                    "content": chunk_content.strip(),
                    "file": file_path
                })
        for child in node.children:
            found_chunks.extend(extract_methods(child, class_name))
        return found_chunks

    def extract_classes(node):
        for child in node.children:
            if child.type == "class_declaration":
                class_name = None
                for grandchild in child.children:
                    if grandchild.type == "identifier":
                        class_name = extract_text(grandchild)
                        break

                class_content = extract_text(child).strip()
                chunks.append({
                    "type": "class",
                    "name": class_name,
                    "content": class_content,
                    "file": file_path
                })

                chunks.extend(extract_methods(child, class_name))

            extract_classes(child)  # Recurse for nested classes

    extract_classes(root)

    return chunks


def split_large_method(content, max_lines=100):
    lines = content.strip().split('\n')
    if len(lines) <= max_lines:
        return [content]
    blocks = []
    current = []
    for line in lines:
        if len(current) >= max_lines and (line.strip() == "" or "//" in line):
            blocks.append('\n'.join(current))
            current = []
        current.append(line)
    if current:
        blocks.append('\n'.join(current))
    return blocks
