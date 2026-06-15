# Main Prompt Index

This is the main entry point for all development prompts and guidelines for the React application migration project.

## 🎯 How to Use This Document

**This document serves as a universal context file for AI assistants.**

**Usage Pattern**:
1. **Upload this file** (`MainPrompt.md`) as the base context/background file
2. **Provide a specific task** using one of the scenario templates from `00-scenarios/ScenarioTemplates.md`
3. **AI will use this file** to understand project structure, standards, and guidelines
4. **AI will follow** the scenario-specific workflow to complete your task

**Example**:
- Upload: `MainPrompt.md` (this file)
- Task: "请根据 MainPrompt.md，完整迁移 RouteCode: MasterDataManagement 的功能模块"
- AI will: Use MainPrompt.md for context, follow migration workflow, implement complete feature

---

## Overview

This prompt library contains structured documentation for migrating an AngularJS application to React + Redux + Tailwind + Wijmo. All prompts are organized by functional modules for easy navigation and reference.

**Key Information**:
- **Project Type**: React 18 + TypeScript + Redux + Tailwind + Wijmo
- **Migration Source**: AngularJS application
- **Standards**: Comprehensive UI standards, coding patterns, and best practices
- **Reference**: AngularJS codebase structure and implementation patterns

## Quick Start

**For AI Assistants**: 
- Read this file to understand project context and standards
- Use scenario templates from `00-scenarios/ScenarioTemplates.md` for specific tasks
- Reference specific prompt files based on task requirements

**For Developers**:
- **General development**: Start with `02-migration/DevelopmentPrompt.md`
- **UI work**: See `03-ui/UIMainPrompt.md` for all UI standards
- **Architecture questions**: See `04-architecture/` for solutions
- **AngularJS reference**: See `01-reference-guides/AngularJsReferenceGuide.md`

---

## Directory Structure

```
Prompt/
├── MainPrompt.md (this file) ⭐ Universal context file
├── 00-scenarios/                  # Scenario-based prompt templates
│   └── ScenarioTemplates.md      # Use these templates with MainPrompt.md
├── 01-reference-guides/          # Reference documentation
│   └── AngularJsReferenceGuide.md
├── 02-migration/                  # Migration guides and workflows
│   ├── ConverterAngularJsPage.md
│   ├── ConverterAngularServiceToReact.md  # Service conversion rules ⭐
│   └── DevelopmentPrompt.md
├── 03-ui/                         # UI standards, layouts, and guidelines
│   ├── UIMainPrompt.md            # Main UI index (use this for UI work)
│   ├── layout/                    # Layout patterns
│   │   ├── TailwindFlexBoxRemainSpace.md
│   │   └── ApplicationConfigurationMenu.md
│   └── standards/                 # UI standards
│       ├── PageLayoutStandards.md
│       ├── ButtonStandards.md
│       ├── FormStandards.md
│       ├── ThemeUsageStandards.md
│       ├── TailwindCSSStandards.md
│       ├── ReferenceComponents.md
│       ├── WijmoGridMultiSelectStandards.md  # FlexGrid 多选、selectedRows、双列表转移
│       ├── WijmoGridNullRefFixes.md   # FlexGrid editRange/finishEditing 空引用 patch
│       └── JsonEditingStandards.md   # JSON stringify: use null, 2 for pretty-print
└── 04-architecture/               # Architecture and solutions
    └── ReactCshtmlCachingSolution.md
```

---

## Quick Reference Table

| Task | File to Read |
|------|-------------|
| **Scenario templates** | **`00-scenarios/ScenarioTemplates.md`** ⭐ Use with MainPrompt.md |
| Find AngularJS code | `01-reference-guides/AngularJsReferenceGuide.md` |
| Convert a feature | `02-migration/ConverterAngularJsPage.md` |
| **Convert Angular service** | **`02-migration/ConverterAngularServiceToReact.md`** ⭐ |
| General development | `02-migration/DevelopmentPrompt.md` |
| **UI standards (main)** | **`03-ui/UIMainPrompt.md`** |
| Page layout standards | `03-ui/standards/PageLayoutStandards.md` |
| Button standards | `03-ui/standards/ButtonStandards.md` |
| Form standards | `03-ui/standards/FormStandards.md` |
| Theme usage | `03-ui/standards/ThemeUsageStandards.md` |
| Tailwind CSS patterns | `03-ui/standards/TailwindCSSStandards.md` |
| Reference components | `03-ui/standards/ReferenceComponents.md` |
| **JSON editing (pretty-print)** | **`03-ui/standards/JsonEditingStandards.md`** |
| Flexbox layout | `03-ui/layout/TailwindFlexBoxRemainSpace.md` |
| Menu implementation | `03-ui/layout/ApplicationConfigurationMenu.md` |
| Caching solution | `04-architecture/ReactCshtmlCachingSolution.md` |

