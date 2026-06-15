import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ComboBox } from '@mescius/wijmo.react.input';
import { PivotGrid, PivotPanel } from '@mescius/wijmo.react.olap';
import * as wjcInput from '@mescius/wijmo.input';
import { PivotGrid as WjPivotGrid, ShowTotals } from '@mescius/wijmo.olap';
import { FlexGridXlsxConverter } from '@mescius/wijmo.grid.xlsx';
import '@mescius/wijmo.styles/wijmo.css';
import { GridViewLayout } from './GridViewLayout';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { buildSearchPivotEngine } from './pivotSearchViewHelper';
import appHelper from '../../../helper/appHelper';

interface PivotViewLayoutProps {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
}

const SHOW_TOTALS_OPTIONS = [
  { name: 'None', value: ShowTotals.None },
  { name: 'Grand totals', value: ShowTotals.GrandTotals },
  { name: 'Subtotals', value: ShowTotals.Subtotals },
];

export const PivotViewLayout: React.FC<PivotViewLayoutProps> = ({
  viewDto,
  viewDataList,
  dataModel,
  onExecuteSearch,
}) => {
  const { theme } = useTheme();
  const emAppWijmoPivotAggregationType = useEnumValues('EmAppWijmoPivotAggregationType');

  const pivotAggregateFunctionList = useMemo(() => {
    if (!emAppWijmoPivotAggregationType) return [];
    return Object.entries(emAppWijmoPivotAggregationType).map(([k, id]) => ({
      Id: id as number,
      Display: k,
    }));
  }, [emAppWijmoPivotAggregationType]);

  const pivotEngine = useMemo(
    () => buildSearchPivotEngine(viewDataList, viewDto, pivotAggregateFunctionList),
    [viewDataList, viewDto, pivotAggregateFunctionList]
  );

  const pivotGridInstanceRef = useRef<WjPivotGrid | null>(null);
  const pivotUpdatedViewUnsubRef = useRef<(() => void) | null>(null);
  const [isPivotPanelCollapsed, setIsPivotPanelCollapsed] = useState(false);
  const [rowTotals, setRowTotals] = useState(ShowTotals.None);
  const [colTotals, setColTotals] = useState(ShowTotals.None);

  const engineKey = useMemo(
    () =>
      `${viewDto?.Id ?? ''}_${Array.isArray(viewDataList) ? viewDataList.length : 0}_${pivotAggregateFunctionList.length}`,
    [viewDto?.Id, viewDataList, pivotAggregateFunctionList.length]
  );

  useEffect(() => {
    setRowTotals(ShowTotals.None);
    setColTotals(ShowTotals.None);
    setIsPivotPanelCollapsed(false);
  }, [engineKey]);

  useEffect(() => {
    if (!pivotEngine) return;
    pivotEngine.showRowTotals = rowTotals;
    pivotEngine.showColumnTotals = colTotals;
    pivotEngine.invalidate();
  }, [pivotEngine, rowTotals, colTotals]);

  const toPivotFieldArray = (col: any): any[] => {
    if (!col) return [];
    if (Array.isArray(col)) return col;
    if (typeof col.toArray === 'function') return col.toArray();
    if (typeof col.length === 'number' && col.length > 0) {
      try {
        return Array.from(col);
      } catch {
        return [];
      }
    }
    return [];
  };

  const pivotGridViewChanged = useCallback((s: any) => {
    if (!s) return;
    const pivotGrid = s;
    if (pivotGrid.engine) {
      const pivotFields = toPivotFieldArray(pivotGrid.engine.fields);
      const columnFields = toPivotFieldArray(pivotGrid.engine.columnFields);
      const isSketchColSelected = pivotFields.filter((o: any) => o.isActive && o.isContentHtml);
      const isSketchInColumnFields = columnFields.filter((o: any) => o.isActive && o.isContentHtml);

      if (isSketchColSelected.length > 0) {
        pivotGrid.rows.defaultSize = 80;
      } else {
        pivotGrid.rows.defaultSize = 24;
      }
      if (isSketchInColumnFields.length > 0) {
        pivotGrid.columnHeaders.rows.defaultSize = 80;
      } else {
        pivotGrid.columnHeaders.rows.defaultSize = 24;
      }
    }
  }, []);

  useEffect(
    () => () => {
      pivotUpdatedViewUnsubRef.current?.();
      pivotUpdatedViewUnsubRef.current = null;
      pivotGridInstanceRef.current = null;
    },
    []
  );

  const onPivotGridInitialized = useCallback(
    (grid: WjPivotGrid) => {
      pivotGridInstanceRef.current = grid;
      pivotUpdatedViewUnsubRef.current?.();
      pivotUpdatedViewUnsubRef.current = null;
      const eng = grid.engine;
      if (eng?.updatedView) {
        const h = () => pivotGridViewChanged(grid);
        eng.updatedView.addHandler(h);
        pivotUpdatedViewUnsubRef.current = () => {
          eng.updatedView.removeHandler(h);
        };
      }
    },
    [pivotGridViewChanged]
  );

  const exportPivotView = useCallback(() => {
    const ctl = pivotGridInstanceRef.current;
    if (!ctl) return;
    try {
      const book = FlexGridXlsxConverter.saveAsync(ctl, {
        includeColumnHeaders: true,
        includeRowHeaders: true,
      });
      book.sheets[0].name = 'Main View';
      if (ctl.rows?.length <= 10000) {
        const raw = FlexGridXlsxConverter.saveAsync(ctl, {
          includeColumnHeaders: true,
          includeRowHeaders: false,
        });
        raw.sheets[0].name = 'Raw Data';
        book.sheets.push(raw.sheets[0]);
      }
      book.saveAsync('PivotView.xlsx');
    } catch (e) {
      appHelper.debugLog('exportPivotView', e);
    }
  }, []);

  const clusterChild = Boolean(viewDto?.IsClusterChildView);
  const hidePivotPanel = clusterChild;

  // Unmounting PivotPanel while it shares the PivotEngine with PivotGrid can leave PivotGrid in a bad
  // render state (_formatItem reading null). Keep the panel mounted and clip width when "collapsed".
  useEffect(() => {
    if (!pivotEngine || hidePivotPanel) return;
    const id = requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        try {
          pivotEngine.invalidate();
          const g = pivotGridInstanceRef.current;
          g?.invalidate();
          g?.refresh(true);
        } catch (e) {
          appHelper.debugLog('pivotLayoutRefresh', e);
        }
      });
    });
    return () => cancelAnimationFrame(id);
  }, [isPivotPanelCollapsed, pivotEngine, hidePivotPanel]);

  if (!pivotEngine) {
    return (
      <div className="w-full h-full flex flex-col">
        <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>
          Pivot view: no column definition or unable to build pivot engine. Showing grid.
        </div>
        <div className="w-full h-1 flex-auto overflow-hidden">
          <GridViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
          />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      <div
        className={`flex flex-wrap items-center gap-x-3 gap-y-1 px-2 py-1 text-xs border-b shrink-0 ${theme.mainContentSection}`}
      >
        <div className="flex items-center gap-1">
          <span className={`${theme.label} whitespace-nowrap`}>Row totals</span>
          <div className="w-[100px]">
            <ComboBox
              key={`row-${engineKey}`}
              itemsSource={SHOW_TOTALS_OPTIONS}
              displayMemberPath="name"
              selectedValuePath="value"
              selectedValue={rowTotals}
              selectedValueChanged={(cb: wjcInput.ComboBox) => {
                const v = cb.selectedValue as ShowTotals;
                if (v !== undefined && v !== null) setRowTotals(v);
              }}
              style={{ height: '22px', fontSize: '11px' }}
            />
          </div>
        </div>
        <div className="flex items-center gap-1">
          <span className={`${theme.label} whitespace-nowrap`}>Column totals</span>
          <div className="w-[100px]">
            <ComboBox
              key={`col-${engineKey}`}
              itemsSource={SHOW_TOTALS_OPTIONS}
              displayMemberPath="name"
              selectedValuePath="value"
              selectedValue={colTotals}
              selectedValueChanged={(cb: wjcInput.ComboBox) => {
                const v = cb.selectedValue as ShowTotals;
                if (v !== undefined && v !== null) setColTotals(v);
              }}
              style={{ height: '22px', fontSize: '11px' }}
            />
          </div>
        </div>
        {!hidePivotPanel && (
          <button
            type="button"
            className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
            title={isPivotPanelCollapsed ? 'Show Panel' : 'Collapse Panel'}
            onClick={() => setIsPivotPanelCollapsed((c) => !c)}
          >
            <span className={isPivotPanelCollapsed ? 'wj-glyph-plus' : 'wj-glyph-minus'} aria-hidden="true" />{' '}
            {isPivotPanelCollapsed ? 'Show Pivot Panel' : 'Collapse Pivot Panel'}
          </button>
        )}
        <button
          type="button"
          className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
          onClick={exportPivotView}
        >
          <i className="fa-solid fa-file-excel mr-1" aria-hidden="true" />
          Export to Excel
        </button>
      </div>

      <div
        id={viewDto?.UiId || dataModel?.uiControl?.uiId || undefined}
        className="w-full h-1 flex-auto flex min-h-0 overflow-hidden"
      >
        <div
          className={`h-full min-h-0 overflow-auto w-1 flex-auto`}
        >
          <PivotGrid
            key={engineKey}
            itemsSource={pivotEngine}
            className="w-full h-full"
            style={{ height: '100%' }}
            initialized={onPivotGridInitialized}
          />
        </div>
        {!hidePivotPanel && (
          <div
            className={`h-full min-h-0 shrink-0 overflow-hidden border-l transition-[width] duration-150 ease-out ${
              isPivotPanelCollapsed
                ? 'w-0 max-w-0 border-transparent pl-0 pointer-events-none'
                : 'w-[300px] pl-1'
            }`}
            aria-hidden={isPivotPanelCollapsed}
          >
            <div className="h-full w-[min(100%,280px)] min-w-[240px] max-w-full">
              <PivotPanel itemsSource={pivotEngine} style={{ height: '100%', width: '100%' }} />
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
