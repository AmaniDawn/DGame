# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.1-exp.1] - 2026-07-16
- Update docs

## [0.3.0-exp.1] - 2026-07-13

- Improve security by ensuring token usage and enforcing read control on the token.
- Fix all upm-pvp warnings.
- Fix Samples installation.
- All server works if the App is minimized (in the RunInBackground).
- Rework all docs.
- Improve connectivity regaridng IPv4 vs IPv6 support.
- Add `eval_file` command: evaluate C# code read from a `.cs` file on disk, as a file-based alternative to `eval` (which takes inline `code`). Both commands run the source through the same evaluation path.
- Add a large set of Editor automation commands for agentic content-pipeline control:
  - **Assets & files:** `create_asset`, `import_asset`, `move_asset`, `copy_asset`, `rename_asset`, `delete_asset`, `find_assets`, `create_folder`, `get_import_settings`, `set_import_settings`, `read_text_file`, `write_text_file`.
  - **Scenes:** `create_scene`, `open_scene`, `save_scene`, `save_all`, `list_open_scenes`, `set_active_scene`, `get_scene_hierarchy`, `add_scene_to_build`, `remove_scene_from_build`.
  - **GameObjects & components:** `create_gameobject`, `create_gameobjects`, `delete_gameobject`, `find_gameobjects`, `rename_gameobject`, `set_parent`, `set_transform`, `set_active`, `set_tag`, `set_layer`, `add_component`, `remove_component`, `get_component_properties`, `set_component_properties`.
  - **Prefabs:** `create_prefab`, `create_prefab_variant`, `instantiate_prefab`, `apply_prefab_overrides`, `revert_prefab_overrides`, `unpack_prefab`, `save_prefab_contents`.
  - **Scripts & serialized fields:** `create_script`, `attach_script`, `get_serialized_fields`, `set_serialized_field`.
  - **Selection & search:** `get_selection`, `set_selection`, `search`.
  - **Capture & screenshots:** `screenshot`, `capture_game_view`, `capture_scene_view`, `capture_editor_element`, `capture_runtime_element`.
  - **Build:** `build`, `build_status`.
  - **Console & diagnostics:** `console`, `clear_console`, `get_console_logs`, `get_performance_stats`.
  - **Editor menus & authoring root:** `menu`, `get_authoring_root`, `set_authoring_root`.
  - **Materials & shaders:** `get_material_properties`, `set_material_properties`, `get_shader_properties`, `list_shaders`.
  - **Animation & Animator:** `create_animation_clip`, `get_animation_clip`, `set_animation_curve`, `remove_animation_curve`, `create_animator_controller`, `get_animator_controller`, `add_animator_layer`, `add_animator_parameter`, `add_animator_state`, `add_animator_transition`.
  - **Timeline:** `create_timeline`, `get_timeline`, `add_timeline_track`, `add_timeline_clip`.
  - **Lighting:** `bake_lighting`, `cancel_lighting_bake`, `lighting_bake_status`, `clear_baked_lighting`, `get_lighting_settings`, `set_lighting_settings`.
  - **NavMesh:** `bake_navmesh`, `bake_navmesh_surfaces`, `cancel_navmesh_bake`, `navmesh_bake_status`, `clear_navmesh`, `get_navmesh_settings`, `set_navmesh_settings`.
  - **Occlusion culling:** `bake_occlusion_culling`, `cancel_occlusion_bake`, `occlusion_bake_status`, `clear_occlusion_culling`.
- Update Wrench
- Warn user if Unity Editor is started in non-automated mode.

## [0.2.0-exp.2] - 2026-06-24

- Fix security audit flaws
- First official published version

## [0.1.0-exp.1] - 2026-06-09

### This is the first release of _Unity Pipeline_.
