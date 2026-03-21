---
name: skill-creator
description: Create or update Codex skills that follow the official structure and validation flow. Use when Codex needs to build a new skill folder, revise an existing SKILL.md, add bundled resources such as scripts or references, or generate agents/openai.yaml metadata for a project-local skill.
---

# Skill Creator

Create or update a Codex skill with the official folder structure and the minimum metadata needed for discovery.

## Workflow

1. Normalize the skill name to lowercase hyphen-case.
2. Create the folder at `.codex/skills/<skill-name>/`.
3. Add `SKILL.md` with YAML frontmatter that contains only `name` and `description`.
4. Add `agents/openai.yaml` when the skill should appear in UI skill lists.
5. Create only the resource folders that materially help repeated work:
   - `scripts/` for deterministic helpers
   - `references/` for detailed docs loaded only when needed
   - `assets/` for templates or output files
6. Keep `SKILL.md` short and procedural. Move long details into `references/`.
7. Validate folder naming, frontmatter, and UI metadata constraints before finishing.

## Writing Rules

- Use imperative instructions.
- Put all trigger conditions in the frontmatter `description`.
- Keep the body focused on steps, decision points, and references to bundled resources.
- Do not add extra documentation files such as `README.md` or `CHANGELOG.md` unless they are required as runtime resources.

## `agents/openai.yaml` Rules

- Quote all string values.
- Keep `interface.short_description` between 25 and 64 characters.
- Make `interface.default_prompt` a short example prompt that explicitly mentions `$skill-name`.
- Add optional fields such as icons or brand color only when the user explicitly provides them.

## Recommended Structure

Use this baseline unless the skill clearly needs more:

```text
skill-name/
|-- SKILL.md
|-- agents/
|   `-- openai.yaml
`-- references/        # optional
```

Add `scripts/` or `assets/` only when they remove repeated work or capture deterministic behavior.

## Validation Checklist

Confirm these points before finishing:

- Folder name matches the skill name.
- `SKILL.md` frontmatter contains exactly `name` and `description`.
- The description states both what the skill does and when it should trigger.
- `agents/openai.yaml` respects field length and formatting limits.
- Resource folders are minimal and relevant.
