using System;
using UnityEngine;

namespace Data.Info
{
    [Serializable]
    public class ShoeItemInfo : ItemInfo
    {
        [Tooltip("速度加成")] public float speedBoost = 0;
        [Tooltip("跳跃高度")] public float jumpHeight = 0;
    }
}