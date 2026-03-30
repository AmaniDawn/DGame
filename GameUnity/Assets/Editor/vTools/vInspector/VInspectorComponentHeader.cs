#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using UnityEditor;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
// using static VTools.VDebug;



namespace VInspector
{
    public class VInspectorComponentHeader
    {

        void OnGUI()
        {
            void masks()
            {
                if (!curEvent.isRepaint) return;


                Color backgroundColor;

                void set_backgroundColor()
                {
                    var hovered = EditorGUIUtility.isProSkin ? Greyscale(.28f) : Greyscale(.84f);
                    var normal = EditorGUIUtility.isProSkin ? Greyscale(.244f) : Greyscale(.8f);

                    backgroundColor = normal;

                    if (headerRect.IsHovered() && !mousePressedOnBackground && EditorWindow.mouseOverWindow == window)
                        backgroundColor = hovered;

                }
                void set_buttonMaskSize()
                {
                    buttonsList ??= typeof(EditorGUIUtility).GetMemberValue<IList>("s_EditorHeaderItemsMethods");

                    var buttonsCount = 1 + buttonsList.Count;

                    if (VInspectorMenu.playmodeSaveButtonEnabled && !Application.isPlaying)
                        buttonsCount--;


                    buttonMaskRect = headerRect.AddHeightFromMid(-2).SetXMax(headerRect.xMax - 3).SetWidthFromRight(buttonsCount * 20);

                }

                void hideArrow()
                {
                    if (headerRect.IsHovered()) return;
                    if (!VInspectorMenu.minimalModeEnabled) return;

                    headerRect.SetWidth(17).MoveX(-2).AddHeightFromMid(-2).AddWidthFromRight(-4).Draw(backgroundColor);

                }
                void hideButtons()
                {
                    if (headerRect.IsHovered()) return;
                    if (!VInspectorMenu.minimalModeEnabled) return;

                    buttonMaskRect.Draw(backgroundColor);

                }
                void hideScriptText()
                {
                    if (component is not MonoBehaviour) return;
                    if (!VInspectorMenu.minimalModeEnabled) return;
                    if (imguiContainer.onGUIHandler.Method.DeclaringType.FullName.StartsWith("Sisus")) return;


                    var name = component.GetType().Name.Decamelcase();

                    var rect = headerRect.AddHeightFromMid(-2).SetWidth(60).MoveX(name.GetLabelWidth(fontSize: 12, isBold: true) + 60 - 3);
                    rect = rect.MoveX(-2);

                    rect.xMax = rect.xMax.Min(buttonMaskRect.x).Max(rect.xMin);


                    rect.Draw(backgroundColor);

                }
                void tintScriptIcon()
                {
                    if (component is not MonoBehaviour) return;
                    if (!mousePressedOnScriptIcon) return;

                    var iconRect = headerRect.MoveX(18).SetWidth(22);

                    iconRect.Draw(backgroundColor.SetAlpha(EditorGUIUtility.isProSkin ? .3f : .45f));

                }
                void nameCurtain()
                {
                    var rect = headerRect.AddHeightFromMid(-2).SetXMax(buttonMaskRect.x + 2).SetWidthFromRight(18);

                    rect.DrawCurtainLeft(backgroundColor);

                }


                set_backgroundColor();
                set_buttonMaskSize();

                hideArrow();
                hideButtons();
                hideScriptText();
                tintScriptIcon();
                nameCurtain();

            }

            void scriptIconClicks()
            {
                if (component is not MonoBehaviour) return;
                if (curEvent.mouseButton != 0) return;
                if (!VInspectorMenu.hideScriptFieldEnabled) return;

                var iconRect = headerRect.MoveX(18).SetWidth(22);


                void mosueDown()
                {
                    if (!curEvent.isMouseDown) return;
                    if (!iconRect.IsHovered()) return;


                    mousePressedOnScriptIcon = true;
                    mousePressedOnScriptIcon_initPos = curEvent.mousePosition;


                    var script = MonoScript.FromMonoBehaviour(component as MonoBehaviour);

                    // if (curEvent.holdingAlt)


                    if (curEvent.clickCount == 2)
                        AssetDatabase.OpenAsset(script);


                    curEvent.Use();

                }
                void mouseUp()
                {
                    if (!curEvent.isMouseUp) return;
                    if (!mousePressedOnScriptIcon) return;

                    var script = MonoScript.FromMonoBehaviour(component as MonoBehaviour);

                    if (curEvent.clickCount == 1)
                        EditorUtils.PingObject(script);

                    mousePressedOnScriptIcon = false;

                    window.Repaint();

                    curEvent.Use();

                }
                void startDrag()
                {
                    if (!curEvent.isMouseDrag) return;
                    if (!mousePressedOnScriptIcon) return;
                    if ((mousePressedOnScriptIcon_initPos - curEvent.mousePosition).magnitude < 2) return;

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { component };
                    DragAndDrop.StartDrag(component.ToString());

                    mousePressedOnScriptIcon = false;
                    mousePressedOnBackground = false;

                }


                mosueDown();
                mouseUp();
                startDrag();

            }

            void expandWithAnimation()
            {
                if (!mousePressedOnBackground) return;
                if (!curEvent.isMouseUp) return;
                if (curEvent.mouseButton != 0) return;
                if (headerRect.SetWidth(16).MoveX(40).IsHovered()) return; // enabled toggle
                if (buttonMaskRect.IsHovered()) return; // right buttons


                if (curEvent.holdingShift)
                    VInspector.CollapseOtherComponents(component, window);

                else if (VInspectorMenu.componentAnimationsEnabled)
                    VInspector.ToggleComponentExpanded(component, window);

                else return;


                mousePressedOnBackground = false;

                curEvent.Use();

                EditorGUIUtility.hotControl = 0;

            }
            void createComponentWindow()
            {
                if (!mousePressedOnBackground) return;
                if (!curEvent.isMouseDrag) return;
                if (!curEvent.holdingAlt) return;
                if (curEvent.mousePosition.DistanceTo(mousePressedOnBackground_initPos) < 2) return;
                if (!VInspectorMenu.componentWindowsEnabled) return;

                if (VInspectorComponentWindow.draggedInstance != null) return;



                var position = EditorGUIUtility.GUIToScreenPoint(headerRect.position + (curEvent.mousePosition - mousePressedOnBackground_initPos));

                VInspectorComponentWindow.CreateDraggedInstance(component, position, headerRect.width);



                EditorGUIUtility.hotControl = 0;

                mousePressedOnBackground = false;



            }

            void set_mousePressedOnBackground()
            {
                if (curEvent.isMouseDown && !buttonMaskRect.IsHovered())
                {
                    mousePressedOnBackground = true;
                    mousePressedOnBackground_initPos = curEvent.mousePosition;
                }

                if (curEvent.isMouseUp || curEvent.isDragUpdate)
                    mousePressedOnBackground = false;

                if (!imguiContainer.contentRect.IsHovered())
                    mousePressedOnBackground = false;

            }
            void set_hoveredComponentHeader()
            {
                if (!curEvent.isRepaint) return;

                if (component is Transform)
                    VInspector.hoveredComponentHeader = null;

                if (headerRect.IsHovered())
                    VInspector.hoveredComponentHeader = this;

            }

            void defaultHeaderGUI()
            {
                void initOffsets()
                {
                    if (!VInspectorMenu.minimalModeEnabled) return;
                    if (headerContentStyle != null) return;

                    headerContentStyle = typeof(EditorStyles).GetMemberValue<GUIStyle>("inspectorTitlebar");
                    headerFoldoutStyle = typeof(EditorStyles).GetMemberValue<GUIStyle>("titlebarFoldout");

                    headerContentStyle_defaultLeftPadding = headerContentStyle.padding.left;
                    headerFoldoutStyle_defaultLeftMargin = headerFoldoutStyle.margin.left;

                }
                void setAdjustedOffsets()
                {
                    if (!VInspectorMenu.minimalModeEnabled) return;

                    headerContentStyle.padding.left = headerContentStyle_defaultLeftPadding - 2;
                    headerFoldoutStyle.margin.left = headerFoldoutStyle_defaultLeftMargin - 1;

                }
                void setDefaultOffsets()
                {
                    if (!VInspectorMenu.minimalModeEnabled) return;

                    headerContentStyle.padding.left = headerContentStyle_defaultLeftPadding;
                    headerFoldoutStyle.margin.left = headerFoldoutStyle_defaultLeftMargin;

                }

                initOffsets();
                setAdjustedOffsets();
                defaultHeaderGUIAction.Invoke();
                setDefaultOffsets();

            }

            void preventKeyboardFocus()
            {
                if (!curEvent.isUsed) return;
                if (!headerRect.IsHovered()) return;
                if (imguiContainer.onGUIHandler.Method.DeclaringType.FullName.StartsWith("Sisus")) return;

                GUIUtility.keyboardControl = 0;

                // removes that annoying blue highlight after clicking on header

            }



            if (curEvent.isRepaint)
                defaultHeaderGUI();

            masks();

            scriptIconClicks();
            expandWithAnimation();
            createComponentWindow();

            set_mousePressedOnBackground();
            set_hoveredComponentHeader();

            if (!curEvent.isRepaint)
                defaultHeaderGUI();

            preventKeyboardFocus();

        }

