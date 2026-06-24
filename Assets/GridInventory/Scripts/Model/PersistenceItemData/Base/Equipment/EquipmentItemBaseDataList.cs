using UnityEngine;
using System.Collections.Generic;
using System;
namespace MmInventory
{
    /// <summary>
    /// 装备类物品列表
    /// 用于存储所有装备类物品的数据资产
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentItemBaseDataList", menuName = "MmInventory/Data/Equipment/Equipment Item Base Data List")]
    [Serializable]
    public class EquipmentItemBaseDataList : ScriptableObject
    {
         public List<EquipmentItemBaseData> itemList;
    }
}