#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.IMGUI.Controls;
using Type = System.Type;
using static VFolders.Libs.VUtils;
using static VFolders.Libs.VGUI;
// using static VTools.VDebug;
using static VFolders.VFoldersData;
using static VFolders.VFoldersCache;

#if UNITY_6000_3_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<UnityEngine.EntityId>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<UnityEngine.EntityId>;
#elif UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif




namespace VFolders
{
    public static class VFolders
    {

        static void WrappedGUI(EditorWindow window)
        {
            var navbarHeight = 26;

            var isOneColumn = window.GetMemberValue<int>("m_ViewMode") == 0;

            var prevSelection = Selection.objects;


            void navbarGui()
            {
                if (!navbars_byWindow.ContainsKey(window))
                    navbars_byWindow[window] = new VFoldersNavbar(window);

                var navbarRect = window.position.SetPos(0, 0).SetHeight(navbarHeight);

                navbars_byWindow[window].OnGUI(navbarRect);


            }
            void hideDefaultTopBar()
            {
                if (curEvent.isLayout || typeof(GUILayoutUtility).GetMemberValue("current")?.GetMemberValue("topLevel")?.GetMemberValue<IList>("entries").Count != 1) // prevents exception on focus by vTabs shift-scroll  // doesnt always work. tbh not sure why it works at all
                    Space(-123);

            }
            void defaultGuiWithOffset()
            {
                var defaultTopBarHeight = 20;
                var topOffset = navbarHeight - defaultTopBarHeight;

                var m_Pos_original = window.GetFieldValue<Rect>("m_Pos");




                GUI.BeginGroup(m_Pos_original.SetPos(0, 0).AddHeightFromBottom(-topOffset));

                window.SetFieldValue("m_Pos", m_Pos_original.AddHeightFromBottom(-topOffset));


                try { window.InvokeMethod("OnGUI"); }
                catch (System.Exception exception)
                {
                    if (exception.InnerException is ExitGUIException)
                        throw exception.InnerException;
                    else
                        throw exception;

                    // GUIUtility.ExitGUI() works by throwing ExitGUIException, which just exits imgui loop and doesn't appear in console
                    // but if ExitGUI is called from a reflected method (OnGUI in this case), the exception becomes TargetInvokationException
                    // which gets logged to console (only if debugger is attached, for some reason)
                    // so here in such cases we rethrow the original ExitGUIException

                }


                window.SetFieldValue("m_Pos", m_Pos_original);

                GUI.EndGroup();

            }
            void treeViewShadow()
            {
                if (!curEvent.isRepaint) return;

                var shadowLength = 30;
                var shadowPos = 21;
                var shadowGreyscale = isDarkTheme ? .1f : .28f;
                var shadowAlpha = isDarkTheme ? .35f : .15f;

                var minScrollPos = 10;
                var maxScrollPos = 20;


                var scrollPos = window.GetMemberValue(isOneColumn ? "m_AssetTree" : "m_FolderTree").GetMemberValue<TreeViewState>("state").scrollPos.y;

                var opacity = ((scrollPos - minScrollPos) / (maxScrollPos - minScrollPos)).Clamp01();


                var rectWidth = isOneColumn ? window.position.width : window.GetMemberValue<Rect>("m_TreeViewRect").width;// - 12;

                var rect = window.position.SetPos(0, 0).MoveY(shadowPos).SetHeight(shadowLength).SetWidth(rectWidth);



                var clipAtY = navbarHeight + 1;

                GUI.BeginClip(window.position.SetPos(0, clipAtY));

                rect.MoveY(-clipAtY).DrawCurtainDown(Greyscale(shadowGreyscale, shadowAlpha * opacity));

                GUI.EndClip();




                if (isOneColumn) return;

                var dividerRect = window.GetMemberValue<Rect>("m_TreeViewRect").SetWidthFromRight(1).MoveX(1);

                var dividerColor = Greyscale(.16f);

                dividerRect.Draw(dividerColor);


            }
            void listAreaShadow()
            {
                if (isOneColumn) return;
                if (!curEvent.isRepaint) return;


                var shadowLength = 30;
                var shadowPos = navbarHeight + 17;
                var shadowGreyscale = isDarkTheme ? .1f : .28f;
                var shadowAlpha = .35f;

                var minScrollPos = 10;
                var maxScrollPos = 20;


                var scrollPos = window.GetMemberValue("m_ListAreaState").GetMemberValue<Vector2>("m_ScrollPosition").y;

                var opacity = ((scrollPos - minScrollPos) / (maxScrollPos - minScrollPos)).Clamp01();


                var rectX = window.GetMemberValue<Rect>("m_TreeViewRect").width + 1;
                var rectWidth = window.position.width - rectX;// - 12;

                var rect = window.position.SetPos(rectX, 0).MoveY(shadowPos).SetHeight(shadowLength).SetWidth(rectWidth);



                var clipAtY = navbarHeight + 22;

                GUI.BeginClip(window.position.SetPos(0, clipAtY));

                rect.MoveY(-clipAtY).DrawCurtainDown(Greyscale(shadowGreyscale, shadowAlpha * opacity));

                GUI.EndClip();



                if (isDarkTheme)
                    window.position.SetPos(rectX, clipAtY - 1).SetSize(12321, 1).Draw(Greyscale(.175f)); // line under breadcrumbs

            }
            // void preventSelectionChangeInOtherBrowsers()
            // {
            //     if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) return; // fixes https://discord.com/channels/1120291189707509840/1121124228507381791/1418551070933913611
            //     if (Selection.objects.SequenceEqual(prevSelection)) return;


            //     allBrowsers.ForEach(r => r?.SetMemberValue("m_InternalSelectionChange", true));

            //     EditorApplication.delayCall += () =>
            //         allBrowsers.ForEach(r => r?.SetMemberValue("m_InternalSelectionChange", false));

            // }






            var doNavbarFirst = GUI.GetNameOfFocusedControl() == "navbar search field" || curEvent.keyCode == KeyCode.Escape;

            if (doNavbarFirst)
                navbarGui();

            hideDefaultTopBar();
            defaultGuiWithOffset();
            treeViewShadow();
            listAreaShadow();

            if (!doNavbarFirst)
                navbarGui();

        }

