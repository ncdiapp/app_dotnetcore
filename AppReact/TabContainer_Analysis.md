# Tab Container 实现分析文档

## 1. Angular 版本实现分析

### 1.1 核心功能

#### 1.1.1 创建 Tab Container
- **位置**: `formDesignFlexCtrl.js` 第 1424-1448 行
- **逻辑**:
  - 当将 TabContainer 类型拖拽到 placeholder 时，会创建一个新的 Tab Container
  - 设置 `DisplayName = "Tab Container"`
  - 初始化 `AppFormLayoutItem_List = []`
  - **自动创建两个默认 Tab**: `appendNewLayoutTab(layoutItem, 'Tab1')` 和 `appendNewLayoutTab(layoutItem, 'Tab2')`
  - 设置第一个 Tab 为 active: `layoutItem.activeTab = newLayoutTab`

#### 1.1.2 添加新 Tab (`appendNewLayoutTab`)
- **位置**: `formDesignFlexCtrl.js` 第 583-615 行
- **功能**:
  - 接收参数: `tabContainer` 和 `defautTabName` (默认 "New Tab")
  - 创建新的 Tab 项，类型为 `Section` (`WidgetDisplayType: EmAppFormLayoutItemType.Section`)
  - 设置 `IsTab: true`
  - 设置 `DefaultNbColumns: 2`
  - 自动为新 Tab 添加一个 LayoutRow (`appendNewLayoutRow(newLayoutTab)`)
  - 返回新创建的 Tab

#### 1.1.3 初始化 Tab Container
- **位置**: `formDesignFlexCtrl.js` 第 844-848 行
- **逻辑**:
  - 在 `initLayoutItemAndChildItems` 函数中
  - 如果 Tab Container 有子项，设置第一个为 active: `layoutItem.activeTab = layoutItem.AppFormLayoutItem_List[0]`

### 1.2 UI 结构

#### 1.2.1 HTML 结构 (来自 `_OneLayoutItem.cshtml`)
```html
<div class="FormTabContainer" style="">
    <div class="FormTabHeader" style="">
        @foreach (var aLayoutTab in layoutItem.AppFormLayoutItem_List.OrderBy(o => o.FlowOrGridLayoutSortOrder))
        {
            <div class="FormTabButton" style=""
                 ng-click="controllerModel.dictTabContainerIdAndActiveTabId['@layoutItem.Id'] = @(aLayoutTab.Id);"
                 ng-class="{ActiveTab: isTabActive('@aLayoutTab.Id', '@layoutItem.Id', '@defaultTabId')}">
                @aLayoutTab.DomAttribute.DisplayName
            </div>
        }
        <div style="width:100%;height:3px;background-color:white;"></div>
    </div>
    <div style="width:100%;padding:0px 1px 1px 1px;">
        @foreach (AppFormLayoutItemExDto layoutTabSectionExDto in layoutItem.AppFormLayoutItem_List.OrderBy(o => o.FlowOrGridLayoutSortOrder))
        {
            <div style="width:100%;" ng-if="isTabActive('@layoutTabSectionExDto.Id', '@layoutItem.Id', '@defaultTabId')">
                @Html.Partial("_OneLayoutItem.cshtml", layoutTabSectionExDto)
            </div>
        }
    </div>
</div>
```

#### 1.2.2 CSS 样式 (来自 `appStyle.css`)
```css
.FormTabContainer {
    width: 100%;
    background-color: #40404022;
    border-radius: 9px 9px 0px 0px;
}

.FormTabHeader {
    width: 100%;
    display: flex;
    flex-wrap: wrap;
    padding: 0px 1px 0px 1px;
    background-color: #404040ee;
    border-radius: 9px 9px 0px 0px;
}

.FormTabButton {
    flex: 1 1 auto;
    width: 150px;
    max-width: 200px;
    height: 23px;
    border-radius: 8px 8px 0px 0px;
    background-color: rgb(231,234,237);
    color: #333;
    font-weight: 500;
    cursor: pointer;
    padding: 3px 6px 3px 6px;
    position: relative;
    text-overflow: ellipsis;
    overflow: hidden;
    border-left: solid 1px rgba(0,0,0,0.03);
    margin-top: 1px;
}

.FormTabButton:hover {
    background-color: rgb(241,244,247);
}

.FormTabButton.ActiveTab {
    background-color: #fff;
    color: #222;
}
```

### 1.3 Tab 切换机制

#### 1.3.1 状态管理
- 使用 `controllerModel.dictTabContainerIdAndActiveTabId[containerId] = tabId` 来存储每个 Tab Container 的当前激活 Tab
- 使用 `isTabActive(tabId, containerId, defaultTabId)` 函数来判断 Tab 是否激活

#### 1.3.2 切换逻辑
- 点击 Tab 按钮时，更新 `dictTabContainerIdAndActiveTabId`
- 使用 `ng-if` 条件渲染来显示/隐藏 Tab 内容

