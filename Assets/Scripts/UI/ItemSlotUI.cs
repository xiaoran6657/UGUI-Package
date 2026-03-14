using System;
using Data;
using Data.Info;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 物品格子UI
    /// </summary>
    public class ItemSlotUI : MonoBehaviour
    {
        [Header("UI组件")] 
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameTMP;
        [SerializeField] private TextMeshProUGUI countTMP;
        //[SerializeField] private Button slotButton;

        [Header("数据")]
        private int _slotIndex;
        private ItemInstance _itemInstance;
        
        [Tooltip("长按持续时间 (秒) 后开始拖拽")] public float longPressThreshold = 0.2f;

        // 事件，外部可订阅
        public event Action<int> OnSlotClicked;
        public event Action<int> OnSlotRightClicked;
        
        // 长按检测状态
        private bool _pointerDown = false;
        private float _pointerDownTime = 0f;
        private bool _isDragging = false;

        private void Awake()
        {
            //if (slotButton) slotButton.onClick.AddListener(OnSlotClick);
        }
        
        private void Update()
        {
            //Debug.Log($"ItemSlotUI Update slot {_slotIndex} | pointerDown={_pointerDown} | isDragging={_isDragging} | now={Time.unscaledTime:F3} | downTime={_pointerDownTime:F3}");

            // 检测长按阈值
            if (!_pointerDown)
            {
                // pointer 没按下，常见
                return;
            }
            if (_isDragging)
            {
                // 已经处于拖拽状态，长按逻辑被短路
                return;
            }
            var delta = Time.unscaledTime - _pointerDownTime;
            if (delta < longPressThreshold)
            {
                // 还没到阈值
                return;
            }

            // 到阈值了：开始拖拽
            Debug.Log($"ItemSlotUI Update: longPress slot {_slotIndex} (held {delta:F3}s)");
            if (_itemInstance != null)
            {
                Debug.Log($"ItemSlotUI Update: begin dragging slot {_slotIndex}");
                //DragController.Instance?.BeginDragFromSlot(this);
                // 可根据 BeginDragFromSlot 的返回值决定是否设为 dragging（若你未改 BeginDragFromSlot 则始终设 true）
                _isDragging = true;
            }

            // 防止重复启动（注意：我们只在真正进入拖拽时清掉 pointerDown）
            _pointerDown = false;
        }

        /// <summary>
        /// 初始化格子
        /// </summary>
        /// <param name="index"></param>
        public void Initialize(int index)
        {
            _slotIndex = index;
            ClearSlot();
        }

        /// <summary>
        /// 填充物品数据
        /// </summary>
        /// <param name="instance"></param>
        public void SetItem(ItemInstance instance)
        {
            _itemInstance = instance;

            if (instance == null)
            {
                ClearSlot();
                return;
            }
            
            ItemInfo itemInfo = instance.GetItemInfo();
            
            // 图标
            if (iconImage)
            {
                iconImage.sprite = itemInfo?.icon;
                iconImage.enabled = itemInfo?.icon;
            }
            
            // 名称
            if (nameTMP)
            {
                nameTMP.text = itemInfo?.itemName ?? "";
                nameTMP.gameObject.SetActive(!string.IsNullOrEmpty(itemInfo?.itemName));
            }
        
            // 数量
            if (countTMP)
            {
                if (instance.count > 1)
                {
                    countTMP.text = instance.count.ToString();
                    countTMP.gameObject.SetActive(true);
                }
                else
                {
                    countTMP.text = "";
                    countTMP.gameObject.SetActive(false);
                }
            }
            
            // 根据稀有度调节背景颜色
            if (backgroundImage)
            {
                Color bgColor = itemInfo?.GetRarityColor() ?? Color.white;
                bgColor.a = 0.3f;  // 降低透明度
                backgroundImage.color = bgColor;
            }
        }

        /// <summary>
        /// 清理格子
        /// </summary>
        public void ClearSlot()
        {
            _itemInstance = null;

            if (iconImage)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (nameTMP)
            {
                nameTMP.text = "";
                nameTMP.gameObject.SetActive(false);
            }
            
            if (countTMP)
            {
                countTMP.text = "";
                countTMP.gameObject.SetActive(false);
            }

            if (backgroundImage)
            {
                backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0.1f);
            }
        }

        /// <summary>
        /// 更新数量显示
        /// </summary>
        /// <param name="count"></param>
        public void UpdateCount(int count)
        {
            if (_itemInstance != null) _itemInstance.count = count;
            if (!countTMP) return;
            
            if (count > 1)
            {
                countTMP.text = count.ToString();
                countTMP.gameObject.SetActive(true);
            }
            else
            {
                countTMP.text = "";
                countTMP.gameObject.SetActive(false);
            }
        }

        // 
        public ItemInstance GetItemInstance() => _itemInstance;
        public int GetSlotIndex() => _slotIndex;
        
        // 
        public void SetDraggingVisibility(bool hide)
        {
            if (iconImage) iconImage.enabled = !hide;
            if (countTMP) countTMP.gameObject.SetActive(!hide && _itemInstance != null && _itemInstance.count > 1);
        }
        
        // OnPointerDown: 仅记录时间，不设 _isDragging = true
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _pointerDown = true;
            _pointerDownTime = Time.unscaledTime;
            _isDragging = false; // <- 关键：不要在此就设 true
            Debug.Log($"OnPointerDown slot {_slotIndex} at {_pointerDownTime:F3}");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (_isDragging)
                {
                    Debug.Log($"OnPointerUp slot {_slotIndex} (was dragging) at {Time.unscaledTime:F3}");
                    _isDragging = false;
                }
                else
                {
                    Debug.Log($"OnPointerUp slot {_slotIndex} (not dragging) at {Time.unscaledTime:F3}");
                }
            }

            _pointerDown = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Right:
                    OnSlotRightClicked?.Invoke(_slotIndex);
                    return;
                case PointerEventData.InputButton.Left:
                    if (!_isDragging) OnSlotClicked?.Invoke(_slotIndex);
                    break;
            }
        }
    }
}