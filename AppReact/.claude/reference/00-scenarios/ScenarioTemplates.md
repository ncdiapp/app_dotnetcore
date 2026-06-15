# Scenario-Based Prompt Templates

## Overview

This document provides prompt templates for common development scenarios. Use these templates with `MainPrompt.md` as the base context.

**Usage Pattern**:
1. Upload `MainPrompt.md` as the base context file
2. Use one of the scenario templates below as your task instruction
3. AI will use MainPrompt.md for context and follow the scenario-specific workflow

---

## Scenario 1: Complete Feature Migration

### Template

```
请根据 MainPrompt.md 中的标准和指南，完整迁移一个 AngularJS 功能模块到 React。

**任务**：
- AngularJS RouteCode: [RouteCode]
- 要求：完整实现起始页面、所有嵌套页面、链接页面和相关功能
- 目标：功能完全符合 AngularJS 版本，UI 符合 React 项目标准

**工作流程**：
1. 根据 RouteCode 定位 AngularJS 实现（route, controller, service, view, WebAPI）
2. 分析完整功能范围（包括所有相关页面）
3. 规划 React 实现（routes, components, state, APIs, UI）
4. 实现所有组件和功能
5. 验证功能匹配 AngularJS
6. 验证 UI 符合标准
7. 修复问题并迭代直到完成

**完成标准**：
- ✅ 所有功能正常工作
- ✅ 所有相关页面已实现
- ✅ UI 符合项目标准
- ✅ 无错误（linter, TypeScript, console）
- ✅ 功能与 AngularJS 版本一致
```

### When to Use
- 迁移新功能模块
- 需要完整实现所有相关页面

---

## Scenario 2: Analyze Incomplete Implementation

### Template

```
请根据 MainPrompt.md 中的标准，分析当前 React 实现与 AngularJS 版本的差异。

**任务**：
- React 组件路径: [组件路径，如 src/components/xxx/ComponentName.tsx]
- AngularJS RouteCode: [RouteCode]
- 要求：对比分析，找出缺失的功能和不符合标准的地方，等待我验证后再继续

**工作流程**：
1. 读取当前 React 实现
2. 根据 RouteCode 定位 AngularJS 实现
3. 对比功能差异：
   - 缺失的功能
   - 实现不完整的地方
   - UI 不符合标准的地方
   - 代码质量问题
4. 生成详细的分析报告
5. **等待用户验证**后再进行下一步

**输出格式**：
- 功能对比清单
- 缺失功能列表
- UI 标准符合性检查
- 代码质量检查
- 建议的修复方案
```

### When to Use
- 检查已做一半的页面
- 需要了解缺失什么功能
- 需要对比分析

---

## Scenario 3: Add Feature to Existing Page

### Template

```
请根据 MainPrompt.md 中的标准，在现有 React 页面中添加 AngularJS 已有的功能。

**任务**：
- React 组件路径: [组件路径]
- AngularJS RouteCode: [RouteCode]
- 要添加的功能: [功能描述或功能在 AngularJS 中的位置]
- 要求：添加功能后，页面功能完整，UI 符合标准

**工作流程**：
1. 读取当前 React 实现
2. 根据 RouteCode 和功能描述定位 AngularJS 实现
3. 分析要添加的功能：
   - 功能逻辑
   - UI 组件
   - API 调用
   - 状态管理
4. 规划如何集成到现有页面
5. 实现功能
6. 验证功能正常
7. 验证 UI 符合标准
8. 修复问题直到完成

**完成标准**：
- ✅ 新功能正常工作
- ✅ 不影响现有功能
- ✅ UI 符合项目标准
- ✅ 无错误
```

### When to Use
- 在现有页面添加新功能
- 补充缺失的功能点

---

## Scenario 4: Optimize Draft Page UI

### Template

```
请根据 MainPrompt.md 中的 UI 标准，优化草稿页面的 UI。

**任务**：
- React 组件路径: [组件路径]
- 当前状态: 草稿/初步实现
- 要求：按照项目 UI 标准优化和修改

**工作流程**：
1. 读取当前实现
2. 参考 UI 标准文档：
   - PageLayoutStandards.md
   - ButtonStandards.md
   - FormStandards.md
   - ThemeUsageStandards.md
   - TailwindCSSStandards.md
   - ReferenceComponents.md
3. 检查并修复：
   - 页面布局结构
   - 按钮样式和位置
   - 表单字段布局
   - 主题系统使用（无硬编码颜色）
   - Tailwind CSS 类使用
   - 间距和尺寸
4. 参考标准组件进行优化
5. 验证符合所有标准
6. 修复问题直到完成

**完成标准**：
- ✅ 页面布局符合标准
- ✅ 所有组件样式符合标准
- ✅ 使用主题系统（无硬编码颜色）
- ✅ Tailwind CSS 使用正确
- ✅ 间距和尺寸符合标准
- ✅ 无错误
```

