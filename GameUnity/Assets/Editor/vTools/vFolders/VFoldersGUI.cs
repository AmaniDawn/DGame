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
using static VFolders.VFolders;
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
    public class VFoldersGUI
    {

        public void RowGUI(Rect rowRect, string guid, int instanceId)
        {
            var fullRowRect = rowRect.AddWidthFromRight(rowRect.x);

            var isRowHovered = fullRowRect.IsHovered();

            var isFolder = AssetDatabase.IsValidFolder(guid.ToPath());
            var isAsset = !isFolder && !guid.IsNullOrEmpty();
            var isSubasset = isAsset && typeof(AssetDatabase).InvokeMethod<bool>("IsSubAsset", instanceId);
            var isFavorite = !isFolder && !isAsset && rowRect.x != 16;

            var isFavoritesRoot = rowRect.x == 16 && !isFolder && rowRect.y == 0;
            var isAssetsRoot = rowRect.x == 16 && isFolder && guid.ToPath() == "Assets";
            var isPackagesRoot = rowRect.x == 16 && !isFavoritesRoot && !isAssetsRoot && guid.IsNullOrEmpty();

            var isListArea = isTwoColumns && (rowRect.x == 14 || !isFolder);

            var useMinimalMode = VFoldersMenu.minimalModeEnabled && !isListArea;
            var useBackgroundColors = (VFoldersMenu.backgroundColorsEnabled || useMinimalMode) && !isListArea;
            var useHierarchyLines = VFoldersMenu.hierarchyLinesEnabled && !isListArea;
            var useContentMinimap = VFoldersMenu.contentMinimapEnabled;
            var useZebraStriping = VFoldersMenu.zebraStripingEnabled;



            TreeViewItem treeItem = null;

            var isRowSelected = false;
            var isRowBeingRenamed = false;

            void setObjects()
            {
                if (isListArea) return;

                if (!curEvent.isRepaint)     // only needed for drawing,
                    if (!curEvent.isMouseUp) // altClick,
                        if (!isRowHovered)   // and setting last hovered tree item
                            return;


                void set_treeItem_byRect()
                {
                    if (treeViewAnimatesExpansion) return;

                    var offest = isTwoColumns ? -15 : -4;


                    if ((rowRect.y + offest) % 16 != 0) return;

                    var rowIndex = ((rowRect.y + offest) / 16).ToInt();


                    if (rows == null) return;
                    if (rowIndex < 0 || rowIndex >= rows.Count) return;

                    treeItem = rows[rowIndex];

                }
                void set_treeItem_byInstanceId()
                {
                    if (treeItem != null) return;
                    if (isFavorite || isFavoritesRoot) return;

                    treeItem = treeViewController?.InvokeMethod<TreeViewItem>("FindItem", instanceId.ToIdType());

                }

                set_treeItem_byRect();
                set_treeItem_byInstanceId();

            }
            void setState()
            {
                void set_isRowSelected_oneColumn()
                {
                    if (!curEvent.isRepaint) return;
                    if (isListArea) return;
                    if (treeItem == null) return;
                    if (isTwoColumns) return;


                    var dragging = treeView_dragSelectionList?.Any() == true;

                    isRowSelected = dragging ? (treeView_dragSelectionList.Contains(treeItem.id)) : Selection.Contains(treeItem.id);

                }
                void set_isRowSelected_twoColumns()
                {
                    if (!curEvent.isRepaint) return;
                    if (isListArea) return;
                    if (treeItem == null) return;
                    if (!isTwoColumns) return;


                    var dragging = treeView_dragSelectionList != null
                                && treeView_dragSelectionList.Any();

                    isRowSelected = dragging ? (treeView_dragSelectionList.Contains(treeItem.id)) : treeView_normalSelectionList != null && treeView_normalSelectionList.Contains(treeItem.id);

                }
                void set_lastHovered()
                {
                    if (isListArea) return;
                    if (!isRowHovered) return;

                    lastHoveredRowRect_screenSpace = EditorGUIUtility.GUIToScreenRect(fullRowRect);
                    lastHoveredTreeItem = treeItem;

                }
                void set_lastKnownMousePosition()
                {
                    if (!curEvent.isRepaint) return;

                    lastKnownMousePosition_screenSpace = curEvent.mousePosition_screenSpace;

                }

                set_isRowSelected_oneColumn();
                set_isRowSelected_twoColumns();
                set_lastHovered();
                set_lastKnownMousePosition();

                isRowBeingRenamed = renamingRow && isRowSelected;

            }

            void drawing()
            {
                if (!curEvent.isRepaint) { hierarchyLines_isFirstRowDrawn = false; return; }



                var folderInfo = GetFolderInfo(guid);

                var rowHasColor = useBackgroundColors && folderInfo.hasColor;

                var rowHasCustomIcon = !isFolder ? false
                                                 : useBackgroundColors ? folderInfo.hasIcon
                                                                       : folderInfo.hasIcon || folderInfo.hasColor;
                var rowHasIcon = !useMinimalMode ? true
                                                 : rowHasCustomIcon || isAsset || isFavorite || isRowBeingRenamed;


                var hideName = rowHasColor || !rowHasIcon;
                var hideDefaultIcon = rowHasCustomIcon || !rowHasIcon;

                var drawTriangle = rowHasColor && treeItem?.hasChildren == true;
                var drawName = rowHasColor || !rowHasIcon;
                var drawDefaultIcon = (rowHasColor && !rowHasCustomIcon) && rowHasIcon;

                var makeTriangleBrighter = rowHasColor && isDarkTheme;
                var makeNameBrighter = rowHasColor && isDarkTheme;
                var makeIconBrighter = rowHasColor && isFolder;
                var makeExtraSpaceForVCSIcons = isVersionControlEnabled;


                var defaultBackground = isListArea ? Greyscale(isDarkTheme ? .2f : .75f)
                                                   : GUIColors.windowBackground;
                if (isRowSelected)
                    if (!isRowBeingRenamed)
                        defaultBackground = isTreeFocused ? GUIColors.selectedBackground
                                                          : Greyscale(isDarkTheme ? .3f : .68f);



                void hideName_()
                {
                    if (!hideName) return;
                    if (isFavorite) return;

                    var name = isListArea ? guid.ToPath().GetFilename() : treeItem != null ? treeItem.displayName : "Favorites";

                    var nameRect = rowRect.SetWidth(name.GetLabelWidth(isBold: isFavorite || isAssetsRoot || isPackagesRoot) + 6).MoveX(16).MoveX(isListArea ? 3 : 0);

                    nameRect.Draw(defaultBackground);

                }
                void hideDefaultIcon_()
                {
                    if (!hideDefaultIcon) return;

                    var iconRect = rowRect.SetWidth(16).MoveX(isListArea ? 3 : 0);

                    if (makeExtraSpaceForVCSIcons)
                        iconRect = iconRect.MoveX(8);


                    if (isListArea)
                    {
                        SetGUIColor(defaultBackground);

                        GUI.DrawTexture(iconRect, EditorIcons.GetIcon("Folder On Icon"));

                        ResetGUIColor();

                    }
                    else
                        iconRect.Draw(defaultBackground);

                }

                void color()
                {
                    if (!rowHasColor) return;



                    var color = (isDarkTheme ? Color.Lerp(folderInfo.color, Greyscale(.05f), .42f)
                                             : Color.Lerp(folderInfo.color, Greyscale(.8f), .5f)).SetAlpha(1);
                    if (isRowHovered)
                        color *= isDarkTheme ? 1.1f : .92f;

                    if (isRowSelected)
                        color *= isDarkTheme ? 1.2f : .8f;

                    if (palette?.colorGradientsEnabled == false)
                        color = MathUtil.Lerp(color, Greyscale(.2f), isDarkTheme ? .25f : .03f);

                    if (folderInfo.hasColorByRecursion)
                        color = MathUtil.Lerp(color, Greyscale(isDarkTheme ? .24f : .8f), .45f);






                    var colorRect = rowRect.AddWidthFromRight(30).AddWidth(16);

                    if (folderInfo.hasColorByRecursion)
                        colorRect = colorRect.AddWidthFromRight((folderInfo.maxColorRecursionDepth + (isSubasset ? 1 : 0)) * 14);

                    if (!isRowSelected && !folderInfo.hasColorByRecursion)
                        colorRect = colorRect.AddHeightFromMid(EditorGUIUtility.pixelsPerPoint >= 2 ? -.5f : -1);

                    if (folderInfo.hasColorByRecursion)
                        colorRect = colorRect.MoveY(EditorGUIUtility.pixelsPerPoint >= 2 ? -.25f : -.5f);

                    if (palette?.colorGradientsEnabled == false) { colorRect.Draw(color); return; }


                    var hasLeftGradient = colorRect.x > 4;

                    if (hasLeftGradient)
                        colorRect = colorRect.AddWidthFromRight(3);



                    var leftGradientWith = hasLeftGradient ? 22 : 0;
                    var rightGradientWidth = (fullRowRect.width * .77f).Min(colorRect.width - leftGradientWith);

                    var leftGradientRect = colorRect.SetWidth(leftGradientWith);
                    var rightGradientRect = colorRect.SetWidthFromRight(rightGradientWidth);

                    var flatColorRect = colorRect.SetX(leftGradientRect.xMax).SetXMax(rightGradientRect.x);






                    leftGradientRect.AddWidth(1).DrawCurtainLeft(color);

                    flatColorRect.AddWidth(1).Draw(color);

                    rightGradientRect.Draw(color.MultiplyAlpha(.1f));
                    rightGradientRect.DrawCurtainRight(color);


                }
                void triangle()
                {
                    if (!drawTriangle) return;


                    var triangleRect = rowRect.MoveX(-15).SetWidth(16).Resize(-1);

                    GUI.Label(triangleRect, EditorGUIUtility.IconContent(expandedIds.Contains(treeItem.id) ? "IN_foldout_on" : "IN_foldout"));


                    if (!makeTriangleBrighter) return;

                    GUI.Label(triangleRect, EditorGUIUtility.IconContent(expandedIds.Contains(treeItem.id) ? "IN_foldout_on" : "IN_foldout"));

                }
                void name()
                {
                    if (!drawName) return;
                    if (isRowBeingRenamed) return;


                    var nameRect = rowRect.MoveX(18).AddHeight(1); ;

                    if (isListArea)
                        nameRect = nameRect.MoveX(3);

                    if (!rowHasIcon)
                        nameRect = nameRect.MoveX(-17);

                    if (makeNameBrighter)
                        nameRect = nameRect.MoveX(-2).MoveY(-.5f);

                    if (makeExtraSpaceForVCSIcons)
                        nameRect = nameRect.MoveX(16);



                    var styleName = makeNameBrighter ? "WhiteLabel" : "TV Line";

                    if (isFavoritesRoot || isAssetsRoot || isPackagesRoot)
                        styleName = "BoldLabel";



                    var name = isFavoritesRoot ? "Favorites" :
                               isPackagesRoot ? "Packages" :
                               isListArea || treeItem == null ? guid.ToPath().GetFilename() :
                               treeItem.displayName;



                    if (makeNameBrighter)
                        SetGUIColor(Greyscale(isRowSelected ? 1 : .93f));

                    GUI.skin.GetStyle(styleName).Draw(nameRect, name, false, false, isRowSelected, isTreeFocused && styleName != "BoldLabel");

                    if (makeNameBrighter)
                        ResetGUIColor();

                }
                void defaultIcon()
                {
                    if (!drawDefaultIcon) return;


                    var iconRect = rowRect.SetWidth(16).MoveX(isListArea ? 3 : 0);

                    if (makeExtraSpaceForVCSIcons)
                        iconRect = iconRect.MoveX(8);


                    var icon = isAsset ? treeItem?.GetMemberValue<Texture2D>("icon") ?? AssetDatabase.GetCachedIcon(guid.ToPath())
                                       : makeIconBrighter ? EditorIcons.GetIcon(folderInfo.folderState.isEmpty ? "FolderEmpty On Icon" : "Folder On Icon")
                                                          : EditorIcons.GetIcon(folderInfo.folderState.isEmpty ? "FolderEmpty Icon" : "Folder Icon");


                    SetLabelAlignmentCenter();

                    if (makeIconBrighter)
                        SetGUIColor(Greyscale(.88f));

                    GUI.DrawTexture(iconRect, icon);

                    if (makeIconBrighter)
                        ResetGUIColor();

                    ResetLabelStyle();

                }
                void customIcon()
                {
                    if (!rowHasCustomIcon) return;


                    var icon = GetSmallFolderIcon(folderInfo, removeColor: useBackgroundColors);

                    if (useMinimalMode && folderInfo.hasIcon)
                        icon = EditorIcons.GetIcon(folderInfo.iconNameOrPath);

                    if (!icon) return;


                    var iconRect = rowRect.SetWidth(16).MoveX(isListArea ? 3 : 0);

                    iconRect = iconRect.SetWidth(iconRect.height / icon.height * icon.width);

                    if (makeExtraSpaceForVCSIcons)
                        iconRect = iconRect.MoveX(7);


                    GUI.DrawTexture(iconRect, icon);

                }
                void versionControlOverlay()
                {
                    if (!isFolder) return;
                    if (!hideDefaultIcon && !rowHasColor) return;

                    if (!isVersionControlEnabled) return;

                    typeof(Editor).Assembly.GetType("UnityEditorInternal.VersionControl.ProjectHooks").InvokeMethod("HandleVersionControlOverlays", guid, rowRect.MoveX(3));

                }

                void hierarchyLines()
                {
                    if (!useHierarchyLines) return;
                    if (isListArea) return;
                    if (treeItem == null) return;


                    var lineThickness = 1f;
                    var lineColor = isDarkTheme ? Greyscale(1, .165f) : Greyscale(0, .23f);

                    var depth = ((rowRect.x - 16) / 14).RoundToInt();

                    bool isLastChild(TreeViewItem item) => item.parent?.children?.LastOrDefault() == item;
                    bool hasChilren(TreeViewItem item) => item.children != null && item.children.Count > 0;

                    void calcVerticalGaps_beforeFirstRowDrawn()
                    {
                        if (hierarchyLines_isFirstRowDrawn) return;

                        hierarchyLines_verticalGaps.Clear();

                        var curItem = treeItem.parent;
                        var curDepth = depth - 1;

                        while (curItem != null && curItem.parent != null)
                        {
                            if (isLastChild(curItem))
                                hierarchyLines_verticalGaps.Add(curDepth - 1);

                            curItem = curItem.parent;
                            curDepth--;
                        }

                    }
                    void updateVerticalGaps_beforeNextRowDrawn()
                    {
                        if (isLastChild(treeItem))
                            hierarchyLines_verticalGaps.Add(depth - 1);

                        if (depth < hierarchyLines_prevRowDepth)
                            hierarchyLines_verticalGaps.RemoveAll(r => r >= depth);

                    }

                    void drawVerticals()
                    {
                        for (int i = 1; i < depth; i++)
                            if (!hierarchyLines_verticalGaps.Contains(i))
                                rowRect.SetX(9 + i * 14 - lineThickness / 2)
                                       .SetWidth(lineThickness)
                                       .SetHeight(isLastChild(treeItem) && i == depth - 1 ? 8 + lineThickness / 2 : 16)
                                       .Draw(lineColor);

                    }
                    void drawHorizontals()
                    {
                        if (depth == 0) return;
                        if (depth == 1) return;

                        rowRect.MoveX(-21)
                               .SetHeightFromMid(lineThickness)
                               .SetWidth(hasChilren(treeItem) ? 7 : 17)
                               .AddWidthFromRight(-lineThickness / 2f)
                               .Draw(lineColor);

                    }



                    calcVerticalGaps_beforeFirstRowDrawn();

                    drawVerticals();
                    drawHorizontals();

                    updateVerticalGaps_beforeNextRowDrawn();

                    hierarchyLines_prevRowDepth = depth;
                    hierarchyLines_isFirstRowDrawn = true;

                }
                void zebraStriping_()
                {
                    if (!useZebraStriping) return;


                    var contrast = isDarkTheme ? .033f : .05f;


                    var firstRowY = isOneColumn ? 4 : -1;

                    var t = 1 - (rowRect.y - firstRowY).PingPong(16f) / 16f;

                    if (isRowHovered || isRowSelected)
                        if (!VFoldersPaletteWindow.instance || VFoldersPaletteWindow.instance.guids.Contains(guid))
                            t = 1;

                    if (t.Approx(0)) return;



                    fullRowRect.Draw(Greyscale(isDarkTheme ? 1 : 0, contrast * t));


                }
                void highlight_()
                {
                    if (!controller.animatingHighlight) return;
                    if (guid.ToPath() != controller.folderToHighlight) return;


                    var highlightBrightness = isDarkTheme ? .16f : .35f;


                    var highlightAmount = controller.highlightAmount.Clamp01();

                    highlightAmount = highlightAmount * highlightAmount * (3 - 2 * highlightAmount);


                    fullRowRect.AddWidthFromRight(123).Draw(Greyscale(1, highlightBrightness * highlightAmount));

                }
                void hoverHighlight()
                {
                    if (!VFoldersPaletteWindow.instance)
                        if (!rowRect.IsHovered()) return;

                    if (VFoldersPaletteWindow.instance)
                        if (VFoldersPaletteWindow.instance.guids.Count > 1 || VFoldersPaletteWindow.instance.guids.First() != guid || !paletteOpenedOnRow) return;


                    fullRowRect.Draw(Greyscale(isDarkTheme ? 1 : 0, .06f));

                }

                void contentMinimap()
                {
                    if (!isFolder) return;
                    if (!useContentMinimap) return;
                    if (guid.IsNullOrEmpty()) return;

                    void icon(Rect rect, string name)
                    {
                        var icon = EditorIcons.GetIcon(name);

                        if (!icon) return;


                        SetGUIColor(Greyscale(1, isDarkTheme ? .5f : .7f));

                        GUI.DrawTexture(rect, icon);

                        ResetGUIColor();

                    }


                    var iconDistance = 13;
                    var minButtonX = rowRect.x + guid.ToPath().GetFilename().GetLabelWidth() + iconDistance + 2;
                    var iconRect = fullRowRect.SetWidthFromRight(iconDistance).SetSizeFromMid(12, 12).MoveX(-1.5f);

                    foreach (var iconName in folderInfo.folderState.contentMinimapIconNames)
                    {
                        if (iconRect.x < minButtonX) continue;

                        icon(iconRect, iconName);

                        iconRect = iconRect.MoveX(-iconDistance);

                    }

                }



                fullRowRect.MarkInteractive();

                hideName_();
                hideDefaultIcon_();

                color();
                triangle();
                defaultIcon();
                customIcon();
                versionControlOverlay();
                name();

                hierarchyLines();
                zebraStriping_();
                highlight_();
                hoverHighlight();

                contentMinimap();

            }

            void altDrag()
            {
                if (!curEvent.holdingAlt) return;

                void mouseDown()
                {
                    if (!curEvent.isMouseDown) return;
                    if (!isRowHovered) return;

                    mouseDownPos = curEvent.mousePosition;

                }
                void mouseDrag()
                {
                    if (!curEvent.isMouseDrag) return;
                    if ((curEvent.mousePosition - mouseDownPos).magnitude < 5) return;
                    if (!rowRect.Contains(mouseDownPos)) return;
                    if (!rowRect.Contains(curEvent.mousePosition - curEvent.mouseDelta)) return;
                    if (DragAndDrop.objectReferences.Any()) return;

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { AssetDatabase.LoadAssetAtPath<Object>(guid.ToPath()) };
                    DragAndDrop.StartDrag(guid.ToPath().GetFilename());

                }

                mouseDown();
                mouseDrag();

                // altdrag has to be set up manually before altClick 
                // because altClick will use() mouseDown event to prevent selection change

            }
            void altClick()
            {
                if (!isRowHovered) return;
                if (!curEvent.holdingAlt) return;
                if (!isFolder) return;

                void mouseDown()
                {
                    if (!curEvent.isMouseDown) return;

                    curEvent.Use();

                }
                void mouseUp()
                {
                    if (!curEvent.isMouseUp) return;

                    var selectedGuids = isListArea
                                        ?
                                        Selection.objects.Where(r => r is DefaultAsset).Select(r => r.GetPath().ToGuid())
                                        :
#if UNITY_2021_1_OR_NEWER
                                        treeViewController.GetFieldValue("m_CachedSelection").GetIdList("m_List")
#else
                                        treeViewController?.GetMemberValue("state").GetMemberValue<List<int>>("selectedIDs")
#endif


#if UNITY_6000_3_OR_NEWER
                                 .Select(id => treeViewController.InvokeMethod("FindItem", (EntityId)id))
#else
                                 .Select(id => treeViewController.InvokeMethod("FindItem", id))
#endif

                                 .Where(r => r?.GetType().Name == "FolderTreeItem")
                                 .Select(r => r.GetPropertyValue<string>("Guid"))
                                 .Where(r => r != null);


                    var editMultiSelection = selectedGuids.Count() > 1 && selectedGuids.Contains(guid);

                    var guidsToEdit = (editMultiSelection ? selectedGuids.Where(r => AssetDatabase.IsValidFolder(r.ToPath())) : new[] { guid }).ToList();


                    if (VFoldersPaletteWindow.instance && VFoldersPaletteWindow.instance.guids.SequenceEqual(guidsToEdit)) { VFoldersPaletteWindow.instance.Close(); return; }

                    var openNearRect = rowRect;
                    var position = EditorGUIUtility.GUIToScreenPoint(new Vector2(curEvent.mousePosition.x + 20, openNearRect.y - 13));

                    if (!VFoldersPaletteWindow.instance)
                        VFoldersPaletteWindow.CreateInstance(position);

                    VFoldersPaletteWindow.instance.Init(guidsToEdit);
                    VFoldersPaletteWindow.instance.Focus();

                    VFoldersPaletteWindow.instance.targetPosition = position;

                    paletteOpenedOnRow = true;
                    paletteOpenedOnCell = false;

                    if (editMultiSelection)
                        Selection.objects = null;
                }

                mouseDown();
                mouseUp();

            }



            setObjects();
            setState();

            drawing();

            altDrag();
            altClick();

        }

        List<int> hierarchyLines_verticalGaps = new();
        bool hierarchyLines_isFirstRowDrawn;
        int hierarchyLines_prevRowDepth;

        Vector2 mouseDownPos;

        bool paletteOpenedOnRow;




        public void CellGUI(Rect cellRect, string guid, int instanceId)
        {
            var isFolder = AssetDatabase.IsValidFolder(guid.ToPath());

            void setLastVisibleSelectedForAltClick()
            {
                if (!isFolder) return;
                if (!curEvent.isRepaint) return;
                if (!Selection.objects.Contains(AssetDatabase.LoadAssetAtPath<DefaultAsset>(guid.ToPath()))) return;

                lastVisibleSelectedCellRect = cellRect;

            }

            void drawing()
            {
                if (!curEvent.isRepaint) { namesDrawnForGuids.Clear(); return; }

                var folderInfo = isFolder ? GetFolderInfo(guid) : null;
                var showingSecondNameLine = false;

                void hideIcon()
                {
                    if (!isFolder) return;
                    if (!folderInfo.hasColor) return;

                    var maskRect = cellRect.SetHeight(cellRect.width).Resize(4);

                    // if (isVersionControlEnabled)
                    // maskRect = maskRect.SetHeightFromBottom(maskRect.height * 2 / 3);


                    maskRect.Draw(EditorGUIUtility.isProSkin ? Greyscale(.2f) : Greyscale(.75f));

                }
                void icon()
                {
                    if (!isFolder) return;
                    if (!folderInfo.hasColor && !folderInfo.hasIcon) return;

                    DrawBigFolderIcon(cellRect, folderInfo);

                }
                void versionControlOverlay()
                {
                    if (!isFolder) return;
                    if (!folderInfo.hasColor) return;

                    if (!isVersionControlEnabled) return;

                    typeof(Editor).Assembly.GetType("UnityEditorInternal.VersionControl.ProjectHooks").InvokeMethod("HandleVersionControlOverlays", guid, cellRect);

                }
                void twoLineName()
                {
                    if (namesDrawnForGuids.Contains(guid)) return; // disables two-line names on subassets
                    if (!VFoldersMenu.twoLineNamesEnabled) return;


                    var isSelected = listArea_dragSelectionList.Any() ? listArea_dragSelectionList.Contains(instanceId)
                                                                      : _Selection_instanceIDs.Contains(instanceId);

                    var isCellBeingRenamed = isSelected && renamingCell;

                    if (isCellBeingRenamed) return;



                    var maxLineWidth = cellRect.width + 14;

                    var name = guid.ToPath().GetFilename(withExtension: false); // Resources.InstanceIDToObject(instanceId).name;


                    string firstLine = null;
                    string secondLine = null;

                    Rect firstLineRect = default;
                    Rect secondLineRect = default;


                    void getCachedLines()
                    {
                        if (twoLineNamesCachedForCellWidth != cellRect.width)
                            twoLineNamesCache.Clear();

                        if (!twoLineNamesCache.ContainsKey(name)) return;


                        firstLine = twoLineNamesCache[name].Item1;
                        secondLine = twoLineNamesCache[name].Item2;

                    }
                    void calcLines()
                    {
                        if (twoLineNamesCache.ContainsKey(name)) return;
                        if (name.GetLabelWidth() < maxLineWidth)
                        {
                            firstLine = name;

                            twoLineNamesCache[name] = (firstLine, secondLine);
                            twoLineNamesCachedForCellWidth = cellRect.width;

                            return;

                        }


                        var separators = new[] { ' ', '_', '-', '.' };

                        var splitIndexes_separators = Enumerable.Range(1, name.Length - 2).Where(i => separators.Contains(name[i])).Select(i => i + 1);
                        var splitIndexes_camelcase = Enumerable.Range(2, name.Length - 2).Where(i => name[i].IsUpper() && name[i - 1].IsLower() && !separators.Contains(name[i - 1]));
                        var splitIndexes_all = Enumerable.Range(1, name.Length - 1);

                        bool splitSucceeded = false;


                        string fit(string s, IEnumerable<int> splitIndexes, bool splitToRight, bool addEllipsis = false)
                        {
                            foreach (var i in splitToRight ? splitIndexes : splitIndexes.Reverse())
                            {
                                var substring = splitToRight ? s[i..] : s[..i];

                                substring = substring.Trim();

                                if (addEllipsis)
                                    substring += "…";



                                if (substring.GetLabelWidth() < maxLineWidth)
                                    return substring;

                            }

                            return null;

                        }

                        void truncate()
                        {
                            if (splitIndexes_separators.Any()) return;
                            if (splitIndexes_camelcase.Any()) return;

                            firstLine = fit(name, splitIndexes_all, splitToRight: false, addEllipsis: true);

                            splitSucceeded = true;

                        }
                        void split_clean()
                        {
                            if (splitSucceeded) return;


                            firstLine = null;
                            firstLine ??= fit(name, splitIndexes_separators, splitToRight: false);
                            firstLine ??= fit(name, splitIndexes_camelcase, splitToRight: false);

                            if (firstLine == null) return;



                            secondLine = name.Remove(firstLine);

                            splitSucceeded = secondLine.GetLabelWidth() < maxLineWidth;

                        }
                        void split_withEllipsis()
                        {
                            if (splitSucceeded) return;


                            secondLine = null;
                            secondLine ??= fit(name, splitIndexes_separators, splitToRight: true);
                            secondLine ??= fit(name, splitIndexes_camelcase, splitToRight: true);
                            secondLine ??= fit(name, splitIndexes_all, splitToRight: true);



                            firstLine = name.Remove(secondLine).Trim();

                            if (firstLine.GetLabelWidth() > maxLineWidth)
                                firstLine = fit(firstLine, Enumerable.Range(0, firstLine.Length), splitToRight: false, addEllipsis: true);

                        }


                        truncate();
                        split_clean();
                        split_withEllipsis();

                        firstLine = firstLine.Trim();
                        secondLine = secondLine?.Trim();

                        twoLineNamesCache[name] = (firstLine, secondLine);
                        twoLineNamesCachedForCellWidth = cellRect.width;

                    }
                    void calcLineRects()
                    {
                        firstLineRect = cellRect.SetHeightFromBottom(12).AddHeight(2).SetWidthFromMid(maxLineWidth).MoveY(-1);

                        if (secondLine.IsNullOrEmpty()) return;

                        if (isFolder)
                            firstLineRect = firstLineRect.MoveY(-4);
                        else
                            firstLineRect = firstLineRect.MoveY(-1);



                        secondLineRect = firstLineRect.MoveY(12);

                    }

                    void hideDefaultName()
                    {
                        if (!curEvent.isRepaint) return;

                        var defaultNameRect = cellRect.SetHeightFromBottom(16).AddHeight(2).AddWidthFromMid(12);
                        var maskColor = isDarkTheme ? Greyscale(.2f) : Greyscale(.75f);

                        defaultNameRect.Draw(maskColor);

                    }
                    void selectedbackground()
                    {
                        if (!isSelected) return;


                        var backgroundColor = isListAreaFocused ? GUIColors.selectedBackground : Greyscale(isDarkTheme ? .3f : .68f);



                        var longestLine = secondLine?.Length > firstLine.Length ? secondLine : firstLine;

                        var backgroundRect = firstLineRect.SetWidthFromMid(longestLine.GetLabelWidth(fontSize: 10) + 2).SetHeightFromMid(14);

                        if (!secondLine.IsNullOrEmpty())
                            backgroundRect = backgroundRect.SetYMax(secondLineRect.SetHeightFromMid(14).yMax);



                        backgroundRect.DrawRounded(backgroundColor, 4);

                    }
                    void drawName()
                    {
                        SetLabelAlignmentCenter();
                        SetLabelFontSize(10);
                        SetGUIColor(isSelected && isListAreaFocused ? Greyscale(123, 123) : Greyscale(1));

                        GUI.Label(firstLineRect, firstLine);

                        if (secondLine != null)
                            GUI.Label(secondLineRect, secondLine);

                        ResetGUIColor();
                        ResetLabelStyle();

                    }


                    getCachedLines();
                    calcLines();
                    calcLineRects();

                    hideDefaultName();
                    selectedbackground();
                    drawName();

                    showingSecondNameLine = secondLine != null;

                    namesDrawnForGuids.Add(guid);

                }
                void hoverHighlight()
                {

                    var highlightColor = isDarkTheme ? Greyscale(1, .058f) : Greyscale(0, .07f);


                    var highlightRect = cellRect.Resize(-2).AddHeight(4).AddWidthFromMid(4);

                    if (showingSecondNameLine)
                        highlightRect = highlightRect.AddHeight(8);



                    var hoverRect = cellRect.AddWidthFromMid(gridHorizontalSpacing).AddHeight(gridVerticalSpacing);

                    hoverRect.MarkInteractive();




                    if (!VFoldersPaletteWindow.instance)
                        if (!hoverRect.IsHovered()) return;

                    if (VFoldersPaletteWindow.instance)
                        if (!VFoldersPaletteWindow.instance.guids.Contains(guid) || !paletteOpenedOnCell) return;


                    highlightRect.DrawRounded(highlightColor, 5);

                }


                hideIcon();
                icon();
                versionControlOverlay();
                twoLineName();
                hoverHighlight();

            }

            void altDrag()
            {
                if (!isFolder) return;
                if (!curEvent.holdingAlt) return;

                void mouseDown()
                {
                    if (!curEvent.isMouseDown) return;
                    if (!cellRect.IsHovered()) return;

                    mouseDownPos = curEvent.mousePosition;

                }
                void mouseDrag()
                {
                    if (!curEvent.isMouseDrag) return;
                    if ((curEvent.mousePosition - mouseDownPos).magnitude < 5) return;
                    if (!cellRect.Contains(mouseDownPos)) return;
                    if (!cellRect.Contains(curEvent.mousePosition - curEvent.mouseDelta)) return;
                    if (DragAndDrop.objectReferences.Any()) return;

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { AssetDatabase.LoadAssetAtPath<Object>(guid.ToPath()) };
                    DragAndDrop.StartDrag(guid.ToPath().GetFilename());

                }

                mouseDown();
                mouseDrag();

            }
            void altClick()
            {
                if (!isFolder) return;
                if (!cellRect.IsHovered()) return;
                if (!curEvent.holdingAlt) return;

                void mouseDown()
                {
                    if (!curEvent.isMouseDown) return;

                    curEvent.Use();

                }
                void mouseUp()
                {
                    if (!curEvent.isMouseUp) return;

                    var selectedFoldersGuids = Selection.objects.Where(r => r is DefaultAsset).Select(r => r.GetPath().ToGuid());

                    var editMultiSelection = selectedFoldersGuids.Count() > 1 && selectedFoldersGuids.Contains(guid);

                    var guidsToEdit = (editMultiSelection ? selectedFoldersGuids : new[] { guid }).ToList();


                    if (VFoldersPaletteWindow.instance && VFoldersPaletteWindow.instance.guids.SequenceEqual(guidsToEdit)) { VFoldersPaletteWindow.instance.Close(); return; }

                    var openNearRect = editMultiSelection ? lastVisibleSelectedCellRect : cellRect;
                    var position = EditorGUIUtility.GUIToScreenPoint(new Vector2(openNearRect.xMax + 10, openNearRect.y - 5));

                    if (!VFoldersPaletteWindow.instance)
                        VFoldersPaletteWindow.CreateInstance(position);

                    VFoldersPaletteWindow.instance.Init(guidsToEdit);
                    VFoldersPaletteWindow.instance.Focus();

                    VFoldersPaletteWindow.instance.targetPosition = position;

                    paletteOpenedOnCell = true;
                    paletteOpenedOnRow = false;

                    if (editMultiSelection)
                        Selection.objects = null;
                }

                mouseDown();
                mouseUp();

            }


            setLastVisibleSelectedForAltClick();

            drawing();

            altDrag();
            altClick();

        }

        Rect lastVisibleSelectedCellRect;

        HashSet<string> namesDrawnForGuids = new();

        Dictionary<string, (string, string)> twoLineNamesCache = new();

        float twoLineNamesCachedForCellWidth;

        bool paletteOpenedOnCell;











        public void UpdateState_Layout()
        {
            isOneColumn = window.GetFieldValue<int>("m_ViewMode") == 0;


            listArea = isTwoColumns ? window.GetFieldValue("m_ListArea") : null;

            treeViewController = window.GetFieldValue(isTwoColumns ? "m_FolderTree" : "m_AssetTree");
            treeViewControllerData = treeViewController?.GetPropertyValue("data");

            rows = treeViewControllerData?.InvokeMethod<IList<TreeViewItem>>("GetRows");


            var treeViewState = treeViewController?.GetPropertyValue<TreeViewState>("state");

            expandedIds = treeViewState?.expandedIDs?.ToInts() ?? new();



            var treeViewAnimator = treeViewController?.GetMemberValue("m_ExpansionAnimator");

            treeViewAnimatesExpansion = treeViewAnimator?.GetMemberValue<bool>("isAnimating") ?? false;

        }
        public void UpdateState_Repaint()
        {
            var treeViewControlID = window.GetFieldValue<int>("m_TreeViewKeyboardControlID");
            var listAreaControlID = window.GetFieldValue<int>("m_ListKeyboardControlID");

            isTreeFocused = EditorWindow.focusedWindow == window && (GUIUtility.keyboardControl == treeViewControlID || treeViewControlID == 0 || treeViewControlID == -1);
            isListAreaFocused = EditorWindow.focusedWindow == window && (GUIUtility.keyboardControl == listAreaControlID || listAreaControlID == 0 || listAreaControlID == -1);

            renamingRow = EditorGUIUtility.editingTextField && treeViewController?.GetMemberValue("state")?.GetMemberValue("renameOverlay")?.InvokeMethod<bool>("IsRenaming") == true;
            renamingCell = EditorGUIUtility.editingTextField && listArea?.InvokeMethod("GetRenameOverlay")?.InvokeMethod<bool>("IsRenaming") == true;



            var grid = listArea?.GetMemberValue("m_LocalAssets")?.GetMemberValue("m_Grid");

            gridHorizontalSpacing = grid?.GetMemberValue<float>("horizontalSpacing") ?? 0;
            gridVerticalSpacing = grid?.GetMemberValue<float>("verticalSpacing") ?? 0;



            listArea_dragSelectionList = listArea?.GetMemberValue("m_LocalAssets")?.GetIdList("m_DragSelection") ?? new();
            treeView_dragSelectionList = treeViewController?.GetFieldValue("m_DragSelection")?.GetIdList("m_List") ?? new();
            treeView_normalSelectionList = isTwoColumns ? treeViewController?.GetFieldValue("m_CachedSelection")?.GetIdList("m_List") ?? new() : null;



            // isVersionControlEnabled = typeof(Editor).Assembly.GetType("UnityEditor.AssetsTreeViewGUI").GetMemberValue<bool>("s_VCEnabled");


            // only treeView_normalSelectionList must be updated in repaint, the rest can be moved to UpdateState_Layout
            // but they are all grouped here since they are only used for drawing

        }

        bool isOneColumn;
        bool isTwoColumns => !isOneColumn;

        bool treeViewAnimatesExpansion;

        public object listArea;

        public object treeViewController;
        public object treeViewControllerData;

        public IList<TreeViewItem> rows;

        public List<int> expandedIds = new();

        bool isTreeFocused;
        bool isListAreaFocused;

        bool renamingRow;
        bool renamingCell;

        float gridVerticalSpacing;
        float gridHorizontalSpacing;

        List<int> listArea_dragSelectionList = new();
        List<int> treeView_dragSelectionList = new();
        List<int> treeView_normalSelectionList = new();

        bool isVersionControlEnabled;












        public void UpdateFoldersFirst()
        {
            if (!VFoldersMenu.foldersFirstEnabled) return;
            if (Application.platform != RuntimePlatform.OSXEditor) return;

            void oneColumn()
            {
                if (isTwoColumns) return;
                if (foldersFirst_initedForOneColumn) return;


                var m_AssetTree = window.GetFieldValue("m_AssetTree");

                if (m_AssetTree == null) return;

                m_AssetTree.GetPropertyValue("data").SetPropertyValue("foldersFirst", true);
                m_AssetTree.InvokeMethod("ReloadData");


                foldersFirst_initedForOneColumn = true;
                foldersFirst_initedForTwoColumns = false;

            }
            void twoColumns()
            {
                if (!isTwoColumns) return;
                if (foldersFirst_initedForTwoColumns) return;


                var m_ListArea = window.GetFieldValue("m_ListArea");

                if (m_ListArea == null) return;

                m_ListArea.SetPropertyValue("foldersFirst", true);
                window.InvokeMethod("InitListArea");


                foldersFirst_initedForOneColumn = false;
                foldersFirst_initedForTwoColumns = true;

            }

            oneColumn();
            twoColumns();

        }

        bool foldersFirst_initedForOneColumn;
        bool foldersFirst_initedForTwoColumns;












        public VFoldersGUI(EditorWindow window) => this.window = window;

        public EditorWindow window;

        public VFoldersController controller => VFolders.controllers_byWindow[window];

    }
}
#endif