---

## Prompt Files by Category

### 00. Scenario Templates

#### [ScenarioTemplates.md](./00-scenarios/ScenarioTemplates.md) ⭐ **SCENARIO TEMPLATES**
- **Purpose**: Prompt templates for common development scenarios
- **Use When**: You need a specific task template to use with MainPrompt.md
- **Key Topics**:
  - Scenario 1: Complete feature migration
  - Scenario 2: Analyze incomplete implementation
  - Scenario 3: Add feature to existing page
  - Scenario 4: Optimize draft page UI
  - Scenario 5: Fix bugs and errors
  - Scenario 6: Refactor component
  - Scenario 7: Add new component
  - Custom scenario template

---

### 01. Reference Guides

#### [AngularJsReferenceGuide.md](./01-reference-guides/AngularJsReferenceGuide.md)
- **Purpose**: Reference guide for locating AngularJS code, business logic, DTOs, and server-side projects
- **Use When**: You need to find the original AngularJS implementation, understand the codebase structure, or locate DTO definitions
- **Key Topics**:
  - Solution root path configuration
  - Server-side project structure
  - Business logic class locations
  - DTO definitions and locations
  - Architecture differences (server-side rendering vs SPA)

---

### 02. Migration Guides

#### [ConverterAngularJsPage.md](./02-migration/ConverterAngularJsPage.md)
- **Purpose**: Step-by-step workflow for converting AngularJS features to React
- **Use When**: Converting a new feature from AngularJS to React
- **Key Topics**:
  - 9-step conversion workflow (from input collection to finalization)
  - UI conversion rules (Wijmo, Font Awesome, Tailwind, etc.)
  - Component patterns and best practices
  - Theme and styling alignment

#### [ConverterAngularServiceToReact.md](./02-migration/ConverterAngularServiceToReact.md) ⭐ **SERVICE CONVERSION GUIDE**
- **Purpose**: Comprehensive rules for converting AngularJS services to React/TypeScript services
- **Use When**: Converting API service methods from Angular to React, or adding missing/modified APIs
- **Key Topics**:
  - Method name preservation (keep PascalCase from Angular)
  - Query parameters in URL (always include, use empty string for null)
  - HTTP methods (GET, POST, PUT, DELETE)
  - Error handling patterns
  - Parameter types and return types
  - Conversion checklist and examples

#### [DevelopmentPrompt.md](./02-migration/DevelopmentPrompt.md)
- **Purpose**: General development guidelines for React migration
- **Use When**: Starting new development work or debugging issues
- **Key Topics**:
  - Reference implementation rules
  - Migration strategy and workflow
  - Common patterns (data loading, state management)
  - Best practices and debugging tips

---

### 03. UI Standards & Guidelines

#### [UIMainPrompt.md](./03-ui/UIMainPrompt.md) ⭐ **MAIN UI INDEX**
- **Purpose**: Main entry point for all UI-related standards, guidelines, and patterns
- **Use When**: Working on any UI-related task (pages, components, styling, layouts)
- **Key Topics**:
  - Complete UI standards overview
  - Quick reference to all UI documents
  - Usage guidelines and best practices
  - Component checklist

**Sub-directories**:

**Layout Patterns** (`layout/`):
- [TailwindFlexBoxRemainSpace.md](./03-ui/layout/TailwindFlexBoxRemainSpace.md) - Flexbox remaining space patterns
- [ApplicationConfigurationMenu.md](./03-ui/layout/ApplicationConfigurationMenu.md) - Menu implementation patterns

**UI Standards** (`standards/`):
- [PageLayoutStandards.md](./03-ui/standards/PageLayoutStandards.md) - Page layout patterns and structure
- [ButtonStandards.md](./03-ui/standards/ButtonStandards.md) - Button styles and patterns
- [FormStandards.md](./03-ui/standards/FormStandards.md) - Form field styles and layouts
- [ThemeUsageStandards.md](./03-ui/standards/ThemeUsageStandards.md) - Dynamic theme system usage
- [ReferenceComponents.md](./03-ui/standards/ReferenceComponents.md) - Reference components and examples

