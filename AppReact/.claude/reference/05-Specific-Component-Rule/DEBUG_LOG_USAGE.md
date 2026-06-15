# DEBUG 日志使用说明

## ⚠️ 重要规则

**必须使用 `appHelper.debugLog()` 替代 `console.log()` 进行调试日志记录。**

- ✅ **正确**: `appHelper.debugLog('message', data)`
- ❌ **错误**: `console.log('message', data)`

所有调试日志必须通过 `appHelper.debugLog()` 方法统一管理，以便通过全局开关控制是否输出。

## 功能说明

所有 DEBUG 日志现在通过 `appHelper.debugLog(...)` 方法统一管理，可以通过全局开关控制是否输出。

## 使用方法

### 1. 在代码中使用 DEBUG 日志

```typescript
import appHelper from '../../helper/appHelper';

// 使用方式与 console.log 相同
appHelper.debugLog('消息', { 数据对象 });
appHelper.debugLog('========== 某个操作 START ==========');
appHelper.debugLog('变量值:', someVariable);
```

### 2. 控制 DEBUG 日志开关

#### 方法一：在浏览器控制台（推荐，无需重启）

打开浏览器开发者工具（F12），在控制台输入：

```javascript
// 开启 DEBUG 日志
window.__DEBUG_ENABLED__ = true;

// 关闭 DEBUG 日志
window.__DEBUG_ENABLED__ = false;
```

#### 方法二：修改代码（需要重新编译）

编辑 `src/helper/appHelper.ts` 文件，修改 `DEBUG_ENABLED` 常量：

```typescript
// 开启 DEBUG 日志
let DEBUG_ENABLED = true;

// 关闭 DEBUG 日志
let DEBUG_ENABLED = false;
```

#### 方法三：使用 API 方法（在代码中调用）

```typescript
import appHelper from '../../helper/appHelper';

// 开启 DEBUG 日志
appHelper.setDebugEnabled(true);

// 关闭 DEBUG 日志
appHelper.setDebugEnabled(false);

// 检查是否启用
const isEnabled = appHelper.isDebugEnabled();
```

## 当前 DEBUG 日志位置

所有 DEBUG 日志都使用 `[DEBUG]` 前缀，主要分布在：

1. **FormDesign.tsx**:
   - `loadFormData()` - 加载表单数据
   - `useEffect` (formData sync) - 同步 formData
   - `convertPlaceholderToItem()` - 转换占位符为实际项
   - `convertBlankLayoutItemToControl()` - 转换空白布局项
   - `onDropToNewItemButton()` - 拖拽到占位符按钮
   - `handleLayoutItemHover()` - 悬停事件

2. **FormLayoutDesignArea.tsx**:
   - `layoutRows` useMemo - 布局行处理

## 注意事项

- DEBUG 日志默认**关闭**，需要手动开启
- 开启后会在控制台输出大量日志，可能影响性能
- 建议只在调试时开启，生产环境保持关闭
- 所有 DEBUG 日志都会自动添加 `[DEBUG]` 前缀，方便在控制台过滤
