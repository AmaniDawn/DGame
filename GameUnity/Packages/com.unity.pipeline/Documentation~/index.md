# Pipeline Documentation

`com.unity.pipeline` lets a client — CLI, CI, or an agent — drive a running Unity Editor (or a
development Player) over a local HTTP API by executing registered commands. This is the documentation
index; the [README](../README.md) has the full narrative overview, install steps, and CLI walkthrough.

## Guides

| Page | What it covers |
|------|----------------|
| [Creating commands](creating-commands.md) | The command-authoring API: `[CliCommand]`/`[CliArg]`, the handler + response pattern, and the `MainThreadRequired` / `RuntimeOnly` flags. |
| [Creating authoring commands](authoring-commands.md) | Building content-authoring commands: the authoring root sandbox, `ObjectRef` in / `AuthoringResult` out, and undo grouping — with a worked example for a new content type. |
| [Safety & mutations](safety-and-mutations.md) | Conventions shared by state-changing commands: the `confirm`/`dry_run` gate, Undo grouping via `AuthoringUndoScope`, and the path sandbox. |
| [Connectivity](connectivity.md) | How servers bind localhost, the port ranges, the port/descriptor file, and the bearer-token auth workflow. |
| [Runtime connection & setup](runtime-setup.md) | Running the server in a development Player via `RuntimePipelineManager` and the dev-build gating. |
| [Hot reload](hot-reload.md) | The two hot-reload flavors (in-place and override) with examples, plus the Roslyn / in-memory-assembly architecture. |
| [Tests architecture](testing.md) | Writing command tests via `PipelineClient` (over HTTP) and via direct command calls. |

## Command reference

| Page | Commands |
|------|----------|
| [Asset & file commands](commands/assets-and-files.md) | Create / import / move / copy / rename / delete / find assets, import settings, folders, text files. |
| [Scene commands](commands/scenes.md) | Create / open / save scenes, build-settings list, hierarchy, active scene. |
| [GameObject & component commands](commands/gameobjects-and-components.md) | Create / find / transform / parent / tag / layer GameObjects; add / remove / read / set components. |
| [Prefab commands](commands/prefabs.md) | Create, instantiate, variant, apply / revert overrides, unpack, edit prefab contents. |
| [Script commands](commands/scripts.md) | Create and attach scripts; get / set serialized fields. |
| [Animation commands](commands/animation.md) | Create AnimationClips (+ curves), AnimatorControllers (parameters / layers / states / transitions), and Timeline assets. |
| [Material & shader commands](commands/materials.md) | Read / set material shader properties and keywords; list and introspect shaders. |
| [Baking commands](commands/baking.md) | Bake / clear lighting, NavMesh, and occlusion culling (async — poll the matching `*_bake_status`). |
| [Navigation & selection commands](commands/navigation.md) | Read / set the Editor selection; run Unity Search queries. |
| [Capture commands](commands/capture.md) | Screenshot the game / scene view and capture UI Toolkit visual elements. |
| [Build, compilation & test commands](commands/build-and-compilation.md) | Build the Player, switch build target, read/write build settings, list targets/profiles, recompile scripts, and run tests (async — poll the matching `*_status`). |
| [Project settings commands](commands/project-settings.md) | Read / change Audio, Graphics, Input, Physics, Player, Quality, Tags & Layers, and Time settings (`get_*`/`set_*`, gated by `confirm`/`dry_run`). |
| [Package Manager commands](commands/package-manager.md) | List / search / add / remove / resolve UPM packages and poll operation status. |
| [Editor lifecycle & observability commands](commands/editor-lifecycle-and-observability.md) | Play / stop / pause, focus, menus, screenshots, console logs, performance stats, authoring root. |
| [Runtime commands](commands/runtime.md) | Player-side commands: status, quit, frame rate, time scale, log, console, eval, hot reload. |

## Command index

Every available command, grouped by area. Each name links to its full reference (parameters + return type).

### Asset & file commands

