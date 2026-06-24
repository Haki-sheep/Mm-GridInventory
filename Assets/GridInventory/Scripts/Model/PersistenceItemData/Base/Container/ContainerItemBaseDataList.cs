using System.Collections.Generic;
using UnityEngine;
using System;
namespace MmInventory
{   
    /// <summary>
    /// 容器类物品列表
    /// 用于存储所有容器类物品的数据资产
    /// </summary>
    [CreateAssetMenu(fileName = "ContainerItemBaseDataList", menuName = "MmInventory/Data/Container/Container Item Base Data List")]
    [Serializable]
    public class ContainerItemBaseDataList : ScriptableObject
    {
         public List<ContainerItemBaseData> itemList;
    }
}