#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.Linq;
using Type = System.Type;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
// using static VTools.VDebug;
using static VInspector.VInspector;
using static VInspector.VInspectorData;



namespace VInspector
{
    public class VInspectorNavbar
    {

        public void OnGUI(Rect navbarRect)
        {
            void shadow()
            {
                if (!curEvent.isRepaint) return;

                var shadowLength = 30;
                var shadowPos = 21;
                var shadowGreyscale = isDarkTheme ? .08f : .28f;
                var shadowAlpha = isDarkTheme ? .35f : .25f;

                var minScrollPos = 10;
                var maxScrollPos = 20;


                var scrollPos = window.GetMemberValue<UnityEngine.UIElements.ScrollView>("m_ScrollView").scrollOffset.y;

                var opacity = ((scrollPos - minScrollPos) / (maxScrollPos - minScrollPos)).Clamp01();


                navbarRect.MoveY(shadowPos).SetHeight(shadowLength).DrawCurtainDown(Greyscale(shadowGreyscale, shadowAlpha * opacity));

            }
            void background()
            {
                if (!curEvent.isRepaint) return;

                var backgroundColor = Greyscale(isDarkTheme ? .235f : .8f);
                var lineColor = Greyscale(isDarkTheme ? .1f : .53f);

                navbarRect.Draw(backgroundColor);

                navbarRect.SetHeightFromBottom(1).MoveY(1).Draw(lineColor);

            }
            void hiddenMenu()
            {
                if (!curEvent.holdingAlt) return;
                if (!curEvent.isMouseUp) return;
                if (curEvent.mouseButton != 1) return;
                if (!navbarRect.IsHovered()) return;


                var menu = new GenericMenu();

                menu.AddDisabledItem(new GUIContent("vInspector hidden menu"));

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Select data"), false, () => Selection.activeObject = data);
                menu.AddItem(new GUIContent("Disable attributes"), VInspectorMenu.attributesDisabled, () => VInspectorMenu.attributesDisabled = !VInspectorMenu.attributesDisabled);

                menu.ShowAsContext();

            }


            void name()
            {
                if (!curEvent.isRepaint) return;

                var nameRect = navbarRect.MoveX(30 * 2 + 1);

                var minScrollPos = 10;
                var maxScrollPos = 20;


                var scrollPos = window.GetMemberValue<UnityEngine.UIElements.ScrollView>("m_ScrollView").scrollOffset.y;

                var opacity = (scrollPos - minScrollPos) / (maxScrollPos - minScrollPos);



                SetGUIColor(Greyscale(.95f, opacity));
                SetLabelBold();

                GUI.Label(nameRect, Selection.activeObject?.name ?? "No selection");

                ResetLabelStyle();
                ResetGUIColor();


            }
            void nameCurtain()
            {
                if (!curEvent.isRepaint) return;
                if (!data) return;
                if (lastBookmarkX == default) return;

                var backgroundColor = Greyscale(isDarkTheme ? .235f : .8f);

                var curtainRect = navbarRect.AddHeightFromMid(-4).SetXMax(lastBookmarkX).SetWidthFromRight(20);
                var maskRect = navbarRect.AddHeightFromMid(-4).SetX(lastBookmarkX).SetWidth(123);

                curtainRect.DrawCurtainLeft(backgroundColor);
                maskRect.Draw(backgroundColor);


            }

            void moveBackButton()
            {
                var buttonRect = navbarRect.SetWidth(30).MoveX(4);

                if (Application.unityVersion.StartsWith("6000"))
                    buttonRect = buttonRect.MoveY(-.49f);


                var iconName = "Chevron Left";
                var iconSize = 14;
                var colorNormal = Greyscale(isDarkTheme ? .75f : .2f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .5f);
                var colorDisabled = Greyscale(isDarkTheme ? .53f : .55f);


                var disabled = !VInspectorSelectionHistory.instance.prevStates.Any();

                if (disabled) { IconButton(buttonRect, iconName, iconSize, colorDisabled, colorDisabled, colorDisabled); return; }


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                VInspectorSelectionHistory.instance.MoveBack();

            }
            void moveForwardButton()
            {
                var buttonRect = navbarRect.SetWidth(30).MoveX(30).MoveX(1).AddWidthFromMid(-6);

                if (Application.unityVersion.StartsWith("6000"))
                    buttonRect = buttonRect.MoveY(-.49f);


                var iconName = "Chevron Right";
                var iconSize = 14;
                var colorNormal = Greyscale(isDarkTheme ? .75f : .2f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .5f);
                var colorDisabled = Greyscale(isDarkTheme ? .53f : .55f);


                var disabled = !VInspectorSelectionHistory.instance.nextStates.Any();

                if (disabled) { IconButton(buttonRect, iconName, iconSize, colorDisabled, colorDisabled, colorDisabled); return; }


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                VInspectorSelectionHistory.instance.MoveForward();

            }

            void bookmarks()
            {
                void createData()
                {
                    if (data) return;
                    if (!navbarRect.IsHovered()) return;
                    if (!DragAndDrop.objectReferences.Any()) return;

                    data = ScriptableObject.CreateInstance<VInspectorData>();

                    AssetDatabase.CreateAsset(data, GetScriptPath("VInspector").GetParentPath().CombinePath("vInspector Data.asset"));

                }
                void repaintOnUndoRedo()
                {
                    if (!data) return;
                    if (curEvent.commandName != "UndoRedoPerformed") return;
                    if (!repaintNeededAfterUndoRedo) return;

                    window.Repaint();

                    repaintNeededAfterUndoRedo = false;

                }
                void gui()
                {
                    if (!data) return;

                    this.navbarRect = navbarRect;
                    this.bookmarksRect = navbarRect.AddWidth(-5).AddWidthFromRight(-60);

                    BookmarksGUI();

                }

                createData();
                repaintOnUndoRedo();
                gui();

            }



            shadow();
            background();
            hiddenMenu();

            name();
            nameCurtain();

            moveBackButton();
            moveForwardButton();

            bookmarks();


            if (draggingBookmark || animatingDroppedBookmark || animatingGaps || animatingTooltip)
                window.Repaint();

        }

