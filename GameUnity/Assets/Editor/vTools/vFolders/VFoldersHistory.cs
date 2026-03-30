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
    [System.Serializable]
    public class VFoldersHistory
    {

        public void CheckTreeStateChange()
        {
            if (!VFoldersMenu.navigationBarEnabled) return;
            if (!isOneColumn) return;

            if (lastTreeState == null) { lastTreeState = new() { scrollPos = currentScrollPos, expandedIds = expandedIds.ToList() }; return; }


            var curTreeState = new TreeState();

            var targetScrollPosChanged = false;
            var targetExpandedIdsChanged = false;



            void set_curState_scrollPos()
            {
                if (controller.animatingScroll)
                    curTreeState.scrollPos = controller.targetScrollPos;
                else
                    curTreeState.scrollPos = currentScrollPos;

            }
            void set_curState_expandedIds()
            {
                var expandedIdsHashset = expandedIds.ToHashSet();



                if (treeViewAnimatesExpansion)
                    if (animatingItemTragetExpanded_fromTreeViewExpandAnimator == true)
                        expandedIdsHashset.UnionWith(new HashSet<int>() { animatingItemId_fromTreeViewExpandAnimator });
                    else
                        expandedIdsHashset.ExceptWith(new HashSet<int>() { animatingItemId_fromTreeViewExpandAnimator });



                if (controller.animatingExpansion)
                {
                    foreach (var r in controller.expandQueue_toAnimate)
                        if (r.expand)
                            expandedIdsHashset.UnionWith(new HashSet<int>() { r.id });
                        else
                            expandedIdsHashset.ExceptWith(new HashSet<int>() { r.id });

                    expandedIdsHashset.ExceptWith(controller.expandQueue_toCollapseAfterAnimation.ToHashSet());
                }


                curTreeState.expandedIds = expandedIdsHashset.ToList();

            }

            void checkScrollPosChange()
            {
                if (framesSinceLastExpansionAnimation < 2) return;
                if (framesSinceLastScrollAnimation < 2) return;

                if (curTreeState.scrollPos == lastTreeState.scrollPos) return;



                if (EditorApplication.timeSinceStartup - lastScrollTime > 2 || lastTargetStateChangeWasExpandedIds)
                    targetScrollPosChanged = true;

                lastScrollTime = EditorApplication.timeSinceStartup;

            }
            void checkExpandedIdsChange()
            {
                if (lastTreeState.expandedIds.ToHashSet().SetEquals(curTreeState.expandedIds.ToHashSet())) return;

                targetExpandedIdsChanged = true;

                // lastTargetState.expandedIds.LogAll("lastTargetState");
                // curTargetState.expandedIds.LogAll("curTargetState");

            }

            void registerStateChange()
            {
                if (!targetScrollPosChanged && !targetExpandedIdsChanged) return;


                prevTreeStates.Add(lastTreeState);
                nextTreeStates.Clear();

                lastTargetStateChangeWasExpandedIds = targetExpandedIdsChanged;


                if (prevTreeStates.Count > 50)
                    prevTreeStates.RemoveAt(0);


                // if (targetScrollPosChanged && targetExpandedIdsChanged)
                //     "expand and scroll".Log();
                // else if (targetScrollPosChanged)
                //     "scroll".Log();
                // else if (targetExpandedIdsChanged)
                //     "expand".Log();

            }

            void updateCountersSinceAnimation()
            {
                if (controller.animatingExpansion || treeViewAnimatesExpansion)
                    framesSinceLastExpansionAnimation = 0;
                else
                    framesSinceLastExpansionAnimation++;

                if (controller.animatingScroll || treeViewAnimatesScroll)
                    framesSinceLastScrollAnimation = 0;
                else
                    framesSinceLastScrollAnimation++;

            }



            set_curState_scrollPos();
            set_curState_expandedIds();

            checkScrollPosChange();
            checkExpandedIdsChange();

            registerStateChange();

            updateCountersSinceAnimation();


            lastTreeState = curTreeState;

        }

        int framesSinceLastExpansionAnimation;
        int framesSinceLastScrollAnimation;

        public double lastScrollTime = 0;

        bool lastTargetStateChangeWasExpandedIds;

        [System.NonSerialized] TreeState lastTreeState = null;



        public void CheckFolderPathChange()
        {
            if (!VFoldersMenu.navigationBarEnabled) return;
            if (isOneColumn) return;

            var curFolderPath = window.GetMemberValue("m_SearchFilter").GetMemberValue<string[]>("folders").FirstOrDefault();

            if (curFolderPath.IsNullOrEmpty()) return;

            if (lastFolderPath.IsNullOrEmpty()) lastFolderPath = curFolderPath;
            if (curFolderPath == lastFolderPath) return;


            prevFolderPaths.Add(lastFolderPath);
            nextFolderPaths.Clear();

            if (prevFolderPaths.Count > 50)
                prevFolderPaths.RemoveAt(0);


            lastFolderPath = curFolderPath;

        }

        string lastFolderPath = "";







        public void UpdateState()
        {
            if (!VFoldersMenu.navigationBarEnabled) return;


            isOneColumn = window.GetFieldValue<int>("m_ViewMode") == 0;

            var treeViewController = window.GetFieldValue(isOneColumn ? "m_AssetTree" : "m_FolderTree");
            var treeViewControllerData = treeViewController?.GetPropertyValue("data");



            var treeViewControllerState = treeViewController?.GetPropertyValue<TreeViewState>("state");

            currentScrollPos = treeViewControllerState?.scrollPos.y ?? 0;

            expandedIds = treeViewControllerState?.expandedIDs?.ToInts() ?? new();



            var treeViewAnimator = treeViewController?.GetMemberValue("m_ExpansionAnimator");
            var treeViewAnimatorSetup = treeViewAnimator?.GetMemberValue("m_Setup");

            treeViewAnimatesScroll = treeViewController?.GetMemberValue<UnityEditor.AnimatedValues.AnimFloat>("m_FramingAnimFloat").isAnimating ?? false;

            treeViewAnimatesExpansion = treeViewAnimator?.GetMemberValue<bool>("isAnimating") ?? false;
            animatingItemTragetExpanded_fromTreeViewExpandAnimator = treeViewAnimatorSetup?.GetMemberValue<bool>("expanding") ?? false;
#if UNITY_6000_3_OR_NEWER
            animatingItemId_fromTreeViewExpandAnimator = treeViewAnimatorSetup?.GetMemberValue("item").GetMemberValue<EntityId>("id") ?? 0;
#else
            animatingItemId_fromTreeViewExpandAnimator = treeViewAnimatorSetup?.GetMemberValue("item").GetMemberValue<int>("id") ?? 0;
#endif

        }

        public bool isOneColumn;
        public bool isTwoColumns => !isOneColumn;

        public bool isSearchActive;

        public float currentScrollPos;

        public List<int> expandedIds = new();

        public bool treeViewAnimatesScroll;

        public bool treeViewAnimatesExpansion;
        public bool animatingItemTragetExpanded_fromTreeViewExpandAnimator;
        public int animatingItemId_fromTreeViewExpandAnimator;














        public void MoveBack_OneColumn(bool withAnimation = true)
        {
            var prevState = prevTreeStates.Last();

            prevTreeStates.Remove(prevState);
            nextTreeStates.Add(lastTreeState);
            lastTreeState = prevState;


            if (withAnimation)
            {
                controller.StartScrollAnimation(prevState.scrollPos);
                controller.StartExpandAnimation(prevState.expandedIds);
            }
            else
            {
                controller.SetScrollPos(prevState.scrollPos);
                controller.SetExpandedIds(prevState.expandedIds);
            }

        }
        public void MoveForward_OneColumn(bool withAnimation = true)
        {
            var nextState = nextTreeStates.Last();

            nextTreeStates.Remove(nextState);
            prevTreeStates.Add(lastTreeState);
            lastTreeState = nextState;


            if (withAnimation)
            {
                controller.StartScrollAnimation(nextState.scrollPos);
                controller.StartExpandAnimation(nextState.expandedIds);
            }
            else
            {
                controller.SetScrollPos(nextState.scrollPos);
                controller.SetExpandedIds(nextState.expandedIds);
            }

        }

        public List<TreeState> prevTreeStates = new();
        public List<TreeState> nextTreeStates = new();



        public void MoveBack_TwoColumns(bool withAnimation = true)
        {
            var prevPath = prevFolderPaths.Last();

            prevFolderPaths.RemoveLast();
            nextFolderPaths.Add(lastFolderPath);
            lastFolderPath = prevPath;


            if (withAnimation)
            {
                controller.RevealFolder(prevPath, expand: false, highlight: false, snapToTopMargin: false);
                controller.OpenFolder(prevPath);
            }
            else
            {
                window.InvokeMethod("ShowFolderContents", AssetDatabase.LoadAssetAtPath<DefaultAsset>(prevPath).GetInstanceID(), true);
            }

        }
        public void MoveForward_TwoColumns(bool withAnimation = true)
        {
            var nextPath = nextFolderPaths.Last();

            nextFolderPaths.RemoveLast();
            prevFolderPaths.Add(lastFolderPath);
            lastFolderPath = nextPath;


            if (withAnimation)
            {
                controller.RevealFolder(nextPath, expand: false, highlight: false, snapToTopMargin: false);
                controller.OpenFolder(nextPath);
            }
            else
            {
                window.InvokeMethod("ShowFolderContents", AssetDatabase.LoadAssetAtPath<DefaultAsset>(nextPath).GetInstanceID(), true);
            }

        }

        public List<string> prevFolderPaths = new();
        public List<string> nextFolderPaths = new();













        [System.Serializable]
        public class TreeState
        {
            public List<int> expandedIds = new();

            public float scrollPos;

        }








        public VFoldersHistory(EditorWindow window) => this.window = window;

        EditorWindow window;

        VFoldersGUI gui => VFolders.guis_byWindow[window];
        VFoldersController controller => VFolders.controllers_byWindow[window];

    }


    public class VFoldersHistorySingleton : ScriptableSingleton<VFoldersHistorySingleton>
    {
        public SerializableDictionary<EditorWindow, VFoldersHistory> histories_byWindow = new();
    }


}
#endif