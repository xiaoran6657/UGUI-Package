using System;
using UnityEngine;

namespace Data.Info
{
    [Serializable]
    public class ClothingItemInfo : ItemInfo
    {
        [Tooltip("防御值")] public float defense = 0;
        [Tooltip("耐久度")] public float durability = 0;
    }
}