        static IList buttonsList;
        static Rect buttonMaskRect;

        public bool mousePressedOnBackground;
        public bool mousePressedOnScriptIcon;
        public Vector2 mousePressedOnBackground_initPos;
        public Vector2 mousePressedOnScriptIcon_initPos;

        public Rect headerRect
        {
            get
            {
                var contentRect = imguiContainer.contentRect;

                if (contentRect.height == 42) // with extra lines like "Multi-object editing not supported"
                    return contentRect.SetHeight(22);
                else
                    return contentRect.SetHeightFromBottom(22); // fixes offset on transform header in 6000
            }
        }

        static GUIStyle headerContentStyle;
        static GUIStyle headerFoldoutStyle;

        static int headerContentStyle_defaultLeftPadding;
        static int headerFoldoutStyle_defaultLeftMargin;

        public Vector2 mouseDownPos;






        public void Update()
        {
            if (imguiContainer is VisualElement v && v.panel == null) { imguiContainer.onGUIHandler = defaultHeaderGUIAction; imguiContainer = null; }
            if (imguiContainer?.onGUIHandler.Method.DeclaringType == typeof(VInspectorComponentHeader)) return;
            if (imguiContainer?.onGUIHandler.Method.DeclaringType.FullName.StartsWith("Sisus") == true) return;
            if (typeof(ScriptableObject).IsAssignableFrom(component.GetType())) return;
            if (editor.GetPropertyValue("propertyViewer") is not EditorWindow window) return;


            this.window = window;

            void fixWrongWindow()
            {
                if (Application.unityVersion != "2022.3.26f1" && Application.unityVersion != "2023.2.20f1") return;

                if (!window.hasFocus)
                    window = window.GetMemberValue("m_Parent")?.GetMemberValue<List<EditorWindow>>("m_Panes")?.FirstOrDefault(r => r.hasFocus) ?? window;

                // in some versions wrong inspector may be returned by propertyViewer when there are multiple inspectors
                // also the same instance of an editor may be used on all inspectors
                // here we fix it for cases when multiple inspectors are in the same dock area

            }
            void findHeader(VisualElement element)
            {
                if (element == null) return;

                if (element.GetType().Name == "EditorElement")
                {
                    IMGUIContainer curHeaderImguiContainer = null;

                    foreach (var child in element.Children())
                    {
                        curHeaderImguiContainer ??= new[] { child as IMGUIContainer }.FirstOrDefault(r => r != null && r.name.EndsWith("Header"));

                        if (curHeaderImguiContainer is null) continue;
                        if (child is not InspectorElement) continue;
                        if (child.GetFieldValue("m_Editor") is not Editor editor) continue;
                        if (editor.target != component) continue;

                        imguiContainer = curHeaderImguiContainer;

                        if (editingMultiselection = editor.targets.Count() > 1)
                            multiselectedComponents = editor.targets.Cast<Component>().ToList();

                        return;

                    }

                }

                foreach (var r in element.Children())
                    if (imguiContainer == null)
                        findHeader(r);

            }
            void setupGUICallbacks()
            {
                if (imguiContainer == null) return;

                defaultHeaderGUIAction = imguiContainer.onGUIHandler;
                imguiContainer.onGUIHandler = OnGUI;
            }

            fixWrongWindow();
            findHeader(window.rootVisualElement);
            setupGUICallbacks();

        }

        public bool editingMultiselection;
        public List<Component> multiselectedComponents;

        IMGUIContainer imguiContainer;
        System.Action defaultHeaderGUIAction;

        EditorWindow window;







        public VInspectorComponentHeader(Component component, Editor editor) { this.component = component; this.editor = editor; }

        public Editor editor;
        public Component component;

    }
}
#endif