        static Dictionary<EditorWindow, VFoldersNavbar> navbars_byWindow = new();



        static void UpdateGUIWrapping(EditorWindow window)
        {
            if (!window.hasFocus) return;

            var isLocked = window.GetMemberValue<bool>("isLocked");
            var isVTabsActive = t_VTabs != null && !EditorPrefsCached.GetBool("vTabs-pluginDisabled", false);

            var curOnGUIMethod = window.GetMemberValue("m_Parent").GetMemberValue<System.Delegate>("m_OnGUI").Method;

            var isWrapped = curOnGUIMethod == mi_WrappedGUI;
            var shouldBeWrapped = VFoldersMenu.navigationBarEnabled && !(isVTabsActive && isLocked) && curOnGUIMethod != mi_VFavorites_WrappedOnGUI;

            void wrap()
            {
                var hostView = window.GetMemberValue("m_Parent");

                var newDelegate = typeof(VFolders).GetMethod(nameof(WrappedGUI), maxBindingFlags).CreateDelegate(t_EditorWindowDelegate, window);

                hostView.SetMemberValue("m_OnGUI", newDelegate);

                window.Repaint();

            }
            void unwrap()
            {
                var hostView = window.GetMemberValue("m_Parent");

                var originalDelegate = hostView.InvokeMethod("CreateDelegate", "OnGUI");

                hostView.SetMemberValue("m_OnGUI", originalDelegate);

                window.Repaint();

            }


            if (shouldBeWrapped && !isWrapped)
                wrap();

            if (!shouldBeWrapped && isWrapped)
                unwrap();

        }
        static void UpdateGUIWrappingForAllBrowsers() => allBrowsers.ForEach(r => UpdateGUIWrapping(r));

        static void OnDomainReloaded() => toCallInGUI += UpdateGUIWrappingForAllBrowsers;

        static void OnWindowUnmaximized() => UpdateGUIWrappingForAllBrowsers();

        static void OnBrowserFocused() => UpdateGUIWrapping(EditorWindow.focusedWindow);

        static void OnDelayCall() => UpdateGUIWrappingForAllBrowsers();





        static void CheckIfFocusedWindowChanged()
        {
            if (prevFocusedWindow != EditorWindow.focusedWindow)
                if (EditorWindow.focusedWindow?.GetType() == t_ProjectBrowser)
                    OnBrowserFocused();

            prevFocusedWindow = EditorWindow.focusedWindow;

        }

        static EditorWindow prevFocusedWindow;



        static void CheckIfWindowWasUnmaximized()
        {
            var isMaximized = EditorWindow.focusedWindow?.maximized == true;

            if (!isMaximized && wasMaximized)
                OnWindowUnmaximized();

            wasMaximized = isMaximized;

        }

        static bool wasMaximized;



        static void OnSomeGUI()
        {
            toCallInGUI?.Invoke();
            toCallInGUI = null;

            CheckIfFocusedWindowChanged();

        }

        static void ProjectWindowItemOnGUI(string _, Rect __) => OnSomeGUI();
        static void HierarchyWindowItemOnGUI(int _, Rect __) => OnSomeGUI();

        static System.Action toCallInGUI;



        static void DelayCallLoop()
        {
            OnDelayCall();

            EditorApplication.delayCall -= DelayCallLoop;
            EditorApplication.delayCall += DelayCallLoop;

        }














        static void ItemGUI(Rect itemRect, string guid, int instanceId)
        {
            EditorWindow window;

            void findWindow()
            {
                if (allBrowsers.Count() == 1) { window = allBrowsers.First(); return; }


                var pointInsideWindow = EditorGUIUtility.GUIToScreenPoint(itemRect.center);

                window = allBrowsers.FirstOrDefault(r => r.position.AddHeight(30).Contains(pointInsideWindow) && r.hasFocus);

            }
            void updateWindow()
            {
                if (!window) return; // happens on half-visible rows during expand animation

                if (curEvent.isLayout && !lastEventWasLayout)
                    UpdateWindow_Layout(window);

                if (curEvent.isRepaint && !lastEventWasRepaint)
                    UpdateWindow_Repaint(window);


                lastEventWasLayout = curEvent.isLayout;
                lastEventWasRepaint = curEvent.isRepaint;

            }
            void catchScrollInputForController()
            {
                if (!window) return;
                if (!controllers_byWindow.ContainsKey(window)) return;

                if (curEvent.isScroll)
                    controllers_byWindow[window].animatingScroll = false;

            }
            void callGUI()
            {
                if (!window) return;
                if (!guis_byWindow.ContainsKey(window)) return;


                var gui = guis_byWindow[window];

                if (itemRect.height == 16)
                    gui.RowGUI(itemRect, guid, instanceId);
                else
                    gui.CellGUI(itemRect, guid, instanceId);

            }

            findWindow();
            updateWindow();
            catchScrollInputForController();
            callGUI();

        }

        static void ItemGUI_2021_3_and_older(string guid, Rect itemRect)
        {
            var instanceId = typeof(AssetDatabase).InvokeMethod<int>("GetMainAssetOrInProgressProxyInstanceID", guid.ToPath());

            ItemGUI(itemRect, guid, instanceId);

        }
        static void ItemGUI_2022_1_and_newer(int instanceId, Rect itemRect)
        {
            var guid = _AssetDatabase_GetAssetPath(instanceId).ToGuid();

            ItemGUI(itemRect, guid, instanceId);

        }

        static bool lastEventWasLayout;
        static bool lastEventWasRepaint;



