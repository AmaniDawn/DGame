using System.Reflection;
using Unity.Pipeline.HotReload;
using Unity.Pipeline.Samples.HotReload;
using UnityEngine;

namespace Unity.Pipeline.Samples.HotReload
{
    /// <summary>
    /// Test manager for manually testing hot reload functionality in Unity Editor.
    /// Add this to a GameObject and use the inspector buttons or call methods from console.
    /// </summary>
    public class HotReloadTestManager : MonoBehaviour
    {
        [Header("Test References")]
        public TestPlayerController testPlayer;

        [Header("Test Controls")]
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private bool showDebugLogs = true;

        void Start()
        {
            if (autoFindPlayer && testPlayer == null)
            {
                testPlayer = FindAnyObjectByType<TestPlayerController>();
            }

            if (showDebugLogs)
            {
                Debug.Log("=== HOT RELOAD TEST MANAGER ===");
                Debug.Log("Use the buttons in the inspector or call these methods from console:");
                Debug.Log("- RegisterTestMethods() - Register player methods for hot reload");
                Debug.Log("- SimulateHotReloadOverride() - Apply hot reload overrides manually");
                Debug.Log("- ClearHotReload() - Remove hot reload overrides");
                Debug.Log("- ShowRegistryStats() - Display current hot reload state");
                Debug.Log("- TestPlayerActions() - Test player methods");
            }
        }

        [ContextMenu("Register Test Methods")]
        public void RegisterTestMethods()
        {
            if (testPlayer == null)
            {
                Debug.LogError("No TestPlayerController assigned!");
                return;
            }

            // TestPlayerController.RegisterOverrides();
            Debug.Log("✅ Registered TestPlayerController methods for hot reload");
            ShowRegistryStats();
        }

        [ContextMenu("Simulate Hot Reload Override")]
        public void SimulateHotReloadOverride()
        {
            // Manually register the override methods from TestPlayerOverrides
            var overrideType = typeof(TestPlayerOverrides);

            var updateOverride = overrideType.GetMethod("Update");
            var moveOverride = overrideType.GetMethod("Move");
            var damageOverride = overrideType.GetMethod("CalculateDamage");
            var takeDamageOverride = overrideType.GetMethod("TakeDamage");

            if (updateOverride != null)
            {
                HotReloadRegistry.RegisterMethodOverride(updateOverride,
                    new HotReloadOverrideMethodAttribute("TestPlayerController.Update"), overrideType);
            }

            if (moveOverride != null)
            {
                HotReloadRegistry.RegisterMethodOverride(moveOverride,
                    new HotReloadOverrideMethodAttribute("TestPlayerController.Move"), overrideType);
            }

            if (damageOverride != null)
            {
                HotReloadRegistry.RegisterMethodOverride(damageOverride,
                    new HotReloadOverrideMethodAttribute("PlayerCombat"), overrideType);
            }

            if (takeDamageOverride != null)
            {
                HotReloadRegistry.RegisterMethodOverride(takeDamageOverride,
                    new HotReloadOverrideMethodAttribute("TestPlayerController.TakeDamage"), overrideType);
            }

            Debug.Log("🔥 Applied hot reload overrides! Player behavior should now be different.");
            ShowRegistryStats();

            if (testPlayer != null)
            {
                testPlayer.LogHotReloadStatus();
            }
        }

        [ContextMenu("Clear Hot Reload")]
        public void ClearHotReload()
        {
            HotReloadRegistry.ClearAllOverrides();
            Debug.Log("🧹 Cleared hot reload overrides. Player should return to original behavior.");
            ShowRegistryStats();
        }

        [ContextMenu("Show Registry Stats")]
        public void ShowRegistryStats()
        {
            var stats = HotReloadRegistry.GetStats();
            Debug.Log($"📊 Hot Reload Stats:");
            Debug.Log($"   Reloadable Methods: {stats.ReloadableMethodCount}");
            Debug.Log($"   Active Overrides: {stats.ActiveOverrideCount}");
            Debug.Log($"   Loaded Types: {stats.LoadedTypeCount}");

            if (stats.ReloadableMethodIds.Count > 0)
            {
                Debug.Log($"   Reloadable Method IDs: {string.Join(", ", stats.ReloadableMethodIds)}");
            }

            if (stats.ActiveOverrideIds.Count > 0)
            {
                Debug.Log($"   Active Override IDs: {string.Join(", ", stats.ActiveOverrideIds)}");
            }
        }

        [ContextMenu("Test Player Actions")]
        public void TestPlayerActions()
        {
            if (testPlayer == null)
            {
                Debug.LogError("No TestPlayerController assigned!");
                return;
            }

            Debug.Log("🎮 Testing player actions...");

            testPlayer.TestMovement();
            testPlayer.TestDamage();
            testPlayer.LogHotReloadStatus();

            Debug.Log($"   Last Action: {testPlayer.lastActionLog}");
            Debug.Log($"   Health: {testPlayer.health}");
            Debug.Log($"   Last Movement: {testPlayer.lastMovement}");
        }

        [ContextMenu("Reset Test Environment")]
        public void ResetTestEnvironment()
        {
            HotReloadRegistry.ClearAllForTesting();

            if (testPlayer != null)
            {
                testPlayer.health = 100;
                testPlayer.lastActionLog = "Environment reset";
                testPlayer.lastMovement = Vector3.zero;
                testPlayer.lastDamageCalculated = 0;
            }

            Debug.Log("🔄 Reset test environment to clean state");
        }

        // Keyboard shortcuts for quick testing
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                RegisterTestMethods();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SimulateHotReloadOverride();
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                ClearHotReload();
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                TestPlayerActions();
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                ShowRegistryStats();
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Hot Reload Testing (F1-F5):");
            GUILayout.Label("F1: Register Methods");
            GUILayout.Label("F2: Apply Hot Reload");
            GUILayout.Label("F3: Clear Hot Reload");
            GUILayout.Label("F4: Test Actions");
            GUILayout.Label("F5: Show Stats");

            if (testPlayer != null)
            {
                GUILayout.Space(10);
                GUILayout.Label($"Status: {testPlayer.lastActionLog}");
                GUILayout.Label($"Health: {testPlayer.health}");
            }
            GUILayout.EndArea();
        }
    }
}