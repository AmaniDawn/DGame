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
    public class VInspectorClipboard : ScriptableSingleton<VInspectorClipboard>
    {
        public static void CopyComponent(Component component)
        {
            instance.RecordUndo();

            if (instance.copiedComponetDatas.FirstOrDefault(r => r.sourceComponent == component) is ComponentData alreadyCopiedData)
            {
                instance.discardedComponentDatas.Add(alreadyCopiedData);
                instance.copiedComponetDatas.Remove(alreadyCopiedData);
            }
            else
                instance.copiedComponetDatas.Add(GetComponentData(component));

            instance.Dirty();

        }
        public static void PasteComponentValues(ComponentData data, Component component)
        {
            component.RecordUndo();

            ApplyComponentData(data, component);

            component.Dirty();



            instance.RecordUndo();

            instance.copiedComponetDatas.Remove(data);
            instance.discardedComponentDatas.Add(data);

            instance.Dirty();

        }
        public static void PasteComponentAsNew(ComponentData data, GameObject gameObject)
        {
            var addedComponent = Undo.AddComponent(gameObject, data.sourceComponent.GetType());

            ApplyComponentData(data, addedComponent);

        }

        public static void ClearCopiedDatas()
        {
            instance.RecordUndo();

            instance.discardedComponentDatas.AddRange(instance.copiedComponetDatas);
            instance.copiedComponetDatas.Clear();

            instance.Dirty();

        }



        public static bool CanComponentsBePastedTo(IEnumerable<GameObject> targetGos)
        {
            if (!targetGos.Any()) return false;

            foreach (var copiedData in instance.copiedComponetDatas)
                if (nonDuplicableComponentTypes.Contains(copiedData.sourceComponent.GetType()))
                    if (targetGos.Any(r => r.TryGetComponent(copiedData.sourceComponent.GetType(), out _)))
                        return false;

            return true;

        }

        static Type[] nonDuplicableComponentTypes = new[]
        {
                typeof(Transform),
                typeof(RectTransform),
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(SkinnedMeshRenderer),
                typeof(Camera),
                typeof(AudioListener),
                typeof(Rigidbody),
                typeof(Rigidbody2D),
                typeof(Light),
                typeof(Canvas),
                typeof(Animation),
                typeof(Animator),
                typeof(AudioSource),
                typeof(ParticleSystem),
                typeof(TrailRenderer),
                typeof(LineRenderer),
                typeof(LensFlare),
                typeof(Projector),
                typeof(AudioReverbZone),
                typeof(AudioEchoFilter),
                typeof(Terrain),
                typeof(TerrainCollider),

            };



        [SerializeReference] public List<ComponentData> copiedComponetDatas = new();

        [SerializeReference] public List<ComponentData> discardedComponentDatas = new(); // removed datas get stashed here so they can be restored on undo/redo







        public static void SaveComponent(Component component)
        {
            instance.RecordUndo();

            if (instance.savedComponentDatas.FirstOrDefault(r => r.sourceComponent == component) is ComponentData alreadySavedData)
            {
                instance.discardedComponentDatas.Add(alreadySavedData);
                instance.savedComponentDatas.Remove(alreadySavedData);
            }
            else
                instance.savedComponentDatas.Add(GetComponentData(component, saveGlobalId: true));

            instance.Dirty();

        }

        public static void OnPlaymodeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                instance.failedToSaveComponentDatas.Clear();


            if (state != PlayModeStateChange.EnteredEditMode) return;

            foreach (var data in instance.savedComponentDatas)
                if (EditorUtility.InstanceIDToObject(data.sourceComponent.GetInstanceID()) is Component sourceComponent)
                    ApplyComponentData(data, sourceComponent);
                else if (data.globalId.GetObject() is Component sourceComponent_)
                    ApplyComponentData(data, sourceComponent_);
                else
                    instance.failedToSaveComponentDatas.Add(data);

            instance.savedComponentDatas.Clear();

        }

        [SerializeReference] public List<ComponentData> savedComponentDatas = new();
        [SerializeReference] public List<ComponentData> failedToSaveComponentDatas = new();







        public static ComponentData GetComponentData(Component component, bool saveGlobalId = false)
        {
            var data = new ComponentData();

            data.sourceComponent = component;

            if (saveGlobalId)
                data.globalId = component.GetGlobalID();



            var property = new SerializedObject(component).GetIterator();

            if (!property.Next(true)) return data;


            do data.serializedPropertyValues_byPath[property.propertyPath] = property.GetBoxedValue();
            while (property.NextVisible(true));

            return data;

        }
        public static void ApplyComponentData(ComponentData componentData, Component targetComponent)
        {
            foreach (var key in componentData.serializedPropertyValues_byPath.Keys.ToList())
                if (componentData.serializedPropertyValues_byPath[key] is Object unityObject && !unityObject) // sometimes object references become null after playmode in unity 6, so we have to restore them by instanceId
                    componentData.serializedPropertyValues_byPath[key] = EditorUtility.InstanceIDToObject(unityObject.GetInstanceID());

            foreach (var kvp in componentData.serializedPropertyValues_byPath)
            {
                var so = new SerializedObject(targetComponent);
                var property = so.FindProperty(kvp.Key);

                so.Update();

                property.SetBoxedValue(kvp.Value);

                so.ApplyModifiedProperties();

                targetComponent.Dirty();

            }

        }

        [System.Serializable]
        public class ComponentData
        {
            public Component sourceComponent;
            public Dictionary<string, object> serializedPropertyValues_byPath = new();

            public GlobalID globalId;

        }

    }

}
#endif