        Rect navbarRect;
        Rect bookmarksRect;










        void BookmarksGUI()
        {
            void bookmark(Vector2 centerPosition, Bookmark bookmark)
            {
                if (bookmark == null) return;
                if (curEvent.isLayout) return;


                var bookmarkRect = Rect.zero.SetSize(bookmarkWidth, bookmarksRect.height).SetMidPos(centerPosition);


                void shadow()
                {
                    if (!draggingBookmark) return;
                    if (draggedBookmark != bookmark) return;

                    bookmarkRect.SetSizeFromMid(bookmarkWidth - 4, bookmarkWidth - 4).DrawBlurred(Greyscale(0, .3f), 15);

                }
                void background()
                {
                    if (!bookmarkRect.IsHovered()) return;
                    if (draggingBookmark && draggedBookmark != bookmark) return;

                    var backgroundColor = Greyscale(isDarkTheme ? .35f : .7f);

                    var backgroundRect = bookmarkRect.SetSizeFromMid(bookmarkRect.width - 2, bookmarkWidth - 2);

                    backgroundRect.DrawRounded(backgroundColor, 4);


                }
                void icon()
                {
                    var opacity = 1f;
                    var iconTexture = default(Texture);

                    void set_opacity()
                    {
                        var opacityNormal = .9f;
                        var opacityHovered = 1f;
                        var opacityPressed = .75f;
                        var opacityDragged = .75f;
                        var opacityDisabled = .4f;

                        var isDisabled = !bookmark.isLoadable;


                        opacity = opacityNormal;

                        if (draggingBookmark)
                            opacity = bookmark == draggedBookmark ? opacityDragged : opacityNormal;

                        else if (bookmark == pressedBookmark)
                            opacity = opacityPressed;

                        else if (bookmarkRect.IsHovered())
                            opacity = opacityHovered;

                        if (isDisabled)
                            opacity = opacityDisabled;

                    }
                    void getTexture()
                    {
                        if (bookmark.obj is Material)
                            iconTexture = AssetPreview.GetAssetPreview(bookmark.obj) ?? AssetPreview.GetMiniThumbnail(bookmark.obj);

                        else if (bookmark.isAsset)
                            iconTexture = AssetPreview.GetMiniThumbnail(bookmark.obj);

                        else if (bookmark.isSceneGameObject)
                            if (mi_VHierarchy_GetIconName?.Invoke(null, new object[] { bookmark.obj }) is string iconNameFromVHierarchy && !iconNameFromVHierarchy.IsNullOrEmpty())
                                iconTexture = EditorIcons.GetIcon(iconNameFromVHierarchy);
                            else
                                iconTexture = AssetPreview.GetMiniThumbnail(bookmark.obj);

                    }
                    void drawTexture()
                    {
                        if (!iconTexture) return;


                        SetGUIColor(Greyscale(1, opacity));

                        GUI.DrawTexture(bookmarkRect.SetSizeFromMid(iconSize), iconTexture);

                        ResetGUIColor();

                    }

                    set_opacity();
                    getTexture();
                    drawTexture();

                }
                void selectedIndicator()
                {
                    if (Selection.activeObject != bookmark.obj) return;
                    if (Selection.activeObject == null) return;
                    if (draggingBookmark && draggedBookmark == bookmark && centerPosition.y != bookmarksRect.center.y) return;

                    var indicatorColor = Greyscale(isDarkTheme ? .75f : 1f);

                    var rect = bookmarkRect.SetHeightFromBottom(3).MoveY(2).SetWidthFromMid(3);

                    rect.DrawRounded(indicatorColor, 1);

                }
                void tooltip()
                {
                    if (bookmark != (draggingBookmark ? (draggedBookmark) : (lastHoveredBookmark))) return;
                    if (tooltipOpacity == 0) return;

                    var fontSize = 11;
                    var tooltipText = bookmark.name;

                    Rect tooltipRect;

                    void set_tooltipRect()
                    {
                        var width = tooltipText.GetLabelWidth(fontSize) + 6;
                        var height = 16 + (fontSize - 12) * 2;

                        var yOffset = 28;
                        var rightMargin = -1;


                        tooltipRect = Rect.zero.SetMidPos(centerPosition.x, centerPosition.y + yOffset).SetSizeFromMid(width, height);


                        var maxXMax = bookmarksRect.xMax - rightMargin;

                        if (tooltipRect.xMax > maxXMax)
                            tooltipRect = tooltipRect.MoveX(maxXMax - tooltipRect.xMax);

                    }
                    void shadow()
                    {
                        var shadowAmount = .33f;
                        var shadowRadius = 10;

                        tooltipRect.DrawBlurred(Greyscale(0, shadowAmount).MultiplyAlpha(tooltipOpacity), shadowRadius);

                    }
                    void background()
                    {
                        var cornerRadius = 5;

                        var backgroundColor = Greyscale(isDarkTheme ? .13f : .9f);
                        var outerEdgeColor = Greyscale(isDarkTheme ? .25f : .6f);
                        var innerEdgeColor = Greyscale(isDarkTheme ? .0f : .95f);

                        tooltipRect.Resize(-1).DrawRounded(outerEdgeColor.SetAlpha(tooltipOpacity.Pow(2)), cornerRadius + 1);
                        tooltipRect.Resize(0).DrawRounded(innerEdgeColor.SetAlpha(tooltipOpacity.Pow(2)), cornerRadius + 0);
                        tooltipRect.Resize(1).DrawRounded(backgroundColor.SetAlpha(tooltipOpacity), cornerRadius - 1);

                    }
                    void text()
                    {
                        var textRect = tooltipRect.MoveY(-.5f);

                        var textColor = Greyscale(1f);

                        SetLabelAlignmentCenter();
                        SetLabelFontSize(fontSize);
                        SetGUIColor(textColor.SetAlpha(tooltipOpacity));

                        GUI.Label(textRect, tooltipText);

                        ResetLabelStyle();
                        ResetGUIColor();

                    }

                    set_tooltipRect();
                    shadow();
                    background();
                    text();

                }
                void click()
                {
                    if (!bookmarkRect.IsHovered()) return;
                    if (!curEvent.isMouseUp) return;
                    if (doubleclickUnhandled) return;

                    curEvent.Use();


                    if (draggingBookmark) return;
                    if ((curEvent.mousePosition - mouseDownPosiion).magnitude > 2) return;
                    if (!bookmark.isLoadable) return;

                    bookmark.obj.SelectInInspector(frameInHierarchy: false, frameInProject: false);

                    lastClickedBookmark = bookmark;

                    hideTooltip = true;

                }
                void doubleclick()
                {
                    if (!bookmarkRect.IsHovered()) return;
                    if (!curEvent.isMouseUp) return;
                    if (!doubleclickUnhandled) return;

                    void frameSceneGO()
                    {
                        if (!bookmark.isLoadable) return;
                        if (!bookmark.isSceneGameObject) return;
                        if (bookmark.obj is not GameObject go) return;


                        var sv = SceneView.lastActiveSceneView;

                        if (!sv || !sv.hasFocus)
                            sv = SceneView.sceneViews.ToArray().FirstOrDefault(r => (r as SceneView).hasFocus) as SceneView;

                        if (!sv)
                            (sv = SceneView.lastActiveSceneView ?? SceneView.sceneViews[0] as SceneView).Focus();

                        sv.Frame(go.GetBounds(), false);

                    }
                    void loadSceneAndSelect()
                    {
                        if (!bookmark.isSceneGameObject) return;
                        if (bookmark.isLoadable) return;
                        if (bookmark.isDeleted) return;
                        if (Application.isPlaying) return;

                        EditorSceneManager.SaveOpenScenes();
                        EditorSceneManager.OpenScene(bookmark.assetPath);

                        Selection.activeObject = bookmark.obj;

                    }
                    void openPrefab()
                    {
                        if (!bookmark.isLoadable) return;
                        if (bookmark.obj is not GameObject gameObject) return;
                        if (!AssetDatabase.Contains(gameObject)) return;

                        AssetDatabase.OpenAsset(gameObject);

                    }


                    EditorGUIUtility.PingObject(bookmark.obj);

                    frameSceneGO();
                    loadSceneAndSelect();
                    openPrefab();

                    doubleclickUnhandled = false;

                }


                bookmarkRect.MarkInteractive();

                shadow();
                background();
                icon();
                selectedIndicator();
                tooltip();
                click();
                doubleclick();

            }

            void normalBookmark(int i, float centerX)
            {
                if (data.bookmarks[i] == droppedBookmark && animatingDroppedBookmark) return;

                var centerY = bookmarksRect.height / 2;


                var minX = centerX - bookmarkWidth / 2;

                if (minX < bookmarksRect.x) return;

                lastBookmarkX = minX;


                bookmark(new Vector2(centerX, centerY), data.bookmarks[i]);

            }
            void normalBookmarks()
            {
                var curCenterX = bookmarksRect.xMax - bookmarkWidth / 2;

                for (int i = 0; i < data.bookmarks.Count; i++)
                {
                    curCenterX -= gaps[i];

                    if (!data.bookmarks[i].obj) continue;


                    normalBookmark(i, curCenterX);


                    curCenterX -= bookmarkWidth;

                }

            }
            void draggedBookmark_()
            {
                if (!draggingBookmark) return;

                var centerX = curEvent.mousePosition.x + draggedBookmarkHoldOffset.x;
                var centerY = bookmarksRect.IsHovered() ? bookmarksRect.height / 2 : curEvent.mousePosition.y;

                bookmark(new Vector2(centerX, centerY), draggedBookmark);

            }
            void droppedBookmark_()
            {
                if (!animatingDroppedBookmark) return;

                var centerX = droppedBookmarkX;
                var centerY = bookmarksRect.height / 2;

                bookmark(new Vector2(centerX, centerY), droppedBookmark);

            }


            BookmarksMouseState();
            BookmarksDragging();
            BookmarksAnimations();

            normalBookmarks();
            draggedBookmark_();
            droppedBookmark_();

        }

