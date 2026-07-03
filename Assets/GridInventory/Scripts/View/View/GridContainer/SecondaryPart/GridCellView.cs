using UnityEngine;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 格子预览状态
    /// </summary>
    public enum ECellPreviewState
    {
        None,
        Valid,
        Invalid,
    }

    /// <summary>
    /// 格子的UI需要挂载的脚本 
    /// 管理了高亮等功能
    /// </summary>
    public class GridCellView : MonoBehaviour
    {
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private Color defaultColor = new(0, 0, 0, 0.8f);

        [SerializeField]
        private Color highLightColor = new(0, 0, 0, 0.6f);

        /// <summary> 可放置预览色 </summary>
        [SerializeField]
        private Color canPlacePreviewColor = new(0f, 1f, 0f, 0.45f);

        /// <summary> 不可放置预览色 </summary>
        [SerializeField]
        private Color cannotPlacePreviewColor = new(1f, 0f, 0f, 0.45f);

        void Start()
        {
            EnsureHighlightImage();
            SetBkHighLight(false);
        }

        /// <summary>
        /// 设置背景高亮
        /// </summary>
        public void SetBkHighLight(bool isHighLight)
        {
            EnsureHighlightImage();
            if (backgroundImage is null)
                return;

            backgroundImage.color = isHighLight ? highLightColor : defaultColor;
        }

        /// <summary>
        /// 设置拖拽 footprint 预览色
        /// </summary>
        public void SetPreviewState(ECellPreviewState previewState)
        {
            EnsureHighlightImage();
            if (backgroundImage is null)
                return;

            backgroundImage.color = previewState switch
            {
                ECellPreviewState.Valid => canPlacePreviewColor,
                ECellPreviewState.Invalid => cannotPlacePreviewColor,
                _ => defaultColor,
            };
        }

        /// <summary>
        /// 获取高亮 Image
        /// </summary>
        private void EnsureHighlightImage()
        {
            if (backgroundImage is not null)
                return;

            backgroundImage = transform.Find("Highlight")?.GetComponent<Image>();
        }
    }
}