---

### 04. Architecture & Solutions

#### [ReactCshtmlCachingSolution.md](./04-architecture/ReactCshtmlCachingSolution.md)
- **Purpose**: Solution for handling CSHTML server-side caching in React SPA
- **Use When**: Understanding why React doesn't need server-side caching, or implementing transaction form loading
- **Key Topics**:
  - Problem statement (CSHTML rendering vs API calls)
  - Solution approach (no caching needed for React)
  - DynamicLayoutController implementation
  - React service and component usage

---


## Usage Guidelines

### For AI Assistants

**When given a task with MainPrompt.md as context:**

1. **Understand Context**: This file provides project structure, standards, and guidelines
2. **Read Relevant Prompts**: Based on task, read specific prompt files referenced here
3. **Follow Standards**: Always follow UI standards, coding patterns, and best practices
4. **Reference AngularJS**: Use `AngularJsReferenceGuide.md` to locate original code when needed
5. **Verify Compliance**: Ensure implementation matches standards and AngularJS functionality

**Common Workflows**:
- **Feature Migration**: Use `ConverterAngularJsPage.md` workflow
- **UI Work**: Reference `03-ui/UIMainPrompt.md` and sub-documents
- **Code Analysis**: Compare with AngularJS using reference guide
- **UI Optimization**: Follow UI standards from `03-ui/standards/`

**Always Verify**:
- ✅ Functionality matches AngularJS (when applicable)
- ✅ UI follows all standards
- ✅ No errors (linter, TypeScript, console)
- ✅ Code quality meets standards

### For Developers

1. **General development**: Read `02-migration/DevelopmentPrompt.md`
2. **UI work**: Start with `03-ui/UIMainPrompt.md` for all UI standards and guidelines
3. **Architecture questions**: Review `04-architecture/` for solutions

---

## Key Principles

**Core Principles for All Development Work**:

1. **Always reference AngularJS implementation**: The original AngularJS codebase is the source of truth for functionality
2. **Preserve business logic**: Don't change how features work, only how they're implemented
3. **Handle missing data gracefully**: Some server-side model properties may not be in API responses
4. **Follow UI patterns**: Use established patterns for layouts, buttons, and components
5. **Test for parity**: Ensure React implementation matches AngularJS behavior
6. **Use theme system**: Never hardcode colors, always use theme tokens (`theme.*`)
7. **Follow standards**: Adhere to all UI standards, coding patterns, and best practices
8. **Complete implementation**: Implement all related pages, nested components, and linked pages
9. **Use `appHelper.debugLog()` for debug logging**: Always use `appHelper.debugLog()` instead of `console.log()` for debug purposes. This allows global control of debug output. See `Prompt/05-Specific-Component-Rule/DEBUG_LOG_USAGE.md` for details.

**Quality Standards**:
- ✅ No TypeScript errors
- ✅ No linter errors
- ✅ No console errors/warnings
- ✅ Clean imports (no `.tsx` suffix)
- ✅ Proper error handling
- ✅ UI matches standards

---

## Related Resources

- **React App Path**: `C:\DevApp\app-react`
- **AngularJS Solution Path**: `C:\DevApp\App\PlmApplication`
- **Solution Root**: `C:\DevApp\App\`
- **Solution File**: `C:\DevApp\App\PLMS Solution.sln`

---

## File Naming Convention

All prompt files follow this naming convention:
- **Descriptive names**: Clear, descriptive file names that indicate content
- **Markdown format**: All files use `.md` extension
- **Category prefixes**: Numbered prefixes (01-, 02-, etc.) for easy sorting
- **Consistent structure**: Each file has clear sections and headings

---

## Maintenance

When adding new prompts:

1. **Categorize correctly**: Place files in the appropriate category directory
2. **Update MainPrompt.md**: Add entries to the relevant sections
3. **Follow naming convention**: Use descriptive names with proper prefixes
4. **Cross-reference**: Link related prompts where appropriate
5. **Keep organized**: Maintain the directory structure and avoid duplication

---

---

*Last Updated: 2026-01-16*
