import React, { useMemo } from "react";
import { FlexChart, FlexChartSeries, FlexPie } from "@mescius/wijmo.react.chart";
import { TreeMap } from "@mescius/wijmo.react.chart.hierarchical";
import { ChartType, LabelPosition } from "@mescius/wijmo.chart";
import "@mescius/wijmo.styles/wijmo.css";
import { GridViewLayout } from "./GridViewLayout";
import { useTheme } from "../../../redux/hooks/useTheme";

/** APP.Components.Dto.EmAppControlType.Numeric */
const EmAppControlTypeNumeric = 20;

/** APP.Components.Dto.EmAppChartViewType — map to Wijmo ChartType (Angular ChartViewLayout uses enum names compatible with wijmo). */
const EmAppChartViewType = {
  Area: 1,
  Bar: 2,
  Bubble: 3,
  Candlestick: 4,
  Column: 5,
  HighLowOpenClose: 6,
  Line: 7,
  LineSymbols: 8,
  Scatter: 9,
  Spline: 10,
  SplineArea: 11,
  SplineSymbols: 12,
  Pie: 21,
  Donut: 22,
  TreeMap: 23,
  MultipleTypeChart: 100,
} as const;

function mapEmAppChartTypeToWijmo(em: number | null | undefined): ChartType {
  switch (em) {
    case EmAppChartViewType.Area:
      return ChartType.Area;
    case EmAppChartViewType.Bar:
      return ChartType.Bar;
    case EmAppChartViewType.Bubble:
      return ChartType.Bubble;
    case EmAppChartViewType.Candlestick:
      return ChartType.Candlestick;
    case EmAppChartViewType.Column:
      return ChartType.Column;
    case EmAppChartViewType.HighLowOpenClose:
      return ChartType.HighLowOpenClose;
    case EmAppChartViewType.Line:
      return ChartType.Line;
    case EmAppChartViewType.LineSymbols:
      return ChartType.LineSymbols;
    case EmAppChartViewType.Scatter:
      return ChartType.Scatter;
    case EmAppChartViewType.Spline:
      return ChartType.Spline;
    case EmAppChartViewType.SplineArea:
      return ChartType.SplineArea;
    case EmAppChartViewType.SplineSymbols:
      return ChartType.SplineSymbols;
    default:
      return ChartType.Column;
  }
}

function buildChartDataItem(resultDto: any, isTreeMap: boolean, numericBindings: Set<string>): any {
  const base = { ...(resultDto?.DictViewColumnIDKeyValue ?? {}) };
  if (numericBindings.size > 0) {
    numericBindings.forEach((k) => {
      const raw = base?.[k];
      if (raw == null || raw === '') return;
      if (typeof raw === 'number') return;
      const n = Number(String(raw).replace(/,/g, '').trim());
      if (Number.isFinite(n) && !Number.isNaN(n)) {
        base[k] = n;
      }
    });
  }
  if (isTreeMap && Array.isArray(resultDto?.Children) && resultDto.Children.length > 0) {
    base.Children = resultDto.Children.map((c: any) => buildChartDataItem(c, true, numericBindings));
  }
  return base;
}

interface ChartViewLayoutProps {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  onSelectionChanged?: (selectedItems: any[]) => void;
}

