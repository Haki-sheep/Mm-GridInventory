#if UNITY_EDITOR
using System;
using UnityEngine;

namespace MmInventory.Editor
{
  /// <summary>
  /// Editor 用占格物品
  /// </summary>
  public sealed class EditorGridMockItem : IGridItem
  {
    private static int s_NextColorIndex;

    /// <summary> 显示名 </summary>
    public string Label { get; }

    /// <summary> 调试色索引 </summary>
    public int ColorIndex { get; }

    public string InstancedItemId { get; }
    public Vector2Int AnchorPos { get; private set; }
    public Vector2Int DataSize { get; private set; }
    public bool IsRotated { get; private set; }

    /// <summary>
    /// 创建模拟物品
    /// </summary>
    public EditorGridMockItem(string label, Vector2Int dataSize)
    {
      Label = string.IsNullOrEmpty(label) ? "Item" : label;
      DataSize = dataSize;
      InstancedItemId = Guid.NewGuid().ToString("N");
      ColorIndex = s_NextColorIndex++;
    }

    /// <summary>
    /// 从存档恢复
    /// </summary>
    public static EditorGridMockItem FromSave(
      string label,
      Vector2Int dataSize,
      bool isRotated,
      int colorIndex,
      string instanceId)
    {
      var item = new EditorGridMockItem(label, dataSize, isRotated, colorIndex, instanceId);
      return item;
    }

    /// <summary>
    /// 存档用内部构造
    /// </summary>
    private EditorGridMockItem(
      string label,
      Vector2Int dataSize,
      bool isRotated,
      int colorIndex,
      string instanceId)
    {
      Label = string.IsNullOrEmpty(label) ? "Item" : label;
      DataSize = dataSize;
      IsRotated = isRotated;
      InstancedItemId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
      ColorIndex = colorIndex;
      s_NextColorIndex = Mathf.Max(s_NextColorIndex, colorIndex + 1);
    }

    /// <summary>
    /// 设置锚点
    /// </summary>
    public void SetAnchorPos(Vector2Int anchorPos)
    {
      AnchorPos = anchorPos;
    }

    /// <summary>
    /// 切换旋转
    /// </summary>
    public void ToggleRotate()
    {
      IsRotated = !IsRotated;
      DataSize = new Vector2Int(DataSize.y, DataSize.x);
    }
    /// <summary>
    /// 克隆一份用于放置
    /// </summary>
    public EditorGridMockItem Clone()
    {
      return new EditorGridMockItem(Label, DataSize)
      {
        IsRotated = IsRotated
      };
    }
  }
}
#endif
