# Wijmo FlexGrid 空引用错误修复（Reference Rule）

与弹窗、Context Menu、re-render 配合时，Wijmo FlexGrid 可能因延迟执行的逻辑（queueMicrotask / refresh）在网格已 dispose 或处于无效状态后仍运行，导致以下错误。本规则约定通过**应用内 patch** 统一防护，不依赖业务代码改法。

---

## 1. 现象

- **editRange**：`Cannot read properties of null (reading 'editRange')`  
  - 堆栈：`DirectiveCellFactoryBase._autoSizeIfRequired` → 访问 `this.grid.editRange`（或 FlexGrid 的 editRange getter 内部为 null）。
- **finishEditing**：`Cannot read properties of null (reading 'finishEditing')`  
  - 堆栈：`FlexGrid.refresh` → `FlexGrid.finishEditing`，内部某引用为 null。

二者常在与「行 Context Menu → 打开编辑弹窗」或「列表 re-render / 弹窗打开关闭」相关的时机随机出现。

---

## 2. 修复方式（必须保留）

在应用入口前执行 **patch**，对两处做 try/catch 防护，避免抛错：

| 错误 | 修补位置 | 做法 |
|------|----------|------|
| editRange | `@mescius/wijmo.interop.grid` → `DirectiveCellFactoryBase.prototype._autoSizeIfRequired` | 包装原方法：若 `!this.grid` 则 return；否则在 try/catch 中调用原方法，catch 时静默 return。 |
| finishEditing | `@mescius/wijmo.grid` → `FlexGrid.prototype.finishEditing` | 包装原方法：在 try/catch 中调用原方法，catch 时 return false。 |

---

## 3. 实现位置（项目内）

- **Patch 实现**：`src/wijmoPatches.ts`  
  - 对 `DirectiveCellFactoryBase._autoSizeIfRequired` 做 try/catch + `this.grid` 判断。  
  - 对 `FlexGrid.prototype.finishEditing` 做 try/catch，异常时返回 false。
- **入口引入**：`src/index.tsx` 顶部 `import './wijmoPatches';`（须在 App 及任何使用 FlexGrid 的代码之前执行）。

新增或修改 FlexGrid 相关功能时，**不要删除或绕过**上述 patch；若调整 patch 逻辑，需保持对 editRange 与 finishEditing 两处的防护。

---

## 4. 小结

| 项目 | 说明 |
|------|------|
| 错误 | editRange（_autoSizeIfRequired）、finishEditing（refresh 内） |
| 修复 | 应用内 patch，try/catch 防护，静默降级 |
| 文件 | `src/wijmoPatches.ts`，`src/index.tsx` 引入 |

以上为 Reference Rule，供 `.cursor/skills/` 与 `.claude/react-app/reference/` 引用。
