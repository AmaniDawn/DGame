#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Type = System.Type;
using static VFolders.VFolders;
using static VFolders.VFoldersData;
using static VFolders.VFoldersPalette;
using static VFolders.VFoldersCache;
using static VFolders.Libs.VUtils;
using static VFolders.Libs.VGUI;
// using static VTools.VDebug;


namespace VFolders
{
    public class VFoldersPaletteWindow : EditorWindow
    {

        void OnGUI()
        {
            if (!palette) { Close(); return; }

            int hoveredColorIndex = -1;
            string hoveredIconNameOrGuid = null;

            void background()
            {
                position.SetPos(0, 0).Draw(windowBackground);
            }
            void outline()
            {
                if (Application.platform == RuntimePlatform.OSXEditor) return;

                position.SetPos(0, 0).DrawOutline(Greyscale(.1f));

            }
            void colors()
            {
                if (!palette.colorsEnabled) return;

                var rowRect = this.position.SetPos(paddingX, paddingY).SetHeight(cellSize);


                void color(int i)
                {
                    var cellRect = rowRect.MoveX(i * cellSize).SetWidth(cellSize).SetHeightFromMid(cellSize);

                    void backgroundSelected()
                    {
                        if (!colorIndexes_initial.Contains(i)) return;

                        cellRect.Resize(1).DrawRounded(selectedBackground, 2);

                    }
                    void backgroundHovered()
                    {
                        if (!cellRect.IsHovered()) return;

                        cellRect.Resize(1).DrawRounded(this.hoveredBackground, 2);

                    }
                    void crossIcon()
                    {
                        if (i != 0) return;

                        GUI.DrawTexture(cellRect.SetSizeFromMid(iconSize), EditorIcons.GetIcon("CrossIcon"));

                    }
                    void colorOutline()
                    {
                        if (i == 0) return;

                        var outlineColor = i <= VFoldersPalette.greyColorsCount ? Greyscale(.0f, .4f) : Greyscale(.15f, .2f);

                        cellRect.Resize(3).DrawRounded(outlineColor, 4);

                    }
                    void color()
                    {
                        if (i == 0) return;

                        var brightness = palette.colorBrightness;
                        var saturation = palette.colorSaturation;
                        var drawGradients = palette.colorGradientsEnabled;

                        if (!palette.colorGradientsEnabled)
                            brightness *= isDarkTheme ? .75f : .97f;

                        if (i <= VFoldersPalette.greyColorsCount)
                        {
                            saturation = brightness = 1;
                            drawGradients = false;
                        }


                        var colorRaw = palette ? palette.colors[i - 1] : VFoldersPalette.GetDefaultColor(i - 1);

                        var color = MathUtil.Lerp(Greyscale(.2f), colorRaw, brightness);

                        Color.RGBToHSV(color, out float h, out float s, out float v);
                        color = Color.HSVToRGB(h, s * saturation, v);

                        color = MathUtil.Lerp(color, colorRaw, .5f).SetAlpha(1);




                        cellRect.Resize(4).DrawRounded(color, 3);

                        if (drawGradients)
                            cellRect.Resize(4).AddWidthFromRight(-2).DrawCurtainLeft(GUIColors.windowBackground.SetAlpha(.45f));

                    }
                    void recursiveIndicator()
                    {
                        if (!curEvent.isRepaint) return;


                        var isRecursive = folderDatas.First().colorIndex == i && folderDatas.First().isColorRecursive;

                        if (!isRecursive) return;



                        var iconRect = cellRect.SetSizeFromMid(16).Move(-6, -7);
                        var shadowRect = iconRect.Resize(3).Move(2, 1).AddWidthFromRight(3);
                        var shadowRadius = 4;

                        shadowRect.DrawBlurred(GUIColors.windowBackground, shadowRadius);


                        SetGUIColor(Color.white * 2);

                        GUI.DrawTexture(iconRect, EditorIcons.GetIcon("UnityEditor.SceneHierarchyWindow@2x"));

                        ResetGUIColor();


                    }

                    void setHovered()
                    {
                        if (!cellRect.IsHovered()) return;

                        hoveredColorIndex = i;

                    }
                    void closeOnClick()
                    {
                        if (!cellRect.IsHovered()) return;
                        if (!curEvent.isMouseUp) return;

                        curEvent.Use();

                        Close();

                    }



                    cellRect.MarkInteractive();

                    backgroundSelected();
                    backgroundHovered();
                    crossIcon();
                    colorOutline();
                    color();
                    recursiveIndicator();

                    setHovered();
                    closeOnClick();

                }


                for (int i = 0; i < palette.colors.Count + 1; i++)
                    color(i);

            }
            void icons()
            {
                void row(int i, IconRow iconRow)
                {
                    var rowRect = this.position.SetPos(paddingX, paddingY).SetHeight(cellSize).MoveY(palette.colorsEnabled ? cellSize + spaceAfterColors : 0).MoveY(i * (cellSize + rowSpacing));

                    var isFirstEnabledRow = palette.iconRows.First(r => r.enabled) == iconRow;


                    void icon(int i)
                    {
                        var cellRect = rowRect.MoveX(i * cellSize).SetWidth(cellSize).SetHeightFromMid(cellSize);

                        var isCrossIcon = isFirstEnabledRow && i == 0;
                        var actualIconIndex = isFirstEnabledRow ? i - 1 : i;
                        var isCustomIcon = !isCrossIcon && actualIconIndex >= iconRow.builtinIcons.Count;
                        var iconNameOrGuid = isCrossIcon ? "" : isCustomIcon ? iconRow.customIcons[actualIconIndex - iconRow.builtinIcons.Count] : iconRow.builtinIcons[actualIconIndex];


                        void backgroundSelected()
                        {
                            if (!iconNamesOrGuids_initial.Contains(iconNameOrGuid)) return;

                            cellRect.Resize(1).DrawRounded(selectedBackground, 2);

                        }
                        void backgroundHovered()
                        {
                            if (!cellRect.IsHovered()) return;

                            cellRect.Resize(1).DrawRounded(this.hoveredBackground, 2);

                        }
                        void crossIcon()
                        {
                            if (!isCrossIcon) return;

                            GUI.DrawTexture(cellRect.SetSizeFromMid(iconSize), EditorIcons.GetIcon("CrossIcon"));

                        }
                        void normalIcon()
                        {
                            if (isCrossIcon) return;

                            var iconNameOrPath = iconNameOrGuid?.Length == 32 ? iconNameOrGuid.ToPath() : iconNameOrGuid;
                            var icon = EditorIcons.GetIcon(iconNameOrPath) ?? Texture2D.blackTexture;

                            var iconRect = cellRect.SetSizeFromMid(iconSize);

                            if (icon.width < icon.height) iconRect = iconRect.SetWidthFromMid(iconRect.height * icon.width / icon.height);
                            if (icon.height < icon.width) iconRect = iconRect.SetHeightFromMid(iconRect.width * icon.height / icon.width);


                            GUI.DrawTexture(iconRect, icon);

                        }
                        void recursiveIndicator()
                        {
                            if (!curEvent.isRepaint) return;


                            var isRecursive = folderDatas.First().iconNameOrGuid == iconNameOrGuid && folderDatas.First().isIconRecursive;

                            if (!isRecursive) return;



                            var iconRect = cellRect.SetSizeFromMid(16).Move(-6, -7);
                            var shadowRect = iconRect.Resize(3).Move(2, 1).AddWidthFromRight(3);
                            var shadowRadius = 4;

                            shadowRect.DrawBlurred(GUIColors.windowBackground, shadowRadius);



                            SetGUIColor(Color.white * 2);

                            GUI.DrawTexture(iconRect, EditorIcons.GetIcon("UnityEditor.SceneHierarchyWindow@2x"));

                            ResetGUIColor();


                        }

                        void setHovered()
                        {
                            if (!cellRect.IsHovered()) return;

                            hoveredIconNameOrGuid = iconNameOrGuid;

                        }
                        void closeOnClick()
                        {
                            if (!cellRect.IsHovered()) return;
                            if (!curEvent.isMouseUp) return;

                            curEvent.Use();

                            Close();

                        }



                        cellRect.MarkInteractive();

                        backgroundSelected();
                        backgroundHovered();
                        crossIcon();
                        normalIcon();
                        recursiveIndicator();

                        setHovered();
                        closeOnClick();

                    }


                    for (int j = 0; j < iconRow.iconCount + (isFirstEnabledRow ? 1 : 0); j++)
                        icon(j);

                }


                var i = 0;

                foreach (var iconRow in palette.iconRows)
                {
                    if (!iconRow.enabled) continue;
                    if (iconRow.isEmpty) continue;

                    row(i, iconRow);

                    i++;
                }

            }
            void editPaletteButton()
            {
                var buttonRect = position.SetPos(0, 0).SetWidthFromRight(16).SetHeightFromBottom(16).Move(-14, -14);
                var buttonColor = isDarkTheme ? Greyscale(.6f) : Greyscale(1, .6f);

                if (!IconButton(buttonRect, "Toolbar Plus", 16, buttonColor)) return;


                palette.SelectInInspector(frameInProject: false);

                EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow"))?.Show();

                this.Close();

            }

            void setColorsAndIcons()
            {
                if (!curEvent.isLayout) return;


                if (palette.iconRows.Any(r => r.enabled))
                    if (hoveredIconNameOrGuid != null)
                        SetIcon(hoveredIconNameOrGuid, isRecursive: curEvent.holdingAlt);
                    else
                        SetInitialIcons();


                if (palette.colorsEnabled)
                    if (hoveredColorIndex != -1)
                        SetColor(hoveredColorIndex, isRecursive: curEvent.holdingAlt);
                    else
                        SetInitialColors();

            }
            void updatePosition()
            {
                if (!curEvent.isLayout) return;

                void calcDeltaTime()
                {
                    deltaTime = (float)(EditorApplication.timeSinceStartup - lastLayoutTime);

                    if (deltaTime > .05f)
                        deltaTime = .0166f;

                    lastLayoutTime = EditorApplication.timeSinceStartup;

                }
                void resetCurPos()
                {
                    if (currentPosition != default) return;

                    currentPosition = position.position; // position.position is always int, which can't be used for lerping

                }
                void lerpCurPos()
                {
                    var speed = 9;

                    MathUtil.SmoothDamp(ref currentPosition, targetPosition, speed, ref positionDeriv, deltaTime);
                    // MathfUtils.Lerp(ref currentPosition, targetPosition, speed, deltaTime);

                }
                void setCurPos()
                {
                    position = position.SetPos(currentPosition);
                }

                calcDeltaTime();
                resetCurPos();
                lerpCurPos();
                setCurPos();

                if (!currentPosition.magnitude.Approx(targetPosition.magnitude))
                    Repaint();

            }
            void closeOnEscape()
            {
                if (!curEvent.isKeyDown) return;
                if (curEvent.keyCode != KeyCode.Escape) return;

                SetInitialColors();
                SetInitialIcons();

                Close();

            }


            RecordUndoOnDatas();

            background();
            outline();
            colors();
            icons();
            editPaletteButton();

            setColorsAndIcons();
            updatePosition();
            closeOnEscape();



            VFolders.folderInfoCache.Clear();

            EditorApplication.RepaintProjectWindow();

            EditorApplication.delayCall += EditorApplication.RepaintProjectWindow; // to show icons that will be generated in update

        }

