using Data.Info;
using UnityEngine;

namespace Data.Config
{
    [CreateAssetMenu(fileName = "ShoeItemConfig", menuName = "Inventory/ShoeItemConfig")]
    public class ShoeItemConfig : ScriptableObject
    {
        public SerializableDictionary<int, ShoeItemInfo> items = new();

        public ShoeItemInfo GetItem(int id)
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