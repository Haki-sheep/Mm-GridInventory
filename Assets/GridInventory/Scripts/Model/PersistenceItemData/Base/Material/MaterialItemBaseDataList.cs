using  UnityEngine;
using System.Collections.Generic;
using System;
namespace MmInventory
{
    /// <summary>
    /// 材料类物品列表
    /// 用于存储所有材料类物品的数据资产
    /// </summary>
    [CreateAssetMenu(fileName = "MaterialItemBaseDataList", menuName = "MmInventory/Data/Material/Material Item Base Data List")]
    [Serializable]
    public class MaterialItemBaseDataList : ScriptableObject
    {
        [SerializeField] public List<MaterialItemBaseData> itemList;
    }
}