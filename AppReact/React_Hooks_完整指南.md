# React Hooks 完整指南

## 概述

`useEffect`、`useMemo`、`useCallback` 等统称为 **React Hooks**（React 钩子）。

React Hooks 是 React 16.8 引入的功能，允许在函数组件中使用状态（state）和其他 React 特性，而无需编写类组件。

---

## React Hooks 完整列表

### 基础 Hooks
1. **useState** - 状态管理
2. **useEffect** - 副作用处理
3. **useContext** - 上下文访问

### 额外 Hooks
4. **useReducer** - 复杂状态管理
5. **useCallback** - 函数记忆化
6. **useMemo** - 值记忆化
7. **useRef** - 引用对象
8. **useImperativeHandle** - 暴露子组件方法
9. **useLayoutEffect** - 同步副作用
10. **useDebugValue** - 自定义 Hook 调试

### React 18+ 新增 Hooks
11. **useTransition** - 过渡状态管理
12. **useDeferredValue** - 延迟值更新
13. **useId** - 唯一 ID 生成
14. **useSyncExternalStore** - 外部存储同步
15. **useInsertionEffect** - CSS-in-JS 优化

### 第三方库常用 Hooks
- **useSelector** / **useDispatch** (React Redux)
- **useNavigate** / **useParams** (React Router)

---

## 详细说明

### 1. useState - 状态管理

**功能**：在函数组件中添加状态。

**用法**：
```typescript
const [state, setState] = useState(initialValue);
```

**例子**：
```typescript
import { useState } from 'react';

function Counter() {
  const [count, setCount] = useState(0);
  const [name, setName] = useState('');

  return (
    <div>
      <p>计数: {count}</p>
      <button onClick={() => setCount(count + 1)}>增加</button>
      
      <input 
        value={name} 
        onChange={(e) => setName(e.target.value)} 
        placeholder="输入姓名"
      />
      <p>你好, {name}</p>
    </div>
  );
}
```

**使用场景**：
- 组件内部状态管理
- 表单输入值
- UI 状态（如开关、展开/收起）
- 计数器、列表等简单状态

**注意事项**：
- 状态更新是异步的
- 函数式更新：`setCount(prev => prev + 1)` 避免闭包问题（详见下方"闭包问题详解"章节）

---

### 2. useEffect - 副作用处理

**功能**：处理副作用，如数据获取、订阅、DOM 操作等。相当于类组件的 `componentDidMount`、`componentDidUpdate`、`componentWillUnmount`。

**用法**：
```typescript
useEffect(() => {
  // 副作用代码
  return () => {
    // 清理函数（可选）
  };
}, [dependencies]); // 依赖数组
```

**例子**：
```typescript 
import { useState, useEffect } from 'react';

function UserProfile({ userId }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // 1. 组件挂载时执行（空依赖数组）
  useEffect(() => {
    console.log('组件已挂载');
    return () => {
      console.log('组件即将卸载');
    };
  }, []);

  // 2. userId 变化时获取用户数据
  useEffect(() => {
    setLoading(true);
    fetch(`/api/users/${userId}`)
      .then(res => res.json())
      .then(data => {
        setUser(data);
        setLoading(false);
      });
  }, [userId]); // 依赖 userId

  // 3. 每次渲染都执行（无依赖数组）
  useEffect(() => {
    console.log('组件已渲染');
  }); // ⚠️ 不推荐，可能导致无限循环

  // 4. 清理订阅
  useEffect(() => {
    const subscription = subscribe();
    return () => {
      subscription.unsubscribe(); // 清理
    };
  }, []);

  if (loading) return <div>加载中...</div>;
  return <div>{user?.name}</div>;
}
```

**使用场景**：
- 数据获取（API 调用）
- 订阅事件（WebSocket、定时器）
- DOM 操作
- 设置/清理定时器
- 监听窗口大小变化

**依赖数组规则**：
- `[]` - 只在挂载时执行一次
- `[dep1, dep2]` - 依赖变化时执行
- 无依赖数组 - 每次渲染都执行（⚠️ 危险）

---

### 3. useContext - 上下文访问

**功能**：访问 React Context，避免 props 层层传递。

**用法**：
```typescript
const value = useContext(MyContext);
```

**例子**：
```typescript
import { createContext, useContext, useState } from 'react';

// 1. 创建 Context
const ThemeContext = createContext('light');

// 2. Provider 组件
function ThemeProvider({ children }) {
  const [theme, setTheme] = useState('light');
  
  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

// 3. 使用 Context
function Button() {
  const { theme, setTheme } = useContext(ThemeContext);
  
  return (
    <button 
      style={{ background: theme === 'light' ? '#fff' : '#333' }}
      onClick={() => setTheme(theme === 'light' ? 'dark' : 'light')}
    >
      切换主题
    </button>
  );
}

// 4. 应用
function App() {
  return (
    <ThemeProvider>
      <Button />
    </ThemeProvider>
  );
}
```

