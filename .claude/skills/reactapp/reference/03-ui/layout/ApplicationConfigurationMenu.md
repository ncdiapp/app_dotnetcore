# Application Configuration Dynamic Sub-Menus Analysis

## Overview

The "Application Configuration" main menu in the AngularJS app contains dynamic sub-menus that are generated from the user's application list. Each application gets a sub-menu item with the format "Config: {ApplicationName}".

## Data Source

### API Endpoint
- **Endpoint**: `/webapi/Administration/RetrieveUserTreeMenu`
- **Method**: GET
- **Controller**: `AdministrationControllerSecurity.cs` (line 77-90)
- **Business Logic**: `AppSecurityManagementBL.RetrieveUserTreeMenu()`
- **Returns**: `ObservableSet<AppListMenuExDto>` - A hierarchical menu structure

### React Implementation
- **Service**: `src/webapi/adminsvc.ts` - `retrieveUserTreeMenu()`
- **Redux State**: `userSession.userMenu` (stored in `userSessionSlice.ts`)
- **Loaded On**: User login (in `Login.tsx`)

## AngularJS Implementation

### Menu Structure Building

**Location**: `example/angularjs/Scripts1x/mgtNavigationCtrl.txt` (lines 434-478)

The `buildThreeLevelMenuTree()` function processes the menu data from `RetrieveUserTreeMenu()`:

```javascript
var buildThreeLevelMenuTree = function (menuList) {
    $scope.threeLevelMenuList = [];
    let dictMenuIdAndDto = $scope.dictMenuIdAndDto = {};
    if (menuList) {
        angular.forEach(menuList, function (menuL1) {
            var newMenuL1 = $.extend({}, menuL1);
            newMenuL1.AppListMenu_List = [];
            newMenuL1.ImageUrl = newMenuL1.ImageUrl || "Upload to the Cloud-64.png";
            $scope.threeLevelMenuList.push(newMenuL1);
            // ... processes nested menu levels
        });
    }
}
```

**Key Points**:
- `threeLevelMenuList` contains the root-level menu items (applications)
- Each item represents an application package
- The list is built from the hierarchical menu structure returned by the API

### Menu Rendering

**Location**: `example/angularjs/Server/Views/Home/Common/navigation.cshtml` (lines 181-190)

```html
<ul class="nav nav-second-level collapse" ng-class="{in: builtInMenu.ApplicationConfigurationGroup.isActive}">
    <li ng-repeat="item in threeLevelMenuList" ui-sref-active="active">
        <a ng-click="openMyApplicationEditor(item.Id, $event);"
           ng-class="{activeMenuItem: 'ApplicationConfig_' + item.Id == currentActiveMenuItemId}"
           title="Config: {{item.Name}}">
            <i class="fa fa-edit navMenuIcon"></i>
            <span>Config: {{item.Name}}</span>
        </a>
    </li>
    <!-- Static menu items follow -->
</ul>
```

**Key Points**:
- Uses `ng-repeat="item in threeLevelMenuList"` to generate dynamic sub-menus
- Each item displays as "Config: {item.Name}"
- Click handler: `openMyApplicationEditor(item.Id, $event)`

### Click Handler Function

**Location**: `example/angularjs/Scripts1x/Helper/saasNavigationHelper.js` (lines 337-371)

