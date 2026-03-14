using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 右键上下文菜单（单例）。在 ShowAt 时会：
    ///  - 把菜单定位到屏幕坐标并显示
    ///  - 隐藏并禁用 ItemTooltip（防止遮挡/拦射线）
    ///  - 将 root 放到 Canvas 顶层并确保 blocksRaycasts/interactable 为 true
    ///  - Update 时若检测到在菜单外点击则自动 Hide()
    /// </summary>
    public class ContextMenu : MonoBehaviour
    {
        public static ContextMenu Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Canvas rootCanvas;
        public RectTransform root;
        [SerializeField] private Button btnUse;
        [SerializeField] private Button btnSplit;
        [SerializeField] private Button btnSell;
        [SerializeField] private Button btnDrop;
        
        // 
        private int _currentSlotIndex = -1;
        private RecycledInventoryUI _targetInventoryUI;
        private Canvas _rootCanvas;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            
            if (root) root.gameObject.SetActive(false);
            else Debug.LogWarning("[ContextMenu] root not assigned in inspector");

            
            // ensure CanvasGroup exists so we can control raycast behavior
            _canvasGroup = root ? root.GetComponent<CanvasGroup>() : null;
            if (!_canvasGroup && root) _canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            
            // 取消之前的订阅
            btnUse.onClick.RemoveAllListeners();
            btnSplit.onClick.RemoveAllListeners();
            btnSell.onClick.RemoveAllListeners();
            btnDrop.onClick.RemoveAllListeners();
            
            // 绑定按钮
            btnUse.onClick.AddListener(OnUseClicked);
            btnSplit.onClick.AddListener(OnSplitClicked);
            btnSell.onClick.AddListener(OnSellClicked);
            btnDrop.onClick.AddListener(OnDropClicked);
            
            // canvas: prefer explicit field, else GetComponentInParent
            _rootCanvas = rootCanvas ? rootCanvas : GetComponentInParent<Canvas>();
            // 尝试查找场景中的 Canvas
            if (!_rootCanvas) _rootCanvas = FindObjectOfType<Canvas>();
        }
        
        // 每帧检测点击区域：若菜单显示，且鼠标点击在菜单外，则关闭菜单
        private void Update()
        {
            if (!root || !root.gameObject.activeSelf) return;

            // detect any mouse button down
            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1)) return;
            
            // do a UI raycast at mouse pos
            var es = EventSystem.current;
            if (!es) return;
            var ped = new PointerEventData(es) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            // use the canvas's GraphicRaycaster if available
            var gr = root.GetComponentInParent<Canvas>()?.GetComponent<GraphicRaycaster>() ?? FindObjectOfType<GraphicRaycaster>();
            if (!gr)
            {
                Hide();
                return;
            }
            gr.Raycast(ped, results);

            var clickedInside = results.Any(r => r.gameObject.transform.IsChildOf(root.transform));

            if (!clickedInside)
            {
                Hide();
            }
        }
        
        /// <summary>
        /// 显示菜单，screenPos 传 Input.mousePosition，inventory 传 RecycledInventoryUI 实例，slotIndex 指格子索引
        /// </summary>
        public void ShowAt(Vector2 screenPos, RecycledInventoryUI inventory, int slotIndex)
        {
            if (!inventory || slotIndex < 0) return;
            _targetInventoryUI = inventory;
            _currentSlotIndex = slotIndex;
            
            // Position: convert screen pos to canvas local
            Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
            RectTransform canvasRect = _rootCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out var anchored);
            
            // ensure root is child of canvas for correct coordinates & place it on top
            if (root.transform.parent != _rootCanvas.transform)
            {
                root.SetParent(_rootCanvas.transform, worldPositionStays: false);
            }
            root.SetAsLastSibling(); // ensure top ordering visually & for raycasts
            
            // position & enable
            root.anchoredPosition = anchored;
            root.gameObject.SetActive(true);
            
            // ensure canvas group blocks raycasts and interactable so buttons receive clicks
            if (_canvasGroup)
            {
                _canvasGroup.blocksRaycasts = true; 
                _canvasGroup.interactable = true; 
                _canvasGroup.alpha = 1f;
            }

            // hide & disable tooltip while menu open
            if (ItemTooltip.Instance) ItemTooltip.Instance.Disable();
            
            // Position: convert screen pos to canvas local
            var inst = _targetInventoryUI.GetBackendAt(slotIndex);
            if (inst == null)
            {
                Hide();
                return;
            }

            var info = inst.GetItemInfo();
            btnUse.interactable = true;
            btnSplit.interactable = inst.CanStack && inst.count > 1;
            btnSell.interactable = info is { canSell: true };
            btnDrop.interactable = true;
        }

        private void Hide()
        {
            if (root)
            {
                root.gameObject.SetActive(false);
                if (_canvasGroup)
                {
                    _canvasGroup.blocksRaycasts = false; 
                    _canvasGroup.interactable = false;
                }
            }
            
            // re-enable tooltip
            if (ItemTooltip.Instance) ItemTooltip.Instance.Enable();
            
            _targetInventoryUI = null;
            _currentSlotIndex = -1;
        }

        private void OnUseClicked()
        {
            if (_targetInventoryUI && _currentSlotIndex >= 0)
                _targetInventoryUI.UseItemAt(_currentSlotIndex);

            Hide();
        }
        
        private void OnSplitClicked()
        {
            Debug.LogWarning("OnSplitClicked");
            if (_targetInventoryUI && _currentSlotIndex >= 0)
            {
                // compute menu root's screen position
                Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
                Vector3 worldPos = root.TransformPoint(root.rect.center);
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
                
                // open split window
                SplitWindow.Instance?.ShowForSlot(_targetInventoryUI, _currentSlotIndex, screenPos);
            }

            // close menu and open split window (split window will re-disable tooltip)
            Hide();
            if (ItemTooltip.Instance) ItemTooltip.Instance.Disable();
        }
        
        private void OnSellClicked()
        {
            if (_targetInventoryUI && _currentSlotIndex >= 0)
                _targetInventoryUI.SellItemAt(_currentSlotIndex);

            Hide();
        }
        
        private void OnDropClicked()
        {
            if (_targetInventoryUI && _currentSlotIndex >= 0)
                _targetInventoryUI.DropItemAt(_currentSlotIndex);

            Hide();
        }
        
        private bool IsPointerOverGameObject(GameObject go)
        {
            var es = EventSystem.current;
            if (!es) return false;
            
            PointerEventData ped = new PointerEventData(es) { position = Input.mousePosition };
            var results = new System.Collections.Generic.List<RaycastResult>();
            var gr = go.GetComponentInParent<Canvas>()?.GetComponent<GraphicRaycaster>() ?? FindObjectOfType<GraphicRaycaster>(); 
            if (!gr) return false;
            
            gr.Raycast(ped, results);
            return results.Any(r => r.gameObject.transform.IsChildOf(go.transform));
        }
        
        // 
        public static void EnsureInstance()
        {
            if (Instance) return;
            var found = FindObjectOfType<ContextMenu>();
            if (found) Instance = found;
        }
    }
}