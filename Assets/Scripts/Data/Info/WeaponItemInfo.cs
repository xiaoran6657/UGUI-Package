using System;
using UnityEngine;

namespace Data.Info
{
    [Serializable]
    public class WeaponItemInfo : ItemInfo
    {
        [Tooltip("普攻伤害")] public float basicAttackDamage = 0;
        [Tooltip("重击伤害")] public float heavyAttackDamage = 0;
        [Tooltip("暴击系数")] public float criticalHitRatio = 0;
        [Tooltip("暴击概率")] public float criticalHitProbability = 0;
        [Tooltip("耐久度")] public float durability = 0;
    }
}