        static float iconSize => 14;
        static float iconSpacing => 6;
        static float cellSize => iconSize + iconSpacing;
        static float spaceAfterColors => 13;
        public float rowSpacing = 1;
        static float paddingX => 12;
        static float paddingY => 12;

        Color windowBackground => isDarkTheme ? Greyscale(.23f) : Greyscale(.75f);
        Color selectedBackground => isDarkTheme ? new Color(.3f, .5f, .7f, .8f) : new Color(.3f, .5f, .7f, .6f) * 1.25f;
        Color hoveredBackground => isDarkTheme ? Greyscale(1, .3f) : Greyscale(0, .1f);

        public Vector2 targetPosition;
        public Vector2 currentPosition;
        Vector2 positionDeriv;
        float deltaTime;
        double lastLayoutTime;






        void SetIcon(string iconNameOrGuid, bool isRecursive)
        {
            foreach (var r in folderDatas)
            {
                r.isIconRecursive = isRecursive; // setting it firstbecause iconNameOrGuid setter relies on isIconRecursive
                r.iconNameOrGuid = iconNameOrGuid;
            }
        }
        void SetColor(int colorIndex, bool isRecursive)
        {
            foreach (var r in folderDatas)
            {
                r.isColorRecursive = isRecursive;
                r.colorIndex = colorIndex;
            }
        }

