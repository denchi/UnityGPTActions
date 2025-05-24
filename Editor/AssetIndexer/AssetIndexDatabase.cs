using System.Collections.Generic;
using UnityEngine;

namespace GptActions.Editor.AssetIndexer
{
    [CreateAssetMenu(fileName = "AssetIndexDatabase", menuName = "Tools/Asset Index Database")]
    public class AssetIndexDatabase : ScriptableObject
    {
        public List<AssetIndexEntry> entries = new();

        public void UpdateOrAddEntry(AssetIndexEntry entry)
        {
            var existing = entries.Find(e => e.guid == entry.guid);
            if (existing != null)
            {
                existing.path = entry.path;
                existing.type = entry.type;
                existing.extension = entry.extension;
            }
            else
            {
                entries.Add(entry);
            }
        }

        public void RemoveByGUID(string guid)
        {
            entries.RemoveAll(e => e.guid == guid);
        }
    }
}