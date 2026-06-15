# Application Architecture

## Overview

This is a **React 18 + TypeScript** enterprise application featuring:
- Redux Toolkit for state management
- React Router v6 for navigation
- Wijmo for advanced data grids
- Tailwind CSS for styling
- Multi-tab interface with persistence
- Dynamic form execution engine

---

## 1. High-Level Architecture

```mermaid
graph TB
    subgraph "Browser"
        UI[React UI Layer]
        Redux[Redux Store]
        Router[React Router]
    end

    subgraph "Service Layer"
        API[API Services]
        Helper[Helper Utilities]
    end

    subgraph "Backend"
        Server[Web API Server<br/>/appai/webapi/]
    end

    subgraph "Storage"
        LS[LocalStorage<br/>SessionId, Tabs]
        SS[SessionStorage<br/>Themes, Cache]
    end

    UI --> Redux
    UI --> Router
    UI --> API
    Redux --> LS
    Redux --> SS
    API --> Server
    Helper --> API
```

---

## 2. Application Entry Flow

```mermaid
graph LR
    A[index.tsx] --> B[App.tsx]
    B --> C[Redux Provider]
    C --> D[BrowserRouter]
    D --> E[AlertConfirmProvider]
    E --> F[Global Components]
    E --> G[routes.tsx]

    F --> F1[BusyLoader]
    F --> F2[ErrorMessageButton]
    F --> F3[ErrorMessagePopup]

    G --> G1{Auth Check}
    G1 -->|Not Authenticated| H[Login]
    G1 -->|Authenticated| I[LandingPage]

    I --> I1[Header]
    I --> I2[Sidebar]
    I --> I3[TabHeaders]
    I --> I4[Route Content]
```

---

## 3. Redux Store Structure

```mermaid
graph TB
    Store[Redux Store]

    Store --> Counter[counterSlice<br/>Demo counter]
    Store --> Busy[busyLoaderSlice<br/>Loading state]
    Store --> Theme[themeSlice<br/>Light/Dark themes]
    Store --> User[userSessionSlice<br/>Auth & user data]
    Store --> Tab[tabnavSlice<br/>Multi-tab state]
    Store --> Error[errorMessageSlice<br/>Global messages]
    Store --> Side[sidebarSlice<br/>Sidebar collapse]

    User --> U1[isAuthenticated]
    User --> U2[userContext]
    User --> U3[userMenu]
    User --> U4[enumDictionary]

    Tab --> T1[tabs array]
    Tab --> T2[activeTabKey]
    Tab --> T3[cachedData dict]

    Theme --> TH1[currentTheme]
    Theme --> TH2[availableThemes]

    Error --> E1[messages array]
    Error --> E2[isPopupOpen]
```

---

## 4. Authentication Flow

```mermaid
sequenceDiagram
    participant User
    participant Login
    participant Redux
    participant API
    participant LocalStorage

    User->>Login: Enter credentials
    Login->>Redux: dispatch(login())
    Redux->>API: adminSvc.mgtLogin()
    API-->>Redux: SessionId + UserContext
    Redux->>LocalStorage: Save SessionId
    Redux->>API: retrieveUserTreeMenu()
    API-->>Redux: Menu structure
    Redux->>API: Get theme preferences
    API-->>Redux: User themes
    Redux-->>Login: Success
    Login->>User: Navigate to /home

    Note over LocalStorage: On page refresh
    Login->>LocalStorage: Check SessionId
    LocalStorage-->>Login: SessionId exists
    Login->>Redux: dispatch(restoreSession())
    Redux->>API: getUserContextBySessionId()
    API-->>Redux: Validate & return context
    Redux-->>Login: Session restored
```

---

## 5. Component Architecture

