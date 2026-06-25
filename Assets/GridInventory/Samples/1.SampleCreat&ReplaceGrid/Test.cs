using System;
using System.Collections.Generic;
using MmInventory;
using UnityEngine;

public class Test : MonoBehaviour
{
    public ItemView item1X2A;
    public ItemView item1X2B;
    public ItemView item1x1A;
    public ItemView item1x1B;
    public ItemView item1x1C;
    public ItemView item2x2A;
    internal void Init(GridMainContainerView gridMainContainerView,InventoryViewModel inventoryViewModel, Dictionary<string, ItemView> itemViewDict)
    {
        item1X2A.Init(gridMainContainerView.IGridAudioAndAnimation);
        item1X2B.Init(gridMainContainerView.IGridAudioAndAnimation);
        item1x1A.Init(gridMainContainerView.IGridAudioAndAnimation);
        item1x1B.Init(gridMainContainerView.IGridAudioAndAnimation);
        item1x1C.Init(gridMainContainerView.IGridAudioAndAnimation);
        item2x2A.Init(gridMainContainerView.IGridAudioAndAnimation);

        inventoryViewModel.PlaceItem(item1X2A.ItemData, new Vector2Int(0, 0));
        inventoryViewModel.PlaceItem(item1X2B.ItemData, new Vector2Int(3, 0));
        inventoryViewModel.PlaceItem(item1x1A.ItemData, new Vector2Int(0, 2));
        inventoryViewModel.PlaceItem(item1x1B.ItemData, new Vector2Int(1, 2));
        inventoryViewModel.PlaceItem(item1x1C.ItemData, new Vector2Int(0, 3));
        inventoryViewModel.PlaceItem(item2x2A.ItemData, new Vector2Int(4, 2));

        itemViewDict.Add(item1X2A.ItemData.InstancedItemId, item1X2A);
        itemViewDict.Add(item1X2B.ItemData.InstancedItemId, item1X2B);
        itemViewDict.Add(item1x1A.ItemData.InstancedItemId, item1x1A);
        itemViewDict.Add(item1x1B.ItemData.InstancedItemId, item1x1B);
        itemViewDict.Add(item1x1C.ItemData.InstancedItemId, item1x1C);
        itemViewDict.Add(item2x2A.ItemData.InstancedItemId, item2x2A);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
