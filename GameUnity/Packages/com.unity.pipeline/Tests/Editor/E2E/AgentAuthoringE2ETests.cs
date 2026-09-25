using NUnit.Framework;

namespace Unity.Pipeline.Tests.Editor.E2E
{
    // =====================================================================================
    // CLI-196 — End-to-end agent authoring test (SPEC / SKELETON — BLOCKED)
    //
    // This file is intentionally inert. It compiles against NUnit ONLY and is [Ignore]d.
    // It references no authoring command or API, because the commands it will drive do not
    // exist yet — they land in CLI-191..195. See the plan at:
    //     docs/authoring/plans/CLI-196-docs-samples-e2e.md
    //
    // When CLI-191..195 are merged, replace the single Ignored test below with the sequence
    // documented here, driving each step through an isolated PipelineTestServer exactly like
    // Tests/Editor/Authoring/FolderCommandsTests.cs (the ViaClient pattern):
    //
    //     using (var server = new PipelineTestServer())
    //     {
    //         var response = server.Execute("<command>", new { ... });
    //         Assert.IsTrue(response.IsSuccess, response.Error);
    //         // pull globalId / assetPath / guid / instanceId / hierarchyPath out of
    //         // response.JsonResponse["result"] and feed it forward as the next ObjectRef.
    //     }
    //
    // Intended end-to-end sequence (dep ticket in brackets; command names are PROVISIONAL —
    // confirm against the landed tickets before wiring, do not invent commands):
    //
    //   0. [CLI-190] set_authoring_root  -> "Assets/__CLI196E2E"   (reset in TearDown)
    //   1. [CLI-190] create_folder       -> bare "Work" (resolves to Assets/__CLI196E2E/Work;
    //                                    do NOT repeat the root name or it nests) assert IsValidFolder, has guid
    //   2. [CLI-191] create_asset / create_material / create_scriptable_object
    //                                    -> a leaf asset            assert assetPath loads non-null
    //   3. [CLI-193] create_scene        -> bare "E2E.unity" (-> Assets/__CLI196E2E/E2E.unity) assert exists + active
    //   4. [CLI-192] create_gameobject   -> /Root, then /Root/Child assert hierarchyPath + instanceId
    //   5. [CLI-192] add_component       -> e.g. UnityEngine.Rigidbody on a target ObjectRef
    //   6. [CLI-194] create_prefab       -> bare "Root.prefab" (-> Assets/__CLI196E2E/Root.prefab) from the hierarchy; assert guid
    //   7. [CLI-195] create_script (+ recompile + poll recompile_status)
    //                                    -> bare "E2EBehaviour.cs" (-> Assets/__CLI196E2E/E2EBehaviour.cs); assert exists/compiles
    //   8. [CLI-192/195] add_component   -> attach "E2EBehaviour" to the prefab/GameObject
    //   9. [CLI-192/195] set_property / set_serialized_reference
    //                                    -> wire the behaviour's serialized field to the step-2 asset
    //  10. [CLI-193/194] save_scene / save_prefab (or save_asset)
    //  11. round-trip verify: re-resolve via globalId/guid (ObjectResolver) and assert the wired
    //      serialized reference still points at the step-2 asset. THIS is the load-bearing assert:
    //      it proves the full create -> wire -> save -> reload loop survives serialization.
    //
    // Suggested fixture shape once unblocked:
    //   - [SetUp]    : set authoring root to the throwaway folder.
    //   - [TearDown] : ProjectPaths.ResetAuthoringRoot(); delete "Assets/__CLI196E2E";
    //                  close/discard the temporary scene; AssetDatabase.Refresh().
    //   - mode       : EditMode (this assembly). Run via:
    //                  unity command run_tests --mode editor --filter AgentAuthoringE2ETests
    // =====================================================================================

    /// <summary>
    /// End-to-end agent-authoring validation: drives the full create -> build -> wire -> save loop
    /// through pipeline commands and asserts the produced assets/scene exist and the serialized
    /// reference reads back. Blocked on CLI-191..195 (authoring commands not yet implemented); see
    /// docs/authoring/plans/CLI-196-docs-samples-e2e.md.
    /// </summary>
    public class AgentAuthoringE2ETests
    {
        [Test]
        [Ignore("CLI-196 blocked on CLI-191..195: authoring commands not yet implemented")]
        public void FullAuthoringLoop_CreatesAssetsSceneAndPrefab_AndSerializedReferenceReadsBack()
        {
            // Pending CLI-191..195 — body wired up once the authoring commands land.
        }
    }
}