        static void UpdateWindow_Layout(EditorWindow window)
        {
            if (!guis_byWindow.TryGetValue(window, out var gui))
                gui = guis_byWindow[window] = new(window);

            if (!controllers_byWindow.TryGetValue(window, out var controller))
                controller = controllers_byWindow[window] = new(window);

            if (!histories_byWindow.TryGetValue(window, out var history))
                history = histories_byWindow[window] = new(window);


            gui.UpdateState_Layout();
            gui.UpdateFoldersFirst();

            controller.UpdateState();
            controller.UpdateExpandQueue();
            controller.UpdateScrollAnimation();
            controller.UpdateHighlightAnimation();

            history.UpdateState();
            history.CheckTreeStateChange();
            history.CheckFolderPathChange();

        }
        static void UpdateWindow_Repaint(EditorWindow window)
        {
            if (guis_byWindow.ContainsKey(window))
                guis_byWindow[window].UpdateState_Repaint();
        }

        public static Dictionary<EditorWindow, VFoldersGUI> guis_byWindow = new();
        public static Dictionary<EditorWindow, VFoldersController> controllers_byWindow = new();
        public static Dictionary<EditorWindow, VFoldersHistory> histories_byWindow => VFoldersHistorySingleton.instance.histories_byWindow;








        public static Texture2D GetSmallFolderIcon(FolderInfo folderInfo, bool removeColor = false)
        {
            var hasColor = folderInfo.hasColor && !removeColor;
            var hasIcon = folderInfo.hasIcon;

            var color = hasColor ? folderInfo.color : default;
            var iconNameOrPath = hasIcon ? folderInfo.iconNameOrPath : "";

            var isEmpty = folderInfo.folderState.isEmpty;

            var key = new object[] { iconNameOrPath, color, isEmpty, isDarkTheme }.Aggregate(0, (hash, r) => (hash * 2) ^ r.GetHashCode());


            Texture2D icon = null;

            void getCached()
            {
                if (!cache.HasIcon(key)) return;

                icon = cache.GetIcon(key);

            }
            void generateAndCache()
            {
                if (icon != null) return;
                if (Event.current != null) return;  // interactions with gpu in OnGUI may interfere with gui rendering

                var iconSizeX = hasIcon ? 36 : 32;
                var iconSizeY = 32;

                var assetIconSize = 20; // 20 21
                var assetIconOffsetX = 16;
                var assetIconOffsetY = -2; // -2 -3

                var folderIconSize = iconSizeY;
                var folderIconOffsetY_ifHasAssetIcon = 1;

                Color[] iconPixels;

                Texture2D folderIcon;
                Color[] folderIconPixels;


                void createIcon()
                {
                    icon = new Texture2D(iconSizeX, iconSizeY, TextureFormat.RGBA32, 1, false);
                    icon.hideFlags = HideFlags.DontSave;
                    icon.SetPropertyValue("pixelsPerPoint", 2);

                    iconPixels = new Color[iconSizeX * iconSizeY];

                }
                void createFolderIcon()
                {
                    var folderIconName = hasColor ? (isEmpty ? "FolderEmpty On Icon" : "Folder On Icon") :
                                                    (isEmpty ? "FolderEmpty Icon" : "Folder Icon");


                    folderIcon = EditorGUIUtility.FindTexture(folderIconName);

                    if (folderIcon.width != folderIconSize)
                        folderIcon = folderIcon.CreateResizedCopy(folderIconSize, folderIconSize);
                    else
                        folderIcon = folderIcon.CreateCopy();

                    folderIconPixels = folderIcon.GetPixels(0);

                }
                void copyFolderIcon()
                {
                    if (!hasIcon) { iconPixels = folderIconPixels; return; }

                    for (int x = 0; x < folderIcon.width; x++)
                        for (int y = 0; y < folderIcon.height - folderIconOffsetY_ifHasAssetIcon; y++)
                            iconPixels[x + (y + folderIconOffsetY_ifHasAssetIcon) * icon.width] = folderIconPixels[x + y * folderIcon.width];

                }
                void applyColor()
                {
                    if (!hasColor) return;

                    for (int i = 0; i < iconPixels.Length; i++)
                        iconPixels[i] *= (color * 1.06f).SetAlpha(1);

                }
                void insertAssetIcon()
                {
                    if (!hasIcon) return;


                    var assetIconOriginal = EditorIcons.GetIcon(iconNameOrPath);

                    if (!assetIconOriginal) return;


                    var prevFilter = assetIconOriginal.filterMode;

                    assetIconOriginal.filterMode = FilterMode.Bilinear;
                    var assetIconPixels_bilinear = assetIconOriginal.CreateResizedCopy(assetIconSize, assetIconSize).GetPixels();

                    assetIconOriginal.filterMode = FilterMode.Point;
                    var assetIconPixels_point = assetIconOriginal.CreateResizedCopy(assetIconSize, assetIconSize).GetPixels();


                    assetIconOriginal.filterMode = prevFilter;


                    for (int x = 0; x < iconSizeX; x++)
                        for (int y = 0; y < iconSizeY; y++)
                        {
                            var xAssetIcon = x - assetIconOffsetX;
                            var yAssetIcon = y - assetIconOffsetY - folderIconOffsetY_ifHasAssetIcon;

                            if (!xAssetIcon.IsInRange(0, assetIconSize - 1)) continue;
                            if (!yAssetIcon.IsInRange(0, assetIconSize - 1)) continue;


                            var innerRadius = (iconNameOrPath == "AudioClip Icon" ? .2f : .4f);
                            var isInnerPixel = (new Vector2(xAssetIcon, yAssetIcon) / (assetIconSize - 1) - Vector2.one * .5f).magnitude < innerRadius;

                            var isOutlinePixel = false;
                            var outlineRadius = isInnerPixel ? 2 : 1;
                            for (int xx = xAssetIcon - outlineRadius; xx <= xAssetIcon + outlineRadius; xx++)
                                if (!isOutlinePixel)
                                    for (int yy = yAssetIcon - outlineRadius; yy <= yAssetIcon + outlineRadius; yy++)
                                        if (!isOutlinePixel)
                                            if (xx.IsInRange(0, assetIconSize - 1) && yy.IsInRange(0, assetIconSize - 1))
                                                if (assetIconPixels_bilinear[xx + yy * assetIconSize].a > .2f)
                                                    isOutlinePixel = true;


                            var pxBilinear = assetIconPixels_bilinear[xAssetIcon + yAssetIcon * assetIconSize];
                            var pxPoint = assetIconPixels_point[xAssetIcon + yAssetIcon * assetIconSize];
                            var pxCombined = new Color(pxPoint.r, pxPoint.g, pxPoint.b, pxBilinear.a);

                            if (pxCombined.a == 0 && !isOutlinePixel) continue;

                            iconPixels[x + y * iconSizeX] = pxCombined;

                        }

                }


                createIcon();
                createFolderIcon();
                copyFolderIcon();
                applyColor();
                insertAssetIcon();

                icon.SetPixels(iconPixels);
                icon.Apply();

                cache.AddIcon(key, icon);

            }
            void queueGeneration()
            {
                if (icon != null) return;

                toGenerateInUpdate.Add(generateAndCache);

            }


            getCached();
            generateAndCache();
            queueGeneration();

            return icon ?? EditorGUIUtility.FindTexture(isEmpty ? "FolderEmpty Icon" : "Project@2x");

        }

