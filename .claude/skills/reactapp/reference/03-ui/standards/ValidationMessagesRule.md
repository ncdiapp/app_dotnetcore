# RULE: API Validation Messages

## Standard pattern

When an API returns a validation result (e.g. after save/update), show it using the project standard:

```tsx
showValidationMessages(data.ValidationResult, true);
```

- **First argument**: `data.ValidationResult` from the API response.
- **Second argument**: `true` (show in Messages panel).

## When to use

- After save/update/delete API calls that return a `ValidationResult`.
- When `data.ValidationResult?.IsValid === false` or when you need to surface validation errors from the server.

## Do not

- Hardcode a separate success message when the API already returns one (e.g. avoid `showInfo('Saved')` in addition to API success).
- Use a different pattern for validation (e.g. manual mapping of items); use `showValidationMessages` for consistency.

## Example

```tsx
const data = await searchSvc.saveAppSearchExDto(payload);
if (data?.ValidationResult?.IsValid !== false && data?.Id != null) {
  setCurrentSearch(prev => (prev ? { ...prev, Id: data.Id, IsModified: false } : prev));
  await loadData();
} else {
  if (data?.ValidationResult) showValidationMessages(data.ValidationResult, true);
  const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage ?? i.LocalizedMessage).filter(Boolean) ?? [];
  showError(msgs.length ? msgs.join('; ') : 'Save failed');
}
```

## Related

- `FormStandards.md` – Form Validation > API Validation Result (Standard)
- `useErrorMessage` hook – provides `showValidationMessages`, `showError`, `showInfo`, `showWarning`
