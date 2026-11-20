# Navigation & selection commands

Commands to read and drive the Editor selection, and to run Unity Search queries. All commands are read-only or non-destructive and run on the main thread. They write one record per item to stdout (one JSON object per line) and their errors and warnings to stderr; an empty selection or a search with no hits writes nothing and exits `Success`.

### `gameobject get_selection`
Read the current Editor selection as structured object identities.

No parameters.

**Returns:** one record per selected object: its `AuthoringResult` identity (`globalId`, `assetPath`, `guid`, `fileId`, `instanceId`, `hierarchyPath`, `type`) plus `active` (true for the selection's active object).

### `gameobject set_selection`
Set the Editor selection to the given assets/scene objects.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `instance-ids` | no | `–` | Scene/loaded object instance IDs to select. |
| `paths` | no | `–` | Asset paths to select (e.g. Assets/Foo.prefab). |

**Returns:** the new selection, as `gameobject get_selection` writes it. No inputs clears the selection and writes nothing.
**Fails:** `NotFound` when inputs were given and none resolved (the selection is unchanged). Inputs that don't resolve while others do are skipped with a warning on stderr.

### `editor search`
Run a Unity Search query and return structured results.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `query` | yes | `–` | Unity Search query string, e.g. 't:Material', 'p: my asset', 'h: Main Camera'. |
| `limit` | no | `50` | Max results to return (capped 200). |

**Returns:** one record per hit: `id`, `label`, `description`, `provider`, `path` (the asset path, when the item is an asset).
**Fails:** `InvalidUsage` for an empty `query`; `Failed` when Unity Search throws.

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
