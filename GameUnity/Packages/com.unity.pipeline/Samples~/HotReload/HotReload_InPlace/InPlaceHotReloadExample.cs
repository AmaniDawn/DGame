using UnityEngine;
using Unity.Pipeline.HotReload;

namespace Unity.Pipeline.Samples.HotReload
{
    /// <summary>
    /// Example demonstrating in-place hot reload editing workflow.
    ///
    /// Usage:
    /// 1. Add this component to a GameObject in the scene
    /// 2. Play the scene - you'll see the object rotating
    /// 3. Modify the Update() method below (try changing Vector3.up to Vector3.forward)
    /// 4. Run: reload_file Assets/Samples/HotReload_InPlace/InPlaceHotReloadExample.cs
    /// 5. See the changes take effect immediately without stopping play mode!
    ///
    /// Requirements:
    /// - Only use PUBLIC fields, properties, and methods
    /// - Private/internal members will cause validation errors
    /// </summary>
    public class InPlaceHotReloadExample : MonoBehaviour
    {
        [Header("Hot Reload Configuration")]
        public float rotationSpeed = 90f;
        public bool enableRotation = true;
        public Vector3 rotationAxis = Vector3.up;

        [Header("Movement Configuration")]
        public float moveSpeed = 2f;
        public bool enableMovement = false;

        [HotReload]
        void Update()
        {
            // Edit this body live, then run: reload_file <this file>
            // Try changing rotationAxis (e.g. Vector3.up -> Vector3.forward) or the speed.
            if (enableRotation)
            {
                transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
            }

            if (enableMovement)
            {
                var movement = Mathf.Sin(Time.time) * moveSpeed;
                transform.position = new Vector3(movement, transform.position.y, transform.position.z);
            }
        }

        [HotReload]
        public void ResetTransform()
        {
            // Another method you can hot reload
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            Debug.Log("Transform reset via hot reload!");
        }

        // Note: value-returning methods are not yet supported by in-place reload (void methods only),
        // so this one is a normal method.
        public float CalculateDistance(Vector3 targetPosition)
        {
            return Vector3.Distance(transform.position, targetPosition);
        }

        void Awake()
        {
            // Run the sample in a 640x480 window instead of fullscreen.
            Screen.SetResolution(640, 480, FullScreenMode.Windowed);
        }

        // Regular methods (not hot reloadable) - these work normally
        void Start()
        {
            Debug.Log($"InPlaceHotReloadExample started. Try hot reloading the Update method!");
            Debug.Log($"Command: reload_file {GetType().Name}.cs");
        }

        public void TriggerResetFromUI()
        {
            // This calls the hot reloadable method
            ResetTransform();
        }
    }
}