#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 中文标签属性绘制 不修改共享 GUIContent 避开数组套用
    /// </summary>
    [CustomPropertyDrawer(typeof(ChineseLabelAttribute))]
    public sealed class ChineseLabelDrawer : PropertyDrawer
    {
        /// <summary>
        /// 绘制字段
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var content = CreateLabel(label);

            // 数组与列表交给默认绘制 避免元素被错误套用父级标签
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, content, true);
                return;
            }

            var rangeAttr = GetRangeAttribute();
            if (rangeAttr != null && property.propertyType == SerializedPropertyType.Float)
            {
                EditorGUI.Slider(position, property, rangeAttr.min, rangeAttr.max, content);
                return;
            }

            if (rangeAttr != null && property.propertyType == SerializedPropertyType.Integer)
            {
                EditorGUI.IntSlider(position, property, (int)rangeAttr.min, (int)rangeAttr.max, content);
                return;
            }

            EditorGUI.PropertyField(position, property, content, true);
        }

        /// <summary>
        /// 计算高度
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var content = CreateLabel(label);
            return EditorGUI.GetPropertyHeight(property, content, true);
        }

        /// <summary>
        /// 创建独立中文标签
        /// </summary>
        private GUIContent CreateLabel(GUIContent label)
        {
            var attr = (ChineseLabelAttribute)attribute;
            return new GUIContent(attr.Label, label != null ? label.tooltip : string.Empty);
        }

        /// <summary>
        /// 读取同字段 Range
        /// </summary>
        private RangeAttribute GetRangeAttribute()
        {
            if (fieldInfo == null)
                return null;

            var attributeArray = fieldInfo.GetCustomAttributes(typeof(RangeAttribute), true);
            if (attributeArray == null || attributeArray.Length == 0)
                return null;

            return attributeArray[0] as RangeAttribute;
        }
    }
}
#endif
