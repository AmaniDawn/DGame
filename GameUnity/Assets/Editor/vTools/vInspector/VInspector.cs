#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using Type = System.Type;
using static VInspector.VInspectorState;
using static VInspector.VInspectorData;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
// using static VTools.VDebug;


namespace VInspector
{
    public static class VInspector
    {

        static void UpdateNavbars() // update
        {
            void updateNavbar(EditorWindow window)
            {
                if (!window) return;
                if (!window.hasFocus) return;


                var hasNavbar = navbars_byWindow.ContainsKey(window);
                var shouldHaveNavbar = VInspectorMenu.navigationBarEnabled && !window.GetMemberValue<bool>("isLocked");


                if (!hasNavbar && shouldHaveNavbar)
                    createNavbar(window);

                if (hasNavbar && !shouldHaveNavbar)
                    destroyNavbar(window);

            }

            void createNavbar(EditorWindow window)
            {
                var navbar = new IMGUIContainer();

                navbar.name = "vInspector-navbar";

                navbar.style.width = Length.Percent(100);
                navbar.style.height = 28;

                var navbarGui = new VInspectorNavbar(window);
                navbar.onGUIHandler = () => navbarGui.OnGUI(navbar.contentRect);

                navbar.style.position = Position.Absolute;

                window.rootVisualElement.Add(navbar);



                var navbarSpacer = new VisualElement();

                navbarSpacer.name = "vInspector-navbar-spacer";

                navbar.style.width = Length.Percent(100);
                navbarSpacer.style.height = 28 + 1;

                window.rootVisualElement.Insert(0, navbarSpacer);



                navbars_byWindow[window] = navbar;
                navbarSpacers_byWindow[window] = navbarSpacer;

            }
            void destroyNavbar(EditorWindow window)
            {
                var navbar = window.rootVisualElement.Q("vInspector-navbar");
                var navbarSpacer = window.rootVisualElement.Q("vInspector-navbar-spacer");

                navbar.RemoveFromHierarchy();
                navbarSpacer.RemoveFromHierarchy();


                navbars_byWindow.Remove(window);
                navbarSpacers_byWindow.Remove(window);

            }


            foreach (var inspector in allInspectors)
                updateNavbar(inspector);

        }

        static Dictionary<EditorWindow, VisualElement> navbars_byWindow = new();
        static Dictionary<EditorWindow, VisualElement> navbarSpacers_byWindow = new();




        static void PasteButton_OnGUI(EditorWindow window, Rect rect)
        {
            if (!goEditors_byWindow.TryGetValue(window, out var editor)) return;

            var isActive = VInspectorClipboard.CanComponentsBePastedTo(editor.targets.Cast<GameObject>());
            var copiedDatas = VInspectorClipboard.instance.copiedComponetDatas;
            var text = copiedDatas.Count > 1 ? $"Paste {copiedDatas.Count} components" : "Paste Component";

            void pasteButton_active()
            {
                if (!isActive) return;

                if (!GUI.Button(rect, text)) return;

                foreach (var target in editor.targets)
                    foreach (var data in copiedDatas)
                        VInspectorClipboard.PasteComponentAsNew(data, target as GameObject);

                if (!curEvent.holdingAlt)
                    VInspectorClipboard.ClearCopiedDatas();

            }
            void pasteButton_inactive()
            {
                if (isActive) return;

                SetGUIEnabled(false);

                GUI.Button(rect, text);

                ResetGUIEnabled();

            }
            void cancelButton()
            {
                if (!rect.IsHovered()) return;

                var buttonRect = rect.SetWidthFromRight(rect.height).MoveX(-1);
                var iconSize = 12;
                var colorNormal = Greyscale(rect.IsHovered() ? .7f : .6f) * (isActive ? 1 : .9f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .25f) * (isActive ? 1 : .9f);
                var colorPressed = Greyscale(isDarkTheme ? .7f : .65f) * (isActive ? 1 : .9f);


                if (!IconButton(buttonRect, "CrossIcon", iconSize, colorNormal, colorHovered, colorPressed)) return;

                VInspectorClipboard.ClearCopiedDatas();

            }
            void escHint()
            {
                if (!rect.SetWidthFromRight(rect.height).MoveX(-1).IsHovered()) return;

                var textRect = rect.SetWidthFromRight(39).MoveY(-.5f);
                var fontSize = 10;
                var color = Greyscale(isDarkTheme ? .9f : 1f) * (isActive ? 1 : .9f);


                SetLabelFontSize(fontSize);
                SetGUIColor(color);

                GUI.Label(textRect, "Esc");

                ResetGUIColor();
                ResetLabelStyle();

            }


            if (!curEvent.isRepaint)
                cancelButton();

            pasteButton_active();
            pasteButton_inactive();
            escHint();

            if (curEvent.isRepaint)
                cancelButton();
        }

