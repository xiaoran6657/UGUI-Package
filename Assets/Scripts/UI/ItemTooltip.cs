using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ItemTooltip : MonoBehaviour
    {
        public static ItemTooltip Instance { get; private set; }

        [Header("sub-object References")] 
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameTMP;
        [SerializeField] private TextMeshProUGUI priceTMP;
        [SerializeField] private Image rarityAccent;
        
        [Header("Layout")]
        [Tooltip("Tooltip 相对鼠标的偏移(pixels)")]
        public Vector2 mouseOffset = new(12f, -12f);

        [Header("Manual bindings")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform tooltipRect;
        
        private RectTransform _rt;
        private RectTransform _canvasRect;
        private Canvas _rootCanvas;
        private bool _disabled = false;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }

            Instance = this;
            // tooltip RT: prefer explicit field, else GetComponent
            _rt = tooltipRect ? tooltipRect : GetComponent<RectTransform>();

            // canvas: prefer explicit field, else GetComponentInParent
            _rootCanvas = rootCanvas ? rootCanvas : GetComponentInParent<Canvas>();
            // 尝试查找场景中的 Canvas
            if (!_rootCanvas) _rootCanvas = FindObjectOfType<Canvas>();

            // canvas rect: prefer explicit field, else rootCanvas RectTransform
            _canvasRect = canvasRect ? canvasRect : (_rootCanvas ? _rootCanvas.GetComponent<RectTransform>() : null);

            if (!_rt || !_rootCanvas || !_canvasRect)
                Debug.LogWarning("[ItemTooltip] bindings incomplete: tooltipRT/rootCanvas/canvasRect should be assigned in Inspector. Fallback attempted.");
            
            if (iconImage) iconImage.raycastTarget = false; // 不拦截射线
            var cg = GetComponent<CanvasGroup>();
            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; // Tooltip 不应阻挡射线
            cg.interactable = false;

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;
            FollowMouse();
        }

        private void FollowMouse()
        {
            Vector2 screenPos = Input.mousePosition;

            // Convert screen point to canvas local point
            var cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, cam, out var anchored);

            // 
            anchored += mouseOffset;
            
            // 
            var size = _rt.sizeDelta;
            var halfW = _canvasRect.rect.width * 0.5f;
            var halfH = _canvasRect.rect.height * 0.5f;
            
            var minX = -halfW + size.x * 0.5f;
            var maxX =  halfW - size.x * 0.5f;
            var minY = -halfH + size.y * 0.5f;
            var maxY =  halfH - size.y * 0.5f;
            
            anchored.x = Mathf.Clamp(anchored.x, minX, maxX);
            anchored.y = Mathf.Clamp(anchored.y, minY, maxY);
            
            _rt.anchoredPosition = anchored;
        }

        public void Show(ItemInstance inst)
        {
            if (_disabled) return;
            if (inst == null) { Hide(); return; }
            var info = inst.GetItemInfo();
            Show(info, inst.count);
        }

        private void Show(Data.Info.ItemInfo info, int count)
        {
            if (info == null) { Hide(); return; }

            nameTMP.text = info.itemName ?? "未知物品";
            priceTMP.text = $"Price: {info.price}";
            
            // rarity accent
            if (rarityAccent)
            {
                Color bg = info.GetRarityColor();
                bg.a = 0.3f;
                rarityAccent.color = bg;
            }
            
            // 
            if (iconImage)
            {
                iconImage.sprite = info.icon;
                iconImage.enabled = true;
            }
            
            // 
            gameObject.SetActive(true);
            FollowMouse();
        }

        public void Hide() { gameObject.SetActive(false); }
        
        // 
        public static void EnsureInstance()
        {
            if (Instance) return;
            var found = FindObjectOfType<ItemTooltip>();
            if (found) Instance = found;
        }
        
        /// <summary>
        /// 临时关闭 tooltip 的显示（调用后 Show() 不再显示）
        /// </summary>
        public void Disable()
        {
            _disabled = true;
            Hide();
        }

        /// <summary>
        /// 恢复 tooltip 的显示功能
        /// </summary>
        public void Enable()
        {
            _disabled = false;
        }
    }
}