**使用场景**：
- 主题切换
- 用户认证信息
- 多语言（i18n）
- 全局配置

---

### 4. useReducer - 复杂状态管理

**功能**：管理复杂状态逻辑，类似 Redux 的 reducer 模式。

**用法**：
```typescript
const [state, dispatch] = useReducer(reducer, initialState);
```

**例子**：
```typescript
import { useReducer } from 'react';

// Reducer 函数
function todoReducer(state, action) {
  switch (action.type) {
    case 'ADD':
      return [...state, { id: Date.now(), text: action.text, done: false }];
    case 'TOGGLE':
      return state.map(todo =>
        todo.id === action.id ? { ...todo, done: !todo.done } : todo
      );
    case 'DELETE':
      return state.filter(todo => todo.id !== action.id);
    default:
      return state;
  }
}

function TodoList() {
  const [todos, dispatch] = useReducer(todoReducer, []);

  return (
    <div>
      <button onClick={() => dispatch({ type: 'ADD', text: '新任务' })}>
        添加任务
      </button>
      {todos.map(todo => (
        <div key={todo.id}>
          <input
            type="checkbox"
            checked={todo.done}
            onChange={() => dispatch({ type: 'TOGGLE', id: todo.id })}
          />
          <span>{todo.text}</span>
          <button onClick={() => dispatch({ type: 'DELETE', id: todo.id })}>
            删除
          </button>
        </div>
      ))}
    </div>
  );
}
```

**使用场景**：
- 复杂状态逻辑（多个子值）
- 状态更新逻辑集中管理
- 替代多个 useState
- 状态机模式

**vs useState**：
- `useState` 适合简单状态
- `useReducer` 适合复杂状态或状态逻辑复杂的情况

---

### 5. useCallback - 函数记忆化

**功能**：记忆化函数，避免不必要的函数重新创建，优化子组件渲染。

**用法**：
```typescript
const memoizedCallback = useCallback(() => {
  // 函数体
}, [dependencies]);
```

**例子**：
```typescript
import { useState, useCallback, memo } from 'react';

// 子组件（使用 memo 优化）
const ExpensiveChild = memo(({ onClick, name }) => {
  console.log('子组件渲染:', name);
  return <button onClick={onClick}>点击 {name}</button>;
});

function Parent() {
  const [count, setCount] = useState(0);
  const [name, setName] = useState('');

  // ❌ 没有 useCallback - 每次渲染都创建新函数
  // const handleClick = () => {
  //   console.log('点击了');
  // };

  // ✅ 使用 useCallback - 只有 name 变化时才创建新函数
  const handleClick = useCallback(() => {
    console.log('点击了', name);
  }, [name]);

  return (
    <div>
      <input value={name} onChange={(e) => setName(e.target.value)} />
      <p>计数: {count}</p>
      <button onClick={() => setCount(count + 1)}>增加计数</button>
      
      {/* 只有 name 变化时，ExpensiveChild 才会重新渲染 */}
      <ExpensiveChild onClick={handleClick} name={name} />
    </div>
  );
}
```

**为什么只有 name 变化时才重新渲染？**

这是 `React.memo` 和 `useCallback` 配合工作的结果：

1. **React.memo 的工作原理**：
   ```typescript
   const ExpensiveChild = memo(({ onClick, name }) => { ... });
   ```
   - `memo` 会对组件的 props 进行**浅比较**（shallow comparison）
   - 如果 props 没有变化，就跳过重新渲染
   - 相当于：`prevProps === nextProps`（浅比较）

2. **useCallback 的作用**：
   ```typescript
   const handleClick = useCallback(() => {
     console.log('点击了', name);
   }, [name]); // 依赖 name
   ```
   - 当 `name` **不变**时，`handleClick` 的**引用不变**（同一个函数对象）
   - 当 `name` **变化**时，`handleClick` 的**引用改变**（新的函数对象）

3. **执行流程分析**：

   **场景 A：只有 count 变化**
   ```typescript
   // 初始状态：count = 0, name = ''
   // handleClick 引用：func1
   // ExpensiveChild props: { onClick: func1, name: '' }
   
   // 点击"增加计数"按钮 → count 变为 1
   // Parent 重新渲染
   // useCallback 检查依赖 [name] → name 没变 → handleClick 引用仍然是 func1
   // ExpensiveChild props: { onClick: func1, name: '' }
   
   // memo 比较：
   // prevProps.onClick === nextProps.onClick → true (同一个引用)
   // prevProps.name === nextProps.name → true ('')
   // ✅ 所有 props 相同 → 跳过重新渲染！
   ```

   **场景 B：name 变化**
   ```typescript
   // 初始状态：count = 0, name = ''
   // handleClick 引用：func1
   // ExpensiveChild props: { onClick: func1, name: '' }
   
   // 输入框输入 'John' → name 变为 'John'
   // Parent 重新渲染
   // useCallback 检查依赖 [name] → name 变了 → 创建新的 handleClick 引用 func2
   // ExpensiveChild props: { onClick: func2, name: 'John' }
   
   // memo 比较：
   // prevProps.onClick === nextProps.onClick → false (不同引用)
   // prevProps.name === nextProps.name → false ('John' !== '')
   // ❌ props 变化 → 重新渲染！
   ```

