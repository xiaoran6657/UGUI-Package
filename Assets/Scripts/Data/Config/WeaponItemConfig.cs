using Data.Info;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "WeaponItemConfig", menuName = "Inventory/WeaponItemConfig")]
    public class WeaponItemConfig : ScriptableObject
    {
        public SerializableDictionary<int, WeaponItemInfo> items = new();

        public WeaponItemInfo GetItem(int id)
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