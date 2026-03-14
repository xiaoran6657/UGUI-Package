using Data.Info;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "VehicleItemConfig", menuName = "Inventory/VehicleItemConfig")]
    public class VehicleItemConfig : ScriptableObject
    {
        public SerializableDictionary<int, VehicleItemInfo> items = new();

        public VehicleItemInfo GetItem(int id)
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