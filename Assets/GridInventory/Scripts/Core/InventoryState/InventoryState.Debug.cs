#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
  /// <summary>
  /// 该脚本用于编辑器调试，提供一些调试方法
  /// </summary>
  public partial class InventoryState
  {
    /// <summary> 背包列数 </summary>
    public int GridWidth => inventorySize.x;

    /// <summary> 背包行数 </summary>
    public int GridHeight => inventorySize.y;

    /// <summary>
    /// 格子快照
    /// </summary>
    public struct CellSnapshot
    {
      /// <summary> 坐标 </summary>
      public Vector2Int Pos;

      /// <summary> 占用者 </summary>
      public IItemRuntime OccupancyOwner;

      /// <summary> 锚点物品 </summary>
      public IItemRuntime AnchorItem;

      /// <summary> 是否被占用 </summary>
      public bool IsOccupied => OccupancyOwner != null;

      /// <summary> 是否为锚点格 </summary>
      public bool IsAnchor => AnchorItem != null;
    }

    /// <summary>
    /// 填充全部格子快照
    /// </summary>
    public void FillCellSnapshots(CellSnapshot[] buffer)
    {
      int total = inventorySize.x * inventorySize.y;
      for (int i = 0; i < total; i++)
      {
        buffer[i] = new CellSnapshot
        {
          Pos = ToVector2Int(i),
          OccupancyOwner = occupancyOwnerArray[i],
          AnchorItem = itemAnchorArray[i]
        };
      }
    }

    /// <summary>
    /// 获取所有锚点物品
    /// </summary>
    public List<IItemRuntime> GetAllAnchorItems()
    {
      var itemList = new List<IItemRuntime>();
      var idHashList = new HashSet<string>();
      for (int i = 0; i < itemAnchorArray.Length; i++)
      {
        var item = itemAnchorArray[i];
        if (item is null || !idHashList.Add(item.InstancedItemId)) continue;
        itemList.Add(item);
      }
      return itemList;
    }
  }
  #endif
}
