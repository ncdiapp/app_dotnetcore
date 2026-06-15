# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

```bash
npm start          # Development server at http://localhost:3000
npm test           # Jest test runner (interactive watch mode)
npm run build      # Production build to /build folder
```

The app proxies API requests to `http://localhost` (configured in package.json). Backend API base path is `/appai`.

## Architecture Overview

This is a React 18 + TypeScript enterprise application built with Create React App. It's a low-code/no-code platform for building business applications with forms, workflows, dashboards, and data management.

### State Management (Redux Toolkit)

- **Store**: `src/redux/store.ts` - Configured with custom middleware for tab persistence
- **Slices location**: `src/redux/features/`
  - `admin/userSessionSlice.ts` - Authentication and user context
  - `ui/navigation/tabnavSlice.ts` - Multi-tab navigation with localStorage persistence
  - `ui/feedback/` - Busy loader and error message state
  - `ui/theme/` - Light/dark theme management
- **Custom hooks**: `src/redux/hooks/` (useErrorMessage, useTheme)
- Tab state automatically persists to localStorage via middleware

### Routing

- **Entry**: `src/routes.tsx` - Auth guard with session restoration on page reload
- **Route definitions**: `src/routes.shared.tsx` - All authenticated routes
- Unauthenticated users redirect to `/login`
- Session ID stored in localStorage, restored on app mount

### API Layer

- **Services**: `src/webapi/` - Feature-based service modules
- **Endpoints**: `src/webapi/endpoints.ts` - Base URL: `/appai`
- Key services: `adminsvc.ts`, `schemaMetaDataSvc.ts`, `dynamiclayoutsvc.ts`, `dashboardsvc.ts`, `searchSvc.ts`
- SignalR used for real-time communication

### Component Organization

```
src/components/
├── admin/           # User management, app editor, security settings
├── formMgt/         # Form designer and runtime components
├── search/          # Search views (Grid, Calendar, Gantt, Pivot)
├── transaction/     # Data management, form builder
├── dashboard/       # Dashboard editor and widgets
├── project/         # Project management with Gantt
├── integration/     # Third-party API management
├── mainLayout/      # App shell (Header, Sidebar, TabHeaders, LandingPage)
├── common/          # Reusable utilities (BusyLoader, ErrorMessageButton)
└── public/          # Public pages (Home, 404)
```

### Key Libraries

- **Wijmo** (`@mescius/wijmo.*`) - Enterprise grids, inputs, charts
- **DayPilot** (via `/public/daypilot-all.min.js`) - Gantt charts
- **TW Elements** - Tailwind component library
- **SignalR** - Real-time backend communication

### Styling

- Tailwind CSS with PostCSS/Autoprefixer
- Wijmo styles imported separately
- Custom styles in `src/styles/`

## Code Conventions

- Unused variables/parameters can be prefixed with `_` (ESLint configured to allow)
- Feature-based folder organization for components and Redux slices
- Services in `webapi/` follow `*svc.ts` naming pattern
- Type definitions exported from slice files or dedicated `types.ts` files
- Use `appHelper.debugLog()` instead of `console.log()` for debug logging

## AngularJS Migration Project

This React app is being migrated from an AngularJS application. For migration work, see:
- **Migration Skill**: `.claude/skills/angularjs-to-react-migration.md`
- **Cursor (equivalent)**: `.cursor/skills/angularjs-to-react-migration/`, `.cursor/skills/analyze-migration/`, `.cursor/skills/fix-ui/`, `.cursor/skills/migrate/`
- **Full Documentation**: `prompt/` folder contains comprehensive guides

### Quick Migration Reference

**AngularJS Source Locations** (at `C:\DevApp\App\`):
- Controllers: `PlmApplication\Scripts1x\mgtCtrl\`
- Services: `PlmApplication\Scripts1x\Services\`
- WebAPI: `PlmApplication\Server\WebApi\`
- Business Logic: `APP.BL\`
- DTOs: `APP.Components.Dto\`

**Key Conversion Rules**:
- Keep Angular method names (PascalCase preserved)
- Always include query params in URL, use `${param || ''}` for null
- Never use `flex-1` in Tailwind - use `w-1 flex-auto` or `h-1 flex-auto`
- Use theme tokens (`theme.*`) - never hardcode colors
- Font Awesome 6 requires `fa-solid` prefix
- Wijmo FlexGridCellTemplate: use `cell.item` not `cell.dataItem`
- **Wijmo FlexGrid row selection**: In `selectionChanged`, get the grid with `const flex = s?.control ?? s`, then read the selected row with `flex.selection?.row` and `flex.rows[rowIndex]?.dataItem`. Do not rely on `collectionView.currentItem` (it may be stale when the event fires).
- **Wijmo CollectionView for FlexGrid**: Use a non-null default so `itemsSource` is never null. Initialize state with `useState<CollectionView>(() => new CollectionView<any>([]))` (or add sortDescriptions in the initializer). Do not use `CollectionView | null` for grid `itemsSource`—FlexGrid requires `any[] | ICollectionView | undefined`.
- **Wijmo FlexGrid fill container**: FlexGrid should fill its outer container div. Add `className="w-full h-full"` (or `className="h-full w-full"`) to the `<FlexGrid>` so it fills the parent; otherwise the grid may not size correctly inside flex or fixed-height containers.
- Use `useEnumValues('EmAppEnumName')` hook for enum values

### UI Standards

**Page Layout**:
```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
  <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
    <div className={`text-md font-semibold ${theme.title}`}>Title</div>
  </div>
  <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
    {/* Content */}
  </div>
</div>
```

**Button**: `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`

**Input**: `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`

**Label**: `w-32 text-xs ${theme.label} mr-2`