        void SetInitialIcons()
        {
            for (int i = 0; i < folderDatas.Count; i++)
            {
                folderDatas[i].isIconRecursive = isIconRecursives_initial[i];
                folderDatas[i].iconNameOrGuid = iconNamesOrGuids_initial[i];
            }
        }
        void SetInitialColors()
        {
            for (int i = 0; i < folderDatas.Count; i++)
            {
                folderDatas[i].isColorRecursive = isColorRecursives_initial[i];
                folderDatas[i].colorIndex = colorIndexes_initial[i];
            }
        }

        void RemoveEmptyFolderDatas()
        {
            if (VFoldersData.storeDataInMetaFiles) return; // empties removed from meta files in SaveData()

            var toRemove = folderDatas.Select(r => r.folderData).Where(r => r.iconNameOrGuid == "" && r.colorIndex == 0);

            foreach (var r in toRemove)
                data.folderDatas_byGuid.RemoveValue(r);

            if (toRemove.Any())
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup() - 1);

        }

        void RecordUndoOnDatas()
        {
            if (!VFoldersData.storeDataInMetaFiles)
                if (data)
                    data.RecordUndo();

            if (VFoldersData.storeDataInMetaFiles)
                foreach (var r in guids)
                    AssetImporter.GetAtPath(r.ToPath()).RecordUndo();

        }
        void MarkDatasDirty()
        {
            if (!VFoldersData.storeDataInMetaFiles)
                if (data)
                    data.Dirty();

            if (VFoldersData.storeDataInMetaFiles)
                VFolders.folderDatasFromMetaFiles_byGuid.Clear();

        }
        void SaveData()
        {
            if (!VFoldersData.storeDataInMetaFiles) return;
            // if (!VFoldersData.storeDataInMetaFiles) { data.Save(); return; }

            for (int i = 0; i < guids.Count; i++)
                if (folderDatas[i].iconNameOrGuid == "" && folderDatas[i].colorIndex == 0)
                    AssetImporter.GetAtPath(guids[i].ToPath()).userData = "";
                else
                    AssetImporter.GetAtPath(guids[i].ToPath()).userData = JsonUtility.ToJson(folderDatas[i].folderData);

            for (int i = 0; i < guids.Count; i++)
                AssetImporter.GetAtPath(guids[i].ToPath()).SaveAndReimport();

        }









