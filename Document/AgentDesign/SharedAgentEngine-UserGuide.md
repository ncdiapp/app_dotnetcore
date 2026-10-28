# Generic AI Agent Platform — User & Admin Guide

**Product:** AppAI Generic Agent Platform  
**Audience:** SaaS platform administrators, power users  
**Last updated:** 2026-09-02  

---

## What is the Generic AI Agent Platform?

The Generic AI Agent Platform is a built-in AI assistant layer inside AppAI. It lets you give your platform one or more AI assistants — called **agent personas** — that can:

- Hold a conversation with users in natural language
- Call tools on behalf of the user (run SQL queries, read from external APIs, call ERP functions)
- Connect to external systems through MCP (Model Context Protocol) servers
- Propose plans and schema changes that require your approval before anything is saved
- Stream replies word-by-word so users see results in real time

You configure everything — the agent's personality, what tools it can use, and which AI provider powers it — through the admin UI. Once set up, users interact with agents through a chat panel. No code is needed to create a new agent persona or add SQL query tools.

The platform ships with four built-in agents:

| Agent | What it does |
|---|---|
| **App Builder Agent** | Designs and builds full applications: database tables, forms, search screens, menus |
| **App Report Agent** | Finds and displays data from existing search screens in the platform |
| **DB Genie** | Translates natural-language questions into SQL queries and runs them against your database |
| **Data Integration Agent** | Imports data and configuration packs into the platform; runs safe SQL via an approval gate |

---

## Setting Up Your LLM Provider

Before agents can reply to any message, you must configure an AI provider and supply an API key. The platform supports three providers: **Gemini** (Google), **OpenAI**, and **Anthropic**.

### Step-by-step

1. In the AppAI menu, go to **Administration** → **Application Settings** (or wherever tenant settings are managed in your deployment).

2. Find these settings — they all start with `AIConfig`:

   | Setting key | What to enter |
   |---|---|
   | `AIConfigProvider` | The name of your active provider. Enter exactly one of: `Gemini`, `OpenAI`, or `Anthropic` (case-insensitive) |
   | `AIConfigGeminiApiKey` | Your Gemini API key (leave blank if not using Gemini) |
   | `AIConfigOpenAIApiKey` | Your OpenAI API key (leave blank if not using OpenAI) |
   | `AIConfigAnthropicApiKey` | Your Anthropic API key (leave blank if not using Anthropic) |
   | `AIConfigGeminiModel` | Gemini model name — default is `gemini-2.0-flash` |
   | `AIConfigOpenAIModel` | OpenAI model name — default is `gpt-4o` |
   | `AIConfigAnthropicModel` | Anthropic model name — default is `claude-3-5-sonnet-20241022` |

3. Set `AIConfigProvider` to your chosen provider name.

4. Enter the API key for that provider in the matching key field.

5. Save the settings.

**Example:** If you want to use Gemini, set `AIConfigProvider` = `Gemini`, fill in `AIConfigGeminiApiKey` with your key, and leave the OpenAI and Anthropic key fields blank.

**Tip:** You can store API keys for all three providers at the same time. Only the key matching the active `AIConfigProvider` is actually used. This makes it easy to switch providers later — just change `AIConfigProvider` and the platform will start using that provider's stored key immediately.

---

## Managing Agent Personas

Agent personas are listed and edited in **Agent Skill Set** management. To find it, look for the AI / Agent section in your admin menu.

The screen has three tabs: **Skill Sets**, **Tool Register**, and **MCP Servers**.

### Viewing built-in agents

Open the Skill Sets tab. The left panel shows a list of all active agent personas. The four built-in agents (App Builder, App Report, DB Genie, Data Integration) appear here.

Click any row to open its details in the right panel. You can read the system prompt, see which capability flags are enabled, and review the context thresholds.

Built-in agents can be edited — you can adjust their system prompts and thresholds — but be cautious: their system prompts are carefully crafted to work with their registered tools. Large changes may break expected behaviour.

### Creating a new agent persona

1. In the Skill Sets tab, click **+ New** (top-left of the list panel).
2. The right panel shows a blank editor form.
3. Fill in:
   - **Skill Key** (required): a short unique ID, no spaces, e.g. `my-assistant`. This cannot be changed after saving.
   - **Display Name**: the friendly name shown in the UI, e.g. `My Purchase Assistant`
   - **Description**: one or two sentences describing what this agent does