        static void UpdatePasteButtons() // update and selectionChanged
        {
            void updateButton(EditorWindow window)
            {
                if (!window) return;
                if (!window.hasFocus) return;


                var hasCopiedComponents = VInspectorClipboard.instance.copiedComponetDatas.Any();
                var inspectingGameObjects = window.GetType() == t_InspectorWindow ? window.InvokeMethod<Object[]>("GetInspectedObjects")?.All(r => r is GameObject) == true :
                                            window.GetType() == t_PropertyEditor ? propertyEditorsInspectingGameObjects.Contains(window) : true;

                var hasButton = pasteButtons_byWindow.ContainsKey(window);
                var shouldHaveButton = hasCopiedComponents && inspectingGameObjects;


                if (!hasButton && shouldHaveButton)
                    createButton(window);

                if (hasButton && !shouldHaveButton)
                    destroyButton(window);

            }

            void createButton(EditorWindow window)
            {
                var addComponentButton = window.rootVisualElement.Q(className: "unity-inspector-add-component-button");

                if (addComponentButton == null) return;


                var buttonHolder = new VisualElement();

                buttonHolder.name = "vInspector-paste-component-button-holder";

                buttonHolder.style.flexDirection = FlexDirection.Row;
                buttonHolder.style.justifyContent = Justify.Center;


                var button = new IMGUIContainer();

                button.style.width = 230f;
                button.style.height = 25f;
                button.style.marginLeft = 2f;
                button.style.marginRight = 2f;
                button.style.marginTop = -3f;
                button.style.marginBottom = 15f;


                button.onGUIHandler = () => PasteButton_OnGUI(window, button.contentRect);



                addComponentButton.parent.Add(buttonHolder);

                buttonHolder.Add(button);



                pasteButtons_byWindow[window] = button; ;

            }
            void destroyButton(EditorWindow window)
            {
                var buttonHolder = pasteButtons_byWindow[window].parent;

                buttonHolder.RemoveFromHierarchy();


                pasteButtons_byWindow.Remove(window);

            }


            foreach (var inspector in allInspectors)
                updateButton(inspector);

            foreach (var propertyEditor in propertyEditorsInspectingGameObjects)
                updateButton(propertyEditor);

        }

        static Dictionary<EditorWindow, VisualElement> pasteButtons_byWindow = new();




        static void FillCollections(Editor editor) // finishedDefaultHeaderGUI
        {
            if (editor.GetMemberValue("propertyViewer") is not EditorWindow window) return;
            if (editor.target is not GameObject) return;

            goEditors_byWindow[window] = editor;


            if (window.GetType() != t_PropertyEditor) return;
            if (propertyEditorsInspectingGameObjects.Contains(window)) return;

            propertyEditorsInspectingGameObjects.Add(window);

        }

        static List<EditorWindow> propertyEditorsInspectingGameObjects = new();
        static Dictionary<EditorWindow, Editor> goEditors_byWindow = new();















        static void ComponentShortcuts() // globalEventHandler
        {
            if (EditorWindow.mouseOverWindow is not EditorWindow hoveredWindow) return;
            if (!hoveredWindow) return;
            if (hoveredWindow.GetType() != t_InspectorWindow && hoveredWindow.GetType() != t_PropertyEditor) return;
            if (!curEvent.isKeyDown) return;
            if (curEvent.keyCode == KeyCode.None) return;


            void expandOrCollapseHovered()
            {
                if (curEvent.holdingAnyModifierKey) return;
                if (!curEvent.isKeyDown || curEvent.keyCode != KeyCode.E) return;
                if (Tools.viewTool == ViewTool.FPS) return;
                if (!VInspectorMenu.toggleExpandedEnabled) return;
                if (hoveredComponentHeader == null) return;



                ToggleComponentExpanded(hoveredComponent, hoveredWindow);



                hoveredWindow.Repaint();

                curEvent.Use();



                if (!Application.unityVersion.Contains("2022")) return;

                var curTransformTool = Tools.current;

                toCallNextUpdate += () => Tools.current = curTransformTool;

                // E shortcut changes transform tool in 2022
                // here we undo this

            }
            void expandOrCollapseAll()
            {
                if (curEvent.modifiers != (EventModifiers.Shift | EventModifiers.Command) && curEvent.modifiers != (EventModifiers.Shift | EventModifiers.Control)) return;
                if (!curEvent.isKeyDown || curEvent.keyCode != KeyCode.E) return;
                if (!VInspectorMenu.collapseEverythingEnabled) return;


                ToggleAllComponentsExpanded(hoveredWindow);


                hoveredWindow.Repaint();

                curEvent.Use();

            }
            void collapseEverythingElse()
            {
                if (curEvent.modifiers != EventModifiers.Shift) return;
                if (!curEvent.isKeyDown || curEvent.keyCode != KeyCode.E) return;
                if (!VInspectorMenu.collapseEverythingElseEnabled) return;
                if (hoveredComponentHeader == null) return;



                CollapseOtherComponents(hoveredComponent, hoveredWindow);



                hoveredWindow.Repaint();

                curEvent.Use();

            }
            void toggleActive()
            {
                if (curEvent.holdingAnyModifierKey) return;
                if (!curEvent.isKeyDown || curEvent.keyCode != KeyCode.A) return;
                if (Tools.viewTool == ViewTool.FPS) return;
                if (!VInspectorMenu.toggleActiveEnabled) return;
                if (hoveredComponentHeader == null) return;

                if (EditorUtility.GetObjectEnabled(hoveredComponent) == -1) return;


                var components = hoveredComponentHeader.editingMultiselection ? hoveredComponentHeader.multiselectedComponents : new List<Component> { hoveredComponent };

                var anyComponentsEnabled = components.Any(r => EditorUtility.GetObjectEnabled(r) == 1);


                foreach (var r in components)
                    r.RecordUndo();

                foreach (var r in components)
                    EditorUtility.SetObjectEnabled(r, !anyComponentsEnabled);



                hoveredWindow.Repaint();

                curEvent.Use();

            }
            void delete()
            {
                if (curEvent.holdingAnyModifierKey) return;
                if (!curEvent.isKeyDown || curEvent.keyCode != KeyCode.X) return;
                if (!VInspectorMenu.deleteEnabled) return;
                if (hoveredComponentHeader == null) return;



                Component requiredByComponent = null;

                foreach (var otherComponent in hoveredComponent.gameObject.GetComponents<Component>())
                    if (otherComponent.GetType().GetCustomAttributes<RequireComponent>().Any(r => (r.m_Type0 ?? r.m_Type1 ?? r.m_Type2)?.IsAssignableFrom(hoveredComponent.GetType()) ?? false))
                        requiredByComponent = otherComponent;

                if (requiredByComponent != null && hoveredComponent is not Transform)
                    Debug.Log($"Can't delete {hoveredComponent.GetType().Name.Decamelcase()} because it is required by {requiredByComponent.GetType().Name.Decamelcase()}");



                if (requiredByComponent == null && hoveredComponent is not Transform)
                    if (VInspectorMenu.componentAnimationsEnabled)
                        DeleteComponent_withAnimation(hoveredWindow, hoveredComponentHeader.editingMultiselection ? hoveredComponentHeader.multiselectedComponents : new List<Component> { hoveredComponent });
                    else
                        DeleteComponent_withoutAnimation(hoveredComponentHeader.editingMultiselection ? hoveredComponentHeader.multiselectedComponents : new List<Component> { hoveredComponent });




                hoveredWindow.Repaint();

                curEvent.Use();



                if (!Application.unityVersion.Contains("2022")) return;

                var curPivotRotation = Tools.pivotRotation;

                toCallNextUpdate += () => Tools.pivotRotation = curPivotRotation;

                // X shortcut changes Tools.pivotRotation in 2022
                // here we undo this

            }

            void clearCopiedDatas()
            {
                if (curEvent.modifiers != EventModifiers.None) return;
                if (!curEvent.isKeyDown) return;
                if (curEvent.keyCode != KeyCode.Escape) return;


                VInspectorClipboard.ClearCopiedDatas();


                hoveredWindow.Repaint();

                curEvent.Use();

            }
            void moveBack()
            {
                if (!curEvent.isMouseDown) return;
                if (curEvent.mouseButton != 3) return;
                if (hoveredWindow.GetType() != t_InspectorWindow) return;

                if (!VInspectorSelectionHistory.instance.prevStates.Any()) return;

                VInspectorSelectionHistory.instance.MoveBack();

            }
            void moveForward()
            {
                if (!curEvent.isMouseDown) return;
                if (curEvent.mouseButton != 4) return;
                if (hoveredWindow.GetType() != t_InspectorWindow) return;

                if (!VInspectorSelectionHistory.instance.nextStates.Any()) return;

                VInspectorSelectionHistory.instance.MoveForward();
            }

            expandOrCollapseHovered();
            expandOrCollapseAll();
            collapseEverythingElse();
            toggleActive();
            delete();

            clearCopiedDatas();
            moveBack();
            moveForward();

        }