4. **如果没有 useCallback 会怎样？**
   ```typescript
   // ❌ 没有 useCallback
   const handleClick = () => {
     console.log('点击了', name);
   };
   
   // 每次 Parent 渲染都会创建新的函数引用
   // count 变化时：
   // - Parent 重新渲染
   // - handleClick 是新函数（新引用）
   // - memo 检测到 onClick prop 变化
   // - ExpensiveChild 重新渲染 ❌（即使 name 没变）
   ```

5. **完整对比表**：

   | 情况 | count 变化 | name 变化 | handleClick 引用 | ExpensiveChild 渲染？ |
   |------|-----------|----------|-----------------|---------------------|
   | ✅ 有 useCallback | ✓ | ✗ | 不变 | ❌ 不渲染 |
   | ✅ 有 useCallback | ✗ | ✓ | 改变 | ✅ 渲染 |
   | ❌ 无 useCallback | ✓ | ✗ | 改变（新函数） | ✅ 渲染（不必要） |
   | ❌ 无 useCallback | ✗ | ✓ | 改变（新函数） | ✅ 渲染 |

**关键点总结**：
- `React.memo` 通过**引用比较**来判断 props 是否变化
- `useCallback` 确保函数引用**稳定**（除非依赖变化）
- 两者配合：只有当**真正影响子组件的 props 变化**时，子组件才重新渲染
- 这就是为什么只有 `name` 变化时，`ExpensiveChild` 才会重新渲染

**使用场景**：
- 传递给子组件的回调函数（配合 `memo`）
- 作为其他 Hook 的依赖
- 防止不必要的子组件重新渲染
- 优化性能

**注意事项**：
- 不要过度使用，本身也有开销
- 只在确实需要优化时使用
- 配合 `React.memo` 使用效果更好

---

### 6. useMemo - 值记忆化

**功能**：记忆化计算结果，避免每次渲染都重新计算。

**用法**：
```typescript
const memoizedValue = useMemo(() => {
  return expensiveComputation(a, b);
}, [a, b]);
```

**例子**：
```typescript
import { useState, useMemo } from 'react';

function ExpensiveComponent({ items, filter }) {
  // ❌ 没有 useMemo - 每次渲染都重新计算
  // const filteredItems = items.filter(item => 
  //   item.name.includes(filter)
  // );

  // ✅ 使用 useMemo - 只有 items 或 filter 变化时才重新计算
  const filteredItems = useMemo(() => {
    console.log('正在过滤...');
    return items.filter(item => item.name.includes(filter));
  }, [items, filter]);

  // 复杂计算
  const sortedItems = useMemo(() => {
    return [...filteredItems].sort((a, b) => a.price - b.price);
  }, [filteredItems]);

  return (
    <div>
      {sortedItems.map(item => (
        <div key={item.id}>{item.name} - ${item.price}</div>
      ))}
    </div>
  );
}

function App() {
  const [items] = useState([
    { id: 1, name: '苹果', price: 5 },
    { id: 2, name: '香蕉', price: 3 },
  ]);
  const [filter, setFilter] = useState('');
  const [count, setCount] = useState(0);

  return (
    <div>
      <input value={filter} onChange={(e) => setFilter(e.target.value)} />
      <button onClick={() => setCount(count + 1)}>计数: {count}</button>
      {/* count 变化时，ExpensiveComponent 不会重新计算过滤 */}
      <ExpensiveComponent items={items} filter={filter} />
    </div>
  );
}
```

**使用场景**：
- 昂贵的计算（排序、过滤、转换）
- 创建对象/数组引用（避免子组件不必要的渲染）
- 依赖其他值的派生状态

**vs useCallback**：
- `useMemo` 记忆化**值**
- `useCallback` 记忆化**函数**
- `useCallback(fn, deps)` 等价于 `useMemo(() => fn, deps)`

---

### 7. useRef - 引用对象

**功能**：创建一个可变的引用对象，在组件整个生命周期内保持不变。

**用法**：
```typescript
const ref = useRef(initialValue);
// 访问: ref.current
```

