#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Type = System.Type;
using static VInspector.VInspector;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
// using static VTools.VDebug;


namespace VInspector
{
    public class VInspectorComponentWindow : EditorWindow
    {

        void OnGUI()
        {
            if (!component) component = EditorUtility.InstanceIDToObject(componentIid) as Component;
            if (!component) { Close(); return; }
            if (!editor) { Init(component); skipHeightUpdate = true; }


            void background()
            {
                position.SetPos(0, 0).Draw(GUIColors.windowBackground);
            }
            void header()
            {
                var headerRect = ExpandWidthLabelRect(18).Resize(-1).AddWidthFromMid(6);
                var closeButtonRect = headerRect.SetWidthFromRight(16).SetHeightFromMid(16).Move(-4, 0);

                var backgroundColor = isDarkTheme ? Greyscale(.25f) : GUIColors.windowBackground;

                void startDragging()
                {
                    if (isResizingVertically) return;
                    if (isResizingHorizontally) return;
                    if (isDragged) return;
                    if (!curEvent.isMouseDrag) return;
                    if (!headerRect.IsHovered()) return;


                    draggedInstance = this;

                    dragStartMousePos = curEvent.mousePosition_screenSpace;
                    dragStartWindowPos = position.position;

                }
                void updateDragging()
                {
                    if (!isDragged) return;


                    var draggedPosition = dragStartWindowPos + curEvent.mousePosition_screenSpace - dragStartMousePos;

                    if (!curEvent.isRepaint)
                        position = position.SetPos(draggedPosition);


                    EditorGUIUtility.hotControl = EditorGUIUtility.GetControlID(FocusType.Passive);

                }
                void stopDragging()
                {
                    if (!isDragged) return;
                    if (!curEvent.isMouseMove && !curEvent.isMouseUp) return;


                    draggedInstance = null;

                    EditorGUIUtility.hotControl = 0;

                }

                void background()
                {
                    headerRect.Draw(backgroundColor);

                    headerRect.SetHeightFromBottom(1).Draw(isDarkTheme ? Greyscale(.2f) : Greyscale(.7f));

                }
                void icon()
                {
                    var iconRect = headerRect.SetWidth(20).MoveX(14).MoveY(-1);

                    if (!componentIcons_byType.ContainsKey(component.GetType()))
                        componentIcons_byType[component.GetType()] = EditorGUIUtility.ObjectContent(component, component.GetType()).image;

                    GUI.Label(iconRect, componentIcons_byType[component.GetType()]);

                }
                void toggle()
                {
                    var toggleRect = headerRect.MoveX(36).SetSize(20, 20);


                    var pi_enabled = component.GetType().GetProperty("enabled") ??
                                     component.GetType().BaseType?.GetProperty("enabled") ??
                                     component.GetType().BaseType?.BaseType?.GetProperty("enabled") ??
                                     component.GetType().BaseType?.BaseType?.BaseType?.GetProperty("enabled");


                    if (pi_enabled == null) return;

                    var enabled = (bool)pi_enabled.GetValue(component);


                    if (GUI.Toggle(toggleRect, enabled, "") == enabled) return;

                    component.RecordUndo();
                    pi_enabled.SetValue(component, !enabled);

                }
                void name()
                {
                    var nameRect = headerRect.MoveX(54).MoveY(-1);


                    var s = new GUIContent(EditorGUIUtility.ObjectContent(component, component.GetType())).text;
                    s = s.Substring(s.LastIndexOf('(') + 1);
                    s = s.Substring(0, s.Length - 1);

                    if (instances.Any(r => r.component.GetType() == component.GetType() && r.component != component))
                        s += " - " + component.gameObject.name;


                    SetLabelBold();

                    GUI.Label(nameRect, s);

                    ResetLabelStyle();

                }
                void nameCurtain()
                {
                    var flatColorRect = headerRect.SetX(closeButtonRect.x + 3).SetXMax(headerRect.xMax);
                    var gradientRect = headerRect.SetXMax(flatColorRect.x).SetWidthFromRight(30);

                    flatColorRect.Draw(backgroundColor);
                    gradientRect.DrawCurtainLeft(backgroundColor);

                }
                void closeButton()
                {
                    var iconName = "CrossIcon";
                    var iconSize = 14;
                    var color = isDarkTheme ? Greyscale(.65f) : Greyscale(.35f);
                    var colorHovered = isDarkTheme ? Greyscale(.9f) : color;
                    var colorPressed = color;


                    if (!IconButton(closeButtonRect, iconName, iconSize, color, colorHovered, colorPressed)) return;

                    Close();

                    EditorGUIUtility.ExitGUI();

                }
                void rightClick()
                {
                    if (!curEvent.isMouseDown) return;
                    if (curEvent.mouseButton != 1) return;
                    if (!headerRect.IsHovered()) return;

                    typeof(EditorUtility).InvokeMethod("DisplayObjectContextMenu", Rect.zero.SetPos(curEvent.mousePosition), component, 0);

                }

                startDragging();
                updateDragging();
                stopDragging();

                background();
                icon();
                toggle();
                name();
                nameCurtain();
                closeButton();
                rightClick();

            }
            void body_imgui()
            {
                if (useUITK) return;


                EditorGUIUtility.labelWidth = (this.position.width * .4f).Max(120);


                scrollPosition = EditorGUILayout.BeginScrollView(Vector2.up * scrollPosition).y;
                BeginIndent(17);


                editor?.OnInspectorGUI();

                updateHeight_imgui();


                EndIndent(1);
                EditorGUILayout.EndScrollView();


                EditorGUIUtility.labelWidth = 0;

            }
            void outline()
            {
                if (Application.platform == RuntimePlatform.OSXEditor) return;

                position.SetPos(0, 0).DrawOutline(Greyscale(.1f));

            }

            void updateHeight_imgui()
            {
                if (useUITK) return;


                ExpandWidthLabelRect(height: -5);

                if (!curEvent.isRepaint) return;
                if (isResizingVertically) return;


                targetHeight = lastRect.y + 30;

                position = position.SetHeight(targetHeight.Min(maxHeight));


                prevHeight = position.height;

            }
            void updateHeight_uitk()
            {
                if (!useUITK) return;
                if (!curEvent.isRepaint) return;
                if (skipHeightUpdate) { skipHeightUpdate = false; return; } // crashses otherwise


                var lastElement = inspectorElement[inspectorElement.childCount - 1];

                targetHeight = lastElement.contentRect.yMax + 33;

                position = position.SetHeight(targetHeight);

            }

            void horizontalResize()
            {
                var showingScrollbar = targetHeight > maxHeight;

                var resizeArea = this.position.SetPos(0, 0).SetWidthFromRight(showingScrollbar ? 3 : 5).AddHeightFromBottom(-20);

                void startResize()
                {
                    if (isDragged) return;
                    if (isResizingHorizontally) return;
                    if (!curEvent.isMouseDown && !curEvent.isMouseDrag) return;
                    if (!resizeArea.IsHovered()) return;

                    isResizingHorizontally = true;

                    resizeStartMousePos = curEvent.mousePosition_screenSpace;
                    resizeStartWindowSize = this.position.size;

                }
                void updateResize()
                {
                    if (!isResizingHorizontally) return;


                    var resizedWidth = resizeStartWindowSize.x + curEvent.mousePosition_screenSpace.x - resizeStartMousePos.x;

                    var width = resizedWidth.Max(300);

                    if (!curEvent.isRepaint)
                        position = position.SetWidth(width);


                    EditorGUIUtility.hotControl = EditorGUIUtility.GetControlID(FocusType.Passive);
                    // GUI.focused

                }
                void stopResize()
                {
                    if (!isResizingHorizontally) return;
                    if (!curEvent.isMouseUp) return;

                    isResizingHorizontally = false;

                    EditorGUIUtility.hotControl = 0;

                }


                EditorGUIUtility.AddCursorRect(resizeArea, MouseCursor.ResizeHorizontal);

                startResize();
                updateResize();
                stopResize();

            }
            void verticalResize()
            {
                var resizeArea = this.position.SetPos(0, 0).SetHeightFromBottom(5);

                void startResize()
                {
                    if (isDragged) return;
                    if (isResizingVertically) return;
                    if (!curEvent.isMouseDown && !curEvent.isMouseDrag) return;
                    if (!resizeArea.IsHovered()) return;

                    isResizingVertically = true;

                    resizeStartMousePos = curEvent.mousePosition_screenSpace;
                    resizeStartWindowSize = this.position.size;

                }
                void updateResize()
                {
                    if (!isResizingVertically) return;


                    var resizedHeight = resizeStartWindowSize.y + curEvent.mousePosition_screenSpace.y - resizeStartMousePos.y;

                    var height = resizedHeight.Min(targetHeight).Max(50);

                    if (!curEvent.isRepaint)
                        position = position.SetHeight(height);

                    maxHeight = height;


                    EditorGUIUtility.hotControl = EditorGUIUtility.GetControlID(FocusType.Passive);

                }
                void stopResize()
                {
                    if (!isResizingVertically) return;
                    if (!curEvent.isMouseUp) return;

                    isResizingVertically = false;

                    EditorGUIUtility.hotControl = 0;

                }


                EditorGUIUtility.AddCursorRect(resizeArea, MouseCursor.ResizeVertical);

                startResize();
                updateResize();
                stopResize();

            }



            background();
            header();


            horizontalResize();
            verticalResize();


            Space(3);
            body_imgui();
            outline();

            Space(7);

            updateHeight_uitk();


            if (isDragged)
                Repaint();

            EditorApplication.delayCall -= Repaint;
            EditorApplication.delayCall += Repaint;

        }