### When to Use
- 优化草稿页面
- 修复 UI 不符合标准的问题
- 重构页面样式

---

## Scenario 5: Fix Bugs and Errors

### Template

```
请根据 MainPrompt.md 中的标准，修复页面中的错误和问题。

**任务**：
- React 组件路径: [组件路径]
- 错误描述: [错误信息或问题描述]
- 要求：修复错误，确保功能正常，UI 符合标准

**工作流程**：
1. 读取当前实现
2. 分析错误原因
3. 检查是否符合项目标准
4. 修复错误
5. 验证功能正常
6. 验证 UI 符合标准
7. 确保无新错误引入

**完成标准**：
- ✅ 错误已修复
- ✅ 功能正常工作
- ✅ UI 符合标准
- ✅ 无新错误
```

### When to Use
- 修复 bug
- 解决运行时错误
- 修复编译错误

---

## Scenario 6: Refactor Component

### Template

```
请根据 MainPrompt.md 中的标准，重构组件以符合项目规范。

**任务**：
- React 组件路径: [组件路径]
- 重构目标: [具体目标，如：改进代码结构、优化性能、符合标准等]
- 要求：重构后功能不变，代码质量提升，符合项目标准

**工作流程**：
1. 读取当前实现
2. 分析需要改进的地方
3. 参考项目标准和参考组件
4. 规划重构方案
5. 执行重构
6. 验证功能不变
7. 验证符合标准
8. 确保代码质量提升

**完成标准**：
- ✅ 功能保持不变
- ✅ 代码质量提升
- ✅ 符合项目标准
- ✅ 无错误
```

### When to Use
- 重构代码
- 改进代码质量
- 优化性能

---

## Scenario 7: Add New Component

### Template

```
请根据 MainPrompt.md 中的标准，创建新的 React 组件。

**任务**：
- 组件名称: [ComponentName]
- 组件路径: [src/components/xxx/ComponentName.tsx]
- 组件功能: [功能描述]
- 参考 AngularJS: [可选，如果有 AngularJS 参考]
- 要求：符合项目标准，功能完整，UI 符合标准

**工作流程**：
1. 如果参考 AngularJS，先定位 AngularJS 实现
2. 参考 UI 标准和参考组件
3. 规划组件结构
4. 实现组件
5. 添加路由（如需要）
6. 实现功能
7. 验证功能正常
8. 验证 UI 符合标准
9. 修复问题直到完成

**完成标准**：
- ✅ 组件功能完整
- ✅ UI 符合标准
- ✅ 代码质量良好
- ✅ 无错误
```

### When to Use
- 创建新组件
- 添加新功能页面

---

## General Guidelines for All Scenarios

### Always Include
1. **明确的任务描述**
2. **具体的文件路径**
3. **参考的 AngularJS RouteCode（如适用）**
4. **完成标准**

### AI Should Always
1. **读取 MainPrompt.md** 了解项目上下文
2. **参考相关标准文档**（UI标准、迁移指南等）
3. **对比 AngularJS 实现**（如适用）
4. **验证符合标准**
5. **修复问题直到完成**

### Output Format
- **清晰的步骤说明**
- **代码修改说明**
- **验证结果**
- **问题修复记录**

---

## Custom Scenario Template

如果需要自定义场景，使用以下模板：

```
请根据 MainPrompt.md 中的标准，[任务描述]。

**任务**：
- [具体任务要求]
- [相关文件路径]
- [参考信息]

**工作流程**：
1. [步骤1]
2. [步骤2]
3. [步骤3]
...

**完成标准**：
- ✅ [标准1]
- ✅ [标准2]
- ✅ [标准3]
```

---

*这些模板应与 MainPrompt.md 一起使用，MainPrompt.md 提供项目上下文和标准，场景模板提供具体任务指令。*
