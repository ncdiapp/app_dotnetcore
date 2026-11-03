# Agent Chat 循环日志计划

**Date:** 2026-11-03  
**Scope:** Generic / Agent Chat（`SharedAgentEngine.RunAsync`）  
**Logger:** NLog → `{web}/logs/appai-yyyy-MM-dd.log`（全站同一天一份文件，**不是**每 Chat / 每 Agent 一份）

---

## 1. 目的

DEBUG 时 `fullResponse` 只有最终对用户说的那句话。工具调用、LLM 入参、Result 面板都不在那里。

本计划在 **SK agentic loop 的闸门** 打结构化文件日志，使一轮对话可以按时间读出：

1. LLM 看到了什么（message / prompt 规模 / history）  
2. LLM 下一步是调工具还是终答  
3. 工具名、输入、输出（截断）  
4. 工具之后再次问 LLM，直到 FINAL

过滤：`findstr /C:"[AgentLoop]" logs\appai-日期.log`  
再按 `session=` 或 `run=` 收窄到一次 Chat / 一次 Run。

---

## 2. 记哪里（文件），不记哪里（表）

| 通道 | 是否使用 |
|---|---|
| NLog 文件 `logs/appai-${shortdate}.log`，`Info` | **是**（已有配置） |
| UI `OnStep`（Reasoning loop） | 给用户看，不替代运维日志 |
| `AppLogTrack` / `AppMcpAuditLog` | **否**（体积、PII、不是 Agent 循环） |

一行一条，字段内换行压成空格。单字段上限 **4000** 字符。表格数据只记 `dataset_name` / 行数线索（出现在 tool result 截断里），不把 5 万行打进文件。

---

## 3. 一行格式

```
[AgentLoop] run={runId} skill={skillKey} session={chatSessionKey} seq={n} {KIND} {detail}
```

| 字段 | 含义 |
|---|---|
| `run` | 本次 `AgentToolContext.SessionId`（一次用户发送 = 一次 run） |
| `skill` | Agent / SkillKey |
| `session` | ChatSessionKey |
| `seq` | SK `RequestSequenceIndex`：0 = 本 run 第一次 LLM；工具后再问 LLM 则 +1。无工具的终答保持当前 seq |
| `KIND` | 见下表 |

| KIND | 何时 | 作用 |
|---|---|---|
| `RUN_START` | `RunAsync` 开始 | 关联 run / skill / session |
| `LLM_IN` | 第一次 `InvokeStreamingAsync` 之前 | 本轮用户句、prompt 规模、history 条数、工具清单 |
| `LLM_HTTP` | Gemini `SendAsync`（若 provider=Gemini） | 真正离开进程的 HTTP JSON 摘要（截断） |
| `LLM_OUT` | 模型决定下一步 | `next=TOOL name=...` 或 `next=FINAL text=...` |
| `TOOL_IN` | 工具执行前 | 工具名 + args（截断） |
| `TOOL_OUT` | 工具执行后 | ok / ms / result 截断；`next=LLM` |
| `RUN_DONE` / `RUN_STOP` / `RUN_ERROR` | 结束 | 总耗时 + 终答截断 |

**下一步怎么读**

- `LLM_OUT next=TOOL` → 下一条应是 `TOOL_IN`  
- `TOOL_OUT next=LLM` → 下一条应是 `LLM_HTTP` 或下一 `seq` 的 `LLM_OUT`  
- `LLM_OUT next=FINAL` → 循环结束，聊天泡泡那句话  

---

## 4. 代码埋点

### 4.1 辅助类（新建）

**文件:** `APP.BL/AIAgent/GenericAgent/AgentLoopLog.cs`  

- `AsyncLocal` 保存 `run / skill / session`，使 Gemini HTTP handler 能带上同一 run  
- `Begin` / `Write` / `OneLine`（截断 + 去换行）

### 4.2 `RUN_START` + `LLM_IN` + `LLM_OUT FINAL` + `RUN_*`

**文件:** `APP.BL/AIAgent/GenericAgent/SharedAgentEngine.cs`  
**方法:** `RunAsync`

| 位置 | 日志 | 目的 |
|---|---|---|
| 解析完 `resolvedChatKey`、建好 `context.SessionId` 后 | `Begin` + `RUN_START` | 绑定本次 run |
| `history.AddUserMessage` 之后、`InvokeStreamingAsync` 之前 | `LLM_IN` | 第一次发给模型的用户句 / promptChars / hist / tools |
| `InvokeStreamingAsync` 正常结束后 | `LLM_OUT next=FINAL` | 终答（即以前只在 `fullResponse` 里看到的那句） |
| 现有 `[Agent] DONE/STOPPED` 旁 | `RUN_DONE` / `RUN_STOP` / `RUN_ERROR` | 收尾 |
| `finally` | 清除 `AsyncLocal` | 避免串 run |

