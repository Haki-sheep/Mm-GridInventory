using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 物品右键菜单面板
    /// </summary>
    public class ItemMenuPanel : MonoBehaviour
    {
        /// <summary> 单例 </summary>
        private static ItemMenuPanel instance;

        /// <summary> 单例 </summary>
        public static ItemMenuPanel Instance => instance;

        /// <summary> 面板 RectTransform </summary>
        private RectTransform panelRectCache;

        /// <summary> 面板 RectTransform </summary>
        private RectTransform PanelRect => panelRectCache ??= transform as RectTransform;

        /// <summary> 所属 Canvas </summary>
        private Canvas rootCanvasCache;

        /// <summary> 所属 Canvas </summary>
        private Canvas RootCanvas => rootCanvasCache ??= GetComponentInParent<Canvas>();

        /// <summary> 射线检测结果缓存 </summary>
        private readonly List<RaycastResult> raycastResultList = new();

        /// <summary> 当前帧是否由 Show 打开 </summary>
        private int showFrame = -1;

        /// <summary> 当前关联物品视图 </summary>
        public ItemView CurrentItemView { get; private set; }

        /// <summary>
        /// 初始化单例
        /// </summary>
        private void Awake()
        {
            instance = this;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 销毁时清理单例
        /// </summary>
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// 每帧末尾处理菜单关闭
        /// </summary>
        private void LateUpdate()
        {
            if (!gameObject.activeSelf)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                // 左键未点在菜单按钮上则关闭
                if (!IsPointerOverMenuButton())
                    Hide();
            }

            if (Input.GetMouseButtonDown(1) && showFrame != Time.frameCount)
            {
                // 右键点在空白处关闭 Item 上右键会在同帧 Show
                Hide();
            }
        }

        /// <summary>
        /// 显示菜单
        /// </summary>
        public void Show(ItemView itemView, Vector2 screenPosition)
        {
            if (itemView is null)
                return;

            CurrentItemView = itemView;
            showFrame = Time.frameCount;
            SetScreenPosition(screenPosition);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// 隐藏菜单
        /// </summary>
        public void Hide()
        {
            CurrentItemView = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 将屏幕坐标对齐到面板 pivot
        /// </summary>
        private void SetScreenPosition(Vector2 screenPosition)
        {
            if (PanelRect is null || RootCanvas is null)
                return;

            RectTransform parentRect = PanelRect.parent as RectTransform;
            if (parentRect is null)
                return;

            Camera eventCamera = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : RootCanvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
                return;

            PanelRect.anchoredPosition = localPoint;
        }

        /// <summary>
        /// 指针是否点在菜单内 Button 上
        /// </summary>
        private bool IsPointerOverMenuButton()
        {
            if (!TryGetPointerHitGameObject(out GameObject hitObject))
                return false;

            if (!hitObject.transform.IsChildOf(PanelRect))
                return false;

            return hitObject.GetComponentInParent<Button>() is not null;
        }

        /// <summary>
        /// 获取当前指针射线命中的 UI 对象
        /// </summary>
        private bool TryGetPointerHitGameObject(out GameObject hitObject)
        {
            hitObject = null;
            if (EventSystem.current is null)
                return false;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            raycastResultList.Clear();
            EventSystem.current.RaycastAll(pointerData, raycastResultList);
            if (raycastResultList.Count == 0)
                return false;

            hitObject = raycastResultList[0].gameObject;
            return hitObject is not null;
        }
    }
}