        static void GenerateIconsInUpdate()
        {
            foreach (var r in toGenerateInUpdate)
                r.Invoke();

            toGenerateInUpdate.Clear();

        }

        static List<System.Action> toGenerateInUpdate = new();



        public static void DrawBigFolderIcon(Rect rect, FolderInfo folderInfo)
        {
            Rect folderIconRect;
            Rect assetIconRect;


            void calcRects()
            {
                folderIconRect = rect.SetHeight(rect.width);

#if !UNITY_2022_3_OR_NEWER
                if (Application.platform == RuntimePlatform.OSXEditor)
                    if (folderIconRect.width > 64)
                        folderIconRect = folderIconRect.SetSizeFromMid(64, 64);
#endif


                var assetIconOffsetMin = new Vector2(4.5f, 3.5f);
                var assetIconSizeMin = 10;

                var assetIconOffsetMax = new Vector2(19, 15);
                var assetIconSizeMax = 24.5f; // 25

                var t = ((folderIconRect.width - 16) / (64 - 16));

#if !UNITY_2022_3_OR_NEWER
                if (Application.platform == RuntimePlatform.OSXEditor)
                    t = t.Clamp01();
#endif

                var assetIconOffset = MathUtil.Lerp(assetIconOffsetMin, assetIconOffsetMax, t);
                var assetIconSize = MathUtil.Lerp(assetIconSizeMin, assetIconSizeMax, t);

                assetIconRect = folderIconRect.Move(assetIconOffset).SetSizeFromMid(assetIconSize, assetIconSize).AlignToPixelGrid();

            }

            void color()
            {
                if (!folderInfo.hasColor) return;


                SetGUIColor(folderInfo.color.SetAlpha(1));

                GUI.DrawTexture(folderIconRect, EditorGUIUtility.FindTexture(folderInfo.folderState.isEmpty ? "FolderEmpty On Icon" : "Folder On Icon"));

                ResetGUIColor();

            }
            void assetIcon()
            {
                if (!folderInfo.hasIcon) return;


                var texture = EditorIcons.GetIcon(folderInfo.iconNameOrPath);

                if (!texture) return;

                if (texture.width < texture.height) assetIconRect = assetIconRect.SetWidthFromMid(assetIconRect.height * texture.width / texture.height);
                if (texture.height < texture.width) assetIconRect = assetIconRect.SetHeightFromMid(assetIconRect.width * texture.height / texture.width);



                void material()
                {
                    if (!outlineMaterial)
                        outlineMaterial = new Material(Shader.Find("Hidden/Internal-GUITextureClipText"));

                    outlineMaterial.color = isDarkTheme ? Greyscale(.2f, .6f) : Greyscale(.75f);

                    // .color needs to be updated continiously because it gets reset on some editor events
                    // only happens when internal shader is used

                }

                void shadow()
                {
                    var contrast = isDarkTheme ? .6f : .2f; // was .65 then .6

                    assetIconRect.SetSizeFromMid(assetIconRect.width * .8f).DrawBlurred(Greyscale(.2f, contrast), assetIconRect.width * .55f);

                }
                void outline()
                {
                    var outlineRect = assetIconRect.Resize(rect.height >= 70 && EditorGUIUtility.pixelsPerPoint >= 2 ? -1f / EditorGUIUtility.pixelsPerPoint : 0).AlignToPixelGrid();

                    EditorGUI.DrawPreviewTexture(outlineRect.Move(-1, -1), texture, outlineMaterial);
                    EditorGUI.DrawPreviewTexture(outlineRect.Move(-1, 1), texture, outlineMaterial);
                    EditorGUI.DrawPreviewTexture(outlineRect.Move(1, 1), texture, outlineMaterial);
                    EditorGUI.DrawPreviewTexture(outlineRect.Move(1, -1), texture, outlineMaterial);

                }
                void background()
                {
                    for (int i = 0; i < assetIconRect.size.x; i++)
                        EditorGUI.DrawPreviewTexture(assetIconRect.Resize(i * .5f + 1), texture, outlineMaterial);

                }
                void icon()
                {
                    GUI.DrawTexture(assetIconRect, texture);
                }

                material();

                shadow();
                outline();
                background();
                icon();

            }


            calcRects();

            color();
            assetIcon();

        }

        static Material outlineMaterial;

















        public static Texture2D GetSmallFolderIcon_forVTabs(string folderGuid)
        {
            var folderInfo = GetFolderInfo(folderGuid);

            if (folderInfo.hasColor || folderInfo.hasIcon)
                return GetSmallFolderIcon(folderInfo);

            return null;

        }

        public static void DrawBigFolderIcon_forVFavorites(Rect rect, string folderGuid)
        {
            DrawBigFolderIcon(rect, GetFolderInfo(folderGuid));
        }

