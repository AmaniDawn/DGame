#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using UnityEditor;
using Type = System.Type;
using Attribute = System.Attribute;
using static VInspector.VInspector;
using static VInspector.VInspectorState;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
// using static VTools.VDebug;


namespace VInspector
{
    class VInspectorEditor
    {

        public void OnGUI()
        {
            var drawingTabPath = "";
            var drawingFoldoutPath = "";
            var hideField = false;
            var disableField = false;
            var noVariablesShown = true;
            var selectedTabPath = rootTab.GetSelectedTabPath();



            void drawMember(MemberInfo memberInfo)
            {

                void ifs()
                {
                    var endIfAttribute = memberInfo.GetCustomAttributeCached<EndIfAttribute>();

                    if (endIfAttribute != null) hideField = disableField = false;


                    var ifAttribute = memberInfo.GetCustomAttributeCached<IfAttribute>();

                    if (ifAttribute is HideIfAttribute) hideField = ifAttribute.Evaluate(target);
                    if (ifAttribute is ShowIfAttribute) hideField = !ifAttribute.Evaluate(target);
                    if (ifAttribute is DisableIfAttribute) disableField = ifAttribute.Evaluate(target);
                    if (ifAttribute is EnableIfAttribute) disableField = !ifAttribute.Evaluate(target);

                }
                void tabs()
                {
                    void drawSubtabs(Tab tab)
                    {
                        if (!tab.subtabs.Any()) return;


                        Space(noVariablesShown ? 2 : 6);

                        var selName = TabsMultiRow(tab.selectedSubtab.name, false, 24, tab.subtabs.Select(r => r.name).ToArray());

                        Space(5);


                        if (selName != tab.selectedSubtab.name)
                        {
                            tab.selectedSubtabIndex = tab.subtabs.IndexOfFirst(r => r.name == selName);

                            selectedTabPath = rootTab.GetSelectedTabPath();

                        }


                        GUI.backgroundColor = Color.white;

                        tab.subtabsDrawn = true;

                    }

                    void endTab()
                    {
                        if (memberInfo.GetCustomAttributeCached<EndTabAttribute>() is not EndTabAttribute endTabAttribute) return;

                        drawingTabPath = "";
                        drawingFoldoutPath = "";
                        hideField = false;
                        disableField = false;

                    }
                    void beginTab()
                    {
                        if (memberInfo.GetCustomAttributeCached<TabAttribute>() is not TabAttribute tabAttribute) return;

                        drawingTabPath = tabAttribute.name;
                        drawingFoldoutPath = "";
                        hideField = false;
                        disableField = false;

                    }
                    void ensureNeededTabsDrawn()
                    {
                        if (!selectedTabPath.StartsWith(drawingTabPath)) return;


                        var curTab = rootTab;

                        foreach (var name in drawingTabPath.Split('/').Where(r => r != ""))
                        {
                            if (!curTab.subtabsDrawn)
                                drawSubtabs(curTab);

                            curTab = curTab.subtabs.Find(r => r.name == name);

                        }

                    }


                    endTab();
                    beginTab();
                    ensureNeededTabsDrawn();

                }
                void foldouts()
                {
                    bool drawFoldout(string name, bool isExpanded, Foldout foldout)
                    {
                        var controlRect = EditorGUILayout.GetControlRect();
                        var fullRect = controlRect.AddWidthFromRight(-15 * EditorGUI.indentLevel).AddWidthFromRight(18).AddWidth(3);

                        var controlId = EditorGUIUtility.GetControlID(FocusType.Passive);
                        var isPressed = EditorGUIUtility.hotControl == controlId;

                        void name_()
                        {
                            var labelRect = controlRect.AddWidthFromRight(-15 * EditorGUI.indentLevel);


                            SetLabelBold();
                            SetLabelFontSize(12);

                            GUI.Label(labelRect, name);

                            ResetLabelStyle();


                        }
                        void triangle()
                        {
                            // return;
                            // if (!curEvent.isRepaint) return;

                            var unityFoldoutRect = controlRect.MoveX(-.5f);

                            isExpanded = EditorGUI.Foldout(unityFoldoutRect, isExpanded, "");

                        }
                        void highlight()
                        {
                            var hoveredColor = Greyscale(1, .06f);
                            var pressedColor = Greyscale(1, .04f);


                            fullRect.MarkInteractive();

                            if (isPressed)
                                fullRect.Draw(pressedColor);

                            else if (fullRect.IsHovered())
                                fullRect.Draw(hoveredColor);

                        }

                        void mouseDown()
                        {
                            if (!curEvent.isMouseDown) return;
                            if (!fullRect.IsHovered()) return;

                            EditorGUIUtility.hotControl = controlId;

                            curEvent.Use();

                        }
                        void mouseUp()
                        {
                            if (!curEvent.isMouseUp) return;
                            if (!isPressed) return;

                            if (fullRect.IsHovered())
                                isExpanded = !isExpanded;

                            EditorGUIUtility.hotControl = 0;

                            EditorGUIUtility.keyboardControl = 0;

                            curEvent.Use();

                        }


                        // background();
                        name_();
                        triangle();
                        highlight();

                        mouseDown();
                        mouseUp();


                        return isExpanded;


                    }
                    void drawButtons(Foldout foldout)
                    {
                        var noButtonsToShow = true;

                        Space(10);

                        foreach (var button in foldout.buttons)
                            drawButton(button, ref noButtonsToShow);


                        if (noButtonsToShow)
                            Space(-10);
                        else
                            Space(5);

                    }

                    void beginFoldout(string name)
                    {
                        if (!rootFoldout.IsSubfoldoutContentVisible(drawingFoldoutPath)) return;



                        updateIndentLevel();

                        drawingFoldoutPath = drawingFoldoutPath.CombinePath(name);



                        var foldout = rootFoldout.GetSubfoldout(drawingFoldoutPath);

                        foldout.isExpanded = drawFoldout(foldout.name, foldout.isExpanded, foldout);

                    }
                    void endFoldout()
                    {
                        var foldout = rootFoldout.GetSubfoldout(drawingFoldoutPath);

                        if (foldout.isExpanded)
                            drawButtons(foldout);

                        drawingFoldoutPath = drawingFoldoutPath.HasParentPath() ? drawingFoldoutPath.GetParentPath() : "";

                    }



                    var newFoldoutPath = drawingFoldoutPath;

                    if (memberInfo.GetCustomAttributeCached<EndFoldoutAttribute>() is not null)
                        newFoldoutPath = "";

                    if (memberInfo.GetCustomAttributeCached<FoldoutAttribute>() is FoldoutAttribute foldoutAttribute)
                        newFoldoutPath = foldoutAttribute.name;

                    if (newFoldoutPath == drawingFoldoutPath) return;




                    var drawingPathNames = drawingFoldoutPath.Split('/').Where(r => r != "").ToList();

                    var newPathNames = newFoldoutPath.Split('/').Where(r => r != "").ToList();

                    var sharedPathNames = new List<string>();

                    for (int i = 0; i < drawingPathNames.Count && i < newPathNames.Count; i++)
                        if (drawingPathNames[i] == newPathNames[i])
                            sharedPathNames.Add(drawingPathNames[i]);
                        else break;




                    for (int i = drawingPathNames.Count; i > sharedPathNames.Count; i--)
                        endFoldout();

                    for (int i = sharedPathNames.Count; i < newPathNames.Count; i++)
                        beginFoldout(newPathNames[i]);

                }

                void updateIndentLevel()
                {
                    var prev = EditorGUI.indentLevel;

                    EditorGUI.indentLevel = baseIndentLevel + drawingFoldoutPath.Split('/').Where(r => r != "").Count();

                    if (prev > EditorGUI.indentLevel)
                        Space(7);
                }

                void field()
                {
                    var fieldInfo = memberInfo as FieldInfo;
                    var propertyInfo = memberInfo as PropertyInfo;

                    var isSerialized = serializedProperties_byMemberInfos.TryGetValue(memberInfo, out var serializedProeprty);
                    var isNestedEditor = isSerialized && HasVInspectorAttribtues(fieldInfo.FieldType);
                    var isResettable = isSerialized && VInspectorResettableVariables.IsResettable(fieldInfo);
                    var isReadOnly = Attribute.IsDefined(memberInfo, typeof(ReadOnlyAttribute)) || memberInfo is PropertyInfo && !propertyInfo.CanWrite;

                    void serialized_default()
                    {
                        if (!isSerialized) return;
                        if (isNestedEditor) return;
                        if (isResettable) return;
                        if (Attribute.IsDefined(memberInfo, typeof(ButtonAttribute))) return;

                        EditorGUILayout.PropertyField(serializedProeprty, true);

                    }
                    void serialized_resettable()
                    {
                        if (!isSerialized) return;
                        if (isNestedEditor) return;
                        if (!isResettable) return;


                        var lastControlId = typeof(EditorGUIUtility).GetFieldValue<int>("s_LastControlID");



                        if (!curEvent.isRepaint)
                            if (fieldRects_byLastControlId.ContainsKey(lastControlId))
                                VInspectorResettableVariables.ResetButtonGUI(fieldRects_byLastControlId[lastControlId], serializedProeprty, fieldInfo, targets);



                        EditorGUILayout.PropertyField(serializedProeprty, true);

                        if (curEvent.isRepaint)
                            fieldRects_byLastControlId[lastControlId] = lastRect.AddWidthFromRight(-EditorGUIUtility.labelWidth - 2).SetHeightFromBottom(EditorGUIUtility.singleLineHeight);



                        if (curEvent.isRepaint)
                            VInspectorResettableVariables.ResetButtonGUI(fieldRects_byLastControlId[lastControlId], serializedProeprty, fieldInfo, targets);



                    }
                    void serialized_nestedEditor()
                    {
                        if (!isSerialized) return;
                        if (!isNestedEditor) return;

                        EditorGUILayout.PropertyField(serializedProeprty, false);


                        if (!serializedProeprty.isExpanded) return;

                        if (!nestedEditors_byPropertyPath.TryGetValue(serializedProeprty.propertyPath, out var nestedEditor))
                            nestedEditor = nestedEditors_byPropertyPath[serializedProeprty.propertyPath] = new VInspectorEditor(rootPropertyGetter: () => serializedProeprty,
                                                                                                                                   targetsGetter: () => targets.Select(r => fieldInfo.GetValue(r)));
                        nestedEditor.baseIndentLevel = EditorGUI.indentLevel + 1;
                        nestedEditor.OnGUI();

                    }
                    void nonSerialized_field()
                    {
                        if (isSerialized) return;
                        if (!Attribute.IsDefined(memberInfo, typeof(ShowInInspectorAttribute))) return;
                        if (memberInfo is not FieldInfo fieldInfo) return;


                        var type = fieldInfo.FieldType;
                        var name = fieldInfo.Name.FormatVariableName();

                        var curValue = fieldInfo.GetValue(fieldInfo.IsStatic ? null : target);

                        var newValue = curValue;

                        if (type == typeof(int)) newValue = EditorGUILayout.IntField(name, (int)curValue);
                        else if (type == typeof(float)) newValue = EditorGUILayout.FloatField(name, (float)curValue);
                        else if (type == typeof(double)) newValue = EditorGUILayout.DoubleField(name, (float)curValue);
                        else if (type == typeof(string)) newValue = EditorGUILayout.TextField(name, (string)curValue);
                        else if (type == typeof(bool)) newValue = EditorGUILayout.Toggle(name, (bool)curValue);
                        else if (type == typeof(Vector2)) newValue = EditorGUILayout.Vector2Field(name, (Vector2)curValue);
                        else if (type == typeof(Vector3)) newValue = EditorGUILayout.Vector3Field(name, (Vector3)curValue);
                        else if (type == typeof(Vector4)) newValue = EditorGUILayout.Vector4Field(name, (Vector4)curValue);
                        else if (type == typeof(Color)) newValue = EditorGUILayout.ColorField(name, (Color)curValue);
                        else if (type == typeof(Rect)) newValue = EditorGUILayout.RectField(name, (Rect)curValue);
                        else if (type == typeof(RectInt)) newValue = EditorGUILayout.RectIntField(name, (RectInt)curValue);
                        else if (type == typeof(Bounds)) newValue = EditorGUILayout.BoundsField(name, (Bounds)curValue);
                        else if (type == typeof(BoundsInt)) newValue = EditorGUILayout.BoundsIntField(name, (BoundsInt)curValue);
                        else if (type == typeof(Vector2Int)) newValue = EditorGUILayout.Vector2IntField(name, (Vector2Int)curValue);
                        else if (type == typeof(Vector3Int)) newValue = EditorGUILayout.Vector3IntField(name, (Vector3Int)curValue);
                        else if (type.IsEnum) newValue = EditorGUILayout.EnumPopup(name, (System.Enum)curValue);
                        else if (typeof(Object).IsAssignableFrom(type)) newValue = EditorGUILayout.ObjectField(name, (Object)curValue, type, true);
                        else EditorGUILayout.TextField(name, curValue?.ToString());


                        fieldInfo.SetValue(fieldInfo.IsStatic ? null : target, newValue);

                        noVariablesShown = false;

                    }
                    void nonSerialized_property()
                    {
                        if (isSerialized) return;
                        if (!Attribute.IsDefined(memberInfo, typeof(ShowInInspectorAttribute))) return;
                        if (memberInfo is not PropertyInfo propertyInfo) return;


                        var type = propertyInfo.PropertyType;
                        var name = propertyInfo.Name.FormatVariableName();

                        var curValue = propertyInfo.GetValue(propertyInfo.GetAccessors(true).First().IsStatic ? null : target);

                        var newValue = curValue;

                        if (type == typeof(int)) newValue = EditorGUILayout.IntField(name, (int)curValue);
                        else if (type == typeof(float)) newValue = EditorGUILayout.FloatField(name, (float)curValue);
                        else if (type == typeof(double)) newValue = EditorGUILayout.DoubleField(name, (float)curValue);
                        else if (type == typeof(string)) newValue = EditorGUILayout.TextField(name, (string)curValue);
                        else if (type == typeof(bool)) newValue = EditorGUILayout.Toggle(name, (bool)curValue);
                        else if (type == typeof(Vector2)) newValue = EditorGUILayout.Vector2Field(name, (Vector2)curValue);
                        else if (type == typeof(Vector3)) newValue = EditorGUILayout.Vector3Field(name, (Vector3)curValue);
                        else if (type == typeof(Vector4)) newValue = EditorGUILayout.Vector4Field(name, (Vector4)curValue);
                        else if (type == typeof(Color)) newValue = EditorGUILayout.ColorField(name, (Color)curValue);
                        else if (type == typeof(Rect)) newValue = EditorGUILayout.RectField(name, (Rect)curValue);
                        else if (type == typeof(RectInt)) newValue = EditorGUILayout.RectIntField(name, (RectInt)curValue);
                        else if (type == typeof(Bounds)) newValue = EditorGUILayout.BoundsField(name, (Bounds)curValue);
                        else if (type == typeof(BoundsInt)) newValue = EditorGUILayout.BoundsIntField(name, (BoundsInt)curValue);
                        else if (type == typeof(Vector2Int)) newValue = EditorGUILayout.Vector2IntField(name, (Vector2Int)curValue);
                        else if (type == typeof(Vector3Int)) newValue = EditorGUILayout.Vector3IntField(name, (Vector3Int)curValue);
                        else if (type.IsEnum) newValue = EditorGUILayout.EnumPopup(name, (System.Enum)curValue);
                        else if (typeof(Object).IsAssignableFrom(type)) newValue = EditorGUILayout.ObjectField(name, (Object)curValue, type, true);
                        else EditorGUILayout.TextField(name, curValue?.ToString());


                        if (propertyInfo.CanWrite && !Attribute.IsDefined(propertyInfo, typeof(ReadOnlyAttribute)))
                            if (!object.Equals(newValue, curValue))
                                propertyInfo.SetValue(propertyInfo.GetAccessors(true).First().IsStatic ? null : target, newValue);

                        noVariablesShown = false;

                    }

                    void invokeValueChangedCallbacks()
                    {
                        if (memberInfo is not FieldInfo fieldInfo) return;

                        if (!valueChangedCallbacks_byFieldInfo.TryGetValue(fieldInfo, out var methodInfos))
                            if (!valueChangedCallbacks_byGroupPath.TryGetValue(drawingTabPath, out methodInfos))
                                if (!valueChangedCallbacks_byGroupPath.TryGetValue(drawingFoldoutPath, out methodInfos)) return;


                        void invokeCallbacks() => methodInfos.ForEach(r => targets.ForEach(rr => r.Invoke(rr, null)));

                        // invokeCallbacks();
                        AbstractEditor.toCallAfterModifyingSO += invokeCallbacks;

                        valueChangedCallbacks_byUndoPosition[EditorUtils.GetCurrendUndoGroupIndex()] = invokeCallbacks;

                    }


                    SetGUIEnabled(!isReadOnly && !disableField);

                    EditorGUI.BeginChangeCheck();

                    serialized_default();
                    serialized_resettable();
                    serialized_nestedEditor();
                    nonSerialized_field();
                    nonSerialized_property();

                    if (EditorGUI.EndChangeCheck())
                        invokeValueChangedCallbacks();

                    ResetGUIEnabled();

                }





                ifs();

                if (hideField) return;

                GUI.enabled = !disableField;




                tabs();

                if (selectedTabPath != drawingTabPath && !selectedTabPath.StartsWith(drawingTabPath + "/") && drawingTabPath != "") return;

                noVariablesShown = false;




                foldouts();

                if (!rootFoldout.IsSubfoldoutContentVisible(drawingFoldoutPath)) return;





                updateIndentLevel();

                field();

            }

            void drawButton(Button button, ref bool noButtonsShown)
            {
                if (button.tabAttribute != null && !selectedTabPath.StartsWith(button.tabAttribute.name)) return;

                if (button.ifAttribute is HideIfAttribute && button.ifAttribute.Evaluate(target)) return;
                if (button.ifAttribute is ShowIfAttribute && !button.ifAttribute.Evaluate(target)) return;



                var prevGuiEnabled = GUI.enabled;
                if (button.ifAttribute is DisableIfAttribute && button.ifAttribute.Evaluate(target)) GUI.enabled = false;
                if (button.ifAttribute is EnableIfAttribute && !button.ifAttribute.Evaluate(target)) GUI.enabled = false;




                Rect buttonRect;
                Color color = Color.white;

                void set_buttonRect()
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Space(EditorGUI.indentLevel * 15);

                    buttonRect = ExpandWidthLabelRect(height: button.size);

                    GUILayout.EndHorizontal();

                }
                void set_color()
                {
                    if (button.color.ToLower() == "grey" || button.color.ToLower() == "gray") return;


                    var hue = 0f;
                    var saturation = .6f;
                    var lightness = isDarkTheme ? .57f : .64f;

                    if (button.color.ToLower() == "red") hue = 0;
                    else if (button.color.ToLower() == "orange") hue = .08f;
                    else if (button.color.ToLower() == "yellow") hue = .13f;
                    else if (button.color.ToLower() == "green") { hue = .32f; saturation = .49f; lightness = isDarkTheme ? .56f : .6f; }
                    else if (button.color.ToLower() == "blue") hue = .55f;
                    else if (button.color.ToLower() == "pink") hue = .94f;
                    else return;



                    color = ColorUtils.HSLToRGB(hue, saturation, lightness);

                    color *= 2f;
                    color.a = 1;

                }

                void argumentsBackground()
                {
                    if (!button.parameterInfos.Any()) return;
                    if (!button.isExpanded) return;
                    if (!curEvent.isRepaint) return;


                    var backgroundColor = Greyscale(isDarkTheme ? .27f : .83f);
                    var outlineColor = Greyscale(isDarkTheme ? .15f : .65f);
                    var cornerRadius = 3;

                    var backgroundRect = buttonRect.AddHeight(button.parameterInfos.Count * (EditorGUIUtility.singleLineHeight + 2) + 8);

                    backgroundRect.DrawRounded(outlineColor, cornerRadius);
                    backgroundRect.Resize(1).DrawRounded(backgroundColor, cornerRadius - 1);

                }
                void buttonItself()
                {
                    var prevBackgroundColor = GUI.backgroundColor;
                    GUI.backgroundColor = button.isPressed() ? GUIColors.pressedButtonBackground : color;

                    var clicked = GUI.Button(buttonRect, button.name);

                    GUI.backgroundColor = prevBackgroundColor;



                    if (!clicked) return;

                    foreach (var targetObject in rootProperty.serializedObject.targetObjects)
                        targetObject.RecordUndo();

                    foreach (var target in targets)
                        button.action(target);

                    foreach (var targetObject in rootProperty.serializedObject.targetObjects)
                        targetObject.Dirty();

                }
                void expandButton()
                {
                    if (!button.parameterInfos.Any()) return;

                    var expandButtonRect = buttonRect.SetWidth(24).MoveX(1);

                    var colorNormal = Greyscale(isDarkTheme ? (buttonRect.IsHovered() ? .85f : .8f) : .7f);
                    var colorHovered = Greyscale(isDarkTheme ? 10f : 0f, 10f);
                    var colorPressed = Greyscale(.85f);

                    var iconSize = 12;


                    if (!IconButton(expandButtonRect, button.isExpanded ? "d_IN_foldout_act_on" : "d_IN_foldout_act", iconSize, colorNormal, colorHovered, colorPressed)) return;

                    button.isExpanded = !button.isExpanded;

                    GUIUtility.keyboardControl = 0;

                }
                void parameters()
                {
                    if (!button.isExpanded) return;
                    if (!button.parameterInfos.Any()) return;

                    void parameter(int i)
                    {
                        var type = button.parameterInfos[i].ParameterType;
                        var name = button.parameterInfos[i].Name.FormatVariableName();

                        var curValue = button.GetParameterValue(i);

                        var newValue = curValue;

                        if (type == typeof(int)) newValue = EditorGUILayout.IntField(name, (int)curValue);
                        else if (type == typeof(float)) newValue = EditorGUILayout.FloatField(name, (float)curValue);
                        else if (type == typeof(double)) newValue = EditorGUILayout.DoubleField(name, (float)curValue);
                        else if (type == typeof(string)) newValue = EditorGUILayout.TextField(name, (string)curValue);
                        else if (type == typeof(bool)) newValue = EditorGUILayout.Toggle(name, (bool)curValue);
                        else if (type == typeof(Vector2)) newValue = EditorGUILayout.Vector2Field(name, (Vector2)curValue);
                        else if (type == typeof(Vector3)) newValue = EditorGUILayout.Vector3Field(name, (Vector3)curValue);
                        else if (type == typeof(Vector4)) newValue = EditorGUILayout.Vector4Field(name, (Vector4)curValue);
                        else if (type == typeof(Color)) newValue = EditorGUILayout.ColorField(name, (Color)curValue);
                        else if (type == typeof(Rect)) newValue = EditorGUILayout.RectField(name, (Rect)curValue);
                        else if (type == typeof(RectInt)) newValue = EditorGUILayout.RectIntField(name, (RectInt)curValue);
                        else if (type == typeof(Bounds)) newValue = EditorGUILayout.BoundsField(name, (Bounds)curValue);
                        else if (type == typeof(BoundsInt)) newValue = EditorGUILayout.BoundsIntField(name, (BoundsInt)curValue);
                        else if (type == typeof(Vector2Int)) newValue = EditorGUILayout.Vector2IntField(name, (Vector2Int)curValue);
                        else if (type == typeof(Vector3Int)) newValue = EditorGUILayout.Vector3IntField(name, (Vector3Int)curValue);
                        else if (type.IsEnum) newValue = EditorGUILayout.EnumPopup(name, (System.Enum)curValue);
                        else if (typeof(Object).IsAssignableFrom(type)) newValue = EditorGUILayout.ObjectField(name, (Object)curValue, type, true);
                        else EditorGUILayout.PrefixLabel(name);


                        button.SetParameterValue(i, newValue);

                    }



                    BeginIndent(7);
                    Space(1);

                    for (int i = 0; i < button.parameterInfos.Count; i++)
                        parameter(i);

                    Space(11);
                    EndIndent(5);


                }


                GUILayout.Space(button.space - 2);

                set_buttonRect();
                set_color();
                argumentsBackground();

                if (!curEvent.isRepaint)
                    expandButton();

                buttonItself();

                if (curEvent.isRepaint)
                    expandButton();

                parameters();

                if (button.isExpanded)
                    Space(6);




                GUI.enabled = prevGuiEnabled;

                noButtonsShown = false;

            }


