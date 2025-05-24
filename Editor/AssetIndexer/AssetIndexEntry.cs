using System;

namespace GptActions.Editor.AssetIndexer
{
    [Serializable]
    public class AssetIndexEntry
    {
        public string guid;
        public string path;
        public string type;
        public string extension;
    }
}