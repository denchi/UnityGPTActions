import os
import argparse
from tree_sitter import Language


def build_tree_sitter(data_path):
    tree_sitter_dir = os.path.join(data_path, 'tree_sitter')
    os.makedirs(tree_sitter_dir, exist_ok=True)

    grammar_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'tree-sitter-c-sharp'))
    output_file = os.path.join(tree_sitter_dir, 'my-languages.so')

    print(f"Building Tree-sitter .so to: {output_file}")
    print(f"Using grammar at: {grammar_dir}")

    Language.build_library(
        output_file,
        [grammar_dir]
    )

    print("✅ Tree-sitter my-languages.so built successfully at ", output_file)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Build tree-sitter C# library")
    parser.add_argument("--data", required=True, help="Path to the data directory")
    args = parser.parse_args()

    build_tree_sitter(args.data)