        public static VInspectorComponentHeader hoveredComponentHeader;
        public static Component hoveredComponent => hoveredComponentHeader?.component;



        static void UpdateComponentAnimations() // update
        {
            void set_deltaTime()
            {
                deltaTime = (float)(EditorApplication.timeSinceStartup - lastLayoutTime);

                // if (deltaTime > .05f)
                // deltaTime = .0166f;

                lastLayoutTime = EditorApplication.timeSinceStartup;

            }
            void updateExpandAnimationsQueue()
            {
                if (!queuedExpandAnimations.Any()) { lastActivatedQueuedAnimation = null; return; }

                void unqueue(ExpandAnimation animation)
                {
                    queuedExpandAnimations.Remove(animation);

                    animation.Start();

                    lastActivatedQueuedAnimation = animation;

                }

                if (lastActivatedQueuedAnimation == null)
                    unqueue(queuedExpandAnimations.First());

                else if (!lastActivatedQueuedAnimation.expandedInspectorHeightUnknown)
                    if ((lastActivatedQueuedAnimation.currentInspectorHeight - lastActivatedQueuedAnimation.targetInspectorHeight).Abs() < expandAnimation_unqueueAtDistance)
                        unqueue(queuedExpandAnimations.First());

            }
            void updateExpandAnimations()
            {
                foreach (var animation in activeExpandAnimations_byComponent.Values.ToList())
                    animation.Update();

            }
            void updateDeleteAnimations()
            {
                foreach (var animation in activeDeleteAnimations.ToList())
                    animation.Update();

            }


            toCallNextUpdate?.Invoke();
            toCallNextUpdate = null;

            set_deltaTime();
            updateExpandAnimationsQueue();
            updateExpandAnimations();
            updateDeleteAnimations();

        }

        static float expandAnimation_lerpSpeed => 12;
        static float expandAnimation_speedLimit => 4000;
        static float expandAnimation_unqueueAtDistance => 90;

        static float deleteAnimation_lerpSpeed => 10;
        static float deleteAnimation_speedLimit => 3000;

        static System.Action toCallNextUpdate;

        static float deltaTime;
        static double lastLayoutTime;

        static Dictionary<Component, ExpandAnimation> activeExpandAnimations_byComponent = new();
        static List<ExpandAnimation> queuedExpandAnimations = new();
        static ExpandAnimation lastActivatedQueuedAnimation;

        static List<DeleteAnimation> activeDeleteAnimations = new();


