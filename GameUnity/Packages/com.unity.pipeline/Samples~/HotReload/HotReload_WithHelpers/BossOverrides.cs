using Unity.Pipeline.HotReload;
using UnityEngine;

/// <summary>
/// Hot reload overrides for <see cref="BossController"/> (helper workflow).
///
/// The override lives in its OWN file and does NOT redeclare BossController, so it binds to the
/// running type. Edit the tweaked logic here, then apply it without leaving play mode:
///   unity command reload_file_override "(absolute path)/Samples/HotReload_WithHelpers/BossOverrides.cs"
/// </summary>
public static class BossOverrides
{
    [HotReloadOverrideMethod("BossController.Update")]
    public static void TweakedUpdate(BossController instance)
    {
        // Tweaked behaviour: add a color pulse for visual feedback.
        var renderer = instance.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.Lerp(Color.red, Color.yellow, Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f);
        }
    }
}
