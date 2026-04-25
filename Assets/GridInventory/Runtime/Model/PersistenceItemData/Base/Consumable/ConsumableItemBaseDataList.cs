using System.Collections.Generic;
using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 消耗品列表  
    /// 用于存储所有消耗品的数据资产
    /// </summary>
    [CreateAssetMenu(fileName = "ConsumableItemBaseDataList", menuName = "MmInventory/Data/Consumable/Consumable Item Base Data List")]
    [Serializable]
    public class ConsumableItemBaseDataList : ScriptableObject
    {
        public List<ConsumableItemBaseData> itemList;
    }
}