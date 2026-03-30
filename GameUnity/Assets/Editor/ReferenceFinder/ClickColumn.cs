using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace DGame
{
    // MultiColumnHeader ： 创建可点击排序的列标题
    internal sealed class ClickColumn : MultiColumnHeader
    {
        public delegate void SortInColumn();

        public static Dictionary<int, SortInColumn> SortWithIndex = new Dictionary<int, SortInColumn>()
        {
            {0, SortByName},
            {1, SortByPath}
        };

        private static void SortByPath() => SortHelper.SortByName();

        private static void SortByName() => SortHelper.SortByPath();

        // 启用列排序功能
        public ClickColumn(MultiColumnHeaderState state) : base(state) => canSort = true;

        protected override void ColumnHeaderClicked(MultiColumnHeaderState.Column column, int columnIndex)
        {
            base.ColumnHeaderClicked(column, columnIndex);

            if (SortWithIndex.TryGetValue(columnIndex, out SortInColumn sortInColumn))
            {
                // 执行对应的排序方法
                sortInColumn?.Invoke();
                // 获取当前窗口并刷新显示
                var curWindow = EditorWindow.GetWindow<ResourceReferenceInfo>();
                curWindow.mAssetTreeView.SortExpandItem();
            }
        }
    }
}