            void scriptField()
            {
                if (scriptFieldProperty == null) return;
                if (VInspectorMenu.hideScriptFieldEnabled) return;

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(scriptFieldProperty);

            }
            void topMargin()
            {
                if (scriptFieldProperty == null) return;
                if (!VInspectorMenu.hideScriptFieldEnabled) return;

                Space(2);

            }

            void members()
            {
                EditorGUI.indentLevel = baseIndentLevel;

                foreach (var memberInfo in drawableMemberLists_byTargetType[targetType])
                    drawMember(memberInfo);

                EditorGUI.indentLevel = baseIndentLevel;


            }
            void noVariablesToShow()
            {
                if (!noVariablesShown) return;

                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Label("No variables to show");


            }
            void endUnendedFoldouts()
            {
                if (drawingFoldoutPath == "") return;

                void drawButtons(Foldout foldout)
                {
                    var noButtonsToShow = true;

                    Space(10);

                    foreach (var button in foldout.buttons)
                        drawButton(button, ref noButtonsToShow);


                    if (noButtonsToShow)
                        Space(-10);
                    else
                        Space(5);

                }

                while (drawingFoldoutPath != "")
                {
                    var foldout = rootFoldout.GetSubfoldout(drawingFoldoutPath);

                    EditorGUI.indentLevel = baseIndentLevel + drawingFoldoutPath.Split('/').Where(r => r != "").Count();

                    if (rootFoldout.IsSubfoldoutContentVisible(drawingFoldoutPath))
                        drawButtons(foldout);

                    drawingFoldoutPath = drawingFoldoutPath.HasParentPath() ? drawingFoldoutPath.GetParentPath() : "";

                }

                EditorGUI.indentLevel = 0;

            }