`LLM_IN` 不把完整 system prompt 打进文件，只打 `promptChars=` 和开头约 400 字。

### 4.3 `LLM_OUT next=TOOL`（每一轮工具前）

**文件:** 同文件内 `GenericAgentPruneFilter.OnAutoFunctionInvocationAsync`  
（SK 在自动 function calling 里、**执行该轮工具之前**调用）

- `seq = ctx.RequestSequenceIndex`  
- `LLM_OUT next=TOOL name={即将执行的函数}`  
- 若 history 最后几条可摘要，附 `histLast=`（截断）

同一 `seq` 若 LLM 一次返回多个 tool，只在该 seq **第一次** 打 `LLM_OUT`，避免刷屏。

### 4.4 `TOOL_IN` / `TOOL_OUT`

**文件:** `APP.BL/AIAgent/GenericAgent/AgentStepFilter.cs`  
**方法:** `OnFunctionInvocationAsync`

| 位置 | 日志 |
|---|---|
| `await next(ctx)` 前 | `TOOL_IN name=... args=...`（`FormatToolCallDetails` 截断） |
| `finally` 里拿到 `resultText` 后 | `TOOL_OUT name=... ms=... ok=... result=... next=LLM` |

保留原有 `[Agent] →` / `←` 短日志。

### 4.5 `LLM_HTTP`（仅 Gemini）

**文件:** `SharedAgentEngine.GeminiRoleFixHandler.SendAsync`

在 patch 之后、`base.SendAsync` 之前：`LLM_HTTP bytes=... body=...`（4000 字）。  
失败时已有 `Gemini {status}:` Error，不重复打全文。

OpenAI / Anthropic 没有这层：第一轮靠 `LLM_IN`，后续靠 `PruneFilter` 的 `LLM_OUT` + history 摘要。

---

## 5. 输出例子

用户说：`show me product forward timeline`  
Agent 调 MCP 查数，再 `render_interactive`，最后总结一句。

```
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=0 RUN_START
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=0 LLM_IN user="show me product forward timeline" hist=4 promptChars=8120 tools=ask_user,render_interactive,render_none_interactive,PLM_Products_GetLatestStyles promptHead="You are..."
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=0 LLM_HTTP bytes=21400 body={"contents":[{"role":"user",...}]}
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=0 LLM_OUT next=TOOL name=PLM_Products_GetLatestStyles
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=0 TOOL_IN name=PLM_Products_GetLatestStyles args={"## Input":{}}
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=0 TOOL_OUT name=PLM_Products_GetLatestStyles ms=210 ok=true result={"dataset_cached":true,"dataset_name":"PLM_Products_GetLatestStyles_7","rows":100} next=LLM
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=1 LLM_HTTP bytes=9000 body={...tool result envelope...}
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=1 LLM_OUT next=TOOL name=render_interactive
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=1 TOOL_IN name=render_interactive args={"layout":"table","dataset_name":"PLM_Products_GetLatestStyles_7"}
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=1 TOOL_OUT name=render_interactive ms=80 ok=true result={"ok":true,"layout":"table","rowCount":100} next=LLM
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=2 LLM_HTTP bytes=4000 body={...}
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=2 LLM_OUT next=FINAL text="Here is the latest product timeline information retrieved from the PLM system..."
[AgentLoop] run=a1b2c3 skill=platform-ui-render session=Skill:42 seq=2 RUN_DONE ms=4200
```

对应 UI：左边 Reasoning = `TOOL_*`；右边 Result = `render_*` 的 `TOOL_OUT`；聊天总结 = `LLM_OUT next=FINAL`。

---

## 6. 怎么看

1. 再跑一轮 Agent Chat。  
2. 打开 `AppAI.Web` 输出目录下 `logs/appai-当天.log`（或 VS 启动项目的 `bin/.../logs/`）。  
3. `findstr /C:"[AgentLoop]" /C:"run=你的run" appai-当天.log`

不要在 `fullResponse` 上找工具 I/O。

---

## 7. 明确不记

- 完整 cache 行集、完整 TemplateContent HTML  
- 无截断的 Gemini 成功响应流  
- 密钥 / connection string  
- 每个 Plugin 内部再打一份（避免与 `AgentStepFilter` 重复）