**例子**：
```typescript
import { useRef, useEffect, useState } from 'react';

function MyComponent() {
  // 1. DOM 引用
  const inputRef = useRef<HTMLInputElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  // 2. 存储可变值（不触发重新渲染）
  const countRef = useRef(0);
  const previousValueRef = useRef<string>();

  const [value, setValue] = useState('');

  // 3. 访问 DOM
  useEffect(() => {
    inputRef.current?.focus(); // 自动聚焦
  }, []);

  // 4. 保存上一次的值
  useEffect(() => {
    previousValueRef.current = value;
  }, [value]);

  // 5. 计数器（不触发渲染）
  const handleClick = () => {
    countRef.current += 1;
    console.log('点击次数:', countRef.current);
    // 注意：不会触发重新渲染！
  };

  return (
    <div>
      <input ref={inputRef} value={value} onChange={(e) => setValue(e.target.value)} />
      <button ref={buttonRef} onClick={handleClick}>
        点击（不触发渲染）
      </button>
      <p>当前值: {value}</p>
      <p>上一次值: {previousValueRef.current}</p>
    </div>
  );
}
```

**使用场景**：
- 访问 DOM 元素（聚焦、滚动、测量）
- 存储定时器 ID
- 保存上一次的值（不触发渲染）
- 存储任何可变值（不触发重新渲染）

**vs useState**：
- `useState` 变化会触发重新渲染
- `useRef` 变化**不会**触发重新渲染
- `useRef.current` 可以读写，`useState` 只能通过 setter 更新

---

### 8. useImperativeHandle - 暴露子组件方法

**功能**：自定义暴露给父组件的实例值，配合 `forwardRef` 使用。

**用法**：
```typescript
useImperativeHandle(ref, () => ({
  // 暴露的方法或属性
}), [dependencies]);
```

**例子**：
```typescript
import { forwardRef, useImperativeHandle, useRef } from 'react';

// 子组件
const FancyInput = forwardRef((props, ref) => {
  const inputRef = useRef<HTMLInputElement>(null);

  useImperativeHandle(ref, () => ({
    focus: () => {
      inputRef.current?.focus();
    },
    scrollIntoView: () => {
      inputRef.current?.scrollIntoView();
    },
    getValue: () => {
      return inputRef.current?.value;
    },
  }));

  return <input ref={inputRef} {...props} />;
});

// 父组件
function Parent() {
  const inputRef = useRef<{ focus: () => void; getValue: () => string }>(null);

  return (
    <div>
      <FancyInput ref={inputRef} />
      <button onClick={() => inputRef.current?.focus()}>
        聚焦输入框
      </button>
      <button onClick={() => console.log(inputRef.current?.getValue())}>
        获取值
      </button>
    </div>
  );
}
```

**使用场景**：
- 封装第三方组件
- 暴露特定的方法给父组件
- 限制父组件对子组件的访问

---

### 9. useLayoutEffect - 同步副作用

**功能**：与 `useEffect` 类似，但在 DOM 更新后、浏览器绘制前同步执行。

**用法**：
```typescript
useLayoutEffect(() => {
  // 副作用代码
}, [dependencies]);
```

**例子**：
```typescript
import { useState, useLayoutEffect, useRef } from 'react';

function Tooltip({ text }) {
  const [position, setPosition] = useState({ top: 0, left: 0 });
  const tooltipRef = useRef<HTMLDivElement>(null);

  // 需要在浏览器绘制前计算位置，避免闪烁
  useLayoutEffect(() => {
    if (tooltipRef.current) {
      const rect = tooltipRef.current.getBoundingClientRect();
      // 调整位置，避免超出屏幕
      setPosition({
        top: rect.top < 0 ? 0 : rect.top,
        left: rect.left < 0 ? 0 : rect.left,
      });
    }
  }, [text]);

  return (
    <div ref={tooltipRef} style={{ position: 'absolute', ...position }}>
      {text}
    </div>
  );
}
```

**使用场景**：
- DOM 测量和调整（避免闪烁）
- 动画相关操作
- 需要在绘制前完成的 DOM 操作

**vs useEffect**：
- `useEffect` - 异步，在浏览器绘制**后**执行
- `useLayoutEffect` - 同步，在浏览器绘制**前**执行
- 大多数情况用 `useEffect`，只有需要同步执行时才用 `useLayoutEffect`

---

### 10. useTransition - 过渡状态管理（React 18+）

**功能**：标记某些状态更新为"过渡"，不阻塞 UI，提升用户体验。

**用法**：
```typescript
const [isPending, startTransition] = useTransition();
```

**例子**：
```typescript
import { useState, useTransition } from 'react';

function SearchResults() {
  const [isPending, startTransition] = useTransition();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);

  const handleChange = (e) => {
    const value = e.target.value;
    setQuery(value); // 立即更新输入框（高优先级）

    // 搜索结果更新标记为过渡（低优先级）
    startTransition(() => {
      setResults(searchLargeList(value)); // 不会阻塞输入
    });
  };

  return (
    <div>
      <input value={query} onChange={handleChange} />
      {isPending && <div>搜索中...</div>}
      <ul>
        {results.map(item => (
          <li key={item.id}>{item.name}</li>
        ))}
      </ul>
    </div>
  );
}
```

**使用场景**：
- 大量数据渲染
- 搜索过滤
- 标签切换
- 任何可能阻塞 UI 的更新

**⚠️ 重要理解：不是防抖（Debounce）！**