            void buttons()
            {
                GUI.enabled = true;

                var noButtonsToShow = true;

                foreach (var button in this.buttons)
                    drawButton(button, ref noButtonsToShow);


                if (noButtonsToShow)
                    Space(-17);

            }




            rootTab.ResetSubtabsDrawn();

            scriptField();
            topMargin();

            members();
            noVariablesToShow();
            endUnendedFoldouts();

            Space(16);
            buttons();

            Space(4);

        }

        public int baseIndentLevel;

        public SerializedProperty rootProperty => rootPropertyGetter.Invoke();
        public IEnumerable<object> targets => targetsGetter.Invoke();
        public object target => targets.FirstOrDefault();

        public Type targetType => _targetType ??= target?.GetType();
        public Type _targetType;

        static Dictionary<int, Rect> fieldRects_byLastControlId = new();







        public VInspectorEditor(System.Func<SerializedProperty> rootPropertyGetter, System.Func<IEnumerable<object>> targetsGetter)
        {
            this.rootPropertyGetter = rootPropertyGetter;
            this.targetsGetter = targetsGetter;


            void createTabs()
            {
                void setupTab(Tab tab, IEnumerable<string> allSubtabPaths)
                {
                    void refreshSubtabs()
                    {
                        var names = allSubtabPaths.Select(r => r.Split('/').First()).ToList();

                        foreach (var name in names)
                            if (!tab.subtabs.Any(r => r.name == name))
                                tab.subtabs.Add(new Tab() { name = name });

                        foreach (var subtab in tab.subtabs.ToList())
                            if (!names.Any(r => r == subtab.name))
                                tab.subtabs.Remove(subtab);

                        tab.subtabs.SortBy(r => names.IndexOf(r.name));

                    }
                    void setupSubtabs()
                    {
                        foreach (var subtab in tab.subtabs)
                            setupTab(subtab, allSubtabPaths.Where(r => r.StartsWith(subtab.name + "/")).Select(r => r.Remove(subtab.name + "/")).ToList());
                    }

                    refreshSubtabs();
                    setupSubtabs();

                }

                void findAttributes()
                {
                    if (tabAttributes_byTargetType.ContainsKey(targetType)) return;

                    var attributes = TypeCache.GetFieldsWithAttribute<TabAttribute>()
                                              .Where(r => r.DeclaringType.IsAssignableFrom(targetType))
                                              .OrderBy(r => r.MetadataToken)
                                              .Select(r => r.GetCustomAttributeCached<TabAttribute>());

                    tabAttributes_byTargetType[targetType] = attributes.ToList();

                }
                void createTabs()
                {
                    rootTab = new Tab() { isRootTab = true };

                    var allTabPaths = tabAttributes_byTargetType[targetType].Select(r => r.name);

                    setupTab(rootTab, allTabPaths);

                }


                findAttributes();
                createTabs();

            }
            void createFoldouts()
            {
                void setupFoldout(Foldout foldout, IEnumerable<string> allSubfoldoutPaths)
                {
                    void refreshSubfoldouts()
                    {
                        var names = allSubfoldoutPaths.Select(r => r.Split('/').First()).ToList();

                        foreach (var name in names)
                            if (foldout.subfoldouts.Find(r => r.name == name) == null)
                                foldout.subfoldouts.Add(new Foldout() { name = name });

                        foreach (var subtab in foldout.subfoldouts.ToList())
                            if (names.Find(r => r == subtab.name) == null)
                                foldout.subfoldouts.Remove(subtab);

                        foldout.subfoldouts.SortBy(r => names.IndexOf(r.name));

                    }
                    void setupSubfoldouts()
                    {
                        foreach (var subtab in foldout.subfoldouts)
                            setupFoldout(subtab, allSubfoldoutPaths.Where(r => r.StartsWith(subtab.name + "/")).Select(r => r.Remove(subtab.name + "/")).ToList());
                    }

                    refreshSubfoldouts();
                    setupSubfoldouts();

                }

                void findAttributes()
                {
                    if (foldoutAttributes_byTargetType.ContainsKey(targetType)) return;

                    var attributes = TypeCache.GetFieldsWithAttribute<FoldoutAttribute>()
                                              .Where(r => r.DeclaringType.IsAssignableFrom(targetType))
                                              .OrderBy(r => r.MetadataToken)
                                              .Select(r => r.GetCustomAttributeCached<FoldoutAttribute>());

                    foldoutAttributes_byTargetType[targetType] = attributes.ToList();

                }
                void createFoldouts()
                {
                    rootFoldout = new Foldout() { isRootFoldout = true };

                    var allFoldoutPaths = foldoutAttributes_byTargetType[targetType].Select(r => r.name);

                    setupFoldout(rootFoldout, allFoldoutPaths);

                }


                findAttributes();
                createFoldouts();

            }
            void createButtons()
            {
                void createButton(MemberInfo member, ButtonAttribute buttonAttribute)
                {
                    var button = new Button();

                    button.size = buttonAttribute.size;
                    button.space = buttonAttribute.space;
                    button.color = buttonAttribute.color;


                    if (member.GetCustomAttributeCached<TabAttribute>() is TabAttribute tabAttribute)
                        button.tabAttribute = tabAttribute;

                    if (member.GetCustomAttributeCached<FoldoutAttribute>() is FoldoutAttribute foldoutAttribute)
                        button.foldoutAttribute = foldoutAttribute;

                    if (member.GetCustomAttributeCached<IfAttribute>() is IfAttribute ifAttribute)
                        button.ifAttribute = ifAttribute;


                    if (member is FieldInfo field && field.FieldType == typeof(bool))
                    {
                        var fieldTarget = field.IsStatic ? null : target;

                        button.action = (o) => field.SetValue(o, !(bool)field.GetValue(o));
                        button.name = buttonAttribute.name != "" ? buttonAttribute.name : field.Name.FormatVariableName(false);
                        button.isPressed = () => (bool)field.GetValue(fieldTarget);

                    }

                    if (member is MethodInfo method)
                    {
                        if (method.DeclaringType.IsGenericType)
                            method = targetType.GetMethodInfo(method.Name, method.GetParameters().Select(r => r.ParameterType).ToArray()) ?? method; // fixes wrong method when targetType inherits from method.DeclaringType and method.DeclaringType is generic

                        if (!method.GetParameters().Any())
                        {
                            var methodTarget = method.IsStatic ? null : target;

                            button.action = (methodTarget) => method.Invoke(methodTarget, null);
                            button.name = buttonAttribute.name != "" ? buttonAttribute.name : method.Name.FormatVariableName(false);
                            button.isPressed = () => false;

                        }
                        else
                        {
                            var methodTarget = method.IsStatic ? null : target;

                            button.action = (methodTarget) => method.Invoke(methodTarget, Enumerable.Range(0, button.parameterInfos.Count).Select(i => button.GetParameterValue(i)).ToArray());
                            button.name = buttonAttribute.name != "" ? buttonAttribute.name : method.Name.FormatVariableName(false);
                            button.isPressed = () => false;

                            button.parameterInfos = method.GetParameters().ToList();

                        }

                    }



                    if (button.action != null)
                        if (button.foldoutAttribute != null && rootFoldout.GetSubfoldout(button.foldoutAttribute.name) is Foldout foldout)
                            foldout.buttons.Add(button);
                        else
                            this.buttons.Add(button);

                }

                void findFields()
                {
                    if (fieldsWithButtonAttributes_byTargetType.ContainsKey(targetType)) return;

                    var fields = TypeCache.GetFieldsWithAttribute<ButtonAttribute>()
                                          .Where(r => r.DeclaringType.IsAssignableFrom(targetType))
                                          .OrderBy(r => r.MetadataToken);

                    fieldsWithButtonAttributes_byTargetType[targetType] = fields.ToList();

                }
                void findMethods()
                {
                    if (methodsWithButtonAttributes_byTargetType.ContainsKey(targetType)) return;

                    var methods = TypeCache.GetMethodsWithAttribute<ButtonAttribute>()
                                           .Where(r => r.DeclaringType.IsAssignableFrom(targetType) || (targetType.BaseType.IsGenericType && targetType.BaseType.GetGenericTypeDefinition() == r.DeclaringType))
                                           .OrderBy(r => r.MetadataToken);

                    methodsWithButtonAttributes_byTargetType[targetType] = methods.ToList();

                }
                void createButtons()
                {
                    this.buttons = new List<Button>();

                    foreach (var method in methodsWithButtonAttributes_byTargetType[targetType])
                        createButton(method, method.GetCustomAttributeCached<ButtonAttribute>());

                    foreach (var field in fieldsWithButtonAttributes_byTargetType[targetType])
                        createButton(field, field.GetCustomAttributeCached<ButtonAttribute>());

                }


                findFields();
                findMethods();
                createButtons();

            }
            void linkToState()
            {
                if (!rootTab.subtabs.Any() && !rootFoldout.subfoldouts.Any() && !this.buttons.Any(r => r.parameterInfos.Any())) return;


                AttributesState attributesState;

                void set_attributesState()
                {
                    var scriptName = rootProperty.serializedObject.targetObject.GetType().Name;

                    if (!VInspectorState.instance.attributeStates_byScriptName.ContainsKey(scriptName))
                        VInspectorState.instance.attributeStates_byScriptName[scriptName] = new();

                    attributesState = VInspectorState.instance.attributeStates_byScriptName[scriptName];

                }
                void linkTab(Tab tab, string parentPath)
                {
                    tab.attributesState = attributesState;
                    tab.path = parentPath + "/" + tab.name;

                    attributesState.selectedSubtabIndexes_byTabPath.TryGetValue(tab.path, out tab._selectedSubtabIndex);

                    foreach (var subtab in tab.subtabs)
                        linkTab(subtab, tab.path);

                }
                void linkFoldout(Foldout foldout, string parentPath)
                {
                    foldout.attributesState = attributesState;
                    foldout.path = parentPath + "/" + foldout.name;

                    attributesState.isExpandeds_byFoldoutPath.TryGetValue(foldout.path, out foldout._isExpanded);

                    foreach (var subfoldout in foldout.subfoldouts)
                        linkFoldout(subfoldout, foldout.path);

                }
                void linkButton(Button button, string parentPath)
                {
                    button.attributesState = attributesState;
                    button.path = parentPath + "/" + button.name;

                    attributesState.isExpandeds_byButtonPath.TryGetValue(button.path, out button._isExpanded);

                }


                set_attributesState();
                linkTab(rootTab, rootProperty.propertyPath);
                linkFoldout(rootFoldout, rootProperty.propertyPath);

                foreach (var r in this.buttons.Where(r => r.parameterInfos.Any()))
                    linkButton(r, rootProperty.propertyPath);

            }

            void createValueChangedCallbacks()
            {
                if (valueChangedCallbacks_byFieldInfo != null) return;

                valueChangedCallbacks_byFieldInfo = new();
                valueChangedCallbacks_byGroupPath = new();

                var methodInfos = TypeCache.GetMethodsWithAttribute<OnValueChangedAttribute>()
                   .Where(r => r.GetParameters().Length == 0)
                   .OrderBy(r => r.MetadataToken);


                void Add<T1, T2>(Dictionary<T1, List<T2>> dictionary, T1 key, T2 value)
                {
                    if (dictionary.TryGetValue(key, out var alreadyCreatedList))
                        alreadyCreatedList.Add(value);
                    else
                        dictionary[key] = new List<T2> { value };

                }

                foreach (var methodInfo in methodInfos)
                    foreach (var attribute in methodInfo.GetCustomAttributes<OnValueChangedAttribute>())
                        foreach (var name in attribute.variableOrGroupNames)

                            if (methodInfo.DeclaringType.GetFieldInfo(name) is FieldInfo fieldInfo)
                                Add(valueChangedCallbacks_byFieldInfo, fieldInfo, methodInfo);
                            else
                                Add(valueChangedCallbacks_byGroupPath, name, methodInfo);

            }

            void fillPropertiesDictionary()
            {
                serializedProperties_byMemberInfos = new();


                var curProperty = rootProperty.Copy();

                curProperty.NextVisible(true);

                if (curProperty.name == "m_Script")
                {
                    scriptFieldProperty = curProperty.Copy();
                    if (!curProperty.NextVisible(false)) return;
                }


                do
                    if (targetType.GetFieldInfo(curProperty.name) is FieldInfo fieldInfo)
                        if (curProperty.propertyPath.StartsWith(rootProperty.propertyPath) || rootProperty.propertyPath == "") // fixes bug where nested editors could contain members with the same name from parent editors
                            serializedProperties_byMemberInfos[fieldInfo] = curProperty.Copy();

                while (curProperty.NextVisible(false));

            }
            void fillDrawableMembers()
            {
                if (drawableMemberLists_byTargetType.ContainsKey(targetType)) return;



                var membersHashset = new HashSet<MemberInfo>();

                void serializedFields()
                {
                    var curProperty = rootProperty.Copy();

                    curProperty.NextVisible(true);

                    if (curProperty.name == "m_Script")
                        if (!curProperty.NextVisible(false)) return;

                    do if (targetType.GetFieldInfo(curProperty.name) is FieldInfo fieldInfo)
                            membersHashset.Add(fieldInfo);
                    while (curProperty.NextVisible(false));


                }
                void showInInspectorFields()
                {
                    membersHashset.UnionWith(TypeCache.GetFieldsWithAttribute<ShowInInspectorAttribute>()
                                               .Where(r => r.DeclaringType.IsAssignableFrom(targetType))
                                               .Select(r => r as MemberInfo));
                }
                void showInInspectorProperties()
                {
                    membersHashset.UnionWith(targetType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                                .Where(r => Attribute.IsDefined(r, typeof(ShowInInspectorAttribute)))
                                                .Where(r => r.CanRead)
                                                .Select(r => r as MemberInfo));
                }
                void groupingAttributesMembers()
                {
                    membersHashset.UnionWith(TypeCache.GetFieldsWithAttribute<TabAttribute>()
                                               .Where(r => r.DeclaringType.IsAssignableFrom(targetType)));

                    membersHashset.UnionWith(TypeCache.GetFieldsWithAttribute<EndTabAttribute>()
                                               .Where(r => r.DeclaringType.IsAssignableFrom(targetType)));



                    membersHashset.UnionWith(TypeCache.GetFieldsWithAttribute<FoldoutAttribute>()
                                               .Where(r => r.DeclaringType.IsAssignableFrom(targetType)));

                    membersHashset.UnionWith(TypeCache.GetFieldsWithAttribute<EndFoldoutAttribute>()
                                               .Where(r => r.DeclaringType.IsAssignableFrom(targetType)));




                    membersHashset.UnionWith(TypeCache.GetFieldsWithAttribute<IfAttribute>()
                                               .Where(r => r.DeclaringType.IsAssignableFrom(targetType)));

                    membersHashset.UnionWith(TypeCache.GetFieldsWithAttribute<EndIfAttribute>()
                                               .Where(r => r.DeclaringType.IsAssignableFrom(targetType)));




                    // these members aren't necessarily visible 
                    // but need to be drawn anyway for grouping to work as users would expect

                }



                var membersList = new List<MemberInfo>();

                void orderByDeclaration()
                {
                    membersList = membersHashset.OrderBy(r => r.DeclaringType == targetType)
                                                 .ThenBy(r => r.MetadataToken)
                                                 .ToList();
                }
                void insertTypeChangeMarkers()
                {
                    var prevDeclaringType = default(Type);

                    for (int i = 0; i < membersList.Count; i++)
                    {
                        if (prevDeclaringType != membersList[i].DeclaringType && prevDeclaringType != null)
                        {
                            membersList.Insert(i, typeof(VInspectorEditor).GetFieldInfo(nameof(declaringTypeChangeMarker)));
                            i++;

                        }

                        prevDeclaringType = membersList[i].DeclaringType;

                    }

                }
                void mergeFoldouts()
                {
                    var endedFoldouts = new HashSet<string>();
                    var lastMemberInFoldout_byFoldoutPath = new Dictionary<string, MemberInfo>();

                    var curFoldoutPath = "";
                    var prevFoldoutPath = "";

                    for (int i = 0; i < membersList.Count; i++)
                    {
                        if (membersList[i].GetCustomAttributeCached<EndFoldoutAttribute>() is EndFoldoutAttribute)
                            curFoldoutPath = "";

                        if (membersList[i].GetCustomAttributeCached<FoldoutAttribute>() is FoldoutAttribute foldoutAttribute)
                            curFoldoutPath = foldoutAttribute.name;




                        if (prevFoldoutPath != curFoldoutPath && prevFoldoutPath != "")
                        {
                            endedFoldouts.Add(prevFoldoutPath);
                            lastMemberInFoldout_byFoldoutPath[prevFoldoutPath] = membersList[i - 1];
                        }

                        if (endedFoldouts.Contains(curFoldoutPath))
                        {
                            var member = membersList[i];

                            var foldoutEndsAt = membersList.IndexOf(lastMemberInFoldout_byFoldoutPath[curFoldoutPath]);

                            membersList.RemoveAt(i);
                            membersList.AddAt(member, foldoutEndsAt + 1);

                        }



                        prevFoldoutPath = curFoldoutPath;

                    }

                }




                serializedFields();
                showInInspectorFields();
                showInInspectorProperties();
                groupingAttributesMembers();

                orderByDeclaration();
                insertTypeChangeMarkers();
                mergeFoldouts();

                drawableMemberLists_byTargetType[targetType] = membersList;

            }


            createTabs();
            createFoldouts();
            createButtons();
            linkToState();

            createValueChangedCallbacks();

            fillPropertiesDictionary();
            fillDrawableMembers();

        }

