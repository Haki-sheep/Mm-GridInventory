using UnityEngine;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 格子的UI需要挂载的脚本 
    /// 管理了高亮等功能
    /// </summary>
    public class GridCellView : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;

        private Color defaultColor = new(0, 0, 0, 0.8f);
        private Color highLightColor = new(0, 0, 0, 0.6f);
        void Start()
        {
            backgroundImage = this.transform.Find("Bk").GetComponent<Image>();
            SetBkHighLight(false);
        }

        /// <summary>
        /// 设置背景高亮
        /// </summary>
        /// <param name="isHighLight"></param>
        public void SetBkHighLight(bool isHighLight)
        {
            backgroundImage.color = isHighLight ? highLightColor : defaultColor;
        }

        /// <summary>
        /// 清除所有高亮格子
        /// </summary>
        public void ClearCellHighlight(int curHighLightCellIndex, GridCellView[] gridCellViews)
        {
            if (curHighLightCellIndex >= 0)
            {
                gridCellViews[curHighLightCellIndex].SetBkHighLight(false);
                curHighLightCellIndex = -1;
                return;
            }

            foreach (var cellView in gridCellViews)
            {
                cellView.SetBkHighLight(false);
            }
        }

    }
}