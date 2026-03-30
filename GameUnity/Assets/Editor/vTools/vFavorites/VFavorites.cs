#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.Linq;
using UnityEngine.UIElements;
using Type = System.Type;
using static VFavorites.Libs.VUtils;
using static VFavorites.Libs.VGUI;
using static VFavorites.VFavoritesData;


namespace VFavorites
{
    public static class VFavorites
    {
        static void WrappedOnGUI(object _)
        {
            if (wrappedBrowser?.GetType() != t_BrowserWindow) { originalBrowserGUI(); return; }


            void createData()
            {
                if (data) return;

                data = ScriptableObject.CreateInstance<VFavoritesData>();

                AssetDatabase.CreateAsset(data, GetScriptPath("VFavorites").GetParentPath().CombinePath("vFavorites Data.asset"));



                // // migrateDataFromV1

                // if (!EditorPrefsCached.HasKey("vFavorites-guids-" + GetProjectId())) return;
                // if (EditorPrefsCached.HasKey("vHierarchy-dataMigrationFromV1Attempted-" + GetProjectId())) return;

                // EditorPrefsCached.SetBool("vHierarchy-dataMigrationFromV1Attempted-" + GetProjectId(), true);


                // if (!data.pages.Any())
                //     data.pages.Add(new Page("Page 1"));


                // var guidsFromV1 = EditorPrefsCached.GetString("vFavorites-guids-" + GetProjectId()).Split('-').Where(r => r != "").ToList();

                // foreach (var guid in guidsFromV1)
                //     if (AssetDatabase.LoadAssetAtPath<Object>(guid.ToPath()) is Object obj)
                //         data.pages.First().items.Add(new Item(obj));


                // data.Dirty();
                // data.Save();

            }
            void closeNavbarSearch()
            {
                if (!curEvent.isLayout) return;
                if (t_VFolders == null) return;


                if (GUI.GetNameOfFocusedControl() == "navbar search field")
                    GUI.FocusControl(null);

                if (!wrappedBrowser.GetMemberValue<string>("m_SearchFieldText").IsNullOrEmpty())
                {
                    wrappedBrowser.SetMemberValue("m_SearchFieldText", "");
                    wrappedBrowser.GetMemberValue("m_SearchFilter").SetMemberValue("m_NameFilter", "");

                    wrappedBrowser.InvokeMethod("UpdateSearchDelayed");

                    GUIUtility.keyboardControl = 0;
                }


                // mouse input on vFavorites throws exceptions if search is active on vFolders navbar
                // here we close search to avoid this

            }

            void pageScroll()
            {
                if (!curEvent.isScroll) return;
                if (!VFavoritesMenu.pageScrollEnabled) return;



                var scrollDelta = curEvent.mouseDelta.y;

                if (scrollDelta == 0 && curEvent.holdingShift)
                    scrollDelta = curEvent.mouseDelta.x;

                if (scrollDelta < 0 && data.curPageIndex == 0) return;



                if (scrollDelta < 0)
                {
                    data.curPageIndex--;
                    prevPageButtonBrightness = 2;
                }

                if (scrollDelta > 0)
                {
                    data.curPageIndex++;
                    nextPageButtonBrightness = 2;

                }

                CancelDragging();
                CancelRowAnimations();

                curEvent.Use();

            }

            void background()
            {
                var color = isDarkTheme ? Greyscale(.2f) : Greyscale(.78f);

                totalRect_groupSpace.Draw(color);

            }
            void pages()
            {
                void page(Rect pageRect, Page page)
                {
                    void findSelectedItem()
                    {
                        if (!curEvent.isLayout) return;

                        foreach (var item in page.items)
                            item.isSelected = false;

                        if (draggingItem) return;
                        if (mousePresesdOnItem) return;
                        if (page.lastItemDragTime_ticks > page.lastItemSelectTime_ticks) return;

                        Item lastSelectedItem = null;

                        foreach (var item in page.items)
                        {
                            if (!item.isLoadable) continue;
                            if (lastSelectedItem?.lastSelectTime_ticks > item.lastSelectTime_ticks) continue;


                            var isSelected = false;

                            if (item.isFolder)
                            {
                                var targetBrowser = isWrappedBrowserLocked && isOneColumn ? allBrowsers.FirstOrDefault(r => !r.GetMemberValue<bool>("isLocked")) ?? wrappedBrowser
                                                                                          : wrappedBrowser;

                                if (targetBrowser.GetFieldValue<int>("m_ViewMode") == 1)
                                    isSelected = targetBrowser.InvokeMethod<string>("GetActiveFolderPath") == item.assetPath;
                                else
                                    isSelected = Selection.activeObject == item.obj;

                            }

                            if (!item.isFolder)
                                isSelected = Selection.activeObject == item.obj;


                            if (isSelected)
                                lastSelectedItem = item;

                        }

                        if (lastSelectedItem != null)
                            lastSelectedItem.isSelected = true;

                    }

                    void rows()
                    {
                        void row(float y, Item item)
                        {
                            var rowRect = pageRect.SetHeight(rowHeight).SetY(y).SetX(0);

                            var iconOffset = 6;
                            var iconSize = 25 * Mathf.Min(1, data.rowScale);
                            var nameOffset = 3;
                            var deletedOrNotLoadedLabelOffset = 1;

                            float highlightAmount = 0f;


                            void set_highlightAmount()
                            {
                                if (animatingDroppedItem && item == droppedItem)
                                    highlightAmount = droppedItemHighlightAmount;

                                if (item.isSelected)
                                    highlightAmount = 1;

                                if (draggingItem && item == draggedItem)
                                    highlightAmount = 1;

                                if (mousePresesdOnItem && item == pressedItem)
                                    highlightAmount = 1;

                            }

                            void shadow()
                            {
                                if (item != draggedItem && item != droppedItem) return;

                                var amount = item == droppedItem ? droppedItemShadowAmount : 1;

                                if (amount.Approx(0)) return;

                                rowRect.AddWidthFromMid(30).DrawBlurred(Greyscale(0, .55f * amount), 22);

                            }
                            void background()
                            {
                                var evenColor = isDarkTheme ? Greyscale(.249f) : Greyscale(.82f);
                                var oddColor = isDarkTheme ? Greyscale(.228f) : Greyscale(.85f);
                                var highlightedColor = isDarkTheme ? Greyscale(.335f) : Greyscale(.9f);

                                var rowColor = Lerp(evenColor, oddColor, rowRect.y.PingPong(rowHeight) / rowHeight);

                                Lerp(ref rowColor, highlightedColor, highlightAmount);

                                rowRect.Draw(rowColor);

                            }
                            void icon()
                            {
                                var iconRect = rowRect.MoveX(iconOffset).SetWidth(iconSize).SetHeightFromMid(iconSize);

                                if (item.isSceneGameObject)
                                    iconRect = iconRect.MoveX(1).Resize(.5f);

                                void asset()
                                {
                                    if (!item.isAsset) return;

                                    var iconTexture = item.isLoadable ? AssetPreview.GetAssetPreview(item.obj) ?? AssetPreview.GetMiniThumbnail(item.obj) : AssetPreview.GetMiniTypeThumbnail(item.type);

                                    GUI.DrawTexture(iconRect, iconTexture);

                                }
                                void sceneGameObject()
                                {
                                    if (!item.isSceneGameObject) return;

                                    void getIconNameFromAssetPreview()
                                    {
                                        if (!item.isLoadable) return;

                                        item.sceneGameObjectIconName = AssetPreview.GetMiniThumbnail(item.obj).name;

                                    }
                                    void getIconNameFromVHierarchy()
                                    {
                                        if (!item.isLoadable) return;
                                        if (!(item.obj is GameObject gameObject)) return;
                                        if (mi_VHierarchy_GetIconName == null) return;

                                        var iconNameFromVHierarchy = (string)mi_VHierarchy_GetIconName.Invoke(null, new object[] { gameObject });

                                        if (!iconNameFromVHierarchy.IsNullOrEmpty())
                                            item.sceneGameObjectIconName = iconNameFromVHierarchy;

                                    }

                                    getIconNameFromAssetPreview();
                                    getIconNameFromVHierarchy();

                                    var iconTexture = EditorGUIUtility.IconContent(item.sceneGameObjectIconName.IsNullOrEmpty() ? "GameObject icon" : item.sceneGameObjectIconName).image;

                                    GUI.DrawTexture(iconRect, iconTexture);

                                }
                                void folder()
                                {
                                    if (!item.isFolder) return;

                                    iconRect = iconRect.Resize(-1.5f);

                                    void drawNormal()
                                    {
                                        if (isDarkTheme)
                                            if (highlightAmount == 1) return;

                                        GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("Folder icon").image);

                                    }
                                    void drawHighlighted()
                                    {
                                        if (!isDarkTheme) return;
                                        if (highlightAmount != 1) return;

                                        SetGUIColor(Greyscale(.84f));

                                        GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("Folder On icon").image);

                                        ResetGUIColor();

                                    }
                                    void drawViaVFolders()
                                    {
                                        mi_VFolders_DrawBigFolderIcon?.Invoke(null, new object[] { iconRect, item.globalId.guid });
                                    }

                                    drawNormal();
                                    drawHighlighted();
                                    drawViaVFolders();

                                }

                                asset();
                                sceneGameObject();
                                folder();

                            }
                            void name()
                            {
                                var nameRect = rowRect.MoveX(iconOffset + iconSize + nameOffset).MoveY(-.5f).SetHeightFromMid(16);

                                void normal()
                                {
                                    if (isDarkTheme)
                                        if (highlightAmount == 1) return;

                                    GUI.Label(nameRect, item.name);

                                }
                                void highlighted()
                                {
                                    if (!isDarkTheme) return;
                                    if (highlightAmount != 1) return;
                                    if (!curEvent.isRepaint) return;

                                    SetGUIColor(Greyscale(.91f));

                                    GUI.skin.GetStyle("WhiteLabel").Draw(nameRect, item.name, false, false, false, false);

                                    ResetGUIColor();

                                }

                                normal();
                                highlighted();

                            }
                            void deletedOrNotLoaded()
                            {
                                var labelRect = rowRect.MoveX(iconOffset + iconSize + nameOffset + item.name.GetLabelWidth() + deletedOrNotLoadedLabelOffset).MoveY(.5f);

                                SetGUIEnabled(false);
                                SetLabelFontSize(10);

                                if (item.isDeleted)
                                    GUI.Label(labelRect, "Deleted");

                                else if (!item.isLoadable)
                                    GUI.Label(labelRect, "Not loaded");

                                ResetLabelStyle();
                                ResetGUIEnabled();

                            }
                            void crossButton()
                            {
                                if (!rowRect.IsHovered()) return;
                                if (draggingItem) return;
                                // if (mousePresesdOnItem) return; // idk

                                var buttonRect = rowRect.SetWidthFromRight(0).MoveX(-crossButtonOffsetFromRight).SetWidthFromMid(crossButtonSize);
                                var iconRect = buttonRect.SetSizeFromMid(16);

                                var normalColor = Greyscale(item.isSelected ? .48f : .4f);
                                var hoveredColor = isDarkTheme ? Greyscale(.8f) : normalColor;
                                var pressedColor = Greyscale(.6f);

                                SetGUIColor(buttonRect.IsHovered() ? (mousePressed ? pressedColor : hoveredColor) : normalColor);
                                GUI.Label(iconRect, EditorGUIUtility.IconContent("CrossIcon"));
                                ResetGUIColor();

                                buttonRect.MarkInteractive();


                                if (!mousePressedOnCrossButtonArea) return;
                                if (!curEvent.isMouseUp) return;
                                if (!buttonRect.IsHovered()) return;

                                CancelRowAnimations();
                                data.curPage.rowGaps[data.curPage.items.IndexOf(item)] = rowHeight;

                                data.curPage.items.Remove(item);

                                data.Dirty();
                                data.Save();

                                curEvent.Use();

                            }
                            void click()
                            {
                                if (!rowRect.IsHovered()) return;
                                if (!curEvent.isMouseUp) return;

                                curEvent.Use();

                                if (draggingItem) return;
                                if (mouseDragDistance > 2) return;
                                if (!item.isLoadable) return;

                                SelectItem(item);

                            }
                            void doubleclick()
                            {
                                if (!rowRect.IsHovered()) return;
                                if (!doubleclickUnhandled) return;

                                OpenItem(item);

                                doubleclickUnhandled = false;

                            }


                            rowRect.MarkInteractive();

                            set_highlightAmount();

                            shadow();
                            background();
                            icon();
                            name();
                            deletedOrNotLoaded();
                            crossButton();
                            click();
                            doubleclick();

                        }

                        void normalRow(int i)
                        {
                            Space(page.rowGaps[i]);
                            Space(rowHeight);

                            if (page.items[i] == droppedItem && animatingDroppedItem && page == data.curPage) return;

                            row(lastRect.y, page.items[i]);

                        }
                        void draggedRow()
                        {
                            if (!draggingItem) return;
                            if (page != data.curPage) return;


                            row(draggedItemY_rowsSpace, draggedItem);

                        }
                        void droppedRow()
                        {
                            if (!animatingDroppedItem) return;
                            if (page != data.curPage) return;

                            row(droppedItemY_rowsSpace, droppedItem);

                        }


                        if (curEvent.isRepaint && skipNextRepaint) { skipNextRepaint = false; return; }


                        GUILayout.BeginArea(pageRect);
                        page.scrollPos = EditorGUILayout.BeginScrollView(new Vector2(0, page.scrollPos), GUIStyle.none, GUIStyle.none).y;

                        for (int i = 0; i < page.items.Count; i++)
                            normalRow(i);

                        Space(page.rowGaps.Last());

                        Space(60);

                        draggedRow();
                        droppedRow();

                        EditorGUILayout.EndScrollView();
                        GUILayout.EndArea();


                    }
                    void curtains()
                    {
                        var height = 25;
                        var color = isDarkTheme ? Greyscale(.2f) : Greyscale(.78f);

                        pageRect.SetHeight(height).DrawCurtainDown(color.SetAlpha((page.scrollPos / 20).Smoothstep()));
                        pageRect.SetHeightFromBottom(height).DrawCurtainUp(color);

                    }
                    void tutor()
                    {
                        if (page.items.Any() || draggingItem) return;

                        SetGUIEnabled(false);
                        SetLabelFontSize(11);
                        SetLabelAlignmentCenter();

                        GUI.Label(pageRect.MoveY(-13), "Drop folders, assets");
                        GUI.Label(pageRect.MoveY(5), "or GameObjects");

                        ResetGUIEnabled();
                        ResetLabelStyle();
                    }


                    findSelectedItem();

                    rows();
                    curtains();
                    tutor();

                }

