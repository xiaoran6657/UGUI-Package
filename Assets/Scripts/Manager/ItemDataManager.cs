using System.Collections.Generic;
using Data;
using Data.Config;
using Data.Info;
using UnityEngine;

namespace Manager
{
    public class ItemDataManager : MonoBehaviour
    {
        public static ItemDataManager Instance { get; private set; }
        
        [Header("物品配置文件")]
        [Tooltip("衣服配置")] public ClothingItemConfig clothingItemConfig;
        [Tooltip("武器配置")] public WeaponItemConfig weaponItemConfig;
        [Tooltip("载具配置")] public VehicleItemConfig vehicleItemConfig;
        [Tooltip("鞋子配置")] public ShoeItemConfig shoeItemConfig;
        
        // 运行时缓存的字典
        public SerializableDictionary<int, ClothingItemInfo> ClothingDict => clothingItemConfig?.items;
        public SerializableDictionary<int, WeaponItemInfo> WeaponDict => weaponItemConfig?.items;
        public SerializableDictionary<int, VehicleItemInfo> VehicleDict => vehicleItemConfig?.items;
        public SerializableDictionary<int, ShoeItemInfo> ShoeDict => shoeItemConfig?.items;
    
        // 全局ID映射（用于通过任意ID查找）
        private System.Collections.Generic.Dictionary<int, ItemInfo> _allItemsDict = new();

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitAllItemsDict();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化全局字典
        /// </summary>
        private void InitAllItemsDict()
        {
            _allItemsDict.Clear();

            if (clothingItemConfig)
            {
                clothingItemConfig.items.RebuildDictionary();
                foreach (var kvp in clothingItemConfig.items.Dict)
                {
                    _allItemsDict.Add(kvp.Key, kvp.Value);
                }
            }
            
            if (weaponItemConfig)
            {
                weaponItemConfig.items.RebuildDictionary();
                foreach (var kvp in weaponItemConfig.items.Dict)
                {
                    _allItemsDict.Add(kvp.Key, kvp.Value);
                }
            }
            
            if (vehicleItemConfig)
            {
                vehicleItemConfig.items.RebuildDictionary();
                foreach (var kvp in vehicleItemConfig.items.Dict)
                {
                    _allItemsDict.Add(kvp.Key, kvp.Value);
                }
            }
            
            if (shoeItemConfig)
            {
                shoeItemConfig.items.RebuildDictionary();
                foreach (var kvp in shoeItemConfig.items.Dict)
                {
                    _allItemsDict.Add(kvp.Key, kvp.Value);
                }
            }
            
            Debug.Log($"[ItemDataManager] 初始化完成 | 总物品数: {_allItemsDict.Count}");
        }
        
        #region 获取物品

        public ClothingItemInfo GetClothingItemInfo(int id) => clothingItemConfig?.GetItem(id);
        public WeaponItemInfo GetWeaponItemInfo(int id) => weaponItemConfig?.GetItem(id);
        public VehicleItemInfo GetVehicleItemInfo(int id) => vehicleItemConfig?.GetItem(id);
        public ShoeItemInfo GetShoeItemInfo(int id) => shoeItemConfig?.GetItem(id);
        
        public ItemInfo GetItemInfoById(int id) => _allItemsDict.GetValueOrDefault(id);

        public T GetItem<T>(int id) where T : ItemInfo
        {
            var item = GetItemInfoById(id);
            if (item is T typedItem)
            {
                return typedItem;
            }
            Debug.LogWarning($"物品ID {id} 不是类型 {typeof(T).Name}");
            return null;
        }

        #endregion
        
        #region 检查物品
        
        public bool HasClothingItem(int id) => clothingItemConfig?.Contains(id) ?? false;
        public bool HasWeaponItem(int id) => weaponItemConfig?.Contains(id) ?? false;
        public bool HasVehicleItem(int id) => vehicleItemConfig?.Contains(id) ?? false;
        public bool HasShoeItem(int id) => shoeItemConfig?.Contains(id) ?? false;
        
        #endregion

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void Reload()
        {
            InitAllItemsDict();
        }
    }
}