`useTransition` **不是**等待输入结束再执行，而是：
- ✅ **仍然每输入一个字符就执行查询**
- ✅ **但查询不会阻塞输入框**（输入框保持响应）
- ✅ **优先级管理**：输入框更新（高优先级）vs 搜索结果更新（低优先级）

**对比示例**：

```typescript
// ❌ 误解：以为 useTransition 是等待输入结束
// 实际上：每输入一个字符都会执行，只是不阻塞

// ✅ 正确理解：优先级管理
function SearchResults() {
  const [isPending, startTransition] = useTransition();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);

  const handleChange = (e) => {
    const value = e.target.value;
    
    // 高优先级：立即更新输入框（用户看到输入）
    setQuery(value);
    
    // 低优先级：搜索结果更新（不阻塞输入）
    startTransition(() => {
      setResults(searchLargeList(value)); // 仍然会执行，但不会卡住输入框
    });
  };
  
  // 用户输入 "12345"：
  // 1. 输入 "1" → setQuery('1') 立即更新 → startTransition 执行查询（不阻塞）
  // 2. 输入 "2" → setQuery('12') 立即更新 → startTransition 执行查询（不阻塞）
  // 3. 输入 "3" → setQuery('123') 立即更新 → startTransition 执行查询（不阻塞）
  // ... 每输入一个字符都会执行，但输入框始终流畅
}
```

**如果需要"等待输入结束"（防抖），应该这样做**：

```typescript
import { useState, useEffect, useRef } from 'react';

function SearchWithDebounce() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);
  const debounceTimerRef = useRef<NodeJS.Timeout>();

  const handleChange = (e) => {
    const value = e.target.value;
    setQuery(value); // 立即更新输入框
    
    // ✅ 防抖：清除之前的定时器
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }
    
    // ✅ 等待 300ms 没有输入才执行查询
    debounceTimerRef.current = setTimeout(() => {
      setResults(searchLargeList(value)); // 只在输入停止 300ms 后执行
    }, 300);
  };

  useEffect(() => {
    return () => {
      // 清理定时器
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, []);

  return (
    <div>
      <input value={query} onChange={handleChange} />
      <ul>
        {results.map(item => <li key={item.id}>{item.name}</li>)}
      </ul>
    </div>
  );
}
```

**useTransition vs 防抖对比**：

| 特性 | useTransition | 防抖（Debounce） |
|------|--------------|-----------------|
| **执行时机** | 每输入一个字符就执行 | 等待输入停止后执行 |
| **目的** | 不阻塞 UI，保持响应 | 减少执行次数，节省资源 |
| **用户体验** | 输入流畅，结果逐步更新 | 输入流畅，结果延迟显示 |
| **适用场景** | 大量渲染、复杂计算 | API 调用、网络请求 |
| **性能优化** | 优先级管理 | 减少执行次数 |

**组合使用（最佳实践）**：

```typescript
function OptimizedSearch() {
  const [isPending, startTransition] = useTransition();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);
  const debounceTimerRef = useRef<NodeJS.Timeout>();

  const handleChange = (e) => {
    const value = e.target.value;
    setQuery(value); // 立即更新输入框
    
    // ✅ 防抖：等待输入停止
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }
    
    debounceTimerRef.current = setTimeout(() => {
      // ✅ useTransition：不阻塞 UI
      startTransition(() => {
        setResults(searchLargeList(value));
      });
    }, 300);
  };

  return (
    <div>
      <input value={query} onChange={handleChange} />
      {isPending && <div>搜索中...</div>}
      <ul>
        {results.map(item => <li key={item.id}>{item.name}</li>)}
      </ul>
    </div>
  );
}
```

**总结**：
- `useTransition`：**不等待**，每输入都执行，但**不阻塞 UI**
- 防抖：**等待输入结束**，减少执行次数
- 两者可以**组合使用**：防抖减少执行 + useTransition 保持响应

---

### 11. useDeferredValue - 延迟值更新（React 18+）

**功能**：延迟更新值，让 UI 保持响应。

**用法**：
```typescript
const deferredValue = useDeferredValue(value);
```

**例子**：
```typescript
import { useState, useDeferredValue, useMemo } from 'react';

function ProductList({ query }) {
  const deferredQuery = useDeferredValue(query);
  
  // 使用延迟的查询值进行过滤
  const products = useMemo(() => {
    return filterProducts(deferredQuery);
  }, [deferredQuery]);

  return (
    <div>
      {products.map(product => (
        <div key={product.id}>{product.name}</div>
      ))}
    </div>
  );
}

function App() {
  const [query, setQuery] = useState('');
  
  return (
    <div>
      <input value={query} onChange={(e) => setQuery(e.target.value)} />
      {/* query 立即更新，但 ProductList 使用延迟值 */}
      <ProductList query={query} />
    </div>
  );
}
```

