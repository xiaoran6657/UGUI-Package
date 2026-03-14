using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Manager;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 回收/虚拟化的背包 UI：只创建可见区 + buffer 的槽（slot views），
    /// 并在滚动时重定位/更新它们来显示不同索引的数据。
    /// 依赖：slotPrefab 上有 SlotView 组件。
    /// </summary>
    public class RecycledInventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform content;       // ScrollView -> Viewport -> Content
        [SerializeField] private GameObject slotPrefab;       // Must contain SlotView component
        [SerializeField] private SelectiveScrollRect scrollRect;

        [Header("Layout")]
        [Tooltip("初始化格子数")] [SerializeField] private int initCapacity = 16;
        [Tooltip("总格子数")] [SerializeField] private int totalCapacity = 128;
        [Tooltip("额外缓冲的行数")] [SerializeField] private int buffer = 4;
        [Tooltip("单个槽宽度（px）")] [SerializeField] private float slotWidth = 120f;
        [Tooltip("单个槽高度（px）")] [SerializeField] private float slotHeight = 130f;
        [Tooltip("横向间距（px）")] [SerializeField] private float spacingX = 16f;
        [Tooltip("纵向间距（px）")] [SerializeField] private float spacingY = 17f;
        [Tooltip("左内边距")] [SerializeField] private float paddingLeft = 6f;
        [Tooltip("上内边距")] [SerializeField] private float paddingTop = 6f;
        [Tooltip("固定列数（<=0 则自动计算列数）")] [SerializeField] private int columns = -1;
        
        // runtime
        private int _totalSlots;
        private int _visibleCount;
        private int _poolSize;
        private readonly List<GameObject> _pool = new();
        private readonly List<SlotView> _poolViews = new();
        
        // 缓存自动布局信息
        private int _colsCached = 1;
        private int _rowsTotalCached = 1;
        private int _rowsVisibleCached = 1;
        private float _viewportWidthCached;
        private float _viewportHeightCached;
        private int _lastColumnsField = int.MinValue;
        private bool _layoutDirty = true;  // 标记是否需要重新计算布局

        // 后端数据引用；长度应等于 totalSlots
        private List<ItemInstance> _backendRecords;

        private void Awake()
        {
            var backend = new List<ItemInstance>(initCapacity);
            for (var i=0; i < initCapacity; i++) backend.Add(null); // 全部空槽
            Initialize(backend, totalCapacity);
        }
        
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="records"></param>
        /// <param name="maxCapacity"></param>
        private void Initialize(List<ItemInstance> records, int maxCapacity)
        {
            if (!content) Debug.LogError("RecycledInventoryUI: content is null");
            if (!slotPrefab) Debug.LogError("RecycledInventoryUI: slotPrefab is null");
            if (!scrollRect) Debug.LogError("RecycledInventoryUI: scrollRect is null");

            // store / validate backend list
            _backendRecords = records ?? new List<ItemInstance>();
            
            // determine total slots: choose the larger one
            _totalSlots = Mathf.Max(maxCapacity, _backendRecords.Count);

            // pad backendRecords to match total slots (so backend.Count == _totalSlots)
            if (_backendRecords.Count < _totalSlots)
                for (var i = 0; i < _totalSlots - _backendRecords.Count; i++) 
                    _backendRecords.Add(null);

            // compute initial layout and build pool (this will set cached fields)
            RecalculateLayout(forceRebuildPool: true);
            
            // (re)bind scroll callback safely
            scrollRect.onValueChanged.RemoveListener(OnScroll);
            scrollRect.onValueChanged.AddListener(OnScroll);

            // initial fill
            UpdateVisible();
        }

        private void OnDestroy()
        {
            if (scrollRect) scrollRect.onValueChanged.RemoveListener(OnScroll);
        }
        private void OnScroll(Vector2 value)
        {
            UpdateVisible();
        }

        /// <summary>
        /// 更新可见池
        /// </summary>
        private void UpdateVisible()
        {
            if (_totalSlots == 0) return;
            if (!content || !scrollRect) return;

            // 获取当前 viewport 尺寸并检查是否发生变化或 columns 字段更改
            var viewportWidth = scrollRect.viewport ? scrollRect.viewport.rect.width : content.rect.width;
            var viewportHeight = scrollRect.viewport ? scrollRect.viewport.rect.height : ((RectTransform)scrollRect.transform).rect.height;

            var columnsChanged = (_lastColumnsField != columns);
            var viewportChanged = !Mathf.Approximately(viewportWidth, _viewportWidthCached) || !Mathf.Approximately(viewportHeight, _viewportHeightCached);

            if (_layoutDirty || columnsChanged || viewportChanged)
            {
                // 重新计算布局（如果需要会重建 pool）
                RecalculateLayout(forceRebuildPool: false);
                _layoutDirty = false;
            }

            // 现在使用缓存值来计算 startIndex 和摆放 pool 项
            var cols = _colsCached;
            var rowsTotal = _rowsTotalCached;

            // 计算 startRow (根据 verticalNormalizedPosition)，然后转换成 startIndex
            var normalized = scrollRect.verticalNormalizedPosition;
            var rowsVisible = _rowsVisibleCached;
            var maxStartRow = Mathf.Max(0, rowsTotal - rowsVisible);
            var startRow = Mathf.RoundToInt((1f - normalized) * maxStartRow);
            startRow = Mathf.Clamp(startRow, 0, maxStartRow);
            var startIndex = startRow * cols;

            // 绑定/摆放 pool 项
            for (var i = 0; i < _poolSize; i++)
            {
                var index = startIndex + i;
                var go = _pool[i];
                if (index >= _totalSlots)
                {
                    if (go.activeSelf) go.SetActive(false);
                    continue;
                }

                if (!go.activeSelf) go.SetActive(true);

                var rt = go.GetComponent<RectTransform>();

                var row = index / cols;
                var col = index % cols;

                // 计算坐标（以 content 左上角为 origin）
                var x = paddingLeft + col * (slotWidth + spacingX);
                var y = -(paddingTop + row * (slotHeight + spacingY));

                rt.sizeDelta = new Vector2(slotWidth, slotHeight);
                rt.anchoredPosition = new Vector2(x, y);

                var view = _poolViews[i];
                view.SetDisplayIndex(index);

                ItemInstance inst = null;
                if (_backendRecords != null && index < _backendRecords.Count) inst = _backendRecords[index];
                view.Bind(inst);
            }

            // 更新缓存的 viewport 大小（用于后续比较）
            _viewportWidthCached = viewportWidth;
            _viewportHeightCached = viewportHeight;
            _lastColumnsField = columns;
        }
        
        #region Backend helpers

        public ItemInstance GetBackendAt(int index)
        {
            if (index < 0 || index >= _backendRecords.Count) return null;
            return _backendRecords[index];
        }

        private void SetBackendAt(int index, ItemInstance inst)
        {
            if (index < 0 || index >= _backendRecords.Count) return;
            _backendRecords[index] = inst;
            UpdateVisible();
        }
        
        /// <summary>
        /// 处理物品拖拽
        /// </summary>
        /// <param name="srcIndex">源格子索引</param>
        /// <param name="dstIndex">目标格子索引</param>
        public void HandleDrop(int srcIndex, int dstIndex)
        {
            Debug.Log($"RecycledInventoryUI.HandleDrop src={srcIndex} dst={dstIndex}");
            if (srcIndex < 0 || srcIndex >= _backendRecords.Count) return;
            
            if (dstIndex < 0 || dstIndex >= _backendRecords.Count || srcIndex == dstIndex)
            {
                UpdateVisible();
                return;
            }
            
            var src = _backendRecords[srcIndex];
            var dst = _backendRecords[dstIndex];

            if (src == null)
            {
                UpdateVisible();
                return;
            }

            // case1: target empty -> move
            if (dst == null)
            {
                _backendRecords[dstIndex] = src;
                _backendRecords[srcIndex] = null;
                UpdateVisible();
                return;
            }
            
            // case2: same item and can stack -> merge
            if (dst.itemId == src.itemId && src.CanStackWith(dst))
            {
                var canAdd = dst.MaxStack - dst.count;
                if (canAdd > 0)
                {
                    var transfer = Mathf.Min(canAdd, src.count);
                    dst.count += transfer;
                    src.count -= transfer;

                    if (src.count <= 0) _backendRecords[srcIndex] = null;
                }
                else
                {
                    // dst full -> swap
                    _backendRecords[srcIndex] = dst;
                    _backendRecords[dstIndex] = src;
                }

                UpdateVisible();
                return;
            }
            
            // case3: different -> swap
            _backendRecords[srcIndex] = dst;
            _backendRecords[dstIndex] = src;
            UpdateVisible();
        }
        
        #endregion
        
        #region Public API for external callers (tester / managers)

        /// <summary>
        /// 返回后端列表的只读引用（可直接读取长度与元素）。
        /// 注意：返回的 List 仍是内部对象，外部修改应通过 SetSlot/ClearSlot/AddItemById 来保证 UI 同步。
        /// </summary>
        public List<ItemInstance> GetBackendList() => _backendRecords;

        /// <summary>
        /// 背包容量（总槽位数）
        /// </summary>
        public int Capacity => _totalSlots;

        /// <summary>
        /// 将 itemId x count 尝试添加到背包（先尝试堆叠到已有项，再填入空槽）。
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">新增物品数量</param>
        /// <returns>实际添加数量</returns>
        public int AddItemById(int itemId, int count)
        {
            if (count <= 0) return 0;
            
            // lazy init if needed (calls Initialize which will pad backend to total slots)
            if (_backendRecords == null)
            {
                var backend = new List<ItemInstance>(initCapacity);
                for (var i = 0; i < initCapacity; i++) backend.Add(null); // 全部空槽
                Initialize(backend, totalCapacity);
            }

            if (_backendRecords == null)
            {
                Debug.LogWarning("[RecycledInventoryUI] AddItemById: 尝试初始化 _backendRecords 失败");
                return 0;
            }

            var info = ItemDataManager.Instance?.GetItemInfoById(itemId);
            if (info == null)
            {
                Debug.LogWarning($"[RecycledInventoryUI] AddItemById: 物品 id={itemId} 未找到");
                return 0;
            }

            var remaining = count;
            var added = 0;
            //var capacityLocal = _backendRecords.Count;

            // 1) 尝试堆叠到已有相同物品
            for (var i = 0; i < _backendRecords.Count && remaining > 0; i++)
            {
                var inst = _backendRecords[i];
                if (inst == null) continue;
                if (!inst.CanStackById(itemId)) continue;

                var actuallyAdded = inst.TryStack(remaining);
                if (actuallyAdded <= 0) continue;
                
                remaining -= actuallyAdded;
                added += actuallyAdded;
                SetBackendAt(i, inst); // 写回并刷新视图
                
                Debug.Log($"[RecycledInventoryUI] AddItemById: 堆叠到已有物品 idx={i} add={actuallyAdded}");
            }

            // 2) 使用空槽填入，若无空槽则扩容后端并同步 UI
            while (remaining > 0)
            {
                // 找第一个空槽
                var emptyIndex = _backendRecords.FindIndex(x => x == null);
                
                if (emptyIndex == -1)
                {
                    // 扩容策略：至少扩 initCapacity 或当前长度（翻倍行为）
                    var toAdd = Mathf.Max(initCapacity, _backendRecords.Count);
                    for (var i = 0; i < toAdd; i++) _backendRecords.Add(null);
                    
                    /*
                    // 同步 UI 总槽数与 content 高度
                    _totalSlots = _backendRecords.Count;
                    if (content)
                    {
                        var cd = content.sizeDelta;
                        cd.y = totalCapacity * slotHeight;
                        content.sizeDelta = cd;
                    }
                    
                    // 如果扩容后 poolSize 仍小于 visibleCount+buffer，则需要重新创建 pool（rare）
                    var desiredPool = Mathf.Clamp(_visibleCount + buffer, 1, _totalSlots);
                    if (desiredPool > _totalSlots)
                    {
                        // 简单处理：销毁并重新初始化 pool（代价不大，通常 rare）
                        foreach (var go in _pool)
                            if (go) Destroy(go);
                        _pool.Clear();
                        _poolViews.Clear();
                        _poolSize = desiredPool;
                        
                        for (var i = 0; i < _poolSize; i++)
                        {
                            var go = Instantiate(slotPrefab, content, false);
                            go.transform.localScale = Vector3.one;
                            
                            var rt = go.GetComponent<RectTransform>();
                            rt.pivot = new Vector2(0f, 1f);
                            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                            rt.sizeDelta = new Vector2(slotWidth, slotHeight);
                            
                            _pool.Add(go);
                            
                            var view = go.GetComponent<SlotView>();
                            view.Initialize(this);
                            _poolViews.Add(view);
                        }
                    }
                    */
                    
                    // forceRebuildPool: true -> 会销毁并重新创建 pool，使 poolSize 与新的可见/缓冲行一致
                    RecalculateLayout(forceRebuildPool: true);
                    
                    // 刷新可见项以反映新槽
                    UpdateVisible();
                    
                    // 再次查找空槽
                    emptyIndex = _backendRecords.FindIndex(x => x == null);
                    if (emptyIndex == -1)
                    {
                        Debug.LogWarning("[RecycledInventoryUI] AddItemById: 扩容后仍无空槽，退出");
                        break;
                    }
                }

                var put = Mathf.Min(remaining, info.maxStack);
                var newInst = new ItemInstance(itemId, put);
                SetBackendAt(emptyIndex, newInst);
                remaining -= put;
                added += put;
                Debug.Log($"[RecycledInventoryUI] AddItemById: 填入空槽 idx={emptyIndex} put={put}");
            }

            return added;
        }

        /// <summary>
        /// 将指定槽设置为某个 ItemInstance（会写回并刷新视图）
        /// </summary>
        public void SetSlot(int index, ItemInstance inst)
        {
            SetBackendAt(index, inst);
        }

        /// <summary>
        /// 清空指定槽（等同于 SetSlot(index, null)）
        /// </summary>
        public void ClearSlot(int index)
        {
            SetBackendAt(index, null);
        }

        #endregion
        
        // 对外强制刷新布局（当外部修改 columns/slot sizes 等时调用）
        public void ForceRebuildLayout()
        {
            _layoutDirty = true;
            RecalculateLayout(forceRebuildPool: true);
            UpdateVisible();
        }

        // 在 RectTransform 尺寸变化时标记脏（Unity 回调）
        private void OnRectTransformDimensionsChange()
        {
            // Content 或 viewport 改变时 Unity 会调用此回调
            _layoutDirty = true;
        }
        
        // 核心布局计算/pool 重建方法
        private void RecalculateLayout(bool forceRebuildPool)
        {
            if (!content || !scrollRect) return;
            
            // use viewport width (more reliable)
            var viewportWidth = scrollRect.viewport ? scrollRect.viewport.rect.width : content.rect.width;

            int cols;
            if (columns > 0)
            {
                cols = Mathf.Max(1, columns);
                // ensure content width can fit these columns
                var requiredWidth = paddingLeft * 2f + cols * slotWidth + (cols - 1) * spacingX;
                var cdx = content.sizeDelta;
                if (cdx.x < requiredWidth) cdx.x = requiredWidth;
                content.sizeDelta = cdx;
            }
            else
            {
                cols = Mathf.Max(1, Mathf.FloorToInt((viewportWidth - paddingLeft * 2f + spacingX) / (slotWidth + spacingX)));
                cols = Mathf.Max(1, cols);
            }

            var rowsTotal = Mathf.CeilToInt((float)_totalSlots / cols);

            // viewport-based visible rows (include spacing)
            var viewportHeight = scrollRect.viewport ? scrollRect.viewport.rect.height : ((RectTransform)scrollRect.transform).rect.height;
            var rowsVisible = Mathf.CeilToInt(viewportHeight / (slotHeight + spacingY));
            rowsVisible = Mathf.Clamp(rowsVisible, 1, rowsTotal);

            var poolRows = Mathf.Clamp(rowsVisible + buffer, 1, rowsTotal);
            var desiredPoolSize = Mathf.Clamp(poolRows * cols, 1, _totalSlots);

            // set content height according to rowsTotal
            var contentHeight = paddingTop + rowsTotal * (slotHeight + spacingY) - spacingY + paddingTop;
            var cd = content.sizeDelta;
            cd.y = contentHeight;
            content.sizeDelta = cd;

            // if forced or pool size changed, rebuild pool
            if (forceRebuildPool || desiredPoolSize != _poolSize)
            {
                // destroy old pool
                foreach (var go in _pool) 
                    if (go) Destroy(go);
                _pool.Clear();
                _poolViews.Clear();

                _poolSize = desiredPoolSize;
                for (var i = 0; i < _poolSize; i++)
                {
                    var go = Instantiate(slotPrefab, content, false);
                    go.transform.localScale = Vector3.one;
                    var rt = go.GetComponent<RectTransform>();
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.sizeDelta = new Vector2(slotWidth, slotHeight);
                    _pool.Add(go);
                    var view = go.GetComponent<SlotView>();
                    if (!view) throw new Exception("slotPrefab must have SlotView component.");
                    view.Initialize(this);
                    _poolViews.Add(view);
                }
            }

            // cache computed values
            _colsCached = cols;
            _rowsTotalCached = rowsTotal;
            _rowsVisibleCached = rowsVisible;

            // update cached viewport dims
            _viewportWidthCached = scrollRect.viewport ? scrollRect.viewport.rect.width : content.rect.width;
            _viewportHeightCached = scrollRect.viewport ? scrollRect.viewport.rect.height : ((RectTransform)scrollRect.transform).rect.height;
            _lastColumnsField = columns;

            // mark not dirty
            _layoutDirty = false;
        }

        public void DisableScroll() { if (scrollRect) scrollRect.enabled = false; }
        public void EnableScroll()  { if (scrollRect) scrollRect.enabled = true; }

        public int SortAndCompactByRarity()
        {
            if (_backendRecords == null)
            {
                Debug.LogWarning("SortAndCompactByRarity: _backendRecords is null");
                return 0;
            }
            
            // collect current non-empty instance
            var current = _backendRecords.Where(x => x != null).ToList();
            if (current.Count == 0)
            {
                Debug.LogWarning("SortAndCompactByRarity: _backendRecords is empty");
                return 0;
            }
            
            // 按照 itemId 分组
            var groups = current.GroupBy(i => i.itemId)
                .Select(g =>
                {
                    var id = g.Key;
                    var info = ItemDataManager.Instance?.GetItemInfoById(id);
                    var rarity = info?.rarity ?? 0;
                    var maxStack = info?.maxStack ?? 1;
                    var totalCount = g.Sum(x => x.count);
                    return new
                    {
                        ItemId = id,
                        Info = info,
                        Rarity = rarity,
                        MaxStack = Mathf.Max(1, maxStack),
                        TotalCount = totalCount
                    };
                })
                // sort groups by rarity desc, then itemId asc for stability
                .OrderByDescending(x => x.Rarity)
                .ThenBy(x => x.ItemId)
                .ToList();

            var newList = new List<ItemInstance>();

            foreach (var grp in groups)
            {
                var id = grp.ItemId;
                var maxStack = grp.MaxStack;
                var total = grp.TotalCount;

                if (maxStack <= 1)
                {
                    // non-stackable: each is a single instance (count kept as 1 or original counts)
                    // but we don't have original per-instance counts here; assume they were 1 — create total instances
                    for (var k = 0; k < total; k++)
                    {
                        newList.Add(new ItemInstance(id, 1));
                    }
                }
                else
                {
                    var full = total / maxStack;
                    var rem = total % maxStack;
                    // add full stacks first (满足 "满堆叠在前" 的要求)
                    for (var k = 0; k < full; k++) newList.Add(new ItemInstance(id, maxStack));
                    if (rem > 0) newList.Add(new ItemInstance(id, rem));
                }
            }

            var beforeNonEmpty = current.Count;
            var afterNonEmpty = newList.Count;
            
            // pad with nulls to match original capacity (保持 _backendRecords.Count 不变)
            var capacity = _backendRecords.Count;
            if (newList.Count < capacity)
            {
                var toAdd = capacity - newList.Count;
                for (var i=0; i < toAdd; i++) newList.Add(null);
            }
            else if (newList.Count > capacity)
            {
                // 极端：如果合并后非空槽超过容量（理论上不应该，因为合并减少槽数），但兜底处理
                 capacity = newList.Count;
            }
            
            // 写回
            _backendRecords = newList;
            
            // 更新视图
            UpdateVisible();

            var mergedCount = Mathf.Max(0, beforeNonEmpty - afterNonEmpty);
            Debug.Log($"[RecycledInventoryUI] SortAndCompactByRarity: beforeNonEmpty={beforeNonEmpty}, afterNonEmpty={afterNonEmpty}, mergedCompress={mergedCount}");
            return mergedCount;
        }

        #region Context Menu

        /// <summary>
        /// Context Menu-Use: consume 1 unit
        /// </summary>
        /// <param name="index"></param>
        public void UseItemAt(int index)
        {
            if (index < 0 || index >= _backendRecords.Count) return;
            var inst = _backendRecords[index];
            if (inst == null) return;

            if (inst.count <= 1)
                _backendRecords[index] = null;
            else
            {
                inst.count -= 1;
                _backendRecords[index] = inst;
            }
            UpdateVisible();
        }

        /// <summary>
        /// Context Menu-Sell: remove item and add coins (sell price equal to info.price)
        /// </summary>
        /// <param name="index"></param>
        public void SellItemAt(int index)
        {
            if (index < 0 || index >= _backendRecords.Count) return;
            var inst = _backendRecords[index];
            if (inst == null) return;

            // 出售物品
            var info = inst.GetItemInfo();
            var gain = info?.price ?? 0;
            CurrencyManager.Instance.AddCoins(gain * inst.count);
            
            // 清空格子，更新可见区
            _backendRecords[index] = null;
            UpdateVisible();
        }

        /// <summary>
        /// Context Menu-Drop: delete the item (no refund)
        /// </summary>
        /// <param name="index"></param>
        public void DropItemAt(int index)
        {
            if (index < 0 || index >= _backendRecords.Count) return;
            _backendRecords[index] = null;
            UpdateVisible();
        }

        /// <summary>
        /// Context Menu-Split: keepCount remains in the original index, create a new ItemInstance for splitCount
        /// </summary>
        /// <param name="index"></param>
        /// <param name="keepCount"></param>
        public void SplitItemAt(int index, int keepCount)
        {
            if (index < 0 || index >= _backendRecords.Count) return;
            var inst = _backendRecords[index];
            if (inst == null) return;
            if (keepCount <= 0 || keepCount >= inst.count) return;
            
            // 计算数量
            var splitCount = inst.count - keepCount;
            inst.count = keepCount;
            _backendRecords[index] = inst;

            // 安全查找空 slot
            var emptyIndex = _backendRecords.FindIndex(x => x == null);
            if (emptyIndex == -1)
            {
                // 扩展
                var toAdd = Mathf.Max(initCapacity, Mathf.Max(1, _backendRecords.Count));
                for (var i=0; i<toAdd; i++) _backendRecords.Add(null);
                _totalSlots = _backendRecords.Count;
                // 重新计算布局
                RecalculateLayout(true);
                emptyIndex = _backendRecords.FindIndex(x => x == null);
                if (emptyIndex == -1) return;  // 
            }

            // 新增 slot
            var newInst = new ItemInstance(inst.itemId, splitCount);
            _backendRecords[emptyIndex] = newInst;
            UpdateVisible();
        }

        /// <summary>
        /// Context Menu-AutoSplit: split into two equal (or almost equal) stacks: floor and ceil
        /// </summary>
        /// <param name="index"></param>
        public void AutoSplitItemAt(int index)
        {
            if (index < 0 || index >= _backendRecords.Count) return;
            var inst = _backendRecords[index];
            if (inst is not { count: > 1 }) return;
            
            // 二分
            var half1 = inst.count / 2;
            var half2 = inst.count - half1;
            inst.count = half1;
            _backendRecords[index] = inst;
            
            // 安全查找空 slot
            var emptyIndex = _backendRecords.FindIndex(x => x == null);
            if (emptyIndex == -1)
            {
                // 扩展
                var toAdd = Mathf.Max(initCapacity, Mathf.Max(1, _backendRecords.Count));
                for (var i=0; i<toAdd; i++) _backendRecords.Add(null);
                _totalSlots = _backendRecords.Count;
                // 重新计算布局
                RecalculateLayout(true);
                emptyIndex = _backendRecords.FindIndex(x => x == null);
                if (emptyIndex == -1) return;  // 
            }
            
            // 新增 slot
            var newInst = new ItemInstance(inst.itemId, half2);
            _backendRecords[emptyIndex] = newInst;
            UpdateVisible();
        }

        #endregion
    }
}