        void OnLostFocus()
        {
            if (curEvent.holdingAlt && EditorWindow.focusedWindow?.GetType().Name == "ProjectBrowser")
                CloseNextFrameIfNotRefocused();
            else
                Close();

        }

        void CloseNextFrameIfNotRefocused()
        {
            EditorApplication.delayCall += () => { if (EditorWindow.focusedWindow != this) Close(); };
        }




        static void RepaintOnAlt() // Update 
        {
            if (curEvent.holdingAlt != wasHoldingAlt)
                if (EditorWindow.mouseOverWindow is VFoldersPaletteWindow paletteWindow)
                    paletteWindow.Repaint();

            wasHoldingAlt = curEvent.holdingAlt;

        }

        static bool wasHoldingAlt;









        public void Init(List<string> guids)
        {
            void createData()
            {
                if (VFolders.data) return;

                VFolders.data = ScriptableObject.CreateInstance<VFoldersData>();

                AssetDatabase.CreateAsset(VFolders.data, GetScriptPath("VFolders").GetParentPath().CombinePath("vFolders Data.asset"));

            }
            void createPalette()
            {
                if (VFolders.palette) return;

                VFolders.palette = ScriptableObject.CreateInstance<VFoldersPalette>();

                AssetDatabase.CreateAsset(VFolders.palette, GetScriptPath("VFolders").GetParentPath().CombinePath("vFolders Palette.asset"));

            }
            void setSize()
            {
                if (!palette.colorsEnabled && !palette.iconRows.Any(r => r.enabled && !r.isEmpty)) // somehow happened on first palette window opening in 2022.3.50
                    palette.InvokeMethod("Reset");



                var rowCellCounts = new List<int>();

                if (palette.colorsEnabled)
                    rowCellCounts.Add(palette.colors.Count + 1);

                foreach (var r in palette.iconRows.Where(r => r.enabled && !r.isEmpty))
                    rowCellCounts.Add(r.iconCount + (r == palette.iconRows.First(r => r.enabled) ? 1 : 0));

                var width = paddingX
                          + rowCellCounts.Max() * cellSize
                          + (rowCellCounts.Last() == rowCellCounts.Max() ? cellSize : 0)
                          + paddingX;



                var iconRowCount = palette.iconRows.Count(r => r.enabled && !r.isEmpty);

                var height = paddingY
                           + (palette.colorsEnabled ? cellSize : 0)
                           + (palette.colorsEnabled && palette.iconRows.Any(r => r.enabled && !r.isEmpty) ? spaceAfterColors : 0)
                           + cellSize * iconRowCount
                           + rowSpacing * (iconRowCount - 1)
                           + paddingY;


                position = position.SetSize(width, height).SetPos(targetPosition);

            }
            void getFolderDatas()
            {
                folderDatas.Clear();

                foreach (var guid in guids)
                    folderDatas.Add(new FolderDataWrapper(guid));

            }
            void getInitialState()
            {
                iconNamesOrGuids_initial = folderDatas.Select(r => r.iconNameOrGuid).ToList();
                colorIndexes_initial = folderDatas.Select(r => r.colorIndex).ToList();

                isIconRecursives_initial = folderDatas.Select(r => r.isIconRecursive).ToList();
                isColorRecursives_initial = folderDatas.Select(r => r.isColorRecursive).ToList();

            }


            this.guids = guids;

            RecordUndoOnDatas();

            createData();
            createPalette();
            setSize();
            getFolderDatas();
            getInitialState();

            Undo.undoRedoPerformed -= EditorApplication.RepaintProjectWindow;
            Undo.undoRedoPerformed += EditorApplication.RepaintProjectWindow;

            EditorApplication.update -= RepaintOnAlt;
            EditorApplication.update += RepaintOnAlt;

        }