**使用场景**：
- 输入框实时搜索
- 大量列表渲染
- 与 `useTransition` 类似，但用于值而非状态更新

**⚠️ 同样不是防抖！**

`useDeferredValue` 也是**每输入一个字符就更新**，只是：
- ✅ 输入框使用最新值（`query`）
- ✅ 列表使用延迟值（`deferredQuery`）
- ✅ 列表更新不会阻塞输入框

**示例对比**：

```typescript
// useDeferredValue：仍然每输入都更新，只是延迟渲染
function ProductList({ query }) {
  const deferredQuery = useDeferredValue(query);
  // query = '12345' → deferredQuery 可能还是 '1234'（延迟更新）
  // 但最终会更新到 '12345'
  
  const products = useMemo(() => {
    return filterProducts(deferredQuery);
  }, [deferredQuery]);

  return (
    <div>
      {/* 输入框显示最新值 query */}
      <input value={query} onChange={...} />
      
      {/* 列表使用延迟值 deferredQuery，不会阻塞输入 */}
      {products.map(...)}
    </div>
  );
}
```

**如果需要防抖，参考上面的防抖示例。**

---

### 12. useId - 唯一 ID 生成（React 18+）

**功能**：生成唯一的 ID，用于表单标签、ARIA 属性等。

**用法**：
```typescript
const id = useId();
```

**例子**：
```typescript
import { useId } from 'react';

function FormField({ label }) {
  const id = useId(); // 生成唯一 ID，如 ":r1:"

  return (
    <div>
      <label htmlFor={id}>{label}</label>
      <input id={id} type="text" />
    </div>
  );
}

function App() {
  return (
    <form>
      <FormField label="姓名" />
      <FormField label="邮箱" />
      {/* 每个 FormField 都有唯一的 ID */}
    </form>
  );
}
```

**使用场景**：
- 表单 label 和 input 关联
- ARIA 属性
- 需要唯一 ID 的场景

**vs 其他 ID 生成方式**：
- `Math.random()` - 服务端和客户端不一致
- `uuid` - 可以，但 `useId` 更符合 React 规范

---

## 使用场景总结

### 状态管理
- **简单状态** → `useState`
- **复杂状态** → `useReducer`
- **全局状态** → `useContext` + Provider

### 副作用处理
- **一般副作用** → `useEffect`
- **DOM 测量/动画** → `useLayoutEffect`
- **CSS-in-JS** → `useInsertionEffect`

### 性能优化
- **记忆化函数** → `useCallback`（配合 `memo`）
- **记忆化值** → `useMemo`
- **延迟更新** → `useTransition` / `useDeferredValue`

### DOM 操作
- **DOM 引用** → `useRef`
- **暴露方法** → `useImperativeHandle` + `forwardRef`

### React 18+ 新特性
- **过渡更新** → `useTransition`
- **延迟值** → `useDeferredValue`
- **唯一 ID** → `useId`

---

## 最佳实践

### 1. 依赖数组要完整
```typescript
// ❌ 错误：缺少依赖
useEffect(() => {
  fetchData(userId);
}, []); // userId 变化时不会重新获取

// ✅ 正确：包含所有依赖
useEffect(() => {
  fetchData(userId);
}, [userId]);
```

### 2. 不要过度使用 useMemo/useCallback
```typescript
// ❌ 不必要的优化
const value = useMemo(() => count + 1, [count]); // 简单计算不需要

// ✅ 只在真正需要时使用
const expensiveValue = useMemo(() => {
  return heavyComputation(data);
}, [data]);
```

### 3. 清理副作用
```typescript
useEffect(() => {
  const timer = setInterval(() => {
    // ...
  }, 1000);
  
  return () => clearInterval(timer); // ✅ 记得清理
}, []);
```

### 4. 自定义 Hook 提取逻辑
```typescript
// ✅ 提取为自定义 Hook
function useFetch(url) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  
  useEffect(() => {
    fetch(url)
      .then(res => res.json())
      .then(data => {
        setData(data);
        setLoading(false);
      });
  }, [url]);
  
  return { data, loading };
}
```

---

## 闭包问题详解

### 什么是闭包问题？

**闭包（Closure）**是 JavaScript 的特性：函数可以访问其外部作用域的变量，即使外部函数已经执行完毕。

在 React Hooks 中，**闭包问题**指的是：由于闭包的特性，函数"记住"了创建时的旧值，而不是最新的值。

---

### 问题示例 1：useState 中的闭包问题

#### ❌ 错误示例：直接使用状态值

```typescript
import { useState, useEffect } from 'react';

function Counter() {
  const [count, setCount] = useState(0);

  useEffect(() => {
    const timer = setInterval(() => {
      // ❌ 问题：这里访问的 count 是创建时的值（0）
      // 即使 count 更新了，这里仍然使用旧的 count 值
      setCount(count + 1);
    }, 1000);

    return () => clearInterval(timer);
  }, []); // 空依赖数组，只在挂载时执行一次

  return <div>计数: {count}</div>;
}
```