export const ChartViewLayout: React.FC<ChartViewLayoutProps> = ({
  viewDto,
  viewDataList,
  dataModel,
  onExecuteSearch,
  onSelectionChanged,
}) => {
  const { theme } = useTheme();

  const chartTypeNum = viewDto?.ChartType != null ? Number(viewDto.ChartType) : EmAppChartViewType.Bar;

  const { mapToX, mapToYColumns } = useMemo(() => {
    // Cluster child views may send mapping data in different shapes than migrated "reference views".
    const cols = Array.isArray(viewDto?.Columns)
      ? viewDto.Columns
      : Array.isArray(viewDto?.AppSearchViewFieldList)
        ? viewDto.AppSearchViewFieldList
        : [];

    const x = cols.find((c: any) => c?.IsMapToChartX === true || c?.IsMapToChartX === 1) ?? null;
    const y = cols
      .filter((c: any) => c?.IsMapToChartY === true || c?.IsMapToChartY === 1)
      .sort((a: any, b: any) => (Number(a?.ChartYmappingOrder) || 0) - (Number(b?.ChartYmappingOrder) || 0));
    return { mapToX: x, mapToYColumns: y };
  }, [viewDto?.Columns, viewDto?.AppSearchViewFieldList]);

  const chartData = useMemo(() => {
    const rows = Array.isArray(viewDataList) ? viewDataList : [];
    const isTreeMap = chartTypeNum === EmAppChartViewType.TreeMap;
    const numericBindings = new Set<string>(
      mapToYColumns
        .map((c: any) => String(c?.Id ?? ''))
        .filter((x: string) => x.length > 0)
    );
    return rows.map((r: any) => buildChartDataItem(r, isTreeMap, numericBindings));
  }, [viewDataList, chartTypeNum, mapToYColumns]);

  const flexChartTypeIds = new Set<number>([
    EmAppChartViewType.Area,
    EmAppChartViewType.Bar,
    EmAppChartViewType.Bubble,
    EmAppChartViewType.Candlestick,
    EmAppChartViewType.Column,
    EmAppChartViewType.HighLowOpenClose,
    EmAppChartViewType.Line,
    EmAppChartViewType.LineSymbols,
    EmAppChartViewType.Scatter,
    EmAppChartViewType.Spline,
    EmAppChartViewType.SplineArea,
    EmAppChartViewType.SplineSymbols,
    EmAppChartViewType.MultipleTypeChart,
  ]);

  if (!mapToX || mapToYColumns.length === 0) {
    return (
      <div className={`w-full h-full flex items-center justify-center px-4 text-sm ${theme.label}`}>
        Chart view needs one field marked &quot;Is Map To Chart Label&quot; (X) and at least one &quot;Is Map To Chart Value&quot; (Y).
      </div>
    );
  }

  const bindingX = String(mapToX.Id);
  const wijmoMainChartType =
    chartTypeNum === EmAppChartViewType.MultipleTypeChart
      ? ChartType.Column
      : mapEmAppChartTypeToWijmo(chartTypeNum);

  if (chartTypeNum === EmAppChartViewType.TreeMap) {
    const yCol = mapToYColumns[0];
    if (!yCol?.Id) {
      return (
        <div className={`w-full h-full flex items-center justify-center px-4 text-sm ${theme.label}`}>
          TreeMap requires a chart value (Y) column.
        </div>
      );
    }
    return (
      <div className="w-full h-full min-h-[200px] p-1" title={viewDto?.Display}>
        <TreeMap
          className="w-full h-full"
          style={{ width: "100%", height: "100%" }}
          itemsSource={chartData}
          binding={String(yCol.Id)}
          bindingName={bindingX}
          childItemsPath="Children"
          maxDepth={3}
          dataLabel={{ position: LabelPosition.Center, content: "{name}: {value}" }}
        />
      </div>
    );
  }

  if (chartTypeNum === EmAppChartViewType.Pie || chartTypeNum === EmAppChartViewType.Donut) {
    const y0 = mapToYColumns[0];
    if (!y0?.Id) {
      return (
        <div className={`w-full h-full flex items-center justify-center px-4 text-sm ${theme.label}`}>
          Pie/Donut requires a chart value column.
        </div>
      );
    }
    const innerRadius = chartTypeNum === EmAppChartViewType.Donut ? 0.6 : 0;
    return (
      <div className="w-full h-full min-h-[200px]" title={viewDto?.Display}>
        <FlexPie
          className="w-full h-full"
          style={{ width: "100%", height: "calc(100% - 4px)", border: "none" }}
          itemsSource={chartData}
          binding={String(y0.Id)}
          bindingName={bindingX}
          innerRadius={innerRadius}
        />
      </div>
    );
  }

  if (flexChartTypeIds.has(chartTypeNum)) {
    const numericSeries = mapToYColumns.filter(
      (c: any) => Number(c.ControlType) === EmAppControlTypeNumeric
    );
    if (numericSeries.length === 0) {
      return (
        <div className={`w-full h-full flex items-center justify-center px-4 text-sm ${theme.label}`}>
          No numeric chart value columns (ControlType Numeric) are mapped. Add at least one numeric field as chart value (Y).
        </div>
      );
    }
    return (
      <div className="w-full h-full min-h-[200px]" title={viewDto?.Display}>
        <FlexChart
          className="w-full h-full"
          style={{ width: "100%", height: "calc(100% - 4px)", border: "none" }}
          itemsSource={chartData}
          chartType={wijmoMainChartType}
          bindingX={bindingX}
        >
          {numericSeries.map((field: any) => {
            const sid = String(field.Id);
            const name = field.Name || field.Display || sid;
            if (chartTypeNum === EmAppChartViewType.MultipleTypeChart && field.TreeLevel != null) {
              const st = mapEmAppChartTypeToWijmo(Number(field.TreeLevel));
              return <FlexChartSeries key={sid} binding={sid} name={name} chartType={st} />;
            }
            return <FlexChartSeries key={sid} binding={sid} name={name} />;
          })}
        </FlexChart>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col">
      <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>
        Chart type {chartTypeNum} is not supported in React yet. Showing grid.
      </div>
      <div className="w-full h-1 flex-auto overflow-hidden">
        <GridViewLayout
          viewDto={viewDto}
          viewDataList={viewDataList}
          dataModel={dataModel}
          onExecuteSearch={onExecuteSearch}
          onSelectionChanged={onSelectionChanged}
        />
      </div>
    </div>
  );
};