        public static void SetIcon(string folderPath, string iconName, bool recursive = false)
        {
            var folderData = GetFolderData(folderPath.ToGuid(), createDataIfDoesntExist: true);

            folderData.iconNameOrGuid = iconName ?? "";
            folderData.isIconRecursive = recursive;


            folderInfoCache.Clear();

            EditorApplication.RepaintProjectWindow();

        }
        public static void SetColor(string folderPath, int colorIndex, bool recursive = false)
        {
            var folderData = GetFolderData(folderPath.ToGuid(), createDataIfDoesntExist: true);

            folderData.colorIndex = colorIndex;
            folderData.isColorRecursive = recursive;


            folderInfoCache.Clear();

            EditorApplication.RepaintProjectWindow();

        }

















        static void Shortcuts() // globalEventHandler 
        {
            if (EditorWindow.mouseOverWindow is not EditorWindow hoveredWindow) return;
            if (hoveredWindow.GetType() != t_ProjectBrowser) return;

            void toggleExpanded()
            {
                if (!curEvent.isKeyDown) return;
                if (curEvent.keyCode != KeyCode.E) return;
                if (curEvent.holdingAnyModifierKey) return;
                if (!VFoldersMenu.toggleExpandedEnabled) return;

                if (lastHoveredTreeItem == null) return;
                if (!lastHoveredRowRect_screenSpace.Contains(lastKnownMousePosition_screenSpace)) return;

                curEvent.Use();

                if (lastHoveredTreeItem.children == null) return;
                if (lastHoveredTreeItem.children.Count == 0) return;


                controllers_byWindow[hoveredWindow].ToggleExpanded(lastHoveredTreeItem);

            }
            void collapseAll()
            {
                if (!curEvent.isKeyDown) return;
                if (curEvent.keyCode != KeyCode.E) return;
                if (curEvent.modifiers != (EventModifiers.Shift | EventModifiers.Command) && curEvent.modifiers != (EventModifiers.Shift | EventModifiers.Control)) return;
                if (!VFoldersMenu.collapseEverythingEnabled) return;

                curEvent.Use();


                controllers_byWindow[hoveredWindow].CollapseAll();

            }
            void isolate()
            {
                if (!curEvent.isKeyDown) return;
                if (curEvent.keyCode != KeyCode.E) return;
                if (curEvent.modifiers != EventModifiers.Shift) return;
                if (!VFoldersMenu.collapseEverythingElseEnabled) return;

                if (lastHoveredTreeItem == null) return;
                if (!lastHoveredRowRect_screenSpace.Contains(lastKnownMousePosition_screenSpace)) return;

                curEvent.Use();

                if (lastHoveredTreeItem.children == null) return;
                if (lastHoveredTreeItem.children.Count == 0) return;


                controllers_byWindow[hoveredWindow].Isolate(lastHoveredTreeItem);

            }
            void moveBack()
            {
                if (!curEvent.isMouseDown) return;
                if (curEvent.mouseButton != 3) return;


                var isOneColumn = hoveredWindow.GetMemberValue<int>("m_ViewMode") == 0;

                if (isOneColumn) return;



                if (!histories_byWindow.ContainsKey(hoveredWindow))
                    histories_byWindow[hoveredWindow] = new(hoveredWindow);

                var history = histories_byWindow[hoveredWindow];

                if (!history.prevFolderPaths.Any()) return;


                history.MoveBack_TwoColumns();

            }
            void moveForward()
            {
                if (!curEvent.isMouseDown) return;
                if (curEvent.mouseButton != 4) return;


                var isOneColumn = hoveredWindow.GetMemberValue<int>("m_ViewMode") == 0;

                if (isOneColumn) return;



                if (!histories_byWindow.ContainsKey(hoveredWindow))
                    histories_byWindow[hoveredWindow] = new(hoveredWindow);

                var history = histories_byWindow[hoveredWindow];

                if (!history.nextFolderPaths.Any()) return;


                history.MoveForward_TwoColumns();

            }


            toggleExpanded();
            collapseAll();
            isolate();
            moveBack();
            moveForward();

        }

        public static TreeViewItem lastHoveredTreeItem;

        public static Rect lastHoveredRowRect_screenSpace;

        public static Vector2 lastKnownMousePosition_screenSpace;

