4. Check the **Capabilities** you want (see next section for what each one does).
5. Write the **System Prompt** — this is the full instruction text the AI receives before every conversation. It defines the agent's role, rules, and response format. (See tips below.)
6. Adjust the **context thresholds** if needed (defaults are fine for most agents).
7. Click **Save**.

The agent is immediately available through the API and test UI. No server restart is needed.

### Writing a system prompt

The system prompt is what makes the agent behave the way you want. Some tips:

- State the agent's role clearly in the first paragraph: "You are a Purchase Order assistant for Acme Corp. Your job is to..."
- List any rules the agent must follow: "Always ask for a vendor name before querying orders."
- If the agent will return data tables, include this block so the frontend renders them as grids:
  ```
  When a tool returns a list of rows, format the result as:
  ```mcp-ui
  { "ui_hint": "FlexGrid", "data": [...], "columns": [...], "meta": { "title": "..." } }
  ```
  The frontend will render this as an interactive grid with export.
  ```
- Keep the prompt focused. Agents with very long, general-purpose prompts tend to be less reliable than agents with narrow, specific instructions.

### Editing an existing agent

Click the row in the list. Make changes in the right panel. Click **Save**. Changes take effect on the next conversation — sessions that are already running continue with the old prompt.

### Deleting an agent

Select the row, click **Delete**, and confirm. This permanently removes the persona from the database. If you want to temporarily disable an agent without deleting it, uncheck the **Active** checkbox instead and save.

---

## Capability Flags Explained

When you create or edit an agent, the **Capabilities** section shows a list of checkboxes. Each checkbox switches on a specific behaviour. Here is what each one does in plain language:

| Checkbox | What it does for the user |
|---|---|
| **StreamTokens** | The AI's reply appears word-by-word as it is generated, rather than all at once at the end. Strongly recommended for any interactive agent. |
| **MultiTurn** | The agent remembers what was said earlier in the same chat session. Without this, every message is treated as a fresh conversation with no prior context. |
| **PlanGate** | Before the agent takes a significant action (like creating tables or an application), it shows you a summary of its plan and waits for you to click Approve or Reject. Nothing happens until you confirm. |
| **SchemaGate** | Before the agent creates or modifies database tables, it shows you the proposed schema for review. You must approve it before any DDL runs. Used together with PlanGate on the App Builder agent. |
| **InjectMemory** | The agent searches a memory store of past conversations for relevant context and prepends it to its instructions. Useful for agents that help build applications over multiple sessions. |
| **InjectSchema** | The agent automatically receives a summary of your database tables and columns before each reply. Used by DB Genie so it can write accurate SQL without having to ask about the schema. |
| **ExternalBackend** | The agent forwards the request entirely to an external system (such as the Data Integration cloud backend) instead of running its own AI loop. Only used for the Data Integration agent. |

**Recommended defaults for a new general-purpose agent:** check StreamTokens and MultiTurn. Add PlanGate if the agent will make changes to the platform.

---

## Registering Tools

Tools are what an agent can do beyond just talking. Each tool is a named function the AI can call during a conversation. Tools are registered in the **Tool Register** tab.

### What the different tool types mean

| Tool type | What it does | Who sets it up |
|---|---|---|
| **BuiltIn** | Calls a C# method already compiled into the platform. Used for the built-in app builder, schema, and report tools. | Platform developer |
| **SqlQuery** | Runs a specific SQL query against your database, with the AI supplying parameter values. No code needed — you write the SQL here in the admin UI. | Admin (no code) |
| **HttpRest** | Calls an external REST API. You configure the URL and the AI fills in the argument placeholders. No code needed. | Admin (no code) |
| **DynamicCSharp** | Runs a small C# script in a sandboxed environment. Safe — it cannot access your files, network, or reflection. | Admin or AI generates the script |
| **ExternalDll** | Loads a compiled DLL you drop into the server's plugin folder, and calls a method in it. | Developer (write DLL) |
| **PowerShell** | Runs a PowerShell script file. Super-admin use only. | Developer |

### Adding a SqlQuery tool (no code required)

SqlQuery tools are the most common tool type for custom agents. Here is how to add one:

1. Go to the **Tool Register** tab.
2. Click **+ New Tool**.
3. Select the **Skill Key** (agent) this tool belongs to.
4. Enter a **Tool Name** — this is the function name the AI sees, e.g. `get_open_purchase_orders`. Use underscores, no spaces.
5. Enter a **Tool Description** — this tells the AI when and why to call this tool. Be specific: "Returns open purchase orders for the given vendor. Call when the user asks about pending orders."
6. In the **Parameter Schema** field, describe the parameters the tool accepts using JSON Schema format:
   ```json
   {
     "properties": {
       "vendorName": { "description": "The vendor name to filter by", "type": "string" }
     },
     "required": ["vendorName"]
   }
   ```
7. Set **Tool Type** to `SqlQuery`.
8. In **Tool Config**, enter the SQL and return format:
   ```json
   {
     "SqlBody": "SELECT TOP 50 PoId, VendorName, Amount, Status FROM PurchaseOrder WHERE VendorName LIKE '%' + @vendorName + '%' AND Status='Open'",
     "ReturnType": "json"
   }
   ```
   Note: use `@parameterName` in the SQL; the AI will supply the value at call time.
9. Click **Save**.

The tool is immediately available to that agent's next conversation.

---

## Connecting MCP Servers

MCP (Model Context Protocol) is an open standard that lets external systems expose a list of tools to an AI agent automatically. Instead of registering each tool individually, you register the server once, and the agent discovers all its tools when a session starts.

### What you need

A running MCP server accessible from your AppAI server, using the `streamable-http` transport type. The server exposes an HTTP endpoint (e.g. `http://erp-server:5100/mcp`).

### Step-by-step

1. Go to the **MCP Servers** tab.
2. Click **+ New Server**.
3. Fill in:
   - **Skill Key**: which agent this server belongs to
   - **Server Name**: a display name, e.g. `BlueCherry ERP MCP`
   - **Server Type**: select `streamable-http`
   - **Server URL**: the full URL of the MCP server endpoint, e.g. `http://localhost:5100/mcp`
4. Click **Save**.

The next time someone starts a conversation with that agent, the platform will connect to the MCP server, list all its available tools, and make them available to the agent automatically.

**What happens when the MCP server is down?** If the server cannot be reached when a session starts, the platform logs a warning and the agent continues with its other registered tools. The session does not fail. The agent just won't have access to that server's tools for that session.

---

## Running an Agent

### From the Agent Skill Set management screen

1. Select an agent persona from the list.
2. Click the **Run** button (play icon) in the toolbar above the list.
3. A test chat panel opens on the right side of the screen.
4. Type your message in the text box at the bottom and press Enter or click the send button.
5. The agent's reply streams in word-by-word (if StreamTokens is enabled).

### From other parts of the platform

Agents are also accessible from their dedicated pages (App Builder, DB Genie, Data Report, Data Integration) depending on which features are enabled in your tenant.

### Understanding the response

- **Streaming text**: Words appear as they are generated. A blinking cursor shows the AI is still typing.
- **Tool call chips**: When the agent calls a tool, a small indicator appears showing the tool name and whether it succeeded.
- **Data grids**: If the agent returns structured data formatted as `mcp-ui`, the frontend renders it as an interactive sortable grid. You can filter and export the data.
- **Plan Review**: If the agent uses the PlanGate capability and proposes a plan, a **Plan Review** panel appears with an Approve and Reject button. The agent is paused and will not proceed until you respond. Read the plan summary, then click Approve to continue or Reject to stop.

### Resetting a conversation

Click the reset button (circular arrow icon) in the chat input bar to clear all messages and start a fresh session. The agent will have no memory of the previous exchange in that new session.

---

## Built-in Agents Reference

### App Builder Agent

App Builder is designed to create complete business applications inside the AppAI platform: database schema, transaction forms, search screens, entity dropdowns, and navigation menus. Give it a plain English description of a business process and it will propose a plan, wait for your approval, design the database schema (another approval gate), and then build the application step by step.

Best for: creating new data modules (e.g. "Build a purchase order management module with header and line items"), updating existing forms, adding search screens. Not for one-off data queries — use DB Genie for that.

### App Report Agent

