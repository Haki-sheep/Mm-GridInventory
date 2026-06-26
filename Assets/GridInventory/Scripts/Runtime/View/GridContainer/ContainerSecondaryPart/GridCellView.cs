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
        [SerializeField] 
        private Image backgroundImage;
        
        [SerializeField] 
        private Color defaultColor = new(0, 0, 0, 0.8f);
        
        [SerializeField] 
        private Color highLightColor = new(0, 0, 0, 0.6f);
        
        void Start()
        {
            backgroundImage = this.transform.Find("Highlight").GetComponent<Image>();
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

    }
}