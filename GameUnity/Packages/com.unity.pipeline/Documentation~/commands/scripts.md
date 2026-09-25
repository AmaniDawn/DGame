# Script commands

Commands for creating C# script files, attaching MonoBehaviours to GameObjects, and reading/writing serialized fields. Writing a `.cs` file does not make its type available — Unity must import and compile it (a domain reload) first. The flow is: `create_script` → `recompile` → poll `recompile_status` (until completed/up_to_date) → `attach_script`.

### `create_script`
Create a new C# script (default base class MonoBehaviour) from a template under the authoring root. NOTE: the type does not exist until a recompile completes — to attach it, call recompile, poll recompile_status, then attach_script.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `name` | yes | – | Class/file name without extension, e.g. PlayerController. Must be a valid C# identifier. |
| `path` | no | `–` | Folder (relative to the authoring root; the Assets/ prefix is optional) to write the .cs into. Defaults to the authoring root. |
| `namespace` | no | `–` | Optional namespace to wrap the class in. Omit for the global namespace. |
| `base_class` | no | `MonoBehaviour` | Base class to derive from. Defaults to MonoBehaviour. |
| `overwrite` | no | `false` | Overwrite the file if it already exists. Defaults to false (an existing file is an error). |

**Returns:** `AuthoringResult`
**Notes:** Does not trigger a recompile; the created type is not usable until a recompile completes (poll `recompile_status`).

### `attach_script`
Add a MonoBehaviour to a GameObject by its (compiled) type name OR by its script asset path. Provide exactly one of 'type' or 'script'. If the type isn't compiled yet, returns a recoverable error: recompile, poll recompile_status, then retry.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `target` | yes | – | Reference to the GameObject to add the component to (globalId/path/guid/instanceId/hierarchyPath). |
| `type` | no | `–` | Component type name to add, e.g. PlayerController or Game.Player.PlayerController. Must already be compiled. Mutually exclusive with 'script'. |
| `script` | no | `–` | Script asset path, e.g. 'Assets/Pool/Scripts/CueShooter.cs'. The backing class is resolved via MonoScript.GetClass(), so the class name may differ from the filename. Mutually exclusive with 'type'. |

**Returns:** `AuthoringResult`
**Notes:** Undo-able. Provide exactly one of `type` or `script`. A not-yet-compiled type returns a recoverable error (recompile, poll `recompile_status`, then retry).

### `set_serialized_field`
Set a serialized field on a component/asset. Supports primitives, enums, Vector/Color/Rect/Bounds, object references (value = an ObjectRef: asset by guid/fileId/path or scene object by instanceId/hierarchyPath), and array elements via 'name.Array.data[i]' (or 'name.Array.size' to resize).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `target` | yes | – | Reference to the component or asset to modify (globalId/path/guid/instanceId/hierarchyPath). May be a GameObject when 'component' is given. |
| `field` | yes | – | SerializedProperty path, e.g. 'speed', 'settings.speed', or 'waypoints.Array.data[0]'. |
| `value` | yes | – | JSON value to assign. For object references pass an ObjectRef object (or null to clear). For enums pass the value name. |
| `component` | no | `–` | Component type name on the target GameObject (e.g. 'Rigidbody'). Use when 'target' is a GameObject; omit when 'target' is already a component handle. |

**Returns:** `AuthoringResult`
**Notes:** Undo-able.

### `get_serialized_fields`
Read serialized fields of a component/asset. Returns each top-level field's name, type and value (object references are returned as re-usable handles). Pass 'field' to read a single SerializedProperty path.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `target` | yes | – | Reference to the component or asset to read (globalId/path/guid/instanceId/hierarchyPath). May be a GameObject when 'component' is given. |
| `field` | no | `–` | Optional single SerializedProperty path to read (e.g. 'speed' or 'items.Array.data[0]'). Omit to read all top-level fields. |
| `component` | no | `–` | Component type name on the target GameObject (e.g. 'Rigidbody'). Use when 'target' is a GameObject; omit when 'target' is already a component handle. |

**Returns:** `object`

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
