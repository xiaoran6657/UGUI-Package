using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 可选择响应拖拽的 ScrollRect：
    /// - 仅当拖拽在 verticalScrollbar 上时才响应
    /// - 鼠标滚轮仍然可以滚动
    /// - 可在 allowScrollOnScrollbar / allowScrollOnWheel 配置行为
    /// </summary>
    [AddComponentMenu("UI/Selective ScrollRect")]
    public class SelectiveScrollRect : ScrollRect
    {
        [Tooltip("是否允许在 Vertical Scrollbar 上拖拽滚动")] public bool allowScrollOnScrollbar = true;

        [Tooltip("是否允许使用鼠标滚动滚动")] public bool allowScrollOnWheel = true;

        /// <summary>
        /// 判断 PointerEventData 的 pointerEnter 是否在 verticalScrollbar 的层级内
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        private bool IsPointerOverVerticalScrollbar(PointerEventData eventData)
        {
            if (!allowScrollOnScrollbar || !verticalScrollbar || eventData == null) return false;
            
            // 优先使用 eventData 已有信息（更快）
            GameObject go = eventData.pointerEnter ?? eventData.pointerCurrentRaycast.gameObject ?? eventData.rawPointerPress;
            if (go)
            {
                var t = go.transform;
                while (t)
                {
                    if (t.gameObject == verticalScrollbar.gameObject) return true;
                    t = t.parent;
                }
            }
            
            // 回退：做一次显示 GraphicRaycaster 射线检测（更可靠但较慢）
            var es = EventSystem.current;
            if (!es) return false;
            
            // 找到最接近的 GraphicRaycaster （优先使用当前 ScrollRect 的 Canvas）
            GraphicRaycaster gr = null;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas) gr = canvas.GetComponent<GraphicRaycaster>();
            if (!gr) gr = FindObjectOfType<GraphicRaycaster>();
            if (!gr) return false;
            
            var ped = new PointerEventData(es) { position = eventData.position };
            var results = new List<RaycastResult>();
            gr.Raycast(ped, results);

            foreach (var r in results)
            {
                var tt = r.gameObject.transform;
                while (tt)
                {
                    if (tt.gameObject == verticalScrollbar.gameObject) return true;
                    tt = tt.parent;
                }
            }

            return false;
        }
        
         
        /*private bool IsPointerOverVerticalScrollbar(PointerEventData eventData)
        {
            if (!allowScrollOnScrollbar || !verticalScrollbar || eventData == null) return false;

            var go = eventData.pointerEnter;
            if (!go) return false;
            
            var t = go.transform;
            while (t)
            {
                if (t.gameObject == verticalScrollbar.gameObject) return true;
                t = t.parent;
            }
            return false;
        }*/

        /// <summary>
        /// 鼠标滚轮事件保留
        /// </summary>
        /// <param name="eventdata"></param>
        public override void OnScroll(PointerEventData eventdata)
        {
            if (!allowScrollOnWheel) return;
            base.OnScroll(eventdata);
        }

        /// <summary>
        /// 当拖拽开始：仅当在 scrollbar 上时才调用 base
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"OnBeginDrag pointerEnter={eventData.pointerEnter?.name} rawPress={eventData.rawPointerPress?.name}");
            // 确保只有鼠标/触摸左键才考虑
            if (!IsPointerOverVerticalScrollbar(eventData)) return;
            base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (!IsPointerOverVerticalScrollbar(eventData)) return;
            base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (!IsPointerOverVerticalScrollbar(eventData)) return;
            base.OnEndDrag(eventData);
        }
    }
}