        public bool isDragged => draggedInstance == this;
        public Vector2 dragStartMousePos;
        public Vector2 dragStartWindowPos;

        public bool isResizingHorizontally;
        public bool isResizingVertically;
        public Vector2 resizeStartMousePos;
        public Vector2 resizeStartWindowSize;

        public float scrollPosition;

        bool skipHeightUpdate;

        public float targetHeight;
        public float maxHeight;
        public float prevHeight;

        static Dictionary<System.Type, Texture> componentIcons_byType = new();








        public void Init(Component component)
        {
            if (editor)
                editor.DestroyImmediate();

            this.component = component;
            this.editor = Editor.CreateEditor(component);

            this.componentIid = component.GetInstanceID();

            hasCustomUITKEditor = editor.GetType().GetMethod("CreateInspectorGUI", maxBindingFlags) != null;

            if (!instances.Contains(this))
                instances.Add(this);



            if (!useUITK) return;

            inspectorElement = new InspectorElement(editor.serializedObject);

            inspectorElement.style.marginTop = 23;

            this.rootVisualElement.Add(inspectorElement);

        }

        void OnDestroy()
        {
            editor?.DestroyImmediate();

            if (instances.Contains(this))
                instances.Remove(this);

        }

        public Component component;
        public Editor editor;
        public InspectorElement inspectorElement;

        public int componentIid;

        bool useUITK => editor.target is MonoBehaviour && (HasUITKOnlyDrawers(editor.serializedObject) || hasCustomUITKEditor);
        bool hasCustomUITKEditor;

        public static List<VInspectorComponentWindow> instances = new();





        public static void CreateDraggedInstance(Component component, Vector2 windowPosition, float windowWidth)
        {
            draggedInstance = ScriptableObject.CreateInstance<VInspectorComponentWindow>();

            draggedInstance.ShowPopup();
            draggedInstance.Init(component);
            draggedInstance.Focus();


            draggedInstance.wantsMouseMove = true;

            // draggedInstance.minSize = new Vector2(300, 50); // will make window resizeable on mac, but not on windows
            draggedInstance.maxHeight = EditorGUIUtility.GetMainWindowPosition().height * .7f;


            draggedInstance.position = Rect.zero.SetPos(windowPosition).SetWidth(windowWidth).SetHeight(200);
            draggedInstance.prevHeight = draggedInstance.position.height;

            draggedInstance.dragStartMousePos = curEvent.mousePosition_screenSpace;
            draggedInstance.dragStartWindowPos = windowPosition;

        }

        public static VInspectorComponentWindow draggedInstance;

    }
}
#endif