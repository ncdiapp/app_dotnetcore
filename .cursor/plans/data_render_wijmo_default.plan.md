# data_render: default Wijmo, keep AG/Recharts fork

## Decisions (locked)

- Engine switch: **code constant only** — `DATA_RENDER_ENGINE = 'wijmo'` (default). Flip to `'aggrid'` to use AG Grid + Recharts. No `metaJson` / tool-param override.
- Wijmo chart Phase 1: **bar | line | area | pie**
- Card: unchanged HTML `DataRenderCard`
- Keep `ag-grid-*` and `recharts` deps installed (unused when engine=wijmo)

## File layout

Under `AppReact/src/components/aiskill/dataRender/`:

| File | Role |
|------|------|
| `engine.ts` | `DataRenderEngine = 'wijmo' \| 'aggrid'`; `DATA_RENDER_ENGINE = 'wijmo'` |
| `DataRenderGridAg.tsx` | rename from current `DataRenderGrid.tsx` (AG Grid) |
| `DataRenderChartRecharts.tsx` | rename from current `DataRenderChart.tsx` |
| `DataRenderGridWijmo.tsx` | **new** FlexGrid + CollectionView |
| `DataRenderChartWijmo.tsx` | **new** FlexChart / FlexPie |
| `DataRenderPanel.tsx` | pick Wijmo vs AG/Recharts by `DATA_RENDER_ENGINE` |
| `adapters.ts` / `types.ts` / card | unchanged contract |

## Grid (Wijmo)

Mirror admin list patterns (`AgentToolRegisterTab.tsx`):

- `useState(() => new CollectionView<any>([]))`, sync `sourceCollection` when `rowData` changes
- Dynamic `FlexGridColumn` from adapted `colDefs` (`header`/`binding`/`width`; hide skipped)
- Spacer column `width="*"`
- `isReadOnly`, `headersVisibility="Column"`, fixed height ~160–360 like AG version
- `selectionChanged` → `onRowSelected(row)` for Actions `selectionJson`
- Format currency/number/date via column `format` where easy (parity with AG formatters)

## Chart (Wijmo)

Mirror `ChartViewLayout.tsx`:

- Imports from `@mescius/wijmo.react.chart` (+ `ChartType` from `@mescius/wijmo.chart`)
- Shared adapter output (`chartConfig.type/xField/yField/groupBy`, `data`, `meta`)
- `type=pie` → `FlexPie` (`binding`=yField, `bindingName`=xField)
- else → `FlexChart` with `bindingX`, `chartType` mapped: bar→Column, line→Line, area→Area; series from yField or groupBy pivots
- Type switcher buttons for `allowedTypes` (include `pie` when listed; default allowedTypes stay `['bar','line','area']` unless `chartConfigJson` adds `pie`)
- Empty state via `theme.label`

## Panel wiring

```ts
import { DATA_RENDER_ENGINE } from './engine';
// grid:
DATA_RENDER_ENGINE === 'wijmo' ? DataRenderGridWijmo : DataRenderGridAg
// chart:
DATA_RENDER_ENGINE === 'wijmo' ? DataRenderChartWijmo : DataRenderChartRecharts
```

## Out of scope

- Backend / migration / tool schema changes
- Removing AG Grid or Recharts packages
- Runtime per-call engine override
- Card redesign

## Todos

1. Rename DataRenderGrid/Chart to *Ag/*Recharts; add engine.ts constant default wijmo
2. Add DataRenderGridWijmo (CollectionView + FlexGrid + selection)
3. Add DataRenderChartWijmo (FlexChart bar/line/area + FlexPie)
4. DataRenderPanel selects implementation by DATA_RENDER_ENGINE