```javascript
$scope.openMyApplicationEditor = function (packageRootMenuId, clickEvent) {
    clickEvent = clickEvent || event;
    if (clickEvent && clickEvent.stopPropagation) {
        event.stopPropagation();
    }

    if (packageRootMenuId && $scope.menuData) {
        let menuId = packageRootMenuId;
        let packageRootMenu = appHelper.findFirstOrDefaultFromArray($scope.menuData, menuId, "Id");
        if (packageRootMenu) {
            $scope.currentAppPackage = {};
            let displayName = 'Application Builder: ' + packageRootMenu.Name;
            
            // Route based on GlobalGuid
            if (packageRootMenu.GlobalGuid && packageRootMenu.GlobalGuid.toLowerCase() == ESiteConfigurationRootMenuGuid.toLowerCase()) {
                $scope.openPageRouteState({ heading: displayName, route: 'main.ESiteManagement', active: true, Id: menuId }, false, false, 'Tab');
            }
            else if (packageRootMenu.GlobalGuid && packageRootMenu.GlobalGuid.toLowerCase() == AppWebSiteConfigurationRootMenuGuid.toLowerCase()) {
                $scope.openPageRouteState({ heading: displayName, route: 'main.AppWebsiteManagement', active: true, Id: menuId }, false, false, 'Tab');
            }
            else {
                // Default route for regular applications
                $scope.openPageRouteState({ heading: displayName, route: 'main.MyApplicationEditor', active: true, Id: menuId }, false, false, 'Tab');
            }

            $scope.currentActiveMenuItemId = 'ApplicationConfig_' + menuId;
            $scope.builtInMenu.ApplicationConfigurationGroup.isActive = true;
            $scope.collapseOtherMenuGroups([$scope.builtInMenu.ApplicationConfigurationGroup]);
        }
    }
}
```

**Key Points**:
- Finds the menu item from `$scope.menuData` using the `Id`
- Routes based on `GlobalGuid`:
  - **ESiteConfigurationRootMenuGuid** → `main.ESiteManagement` route
  - **AppWebSiteConfigurationRootMenuGuid** → `main.AppWebsiteManagement` route
  - **Default** → `main.MyApplicationEditor` route
- Opens the route in a new tab with the menu `Id` as a parameter
- Sets the active menu item ID to `'ApplicationConfig_' + menuId`

### Route Definition

**Location**: `example/angularjs/Scripts1x/mgtRoute.js` (line 455)

```javascript
.state('main.MyApplicationEditor', { 
    url: '/MyApplicationEditor?Id&param1&param2', 
    controller: 'myApplicationEditorCtrl', 
    templateUrl: function (stateParams) { 
        return domainAndApplicationpath + '/Administration/MyApplicationEditor?CurrentUserSessionId=' + angular.UserContext.SessionId + '&browserInfo=' + angular.getBrowserInfo() 
    } 
})
```

**Controller**: `myApplicationEditorCtrl` (in `myApplicationManagementCtrl.js`)

**View**: `Server/Views/Administration/MyApplicationEditor.cshtml`

This is a comprehensive application configuration editor with multiple tabs:
- Application Setting
- Transaction
- Form
- Transaction Group
- App Website
- Workflow
- Search
- Data Manipulation
- Dashboard
- Report

## React Implementation (Current)

### Current Implementation

**Location**: `src/components/mainLayout/Sidebar.tsx` (lines 77-109)

```typescript
{
  Id:'application-configuration',
  Name: 'Application Configuration',
  AppListMenu_List: [     
    // Add user menu items as single level items at the beginning
    ...(userMenu?.map((item: any) =>  ({
        Id:`config-${item.Id}`,
        Name: `Config: ${item.Name}`,
        RouteCode: item.RouteCode || '/'
    }) || []),
    // Static menu items follow...
  ]
}
```

**Key Points**:
- Uses `userMenu` from Redux state (`state.userSession.userMenu`)
- Maps each item to create "Config: {Name}" sub-menu items
- Currently uses `RouteCode` from the menu item (may not be correct)
- The `userMenu` is loaded on login via `adminSvc.retrieveUserTreeMenu()`

### Data Flow in React

1. **Login** (`src/components/admin/Login.tsx`):
   - Calls `adminSvc.retrieveUserTreeMenu()`
   - Dispatches `setUserMenu(userMenu)` to Redux

2. **Sidebar** (`src/components/mainLayout/Sidebar.tsx`):
   - Reads `userMenu` from Redux: `useSelector((state: RootState) => state.userSession.userMenu)`
   - Maps items to create dynamic sub-menus

3. **Menu Click Handler**:
   - Currently uses `RouteCode` from menu item
   - Should be updated to match AngularJS behavior (route based on GlobalGuid or default to application editor)

## Business Logic

### AppSecurityManagementBL.RetrieveUserTreeMenu()

**Location**: `{SolutionRoot}\APP.BL\AppSecurityManagementBL.cs`

This business logic class:
1. Retrieves the user's accessible menu hierarchy
2. Filters menus based on user permissions
3. Returns a hierarchical structure of `AppListMenuExDto` objects
4. Each root-level item represents an application package