| Command | Description |
|---------|-------------|
| [`create_asset`](commands/assets-and-files.md#create_asset) | Create a ScriptableObject / Object asset at a path. |
| [`import_asset`](commands/assets-and-files.md#import_asset) | Import an external file into the project. |
| [`move_asset`](commands/assets-and-files.md#move_asset) | Move / rename an asset (keeps its GUID). |
| [`copy_asset`](commands/assets-and-files.md#copy_asset) | Copy an asset (fresh GUID). |
| [`rename_asset`](commands/assets-and-files.md#rename_asset) | Rename an asset in place. |
| [`delete_asset`](commands/assets-and-files.md#delete_asset) | Delete an asset (needs confirm). |
| [`find_assets`](commands/assets-and-files.md#find_assets) | Find assets by type / name / label. |
| [`set_import_settings`](commands/assets-and-files.md#set_import_settings) | Set an asset's importer settings and re-import. |
| [`get_import_settings`](commands/assets-and-files.md#get_import_settings) | Read an asset's importer settings. |
| [`create_folder`](commands/assets-and-files.md#create_folder) | Create a folder under the authoring root. |
| [`read_text_file`](commands/assets-and-files.md#read_text_file) | Read a UTF-8 text file. |
| [`write_text_file`](commands/assets-and-files.md#write_text_file) | Write a UTF-8 text file, then import it. |

### Scene commands

| Command | Description |
|---------|-------------|
| [`create_scene`](commands/scenes.md#create_scene) | Create and save a new scene. |
| [`open_scene`](commands/scenes.md#open_scene) | Open an existing scene. |
| [`save_scene`](commands/scenes.md#save_scene) | Save an open scene. |
| [`save_all`](commands/scenes.md#save_all) | Save all dirty open scenes. |
| [`list_open_scenes`](commands/scenes.md#list_open_scenes) | List open scenes and their state. |
| [`set_active_scene`](commands/scenes.md#set_active_scene) | Set which open scene is active. |
| [`get_scene_hierarchy`](commands/scenes.md#get_scene_hierarchy) | Return a scene's GameObject tree. |
| [`add_scene_to_build`](commands/scenes.md#add_scene_to_build) | Add a scene to Build Settings. |
| [`remove_scene_from_build`](commands/scenes.md#remove_scene_from_build) | Remove a scene from Build Settings. |

### GameObject & component commands

| Command | Description |
|---------|-------------|
| [`create_gameobject`](commands/gameobjects-and-components.md#create_gameobject) | Create a GameObject or primitive. |
| [`create_gameobjects`](commands/gameobjects-and-components.md#create_gameobjects) | Batch-create GameObjects / primitives. |
| [`find_gameobjects`](commands/gameobjects-and-components.md#find_gameobjects) | Find GameObjects by name / tag / component / path. |
| [`set_transform`](commands/gameobjects-and-components.md#set_transform) | Set local position / rotation / scale. |
| [`set_parent`](commands/gameobjects-and-components.md#set_parent) | Reparent a GameObject (or detach to root). |
| [`set_active`](commands/gameobjects-and-components.md#set_active) | Set a GameObject's active state. |
| [`set_tag`](commands/gameobjects-and-components.md#set_tag) | Set a GameObject's tag. |
| [`set_layer`](commands/gameobjects-and-components.md#set_layer) | Set a GameObject's layer. |
| [`rename_gameobject`](commands/gameobjects-and-components.md#rename_gameobject) | Rename a GameObject. |
| [`delete_gameobject`](commands/gameobjects-and-components.md#delete_gameobject) | Delete a GameObject (undoable). |
| [`add_component`](commands/gameobjects-and-components.md#add_component) | Add a component by type name. |
| [`remove_component`](commands/gameobjects-and-components.md#remove_component) | Remove a component. |
| [`get_component_properties`](commands/gameobjects-and-components.md#get_component_properties) | Read a component's serialized properties. |
| [`set_component_properties`](commands/gameobjects-and-components.md#set_component_properties) | Set a component's serialized properties. |

### Prefab commands

| Command | Description |
|---------|-------------|
| [`create_prefab`](commands/prefabs.md#create_prefab) | Save a GameObject as a prefab. |
| [`instantiate_prefab`](commands/prefabs.md#instantiate_prefab) | Instantiate a prefab into a scene. |
| [`create_prefab_variant`](commands/prefabs.md#create_prefab_variant) | Create a prefab variant. |
| [`apply_prefab_overrides`](commands/prefabs.md#apply_prefab_overrides) | Apply instance overrides to the source. |
| [`revert_prefab_overrides`](commands/prefabs.md#revert_prefab_overrides) | Revert instance overrides. |
| [`unpack_prefab`](commands/prefabs.md#unpack_prefab) | Unpack a prefab instance. |
| [`save_prefab_contents`](commands/prefabs.md#save_prefab_contents) | Edit a prefab in an isolated stage and save. |

### Script commands

| Command | Description |
|---------|-------------|
| [`create_script`](commands/scripts.md#create_script) | Create a C# script from a template. |
| [`attach_script`](commands/scripts.md#attach_script) | Attach a MonoBehaviour by type / asset. |
| [`set_serialized_field`](commands/scripts.md#set_serialized_field) | Set a serialized field on a component / asset. |
| [`get_serialized_fields`](commands/scripts.md#get_serialized_fields) | Read serialized fields. |

### Animation commands

| Command | Description |
|---------|-------------|
| [`create_animation_clip`](commands/animation.md#create_animation_clip) | Create an empty .anim AnimationClip. |
| [`set_animation_curve`](commands/animation.md#set_animation_curve) | Add / replace a float curve on a clip. |
| [`get_animation_clip`](commands/animation.md#get_animation_clip) | Read a clip's curves and metadata. |
| [`remove_animation_curve`](commands/animation.md#remove_animation_curve) | Remove a curve binding (needs confirm). |
| [`create_animator_controller`](commands/animation.md#create_animator_controller) | Create an .controller AnimatorController. |
| [`add_animator_parameter`](commands/animation.md#add_animator_parameter) | Add a parameter to a controller. |
| [`add_animator_layer`](commands/animation.md#add_animator_layer) | Add a layer to a controller. |
| [`add_animator_state`](commands/animation.md#add_animator_state) | Add a state to a layer. |
| [`add_animator_transition`](commands/animation.md#add_animator_transition) | Add a transition between states. |
| [`get_animator_controller`](commands/animation.md#get_animator_controller) | Read a controller's full structure. |
| [`create_timeline`](commands/animation.md#create_timeline) | Create a .playable Timeline asset. |
| [`add_timeline_track`](commands/animation.md#add_timeline_track) | Add a track to a Timeline. |
| [`add_timeline_clip`](commands/animation.md#add_timeline_clip) | Add a clip to a Timeline track. |
| [`get_timeline`](commands/animation.md#get_timeline) | Read a Timeline's structure. |

### Material & shader commands

| Command | Description |
|---------|-------------|
| [`get_material_properties`](commands/materials.md#get_material_properties) | Read a material's shader and properties. |
| [`set_material_properties`](commands/materials.md#set_material_properties) | Set material shader properties / keywords. |
| [`list_shaders`](commands/materials.md#list_shaders) | Discover available shaders. |
| [`get_shader_properties`](commands/materials.md#get_shader_properties) | Introspect a shader's property list. |

### Baking commands

| Command | Description |
|---------|-------------|
| [`bake_lighting`](commands/baking.md#bake_lighting) | Async lightmap bake (poll `lighting_bake_status`). |
| [`lighting_bake_status`](commands/baking.md#lighting_bake_status) | Status of the last lighting bake. |
| [`cancel_lighting_bake`](commands/baking.md#cancel_lighting_bake) | Cancel an in-progress lighting bake. |
| [`clear_baked_lighting`](commands/baking.md#clear_baked_lighting) | Clear baked lightmaps (needs confirm). |
| [`get_lighting_settings`](commands/baking.md#get_lighting_settings) | Read the active LightingSettings. |
| [`set_lighting_settings`](commands/baking.md#set_lighting_settings) | Apply a subset of lighting settings. |
| [`bake_navmesh`](commands/baking.md#bake_navmesh) | Async legacy NavMesh bake (poll `navmesh_bake_status`). |
| [`navmesh_bake_status`](commands/baking.md#navmesh_bake_status) | Status of the last NavMesh bake. |
| [`cancel_navmesh_bake`](commands/baking.md#cancel_navmesh_bake) | Cancel an in-progress NavMesh bake. |
| [`clear_navmesh`](commands/baking.md#clear_navmesh) | Clear the baked NavMesh (needs confirm). |
| [`get_navmesh_settings`](commands/baking.md#get_navmesh_settings) | Read legacy NavMesh bake settings. |
| [`set_navmesh_settings`](commands/baking.md#set_navmesh_settings) | Apply a subset of NavMesh settings. |
| [`bake_navmesh_surfaces`](commands/baking.md#bake_navmesh_surfaces) | Bake NavMeshSurface components (AI Navigation). |
| [`bake_occlusion_culling`](commands/baking.md#bake_occlusion_culling) | Async occlusion bake (poll `occlusion_bake_status`). |
| [`occlusion_bake_status`](commands/baking.md#occlusion_bake_status) | Status of the last occlusion bake. |
| [`cancel_occlusion_bake`](commands/baking.md#cancel_occlusion_bake) | Cancel an in-progress occlusion bake. |
| [`clear_occlusion_culling`](commands/baking.md#clear_occlusion_culling) | Clear baked occlusion data (needs confirm). |

### Navigation & selection commands

| Command | Description |
|---------|-------------|
| [`get_selection`](commands/navigation.md#get_selection) | Read the current Editor selection. |
| [`set_selection`](commands/navigation.md#set_selection) | Set the Editor selection. |
| [`search`](commands/navigation.md#search) | Run a Unity Search query. |

### Capture commands

| Command | Description |
|---------|-------------|
| [`capture_game_view`](commands/capture.md#capture_game_view) | Render a camera to a PNG (base64). |
| [`capture_scene_view`](commands/capture.md#capture_scene_view) | Render the Scene View to a PNG (base64). |
| [`capture_editor_element`](commands/capture.md#capture_editor_element) | Capture a UI Toolkit element from an EditorWindow (6000.7+). |
| [`capture_runtime_element`](commands/capture.md#capture_runtime_element) | Capture a UI Toolkit element from a runtime panel (6000.7+). |

### Build, compilation & test commands

| Command | Description |
|---------|-------------|
| [`build`](commands/build-and-compilation.md#build) | Async Player build (poll `build_status`). |
| [`build_status`](commands/build-and-compilation.md#build_status) | Status / report of the current build. |
| [`switch_build_target`](commands/build-and-compilation.md#switch_build_target) | Switch the active build target (needs confirm). |
| [`switch_build_target_status`](commands/build-and-compilation.md#switch_build_target_status) | Status of the last target switch. |
| [`list_build_targets`](commands/build-and-compilation.md#list_build_targets) | List known build targets. |
| [`get_build_settings`](commands/build-and-compilation.md#get_build_settings) | Read build configuration. |
| [`set_build_settings`](commands/build-and-compilation.md#set_build_settings) | Set build configuration fields. |
| [`list_build_profiles`](commands/build-and-compilation.md#list_build_profiles) | List Build Profile assets (Unity 6). |
| [`recompile`](commands/build-and-compilation.md#recompile) | Force a script recompile (poll `recompile_status`). |
| [`recompile_status`](commands/build-and-compilation.md#recompile_status) | Status of the last recompile. |
| [`list_tests`](commands/build-and-compilation.md#list_tests) | List available tests without running. |
| [`run_tests`](commands/build-and-compilation.md#run_tests) | Run Unity tests with filters. |
| [`test_status`](commands/build-and-compilation.md#test_status) | Status of async test execution. |
| [`cancel_tests`](commands/build-and-compilation.md#cancel_tests) | Cancel running tests. |

### Project settings commands

| Command | Description |
|---------|-------------|
| [`get_audio_settings`](commands/project-settings.md#get_audio_settings) | Read Audio settings. |
| [`set_audio_settings`](commands/project-settings.md#set_audio_settings) | Change Audio settings (needs confirm). |
| [`get_graphics_settings`](commands/project-settings.md#get_graphics_settings) | Read Graphics settings. |
| [`set_graphics_settings`](commands/project-settings.md#set_graphics_settings) | Set the default render pipeline (needs confirm). |
| [`get_input_settings`](commands/project-settings.md#get_input_settings) | Read legacy Input Manager axes. |
| [`set_input_settings`](commands/project-settings.md#set_input_settings) | Tune a legacy input axis (needs confirm). |
| [`get_physics_settings`](commands/project-settings.md#get_physics_settings) | Read Physics settings. |
| [`set_physics_settings`](commands/project-settings.md#set_physics_settings) | Change Physics settings (needs confirm). |
| [`get_player_settings`](commands/project-settings.md#get_player_settings) | Read PlayerSettings. |
| [`set_player_settings`](commands/project-settings.md#set_player_settings) | Change PlayerSettings (needs confirm). |
| [`get_quality_settings`](commands/project-settings.md#get_quality_settings) | Read QualitySettings. |
| [`set_quality_settings`](commands/project-settings.md#set_quality_settings) | Change QualitySettings (needs confirm). |
| [`get_tags_layers`](commands/project-settings.md#get_tags_layers) | Read tags and layers. |
| [`set_tags_layers`](commands/project-settings.md#set_tags_layers) | Add / remove tags, name layers (needs confirm). |
| [`get_time_settings`](commands/project-settings.md#get_time_settings) | Read Time settings. |
| [`set_time_settings`](commands/project-settings.md#set_time_settings) | Change Time settings (needs confirm). |

### Package Manager commands

| Command | Description |
|---------|-------------|
| [`package_list`](commands/package-manager.md#package_list) | List packages by scope. |
| [`package_search`](commands/package-manager.md#package_search) | Search the registry. |
| [`package_add`](commands/package-manager.md#package_add) | Add a UPM package (needs confirm). |
| [`package_remove`](commands/package-manager.md#package_remove) | Remove a UPM package (needs confirm). |
| [`package_resolve`](commands/package-manager.md#package_resolve) | Resolve / refresh packages. |
| [`package_status`](commands/package-manager.md#package_status) | Status of the last package operation. |

### Editor lifecycle & observability commands

| Command | Description |
|---------|-------------|
| [`editor_play`](commands/editor-lifecycle-and-observability.md#editor_play) | Enter play mode. |
| [`editor_stop`](commands/editor-lifecycle-and-observability.md#editor_stop) | Exit play mode. |
| [`editor_pause`](commands/editor-lifecycle-and-observability.md#editor_pause) | Pause play mode. |
| [`editor_status`](commands/editor-lifecycle-and-observability.md#editor_status) | Detailed Editor status. |
| [`editor_focus`](commands/editor-lifecycle-and-observability.md#editor_focus) | Bring the Editor to the foreground. |
| [`menu`](commands/editor-lifecycle-and-observability.md#menu) | Execute (or list) Editor menu items. |
| [`screenshot`](commands/editor-lifecycle-and-observability.md#screenshot) | Capture Scene / Game view to a PNG file. |
| [`set_autotick`](commands/editor-lifecycle-and-observability.md#set_autotick) | Keep the editor ticking while unfocused. |
| [`get_console_logs`](commands/editor-lifecycle-and-observability.md#get_console_logs) | Read captured Editor console logs. |
| [`clear_console`](commands/editor-lifecycle-and-observability.md#clear_console) | Clear the console / log buffer. |
| [`get_performance_stats`](commands/editor-lifecycle-and-observability.md#get_performance_stats) | Read render / memory / frame stats. |
| [`get_authoring_root`](commands/editor-lifecycle-and-observability.md#get_authoring_root) | Get the authoring-root folder. |
| [`set_authoring_root`](commands/editor-lifecycle-and-observability.md#set_authoring_root) | Set the authoring-root folder. |

### Runtime commands

| Command | Description |
|---------|-------------|
| [`runtime_status`](commands/runtime.md#runtime_status) | Runtime application status. |
| [`quit`](commands/runtime.md#quit) | Quit the application. |
| [`set_target_framerate`](commands/runtime.md#set_target_framerate) | Set the target frame rate. |
| [`set_timescale`](commands/runtime.md#set_timescale) | Set the time scale. |
| [`simulate_key`](commands/runtime.md#simulate_key) | Simulate a keyboard event (Input System). |
| [`simulate_pointer`](commands/runtime.md#simulate_pointer) | Simulate a pointer event (Input System). |
| [`log`](commands/runtime.md#log) | Write a message to the Unity console. |
| [`console`](commands/runtime.md#console) | Read captured console output. |
| [`eval`](commands/runtime.md#eval) | Evaluate C# via Roslyn. |
| [`eval_file`](commands/runtime.md#eval_file) | Evaluate C# from a .cs file. |
| [`reload_file`](commands/runtime.md#reload_file) | Apply in-place [HotReload] edits from a file. |
| [`reload_file_override`](commands/runtime.md#reload_file_override) | Compile & apply override hot reload. |
| [`hotreload_status`](commands/runtime.md#hotreload_status) | Hot reload registry status. |
| [`cleanup_hotreload`](commands/runtime.md#cleanup_hotreload) | Clear old hot reload DLLs / registry. |
