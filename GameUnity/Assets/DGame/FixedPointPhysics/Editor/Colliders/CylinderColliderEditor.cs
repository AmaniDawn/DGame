#if UNITY_EDITOR

using UnityEditor;

namespace DGame.Editor
{
    /// <summary>
    /// A custom editor for the FixedPointCapsuleColliderPresenter, extending the functionality of the BaseCapsuleColliderPresenterEditor.
    /// This editor provides a specialized interface for editing FixedPointCapsuleCollider properties within the Unity Editor.
    /// </summary>
    [CustomEditor(typeof(FPCylinderCollider))]
    [CanEditMultipleObjects]
    internal class CylinderColliderEditor : AACapsuleColliderEditor
    {
    }
}

#endif