        public System.Func<IEnumerable<object>> targetsGetter;
        public System.Func<SerializedProperty> rootPropertyGetter;

        public Tab rootTab;
        public Foldout rootFoldout;
        public List<Button> buttons = new();
        public Dictionary<string, VInspectorEditor> nestedEditors_byPropertyPath = new();

        public SerializedProperty scriptFieldProperty;
        public Dictionary<MemberInfo, SerializedProperty> serializedProperties_byMemberInfos = new();


        static Dictionary<Type, List<MemberInfo>> drawableMemberLists_byTargetType = new();

        static Dictionary<Type, List<FieldInfo>> fieldsWithButtonAttributes_byTargetType = new();
        static Dictionary<Type, List<MethodInfo>> methodsWithButtonAttributes_byTargetType = new();
        static Dictionary<Type, List<TabAttribute>> tabAttributes_byTargetType = new();
        static Dictionary<Type, List<FoldoutAttribute>> foldoutAttributes_byTargetType = new();
        static Dictionary<Type, List<MemberInfo>> showInInspectorMembers_byTargetType = new();
        static Dictionary<FieldInfo, List<MethodInfo>> valueChangedCallbacks_byFieldInfo;
        static Dictionary<string, List<MethodInfo>> valueChangedCallbacks_byGroupPath;


