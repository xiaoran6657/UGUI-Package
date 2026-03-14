using UnityEngine;

namespace Test
{
    /// <summary>
    /// 仅支持 RecycledInventoryUI 的运行时测试器（不再兼容旧 InventoryUI）。
    /// 若 Inspector 未指定 recycledInventoryUI，会在 Awake 时自动查找场景中的实例。
    /// 运行时调用 AddItemById_FromInspector / SetSlot_FromInspector / ClearSlot_FromInspector 等方法进行测试。
    /// </summary>
    public class DebugInventoryTester : MonoBehaviour
    {
        [Header("References")]
        public UI.RecycledInventoryUI recycledInventoryUI;

        [Header("Default Test Values")]
        public int testItemId = 202;
        public int testCount = 1;
        public int testSlotIndex = 0;

        private void Awake()
        {
            if (!recycledInventoryUI)
            {
                recycledInventoryUI = FindObjectOfType<UI.RecycledInventoryUI>();
                if (recycledInventoryUI)
                    Debug.Log("[DebugInventoryTester] 自动绑定 RecycledInventoryUI");
                else
                    Debug.LogWarning("[DebugInventoryTester] 未找到 RecycledInventoryUI，请确保已挂载并已 Initialize()");
            }
        }

        /// <summary>
        /// 在 Inspector 中调用：请求添加物品（会调用 RecycledInventoryUI.AddItemById）
        /// </summary>
        public int AddItemById_FromInspector(int itemId, int count)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DebugTester] 请在 Play 模式下调用测试方法");
                return 0;
            }

            if (!recycledInventoryUI)
            {
                Debug.LogWarning("[DebugTester] recycledInventoryUI 未绑定或未找到");
                return 0;
            }

            int added = recycledInventoryUI.AddItemById(itemId, count);
            Debug.Log($"[DebugTester] AddItemById -> 请求 id={itemId} x{count}，实际添加: {added}");
            return added;
        }

        public void ClearSlot_FromInspector(int slotIndex)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DebugTester] 请在 Play 模式下调用测试方法");
                return;
            }

            if (!recycledInventoryUI)
            {
                Debug.LogWarning("[DebugTester] recycledInventoryUI 未绑定或未找到");
                return;
            }

            recycledInventoryUI.ClearSlot(slotIndex);
            Debug.Log($"[DebugTester] ClearSlot -> slot {slotIndex} 已清空");
        }

        public void SetSlot_FromInspector(int slotIndex, int itemId, int count)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DebugTester] 请在 Play 模式下调用测试方法");
                return;
            }

            if (!recycledInventoryUI)
            {
                Debug.LogWarning("[DebugTester] recycledInventoryUI 未绑定或未找到");
                return;
            }

            var inst = new Data.ItemInstance(itemId, count);
            recycledInventoryUI.SetSlot(slotIndex, inst);
            Debug.Log($"[DebugTester] SetSlot -> slot {slotIndex} <- id:{itemId} x{count}");
        }

        // 快捷：执行默认添加
        public void AddDefault()
        {
            AddItemById_FromInspector(testItemId, testCount);
        }
        
        /// <summary>
        /// 对 RecycledInventoryUI 执行按稀有度合并与排序（将满堆叠放在未满堆叠前）
        /// </summary>
        public void SortInventory_FromInspector()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DebugTester] 请在 Play 模式下调用测试方法");
                return;
            }

            if (!recycledInventoryUI)
            {
                Debug.LogWarning("[DebugTester] recycledInventoryUI 未绑定或未找到");
                return;
            }

            var merged = recycledInventoryUI.SortAndCompactByRarity();
            var nonEmpty = recycledInventoryUI.GetBackendList()?.Count;
            Debug.Log($"[DebugTester] SortInventory_FromInspector: mergedCompress={merged}, nonEmptyAfter={nonEmpty}");
        }
    }
}