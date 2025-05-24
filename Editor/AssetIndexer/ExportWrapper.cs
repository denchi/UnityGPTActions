using System.Collections.Generic;

namespace GptActions.Editor.AssetIndexer
{
    [System.Serializable]
    public class ExportWrapper
    {
        public List<AssetIndexEntry> entries;
        public ExportWrapper(List<AssetIndexEntry> e) { entries = e; }
    }
}