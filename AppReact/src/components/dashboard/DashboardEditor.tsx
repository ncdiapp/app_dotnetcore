import React, { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useSelector } from 'react-redux';
import type { RootState } from '../../redux/store';
import { dashboardService } from '../../webapi/dashboardsvc';
import { BusyLoader } from '../common/BusyLoader';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { DashboardWidgetRenderer } from './widgets/DashboardWidgetRenderer';
import appHelper from '../../helper/appHelper';
import { buildParamObjFromRouteCodeAndLink } from '../../helper/navigationHelper';

interface DashboardEditorParams extends Record<string, string | undefined> {
  param?: string;
}

/** When set, editor loads this desktop id instead of reading from route (e.g. embedded in Company Security Setting by user type tab). */
export interface DashboardEditorProps {
  embeddedDesktopId?: string | number | null;
}

type LayoutType = 1 | 2;

type FlexWidget = {
  Id: string;
  WidgetItemType: number;
  DisplayName?: string;
  DomElementTag?: string;
  DirectiveName?: string;
  ExternalUrl?: string;
  SearchId?: string;
  IsSavedSearchParameter?: boolean | string;
  InternalPageRoute?: string;
  InternalPageParams?: any;
  ParameterKeyValue?: string;
  DisplayTitle?: string;
  TargetId?: string;
  LinkToListMenu?: any;
};

type ColumnContainer = {
  Id: string;
  WidgetItemType: 102;
  ColSpanValue?: number;
  ColumnSpan?: number;
  MinWidth?: number;
  RowHeight?: number | string;
  StyleLayoutInfo?: string;
  DesktopWidget?: FlexWidget | null;
  ChildDesktopItems?: any[];
  // editor-only metadata (not persisted by server logic)
  __editorMerge?: { dir: 'up' | 'down'; targetRowId: string; targetColId: string };
  __editorGhost?: { fromRowId: string; fromColId: string };
};

type RowContainer = {
  Id: string;
  WidgetItemType: 101;
  RowHeight?: number | string;
  StyleLayoutInfo?: string;
  ChildDesktopItems: ColumnContainer[];
};

type ResponsiveContainer = {
  Id: string;
  WidgetItemType: 100;
  StyleLayoutInfo?: string;
  ChildDesktopItems: RowContainer[];
};

type OtherSettingsDto = {
  FlexLayoutItems: any[];
};

type DashboardData = {
  Id?: string;
  DesktopName?: string;
  Description?: string;
  DesktopDescription?: string;
  LayoutType?: LayoutType | number;
  AppDesktopItemList?: any[];
  OtherSettings?: any;
  OtherSettingsDto?: OtherSettingsDto;
};

type PathSeg = { rowId: string; colId: string };
type ContainerPath = PathSeg[];

type SelectedNode =
  | { kind: 'row'; path: ContainerPath; rowId: string }
  | { kind: 'col'; path: ContainerPath; rowId: string; colId: string }
  | { kind: 'widget'; path: ContainerPath; rowId: string; colId: string; widgetId: string }
  | null;

type CellAddress = { path: ContainerPath; rowId: string; colId: string };

type DragPayload =
  | { kind: 'palette'; paletteId: string }
  | { kind: 'widget'; from: CellAddress; widget: FlexWidget };

type PaletteItem =
  | { id: string; name: string; kind: 'widget'; build: () => FlexWidget }
  | { id: string; name: string; kind: 'container'; build: () => ResponsiveContainer };

function newId(prefix: string) {
  try {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const g: any = globalThis as any;
    if (g?.crypto?.randomUUID) return `${prefix}_${g.crypto.randomUUID()}`;
  } catch {
    // ignore
  }
  return `${prefix}_${Date.now()}_${Math.random().toString(16).slice(2)}`;
}

function toBool(value: any): boolean {
  if (typeof value === 'boolean') return value;
  if (typeof value === 'number') return value !== 0;
  if (typeof value === 'string') {
    const v = value.trim().toLowerCase();
    return v === 'true' || v === '1' || v === 'yes';
  }
  return false;
}

function ensureOtherSettingsDto(d: DashboardData): DashboardData {
  const next: DashboardData = { ...(d || {}) };
  if (!next.OtherSettingsDto) {
    if (next.OtherSettings && typeof next.OtherSettings === 'string') {
      try {
        next.OtherSettingsDto = JSON.parse(next.OtherSettings);
      } catch {
        next.OtherSettingsDto = { FlexLayoutItems: [] };
      }
    } else if (next.OtherSettings && typeof next.OtherSettings === 'object') {
      next.OtherSettingsDto = next.OtherSettings;
    } else {
      next.OtherSettingsDto = { FlexLayoutItems: [] };
    }
  }
  if (!next.OtherSettingsDto) next.OtherSettingsDto = { FlexLayoutItems: [] };
  if (!Array.isArray(next.OtherSettingsDto.FlexLayoutItems)) next.OtherSettingsDto.FlexLayoutItems = [];
  return next;
}

function serializeOtherSettings(d: DashboardData): DashboardData {
  const safe = ensureOtherSettingsDto(d);
  const next: DashboardData = { ...safe };
  try {
    next.OtherSettings = JSON.stringify(next.OtherSettingsDto ?? { FlexLayoutItems: [] });
  } catch {
    // ignore
  }
  return next;
}

function createDefaultFlexRows(): RowContainer[] {
  return createFlexRows(1, 1);
}

/** Create N rows × M columns grid (each row sums to 24 span). Used by Initialize Layout popup. */
function createFlexRows(rowsCount: number, colsPerRow: number): RowContainer[] {
  const rows = Math.max(1, Math.min(100, Math.floor(rowsCount) || 1));
  const cols = Math.max(1, Math.min(24, Math.floor(colsPerRow) || 1));
  const spanBase = Math.floor(24 / cols);
  const remainder = 24 - spanBase * cols;
  const rowsOut: RowContainer[] = [];
  for (let r = 0; r < rows; r++) {
    const cells: ColumnContainer[] = [];
    for (let c = 0; c < cols; c++) {
      const span = c === cols - 1 ? spanBase + remainder : spanBase;
      cells.push({
        Id: newId('col'),
        WidgetItemType: 102,
        ColSpanValue: span,
        DesktopWidget: null,
      });
    }
    rowsOut.push({
      Id: newId('row'),
      WidgetItemType: 101,
      RowHeight: 'auto',
      ChildDesktopItems: cells,
    });
  }
  return rowsOut;
}

function createDefaultResponsiveRoot(rows: RowContainer[] = createDefaultFlexRows()): ResponsiveContainer {
  return {
    Id: newId('responsive'),
    WidgetItemType: 100,
    ChildDesktopItems: rows,
  };
}

function ensureFlexIdsDeep(rows: RowContainer[]): RowContainer[] {
  const safeRows = Array.isArray(rows) ? rows : [];
  return safeRows.map((r) => {
    const rowId = r?.Id != null && String((r as any).Id) !== '' ? String((r as any).Id) : newId('row');
    const childCols = Array.isArray((r as any)?.ChildDesktopItems) ? ((r as any).ChildDesktopItems as any[]) : [];

    const nextCols: ColumnContainer[] = childCols.map((cAny) => {
      const c = (cAny || {}) as any;
      const colId = c?.Id != null && String(c.Id) !== '' ? String(c.Id) : newId('col');
      const wAny = c?.DesktopWidget;
      const nextWidget: FlexWidget | null =
        wAny && typeof wAny === 'object'
          ? ({ ...wAny, Id: String((wAny as any).Id ?? newId('w')) } as FlexWidget)
          : wAny
            ? ({ Id: newId('w'), WidgetItemType: Number((wAny as any)?.WidgetItemType ?? 3), DisplayName: String((wAny as any)?.DisplayName ?? 'Widget') } as any)
            : null;

      const root = getNestedResponsiveRoot(c as ColumnContainer);
      const nextRoot: ResponsiveContainer | null = root
        ? {
            ...(root as any),
            Id: String((root as any).Id ?? newId('responsive')),
            ChildDesktopItems: ensureFlexIdsDeep(Array.isArray((root as any).ChildDesktopItems) ? (root as any).ChildDesktopItems : []),
          }
        : null;

      return {
        ...c,
        Id: colId,
        WidgetItemType: 102,
        DesktopWidget: nextRoot ? null : nextWidget,
        ChildDesktopItems: nextRoot ? [nextRoot] : [],
      } as ColumnContainer;
    });

    return {
      ...(r as any),
      Id: rowId,
      WidgetItemType: 101,
      ChildDesktopItems: nextCols,
    } as RowContainer;
  });
}

function hasMissingFlexIdsDeep(rows: RowContainer[]): boolean {
  const safeRows = Array.isArray(rows) ? rows : [];
  for (const r of safeRows) {
    if (!r || (r as any).Id == null || String((r as any).Id) === '') return true;
    const cols = Array.isArray((r as any).ChildDesktopItems) ? ((r as any).ChildDesktopItems as any[]) : [];
    for (const cAny of cols) {
      if (!cAny || cAny.Id == null || String(cAny.Id) === '') return true;
      const w = cAny.DesktopWidget;
      if (w && typeof w === 'object' && ((w as any).Id == null || String((w as any).Id) === '')) return true;
      const root = getNestedResponsiveRoot(cAny as ColumnContainer);
      if (root) {
        if ((root as any).Id == null || String((root as any).Id) === '') return true;
        if (hasMissingFlexIdsDeep(Array.isArray((root as any).ChildDesktopItems) ? (root as any).ChildDesktopItems : [])) return true;
      }
    }
  }
  return false;
}

function normalizeFlexLayoutItemsForEditor(items: any[]): { mode: 'topRows' | 'responsiveRoot'; rootIndex: number; rows: RowContainer[] } {
  const list = Array.isArray(items) ? items : [];

  const topRows = list.filter((x) => x && x.WidgetItemType === 101) as RowContainer[];
  if (topRows.length) {
    return { mode: 'topRows', rootIndex: -1, rows: topRows };
  }

  const rootIndex = list.findIndex((x) => x && x.WidgetItemType === 100);
  if (rootIndex >= 0) {
    const root = list[rootIndex] as ResponsiveContainer;
    const rows = Array.isArray(root?.ChildDesktopItems)
      ? (root.ChildDesktopItems.filter((x) => x && x.WidgetItemType === 101) as RowContainer[])
      : [];
    return { mode: 'responsiveRoot', rootIndex, rows: rows.length ? rows : createDefaultFlexRows() };
  }

  return { mode: 'responsiveRoot', rootIndex: -1, rows: createDefaultFlexRows() };
}

function sumRowColSpan(row: RowContainer): number {
  const cols = Array.isArray(row?.ChildDesktopItems) ? row.ChildDesktopItems : [];
  return cols.reduce((acc, c: any) => {
    const s = Number(c?.ColSpanValue ?? c?.ColumnSpan ?? 1) || 1;
    return acc + Math.max(1, Math.min(24, s));
  }, 0);
}

function normalizeRowSpans(row: RowContainer): RowContainer {
  const cols = Array.isArray(row.ChildDesktopItems) ? [...row.ChildDesktopItems] : [];
  if (!cols.length) return row;
  // Clamp each span to [1,24]
  cols.forEach((c: any) => {
    const s = Math.max(1, Math.min(24, Number(c.ColSpanValue ?? c.ColumnSpan ?? 1) || 1));
    c.ColSpanValue = s;
  });
  let total = cols.reduce((acc: number, c: any) => acc + Number(c.ColSpanValue || 1), 0);
  if (total <= 24) return { ...row, ChildDesktopItems: cols };
  // Reduce from the last column backwards until total == 24, keeping minimum 1
  for (let i = cols.length - 1; i >= 0 && total > 24; i--) {
    const cur = Number(cols[i].ColSpanValue || 1);
    const reducible = Math.max(0, cur - 1);
    const reduceBy = Math.min(reducible, total - 24);
    cols[i].ColSpanValue = cur - reduceBy;
    total -= reduceBy;
  }
  return { ...row, ChildDesktopItems: cols };
}

function getNestedResponsiveRoot(col: ColumnContainer): ResponsiveContainer | null {
  const items = Array.isArray(col?.ChildDesktopItems) ? col.ChildDesktopItems : [];
  const first = items[0];
  if (first && first.WidgetItemType === 100) return first as ResponsiveContainer;
  return null;
}

function hasNestedContainer(col: ColumnContainer): boolean {
  return Boolean(getNestedResponsiveRoot(col));
}