        class ExpandAnimation
        {
            public void Start()
            {
                void expand()
                {
                    if (targetExpandedState != true) return;

                    SetComponentExpanded_withoutAnimation(inspectorWindow, component, true);


                }
                void findInspectorElement()
                {
                    var editorsList = inspectorWindow.rootVisualElement.Q(className: "unity-inspector-editors-list");

                    foreach (var someEditorElement in editorsList.Children())
                        if (someEditorElement.Children().FirstOrDefault(r => r is InspectorElement) is InspectorElement someInspectorElement)
                            if (someInspectorElement.GetFieldValue("m_Editor") is Editor editor)
                                if (editor.target == component)
                                {
                                    inspectorElement = someInspectorElement;
                                    break;

                                }

                }
                void detachInspectorElement()
                {
                    if (inspectorElement == null) return;
                    if (targetExpandedState != true) return;

                    inspectorElement.style.position = Position.Absolute;
                    inspectorElement.style.visibility = Visibility.Hidden;

                    // needed to read inspectorElement height without it affecting layout
                    // reattached in UpdateAnimation

                }
                void createMaskElement()
                {
                    if (inspectorElement == null) return;


                    maskElement = new IMGUIContainer();

                    maskElement.name = "vInspector-mask-for-expand-animation";

                    (maskElement as IMGUIContainer).onGUIHandler = () => new Rect(0, 0, 1232, 1232).Draw(GUIColors.windowBackground);

                    inspectorElement.parent.Add(maskElement);

                }
                void set_expandedInspectorHeight()
                {
                    if (targetExpandedState == true)
                        expandedInspectorHeightUnknown = true;
                    else
                        expandedInspectorHeight = inspectorElement.layout.height;

                }
                void set_currentInspectorHeight()
                {
                    if (targetExpandedState == true)
                        currentInspectorHeight = collapsedInspectorHeight;
                    else
                        currentInspectorHeight = expandedInspectorHeight;

                }


                expand();
                findInspectorElement();
                detachInspectorElement();
                createMaskElement();
                set_expandedInspectorHeight();
                set_currentInspectorHeight();

                activeExpandAnimations_byComponent[component] = this;


            }
            public void Finish()
            {
                void collapse()
                {
                    if (targetExpandedState != false) return;

                    SetComponentExpanded_withoutAnimation(inspectorWindow, component, false);

                }
                void resetInspectorElementStyle()
                {
                    inspectorElement.style.maxHeight = StyleKeyword.Null;
                    inspectorElement.style.marginBottom = 0;

                }
                void removeMaskElement()
                {
                    maskElement.RemoveFromHierarchy();
                }


                collapse();

                toCallNextUpdate += resetInspectorElementStyle;
                toCallNextUpdate += removeMaskElement;

                activeExpandAnimations_byComponent.Remove(component);

            }

            public void Update()
            {
                void set_expandedInspectorHeight()
                {
                    if (!expandedInspectorHeightUnknown) return;
                    if (inspectorElement.layout.height == 0) return;

                    expandedInspectorHeight = inspectorElement.layout.height;
                    expandedInspectorHeightUnknown = false;

                    reattachInspectorElement();

                }
                void reattachInspectorElement()
                {
                    inspectorElement.style.position = Position.Relative;
                    inspectorElement.style.visibility = Visibility.Visible;

                }
                void lerp()
                {
                    if (expandedInspectorHeightUnknown) return;


                    MathUtil.SmoothDamp(ref currentInspectorHeight, targetInspectorHeight, expandAnimation_lerpSpeed, ref currentInspectorHeightDerivative, deltaTime, expandAnimation_speedLimit);

                }
                void modifyInspectorElementStyle()
                {
                    if (expandedInspectorHeightUnknown) return;

                    inspectorElement.style.maxHeight = currentInspectorHeight.Max(0);
                    inspectorElement.style.marginBottom = currentInspectorHeight.Min(0);

                }
                void finish()
                {
                    if ((currentInspectorHeight - targetInspectorHeight).Abs() > .5f) return;

                    Finish();

                }

                set_expandedInspectorHeight();
                lerp();
                modifyInspectorElementStyle();
                finish();

            }



            public EditorWindow inspectorWindow;

            public Component component;

            public VisualElement inspectorElement;
            public VisualElement maskElement;

            public float expandedInspectorHeight;
            public bool expandedInspectorHeightUnknown;

            public float collapsedInspectorHeight => -7;

            public float currentInspectorHeight;
            public float currentInspectorHeightDerivative;

            public float targetInspectorHeight => targetExpandedState == true ? expandedInspectorHeight : collapsedInspectorHeight;

            public bool targetExpandedState;

        }

        class DeleteAnimation
        {
            public void Start()
            {
                void findEditorElement()
                {
                    var editorsList = inspectorWindow.rootVisualElement.Q(className: "unity-inspector-editors-list");

                    foreach (var someEditorElement in editorsList.Children())
                        if (someEditorElement.Children().FirstOrDefault(r => r is InspectorElement) is InspectorElement someInspectorElement)
                            if (someInspectorElement.GetFieldValue("m_Editor") is Editor editor)
                                if (editor.target == component)
                                {
                                    editorElement = someEditorElement;
                                    break;
                                }

                }
                void createSpacerElement()
                {
                    spacerElement = new IMGUIContainer();

                    spacerElement.name = "vInspector-spacer-for-delete-animation";

                    (spacerElement as IMGUIContainer).onGUIHandler = () => new Rect(0, 0, 1232, 1).Draw(Greyscale(.1f));

                    editorElement.parent.Insert(editorElement.parent.IndexOf(editorElement), spacerElement);

                    spacerElement.style.height = editorElement.layout.height;

                    currentSpacerHeight = editorElement.layout.height;


                }

                findEditorElement();
                createSpacerElement();

                activeDeleteAnimations.Add(this);

            }
            public void Finish()
            {
                spacerElement.RemoveFromHierarchy();

                activeDeleteAnimations.Remove(this);

            }

            public void Update()
            {
                void lerp()
                {
                    MathUtil.SmoothDamp(ref currentSpacerHeight, 0, deleteAnimation_lerpSpeed, ref currentSpacerHeightDerivative, deltaTime, deleteAnimation_speedLimit);
                }
                void modifySpacerElement()
                {
                    spacerElement.style.height = currentSpacerHeight;
                }
                void finish()
                {
                    if (currentSpacerHeight > .5f) return;

                    Finish();

                }

                lerp();
                modifySpacerElement();
                finish();

            }



            public EditorWindow inspectorWindow;

            public Component component;

            public VisualElement editorElement;
            public VisualElement spacerElement;

            public float currentSpacerHeight;
            public float currentSpacerHeightDerivative;

        }



