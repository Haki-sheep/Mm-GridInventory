using UnityEngine;
using UnityEngine.UI;
public enum EFrameBoard
{
    Normal,
    CanPlace,
    CanPlaceSwap,
    CanStack,
    CannotPlace,
    Hidden,
}

public class FrameBoardView : MonoBehaviour
{  

    private Image frameBoard;
    private RectTransform frameBoardBackground;
    public Color NormalColor = new Color(1, 1, 1, 0.3f);
    public Color CanPlaceColor = Color.green;
    public Color CannotPlaceColor = Color.red;
    public Color CanPlaceSwapColor = Color.blue;
    public Color CanStackColor = Color.yellow;
    void Start()
    {
        frameBoard = this.GetComponent<Image>();
        frameBoardBackground = this.transform as RectTransform;
    }
    /// <summary>
    /// 设置吸附框信息
    /// </summary>
    /// <param name="frameBoardColor">吸附框</param>
    public void SetFrameBoardView(EFrameBoard frameBoardColor, Vector2 pos, Vector2 size)
    {

        // 显示和隐藏
        if (frameBoardColor == EFrameBoard.Hidden)
        {
            if (this.gameObject.activeSelf) this.gameObject.SetActive(false);
            return;
        }

        if (!this.gameObject.activeSelf) this.gameObject.SetActive(true);

        // 颜色
        switch (frameBoardColor)
        {
            case EFrameBoard.Normal:
                frameBoard.color = NormalColor;
                break;
            case EFrameBoard.CanPlace:
                frameBoard.color = CanPlaceColor;
                break;
            case EFrameBoard.CanPlaceSwap:
                frameBoard.color = CanPlaceSwapColor;
                break;
            case EFrameBoard.CanStack:
                frameBoard.color = CanStackColor;
                break;
            case EFrameBoard.CannotPlace:
                frameBoard.color = CannotPlaceColor;
                break;
            default:
                frameBoard.color = NormalColor;
                break;
        }

        // 位置和大小
        frameBoardBackground.localPosition = pos;
        frameBoardBackground.sizeDelta = size;
    }

}
