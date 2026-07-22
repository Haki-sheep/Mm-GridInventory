using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品稀有度 白蓝紫金红
    /// </summary>
    [Serializable]
    public enum EItemRarity : byte
    {
        [InspectorName("白")]
        White = 0,

        [InspectorName("蓝")]
        Blue = 1,

        [InspectorName("紫")]
        Purple = 2,

        [InspectorName("金")]
        Gold = 3,

        [InspectorName("红")]
        Red = 4
    }
}