        float bookmarkWidth => 24;
        float iconSize => 16;

        float lastBookmarkX;

        static bool repaintNeededAfterUndoRedo;



        int GetBookmarkIndex(float mouseX)
        {
            var curBookmarkWidthSum = 0f;

            for (int i = 0; i < data.bookmarks.Count; i++)
            {
                if (!data.bookmarks[i].obj) continue;

                curBookmarkWidthSum += bookmarkWidth;

                if (bookmarksRect.xMax - curBookmarkWidthSum < mouseX + .5f)
                    return i;
            }

            return data.bookmarks.IndexOfLast(r => r.obj) + 1;

        }

        float GetBookmarkCenterX(int i, bool includeGaps = true)
        {
            return bookmarksRect.xMax
                 - bookmarkWidth / 2
                 - data.bookmarks.Take(i).Sum(r => r.obj ? bookmarkWidth : 0)
                 - (includeGaps ? gaps.Take(i + 1).Sum() : 0);

        }








        void BookmarksMouseState()
        {
            void down()
            {
                if (!curEvent.isMouseDown) return;

                mousePressed = true;

                mouseDownPosiion = curEvent.mousePosition;

                var pressedBookmarkIndex = GetBookmarkIndex(mouseDownPosiion.x);

                if (pressedBookmarkIndex.IsInRangeOf(data.bookmarks))
                    pressedBookmark = data.bookmarks[pressedBookmarkIndex];

                doubleclickUnhandled = curEvent.clickCount == 2;

                curEvent.Use();

            }
            void up()
            {
                if (!curEvent.isMouseUp) return;

                mousePressed = false;
                pressedBookmark = null;

            }
            void hover()
            {
                var hoveredBookmarkIndex = GetBookmarkIndex(curEvent.mousePosition.x);

                mouseHoversBookmark = bookmarksRect.IsHovered() && hoveredBookmarkIndex.IsInRangeOf(data.bookmarks);

                if (mouseHoversBookmark)
                    lastHoveredBookmark = data.bookmarks[hoveredBookmarkIndex];


            }

            down();
            up();
            hover();

        }

