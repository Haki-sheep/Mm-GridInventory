#if UNITY_EDITOR
using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 物品配置 Odin 树形编辑器
    /// </summary>
    public sealed class ItemDataEditorWindow : OdinMenuEditorWindow
    {
        private const string BaseTitle = "Item Data Editor";

        private ItemDataEditorHomePage homePage;

        /// <summary> 是否有未保存修改 </summary>
        private bool isDirty;

        /// <summary>
        /// 主页签
        /// </summary>
        public ItemDataEditorHomePage HomePage => homePage;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/MmInventory/Item Data Editor")]
        private static void Open()
        {
            var window = GetWindow<ItemDataEditorWindow>();
            window.minSize = new Vector2(900f, 560f);
            window.ClearDirty();
            window.Show();
        }

        /// <summary>
        /// 标记为已修改
        /// </summary>
        public void MarkDirty()
        {
            if (isDirty) return;
            isDirty = true;
            UpdateTitle();
        }

        /// <summary>
        /// 清除修改标记
        /// </summary>
        public void ClearDirty()
        {
            if (!isDirty) return;
            isDirty = false;
            UpdateTitle();
        }

        /// <summary>
        /// 保存全部并清除修改标记
        /// </summary>
        public void SaveAll()
        {
            if (homePage == null) return;

            homePage.ListSo?.EditorSave();
            homePage.ViewPrefabListSo?.EditorSave();

            if (!string.IsNullOrEmpty(homePage.EnumFilePath))
                EItemTypeCodeGenerator.WriteEnum(homePage.EnumNameList, homePage.EnumFilePath);

            ClearDirty();
        }

        /// <summary>
        /// 更新窗口标题
        /// </summary>
        private void UpdateTitle()
        {
            titleContent = new GUIContent(isDirty ? $"{BaseTitle} *" : BaseTitle);
        }

        protected override void OnImGUI()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown
                && currentEvent.control
                && currentEvent.keyCode == KeyCode.S)
            {
                SaveAll();
                currentEvent.Use();
            }

            base.OnImGUI();
        }

        /// <summary>
        /// 重建目录树
        /// </summary>
        public void RequestTreeRebuild()
        {
            ForceMenuTreeRebuild();
            Repaint();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            if (homePage == null)
            {
                homePage = new ItemDataEditorHomePage(this);
                homePage.LoadFromSession();
            }
            else
            {
                homePage.Window = this;
            }

            var tree = new OdinMenuTree(supportsMultiSelect: false)
            {
                Config = { DrawSearchToolbar = true }
            };

            tree.Add("枚举管理器", homePage);
            tree.Add("视图预制体", new ItemViewPrefabPage(this, homePage));

            var listSo = homePage.ListSo;
            if (listSo == null)
                return tree;

            var enumNameList = homePage.EnumNameList;
            var itemList = listSo.ItemDataList;

            for (int t = 0; t < enumNameList.Count; t++)
            {
                string typeName = enumNameList[t];
                if (string.IsNullOrWhiteSpace(typeName)) continue;

                string folderPath = $"条目/{typeName}";
                tree.Add(folderPath, new ItemDataCategoryPage(this, homePage, typeName));

                for (int i = 0; i < itemList.Count; i++)
                {
                    var item = itemList[i];
                    if (!string.Equals(item.ItemType.ToString(), typeName, StringComparison.Ordinal))
                        continue;

                    string itemLabel = string.IsNullOrEmpty(item.Name)
                        ? $"Item_{item.ExcelItemId}"
                        : item.Name;

                    tree.Add($"{folderPath}/{itemLabel}", new ItemDataEntryPage(this, homePage, i));
                }
            }

            return tree;
        }
    }
}
#endif