**问题分析**：
1. `useEffect` 只在组件挂载时执行一次（依赖数组为空 `[]`）
2. 此时 `count` 的值是 `0`
3. `setInterval` 的回调函数"闭包"捕获了这个 `count = 0`
4. 即使 `count` 更新为 1、2、3...，定时器回调中的 `count` 仍然是 `0`
5. 结果：`setCount(0 + 1)` 总是设置为 `1`，计数器卡在 1

#### ✅ 解决方案 1：函数式更新

```typescript
function Counter() {
  const [count, setCount] = useState(0);

  useEffect(() => {
    const timer = setInterval(() => {
      // ✅ 使用函数式更新：prev 总是最新的状态值
      setCount(prev => prev + 1);
    }, 1000);

    return () => clearInterval(timer);
  }, []); // 不需要依赖 count

  return <div>计数: {count}</div>;
}
```

**为什么有效**：
- `setCount(prev => prev + 1)` 中的 `prev` 参数是 React 提供的**最新状态值**
- 不依赖外部的 `count` 变量，避免了闭包问题

---

### 问题示例 2：useEffect 中的闭包问题

#### ❌ 错误示例：缺少依赖导致闭包问题

```typescript
function UserProfile({ userId }) {
  const [user, setUser] = useState(null);

  useEffect(() => {
    // ❌ 问题：这里访问的 userId 是创建时的值
    // 如果 userId 从 1 变成 2，这个 effect 不会重新执行
    // 仍然使用旧的 userId = 1 去获取数据
    fetch(`/api/users/${userId}`)
      .then(res => res.json())
      .then(data => setUser(data));
  }, []); // 缺少 userId 依赖！

  return <div>{user?.name}</div>;
}
```

**问题分析**：
- `useEffect` 只在挂载时执行一次
- 闭包捕获了初始的 `userId` 值
- 当 `userId` prop 变化时，effect 不会重新执行
- 结果：始终使用旧的 `userId` 获取数据

#### ✅ 解决方案：添加依赖

```typescript
function UserProfile({ userId }) {
  const [user, setUser] = useState(null);

  useEffect(() => {
    // ✅ 正确：userId 在依赖数组中
    // 当 userId 变化时，effect 会重新执行
    fetch(`/api/users/${userId}`)
      .then(res => res.json())
      .then(data => setUser(data));
  }, [userId]); // ✅ 包含 userId 依赖

  return <div>{user?.name}</div>;
}
```

---

### 问题示例 3：事件处理函数中的闭包问题

#### ❌ 错误示例：闭包捕获旧值

```typescript
function App() {
  const [count, setCount] = useState(0);
  const [message, setMessage] = useState('');

  useEffect(() => {
    // ❌ 问题：这个函数闭包捕获了初始的 count = 0
    const handleClick = () => {
      alert(`当前计数是: ${count}`); // 总是显示 0
    };

    // 假设某个第三方库需要这个处理函数
    someLibrary.on('click', handleClick);

    return () => {
      someLibrary.off('click', handleClick);
    };
  }, []); // 缺少 count 依赖

  return (
    <div>
      <p>计数: {count}</p>
      <button onClick={() => setCount(count + 1)}>增加</button>
    </div>
  );
}
```

#### ✅ 解决方案 1：使用 useRef 存储最新值

```typescript
function App() {
  const [count, setCount] = useState(0);
  const countRef = useRef(count);

  // 保持 ref 与 state 同步
  useEffect(() => {
    countRef.current = count;
  }, [count]);

  useEffect(() => {
    // ✅ 使用 ref.current 获取最新值
    const handleClick = () => {
      alert(`当前计数是: ${countRef.current}`); // 总是最新值
    };

    someLibrary.on('click', handleClick);

    return () => {
      someLibrary.off('click', handleClick);
    };
  }, []); // 不需要依赖 count

  return (
    <div>
      <p>计数: {count}</p>
      <button onClick={() => setCount(count + 1)}>增加</button>
    </div>
  );
}
```

#### ✅ 解决方案 2：添加依赖（如果可能）

```typescript
function App() {
  const [count, setCount] = useState(0);

  useEffect(() => {
    // ✅ 每次 count 变化时重新注册
    const handleClick = () => {
      alert(`当前计数是: ${count}`); // 使用最新的 count
    };

    someLibrary.on('click', handleClick);

    return () => {
      someLibrary.off('click', handleClick);
    };
  }, [count]); // ✅ 包含 count 依赖

  return (
    <div>
      <p>计数: {count}</p>
      <button onClick={() => setCount(count + 1)}>增加</button>
    </div>
  );
}
```

---

### 问题示例 4：useCallback 中的闭包问题

#### ❌ 错误示例：依赖不完整