### 1.4 属性设置

#### 1.4.1 Field Setting Toolbox 中的属性
- **位置**: `_FieldSettingToolbox.cshtml` 和 `formDesignFlexCtrl.js` 第 161-163 行
- **禁用属性**:
  - `EnableBackgroundColor = false` (不允许设置背景色)
  - `EnableTextColor = false` (不允许设置文字颜色)
  - `EnableVisibleExpression = false` (不允许设置可见表达式)
- **允许属性**:
  - Container Width (ColSpan)
  - Child Row Total Cells (如果适用)

### 1.5 Tab 项结构

每个 Tab 项 (`AppFormLayoutItem_List` 中的项):
- **类型**: `Section` (`WidgetDisplayType: EmAppFormLayoutItemType.Section`)
- **属性**:
  - `IsTab: true` (标识这是一个 Tab)
  - `DisplayName`: Tab 的显示名称
  - `DefaultNbColumns: 2` (默认列数)
  - `AppFormLayoutItem_List`: 包含该 Tab 内的布局项（通常是 LayoutRow）

## 2. React 版本当前实现

### 2.1 已实现功能
- ✅ Tab Container 的基本渲染 (`OneLayoutItemDesign.tsx` 第 966-1060 行)
- ✅ Tab 切换功能 (`isTabActive`, `setActiveTab`)
- ✅ Tab 按钮的点击处理
- ✅ Tab 内容的条件渲染

### 2.2 需要改进/实现的功能

#### 2.2.1 创建 Tab Container 时的默认行为
- **当前**: 可能没有自动创建默认 Tab
- **需要**: 创建 Tab Container 时，自动创建两个默认 Tab ("Tab1" 和 "Tab2")
- **位置**: `FormDesign.tsx` 中的 `convertBlankLayoutItemToControl` 函数

#### 2.2.2 添加/删除 Tab 功能
- **当前**: 可能没有 UI 来添加/删除 Tab
- **需要**: 
  - 实现 `appendNewLayoutTab` 函数
  - 在 Tab Container 的上下文菜单或 UI 中添加 "Add Tab" 和 "Remove Tab" 选项
  - 删除 Tab 时需要处理 activeTab 的切换

#### 2.2.3 UI 样式对齐
- **当前**: 使用 Tailwind CSS，样式可能与 Angular 版本不完全一致
- **需要**: 
  - 检查并调整 CSS 样式，使其与 Angular 版本一致
  - 特别是 `.FormTabContainer`, `.FormTabHeader`, `.FormTabButton` 的样式

#### 2.2.4 Tab 初始化
- **当前**: 可能没有在初始化时设置 activeTab
- **需要**: 在 `initLayoutItemAndChildItems` 中，如果 Tab Container 有子项，设置第一个为 active

#### 2.2.5 Field Setting Toolbox
- **当前**: 可能没有正确处理 Tab Container 的属性设置
- **需要**: 
  - 禁用 Background Color, Text Color, Visible Expression
  - 确保 Container Width 和 Child Row Total Cells 正确显示

## 3. 实施计划

### 3.1 优先级 1: 核心功能
1. 实现创建 Tab Container 时自动创建两个默认 Tab
2. 实现 `appendNewLayoutTab` 函数
3. 实现 Tab 初始化逻辑

### 3.2 优先级 2: UI 改进
1. 调整 Tab Container 的 CSS 样式，使其与 Angular 版本一致
2. 添加 Tab 的添加/删除 UI（上下文菜单或按钮）

### 3.3 优先级 3: 完善功能
1. 完善 Field Setting Toolbox 中对 Tab Container 的支持
2. 处理 Tab 删除时的 activeTab 切换逻辑
3. 添加 Tab 重命名功能

## 4. 关键代码位置

### Angular 版本
- **控制器**: `C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\Form\formDesignFlexCtrl.js`
  - `appendNewLayoutTab`: 第 583-615 行
  - `convertBlankLayoutItemToControl`: 第 1357-1448 行
  - `initLayoutItemAndChildItems`: 第 844-848 行
- **视图**: `C:\DevApp\App\PlmApplication\Server\Views\FormMgt\FormMasterDetail\MasterDetailFlexLayoutForm\_OneLayoutItem.cshtml` 第 100-129 行
- **样式**: `C:\DevApp\App\PlmApplication\Styles1x\customized\appStyle.css` 第 1919-1960 行

### React 版本
- **渲染组件**: `c:\DevApp\app-react\src\components\formMgt\FormDesign\OneLayoutItemDesign.tsx` 第 966-1060 行
- **主组件**: `c:\DevApp\app-react\src\components\formMgt\FormDesign.tsx`
- **工具箱**: `c:\DevApp\app-react\src\components\formMgt\FormDesign\AddFieldToolbox.tsx` 第 100-114 行
