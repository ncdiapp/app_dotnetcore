# Agent Chat UI

Date: 2026-10-19. Replaces `AI-Agent-Chat-Render-Panel.md`. Export engines stay in `AI-Agent-Document-Export-Tools-V2.md`.

The reference is `C:\Dev\MCP\CGS.BlueCherry.MCP.Client`, mainly `ChatWindow.tsx`, `ChatInput.tsx`, `MessageItem.tsx`, `ToolCallBlock.tsx`, `ArtifactCard.tsx`, and `ArtifactPanel.tsx`. Copy the layout and the motion. Colors, type, and borders come from `useTheme()`. Do not copy that project's hardcoded hex values, Inter-only rules, iframe HTML artifacts, or Tailwind CDN.

## Page

The chat column is a header, a transcript, and a composer.

The header is one row. On the left, a small chip holds a chat icon and the conversation title. A new chat reads "New Conversation". On the right, the model and skill picker. After the first message, that picker is locked for this conversation. The header spans the full width, including when the render panel is open.

The transcript and the composer share one centered column, 80% of the chat pane. On a narrow viewport that column is the full width with a small side inset.

Welcome stays the greeting already in `GenericAgentChat`: time of day, first name, skill name, and starter prompts. No spinning logo and no speech-bubble tooltip.

Under the composer, one centered line of 12px muted text: the agent can be wrong about people, places, and facts. Hide that line on a narrow viewport.

## Composer

The box is at least 100px tall, radius 8px, with the input border from the theme. The textarea sits at the top and grows with the text, up to 200px (120px when narrow), then scrolls. Placeholder is `Message {skill}…` in the muted label color. Enter sends. Shift+Enter inserts a line.

Actions sit on the bottom right of the same box: Stop while a turn is running, Send when it is idle. There is no microphone and no attach button in this change.

When the box has focus, the bottom edge becomes a thicker accent line and slowly shimmers, one cycle about 3 seconds. That is the waiting-and-typing frame from the reference (`chatBorderShimmer` on `.chat-input-box`). It runs only while focused. It stops when focus leaves.

## One turn

A user turn is right-aligned, at most 70% of the column (85% when narrow). The bubble uses the theme surface, radius `8px 8px 0 8px`, 16px text, 24px line height. Hover reveals Copy. Edit and retry are not added.

An assistant turn has no outer bubble. Order, top to bottom:

1. Tool header, when this turn called tools.
2. The reply text.
3. Small renders, drawn in place.
4. One chip per large render.
5. Follow-up suggestions, after the turn finishes.

The tool header is one collapsible row, the reference `ToolChain`. While any call in that burst is running, it shows a spinner and "Working…". About 800ms after the burst finishes, it collapses to "N tools used", unless the user has opened or closed it by hand. Expanding it groups calls by library and lists each tool name with success or failure. Input and result are behind that row, in a small monospace block. The header does not stay open for the rest of the conversation.

While text is streaming, one accent caret pulses at the last character. It is inline with that character, not a block on the next line. While a large render is still arriving, the chip's skeleton is the only motion; the caret is hidden. While tools are running and no text is streaming, the tool header is the only motion. Three bouncing dots appear only when the turn is running and none of those three is already showing.

A markdown table that stays in the reply uses a rounded 8px frame, a header row, and a light row hover. It is not a second grid component.

## What stays in the chat

A block stays in the transcript when it is markdown, a KPI row, or a grid, chart, or card list of at most 8 rows.

A `kpi_dashboard` can contain both. Its markdown and KPI blocks, and any grid, chart, or card block of at most 8 rows, stay under the reply. Each larger block becomes a chip. This matches the reference screen: the short summary stays in the conversation, and the full grid is "Open in panel".

An event that is only a grid, chart, or card list, and has more than 8 rows, becomes one chip. Nothing from that event is drawn in full under the message.

Eight is the cut. A result of 8 rows is still readable in the thread. 9 rows opens in the panel.

## Chip and panel

The chip is the reference `ArtifactCard`: at most a short card wide, icon, title, one fact line such as `684 records`, the caption `Open in panel` or `Showing in panel`, and a chevron. The whole chip is the button. While the event is still streaming, the chip is a slow-pulsing rounded frame with three dots and "Building…". It is not a live grid.

The panel is the right side of the split, default 55% of the row, dragged by the user and stored as `agentRenderPanelPct`. It hosts the existing `DataRenderPanel` for the open render. One render is open. The open chip uses the accent border. Close leaves the chips. Maximize covers the chat only after the user clicks maximize. Restore returns the split.

A new large render on the latest turn opens the panel once. It does not steal the panel if the user has opened an older chip or closed the panel. Switching conversations closes the panel. After history reload, chips are back and the panel stays closed.

The first time a large render opens, the app sidebar collapses once for that conversation.

On a narrow viewport the panel is a layer over the chat, with the same close control. The thread under it still shows chips and small renders, not the full grid.

`DataRenderEvent` stays on the message. The chip reads title, `Ui`, and `RowCount`. Actions in the panel still call `handleDataRenderAction`. The panel is disabled while the turn is blocked.

## Left out

Microphone, file attach, edit, retry, token counts, an iframe artifact, a CDN stylesheet, and a speech bubble on the avatar. Document export and the chat cache are unchanged. A chip is not an export.

## Code

`GenericAgentChat.tsx` draws each `dataRenders` entry as a full `DataRenderPanel`. Small blocks stay there. Large blocks become `RenderChip`. `RenderPanel` sits beside the transcript and hosts one `DataRenderPanel`.

The tool steps already stored on the assistant message become the collapsible header. The composer in that file gains the top textarea, the bottom-right Send and Stop, and the focus shimmer.

`DataRenderGridAg`, `DataRenderCard`, the chart components, and `DataRenderKpiRow` stay.

## Checks

- A 684-row grid is a chip. The rows appear in the right panel. A 4-row summary under the same reply stays in the thread.
- A dashboard with four KPIs and a large grid shows the KPIs in the thread and one chip for the grid.
- A focused composer shimmers only its bottom edge, and stops when blurred.
- During a tool call the thread shows "Working…", then collapses to "N tools used". The caret does not also show.
- Closing the panel keeps chips and inline summaries. Reload shows them with the panel closed.
- A row action in the panel still reaches `handleDataRenderAction`.