        public static FolderInfo GetFolderInfo(string guid)
        {
            if (folderInfoCache.TryGetValue(guid, out var cachedFolderInfo)) return cachedFolderInfo;


            var folderInfo = new FolderInfo();

            var folderData = folderInfo.folderData = GetFolderData(guid, createDataIfDoesntExist: false);
            var folderState = folderInfo.folderState = GetFolderState(guid);


            var recursiveIconNameOrGuid = "";
            var recursiveColorIndex = 0;

            var ruledIconNameOrGuid = "";
            var ruledColorIndex = 0;

            void checkRules()
            {
                if (rules == null)
                    rules = TypeCache.GetMethodsWithAttribute<RuleAttribute>()
                                     .Where(r => r.IsStatic
                                              && r.GetParameters().Count() == 1
                                              && r.GetParameters().First().ParameterType == typeof(Folder)).ToList();

                if (!rules.Any()) return;



                var folder = new Folder(guid);

                foreach (var rule in rules)
                    rule.Invoke(null, new[] { folder });


                ruledIconNameOrGuid = folder.icon;
                ruledColorIndex = folder.color;


            }
            void checkRecursion(string path, int depth)
            {
                if (!path.HasParentPath()) return;

                var parentFolderData = GetFolderData(path.GetParentPath().ToGuid(), createDataIfDoesntExist: false);

                if (parentFolderData != null)
                {

                    if (parentFolderData.isIconRecursive && parentFolderData.iconNameOrGuid != "")
                        if (recursiveIconNameOrGuid == "")
                            recursiveIconNameOrGuid = parentFolderData.iconNameOrGuid;

                    if (parentFolderData.isColorRecursive && parentFolderData.colorIndex != 0)
                        if (recursiveColorIndex == 0)
                            recursiveColorIndex = parentFolderData.colorIndex;


                    if (parentFolderData.isColorRecursive && parentFolderData.colorIndex != 0)
                        folderInfo.maxColorRecursionDepth = depth + 1;

                }



                checkRecursion(path.GetParentPath(), depth + 1);

            }
            void setIcon()
            {
                var iconNameOrGuid = "";

                if (folderData != null && folderData.iconNameOrGuid != "")
                    iconNameOrGuid = folderData.iconNameOrGuid;

                else if (recursiveIconNameOrGuid != "")
                    iconNameOrGuid = recursiveIconNameOrGuid;

                else if (ruledIconNameOrGuid != "")
                    iconNameOrGuid = ruledIconNameOrGuid;

                else if (VFoldersMenu.autoIconsEnabled && folderState.autoIconName != "" && folderData?.isIconRecursive != true)
                    iconNameOrGuid = folderState.autoIconName;



                if (iconNameOrGuid == "" || iconNameOrGuid == "none") { folderInfo.hasIcon = false; return; }

                folderInfo.hasIcon = true;
                folderInfo.hasIconByRecursion = recursiveIconNameOrGuid != "";

                folderInfo.iconNameOrPath = iconNameOrGuid.Length == 32 ? iconNameOrGuid.ToPath()
                                                                        : iconNameOrGuid;

            }
            void setColor()
            {
                var colorIndex = 0;

                if (folderData != null && folderData.colorIndex > 0)
                    colorIndex = folderData.colorIndex;

                else if (recursiveColorIndex != 0)
                    colorIndex = recursiveColorIndex;

                else if (ruledColorIndex != 0)
                    colorIndex = ruledColorIndex;



                if (colorIndex == 0) { folderInfo.hasColor = false; return; }

                folderInfo.hasColor = true;
                folderInfo.hasColorByRecursion = recursiveColorIndex != 0;




                var brightness = palette?.colorBrightness ?? 1;
                var saturation = palette?.colorSaturation ?? 1;


                var rawColor = palette ? palette.colors[colorIndex - 1] : VFoldersPalette.GetDefaultColor(colorIndex - 1);

                var brightenedColor = MathUtil.Lerp(Greyscale(.2f), rawColor, brightness);

                Color.RGBToHSV(brightenedColor, out float h, out float s, out float v);
                var saturatedColor = Color.HSVToRGB(h, s * saturation, v);


                folderInfo.color = saturatedColor;

            }

            checkRules();
            checkRecursion(guid.ToPath(), 0);
            setIcon();
            setColor();


            return folderInfoCache[guid] = folderInfo;

        }

        public class FolderInfo
        {
            public string iconNameOrPath = "";
            public bool hasIcon;
            public bool hasIconByRecursion;

            public Color color;
            public bool hasColor;
            public bool hasColorByRecursion;
            public int maxColorRecursionDepth;


            public FolderData folderData;
            public FolderState folderState;

        }

        public static Dictionary<string, FolderInfo> folderInfoCache = new();

        public static List<MethodInfo> rules = null;



        public static FolderData GetFolderData(string guid, bool createDataIfDoesntExist)
        {
            if (!data) return null;

            FolderData folderData = null;

            void fromScripableObject()
            {
                if (VFoldersData.storeDataInMetaFiles) return;

                data.folderDatas_byGuid.TryGetValue(guid, out folderData);


                if (folderData != null || !createDataIfDoesntExist) return;

                folderData = new FolderData();

                data.folderDatas_byGuid[guid] = folderData;

            }
            void fromMetaFile()
            {
                if (!VFoldersData.storeDataInMetaFiles) return;

                folderDatasFromMetaFiles_byGuid.TryGetValue(guid, out folderData);



                if (folderData != null) return;

                var importer = AssetImporter.GetAtPath(guid.ToPath());

                try { folderData = JsonUtility.FromJson<FolderData>(importer.userData); } catch { }

                folderDatasFromMetaFiles_byGuid[guid] = folderData;



                if (folderData != null || !createDataIfDoesntExist) return;

                folderData = new FolderData();

                folderDatasFromMetaFiles_byGuid[guid] = folderData;

            }

            fromScripableObject();
            fromMetaFile();

            return folderData;

        }

        public static Dictionary<string, FolderData> folderDatasFromMetaFiles_byGuid = new();



