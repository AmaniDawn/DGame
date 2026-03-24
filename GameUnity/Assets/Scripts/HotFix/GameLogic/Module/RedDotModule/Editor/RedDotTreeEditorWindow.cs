#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 红点树可视化编辑器窗口
    /// </summary>
    public class RedDotTreeEditorWindow : EditorWindow
    {
        private RedDotTreeConfig m_config;
        private Vector2 m_scrollPosition;
        private RedDotNodeConfig m_selectedNode;
        private RedDotNodeConfig m_draggedNode;
        private List<RedDotNodeConfig> m_draggedNodeList;
        private bool m_isDragging;
        private RedDotNodeConfig m_dropTargetNode;
        private DropPosition m_dropPosition;

        private enum DropPosition
        {
            None,
            Above,      // 插入到目标节点上方
            Below,      // 插入到目标节点下方
            AsChild     // 作为目标节点的子节点
        }

        // 样式
        private GUIStyle m_nodeStyle;
        private GUIStyle m_selectedNodeStyle;
        private GUIStyle m_pathLabelStyle;
        private GUIStyle m_headerStyle;

        // 颜色
        private readonly Color m_selectedColor = new Color(0.3f, 0.6f, 0.9f, 0.3f);
        private readonly Color m_dragTargetColor = new Color(0.9f, 0.6f, 0.3f, 0.3f);
        private readonly Color m_duplicateNameColor = new Color(0.9f, 0.3f, 0.3f, 0.3f);

        // 重复路径检测
        private HashSet<string> m_duplicatePaths = new HashSet<string>();

        [MenuItem("DGame Tools/RedDot/Tree Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<RedDotTreeEditorWindow>("RedDot Tree Editor");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        /// <summary>
        /// 双击 RedDotTreeConfig 资源时打开编辑器窗口
        /// </summary>
        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset =
#if UNITY_6000_0_OR_NEWER
                EditorUtility.EntityIdToObject(instanceID) as RedDotTreeConfig;
#else
                EditorUtility.InstanceIDToObject(instanceID) as RedDotTreeConfig;
#endif

            if (asset == null)
                return false;

            var window = GetWindow<RedDotTreeEditorWindow>("RedDot Tree Editor");
            window.minSize = new Vector2(400, 300);
            window.SetConfig(asset);
            window.Show();
            return true;
        }

        /// <summary>
        /// 设置要编辑的配置
        /// </summary>
        public void SetConfig(RedDotTreeConfig config)
        {
            m_config = config;
            if (m_config != null)
            {
                m_config.RefreshPaths();
            }
            Repaint();
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnDisable()
        {
            if (m_config != null)
            {
                RedDotTreeCodeGenerator.GenerateCode(m_config, false);
            }
        }

        private void LoadConfig()
        {
            // 尝试加载现有配置
            string[] guids = AssetDatabase.FindAssets("t:RedDotTreeConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_config = AssetDatabase.LoadAssetAtPath<RedDotTreeConfig>(path);
            }
        }

        private void InitStyles()
        {
            if (m_nodeStyle == null)
            {
                m_nodeStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(4, 4, 2, 2)
                };
            }

            if (m_selectedNodeStyle == null)
            {
                m_selectedNodeStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(4, 4, 2, 2),
                    fontStyle = FontStyle.Bold
                };
            }

            if (m_pathLabelStyle == null)
            {
                m_pathLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = Color.gray }
                };
            }

            if (m_headerStyle == null)
            {
                m_headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14
                };
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // 工具栏
            DrawToolbar();

            if (m_config == null)
            {
                DrawNoConfigMessage();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // 左侧：树形视图
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));
            DrawTreeView();
            EditorGUILayout.EndVertical();

            // 分隔线
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));

            // 右侧：节点详情
            EditorGUILayout.BeginVertical();
            DrawNodeInspector();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // 处理拖拽
            HandleDragAndDrop();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 配置选择
            EditorGUI.BeginChangeCheck();
            m_config = (RedDotTreeConfig)EditorGUILayout.ObjectField(m_config, typeof(RedDotTreeConfig), false, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck() && m_config != null)
            {
                m_config.RefreshPaths();
            }

            if (GUILayout.Button("New Config", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CreateNewConfig();
            }

            GUILayout.FlexibleSpace();

            // 操作按钮
            GUI.enabled = m_config != null;

            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                SetAllFoldout(true);
            }

            if (GUILayout.Button("Collapse", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SetAllFoldout(false);
            }

            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("Generate Code", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                GenerateCode();
            }
            GUI.backgroundColor = Color.white;

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoConfigMessage()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("Please select or create a RedDotTreeConfig asset.", MessageType.Info);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Create New Config", GUILayout.Height(30)))
            {
                CreateNewConfig();
            }
        }

        private void CreateNewConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create RedDot Tree Config",
                "RedDotTreeConfig",
                "asset",
                "Choose a location to save the config");

            if (!string.IsNullOrEmpty(path))
            {
                m_config = ScriptableObject.CreateInstance<RedDotTreeConfig>();
                AssetDatabase.CreateAsset(m_config, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = m_config;
            }
        }

        private void DrawTreeView()
        {
            EditorGUILayout.LabelField("Red Dot Tree", m_headerStyle);
            EditorGUILayout.Space(5);

            // 检测重复路径
            RefreshDuplicatePaths();

            // 显示重复路径警告
            if (m_duplicatePaths.Count > 0)
            {
                EditorGUILayout.HelpBox($"存在重复的节点路径: {string.Join(", ", m_duplicatePaths)}", MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            // 添加根节点按钮
            if (GUILayout.Button("+ Add Root Node", GUILayout.Height(24)))
            {
                AddRootNode();
            }

            EditorGUILayout.Space(5);

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            for (int i = 0; i < m_config.RootNodes.Count; i++)
            {
                DrawNode(m_config.RootNodes[i], 0, m_config.RootNodes, i);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 检测并刷新重复路径列表
        /// </summary>
        private void RefreshDuplicatePaths()
        {
            m_duplicatePaths.Clear();
            var pathCount = new Dictionary<string, int>();

            foreach (var root in m_config.RootNodes)
            {
                CollectNodePaths(root, pathCount);
            }

            foreach (var kvp in pathCount)
            {
                if (kvp.Value > 1)
                {
                    m_duplicatePaths.Add(kvp.Key);
                }
            }
        }

        private void CollectNodePaths(RedDotNodeConfig node, Dictionary<string, int> pathCount)
        {
            if (node == null) return;

            string path = node.generatedPath;
            if (!string.IsNullOrEmpty(path))
            {
                if (pathCount.ContainsKey(path))
                    pathCount[path]++;
                else
                    pathCount[path] = 1;
            }

            foreach (var child in node.children)
            {
                CollectNodePaths(child, pathCount);
            }
        }

        private void DrawNode(RedDotNodeConfig node, int depth, List<RedDotNodeConfig> parentList, int index)
        {
            if (node == null) return;

            // 拖拽目标：上方插入指示线
            if (m_isDragging && m_dropTargetNode == node && m_dropPosition == DropPosition.Above)
            {
                Rect lineRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(2));
                EditorGUI.DrawRect(lineRect, m_dragTargetColor);
                EditorGUILayout.EndHorizontal();
            }

            // 绘制当前节点行
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

            // 检查是否是重复路径节点
            bool isDuplicatePath = m_duplicatePaths.Contains(node.generatedPath);

            // 拖拽目标高亮（作为子节点）
            if (m_isDragging && m_dropTargetNode == node && m_dropPosition == DropPosition.AsChild)
            {
                EditorGUI.DrawRect(rowRect, m_dragTargetColor);
            }
            // 重复路径高亮
            else if (isDuplicatePath)
            {
                EditorGUI.DrawRect(rowRect, m_duplicateNameColor);
            }
            // 选中高亮
            else if (m_selectedNode == node)
            {
                EditorGUI.DrawRect(rowRect, m_selectedColor);
            }

            // 子节点缩进（根据深度）
            if (depth > 0)
            {
                GUILayout.Space(depth * 20);
            }

            // 折叠箭头
            if (node.children.Count > 0)
            {
                string arrow = node.foldout ? "▼" : "▶";
                if (GUILayout.Button(arrow, EditorStyles.label, GUILayout.Width(16)))
                {
                    node.foldout = !node.foldout;
                }
            }
            else
            {
                GUILayout.Space(16);
            }

            // 节点图标（重复路径显示警告图标）
            GUIContent icon = isDuplicatePath
                ? EditorGUIUtility.IconContent("console.warnicon.sml")
                : EditorGUIUtility.IconContent("d_Favorite Icon");
            GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));

            // 节点名称（可点击选中）
            var style = m_selectedNode == node ? m_selectedNodeStyle : m_nodeStyle;
            if (GUILayout.Button(string.IsNullOrEmpty(node.description) ? node.name : node.description, style, GUILayout.MinWidth(60)))
            {
                m_selectedNode = node;
            }

            // 路径预览
            GUILayout.Label($"({node.generatedPath})", m_pathLabelStyle);

            GUILayout.FlexibleSpace();

            // 操作按钮
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                AddChildNode(node);
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Node",
                    $"Are you sure you want to delete '{node.name}' and all its children?", "Yes", "No"))
                {
                    parentList.RemoveAt(index);
                    if (m_selectedNode == node) m_selectedNode = null;
                    MarkDirty();
                }
            }

            // 拖拽句柄
            GUILayout.Label("≡", GUILayout.Width(18));
            Rect dragRect = GUILayoutUtility.GetLastRect();
            HandleNodeDrag(node, parentList, index, dragRect, rowRect);

            EditorGUILayout.EndHorizontal();

            // 拖拽目标：下方插入指示线
            if (m_isDragging && m_dropTargetNode == node && m_dropPosition == DropPosition.Below)
            {
                Rect lineRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(2));
                EditorGUI.DrawRect(lineRect, m_dragTargetColor);
                EditorGUILayout.EndHorizontal();
            }

            // 递归绘制子节点（展开时）
            if (node.foldout && node.children.Count > 0)
            {
                for (int i = 0; i < node.children.Count; i++)
                {
                    DrawNode(node.children[i], depth + 1, node.children, i);
                }
            }
        }

        private void HandleNodeDrag(RedDotNodeConfig node, List<RedDotNodeConfig> parentList, int index, Rect dragRect, Rect rowRect)
        {
            Event e = Event.current;

            // 开始拖拽
            if (e.type == EventType.MouseDown && dragRect.Contains(e.mousePosition))
            {
                m_isDragging = true;
                m_draggedNode = node;
                m_draggedNodeList = parentList;
                m_dropTargetNode = null;
                m_dropPosition = DropPosition.None;
                e.Use();
            }

            // 拖拽过程中：检测目标位置
            if (m_isDragging && m_draggedNode != node && rowRect.Contains(e.mousePosition))
            {
                // 不能拖拽到自己的子节点
                if (IsDescendantOf(node, m_draggedNode))
                {
                    m_dropTargetNode = null;
                    m_dropPosition = DropPosition.None;
                }
                else
                {
                    m_dropTargetNode = node;

                    // 根据鼠标在行内的位置决定插入方式
                    float relativeY = (e.mousePosition.y - rowRect.y) / rowRect.height;

                    if (relativeY < 0.25f)
                    {
                        m_dropPosition = DropPosition.Above;
                    }
                    else if (relativeY > 0.75f)
                    {
                        m_dropPosition = DropPosition.Below;
                    }
                    else
                    {
                        m_dropPosition = DropPosition.AsChild;
                    }

                    Repaint();
                }
            }
        }

        /// <summary>
        /// 检查 node 是否是 potentialAncestor 的后代
        /// </summary>
        private bool IsDescendantOf(RedDotNodeConfig node, RedDotNodeConfig potentialAncestor)
        {
            foreach (var child in potentialAncestor.children)
            {
                if (child == node || IsDescendantOf(node, child))
                    return true;
            }
            return false;
        }

        private void HandleDragAndDrop()
        {
            Event e = Event.current;

            if (!m_isDragging)
                return;

            // 拖拽中显示自定义光标
            if (e.type == EventType.Repaint)
            {
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.MoveArrow);
            }

            // 释放鼠标：执行移动
            if (e.type == EventType.MouseUp)
            {
                if (m_dropTargetNode != null && m_dropPosition != DropPosition.None)
                {
                    PerformNodeMove();
                }

                // 重置拖拽状态
                m_isDragging = false;
                m_draggedNode = null;
                m_draggedNodeList = null;
                m_dropTargetNode = null;
                m_dropPosition = DropPosition.None;
                Repaint();
            }

            // 按 Esc 取消拖拽
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                m_isDragging = false;
                m_draggedNode = null;
                m_draggedNodeList = null;
                m_dropTargetNode = null;
                m_dropPosition = DropPosition.None;
                e.Use();
                Repaint();
            }
        }

        private void PerformNodeMove()
        {
            if (m_draggedNode == null || m_dropTargetNode == null)
                return;

            // 从原位置移除
            m_draggedNodeList.Remove(m_draggedNode);

            // 找到目标节点所在的列表
            List<RedDotNodeConfig> targetList = FindParentList(m_dropTargetNode);
            if (targetList == null)
                return;

            int targetIndex = targetList.IndexOf(m_dropTargetNode);

            switch (m_dropPosition)
            {
                case DropPosition.Above:
                    targetList.Insert(targetIndex, m_draggedNode);
                    break;

                case DropPosition.Below:
                    targetList.Insert(targetIndex + 1, m_draggedNode);
                    break;

                case DropPosition.AsChild:
                    m_dropTargetNode.children.Add(m_draggedNode);
                    m_dropTargetNode.foldout = true;
                    break;
            }

            m_config.RefreshPaths();
            MarkDirty();
        }

        private List<RedDotNodeConfig> FindParentList(RedDotNodeConfig node)
        {
            // 检查是否在根节点列表中
            if (m_config.RootNodes.Contains(node))
                return m_config.RootNodes;

            // 递归查找
            foreach (var root in m_config.RootNodes)
            {
                var result = FindParentListRecursive(root, node);
                if (result != null)
                    return result;
            }

            return null;
        }

        private List<RedDotNodeConfig> FindParentListRecursive(RedDotNodeConfig parent, RedDotNodeConfig target)
        {
            if (parent.children.Contains(target))
                return parent.children;

            foreach (var child in parent.children)
            {
                var result = FindParentListRecursive(child, target);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void DrawNodeInspector()
        {
            EditorGUILayout.LabelField("Node Properties", m_headerStyle);
            EditorGUILayout.Space(5);

            if (m_selectedNode == null)
            {
                EditorGUILayout.HelpBox("Select a node to edit its properties.", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();

            // 节点名称
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel);
            m_selectedNode.name = EditorGUILayout.TextField(m_selectedNode.name);

            EditorGUILayout.Space(5);

            // 生成的路径（只读）
            EditorGUILayout.LabelField("Generated Path", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(m_selectedNode.generatedPath);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            // 描述
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            var textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            m_selectedNode.description = EditorGUILayout.TextArea(m_selectedNode.description, textAreaStyle, GUILayout.Height(40));

            EditorGUILayout.Space(5);

            // 类型
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel);
            m_selectedNode.type = (RedDotType)EditorGUILayout.EnumPopup(m_selectedNode.type);

            EditorGUILayout.Space(5);

            // 聚合策略
            EditorGUILayout.LabelField("Aggregate Strategy", EditorStyles.boldLabel);
            m_selectedNode.strategy = (RedDotAggregateStrategy)EditorGUILayout.EnumPopup(m_selectedNode.strategy);

            EditorGUILayout.Space(10);

            // 子节点信息
            EditorGUILayout.LabelField($"Children: {m_selectedNode.children.Count}", EditorStyles.miniLabel);

            if (EditorGUI.EndChangeCheck())
            {
                m_config.RefreshPaths();
                MarkDirty();
            }

            EditorGUILayout.Space(10);

            // 快捷操作
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Child Node"))
            {
                AddChildNode(m_selectedNode);
            }

            if (GUILayout.Button("Duplicate Node"))
            {
                DuplicateNode(m_selectedNode);
            }

            EditorGUILayout.Space(10);

            // 复制路径按钮
            if (GUILayout.Button("Copy Path to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = m_selectedNode.generatedPath;
                Debug.Log($"Copied: {m_selectedNode.generatedPath}");
            }
        }

        private void AddRootNode()
        {
            var newNode = new RedDotNodeConfig("NewRoot");
            m_config.RootNodes.Add(newNode);
            m_config.RefreshPaths();
            m_selectedNode = newNode;
            MarkDirty();
        }

        private void AddChildNode(RedDotNodeConfig parent)
        {
            var newNode = new RedDotNodeConfig("NewNode");
            parent.children.Add(newNode);
            parent.foldout = true;
            m_config.RefreshPaths();
            m_selectedNode = newNode;
            MarkDirty();
        }

        private void DuplicateNode(RedDotNodeConfig node)
        {
            var copy = new RedDotNodeConfig(node.name + "_Copy")
            {
                description = node.description,
                type = node.type,
                strategy = node.strategy
            };

            // 找到父节点并添加
            foreach (var root in m_config.RootNodes)
            {
                if (TryAddAfterNode(root, node, copy, m_config.RootNodes))
                {
                    m_config.RefreshPaths();
                    m_selectedNode = copy;
                    MarkDirty();
                    return;
                }
            }

            // 如果是根节点
            int index = m_config.RootNodes.IndexOf(node);
            if (index >= 0)
            {
                m_config.RootNodes.Insert(index + 1, copy);
                m_config.RefreshPaths();
                m_selectedNode = copy;
                MarkDirty();
            }
        }

        private bool TryAddAfterNode(RedDotNodeConfig current, RedDotNodeConfig target, RedDotNodeConfig newNode, List<RedDotNodeConfig> parentList)
        {
            int index = current.children.IndexOf(target);
            if (index >= 0)
            {
                current.children.Insert(index + 1, newNode);
                return true;
            }

            foreach (var child in current.children)
            {
                if (TryAddAfterNode(child, target, newNode, current.children))
                    return true;
            }

            return false;
        }

        private void SetAllFoldout(bool foldout)
        {
            foreach (var root in m_config.RootNodes)
            {
                SetNodeFoldout(root, foldout);
            }
            Repaint();
        }

        private void SetNodeFoldout(RedDotNodeConfig node, bool foldout)
        {
            node.foldout = foldout;
            foreach (var child in node.children)
            {
                SetNodeFoldout(child, foldout);
            }
        }

        private void MarkDirty()
        {
            if (m_config != null)
            {
                EditorUtility.SetDirty(m_config);
            }
        }

        #region Code Generation

        private void GenerateCode()
        {
            RedDotTreeCodeGenerator.GenerateCode(m_config, true);
        }

        #endregion
    }
}

#endif
