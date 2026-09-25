using Unity.Pipeline.HotReload;
using UnityEngine;

namespace Unity.Pipeline.Samples.HotReload
{
    /// <summary>
    /// Hot reload overrides for TestPlayerController.
    /// This file demonstrates different override patterns and can be compiled manually for testing.
    /// 
    /// You can tweak gameplay code here. When done run:
    /// ucli request --runtime UnityPipelineTests.exe reload_file_override "(Absolute Path To)\com.unity.pipeline\Samples\HotReload_WithHelpers\TestPlayerOverrides.cs"
    /// </summary>
    public static class TestPlayerOverrides
    {
        [HotReloadOverrideMethod("TestPlayerController.Update")]
        public static void Update(TestPlayerController instance)
        {
            // HOT RELOAD VERSION: Different behavior
            instance.transform.Rotate(0, -90 * Time.deltaTime, 0); // Rotate opposite direction
            instance.lastActionLog = $"HOT RELOAD Update: Reverse rotation at {Time.time:F1}s";

            // Add some color change for visual feedback
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.Lerp(Color.red, Color.yellow, Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f);
            }

            // Auto-move with different key
            if (Input.GetKey(KeyCode.LeftShift))
            {
                instance.Move(Vector3.up * 0.5f); // Move up instead of forward
            }
        }

        [HotReloadOverrideMethod("TestPlayerController.Move")]
        public static void Move(TestPlayerController instance, Vector3 direction)
        {
            // HOT RELOAD VERSION: Faster, bouncy movement
            Vector3 movement = direction * instance.moveSpeed * 2.5f * Time.deltaTime; // 2.5x faster
            movement.y += Mathf.Sin(Time.time * 5f) * 0.02f; // Add bounce

            instance.transform.position += movement;
            instance.lastMovement = movement;
            instance.lastActionLog = $"HOT RELOAD Move: Bouncy {movement} at 2.5x speed";

            // Add trail effect
            Debug.DrawRay(instance.transform.position, movement * 10f, Color.cyan, 1f);
        }

        [HotReloadOverrideMethod("PlayerCombat")]
        public static float CalculateDamage(TestPlayerController instance, float baseDamage, float multiplier)
        {
            // HOT RELOAD VERSION: Different damage formula with randomness
            float damage = (baseDamage + Random.Range(0f, 5f)) * multiplier * 1.2f; // +randomness, +20% bonus
            damage = Mathf.Round(damage * 10f) / 10f; // Round to 1 decimal

            instance.lastDamageCalculated = damage;
            instance.lastActionLog = $"HOT RELOAD Damage: ({baseDamage} + rnd) * {multiplier} * 1.2 = {damage}";

            return damage;
        }

        [HotReloadOverrideMethod("TestPlayerController.TakeDamage")]
        public static void TakeDamage(TestPlayerController instance, float damage)
        {
            // HOT RELOAD VERSION: Damage reduction and visual feedback
            float reducedDamage = damage * 0.7f; // 30% damage reduction
            instance.health = Mathf.Max(0, instance.health - Mathf.RoundToInt(reducedDamage));
            instance.lastActionLog = $"HOT RELOAD TakeDamage: -{damage} reduced to -{reducedDamage}, health now {instance.health}";

            // Visual feedback
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Flash red briefly
                renderer.material.color = Color.red;
            }

            // Screen shake effect (simple)
            if (Camera.main != null)
            {
                Debug.Log("*SCREEN SHAKE*");
            }
        }
    }
}