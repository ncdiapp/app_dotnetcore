# JSON Editing Standards

## Overview

When the app edits or serializes JSON (e.g. for display in a textarea, for save payload, or for API), output must be **pretty-printed** so users see readable format with line breaks and indentation.

## Rule: Pretty-Print JSON Output

**Always use 2-space indentation when stringifying JSON** that may be shown in the UI or stored for later display:

```ts
// ✅ Correct: readable format (2-space indent + newlines)
const jsonString = JSON.stringify(obj, null, 2);

// ❌ Avoid: minified (no line breaks, hard to read in memo/textarea)
const jsonString = JSON.stringify(obj);
```

**Where this applies:**

- Any code that builds or normalizes a JSON **string** for:
  - Display in a textarea / memo (e.g. Api Config Parameters, JSON sample data)
  - Payload fields that are JSON strings (e.g. `ApiconfigParameters`, `DefaultValue`)
- Helpers that parse then re-stringify JSON (e.g. `integrationPayloadHelper.ts`): use `JSON.stringify(result, null, 2)` so the result stays readable.

**Reference implementation:** `src/helper/integrationPayloadHelper.ts` — when normalizing `ApiconfigParameters` string, it uses `JSON.stringify(normalized, null, 2)`.

## Summary

| Context | Use |
|--------|-----|
| JSON string for UI / memo / config textarea | `JSON.stringify(value, null, 2)` |
| JSON only for API body (no user display) | Either is fine; prefer `null, 2` if the string might be shown later |
