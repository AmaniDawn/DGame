# Scene Preview

Build a content archive (`.ca`) for any asset on demand, and load it into a running player. A preview player built from your project can then show an asset an artist just pushed, inside a scene you control, without a full game build. The **Scene Preview** sample in `com.unity.pipeline.samples` is a complete preview player that uses this through Project Service.

The package provides three pieces:

| Piece | Assembly | Purpose |
|-------|----------|---------|
| `PreviewSceneArtifactImporter` | `Unity.Pipeline.Editor` (`Unity.Pipeline.Editor.Previews`) | Builds the archive for one asset, for the platform the import runs under. |
| `ContentArchiveLoader` | `Unity.Pipeline.ScenePreview` | Mounts an archive in a player and loads its content. |
| Scene Preview settings | `Assets/Settings/ScenePreview/ScenePreviewSettings.asset`, edited in **Project Settings → Pipeline → Editor** | The preview build profile and the assemblies a preview may contain. |

`Unity.Pipeline.ScenePreview` compiles into a player only when the `ENABLE_PIPELINE_SCENE_PREVIEW` scripting define is set, so your game's own builds leave it out. The preview build profile sets it, independently of the **Project Settings → Pipeline → Runtime** toggles.

Project Service supports Unity 6000.7 and later, so previews served through it need one of those versions. The importer and the loader work on every version `com.unity.pipeline` supports, so you can still produce an archive locally (see [Producing an archive](#producing-an-archive)) and load it on older versions.

## Setting up

1. Make a preview scene: your lighting, volume and camera, and a place to show the asset.
2. In **Project Settings → Pipeline → Editor → Scene Preview**, with the preview platform active and the preview scene open, press **Create Preview Build Profile**. It creates `Assets/Settings/Build Profiles/ScenePreview.asset`, a build profile whose only scene is the open one and whose scripting defines include `ENABLE_PIPELINE_SCENE_PREVIEW`, and selects it as the **Preview Build Profile**. You can select any other build profile instead; the section offers to add the define to it, and a build of the preview build profile fails without it.
3. Press **Generate Preview Assemblies**. It lists the engine and package assemblies a preview may contain. The section keeps both settings in `Assets/Settings/ScenePreview/ScenePreviewSettings.asset`.
4. Commit the build profile and the settings asset. Project Service imports from the repository, so both must be in it.
5. Build the preview player from the preview build profile.

## What the settings do

- **Preview Build Profile.** The importer strips shader variants against the lighting and fog modes of this profile's scenes, so an archive only carries the variants the preview player can render. When none is set, the active build profile is used and every import logs a warning.
- **Preview assemblies.** The importer drops any object whose type is not in one of these assemblies, so an archive never references a type the player stripped. Builds of the preview build profile receive them as a link.xml, generated under `Temp/`; your game's own builds keep their normal stripping. With no assemblies listed, nothing is filtered or added.

Every preview import depends on the settings asset, so changing it, from the settings page or through version control, rebuilds existing previews, and so does creating it.

A preview is also rebuilt when anything it was built from changes: the asset or an asset it references, the preview build profile, one of its scenes or that scene's lighting settings and baked lighting data, the render pipeline assets in use and the assets they reference, or the Graphics and Quality settings.

## Producing an archive

Project Service runs the importer on demand with the import address `T:{guid}+Unity.Pipeline.Editor.Previews.PreviewSceneArtifactImporter` and the artifact name `ca`.

To build one locally, without Project Service:

```
Unity -batchmode -projectPath <project> -buildTarget <platform> \
  -executeMethod Unity.Pipeline.Editor.Previews.PreviewProducer.ProduceFromCommandLine \
  -previewAsset Assets/…/Church.prefab -previewOut /tmp/church.ca
```

From editor code, `PreviewSceneArtifactImporter.Produce(assetPath)` builds it and `PreviewSceneArtifactImporter.GetArchivePath(assetPath)` returns where it is. Your open scenes are left as they are, unsaved changes included.

## Loading an archive

```csharp
var loader = ContentArchiveLoader.Load(path);
yield return loader.WaitForLoad();
if (loader.Succeeded)
    instance = Instantiate(loader.Objects.OfType<GameObject>().First());
…
DestroyImmediate(instance);
loader.Dispose();
```

- Create one loader per archive. Each mounts under a fresh prefix, because a remounted prefix yields a content file that never finishes loading.
- Destroy every instance with `DestroyImmediate` before `Dispose`. A deferred `Destroy` keeps the content file referenced, and its unload times out.
- If the unload times out, `Dispose` logs an error and leaves the archive mounted rather than unmounting it under live content. Call `Dispose` again once the instances are gone.

Each archive holds one content file and an `archive_manifest.json` naming it and saying whether it references the engine's default resources. The loader reads it; its format is internal to the package and numbered, and the loader refuses an archive of another format with an error naming both, so a player and its archives must come from the same package version.