        bool mouseHoversBookmark;
        bool mousePressed;
        bool doubleclickUnhandled;

        Vector2 mouseDownPosiion;

        Bookmark pressedBookmark;
        Bookmark lastHoveredBookmark;






        void BookmarksDragging()
        {
            void initFromOutside()
            {
                if (draggingBookmark) return;
                if (!bookmarksRect.IsHovered()) return;
                if (!curEvent.isDragUpdate) return;
                if (DragAndDrop.objectReferences.FirstOrDefault() is not Object draggedObject) return;

                if (draggedObject is DefaultAsset) return;
                if (draggedObject is Component) return;
                if (draggedObject is GameObject go && StageUtility.GetCurrentStage() is PrefabStage && !AssetDatabase.Contains(go)) return;

                animatingDroppedBookmark = false;

                draggingBookmark = true;
                draggingBookmarkFromInside = false;

                draggedBookmark = new Bookmark(draggedObject);
                draggedBookmarkHoldOffset = Vector2.zero;

            }
            void initFromInside()
            {
                if (draggingBookmark) return;
                if (!mousePressed) return;
                if ((curEvent.mousePosition - mouseDownPosiion).magnitude <= 2) return;
                if (pressedBookmark == null) return;

                var i = GetBookmarkIndex(mouseDownPosiion.x);

                if (i >= data.bookmarks.Count) return;
                if (i < 0) return;


                animatingDroppedBookmark = false;

                draggingBookmark = true;
                draggingBookmarkFromInside = true;

                draggedBookmark = data.bookmarks[i];
                draggedBookmarkHoldOffset = new Vector2(GetBookmarkCenterX(i) - mouseDownPosiion.x, bookmarksRect.center.y - mouseDownPosiion.y);

                gaps[i] = bookmarkWidth;


                data.RecordUndo();

                data.bookmarks.Remove(draggedBookmark);

            }

            void acceptFromOutside()
            {
                if (!draggingBookmark) return;
                if (!curEvent.isDragPerform) return;
                if (!bookmarksRect.IsHovered()) return;

                DragAndDrop.AcceptDrag();
                curEvent.Use();

                data.RecordUndo();

                accept();

                data.Dirty();

            }
            void acceptFromInside()
            {
                if (!draggingBookmark) return;
                if (!curEvent.isMouseUp) return;
                if (!bookmarksRect.IsHovered()) return;

                curEvent.Use();
                EditorGUIUtility.hotControl = 0;

                DragAndDrop.PrepareStartDrag(); // fixes phantom dragged component indicator after reordering bookmarks

                data.RecordUndo();
                data.Dirty();

                accept();

            }
            void accept()
            {
                draggingBookmark = false;
                draggingBookmarkFromInside = false;
                mousePressed = false;

                data.bookmarks.AddAt(draggedBookmark, insertDraggedBookmarkAtIndex);

                gaps[insertDraggedBookmarkAtIndex] -= bookmarkWidth;
                gaps.AddAt(0, insertDraggedBookmarkAtIndex);

                droppedBookmark = draggedBookmark;

                droppedBookmarkX = curEvent.mousePosition.x + draggedBookmarkHoldOffset.x;
                droppedBookmarkXDerivative = 0;
                animatingDroppedBookmark = true;

                draggedBookmark = null;

                EditorGUIUtility.hotControl = 0;

                repaintNeededAfterUndoRedo = true;

            }

            void cancelFromOutside()
            {
                if (!draggingBookmark) return;
                if (draggingBookmarkFromInside) return;
                if (bookmarksRect.IsHovered()) return;

                draggingBookmark = false;
                mousePressed = false;

            }
            void cancelFromInsideAndDelete()
            {
                if (!draggingBookmark) return;
                if (!curEvent.isMouseUp) return;
                if (bookmarksRect.IsHovered()) return;

                draggingBookmark = false;

                DragAndDrop.PrepareStartDrag(); // fixes phantom dragged component indicator after reordering bookmarks

                data.Dirty();

                repaintNeededAfterUndoRedo = true;

            }

            void update()
            {
                if (!draggingBookmark) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                if (draggingBookmarkFromInside) // otherwise it breaks vTabs dragndrop
                    EditorGUIUtility.hotControl = EditorGUIUtility.GetControlID(FocusType.Passive);



                insertDraggedBookmarkAtIndex = GetBookmarkIndex(curEvent.mousePosition.x + draggedBookmarkHoldOffset.x);

            }


            initFromOutside();
            initFromInside();

            acceptFromOutside();
            acceptFromInside();

            cancelFromOutside();
            cancelFromInsideAndDelete();

            update();


        }