        public static void ToggleComponentExpanded(Component component, EditorWindow inspectorWindow)
        {
            if (VInspectorMenu.componentAnimationsEnabled)
                SetComponentExpanded_withAnimation(inspectorWindow, component, newExpandedState: !GetCompnentExpanded(inspectorWindow, component), queueAnimation: false);
            else
                SetComponentExpanded_withoutAnimation(inspectorWindow, component, newExpandedState: !GetCompnentExpanded(inspectorWindow, component));

        }
        public static void ToggleAllComponentsExpanded(EditorWindow inspectorWindow)
        {
            var firstEditor = inspectorWindow.GetMemberValue<ActiveEditorTracker>("m_Tracker").activeEditors.First();

            var allComponents = inspectorWindow.GetMemberValue<ActiveEditorTracker>("m_Tracker").activeEditors.Where(r => r.target is Component && r.targets.Length == firstEditor.targets.Length && r.target is not ParticleSystemRenderer)
                                                                                                            .Select(r => r.target as Component);
            var anyComponentsExpanded = allComponents.Any(r => GetCompnentExpanded(inspectorWindow, r));


            queuedExpandAnimations.Clear();
            lastActivatedQueuedAnimation = null;

            foreach (var component in !anyComponentsExpanded ? allComponents : allComponents.Reverse())
                if (VInspectorMenu.componentAnimationsEnabled)
                    SetComponentExpanded_withAnimation(inspectorWindow, component, newExpandedState: !anyComponentsExpanded, queueAnimation: true);
                else
                    SetComponentExpanded_withoutAnimation(inspectorWindow, component, newExpandedState: !anyComponentsExpanded);

        }
        public static void CollapseOtherComponents(Component component, EditorWindow inspectorWindow)
        {
            var firstEditor = inspectorWindow.GetMemberValue<ActiveEditorTracker>("m_Tracker").activeEditors.First();

            var allComponents = inspectorWindow.GetMemberValue<ActiveEditorTracker>("m_Tracker").activeEditors.Where(r => r.target is Component && r.targets.Length == firstEditor.targets.Length && r.target is not ParticleSystemRenderer)
                                                                                                            .Select(r => r.target as Component);
            foreach (var someComponent in allComponents)
                if (someComponent != component)
                    if (VInspectorMenu.componentAnimationsEnabled)
                        SetComponentExpanded_withAnimation(inspectorWindow, someComponent, newExpandedState: false, queueAnimation: true);
                    else
                        SetComponentExpanded_withoutAnimation(inspectorWindow, someComponent, newExpandedState: false);

            if (VInspectorMenu.componentAnimationsEnabled)
                SetComponentExpanded_withAnimation(inspectorWindow, component, newExpandedState: true, queueAnimation: false);
            else
                SetComponentExpanded_withoutAnimation(inspectorWindow, component, newExpandedState: true);

        }

        static bool GetCompnentExpanded(EditorWindow hoveredWindow, Component component)
        {
            if (activeExpandAnimations_byComponent.TryGetValue(component, out var animation))
                if (animation.targetExpandedState == false)
                    return false;

            var tracker = hoveredWindow.GetMemberValue<ActiveEditorTracker>("m_Tracker");

            var editorIndex = tracker.activeEditors.ToList().IndexOfFirst(r => r.target == component);

            if (!editorIndex.IsInRangeOf(tracker.activeEditors)) return false;

            return tracker.GetVisible(editorIndex) == 1;

        }

        public static void SetComponentExpanded_withAnimation(EditorWindow inspectorWindow, Component component, bool newExpandedState, bool queueAnimation)
        {
            if (activeExpandAnimations_byComponent.TryGetValue(component, out var activeAnimation)) { activeAnimation.targetExpandedState = newExpandedState; return; };


            var tracker = inspectorWindow.GetMemberValue<ActiveEditorTracker>("m_Tracker");

            var visibleExpandedState = tracker.GetVisible(tracker.activeEditors.ToList().IndexOfFirst(r => r.target == component)) == 1;

            if (visibleExpandedState == newExpandedState) return;



            var animation = new ExpandAnimation();

            animation.inspectorWindow = inspectorWindow;
            animation.component = component;
            animation.targetExpandedState = newExpandedState;



            if (queueAnimation)
                queuedExpandAnimations.Add(animation);
            else
                animation.Start();

        }
        public static void SetComponentExpanded_withoutAnimation(EditorWindow inspectorWindow, Component component, bool newExpandedState)
        {
            // sets saved state which applies to all components of same type
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(component, newExpandedState);


            // sets visible state that lives as long as selection is unchanged
            var tracker = inspectorWindow.GetMemberValue<ActiveEditorTracker>("m_Tracker");

            var editorIndex = tracker.activeEditors.ToList().IndexOfFirst(r => r.target == component);

            if (!editorIndex.IsInRangeOf(tracker.activeEditors)) return;

            tracker.SetVisible(editorIndex, newExpandedState ? 1 : 0);

        }


        public static void DeleteComponent_withAnimation(EditorWindow inspectorWindow, List<Component> multiselectedComponents)
        {
            var animation = new DeleteAnimation();

            animation.inspectorWindow = inspectorWindow;
            animation.component = multiselectedComponents.First();

            animation.Start();

            DeleteComponent_withoutAnimation(multiselectedComponents);

        }
        public static void DeleteComponent_withoutAnimation(List<Component> multiselectedComponents)
        {
            foreach (var r in multiselectedComponents)
                Undo.DestroyObjectImmediate(r);

        }