                var spaceBetweenPages = 10;


                if (pagesScrollPos == -1)
                    pagesScrollPos = data.curPageIndex;

                while (data.pages.Count <= pagesScrollPos.CeilToInt())
                    data.pages.Add(new Page("Page " + (data.pages.Count + 1)));


                for (int i = pagesScrollPos.FloorToInt(); i <= pagesScrollPos.CeilToInt(); i++)
                    page(totalRect_groupSpace.SetX((totalRect_groupSpace.width + spaceBetweenPages) * (i - pagesScrollPos)), data.pages[i]);

            }
            void widget()
            {
                var widthToAdd = 48;
                var height = 24;
                var distToBottom = 13;

                var color = isDarkTheme ? Greyscale(.1f) : Greyscale(.85f);
                var shadowSize = 10;
                var shadowAlpha = .23f;

                var chevronSize = 15;
                var chevronOffset = 12;
                var chevronBrightness = .5f;

                var textSize = 11;
                var textBrightness = .65f;

                widgetRect_groupSpace = totalRect_groupSpace.SetWidthFromMid(data.curPage.name.GetLabelWidth(textSize) + widthToAdd).SetHeightFromBottom(height).MoveY(-distToBottom);


                void shadow()
                {
                    widgetRect_groupSpace.Resize(1).DrawBlurred(Greyscale(0f, shadowAlpha), shadowSize);
                }
                void background()
                {
                    widgetRect_groupSpace.DrawWithRoundedCorners(color, 1223);
                }

                void nameLabel()
                {
                    if (renamingPage) return;


                    var buttonRect = widgetRect_groupSpace.SetWidthFromMid(data.curPage.name.GetLabelWidth(textSize) - 2);

                    buttonRect.MarkInteractive();


                    var activated = curEvent.isMouseUp && buttonRect.IsHovered();


                    var brightness = !buttonRect.IsHovered() ? textBrightness : (mousePressed ? .75f : 1);

                    SetGUIColor(Greyscale(brightness));
                    SetLabelAlignmentCenter();
                    SetLabelFontSize(textSize);
                    SetLabelBold();

                    GUI.Label(widgetRect_groupSpace, data.curPage.name);

                    ResetGUIColor();
                    ResetLabelStyle();


                    if (!activated) return;

                    renamingPage = true;
                    prevPageName = data.curPage.name;

                    curEvent.Use();

                }
                void leftButton()
                {
                    if (renamingPage) return;

                    var iconRect = widgetRect_groupSpace.SetWidth(chevronOffset * 2).SetSizeFromMid(chevronSize, chevronSize);
                    var buttonRect = Rect.zero.SetX(0).SetY(widgetRect_groupSpace.y).SetXMax(iconRect.xMax + 4).SetYMax(totalRect_groupSpace.yMax);

                    buttonRect.MarkInteractive();


                    var active = data.curPageIndex > 0;
                    var activated = curEvent.isMouseUp && buttonRect.IsHovered();


                    var brightness = prevPageButtonBrightness * (!buttonRect.IsHovered() || !active ? chevronBrightness : (mousePressed ? .75f : 1));

                    SetGUIColor(Greyscale(brightness));

                    GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("NodeChevronLeft@2x").image);

                    ResetGUIColor();



                    if (!activated) return;

                    curEvent.Use();



                    if (!active) return;

                    CancelDragging();
                    CancelRowAnimations();

                    data.curPageIndex--;


                }
                void rightButton()
                {
                    if (renamingPage) return;

                    var iconRect = widgetRect_groupSpace.SetWidthFromRight(chevronOffset * 2).SetSizeFromMid(chevronSize, chevronSize);
                    var buttonRect = Rect.zero.SetX(iconRect.x - 6).SetY(widgetRect_groupSpace.y).SetXMax(totalRect_groupSpace.xMax).SetYMax(totalRect_groupSpace.yMax);

                    buttonRect.MarkInteractive();


                    var active = true;// data.curPageIndex < data.pages.Count - 1 && data.curPage.items.Any();
                    var activated = curEvent.isMouseUp && buttonRect.IsHovered();


                    var brightness = nextPageButtonBrightness * (!buttonRect.IsHovered() || !active ? chevronBrightness : (mousePressed ? .75f : 1));

                    SetGUIColor(Greyscale(brightness));

                    GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("NodeChevronRight@2x").image);