```mermaid
graph TB
    subgraph "Main Layout"
        LP[LandingPage]
        LP --> Header
        LP --> Sidebar
        LP --> TabHeaders
        LP --> Outlet[Route Outlet]
    end

    subgraph "Admin Features"
        Outlet --> Login
        Outlet --> MyApps[MyApplications]
        Outlet --> UserMgmt[UserManagement]
        Outlet --> AppEditor[MyApplicationEditor]
        Outlet --> ThemeMgmt[ThemeManagement]
        Outlet --> DataSource[DataSourceRegister]
    end

    subgraph "Search Module"
        Outlet --> Search[AppSearch]
        Search --> Filter[SearchFilter]
        Search --> View[SearchView]
        View --> Grid[GridViewLayout]
        View --> Calendar[CalendarViewLayout]
        View --> Gantt[GanttViewLayout]
        View --> Pivot[PivotViewLayout]
    end

    subgraph "Form Module"
        Outlet --> Form[FormMasterDetail]
        Form --> EditLayout[MasterDetailEditLayoutForm]
        EditLayout --> Controls[Form Controls]
        Controls --> TB[TextBoxControl]
        Controls --> DDL[DDLControl]
        Controls --> Date[DateControl]
        Controls --> Num[NumericControl]
        Controls --> CB[CheckBoxControl]
        Controls --> Memo[MemoControl]
        EditLayout --> DataGrid[DataGridLayout]
    end

    subgraph "Form Builder"
        Outlet --> Builder[ApplicationFormBuilder]
        Builder --> Designer[FormDesign]
        Builder --> GraphicEditor[TransactionGraphicEditor]
        Builder --> UnitEditor[UnitStructureEditor]
    end

    subgraph "Common Components"
        Outlet --> Common[Shared Components]
        Common --> BusyLoader
        Common --> ErrorPopup[ErrorMessagePopup]
        Common --> ErrorBtn[ErrorMessageButton]
        Common --> Confirm
        Common --> Alert
    end
```

---

## 6. Multi-Tab Navigation System

```mermaid
graph TB
    subgraph "Tab State Management"
        TS[tabnavSlice]
        TS --> Tabs[tabs: Tab array]
        TS --> Active[activeTabKey]
        TS --> Cache[dictDataModelKeyAndCachedData]
    end

    subgraph "Tab Operations"
        Add[addTab<br/>Create or activate]
        Activate[activateTab<br/>Switch tab]
        Close[closeTab<br/>Remove tab]
        Update[updateCurrentTabLabel<br/>Rename tab]
        SetCache[setCurrentTabDataToCache<br/>Store data]
    end

    subgraph "Persistence"
        MW[tabsPersistenceMiddleware]
        LS2[LocalStorage]
        MW -->|Save on change| LS2
        LS2 -->|Restore on load| MW
    end

    User[User Action] --> Add
    Add --> TS
    TS --> MW
    Activate --> TS
    Close --> TS
    Update --> TS
    SetCache --> Cache
```

---

## 7. API Service Layer

```mermaid
graph TB
    subgraph "Services in src/webapi/"
        Admin[adminSvc<br/>Auth, Users, Themes]
        Search[searchSvc<br/>Master Data Search]
        Transaction[apptransactionsvc<br/>Form CRUD]
        Layout[dynamiclayoutsvc<br/>Form Layouts]
        Schema[schemaMetaDataSvc<br/>DB Schema]
        Navigation[navigationSvc<br/>Menu/Routes]
        Workflow[projectWorkFlowSvc<br/>Workflows]
        Integration[integrationsvc<br/>3rd Party]
        File[appfilesvc<br/>File Ops]
        Message[appmessagesvc<br/>Messages]
        Chat[chatsvc<br/>Real-time Chat]
        Language[languageSvc<br/>i18n]
        Dashboard[dashboardsvc<br/>Dashboards]
    end

    subgraph "API Configuration"
        Helper[apiServiceHelper<br/>Headers + SessionId]
        Endpoints[endpoints.ts<br/>BASE_URL=/appai]
    end

    subgraph "Backend"
        Server[Web API Server]
    end

    Admin --> Helper
    Search --> Helper
    Transaction --> Helper
    Layout --> Helper
    Helper --> Endpoints
    Endpoints --> Server
```

---

## 8. Theming System

```mermaid
graph TB
    subgraph "Theme State"
        TS[themeSlice]
        TS --> Current[currentTheme: Theme]
        TS --> Available[availableThemes]
        TS --> ID[currentThemeId]
    end

    subgraph "Built-in Themes"
        Light[light.json]
        Dark[dark.json]
    end

    subgraph "User Themes"
        Server[Server Theme API]
        Session[SessionStorage Cache]
        Server --> Session
    end

    subgraph "Theme Application"
        Hook[useTheme Hook]
        Helper[themeHelper]
        Wijmo[Wijmo CSS Override]
        Tailwind[Tailwind Classes]
    end

    User[User Selects Theme] --> TS
    TS --> Hook
    Hook --> Helper
    Helper --> Wijmo
    Helper --> Tailwind

    Light --> TS
    Dark --> TS
    Session --> TS
```

---

## 9. Data Flow: Opening a Search

