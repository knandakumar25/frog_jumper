# Narrative JSON Files — Translation Guide

## Schema

Each locale file lives at `assets/narrative/{locale}.json` and follows this structure:

```json
{
  "meta": {
    "language": "es",
    "version": "1.0",
    "description": "Axiom Force — Spanish narrative data"
  },
  "acts": {
    "act2_arc1": { "title": "", "subtitle": "", "description": "" },
    "act2_arc2": { "title": "", "subtitle": "", "description": "" },
    "act2_arc3": { "title": "", "subtitle": "", "description": "" }
  },
  "levels": {
    "0": {
      "title": "",
      "arc": null,
      "setting": "",
      "objective": "",
      "solo_monologues": [],
      "dialogue_status": "pending",
      "dialogue": []
    }
  }
}
```

Each `dialogue` entry:
```json
{ "speaker": "Character Name", "type": "spoken", "text": "..." }
```

`type` values: `internal`, `spoken`, `radio`, `vo`, `combat`, `action_prompt`, `title_card`, `phase_header`, `mission_complete`

## Locale → File Mapping

| Locale string (used in Godot)  | File              |
|-------------------------------|-------------------|
| `en`                          | `en.json` ✅      |
| `english`                     | `english.json` *  |
| `es` / `spanish`              | `es.json`         |
| `fr` / `eurofrench`           | `fr.json`         |
| *(add more as translated)*    |                   |

> **Note:** This project's `global.gd` sets `TranslationServer` locale to the full word
> `"english"` rather than the ISO code `"en"`. `NarrativeManager.gd` will first look for
> `english.json`; if not found, it falls back to `en.json`.  
> To use standard ISO locale codes everywhere, update `global.gd`'s locale string.

## Adding a New Language

1. Copy `en.json` to `{locale}.json` (e.g. `es.json`).  
2. Update `meta.language`.  
3. Translate all `text` fields.  
4. Leave `dialogue_status` values as-is — they document script completeness in the source, not translation status.

## Dialogue Completeness (source English)

| Level | Status   | Notes                         |
|-------|----------|-------------------------------|
| 0     | pending  | Training level — no dialogue  |
| 1–13  | scripted | Full dialogue arrays           |
| 14    | pending  | Citadel Approach — no dialogue|
| 15–16 | scripted | Full dialogue arrays           |