function tryParseDragPayload(data: string): DragPayload | null {
  if (!data) return null;
  const raw = data.trim();
  // Legacy: palette uses just an id string
  if (raw && !raw.startsWith('{')) {
    return { kind: 'palette', paletteId: raw };
  }
  try {
    const obj = JSON.parse(raw);
    if (obj?.kind === 'palette' && typeof obj.paletteId === 'string') return obj as DragPayload;
    // New shape
    if (
      obj?.kind === 'widget' &&
      obj.from &&
      Array.isArray(obj.from.path) &&
      typeof obj.from.rowId === 'string' &&
      typeof obj.from.colId === 'string' &&
      obj.widget &&
      typeof obj.widget.Id === 'string'
    ) {
      return obj as DragPayload;
    }

    // Back-compat: {kind:'widget', origin:'outer'|'nested', ...}
    if (obj?.kind === 'widget' && typeof obj.fromRowId === 'string' && typeof obj.fromColId === 'string' && obj.widget) {
      if (obj.origin === 'nested' && typeof obj.fromNestedRowId === 'string' && typeof obj.fromNestedColId === 'string') {
        return {
          kind: 'widget',
          from: {
            path: [{ rowId: obj.fromRowId, colId: obj.fromColId }],
            rowId: obj.fromNestedRowId,
            colId: obj.fromNestedColId,
          },
          widget: obj.widget,
        };
      }
      // Treat as outer by default
      return {
        kind: 'widget',
        from: { path: [], rowId: obj.fromRowId, colId: obj.fromColId },
        widget: obj.widget,
      };
    }
  } catch {
    // ignore
  }
  return null;
}

function mapCanvasToFlexRows(appDesktopItemList: any[] | undefined | null): RowContainer[] {
  const list = Array.isArray(appDesktopItemList) ? appDesktopItemList : [];
  const sorted = [...list].sort((a: any, b: any) => {
    const ay = Number(a?.Y ?? 0);
    const by = Number(b?.Y ?? 0);
    if (ay !== by) return ay - by;
    const ax = Number(a?.X ?? 0);
    const bx = Number(b?.X ?? 0);
    return ax - bx;
  });

  if (!sorted.length) return createDefaultFlexRows();

  return sorted.map((item: any) => {
    const w: FlexWidget = {
      ...item,
      Id: String(item?.Id ?? newId('w')),
      WidgetItemType: Number(item?.WidgetItemType ?? 3),
      DisplayName: String(item?.DisplayName ?? item?.Name ?? 'Widget'),
    };
    return {
      Id: newId('row'),
      WidgetItemType: 101,
      RowHeight: 'auto',
      ChildDesktopItems: [
        {
          Id: newId('col'),
          WidgetItemType: 102,
          ColSpanValue: 24,
          MinWidth: 300,
          DesktopWidget: w,
        },
      ],
    };
  });
}

