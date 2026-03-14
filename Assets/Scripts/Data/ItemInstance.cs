using System;
using Data.Info;
using Manager;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// 运行时物品实例（用于背包中的实际物品）
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public int itemId;
        public int count;
        public string uniqueId;
        
        // 
        [NonSerialized] public ItemInfo cachedItemInfo;
        [NonSerialized] private bool _isCacheDirty = true;
        
        #region 构造函数

        public ItemInstance()
        {
            itemId = 0;
            count = 0;
            uniqueId = Guid.NewGuid().ToString();
        }

        public ItemInstance(int itemId, int count = 1)
        {
            this.itemId = itemId;
            this.count = count;
            this.uniqueId = Guid.NewGuid().ToString();
            _isCacheDirty = true;
        }
        
        #endregion
        
        #region 数据获取

        /// <summary>
        /// 获取物品信息基类（自动缓存）
        /// </summary>
        /// <returns></returns>
        public ItemInfo GetItemInfo()
        {
            if (!_isCacheDirty && cachedItemInfo != null) return cachedItemInfo;
            
            cachedItemInfo = ItemDataManager.Instance.GetItemInfoById(itemId);
            _isCacheDirty = false;
            return cachedItemInfo;
        }

        /// <summary>
        /// 获取特定类型的物品信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetItemInfo<T>() where T : ItemInfo
        {
            return ItemDataManager.Instance.GetItem<T>(itemId);
        }

        public void RefreshCache()
        {
            _isCacheDirty = true;
            cachedItemInfo = null;
            GetItemInfo();
        }

        #endregion
        
        #region 便捷属性获取

        public string Name => GetItemInfo()?.itemName ?? "未知物品";
        public Sprite Icon => GetItemInfo()?.icon;
        public int Price => GetItemInfo()?.price ?? 0;
        public int MaxStack => GetItemInfo()?.maxStack ?? 1;
        public bool CanStack => GetItemInfo()?.CanStack ?? false;
        public int Rarity => GetItemInfo()?.rarity ?? 1;
        public Color RarityColor => GetItemInfo()?.GetRarityColor() ?? Color.white;
        public string Description => GetItemInfo()?.description ?? "";
        public bool CanSell => GetItemInfo()?.canSell ?? false;

        #endregion
        
        #region 堆叠逻辑
        
        /// <summary>
        /// 尝试堆叠物品
        /// </summary>
        /// <param name="addCount">要添加的数量</param>
        /// <returns>实际堆叠的数量</returns>
        public int TryStack(int addCount)
        {
            // 判断能否堆叠
            if (!CanStack) return 0;

            // 判断该格子是否有剩余
            var canAdd = MaxStack - count;
            if (canAdd <= 0) return 0;

            // 计算该格子实际增加数量，并更新 count
            var actualAdd = Mathf.Min(addCount, canAdd);
            count += actualAdd;
            return actualAdd;
        }

        /// <summary>
        /// 检查是否可以与另一个实例堆叠
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CanStackWith(ItemInstance other)
        {
            // 不是同类物品 or 该类物品不能堆叠
            if (other == null || !CanStack || other.itemId != itemId) return false;
            return count < MaxStack;
        }

        public bool CanStackById(int id)
        {
            if (!CanStack || id != itemId) return false;
            return count < MaxStack;
        }
        
        #endregion
        
        #region 物品操作

        /// <summary>
        /// 消耗物品
        /// </summary>
        /// <param name="consumeCount">消耗数量</param>
        /// <returns>是否成功消耗</returns>
        public bool Consume(int consumeCount = 1)
        {
            if (count < consumeCount) return false;
            count -= consumeCount;
            return true;
        }

        /// <summary>
        /// 拆分物品
        /// </summary>
        /// <param name="splitCount">拆分数量</param>
        /// <returns>新的物品实例，失败返回null</returns>
        public ItemInstance Split(int splitCount)
        {
            if (splitCount <= 0 || splitCount > count || !CanStack) return null;

            count -= splitCount;
            return new ItemInstance(itemId, splitCount);
        }

        /// <summary>
        /// 合并另一个实例到此
        /// </summary>
        /// <param name="other"></param>
        /// <returns>实际合并数量</returns>
        public int Merge(ItemInstance other)
        {
            if (other == null || itemId != other.itemId || !CanStack) return 0;

            int canAdd = MaxStack - count;
            int actualMerge = Mathf.Min(other.count, canAdd);

            count += actualMerge;
            other.count -= actualMerge;
            return actualMerge;
        }
        
        #endregion
        
        #region 类型特定方法

        // 获取特定物品类信息
        public ClothingItemInfo GetClothingInfo() => GetItemInfo<ClothingItemInfo>();
        public ShoeItemInfo GetShoeInfo() => GetItemInfo<ShoeItemInfo>();
        public VehicleItemInfo GetVehicleInfo() => GetItemInfo<VehicleItemInfo>();
        public WeaponItemInfo GetWeaponInfo() => GetItemInfo<WeaponItemInfo>();
        
        // 判断物品类别
        public bool IsClothing() => GetItemInfo() is ClothingItemInfo;
        public bool IsShoe() => GetItemInfo() is ShoeItemInfo;
        public bool IsVehicle() => GetItemInfo() is VehicleItemInfo;
        public bool IsWeapon() => GetItemInfo() is WeaponItemInfo;

        #endregion

        #region 克隆与比较

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns></returns>
        public ItemInstance Clone() => new ItemInstance(itemId, count);

        /// <summary>
        /// 比较是否相同物品
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSameItem(ItemInstance other)
        {
            if (other == null) return false;
            return itemId == other.itemId;
        }
        
        #endregion
    }
}