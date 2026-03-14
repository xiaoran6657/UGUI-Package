using System.Collections.Generic;
using Data;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 背包UI管理器
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI References")] 
        [SerializeField] private RectTransform content;  // ScrollView -> Viewport -> Content
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private ScrollRect scrollRect;  // ScrollView -> Scrollbar Vertical

        [Header("Settings")]
        public int initialSlots = 8;   // 初始创建的格子数
        public int maxCapacity = 128;  // 背包最大容量
        public bool autoCreateOnAwake = true;  // 是否Awake 时初始化格子
        public bool keepScrollAtTopAfterAdd = true;  // 添加物品后是否把Scroll定位到顶部
        
        // 运行时数据
        private readonly List<GameObject> _runtimeSlots = new();
        private readonly List<ItemInstance> _slotRecords = new();
        
        // 事件：外部订阅 (index, itemId, count)
        public System.Action<int, int, int> OnSlotClicked;

        private void Awake()
        {
            if (autoCreateOnAwake) BuildEmptySlots(Mathf.Min(initialSlots, maxCapacity));
        }
        
        #region Build / Clear

        public void BuildEmptySlots(int count)
        {
            if (count < 0) return;

            var canCreate = Mathf.Min(count, maxCapacity - _runtimeSlots.Count);
            for (var i = 0; i < canCreate; i++) CreateSlot();

            ForceRebuildLayout();
        }

        /// <summary>
        /// 创建一个槽
        /// </summary>
        /// <returns>索引</returns>
        private int CreateSlot()
        {
            if (_runtimeSlots.Count >= maxCapacity)
            {
                Debug.LogWarning("[InventoryUI] 已达到最大容量，无法创建更多槽");
                return -1;
            }

            var go = Instantiate(slotPrefab, content, false);
            go.transform.localScale = Vector3.one;
            var index = _runtimeSlots.Count;
            _runtimeSlots.Add(go);

            var slotUI = go.GetComponent<ItemSlotUI>();
            if (slotUI)
            {
                slotUI.Initialize(index);
                slotUI.OnSlotClicked += HandleSlotClick;
                slotUI.OnSlotRightClicked += HandleSlotRightClick;
            }
            
            _slotRecords.Add(null);
            return index;
        }

        /// <summary>
        /// 清空_runtimeSlots和_slotRecords，并且取消订阅
        /// </summary>
        public void ClearAllSlots()
        {
            // 
            foreach (var go in _runtimeSlots)
            {
                if (!go) continue;
                var slotUI = go.GetComponent<ItemSlotUI>();
                if (slotUI)
                {
                    slotUI.OnSlotClicked -= HandleSlotClick;
                    slotUI.OnSlotRightClicked -= HandleSlotRightClick;
                }
                Destroy(go);
            }
            
            _runtimeSlots.Clear();
            _slotRecords.Clear();
        }
        
        #endregion
        
        #region API - 添加 / 设置 / 移除 / 交换 / 查询

        /// <summary>
        /// 添加物品
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int AddItemById(int itemId, int count)
        {
            if (count <= 0) return 0;

            var info = ItemDataManager.Instance?.GetItemInfoById(itemId);
            if (info == null) return 0;

            var remaining = count;
            var added = 0;
            
            // 尝试堆叠到已有同类物品
            for (var i = 0; i < _slotRecords.Count && remaining > 0; i++)
            {
                var inst = _slotRecords[i];
                if (inst == null || inst.itemId != itemId || !inst.CanStackById(inst.itemId)) continue;
                
                var actuallyAdded = inst.TryStack(remaining);
                if (actuallyAdded > 0)
                {
                    remaining -= actuallyAdded;
                    added += actuallyAdded;
                    UpdateSlotView(i);
                }
            }
            
            // 使用空槽填入，若无空槽则尝试新建再放入
            while (remaining > 0)
            {
                var emptyIndex = _slotRecords.FindIndex(x => x == null);
                if (emptyIndex == -1)
                {
                    // 没有空槽则新建
                    var newIndex = CreateSlot();
                    if (newIndex == -1) break;  // 已达到最大容量
                    emptyIndex = newIndex;
                }

                var put = Mathf.Min(remaining, info.maxStack);
                var newInst = new ItemInstance(itemId, put);
                _slotRecords[emptyIndex] = newInst;
                added += put;
                remaining -= put;
                UpdateSlotView(emptyIndex);
            }
            
            //
            if (added > 0)
            {
                if (keepScrollAtTopAfterAdd && scrollRect) scrollRect.verticalNormalizedPosition = 1f;
                ForceRebuildLayout();
            }

            return added;
        }
        
        public void SetSlot(int slotIndex, ItemInstance instance)
        {
            if (!IsValidIndex(slotIndex)) return;
            _slotRecords[slotIndex] = instance;
            UpdateSlotView(slotIndex);
        }

        public void ClearSlot(int slotIndex)
        {
            if (!IsValidIndex(slotIndex)) return;
            _slotRecords[slotIndex] = null;
            UpdateSlotView(slotIndex);
        }
        
        #endregion
        
        #region Helpers - 更新 UI

        private void UpdateSlotView(int index)
        {
            if (!IsValidIndex(index)) return;
            
            var go = _runtimeSlots[index];
            if (!go) return;

            var slotUI = go.GetComponent<ItemSlotUI>();
            var inst = _slotRecords[index];
            if (inst == null)
            {
                slotUI?.ClearSlot();
                return;
            }
            
            slotUI?.SetItem(inst);
        }

        private void UpdateAllViews()
        {
            for (var i=0; i<_runtimeSlots.Count; i++)
                UpdateSlotView(i);
        }
        
        #endregion
        
        #region Events

        private void HandleSlotClick(int index)
        {
            Debug.Log($"[UI] Slot {index} clicked");
            var inst = _slotRecords[index];
            OnSlotClicked?.Invoke(index, inst?.itemId ?? -1, inst?.count ?? 0);
        }

        private void HandleSlotRightClick(int index)
        {
            var inst = _slotRecords[index];
            Debug.Log("RightClick: " + inst.itemId);
        }
        
        #endregion
        
        #region Util

        private bool IsValidIndex(int index) => index >= 0 && index < _slotRecords.Count;

        private void ForceRebuildLayout()
        {
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        public void HandleDrop(int srcIndex, int dstIndex)
        {
            // 
            if (!IsValidIndex(srcIndex)) return;

            // 
            if (dstIndex < 0 || dstIndex >= _slotRecords.Count || srcIndex == dstIndex)
            {
                UpdateSlotView(srcIndex);
                return;
            }
            
            var src = _slotRecords[srcIndex];
            var dst = _slotRecords[dstIndex];

            // 
            if (src == null)
            {
                UpdateSlotView(srcIndex);
                UpdateSlotView(dstIndex);
                return;
            }
            
            // case1: 目标为空 -> 直接移动
            if (dst == null)
            {
                _slotRecords[dstIndex] = src;
                _slotRecords[srcIndex] = null;
                UpdateSlotView(dstIndex);
                UpdateSlotView(srcIndex);
                return;
            }

            // case2: 同类物品并可堆叠 -> 合并
            if (dst.itemId == src.itemId && src.CanStackWith(dst))
            {
                var canAdd = dst.MaxStack - dst.count;
                if (canAdd > 0)
                {
                    // dst剩余，可堆叠
                    var transfer = Mathf.Min(canAdd, src.count);
                    dst.count += transfer;
                    src.count -= transfer;
                    
                    // 如果 src 用尽，则清空
                    if (src.count <= 0) _slotRecords[srcIndex] = null;
                    
                    UpdateSlotView(dstIndex);
                    UpdateSlotView(srcIndex);
                }
                else
                {
                    // dst已满，无法合并 -> 交换
                    (_slotRecords[srcIndex], _slotRecords[dstIndex]) = (_slotRecords[dstIndex], _slotRecords[srcIndex]);
                    UpdateSlotView(srcIndex);
                    UpdateSlotView(dstIndex);
                }

                return;
            }
            
            // case3: 不同类物品 -> 交换
            (_slotRecords[srcIndex], _slotRecords[dstIndex]) = (_slotRecords[dstIndex], _slotRecords[srcIndex]);
            UpdateSlotView(srcIndex);
            UpdateSlotView(dstIndex);
        }

        #endregion
    }
}