using Unity.Pipeline.HotReload;
using UnityEngine;

public class BossController : MonoBehaviour
{
    void Awake()
    {
        // Run the sample in a 640x480 window instead of fullscreen.
        Screen.SetResolution(640, 480, FullScreenMode.Windowed);
    }

    // No manual registration needed: RuntimePipelineManager auto-discovers [HotReloadWithOverrides] methods.

    [HotReloadWithOverrides]
    void Update()
    {
        HotReloadHelper.ExecuteWithHotReload(this, "Update", OriginalUpdate);
    }

    public void OriginalUpdate()
    {
        transform.Rotate(45 * Time.deltaTime, 0, 0);
    }
}