                    ResetGUIColor();



                    if (!activated) return;

                    curEvent.Use();



                    if (!active) return;

                    CancelDragging();
                    CancelRowAnimations();

                    data.curPageIndex++;

                }

                void nameTextField()
                {
                    if (!renamingPage) return;

                    var textFieldRect = widgetRect_groupSpace.AddHeightFromMid(-2).SetWidthFromMid(data.curPage.name.GetLabelWidth(textSize) + 4);

                    var s = new GUIStyle(GUI.skin.textField);
                    s.alignment = TextAnchor.MiddleCenter;
                    s.fontSize = 11;

                    EditorGUIUtility.AddCursorRect(textFieldRect, MouseCursor.CustomCursor);

                    GUI.SetNextControlName("asdasdasd");
                    EditorGUI.FocusTextInControl("asdasdasd");


                    data.curPage.name = EditorGUI.TextField(textFieldRect, data.curPage.name, s);


                    if (data.curPage.name.IsNullOrEmpty())
                        data.curPage.name = "Page " + (data.curPageIndex + 1);

                    EditorGUIUtility.AddCursorRect(textFieldRect, MouseCursor.CustomCursor);



                }
                void cancelRename_button()
                {
                    if (!renamingPage) return;

                    var iconRect = widgetRect_groupSpace.SetWidth(chevronOffset * 2).SetSizeFromMid(chevronSize - 2);
                    var buttonRect = iconRect.SetSizeFromMid(25, 25);

                    buttonRect.MarkInteractive();


                    var brightness = !buttonRect.IsHovered() ? chevronBrightness : (mousePressed ? .75f : 1);

                    SetGUIColor(Greyscale(brightness));

                    GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("CrossIcon").image);

                    ResetGUIColor();



                    if (!curEvent.isMouseUp) return;
                    if (!buttonRect.IsHovered()) return;

                    curEvent.Use();

                    data.curPage.name = prevPageName;

                    renamingPage = false;

                }
                void acceptRename_button()
                {
                    if (!renamingPage) return;

                    var iconRect = widgetRect_groupSpace.SetWidthFromRight(chevronOffset * 2).SetSizeFromMid(chevronSize - 0);
                    var buttonRect = iconRect.SetSizeFromMid(25, 25);

                    buttonRect.MarkInteractive();


                    var brightness = !buttonRect.IsHovered() ? chevronBrightness : (mousePressed ? .75f : 1);

                    SetGUIColor(Greyscale(brightness));

                    GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("check").image);

                    ResetGUIColor();



                    if (!curEvent.isMouseUp) return;
                    if (!buttonRect.IsHovered()) return;

                    curEvent.Use();

                    renamingPage = false;

                    data.Dirty();
                    data.Save();

                }
                void acceptRename_enterKey()
                {
                    if (!renamingPage) return;
                    if (curEvent.keyCode != KeyCode.Return) return;

                    renamingPage = false;

                    data.Dirty();
                    data.Save();

                }
                void cancelRename_escapeKey()
                {
                    if (!renamingPage) return;
                    if (curEvent.keyCode != KeyCode.Escape) return;

                    data.curPage.name = prevPageName;

                    renamingPage = false;

                }


                shadow();
                background();

                leftButton();
                rightButton();
                nameLabel();

                nameTextField();

                cancelRename_button();
                acceptRename_button();

                acceptRename_enterKey();
                cancelRename_escapeKey();

            }
            void keys()
            {
                if (isWrappedBrowserLocked && !totalRect_browserSpace.IsHovered()) return;

                void prevPage()
                {
                    if (!curEvent.isKeyDown) return;
                    if (curEvent.keyCode != KeyCode.LeftArrow) return;
                    if (!VFavoritesMenu.arrowKeysEnabled) return;
                    if (data.curPageIndex == 0) return;

                    CancelDragging();
                    CancelRowAnimations();

                    data.curPageIndex--;

                    prevPageButtonBrightness = 2;

                    curEvent.Use();

                }
                void nextPage()
                {
                    if (curEvent.keyCode != KeyCode.RightArrow) return;
                    if (!curEvent.isKeyDown) return;
                    if (!VFavoritesMenu.arrowKeysEnabled) return;

                    CancelDragging();
                    CancelRowAnimations();

                    data.curPageIndex++;

                    nextPageButtonBrightness = 2;

                    curEvent.Use();

                }
                void selectPrev()
                {
                    if (!curEvent.isKeyDown) return;
                    if (curEvent.keyCode != KeyCode.UpArrow) return;
                    if (!VFavoritesMenu.arrowKeysEnabled) return;

                    var iToSelect = data.curPage.items.IndexOfFirst(r => r.isSelected) - 1;

                    if (iToSelect < 0)
                        iToSelect = data.curPage.items.LastIndex();

                    if (iToSelect.IsInRangeOf(data.curPage.items))
                        SelectItem(data.curPage.items[iToSelect]);

                    curEvent.Use();

                }
                void selectNext()
                {
                    if (!curEvent.isKeyDown) return;
                    if (curEvent.keyCode != KeyCode.DownArrow) return;
                    if (!VFavoritesMenu.arrowKeysEnabled) return;

                    var iToSelect = data.curPage.items.IndexOfFirst(r => r.isSelected) + 1;

                    if (iToSelect >= data.curPage.items.Count)
                        iToSelect = 0;

                    if (iToSelect.IsInRangeOf(data.curPage.items))
                        SelectItem(data.curPage.items[iToSelect]);

                    curEvent.Use();

                }
                void numberKeys()
                {
                    if (!curEvent.isKeyDown) return;
                    if (!VFavoritesMenu.numberKeysEnabled) return;
                    if (EditorGUIUtility.editingTextField) return;


                    var i = ((int)curEvent.keyCode - 48);

                    if (i == 0) i = 10;

                    if (!i.IsInRange(1, 10)) return;


                    data.curPageIndex = i - 1;

                    curEvent.Use();


                }

                prevPage();
                nextPage();
                selectPrev();
                selectNext();
                numberKeys();

            }

            void doOriginalGUIFirst_()
            {
                originalBrowserGUI();

                GUI.BeginGroup(totalRect_browserSpace);
                GUI.color = GUI.color.SetAlpha(currentOpacity);

                background();
                pages();
                widget();

                GUI.color = GUI.color.SetAlpha(1);
                GUI.EndGroup();

            }
            void doVFavoritesGUIFirst_()
            {
                keys();
                pageScroll();

                GUI.BeginGroup(totalRect_browserSpace);

                widget();
                pages();

                GUI.EndGroup();

                if (totalRect_browserSpace.IsHovered())
                    if (curEvent.isMouseUp || curEvent.isMouseDrag || curEvent.isScroll) // prevents these events from reaching original gui
                        curEvent.Use();

                originalBrowserGUI();

            }

            void originalBrowserGUI()
            {
                if (origBrowserOnGUIDelegate.GetMethodInfo().DeclaringType.Name.Contains("VTabs")) return;

                if (isOneColumn && currentOpacity.Approx(1)) // to optimize locked one-column browser functioning as a dedicated favorites window
                    if (originalGUICalledOnce) // needs to be called once to init stuff so pinging object won't throw exceptions
                        return;



                if (origBrowserOnGUIDelegate.GetMethodInfo().IsStatic) // wrapped by vFolders 
                    origBrowserOnGUIDelegate.GetMethodInfo().Invoke(null, new[] { wrappedBrowser });
                else
                    origBrowserOnGUIDelegate.GetMethodInfo().Invoke(wrappedBrowser, null);

                originalGUICalledOnce = true;

            }




            createData();
            closeNavbarSearch();

            UpdateMouseState();
            UpdateDragging();


            var doOriginalGUIFirst = VFavoritesMenu.pageScrollEnabled ? curEvent.isRepaint || curEvent.isLayout
                                                                      : curEvent.isRepaint;

            if (renamingPage)
                doOriginalGUIFirst = !curEvent.isMouseDown && !curEvent.isMouseUp;

            if (renamingPage && curEvent.isMouseDown)
                curEvent.Use();


            if (doOriginalGUIFirst)
                doOriginalGUIFirst_();
            else
                doVFavoritesGUIFirst_();


            if (isWrappedBrowserLocked)
                if (!totalRect_browserSpace.IsHovered() && !animatingDroppedItem && !animatingPageScroll) return;

            if (animatingOpacity || animatingDroppedItem || animatingPageScroll || animatingRowGaps)
                wrappedBrowser.Repaint();

        }

        static void SelectItem(Item item)
        {
            void openFolder(EditorWindow browser, string path)
            {
                var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(path);

                if (browser.GetFieldValue<int>("m_ViewMode") == 1)
                    browser.InvokeMethod("SetFolderSelection", new[] { folderAsset.GetInstanceID() }, false);
                else
                {
                    Selection.activeObject = folderAsset;

                    browser.GetMemberValue("m_AssetTree")?.GetPropertyValue("data")?.InvokeMethod("SetExpanded", folderAsset.GetInstanceID(), true);

                }

            }

            void selectSceneObject()
            {
                if (!item.isSceneGameObject) return;

                Selection.activeObject = item.obj;

            }
            void selectAsset_twoColumns()
            {
                if (!item.isAsset) return;
                if (wrappedBrowser.GetFieldValue<int>("m_ViewMode") != 1) return;

                openFolder(wrappedBrowser, item.assetPath.GetParentPath());

                Selection.activeObject = item.obj;

            }
            void selectAsset_oneColumn()
            {
                if (!item.isAsset) return;
                if (wrappedBrowser.GetFieldValue<int>("m_ViewMode") != 0) return;

                Selection.activeObject = item.obj;

            }
            void openFolder_unlocked()
            {
                if (!item.isFolder) return;
                if (isWrappedBrowserLocked) return;

                openFolder(wrappedBrowser, item.assetPath);

            }
            void openFolder_locked()
            {
                if (!item.isFolder) return;
                if (!isWrappedBrowserLocked) return;

                var unlockedBrowser = allBrowsers.FirstOrDefault(r => !r.GetMemberValue<bool>("isLocked"));
                var browserToUse = isOneColumn ? unlockedBrowser : lockedBrowser;

                if (!browserToUse) return;

                openFolder(browserToUse, item.assetPath);

            }

            selectSceneObject();
            selectAsset_twoColumns();
            selectAsset_oneColumn();
            openFolder_unlocked();
            openFolder_locked();

            item.lastSelectTime_ticks = data.curPage.lastItemSelectTime_ticks = System.DateTime.UtcNow.Ticks;

        }
        static void OpenItem(Item item)
        {
            void openText()
            {
                if (item.assetPath.GetExtension() != ".cs"
                 && item.assetPath.GetExtension() != ".shader"
                 && item.assetPath.GetExtension() != ".compute"
                 && item.assetPath.GetExtension() != ".cginc"
                 && item.assetPath.GetExtension() != ".json") return;


                AssetDatabase.OpenAsset(item.globalId.guid.LoadGuid());

            }
            void openPrefab()
            {
                if (item.type != typeof(GameObject)) return;
                if (!item.isLoadable) return;
                if ((item.obj as GameObject).scene.rootCount != 0) return;

                AssetDatabase.OpenAsset(item.obj);

            }
            void openScene()
            {
                if (item.type != typeof(SceneAsset)) return;

                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(item.assetPath);

            }
            void openSceneForNotLoadedGameObject()
            {
                if (!item.isSceneGameObject) return;
                if (item.isLoadable) return;
                if (item.isDeleted) return;

                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(item.assetPath);

                Selection.activeObject = item.obj;

            }

            openText();
            openPrefab();
            openScene();
            openSceneForNotLoadedGameObject();

        }

        static float rowHeight => 44 * data.rowScale;

        static float crossButtonOffsetFromRight = 23;
        static float crossButtonSize = 16;

        static Rect totalRect_browserSpace
        {
            get
            {
                if (isOneColumn)
                    return wrappedBrowser.position.SetPos(0, 0);


                var treeViewRect = wrappedBrowser.GetFieldValue<Rect>("m_TreeViewRect");

                return wrappedBrowser.position.SetPos(0, 0).SetWidth(treeViewRect.width).SetHeightFromBottom(treeViewRect.height);

            }
        }
        static Rect totalRect_groupSpace => totalRect_browserSpace.SetPos(0, 0);

        static Rect widgetRect_browserSpace => widgetRect_groupSpace.MoveY(totalRect_browserSpace.y);
        static Rect widgetRect_groupSpace;

        static bool renamingPage;
        static string prevPageName;

        static int lastNumberKeyPressedIndex;
        static long lastNumberKeyPressedTime_ticks;

        static bool isOneColumn => wrappedBrowser?.GetFieldValue<int>("m_ViewMode") == 0;

        static bool skipNextRepaint;
        static bool originalGUICalledOnce;







        static void UpdateMouseState() // called from WrappedOnGUI 
        {
            if (!shortcutPressed && !isWrappedBrowserLocked) { setDefaultState(); return; }
            if (!totalRect_browserSpace.IsHovered()) { setDefaultState(); return; }

            void setDefaultState()
            {
                mousePressed = false;
                mousePressedOnCrossButtonArea = false;
                mousePressedOnWidget = false;
                pressedItem = null;
                doubleclickUnhandled = false;

            }

            void position()
            {
                mousePosiion_browserSpace = curEvent.mousePosition;
            }
            void down()
            {
                if (!curEvent.isMouseDown) return;

                mousePressed = true;
                mousePressedOnCrossButtonArea = totalRect_browserSpace.SetWidthFromRight(0).MoveX(-crossButtonOffsetFromRight).SetWidthFromMid(crossButtonSize).IsHovered();
                mousePressedOnWidget = curEvent.mousePosition.y >= widgetRect_browserSpace.y;

                mouseDownPosiion_browserSpace = curEvent.mousePosition;

                var pressedItemIndex = (mouseDownPosition_rowsSpace.y / rowHeight).FloorToInt();
                if (pressedItemIndex.IsInRangeOf(data.curPage.items) && !mousePressedOnCrossButtonArea && !mousePressedOnWidget)
                    pressedItem = data.curPage.items[pressedItemIndex];

                doubleclickUnhandled = !mousePressedOnCrossButtonArea && curEvent.clickCount == 2;

                curEvent.Use();

            }
            void up()
            {
                if (!curEvent.isMouseUp) return;

                mousePressed = false;
                doubleclickUnhandled = false;
                pressedItem = null;

            }

            position();
            down();
            up();

        }

        static bool mousePressed;
        static bool mousePresesdOnItem => pressedItem != null;
        static bool mousePressedOnCrossButtonArea;
        static bool mousePressedOnWidget;
        static bool doubleclickUnhandled;

        static float groupRectOffsetY => isOneColumn ? 0 : -20;

        static Vector2 mousePosiion_browserSpace;
        static Vector2 mousePosition_groupSpace => mousePosiion_browserSpace.AddY(groupRectOffsetY);
        static Vector2 mousePosition_rowsSpace => mousePosiion_browserSpace.AddY(groupRectOffsetY + data.curPage.scrollPos);

        static Vector2 mouseDownPosiion_browserSpace;
        static Vector2 mouseDownPosition_groupSpace => mouseDownPosiion_browserSpace.AddY(groupRectOffsetY);
        static Vector2 mouseDownPosition_rowsSpace => mouseDownPosiion_browserSpace.AddY(groupRectOffsetY + data.curPage.scrollPos);

        static float mouseDragDistance => (mousePosiion_browserSpace - mouseDownPosiion_browserSpace).magnitude;


        static Item pressedItem;






        static void UpdateAnimations() // Update
        {
            void calcDeltaTime()
            {
                deltaTime = (float)(EditorApplication.timeSinceStartup - lastLayoutTime);

                if (deltaTime > .05f)
                    deltaTime = .0166f;

                lastLayoutTime = EditorApplication.timeSinceStartup;

            }
            void opacity()
            {
                if (!VFavoritesMenu.fadeAnimationsEnabled) { currentOpacity = targetOpacity; return; }
                if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) { currentOpacity = targetOpacity; return; }

                SmoothDamp(ref currentOpacity, targetOpacity, 11, ref currentOpacityDerivative, deltaTime);

                if (targetOpacity == 0 && currentOpacity < .04f)
                    currentOpacity = 0;

            }
            void pagesScroll()
            {
                if (!data) return;
                if (!VFavoritesMenu.pageScrollAnimationEnabled) { pagesScrollPos = data.curPageIndex; return; }

                if (pagesScrollPos == -1)
                    pagesScrollPos = data.curPageIndex;

                SmoothDamp(ref pagesScrollPos, data.curPageIndex, 5, ref pagesScrollDerivative, deltaTime);

                if (pagesScrollPos.DistanceTo(data.curPageIndex) < .001f)
                    pagesScrollPos = data.curPageIndex;

            }
            void rowGaps()
            {
                if (!data) return;

                var lerpSpeed = 10;

                for (int i = 0; i < data.curPage.rowGaps.Count; i++)
                    data.curPage.rowGaps[i] = Lerp(data.curPage.rowGaps[i], draggingItem && i == insertDraggedItemAtIndex ? rowHeight : 0, lerpSpeed, deltaTime);


            }
            void droppedItem()
            {
                if (!animatingDroppedItem) return;

                var yLerpSpeed = 8;
                var shadowLerpSpeed = 8;
                var highlightLerpSpeed = 10;

                SmoothDamp(ref droppedItemY_rowsSpace, data.curPage.items.IndexOf(VFavorites.droppedItem) * rowHeight, yLerpSpeed, ref droppedItemYDerivative, deltaTime);
                Lerp(ref droppedItemShadowAmount, 0, shadowLerpSpeed, deltaTime);
                Lerp(ref droppedItemHighlightAmount, 0, highlightLerpSpeed, deltaTime);

                if (droppedItemShadowAmount < .01f)
                    animatingDroppedItem = false;

            }
            void pageButtons()
            {
                if (!VFavoritesMenu.pageScrollAnimationEnabled) { prevPageButtonBrightness = nextPageButtonBrightness = 1; return; }

                var lerpSpeed = 7;

                Lerp(ref prevPageButtonBrightness, 1, lerpSpeed, deltaTime);
                Lerp(ref nextPageButtonBrightness, 1, lerpSpeed, deltaTime);


            }

            calcDeltaTime();
            opacity();
            pagesScroll();
            rowGaps();
            droppedItem();
            pageButtons();

        }

        static void CancelRowAnimations()
        {
            for (int i = 0; i < data.curPage.rowGaps.Count; i++)
                data.curPage.rowGaps[i] = 0;

            animatingDroppedItem = false;
            droppedItem = null;

        }

        static float deltaTime;
        static double lastLayoutTime;

        static bool animatingRowGaps => data.curPage.rowGaps.Any(r => r > .1f && r < rowHeight - .1f);

        static float pagesScrollPos = -1;
        static float pagesScrollDerivative;
        static bool animatingPageScroll => !pagesScrollPos.Approx(data ? data.curPageIndex : 0);

        static float droppedItemY_rowsSpace;
        static float droppedItemYDerivative;
        static float droppedItemShadowAmount;
        static float droppedItemHighlightAmount;
        static bool animatingDroppedItem;

        static float prevPageButtonBrightness = 1;
        static float nextPageButtonBrightness = 1;

        static float currentOpacity;
        static float currentOpacityDerivative;
        static float targetOpacity => curEvent.holdingAlt || renamingPage || draggingItemFromPageToOutside || isWrappedBrowserLocked ? 1 : 0; // holdingAlt instead of shortcutPressed to prevent unwrapping due to incorrect event modifiers on key down on mac
        static bool animatingOpacity => currentOpacity.DistanceTo(targetOpacity) > .01f;





        static void UpdateDragging() // called from WrappedOnGUI 
        {
            void initFromOutside()
            {
                if (draggingItem) return;
                if (!totalRect_browserSpace.IsHovered()) return;
                if (!curEvent.isDragUpdate) return;
                if (!DragAndDrop.objectReferences.FirstOrDefault()) return;
                if (draggingItemFromPageToOutside) return; // to avoid duplication when dragging item from page to outside and back


                animatingDroppedItem = false;

                draggingItem = true;
                draggingItemFromPage = false;

                draggedItem = new Item(DragAndDrop.objectReferences.FirstOrDefault());
                draggedItemHoldOffset = 0;

                data.curPage.lastItemDragTime_ticks = System.DateTime.UtcNow.Ticks;

            }
            void initFromPage()
            {
                if (draggingItem) return;
                if (!totalRect_browserSpace.IsHovered()) return;
                if (!curEvent.isMouseDrag) return;
                if (mouseDragDistance < 2) return;


                var i = (mouseDownPosition_rowsSpace.y / rowHeight).FloorToInt();

                if (i >= data.curPage.items.Count) return;
                if (i < 0) return; // somehow i = -1 with mouseDownPosition_rowsSpace.y = -20 when dragging in from outside quickly


                animatingDroppedItem = false;

                draggingItem = true;
                draggingItemFromPage = true;
                draggingItemFromPageAtIndex = i;

                draggedItem = data.curPage.items[i];
                draggedItemHoldOffset = (i * rowHeight + rowHeight / 2) - mouseDownPosition_rowsSpace.y;

                data.curPage.lastItemDragTime_ticks = System.DateTime.UtcNow.Ticks;

                data.curPage.items.Remove(draggedItem);
                data.curPage.rowGaps[draggingItemFromPageAtIndex] = rowHeight;

                data.Dirty();
                data.Save();

            }

            void acceptFromOutside()
            {
                if (!draggingItem) return;
                if (!curEvent.isDragPerform) return;

                DragAndDrop.AcceptDrag();
                curEvent.Use();

                AcceptDragging();

            }
            void acceptFromPage()
            {
                if (!draggingItem) return;
                if (!curEvent.isMouseUp) return;

                curEvent.Use();

                AcceptDragging();
            }

            void cancelFromOutside()
            {
                if (!draggingItemFromOutside) return;
                if (totalRect_browserSpace.IsHovered()) return;

                CancelDragging();

            }
            void cancelFromPageAndInitToOutside()
            {
                if (!curEvent.isMouseDrag) return;
                if (!draggingItemFromPage) return;
                if (totalRect_browserSpace.IsHovered()) return;
                if (DragAndDrop.objectReferences.Any()) return;


                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { draggedItem.obj };
                DragAndDrop.StartDrag(draggedItem.name);

                CancelDragging();

                draggingItemFromPageToOutside = true;

            }

            void setVisualMode()
            {
                if (!draggingItem) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

            }
            void setHotControl()
            {
                if (!draggingItem) return;

                EditorGUIUtility.hotControl = EditorGUIUtility.GetControlID(FocusType.Passive);

            }

            void resetDraggingItemFromPageToOutside() // delayCall loop
            {
                if (!DragAndDrop.objectReferences.Any())
                    draggingItemFromPageToOutside = false;

                EditorApplication.delayCall -= resetDraggingItemFromPageToOutside;
                EditorApplication.delayCall += resetDraggingItemFromPageToOutside;

                // DragAndDrop.objectReferences is unreliable outside of delayCall

            }


            initFromOutside();
            initFromPage();

            acceptFromOutside();
            acceptFromPage();

            cancelFromOutside();
            cancelFromPageAndInitToOutside();

            setVisualMode();
            setHotControl();

            EditorApplication.delayCall -= resetDraggingItemFromPageToOutside;
            EditorApplication.delayCall += resetDraggingItemFromPageToOutside;

        }

        static void AcceptDragging()
        {
            draggingItem = false;
            draggingItemFromPage = false;
            mousePressed = false;

            data.curPage.items.AddAt(draggedItem, insertDraggedItemAtIndex);

            data.curPage.rowGaps[insertDraggedItemAtIndex] -= rowHeight;
            data.curPage.rowGaps.AddAt(0, insertDraggedItemAtIndex);

            droppedItem = draggedItem;

            droppedItemY_rowsSpace = draggedItemY_groupSpace + data.curPage.scrollPos;
            droppedItemYDerivative = 0;
            droppedItemShadowAmount = droppedItemHighlightAmount = 1;
            animatingDroppedItem = true;

            draggedItem = null;

            EditorGUIUtility.hotControl = 0;

            data.Dirty();
            data.Save();

        }
        static void CancelDragging()
        {
            if (!draggingItem) return;

            draggingItem = false;
            mousePressed = false;


            if (!draggingItemFromPage) { draggedItem = null; EditorGUIUtility.hotControl = 0; return; }

            data.curPage.items.AddAt(draggedItem, draggingItemFromPageAtIndex);

            data.curPage.rowGaps[draggingItemFromPageAtIndex] -= rowHeight;
            droppedItem = draggedItem;
            droppedItemY_rowsSpace = draggedItemY_groupSpace - data.curPage.scrollPos;
            droppedItemShadowAmount = droppedItemHighlightAmount = 1;
            animatingDroppedItem = true;

            draggingItemFromPage = false;

            draggedItem = null;

            EditorGUIUtility.hotControl = 0;

            data.Dirty();
            data.Save();

        }

        static bool draggingItem;
        static bool draggingItemFromPage;
        static bool draggingItemFromPageToOutside;
        static bool draggingItemFromOutside => draggingItem && !draggingItemFromPage;
        static int draggingItemFromPageAtIndex;
        static Item draggedItem;
        static float draggedItemHoldOffset;
        static float draggedItemY_groupSpace => (mousePosition_groupSpace.y - rowHeight / 2 + draggedItemHoldOffset).Clamp(0, 12321);
        static float draggedItemY_rowsSpace => draggedItemY_groupSpace + data.curPage.scrollPos;
        static int insertDraggedItemAtIndex => ((mousePosition_rowsSpace.y + draggedItemHoldOffset) / rowHeight).FloorToInt().Clamp(0, data.curPage.items.Count);

        static Item droppedItem;














        static void UpdateGUIWrapping() // called from EditorApplicaton.update 
        {
            void wrap()
            {
                if (wrappedBrowser) return;
                if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) return;
                if (!shortcutPressed) return;
                if (EditorWindow.mouseOverWindow?.GetType() != t_BrowserWindow) return;
                if (t_VTabs != null && !EditorPrefsCached.GetBool("vTabs-pluginDisabled", false) && EditorWindow.mouseOverWindow.GetMemberValue<bool>("isLocked")) return;


                WrapBrowserGUI(EditorWindow.mouseOverWindow);

                wrappedBrowser = EditorWindow.mouseOverWindow;

                wrappedBrowser.Focus();
                wrappedBrowser.Repaint();

                t_BrowserWindow.SetFieldValue("s_LastInteractedProjectBrowser", wrappedBrowser); // so vTabs can copy its layout setting


                wrappedBrowserWasLockedBeforeWrapping = wrappedBrowser.GetMemberValue<bool>("isLocked");

            }
            void unwrap()
            {
                if (!wrappedBrowser) return;
                if (shortcutPressed && wrappedBrowser.hasFocus) return;
                if (currentOpacity > 0 && wrappedBrowser.hasFocus) return;


                CancelDragging();
                CancelRowAnimations();

                UnwrapBrowserGUI();

                wrappedBrowser.Repaint();

                wrappedBrowser = null;

            }

            unwrap();
            wrap();

        }

        static void WrapBrowserGUI(EditorWindow browser)
        {
            var hostView = fi_m_Parent.GetValue(browser);
            var newDelegate = mi_WrappedOnGUI.CreateDelegate(t_EditorWindowDelegate, hostView);

            origBrowserOnGUIDelegate = fi_m_OnGUI.GetValue(hostView) as System.Delegate;
            fi_m_OnGUI.SetValue(hostView, newDelegate);

        }
        static void UnwrapBrowserGUI()
        {
            if (wrappedBrowser.GetFieldValue("m_Parent").GetFieldValue<System.Delegate>("m_OnGUI").Method != mi_WrappedOnGUI) return;

            wrappedBrowser.GetFieldValue("m_Parent").SetFieldValue("m_OnGUI", origBrowserOnGUIDelegate);

        }

        static EditorWindow wrappedBrowser;
        static System.Delegate origBrowserOnGUIDelegate;

        static bool wrappedBrowserWasLockedBeforeWrapping;

        static bool shortcutPressed
        {
            get
            {
                if (VFavoritesMenu.activeOnAltEnabled)
                    return curEvent.holdingAlt;

                if (VFavoritesMenu.activeOnAltShiftEnabled)
                    return curEvent.modifiers == (EventModifiers.Alt | EventModifiers.Shift);

                if (VFavoritesMenu.activeOnCtrlAltEnabled)
                    if (Application.platform == RuntimePlatform.OSXEditor)
                        return curEvent.modifiers == (EventModifiers.Command | EventModifiers.Alt);
                    else
                        return curEvent.modifiers == (EventModifiers.Control | EventModifiers.Alt);

                return false;
            }
        }






        static void UpdateLocking() // called from EditorApplicaton.update 
        {
            void unsetWrappedBrowser()
            {
                if (!isWrappedBrowserLocked) return;

                if (wrappedBrowser.GetFieldValue("m_Parent").GetFieldValue<System.Delegate>("m_OnGUI").Method == mi_WrappedOnGUI) return;

                wrappedBrowser = null;

                // nulls wrappedBrowser if it was unwrapped externally
                // ie when locked browser gets moved or maximized

            }
            void markLockedBrowser()
            {
                if (!lockedBrowser) return;

                MarkAsLocked(lockedBrowser);

                // fixes marking getting reset on scene load

            }
            void setMinWidthOnLockedBrowser()
            {
                if (!lockedBrowser) return;
                if (lockedBrowser.minSize.x == 100) return;

                lockedBrowser.minSize = Vector2.one * 100;

            }
            void setTitle()
            {

                if (!lockedBrowser) return;
                if (lockedBrowser.GetFieldValue<int>("m_ViewMode") != 0) return; // one column)
                if (lockedBrowser.titleContent.text == "vFavorites") return;

                var icon = EditorIcons.GetIcon("Favorite");

                lockedBrowser.titleContent = new GUIContent("vFavorites", icon);

            }

            void lock_()
            {
                if (lockedBrowser) return;
                if (!wrappedBrowser) return;
                if (!wrappedBrowser.GetMemberValue<bool>("isLocked")) return;
                if (wrappedBrowserWasLockedBeforeWrapping) return;

                lockedBrowser = wrappedBrowser;

                EditorPrefsCached.SetInt("vFavorites-lockedBrowserHash", lockedBrowser.GetHashCode());
                EditorPrefsCached.SetInt("vFavorites-lockedBrowserDockAreaInstanceId", lockedBrowser.GetMemberValue<Object>("m_Parent").GetInstanceID());

                curEvent.Use();

            }
            void unlock()
            {
                if (!lockedBrowser) return;
                if (lockedBrowser.GetMemberValue<bool>("isLocked")) return;


                lockedBrowser.titleContent = lockedBrowser.InvokeMethod<GUIContent>("GetLocalizedTitleContent");

                lockedBrowser = null;

                EditorPrefsCached.SetInt("vFavorites-lockedBrowserHash", 0);
                EditorPrefsCached.SetInt("vFavorites-lockedBrowserDockAreaInstanceId", 0);

                curEvent.Use();

            }
            void wrap()
            {
                if (!lockedBrowser) return;
                if (!lockedBrowser.hasFocus) return;
                if (isWrappedBrowserLocked) return;

                WrapBrowserGUI(lockedBrowser);

                currentOpacity = 1;

                wrappedBrowser = lockedBrowser;

            }


            unsetWrappedBrowser();
            markLockedBrowser();
            setMinWidthOnLockedBrowser();
            setTitle();

            lock_();
            unlock();
            wrap();

        }

        static EditorWindow lockedBrowser
        {
            get
            {
                if (_lockedBrowser) return _lockedBrowser;


                var lockedBrowserInstanceId = EditorPrefsCached.GetInt("vFavorites-lockedBrowserInstanceId", 0);

                if (lockedBrowserInstanceId == 0) return null;



                var window = Resources.InstanceIDToObject(lockedBrowserInstanceId) as EditorWindow;

                if (window && window.GetType() == t_BrowserWindow) // prevents iid collisions
                    _lockedBrowser = window;

                if (_lockedBrowser) return _lockedBrowser;



                _lockedBrowser = allBrowsers.FirstOrDefault(r => IsMarkedAsLocked(r));

                if (_lockedBrowser) return _lockedBrowser; // todo set instanceid



                // EditorPrefsCached.SetInt("vFavorites-lockedBrowserInstanceId", 0); // one attempt to find the locked browser isn't enough after unmaximize

                return null;

            }
            set
            {
                if (_lockedBrowser)
                    MarkAsUnlocked(_lockedBrowser);

                if (value == null)
                {
                    EditorPrefsCached.SetInt("vFavorites-lockedBrowserInstanceId", 0);

                    _lockedBrowser = null;

                }
                else
                {
                    EditorPrefsCached.SetInt("vFavorites-lockedBrowserInstanceId", value.GetInstanceID());

                    MarkAsLocked(value);

                    _lockedBrowser = value;

                }

            }
        }
        static EditorWindow _lockedBrowser;

        static bool IsMarkedAsLocked(EditorWindow browser) => browser.GetMemberValue("m_SearchFilter")?.GetMemberValue<string>("m_OriginalText") == "asd";
        static void MarkAsLocked(EditorWindow browser) => browser.GetMemberValue("m_SearchFilter")?.SetMemberValue("m_OriginalText", "asd");
        static void MarkAsUnlocked(EditorWindow browser) => browser.GetMemberValue("m_SearchFilter")?.SetMemberValue("m_OriginalText", "");

        static bool isWrappedBrowserLocked => wrappedBrowser && wrappedBrowser == lockedBrowser;

        static bool CanBrowserBeWrapped_byVTabs(EditorWindow browser) => !IsMarkedAsLocked(browser);




        static void RepaintOnAltUp() // Update 
        {
            var lastEvent = typeof(Event).GetFieldValue<Event>("s_Current");

            if (wasAlt && !lastEvent.alt)
                if (EditorWindow.mouseOverWindow?.GetType() == t_BrowserWindow)
                    EditorApplication.RepaintProjectWindow();

            wasAlt = lastEvent.alt;

        }

        static bool wasAlt;












        [InitializeOnLoadMethod]
        static void Init()
        {
            if (VFavoritesMenu.pluginDisabled) return;

            void subscribe()
            {
                EditorApplication.update -= UpdateGUIWrapping;
                EditorApplication.update += UpdateGUIWrapping;

                EditorApplication.update -= UpdateLocking;
                EditorApplication.update += UpdateLocking;

                EditorApplication.update -= UpdateAnimations;
                EditorApplication.update += UpdateAnimations;

                EditorApplication.update -= RepaintOnAltUp;
                EditorApplication.update += RepaintOnAltUp;



                EditorApplication.quitting -= VFavoritesState.Save;
                EditorApplication.quitting += VFavoritesState.Save;

            }
            void loadData()
            {
                data = AssetDatabase.LoadAssetAtPath<VFavoritesData>(EditorPrefsCached.GetString("vFavorites-lastKnownDataPath-" + GetProjectId()));


                if (data) return;

                data = AssetDatabase.FindAssets("t:VFavoritesData").Select(guid => AssetDatabase.LoadAssetAtPath<VFavoritesData>(guid.ToPath())).FirstOrDefault();


                if (!data) return;

                EditorPrefsCached.SetString("vFavorites-lastKnownDataPath-" + GetProjectId(), data.GetPath());

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

            subscribe();
            loadData();
            loadDataDelayed();

        }

        public static VFavoritesData data;







        static void BeforeWindowCreated_byVTabs(object dockArea)
        {
            if (!wrappedBrowser) return;
            if (wrappedBrowser.GetFieldValue("m_Parent") != dockArea) return;

            CancelDragging();
            CancelRowAnimations();

            UnwrapBrowserGUI();

            draggingItemFromPageToOutside = false;

        }






        static IEnumerable<EditorWindow> allBrowsers => _allBrowsers ??= t_BrowserWindow.GetFieldValue<IList>("s_ProjectBrowsers").Cast<EditorWindow>();
        static IEnumerable<EditorWindow> _allBrowsers;

        static Type t_BrowserWindow = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
        static Type t_HostView = typeof(Editor).Assembly.GetType("UnityEditor.HostView");
        static Type t_EditorWindowDelegate = t_HostView.GetNestedType("EditorWindowDelegate", maxBindingFlags);
        static FieldInfo fi_m_Parent = typeof(EditorWindow).GetField("m_Parent", maxBindingFlags);
        static FieldInfo fi_m_OnGUI = t_HostView.GetField("m_OnGUI", maxBindingFlags);
        static MethodInfo mi_WrappedOnGUI = typeof(VFavorites).GetMethod(nameof(WrappedOnGUI), maxBindingFlags);


        static Type t_VHierarchy = Type.GetType("VHierarchy.VHierarchy") ?? Type.GetType("VHierarchy.VHierarchy, VHierarchy, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        static Type t_VFolders = Type.GetType("VFolders.VFolders") ?? Type.GetType("VFolders.VFolders, VFolders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        static Type t_VTabs = Type.GetType("VTabs.VTabs") ?? Type.GetType("VTabs.VTabs, VTabs, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

        static MethodInfo mi_VHierarchy_GetIconName = t_VHierarchy?.GetMethod("GetIconName_forVFavorites", maxBindingFlags);
        static MethodInfo mi_VFolders_DrawBigFolderIcon = t_VFolders?.GetMethod("DrawBigFolderIcon_forVFavorites", maxBindingFlags);





        const string version = "2.0.11";

    }
}
#endif