const DashboardEditor: React.FC<DashboardEditorProps> = ({ embeddedDesktopId }) => {
  const { param } = useParams<DashboardEditorParams>();
  const { t, theme } = useTheme();
  const { addTabAndNavigate } = useTabNavigation();
  const { showError, showInfo } = useErrorMessage();
  const userMenu = useSelector((state: RootState) => state.userSession.userMenu);
  
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isNewDashboard, setIsNewDashboard] = useState(false);
  const [selectedNode, setSelectedNode] = useState<SelectedNode>(null);
  const [activeTab, setActiveTab] = useState<'design' | 'preview' | 'json'>('design');
  const [jsonDraft, setJsonDraft] = useState<string>('');
  const [cellAddMenu, setCellAddMenu] = useState<{ open: boolean; path: ContainerPath; rowId: string; colId: string; x: number; y: number } | null>(null);
  const [cellContextMenu, setCellContextMenu] = useState<{ open: boolean; addr: CellAddress; x: number; y: number } | null>(
    null
  );
  const [showToolbox, setShowToolbox] = useState(true);
  const [toolboxExpanded, setToolboxExpanded] = useState<Record<string, boolean>>({
    dashboard: true,
    properties: true,
    builtIn: true,
    appMenu: true,
    folder: true,
  });
  const [menuExpanded, setMenuExpanded] = useState<Set<string>>(new Set());
  const [addAsShortcut, setAddAsShortcut] = useState(false);
  const cellElByKeyRef = useRef<Record<string, HTMLDivElement | null>>({});
  const [mergeOverlayExtraPx, setMergeOverlayExtraPx] = useState<Record<string, number>>({});
  const [hoveredCellKey, setHoveredCellKey] = useState<string | null>(null);
  const [dragOverCellKey, setDragOverCellKey] = useState<string | null>(null);
  const [initLayoutPopup, setInitLayoutPopup] = useState<{ open: boolean; rows: number; cols: number }>({
    open: false,
    rows: 1,
    cols: 1,
  });

  // Parse parameters (embedded mode: use embeddedDesktopId; else from route param)
  const getParams = () => {
    if (embeddedDesktopId != null && embeddedDesktopId !== '') {
      return { id: String(embeddedDesktopId), layoutType: null };
    }
    if (!param) return { id: null, layoutType: null };
    try {
      const decoded = decodeURIComponent(param);
      const paramObj = JSON.parse(decoded);
      return {
        id: paramObj.id || null,
        layoutType: paramObj.layoutType || paramObj.param1 || null
      };
    } catch {
      return { id: param, layoutType: null };
    }
  };

  const reloadDashboard = useCallback(async () => {
    setLoading(true);
    setError(null);
    setSelectedNode(null);
    setActiveTab('design');
    setCellAddMenu(null);

    const { id } = getParams();

    if (!id) {
      // New dashboard
      setIsNewDashboard(true);
      const initial: DashboardData = {
        DesktopName: '',
        // Always create Flex/Responsive dashboards (LayoutType = 2)
        LayoutType: 2,
        AppDesktopItemList: [],
        OtherSettingsDto: {
          FlexLayoutItems: createDefaultFlexRows(),
        },
      };
      setDashboardData(serializeOtherSettings(initial));
      setJsonDraft(JSON.stringify(initial.OtherSettingsDto?.FlexLayoutItems ?? [], null, 2));
      setLoading(false);
      return;
    }

    // Load existing dashboard
    setIsNewDashboard(false);
    try {
      const data = await dashboardService.getOneAppDesktop(id);
      const safe = ensureOtherSettingsDto(data as DashboardData);
      // If server returns no flex structure but LayoutType is 2, create a default row.
      if ((safe.LayoutType === 2 || String(safe.LayoutType) === '2') && !safe.OtherSettingsDto?.FlexLayoutItems?.length) {
        safe.OtherSettingsDto = { FlexLayoutItems: createDefaultFlexRows() };
      }
      const next = serializeOtherSettings(safe);
      setDashboardData(next);
      setJsonDraft(JSON.stringify(next.OtherSettingsDto?.FlexLayoutItems ?? [], null, 2));
      setLoading(false);
    } catch (err: any) {
      console.error('Failed to load dashboard:', err);
      setError(err.message || 'Failed to load dashboard');
      setLoading(false);
    }
  }, [param, embeddedDesktopId]); // param / embeddedDesktopId consumed via getParams()

  useEffect(() => {
    reloadDashboard();
  }, [reloadDashboard]);

  // Close cell add menu on outside click
  useEffect(() => {
    const handleClick = () => {
      if (cellAddMenu?.open) setCellAddMenu(null);
      if (cellContextMenu?.open) setCellContextMenu(null);
    };
    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [cellAddMenu?.open, cellContextMenu?.open]);

  const handleSave = async () => {
    if (!dashboardData) return;

    setSaving(true);
    setError(null);

    try {
      const payload = serializeOtherSettings(dashboardData);
      const result = await dashboardService.saveOneDashboard(payload);
      
      if (result && result.ValidationResult) {
        if (result.ValidationResult.IsValid) {
          showInfo('Dashboard saved successfully', true);
          // Update the dashboard data with the saved result
          if (result.DesktopExDto) {
            const safe = ensureOtherSettingsDto(result.DesktopExDto as DashboardData);
            setDashboardData(serializeOtherSettings(safe));
            setIsNewDashboard(false);
          }
        } else {
          showError(result.ValidationResult.LocalizedResult || 'Failed to save dashboard');
        }
      }
    } catch (err: any) {
      console.error('Failed to save dashboard:', err);
      showError(err.message || 'Failed to save dashboard');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveAs = async () => {
    if (!dashboardData?.Id) return;
    setSaving(true);
    setError(null);
    try {
      const result = await dashboardService.saveAsAppDesktopExDto(String(dashboardData.Id));
      const nextDto = (result?.DesktopExDto ?? result) as DashboardData;
      const safe = ensureOtherSettingsDto(nextDto);
      const serialized = serializeOtherSettings(safe);
      setDashboardData(serialized);
      setJsonDraft(JSON.stringify(serialized.OtherSettingsDto?.FlexLayoutItems ?? [], null, 2));
      setIsNewDashboard(false);
      showInfo('Dashboard saved as new successfully', true);
    } catch (err: any) {
      showError(err?.message || 'Failed to save as');
    } finally {
      setSaving(false);
    }
  };

  const handleRefresh = async () => {
    // Discard local changes and reload from server.
    await reloadDashboard();
  };

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setDashboardData((prev: any) => ({
      ...prev,
      DesktopName: e.target.value
    }));
  };

  const handleDescriptionChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const v = e.target.value;
    setDashboardData((prev: any) => ({
      ...prev,
      Description: v,
      DesktopDescription: v,
    }));
  };

  const rows: RowContainer[] = useMemo(() => {
    if (!dashboardData) return [];
    const safe = ensureOtherSettingsDto(dashboardData);
    const items = safe.OtherSettingsDto?.FlexLayoutItems || [];
    return ensureFlexIdsDeep(normalizeFlexLayoutItemsForEditor(items as any[]).rows);
  }, [dashboardData]);

  function normalizeRowsDeep(rws: RowContainer[]): RowContainer[] {
    const safeRows = Array.isArray(rws) ? rws : [];
    return safeRows.map((r) => {
      const nr = normalizeRowSpans({ ...r, ChildDesktopItems: Array.isArray(r.ChildDesktopItems) ? r.ChildDesktopItems : [] } as RowContainer);
      const nextCols = (nr.ChildDesktopItems || []).map((c) => {
        const col = { ...c } as ColumnContainer;
        const root = getNestedResponsiveRoot(col);
        if (!root) return col;
        const nextRoot: ResponsiveContainer = {
          ...root,
          ChildDesktopItems: normalizeRowsDeep(Array.isArray(root.ChildDesktopItems) ? root.ChildDesktopItems : []),
        };
        return { ...col, DesktopWidget: null, ChildDesktopItems: [nextRoot] };
      });
      return { ...nr, ChildDesktopItems: nextCols };
    });
  }

  const updateRows = (nextRows: RowContainer[]) => {
    const normalizedRows = normalizeRowsDeep(nextRows || []);
    setDashboardData((prev) => {
      if (!prev) return prev;
      const safe = ensureOtherSettingsDto(prev);
      const norm = normalizeFlexLayoutItemsForEditor(safe.OtherSettingsDto?.FlexLayoutItems as any[]);
      let nextFlexItems: any[] = Array.isArray(safe.OtherSettingsDto?.FlexLayoutItems)
        ? [...(safe.OtherSettingsDto!.FlexLayoutItems as any[])]
        : [];

      if (norm.mode === 'topRows') {
        const nonRows = nextFlexItems.filter((x) => !(x && x.WidgetItemType === 101));
        nextFlexItems = [...nonRows, ...normalizedRows];
      } else {
        if (norm.rootIndex >= 0 && nextFlexItems[norm.rootIndex]?.WidgetItemType === 100) {
          const root = { ...(nextFlexItems[norm.rootIndex] as any) };
          root.ChildDesktopItems = normalizedRows;
          nextFlexItems[norm.rootIndex] = root;
        } else {
          nextFlexItems = [createDefaultResponsiveRoot(normalizedRows)];
        }
      }
      const next: DashboardData = {
        ...safe,
        OtherSettingsDto: {
          ...(safe.OtherSettingsDto || {}),
          FlexLayoutItems: nextFlexItems,
        },
      };
      const serialized = serializeOtherSettings(next);
      try {
        setJsonDraft(JSON.stringify(serialized.OtherSettingsDto?.FlexLayoutItems ?? [], null, 2));
      } catch {
        // ignore
      }
      return serialized;
    });
  };

  const handleInitializeLayout = () => {
    setInitLayoutPopup({ open: true, rows: 1, cols: 1 });
  };

  const handleInitLayoutConfirm = () => {
    const { rows, cols } = initLayoutPopup;
    setInitLayoutPopup((p) => ({ ...p, open: false }));
    setSelectedNode(null);
    setCellAddMenu(null);
    setCellContextMenu(null);
    setHoveredCellKey(null);
    setDragOverCellKey(null);
    updateRows(createFlexRows(rows, cols));
  };

  const handleInitLayoutCancel = () => {
    setInitLayoutPopup((p) => ({ ...p, open: false }));
  };

  const mutateOuterRows = (mutate: (outerRows: RowContainer[]) => RowContainer[]) => {
    setDashboardData((prev) => {
      if (!prev) return prev;
      const safe = ensureOtherSettingsDto(prev);
      const items = safe.OtherSettingsDto?.FlexLayoutItems || [];
      const norm = normalizeFlexLayoutItemsForEditor(items as any[]);
      const baseRows = ensureFlexIdsDeep(norm.rows);

      const nextRows = normalizeRowsDeep(mutate(baseRows));

      let nextFlexItems: any[] = Array.isArray(safe.OtherSettingsDto?.FlexLayoutItems)
        ? [...(safe.OtherSettingsDto!.FlexLayoutItems as any[])]
        : [];

      if (norm.mode === 'topRows') {
        const nonRows = nextFlexItems.filter((x) => !(x && x.WidgetItemType === 101));
        nextFlexItems = [...nonRows, ...nextRows];
      } else {
        if (norm.rootIndex >= 0 && nextFlexItems[norm.rootIndex]?.WidgetItemType === 100) {
          const root = { ...(nextFlexItems[norm.rootIndex] as any) };
          root.ChildDesktopItems = nextRows;
          nextFlexItems[norm.rootIndex] = root;
        } else {
          nextFlexItems = [createDefaultResponsiveRoot(nextRows)];
        }
      }

      const next: DashboardData = {
        ...safe,
        OtherSettingsDto: {
          ...(safe.OtherSettingsDto || {}),
          FlexLayoutItems: nextFlexItems,
        },
      };
      const serialized = serializeOtherSettings(next);
      try {
        setJsonDraft(JSON.stringify(serialized.OtherSettingsDto?.FlexLayoutItems ?? [], null, 2));
      } catch {
        // ignore
      }
      return serialized;
    });
  };

  const setWidgetInOuterRows = (outerRows: RowContainer[], addr: CellAddress, w: FlexWidget | null): RowContainer[] => {
    return updateRowsByPath(outerRows, addr.path, (rws) => {
      const rowIdx = (rws || []).findIndex((r) => String(r.Id) === String(addr.rowId));
      if (rowIdx < 0) return rws;
      const nextRows = [...rws];
      const row = { ...nextRows[rowIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const colIdx = cols.findIndex((c) => String((c as any).Id) === String(addr.colId));
      if (colIdx < 0) return rws;
      cols[colIdx] = { ...(cols[colIdx] as any), DesktopWidget: w, ChildDesktopItems: [] };
      row.ChildDesktopItems = cols as any;
      nextRows[rowIdx] = row as any;
      return nextRows as any;
    });
  };

  const getColInOuterRows = (outerRows: RowContainer[], addr: CellAddress): ColumnContainer | null => {
    const rws = getRowsAtPath(outerRows, addr.path);
    const row = (rws || []).find((r) => String(r.Id) === String(addr.rowId));
    const col = row?.ChildDesktopItems?.find((c) => String((c as any).Id) === String(addr.colId)) ?? null;
    return (col as any) ?? null;
  };

  // Ensure stable Ids exist in persisted dashboardData (critical for drag/drop).
  useEffect(() => {
    if (!dashboardData) return;
    try {
      const safe = ensureOtherSettingsDto(dashboardData);
      const items = safe.OtherSettingsDto?.FlexLayoutItems || [];
      const norm = normalizeFlexLayoutItemsForEditor(items as any[]);
      if (!hasMissingFlexIdsDeep(norm.rows)) return;
      updateRows(ensureFlexIdsDeep(norm.rows));
    } catch {
      // ignore
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dashboardData?.Id, dashboardData?.OtherSettings]);

  const pathKey = (path: ContainerPath) => (path || []).map((s) => `${s.rowId}:${s.colId}`).join('>');

  const getRowsAtPath = (allRows: RowContainer[], path: ContainerPath): RowContainer[] => {
    let cur: RowContainer[] = allRows;
    for (const seg of path || []) {
      const r = (cur || []).find((x) => String(x.Id) === String(seg.rowId));
      const c = r?.ChildDesktopItems?.find((x) => String(x.Id) === String(seg.colId));
      const root = c ? getNestedResponsiveRoot(c) : null;
      cur = (root?.ChildDesktopItems as any) || [];
    }
    return Array.isArray(cur) ? cur : [];
  };

  const updateRowsByPath = (allRows: RowContainer[], path: ContainerPath, updater: (target: RowContainer[]) => RowContainer[]): RowContainer[] => {
    const p = Array.isArray(path) ? path : [];
    if (!p.length) return updater(allRows);
    const [seg, ...rest] = p;
    return (allRows || []).map((r) => {
      if (String(r.Id) !== String(seg.rowId)) return r;
      const nextCols = (r.ChildDesktopItems || []).map((c) => {
        if (String(c.Id) !== String(seg.colId)) return c;
        const root = getNestedResponsiveRoot(c) ?? createDefaultResponsiveRoot(createDefaultFlexRows());
        const nextChildRows = updateRowsByPath(
          Array.isArray(root.ChildDesktopItems) ? (root.ChildDesktopItems as RowContainer[]) : [],
          rest,
          updater
        );
        const nextRoot: ResponsiveContainer = { ...root, ChildDesktopItems: nextChildRows };
        return { ...c, DesktopWidget: null, ChildDesktopItems: [nextRoot] };
      });
      return { ...r, ChildDesktopItems: nextCols };
    });
  };

  const updateInContainer = (path: ContainerPath, updater: (target: RowContainer[]) => RowContainer[]) => {
    const nextOuter = updateRowsByPath(rows, path, updater);
    updateRows(nextOuter);
  };

  const getRowAt = (path: ContainerPath, rowId: string): RowContainer | null => {
    const rws = getRowsAtPath(rows, path);
    return (rws || []).find((r) => String(r.Id) === String(rowId)) ?? null;
  };

  const getColAt = (path: ContainerPath, rowId: string, colId: string): ColumnContainer | null => {
    const row = getRowAt(path, rowId);
    const col = row?.ChildDesktopItems?.find((c) => String(c.Id) === String(colId)) ?? null;
    return (col as any) ?? null;
  };

  const clearCellContentAt = (path: ContainerPath, rowId: string, colId: string) => {
    // clear both widget and container
    setWidgetAt(path, rowId, colId, null);
    setNestedContainerAt(path, rowId, colId, null);
  };

  const insertRowRelativeAt = (path: ContainerPath, rowId: string, where: 'above' | 'below') => {
    updateInContainer(path, (rws) => {
      const list = [...(rws || [])];
      const idx = list.findIndex((r) => String(r.Id) === String(rowId));
      if (idx < 0) return rws;
      const insertIdx = where === 'above' ? idx : idx + 1;
      const newRow = createDefaultFlexRows()[0];
      list.splice(insertIdx, 0, newRow);
      return list;
    });
  };

  const insertColumnRelativeAt = (path: ContainerPath, rowId: string, colId: string, where: 'left' | 'right') => {
    updateInContainer(path, (rws) => {
      const list = [...(rws || [])];
      const rIdx = list.findIndex((r) => String(r.Id) === String(rowId));
      if (rIdx < 0) return rws;
      const row = { ...list[rIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const cIdx = cols.findIndex((c) => String(c.Id) === String(colId));
      if (cIdx < 0) return rws;

      const curSpan = Math.max(1, Math.min(24, Number(cols[cIdx].ColSpanValue ?? cols[cIdx].ColumnSpan ?? 1) || 1));
      const newSpan = Math.max(1, Math.floor(curSpan / 2));
      cols[cIdx] = { ...cols[cIdx], ColSpanValue: Math.max(1, curSpan - newSpan) };
      const newCol: ColumnContainer = {
        Id: newId('col'),
        WidgetItemType: 102,
        ColSpanValue: newSpan,
        MinWidth: 300,
        DesktopWidget: null,
        ChildDesktopItems: [],
      };
      const insertIdx = where === 'left' ? cIdx : cIdx + 1;
      cols.splice(insertIdx, 0, newCol);
      row.ChildDesktopItems = cols;
      list[rIdx] = row;
      return list;
    });
  };

  const splitColumnAt = (path: ContainerPath, rowId: string, colId: string, parts: 2 | 3 | 4) => {
    updateInContainer(path, (rws) => {
      const list = [...(rws || [])];
      const rIdx = list.findIndex((r) => String(r.Id) === String(rowId));
      if (rIdx < 0) return rws;
      const row = { ...list[rIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const cIdx = cols.findIndex((c) => String(c.Id) === String(colId));
      if (cIdx < 0) return rws;

      const cur = cols[cIdx];
      const curSpan = Math.max(1, Math.min(24, Number(cur.ColSpanValue ?? cur.ColumnSpan ?? 1) || 1));
      const base = Math.max(1, Math.floor(curSpan / parts));
      const spans = Array.from({ length: parts }, (_, i) => (i === parts - 1 ? curSpan - base * (parts - 1) : base)).map(
        (s) => Math.max(1, s)
      );

      const newCols: ColumnContainer[] = spans.map((s, i) => ({
        Id: newId('col'),
        WidgetItemType: 102,
        ColSpanValue: s,
        MinWidth: cur.MinWidth ?? 300,
        DesktopWidget: i === 0 ? (cur.DesktopWidget ?? null) : null,
        ChildDesktopItems: i === 0 ? (cur.ChildDesktopItems ?? []) : [],
        StyleLayoutInfo: i === 0 ? cur.StyleLayoutInfo : undefined,
      }));
      cols.splice(cIdx, 1, ...newCols);
      row.ChildDesktopItems = cols;
      list[rIdx] = row;
      return list;
    });
  };

  const mergeColumnAt = (path: ContainerPath, rowId: string, colId: string, dir: 'left' | 'right') => {
    updateInContainer(path, (rws) => {
      const list = [...(rws || [])];
      const rIdx = list.findIndex((r) => String(r.Id) === String(rowId));
      if (rIdx < 0) return rws;
      const row = { ...list[rIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const cIdx = cols.findIndex((c) => String(c.Id) === String(colId));
      if (cIdx < 0) return rws;
      const targetIdx = dir === 'left' ? cIdx - 1 : cIdx + 1;
      if (targetIdx < 0 || targetIdx >= cols.length) return rws;

      const cur = cols[cIdx];
      const tgt = cols[targetIdx];
      const curSpan = Math.max(1, Math.min(24, Number(cur.ColSpanValue ?? cur.ColumnSpan ?? 1) || 1));
      const tgtSpan = Math.max(1, Math.min(24, Number(tgt.ColSpanValue ?? tgt.ColumnSpan ?? 1) || 1));

      const mergedWidget = tgt.DesktopWidget ?? cur.DesktopWidget ?? null;
      const mergedChild = (tgt.ChildDesktopItems && tgt.ChildDesktopItems.length ? tgt.ChildDesktopItems : cur.ChildDesktopItems) ?? [];

      const nextTarget: ColumnContainer = {
        ...tgt,
        ColSpanValue: Math.min(24, tgtSpan + curSpan),
        DesktopWidget: mergedWidget,
        ChildDesktopItems: mergedChild,
      };
      cols[targetIdx] = nextTarget;
      cols.splice(cIdx, 1);
      row.ChildDesktopItems = cols;
      list[rIdx] = row;
      return list;
    });
  };

  const _getRowIndexAt = (path: ContainerPath, rowId: string): number => {
    const rws = getRowsAtPath(rows, path);
    return (rws || []).findIndex((r) => String(r.Id) === String(rowId));
  };

  const getColRangeInRow = (row: RowContainer, colId: string): { start: number; end: number; idx: number; span: number } | null => {
    let cursor = 0;
    const cols = row.ChildDesktopItems || [];
    for (let i = 0; i < cols.length; i++) {
      const c = cols[i];
      const span = Math.max(1, Math.min(24, Number(c.ColSpanValue ?? c.ColumnSpan ?? 1) || 1));
      const start = cursor;
      const end = cursor + span;
      if (String(c.Id) === String(colId)) return { start, end, idx: i, span };
      cursor = end;
    }
    return null;
  };

  const findBestOverlappingColId = (row: RowContainer, start: number, end: number): string | null => {
    let cursor = 0;
    let bestId: string | null = null;
    let bestOverlap = -1;
    const cols = row.ChildDesktopItems || [];
    for (const c of cols) {
      const span = Math.max(1, Math.min(24, Number(c.ColSpanValue ?? c.ColumnSpan ?? 1) || 1));
      const s = cursor;
      const e = cursor + span;
      cursor = e;
      const overlap = Math.max(0, Math.min(end, e) - Math.max(start, s));
      if (overlap > bestOverlap) {
        bestOverlap = overlap;
        bestId = String(c.Id);
      }
    }
    return bestId;
  };

  const cellContentToRows = (col: ColumnContainer): RowContainer[] => {
    const root = getNestedResponsiveRoot(col);
    if (root?.ChildDesktopItems?.length) return root.ChildDesktopItems as RowContainer[];
    const w = col.DesktopWidget;
    if (!w) return [];
    const row: RowContainer = {
      Id: newId('row'),
      WidgetItemType: 101,
      RowHeight: 'auto',
      ChildDesktopItems: [
        {
          Id: newId('col'),
          WidgetItemType: 102,
          ColSpanValue: 24,
          MinWidth: col.MinWidth ?? 300,
          DesktopWidget: w,
          ChildDesktopItems: [],
        },
      ],
    };
    return [row];
  };

  const mergeVerticalAt = (path: ContainerPath, rowId: string, colId: string, dir: 'up' | 'down') => {
    updateInContainer(path, (rws) => {
      const list = [...(rws || [])];
      const rIdx = list.findIndex((r) => String(r.Id) === String(rowId));
      if (rIdx < 0) return rws;
      const targetIdx = dir === 'up' ? rIdx - 1 : rIdx + 1;
      if (targetIdx < 0 || targetIdx >= list.length) return rws;

      const row = list[rIdx];
      const targetRow = list[targetIdx];
      const range = getColRangeInRow(row, colId);
      if (!range) return rws;

      const targetColId = findBestOverlappingColId(targetRow, range.start, range.end);
      if (!targetColId) return rws;

      const curCol = row.ChildDesktopItems.find((c) => String(c.Id) === String(colId));
      const tgtCol = targetRow.ChildDesktopItems.find((c) => String(c.Id) === String(targetColId));
      if (!curCol || !tgtCol) return rws;

      const curRows = cellContentToRows(curCol);
      const tgtRows = cellContentToRows(tgtCol);
      const mergedRows = dir === 'up' ? [...tgtRows, ...curRows] : [...curRows, ...tgtRows];
      const mergedRoot: ResponsiveContainer = createDefaultResponsiveRoot(mergedRows.length ? mergedRows : createDefaultFlexRows());

      // Apply to current cell
      const nextRow: RowContainer = {
        ...row,
        ChildDesktopItems: row.ChildDesktopItems.map((c) =>
          String(c.Id) === String(colId)
            ? ({
                ...c,
                DesktopWidget: null,
                ChildDesktopItems: [mergedRoot],
                __editorMerge: { dir, targetRowId: String(targetRow.Id), targetColId: String(targetColId) },
              } as ColumnContainer)
            : c
        ),
      };

      // Clear target cell
      const nextTargetRow: RowContainer = {
        ...targetRow,
        ChildDesktopItems: targetRow.ChildDesktopItems.map((c) =>
          String(c.Id) === String(targetColId)
            ? ({
                ...c,
                DesktopWidget: null,
                ChildDesktopItems: [],
                __editorGhost: { fromRowId: String(row.Id), fromColId: String(colId) },
              } as ColumnContainer)
            : c
        ),
      };

      list[rIdx] = nextRow;
      list[targetIdx] = nextTargetRow;
      return list;
    });
  };

  const openCellContextMenu = (addr: CellAddress, x: number, y: number) => {
    const { x: clampedX, y: clampedY } = appHelper.clampMenuPositionToViewport({
      x,
      y,
      menuWidth: 300,
      menuHeight: 480,
      margin: 8,
    });
    setCellContextMenu({ open: true, addr, x: clampedX, y: clampedY });
    setSelectedNode({ kind: 'col', path: addr.path, rowId: addr.rowId, colId: addr.colId });
  };

  const findRowIndexIn = (rws: RowContainer[], rowId: string) => (rws || []).findIndex((r) => String(r.Id) === String(rowId));
  const findColIndexIn = (row: RowContainer, colId: string) => (row.ChildDesktopItems || []).findIndex((c) => String(c.Id) === String(colId));

  const addRowAt = (path: ContainerPath) => updateInContainer(path, (rws) => [...(rws || []), ...createDefaultFlexRows()]);

  const deleteRowAt = (path: ContainerPath, rowId: string) => {
    updateInContainer(path, (rws) => {
      const next = (rws || []).filter((r) => String(r.Id) !== String(rowId));
      return next.length ? next : createDefaultFlexRows();
    });
    setSelectedNode(null);
  };

  const moveRowAt = (path: ContainerPath, rowId: string, dir: -1 | 1) => {
    updateInContainer(path, (rws) => {
      const idx = findRowIndexIn(rws, rowId);
      if (idx < 0) return rws;
      const nextIdx = idx + dir;
      if (nextIdx < 0 || nextIdx >= rws.length) return rws;
      const next = [...rws];
      const tmp = next[idx];
      next[idx] = next[nextIdx];
      next[nextIdx] = tmp;
      return next;
    });
  };

  const addColumnAt = (path: ContainerPath, rowId: string) => {
    updateInContainer(path, (rws) => {
      const rowIdx = findRowIndexIn(rws, rowId);
      if (rowIdx < 0) return rws;
      const nextRows = [...rws];
      const row = { ...nextRows[rowIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const used = sumRowColSpan(row);
      const remaining = Math.max(1, 24 - used);
      cols.push({
        Id: newId('col'),
        WidgetItemType: 102,
        ColSpanValue: Math.min(12, remaining),
        MinWidth: 300,
        DesktopWidget: null,
      });
      row.ChildDesktopItems = cols;
      nextRows[rowIdx] = row;
      return nextRows;
    });
  };

  const deleteColumnAt = (path: ContainerPath, rowId: string, colId: string) => {
    updateInContainer(path, (rws) => {
      const rowIdx = findRowIndexIn(rws, rowId);
      if (rowIdx < 0) return rws;
      const nextRows = [...rws];
      const row = { ...nextRows[rowIdx] };
      const cols = (row.ChildDesktopItems || []).filter((c) => String(c.Id) !== String(colId));
      row.ChildDesktopItems = cols.length
        ? cols
        : [
            {
              Id: newId('col'),
              WidgetItemType: 102,
              ColSpanValue: 24,
              MinWidth: 300,
              DesktopWidget: null,
            },
          ];
      nextRows[rowIdx] = row;
      return nextRows;
    });
    setSelectedNode(null);
  };

  const moveColAt = (path: ContainerPath, rowId: string, colId: string, dir: -1 | 1) => {
    updateInContainer(path, (rws) => {
      const rowIdx = findRowIndexIn(rws, rowId);
      if (rowIdx < 0) return rws;
      const row = rws[rowIdx];
      const cols = [...(row.ChildDesktopItems || [])];
      const idx = cols.findIndex((c) => String(c.Id) === String(colId));
      if (idx < 0) return rws;
      const nextIdx = idx + dir;
      if (nextIdx < 0 || nextIdx >= cols.length) return rws;
      const tmp = cols[idx];
      cols[idx] = cols[nextIdx];
      cols[nextIdx] = tmp;
      const nextRows = [...rws];
      nextRows[rowIdx] = { ...row, ChildDesktopItems: cols };
      return nextRows;
    });
  };

  const setColumnSpanAt = (path: ContainerPath, rowId: string, colId: string, span: number) => {
    const s = Math.max(1, Math.min(24, Number(span) || 1));
    updateInContainer(path, (rws) => {
      const rowIdx = findRowIndexIn(rws, rowId);
      if (rowIdx < 0) return rws;
      const nextRows = [...rws];
      const row = { ...nextRows[rowIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const colIdx = findColIndexIn(row, colId);
      if (colIdx < 0) return rws;
      cols[colIdx] = { ...cols[colIdx], ColSpanValue: s };
      row.ChildDesktopItems = cols;
      nextRows[rowIdx] = row;
      return nextRows;
    });
  };

  const setColumnMinWidthAt = (path: ContainerPath, rowId: string, colId: string, minWidth: number) => {
    const n = Number(minWidth);
    const mw = Number.isFinite(n) && !Number.isNaN(n) ? Math.max(0, n) : 0;
    updateInContainer(path, (rws) => {
      const rowIdx = findRowIndexIn(rws, rowId);
      if (rowIdx < 0) return rws;
      const nextRows = [...rws];
      const row = { ...nextRows[rowIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const colIdx = findColIndexIn(row, colId);
      if (colIdx < 0) return rws;
      cols[colIdx] = { ...cols[colIdx], MinWidth: mw };
      row.ChildDesktopItems = cols;
      nextRows[rowIdx] = row;
      return nextRows;
    });
  };

  const setColumnStyleLayoutInfoAt = (path: ContainerPath, rowId: string, colId: string, style: string) => {
    updateInContainer(path, (rws) => {
      const rowIdx = findRowIndexIn(rws, rowId);
      if (rowIdx < 0) return rws;
      const nextRows = [...rws];
      const row = { ...nextRows[rowIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const colIdx = findColIndexIn(row, colId);
      if (colIdx < 0) return rws;
      cols[colIdx] = { ...cols[colIdx], StyleLayoutInfo: style };
      row.ChildDesktopItems = cols;
      nextRows[rowIdx] = row;
      return nextRows;
    });
  };

  const setRowHeightAt = (path: ContainerPath, rowId: string, rowHeight: number | string) => {
    updateInContainer(path, (rws) => (rws || []).map((r) => (String(r.Id) === String(rowId) ? ({ ...r, RowHeight: rowHeight } as RowContainer) : r)));
  };

  const setRowStyleLayoutInfoAt = (path: ContainerPath, rowId: string, style: string) => {
    updateInContainer(path, (rws) => (rws || []).map((r) => (String(r.Id) === String(rowId) ? ({ ...r, StyleLayoutInfo: style } as RowContainer) : r)));
  };

  const setWidgetAt = (path: ContainerPath, rowId: string, colId: string, widget: FlexWidget | null) => {
    updateInContainer(path, (rws) => {
      const rowIdx = findRowIndexIn(rws, rowId);
      if (rowIdx < 0) return rws;
      const nextRows = [...rws];
      const row = { ...nextRows[rowIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const colIdx = findColIndexIn(row, colId);
      if (colIdx < 0) return rws;
      cols[colIdx] = { ...cols[colIdx], DesktopWidget: widget, ChildDesktopItems: [] };
      row.ChildDesktopItems = cols;
      nextRows[rowIdx] = row;
      return nextRows;
    });
  };

  const setNestedContainerAt = (path: ContainerPath, rowId: string, colId: string, container: ResponsiveContainer | null) => {
    updateInContainer(path, (rws) => {
      const rowIdx = findRowIndexIn(rws, rowId);
      if (rowIdx < 0) return rws;
      const nextRows = [...rws];
      const row = { ...nextRows[rowIdx] };
      const cols = [...(row.ChildDesktopItems || [])];
      const colIdx = findColIndexIn(row, colId);
      if (colIdx < 0) return rws;
      const prevCol = cols[colIdx];
      cols[colIdx] = { ...prevCol, DesktopWidget: null, ChildDesktopItems: container ? [container] : [] };
      row.ChildDesktopItems = cols;
      nextRows[rowIdx] = row;
      return nextRows;
    });
  };

  const _clearWidgetFromPayload = (payload: Exclude<DragPayload, { kind: 'palette'; paletteId: string }>) => {
    setWidgetAt(payload.from.path, payload.from.rowId, payload.from.colId, null);
  };

  // Back-compat wrappers for root-level buttons (reserved for future UI)
  const _addRow = () => addRowAt([]);
  const _deleteRow = (rowId: string) => deleteRowAt([], rowId);
  const _moveRow = (rowId: string, dir: -1 | 1) => moveRowAt([], rowId, dir);
  const _addColumn = (rowId: string) => addColumnAt([], rowId);
  const _deleteColumn = (rowId: string, colId: string) => deleteColumnAt([], rowId, colId);
  const _moveCol = (rowId: string, colId: string, dir: -1 | 1) => moveColAt([], rowId, colId, dir);
  const _setColumnSpan = (rowId: string, colId: string, span: number) => setColumnSpanAt([], rowId, colId, span);
  const _setColumnMinWidth = (rowId: string, colId: string, minWidth: number) => setColumnMinWidthAt([], rowId, colId, minWidth);
  const _setWidget = (rowId: string, colId: string, widget: FlexWidget | null) => setWidgetAt([], rowId, colId, widget);
  const _setNestedContainer = (rowId: string, colId: string, container: ResponsiveContainer | null) => setNestedContainerAt([], rowId, colId, container);

  const applyJsonDraft = () => {
    try {
      const parsed = JSON.parse(jsonDraft || '[]');
      const list = Array.isArray(parsed) ? parsed : [];
      // Accept either:
      // - top-level rows (101)
      // - a responsive root (100) that contains rows
      const norm = normalizeFlexLayoutItemsForEditor(list);

      const coerceWidget = (x: any): FlexWidget | null => {
        if (!x || typeof x !== 'object') return null;
        return { ...(x as any), Id: String((x as any).Id ?? newId('w')), WidgetItemType: Number((x as any).WidgetItemType ?? 3) };
      };

      const coerceRows = (arr: any[]): RowContainer[] =>
        (Array.isArray(arr) ? arr : [])
          .filter(Boolean)
          .map((r: any) => {
            const colsAny = Array.isArray(r?.ChildDesktopItems) ? r.ChildDesktopItems : [];
            const cols: ColumnContainer[] = colsAny.length
              ? colsAny.map((c: any) => {
                  const childAny = Array.isArray(c?.ChildDesktopItems) ? c.ChildDesktopItems : [];
                  const first = childAny[0];
                  let childRoot: ResponsiveContainer | null = null;
                  if (first && Number(first.WidgetItemType) === 100) {
                    childRoot = {
                      ...(first as any),
                      Id: String(first.Id ?? newId('responsive')),
                      WidgetItemType: 100,
                      ChildDesktopItems: coerceRows((first as any).ChildDesktopItems || []),
                    };
                  } else if (first && Number(first.WidgetItemType) === 101) {
                    childRoot = createDefaultResponsiveRoot(coerceRows(childAny));
                  }

                  const desktopWidget =
                    c?.DesktopWidget != null
                      ? coerceWidget(c.DesktopWidget)
                      : c?.WidgetItemType != null && ![100, 101, 102].includes(Number(c.WidgetItemType))
                        ? coerceWidget(c)
                        : null;

                  return {
                    ...(c as any),
                    Id: String(c?.Id ?? newId('col')),
                    WidgetItemType: 102,
                    DesktopWidget: desktopWidget,
                    ChildDesktopItems: childRoot ? [childRoot] : [],
                  } as ColumnContainer;
                })
              : (createDefaultFlexRows()[0].ChildDesktopItems as any);

            return {
              ...(r as any),
              Id: String(r?.Id ?? newId('row')),
              WidgetItemType: 101,
              ChildDesktopItems: cols,
            } as RowContainer;
          });

      const rowsToApply: RowContainer[] = coerceRows(norm.rows || []);
      updateRows(rowsToApply.length ? rowsToApply : createDefaultFlexRows());
      setError(null);
      setActiveTab('design');
    } catch (e: any) {
      setError(e?.message || 'Invalid JSON');
    }
  };

  const convertCanvasToFlex = () => {
    if (!dashboardData) return;
    const nextRows = mapCanvasToFlexRows(dashboardData.AppDesktopItemList);
    setDashboardData((prev) => {
      if (!prev) return prev;
      const safe = ensureOtherSettingsDto(prev);
      const next: DashboardData = {
        ...safe,
        LayoutType: 2,
        OtherSettingsDto: { ...(safe.OtherSettingsDto || {}), FlexLayoutItems: [createDefaultResponsiveRoot(nextRows)] },
      };
      const serialized = serializeOtherSettings(next);
      setJsonDraft(JSON.stringify(serialized.OtherSettingsDto?.FlexLayoutItems ?? [], null, 2));
      return serialized;
    });
    setSelectedNode(null);
    setActiveTab('design');
  };

  const selected = useMemo(() => {
    if (!selectedNode) return null;
    const targetRows = getRowsAtPath(rows, selectedNode.path);
    const row = targetRows.find((r) => String(r.Id) === String(selectedNode.rowId));
    if (!row) return null;
    if (selectedNode.kind === 'row') return { path: selectedNode.path, row };
    const col = (row.ChildDesktopItems || []).find((c) => String(c.Id) === String(selectedNode.colId));
    if (!col) return null;
    if (selectedNode.kind === 'col') return { path: selectedNode.path, row, col };
    const w = col.DesktopWidget;
    return { path: selectedNode.path, row, col, widget: w ?? null };
  }, [rows, selectedNode]);

  const palette: PaletteItem[] = useMemo(
    () => {
      // Built-in widgets: only File Drop Box and My Application Management
      const base: PaletteItem[] = [
      {
        id: 'fileDropBox',
        name: 'File Drop Box',
        kind: 'widget',
        build: (): FlexWidget => ({
          Id: newId('w'),
          WidgetItemType: 2,
          DisplayName: 'File Drop Box',
          DisplayTitle: 'File Drop Box',
          InternalPageRoute: 'test-dom-drag-and-drop',
          DomElementTag: 'test-dom-drag-and-drop',
          InternalPageParams: {},
        }),
      },
      {
        id: 'myApplicationManagement',
        name: 'My Application Management',
        kind: 'widget',
        build: (): FlexWidget => ({
          Id: newId('w'),
          WidgetItemType: 2,
          DisplayName: 'My Application Management',
          DisplayTitle: 'My Application Management',
          InternalPageRoute: 'my-applications',
          DomElementTag: 'my-applications',
          InternalPageParams: {},
        }),
      },
      {
        id: 'responsiveContainer',
        name: 'Container: Responsive (Nested)',
        kind: 'container',
        build: (): ResponsiveContainer => createDefaultResponsiveRoot(createDefaultFlexRows()),
      },
      ];

      const flattenLeaves = (list: any[]): any[] => {
        const out: any[] = [];
        const walk = (node: any) => {
          if (!node) return;
          const children = (node.AppListMenu_List || node.Children || []) as any[];
          if (Array.isArray(children) && children.length) {
            children.forEach(walk);
            return;
          }
          if (node.RouteCode || node.Link) out.push(node);
        };
        (Array.isArray(list) ? list : []).forEach(walk);
        return out;
      };

      const leaves = flattenLeaves(userMenu || []);
      const menuItems: PaletteItem[] = leaves
        .filter((m: any) => m && (m.Id || m.Name))
        .map((m: any) => {
          const id = String(m.Id ?? m.Name ?? newId('menu'));
          const name = String(m.Name ?? m.DisplayName ?? 'Menu');
          const routeCode = String(m.RouteCode ?? m.InternalPageRoute ?? '');
          const link = m.Link ?? m.MenuLinkStr ?? '';
          const linkType = m.LinkType ?? 0;
          const imageUrl = m.ImageUrl ?? m.IconUrl ?? null;

          return {
            id: `menu:${id}`,
            name,
            kind: 'widget',
            build: (): FlexWidget => {
              if (!addAsShortcut) {
                // API/DB expects DomElementTag, DisplayTitle, ParameterKeyValue, LinkToListMenu for WidgetItemType 2
                const linkToListMenu =
                  m && typeof m === 'object'
                    ? {
                        Id: m.Id,
                        ParentId: m.ParentId,
                        Name: name,
                        Description: m.Description ?? '',
                        IconName: m.IconName ?? m.IconUrl ?? '',
                        RouteCode: routeCode || '',
                        Link: String(link ?? ''),
                        Sort: m.Sort ?? 0,
                        LinkType: linkType,
                        ImageUrl: imageUrl ?? undefined,
                        MenuPath: m.MenuPath ?? '',
                        ...(typeof m.GlobalGuid !== 'undefined' && { GlobalGuid: m.GlobalGuid }),
                        ...(typeof m.ModuleRegisterId !== 'undefined' && { ModuleRegisterId: m.ModuleRegisterId }),
                      }
                    : undefined;
                return {
                  Id: newId('w'),
                  WidgetItemType: 2,
                  DisplayName: name,
                  DisplayTitle: name,
                  InternalPageRoute: routeCode || '',
                  DomElementTag: routeCode || undefined,
                  ParameterKeyValue: m?.Id != null ? String(m.Id) : (link || undefined),
                  InternalPageParams: link ? buildParamObjFromRouteCodeAndLink(routeCode, link) : {},
                  ...(linkToListMenu && { LinkToListMenu: linkToListMenu }),
                };
              }
              return {
                Id: newId('w'),
                WidgetItemType: 6,
                DisplayName: name,
                LinkToListMenu: {
                  Name: name,
                  RouteCode: routeCode || '',
                  LinkType: linkType,
                  Link: String(link ?? ''),
                  ImageUrl: imageUrl ?? undefined,
                },
              };
            },
          } as PaletteItem;
        });

      return [...base, ...menuItems];
    },
    [userMenu, addAsShortcut]
  );

  const handleInternalShortcut = (stateName: string, menuLinkStr: string, menuName?: string) => {
    const tabHeading = menuName || stateName;
    // best-effort param mapping; real widgets usually carry richer routing info
    addTabAndNavigate(`/${stateName}`, tabHeading, { id: menuLinkStr || null }, true);
  };

  const handleSearchShortcut = (searchId: string, isSavedSearchParameter?: boolean, displayTitle?: string) => {
    const tabHeading = displayTitle || 'Search';
    const isSavedSearch = toBool(isSavedSearchParameter);
    addTabAndNavigate('/masterdatamanagement', tabHeading, { searchId, isSavedSearch }, true);
  };

  useLayoutEffect(() => {
    // Compute overlay extra height for vertically-merged cells (visual rowSpan)
    const next: Record<string, number> = {};

    const walk = (path: ContainerPath, rws: RowContainer[]) => {
      const pKey = pathKey(path);
      for (const row of rws || []) {
        const rowId = String(row.Id);
        for (const col of row.ChildDesktopItems || []) {
          const c = col as ColumnContainer;
          if (!c.__editorMerge) continue;
          const fromKey = `${pKey}|${rowId}|${String(c.Id)}`;
          const targetKey = `${pKey}|${String(c.__editorMerge.targetRowId)}|${String(c.__editorMerge.targetColId)}`;
          const fromEl = cellElByKeyRef.current[fromKey];
          const targetEl = cellElByKeyRef.current[targetKey];
          if (!fromEl || !targetEl) continue;
          const fromRect = fromEl.getBoundingClientRect();
          const targetRect = targetEl.getBoundingClientRect();
          const extra = Math.max(0, targetRect.bottom - fromRect.bottom);
          if (extra > 0) next[fromKey] = extra;
        }
      }
      // recurse into nested containers
      for (const row of rws || []) {
        for (const col of row.ChildDesktopItems || []) {
          const root = getNestedResponsiveRoot(col);
          if (root?.ChildDesktopItems?.length) {
            walk([...path, { rowId: String(row.Id), colId: String(col.Id) }], root.ChildDesktopItems || []);
          }
        }
      }
    };

    walk([], rows);
    setMergeOverlayExtraPx(next);
  }, [rows]);

  const toggleToolboxSection = (key: string) => {
    setToolboxExpanded((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const toggleMenuNode = (id: string) => {
    setMenuExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const renderMenuTree = (nodes: any[], level: number = 0): React.ReactNode => {
    const list = Array.isArray(nodes) ? nodes : [];
    return (
      <div className={level === 0 ? 'space-y-1' : 'space-y-1'}>
        {list.map((n: any, idx: number) => {
          const id = String(n?.Id ?? n?.Name ?? `menu_${level}_${idx}`);
          const name = String(n?.Name ?? 'Menu');
          const children = (n?.AppListMenu_List || n?.Children || []) as any[];
          const hasChildren = Array.isArray(children) && children.length > 0;
          const expanded = menuExpanded.has(id);
          const leafPaletteId = `menu:${id}`;

          if (hasChildren) {
            return (
              <div key={id} className="select-none">
                <button
                  className={`w-full flex items-center justify-between px-2 py-1 rounded hover:bg-slate-50 dark:hover:bg-slate-800 ${theme.label}`}
                  onClick={() => toggleMenuNode(id)}
                  title={name}
                  type="button"
                >
                  <span className="text-xs truncate" style={{ paddingLeft: level * 12 }}>
                    {name}
                  </span>
                  <i className={`fa ${expanded ? 'fa-chevron-down' : 'fa-chevron-right'} text-[10px]`} aria-hidden="true"></i>
                </button>
                {expanded && <div className="mt-1">{renderMenuTree(children, level + 1)}</div>}
              </div>
            );
          }

          // leaf
          return (
            <div
              key={id}
              draggable
              onDragStart={(e) => {
                const payload: DragPayload = { kind: 'palette', paletteId: leafPaletteId };
                const data = JSON.stringify(payload);
                e.dataTransfer.setData('text/plain', data);
                e.dataTransfer.setData('text', data);
                try {
                  e.dataTransfer.setData('application/json', data);
                } catch {
                  // ignore
                }
                e.dataTransfer.effectAllowed = 'copyMove';
              }}
              className="px-2 py-1 border border-slate-200 dark:border-slate-700 rounded-[4px] cursor-move hover:bg-slate-50 dark:hover:bg-slate-800"
              title="Drag into a cell"
            >
              <div className={`text-xs font-semibold ${theme.title} truncate`} style={{ paddingLeft: level * 12 }}>
                {name}
              </div>
            </div>
          );
        })}
      </div>
    );
  };

  const renderRowsCanvas = (path: ContainerPath, rws: RowContainer[], depth: number = 0): React.ReactNode => {
    const pKey = pathKey(path);
    const samePath = (a: ContainerPath, b: ContainerPath) => pathKey(a) === pathKey(b);

    return (
      <div className={depth > 0 ? 'pl-2 border-l border-slate-200 dark:border-slate-700' : ''}>
        <div className="space-y-3">
                    {(rws || []).map((row, rowIdx) => {
            const rowIdRaw = row?.Id as any;
            const rowId = rowIdRaw != null ? String(rowIdRaw) : '';
            const rowKey = rowId && rowId !== 'undefined' ? rowId : `row_${rowIdx}`;
            const totalSpan = sumRowColSpan(row);
            const _isSelectedRow = selectedNode?.kind === 'row' && samePath(selectedNode.path, path) && String(selectedNode.rowId) === rowId;

            // Row Height: apply only to the row wrapper so design panel reflects the value; cells/widgets unchanged
            const rowHeightRaw = row?.RowHeight ?? 'auto';
            const rowHeightStyle: React.CSSProperties | undefined = (() => {
              if (rowHeightRaw === 'auto' || rowHeightRaw === '' || rowHeightRaw == null) return undefined;
              if (typeof rowHeightRaw === 'number' && Number.isFinite(rowHeightRaw)) return { height: `${rowHeightRaw}px` };
              const s = String(rowHeightRaw).trim();
              if (!s) return undefined;
              if (/^\d+$/.test(s)) return { height: `${s}px` };
              return { height: s };
            })();

            return (
              <div
                key={`${pKey}:${rowKey}`}
                className="border rounded-[6px] border-slate-200 dark:border-slate-700 relative group/row"
                style={rowHeightStyle}
                onClick={() => setSelectedNode({ kind: 'row', path, rowId })}
              >
                {/* Insert row controls (Angular-style) */}
                {/* double-line indicator */}
                <div className="absolute -left-3 right-0 -top-2 h-3 opacity-0 group-hover/row:opacity-100 transition-opacity pointer-events-none z-20">
                  <div className="absolute left-0 right-0 top-1 border-t border-slate-400/80 dark:border-slate-500/70" />
                  <div className="absolute left-0 right-0 top-2.5 border-t border-slate-400/80 dark:border-slate-500/70" />
                </div>
                <button
                  className="absolute -left-3 -top-3 w-6 h-6 rounded-full bg-green-500 text-white shadow flex items-center justify-center opacity-0 group-hover/row:opacity-100 transition-opacity z-30"
                  title="Insert new row"
                  onClick={(e) => {
                    e.stopPropagation();
                    insertRowRelativeAt(path, rowId, 'above');
                  }}
                >
                  <i className="fa fa-plus text-[12px]" aria-hidden="true"></i>
                </button>
                {rowIdx === (rws || []).length - 1 && (
                  <>
                    <div className="absolute -left-3 right-0 -bottom-2 h-3 opacity-0 group-hover/row:opacity-100 transition-opacity pointer-events-none z-20">
                      <div className="absolute left-0 right-0 top-0.5 border-t border-slate-400/80 dark:border-slate-500/70" />
                      <div className="absolute left-0 right-0 top-2 border-t border-slate-400/80 dark:border-slate-500/70" />
                    </div>
                  <button
                    className="absolute -left-3 -bottom-3 w-6 h-6 rounded-full bg-green-500 text-white shadow flex items-center justify-center opacity-0 group-hover/row:opacity-100 transition-opacity z-30"
                    title="Insert new row"
                    onClick={(e) => {
                      e.stopPropagation();
                      insertRowRelativeAt(path, rowId, 'below');
                    }}
                  >
                    <i className="fa fa-plus text-[12px]" aria-hidden="true"></i>
                  </button>
                  </>
                )}
                <div className="w-full h-full p-2">
                  {/* Use Angular CSS: TotalColumnsX + ColSpanY width math */}
                  <div className={`Ctn-HorizontalFlexDiv TotalColumns${Math.max(1, totalSpan)}`}>
                    {(row.ChildDesktopItems || []).map((col, colIdx) => {
                      const colIdRaw = col?.Id as any;
                      const colId = colIdRaw != null ? String(colIdRaw) : '';
                      const colKey = colId && colId !== 'undefined' ? colId : `col_${rowKey}_${colIdx}`;
                      const hoverKey = `${pKey}|${rowKey}|${colKey}`;
                      const refKey = `${pKey}|${rowId}|${colId}`;
                      const span = Math.max(1, Math.min(24, Number(col.ColSpanValue ?? col.ColumnSpan ?? 24) || 24));
                      const widget = col.DesktopWidget || null;
                      const nestedRoot = getNestedResponsiveRoot(col);

                      const _isSelectedCol =
                        selectedNode?.kind === 'col' &&
                        samePath(selectedNode.path, path) &&
                        String(selectedNode.rowId) === rowId &&
                        String(selectedNode.colId) === colId;
                      const _isSelectedWidget =
                        selectedNode?.kind === 'widget' &&
                        samePath(selectedNode.path, path) &&
                        String(selectedNode.rowId) === rowId &&
                        String(selectedNode.colId) === colId &&
                        widget &&
                        String(widget.Id) === String(selectedNode.widgetId);

                      return (
                        <div
                          key={`${pKey}:${rowKey}:${colKey}`}
                          className={`CellContainerControl ColSpan${span} rounded-[6px] border border-slate-200 dark:border-slate-700 p-2 flex flex-col gap-2 relative group/cell transition-colors ${
                            dragOverCellKey === hoverKey ? 'border-dashed border-blue-400' : 'hover:border-dashed hover:border-blue-400'
                          }`}
                          data-cell-key={hoverKey}
                          style={col.MinWidth != null ? { minWidth: `${Number(col.MinWidth)}px` } : undefined}
                          ref={(el) => {
                            cellElByKeyRef.current[refKey] = el;
                          }}
                          onClick={(e) => {
                            e.stopPropagation();
                            setSelectedNode({ kind: 'col', path, rowId, colId });
                          }}
                          onContextMenu={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            openCellContextMenu({ path, rowId, colId }, e.clientX, e.clientY);
                          }}
                          onDragOver={(e) => {
                            e.preventDefault();
                            const allowed = String((e.dataTransfer as any)?.effectAllowed || '').toLowerCase();
                            if (allowed.includes('copy') && !allowed.includes('move')) {
                              e.dataTransfer.dropEffect = 'copy';
                            } else if (allowed.includes('move') || allowed.includes('copymove') || allowed === 'all') {
                              e.dataTransfer.dropEffect = 'move';
                            } else if (allowed.includes('copy')) {
                              e.dataTransfer.dropEffect = 'copy';
                            }
                            if (dragOverCellKey !== hoverKey) setDragOverCellKey(hoverKey);
                            e.stopPropagation();
                          }}
                          onDragEnter={(e) => {
                            e.preventDefault();
                            if (dragOverCellKey !== hoverKey) setDragOverCellKey(hoverKey);
                            e.stopPropagation();
                          }}
                          onDragLeave={(e) => {
                            // Only clear when leaving the cell entirely (not moving between children)
                            const related = (e as any).relatedTarget as HTMLElement | null;
                            const stillInside = related?.closest ? related.closest(`[data-cell-key="${hoverKey}"]`) : null;
                            if (!stillInside && dragOverCellKey === hoverKey) setDragOverCellKey(null);
                            e.stopPropagation();
                          }}
                          onDrop={(e) => {
                            e.preventDefault();
                            if (dragOverCellKey === hoverKey) setDragOverCellKey(null);
                            const raw =
                              e.dataTransfer.getData('application/json') ||
                              e.dataTransfer.getData('text/plain') ||
                              e.dataTransfer.getData('text');
                            const payload = tryParseDragPayload(raw);
                            if (!payload) return;

                            if (payload.kind === 'palette') {
                              // File Drop Box is not implemented yet - do nothing on drop
                              if (payload.paletteId === 'fileDropBox') return;
                              const item = palette.find((p) => p.id === payload.paletteId);
                              if (!item) return;
                              if (item.kind === 'container') {
                                const ctn = item.build();
                                setNestedContainerAt(path, rowId, colId, ctn);
                                setSelectedNode({ kind: 'col', path, rowId, colId });
                                return;
                              }
                              const w = item.build();
                              setWidgetAt(path, rowId, colId, w);
                              setSelectedNode({ kind: 'widget', path, rowId, colId, widgetId: String(w.Id) });
                              return;
                            }

                            if (payload.kind === 'widget') {
                              // If dropping onto a container cell, ignore (widget <-> container swap not supported).
                              if (nestedRoot) return;

                              const sameCell =
                                pathKey(payload.from.path) === pathKey(path) &&
                                String(payload.from.rowId) === String(rowId) &&
                                String(payload.from.colId) === String(colId);
                              if (sameCell) return;

                              const toAddr: CellAddress = { path, rowId, colId };
                              mutateOuterRows((outerRows) => {
                                const targetCol = getColInOuterRows(outerRows, toAddr);
                                if (targetCol && hasNestedContainer(targetCol as any)) return outerRows;
                                const targetWidget = (targetCol as any)?.DesktopWidget ?? null;

                                let next = outerRows;
                                // Swap when target already has a widget; otherwise cut/paste into empty cell.
                                next = setWidgetInOuterRows(next, payload.from, targetWidget ? (targetWidget as any) : null);
                                next = setWidgetInOuterRows(next, toAddr, payload.widget);
                                return next;
                              });
                              setSelectedNode({ kind: 'widget', path, rowId, colId, widgetId: String(payload.widget.Id) });
                            }
                          }}
                        >
                          {/* Drag-over dashed highlight (always visible) */}
                          {dragOverCellKey === hoverKey && !(col as any).__editorGhost && (
                            <div className="absolute inset-0 rounded-[6px] border-2 border-dashed border-blue-400 pointer-events-none z-20" />
                          )}

                          {/* Visual rowSpan overlay for vertical merge */}
                          {(col as any).__editorMerge && mergeOverlayExtraPx[`${pKey}|${rowId}|${colId}`] != null && (
                            <div
                              className={`absolute left-0 top-0 w-full rounded-[6px] border border-slate-200 dark:border-slate-700 ${theme.mainContentSection}`}
                              style={{
                                height: `calc(100% + ${mergeOverlayExtraPx[`${pKey}|${rowId}|${colId}`]}px)`,
                                zIndex: 0,
                                pointerEvents: 'none',
                              }}
                            />
                          )}

                          {/* Ghost cell (merged into another) should disappear visually */}
                          {(col as any).__editorGhost && (
                            <div className="absolute inset-0 bg-transparent" style={{ pointerEvents: 'none', zIndex: 1 }} />
                          )}

                          {/* Cell context menu button (Angular-style: show on hover, bottom-right) */}
                          {!(col as any).__editorGhost && (
                            <>
                              {hoveredCellKey === hoverKey && (
                                <button
                                  className="absolute bottom-2 right-2 w-7 h-6 bg-slate-500 text-white rounded-[4px] text-xs hover:bg-slate-600 flex items-center justify-center z-30"
                                  title="Cell Menu"
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    openCellContextMenu({ path, rowId, colId }, e.clientX, e.clientY);
                                  }}
                                >
                                  <i className="fa fa-ellipsis-h" aria-hidden="true"></i>
                                </button>
                              )}
                            </>
                          )}

                          {!widget && !nestedRoot && !(col as any).__editorGhost && (
                            <div className="h-full flex-auto min-h-[50px] border border-dashed border-slate-300 dark:border-slate-600 rounded-[6px] flex items-center justify-center">
                              <div className={`text-[11px] ${theme.label}`}>Drop widget here</div>
                            </div>
                          )}

                          {nestedRoot && !(col as any).__editorGhost && (
                            <div className="h-full rounded-[6px] border border-slate-200 dark:border-slate-700 p-1.5">
                              {renderRowsCanvas([...path, { rowId, colId }], nestedRoot.ChildDesktopItems || [], depth + 1)}
                            </div>
                          )}

                          {widget && activeTab === 'design' && !(col as any).__editorGhost && (
                            <div
                              className={`h-full rounded-[6px] border ${theme.mainContentSection} overflow-hidden relative group/widget cursor-move`}
                              draggable
                              onDragStart={(e) => {
                                const payload: DragPayload = { kind: 'widget', from: { path, rowId, colId }, widget };
                                const data = JSON.stringify(payload);
                                e.dataTransfer.setData('text/plain', data);
                                e.dataTransfer.setData('text', data);
                                try {
                                  e.dataTransfer.setData('application/json', data);
                                } catch {
                                  // ignore
                                }
                                e.dataTransfer.effectAllowed = 'move';
                                e.stopPropagation();
                              }}
                              onClick={(e) => {
                                e.stopPropagation();
                                setSelectedNode({ kind: 'widget', path, rowId, colId, widgetId: String(widget.Id) });
                              }}
                            >
                              {/* runtime widget preview, but non-interactive */}
                              <div className="w-full h-full pointer-events-none">
                                <DashboardWidgetRenderer
                                  item={widget}
                                  theme={theme as any}
                                  t={t as any}
                                  onInternalShortcut={handleInternalShortcut}
                                  onSearchShortcut={handleSearchShortcut}
                                />
                              </div>
                              {/* transparent shield (still keeps menu button usable via z-index) */}
                              <div className="absolute inset-0 bg-transparent pointer-events-none z-10" />

                              {/* Hover drag affordance (center icon); whole widget is draggable */}
                              <div className="absolute inset-0 flex items-center justify-center opacity-0 group-hover/widget:opacity-100 transition-opacity pointer-events-none z-20">
                                <div className="w-10 h-10 rounded-full bg-black/40 text-white flex items-center justify-center">
                                  <i className="fa fa-arrows" aria-hidden="true"></i>
                                </div>
                              </div>
                            </div>
                          )}

                          {widget && activeTab === 'preview' && (
                            <div
                              className={`h-full rounded-[6px] border ${theme.mainContentSection} overflow-hidden`}
                              style={{ height: '220px' }}
                              onClick={(e) => {
                                e.stopPropagation();
                                setSelectedNode({ kind: 'widget', path, rowId, colId, widgetId: String(widget.Id) });
                              }}
                            >
                              <DashboardWidgetRenderer
                                item={widget}
                                theme={theme as any}
                                t={t as any}
                                onInternalShortcut={handleInternalShortcut}
                                onSearchShortcut={handleSearchShortcut}
                              />
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="p-5 text-center">
        <BusyLoader />
      </div>
    );
  }

  if (error && !dashboardData) {
    return (
      <div className={`p-5 ${theme.mainContentSection}`}>
        <div className="text-sm text-red-500">Error: {error}</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header / Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className="flex items-center gap-2">
          <div className={`text-md font-semibold ${theme.title}`}>
            {isNewDashboard ? 'New Dashboard' : 'Dashboard Editor'}
          </div>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleRefresh}
            disabled={saving || loading}
            className={`px-3 py-1 rounded text-xs ${theme.button_default} disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1`}
            title="Refresh"
          >
            <i className="fa fa-refresh" aria-hidden="true"></i>
            Refresh
          </button>

          <button
            onClick={handleSave}
            disabled={saving}
            className={`px-3 py-1 rounded text-xs ${theme.button_default} disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1`}
          >
            <i className="fa fa-save" aria-hidden="true"></i>
            {saving ? 'Saving...' : 'Save'}
          </button>

          {dashboardData?.LayoutType !== 2 && (
            <button
              onClick={convertCanvasToFlex}
              className="px-3 py-1 rounded text-xs bg-orange-500 text-white hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
              title="Convert Canvas to Flex"
              disabled={saving}
            >
              <i className="fa fa-exchange" aria-hidden="true"></i>
              Convert
            </button>
          )}

          <button
            onClick={handleInitializeLayout}
            disabled={saving || loading}
            className={`px-3 py-1 rounded text-xs ${theme.button_default} disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1`}
            title="Initialize Layout"
          >
            <i className="fa fa-th" aria-hidden="true"></i>
            Initialize Layout
          </button>

          {dashboardData?.Id && (
            <button
              onClick={handleSaveAs}
              disabled={saving}
              className={`px-3 py-1 rounded text-xs ${theme.button_default} disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1`}
              title="Save As"
            >
              <i className="fa fa-copy" aria-hidden="true"></i>
              Save As
            </button>
          )}
        </div>
      </div>

      {/* Initialize Layout popup (Angular-style: set rows/cols then reset) */}
      {initLayoutPopup.open && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
          onClick={(e) => e.stopPropagation()}
        >
          <div
            className={`${theme.mainContentSection} border rounded-lg shadow-xl p-4 w-[280px]`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`text-sm font-semibold ${theme.title} mb-3`}>Initialize Layout</div>
            <div className="space-y-3">
              <div className="flex items-center justify-between gap-2">
                <label className={`text-xs ${theme.label}`}>Rows</label>
                <input
                  type="number"
                  min={1}
                  max={100}
                  value={initLayoutPopup.rows}
                  onChange={(e) =>
                    setInitLayoutPopup((p) => ({
                      ...p,
                      rows: Math.max(1, Math.min(100, parseInt(e.target.value, 10) || 1)),
                    }))
                  }
                  className={`w-20 h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                />
              </div>
              <div className="flex items-center justify-between gap-2">
                <label className={`text-xs ${theme.label}`}>Columns</label>
                <input
                  type="number"
                  min={1}
                  max={24}
                  value={initLayoutPopup.cols}
                  onChange={(e) =>
                    setInitLayoutPopup((p) => ({
                      ...p,
                      cols: Math.max(1, Math.min(24, parseInt(e.target.value, 10) || 1)),
                    }))
                  }
                  className={`w-20 h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                />
              </div>
            </div>
            <div className="flex justify-end gap-2 mt-4">
              <button
                type="button"
                onClick={handleInitLayoutCancel}
                className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleInitLayoutConfirm}
                className="px-3 py-1 rounded text-xs bg-blue-500 text-white hover:bg-blue-600"
              >
                OK
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Editor Content */}
      <div className={`w-full flex-auto h-1 overflow-hidden ${theme.mainContentSection}`}>
        <div className="h-full w-full overflow-hidden flex">
          {/* Left toolbox (Angular-style) */}
          {showToolbox ? (
            <div className="w-[320px] flex-shrink-0 border-r border-slate-200 dark:border-slate-700 overflow-hidden flex flex-col">
              <div className="px-3 py-2 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between">
                <div className={`text-xs font-semibold ${theme.title}`}>Toolbox</div>
                <button
                  className="w-7 h-6 bg-slate-500 text-white rounded-[4px] text-xs hover:bg-slate-600 flex items-center justify-center"
                  title="Hide Toolbox"
                  onClick={() => setShowToolbox(false)}
                >
                  <i className="fa fa-chevron-left" aria-hidden="true"></i>
                </button>
              </div>

              <div className="flex-auto overflow-auto px-3 py-3 space-y-4">
                {error && (
                  <div className="rounded border border-red-200 bg-red-50 text-red-600 text-xs px-3 py-2">
                    {error}
                  </div>
                )}

                
                <div>
                  <button
                    type="button"
                    className="w-full flex items-center justify-between py-1"
                    onClick={() => toggleToolboxSection('properties')}
                  >
                    <div className={`text-xs font-semibold ${theme.title}`}>Dashboard Setting</div>
                    <i className={`fa ${toolboxExpanded.properties ? 'fa-chevron-down' : 'fa-chevron-right'} text-[10px]`} aria-hidden="true"></i>
                  </button>
                  {toolboxExpanded.properties && (
                    <>
                      {!selectedNode && (
                        <div className="space-y-3 py-4">
                          <div>
                            <div className={`text-xs ${theme.label} mb-1`}>Dashboard Name</div>
                            <input
                              type="text"
                              value={dashboardData?.DesktopName || ''}
                              onChange={handleNameChange}
                              className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                            />
                          </div>
                          <div>
                            <div className={`text-xs ${theme.label} mb-1`}>Description</div>
                            <input
                              type="text"
                              value={String((dashboardData as any)?.Description ?? (dashboardData as any)?.DesktopDescription ?? '')}
                              onChange={handleDescriptionChange}
                              placeholder="Description"
                              className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                            />
                          </div>
                        </div>
                      )}

                  {selected?.row && selectedNode?.kind === 'row' && (
                    <div>
                      <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Row</div>
                      <div className="flex items-center py-1">
                        <label className={`w-28 text-xs ${theme.label} mr-2`}>RowHeight</label>
                        <input
                          value={String(selected.row.RowHeight ?? '')}
                          onChange={(e) => {
                            const raw = e.target.value;
                            const asNum = Number(raw);
                            const nextVal: any =
                              raw.trim() === '' ? 'auto' : Number.isFinite(asNum) && !Number.isNaN(asNum) ? asNum : raw;
                            setRowHeightAt(selectedNode.path, selectedNode.rowId, nextVal);
                          }}
                          className={`flex-auto h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                          placeholder="auto / 300 / 300px"
                        />
                      </div>
                      <div className="flex items-start py-1">
                        <label className={`w-28 text-xs ${theme.label} mr-2 pt-1`}>Style</label>
                        <textarea
                          value={String(selected.row.StyleLayoutInfo ?? '')}
                          onChange={(e) => setRowStyleLayoutInfoAt(selectedNode.path, selectedNode.rowId, e.target.value)}
                          className={`flex-auto h-20 px-2 py-1 text-xs border rounded ${theme.inputBox} resize-none focus:outline-none`}
                          placeholder="e.g. gap:10px;"
                        />
                      </div>
                    </div>
                  )}

                  {selected?.col && selectedNode?.kind === 'col' && (
                    <div>
                      <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Cell</div>
                      <div className="flex items-center py-1">
                        <label className={`w-28 text-xs ${theme.label} mr-2`}>Content</label>
                        <select
                          value={hasNestedContainer(selected.col as any) ? 'container' : 'widget'}
                          onChange={(e) => {
                            if (e.target.value === 'container') {
                              setNestedContainerAt(selectedNode.path, selectedNode.rowId, selectedNode.colId, createDefaultResponsiveRoot(createDefaultFlexRows()));
                            } else {
                              setNestedContainerAt(selectedNode.path, selectedNode.rowId, selectedNode.colId, null);
                            }
                          }}
                          className={`flex-auto h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                        >
                          <option value="widget">Widget</option>
                          <option value="container">Responsive Container</option>
                        </select>
                      </div>
                      <div className="flex items-center py-1">
                        <label className={`w-28 text-xs ${theme.label} mr-2`}>ColSpan</label>
                        <input
                          type="number"
                          min={1}
                          max={24}
                          value={Number(selected.col.ColSpanValue ?? selected.col.ColumnSpan ?? 24)}
                          onChange={(e) => setColumnSpanAt(selectedNode.path, selectedNode.rowId, selectedNode.colId, Number(e.target.value))}
                          className={`w-20 h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                        />
                        <div className={`text-[11px] ml-2 ${theme.label}`}>1–24</div>
                      </div>
                      <div className="flex items-center py-1">
                        <label className={`w-28 text-xs ${theme.label} mr-2`}>MinWidth</label>
                        <input
                          type="number"
                          min={150}
                          value={Number(selected.col.MinWidth ?? 300)}
                          onChange={(e) => setColumnMinWidthAt(selectedNode.path, selectedNode.rowId, selectedNode.colId, Number(e.target.value))}
                          className={`w-24 h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                        />
                        <div className={`text-[11px] ml-2 ${theme.label}`}>px</div>
                      </div>
                      <div className="flex items-start py-1">
                        <label className={`w-28 text-xs ${theme.label} mr-2 pt-1`}>Style</label>
                        <textarea
                          value={String(selected.col.StyleLayoutInfo ?? '')}
                          onChange={(e) => setColumnStyleLayoutInfoAt(selectedNode.path, selectedNode.rowId, selectedNode.colId, e.target.value)}
                          className={`flex-auto h-20 px-2 py-1 text-xs border rounded ${theme.inputBox} resize-none focus:outline-none`}
                          placeholder="e.g. padding:8px;"
                        />
                      </div>
                    </div>
                  )}

                    </>
                  )}
                </div>

                <div>
                  <button
                    type="button"
                    className="w-full flex items-center justify-between py-1"
                    onClick={() => toggleToolboxSection('builtIn')}
                  >
                    <div className={`text-xs font-semibold ${theme.title}`}>Built-in Widgets</div>
                    <i className={`fa ${toolboxExpanded.builtIn ? 'fa-chevron-down' : 'fa-chevron-right'} text-[10px]`} aria-hidden="true"></i>
                  </button>
                  {toolboxExpanded.builtIn && (
                    <div className="space-y-2 py-4">
                      {palette
                        .filter((p) => !String(p.id).startsWith('menu:') && p.kind === 'widget')
                        .map((p) => (
                          <div
                            key={p.id}
                            draggable
                            onDragStart={(e) => {
                              const payload: DragPayload = { kind: 'palette', paletteId: p.id };
                              const data = JSON.stringify(payload);
                              e.dataTransfer.setData('text/plain', data);
                              e.dataTransfer.setData('text', data);
                              try {
                                e.dataTransfer.setData('application/json', data);
                              } catch {
                                // ignore
                              }
                              e.dataTransfer.effectAllowed = 'copyMove';
                            }}
                            className="px-2 py-2 border border-slate-200 dark:border-slate-700 rounded-[4px] cursor-move hover:bg-slate-50 dark:hover:bg-slate-800 flex items-center gap-2"
                            title="Drag into a cell"
                          >
                            <i className="fa fa-windows text-[12px] text-slate-600 dark:text-slate-300" aria-hidden="true"></i>
                            <div className={`text-xs ${theme.title}`}>{p.name}</div>
                          </div>
                        ))}
                    </div>
                  )}
                </div>

                <div>
                  <button
                    type="button"
                    className="w-full flex items-center justify-between py-1"
                    onClick={() => toggleToolboxSection('appMenu')}
                  >
                    <div className={`text-xs font-semibold ${theme.title}`}>App Menu Page Widgets</div>
                    <i className={`fa ${toolboxExpanded.appMenu ? 'fa-chevron-down' : 'fa-chevron-right'} text-[10px]`} aria-hidden="true"></i>
                  </button>
                  {toolboxExpanded.appMenu && (
                    <div className="space-y-2 py-4">
                      <label className="flex items-center gap-2">
                        <input type="checkbox" className="w-4 h-4" checked={addAsShortcut} onChange={(e) => setAddAsShortcut(e.target.checked)} />
                        <span className={`text-xs ${theme.label}`}>Add as Shortcut</span>
                      </label>
                      <div className="max-h-[280px] overflow-auto pr-1">{renderMenuTree(userMenu || [])}</div>
                    </div>
                  )}
                </div>

                <div>
                  <button
                    type="button"
                    className="w-full flex items-center justify-between py-1"
                    onClick={() => toggleToolboxSection('folder')}
                  >
                    <div className={`text-xs font-semibold ${theme.title}`}>Folder Widgets</div>
                    <i className={`fa ${toolboxExpanded.folder ? 'fa-chevron-down' : 'fa-chevron-right'} text-[10px]`} aria-hidden="true"></i>
                  </button>
                  {toolboxExpanded.folder && <div className="max-h-[280px] overflow-auto pr-1">{renderMenuTree(userMenu || [])}</div>}
                </div>
              </div>
            </div>
          ) : (
            <div className="w-[42px] flex-shrink-0 border-r border-slate-200 dark:border-slate-700 flex items-start justify-center pt-2">
              <button
                className="w-8 h-6 bg-slate-500 text-white rounded-[4px] text-xs hover:bg-slate-600 flex items-center justify-center"
                title="Show Toolbox"
                onClick={() => setShowToolbox(true)}
              >
                <i className="fa fa-chevron-right" aria-hidden="true"></i>
              </button>
            </div>
          )}

          {/* Right: Canvas */}
          <div className="flex-auto w-1 overflow-hidden flex flex-col">
            <div className="flex-auto overflow-auto p-3">
              {activeTab === 'json' && (
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <button
                      className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center justify-center"
                      title="Apply JSON"
                      onClick={applyJsonDraft}
                    >
                      <i className="fa fa-check" aria-hidden="true"></i>
                    </button>
                    <div className={`text-[11px] ${theme.label}`}>Edit JSON and apply to update layout.</div>
                  </div>
                  <textarea
                    value={jsonDraft}
                    onChange={(e) => setJsonDraft(e.target.value)}
                    className={`w-full h-[520px] p-2 border rounded ${theme.inputBox} font-mono text-[11px] resize-none`}
                    spellCheck={false}
                  />
                </div>
              )}

              {activeTab !== 'json' && (
                <div
                  className="space-y-3"
                  onMouseMove={(e) => {
                    const target = e.target as any;
                    const el = target?.closest ? (target.closest('[data-cell-key]') as HTMLElement | null) : null;
                    const key = el?.getAttribute('data-cell-key') || null;
                    setHoveredCellKey((prev) => (prev === key ? prev : key));
                  }}
                  onMouseLeave={() => setHoveredCellKey(null)}
                >
                  {renderRowsCanvas([], rows)}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Cell Add Menu */}
      {cellAddMenu?.open && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[220px]`}
          style={{ left: cellAddMenu.x, top: cellAddMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {palette.map((p) => (
            <button
              key={p.id}
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
              onClick={() => {
                // File Drop Box is not implemented yet - do nothing on choose
                if (p.id === 'fileDropBox') {
                  setCellAddMenu(null);
                  return;
                }
                if (p.kind === 'container') {
                  const ctn = p.build();
                  setNestedContainerAt(cellAddMenu.path, cellAddMenu.rowId, cellAddMenu.colId, ctn);
                  setSelectedNode({ kind: 'col', path: cellAddMenu.path, rowId: cellAddMenu.rowId, colId: cellAddMenu.colId });
                  setCellAddMenu(null);
                  return;
                }
                const w = p.build();
                setWidgetAt(cellAddMenu.path, cellAddMenu.rowId, cellAddMenu.colId, w);
                setSelectedNode({
                  kind: 'widget',
                  path: cellAddMenu.path,
                  rowId: cellAddMenu.rowId,
                  colId: cellAddMenu.colId,
                  widgetId: String(w.Id),
                });
                setCellAddMenu(null);
              }}
            >
              <i className="fa fa-plus mr-2" aria-hidden="true"></i>
              {p.name}
            </button>
          ))}
        </div>
      )}

      {/* Cell Context Menu (Angular-style) */}
      {cellContextMenu?.open && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg p-3 w-[300px]`}
          style={{ left: cellContextMenu.x, top: cellContextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {(() => {
            const addr = cellContextMenu.addr;
            const row = getRowAt(addr.path, addr.rowId);
            const col = getColAt(addr.path, addr.rowId, addr.colId);
            const span = Math.max(1, Math.min(24, Number(col?.ColSpanValue ?? col?.ColumnSpan ?? 1) || 1));
            const minW = Math.max(0, Number(col?.MinWidth ?? 0) || 0);
            const totalCols = row ? Math.max(1, sumRowColSpan(row)) : 24;
            const rowHeightRaw: any = row?.RowHeight ?? 'auto';
            const rowHeightNum = Number(rowHeightRaw);
            const rowHeightIsNum = Number.isFinite(rowHeightNum) && !Number.isNaN(rowHeightNum);

            const setSpan = (next: number) => setColumnSpanAt(addr.path, addr.rowId, addr.colId, next);
            const setMinWidth = (next: number) => setColumnMinWidthAt(addr.path, addr.rowId, addr.colId, next);

            return (
              <div className="space-y-3">
                <div className={`text-xs font-semibold ${theme.title}`}>Cell</div>

                <div className="flex items-center justify-between">
                  <div className={`text-xs ${theme.label}`}>Cell Width</div>
                  <div className="flex items-center gap-1">
                    <button
                      className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      onClick={() => setSpan(span - 1)}
                      title="Decrease"
                    >
                      -
                    </button>
                    <div className={`w-10 text-center text-xs ${theme.title}`}>{span}</div>
                    <button
                      className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      onClick={() => setSpan(span + 1)}
                      title="Increase"
                    >
                      +
                    </button>
                    <div className={`text-[11px] ${theme.label}`}>/{totalCols}</div>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                    <div className={`text-xs ${theme.label}`}>Warp Min Width</div>
                  <div className="flex items-center gap-1">
                    <button
                      className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      onClick={() => setMinWidth(minW - 50)}
                      title="Decrease"
                    >
                      -
                    </button>
                    <input
                      className={`w-20 h-6 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                      value={String(minW)}
                      onChange={(e) => setMinWidth(Number(e.target.value))}
                    />
                    <div className={`text-[11px] ${theme.label}`}>px</div>
                    <button
                      className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      onClick={() => setMinWidth(minW + 50)}
                      title="Increase"
                    >
                      +
                    </button>
                  </div>
                </div>

                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="w-4 h-4"
                    onChange={(e) => {
                      if (!e.target.checked) return;
                      const style = String(col?.StyleLayoutInfo ?? '');
                      setRowStyleLayoutInfoAt(addr.path, addr.rowId, style);
                    }}
                  />
                  <span className={`text-xs ${theme.label}`}>Apply Cell Style To Entire Row</span>
                </label>

                <div>
                  <div className={`text-xs ${theme.label} mb-1`}>Merge Cell</div>
                  <div className="flex items-center gap-1">
                    <button
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Merge Left"
                      onClick={() => mergeColumnAt(addr.path, addr.rowId, addr.colId, 'left')}
                    >
                      <i className="fa fa-arrow-left" aria-hidden="true"></i>
                    </button>
                    <button
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Merge Right"
                      onClick={() => mergeColumnAt(addr.path, addr.rowId, addr.colId, 'right')}
                    >
                      <i className="fa fa-arrow-right" aria-hidden="true"></i>
                    </button>
                    <button
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Merge Up"
                      onClick={() => mergeVerticalAt(addr.path, addr.rowId, addr.colId, 'up')}
                    >
                      <i className="fa fa-arrow-up" aria-hidden="true"></i>
                    </button>
                    <button
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Merge Down"
                      onClick={() => mergeVerticalAt(addr.path, addr.rowId, addr.colId, 'down')}
                    >
                      <i className="fa fa-arrow-down" aria-hidden="true"></i>
                    </button>
                  </div>
                </div>

                <div>
                  <div className={`text-xs ${theme.label} mb-1`}>Vertical Split</div>
                  <div className="flex items-center gap-1">
                    <button
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Split into 2"
                      onClick={() => splitColumnAt(addr.path, addr.rowId, addr.colId, 2)}
                    >
                      2
                    </button>
                    <button
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Split into 3"
                      onClick={() => splitColumnAt(addr.path, addr.rowId, addr.colId, 3)}
                    >
                      3
                    </button>
                    <button
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Split into 4"
                      onClick={() => splitColumnAt(addr.path, addr.rowId, addr.colId, 4)}
                    >
                      4
                    </button>
                  </div>
                </div>

                <div>
                  <div className={`text-xs ${theme.label} mb-1`}>Horizontal Insert</div>
                  <div className="flex items-center gap-1">
                    <button
                      className="px-2 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300"
                      onClick={() => insertColumnRelativeAt(addr.path, addr.rowId, addr.colId, 'left')}
                    >
                      Left
                    </button>
                    <button
                      className="px-2 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300"
                      onClick={() => insertColumnRelativeAt(addr.path, addr.rowId, addr.colId, 'right')}
                    >
                      Right
                    </button>
                    <button
                      className="ml-auto w-8 h-7 bg-red-400 text-white rounded-[4px] text-xs hover:bg-red-500 flex items-center justify-center"
                      title="Delete Cell"
                      onClick={() => deleteColumnAt(addr.path, addr.rowId, addr.colId)}
                    >
                      <i className="fa fa-trash" aria-hidden="true"></i>
                    </button>
                  </div>
                </div>

                <div className="pt-2 border-t border-slate-200 dark:border-slate-700">
                  <div className={`text-xs font-semibold ${theme.title}`}>Row</div>
                  <div className="flex items-center justify-between mt-2">
                    <div className={`text-xs ${theme.label}`}>Row Height</div>
                    <div className="flex items-center gap-1">
                      <button
                        className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                        title="Decrease"
                        onClick={() => {
                          const next = rowHeightIsNum ? rowHeightNum - 50 : 300;
                          setRowHeightAt(addr.path, addr.rowId, Math.max(50, next));
                        }}
                      >
                        -
                      </button>
                      <input
                        className={`w-20 h-6 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                        value={String(row?.RowHeight ?? '')}
                        onChange={(e) => {
                          const raw = e.target.value;
                          const asNum = Number(raw);
                          const nextVal: any =
                            raw.trim() === '' ? 'auto' : Number.isFinite(asNum) && !Number.isNaN(asNum) ? asNum : raw;
                          setRowHeightAt(addr.path, addr.rowId, nextVal);
                        }}
                      />
                      <div className={`text-[11px] ${theme.label}`}>px</div>
                      <button
                        className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                        title="Increase"
                        onClick={() => {
                          const next = rowHeightIsNum ? rowHeightNum + 50 : 400;
                          setRowHeightAt(addr.path, addr.rowId, next);
                        }}
                      >
                        +
                      </button>
                    </div>
                  </div>

                  <div className="flex items-center justify-between mt-2">
                    <div className={`text-xs ${theme.label}`}>Insert Row</div>
                    <div className="flex items-center gap-1">
                      <button
                        className="px-2 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300"
                        onClick={() => insertRowRelativeAt(addr.path, addr.rowId, 'above')}
                      >
                        Above
                      </button>
                      <button
                        className="px-2 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300"
                        onClick={() => insertRowRelativeAt(addr.path, addr.rowId, 'below')}
                      >
                        Below
                      </button>
                      <button
                        className="ml-auto w-8 h-7 bg-red-400 text-white rounded-[4px] text-xs hover:bg-red-500 flex items-center justify-center"
                        title="Delete Row"
                        onClick={() => deleteRowAt(addr.path, addr.rowId)}
                      >
                        <i className="fa fa-trash" aria-hidden="true"></i>
                      </button>
                    </div>
                  </div>
                </div>

                <div className="pt-2 border-t border-slate-200 dark:border-slate-700">
                  <div className={`text-xs font-semibold ${theme.title}`}>Content</div>
                  <div className="flex items-center justify-between mt-2">
                    <div className={`text-xs ${theme.label}`}>Clear Content</div>
                    <button
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Clear Content"
                      onClick={() => {
                        clearCellContentAt(addr.path, addr.rowId, addr.colId);
                        setCellContextMenu(null);
                      }}
                    >
                      <i className="fa fa-refresh" aria-hidden="true"></i>
                    </button>
                  </div>
                </div>
              </div>
            );
          })()}
        </div>
      )}
    </div>
  );
};

export default DashboardEditor;
