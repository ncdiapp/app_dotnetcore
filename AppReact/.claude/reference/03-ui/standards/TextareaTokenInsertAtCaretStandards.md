# Textarea Token 插入光标位置（Click / Drag & Drop）

## 适用场景

在 Message Template / 代码编辑类页面里，经常需要：

- **点击 Token**：在当前光标位置插入（不是永远 append 到末尾）
- **拖拽 Token**：拖拽过程中光标跟随鼠标移动；松手时在该位置插入

本文提供一个稳定、可复用的实现方式，重点覆盖：

- `monospace` + `white-space: pre`（不自动换行、水平滚动）textarea 的 **point → caret** 映射
- HTML5 drag/drop 的事件组合（`onDragStart/onDragOver/onDrop`）

## 1) 关键结论（避免常见坑）

- 对 `white-space: pre` 的 textarea，**不要**用 “mirror div + DOM rect” 来反推 caret（跨浏览器水平坐标很容易漂移）。
- 更稳定的方式是：用 **行高 + 字符宽度（canvas measureText）** 做确定性映射：
  - 鼠标 \(x,y\) → (lineIndex, column) → absolute index

## 2) 坐标系统：鼠标点 → content 坐标

`clientX/Y` 基于 border box；caret 计算需要 content box（排除 border + padding）并包含 scroll：

```ts
const rect = ta.getBoundingClientRect();
const localX = (clientX - rect.left) - (ta.clientLeft || 0);
const localY = (clientY - rect.top) - (ta.clientTop || 0);
const cs = window.getComputedStyle(ta);
const padLeft = parseFloat(cs.paddingLeft || '0') || 0;
const padTop = parseFloat(cs.paddingTop || '0') || 0;

// content-start 坐标（排除 padding）+ scroll
const targetX = Math.max(0, localX - padLeft) + (ta.scrollLeft || 0);
const targetY = Math.max(0, localY - padTop) + (ta.scrollTop || 0);
```

## 3) `white-space: pre` 的 point → caret index（推荐实现）

### (1) 行高 lineHeight

- 优先用 computed `lineHeight`
- 如果是 `normal`，fallback：`fontSize * 1.2`

### (2) 字符宽度 charWidth（monospace）

用 canvas 按 textarea 的 font 测量（例如用 `M`）：

```ts
const font = `${cs.fontStyle} ${cs.fontVariant} ${cs.fontWeight} ${cs.fontSize} / ${cs.lineHeight} ${cs.fontFamily}`;
const canvas = document.createElement('canvas');
const ctx = canvas.getContext('2d');
ctx.font = font;
const charWidth = Math.max(1, ctx.measureText('M').width);
```

### (3) 行、列与 index 映射

```ts
const lines = value.split('\n');
const lineIndex = clamp(Math.floor(targetY / lineHeight), 0, lines.length - 1);
const line = lines[lineIndex] ?? '';
const col = Math.max(0, Math.floor(targetX / charWidth));

// 可选：tabSize（让 \t 的视觉列更接近实际）
const tabSize = parseInt(cs.tabSize ?? '8', 10) || 8;
let visualCol = 0;
let idxInLine = 0;
while (idxInLine < line.length) {
  const ch = line[idxInLine];
  if (ch === '\t') {
    const next = visualCol + (tabSize - (visualCol % tabSize));
    if (next > col) break;
    visualCol = next;
    idxInLine++;
    continue;
  }
  if (visualCol >= col) break;
  visualCol++;
  idxInLine++;
}

let abs = 0;
for (let i = 0; i < lineIndex; i++) abs += lines[i].length + 1; // + '\n'
abs += Math.min(idxInLine, line.length);
```

## 4) 插入 Token（Click）

```ts
const idx = ta.selectionStart ?? 0;
value = value.slice(0, idx) + token + value.slice(idx);
requestAnimationFrame(() => ta.setSelectionRange(idx + token.length, idx + token.length));
```

## 5) 插入 Token（Drag & Drop）

### Token 按钮

- `draggable`
- `onDragStart`: `dataTransfer.setData('text/plain', tokenText)`

### Textarea

- `onDragOver`:
  - `e.preventDefault()`（不然 drop 不会触发）
  - 计算 caret index
  - `ta.setSelectionRange(idx, idx)` 让光标跟随鼠标
- `onDrop`:
  - `e.preventDefault()`
  - 读取 `dataTransfer.getData('text/plain')`
  - 在 drop caret index 插入 token

## 6) 参考实现

- `src/components/message/MessageEditor.tsx`（Message Template code editor 的 click/drag token 插入）

