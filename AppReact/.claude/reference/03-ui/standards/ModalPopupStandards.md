# Modal / Popup 规范

## Backdrop 点击不关闭

**约定**：点击弹窗外的背景（backdrop）区域时，**不要**关闭弹窗。用户必须通过标题栏关闭按钮、取消/确定按钮等明确操作关闭。

### 实现方式

- **Backdrop 容器**：使用 `onClick={(e) => e.stopPropagation()}`，不调用 `onClose` 或 `setShowX(false)`。
- **内容区**：保持 `onClick={(e) => e.stopPropagation()}`，防止点击内容时事件冒泡到 backdrop。

```tsx
<div
  className="fixed inset-0 z-50 flex items-center justify-center bg-black/30"
  onClick={(e) => e.stopPropagation()}
>
  <div
    className={/* modal content */}
    onClick={(e) => e.stopPropagation()}
  >
    {/* 标题栏带关闭按钮，用户通过按钮关闭 */}
  </div>
</div>
```

若使用 `handleBackdropClick`，则仅做 `e.stopPropagation()`，不要在其中调用 `onClose()`。

### 参考组件

- `RoleEditorModal.tsx`、`UserLoginInfoEditor.tsx`、`BusinessPartnerEditor.tsx`
- `AddUnitDialog.tsx`、`TableColumnSelectorDialog.tsx`、`TransactionUnitEditor.tsx`
- `Alert.tsx`、`Confirm.tsx`、`FileUploader.tsx`
