import os
from tree_sitter import Language

# Get absolute path to this script's directory
root = os.path.dirname(os.path.abspath(__file__))

# Correctly resolve grammar directory absolutely
grammar_dir = os.path.abspath(os.path.join(root, '..', 'tree-sitter-c-sharp'))
build_dir = os.path.join(root, 'build')
output_file = os.path.join(build_dir, 'my-languages.so')

os.makedirs(build_dir, exist_ok=True)

print(f"Building Tree-sitter .so to: {output_file}")
print(f"Using grammar at: {grammar_dir}")

if not os.path.exists(grammar_dir):
    raise RuntimeError(f"Grammar path not found: {grammar_dir}")

Language.build_library(
    output_file,
    [grammar_dir]
)

print("✅ Tree-sitter my-languages.so built successfully.")