### AppListMenuExDto Structure

**Location**: `{SolutionRoot}\APP.Components.Dto\EntityExdto\AppListMenuExDto.cs`

Key properties:
- `Id`: Menu item ID
- `Name`: Menu item name (application name)
- `GlobalGuid`: GUID that determines routing behavior
- `AppListMenu_List`: Child menu items (hierarchical)
- `RouteCode`: Optional route code
- `Sort`: Sort order
- `ImageUrl`: Icon/image URL

## Differences: AngularJS vs React

### AngularJS
- Uses `threeLevelMenuList` which contains only root-level applications
- Routes dynamically based on `GlobalGuid`:
  - ESite → `main.ESiteManagement`
  - AppWebsite → `main.AppWebsiteManagement`
  - Default → `main.MyApplicationEditor`
- Opens routes in tabs with `openPageRouteState()`

### React (Current)
- Uses entire `userMenu` hierarchy
- Maps all items to sub-menus (may include nested items incorrectly)
- Uses `RouteCode` directly (may not match AngularJS routing logic)
- Needs to implement routing based on `GlobalGuid` like AngularJS

## Recommendations for React Implementation

1. **Filter Root-Level Items Only**:
   - Extract only root-level menu items (applications) from `userMenu`
   - Similar to how `threeLevelMenuList` contains only root items
   - Currently, the React implementation maps all items from `userMenu`, which may include nested items incorrectly

2. **Implement Routing Logic**:
   - Check `GlobalGuid` property to determine route
   - Map to appropriate React routes:
     - ESite → `/esite-management` (route not yet created)
     - AppWebsite → `/app-website-management` (route not yet created)
     - Default → `/my-application-editor` (route not yet created)
   - **Note**: These routes do not exist in `src/routes.tsx` yet and need to be created

3. **Menu Click Handler**:
   - Create handler similar to `openMyApplicationEditor()`
   - Pass menu `Id` as route parameter
   - Use tab navigation system (`useTabNavigation` hook) if available
   - Handler should:
     - Find the menu item from `userMenu` using the `Id`
     - Determine route based on `GlobalGuid`
     - Navigate to the appropriate route with `Id` parameter

4. **Create Application Editor Component**:
   - **Component does not exist yet** - needs to be created
   - Should match functionality of AngularJS `MyApplicationEditor`:
     - Multiple tabs/sections: Application Setting, Transaction, Form, Transaction Group, App Website, Workflow, Search, Data Manipulation, Dashboard, Report
     - Loads application configuration based on `Id` parameter
     - Similar to `src/components/admin/ApplicationSetting.tsx` but more comprehensive

5. **Route Parameters**:
   - Pass `Id` parameter to the application editor component via route
   - Component should load application configuration based on `Id` from API
   - Use `useParams()` from `react-router-dom` to read the `Id`

## Related Files

### AngularJS
- Navigation Controller: `example/angularjs/Scripts1x/mgtNavigationCtrl.txt`
- Navigation Helper: `example/angularjs/Scripts1x/Helper/saasNavigationHelper.js`
- Navigation View: `example/angularjs/Server/Views/Home/Common/navigation.cshtml`
- Route Definition: `example/angularjs/Scripts1x/mgtRoute.js`
- Application Editor Controller: `example/angularjs/Scripts1x/mgtCtrl/Administration/myApplicationManagementCtrl.js`
- WebAPI: `example/angularjs/Server/WebApi/AdministrationControllerSecurity.cs`

### React
- Sidebar Component: `src/components/mainLayout/Sidebar.tsx`
- Admin Service: `src/webapi/adminsvc.ts`
- User Session Slice: `src/redux/features/admin/userSessionSlice.ts`
- Login Component: `src/components/admin/Login.tsx`

### Server-Side (Reference)
- Business Logic: `{SolutionRoot}\APP.BL\AppSecurityManagementBL.cs`
- DTO: `{SolutionRoot}\APP.Components.Dto\EntityExdto\AppListMenuExDto.cs`
- WebAPI Controller: `{SolutionRoot}\PlmApplication\Server\WebApi\AdministrationControllerSecurity.cs`