        static void UpdateComponentHeaders(Editor editor) // finishedDefaultHeaderGUI
        {
            if (!curEvent.isLayout) return;
            if (editor.GetType() != t_GameObjectInspector) return;
            if (editor.target is not GameObject gameObject) return;
            // if (editor.GetMemberValue("propertyViewer") is not EditorWindow window) return;
            // if (window.GetMemberValue("m_Tracker") is not ActiveEditorTracker tracker) return;


            var components = gameObject.GetComponents<Component>().Where(r => r);

            // if (editor.targets.Length > 1)
            // components = tracker.activeEditors.Where(r => r.target && r.target is Component && r.targets.Length == editor.targets.Length).Select(r => r.targets.First() as Component).ToArray();

            void clearHeadersOnReorder()
            {
                var curOrderHash = components.Aggregate(17, (hash, element) => hash * 31 + (element?.GetHashCode() ?? 0));

                if (curOrderHash != componentOrderHashes_byEditor.GetValueOrDefault(editor))
                    componentHeaders_byComponent.Clear();

                componentOrderHashes_byEditor[editor] = curOrderHash;

            }
            void createHeader(Component component)
            {
                if (!component) return;
                if (componentHeaders_byComponent.ContainsKey(component)) return;

                componentHeaders_byComponent[component] = new VInspectorComponentHeader(component, editor);

            }


            clearHeadersOnReorder();

            foreach (var component in components)
                createHeader(component);

            foreach (var component in components)
                componentHeaders_byComponent[component].Update();

        }

        static Dictionary<Component, VInspectorComponentHeader> componentHeaders_byComponent = new();

        static Dictionary<Editor, int> componentOrderHashes_byEditor = new();





        public static void UpdateHeaderButtons(Editor _)
        {

            var buttons = typeof(EditorGUIUtility).GetMemberValue<IList>("s_EditorHeaderItemsMethods");

            if (buttons == null) return; // will be checked again next gui frame



            var t_HeaderItemDelegate = typeof(EditorGUIUtility).GetNestedType("HeaderItemDelegate", maxBindingFlags);

            helpButton ??= buttons.Cast<System.Delegate>().FirstOrDefault(r => r.Method.Name == "HelpIconButton");
            presetsButton ??= buttons.Cast<System.Delegate>().FirstOrDefault(r => r.Method.Name == "DrawPresetButton");
            copyPasteButton ??= typeof(VInspector).GetMethod(nameof(CopyPasteButton), maxBindingFlags).CreateDelegate(t_HeaderItemDelegate);
            playmodeSaveButton ??= typeof(VInspector).GetMethod(nameof(PlaymodeSaveButton), maxBindingFlags).CreateDelegate(t_HeaderItemDelegate);




            buttons.Remove(helpButton);
            buttons.Remove(presetsButton);
            buttons.Remove(copyPasteButton);
            buttons.Remove(playmodeSaveButton);


            if (VInspectorMenu.playmodeSaveButtonEnabled)
                buttons.Insert(0, playmodeSaveButton);

            if (VInspectorMenu.copyPasteButtonsEnabled)
                buttons.Insert(0, copyPasteButton);

            if (!VInspectorMenu.hideHelpButtonEnabled && helpButton != null)
                buttons.Insert(0, helpButton);

            if (!VInspectorMenu.hidePresetsButtonEnabled && presetsButton != null)
                buttons.Insert(0, presetsButton);




            Editor.finishedDefaultHeaderGUI -= UpdateHeaderButtons;

        }

        static System.Delegate helpButton;
        static System.Delegate presetsButton;
        static System.Delegate copyPasteButton;
        static System.Delegate playmodeSaveButton;


        static bool CopyPasteButton(Rect buttonRect, Object[] targets)
        {
            if (targets.First() is not Component component) return false;


            var copiedData = VInspectorClipboard.instance.copiedComponetDatas.FirstOrDefault(r => r.sourceComponent == component);
            var pastableData = VInspectorClipboard.instance.copiedComponetDatas.FirstOrDefault(r => r.sourceComponent.GetType() == component.GetType());

            var isCopied = copiedData != null;
            var canValuesBePasted = !isCopied && pastableData != null;

            GUI.Label(buttonRect, new GUIContent("", canValuesBePasted ? "Paste values" : isCopied ? "Copied" : "Copy component"));



            var iconName = canValuesBePasted ? "Paste values" : isCopied ? "Copied" : "Copy";
            var iconSize = 16;
            var color = Greyscale(isDarkTheme ? .78f : .49f);
            var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
            var colorPressed = Greyscale(isDarkTheme ? .8f : .6f);
            var colorDisabled = Greyscale(.52f);




            var disabled = targets.Length > 1;

            if (disabled) { IconButton(buttonRect, iconName, iconSize, colorDisabled, colorDisabled, colorDisabled); return true; }



            if (!IconButton(buttonRect, iconName, iconSize, color, colorHovered, colorPressed)) return true;

            if (canValuesBePasted)
                VInspectorClipboard.PasteComponentValues(pastableData, component);
            else
                VInspectorClipboard.CopyComponent(component);

            return true;

        }

        static bool PlaymodeSaveButton(Rect buttonRect, Object[] targets)
        {

            void tryApplyingFailedToSaveDatas()
            {
                if (Application.isPlaying) return;
                if (!VInspectorClipboard.instance.failedToSaveComponentDatas.Any()) return;

                foreach (var data in VInspectorClipboard.instance.failedToSaveComponentDatas.ToList())
                    foreach (var target in targets)
                        if (target is Component component_)
                            if (target.GetGlobalID().UnpackForPrefab() == data.globalId)
                            {
                                VInspectorClipboard.ApplyComponentData(data, component_);
                                VInspectorClipboard.instance.failedToSaveComponentDatas.Remove(data);
                            }


                // applies saved component datas that couldn't be applied on playmode exit
                // such datas correspond to:
                // - prefabs (because prefabs produce different (unpacked) globalId in playmode) 
                // - objects from scenes that weren't loaded before playmode (because datas can only be applied to loaded object)
                //
                // this solution applies datas once respective components become visible in inspector
                // a "proper" solution would involve looping through all objects in a scene once it's opened

            }

            tryApplyingFailedToSaveDatas();



            if (!Application.isPlaying) return false;
            if (targets.First() is not Component component) return false;


            var savedData = VInspectorClipboard.instance.savedComponentDatas.FirstOrDefault(r => r.sourceComponent == component);

            var isSaved = savedData != null;

            GUI.Label(buttonRect, new GUIContent("", isSaved ? "Saved" : "Save in play mode"));



            var iconName = isSaved ? "Saved" : "Save";
            var iconSize = 16;
            var color = Greyscale(isDarkTheme ? .8f : .46f);
            var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
            var colorPressed = Greyscale(isDarkTheme ? .8f : .6f);




            if (!IconButton(buttonRect, iconName, iconSize, color, colorHovered, colorPressed)) return true;

            if (targets.Length > 1)
                foreach (var target in targets)
                    VInspectorClipboard.SaveComponent(target as Component);
            else
                VInspectorClipboard.SaveComponent(component);



            return true;

        }















