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
    public class VFoldersController
    {

        public void UpdateExpandQueue()
        {
            if (treeViewAnimatesExpansion) return;

            if (!expandQueue_toAnimate.Any())
            {
                if (!expandQueue_toCollapseAfterAnimation.Any()) return;

                foreach (var id in expandQueue_toCollapseAfterAnimation)
                    SetExpanded_withoutAnimation(id, false);

                expandQueue_toCollapseAfterAnimation.Clear();

                return;
            }


            var iid = expandQueue_toAnimate.First().id;
            var expand = expandQueue_toAnimate.First().expand;


            if (expandedIds.Contains(iid) != expand)
                SetExpanded_withAnimation(iid, expand);

            expandQueue_toAnimate.RemoveAt(0);


            window.Repaint();

        }

        public List<ExpandQueueEntry> expandQueue_toAnimate = new();
        public List<int> expandQueue_toCollapseAfterAnimation = new();

        public struct ExpandQueueEntry { public int id; public bool expand; }

        public bool animatingExpansion => expandQueue_toAnimate.Any() || expandQueue_toCollapseAfterAnimation.Any();






        public void UpdateScrollAnimation()
        {
            if (!animatingScroll) return;


            var lerpSpeed = 10;

            var lerpedScrollPos = MathUtil.SmoothDamp(currentScrollPos, targetScrollPos, lerpSpeed, ref scrollPosDerivative, editorDeltaTime);

            SetScrollPos(lerpedScrollPos);

            window.Repaint();



            if (lerpedScrollPos.DistanceTo(targetScrollPos) > .4f) return;

            SetScrollPos(targetScrollPos);

            animatingScroll = false;


        }

        public float targetScrollPos;
        public float scrollPosDerivative;

        public bool animatingScroll;







        public void UpdateHighlightAnimation()
        {
            if (!animatingHighlight) return;


            var lerpSpeed = 1.2f;

            MathUtil.SmoothDamp(ref highlightAmount, 0, lerpSpeed, ref highlightDerivative, editorDeltaTime);

            window.Repaint();



            if (highlightAmount > .05f) return;

            highlightAmount = 0;

            animatingHighlight = false;


        }

        public float highlightAmount;
        public float highlightDerivative;

        public bool animatingHighlight;

        public string folderToHighlight;







        public void UpdateState()
        {
            isOneColumn = window.GetFieldValue<int>("m_ViewMode") == 0;

            treeViewController = window.GetFieldValue(isOneColumn ? "m_AssetTree" : "m_FolderTree");
            treeViewControllerData = treeViewController?.GetPropertyValue("data");



            var treeViewState = treeViewController?.GetPropertyValue<TreeViewState>("state");

            currentScrollPos = treeViewState?.scrollPos.y ?? 0;

            expandedIds = treeViewState?.expandedIDs?.ToInts() ?? new();



            var treeViewAnimator = treeViewController?.GetMemberValue("m_ExpansionAnimator");
            var treeViewAnimatorSetup = treeViewAnimator?.GetMemberValue("m_Setup");

            treeViewAnimatesScroll = treeViewController?.GetMemberValue<UnityEditor.AnimatedValues.AnimFloat>("m_FramingAnimFloat").isAnimating ?? false;

            treeViewAnimatesExpansion = treeViewAnimator?.GetMemberValue<bool>("isAnimating") ?? false;

        }

        bool isOneColumn;

        object treeViewController;
        object treeViewControllerData;

        public float currentScrollPos;

        public List<int> expandedIds = new();

        public bool treeViewAnimatesScroll;
        public bool treeViewAnimatesExpansion;

        public int GetRowIndex(int instanceId)
        {
            return treeViewControllerData.InvokeMethod<int>("GetRow", instanceId.ToIdType());
        }
















        public void ToggleExpanded(TreeViewItem item)
        {
            SetExpanded_withAnimation(item.id, !expandedIds.Contains(item.id));

            window.Repaint();

        }

        public void CollapseAll()
        {

            var idsToCollapse_roots = expandedIds.Where(id => _EditorUtility_InstanceIDToObject(id).GetPath() is string path &&
                                                                   path.HasParentPath() &&
                                                                  (path.GetParentPath() == "Assets" || path.GetParentPath() == "Packages"));


            var idsToCollapse_children = expandedIds.Where(id => _EditorUtility_InstanceIDToObject(id).GetPath() is string path &&
                                                                     !path.IsNullOrEmpty() &&
                                                                      path != "Assets" &&
                                                                      path != "Packages" &&
                                                                     !idsToCollapse_roots.Contains(id));


            expandQueue_toCollapseAfterAnimation = idsToCollapse_children.ToList();

            expandQueue_toAnimate = idsToCollapse_roots.Select(id => new ExpandQueueEntry { id = id, expand = false })
                                                       .OrderBy(row => GetRowIndex(row.id)).ToList();


            StartScrollAnimation(targetScrollPos: 0);


            window.Repaint();

        }

        public void Isolate(TreeViewItem targetItem)
        {

            List<TreeViewItem> getParents(TreeViewItem item)
            {
                var parents = new List<TreeViewItem>();

                while (item.parent != null)
                    parents.Add(item = item.parent);

                return parents;

            }

            var targetItemParents = getParents(targetItem);



            var expandedVisibleItems = new List<TreeViewItem>();

            foreach (var expandedId in expandedIds)
                if (GetRowIndex(expandedId) is int rowIndex && rowIndex != -1)
                    expandedVisibleItems.Add(treeViewControllerData.InvokeMethod<TreeViewItem>("GetItem", rowIndex));



            var itemsToCollapse = expandedVisibleItems.ToList();

            itemsToCollapse.Remove(targetItem);
            itemsToCollapse.RemoveAll(r => targetItemParents.Contains(r));
            itemsToCollapse.RemoveAll(r => itemsToCollapse.Intersect(getParents(r)).Any());



            expandQueue_toAnimate = itemsToCollapse.Select(item => new ExpandQueueEntry { id = item.id, expand = false })
                                                           .Append(new ExpandQueueEntry { id = targetItem.id, expand = true })
                                                           .OrderBy(r => GetRowIndex(r.id)).ToList();


            window.Repaint();

        }





        public void StartExpandAnimation(List<int> targetExpandedIds)
        {

            var toExpand = targetExpandedIds.Except(expandedIds).ToHashSet();
            var toCollapse = expandedIds.Except(targetExpandedIds).ToHashSet();



            // hanlde non-animated expansions/collapses

            bool hasParentToCollapse(int id)
            {
                var o = _EditorUtility_InstanceIDToObject(id);

                if (!o) return false;


                var assetPath = AssetDatabase.GetAssetPath(o);

                if (!assetPath.HasParentPath()) return false;
                if (assetPath == "Assets") return false;


                var parentAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetPath.GetParentPath());

                if (!parentAsset) return false; // packages item


                var parentId = parentAsset.GetInstanceID();

                return toCollapse.Contains(parentId)
                    || hasParentToCollapse(parentId);

            }
            bool areAllParentsExpanded(int id)
            {
                var o = _EditorUtility_InstanceIDToObject(id);

                if (!o) return true;


                var assetPath = AssetDatabase.GetAssetPath(o);

                if (!assetPath.HasParentPath()) return true;
                if (assetPath == "Assets") return true;


                var parentAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetPath.GetParentPath());

                if (!parentAsset) return true; // packages item


                var parentId = parentAsset.GetInstanceID();

                return expandedIds.Contains(parentId)
                         && areAllParentsExpanded(parentId);

            }

            var toExpand_beforeAnimation = toExpand.Where(id => !areAllParentsExpanded(id)).ToHashSet();
            var toCollapse_afterAnimation = toCollapse.Where(id => hasParentToCollapse(id)).ToHashSet();


            foreach (var id in toExpand_beforeAnimation)
                SetExpanded_withoutAnimation(id, true);

            foreach (var id in toCollapse_afterAnimation)
                expandQueue_toCollapseAfterAnimation.Add(id);


            toExpand.ExceptWith(toExpand_beforeAnimation);
            toCollapse.ExceptWith(toCollapse_afterAnimation);




            // setup animation

            expandQueue_toAnimate = toCollapse.Select(id => new ExpandQueueEntry { id = id, expand = false })
                                .Concat(toExpand.Select(id => new ExpandQueueEntry { id = id, expand = true }))
                                .OrderBy(r => GetRowIndex(r.id)).ToList();

        }

        public void SetExpandedIds(List<int> targetExpandedIds)
        {
            treeViewControllerData.InvokeMethod("SetExpandedIDs", targetExpandedIds.ToArray()); // won't work on 6.3 but it's unused anyway
        }
        public void SetExpanded_withAnimation(int instanceId, bool expanded)
        {
            treeViewController.InvokeMethod("ChangeFoldingForSingleItem", instanceId.ToIdType(), expanded);
        }
        public void SetExpanded_withoutAnimation(int instanceId, bool expanded)
        {
            treeViewControllerData.InvokeMethod("SetExpanded", instanceId.ToIdType(), expanded);
        }



        public void StartScrollAnimation(float targetScrollPos)
        {
            if (targetScrollPos.DistanceTo(currentScrollPos) < .05f) return;

            this.targetScrollPos = targetScrollPos;

            animatingScroll = true;

        }

        public void SetScrollPos(float targetScrollPos)
        {
            treeViewController.GetPropertyValue<TreeViewState>("state").scrollPos = Vector2.up * targetScrollPos;
        }




        public void RevealFolder(string path, bool expand, bool highlight, bool snapToTopMargin)
        {

            // int getId(string path) => AssetDatabase.LoadAssetAtPath<DefaultAsset>(path).GetInstanceID();
            int getId(string path) => typeof(AssetDatabase).InvokeMethod<int>("GetMainAssetOrInProgressProxyInstanceID", path);


            var idsToExpand = new List<int>();

            if (expand)
                idsToExpand.Add(getId(path));

            var cur = path;
            while (!(cur = cur.GetParentPath()).IsNullOrEmpty())
                idsToExpand.Add(getId(cur));

            idsToExpand.RemoveAll(r => expandedIds.Contains(r));




            foreach (var id in idsToExpand.SkipLast(1))
                SetExpanded_withoutAnimation(id, true);

            if (idsToExpand.Any())
                SetExpanded_withAnimation(idsToExpand.Last(), true);




            var rowCount = treeViewControllerData.GetMemberValue<ICollection>("m_Rows").Count;
            var maxScrollPos = rowCount * 16 - window.position.height + (isOneColumn ? 49.9f : 45.9f);

            var rowIndex = treeViewControllerData.InvokeMethod<int>("GetRow", getId(path).ToIdType());
            var rowPos = rowIndex * 16f + (isOneColumn ? 11 : 23);

            var scrollAreaHeight = window.GetMemberValue<Rect>("m_TreeViewRect").height;




            var margin = 48;

            var targetScrollPos = 0f;

            if (snapToTopMargin)
                targetScrollPos = (rowPos - margin).Min(maxScrollPos)
                                                   .Max(0);
            else
                targetScrollPos = currentScrollPos.Min(rowPos - margin)
                                                  .Max(rowPos - scrollAreaHeight + margin)
                                                  .Min(maxScrollPos)
                                                  .Max(0);
            if (targetScrollPos < 25)
                targetScrollPos = 0;

            StartScrollAnimation(targetScrollPos);





            if (!highlight) return;

            highlightAmount = 2.2f;

            animatingHighlight = true;

            folderToHighlight = path;


        }

        public void OpenFolder(string path)
        {
            // update search
            window.GetMemberValue("m_SearchFilter").InvokeMethod("ClearSearch");
            window.GetMemberValue("m_SearchFilter").SetMemberValue("folders", new[] { path });


            // update folder tree
            window.GetMemberValue("m_FolderTree").InvokeMethod("SetSelection", new[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(path).GetInstanceID().ToIdType() }, false);


            // update list area
            var listAreaRect = window.GetMemberValue("m_ListAreaRect");
            var searchFilter = window.GetMemberValue("m_SearchFilter");
            var checkThumbnails = false;
            var assetToInstanceId = (System.Func<string, int>)((s) => typeof(AssetDatabase).InvokeMethod<int>("GetMainAssetInstanceID", s));

            window.GetMemberValue("m_ListArea")?.InvokeMethod("InitForSearch", listAreaRect, HierarchyType.Assets, searchFilter, checkThumbnails, assetToInstanceId);


            // updat breadcrumbs
            window.GetMemberValue<IList>("m_BreadCrumbs").Clear();



            // pretty much the same as ProjectBrowser.ShowFolderContents()
            // but without m_FolderTree.SetSelection()

        }












        public VFoldersController(EditorWindow window) => this.window = window;

        public EditorWindow window;

        public VFoldersGUI gui => VFolders.guis_byWindow[window];

    }
}
#endif