App Report helps users find and display data from the platform's existing search screens. Describe what data you want to see and the agent will find the most relevant search screen, apply filters, and run the query. It can also create a new search screen backed by a custom SQL query if no existing screen matches.

Best for: answering questions like "Show me all open sales orders for customer Acme from last month." Not for modifying data or building new application modules.

### DB Genie

DB Genie is a senior-DBA-level SQL assistant. It reads the database schema automatically (InjectSchema flag) and translates natural-language questions into optimized, correct SQL queries. It handles SQL Server, MySQL, and Oracle dialect differences. It lists the matching tables it found, shows the generated query in a SQL code block, and runs it.

Best for: technical users and analysts who want to explore the database without writing SQL themselves, or who want a second opinion on a query they have already written. All queries are SELECT-only by default unless you explicitly ask for data modifications (which the agent will flag clearly).

### Data Integration Agent

Data Integration imports data and platform configuration into AppAI. It can write AppConfigPack JSON files (the platform's portable app configuration format), preview table data, run safe SELECT queries, and propose data modification SQL (INSERT/UPDATE) through an approval gate before anything is executed. It operates in a sandboxed workspace with designated folders for packs, scripts, output, and notes.

Best for: data migration projects, importing platform configuration from one tenant to another, exploring external database tables, and setting up new tenant environments. All destructive SQL (CREATE TABLE, ALTER TABLE, INSERT, UPDATE) goes through an explicit approval step — nothing is applied without your confirmation.

---

## Frequently Asked Questions

**Why is my API key not working?**

First, check that the `AIConfigProvider` setting matches the provider whose key you entered. For example, if `AIConfigProvider` is `Gemini` but you entered the key in `AIConfigOpenAIApiKey`, the platform will try to call Gemini with an empty key. Also verify the key has the necessary permissions for the model you are using.

**How do I switch from Gemini to OpenAI?**

Change the `AIConfigProvider` setting value to `OpenAI` and enter your OpenAI key in `AIConfigOpenAIApiKey`. Save. The next agent session will use OpenAI. Your Gemini key remains stored and can be switched back by changing `AIConfigProvider` again.

**Can different tenants use different providers?**

Yes. LLM provider settings are stored in `AppTenantSetting`, which is per-tenant. Each tenant can independently choose their provider, enter their own API key, and select their model. One tenant using Anthropic does not affect another tenant using Gemini.

**Can I have different models for different agents within the same tenant?**

Not directly through CapabilityFlags — the active model is a per-tenant setting, not per-agent. All agents within a tenant use the same provider and model. If you need different models per agent, contact your platform developer to discuss custom configuration options.

**What happens if I change an agent's system prompt while someone is in a conversation?**

The running session is not affected. System prompts are loaded at session start. The user currently chatting will see the old behaviour until they start a new session (or reset the chat). The next session will use the updated prompt.

**How do I temporarily disable an agent without deleting it?**

Uncheck the **Active** checkbox in the agent editor and click Save. Inactive agents are excluded from the list and from the API's agent loading. To re-enable it, check Active and save again.

**Can I add a tool to a built-in agent?**

Yes. Built-in agents can have additional tools registered in the Tool Register tab. For example, you could add a SqlQuery tool to the DB Genie agent that provides quick access to a specific reporting view. The agent will have all its original built-in tools plus your new ones.

**What is the difference between AppAISkill and an agent persona?**

`AppAISkill` (found in the AI Skills section) is a flat prompt library — a place to store reusable prompt snippets that users can paste or reference manually. It has nothing to do with the agent system. Agent personas (in Agent Skill Set) are full agent configurations: system prompt, tools, MCP servers, and capability settings. They are entirely separate features.

**How many MCP tools can an agent have?**

There is no hard limit. The agent's tool list is built from the `AppAgentToolRegister` rows plus whatever the connected MCP servers expose. However, very large tool lists (hundreds of tools) can cause performance issues with some LLM providers because the tool definitions must be sent with every request. Keep the tool list focused on what the agent actually needs.

**The agent stopped mid-conversation and nothing is happening. What should I do?**

This can happen if a plan/schema gate was proposed and you missed the Approve/Reject prompt, or if the gate timed out (gates auto-reject after 10 minutes of no response). Try resetting the chat (circular arrow button) and starting a new message. If the issue recurs, check the application logs for error messages mentioning the agent's skill key.
