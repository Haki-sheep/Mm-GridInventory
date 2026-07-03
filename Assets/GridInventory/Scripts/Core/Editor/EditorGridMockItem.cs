#if UNITY_EDITOR
using System;
using UnityEngine;

namespace MmInventory.Editor
{
  /// <summary>
  /// Editor 用占格物品
  /// </summary>
  public sealed class EditorGridMockItem : IItemRuntime
  {
    private static int s_NextColorIndex;

    /// <summary> 显示名 </summary>
    public string Label { get; }

    /// <summary> 调试色索引 </summary>
    public int ColorIndex { get; }

    /// <summary> 模拟配表ID </summary>
    public int ExcelItemId { get; }

    public string InstancedItemId { get; }
    public Vector2Int AnchorPos { get; private set; }
    public Vector2Int DataSize { get; private set; }
    public bool IsRotated { get; private set; }

    public EItemStackType ItemStackType { get; set; }
    public int MaxStackCount { get; set; } = 1;
    public int CurrStackCount { get; set; } = 1;

    /// <summary>
    /// 创建模拟物品
    /// </summary>
    public EditorGridMockItem(string label, Vector2Int dataSize)
    {
      Label = string.IsNullOrEmpty(label) ? "Item" : label;
      DataSize = dataSize;
      ExcelItemId = Label.GetHashCode();
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
      ExcelItemId = Label.GetHashCode();
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

    /// <summary>
    /// 拆出新堆
    /// </summary>
    IItemRuntime IItemRuntime.Clone(int stackCount)
    {
      return new EditorGridMockItem(Label, DataSize)
      {
        IsRotated = IsRotated,
        CurrStackCount = stackCount
      };
    }

        public void SetRotated(bool rotated)
        {
            IsRotated = rotated;
            DataSize = new Vector2Int(DataSize.y, DataSize.x);
        }
    }
}
#endif
