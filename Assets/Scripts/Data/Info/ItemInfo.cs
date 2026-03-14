using System;
using UnityEngine;

namespace Data.Info
{
    /// <summary>
    /// 物品类型
    /// </summary>
    public enum ItemType
    {
        Clothing,      // 衣服
        Shoe,          // 鞋
        Weapon,        // 武器
        Vehicle,       // 载具（自行车、电瓶车、摩托车、车）
        Other          // 其他
    }
    
    /// <summary>
    /// 物品信息基类，包含基本信息如名称、价格、Icon
    /// </summary>
    [Serializable]
    public abstract class ItemInfo
    {
        [Tooltip("物品名称")]
        public string itemName;
        
        [Tooltip("物品描述")]
        [TextArea(3, 5)]
        public string description;
    
        [Tooltip("物品图标")]
        public Sprite icon;
    
        [Tooltip("物品价格（金币）")]
        public int price;
    
        [Tooltip("能否出售")]
        public bool canSell = true;
    
        [Tooltip("最大堆叠数量")]
        public int maxStack = 9;
    
        [Tooltip("能否堆叠")]
        public bool CanStack => maxStack > 1;
        
        [Range(1,5)]
        public int rarity = 5;

        // 根据稀有度返回物品背景颜色
        public Color GetRarityColor()
        {
            return rarity switch
            {
                1 => Color.white,
                2 => Color.green,
                3 => Color.yellow,
                4 => Color.magenta,
                5 => Color.red,
                _ => Color.white
            };
        }
    }
}