```mermaid
sequenceDiagram
    participant User
    participant Sidebar
    participant Redux
    participant Router
    participant AppSearch
    participant SearchSvc
    participant Server

    User->>Sidebar: Click menu item
    Sidebar->>Redux: addTab({path, label})
    Redux->>Router: navigate('/MasterDataManagement/:param')
    Router->>AppSearch: Mount component
    AppSearch->>Redux: Check cached data
    alt No cached data
        AppSearch->>SearchSvc: retrieveAllAppDataSetEntityDto()
        SearchSvc->>Server: GET /webapi/search/...
        Server-->>SearchSvc: Dataset config
        SearchSvc-->>AppSearch: Return data
        AppSearch->>Redux: setCurrentTabDataToCache()
    else Cached data exists
        Redux-->>AppSearch: Return cached data
    end
    AppSearch->>User: Render SearchFilter + SearchView
    User->>AppSearch: Apply filters
    AppSearch->>SearchSvc: Search query
    SearchSvc-->>AppSearch: Results
    AppSearch->>User: Display in GridViewLayout (Wijmo)
```

---

## 10. Data Flow: Form Execution

```mermaid
sequenceDiagram
    participant User
    participant Search
    participant Redux
    participant Router
    participant Form
    participant LayoutSvc
    participant TransactionSvc

    User->>Search: Click record to edit
    Search->>Redux: addTab({path: '/FormMasterDetail', ...})
    Redux->>Router: Navigate with encoded JSON params
    Router->>Form: Mount FormMasterDetail
    Form->>LayoutSvc: getTransactionForm(transactionId)
    LayoutSvc-->>Form: Form structure + fields
    Form->>TransactionSvc: getData(recordId)
    TransactionSvc-->>Form: Record data
    Form->>User: Render form controls

    User->>Form: Edit fields
    User->>Form: Click Save
    Form->>TransactionSvc: saveData(formData)
    TransactionSvc-->>Form: Success/Error
    alt Success
        Form->>Redux: addErrorMessage({type: Message})
        Form->>User: Show success message
    else Error
        Form->>Redux: addErrorMessage({type: Error})
        Form->>User: Show error popup
    end
```

---

## 11. Error Handling System

```mermaid
graph TB
    subgraph "Error Sources"
        API[API Errors]
        Validation[Form Validation]
        System[System Errors]
    end

    subgraph "Error Message Redux"
        Slice[errorMessageSlice]
        Slice --> Queue[messages: array]
        Slice --> Popup[isPopupOpen: bool]
    end

    subgraph "Error Types"
        E1[Type 1: Error red]
        E2[Type 2: Warning yellow]
        E3[Type 3: Info blue]
    end

    subgraph "UI Components"
        Button[ErrorMessageButton<br/>Badge with count]
        Pop[ErrorMessagePopup<br/>List of messages]
    end

    API --> Slice
    Validation --> Slice
    System --> Slice

    Slice --> E1
    Slice --> E2
    Slice --> E3

    E1 --> Button
    E2 --> Button
    E3 --> Button

    Button -->|Click| Pop
    Pop -->|Dismiss| Slice
```

---

## 12. Key Technology Stack

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **UI Framework** | React | 18.3.1 | Component-based UI |
| **Language** | TypeScript | 4.9.5 | Type safety |
| **State Management** | Redux Toolkit | 2.8.2 | Global state |
| **Routing** | React Router | 6.28.0 | Navigation |
| **Styling** | Tailwind CSS | 3.4.17 | Utility-first CSS |
| **Data Grids** | Wijmo | 5.20252.42 | Enterprise grids |
| **Real-time** | SignalR | 8.0.7 | WebSocket communication |
| **Build Tool** | React Scripts | 5.0.1 | CRA build system |
| **Testing** | Jest + React Testing | Built-in | Unit testing |

---

## 13. File Structure

