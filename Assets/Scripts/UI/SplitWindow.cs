using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SplitWindow : MonoBehaviour
    {
        public static SplitWindow Instance { get; private set; }

        [Header("References")]
        public Canvas rootCanvas;
        public RectTransform root;
        public TMP_Text titleTMP;
        public Slider slider;
        public TMP_Text leftLabel;
        public TMP_Text rightLabel;
        public TMP_Text centerLabel;
        public Button btnSplit;
        public Button btnAutoSplit;
        public Button btnCancel;
        public float horizontalOffset = 8f;  // px to the right of reference
        public float verticalOffset = 0f;    // optional vertical shift
        
        private RecycledInventoryUI _inventoryUI;
        private int _slotIndex;
        private int _currentCount;
        private CanvasGroup _canvasGroup;
        
        // public state used by SlotView to disable interaction
        public bool IsOpen { get; private set; } = false;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (!root) Debug.LogWarning("[SplitWindow] root not assigned");
            _canvasGroup = root ? root.GetComponent<CanvasGroup>() : null;
            if (!_canvasGroup && root) _canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            
            btnSplit.onClick.RemoveAllListeners();
            btnAutoSplit.onClick.RemoveAllListeners();
            btnCancel.onClick.RemoveAllListeners();
            
            btnSplit.onClick.AddListener(OnSplitButton);
            btnAutoSplit.onClick.AddListener(OnAutoSplitButton);
            btnCancel.onClick.AddListener(OnCancelButton);
            slider.onValueChanged.AddListener(OnSliderChanged);
            
            if (root) root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Show window for a slot. screenPos is a reference screen point (e.g. context menu or slot's screen pos).
        /// The window will attempt to position to the right of that point and clamp to screen edges.
        /// </summary>
        public void ShowForSlot(RecycledInventoryUI inventoryUI, int slotIndex, Vector2 screenPos)
        {
            if (!inventoryUI || slotIndex < 0) return;
            _inventoryUI = inventoryUI;
            _slotIndex = slotIndex;

            var inst = _inventoryUI.GetBackendAt(_slotIndex);
            if (inst == null) return;
            
            // 更新组件
            _currentCount = inst.count;
            titleTMP.text = "物品拆分";
            leftLabel.text = "1";
            rightLabel.text = _currentCount.ToString();
            slider.minValue = 1;
            slider.maxValue = Mathf.Max(1, _currentCount - 1);
            // 不可拆分
            if (slider.maxValue < 1)
            {
                root.gameObject.SetActive(false);
                return;
            }
            
            slider.value = Mathf.Clamp(Mathf.Floor((slider.minValue + slider.maxValue) / 2f), slider.minValue, slider.maxValue);
            UpdateCenterLabel();
            
            // position window: convert screenPos to canvas local and offset to the right
            Canvas canvas = rootCanvas ? rootCanvas : root.GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[SplitWindow] No Canvas found, cannot position split window.");
                root.gameObject.SetActive(true); // still show but may be misplaced
            }
            else
            {
                Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out var anchored);

                // compute right-shift by half width of this window + offset
                // temporarily set active to get sizeDelta if needed
                root.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(root); // ensure size updated
                var width = root.sizeDelta.x;
                var height = root.sizeDelta.y;

                // shift to the right of reference point
                anchored.x += (width * 0.5f) + horizontalOffset;
                anchored.y += verticalOffset;

                // clamp to canvas bounds
                var halfW = canvasRect.rect.width * 0.5f;
                var halfH = canvasRect.rect.height * 0.5f;
                var minX = -halfW + width * 0.5f;
                var maxX = halfW - width * 0.5f;
                var minY = -halfH + height * 0.5f;
                var maxY = halfH - height * 0.5f;
                anchored.x = Mathf.Clamp(anchored.x, minX, maxX);
                anchored.y = Mathf.Clamp(anchored.y, minY, maxY);

                // parent to canvas if not already
                if (root.transform.parent != canvas.transform)
                {
                    root.SetParent(canvas.transform, worldPositionStays: false);
                }
                root.SetAsLastSibling();
                root.anchoredPosition = anchored;
            }

            // ensure canvas group will block raycasts so window buttons receive clicks
            if (_canvasGroup)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
                _canvasGroup.alpha = 1f;
            }

            // disable tooltip & set modal state
            ItemTooltip.Instance?.Disable();
            IsOpen = true;

            // show window
            root.gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏 SplitWindow 并收尾
        /// </summary>
        private void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            if (_canvasGroup)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            ItemTooltip.Instance?.Enable();
            IsOpen = false;
            _inventoryUI = null;
            _slotIndex = -1;
        }

        private void OnSliderChanged(float value)
        {
            UpdateCenterLabel();
        }

        private void UpdateCenterLabel()
        {
            var keep = Mathf.RoundToInt(slider.value);
            centerLabel.text = keep.ToString();
        }

        private void OnSplitButton()
        {
            var keep = Mathf.RoundToInt(slider.value);
            if (_inventoryUI && _slotIndex >= 0)
                _inventoryUI.SplitItemAt(_slotIndex, keep);
            Hide();
        }

        private void OnAutoSplitButton()
        {
            if (_inventoryUI && _slotIndex >= 0)
                _inventoryUI.AutoSplitItemAt(_slotIndex);
            Hide();
        }

        private void OnCancelButton() { Hide(); }
    }
}