        bool draggingBookmark;
        bool draggingBookmarkFromInside;

        int insertDraggedBookmarkAtIndex;

        Vector2 draggedBookmarkHoldOffset;

        Bookmark draggedBookmark;
        Bookmark droppedBookmark;






        void BookmarksAnimations()
        {
            if (!curEvent.isLayout) return;

            void gaps_()
            {
                var makeSpaceForDraggedBookmark = draggingBookmark && bookmarksRect.IsHovered();

                var lerpSpeed = 12;

                for (int i = 0; i < gaps.Count; i++)
                    if (makeSpaceForDraggedBookmark && i == insertDraggedBookmarkAtIndex)
                        gaps[i] = MathUtil.Lerp(gaps[i], bookmarkWidth, lerpSpeed, editorDeltaTime);
                    else
                        gaps[i] = MathUtil.Lerp(gaps[i], 0, lerpSpeed, editorDeltaTime);



                for (int i = 0; i < gaps.Count; i++)
                    if (gaps[i].Approx(0))
                        gaps[i] = 0;



                animatingGaps = gaps.Any(r => r > .1f);


            }
            void droppedBookmark_()
            {
                if (!animatingDroppedBookmark) return;

                var lerpSpeed = 8;

                var targX = GetBookmarkCenterX(data.bookmarks.IndexOf(droppedBookmark), includeGaps: true);

                MathUtil.SmoothDamp(ref droppedBookmarkX, targX, lerpSpeed, ref droppedBookmarkXDerivative, editorDeltaTime);

                if ((droppedBookmarkX - targX).Abs() < .5f)
                    animatingDroppedBookmark = false;

            }
            void tooltip()
            {
                if (!mouseHoversBookmark || lastHoveredBookmark != lastClickedBookmark)
                    hideTooltip = false;


                var lerpSpeed = UnityEditorInternal.InternalEditorUtility.isApplicationActive ? 15 : 12321;

                if (mouseHoversBookmark && !draggingBookmark && !hideTooltip)
                    MathUtil.SmoothDamp(ref tooltipOpacity, 1, lerpSpeed, ref tooltipOpacityDerivative, editorDeltaTime);
                else
                    MathUtil.SmoothDamp(ref tooltipOpacity, 0, lerpSpeed, ref tooltipOpacityDerivative, editorDeltaTime);


                if (tooltipOpacity > .99f)
                    tooltipOpacity = 1;

                if (tooltipOpacity < .01f)
                    tooltipOpacity = 0;


                animatingTooltip = tooltipOpacity != 0 && tooltipOpacity != 1;

            }

            gaps_();
            droppedBookmark_();
            tooltip();

        }

        float droppedBookmarkX;
        float droppedBookmarkXDerivative;

        float tooltipOpacity;
        float tooltipOpacityDerivative;

        bool animatingDroppedBookmark;
        bool animatingGaps;
        bool animatingTooltip;

        bool hideTooltip;

        List<float> gaps
        {
            get
            {
                while (_gaps.Count < data.bookmarks.Count + 1) _gaps.Add(0);
                while (_gaps.Count > data.bookmarks.Count + 1) _gaps.RemoveLast();

                return _gaps;

            }
        }
        List<float> _gaps = new();

        Bookmark lastClickedBookmark;













        public VInspectorNavbar(EditorWindow window) => this.window = window;

        public EditorWindow window;

    }
}
#endif