        [EndFoldout, EndTab, EndIf]
        static object declaringTypeChangeMarker;

    }

    class Tab
    {
        public string name;

        public List<Tab> subtabs = new();

        public bool isRootTab;




        public Tab selectedSubtab => selectedSubtabIndex.IsInRange(0, subtabs.Count - 1) ? subtabs[selectedSubtabIndex] : null;

        public int selectedSubtabIndex
        {
            get => _selectedSubtabIndex;
            set
            {
                _selectedSubtabIndex = value;

                if (attributesState != null)
                    attributesState.selectedSubtabIndexes_byTabPath[path] = value;

            }
        }
        public int _selectedSubtabIndex;

        public AttributesState attributesState;
        public string path;





        public string GetSelectedTabPath()
        {
            if (!subtabs.Any()) return "";

            if (selectedSubtab == null)
                selectedSubtabIndex = 0;

            return (selectedSubtab.name + "/" + selectedSubtab.GetSelectedTabPath()).Trim('/');

        }


        public void ResetSubtabsDrawn()
        {
            subtabsDrawn = false;

            foreach (var r in subtabs)
                r.ResetSubtabsDrawn();

        }

        public bool subtabsDrawn;

    }
    class Foldout
    {
        public string name;

        public List<Foldout> subfoldouts = new();

        public List<Button> buttons = new();

        public bool isRootFoldout;




        public bool isExpanded
        {
            get => _isExpanded || isRootFoldout;
            set
            {
                _isExpanded = value;

                if (attributesState != null)
                    attributesState.isExpandeds_byFoldoutPath[path] = value;

            }
        }
        public bool _isExpanded;

        public AttributesState attributesState;
        public string path;




        public Foldout GetSubfoldout(string path)
        {
            if (path == "")
                return this;
            else if (!path.Contains('/'))
                return subfoldouts.Find(r => r.name == path);
            else
                return subfoldouts.Find(r => r.name == path.Split('/').First()).GetSubfoldout(path.Substring(path.IndexOf('/') + 1));

        }

        public bool IsSubfoldoutContentVisible(string path)
        {
            if (path == "")
                return isExpanded;
            else if (!path.Contains('/'))
                return isExpanded && subfoldouts.Find(r => r.name == path).isExpanded;
            else
                return isExpanded && subfoldouts.Find(r => r.name == path.Split('/').First()).IsSubfoldoutContentVisible(path.Substring(path.IndexOf('/') + 1));

        }

    }
    class Button
    {
        public string name;
        public float size;
        public float space;
        public string color;

        public TabAttribute tabAttribute;
        public FoldoutAttribute foldoutAttribute;
        public IfAttribute ifAttribute;

        public System.Action<object> action;
        public System.Func<bool> isPressed;




        public bool isExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;

                if (attributesState != null)
                    attributesState.isExpandeds_byButtonPath[path] = value;

            }
        }
        public bool _isExpanded;

        public string path;

        public AttributesState attributesState;






        public object GetParameterValue(int i)
        {
            if (!parameterValues_byIndex.ContainsKey(i))
            {
                var parameterInfo = parameterInfos[i];

                var defaultValue = parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : parameterInfo.ParameterType.IsValueType ? System.Activator.CreateInstance(parameterInfo.ParameterType) : null;

                parameterValues_byIndex[i] = defaultValue;

            }


            return parameterValues_byIndex[i];

        }
        public void SetParameterValue(int i, object value)
        {
            parameterValues_byIndex[i] = value;
        }

        public Dictionary<int, object> parameterValues_byIndex = new();


        public List<ParameterInfo> parameterInfos = new();

    }



    #region custom editors

    class AbstractEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            if (isScriptMissing) { MissingScriptGUI(); return; }

            serializedObject.UpdateIfRequiredOrScript();

            rootEditor.OnGUI();

            if (serializedObject.ApplyModifiedProperties())
                toCallAfterModifyingSO?.Invoke();

            toCallAfterModifyingSO = null;


            // GUILayout.Label("vInspector's IMGUI editor");

        }

        public static System.Action toCallAfterModifyingSO;

        void MissingScriptGUI()
        {
            SetGUIEnabled(true);

            if (serializedObject.FindProperty("m_Script") is SerializedProperty scriptProperty)
            {
                EditorGUILayout.PropertyField(scriptProperty);
                serializedObject.ApplyModifiedProperties();
            }

            var s = "Script cannot be loaded";
            s += "\nPossible reasons:";
            s += "\n- Compile erros";
            s += "\n- Script is deleted";
            s += "\n- Script file name doesn't match class name";
            s += "\n- Class doesn't inherit from MonoBehaviour";

            Space(4);
            EditorGUILayout.HelpBox(s, MessageType.Warning, true);

            Space(4);

            ResetGUIEnabled();

        }




        public override VisualElement CreateInspectorGUI()
        {
            if (!useUITK) return null;


            var rootElement = new VisualElement();

            rootElement.style.paddingTop = VInspectorMenu.hideScriptFieldEnabled ? 3 : 1;
            rootElement.style.paddingBottom = 4;


            InspectorElement.FillDefaultInspector(rootElement, serializedObject, this);

            if (VInspectorMenu.hideScriptFieldEnabled)
                rootElement.Q("PropertyField:m_Script")?.RemoveFromHierarchy();

            if (HasVInspectorAttribtues(target.GetType()))
                rootElement.Add(new HelpBox("vInspector attributes are disabled in this script because it contains property drawers implemented with UI Toolkit, which doesn't allow using IMGUI editors such as vInspector's attribute system", HelpBoxMessageType.Info));


            return rootElement;

        }

        bool useUITK => HasUITKOnlyDrawers(serializedObject);




        void OnEnable()
        {
            if (target)
                isScriptMissing = target.GetType() == typeof(MonoBehaviour) || target.GetType() == typeof(ScriptableObject);
            else
                isScriptMissing = target is MonoBehaviour || target is ScriptableObject;


            if (isScriptMissing) return;

            rootEditor = new VInspectorEditor(rootPropertyGetter: () => serializedObject.GetIterator(),
                                                   targetsGetter: () => serializedObject.targetObjects);

        }

        VInspectorEditor rootEditor;

        bool isScriptMissing;


    }