        void OnDestroy()
        {
            RemoveEmptyFolderDatas();
            MarkDatasDirty();
            SaveData();

            EditorApplication.update -= RepaintOnAlt;

        }

        public List<string> guids = new();
        public List<FolderDataWrapper> folderDatas = new();

        public List<string> iconNamesOrGuids_initial = new();
        public List<int> colorIndexes_initial = new();

        public List<bool> isIconRecursives_initial = new();
        public List<bool> isColorRecursives_initial = new();

        static VFoldersPalette palette => VFolders.palette;
        static VFoldersData data => VFolders.data;







        public static void CreateInstance(Vector2 position)
        {
            instance = ScriptableObject.CreateInstance<VFoldersPaletteWindow>();

            instance.ShowPopup();

            instance.position = instance.position.SetPos(position).SetSize(200, 300);
            instance.targetPosition = position;

        }

        public static VFoldersPaletteWindow instance;






        public class FolderDataWrapper
        {
            public string iconNameOrGuid
            {
                get
                {
                    if (folderData != null && folderData.iconNameOrGuid != "")
                        if (folderData.iconNameOrGuid == "none") return "";
                        else return folderData.iconNameOrGuid ?? "";

                    else if (VFoldersMenu.autoIconsEnabled && folderState.autoIconName != "")
                        return folderState.autoIconName;

                    else return "";

                }
                set
                {
                    if (VFoldersMenu.autoIconsEnabled && folderState.autoIconName != "")
                        if (value == folderState.autoIconName && !folderData.isIconRecursive)
                            folderData.iconNameOrGuid = "";
                        else if (value == "")
                            folderData.iconNameOrGuid = "none";
                        else
                            folderData.iconNameOrGuid = value;


                    else folderData.iconNameOrGuid = value;

                }

            }
            public bool isIconRecursive { get => folderData.isIconRecursive; set => folderData.isIconRecursive = value; }

            public int colorIndex { get => folderData.colorIndex; set => folderData.colorIndex = value; }
            public bool isColorRecursive { get => folderData.isColorRecursive; set => folderData.isColorRecursive = value; }


            public FolderDataWrapper(string guid)
            {
                folderData = VFolders.GetFolderData(guid, createDataIfDoesntExist: true);
                folderState = VFolders.GetFolderState(guid);
            }

            public FolderData folderData;
            public FolderState folderState;


            // used as an interlayer between folderData and palette window to account for automatic icons
            // it's the only structural difference between vFolders' PaletteWindow and vHierarchy's

        }


    }
}
#endif