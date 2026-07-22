#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// Loot 序列化类型中文绘制
    /// </summary>
    [CustomPropertyDrawer(typeof(LootDensityProfile))]
    public sealed class LootDensityProfileDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawChildren(position, property, label, new[]
            {
                ("emptyChance", "空箱概率"),
                ("rarityWeightList", "稀有度权重")
            });
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return CalcChildrenHeight(property, new[]
            {
                "emptyChance",
                "rarityWeightList"
            }, label);
        }

        /// <summary>
        /// 绘制子字段
        /// </summary>
        internal static void DrawChildren(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            (string fieldName, string chineseLabel)[] fieldList)
        {
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y;
            if (label != null && label != GUIContent.none && !string.IsNullOrEmpty(label.text))
            {
                var headerRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);
                y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                if (!property.isExpanded)
                {
                    EditorGUI.EndProperty();
                    return;
                }
            }
            else
            {
                property.isExpanded = true;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < fieldList.Length; i++)
            {
                var childProperty = property.FindPropertyRelative(fieldList[i].fieldName);
                if (childProperty == null)
                    continue;

                float height = EditorGUI.GetPropertyHeight(childProperty, true);
                var rect = new Rect(position.x, y, position.width, height);
                EditorGUI.PropertyField(
                    rect,
                    childProperty,
                    new GUIContent(fieldList[i].chineseLabel),
                    true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 计算子字段总高度
        /// </summary>
        internal static float CalcChildrenHeight(
            SerializedProperty property,
            string[] fieldNameList,
            GUIContent label)
        {
            float height = 0f;
            bool showHeader = label != null && label != GUIContent.none && !string.IsNullOrEmpty(label.text);
            if (showHeader)
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                if (!property.isExpanded)
                    return height;
            }

            for (int i = 0; i < fieldNameList.Length; i++)
            {
                var childProperty = property.FindPropertyRelative(fieldNameList[i]);
                if (childProperty == null)
                    continue;

                height += EditorGUI.GetPropertyHeight(childProperty, true);
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }

    /// <summary>
    /// 稀有度权重中文绘制
    /// </summary>
    [CustomPropertyDrawer(typeof(LootRarityWeight))]
    public sealed class LootRarityWeightDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LootDensityProfileDrawer.DrawChildren(position, property, label, new[]
            {
                ("itemRarity", "稀有度"),
                ("weight", "权重")
            });
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LootDensityProfileDrawer.CalcChildrenHeight(
                property,
                new[] { "itemRarity", "weight" },
                label);
        }
    }

    /// <summary>
    /// 内容方向表中文绘制
    /// </summary>
    [CustomPropertyDrawer(typeof(LootContentTable))]
    public sealed class LootContentTableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LootDensityProfileDrawer.DrawChildren(position, property, label, new[]
            {
                ("contentId", "内容方向ID"),
                ("itemCountMin", "最少物品个数"),
                ("itemCountMax", "最多物品个数"),
                ("allowedItemTypeList", "允许物品类型")
            });
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LootDensityProfileDrawer.CalcChildrenHeight(
                property,
                new[] { "contentId", "itemCountMin", "itemCountMax", "allowedItemTypeList" },
                label);
        }
    }

    /// <summary>
    /// 稀有度枚举中文显示
    /// </summary>
    [CustomPropertyDrawer(typeof(EItemRarity))]
    public sealed class EItemRarityDrawer : PropertyDrawer
    {
        private static readonly string[] RarityLabelList =
        {
            "白",
            "蓝",
            "紫",
            "金",
            "红"
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int index = Mathf.Clamp(property.enumValueIndex, 0, RarityLabelList.Length - 1);
            int newIndex = EditorGUI.Popup(position, label.text, index, RarityLabelList);
            property.enumValueIndex = newIndex;
        }
    }
}
#endif