#if !VINSPECTOR_ATTRIBUTES_DISABLED
    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
#endif
    class ScriptEditor : AbstractEditor { }


#if !VINSPECTOR_ATTRIBUTES_DISABLED
    [CustomEditor(typeof(ScriptableObject), true), CanEditMultipleObjects]
#endif
    class ScriptableObjectEditor : AbstractEditor { }





    #endregion

    #region static inspector


    class StaticInspector
    {

        static void HeaderGUI(Editor editor)
        {
            if (editor.GetType().Name != "MonoScriptImporterInspector") return;
            if ((editor.target as MonoImporter)?.GetScript() is not MonoScript script) return;
            if (script.GetClass() is not Type classType) return;



            List<FieldInfo> fields;
            List<Button> buttons;

            void findFields()
            {
                if (fields_byClassType.TryGetValue(classType, out fields)) return;


                fields = classType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                                  .Where(r => !r.IsLiteral && !r.IsInitOnly)
                                  .Where(r => Attribute.IsDefined(r, typeof(ShowInInspectorAttribute))).ToList();

                fields_byClassType[classType] = fields;

            }
            void createButtons()
            {
                if (buttons_byClassType.TryGetValue(classType, out buttons)) return;


                buttons = new List<Button>();

                void createButton(MemberInfo member, ButtonAttribute buttonAttribute)
                {
                    var button = new Button();

                    button.size = buttonAttribute.size;
                    button.space = buttonAttribute.space;
                    button.color = buttonAttribute.color;


                    if (member.GetCustomAttributeCached<TabAttribute>() is TabAttribute tabAttribute)
                        button.tabAttribute = tabAttribute;

                    if (member.GetCustomAttributeCached<FoldoutAttribute>() is FoldoutAttribute foldoutAttribute)
                        button.foldoutAttribute = foldoutAttribute;

                    if (member.GetCustomAttributeCached<IfAttribute>() is IfAttribute ifAttribute)
                        button.ifAttribute = ifAttribute;


                    if (member is MethodInfo method)
                        if (!method.GetParameters().Any())
                        {
                            var methodTarget = method.IsStatic ? null : new object();

                            button.action = (methodTarget) => method.Invoke(methodTarget, null);
                            button.name = buttonAttribute.name != "" ? buttonAttribute.name : method.Name.FormatVariableName(false);
                            button.isPressed = () => false;

                        }
                        else
                        {
                            var methodTarget = method.IsStatic ? null : new object();

                            button.action = (methodTarget) => method.Invoke(methodTarget, Enumerable.Range(0, button.parameterInfos.Count).Select(i => button.GetParameterValue(i)).ToArray());
                            button.name = buttonAttribute.name != "" ? buttonAttribute.name : method.Name.FormatVariableName(false);
                            button.isPressed = () => false;

                            button.parameterInfos = method.GetParameters().ToList();

                        }



                    if (button.action != null)
                        buttons.Add(button);

                }


                var staticMethods = classType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                                             .Where(r => Attribute.IsDefined(r, typeof(ButtonAttribute)));

                foreach (var method in staticMethods)
                    createButton(method, method.GetCustomAttributeCached<ButtonAttribute>());


                buttons_byClassType[classType] = buttons;


            }

            findFields();
            createButtons();

            if (!fields.Any() && !buttons.Any()) return;




            void drawField(FieldInfo fieldInfo)
            {
                var type = fieldInfo.FieldType;
                var name = fieldInfo.Name.FormatVariableName();

                var curValue = fieldInfo.GetValue(fieldInfo.IsStatic ? null : new object());

                var newValue = curValue;

                if (type == typeof(int)) newValue = EditorGUILayout.IntField(name, (int)curValue);
                else if (type == typeof(float)) newValue = EditorGUILayout.FloatField(name, (float)curValue);
                else if (type == typeof(double)) newValue = EditorGUILayout.DoubleField(name, (float)curValue);
                else if (type == typeof(string)) newValue = EditorGUILayout.TextField(name, (string)curValue);
                else if (type == typeof(bool)) newValue = EditorGUILayout.Toggle(name, (bool)curValue);
                else if (type == typeof(Vector2)) newValue = EditorGUILayout.Vector2Field(name, (Vector2)curValue);
                else if (type == typeof(Vector3)) newValue = EditorGUILayout.Vector3Field(name, (Vector3)curValue);
                else if (type == typeof(Vector4)) newValue = EditorGUILayout.Vector4Field(name, (Vector4)curValue);
                else if (type == typeof(Color)) newValue = EditorGUILayout.ColorField(name, (Color)curValue);
                else if (type == typeof(Rect)) newValue = EditorGUILayout.RectField(name, (Rect)curValue);
                else if (type == typeof(RectInt)) newValue = EditorGUILayout.RectIntField(name, (RectInt)curValue);
                else if (type == typeof(Bounds)) newValue = EditorGUILayout.BoundsField(name, (Bounds)curValue);
                else if (type == typeof(BoundsInt)) newValue = EditorGUILayout.BoundsIntField(name, (BoundsInt)curValue);
                else if (type == typeof(Vector2Int)) newValue = EditorGUILayout.Vector2IntField(name, (Vector2Int)curValue);
                else if (type == typeof(Vector3Int)) newValue = EditorGUILayout.Vector3IntField(name, (Vector3Int)curValue);
                else if (type.IsEnum) newValue = EditorGUILayout.EnumPopup(name, (System.Enum)curValue);
                else if (typeof(Object).IsAssignableFrom(type)) newValue = EditorGUILayout.ObjectField(name, (Object)curValue, type, true);
                else EditorGUILayout.TextField(name, curValue?.ToString());


                fieldInfo.SetValue(fieldInfo.IsStatic ? null : new object(), newValue);

            }
            void drawButton(Button button, ref bool noButtonsShown)
            {
                Rect buttonRect;
                Color color = Color.white;

                void set_buttonRect()
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Space(EditorGUI.indentLevel * 15);

                    buttonRect = ExpandWidthLabelRect(height: button.size);

                    GUILayout.EndHorizontal();

                }
                void set_color()
                {
                    if (button.color.ToLower() == "grey" || button.color.ToLower() == "gray") return;


                    var hue = 0f;
                    var saturation = .6f;
                    var lightness = isDarkTheme ? .57f : .64f;

                    if (button.color.ToLower() == "red") hue = 0;
                    else if (button.color.ToLower() == "orange") hue = .08f;
                    else if (button.color.ToLower() == "yellow") hue = .13f;
                    else if (button.color.ToLower() == "green") { hue = .32f; saturation = .49f; lightness = isDarkTheme ? .56f : .6f; }
                    else if (button.color.ToLower() == "blue") hue = .55f;
                    else if (button.color.ToLower() == "pink") hue = .94f;
                    else return;



                    color = ColorUtils.HSLToRGB(hue, saturation, lightness);

                    color *= 2f;
                    color.a = 1;

                }

                void argumentsBackground()
                {
                    if (!button.parameterInfos.Any()) return;
                    if (!button.isExpanded) return;
                    if (!curEvent.isRepaint) return;


                    var backgroundColor = Greyscale(isDarkTheme ? .27f : .83f);
                    var outlineColor = Greyscale(isDarkTheme ? .15f : .65f);
                    var cornerRadius = 3;

                    var backgroundRect = buttonRect.AddHeight(button.parameterInfos.Count * (EditorGUIUtility.singleLineHeight + 2) + 8);

                    backgroundRect.DrawRounded(outlineColor, cornerRadius);
                    backgroundRect.Resize(1).DrawRounded(backgroundColor, cornerRadius - 1);

                }
                void buttonItself()
                {
                    var prevBackgroundColor = GUI.backgroundColor;
                    GUI.backgroundColor = button.isPressed() ? GUIColors.pressedButtonBackground : color;

                    var clicked = GUI.Button(buttonRect, button.name);

                    GUI.backgroundColor = prevBackgroundColor;



                    if (!clicked) return;

                    button.action(null);

                }
                void expandButton()
                {
                    if (!button.parameterInfos.Any()) return;

                    var expandButtonRect = buttonRect.SetWidth(24).MoveX(1);

                    var colorNormal = Greyscale(isDarkTheme ? (buttonRect.IsHovered() ? .85f : .8f) : .7f);
                    var colorHovered = Greyscale(isDarkTheme ? 10f : 0f, 10f);
                    var colorPressed = Greyscale(.85f);

                    var iconSize = 12;


                    if (!IconButton(expandButtonRect, button.isExpanded ? "d_IN_foldout_act_on" : "d_IN_foldout_act", iconSize, colorNormal, colorHovered, colorPressed)) return;

                    button.isExpanded = !button.isExpanded;

                    // GUI.DrawTexture(buttonRect.SetWidth(24).SetHeightFromMid(24).Resize(6).MoveX(2), EditorIcons.GetIcon("d_IN_foldout_on"));
                    // GUI.DrawTexture(buttonRect.SetWidth(24).SetHeightFromMid(24).Resize(6).MoveX(2), EditorIcons.GetIcon("d_IN_foldout_on"));

                }
                void parameters()
                {
                    if (!button.isExpanded) return;
                    if (!button.parameterInfos.Any()) return;

                    void parameter(int i)
                    {
                        var type = button.parameterInfos[i].ParameterType;
                        var name = button.parameterInfos[i].Name.FormatVariableName();

                        var curValue = button.GetParameterValue(i);

                        var newValue = curValue;

                        if (type == typeof(int)) newValue = EditorGUILayout.IntField(name, (int)curValue);
                        else if (type == typeof(float)) newValue = EditorGUILayout.FloatField(name, (float)curValue);
                        else if (type == typeof(double)) newValue = EditorGUILayout.DoubleField(name, (float)curValue);
                        else if (type == typeof(string)) newValue = EditorGUILayout.TextField(name, (string)curValue);
                        else if (type == typeof(bool)) newValue = EditorGUILayout.Toggle(name, (bool)curValue);
                        else if (type == typeof(Vector2)) newValue = EditorGUILayout.Vector2Field(name, (Vector2)curValue);
                        else if (type == typeof(Vector3)) newValue = EditorGUILayout.Vector3Field(name, (Vector3)curValue);
                        else if (type == typeof(Vector4)) newValue = EditorGUILayout.Vector4Field(name, (Vector4)curValue);
                        else if (type == typeof(Color)) newValue = EditorGUILayout.ColorField(name, (Color)curValue);
                        else if (type == typeof(Rect)) newValue = EditorGUILayout.RectField(name, (Rect)curValue);
                        else if (type == typeof(RectInt)) newValue = EditorGUILayout.RectIntField(name, (RectInt)curValue);
                        else if (type == typeof(Bounds)) newValue = EditorGUILayout.BoundsField(name, (Bounds)curValue);
                        else if (type == typeof(BoundsInt)) newValue = EditorGUILayout.BoundsIntField(name, (BoundsInt)curValue);
                        else if (type == typeof(Vector2Int)) newValue = EditorGUILayout.Vector2IntField(name, (Vector2Int)curValue);
                        else if (type == typeof(Vector3Int)) newValue = EditorGUILayout.Vector3IntField(name, (Vector3Int)curValue);
                        else if (type.IsEnum) newValue = EditorGUILayout.EnumPopup(name, (System.Enum)curValue);
                        else if (typeof(Object).IsAssignableFrom(type)) newValue = EditorGUILayout.ObjectField(name, (Object)curValue, type, true);
                        else EditorGUILayout.PrefixLabel(name);


                        button.SetParameterValue(i, newValue);



                        // if (button.parameterInfos[i].ParameterType == typeof(int))
                        //     button.parameterValues[i] = Field<int>(button.parameterInfos[i].Name, (int)button.parameterValues[i]);

                        // else if (button.parameterInfos[i].ParameterType == typeof(float))
                        //     button.parameterValues[i] = Field<float>(button.parameterInfos[i].Name, (float)button.parameterValues[i]);

                        // else if (button.parameterInfos[i].ParameterType == typeof(string))
                        //     button.parameterValues[i] = Field<string>(button.parameterInfos[i].Name, (string)(button.parameterValues[i] ?? ""));

                        // else if (button.parameterInfos[i].ParameterType == typeof(bool))
                        //     button.parameterValues[i] = Field<bool>(button.parameterInfos[i].Name, (bool)button.parameterValues[i]);

                        // else if (typeof(Object).IsAssignableFrom(button.parameterInfos[i].ParameterType))
                        //     button.parameterValues[i] = Field<Object>(button.parameterInfos[i].Name, (Object)button.parameterValues[i]);

                        // else
                        //     GUILayout.Label("asd");

                    }



                    BeginIndent(7);
                    Space(1);

                    for (int i = 0; i < button.parameterInfos.Count; i++)
                        parameter(i);

                    Space(11);
                    EndIndent(5);


                }


                GUILayout.Space(button.space - 2);

                set_buttonRect();
                set_color();
                argumentsBackground();

                if (!curEvent.isRepaint)
                    expandButton();

                buttonItself();

                if (curEvent.isRepaint)
                    expandButton();

                parameters();

                if (button.isExpanded)
                    Space(6);


                noButtonsShown = false;

            }

            void background()
            {
                var lineColor = Greyscale(.1f);
                var backgroundColor = GUIColors.windowBackground;


                Space(10);

                var lineRect = ExpandWidthLabelRect(height: -1).AddWidthFromMid(123).SetHeight(1);

                lineRect.Draw(lineColor);

                Space(-10);



                var bgRect = lineRect.MoveY(1).SetHeight(123123);

                bgRect.Draw(GUIColors.windowBackground);

            }
            void drawFields()
            {
                foreach (var field in fields_byClassType[classType])
                    drawField(field);

            }
            void drawButtons()
            {
                var noButtonsToShow = true;

                foreach (var button in buttons_byClassType[classType])
                    drawButton(button, ref noButtonsToShow);

                if (noButtonsToShow)
                    Space(-16);

            }


            background();

            Space(fields.Any() ? 20 : 14);
            EditorGUI.indentLevel = 1;

            drawFields();

            Space(16);
            drawButtons();

            EditorGUI.indentLevel = 0;
            Space(buttons.Any() ? -18 : -20);

        }

        static Dictionary<Type, List<Button>> buttons_byClassType = new();
        static Dictionary<Type, List<FieldInfo>> fields_byClassType = new();



#if !VINSPECTOR_ATTRIBUTES_DISABLED
        [InitializeOnLoadMethod]
#endif
        static void Subscribe() => Editor.finishedDefaultHeaderGUI += HeaderGUI;

    }




    #endregion

}
#endif
