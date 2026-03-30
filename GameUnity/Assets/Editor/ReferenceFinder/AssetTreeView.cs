using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

#pragma warning disable CS0618 // 禁用过时警告

namespace DGame
{
    internal sealed class AssetTreeView : TreeView
    {
        private const float K_ICON_WIDTH = 18f;
        private const float K_ICON_HEIGHT = 20f;

        private readonly GUIStyle m_stateGuiStyle = new GUIStyle()
            { richText = true, alignment = TextAnchor.MiddleCenter };

        public AssetViewItem assetRoot;

        public AssetTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            // 设置行高
            rowHeight = K_ICON_HEIGHT;
            // 树形折叠箭头在第0列
            columnIndexForTreeFoldouts = 0;
            // 显示交替行背景色
            showAlternatingRowBackgrounds = true;
            // 不显示边框
            showBorder = false;
            // 折叠箭头垂直偏移
            customFoldoutYOffset = (K_ICON_HEIGHT - EditorGUIUtility.singleLineHeight) * 0.5f;
            // 图标前的额外空间
            extraSpaceBeforeIconAndLabel = K_ICON_WIDTH;
        }

        protected override TreeViewItem BuildRoot() => assetRoot;

        protected override void DoubleClickedItem(int id)
        {
            // 双击资源
            AssetViewItem item = (AssetViewItem)FindItem(id, rootItem);

            if (item != null)
            {
                Object assetObject = AssetDatabase.LoadAssetAtPath(item.data.path, typeof(Object));
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = assetObject;
                EditorGUIUtility.PingObject(assetObject);
            }
        }

        // 展开状态变化
        protected override void ExpandedStateChanged() => SortExpandItem();

        public void SortExpandItem()
        {
            // 无排序时直接返回
            if (SortHelper.CurSortType == SortType.None)
            {
                return;
            }

            // 获取所有展开项的ID
            IList<int> expandItemList = GetExpanded();

            // 对每个展开项的子项进行排序
            foreach (var i in expandItemList)
            {
                var item = (AssetViewItem)FindItem(i, rootItem);
                SortHelper.SortChild(item.data);
            }
            // 标记需要更新树视图
            var curWindow = EditorWindow.GetWindow<ResourceReferenceInfo>();
            curWindow.needUpdateAssetTree = true;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth, bool isDepend)
        {
            List<MultiColumnHeaderState.Column> columns = new List<MultiColumnHeaderState.Column>()
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("名称"),
                    headerTextAlignment = TextAlignment.Center,
                    // 列的初始排序方向
                    sortedAscending = false,
                    width = 200,
                    minWidth = 60,
                    // 自动调整大小
                    autoResize = false,
                    // 不可隐藏
                    allowToggleVisibility = false,
                    // 可排序
                    canSort = true,
                    sortingArrowAlignment = TextAlignment.Center
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("路径"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = false,
                    width = 360,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = true,
                    sortingArrowAlignment = TextAlignment.Center
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("状态"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = false,
                    width = 60,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true,
                    canSort = false
                }
            };
            if (!isDepend)
            {
                columns.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("引用数量"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = false,
                    width = 60,
                    minWidth = 60,
                    // 自动调整大小
                    autoResize = true,
                    allowToggleVisibility = true,
                    canSort = false
                });
            }
            MultiColumnHeaderState state = new MultiColumnHeaderState(columns.ToArray());
            return state;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            AssetViewItem item = (AssetViewItem)args.item;

            // 遍历所有可见列进行渲染
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, AssetViewItem item, MyColumns column, ref RowGUIArgs args)
        {
            // 垂直居中矩形
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case MyColumns.Name:
                    Rect iconRect = cellRect;
                    // 根据层级缩进
                    iconRect.x += GetContentIndent(item);
                    iconRect.width = K_ICON_WIDTH;
                    if (iconRect.x < cellRect.xMax)
                    {
                        Texture2D icon = GetIcon(item.data.path);
                        if (icon != null)
                            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    }

                    args.rowRect = cellRect;
                    // 调用基类渲染树形结构
                    base.RowGUI(args);
                    break;

                case MyColumns.Path:
                    GUI.Label(cellRect, item.data.path);
                    break;

                case MyColumns.State:
                    GUI.Label(cellRect, ReferenceFinderData.GetInfoByState(item.data.state), m_stateGuiStyle);
                    break;

                case MyColumns.RefCount:
                    GUI.Label(cellRect, ResourceReferenceInfo.Data.GetRefCount(item.data, (item.parent as AssetViewItem)?.data), m_stateGuiStyle);
                    break;
            }
        }

        private Texture2D GetIcon(string path)
        {
            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));

            if (obj)
            {
                // 获取迷你缩略图
                Texture2D icon = AssetPreview.GetMiniThumbnail(obj);

                if (!icon)
                {
                    // 获取类型图标
                    icon = AssetPreview.GetMiniTypeThumbnail(obj.GetType());
                }
                return icon;
            }
            return null;
        }

        private enum MyColumns
        {
            Name,
            Path,
            State,
            RefCount
        }
    }
}

#pragma warning disable CS0618 // 禁用过时警告