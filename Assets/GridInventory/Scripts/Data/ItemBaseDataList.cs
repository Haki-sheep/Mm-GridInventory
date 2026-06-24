using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品配置列表
    /// 可按分类创建多个资产文件
    /// </summary>
    [CreateAssetMenu(fileName = "ItemBaseDataList", menuName = "MmInventory/Data/Item Base Data List")]
    public class ItemBaseDataList : ScriptableObject
    {
        [SerializeField]
        private List<ItemBaseData> itemDataList = new();

        public IReadOnlyList<ItemBaseData> ItemDataList => itemDataList;
    }
}
