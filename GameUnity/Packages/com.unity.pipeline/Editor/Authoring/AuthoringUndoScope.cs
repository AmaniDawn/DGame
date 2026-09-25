using System;
using UnityEditor;

namespace Unity.Pipeline.Editor.Authoring
{
    /// <summary>
    /// <para>
    /// Groups all Undo operations registered during its lifetime into a single, collapsible Editor
    /// Undo step, so a multi-step agent action reverts as one. Register mutations inside the scope
    /// with Undo.RegisterCreatedObjectUndo / RegisterCompleteObjectUndo / etc.
    /// </para>
    ///
    /// <code>
    /// using (new AuthoringUndoScope("Create Enemy"))
    /// {
    ///     var go = new GameObject("Enemy");
    ///     Undo.RegisterCreatedObjectUndo(go, "Create Enemy");
    ///     // ... further registered mutations collapse into the same step
    /// }
    /// </code>
    ///
    /// <para>
    /// NOTE: minimal seed for the shared safety policy (CAT-2509). AssetDatabase operations
    /// (folder/asset creation, import) are NOT part of Unity's Undo system, so this scope only
    /// affects scene/object mutations.
    /// </para>
    /// </summary>
    public sealed class AuthoringUndoScope : IDisposable
    {
        private readonly int m_Group;

        public AuthoringUndoScope(string name)
        {
            Undo.IncrementCurrentGroup();
            m_Group = Undo.GetCurrentGroup();
            if (!string.IsNullOrEmpty(name))
                Undo.SetCurrentGroupName(name);
        }

        public void Dispose()
        {
            Undo.CollapseUndoOperations(m_Group);
        }
    }
}