        public static FolderState GetFolderState(string guid)
        {
            FolderState folderState = null;

            void getCached()
            {
                cache.folderStates_byGuid.TryGetValue(guid, out folderState);
            }
            void create()
            {
                if (folderState != null) return;

                folderState = new FolderState();

                folderState.needsUpdate = true;

                cache.folderStates_byGuid[guid] = folderState;

            }
            void update()
            {
                if (!folderState.needsUpdate) return;
                if (!Directory.Exists(guid.ToPath())) { folderState.needsUpdate = false; return; }


                var typesInFolder = Directory.GetFiles(guid.ToPath(), "*.*").Select(r => AssetDatabase.GetMainAssetTypeAtPath(r)).Where(r => r != null);

                void isEmpty()
                {
                    folderState.isEmpty = !Directory.EnumerateFileSystemEntries(guid.ToPath()).Any();
                }
                void contentMinimap()
                {
                    var iconNames = new List<string>();

                    void fill()
                    {
                        foreach (var type in typesInFolder)

                            if (type == typeof(Texture2D))
                                iconNames.Add("Texture Icon");

                            else if (type == typeof(GameObject))
                                iconNames.Add("Prefab Icon");

                            else if (type.BaseType == typeof(ScriptableObject) || type.BaseType?.BaseType == typeof(ScriptableObject))
                                iconNames.Add("ScriptableObject Icon");

                            else if (type == typeof(MonoScript))
                                iconNames.Add("cs Script Icon");

                            else if (AssetPreview.GetMiniTypeThumbnail(type) is Texture2D icon)
                                if (AssetDatabase.Contains(icon) && !icon.GetPath().StartsWith("Library"))
                                    iconNames.Add(icon.GetPath());
                                else
                                    iconNames.Add(icon.name);

                    }
                    void filter()
                    {
                        iconNames = iconNames.Distinct().ToList();


                        for (int i = 0; i < iconNames.Count; i++)
                            if (iconNames[i].StartsWith("d_"))
                                iconNames[i] = iconNames[i].Substring(2);



                        iconNames.Remove("DefaultAsset Icon");
                        iconNames.Remove("TextAsset Icon");



                        if (iconNames.Contains("cs Script Icon"))
                            iconNames.Remove("AssemblyDefinitionAsset Icon");

                        if (iconNames.Contains("Shader Icon"))
                            iconNames.Remove("ShaderInclude Icon");

                    }
                    void order()
                    {
                        var order = new List<string>
                        {

                            "SceneAsset Icon",


                            "Prefab Icon",
                            "Mesh Icon",
                            "Material Icon",
                            "Texture Icon",


                            "cs Script Icon",
                            "Shader Icon",
                            "ComputeShader Icon",
                            "ShaderInclude Icon",


                            "ScriptableObject Icon",

                        };

                        iconNames = iconNames.OrderBy(r => order.IndexOf(r) is int i && i != -1 ? i : 1232)
                                              .ThenBy(r => r)
                                              .ToList();
                    }

                    fill();
                    filter();
                    order();

                    folderState.contentMinimapIconNames = iconNames;

                }
                void autoIcon()
                {
                    folderState.autoIconName = "";


                    if (!typesInFolder.Any()) return;
                    if (!typesInFolder.All(r => r == typesInFolder.First()) && !typesInFolder.All(r => typeof(ScriptableObject).IsAssignableFrom(r))) return;

                    var type = typesInFolder.First();


                    if (type == typeof(SceneAsset))
                        folderState.autoIconName = "SceneAsset Icon";

                    else if (type == typeof(GameObject))
                        folderState.autoIconName = "Prefab Icon";

                    else if (type == typeof(Material))
                        folderState.autoIconName = "Material Icon";

                    else if (type == typeof(Texture))
                        folderState.autoIconName = "Texture Icon";

                    else if (type == typeof(TerrainData))
                        folderState.autoIconName = "TerrainData Icon";

                    else if (type == typeof(AudioClip))
                        folderState.autoIconName = "AudioClip Icon";

                    else if (type == typeof(Shader))
                        folderState.autoIconName = "Shader Icon";

                    else if (type == typeof(ComputeShader))
                        folderState.autoIconName = "ComputeShader Icon";

                    else if (type == typeof(MonoScript) || type == typeof(UnityEditorInternal.AssemblyDefinitionAsset) || type == typeof(UnityEditorInternal.AssemblyDefinitionReferenceAsset))
                        folderState.autoIconName = "cs Script Icon";

                    else if (typeof(ScriptableObject).IsAssignableFrom(type))
                        folderState.autoIconName = "ScriptableObject Icon";

                }


                isEmpty();
                contentMinimap();
                autoIcon();

                folderState.needsUpdate = false;

            }

            getCached();
            create();
            update();

            return folderState;

        }

        class FolderStateChangeDetector : AssetPostprocessor
        {
#if UNITY_2021_1_OR_NEWER
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
#else
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#endif
            {
                foreach (var path in importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths))
                    if (path.HasParentPath())
                        if (cache.folderStates_byGuid.TryGetValue(path.GetParentPath().ToGuid(), out var folderState))
                            folderState.needsUpdate = true; // todo only clear folderinfo cache here?
            }
        }

        public static VFoldersCache cache => VFoldersCache.instance;



        public static void OnProjectChanged() => folderInfoCache.Clear();
        public static void OnDataSerialization() => folderInfoCache.Clear();
















