# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an enterprise low-code/no-code platform (AppAI/AppBuilder) consisting of:
- **React Frontend** (`PlmApplication/AppReact/`) - React 18 + TypeScript SPA being migrated from AngularJS
- **.NET Backend** (`PlmApplication/`) - ASP.NET MVC 4 + Web API 2 with Entity Framework

## Build & Development Commands

### React Frontend
```bash
cd PlmApplication/AppReact
npm start          # Dev server at http://localhost:3000 (proxies to http://localhost/appai)
npm test           # Jest test runner (interactive watch mode)
npm run build      # Production build to /build folder
```

### .NET Backend
```bash
# Open solution in Visual Studio
# Solution file: "PLMS  Solution 555 old.sln"
# Main project: PlmApplication/AppBuilder.csproj
# IIS URL: http://localhost/appai/
```

## Architecture Overview

### Solution Structure
```
AppAI/
├── PlmApplication/           # Main web application (ASP.NET MVC + React)
│   ├── AppReact/             # React 18 + TypeScript SPA
│   ├── Server/WebApi/        # Web API controllers
│   └── Server/Models/        # MVC models
├── APP.BL/                   # Business logic layer
├── APP.Components.Dto/       # Data transfer objects
│   ├── EntityDto/            # Entity DTOs
│   ├── EntityExdto/          # Extended DTOs
│   ├── UserDefine/           # Custom DTOs (AppFormData, Search, ProjectWorkFlow)
│   └── AppEnums.cs           # Application enums
├── APP.Framework/            # Core framework utilities
├── APP.Components.EntityConverter/  # Entity conversion utilities
├── DatabaseSchemaReader/     # Schema reading utilities
└── DataExChange/             # Data exchange modules
```

### React App Architecture (`PlmApplication/AppReact/src/`)
- **State**: Redux Toolkit with slices in `redux/features/`
  - `admin/userSessionSlice.ts` - Authentication and user context
  - `ui/navigation/tabnavSlice.ts` - Multi-tab navigation with localStorage persistence
  - `ui/theme/themeSlice.ts` - Light/dark theme management
- **Routing**: `routes.tsx` (auth guard), `routes.shared.tsx` (route definitions)
- **API Layer**: `webapi/*svc.ts` services; base URL `/appai` in `endpoints.ts`
- **Components**: `components/` organized by feature (admin, formMgt, search, transaction, dashboard, project, integration, mainLayout, common)

### Backend WebAPI Controllers (`PlmApplication/Server/WebApi/`)
Key controllers: `AdministrationController`, `AppTransactionController`, `AppSearchController`, `DynamicLayoutController`, `ProjectWorkFlowController`, `DashBoardController`, `IntegrationController`, `SchemaMetaDataController`

## Code Conventions

### React/TypeScript
- Use `appHelper.debugLog()` instead of `console.log()` for debug logging
- Services follow `*svc.ts` naming; methods use PascalCase (matching Angular originals)
- Unused variables can be prefixed with `_` (ESLint configured to allow)
- API calls: always include query params in URL, use `${param || ''}` for null values
- Use `useEnumValues('EmAppEnumName')` hook for enum values

### Tailwind CSS
- **Never use `flex-1`** - use `w-1 flex-auto` (horizontal) or `h-1 flex-auto` (vertical)
- Always use theme tokens from `useTheme()` hook - never hardcode colors
- Standard button: `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`
- Standard input: `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`
- Standard label: `w-32 text-xs ${theme.label} mr-2`

### Wijmo Components
- FlexGridCellTemplate: use `cell.item` NOT `cell.dataItem`
- FlexGrid access via ref: `flexGridRef.current.control`
- Row selection in `selectionChanged`: get grid with `const flex = s?.control ?? s`, then `flex.selection?.row`
- CollectionView: initialize with `useState(() => new CollectionView<any>([]))` - never use null
- Always add spacer column: `<FlexGridColumn header="" binding="" width="*" />`

### Font Awesome 6
Always use `fa-solid` prefix:
- `fa-trash-o` → `fa-solid fa-trash`
- `fa-pencil` → `fa-solid fa-pencil`
- `fa-refresh` → `fa-solid fa-rotate`
- `fa-save` → `fa-solid fa-floppy-disk`

## React App AI Help (Claude Code)

**Reference index**: `.claude/react-app/reference/ReactAppReferenceIndex.md` — project overview and index of all React app reference docs.

**Commands** (`.claude/commands/`): `fix-ui` — fix a React component’s UI to match project standards.

**Skills** (`.claude/skills/reactapp/`): See `SKILL.md` there for the full list and how to use. Use these when working on the React app:
- **reactapp-fix-api** — Add/fix API calls, WebAPI service methods, call API from components (query params, POST, headers, errors)
- **reactapp-use-theme** — Theme tokens, no hardcoded colors
- **reactapp-use-wijmo** — FlexGrid, ComboBox, CollectionView, cell.item, refs
- **reactapp-create-component** — Creating new components/pages (theme, Tailwind, forms, Wijmo, conventions)
- **reactapp-fix-ui** — Fixing existing component UI to match standards (theme, Tailwind, forms, Wijmo)
- **reactapp-use-context-menu** — Grid/list context menu (Actions column + floating menu)

## Key Libraries

- **React**: 18.2.0 with Redux Toolkit 2.8.2
- **Wijmo** (`@mescius/wijmo.*`): Enterprise grids, inputs, charts
- **SignalR** (`@microsoft/signalr` 8.0.7): Real-time communication
- **DayPilot**: Gantt charts (loaded from `/public/daypilot-all.min.js`)
- **Tailwind CSS** 3.4.17 with PostCSS
- **.NET**: Entity Framework 6.4, ASP.NET MVC 4, Web API 2

## Reference Components (React app: `PlmApplication/AppReact/src/components/`)

- Form editor: `admin/UserLoginInfoEditor.tsx`
- List/grid: `admin/UserManagement.tsx`
- Multi-panel: `message/MessageManagement.tsx`
- Master-detail form: `formMgt/FormMasterDetail.tsx`
- Form builder: `transaction/ApplicationFormBuilder.tsx`
- Context menu (grid): `admin/MyApplicationEditor/DataModelDesign.tsx`, `dbmgt/RestApiImportManagement.tsx`
