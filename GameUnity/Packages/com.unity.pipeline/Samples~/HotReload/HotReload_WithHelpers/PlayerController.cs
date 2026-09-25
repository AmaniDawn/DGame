using System.Reflection;
using Unity.Pipeline.HotReload;
using UnityEngine;

namespace Unity.Pipeline.Samples.HotReload
{
    /// <summary>
    /// Test component to demonstrate hot reload functionality in Unity Editor.
    /// Add this to a GameObject to test the hot reload workflow.
    /// </summary>
    public class TestPlayerController : MonoBehaviour
    {
        [Header("Hot Reload Test Values")]
        public float moveSpeed = 5f;
        public Color debugColor = Color.white;
        public int health = 100;

        [Header("Runtime Values - Watch These Change")]
        public Vector3 lastMovement;
        public string lastActionLog = "No action yet";
        public float lastDamageCalculated;

        // No manual registration needed: RuntimePipelineManager auto-discovers [HotReloadWithOverrides] methods.

        void Start()
        {
            Debug.Log("TestPlayerController: Starting up - ready for hot reload testing");
        }

        [HotReloadWithOverrides]
        void Update()
        {
            HotReloadHelper.ExecuteWithHotReload(this, "Update", OriginalUpdate);
        }

        [HotReloadWithOverrides]
        public void Move(Vector3 direction)
        {
            HotReloadHelper.ExecuteWithHotReload(this, "Move",
                () => OriginalMove(direction), direction);
        }

        [HotReloadWithOverrides(Id = "PlayerCombat")]
        public float CalculateDamage(float baseDamage, float multiplier)
        {
            var result = HotReloadHelper.ExecuteWithHotReloadCustomId(this, "PlayerCombat",
                () => OriginalCalculateDamage(baseDamage, multiplier), baseDamage, multiplier);

            return result != null ? (float)result : OriginalCalculateDamage(baseDamage, multiplier);
        }

        [HotReloadWithOverrides]
        public void TakeDamage(float damage)
        {
            HotReloadHelper.ExecuteWithHotReload(this, "TakeDamage",
                () => OriginalTakeDamage(damage), damage);
        }

        // Original implementations
        private void OriginalUpdate()
        {
            // Simple rotation for visual feedback
            transform.Rotate(0, 45 * Time.deltaTime, 0);
            lastActionLog = $"Original Update: Rotating at {Time.time:F1}s";

            // Auto-move for testing
            if (Input.GetKey(KeyCode.Space))
            {
                Move(Vector3.forward);
            }
        }

        private void OriginalMove(Vector3 direction)
        {
            Vector3 movement = direction * moveSpeed * Time.deltaTime;
            transform.position += movement;
            lastMovement = movement;
            lastActionLog = $"Original Move: {movement} at speed {moveSpeed}";
        }

        private float OriginalCalculateDamage(float baseDamage, float multiplier)
        {
            float damage = baseDamage * multiplier;
            lastDamageCalculated = damage;
            lastActionLog = $"Original Damage: {baseDamage} * {multiplier} = {damage}";
            return damage;
        }

        private void OriginalTakeDamage(float damage)
        {
            health = Mathf.Max(0, health - Mathf.RoundToInt(damage));
            lastActionLog = $"Original TakeDamage: -{damage}, health now {health}";
        }

        // Test methods you can call from other scripts or console
        public void TestMovement()
        {
            Move(Vector3.right);
            Debug.Log($"Tested movement: {lastActionLog}");
        }

        public void TestDamage()
        {
            float damage = CalculateDamage(10f, 1.5f);
            TakeDamage(damage);
            Debug.Log($"Tested damage: calculated {damage}, {lastActionLog}");
        }

        public void LogHotReloadStatus()
        {
            bool updateActive = HotReloadHelper.IsHotReloadActive<TestPlayerController>("Update");
            bool moveActive = HotReloadHelper.IsHotReloadActive<TestPlayerController>("Move");
            bool combatActive = HotReloadHelper.IsHotReloadActive("PlayerCombat");
            bool damageActive = HotReloadHelper.IsHotReloadActive<TestPlayerController>("TakeDamage");

            Debug.Log($"Hot Reload Status - Update: {updateActive}, Move: {moveActive}, Combat: {combatActive}, TakeDamage: {damageActive}");

            var stats = HotReloadRegistry.GetStats();
            Debug.Log($"Registry Stats - Reloadable: {stats.ReloadableMethodCount}, Active: {stats.ActiveOverrideCount}, Types: {stats.LoadedTypeCount}");
        }
    }
}