Fix Tailwind flexbox remaining-space violations in a React component.

**Rule**: Never use `flex-1`. Use `w-1 flex-auto` (horizontal/row layout) or `h-1 flex-auto` (vertical/column layout).

**Reference**: `.claude/react-app/reference/03-ui/layout/TailwindFlexBoxRemainSpace.md`

**Workflow**:
1. Read the component at the given path.
2. Find every `flex-1` occurrence.
3. For each, check the parent's className:
   - Parent has `flex-col` → replace with `h-1 flex-auto`
   - Parent has `flex` (no `flex-col`) → replace with `w-1 flex-auto`
4. Fix all occurrences and verify no new errors.

Argument: React component path to fix.
