/**
 * ErDiagramEditor – match Angular: header row, left table list, resizable divider, diagram, all buttons, edge-connected arrows.
 */

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { dbGenieService } from '../../webapi/dbgeniesvc';
import { closeTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import type { RootState } from '../../redux/store';
import TableDataPreview from '../transaction/TableDataPreview';
import MetaDataTableDesign from '../transaction/metaDataTableDesign';

const EmAppDataSetUsageType_ErDiagram = 3;
const DIAGRAM_PAD = 10000;
const MIN_LEFT_PANEL = 180;
const MAX_LEFT_PANEL = 500;

const ErDiagramEditor: React.FC = () => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { addTabAndNavigate } = useTabNavigation();
  const { param } = useParams();
  const tabs = useSelector((state: RootState) => state.tabnav.tabs);
  const activeTabKey = useSelector((state: RootState) => state.tabnav.activeTabKey);
  const previousActiveTabKey = useSelector((state: RootState) => state.tabnav.previousActiveTabKey);

  const [dto, setDto] = useState<any | null>(null);
  const [dataSourceList, setDataSourceList] = useState<{ Id: number; DataSourceName: string }[]>([]);
  const [schemaTableList, setSchemaTableList] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [draggingTable, setDraggingTable] = useState<string | null>(null);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });
  const [showTableContextMenu, setShowTableContextMenu] = useState<string | null>(null);
  const [tableContextMenuPosition, setTableContextMenuPosition] = useState({ x: 0, y: 0 });
  const [showFkRefSubmenu, setShowFkRefSubmenu] = useState<string | null>(null);
  const [showFkRefedSubmenu, setShowFkRefedSubmenu] = useState<string | null>(null);
  const [showPreviewPopup, setShowPreviewPopup] = useState(false);
  const [previewTableInfo, setPreviewTableInfo] = useState<{ tableName: string; dataSourceRegisterId: number | null; schemaOwner: string | null } | null>(null);
  const [showTableDesignPopup, setShowTableDesignPopup] = useState(false);
  const [tableDesignInfo, setTableDesignInfo] = useState<{ tableName: string | null; dataSourceRegisterId: number | null; schemaOwner: string | null; applicationId: number | null } | null>(null);
  const [draggingColumn, setDraggingColumn] = useState<{ sourceTableId: string; sourceColumnId: string } | null>(null);
  const [showJoinContextMenu, setShowJoinContextMenu] = useState(false);
  const [joinContextMenuPosition, setJoinContextMenuPosition] = useState({ x: 0, y: 0 });
  const [currentConditionLine, setCurrentConditionLine] = useState<any | null>(null);
  const [currentJoin, setCurrentJoin] = useState<any | null>(null);
  const [showTableSelectorPopup, setShowTableSelectorPopup] = useState(false);
  const [tableSelectorSearch, setTableSelectorSearch] = useState('');
  const [tableSelectorSelected, setTableSelectorSelected] = useState<Set<string>>(new Set());
  const [createDataModelOpen, setCreateDataModelOpen] = useState(false);
  const [showExportDdlModal, setShowExportDdlModal] = useState(false);
  const [exportDdlScript, setExportDdlScript] = useState<string>('');
  const [leftPanelWidth, setLeftPanelWidth] = useState(300);
  const [isResizing, setIsResizing] = useState(false);
  const resizeStartRef = useRef<{ clientX: number; width: number } | null>(null);
  const resizeBoxStartRef = useRef<{ mouseX: number; mouseY: number; posX: number; posY: number; width: number; height: number } | null>(null);
  const [tableViewNameFilter, setTableViewNameFilter] = useState('');
  const [draggedTableItem, setDraggedTableItem] = useState<{ Key: string | null; Value: string } | null>(null);
  const [editingAnnotationGuid, setEditingAnnotationGuid] = useState<string | null>(null);
  const [draggingAnnotationGuid, setDraggingAnnotationGuid] = useState<string | null>(null);
  const [resizingItem, setResizingItem] = useState<{ type: 'table'; id: string } | { type: 'annotation'; id: string } | null>(null);

  const diagramContainerRef = useRef<HTMLDivElement>(null);
  const diagramScrollRef = useRef<HTMLDivElement>(null);
  const createDataModelRef = useRef<HTMLDivElement>(null);

  const dataSetId: number | null = useMemo(() => {
    if (!param) return null;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      const id = obj.id ?? obj.Id ?? null;
      if (id == null) return null;
      const n = Number(id);
      return Number.isNaN(n) ? null : n;
    } catch {
      const n = Number(param);
      return Number.isNaN(n) ? null : n;
    }
  }, [param]);

  const viewDto = useMemo(() => {
    const info = dto?.OtherSettingsDto?.DatabaseDiagramInfo;
    return info != null
      ? {
          DictTables: info.DictTables ?? {},
          Joins: info.Joins ?? [],
          DictAllColumns: info.DictAllColumns ?? {},
          DictTextAnnotationGuidAndDto: info.DictTextAnnotationGuidAndDto ?? {}
        }
      : { DictTables: {}, Joins: [], DictAllColumns: {}, DictTextAnnotationGuidAndDto: {} };
  }, [dto?.OtherSettingsDto?.DatabaseDiagramInfo]);

  const updateDiagramView = useCallback((updater: (prev: any) => any) => {
    setDto((prev: any) => {
      if (!prev) return prev;
      const next = { ...prev };
      next.OtherSettingsDto = { ...(prev.OtherSettingsDto || {}), DatabaseDiagramInfo: { ...(prev.OtherSettingsDto?.DatabaseDiagramInfo || {}) } };
      const current = next.OtherSettingsDto.DatabaseDiagramInfo;
      const updated = updater({
        DictTables: current.DictTables ?? {},
        Joins: current.Joins ?? [],
        DictAllColumns: current.DictAllColumns ?? {},
        DictTextAnnotationGuidAndDto: current.DictTextAnnotationGuidAndDto ?? {}
      });
      next.OtherSettingsDto.DatabaseDiagramInfo = { ...current, ...updated };
      return next;
    });
  }, []);

  const loadData = useCallback(async () => {
    if (dataSetId != null) {
      dispatch(setIsBusy());
      try {
        const [diagram, dsList] = await Promise.all([
          appTransactionService.RetrieveOneErDiagramExDto(dataSetId),
          adminSvc.getDataSourceRegisterList(false)
        ]);
        setDto(diagram != null ? { ...diagram } : null);
        setDataSourceList(Array.isArray(dsList) ? dsList : []);
      } catch (err) {
        showError('Failed to load ER diagram: ' + (err instanceof Error ? err.message : String(err)));
      } finally {
        dispatch(setIsNotBusy());
        setIsLoading(false);
      }
    } else {
      try {
        const dsList = await adminSvc.getDataSourceRegisterList(false);
        setDataSourceList(Array.isArray(dsList) ? dsList : []);
        setDto({
          Id: null,
          Name: '',
          Description: '',
          DataSourceFrom: dsList?.[0]?.Id ?? null,
          UsageTypeId: EmAppDataSetUsageType_ErDiagram,
          OtherSettingsDto: { DatabaseDiagramInfo: { IsErDiagram: true } }
        });
      } catch (err) {
        showError('Failed to load data sources: ' + (err instanceof Error ? err.message : String(err)));
      } finally {
        setIsLoading(false);
      }
    }
  }, [dataSetId, dispatch, showError]);

  const loadSchemaTables = useCallback(async () => {
    const dsId = dto?.DataSourceFrom;
    if (dsId == null) {
      setSchemaTableList([]);
      return;
    }
    try {
      dispatch(setIsBusy());
      const data = await schemaMetadataService.getDataSourceTableAndViewList(dsId, null, null);
      setSchemaTableList(
        (Array.isArray(data) ? data : []).map((item: any) => ({
          ...item,
          uiDisplayName: item.SchemaOwner ? `${item.SchemaOwner}.${item.Name}` : item.Name
        }))
      );
    } catch {
      setSchemaTableList([]);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dto?.DataSourceFrom, dispatch]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    if (dto?.DataSourceFrom != null) loadSchemaTables();
    else setSchemaTableList([]);
  }, [dto?.DataSourceFrom]);

  useEffect(() => {
    setTableViewNameFilter('');
  }, [dto?.DataSourceFrom]);

  const filteredSchemaTableList = useMemo(() => {
    const nameFilter = tableViewNameFilter.trim().toLowerCase();
    if (!nameFilter) return schemaTableList;
    return schemaTableList.filter((t: any) => {
      const displayName = (t.uiDisplayName ?? (t.SchemaOwner ? `${t.SchemaOwner}.${t.Name}` : t.Name) ?? '').toLowerCase();
      const bareName = String(t.Name ?? t.TableName ?? '').toLowerCase();
      return displayName.includes(nameFilter) || bareName.includes(nameFilter);
    });
  }, [schemaTableList, tableViewNameFilter]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (createDataModelRef.current && !createDataModelRef.current.contains(e.target as Node)) setCreateDataModelOpen(false);
      if (showTableContextMenu) setShowTableContextMenu(null);
      if (showFkRefSubmenu) setShowFkRefSubmenu(null);
      if (showFkRefedSubmenu) setShowFkRefedSubmenu(null);
      if (showJoinContextMenu) {
        const joinMenu = document.getElementById('er-join-context-menu');
        if (!joinMenu?.contains(e.target as Node)) {
          setShowJoinContextMenu(false);
          setCurrentConditionLine(null);
          setCurrentJoin(null);
        }
      }
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, [showTableContextMenu, showFkRefSubmenu, showFkRefedSubmenu, showJoinContextMenu]);

  useEffect(() => {
    if (!isResizing) return;
    const onMove = (e: MouseEvent) => {
      const start = resizeStartRef.current;
      if (start) {
        const delta = e.clientX - start.clientX;
        const w = Math.max(MIN_LEFT_PANEL, Math.min(MAX_LEFT_PANEL, start.width + delta));
        setLeftPanelWidth(w);
      }
    };
    const onUp = () => {
      resizeStartRef.current = null;
      setIsResizing(false);
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    return () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
  }, [isResizing]);

  const handleFieldChange = (field: string, value: any) => {
    setDto((prev: any) => (prev ? { ...prev, [field]: value } : null));
  };

  /** Join lines store LeftSideTable/RightSideTable from the server; keys may differ in casing or use bare TableName vs schema-qualified DictTables key. */
  const resolveDiagramTableKey = useCallback(
    (tableRef: string): string | null => {
      const dict = viewDto.DictTables ?? {};
      const ref = (tableRef ?? '').trim();
      if (!ref) return null;
      const lower = ref.toLowerCase();
      const keys = Object.keys(dict);
      for (const k of keys) {
        if (k.toLowerCase() === lower) return k;
      }
      for (const k of keys) {
        const tn = ((dict[k] as any)?.TableName ?? '').toLowerCase();
        if (tn && tn === lower) return k;
      }
      const dot = lower.lastIndexOf('.');
      const bare = dot >= 0 ? lower.slice(dot + 1) : lower;
      for (const k of keys) {
        if (k.toLowerCase().endsWith(`.${bare}`)) return k;
        const tn = ((dict[k] as any)?.TableName ?? '').toLowerCase();
        if (tn === bare) return k;
      }
      return null;
    },
    [viewDto.DictTables]
  );

  const getTablePosition = useCallback(
    (tableRef: string) => {
      const def = { x: 0, y: 0, width: 200, height: 200 };
      const key = resolveDiagramTableKey(tableRef);
      const t = key ? viewDto.DictTables[key] : undefined;
      if (t) {
        def.x = t.PositionX ?? 0;
        def.y = t.PositionY ?? 0;
        def.width = t.Width ?? 200;
        def.height = t.Height ?? 200;
      }
      return def;
    },
    [viewDto.DictTables, resolveDiagramTableKey]
  );

  const TABLE_HEADER_HEIGHT = 40;
  const TABLE_ROW_HEIGHT = 20.5;
  const FK_LINE_EDGE_OFFSET = 5; // min distance from table bottom (lowest Y)

  const getColumnCenterY = useCallback(
    (tableRef: string, columnName: string) => {
      const key = resolveDiagramTableKey(tableRef);
      const cols = key ? viewDto.DictAllColumns?.[key.toLowerCase()] : undefined;
      if (!cols || !columnName) return null;
      const colNames = Object.keys(cols);
      const idx = colNames.findIndex((c) => c.toLowerCase() === columnName.toLowerCase());
      if (idx < 0) return null;
      return TABLE_HEADER_HEIGHT + (idx + 0.5) * TABLE_ROW_HEIGHT;
    },
    [viewDto.DictAllColumns, resolveDiagramTableKey]
  );

  /** Line from table edge to table edge, connecting at column center Y (avoids header offset). */
  const getLineCoordinate = useCallback(
    (join: any, line: any) => {
      const leftPos = getTablePosition(line.LeftSideTable);
      const rightPos = getTablePosition(line.RightSideTable);
      const leftCenterX = leftPos.x + leftPos.width / 2;
      const rightCenterX = rightPos.x + rightPos.width / 2;
      const leftColY = getColumnCenterY(line.LeftSideTable, line.LeftSideColumn);
      const rightColY = getColumnCenterY(line.RightSideTable, line.RightSideColumn);
      const leftBottomMaxY = leftPos.y + leftPos.height - FK_LINE_EDGE_OFFSET;
      const rightBottomMaxY = rightPos.y + rightPos.height - FK_LINE_EDGE_OFFSET;
      let leftCenterY = leftColY != null ? leftPos.y + leftColY : leftPos.y + leftPos.height / 2;
      let rightCenterY = rightColY != null ? rightPos.y + rightColY : rightPos.y + rightPos.height / 2;
      leftCenterY = Math.max(leftPos.y, Math.min(leftBottomMaxY, leftCenterY));
      rightCenterY = Math.max(rightPos.y, Math.min(rightBottomMaxY, rightCenterY));
      const gap = 20;
      let startX: number, startY: number, endX: number, endY: number, step1X: number, step2X: number;
      if (leftCenterX < rightCenterX) {
        startX = leftPos.x + leftPos.width;
        endX = rightPos.x;
        startY = leftCenterY;
        endY = rightCenterY;
        step1X = startX + gap;
        step2X = endX - gap;
      } else {
        startX = leftPos.x;
        endX = rightPos.x + rightPos.width;
        startY = leftCenterY;
        endY = rightCenterY;
        step1X = startX - gap;
        step2X = endX + gap;
      }
      return {
        startX,
        startY,
        step1X,
        step1Y: startY,
        step2X,
        step2Y: endY,
        endX,
        endY,
        midX: (step1X + step2X) / 2,
        midY: (startY + endY) / 2,
        logoPath1: '',
        logoPath2: ''
      };
    },
    [getTablePosition, getColumnCenterY]
  );

  const addTables = useCallback(
    async (ownerTableNamePairList: Array<{ Key: string | null; Value: string }>): Promise<{ merged: Record<string, any>; joins: any[]; dictAllColumns: Record<string, any> } | null> => {
      if (!ownerTableNamePairList?.length || !dto?.DataSourceFrom) return null;
      const diagram = dto.OtherSettingsDto?.DatabaseDiagramInfo ?? {};
      const viewDtoForApi = {
        ...diagram,
        DictTables: diagram.DictTables ?? {},
        Joins: diagram.Joins ?? [],
        DictAllColumns: diagram.DictAllColumns ?? {},
        SelectedColumnsList: diagram.SelectedColumnsList ?? [],
        WhereConditionFilterColumns: diagram.WhereConditionFilterColumns ?? [],
        DataSourceRegisterId: dto.DataSourceFrom,
        IsErDiagram: true
      };
      try {
        dispatch(setIsBusy());
        const tableAddRemoveDto = {
          ...viewDtoForApi,
          NeedToAddOwnerTablePairList: ownerTableNamePairList,
          DataSourceRegisterId: dto.DataSourceFrom,
          IsErDiagram: true
        };
        const updated = await schemaMetadataService.addTablesToDatabaseView(tableAddRemoveDto);
        if (updated?.ErrorMessage) {
          showError(updated.ErrorMessage);
          return null;
        }
        if (updated) {
          const existing = diagram.DictTables || {};
          const merged = { ...(updated.DictTables || {}) };
          Object.keys(existing).forEach((k) => {
            if (merged[k] && existing[k].PositionX != null) {
              merged[k] = { ...merged[k], PositionX: existing[k].PositionX, PositionY: existing[k].PositionY, Width: existing[k].Width ?? 200, Height: existing[k].Height ?? 200 };
            }
          });
          updateDiagramView(() => ({
            DictTables: merged,
            Joins: updated.Joins ?? diagram.Joins ?? [],
            DictAllColumns: updated.DictAllColumns ?? diagram.DictAllColumns ?? {}
          }));
          showInfo('Table(s) added');
          return { merged, joins: updated.Joins ?? diagram.Joins ?? [], dictAllColumns: updated.DictAllColumns ?? diagram.DictAllColumns ?? {} };
        }
      } catch (err) {
        showError('Failed to add table: ' + (err instanceof Error ? err.message : String(err)));
      }       finally {
        dispatch(setIsNotBusy());
        setShowTableSelectorPopup(false);
      }
      return null;
    },
    [dto, dispatch, showError, showInfo, updateDiagramView]
  );

  const removeOneTable = useCallback(
    async (uniqTableOrAliasName: string) => {
      if (!dto?.DataSourceFrom) return;
      const diagram = dto.OtherSettingsDto?.DatabaseDiagramInfo ?? {};
      try {
        dispatch(setIsBusy());
        const tableAddRemoveDto = {
          ...diagram,
          NeedToRemoveUniqTableOrAliasNames: [uniqTableOrAliasName],
          DataSourceRegisterId: dto.DataSourceFrom,
          IsErDiagram: true
        };
        const updated = await schemaMetadataService.removeTablesFromDatabaseView(tableAddRemoveDto);
        if (updated?.ErrorMessage) {
          showError(updated.ErrorMessage);
          return;
        }
        if (updated) {
          updateDiagramView(() => ({
            DictTables: updated.DictTables ?? {},
            Joins: updated.Joins ?? diagram.Joins ?? [],
            DictAllColumns: updated.DictAllColumns ?? diagram.DictAllColumns ?? {}
          }));
          showInfo('Table removed from diagram');
        }
      } catch (err) {
        showError('Failed to remove table: ' + (err instanceof Error ? err.message : String(err)));
      } finally {
        dispatch(setIsNotBusy());
        setShowTableContextMenu(null);
      }
    },
    [dto, dispatch, showError, showInfo, updateDiagramView]
  );

  const addJoinConditionLine = useCallback(
    async (leftTable: string, leftColumn: string, rightTable: string, rightColumn: string, diagramOverride?: { DictTables?: Record<string, any>; Joins?: any[]; DictAllColumns?: Record<string, any> }) => {
      if (!leftTable || !leftColumn || !rightTable || !rightColumn || leftTable === rightTable || !dto?.DataSourceFrom) return;
      const diagram = diagramOverride ?? dto.OtherSettingsDto?.DatabaseDiagramInfo ?? {};
      const dictTables = diagram.DictTables ?? {};
      let leftSideSchemaOwner = dictTables[leftTable] && !dictTables[leftTable].TableAlias ? (dictTables[leftTable].SchemaOwner ?? 'dbo') : '';
      let rightSideSchemaOwner = dictTables[rightTable] && !dictTables[rightTable].TableAlias ? (dictTables[rightTable].SchemaOwner ?? 'dbo') : '';
      const tablesWithIsSelected = Object.fromEntries(
        Object.entries(dictTables).map(([k, v]) => {
          const tbl = v as Record<string, unknown>;
          return [k, { ...tbl, isSelected: (tbl?.isSelected as boolean) ?? false }];
        })
      );
      try {
        dispatch(setIsBusy());
        const joinUpdateDto = {
          QueryString: diagram.QueryString ?? '',
          DictTables: tablesWithIsSelected,
          DictAllColumns: diagram.DictAllColumns ?? {},
          SelectedColumnsList: diagram.SelectedColumnsList ?? [],
          Joins: diagram.Joins ?? [],
          WhereConditionFilterColumns: diagram.WhereConditionFilterColumns ?? [],
          DataSourceRegisterId: dto.DataSourceFrom,
          IsErDiagram: true,
          NeedToAddJoinCondition: {
            LeftSideSchemaOwner: leftSideSchemaOwner,
            LeftSideTable: leftTable,
            LeftSideColumn: leftColumn,
            RightSideSchemaOwner: rightSideSchemaOwner,
            RightSideTable: rightTable,
            RightSideColumn: rightColumn
          }
        };
        const updated = await schemaMetadataService.addOneFkLineToErDiagram(joinUpdateDto);
        if (updated?.ErrorMessage) {
          showError(updated.ErrorMessage);
          return;
        }
        if (updated) {
          const existing = diagram.DictTables ?? {};
          const merged = { ...(updated.DictTables || {}) };
          Object.keys(existing).forEach((k) => {
            if (merged[k] && existing[k].PositionX != null) {
              merged[k] = { ...merged[k], PositionX: existing[k].PositionX, PositionY: existing[k].PositionY, Width: existing[k].Width ?? 200, Height: existing[k].Height ?? 200 };
            }
          });
          updateDiagramView(() => ({
            DictTables: merged,
            Joins: updated.Joins ?? diagram.Joins ?? [],
            DictAllColumns: updated.DictAllColumns ?? diagram.DictAllColumns ?? {}
          }));
          showInfo('FK line added');
        }
      } catch (err) {
        showError('Failed to add FK line: ' + (err instanceof Error ? err.message : String(err)));
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dto, dispatch, showError, showInfo, updateDiagramView]
  );

  const removeJoinConditionLines = useCallback(async () => {
    if (!currentConditionLine || !dto?.DataSourceFrom) return;
    const diagram = dto.OtherSettingsDto?.DatabaseDiagramInfo ?? {};
    const viewDtoWithSelection = { ...diagram, Joins: (diagram.Joins ?? []).map((join: any) => ({
      ...join,
      JoinConditionList: join.JoinConditionList?.map((condition: any) => ({
        ...condition,
        isSelected: condition.GUID === currentConditionLine.GUID
      }))
    })) };
    const needToRemoveGUIDs: string[] = [];
    viewDtoWithSelection.Joins.forEach((join: any) => {
      join.JoinConditionList?.forEach((condition: any) => {
        if (condition.isSelected && condition.GUID) needToRemoveGUIDs.push(condition.GUID.toString());
      });
    });
    if (needToRemoveGUIDs.length === 0) return;
    try {
      dispatch(setIsBusy());
      const dictTables = diagram.DictTables ?? {};
      const tablesWithIsSelected = Object.fromEntries(
        Object.entries(dictTables).map(([k, v]) => {
          const tbl = v as Record<string, unknown>;
          return [k, { ...tbl, isSelected: (tbl?.isSelected as boolean) ?? false }];
        })
      );
      const joinUpdateDto = {
        NeedToRemoveJoinConditionGUIDs: needToRemoveGUIDs,
        QueryString: diagram.QueryString ?? '',
        DictTables: tablesWithIsSelected,
        DictAllColumns: diagram.DictAllColumns ?? {},
        SelectedColumnsList: diagram.SelectedColumnsList ?? [],
        Joins: viewDtoWithSelection.Joins ?? [],
        WhereConditionFilterColumns: diagram.WhereConditionFilterColumns ?? [],
        DataSourceRegisterId: dto.DataSourceFrom,
        IsErDiagram: true
      };
      const updated = await schemaMetadataService.removeFkLinesFromErDiagram(joinUpdateDto);
      if (updated?.ErrorMessage) {
        showError(updated.ErrorMessage);
        return;
      }
      if (updated) {
        const existing = diagram.DictTables || {};
        const merged = { ...(updated.DictTables || {}) };
        Object.keys(existing).forEach((k) => {
          if (merged[k] && existing[k].PositionX != null) {
            merged[k] = { ...merged[k], PositionX: existing[k].PositionX, PositionY: existing[k].PositionY, Width: existing[k].Width ?? 200, Height: existing[k].Height ?? 200 };
          }
        });
        updateDiagramView(() => ({
          DictTables: merged,
          Joins: updated.Joins ?? diagram.Joins ?? [],
          DictAllColumns: updated.DictAllColumns ?? diagram.DictAllColumns ?? {}
        }));
        showInfo('FK line removed');
      }
      setShowJoinContextMenu(false);
      setCurrentConditionLine(null);
      setCurrentJoin(null);
    } catch (err) {
      showError('Failed to remove FK line: ' + (err instanceof Error ? err.message : String(err)));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentConditionLine, dto, dispatch, showError, showInfo, updateDiagramView]);

  const updateJoinMethod = useCallback(async (isUpdateJoinMethodForLeftTable: boolean) => {
    if (!currentConditionLine || !currentJoin || !dto?.DataSourceFrom) return;
    const diagram = dto.OtherSettingsDto?.DatabaseDiagramInfo ?? {};
    try {
      dispatch(setIsBusy());
      const joinUpdateDto = {
        ...diagram,
        NeedToUpdateJoinMethodConditionDto: currentConditionLine,
        IsUpdateJoinMethodForLeftTable: isUpdateJoinMethodForLeftTable,
        DataSourceRegisterId: dto.DataSourceFrom
      };
      const updated = await schemaMetadataService.updateDatabaseViewJoinMethod(joinUpdateDto);
      if (updated?.ErrorMessage) {
        showError(updated.ErrorMessage);
        return;
      }
      if (updated) {
        const existing = diagram.DictTables || {};
        const merged = { ...(updated.DictTables || {}) };
        Object.keys(existing).forEach((k) => {
          if (merged[k] && existing[k].PositionX != null) {
            merged[k] = { ...merged[k], PositionX: existing[k].PositionX, PositionY: existing[k].PositionY, Width: existing[k].Width ?? 200, Height: existing[k].Height ?? 200 };
          }
        });
        updateDiagramView(() => ({
          DictTables: merged,
          Joins: updated.Joins ?? diagram.Joins ?? [],
          DictAllColumns: updated.DictAllColumns ?? diagram.DictAllColumns ?? {}
        }));
        showInfo('Join method updated');
      }
      setShowJoinContextMenu(false);
      setCurrentConditionLine(null);
      setCurrentJoin(null);
    } catch (err) {
      showError('Failed to update join method: ' + (err instanceof Error ? err.message : String(err)));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentConditionLine, currentJoin, dto, dispatch, showError, showInfo, updateDiagramView]);

  const handleLineSelected = useCallback((e: React.MouseEvent, line: any, join: any) => {
    e.stopPropagation();
    setCurrentConditionLine(line);
    setCurrentJoin(join);
    setJoinContextMenuPosition({ x: e.clientX, y: e.clientY });
    setShowJoinContextMenu(true);
  }, []);

  const addFkRefTable = useCallback(
    async (fkObj: any, isAddingFkChildTable: boolean, addFromUniqTableOrAliasName: string) => {
      if (!fkObj || !addFromUniqTableOrAliasName || !schemaTableList?.length) return;
      const tableNameToFind = isAddingFkChildTable ? fkObj.ChildTableName : fkObj.ParentTableName;
      const tableDtoFound = schemaTableList.find(
        (t: any) => (t.Name ?? t.Value)?.toLowerCase() === (tableNameToFind ?? '').toLowerCase()
      );
      if (!tableDtoFound) {
        showError(`The table ${tableNameToFind} is not available.`);
        return;
      }
      const ownerTableNamePairList = [{ Key: tableDtoFound.SchemaOwner ?? tableDtoFound.Key, Value: tableDtoFound.Name ?? tableDtoFound.Value }];
      const orgDictTables = { ...viewDto.DictTables };
      const result = await addTables(ownerTableNamePairList);
      if (!result) return;
      const { merged, joins, dictAllColumns } = result;
      let targetUniqTableOrAliasName = '';
      const sourceCol = isAddingFkChildTable ? fkObj.ParentTablePkColumnName : fkObj.ChildTableFkColumnName;
      const targetCol = isAddingFkChildTable ? fkObj.ChildTableFkColumnName : fkObj.ParentTablePkColumnName;
      Object.keys(merged).forEach((k) => {
        if (!orgDictTables[k]) {
          const tbl = merged[k];
          const match = isAddingFkChildTable
            ? (tbl?.TableName ?? k)?.toLowerCase() === (fkObj.ChildTableName ?? '').toLowerCase()
            : (tbl?.TableName ?? k)?.toLowerCase() === (fkObj.ParentTableName ?? '').toLowerCase();
          if (match) targetUniqTableOrAliasName = k;
        }
      });
      if (targetUniqTableOrAliasName) {
        const diagramOverride = { DictTables: merged, Joins: joins, DictAllColumns: dictAllColumns };
        await addJoinConditionLine(addFromUniqTableOrAliasName, sourceCol, targetUniqTableOrAliasName, targetCol, diagramOverride);
      }
      setShowFkRefSubmenu(null);
      setShowFkRefedSubmenu(null);
      setShowTableContextMenu(null);
    },
    [schemaTableList, addTables, addJoinConditionLine, showError, viewDto.DictTables]
  );

  const previewTableData = useCallback(
    (tableObj: any) => {
      if (tableObj && (tableObj.TableName ?? tableObj.UniqTableOrAliasName) && dto?.DataSourceFrom) {
        setPreviewTableInfo({
          tableName: tableObj.TableName ?? tableObj.UniqTableOrAliasName ?? '',
          dataSourceRegisterId: dto.DataSourceFrom,
          schemaOwner: tableObj.SchemaOwner ?? null
        });
        setShowPreviewPopup(true);
      }
      setShowTableContextMenu(null);
    },
    [dto?.DataSourceFrom]
  );

  const openTableDesign = useCallback(
    (tableObj: any) => {
      if (tableObj && (tableObj.TableName ?? tableObj.UniqTableOrAliasName) && dto?.DataSourceFrom) {
        setTableDesignInfo({
          tableName: tableObj.TableName ?? tableObj.UniqTableOrAliasName ?? null,
          dataSourceRegisterId: dto.DataSourceFrom,
          schemaOwner: tableObj.SchemaOwner ?? null,
          applicationId: dto?.ApplicationId ?? null
        });
        setShowTableDesignPopup(true);
      }
      setShowTableContextMenu(null);
    },
    [dto?.DataSourceFrom, dto?.ApplicationId]
  );

  const handleColumnDragStart = useCallback((e: React.DragEvent, sourceTableId: string, sourceColumnId: string) => {
    setDraggingColumn({ sourceTableId, sourceColumnId });
    e.dataTransfer.effectAllowed = 'copy';
    e.dataTransfer.setData('text/plain', '');
  }, []);

  const handleColumnDrop = useCallback(
    (e: React.DragEvent, targetTableId: string, targetColumnId: string) => {
      e.preventDefault();
      e.stopPropagation();
      if (draggingColumn && draggingColumn.sourceTableId !== targetTableId) {
        addJoinConditionLine(draggingColumn.sourceTableId, draggingColumn.sourceColumnId, targetTableId, targetColumnId);
      }
      setDraggingColumn(null);
    },
    [draggingColumn, addJoinConditionLine]
  );

  const handleColumnDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    e.dataTransfer.dropEffect = 'copy';
  }, []);

  const handleColumnDragEnd = useCallback(() => {
    setDraggingColumn(null);
  }, []);

  const handleTableBoxDragStart = useCallback((e: React.MouseEvent, uniqTableOrAliasName: string) => {
    const target = e.target as HTMLElement;
    if (!target.closest('.table-drag-handle')) return;
    const t = viewDto.DictTables[uniqTableOrAliasName];
    if (!t) return;
    const scrollEl = diagramScrollRef.current;
    if (!scrollEl) return;
    const scrollRect = scrollEl.getBoundingClientRect();
    const posX = t.PositionX ?? 0;
    const posY = t.PositionY ?? 0;
    const mouseContentX = e.clientX - scrollRect.left + scrollEl.scrollLeft;
    const mouseContentY = e.clientY - scrollRect.top + scrollEl.scrollTop;
    setDragOffset({ x: mouseContentX - posX, y: mouseContentY - posY });
    setDraggingTable(uniqTableOrAliasName);
  }, [viewDto.DictTables]);

  useEffect(() => {
    if (!draggingTable) return;
    const handleMouseMove = (e: MouseEvent) => {
      const scrollEl = diagramScrollRef.current;
      if (!scrollEl) return;
      const scrollRect = scrollEl.getBoundingClientRect();
      const mouseContentX = e.clientX - scrollRect.left + scrollEl.scrollLeft;
      const mouseContentY = e.clientY - scrollRect.top + scrollEl.scrollTop;
      const newX = Math.max(0, mouseContentX - dragOffset.x);
      const newY = Math.max(0, mouseContentY - dragOffset.y);
      updateDiagramView((prev) => {
        const next = { ...prev };
        if (next.DictTables[draggingTable]) {
          next.DictTables = { ...next.DictTables, [draggingTable]: { ...next.DictTables[draggingTable], PositionX: newX, PositionY: newY } };
        }
        return next;
      });
    };
    const handleMouseUp = () => setDraggingTable(null);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [draggingTable, dragOffset, updateDiagramView]);

  const handleRefresh = useCallback(() => {
    loadSchemaTables();
    if (dataSetId != null) loadData();
    showInfo('Refreshed');
  }, [dataSetId, loadData, loadSchemaTables, showInfo]);

  const handleAddAnnotation = useCallback(() => {
    const guid = crypto.randomUUID();
    const newAnnotation = {
      GUID: guid,
      Text: 'New annotation',
      PositionX: 150,
      PositionY: 150,
      Width: 240,
      Height: 160
    };
    updateDiagramView((prev) => {
      const dict = prev.DictTextAnnotationGuidAndDto ?? {};
      return {
        ...prev,
        DictTextAnnotationGuidAndDto: { ...dict, [guid]: newAnnotation }
      };
    });
    setEditingAnnotationGuid(guid);
    showInfo('Annotation added');
  }, [updateDiagramView, showInfo]);

  const handleUpdateAnnotationText = useCallback((guid: string, text: string) => {
    updateDiagramView((prev) => {
      const dict = prev.DictTextAnnotationGuidAndDto ?? {};
      const existing = dict[guid];
      if (!existing) return prev;
      return {
        ...prev,
        DictTextAnnotationGuidAndDto: { ...dict, [guid]: { ...existing, Text: text } }
      };
    });
  }, [updateDiagramView]);

  const handleRemoveAnnotation = useCallback((guid: string) => {
    updateDiagramView((prev) => {
      const dict = { ...(prev.DictTextAnnotationGuidAndDto ?? {}) };
      delete dict[guid];
      return { ...prev, DictTextAnnotationGuidAndDto: dict };
    });
    if (editingAnnotationGuid === guid) setEditingAnnotationGuid(null);
    showInfo('Annotation removed');
  }, [updateDiagramView, editingAnnotationGuid, showInfo]);

  const handleAnnotationDragStart = useCallback((e: React.MouseEvent, guid: string) => {
    const ann = viewDto.DictTextAnnotationGuidAndDto?.[guid];
    if (!ann || editingAnnotationGuid === guid) return;
    const scrollEl = diagramScrollRef.current;
    if (!scrollEl) return;
    const scrollRect = scrollEl.getBoundingClientRect();
    const posX = ann.PositionX ?? 0;
    const posY = ann.PositionY ?? 0;
    const mouseContentX = e.clientX - scrollRect.left + scrollEl.scrollLeft;
    const mouseContentY = e.clientY - scrollRect.top + scrollEl.scrollTop;
    setDragOffset({ x: mouseContentX - posX, y: mouseContentY - posY });
    setDraggingAnnotationGuid(guid);
  }, [viewDto.DictTextAnnotationGuidAndDto, editingAnnotationGuid]);

  useEffect(() => {
    if (!draggingAnnotationGuid) return;
    const handleMouseMove = (e: MouseEvent) => {
      const scrollEl = diagramScrollRef.current;
      if (!scrollEl) return;
      const scrollRect = scrollEl.getBoundingClientRect();
      const mouseContentX = e.clientX - scrollRect.left + scrollEl.scrollLeft;
      const mouseContentY = e.clientY - scrollRect.top + scrollEl.scrollTop;
      const newX = Math.max(0, mouseContentX - dragOffset.x);
      const newY = Math.max(0, mouseContentY - dragOffset.y);
      updateDiagramView((prev) => {
        const dict = prev.DictTextAnnotationGuidAndDto ?? {};
        const existing = dict[draggingAnnotationGuid];
        if (!existing) return prev;
        return {
          ...prev,
          DictTextAnnotationGuidAndDto: { ...dict, [draggingAnnotationGuid]: { ...existing, PositionX: newX, PositionY: newY } }
        };
      });
    };
    const handleMouseUp = () => setDraggingAnnotationGuid(null);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [draggingAnnotationGuid, dragOffset, updateDiagramView]);

  const handleResizeStart = useCallback(
    (e: React.MouseEvent, type: 'table' | 'annotation', id: string) => {
      e.preventDefault();
      e.stopPropagation();
      const scrollEl = diagramScrollRef.current;
      if (!scrollEl) return;
      const scrollRect = scrollEl.getBoundingClientRect();
      const mouseContentX = e.clientX - scrollRect.left + scrollEl.scrollLeft;
      const mouseContentY = e.clientY - scrollRect.top + scrollEl.scrollTop;
      let posX = 0,
        posY = 0,
        width = 200,
        height = 200;
      if (type === 'table') {
        const t = viewDto.DictTables[id];
        if (!t) return;
        posX = t.PositionX ?? 0;
        posY = t.PositionY ?? 0;
        width = t.Width ?? 200;
        height = t.Height ?? 200;
      } else {
        const ann = viewDto.DictTextAnnotationGuidAndDto?.[id];
        if (!ann) return;
        posX = ann.PositionX ?? 0;
        posY = ann.PositionY ?? 0;
        width = ann.Width ?? 200;
        height = ann.Height ?? 160;
      }
      resizeBoxStartRef.current = { mouseX: mouseContentX, mouseY: mouseContentY, posX, posY, width, height };
      setResizingItem({ type, id });
    },
    [viewDto.DictTables, viewDto.DictTextAnnotationGuidAndDto]
  );

  useEffect(() => {
    if (!resizingItem) return;
    const minW = resizingItem.type === 'table' ? 120 : 160;
    const minH = resizingItem.type === 'table' ? 60 : 80;
    const handleMouseMove = (e: MouseEvent) => {
      const start = resizeBoxStartRef.current;
      const scrollEl = diagramScrollRef.current;
      if (!start || !scrollEl) return;
      const scrollRect = scrollEl.getBoundingClientRect();
      const mouseContentX = e.clientX - scrollRect.left + scrollEl.scrollLeft;
      const mouseContentY = e.clientY - scrollRect.top + scrollEl.scrollTop;
      const newWidth = Math.max(minW, Math.round(mouseContentX - start.posX));
      const newHeight = Math.max(minH, Math.round(mouseContentY - start.posY));
      if (resizingItem.type === 'table') {
        updateDiagramView((prev) => {
          const next = { ...prev };
          if (next.DictTables[resizingItem.id]) {
            next.DictTables = {
              ...next.DictTables,
              [resizingItem.id]: { ...next.DictTables[resizingItem.id], Width: newWidth, Height: newHeight }
            };
          }
          return next;
        });
      } else {
        updateDiagramView((prev) => {
          const dict = prev.DictTextAnnotationGuidAndDto ?? {};
          const existing = dict[resizingItem.id];
          if (!existing) return prev;
          return {
            ...prev,
            DictTextAnnotationGuidAndDto: {
              ...dict,
              [resizingItem.id]: { ...existing, Width: newWidth, Height: newHeight }
            }
          };
        });
      }
    };
    const handleMouseUp = () => setResizingItem(null);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [resizingItem, updateDiagramView]);

  const openNewTableDesign = useCallback(() => {
        setShowTableSelectorPopup(false);
    setCreateDataModelOpen(false);
    if (!dto?.DataSourceFrom) return;
    setTableDesignInfo({
      tableName: null,
      dataSourceRegisterId: dto.DataSourceFrom,
      schemaOwner: 'dbo',
      applicationId: dto?.ApplicationId ?? null
    });
    setShowTableDesignPopup(true);
  }, [dto?.DataSourceFrom, dto?.ApplicationId]);

  const handleNewTableSaved = useCallback(
    (tableData: any, isNewTable: boolean) => {
      if (isNewTable && tableData?.Name && dto?.DataSourceFrom) {
        addTables([{ Key: tableData.SchemaOwner ?? 'dbo', Value: tableData.Name }]);
        loadSchemaTables();
        showInfo('Table created and added to diagram');
      }
    },
    [addTables, dto?.DataSourceFrom, loadSchemaTables, showInfo]
  );

  const handleGenerateFromDiagram = useCallback(() => {
    setCreateDataModelOpen(false);
    if (!dto?.DataSourceFrom || !dataSetId) {
      showError('Save the ER diagram first to generate a data model from it.');
      return;
    }
    addTabAndNavigate('application-form-builder', 'Form from ER Diagram', {
      id: dto?.ApplicationId ?? null,
      defaultSectionCode: 'TransactionGraphicEditor',
      isCreateNewItem: true,
      transactionId: null,
      erDiagramId: dataSetId,
      dataSourceRegisterId: dto.DataSourceFrom,
      isCreateDtoDataModel: false,
      isCreateApiDataModel: false,
      isCreateDataModelView: false,
      modelName: 'Form from ER Diagram'
    }, true);
  }, [addTabAndNavigate, dataSetId, dto?.ApplicationId, dto?.DataSourceFrom, showError]);

  const handleExportDdl = useCallback(async () => {
    setCreateDataModelOpen(false);
    if (!dto?.DataSourceFrom) {
      showError('Select a data source first.');
      return;
    }
    const dictTables = viewDto.DictTables ?? {};
    const tableList = Object.values(dictTables).filter((t: any) => t?.TableName);
    if (tableList.length === 0) {
      showError('No tables in diagram to export.');
      return;
    }
    try {
      dispatch(setIsBusy());
      const schemaResult = await dbGenieService.getSchemaContext(dto.DataSourceFrom);
      if (!schemaResult?.Object) {
        showError('Failed to get schema context.');
        return;
      }
      const diagramTableKeys = new Set(
        tableList.map((t: any) => `${(t.SchemaOwner || 'dbo').toLowerCase()}.${(t.TableName || '').toLowerCase()}`)
      );
      const filteredTables = (schemaResult.Object as any[]).filter(
        (t: any) => diagramTableKeys.has(`${(t.SchemaOwner || 'dbo').toLowerCase()}.${(t.Name || '').toLowerCase()}`)
      );
      if (filteredTables.length === 0) {
        showError('Could not find diagram tables in schema.');
        return;
      }
      const scriptResult = await dbGenieService.generateCreateScript(
        { Tables: filteredTables, IsSuccess: true },
        'dbo'
      );
      if (scriptResult?.Object) {
        setExportDdlScript(scriptResult.Object);
        setShowExportDdlModal(true);
      } else {
        showError(scriptResult?.ValidationResult?.Items?.[0]?.Message ?? 'Failed to generate DDL');
      }
    } catch (err) {
      showError('Export DDL failed: ' + (err instanceof Error ? err.message : String(err)));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dto?.DataSourceFrom, dispatch, showError, viewDto.DictTables]);

  const handleClose = useCallback(() => {
    if (activeTabKey == null) {
      navigate('/home');
      return;
    }
    const remainingTabs = tabs.filter((t) => t.tabKey !== activeTabKey);
    const prevActive = previousActiveTabKey
      ? remainingTabs.find((t) => t.tabKey === previousActiveTabKey)
      : undefined;
    const newPath = (prevActive?.path ?? remainingTabs[remainingTabs.length - 1]?.path) ?? '/home';
    dispatch(closeTab(activeTabKey));
    navigate(newPath);
  }, [activeTabKey, previousActiveTabKey, tabs, dispatch, navigate]);

  const handleSave = useCallback(async () => {
    if (!dto) return;
    if (!dto.Name?.trim()) {
      showWarning('Name is required');
      return;
    }
    if (!dto.DataSourceFrom) {
      showWarning('Data source is required');
      return;
    }
    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const diagramInfo = { ...(dto.OtherSettingsDto?.DatabaseDiagramInfo ?? {}), IsErDiagram: true };
      const payload = {
        ...dto,
        UsageTypeId: EmAppDataSetUsageType_ErDiagram,
        OtherSettingsDto: { ...(dto.OtherSettingsDto ?? {}), DatabaseDiagramInfo: diagramInfo }
      };
      const result = await appTransactionService.SaveOneErDiagramExDto(payload);
      if (result?.ValidationResult?.IsValid !== false && result?.Object) {
        showInfo('ER diagram saved');
        setDto(result.Object);
        if (dataSetId == null && result.Object?.Id) {
          navigate(`/er-diagram-editor/${encodeURIComponent(JSON.stringify({ id: result.Object.Id }))}`);
        }
      } else {
        const msg = result?.ValidationResult?.Items?.[0]?.Message ?? result?.ValidationResult?.ErrorMessage ?? 'Save failed';
        showError(msg);
      }
    } catch (err) {
      showError('Failed to save: ' + (err instanceof Error ? err.message : String(err)));
    } finally {
      setIsSaving(false);
      dispatch(setIsNotBusy());
    }
  }, [dto, dataSetId, dispatch, showError, showInfo, showWarning, navigate]);

  if (isLoading) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <span className={`text-sm ${theme.label}`}>Loading…</span>
      </div>
    );
  }

  if (!dto && dataSetId != null) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <span className={`text-sm ${theme.label}`}>ER diagram not found.</span>
      </div>
    );
  }

  const dictTables = viewDto.DictTables ?? {};
  const dictTablesEntries = Object.entries(dictTables);
  const joins = viewDto.Joins ?? [];

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header: one row – title + 3 fields + toolbar buttons */}
      <div className={`flex items-center justify-between gap-4 px-3 py-2 flex-wrap ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold shrink-0 ${theme.title}`}>ER Diagram Editor</div>
        <div className="flex items-center gap-3 flex-wrap shrink-0">
          <div className="flex items-center gap-2">
            <label className={`w-16 text-xs ${theme.label} shrink-0`}>Name</label>
            <input
              type="text"
              autoComplete="off"
              value={dto?.Name ?? ''}
              onChange={(e) => handleFieldChange('Name', e.target.value)}
              className={`w-40 h-7 px-2 text-xs border ${theme.inputBox}`}
            />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-20 text-xs ${theme.label} shrink-0`}>Description</label>
            <input
              type="text"
              autoComplete="off"
              value={dto?.Description ?? ''}
              onChange={(e) => handleFieldChange('Description', e.target.value)}
              className={`w-40 h-7 px-2 text-xs border ${theme.inputBox}`}
            />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label} shrink-0`}>Datasource</label>
            <select
              value={dto?.DataSourceFrom ?? ''}
              onChange={(e) => handleFieldChange('DataSourceFrom', e.target.value ? Number(e.target.value) : null)}
              className={`w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
            >
              <option value="">Select</option>
              {dataSourceList.map((ds) => (
                <option key={ds.Id} value={ds.Id}>{ds.DataSourceName}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="flex items-center gap-1 flex-wrap">
          <button type="button" onClick={handleRefresh} className={`px-2 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} title="Refresh">
            <i className="fa-solid fa-rotate" aria-hidden />
          </button>
          <button type="button" onClick={handleSave} disabled={isSaving} className={`px-2 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} title="Save">
            <i className="fa-solid fa-floppy-disk" aria-hidden />
          </button>
          <button type="button" onClick={openNewTableDesign} disabled={!dto?.DataSourceFrom} className={`px-2 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} title="New Table">
            <i className="fa-solid fa-plus" aria-hidden />
          </button>
          <button type="button" onClick={() => { setShowTableSelectorPopup(true); setCreateDataModelOpen(false); setTableSelectorSearch(''); setTableSelectorSelected(new Set()); loadSchemaTables(); }} disabled={!dto?.DataSourceFrom} className={`px-2 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} title="Add Existing Tables">
            <i className="fa-solid fa-folder-plus" aria-hidden />
          </button>
          <button type="button" onClick={handleAddAnnotation} className={`px-2 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} title="Add Text Annotation">
            <i className="fa-solid fa-file-lines" aria-hidden />
          </button>
          <div className="relative" ref={createDataModelRef}>
            <button type="button" onClick={() => { setCreateDataModelOpen((v) => !v); setShowTableSelectorPopup(false); }} className={`px-2 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} title="Create Data Model">
              <i className="fa-solid fa-diagram-project" aria-hidden />
              <i className="fa-solid fa-caret-down ml-1 text-[10px]" aria-hidden />
            </button>
            {createDataModelOpen && (
              <div className={`absolute right-0 mt-1 min-w-[180px] rounded shadow-lg border z-50 py-1 ${theme.mainContentSection}`}>
                <button type="button" className={`w-full text-left px-3 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={handleGenerateFromDiagram}>
                  <i className="fa-solid fa-wand-magic-sparkles" aria-hidden /> Generate from diagram
                </button>
                <button type="button" className={`w-full text-left px-3 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={handleExportDdl}>
                  <i className="fa-solid fa-download" aria-hidden /> Export DDL
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Main: left panel (table list) | resizer | right (diagram) */}
      <div ref={diagramContainerRef} className="w-full h-1 flex-auto flex overflow-hidden">
        {/* Left: Available Tables - shrink-0 so flex does not compress it */}
        <div className={`flex flex-col overflow-hidden border-r shrink-0 ${theme.mainContentSection}`} style={{ width: leftPanelWidth, minWidth: leftPanelWidth }}>
          <div className={`px-2 py-1.5 border-b ${theme.mainContentSection}`}>
            <span className={`text-xs font-semibold ${theme.title}`}>Available Tables</span>
          </div>
          <div className={`px-2 py-1.5 border-b ${theme.mainContentSection}`}>
            <div className="relative">
              <input
                type="text"
                value={tableViewNameFilter}
                onChange={(e) => setTableViewNameFilter(e.target.value)}
                placeholder="Filter tables/views..."
                className={`w-full h-7 pl-2 pr-7 text-xs border rounded-[4px] ${theme.inputBox}`}
              />
              {tableViewNameFilter && (
                <button
                  type="button"
                  onClick={() => setTableViewNameFilter('')}
                  className={`absolute right-1 top-1/2 -translate-y-1/2 w-5 h-5 flex items-center justify-center rounded ${theme.button_default}`}
                  title="Clear filter"
                >
                  <i className="fa-solid fa-xmark text-[10px]" aria-hidden />
                </button>
              )}
            </div>
          </div>
          <div className="flex-auto overflow-auto p-1">
            {schemaTableList.length === 0 ? (
              <div className={`text-xs ${theme.label} p-2`}>Select datasource and refresh.</div>
            ) : filteredSchemaTableList.length === 0 ? (
              <div className={`text-xs ${theme.label} p-2`}>No tables or views match filter.</div>
            ) : (
              filteredSchemaTableList.map((t: any) => {
                const key = t.SchemaOwner ? `${t.SchemaOwner}.${t.Name}` : t.Name;
                const inDiagram = dictTablesEntries.some(([, o]) => (o as any).TableName?.toLowerCase() === t.Name?.toLowerCase());
                return (
                  <div
                    key={key}
                    draggable={!inDiagram}
                    onDragStart={() => { if (!inDiagram) setDraggedTableItem({ Key: t.SchemaOwner ?? null, Value: t.Name }); }}
                    onDragEnd={() => setDraggedTableItem(null)}
                    className={`flex items-center gap-2 px-2 py-1.5 rounded border-b border-transparent hover:bg-gray-100 dark:hover:bg-gray-700/50 ${theme.contextMenu} ${inDiagram ? '' : 'cursor-move'}`}
                  >
                    <i className="fa-solid fa-table text-xs opacity-70 shrink-0" aria-hidden />
                    <span className="flex-auto w-1 truncate text-xs" title={t.uiDisplayName ?? key}>{t.uiDisplayName ?? key}</span>
                    <button type="button" onClick={() => addTables([{ Key: t.SchemaOwner ?? null, Value: t.Name }])} disabled={inDiagram} className="p-0.5 shrink-0" title={inDiagram ? 'In diagram' : 'Add'}>
                      <i className={`fa-solid fa-eye text-xs ${inDiagram ? 'opacity-40' : ''}`} aria-hidden />
                    </button>
                    <button type="button" className="p-0.5 shrink-0" title="Settings" onClick={() => showInfo('Table settings')}>
                      <i className="fa-solid fa-gear text-xs" aria-hidden />
                    </button>
                  </div>
                );
              })
            )}
          </div>
        </div>

        {/* Resizer - wider strip so user can drag easily */}
        <div
          role="separator"
          aria-label="Resize panels"
          onMouseDown={(e) => {
            e.preventDefault();
            resizeStartRef.current = { clientX: e.clientX, width: leftPanelWidth };
            setIsResizing(true);
          }}
          className={`w-2 cursor-col-resize shrink-0 flex items-center justify-center select-none ${isResizing ? 'bg-blue-400' : 'hover:bg-gray-300 dark:hover:bg-gray-600'} border-l border-r border-gray-300 dark:border-gray-600`}
          title="Drag to resize"
        >
          <div className="w-px h-10 bg-gray-400 dark:bg-gray-500" />
        </div>

        {/* Right: Diagram */}
        <div
          ref={diagramScrollRef}
          className="relative flex-auto overflow-auto border-l border-gray-200 dark:border-gray-700"
          onClick={() => { setShowTableContextMenu(null); setShowJoinContextMenu(false); setCurrentConditionLine(null); setCurrentJoin(null); }}
          onDragOver={(e) => { e.preventDefault(); e.dataTransfer.dropEffect = 'copy'; }}
          onDrop={(e) => {
            e.preventDefault();
            if (draggedTableItem) {
              addTables([draggedTableItem]);
              setDraggedTableItem(null);
            }
          }}
        >
          {draggingColumn && (
            <div className="absolute bottom-2 left-1/2 -translate-x-1/2 z-10 px-3 py-2 rounded shadow-lg bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-xs font-medium">
              Drag onto another table&apos;s column to create FK
            </div>
          )}
          <div className="relative" style={{ width: DIAGRAM_PAD, height: DIAGRAM_PAD }}>
            <svg className="absolute inset-0" style={{ width: '100%', height: '100%' }}>
              <defs>
                <marker id="er-arrowhead" markerWidth="11" markerHeight="7" refX="10" refY="3.5" orient="auto">
                  <polygon points="0 0, 11 3.5, 0 7" fill="#eab308" />
                </marker>
              </defs>
              {joins
                .filter((join: any) => join.JoinConditionList?.length && join.JoinConditionList.some((c: any) => {
                  return !!resolveDiagramTableKey(c.LeftSideTable ?? '') && !!resolveDiagramTableKey(c.RightSideTable ?? '');
                }))
                .map((join: any, joinIndex: number) => {
                  const valid = (join.JoinConditionList ?? []).filter((c: any) => {
                    return !!resolveDiagramTableKey(c.LeftSideTable ?? '') && !!resolveDiagramTableKey(c.RightSideTable ?? '');
                  });
                  if (valid.length === 0) return null;
                  return (
                    <g key={joinIndex} style={{ pointerEvents: 'none' }}>
                      {valid.map((line: any, lineIndex: number) => {
                        const c = getLineCoordinate(join, line);
                        const pathD = `M ${c.startX} ${c.startY} L ${c.step1X} ${c.step1Y} L ${c.step2X} ${c.step2Y} L ${c.endX} ${c.endY}`;
                        return (
                          <g key={lineIndex} style={{ pointerEvents: 'all' }}>
                            <path
                              d={pathD}
                              stroke="#94a3b8"
                              strokeWidth="1"
                              fill="none"
                              markerEnd="url(#er-arrowhead)"
                              style={{ pointerEvents: 'none' }}
                            />
                            <path
                              d={pathD}
                              stroke="transparent"
                              strokeWidth="12"
                              fill="none"
                              style={{ cursor: 'pointer' }}
                              onClick={(e) => handleLineSelected(e, line, join)}
                            />
                          </g>
                        );
                      })}
                    </g>
                  );
                })}
            </svg>

            {dictTablesEntries.map(([uniqTableOrAliasName, tableObj]: [string, any]) => (
              <div
                key={uniqTableOrAliasName}
                className="group absolute bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-600 flex flex-col overflow-hidden"
                style={{
                  left: `${tableObj.PositionX ?? 0}px`,
                  top: `${tableObj.PositionY ?? 0}px`,
                  width: `${tableObj.Width ?? 200}px`,
                  height: `${tableObj.Height ?? 200}px`,
                  minWidth: 120,
                  minHeight: 60
                }}
                onClick={(e) => { e.stopPropagation(); setShowJoinContextMenu(false); setCurrentConditionLine(null); setCurrentJoin(null); }}
              >
                <div className={`flex items-center justify-between px-2 py-1 border-b select-none ${theme.mainContentSection}`} style={{ minHeight: 36 }}>
                  <label
                    className="table-drag-handle text-xs font-semibold truncate flex-auto w-1 cursor-move"
                    title={tableObj.UniqTableOrAliasName ?? tableObj.TableName ?? uniqTableOrAliasName}
                    onMouseDown={(e) => { if (e.button === 0) handleTableBoxDragStart(e, uniqTableOrAliasName); }}
                  >
                    {tableObj.UniqTableOrAliasName ?? tableObj.TableName ?? uniqTableOrAliasName}
                  </label>
                  <div className="flex items-center gap-1">
                    <button type="button" onClick={(e) => { e.stopPropagation(); setTableContextMenuPosition({ x: e.clientX, y: e.clientY }); setShowTableContextMenu(uniqTableOrAliasName); }} className="text-xs px-1" title="Options">
                      <i className="fa-solid fa-ellipsis-vertical" aria-hidden />
                    </button>
                    <button type="button" onClick={() => removeOneTable(uniqTableOrAliasName)} className="text-gray-500 hover:text-red-600" title="Remove from diagram">
                      <i className="fa-solid fa-times" aria-hidden />
                    </button>
                  </div>
                </div>
                <div className="flex-1 overflow-y-auto overflow-x-hidden p-1" style={{ fontSize: '11px' }}>
                  {viewDto.DictAllColumns[uniqTableOrAliasName.toLowerCase()] &&
                    Object.entries(viewDto.DictAllColumns[uniqTableOrAliasName.toLowerCase()]).map(([colName]) => (
                      <div
                        key={colName}
                        className={`truncate py-0.5 px-1 flex items-center gap-1 cursor-grab active:cursor-grabbing ${draggingColumn?.sourceTableId === uniqTableOrAliasName && draggingColumn?.sourceColumnId === colName ? 'opacity-50' : ''}`}
                        draggable
                        onDragStart={(e) => handleColumnDragStart(e, uniqTableOrAliasName, colName)}
                        onDragOver={handleColumnDragOver}
                        onDrop={(e) => handleColumnDrop(e, uniqTableOrAliasName, colName)}
                        onDragEnd={handleColumnDragEnd}
                      >
                        <i className="fa-solid fa-magnifying-glass text-[10px] opacity-50 shrink-0" aria-hidden />
                        {colName}
                      </div>
                    ))}
                </div>
                <div
                  className={`absolute bottom-0 right-0 w-3 h-3 cursor-se-resize transition-opacity flex items-center justify-center text-gray-500 dark:text-gray-400 ${resizingItem?.type === 'table' && resizingItem?.id === uniqTableOrAliasName ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}
                  onMouseDown={(e) => { e.stopPropagation(); handleResizeStart(e, 'table', uniqTableOrAliasName); }}
                  title="Resize"
                >
                  <i className="fa-solid fa-grip-lines text-[10px] -rotate-45" aria-hidden />
                </div>
              </div>
            ))}

            {/* Text annotations */}
            {Object.entries(viewDto.DictTextAnnotationGuidAndDto ?? {}).map(([guid, ann]: [string, any]) => (
              <div
                key={guid}
                className={`group absolute border flex flex-col overflow-hidden ${theme.mainContentSection}`}
                style={{
                  left: `${ann.PositionX ?? 0}px`,
                  top: `${ann.PositionY ?? 0}px`,
                  width: `${ann.Width ?? 200}px`,
                  height: `${ann.Height ?? 160}px`,
                  minWidth: 160,
                  minHeight: 120
                }}
                onClick={(e) => e.stopPropagation()}
              >
                <div
                  className={`flex items-center justify-between px-2 py-1 border-b shrink-0 cursor-move select-none ${theme.mainContentSection} ${draggingAnnotationGuid === guid ? 'opacity-70' : ''}`}
                  onMouseDown={(e) => { if (e.button === 0) handleAnnotationDragStart(e, guid); }}
                >
                  <span className="text-xs font-medium opacity-80">Annotation</span>
                  <button type="button" onClick={(e) => { e.stopPropagation(); handleRemoveAnnotation(guid); }} className="text-gray-500 hover:text-red-600" title="Remove annotation">
                    <i className="fa-solid fa-times" aria-hidden />
                  </button>
                </div>
                <div className="flex-1 min-h-0 relative overflow-hidden">
                  {editingAnnotationGuid === guid ? (
                    <textarea
                      className={`absolute inset-0 w-full h-full text-xs p-2 border-0 resize-none box-border ${theme.inputBox}`}
                      value={ann.Text ?? ''}
                      onChange={(e) => handleUpdateAnnotationText(guid, e.target.value)}
                      onBlur={() => setEditingAnnotationGuid(null)}
                      autoFocus
                      onClick={(e) => e.stopPropagation()}
                    />
                  ) : (
                    <div
                      className={`absolute inset-0 overflow-auto p-2 text-xs whitespace-pre-wrap cursor-text ${theme.label}`}
                      onDoubleClick={(e) => { e.stopPropagation(); setEditingAnnotationGuid(guid); }}
                      title="Double-click to edit"
                    >
                      {(ann.Text ?? '').trim() || 'Double-click to add text'}
                    </div>
                  )}
                </div>
                <div
                  className={`absolute bottom-0 right-0 w-3 h-3 cursor-se-resize transition-opacity flex items-center justify-center text-gray-500 dark:text-gray-400 ${resizingItem?.type === 'annotation' && resizingItem?.id === guid ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}
                  onMouseDown={(e) => { e.stopPropagation(); handleResizeStart(e, 'annotation', guid); }}
                  title="Resize"
                >
                  <i className="fa-solid fa-grip-lines text-[10px] -rotate-45" aria-hidden />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {showTableContextMenu && (() => {
        const tableObj = viewDto.DictTables[showTableContextMenu];
        return (
          <div className={`fixed z-50 py-1 min-w-[200px] rounded shadow-lg border ${theme.mainContentSection}`} style={{ left: tableContextMenuPosition.x, top: tableContextMenuPosition.y }} onClick={(e) => e.stopPropagation()}>
            <button type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={() => previewTableData(tableObj)}>
              <i className="fa-solid fa-eye" aria-hidden /> Preview Table Data
            </button>
            <button type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={() => openTableDesign(tableObj)}>
              <i className="fa-solid fa-pen-to-square" aria-hidden /> Open Table Design
            </button>
            {(tableObj?.FKRefTables?.length ?? 0) > 0 && (
              <div className="relative">
                <button type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={() => setShowFkRefSubmenu(showFkRefSubmenu === showTableContextMenu ? null : showTableContextMenu)}>
                  <i className="fa-solid fa-plus-square" aria-hidden /> Add Reference (Parent) Tables
                  <i className="fa-solid fa-chevron-right ml-auto" aria-hidden />
                </button>
                {showFkRefSubmenu === showTableContextMenu && (
                  <div className={`absolute left-full top-0 ml-1 min-w-[200px] rounded shadow-lg border py-1 ${theme.mainContentSection}`} onClick={(e) => e.stopPropagation()}>
                    {tableObj.FKRefTables.map((fkObj: any, idx: number) => {
                      const isTableAdded = Object.keys(viewDto.DictTables).some((k) => (viewDto.DictTables[k]?.TableName ?? k)?.toLowerCase() === (fkObj.ParentTableName ?? '').toLowerCase());
                      return (
                        <button key={idx} type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={() => addFkRefTable(fkObj, false, showTableContextMenu)}>
                          <i className="fa-solid fa-database" aria-hidden /> <span className="truncate">{fkObj.ParentTableName}</span>
                          {isTableAdded && <i className="fa-solid fa-check ml-auto" aria-hidden />}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            )}
            {(tableObj?.FKRefedTables?.length ?? 0) > 0 && (
              <div className="relative">
                <button type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={() => setShowFkRefedSubmenu(showFkRefedSubmenu === showTableContextMenu ? null : showTableContextMenu)}>
                  <i className="fa-solid fa-plus-square" aria-hidden /> Add Referenced (Child) Tables
                  <i className="fa-solid fa-chevron-right ml-auto" aria-hidden />
                </button>
                {showFkRefedSubmenu === showTableContextMenu && (
                  <div className={`absolute left-full top-0 ml-1 min-w-[200px] rounded shadow-lg border py-1 ${theme.mainContentSection}`} onClick={(e) => e.stopPropagation()}>
                    {tableObj.FKRefedTables.map((fkObj: any, idx: number) => {
                      const isTableAdded = Object.keys(viewDto.DictTables).some((k) => (viewDto.DictTables[k]?.TableName ?? k)?.toLowerCase() === (fkObj.ChildTableName ?? '').toLowerCase());
                      return (
                        <button key={idx} type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={() => addFkRefTable(fkObj, true, showTableContextMenu)}>
                          <i className="fa-solid fa-database" aria-hidden /> <span className="truncate">{fkObj.ChildTableName}</span>
                          {isTableAdded && <i className="fa-solid fa-check ml-auto" aria-hidden />}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            )}
            <div className="border-t my-1" />
            <button type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={() => removeOneTable(showTableContextMenu)}>
              <i className="fa-solid fa-trash" aria-hidden /> Remove from diagram
            </button>
          </div>
        );
      })()}

      {showJoinContextMenu && currentConditionLine && currentJoin && (
        <div id="er-join-context-menu" className={`fixed z-[10001] w-64 rounded shadow-lg border py-1 ${theme.mainContentSection}`} style={{ left: joinContextMenuPosition.x, top: joinContextMenuPosition.y }} onClick={(e) => e.stopPropagation()}>
          <button type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`} onClick={() => removeJoinConditionLines()}>
            <i className="fa-solid fa-trash" aria-hidden /> Remove
          </button>
          <div className="border-t my-1" />
          <button type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu} whitespace-nowrap`} onClick={() => updateJoinMethod(true)}>
            Select All Rows From {currentConditionLine.LeftSideTable}
          </button>
          <button type="button" className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu} whitespace-nowrap`} onClick={() => updateJoinMethod(false)}>
            Select All Rows From {currentConditionLine.RightSideTable}
          </button>
          <div className="border-t my-1" />
          <div className="px-4 py-2 text-xs text-gray-600 dark:text-gray-400">
            <div className="font-semibold">Condition:</div>
            <div className="text-[10px] mt-1 italic whitespace-normal">
              {currentConditionLine.LeftSideTable && currentConditionLine.LeftSideColumn && currentConditionLine.RightSideTable && currentConditionLine.RightSideColumn
                ? `[${currentConditionLine.LeftSideTable}].[${currentConditionLine.LeftSideColumn}] = [${currentConditionLine.RightSideTable}].[${currentConditionLine.RightSideColumn}]`
                : currentJoin.JoinConditionDisplay || 'N/A'}
            </div>
          </div>
        </div>
      )}

      {previewTableInfo && (
        <TableDataPreview
          isOpen={showPreviewPopup}
          onClose={() => { setShowPreviewPopup(false); setPreviewTableInfo(null); }}
          tableName={previewTableInfo.tableName}
          dataSourceRegisterId={previewTableInfo.dataSourceRegisterId}
          schemaOwner={previewTableInfo.schemaOwner}
        />
      )}

      {tableDesignInfo && (
        <MetaDataTableDesign
          isOpen={showTableDesignPopup}
          onClose={() => { setShowTableDesignPopup(false); setTableDesignInfo(null); }}
          onSave={handleNewTableSaved}
          tableName={tableDesignInfo.tableName}
          dataSourceRegisterId={tableDesignInfo.dataSourceRegisterId}
          schemaOwner={tableDesignInfo.schemaOwner}
          applicationId={tableDesignInfo.applicationId}
        />
      )}

      {showTableSelectorPopup && (
        <div className="fixed inset-0 z-[10002] flex items-center justify-center bg-black/50" onClick={() => setShowTableSelectorPopup(false)}>
          <div className={`flex flex-col rounded-lg shadow-xl border ${theme.mainContentSection}`} style={{ width: 'min(640px, 92vw)', maxHeight: '80vh' }} onClick={(e) => e.stopPropagation()}>
            <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
              <h3 className={`text-base font-semibold ${theme.title}`}>Select Tables to Add to Diagram</h3>
              <button type="button" onClick={() => setShowTableSelectorPopup(false)} className={`p-1.5 rounded ${theme.button_default}`} aria-label="Close">
                <i className="fa-solid fa-times" aria-hidden />
              </button>
            </div>
            <div className={`px-4 py-3 border-b ${theme.mainContentSection}`}>
              <input
                type="text"
                value={tableSelectorSearch}
                onChange={(e) => setTableSelectorSearch(e.target.value)}
                placeholder="Search table name..."
                className={`w-full px-3 py-2 text-sm border rounded ${theme.inputBox}`}
              />
            </div>
            <div className="flex-1 overflow-y-auto min-h-[280px] max-h-[50vh] px-4 py-2">
              {schemaTableList.length === 0 ? (
                <div className={`py-8 text-center text-sm ${theme.label}`}>No tables. Select datasource and refresh.</div>
              ) : (
                (() => {
                  const searchLower = tableSelectorSearch.trim().toLowerCase();
                  const filtered = searchLower
                    ? schemaTableList.filter((t: any) => (t.uiDisplayName ?? (t.SchemaOwner ? `${t.SchemaOwner}.${t.Name}` : t.Name)).toLowerCase().includes(searchLower))
                    : schemaTableList;
                  return filtered.length === 0 ? (
                    <div className={`py-8 text-center text-sm ${theme.label}`}>No tables match search.</div>
                  ) : (
                    <div className="space-y-1">
                      {filtered.map((t: any) => {
                        const key = t.SchemaOwner ? `${t.SchemaOwner}.${t.Name}` : t.Name;
                        const inDiagram = dictTablesEntries.some(([, o]) => (o as any).TableName?.toLowerCase() === t.Name?.toLowerCase());
                        const isSelected = tableSelectorSelected.has(key);
                        return (
                          <label
                            key={key}
                            className={`flex items-center gap-3 px-3 py-2 rounded border cursor-pointer transition-colors ${inDiagram ? 'opacity-60 cursor-not-allowed' : `${theme.contextMenu} hover:bg-gray-100 dark:hover:bg-gray-700/50`}`}
                          >
                            <input
                              type="checkbox"
                              checked={inDiagram || isSelected}
                              disabled={inDiagram}
                              onChange={() => {
                                if (inDiagram) return;
                                setTableSelectorSelected((prev) => {
                                  const next = new Set(prev);
                                  if (next.has(key)) next.delete(key);
                                  else next.add(key);
                                  return next;
                                });
                              }}
                              className="rounded shrink-0"
                            />
                            <i className="fa-solid fa-table text-sm opacity-70 shrink-0" aria-hidden />
                            <span className={`flex-1 text-sm ${theme.title}`}>{t.uiDisplayName ?? key}</span>
                            {inDiagram && <span className={`text-xs ${theme.label}`}>In diagram</span>}
                          </label>
                        );
                      })}
                    </div>
                  );
                })()
              )}
            </div>
            <div className={`flex justify-end gap-2 px-4 py-3 border-t ${theme.mainContentSection}`}>
              <button type="button" onClick={() => setShowTableSelectorPopup(false)} className={`px-4 py-2 text-sm rounded-[4px] ${theme.button_default}`}>Cancel</button>
              <button
                type="button"
                disabled={tableSelectorSelected.size === 0}
                onClick={async () => {
                  const toAdd = Array.from(tableSelectorSelected).map((k) => {
                    const dotIdx = k.indexOf('.');
                    return { Key: dotIdx >= 0 ? k.substring(0, dotIdx) : null, Value: dotIdx >= 0 ? k.substring(dotIdx + 1) : k };
                  });
                  if (toAdd.length > 0) {
                    await addTables(toAdd);
                    setTableSelectorSelected(new Set());
                    setShowTableSelectorPopup(false);
                  }
                }}
                className={`px-4 py-2 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Add Selected ({tableSelectorSelected.size})
              </button>
            </div>
          </div>
        </div>
      )}

      {showExportDdlModal && (
        <div className="fixed inset-0 z-[10002] flex items-center justify-center bg-black/50" onClick={() => setShowExportDdlModal(false)}>
          <div className={`max-w-2xl w-full max-h-[80vh] flex flex-col rounded shadow-xl ${theme.mainContentSection}`} onClick={(e) => e.stopPropagation()}>
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Export DDL</span>
              <button type="button" onClick={() => setShowExportDdlModal(false)} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>Close</button>
            </div>
            <div className="flex-1 overflow-auto p-3">
              <pre className={`text-xs font-mono whitespace-pre-wrap break-words ${theme.label}`} style={{ maxHeight: '60vh' }}>{exportDdlScript}</pre>
            </div>
            <div className={`flex justify-end gap-2 px-3 py-2 border-t ${theme.mainContentSection}`}>
              <button type="button" onClick={() => { navigator.clipboard.writeText(exportDdlScript); showInfo('DDL copied to clipboard'); }} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-copy mr-1" aria-hidden /> Copy
              </button>
              <button type="button" onClick={() => setShowExportDdlModal(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Close</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ErDiagramEditor;
