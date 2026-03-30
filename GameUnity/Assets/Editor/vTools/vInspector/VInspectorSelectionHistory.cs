#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using Type = System.Type;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
// using static VTools.VDebug;


namespace VInspector
{
    public class VInspectorSelectionHistory : ScriptableSingleton<VInspectorSelectionHistory>
    {

        public void MoveBack()
        {
            var prevState = prevStates.Last();

            instance.RecordUndo("VInspectorSelectionHistory.MoveBack");

            prevStates.Remove(prevState);
            nextStates.Add(curState);
            curState = prevState;


            ignoreThisSelectionChange = true;

            prevState.selectedObjects.ToArray().SelectInInspector(frameInHierarchy: false, frameInProject: false);

        }
        public void MoveForward()
        {
            var nextState = nextStates.Last();

            instance.RecordUndo("VInspectorSelectionHistory.MoveForward");

            nextStates.Remove(nextState);
            prevStates.Add(curState);
            curState = nextState;


            ignoreThisSelectionChange = true;

            nextState.selectedObjects.ToArray().SelectInInspector(frameInHierarchy: false, frameInProject: false);

        }

        static void OnSelectionChange()
        {
            if (ignoreThisSelectionChange) { ignoreThisSelectionChange = false; return; }

            if (curEvent.modifiers == EventModifiers.Command && curEvent.keyCode == KeyCode.Z) return;
            if (curEvent.modifiers == (EventModifiers.Command | EventModifiers.Shift) && curEvent.keyCode == KeyCode.Z) return;

            if (curEvent.modifiers == EventModifiers.Control && curEvent.keyCode == KeyCode.Z) return;
            if (curEvent.modifiers == EventModifiers.Control && curEvent.keyCode == KeyCode.Y) return;


            instance.RecordUndo(Undo.GetCurrentGroupName());

            instance.prevStates.Add(instance.curState);
            instance.curState = new SelectionState() { selectedObjects = Selection.objects.ToList() };
            instance.nextStates.Clear();

            if (instance.prevStates.Count > 50)
                instance.prevStates.RemoveAt(0);

        }

        static bool ignoreThisSelectionChange;


        public List<SelectionState> prevStates = new();
        public List<SelectionState> nextStates = new();
        public SelectionState curState;

        [System.Serializable]
        public class SelectionState { public List<Object> selectedObjects = new(); }



        [InitializeOnLoadMethod]
        static void Init()
        {
            if (VInspectorMenu.pluginDisabled) return;


            Selection.selectionChanged -= OnSelectionChange;
            Selection.selectionChanged += OnSelectionChange;


            // var globalEventHandler = typeof(EditorApplication).GetFieldValue<EditorApplication.CallbackFunction>("globalEventHandler");
            // typeof(EditorApplication).SetFieldValue("globalEventHandler", ClearHistories + (globalEventHandler - ClearHistories));


            instance.curState = new SelectionState() { selectedObjects = Selection.objects.ToList() };

        }





        // static void ClearHistories() // just for debug
        // {
        //     if (curEvent.holdingAnyModifierKey) return;
        //     if (!curEvent.isKeyDown || curEvent.keyCode != KeyCode.Y) return;

        //     VInspectorSelectionHistory.instance.prevStates.Clear();
        //     VInspectorSelectionHistory.instance.nextStates.Clear();

        //     Undo.ClearAll();

        //     VInspectorMenu.RepaintInspectors();

        // }



    }
}
#endif