using System;
using UnityEngine;

namespace Data.Info
{
    [Serializable]
    public class VehicleItemInfo : ItemInfo
    {
        [Tooltip("速度")] public float speed = 0;
        [Tooltip("每秒能量消耗")] public float energyConsumptionPerSecond = 0;
        [Tooltip("最大能量存储")] public float maximumEnergyStorage = 0;
        [Tooltip("耐久度")] public float durability = 0;
    }
}