```typescript
function Parent() {
  const [count, setCount] = useState(0);
  const [name, setName] = useState('');

  // ❌ 问题：useCallback 闭包捕获了初始的 count = 0
  const handleSubmit = useCallback(() => {
    console.log(`提交: ${name}, 计数: ${count}`); // count 总是 0
    // 发送数据...
  }, [name]); // 缺少 count 依赖！

  return (
    <div>
      <input value={name} onChange={(e) => setName(e.target.value)} />
      <button onClick={() => setCount(count + 1)}>计数: {count}</button>
      <button onClick={handleSubmit}>提交</button>
    </div>
  );
}
```

#### ✅ 解决方案：完整依赖数组

```typescript
function Parent() {
  const [count, setCount] = useState(0);
  const [name, setName] = useState('');

  // ✅ 正确：包含所有依赖
  const handleSubmit = useCallback(() => {
    console.log(`提交: ${name}, 计数: ${count}`); // 使用最新值
    // 发送数据...
  }, [name, count]); // ✅ 包含所有依赖

  return (
    <div>
      <input value={name} onChange={(e) => setName(e.target.value)} />
      <button onClick={() => setCount(count + 1)}>计数: {count}</button>
      <button onClick={handleSubmit}>提交</button>
    </div>
  );
}
```

---

### 闭包问题的根本原因

1. **JavaScript 闭包特性**：函数可以访问外部作用域的变量
2. **React 函数组件**：每次渲染都是新的函数调用，创建新的作用域
3. **Hooks 的依赖机制**：如果依赖数组不完整，函数会"记住"创建时的旧值

---

### 解决闭包问题的策略

#### 1. 函数式更新（useState）
```typescript
// ❌ 直接使用状态值
setCount(count + 1);

// ✅ 函数式更新
setCount(prev => prev + 1);
```

#### 2. 完整的依赖数组（useEffect, useCallback, useMemo）
```typescript
// ❌ 缺少依赖
useEffect(() => {
  doSomething(value);
}, []);

// ✅ 包含所有依赖
useEffect(() => {
  doSomething(value);
}, [value]);
```

#### 3. 使用 useRef 存储最新值
```typescript
const valueRef = useRef(value);
useEffect(() => {
  valueRef.current = value; // 保持最新
}, [value]);

// 在闭包中使用
someFunction(() => {
  console.log(valueRef.current); // 总是最新值
});
```

#### 4. 使用 useReducer（复杂状态）
```typescript
// useReducer 的 dispatch 是稳定的，不会闭包问题
const [state, dispatch] = useReducer(reducer, initialState);

useEffect(() => {
  // dispatch 不需要在依赖数组中
  someFunction(() => {
    dispatch({ type: 'UPDATE' }); // ✅ 安全
  });
}, []);
```

---

### 如何发现闭包问题？

1. **ESLint 插件**：安装 `eslint-plugin-react-hooks`
   ```json
   {
     "plugins": ["react-hooks"],
     "rules": {
       "react-hooks/exhaustive-deps": "warn"
     }
   }
   ```

2. **常见症状**：
   - 定时器/事件监听器使用旧值
   - 异步回调中使用过时的状态
   - 依赖数组警告但被忽略

3. **调试技巧**：
   ```typescript
   useEffect(() => {
     console.log('当前值:', count); // 检查是否是旧值
   }, [count]);
   ```

---

### 总结

| 场景 | 问题 | 解决方案 |
|------|------|----------|
| `useState` 更新 | 闭包捕获旧值 | 使用函数式更新 `setState(prev => ...)` |
| `useEffect` 依赖 | 缺少依赖导致使用旧值 | 添加完整依赖数组 |
| `useCallback` 依赖 | 回调函数使用旧值 | 包含所有依赖 |
| 事件监听器 | 闭包捕获初始值 | 使用 `useRef` 或添加依赖 |
| 定时器 | 定时器回调使用旧值 | 函数式更新或 `useRef` |

**记住**：闭包本身不是问题，但在 React Hooks 中，如果不正确处理依赖，就会导致使用过时的值。始终确保依赖数组完整，或使用函数式更新/useRef 来避免闭包陷阱。

---

## 常见问题

### Q: useEffect 和 useLayoutEffect 的区别？
A: `useEffect` 异步执行（绘制后），`useLayoutEffect` 同步执行（绘制前）。大多数情况用 `useEffect`。

### Q: useMemo 和 useCallback 的区别？
A: `useMemo` 记忆化值，`useCallback` 记忆化函数。`useCallback(fn, deps)` 等价于 `useMemo(() => fn, deps)`。

### Q: 什么时候用 useRef 而不是 useState？
A: 当值变化不需要触发重新渲染时用 `useRef`（如定时器 ID、DOM 引用）。

### Q: 依赖数组为空 [] 是什么意思？
A: 只在组件挂载时执行一次，卸载时清理。相当于 `componentDidMount` 和 `componentWillUnmount`。

---

## 参考资源

- [React 官方文档 - Hooks](https://react.dev/reference/react)
- [React Hooks 最佳实践](https://react.dev/learn/escape-hatches)