```
app-react/
├── public/                      # Static assets
├── src/
│   ├── index.tsx                # Application entry point
│   ├── App.tsx                  # Root component with providers
│   ├── routes.tsx               # Route definitions + auth guard
│   │
│   ├── components/              # React components
│   │   ├── admin/               # Admin features (Login, Users, etc.)
│   │   ├── mainLayout/          # Layout components (Header, Sidebar, Tabs)
│   │   ├── formMgt/             # Dynamic form execution
│   │   ├── search/              # Master data search
│   │   ├── transaction/         # Form builder
│   │   └── common/              # Shared UI components
│   │
│   ├── redux/
│   │   ├── store.ts             # Redux store configuration
│   │   ├── rootReducer.ts       # Combined reducers
│   │   ├── hooks/               # Custom Redux hooks
│   │   └── features/            # Redux slices by feature
│   │       ├── admin/           # userSessionSlice
│   │       └── ui/              # UI-related slices
│   │           ├── theme/       # themeSlice
│   │           ├── navigation/  # tabnavSlice, sidebarSlice
│   │           └── feedback/    # errorMessageSlice, busyLoaderSlice
│   │
│   ├── webapi/                  # API service classes
│   │   ├── endpoints.ts         # API base URL config
│   │   ├── adminsvc.ts          # Authentication & admin APIs
│   │   ├── searchSvc.ts         # Search APIs
│   │   ├── apptransactionsvc.ts # Form transaction APIs
│   │   └── ...                  # Other service modules
│   │
│   ├── helper/                  # Utility functions
│   │   ├── apiServiceHelper.ts  # API headers with SessionId
│   │   ├── themeHelper.ts       # Theme utilities
│   │   ├── navigationHelper.ts  # URL building
│   │   └── ...
│   │
│   ├── types/                   # TypeScript type definitions
│   ├── styles/                  # CSS and theme files
│   │   └── theme/               # Theme JSON files (light.json, dark.json)
│   └── setupTests.ts            # Test configuration
│
├── package.json                 # Dependencies
├── tsconfig.json                # TypeScript config
├── tailwind.config.js           # Tailwind configuration
└── CLAUDE.md                    # Development instructions
```

---

## 14. Key Patterns & Best Practices

### State Management Pattern
- **Local state** for UI-only concerns (modals, form inputs)
- **Redux** for:
  - Authentication & user session
  - Global UI state (theme, sidebar, busy loader)
  - Multi-tab navigation with caching
  - Error message queue

### Data Fetching Pattern
```typescript
// In component:
useEffect(() => {
  const fetchData = async () => {
    dispatch(setIsBusy(true));
    try {
      const data = await myService.getData();
      setLocalState(data);
    } catch (error) {
      dispatch(addErrorMessage({message: error.message, type: MessageType.Error}));
    } finally {
      dispatch(setIsNotBusy(false));
    }
  };
  fetchData();
}, [dependencies]);
```

### Custom Hooks Pattern
- `useTabNavigation()` - Tab operations + routing
- `useTheme()` - Theme access + CSS class helpers
- `useErrorMessage()` - Error/warning/info messages
- `useAppDispatch()` / `useAppSelector()` - Typed Redux hooks

### Persistence Pattern
- **localStorage**: SessionId, tab state
- **sessionStorage**: Theme cache, user preferences
- **Redux middleware**: Automatic persistence on state changes

### Route Protection Pattern
```typescript
// In routes.tsx:
{!userContext.IsLoginFailed ? (
  <Route path="/protected" element={<ProtectedComponent />} />
) : (
  <Route path="*" element={<Navigate to="/login" />} />
)}
```

---

## 15. Performance Optimizations

1. **Lazy Loading**: Routes loaded on-demand with `React.lazy()`
2. **Data Caching**: Per-tab data cache in Redux prevents refetching
3. **Persistence**: Tabs and session persisted to avoid reload costs
4. **Middleware**: Custom middleware for selective persistence
5. **Memoization**: Components can use `useMemo` / `useCallback` for expensive operations

---

## 16. Security Considerations

1. **SessionId Authentication**: All API calls include SessionId in headers
2. **Route Guards**: Unauthenticated users redirected to login
3. **Session Validation**: SessionId validated on page refresh
4. **LocalStorage**: SessionId stored (consider HttpOnly cookies for production)
5. **Error Handling**: Sensitive errors not exposed to UI

---

## 17. Future Enhancement Opportunities

1. **Type Safety**: Replace `any` types with proper interfaces
2. **Error Boundaries**: Add React error boundaries for graceful failures
3. **Code Splitting**: Further split large components
4. **Testing**: Expand test coverage beyond basic setup
5. **SignalR**: Implement real-time features using existing infrastructure
6. **Performance**: Add React.memo to frequently re-rendering components
7. **Security**: Move from localStorage to HttpOnly cookies
8. **i18n**: Leverage languageSvc for full internationalization

---

This architecture provides a solid foundation for an enterprise React application with advanced features like multi-tab navigation, dynamic forms, and a sophisticated theming system integrated with professional data grid components.