        [InitializeOnLoadMethod]
        static void Init()
        {
            if (VFoldersMenu.pluginDisabled) return;

            void subscribe()
            {

                // gui

#if UNITY_2022_1_OR_NEWER
                EditorApplication.projectWindowItemInstanceOnGUI -= ItemGUI_2022_1_and_newer;
                EditorApplication.projectWindowItemInstanceOnGUI = ItemGUI_2022_1_and_newer + EditorApplication.projectWindowItemInstanceOnGUI;
#else
                EditorApplication.projectWindowItemOnGUI -= ItemGUI_2021_3_and_older;
                EditorApplication.projectWindowItemOnGUI = ItemGUI_2021_3_and_older + EditorApplication.projectWindowItemOnGUI;
#endif



                // wrapping updaters            

                EditorApplication.projectWindowItemOnGUI -= ProjectWindowItemOnGUI;
                EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;

                EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
                EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;

                EditorApplication.delayCall -= DelayCallLoop;
                EditorApplication.delayCall += DelayCallLoop;

                EditorApplication.update -= CheckIfFocusedWindowChanged;
                EditorApplication.update += CheckIfFocusedWindowChanged;



                // shortcuts

                var globalEventHandler = typeof(EditorApplication).GetFieldValue<EditorApplication.CallbackFunction>("globalEventHandler");
                typeof(EditorApplication).SetFieldValue("globalEventHandler", Shortcuts + (globalEventHandler - Shortcuts));



                // other

                EditorApplication.update -= GenerateIconsInUpdate;
                EditorApplication.update += GenerateIconsInUpdate;

                EditorApplication.projectChanged -= OnProjectChanged;
                EditorApplication.projectChanged += OnProjectChanged;


            }
            void loadData()
            {
                data = AssetDatabase.LoadAssetAtPath<VFoldersData>(ProjectPrefs.GetString("vFolders-lastKnownDataPath"));


                if (data) return;

                data = AssetDatabase.FindAssets("t:VFoldersData").Select(guid => AssetDatabase.LoadAssetAtPath<VFoldersData>(guid.ToPath())).FirstOrDefault();


                if (!data) return;

                ProjectPrefs.SetString("vFolders-lastKnownDataPath", data.GetPath());

            }
            void loadPalette()
            {
                palette = AssetDatabase.LoadAssetAtPath<VFoldersPalette>(ProjectPrefs.GetString("vFolders-lastKnownPalettePath"));


                if (palette) return;

                palette = AssetDatabase.FindAssets("t:VFoldersPalette").Select(guid => AssetDatabase.LoadAssetAtPath<VFoldersPalette>(guid.ToPath())).FirstOrDefault();


                if (!palette) return;

                ProjectPrefs.SetString("vFolders-lastKnownPalettePath", palette.GetPath());

            }
            void loadDataAndPaletteDelayed()
            {
                if (!data)
                    EditorApplication.delayCall += () => EditorApplication.delayCall += loadData;

                if (!palette)
                    EditorApplication.delayCall += () => EditorApplication.delayCall += loadPalette;

                // AssetDatabase isn't up to date at this point (it gets updated after InitializeOnLoadMethod)
                // and if current AssetDatabase state doesn't contain the data - it won't be loaded during Init()
                // so here we schedule an additional, delayed attempt to load the data
                // this addresses reports of data loss when trying to load it on a new machine

            }
            void migrateDataFromV1()
            {
                if (!data) return;
                if (ProjectPrefs.GetBool("vFolders-dataMigrationFromV1Attempted", false)) return;

                ProjectPrefs.SetBool("vFolders-dataMigrationFromV1Attempted", true);

                var lines = System.IO.File.ReadAllLines(data.GetPath());

                if (lines.Length < 15 || !lines[14].Contains("folderDatasByGuid")) return;

                var guids = new List<string>();
                var icons = new List<string>();
                var colors = new List<int>();

                void parseGudis()
                {
                    for (int i = 16; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("values:")) break;

                        var startIndex = lines[i].IndexOf("- ") + 2;

                        if (startIndex < lines[i].Length)
                            guids.Add(lines[i].Substring(startIndex));
                        else
                            guids.Add("");

                    }

                }
                void parseIcons()
                {
                    for (int i = 0; i < guids.Count; i++)
                        if (lines[29 + i * 5 + 3] is string line)
                            if (line.Length > line.IndexOf(": ") + 2)
                                icons.Add(line.Substring(line.IndexOf(": ") + 2));
                            else
                                icons.Add("");

                }
                void parseColors()
                {
                    for (int i = 0; i < guids.Count; i++)
                        if (lines[29 + i * 5 + 1] is string line)
                            if (line.Length > line.IndexOf(": ") + 2)
                                colors.Add(int.Parse(line.Substring(line.IndexOf(": ") + 2)));
                            else
                                colors.Add(0);

                }

                void remapColors()
                {
                    for (int i = 0; i < colors.Count; i++)
                        if (colors[i] == 10)
                            colors[i] = 1;
                        else if (colors[i] != 0)
                            colors[i]++;

                }
                void fillData()
                {
                    for (int i = 0; i < guids.Count; i++)
                        if (icons[i] != "" || colors[i] != 0)
                            data.folderDatas_byGuid[guids[i]] = new FolderData { iconNameOrGuid = icons[i], colorIndex = colors[i] };

                    data.Dirty();
                    data.Save();

                }


                try
                {
                    parseGudis();
                    parseIcons();
                    parseColors();

                    remapColors();
                    fillData();

                }
                catch { }

            }
            void fixIconNamesForUnity6()
            {
                if (!Application.unityVersion.Contains("6000")) return;
                if (ProjectPrefs.GetBool("vFolders-iconNamesForUnity6Fixed", false)) return;
                if (!palette) return;
                if (!data) return;

                foreach (var iconRow in palette.iconRows)
                    if (iconRow.builtinIcons.Contains("PhysicMaterial Icon"))
                        iconRow.builtinIcons[iconRow.builtinIcons.IndexOf("PhysicMaterial Icon")] = "PhysicsMaterial Icon";

                foreach (var folderData in data.folderDatas_byGuid.Values)
                    if (folderData.iconNameOrGuid == "PhysicMaterial Icon")
                        folderData.iconNameOrGuid = "PhysicsMaterial Icon";

                ProjectPrefs.SetBool("vFolders-iconNamesForUnity6Fixed", true);

            }

            subscribe();
            loadData();
            loadPalette();
            loadDataAndPaletteDelayed();
            migrateDataFromV1();
            fixIconNamesForUnity6();

            OnDomainReloaded();

        }

        public static VFoldersData data;
        public static VFoldersPalette palette;





        static IEnumerable<EditorWindow> allBrowsers => _allBrowsers ??= t_ProjectBrowser.GetFieldValue<IList>("s_ProjectBrowsers").Cast<EditorWindow>();
        static IEnumerable<EditorWindow> _allBrowsers;

        static Type t_ProjectBrowser = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
        static Type t_HostView = typeof(Editor).Assembly.GetType("UnityEditor.HostView");
        static Type t_EditorWindowDelegate = t_HostView.GetNestedType("EditorWindowDelegate", maxBindingFlags);

        static Type t_VTabs = Type.GetType("VTabs.VTabs") ?? Type.GetType("VTabs.VTabs, VTabs, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        static Type t_VFavorites = Type.GetType("VFavorites.VFavorites") ?? Type.GetType("VFavorites.VFavorites, VFavorites, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

        static MethodInfo mi_WrappedGUI = typeof(VFolders).GetMethod(nameof(WrappedGUI), maxBindingFlags);
        static MethodInfo mi_VFavorites_WrappedOnGUI = t_VFavorites?.GetMethod("WrappedOnGUI", maxBindingFlags);






#if UNITY_6000_3_OR_NEWER
        public static EntityId ToIdType(this int id) => id;
        public static List<int> ToInts(this List<EntityId> ids) => ids.Select(r => (int)r).ToList();
        public static List<int> GetIdList(this object o, string listName) => o.GetMemberValue<List<EntityId>>(listName)?.ToInts();
#else
        public static int ToIdType(this int id) => id;
        public static List<int> ToInts(this List<int> ids) => ids;
        public static List<int> GetIdList(this object o, string listName) => o.GetMemberValue<List<int>>(listName);
#endif







        const string version = "2.1.12";

    }

    #region Rules

    public class RuleAttribute : System.Attribute { }

    public class Folder
    {
        public string path => guid.ToPath();
        public string name => path.Split('/').Last();

        public int color = 0;
        public string icon = "";



        public Folder(string guid) => this.guid = guid;

        string guid;


    }



    #endregion

}
#endif
