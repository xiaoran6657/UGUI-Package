using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 全局拖拽控制器。负责：
    /// - 开始拖拽（记录 srcIndex，创建拖拽图标，隐藏源 view）
    /// - 跟随鼠标（OnDrag / Update）
    /// - 结束拖拽（raycast 到 SlotView，读取 displayIndex，调用 RecycledInventoryUI.HandleDrop）
    /// 
    /// 需要在 Inspector 指定 rootCanvas（或自动查找），并指定 targetInventory (RecycledInventoryUI)。
    /// </summary>
    public class DragController : MonoBehaviour
    {
        public static DragController Instance { get; private set; }
        
        [Header("References")]
        public Canvas rootCanvas;        // assign root Canvas (Screen Space - Overlay / Camera)
        //public InventoryUI inventoryUI;  // assign InventoryUI in Inspector (or left null to Find)
        public RecycledInventoryUI targetInventory;  // assign the RecycledInventoryUI instance
        public SelectiveScrollRect targetScrollRect;  // used to disable scroll during drag

        [Header("Drag Icon")]
        [Tooltip("拖拽图标透明度")] public float dragIconAlpha = 0.8f;
        [Tooltip("拖拽图标缩放")] public float dragIconScale = 1f;
        
        private GraphicRaycaster _raycaster;
        private GameObject _dragIconGo;
        private Image _dragIconImage;
        private RectTransform _dragIconRT;

        private int _srcIndex = -1;
        private SlotView _srcView;
        
        private bool _isDragging = false;

        private void Awake()
        {
            if (Instance && Instance != this) 
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (!rootCanvas)
            {
                rootCanvas = GetComponentInParent<Canvas>();
                if (!rootCanvas) rootCanvas = FindObjectOfType<Canvas>();
            }

            if (rootCanvas) _raycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            if (!_raycaster) Debug.LogWarning("[DragController] GraphicRaycaster is missing on rootCanvas.");

            if (!EventSystem.current)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Debug.Log("[DragController] EventSystem auto-created.");
            }
        }

        private void Update()
        {
            if (_isDragging)
            {
                FollowMouse();
                if (Input.GetMouseButtonUp(0)) EndDragInternal();
            }
        }

        public void BeginDragFromIndex(int srcIndex, SlotView srcView)
        {
            if (_isDragging) EndDragInternal();

            if (srcIndex < 0) return;
            _srcIndex = srcIndex;
            _srcView = srcView;
            var inst = targetInventory?.GetBackendAt(srcIndex);
            if (inst == null) return;
            
            CreateDragIcon(inst);
            // 
            _srcView?.SetDraggingVisibility(true);
            
            // 
            if (targetScrollRect) targetScrollRect.enabled = false;
            
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            FollowMouse();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            EndDragInternal();
            
            // 恢复 ScrollRect
            if (targetInventory) targetInventory.EnableScroll();
        }

        private void EndDragInternal()
        {
            // 
            var dstIndex = GetSlotIndexUnderPointer();
            Debug.Log($"DragController.EndDrag -> src:{_srcIndex} dst:{dstIndex}");
            
            // 
            if (targetInventory)
                targetInventory.HandleDrop(_srcIndex, dstIndex);
            else
                Debug.LogWarning("[DragController] targetInventory is not assigned");

            // 
            if (_dragIconGo) Destroy(_dragIconGo);
            _dragIconGo = null;
            _dragIconImage = null;
            _dragIconRT = null;
            
            // 
            _srcView?.SetDraggingVisibility(false);
            _srcView = null;
            _srcIndex = -1;
            
            // 
            if (targetScrollRect) 
                targetScrollRect.enabled = true;

            _isDragging = false;
        }

        #region Drag Icon

        private void CreateDragIcon(Data.ItemInstance inst)
        {
            if (!rootCanvas) rootCanvas = FindObjectOfType<Canvas>();
            if (!rootCanvas) { Debug.LogError("[DragController] rootCanvas not found for drag icon"); return; }
            
            // create GameObject under canvas
            _dragIconGo = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasGroup));
            _dragIconGo.transform.SetParent(rootCanvas.transform, false);

            _dragIconRT = _dragIconGo.GetComponent<RectTransform>();
            _dragIconRT.pivot = new Vector2(0.5f, 0.5f);

            // size
            Vector2 size = new Vector2(64, 64) * dragIconScale;
            _dragIconRT.sizeDelta = size;
            
            // image
            _dragIconImage = _dragIconGo.AddComponent<Image>();
            var sprite = inst?.Icon;
            if (sprite)
                _dragIconImage.sprite = sprite;
            else
            {
                // fallback visual if no sprite: use a colored square
                var tex = Texture2D.whiteTexture;
                _dragIconImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
            }
            _dragIconImage.preserveAspect = true;

            var cg = _dragIconGo.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = dragIconAlpha;
            
            // initial follow
            FollowMouse();
        }

        private void FollowMouse(PointerEventData eventData = null)
        {
            if (!_dragIconRT) return;

            Vector2 pos;
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            if (eventData != null)
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas.transform as RectTransform, eventData.position, cam, out pos);
            else
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas.transform as RectTransform, Input.mousePosition, cam, out pos);
            
            _dragIconRT.anchoredPosition = pos;
        }

        #endregion

        #region Raycast helper

        /// <summary>
        /// 通过射线检测找到鼠标下方的物品槽界面。返回槽索引，如果没有则返回-1。
        /// </summary>
        /// <returns></returns>
        private int GetSlotIndexUnderPointer()
        {
            if (!_raycaster && rootCanvas) _raycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            if (!_raycaster)
            {
                Debug.LogWarning("[DragController] GraphicRaycaster missing, cannot raycast for slot under pointer.");
                return -1;
            }

            PointerEventData ped = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            _raycaster.Raycast(ped, results);

            foreach (var r in results)
            {
                var go = r.gameObject;
                var slot = go.GetComponentInParent<SlotView>();
                if (!slot) continue;
                
                var idx = slot.GetDisplayIndex();
                Debug.Log($"GetSlotIndexUnderPointer -> hit {go.name}, slot displayIndex: {idx}");
                return idx;
            }

            Debug.Log("GetSlotIndexUnderPointer -> no slot found under pointer.");
            return -1;
        }

        #endregion
    }
}