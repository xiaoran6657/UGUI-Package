using Data.Info;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "ClothingItemConfig", menuName = "Inventory/ClothingItemConfig")]
    public class ClothingItemConfig : ScriptableObject
    {
        public SerializableDictionary<int, ClothingItemInfo> items = new();

        public ClothingItemInfo GetItem(int id)
        {
            items.RebuildDictionary();
            items.Dict.TryGetValue(id, out var item);
            return item;
        }

        public bool Contains(int id)
        {
            items.RebuildDictionary();
            return items.Dict.ContainsKey(id);
        }
    }
}