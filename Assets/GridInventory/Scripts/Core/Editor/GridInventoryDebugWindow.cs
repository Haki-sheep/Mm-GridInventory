#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
  /// <summary>
  /// 网格背包 Core 调试面板
  /// </summary>
  public sealed class GridInventoryDebugWindow : EditorWindow
  {
    private const float MinCellSize = 28f;
    private const float MaxCellSize = 64f;
    private const float LeftPanelMinWidth = 240f;
    private const float LeftPanelMaxWidth = 400f;
    private const float LegendStripWidth = 52f;

    private static readonly Color EmptyColor = new(0.22f, 0.22f, 0.22f, 1f);
    private static readonly Color GridLineColor = new(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Color AnchorMarkColor = new(1f, 0.85f, 0.2f, 1f);
    private static readonly Color SelectColor = new(0.2f, 0.75f, 1f, 0.9f);

    private InventoryState inventoryState;
    private InventoryState.CellSnapshot[] cellSnapshots;

    private Vector2Int gridSize = new(6, 8);
    private Vector2Int mockItemSize = new(2, 1);
    private string mockItemLabel = "Mock";
    private float cellPixelSize = 40f;
    private Vector2 gridScrollPos;
    private Vector2 leftScrollPos;
    private Vector2Int? selectedPos;
    private string statusMessage = "就绪";

    private readonly List<EditorGridMockItem> mockItemList = new();
    private EditorGridMockItem activeMockItem;
    private int activeMockIndex;
    private IItemRuntime swapSourceItem;

    /// <summary>
    /// 打开窗口
    /// </summary>
    [MenuItem("Tools/MmInventory/Grid Debug")]
    private static void Open()
    {
      var window = GetWindow<GridInventoryDebugWindow>("Grid Debug");
      window.minSize = new Vector2(560f, 420f);
      window.Show();
    }

    private void OnEnable()
    {
      if (GridInventoryDebugSession.TryLoad(out var saveData))
        ApplySaveData(saveData);
      else
        RebuildInventory();
    }

    private void OnDisable()
    {
      SaveSession();
    }

    private void OnGUI()
    {
      float leftWidth = Mathf.Clamp(position.width * 0.30f, LeftPanelMinWidth, LeftPanelMaxWidth);

      using (new EditorGUILayout.HorizontalScope())
      {
        DrawLeftPanel(leftWidth);
        DrawRightPanel();
      }
    }

    /// <summary>
    /// 左侧面板
    /// </summary>
    private void DrawLeftPanel(float width)
    {
      using (new EditorGUILayout.VerticalScope(GUILayout.Width(width), GUILayout.ExpandHeight(true)))
      {
        using (var scroll = new EditorGUILayout.ScrollViewScope(leftScrollPos, GUILayout.ExpandHeight(true)))
        {
          leftScrollPos = scroll.scrollPosition;
          DrawLeftToolbar();
          EditorGUILayout.Space(6f);
          DrawGridSettings();
          EditorGUILayout.Space(6f);
          DrawMockItemSettings();
          EditorGUILayout.Space(6f);
          DrawSelectedCellInfo();
          EditorGUILayout.Space(6f);
          DrawSwapSection();
          EditorGUILayout.Space(6f);
          DrawPlacedItemList();
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
      }
    }

    /// <summary>
    /// 右侧网格面板
    /// </summary>
    private void DrawRightPanel()
    {
      if (inventoryState == null || cellSnapshots == null)
        return;

      using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
      {
        DrawGridScrollArea();
        DrawVerticalLegend();
      }
    }

    /// <summary>
    /// 左侧工具栏
    /// </summary>
    private void DrawLeftToolbar()
    {
      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("重建网格", GUILayout.Height(22f)))
          RebuildInventory();

        if (GUILayout.Button("清空物品", GUILayout.Height(22f)))
          ClearAllItems();

        if (GUILayout.Button("清除存档", GUILayout.Height(22f)))
          ClearSavedSession();
      }

      EditorGUILayout.LabelField($"尺寸 {gridSize.x} x {gridSize.y}  关闭窗口自动保存", EditorStyles.miniLabel);
    }

    /// <summary>
    /// 网格设置
    /// </summary>
    private void DrawGridSettings()
    {
      EditorGUILayout.LabelField("网格", EditorStyles.boldLabel);
      gridSize.x = EditorGUILayout.IntSlider("列 X", gridSize.x, 1, 16);
      gridSize.y = EditorGUILayout.IntSlider("行 Y", gridSize.y, 1, 16);
      cellPixelSize = EditorGUILayout.Slider("格像素", cellPixelSize, MinCellSize, MaxCellSize);
    }

    /// <summary>
    /// 模拟物品设置
    /// </summary>
    private void DrawMockItemSettings()
    {
      EditorGUILayout.LabelField("模拟物品", EditorStyles.boldLabel);
      mockItemLabel = EditorGUILayout.TextField("名称", mockItemLabel);
      mockItemSize.x = EditorGUILayout.IntField("宽", mockItemSize.x);
      mockItemSize.y = EditorGUILayout.IntField("高", mockItemSize.y);
      mockItemSize.x = Mathf.Max(1, mockItemSize.x);
      mockItemSize.y = Mathf.Max(1, mockItemSize.y);

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("新建物品", GUILayout.Height(22f)))
          CreateMockItem();

        if (GUILayout.Button("旋转模板", GUILayout.Height(22f)))
          mockItemSize = new Vector2Int(mockItemSize.y, mockItemSize.x);
      }

      DrawMockItemPicker();
    }

    /// <summary>
    /// 模拟物品选择
    /// </summary>
    private void DrawMockItemPicker()
    {
      if (mockItemList.Count == 0)
      {
        EditorGUILayout.HelpBox("先新建一个模拟物品", MessageType.None);
        return;
      }

      var nameList = new string[mockItemList.Count];
      for (int i = 0; i < mockItemList.Count; i++)
      {
        var item = mockItemList[i];
        nameList[i] = $"{item.Label} {item.DataSize.x}x{item.DataSize.y}";
      }

      activeMockIndex = EditorGUILayout.Popup("当前模板", activeMockIndex, nameList);
      activeMockIndex = Mathf.Clamp(activeMockIndex, 0, mockItemList.Count - 1);
      activeMockItem = mockItemList[activeMockIndex];

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("旋转模板物品", GUILayout.Height(22f)) && activeMockItem != null)
          activeMockItem.ToggleRotate();

        if (GUILayout.Button("删模板", GUILayout.Height(22f)))
        {
          mockItemList.RemoveAt(activeMockIndex);
          activeMockIndex = Mathf.Clamp(activeMockIndex, 0, Mathf.Max(0, mockItemList.Count - 1));
          activeMockItem = mockItemList.Count > 0 ? mockItemList[activeMockIndex] : null;
          SaveSession();
        }
      }
    }

    /// <summary>
    /// 选中格信息
    /// </summary>
    private void DrawSelectedCellInfo()
    {
      EditorGUILayout.LabelField("选中格", EditorStyles.boldLabel);
      if (!selectedPos.HasValue)
      {
        EditorGUILayout.LabelField("点击右侧格子选中", EditorStyles.miniLabel);
        return;
      }

      var pos = selectedPos.Value;
      EditorGUILayout.LabelField($"坐标 ({pos.x}, {pos.y})");

      if (cellSnapshots == null) return;

      int idx = pos.y * inventoryState.GridWidth + pos.x;
      if (idx < 0 || idx >= cellSnapshots.Length) return;

      ref var cell = ref cellSnapshots[idx];
      EditorGUILayout.LabelField(cell.IsOccupied ? "状态 占用" : "状态 空闲", EditorStyles.miniLabel);
      if (cell.IsAnchor)
        EditorGUILayout.LabelField("锚点格", EditorStyles.miniLabel);

      if (cell.OccupancyOwner is EditorGridMockItem mock)
        EditorGUILayout.LabelField($"物品 {mock.Label}", EditorStyles.miniLabel);

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("放置", GUILayout.Height(22f)))
          TryPlaceAt(pos);

        if (GUILayout.Button("移除", GUILayout.Height(22f)))
          TryRemoveAt(pos);
      }
    }

    /// <summary>
    /// 交换操作区
    /// </summary>
    private void DrawSwapSection()
    {
      EditorGUILayout.LabelField("交换", EditorStyles.boldLabel);

      if (swapSourceItem == null)
      {
        EditorGUILayout.LabelField("交换源 未设置", EditorStyles.miniLabel);
      }
      else
      {
        string sourceText = swapSourceItem is EditorGridMockItem mock
          ? $"{mock.Label} @({swapSourceItem.AnchorPos.x},{swapSourceItem.AnchorPos.y})"
          : $"@({swapSourceItem.AnchorPos.x},{swapSourceItem.AnchorPos.y})";
        EditorGUILayout.LabelField($"交换源 {sourceText}", EditorStyles.miniLabel);
      }

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("设为交换源", GUILayout.Height(22f)))
          SetSwapSourceFromSelection();

        if (GUILayout.Button("清除源", GUILayout.Height(22f)))
          ClearSwapSource();
      }

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("试算交换", GUILayout.Height(22f)))
          PreviewSwapAtSelected();

        if (GUILayout.Button("执行交换", GUILayout.Height(22f)))
          TrySwapAtSelected();
      }
    }

    /// <summary>
    /// 已放置物品列表
    /// </summary>
    private void DrawPlacedItemList()
    {
      EditorGUILayout.LabelField("网格内物品", EditorStyles.boldLabel);
      if (inventoryState == null) return;

      var itemList = inventoryState.GetAllAnchorItems();
      if (itemList.Count == 0)
      {
        EditorGUILayout.LabelField("无", EditorStyles.miniLabel);
        return;
      }

      for (int i = 0; i < itemList.Count; i++)
      {
        var item = itemList[i];
        string text = item is EditorGridMockItem mock
          ? $"{mock.Label} @({item.AnchorPos.x},{item.AnchorPos.y}) {item.DataSize.x}x{item.DataSize.y}"
          : $"{item.InstancedItemId.Substring(0, Mathf.Min(8, item.InstancedItemId.Length))} @({item.AnchorPos.x},{item.AnchorPos.y})";

        if (GUILayout.Button(text, EditorStyles.miniButton))
        {
          selectedPos = item.AnchorPos;
          swapSourceItem = item;
          statusMessage = $"已选为交换源 {text}";
        }
      }
    }

    /// <summary>
    /// 网格滚动区
    /// </summary>
    private void DrawGridScrollArea()
    {
      float gridPixelW = inventoryState.GridWidth * cellPixelSize;
      float gridPixelH = inventoryState.GridHeight * cellPixelSize;

      using (var scroll = new EditorGUILayout.ScrollViewScope(
               gridScrollPos,
               GUILayout.ExpandWidth(true),
               GUILayout.ExpandHeight(true)))
      {
        gridScrollPos = scroll.scrollPosition;

        var gridRect = GUILayoutUtility.GetRect(gridPixelW + 12f, gridPixelH + 12f);
        gridRect.x += 6f;
        gridRect.y += 6f;
        gridRect.width = gridPixelW;
        gridRect.height = gridPixelH;

        if (Event.current.type == EventType.Repaint)
          DrawGridCells(gridRect);

        HandleGridInput(gridRect);
      }
    }

    /// <summary>
    /// 竖向图例
    /// </summary>
    private void DrawVerticalLegend()
    {
      using (new EditorGUILayout.VerticalScope(GUILayout.Width(LegendStripWidth), GUILayout.ExpandHeight(true)))
      {
        GUILayout.Space(8f);
        DrawLegendSwatch(EmptyColor, "空闲");
        DrawLegendSwatch(GetPaletteColor(0), "占用");
        DrawLegendSwatch(AnchorMarkColor, "锚点");
        DrawLegendSwatch(SelectColor, "选中");
        GUILayout.FlexibleSpace();
      }
    }

    /// <summary>
    /// 绘制格子
    /// </summary>
    private void DrawGridCells(Rect gridRect)
    {
      int w = inventoryState.GridWidth;
      int h = inventoryState.GridHeight;

      var labelStyle = new GUIStyle(EditorStyles.miniLabel)
      {
        alignment = TextAnchor.MiddleCenter,
        normal = { textColor = Color.white }
      };

      for (int y = 0; y < h; y++)
      {
        for (int x = 0; x < w; x++)
        {
          int idx = y * w + x;
          ref var cell = ref cellSnapshots[idx];

          var cellRect = GetCellRect(gridRect, x, y);
          Color fill = cell.IsOccupied ? GetItemColor(cell.OccupancyOwner) : EmptyColor;

          EditorGUI.DrawRect(cellRect, fill);
          DrawRectBorder(cellRect, GridLineColor, 1f);

          if (selectedPos.HasValue && selectedPos.Value.x == x && selectedPos.Value.y == y)
            DrawRectBorder(cellRect, SelectColor, 2f);

          if (cell.IsAnchor)
          {
            var markRect = new Rect(cellRect.x + 2f, cellRect.y + 2f, 10f, 10f);
            EditorGUI.DrawRect(markRect, AnchorMarkColor);
          }

          if (swapSourceItem != null && cell.OccupancyOwner == swapSourceItem)
            DrawRectBorder(cellRect, new Color(1f, 0.5f, 0.1f, 0.9f), 2f);

          GUI.Label(cellRect, $"{x},{y}", labelStyle);
        }
      }
    }

    private void DrawLegendSwatch(Color color, string text)
    {
      using (new EditorGUILayout.HorizontalScope())
      {
        var r = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f));
        EditorGUI.DrawRect(r, color);
        GUILayout.Label(text, EditorStyles.miniLabel);
      }

      GUILayout.Space(4f);
    }

    /// <summary>
    /// 格子输入
    /// </summary>
    private void HandleGridInput(Rect gridRect)
    {
      var evt = Event.current;
      if (!gridRect.Contains(evt.mousePosition) || evt.type != EventType.MouseDown)
        return;

      if (!TryGetGridPos(gridRect, evt.mousePosition, out var gridPos))
        return;

      selectedPos = gridPos;

      if (evt.button == 0)
        TryPlaceAt(gridPos);
      else if (evt.button == 1)
        TryRemoveAt(gridPos);
      else if (evt.button == 2 && inventoryState.GetItemByMask(gridPos) is IItemRuntime item)
      {
        swapSourceItem = item;
        statusMessage = $"中键设为交换源 @({gridPos.x},{gridPos.y})";
      }

      evt.Use();
      Repaint();
    }

    private Rect GetCellRect(Rect gridRect, int x, int y)
    {
      return new Rect(
        gridRect.x + x * cellPixelSize,
        gridRect.y + y * cellPixelSize,
        cellPixelSize,
        cellPixelSize);
    }

    private bool TryGetGridPos(Rect gridRect, Vector2 mousePos, out Vector2Int gridPos)
    {
      gridPos = default;
      if (!gridRect.Contains(mousePos)) return false;

      int x = Mathf.FloorToInt((mousePos.x - gridRect.x) / cellPixelSize);
      int y = Mathf.FloorToInt((mousePos.y - gridRect.y) / cellPixelSize);
      if (x < 0 || y < 0 || x >= inventoryState.GridWidth || y >= inventoryState.GridHeight)
        return false;

      gridPos = new Vector2Int(x, y);
      return true;
    }

    private static void DrawRectBorder(Rect rect, Color color, float thickness)
    {
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
      EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
      EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void RebuildInventory()
    {
      gridSize.x = Mathf.Max(1, gridSize.x);
      gridSize.y = Mathf.Max(1, gridSize.y);
      inventoryState = new InventoryState(gridSize);
      cellSnapshots = new InventoryState.CellSnapshot[gridSize.x * gridSize.y];
      selectedPos = null;
      swapSourceItem = null;
      statusMessage = $"已重建 {gridSize.x}x{gridSize.y} 网格";
      RefreshSnapshots();
      SaveSession();
    }

    private void RefreshSnapshots()
    {
      if (inventoryState == null || cellSnapshots == null) return;
      inventoryState.FillCellSnapshots(cellSnapshots);
    }

    private void ClearAllItems()
    {
      mockItemList.Clear();
      activeMockItem = null;
      activeMockIndex = 0;
      swapSourceItem = null;
      RebuildInventory();
      statusMessage = "已清空";
      SaveSession();
    }

    private void CreateMockItem()
    {
      var item = new EditorGridMockItem(mockItemLabel, mockItemSize);
      mockItemList.Add(item);
      activeMockIndex = mockItemList.Count - 1;
      activeMockItem = item;
      statusMessage = $"已创建模板 {item.Label} {item.DataSize.x}x{item.DataSize.y}";
      SaveSession();
    }

    private void TryPlaceAt(Vector2Int anchorPos)
    {
      if (inventoryState == null) return;

      if (activeMockItem == null)
      {
        statusMessage = "请先新建并选择模拟物品";
        return;
      }

      var placeItem = activeMockItem.Clone();
      placeItem.SetAnchorPos(anchorPos);
      if (!inventoryState.CanPlace(placeItem, anchorPos))
      {
        statusMessage = $"无法放置到 ({anchorPos.x},{anchorPos.y})";
        RefreshSnapshots();
        return;
      }

      inventoryState.SetAt(anchorPos, placeItem);
      statusMessage = $"已放置 {placeItem.Label} @({anchorPos.x},{anchorPos.y})";
      RefreshSnapshots();
      Repaint();
      SaveSession();
    }

    private void TryRemoveAt(Vector2Int pos)
    {
      if (inventoryState == null) return;

      var itemBefore = inventoryState.GetItemByMask(pos);
      if (!inventoryState.RemoveAtAny(pos))
      {
        statusMessage = $"({pos.x},{pos.y}) 无物品可移除";
        RefreshSnapshots();
        return;
      }

      if (swapSourceItem != null && itemBefore == swapSourceItem)
        swapSourceItem = null;

      statusMessage = $"已移除 ({pos.x},{pos.y}) 上的物品";
      RefreshSnapshots();
      Repaint();
      SaveSession();
    }

    private void SetSwapSourceFromSelection()
    {
      if (!selectedPos.HasValue)
      {
        statusMessage = "请先选中一格";
        return;
      }

      var item = inventoryState.GetItemByMask(selectedPos.Value);
      if (item == null)
      {
        statusMessage = "选中格无物品";
        return;
      }

      swapSourceItem = item;
      statusMessage = $"交换源 @({item.AnchorPos.x},{item.AnchorPos.y})";
      Repaint();
    }

    private void ClearSwapSource()
    {
      swapSourceItem = null;
      statusMessage = "已清除交换源";
      Repaint();
    }

    private void PreviewSwapAtSelected()
    {
      if (!TryGetSwapContext(out var source, out var placeAnchor, out var target))
        return;

      bool canSwap = inventoryState.CanSwap(source, target, placeAnchor);
      statusMessage = canSwap
        ? $"可交换 源@({source.AnchorPos.x},{source.AnchorPos.y}) → 落点({placeAnchor.x},{placeAnchor.y})"
        : $"不可交换 @({placeAnchor.x},{placeAnchor.y})";
    }

    private void TrySwapAtSelected()
    {
      if (!TryGetSwapContext(out var source, out var placeAnchor, out var target))
        return;

      if (!inventoryState.CanSwap(source, target, placeAnchor))
      {
        statusMessage = $"交换失败 不可交换 @({placeAnchor.x},{placeAnchor.y})";
        return;
      }

      var displacedList = new List<IItemRuntime>();
      if (!inventoryState.TrySwap(source, target, displacedList, placeAnchor))
      {
        statusMessage = $"交换失败 @({placeAnchor.x},{placeAnchor.y})";
        RefreshSnapshots();
        return;
      }

      swapSourceItem = null;
      statusMessage = displacedList.Count > 0
        ? $"交换成功 挤出 {displacedList.Count} 个物品"
        : $"交换成功 @({placeAnchor.x},{placeAnchor.y})";
      RefreshSnapshots();
      Repaint();
      SaveSession();
    }

    /// <summary>
    /// 收集当前会话数据
    /// </summary>
    private GridDebugSaveData CollectSaveData()
    {
      var templateSaveList = new GridDebugTemplateSave[mockItemList.Count];
      for (int i = 0; i < mockItemList.Count; i++)
      {
        var template = mockItemList[i];
        templateSaveList[i] = new GridDebugTemplateSave
        {
          label = template.Label,
          sizeX = template.DataSize.x,
          sizeY = template.DataSize.y,
          isRotated = template.IsRotated
        };
      }

      var placedSaveList = new List<GridDebugPlacedSave>();
      if (inventoryState != null)
      {
        var anchorItemList = inventoryState.GetAllAnchorItems();
        for (int i = 0; i < anchorItemList.Count; i++)
        {
          if (anchorItemList[i] is not EditorGridMockItem placed) continue;
          placedSaveList.Add(new GridDebugPlacedSave
          {
            label = placed.Label,
            sizeX = placed.DataSize.x,
            sizeY = placed.DataSize.y,
            isRotated = placed.IsRotated,
            anchorX = placed.AnchorPos.x,
            anchorY = placed.AnchorPos.y,
            colorIndex = placed.ColorIndex,
            instanceId = placed.InstancedItemId
          });
        }
      }

      return new GridDebugSaveData
      {
        gridWidth = gridSize.x,
        gridHeight = gridSize.y,
        cellPixelSize = cellPixelSize,
        mockLabel = mockItemLabel,
        mockSizeX = mockItemSize.x,
        mockSizeY = mockItemSize.y,
        activeMockIndex = activeMockIndex,
        templates = templateSaveList,
        placedItems = placedSaveList.ToArray()
      };
    }

    /// <summary>
    /// 写入 EditorPrefs
    /// </summary>
    private void SaveSession()
    {
      GridInventoryDebugSession.Save(CollectSaveData());
    }

    /// <summary>
    /// 从存档恢复
    /// </summary>
    private void ApplySaveData(GridDebugSaveData data)
    {
      gridSize = new Vector2Int(Mathf.Max(1, data.gridWidth), Mathf.Max(1, data.gridHeight));
      cellPixelSize = Mathf.Clamp(data.cellPixelSize, MinCellSize, MaxCellSize);
      mockItemLabel = string.IsNullOrEmpty(data.mockLabel) ? "Mock" : data.mockLabel;
      mockItemSize = new Vector2Int(Mathf.Max(1, data.mockSizeX), Mathf.Max(1, data.mockSizeY));

      mockItemList.Clear();
      if (data.templates != null)
      {
        for (int i = 0; i < data.templates.Length; i++)
        {
          var template = data.templates[i];
          var item = EditorGridMockItem.FromSave(
            template.label,
            new Vector2Int(Mathf.Max(1, template.sizeX), Mathf.Max(1, template.sizeY)),
            template.isRotated,
            i,
            null);
          mockItemList.Add(item);
        }
      }

      activeMockIndex = data.templates != null && data.templates.Length > 0
        ? Mathf.Clamp(data.activeMockIndex, 0, data.templates.Length - 1)
        : 0;
      activeMockItem = mockItemList.Count > 0 ? mockItemList[activeMockIndex] : null;

      inventoryState = new InventoryState(gridSize);
      cellSnapshots = new InventoryState.CellSnapshot[gridSize.x * gridSize.y];
      selectedPos = null;
      swapSourceItem = null;

      if (data.placedItems != null)
      {
        for (int i = 0; i < data.placedItems.Length; i++)
        {
          var placed = data.placedItems[i];
          var anchor = new Vector2Int(placed.anchorX, placed.anchorY);
          var item = EditorGridMockItem.FromSave(
            placed.label,
            new Vector2Int(Mathf.Max(1, placed.sizeX), Mathf.Max(1, placed.sizeY)),
            placed.isRotated,
            placed.colorIndex,
            placed.instanceId);
          item.SetAnchorPos(anchor);
          if (inventoryState.CanPlace(item, anchor))
            inventoryState.SetAt(anchor, item);
        }
      }

      RefreshSnapshots();
      statusMessage = "已恢复上次会话";
    }

    /// <summary>
    /// 清除本地存档
    /// </summary>
    private void ClearSavedSession()
    {
      GridInventoryDebugSession.Clear();
      statusMessage = "已清除存档";
    }

    private bool TryGetSwapContext(out IItemRuntime source, out Vector2Int placeAnchor, out IItemRuntime target)
    {
      source = swapSourceItem;
      target = null;
      placeAnchor = default;

      if (source == null)
      {
        statusMessage = "请先设置交换源";
        return false;
      }

      if (!selectedPos.HasValue)
      {
        statusMessage = "请先选中落点格";
        return false;
      }

      placeAnchor = selectedPos.Value;
      if (!inventoryState.TryGetSwapTargetItem(source, placeAnchor, out target) || target == null)
      {
        statusMessage = $"落点 ({placeAnchor.x},{placeAnchor.y}) 无有效交换目标";
        return false;
      }

      if (source.InstancedItemId == target.InstancedItemId)
      {
        statusMessage = "不能和自己交换";
        return false;
      }

      return true;
    }

    private static Color GetItemColor(IItemRuntime item)
    {
      if (item is EditorGridMockItem mock)
        return GetPaletteColor(mock.ColorIndex);

      int hash = item.InstancedItemId.GetHashCode();
      return GetPaletteColor(Mathf.Abs(hash) % 8);
    }

    private static Color GetPaletteColor(int index)
    {
      Color[] palette =
      {
        new(0.35f, 0.55f, 0.85f),
        new(0.40f, 0.75f, 0.45f),
        new(0.85f, 0.45f, 0.40f),
        new(0.75f, 0.50f, 0.85f),
        new(0.45f, 0.80f, 0.80f),
        new(0.90f, 0.70f, 0.35f),
        new(0.60f, 0.60f, 0.90f),
        new(0.85f, 0.55f, 0.70f)
      };
      return palette[index % palette.Length];
    }
  }
}
#endif
