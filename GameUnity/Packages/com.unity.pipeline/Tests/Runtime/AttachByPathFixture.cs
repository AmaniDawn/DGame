using UnityEngine;

namespace Unity.Pipeline.Tests.Runtime
{
    /// <summary>
    /// CLI-224 fixture: an already-compiled MonoBehaviour used to exercise attach_script's --script
    /// (asset path) addressing — the command resolves the backing class from the .cs asset via
    /// MonoScript.GetClass(), so an agent can pass the create_script assetPath directly.
    ///
    /// It deliberately lives in the RUNTIME test assembly (Unity.Pipeline.Tests.Runtime, all-platforms),
    /// NOT the editor test assembly: Unity refuses AddComponent for a MonoBehaviour whose dedicated
    /// MonoScript belongs to an editor-only assembly, so a real attach must target a runtime script —
    /// which also matches how agents author scripts under Assets/. The class name matches the file name
    /// (Unity requires that for a single-type script to be addable).
    /// </summary>
    public class AttachByPathFixture : MonoBehaviour
    {
        [SerializeField] private int m_Value;

        public int Value => m_Value;
    }
}
