using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// Inspector 中文显示名
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ChineseLabelAttribute : PropertyAttribute
    {
        /// <summary> 中文标签 </summary>
        public readonly string Label;

        public ChineseLabelAttribute(string label)
        {
            Label = label;
        }
    }
}
