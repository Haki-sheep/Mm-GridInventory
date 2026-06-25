#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
  /// <summary>
  /// 模板存档条目
  /// </summary>
  [Serializable]
  public struct GridDebugTemplateSave
  {
    public string label;
    public int sizeX;
    public int sizeY;
    public bool isRotated;
  }

  /// <summary>
  /// 网格内物品存档条目
  /// </summary>
  [Serializable]
  public struct GridDebugPlacedSave
  {
    public string label;
    public int sizeX;
    public int sizeY;
    public bool isRotated;
    public int anchorX;
    public int anchorY;
    public int colorIndex;
    public string instanceId;
  }

  /// <summary>
  /// 调试面板存档
  /// </summary>
  [Serializable]
  public class GridDebugSaveData
  {
    public int gridWidth = 6;
    public int gridHeight = 8;
    public float cellPixelSize = 40f;
    public string mockLabel = "Mock";
    public int mockSizeX = 2;
    public int mockSizeY = 1;
    public int activeMockIndex;
    public GridDebugTemplateSave[] templates = Array.Empty<GridDebugTemplateSave>();
    public GridDebugPlacedSave[] placedItems = Array.Empty<GridDebugPlacedSave>();
  }

  /// <summary>
  /// EditorPrefs 读写
  /// </summary>
  public static class GridInventoryDebugSession
  {
    private const string PrefsKey = "MmInventory.GridDebug.Session.v1";

    /// <summary>
    /// 保存
    /// </summary>
    public static void Save(GridDebugSaveData data)
    {
      if (data == null) return;
      EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
    }

    /// <summary>
    /// 读取
    /// </summary>
    public static bool TryLoad(out GridDebugSaveData data)
    {
      data = null;
      if (!EditorPrefs.HasKey(PrefsKey)) return false;

      string json = EditorPrefs.GetString(PrefsKey, string.Empty);
      if (string.IsNullOrEmpty(json)) return false;

      data = JsonUtility.FromJson<GridDebugSaveData>(json);
      return data != null;
    }

    /// <summary>
    /// 清除存档
    /// </summary>
    public static void Clear()
    {
      EditorPrefs.DeleteKey(PrefsKey);
    }
  }
}
#endif