        static void LoadSceneBookmarkObjects() // update
        {
            if (!data) return;


            var scenesToLoadFor = unloadedSceneBookmarks_sceneGuids.Select(r => EditorSceneManager.GetSceneByPath(r.ToPath()))
                                                                   .Where(r => r.isLoaded);
            if (!scenesToLoadFor.Any()) return;



            foreach (var scene in scenesToLoadFor)
            {
                var bookmarksFromThisScene = data.bookmarks.Where(r => r.globalId.guid == scene.path.ToGuid()).ToList();

                var objectsForTheseBookmarks = bookmarksFromThisScene.Select(r => !Application.isPlaying ? r.globalId
                                                                                                         : r.globalId.UnpackForPrefab()).GetObjects();

                for (int i = 0; i < bookmarksFromThisScene.Count; i++)
                    if (objectsForTheseBookmarks[i])
                        bookmarksFromThisScene[i]._obj = objectsForTheseBookmarks[i];
                    else
                        bookmarksFromThisScene[i].failedToLoadSceneObject = true;

            }

            unloadedSceneBookmarks_sceneGuids.Clear();


            foreach (var inspector in allInspectors)
                inspector.Repaint();

        }

        public static HashSet<string> unloadedSceneBookmarks_sceneGuids = new();




        static void StashBookmarkObjects() // on playmode enter before awake
        {
            stashedBookmarkObjects_byBookmark.Clear();

            foreach (var bookmark in data.bookmarks)
                stashedBookmarkObjects_byBookmark[bookmark] = bookmark._obj;

        }
        static void UnstashBookmarkObjects() // on playmode exit
        {
            foreach (var bookmark in data.bookmarks)
                if (stashedBookmarkObjects_byBookmark.TryGetValue(bookmark, out var stashedObject))
                    if (stashedObject != null)
                        bookmark._obj = stashedObject;

        }

        static Dictionary<Bookmark, Object> stashedBookmarkObjects_byBookmark = new();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnPlaymodeEnter_beforeAwake()
        {
            if (!data) return;

            StashBookmarkObjects();

        }
        static void OnPlaymodeExit(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            if (!data) return;


            UnstashBookmarkObjects();

            // scene objects can get recreated in playmode if the scene was reloaded
            // in this case their respective bookmarks will be updated in OnSceneLoaded_inPlaymode to reference the recreated versions
            // so we ensure that after playmode bookmarks reference the same objects as they did before playmode




            foreach (var bookmark in data.bookmarks)
                if (bookmark.globalId.guid == "00000000000000000000000000000000")
                    if (bookmark._obj is GameObject gameObject)
                    {
                        bookmark.globalId = new GlobalID(bookmark.globalId.ToString().Replace("00000000000000000000000000000000", gameObject.scene.path.ToGuid()));
                        data.Dirty();
                    }

            // objects from DontDestroyOnLoad that were bookmarked in playmode have globalIds with blank scene guids
            // we fix this after playmode, when scene guids become available

        }















        public static bool HasVInspectorAttribtues(Type type)
        {
            if (typesWithVInspectorAttributes != null) return typesWithVInspectorAttributes.Contains(type);


            typesWithVInspectorAttributes = new();

            typesWithVInspectorAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<FoldoutAttribute>().Select(r => r.DeclaringType));
            typesWithVInspectorAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<TabAttribute>().Select(r => r.DeclaringType));

            typesWithVInspectorAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<VariantsAttribute>().Select(r => r.DeclaringType));
            typesWithVInspectorAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<IfAttribute>().Select(r => r.DeclaringType));
            typesWithVInspectorAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<ReadOnlyAttribute>().Select(r => r.DeclaringType));
            typesWithVInspectorAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<ShowInInspectorAttribute>().Select(r => r.DeclaringType));

            typesWithVInspectorAttributes.UnionWith(TypeCache.GetFieldsWithAttribute<ButtonAttribute>().Select(r => r.DeclaringType));
            typesWithVInspectorAttributes.UnionWith(TypeCache.GetMethodsWithAttribute<ButtonAttribute>().Select(r => r.DeclaringType));

            typesWithVInspectorAttributes.UnionWith(TypeCache.GetMethodsWithAttribute<OnValueChangedAttribute>().Select(r => r.DeclaringType));

            foreach (var r in typesWithVInspectorAttributes.ToHashSet())
                typesWithVInspectorAttributes.UnionWith(TypeCache.GetTypesDerivedFrom(r));



            return typesWithVInspectorAttributes.Contains(type);

        }

        static HashSet<Type> typesWithVInspectorAttributes = null;



        public static bool HasUITKOnlyDrawers(SerializedObject serializedObject)
        {
            if (serializedObject.targetObject == null) return false;


            var targetType = serializedObject.targetObject.GetType();

            if (uitkUsage_byType.ContainsKey(targetType)) return uitkUsage_byType[targetType];




            var maxSearchDepth = 1;

            var curProperty = serializedObject.GetIterator();

            while (curProperty.NextVisible(enterChildren: curProperty.depth < maxSearchDepth))
            {
                var handler = typeof(Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility").InvokeMethod("GetHandler", curProperty);

                var propertyDrawer = handler.GetPropertyValue<PropertyDrawer>("propertyDrawer");

                if (propertyDrawer == null) continue;


                var hasUITKimplementation = propertyDrawer.CreatePropertyGUI(curProperty.Copy()) != null;
                var hasIMGUIimplementation = propertyDrawer.GetType().GetMethod("OnGUI", new[] { typeof(Rect), typeof(SerializedProperty), typeof(GUIContent) }).DeclaringType == propertyDrawer.GetType();

                if (hasUITKimplementation && !hasIMGUIimplementation)
                    return uitkUsage_byType[targetType] = true;

            }




            return uitkUsage_byType[targetType] = false;

        }

        static Dictionary<Type, bool> uitkUsage_byType = new();



        static void OnUndoRedo()
        {
            if (!valueChangedCallbacks_byUndoPosition.Any()) return;

            if (valueChangedCallbacks_byUndoPosition.TryGetValue(EditorUtils.GetCurrendUndoGroupIndex(), out var callback))
                callback.Invoke();

        }

        public static Dictionary<int, System.Action> valueChangedCallbacks_byUndoPosition = new();















        [InitializeOnLoadMethod]
        static void Init()
        {
            if (VInspectorMenu.pluginDisabled) return;

            void subscribe()
            {

                Editor.finishedDefaultHeaderGUI -= UpdateComponentHeaders;
                Editor.finishedDefaultHeaderGUI += UpdateComponentHeaders;


                Editor.finishedDefaultHeaderGUI -= FillCollections;
                Editor.finishedDefaultHeaderGUI += FillCollections;


                EditorApplication.update -= UpdatePasteButtons;
                EditorApplication.update += UpdatePasteButtons;

                Selection.selectionChanged -= UpdatePasteButtons;
                Selection.selectionChanged += UpdatePasteButtons;


                EditorApplication.update -= UpdateNavbars;
                EditorApplication.update += UpdateNavbars;

                EditorApplication.update -= UpdateComponentAnimations;
                EditorApplication.update += UpdateComponentAnimations;

                Editor.finishedDefaultHeaderGUI -= UpdateHeaderButtons;
                Editor.finishedDefaultHeaderGUI += UpdateHeaderButtons;


                Undo.undoRedoPerformed -= OnUndoRedo;
                Undo.undoRedoPerformed += OnUndoRedo;


                EditorApplication.update -= LoadSceneBookmarkObjects;
                EditorApplication.update += LoadSceneBookmarkObjects;




                var globalEventHandler = typeof(EditorApplication).GetFieldValue<EditorApplication.CallbackFunction>("globalEventHandler");
                typeof(EditorApplication).SetFieldValue("globalEventHandler", ComponentShortcuts + (globalEventHandler - ComponentShortcuts));


                EditorApplication.quitting -= VInspectorState.Save;
                EditorApplication.quitting += VInspectorState.Save;

                EditorApplication.playModeStateChanged -= OnPlaymodeExit;
                EditorApplication.playModeStateChanged += OnPlaymodeExit;

                EditorApplication.playModeStateChanged -= VInspectorClipboard.OnPlaymodeStateChanged;
                EditorApplication.playModeStateChanged += VInspectorClipboard.OnPlaymodeStateChanged;


            }
            void loadData()
            {
                data = AssetDatabase.LoadAssetAtPath<VInspectorData>(ProjectPrefs.GetString("vInspector-lastKnownDataPath"));


                if (data) return;

                data = AssetDatabase.FindAssets("t:VInspectorData").Select(guid => AssetDatabase.LoadAssetAtPath<VInspectorData>(guid.ToPath())).FirstOrDefault();


                if (!data) return;

                ProjectPrefs.SetString("vInspector-lastKnownDataPath", data.GetPath());

            }
            void loadDataDelayed()
            {
                if (data) return;

                EditorApplication.delayCall += () => EditorApplication.delayCall += loadData;

                // AssetDatabase isn't up to date at this point (it gets updated after InitializeOnLoadMethod)
                // and if current AssetDatabase state doesn't contain the data - it won't be loaded during Init()
                // so here we schedule an additional, delayed attempt to load the data
                // this addresses reports of data loss when trying to load it on a new machine

            }
            void removeDeletedBookmarks()
            {
                if (!data) return;


                var toRemove = data.bookmarks.Where(r => r.isDeleted);

                if (!toRemove.Any()) return;


                foreach (var r in toRemove.ToList())
                    data.bookmarks.Remove(r);

                data.Dirty();


                // delayed to give bookmarks a chance to load in update

            }

            subscribe();
            loadData();
            loadDataDelayed();

            EditorApplication.delayCall += () => EditorApplication.delayCall += () => removeDeletedBookmarks();

        }

        public static VInspectorData data;




        static IEnumerable<EditorWindow> allInspectors => _allInspectors ??= t_InspectorWindow.GetFieldValue<IList>("m_AllInspectors").Cast<EditorWindow>().Where(r => r.GetType() == t_InspectorWindow);
        static IEnumerable<EditorWindow> _allInspectors;

        static Type t_InspectorWindow = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        static Type t_PropertyEditor = typeof(Editor).Assembly.GetType("UnityEditor.PropertyEditor");
        static Type t_GameObjectInspector = typeof(Editor).Assembly.GetType("UnityEditor.GameObjectInspector");

        static Type t_VHierarchy = Type.GetType("VHierarchy.VHierarchy") ?? Type.GetType("VHierarchy.VHierarchy, VHierarchy, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

        public static MethodInfo mi_VHierarchy_GetIconName = t_VHierarchy?.GetMethod("GetIconName_forVInspector", maxBindingFlags);







        const string version = "2.0.14";


    }
}
#endif