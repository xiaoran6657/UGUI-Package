using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 单个池化槽视图。
    /// 通过 SetDisplayIndex(index) 与 RecycledInventoryUI 建立索引映射。
    /// 实现 IBeginDrag/IDrag/IEndDrag，并把 displayIndex 传给 DragController。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")] 
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countTMP;
        [SerializeField] private TextMeshProUGUI nameTMP;
        
        // 当前这个 view 在展示哪个后端索引（由 RecycledInventoryUI 维护）
        private int _displayIndex = -1;
        private RecycledInventoryUI _parentUI;

        public void Initialize(RecycledInventoryUI parent)
        {
            _parentUI = parent;
        }

        public void SetDisplayIndex(int index)
        {
            _displayIndex = index;
        }

        /// <summary>
        /// 用后端 ItemInstance 更新显示（inst 可为 null）
        /// </summary>
        public void Bind(Data.ItemInstance inst)
        {
            if (inst == null)
            {
                if (iconImage) { iconImage.sprite = null; iconImage.enabled = false; }
                if (countTMP) { countTMP.text = ""; countTMP.gameObject.SetActive(false); }
                if (nameTMP) { nameTMP.text = ""; nameTMP.gameObject.SetActive(false); }
                if (backgroundImage)
                {
                    // 恢复为默认白色（半透明）
                    Color defaultBg = Color.white;
                    defaultBg.a = 0.1f;
                    backgroundImage.color = defaultBg;
                }
                return;
            }

            var info = inst.GetItemInfo();
            if (iconImage)
            {
                iconImage.sprite = info?.icon;
                iconImage.enabled = info?.icon;
            }

            if (countTMP)
            {
                if (inst.count > 1)
                {
                    countTMP.text = inst.count.ToString();
                    countTMP.gameObject.SetActive(true);
                }
                else
                {
                    countTMP.text = "";
                    countTMP.gameObject.SetActive(false);
                }
            }

            if (nameTMP)
            {
                nameTMP.text = info?.itemName;
                nameTMP.gameObject.SetActive(info?.itemName != null);
            }
            
            if (backgroundImage)
            {
                Color bg = info?.GetRarityColor() ?? Color.white;
                bg.a = 0.3f;
                backgroundImage.color = bg;
            }
        }
        
        #region Drag interface

        public void OnBeginDrag(PointerEventData eventData)
        {
            // block interactions when split window is open
            if (SplitWindow.Instance && SplitWindow.Instance.IsOpen) return;
            
            // 
            if (_displayIndex < 0) return;
            var inst = _parentUI.GetBackendAt(_displayIndex);
            if (inst == null) return;
            
            // 先禁用滚动，确保 ScrollRect 不会处理该次拖拽
            _parentUI.DisableScroll();
            
            // 请求开始拖拽
            DragController.Instance?.BeginDragFromIndex(_displayIndex, this);
            //eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            DragController.Instance?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragController.Instance?.OnEndDrag(eventData);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // block interactions when split window is open
            if (SplitWindow.Instance && SplitWindow.Instance.IsOpen) return;
            
            // 右键逻辑
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // only open context menu for non-empty slot
                var inst = _parentUI.GetBackendAt(_displayIndex);

                if (inst == null) return;
                
                ItemTooltip.EnsureInstance();
                ContextMenu.EnsureInstance();
                
                if (!ContextMenu.Instance)
                {
                    Debug.LogWarning("[SlotView] ContextMenu.Instance is null — make sure ContextMenu prefab is in the scene under Canvas.");
                    return;
                }
                
                // Show context menu at mouse position
                ContextMenu.Instance?.ShowAt(Input.mousePosition, _parentUI, _displayIndex);
                return;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // if a modal window is open (SplitWindow) or context menu is visible, block hover/tooltip
            if ((SplitWindow.Instance && SplitWindow.Instance.IsOpen) || (ContextMenu.Instance && ContextMenu.Instance.root && ContextMenu.Instance.root.gameObject.activeSelf))
                return;

            if (!_parentUI) return;
            var inst = _parentUI.GetBackendAt(_displayIndex);
            if (inst == null) return;
            ItemTooltip.Instance?.Show(inst);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //Debug.LogWarning($"[SlotView] OnPointerExit idx={_displayIndex}");
            ItemTooltip.Instance?.Hide();
        }
        
        #endregion
        
        // DragController 在拖拽时调用：隐藏/显示 slot 内显示内容
        public void SetDraggingVisibility(bool hide)
        {
            if (iconImage) iconImage.enabled = !hide;
            if (countTMP) countTMP.gameObject.SetActive(!hide && countTMP.text != "");
            if (nameTMP) nameTMP.gameObject.SetActive(!hide);
        }

        public int GetDisplayIndex() => _displayIndex;
    }
}