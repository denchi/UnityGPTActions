def make_unity_relative(path):
    """Convert an absolute path to a Unity-friendly relative path starting from 'Assets/'."""
    parts = path.replace("\\", "/").split("/")
    if "Assets" in parts:
        idx = len(parts) - 1 - parts[::-1].index("Assets")
        return "/".join(parts[idx:])
    return path  # fallback, unlikely to happen