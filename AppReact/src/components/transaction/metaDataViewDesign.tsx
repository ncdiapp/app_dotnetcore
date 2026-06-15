import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { addErrorMessage, MessageType } from '../../redux/features/ui/feedback/errorMessageSlice';
import TableDataPreview from './TableDataPreview';
import MetaDataTableDesign from './metaDataTableDesign';

interface MetaDataViewDesignProps {
  isOpen: boolean;
  onClose: () => void;
  onSave?: () => void;
  viewName?: string | null;
  dataSourceRegisterId: number | null;
  schemaOwner?: string | null;
  applicationId?: number | null;
  isNewView?: boolean;
  // Embedded query-builder mode (Angular: directivOutercontrol)
  isEmbeddedByOtherPage?: boolean;
  initialQueryText?: string | null;
  onQueryBuilt?: (queryText: string) => void;
}

const MetaDataViewDesign: React.FC<MetaDataViewDesignProps> = ({
  isOpen,
  onClose,
  onSave,
  viewName = null,
  dataSourceRegisterId,
  schemaOwner = null,
  applicationId = null,
  isNewView = true,
  isEmbeddedByOtherPage = false,
  initialQueryText = null,
  onQueryBuilt
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showError, showInfo, showValidationMessages, logMessage } = useErrorMessage();

  const queryTextAreaRef = useRef<HTMLTextAreaElement>(null);
  const flexQueryResultRef = useRef<any>(null);
  const flexAvailableTablesRef = useRef<any>(null);
  const flexSelectedColumnsRef = useRef<any>(null);
  const diagramContainerRef = useRef<HTMLDivElement>(null);
  const [showPreviewPopup, setShowPreviewPopup] = useState(false);
  const [previewTableInfo, setPreviewTableInfo] = useState<{ tableName: string; dataSourceRegisterId: number | null; schemaOwner: string | null } | null>(null);
  const [showTableDesignPopup, setShowTableDesignPopup] = useState(false);
  const [tableDesignInfo, setTableDesignInfo] = useState<{ tableName: string | null; dataSourceRegisterId: number | null; schemaOwner: string | null; applicationId: number | null } | null>(null);
  const [isFullscreen, setIsFullscreen] = useState<boolean>(false);

  // View data model
  const [viewNameValue, setViewNameValue] = useState<string>('');
  const [queryText, setQueryText] = useState<string>('');
  const [queryResult, setQueryResult] = useState<any[]>([]);
  const [queryResultCV, setQueryResultCV] = useState<CollectionView | null>(null);
  const [databaseSchemaData, setDatabaseSchemaData] = useState<any[]>([]);
  const [currentSourceTableObj, setCurrentSourceTableObj] = useState<any | null>(null);

  // View DTO structure (matching AngularJS)
  const [viewDto, setViewDto] = useState<{
    DictAllColumns: { [key: string]: { [key: string]: boolean } };
    DictTables: { [key: string]: any };
    Joins: any[];
    SelectedColumnsList: any[];
    WhereConditionFilterColumns: any[];
    QueryString: string;
    DataSourceRegisterId: number | null;
  }>({
    DictAllColumns: {},
    DictTables: {},
    Joins: [],
    SelectedColumnsList: [],
    WhereConditionFilterColumns: [],
    QueryString: '',
    DataSourceRegisterId: null
  });

  const [selectedColumnsListCV, setSelectedColumnsListCV] = useState<CollectionView | null>(null);
  const [isSelectedColumnChanged, setIsSelectedColumnChanged] = useState(false);
  const [queryChanged, setQueryChanged] = useState(false);

  // Ref to track latest viewDto for async operations
  const viewDtoRef = useRef(viewDto);
  const debounceTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const isSelectedColumnChangedRef = useRef(false);

  // Keep viewDtoRef in sync with viewDto state
  useEffect(() => {
    viewDtoRef.current = viewDto;
  }, [viewDto]);

  // Initialize from embedded query-builder props (Angular: queryBuilderOutControl.queryText)
  useEffect(() => {
    if (isEmbeddedByOtherPage && initialQueryText != null) {
      setQueryText(initialQueryText);
      setViewDto((prev) => ({
        ...prev,
        QueryString: initialQueryText,
        DataSourceRegisterId: dataSourceRegisterId
      }));
      setQueryChanged(false);
    }
  }, [isEmbeddedByOtherPage, initialQueryText, dataSourceRegisterId]);

  // Memoize the available tables CollectionView to prevent flashing during drag operations
  const availableTablesCV = useMemo(() => {
    return new CollectionView<any>(databaseSchemaData);
  }, [databaseSchemaData]);

  // Resizable panel sizes
  const [leftPanelWidth, setLeftPanelWidth] = useState(300);
  const [diagramWidth, setDiagramWidth] = useState(50);
  const [rightPanelWidth, setRightPanelWidth] = useState(500); // Fixed width in pixels for right panel
  const [selectedColumnsHeight, setSelectedColumnsHeight] = useState(30);
  const [isResizingHorizontal, setIsResizingHorizontal] = useState(false);
  const [isResizingVertical, setIsResizingVertical] = useState(false);
  const [queryTextHeight, setQueryTextHeight] = useState(50); // Percentage height for query text area
  const [isResizingQueryText, setIsResizingQueryText] = useState(false);
  const [isResizingDiagram, setIsResizingDiagram] = useState(false);

  // Store initial positions for resize calculations to prevent jumping
  const initialResizeStateRef = useRef<{ startX: number; startWidth: number } | null>(null);
  const initialDiagramResizeStateRef = useRef<{ startX: number; startWidth: number } | null>(null);
  const initialQueryTextResizeStateRef = useRef<{ startY: number; startHeight: number } | null>(null);

  // Table dragging state
  const [draggingTable, setDraggingTable] = useState<string | null>(null);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });

  // Column dragging state
  const [draggingColumn, setDraggingColumn] = useState<{ sourceTableId: string; sourceColumnId: string } | null>(null);
  const [dragHelperVisible, setDragHelperVisible] = useState(false);
  const [dragHelperPosition, setDragHelperPosition] = useState({ x: 0, y: 0 });

  // Table-Column delimiter constant
  const TABLE_COLUMN_DELIMITER = '_____';

  // Context menu states
  const [showTableContextMenu, setShowTableContextMenu] = useState<string | null>(null);
  const [tableContextMenuPosition, setTableContextMenuPosition] = useState({ x: 0, y: 0 });
  const [showFkRefSubmenu, setShowFkRefSubmenu] = useState<string | null>(null);
  const [showFkRefedSubmenu, setShowFkRefedSubmenu] = useState<string | null>(null);
  const [showJoinContextMenu, setShowJoinContextMenu] = useState(false);
  const [joinContextMenuPosition, setJoinContextMenuPosition] = useState({ x: 0, y: 0 });
  const [currentConditionLine, setCurrentConditionLine] = useState<any | null>(null);
  const [currentJoin, setCurrentJoin] = useState<any | null>(null);

  // Load available tables
  const loadAvailableTableData = useCallback(async (isForceRefresh: boolean = false) => {
    if (!dataSourceRegisterId) return;

    try {
      dispatch(setIsBusy());
      const data = await schemaMetadataService.getDataSourceTableAndViewList(
        dataSourceRegisterId,
        null,
        null
      );

      // Build UI display names
      const dataWithDisplayNames = data.map((item: any) => ({
        ...item,
        uiDisplayName: item.SchemaOwner ? `${item.SchemaOwner}.${item.Name}` : item.Name
      }));

      setDatabaseSchemaData(dataWithDisplayNames);
    } catch (error: any) {
      showError(error.message || 'Failed to load tables');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataSourceRegisterId, dispatch, showError]);

  // Initialize view DTO
  const initializeViewDto = useCallback(() => {
    const newViewDto = {
      DictAllColumns: {},
      DictTables: {},
      Joins: [],
      SelectedColumnsList: [],
      WhereConditionFilterColumns: [],
      QueryString: '',
      DataSourceRegisterId: dataSourceRegisterId
    };
    setViewDto(newViewDto);
    setSelectedColumnsListCV(new CollectionView<any>([]));
    setQueryText('');
    setIsSelectedColumnChanged(false);
    setQueryChanged(false);
  }, [dataSourceRegisterId]);

  // Load view data if editing existing view
  const loadViewData = useCallback(async () => {
    if (!dataSourceRegisterId) return;

    if (!viewName) {
      setViewNameValue('');
      // Embedded query builder (e.g. command SQL editor): keep seed query; do not wipe via initializeViewDto().
      if (isEmbeddedByOtherPage) {
        const seed = initialQueryText ?? '';
        setQueryText(seed);
        const initialViewDto = {
          DictAllColumns: {},
          DictTables: {},
          Joins: [],
          SelectedColumnsList: [],
          WhereConditionFilterColumns: [],
          QueryString: seed,
          DataSourceRegisterId: dataSourceRegisterId
        };
        setViewDto(initialViewDto);
        setSelectedColumnsListCV(new CollectionView<any>([]));
        setIsSelectedColumnChanged(false);
        setQueryChanged(!!String(seed).trim());
        await loadAvailableTableData();
        if (String(seed).trim()) {
          await loadViewDtoFromQuery(initialViewDto);
        }
        return;
      }
      initializeViewDto();
      await loadAvailableTableData();
      return;
    }

    try {
      dispatch(setIsBusy());

      // Load query text
      const queryTextValue = await schemaMetadataService.getViewQueryText(viewName);

      setViewNameValue(viewName);
      setQueryText(queryTextValue || '');

      // Initialize view DTO with query
      const initialViewDto = {
        DictAllColumns: {},
        DictTables: {},
        Joins: [],
        SelectedColumnsList: [],
        WhereConditionFilterColumns: [],
        QueryString: queryTextValue || '',
        DataSourceRegisterId: dataSourceRegisterId
      };
      setViewDto(initialViewDto);

      // Parse query to build diagram
      if (queryTextValue) {
        await loadViewDtoFromQuery(initialViewDto);
      } else {
        setSelectedColumnsListCV(new CollectionView<any>([]));
      }

      await loadAvailableTableData();
    } catch (error: any) {
      showError(error.message || 'Failed to load view data');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    viewName,
    dataSourceRegisterId,
    dispatch,
    showError,
    initializeViewDto,
    loadAvailableTableData,
    isEmbeddedByOtherPage,
    initialQueryText,
  ]);

  // Load view DTO from query (parse query and build diagram)
  const loadViewDtoFromQuery = useCallback(async (currentViewDto: any) => {
    if (!currentViewDto.QueryString || !dataSourceRegisterId) return;

    try {
      dispatch(setIsBusy());
      const updatedViewDto = await schemaMetadataService.updateDatabaseViewDtoFromQuery({
        ...currentViewDto,
        DataSourceRegisterId: dataSourceRegisterId
      });

      if (updatedViewDto) {
        if (updatedViewDto.ErrorMessage) {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: updatedViewDto.ErrorMessage
            }]
          });
        } else {
          // Use the parsed data from the API - don't overwrite with empty initial state
          // Only preserve table positions if they already exist in the current viewDto (for user drag operations)
          if (viewDto.DictTables && updatedViewDto.DictTables && Object.keys(viewDto.DictTables).length > 0) {
            Object.keys(updatedViewDto.DictTables).forEach((key) => {
              if (viewDto.DictTables[key] && viewDto.DictTables[key].PositionX !== undefined) {
                // Preserve user's table positions if they exist
                updatedViewDto.DictTables[key].PositionX = viewDto.DictTables[key].PositionX;
                updatedViewDto.DictTables[key].PositionY = viewDto.DictTables[key].PositionY;
                updatedViewDto.DictTables[key].Width = viewDto.DictTables[key].Width || 200;
                updatedViewDto.DictTables[key].Height = viewDto.DictTables[key].Height || 200;
              }
            });
          }

          // Ensure DictTables and DictAllColumns are set from parsed data
          if (!updatedViewDto.DictTables) {
            updatedViewDto.DictTables = {};
          }
          if (!updatedViewDto.DictAllColumns) {
            updatedViewDto.DictAllColumns = {};
          }

          setViewDto(updatedViewDto);
          setQueryText(updatedViewDto.QueryString || '');
          setSelectedColumnsListCV(new CollectionView<any>(updatedViewDto.SelectedColumnsList || []));
          setIsSelectedColumnChanged(false);
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to parse query');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataSourceRegisterId, dispatch, showError, showValidationMessages, viewDto.DictTables]);

  // Add tables to diagram
  const addTables = useCallback(async (ownerTableNamePairList: Array<{ Key: string | null; Value: string }>) => {
    if (!ownerTableNamePairList || ownerTableNamePairList.length === 0) return;

    try {
      dispatch(setIsBusy());
      const tableAddRemoveDto = {
        ...viewDto,
        NeedToAddOwnerTablePairList: ownerTableNamePairList,
        DataSourceRegisterId: dataSourceRegisterId
      };

      const updatedViewDto = await schemaMetadataService.addTablesToDatabaseView(tableAddRemoveDto);

      if (updatedViewDto) {
        if (updatedViewDto.ErrorMessage) {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: updatedViewDto.ErrorMessage
            }]
          });
        } else {
          // Preserve table positions
          if (viewDto.DictTables) {
            Object.keys(viewDto.DictTables).forEach((key) => {
              if (updatedViewDto.DictTables[key]) {
                updatedViewDto.DictTables[key].PositionX = viewDto.DictTables[key].PositionX || 0;
                updatedViewDto.DictTables[key].PositionY = viewDto.DictTables[key].PositionY || 0;
                updatedViewDto.DictTables[key].Width = viewDto.DictTables[key].Width || 200;
                updatedViewDto.DictTables[key].Height = viewDto.DictTables[key].Height || 200;
              }
            });
          }

          setViewDto(updatedViewDto);
          setQueryText(updatedViewDto.QueryString || '');
          setSelectedColumnsListCV(new CollectionView<any>(updatedViewDto.SelectedColumnsList || []));
          setIsSelectedColumnChanged(false);
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to add tables');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [viewDto, dataSourceRegisterId, dispatch, showError, showValidationMessages]);

  // Remove tables from diagram
  const removeTables = useCallback(async (needToRemoveUniqTableOrAliasNames: string[]) => {
    if (!needToRemoveUniqTableOrAliasNames || needToRemoveUniqTableOrAliasNames.length === 0) return;

    try {
      dispatch(setIsBusy());
      const tableAddRemoveDto = {
        ...viewDto,
        NeedToRemoveUniqTableOrAliasNames: needToRemoveUniqTableOrAliasNames,
        DataSourceRegisterId: dataSourceRegisterId
      };

      const updatedViewDto = await schemaMetadataService.removeTablesFromDatabaseView(tableAddRemoveDto);

      if (updatedViewDto) {
        if (updatedViewDto.ErrorMessage) {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: updatedViewDto.ErrorMessage
            }]
          });
        } else {
          // Clean up joins that reference removed tables (matching AngularJS behavior)
          const cleanedViewDto = { ...updatedViewDto };
          if (cleanedViewDto.Joins && cleanedViewDto.DictTables) {
            const existingTableNames = Object.keys(cleanedViewDto.DictTables).map(name => name.toLowerCase());
            cleanedViewDto.Joins = cleanedViewDto.Joins.filter((join: any) => {
              if (!join.JoinConditionList || join.JoinConditionList.length === 0) return false;
              
              // Check if all join conditions reference existing tables
              return join.JoinConditionList.some((condition: any) => {
                const leftTable = condition.LeftSideTable?.toLowerCase();
                const rightTable = condition.RightSideTable?.toLowerCase();
                return leftTable && rightTable && 
                       existingTableNames.includes(leftTable) && 
                       existingTableNames.includes(rightTable);
              });
            });
            
            // Also clean up join conditions within remaining joins
            cleanedViewDto.Joins = cleanedViewDto.Joins.map((join: any) => ({
              ...join,
              JoinConditionList: join.JoinConditionList?.filter((condition: any) => {
                const leftTable = condition.LeftSideTable?.toLowerCase();
                const rightTable = condition.RightSideTable?.toLowerCase();
                return leftTable && rightTable && 
                       existingTableNames.includes(leftTable) && 
                       existingTableNames.includes(rightTable);
              }) || []
            })).filter((join: any) => join.JoinConditionList && join.JoinConditionList.length > 0);
          }
          
          setViewDto(cleanedViewDto);
          setQueryText(cleanedViewDto.QueryString || '');
          setSelectedColumnsListCV(new CollectionView<any>(cleanedViewDto.SelectedColumnsList || []));
          setIsSelectedColumnChanged(false);
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to remove tables');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [viewDto, dataSourceRegisterId, dispatch, showError, showValidationMessages]);

  // Update selected columns (when checkboxes change)
  const updateDatabaseViewSelectedColumns = useCallback(async (isOnlyUpdateQueryText: boolean = false) => {
    if (!isSelectedColumnChangedRef.current) return;

    // Capture the current state at the time of API call
    const stateAtCallTime = viewDtoRef.current;
    const stateAtCallTimeString = JSON.stringify(stateAtCallTime.DictAllColumns);

    try {
      dispatch(setIsBusy());
      const updatedViewDto = await schemaMetadataService.updateDatabaseViewSelectedColumns({
        ...stateAtCallTime,
        DataSourceRegisterId: dataSourceRegisterId
      });

      if (updatedViewDto) {
        if (updatedViewDto.ErrorMessage) {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: updatedViewDto.ErrorMessage
            }]
          });
        } else {
          // Check if state has changed since API call was made
          const currentState = viewDtoRef.current;
          const currentStateString = JSON.stringify(currentState.DictAllColumns);

          // Only update if state hasn't changed, or merge the changes
          if (currentStateString === stateAtCallTimeString) {
            // State hasn't changed, safe to update
            setIsSelectedColumnChanged(false);
            isSelectedColumnChangedRef.current = false;

            if (isOnlyUpdateQueryText) {
              setQueryText(updatedViewDto.QueryString || '');
              // Also update the CollectionView to reflect the alias changes
              setSelectedColumnsListCV(new CollectionView<any>(updatedViewDto.SelectedColumnsList || []));
            } else {
              // Preserve table positions and column selections from the latest state
              updatedViewDto.DictTables = currentState.DictTables;
              updatedViewDto.DictAllColumns = currentState.DictAllColumns;

              setViewDto(updatedViewDto);
              setQueryText(updatedViewDto.QueryString || '');
              setSelectedColumnsListCV(new CollectionView<any>(updatedViewDto.SelectedColumnsList || []));
            }
          } else {
            // State has changed since API call - merge the response but keep current DictAllColumns
            setIsSelectedColumnChanged(true); // Keep flag true to trigger another update
            isSelectedColumnChangedRef.current = true;
            updatedViewDto.DictTables = currentState.DictTables;
            updatedViewDto.DictAllColumns = currentState.DictAllColumns;

            setViewDto(updatedViewDto);
            setQueryText(updatedViewDto.QueryString || '');
            setSelectedColumnsListCV(new CollectionView<any>(updatedViewDto.SelectedColumnsList || []));
          }
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to update selected columns');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataSourceRegisterId, dispatch, showError, showValidationMessages]);

  // Handle diagram changed (column checkbox changed)
  const diagramChanged = useCallback(() => {
    // Clear any existing debounce timeout to reset the timer
    if (debounceTimeoutRef.current) {
      clearTimeout(debounceTimeoutRef.current);
      debounceTimeoutRef.current = null;
    }

    setIsSelectedColumnChanged(true);
    isSelectedColumnChangedRef.current = true;

    // Set new debounce timeout - this will be reset on each click
    debounceTimeoutRef.current = setTimeout(() => {
      updateDatabaseViewSelectedColumns();
      debounceTimeoutRef.current = null;
    }, 800); //  debounce
  }, [updateDatabaseViewSelectedColumns]);

  // Handle query text changed
  const handleQueryChanged = useCallback(() => {
    setQueryChanged(true);
  }, []);

  // Handle drop table on diagram
  const handleDropToDiagram = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (currentSourceTableObj) {
      const ownerTableNamePairList = [{
        Key: currentSourceTableObj.SchemaOwner || null,
        Value: currentSourceTableObj.Name
      }];
      addTables(ownerTableNamePairList);
      setCurrentSourceTableObj(null);
    }
  }, [currentSourceTableObj, addTables]);

  // Handle drag over diagram
  const handleDragOverDiagram = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    e.dataTransfer.dropEffect = 'copy';
  }, []);

  // Handle table drag start
  const handleTableDragStart = useCallback((e: React.DragEvent, tableItem: any) => {
    setCurrentSourceTableObj(tableItem);
    e.dataTransfer.effectAllowed = 'copy';
  }, []);

  // Handle table box drag start (in diagram) - only from header
  const handleTableBoxDragStart = useCallback((e: React.MouseEvent, uniqTableOrAliasName: string) => {
    // Only allow dragging from the header area (check if clicked on header or its children)
    const target = e.target as HTMLElement;
    const isHeaderClick = target.closest('.table-drag-handle') !== null;

    if (!isHeaderClick) {
      e.preventDefault();
      return;
    }

    const tableObj = viewDto.DictTables[uniqTableOrAliasName];
    if (!tableObj) return;

    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    setDragOffset({
      x: e.clientX - rect.left,
      y: e.clientY - rect.top
    });
    setDraggingTable(uniqTableOrAliasName);
  }, [viewDto.DictTables]);

  // Handle table box dragging
  useEffect(() => {
    if (!draggingTable) return;

    const handleMouseMove = (e: MouseEvent) => {
      if (diagramContainerRef.current) {
        const containerRect = diagramContainerRef.current.getBoundingClientRect();
        const newX = e.clientX - containerRect.left - dragOffset.x;
        const newY = e.clientY - containerRect.top - dragOffset.y;

        setViewDto(prev => {
          const updated = { ...prev };
          if (updated.DictTables[draggingTable]) {
            updated.DictTables[draggingTable] = {
              ...updated.DictTables[draggingTable],
              PositionX: Math.max(0, newX),
              PositionY: Math.max(0, newY)
            };
          }
          return updated;
        });
      }
    };

    const handleMouseUp = () => {
      setDraggingTable(null);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [draggingTable, dragOffset]);

  // Uncheck all table columns
  const uncheckAllTableColumns = useCallback((uniqTableOrAliasName: string) => {
    const tableKey = uniqTableOrAliasName.toLowerCase();
    if (viewDto.DictAllColumns[tableKey]) {
      setViewDto(prev => {
        const updated = { ...prev };
        const updatedColumns = { ...updated.DictAllColumns[tableKey] };
        Object.keys(updatedColumns).forEach(columnName => {
          updatedColumns[columnName] = false;
        });
        updated.DictAllColumns[tableKey] = updatedColumns;
        return updated;
      });
      diagramChanged();
    }
  }, [viewDto.DictAllColumns, diagramChanged]);

  // Check/uncheck all columns for a table
  const checkAllTableColumns = useCallback((uniqTableOrAliasName: string, checked: boolean) => {
    const tableKey = uniqTableOrAliasName.toLowerCase();
    if (viewDto.DictAllColumns[tableKey]) {
      setViewDto(prev => {
        const updated = { ...prev };
        const updatedColumns = { ...updated.DictAllColumns[tableKey] };
        Object.keys(updatedColumns).forEach(columnName => {
          updatedColumns[columnName] = checked;
        });
        updated.DictAllColumns[tableKey] = updatedColumns;
        return updated;
      });
      diagramChanged();
    }
  }, [viewDto.DictAllColumns, diagramChanged]);

  // Delete one table
  const deleteOneTable = useCallback((uniqTableOrAliasName: string) => {
    removeTables([uniqTableOrAliasName]);
  }, [removeTables]);

  // Remove join condition lines
  const removeJoinConditionLines = useCallback(async () => {
    console.log('removeJoinConditionLines called', { currentConditionLine });
    if (!currentConditionLine) {
      console.log('No currentConditionLine, returning');
      return;
    }

    // First, deselect all lines and select the current one (matching AngularJS behavior)
    const viewDtoWithSelection = { ...viewDto };
    viewDtoWithSelection.Joins = viewDtoWithSelection.Joins.map((join: any) => ({
      ...join,
      JoinConditionList: join.JoinConditionList?.map((condition: any) => ({
        ...condition,
        isSelected: condition.GUID === currentConditionLine.GUID
      }))
    }));

    const needToRemoveGUIDs: string[] = [];
    viewDtoWithSelection.Joins.forEach((join: any) => {
      join.JoinConditionList?.forEach((condition: any) => {
        if (condition.isSelected && condition.GUID) {
          needToRemoveGUIDs.push(condition.GUID.toString());
        }
      });
    });

    if (needToRemoveGUIDs.length === 0) return;

    try {
      dispatch(setIsBusy());
      const joinUpdateDto = {
        ...viewDtoWithSelection,
        NeedToRemoveJoinConditionGUIDs: needToRemoveGUIDs,
        DataSourceRegisterId: dataSourceRegisterId
      };

      const updatedViewDto = await schemaMetadataService.removeJoinConditionLinesFromDatabaseView(joinUpdateDto);

      if (updatedViewDto) {
        if (updatedViewDto.ErrorMessage) {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: updatedViewDto.ErrorMessage
            }]
          });
        } else {
          // Preserve table positions and column selections
          updatedViewDto.DictTables = viewDto.DictTables;
          updatedViewDto.DictAllColumns = viewDto.DictAllColumns;

          setViewDto(updatedViewDto);
          setQueryText(updatedViewDto.QueryString || '');
          setSelectedColumnsListCV(new CollectionView<any>(updatedViewDto.SelectedColumnsList || []));
          setIsSelectedColumnChanged(false);
        }
      }
      setShowJoinContextMenu(false);
      setCurrentConditionLine(null);
      setCurrentJoin(null);
    } catch (error: any) {
      showError(error.message || 'Failed to remove join condition');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [viewDto, currentConditionLine, dataSourceRegisterId, dispatch, showError, showValidationMessages]);

  // Update join method
  const updateJoinMethod = useCallback(async (isUpdateJoinMethodForLeftTable: boolean) => {
    console.log('updateJoinMethod called', { isUpdateJoinMethodForLeftTable, currentConditionLine, currentJoin });
    if (!currentConditionLine || !currentJoin) {
      console.log('Missing currentConditionLine or currentJoin, returning');
      return;
    }

    try {
      dispatch(setIsBusy());
      const joinUpdateDto = {
        ...viewDto,
        NeedToUpdateJoinMethodConditionDto: currentConditionLine,
        IsUpdateJoinMethodForLeftTable: isUpdateJoinMethodForLeftTable,
        DataSourceRegisterId: dataSourceRegisterId
      };

      const updatedViewDto = await schemaMetadataService.updateDatabaseViewJoinMethod(joinUpdateDto);

      if (updatedViewDto) {
        if (updatedViewDto.ErrorMessage) {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: updatedViewDto.ErrorMessage
            }]
          });
        } else {
          // Preserve table positions and column selections
          updatedViewDto.DictTables = viewDto.DictTables;
          updatedViewDto.DictAllColumns = viewDto.DictAllColumns;

          setViewDto(updatedViewDto);
          setQueryText(updatedViewDto.QueryString || '');
          setSelectedColumnsListCV(new CollectionView<any>(updatedViewDto.SelectedColumnsList || []));
          setIsSelectedColumnChanged(false);
        }
      }
      setShowJoinContextMenu(false);
      setCurrentConditionLine(null);
      setCurrentJoin(null);
    } catch (error: any) {
      showError(error.message || 'Failed to update join method');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentConditionLine, currentJoin, viewDto, dataSourceRegisterId, dispatch, showError, showValidationMessages]);

  // Handle line selection (join click)
  const handleLineSelected = useCallback((e: React.MouseEvent, line: any, join: any) => {
    e.stopPropagation();

    // Deselect all lines first
    setViewDto(prev => {
      const updated = { ...prev };
      updated.Joins.forEach((j: any) => {
        j.JoinConditionList?.forEach((l: any) => {
          l.isSelected = false;
        });
      });
      return updated;
    });

    // Select this line
    line.isSelected = true;
    setCurrentConditionLine(line);
    setCurrentJoin(join);

    // Show context menu at click position
    setJoinContextMenuPosition({ x: e.clientX, y: e.clientY });
    setShowJoinContextMenu(true);
  }, []);

  // Handle table context menu
  const handleTableContextMenuClick = useCallback((e: React.MouseEvent, uniqTableOrAliasName: string) => {
    e.stopPropagation();
    setTableContextMenuPosition({ x: e.clientX, y: e.clientY });
    setShowTableContextMenu(uniqTableOrAliasName);
  }, []);

  // Add FK reference table (parent or child) - must be defined after addJoinConditionLine
  const addFkRefTable = useCallback(async (fkObj: any, isAddingFkChildTable: boolean, addFromUniqTableOrAliasName: string, addJoinConditionLineFn: (leftTable: string, leftColumn: string, rightTable: string, rightColumn: string) => Promise<void>) => {
    if (!fkObj || !addFromUniqTableOrAliasName || !databaseSchemaData) return;

    // Find the table in databaseSchemaData
    let tableDtoFound: any = null;
    for (let i = 0; i < databaseSchemaData.length; i++) {
      const tableDto = databaseSchemaData[i];
      if (isAddingFkChildTable) {
        if (fkObj.ChildTableName && tableDto.Name.toLowerCase() === fkObj.ChildTableName.toLowerCase()) {
          tableDtoFound = tableDto;
          break;
        }
      } else {
        if (fkObj.ParentTableName && tableDto.Name.toLowerCase() === fkObj.ParentTableName.toLowerCase()) {
          tableDtoFound = tableDto;
          break;
        }
      }
    }

    if (!tableDtoFound) {
      const tableName = isAddingFkChildTable ? fkObj.ChildTableName : fkObj.ParentTableName;
      showError(`The table ${tableName} is not allowed to be added.`);
      return;
    }

    // Add the table
    const ownerTableNamePairList = [{ Key: tableDtoFound.SchemaOwner, Value: tableDtoFound.Name }];
    const orgDictTables = { ...viewDto.DictTables };
    
    await addTables(ownerTableNamePairList);

    // After tables are added, mark new tables and create join condition
    setTimeout(() => {
      setViewDto(prev => {
        const updated = { ...prev };
        const newDictTables = { ...updated.DictTables };
        
        // Mark newly added tables
        Object.keys(newDictTables).forEach(key => {
          if (!orgDictTables[key]) {
            newDictTables[key] = { ...newDictTables[key], isNewAdded: true };
          }
        });
        updated.DictTables = newDictTables;

        // Find the newly added table and create join condition
        const sourceUniqTableOrAliasName = addFromUniqTableOrAliasName;
        let sourceColumnName = '';
        let targetUniqTableOrAliasName = '';
        let targetColumnName = '';

        if (isAddingFkChildTable) {
          sourceColumnName = fkObj.ParentTablePkColumnName;
          targetColumnName = fkObj.ChildTableFkColumnName;
        } else {
          sourceColumnName = fkObj.ChildTableFkColumnName;
          targetColumnName = fkObj.ParentTablePkColumnName;
        }

        Object.keys(newDictTables).forEach(uniqTableOrAliasName => {
          const tableDto = newDictTables[uniqTableOrAliasName];
          if (tableDto.isNewAdded) {
            if (isAddingFkChildTable) {
              if (tableDto.TableName.toLowerCase() === fkObj.ChildTableName.toLowerCase()) {
                targetUniqTableOrAliasName = uniqTableOrAliasName;
                addJoinConditionLineFn(sourceUniqTableOrAliasName, sourceColumnName, targetUniqTableOrAliasName, targetColumnName);
              }
            } else {
              if (tableDto.TableName.toLowerCase() === fkObj.ParentTableName.toLowerCase()) {
                targetUniqTableOrAliasName = uniqTableOrAliasName;
                addJoinConditionLineFn(sourceUniqTableOrAliasName, sourceColumnName, targetUniqTableOrAliasName, targetColumnName);
              }
            }
          }
        });

        return updated;
      });
    }, 100);
  }, [databaseSchemaData, viewDto.DictTables, addTables, showError]);

  // Add join condition line
  const addJoinConditionLine = useCallback(async (leftTable: string, leftColumn: string, rightTable: string, rightColumn: string) => {
    if (!leftTable || !leftColumn || !rightTable || !rightColumn || leftTable === rightTable) {
      return;
    }

    const dictTables = viewDto.DictTables;
    let leftSideSchemaOwner = '';
    let rightSideSchemaOwner = '';

    if (leftTable && dictTables[leftTable] && !dictTables[leftTable].TableAlias) {
      leftSideSchemaOwner = dictTables[leftTable].SchemaOwner || '';
    }

    if (rightTable && dictTables[rightTable] && !dictTables[rightTable].TableAlias) {
      rightSideSchemaOwner = dictTables[rightTable].SchemaOwner || '';
    }

    try {
      dispatch(setIsBusy());
      const joinUpdateDto = {
        ...viewDto,
        NeedToAddJoinCondition: {
          LeftSideSchemaOwner: leftSideSchemaOwner,
          LeftSideTable: leftTable,
          LeftSideColumn: leftColumn,
          RightSideSchemaOwner: rightSideSchemaOwner,
          RightSideTable: rightTable,
          RightSideColumn: rightColumn
        },
        DataSourceRegisterId: dataSourceRegisterId
      };

      const updatedViewDto = await schemaMetadataService.addOneJoinConditionLineToDatabaseView(joinUpdateDto);

      if (updatedViewDto) {
        if (updatedViewDto.ErrorMessage) {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: updatedViewDto.ErrorMessage
            }]
          });
        } else {
          // Preserve table positions and column selections
          updatedViewDto.DictTables = viewDto.DictTables;
          updatedViewDto.DictAllColumns = viewDto.DictAllColumns;

          setViewDto(updatedViewDto);
          setQueryText(updatedViewDto.QueryString || '');
          setSelectedColumnsListCV(new CollectionView<any>(updatedViewDto.SelectedColumnsList || []));
          setIsSelectedColumnChanged(false);
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to add join condition');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [viewDto, dataSourceRegisterId, dispatch, showError, showValidationMessages]);

  // Handle column drag start
  const handleColumnDragStart = useCallback((e: React.DragEvent, sourceTableId: string, sourceColumnId: string) => {
    setDraggingColumn({ sourceTableId, sourceColumnId });
    setDragHelperVisible(true);
    setDragHelperPosition({ x: e.clientX, y: e.clientY });
    e.dataTransfer.effectAllowed = 'copy';
    e.dataTransfer.setData('text/plain', ''); // Required for Firefox
  }, []);

  // Handle column dragging
  const handleColumnDrag = useCallback((e: React.DragEvent) => {
    if (draggingColumn) {
      setDragHelperPosition({ x: e.clientX, y: e.clientY });
    }
  }, [draggingColumn]);

  // Track mouse position for drag helper tooltip
  useEffect(() => {
    if (dragHelperVisible) {
      const handleMouseMove = (e: MouseEvent) => {
        setDragHelperPosition({ x: e.clientX, y: e.clientY });
      };

      document.addEventListener('mousemove', handleMouseMove);
      return () => {
        document.removeEventListener('mousemove', handleMouseMove);
      };
    }
  }, [dragHelperVisible]);

  // Handle column drag end
  const handleColumnDragEnd = useCallback(() => {
    setDraggingColumn(null);
    setDragHelperVisible(false);
  }, []);

  // Handle column drop
  const handleColumnDrop = useCallback((e: React.DragEvent, targetTableId: string, targetColumnId: string) => {
    e.preventDefault();
    e.stopPropagation();

    if (draggingColumn && draggingColumn.sourceTableId !== targetTableId) {
      addJoinConditionLine(
        draggingColumn.sourceTableId,
        draggingColumn.sourceColumnId,
        targetTableId,
        targetColumnId
      );
    }

    setDraggingColumn(null);
    setDragHelperVisible(false);
  }, [draggingColumn, addJoinConditionLine]);

  // Handle column drag over
  const handleColumnDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    e.dataTransfer.dropEffect = 'copy';
  }, []);

  // Preview table data
  const previewTableData = useCallback((tableObj: any) => {
    if (tableObj && tableObj.Name && dataSourceRegisterId) {
      setPreviewTableInfo({
        tableName: tableObj.Name,
        dataSourceRegisterId: dataSourceRegisterId,
        schemaOwner: tableObj.SchemaOwner || null
      });
      setShowPreviewPopup(true);
    }
  }, [dataSourceRegisterId]);

  // Open table design
  const openTableDesign = useCallback((tableObj: any) => {
    if (tableObj && tableObj.TableName && dataSourceRegisterId) {
      setTableDesignInfo({
        tableName: tableObj.TableName,
        dataSourceRegisterId: dataSourceRegisterId,
        schemaOwner: tableObj.SchemaOwner || null,
        applicationId: applicationId
      });
      setShowTableDesignPopup(true);
    }
  }, [dataSourceRegisterId, applicationId]);

  // Execute query
  const executeQuery = useCallback(async () => {
    if (!queryText?.trim() || !dataSourceRegisterId) {
      showError('Please enter a query and select a data source');
      return;
    }

    try {
      dispatch(setIsBusy());
      const keyValue = {
        Key: dataSourceRegisterId,
        Value: queryText
      };

      const result = await schemaMetadataService.executeQueryResult(keyValue);

      const resultData = result.DataRowList || [];
      setQueryResult(resultData);
      setQueryResultCV(new CollectionView<any>(resultData));

      if (result.ErrorMessage) {
        if (result.ErrorMessage.indexOf("Query completed successfully") >= 0) {
          showInfo(result.ErrorMessage, true);
        } else {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: result.ErrorMessage
            }]
          });
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to execute query');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [queryText, dataSourceRegisterId, dispatch, showError, showInfo, showValidationMessages]);

  // Save view
  const save = useCallback(async () => {
    if (!viewNameValue || !viewNameValue.trim()) {
      showError('Please enter a view name');
      return;
    }

    if (!queryText || !queryText.trim()) {
      showError('Please enter a query');
      return;
    }

    if (!dataSourceRegisterId) {
      showError('Data source is not available');
      return;
    }

    try {
      dispatch(setIsBusy());

      const databaseViewDto: any = {
        ViewName: viewNameValue.trim(),
        QueryString: queryText,
        IsNewView: isNewView
      };

      if (isNewView && applicationId) {
        databaseViewDto.ApplicationId = applicationId;
      }

      const result = await schemaMetadataService.saveDatabaseViewFromDesignQuery(databaseViewDto);

      if (result && result.ErrorMessage) {
        if (result.ErrorMessage.indexOf('System Error') >= 0) {
          showValidationMessages({
            Items: [{
              ItemType: 2,
              LocalizedMessage: result.ErrorMessage
            }]
          });
        } else {
          showInfo('View saved successfully');
          if (onSave) {
            onSave();
          }
          onClose();
        }
      } else {
        showInfo('View saved successfully');
        if (onSave) {
          onSave();
        }
        onClose();
      }
    } catch (error: any) {
      showError(error.message || 'Failed to save view');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [viewNameValue, queryText, isNewView, applicationId, dataSourceRegisterId, dispatch, showError, showInfo, showValidationMessages, onSave, onClose]);

  // Refresh
  const refresh = useCallback(() => {
    loadViewData();
  }, [loadViewData]);

  // Load data when component opens
  useEffect(() => {
    if (isOpen && dataSourceRegisterId) {
      loadViewData();
    }
  }, [isOpen, dataSourceRegisterId, loadViewData]);

  // Close context menus when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as HTMLElement;
      
      // Check if click is inside table context menu
      if (showTableContextMenu) {
        const tableMenu = document.querySelector('[role="menu"]');
        if (tableMenu && !tableMenu.contains(target)) {
          setShowTableContextMenu(null);
        }
      }
      
      // Check if click is inside join context menu
      if (showJoinContextMenu) {
        const joinMenu = document.getElementById('join-context-menu');
        // Only close if click is outside the menu
        if (joinMenu && !joinMenu.contains(target)) {
          setShowJoinContextMenu(false);
          setCurrentConditionLine(null);
          setCurrentJoin(null);
        }
      }
    };

    if (showTableContextMenu || showJoinContextMenu) {
      // Use a small delay to allow button clicks to fire first
      const timeoutId = setTimeout(() => {
        document.addEventListener('click', handleClickOutside, true);
      }, 0);
      
      return () => {
        clearTimeout(timeoutId);
        document.removeEventListener('click', handleClickOutside, true);
      };
    }
  }, [showTableContextMenu, showJoinContextMenu]);

  // Update query when it changes manually
  useEffect(() => {
    if (queryChanged && queryText) {
      const currentViewDto = {
        ...viewDto,
        QueryString: queryText,
        DataSourceRegisterId: dataSourceRegisterId
      };
      loadViewDtoFromQuery(currentViewDto);
      setQueryChanged(false);
    }
  }, [queryChanged, queryText, viewDto, dataSourceRegisterId, loadViewDtoFromQuery]);

  // Handle horizontal resize (left panel)
  const handleHorizontalResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    const container = document.querySelector('.main-content-area') as HTMLElement;
    if (container) {
      const containerRect = container.getBoundingClientRect();
      // Store initial mouse position and panel width to prevent jumping
      initialResizeStateRef.current = {
        startX: e.clientX,
        startWidth: leftPanelWidth
      };
    }
    setIsResizingHorizontal(true);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, [leftPanelWidth]);

  const handleHorizontalResize = useCallback((e: MouseEvent) => {
    if (!isResizingHorizontal || !initialResizeStateRef.current) return;
    const container = document.querySelector('.main-content-area') as HTMLElement;
    if (container) {
      const containerRect = container.getBoundingClientRect();
      // Calculate new width based on the difference from initial position
      const deltaX = e.clientX - initialResizeStateRef.current.startX;
      const newWidth = initialResizeStateRef.current.startWidth + deltaX;
      if (newWidth >= 200 && newWidth <= containerRect.width - 400) {
        setLeftPanelWidth(newWidth);
      }
    }
  }, [isResizingHorizontal]);

  const handleHorizontalResizeEnd = useCallback(() => {
    setIsResizingHorizontal(false);
    initialResizeStateRef.current = null; // Clear initial state
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Handle vertical resize (selected columns panel)
  const handleVerticalResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingVertical(true);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const handleVerticalResize = useCallback((e: MouseEvent) => {
    if (!isResizingVertical) return;
    const container = document.querySelector('.right-panel-container') as HTMLElement;
    if (container) {
      const containerRect = container.getBoundingClientRect();
      const newHeight = ((e.clientY - containerRect.top) / containerRect.height) * 100;
      if (newHeight >= 20 && newHeight <= 80) {
        setSelectedColumnsHeight(newHeight);
      }
    }
  }, [isResizingVertical]);

  const handleVerticalResizeEnd = useCallback(() => {
    setIsResizingVertical(false);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Handle query text/result vertical resize
  const handleQueryTextResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    const container = document.querySelector('.right-panel-container') as HTMLElement;
    if (container) {
      const queryTextResultContainer = container.querySelector('.queryTextAndResultContainer') as HTMLElement;
      if (queryTextResultContainer) {
        const queryTextResultRect = queryTextResultContainer.getBoundingClientRect();
        // Store initial mouse position and query text height to prevent jumping
        initialQueryTextResizeStateRef.current = {
          startY: e.clientY,
          startHeight: queryTextHeight
        };
      }
    }
    setIsResizingQueryText(true);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }, [queryTextHeight]);

  const handleQueryTextResize = useCallback((e: MouseEvent) => {
    if (!isResizingQueryText || !initialQueryTextResizeStateRef.current) return;
    const container = document.querySelector('.right-panel-container') as HTMLElement;
    if (container) {
      const queryTextResultContainer = container.querySelector('.queryTextAndResultContainer') as HTMLElement;
      if (queryTextResultContainer) {
        const queryTextResultRect = queryTextResultContainer.getBoundingClientRect();
        // Calculate new height based on the difference from initial position
        const deltaY = e.clientY - initialQueryTextResizeStateRef.current.startY;
        const deltaPercent = (deltaY / queryTextResultRect.height) * 100;
        const newHeight = initialQueryTextResizeStateRef.current.startHeight + deltaPercent;
        if (newHeight >= 20 && newHeight <= 80) {
          setQueryTextHeight(newHeight);
        }
      }
    }
  }, [isResizingQueryText]);

  const handleQueryTextResizeEnd = useCallback(() => {
    setIsResizingQueryText(false);
    initialQueryTextResizeStateRef.current = null; // Clear initial state
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Handle diagram resize (resizes right panel instead of diagram)
  const handleDiagramResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    const container = document.querySelector('.main-content-area') as HTMLElement;
    if (container) {
      const containerRect = container.getBoundingClientRect();
      // Store initial mouse position and right panel width to prevent jumping
      initialDiagramResizeStateRef.current = {
        startX: e.clientX,
        startWidth: rightPanelWidth
      };
    }
    setIsResizingDiagram(true);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, [rightPanelWidth]);

  const handleDiagramResize = useCallback((e: MouseEvent) => {
    if (!isResizingDiagram || !initialDiagramResizeStateRef.current) return;
    const container = document.querySelector('.main-content-area') as HTMLElement;
    if (container) {
      const containerRect = container.getBoundingClientRect();
      // Calculate new right panel width based on the difference from initial position
      // Moving mouse right decreases right panel width, moving left increases it
      const deltaX = initialDiagramResizeStateRef.current.startX - e.clientX; // Inverted because we're resizing from the right
      const newWidth = initialDiagramResizeStateRef.current.startWidth + deltaX;
      // Constrain right panel width between 300px and container width - 400px
      if (newWidth >= 300 && newWidth <= containerRect.width - 400) {
        setRightPanelWidth(newWidth);
      }
    }
  }, [isResizingDiagram]);

  const handleDiagramResizeEnd = useCallback(() => {
    setIsResizingDiagram(false);
    initialDiagramResizeStateRef.current = null; // Clear initial state
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Cleanup debounce timeout on unmount
  useEffect(() => {
    return () => {
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current);
        debounceTimeoutRef.current = null;
      }
    };
  }, []);

  // Set up resize event listeners
  useEffect(() => {
    if (isResizingHorizontal) {
      document.addEventListener('mousemove', handleHorizontalResize);
      document.addEventListener('mouseup', handleHorizontalResizeEnd);
      return () => {
        document.removeEventListener('mousemove', handleHorizontalResize);
        document.removeEventListener('mouseup', handleHorizontalResizeEnd);
      };
    }
  }, [isResizingHorizontal, handleHorizontalResize, handleHorizontalResizeEnd]);

  useEffect(() => {
    if (isResizingVertical) {
      document.addEventListener('mousemove', handleVerticalResize);
      document.addEventListener('mouseup', handleVerticalResizeEnd);
      return () => {
        document.removeEventListener('mousemove', handleVerticalResize);
        document.removeEventListener('mouseup', handleVerticalResizeEnd);
      };
    }
  }, [isResizingVertical, handleVerticalResize, handleVerticalResizeEnd]);

  // Set up query text resize event listeners
  useEffect(() => {
    if (isResizingQueryText) {
      document.addEventListener('mousemove', handleQueryTextResize);
      document.addEventListener('mouseup', handleQueryTextResizeEnd);
      return () => {
        document.removeEventListener('mousemove', handleQueryTextResize);
        document.removeEventListener('mouseup', handleQueryTextResizeEnd);
      };
    }
  }, [isResizingQueryText, handleQueryTextResize, handleQueryTextResizeEnd]);

  useEffect(() => {
    if (isResizingDiagram) {
      document.addEventListener('mousemove', handleDiagramResize);
      document.addEventListener('mouseup', handleDiagramResizeEnd);
      return () => {
        document.removeEventListener('mousemove', handleDiagramResize);
        document.removeEventListener('mouseup', handleDiagramResizeEnd);
      };
    }
  }, [isResizingDiagram, handleDiagramResize, handleDiagramResizeEnd]);

  // Get table position
  const getTablePosition = useCallback((tableName: string) => {
    const position = { x: 0, y: 0, width: 200, height: 200 };
    const tableObj = viewDto.DictTables[tableName];
    if (tableObj) {
      position.x = tableObj.PositionX || 0;
      position.y = tableObj.PositionY || 0;
      position.width = tableObj.Width || 200;
      position.height = tableObj.Height || 200;
    }
    return position;
  }, [viewDto.DictTables]);

  // Get column position (returns absolute Y position relative to diagram scrollable container, matching AngularJS element.position().top)
  const getColumnPosition = useCallback((columnId: string) => {
    const position = { x: 0, y: 0 };
    const element = document.getElementById(columnId);
    if (element) {
      // Find the scrollable diagram container (the 10000x10000px div that contains all tables)
      // Try multiple selectors to find the diagram container
      let diagramContainer: HTMLElement | null = null;

      // Method 1: Look for the div with 10000px dimensions
      let parent: HTMLElement | null = element.parentElement;
      while (parent && !diagramContainer) {
        const style = window.getComputedStyle(parent);
        if (parent.style.width === '10000px' || parent.style.height === '10000px' ||
          parent.getAttribute('style')?.includes('10000px')) {
          diagramContainer = parent;
          break;
        }
        parent = parent.parentElement;
      }

      // Method 2: Find by class or data attribute if available
      if (!diagramContainer) {
        diagramContainer = element.closest('[style*="10000px"]') as HTMLElement;
      }

      if (diagramContainer) {
        const diagramRect = diagramContainer.getBoundingClientRect();
        const elementRect = element.getBoundingClientRect();
        // Get absolute position relative to diagram container (matching jQuery position().top)
        // Account for scroll position
        const scrollTop = diagramContainer.scrollTop || 0;
        const scrollLeft = diagramContainer.scrollLeft || 0;
        position.y = elementRect.top - diagramRect.top + scrollTop;
        position.x = elementRect.left - diagramRect.left + scrollLeft;
      } else {
        // Fallback: calculate from table position + column offset within table
        const tableContainer = element.closest('.dbViewTableContainer');
        if (tableContainer) {
          const tableBox = tableContainer.closest('[id^="dbViewTable_"]');
          if (tableBox) {
            const tableName = tableBox.id.replace('dbViewTable_', '');
            const tablePos = getTablePosition(tableName);
            const tableContainerRect = tableContainer.getBoundingClientRect();
            const elementRect = element.getBoundingClientRect();
            // Column position = table absolute position + column offset within table container
            // Add 45px for table header height
            const columnOffsetY = elementRect.top - tableContainerRect.top;
            const columnOffsetX = elementRect.left - tableContainerRect.left;
            position.y = tablePos.y + 45 + columnOffsetY;
            position.x = tablePos.x + columnOffsetX;
          }
        }
      }
    }
    return position;
  }, [getTablePosition]);

  // Get line coordinates for SVG joins with proper arrow direction logic
  const getLineCoordinate = useCallback((join: any, line: any) => {
    const coordinate = {
      startX: 0, startY: 0, step1X: 0, step1Y: 0, step2X: 0, step2Y: 0,
      endX: 0, endY: 0, midX: 0, midY: 0, logoPath1: '', logoPath2: ''
    };

    const leftTablePosition = getTablePosition(line.LeftSideTable);
    const rightTablePosition = getTablePosition(line.RightSideTable);

    const leftColumnId = `${line.LeftSideTable}${TABLE_COLUMN_DELIMITER}${line.LeftSideColumn}`;
    const rightColumnId = `${line.RightSideTable}${TABLE_COLUMN_DELIMITER}${line.RightSideColumn}`;

    const leftColumnPos = getColumnPosition(leftColumnId);
    const rightColumnPos = getColumnPosition(rightColumnId);

    // Calculate start point (left table) - X is center of table
    coordinate.startX = leftTablePosition.x + (leftTablePosition.width / 2);
    coordinate.endX = rightTablePosition.x + (rightTablePosition.width / 2);

    // Calculate Y position based on column position (matching AngularJS logic exactly)
    if (leftColumnPos.y > 0 && rightColumnPos.y > 0) {
      // We have valid column positions - use them
      const leftColumnY = leftColumnPos.y; // Absolute position relative to scrollable container
      const leftColumnYRelative = leftColumnY - leftTablePosition.y; // Relative to table top

      if (leftColumnYRelative < 20) {
        coordinate.startY = leftTablePosition.y + 10;
      }
      else if (leftColumnYRelative >= leftTablePosition.height - 20) {
        coordinate.startY = leftTablePosition.y + leftTablePosition.height - 3;
      }
      else {
        coordinate.startY = leftTablePosition.y + leftColumnYRelative + 10;
      }

      const rightColumnY = rightColumnPos.y; // Absolute position
      const rightColumnYRelative = rightColumnY - rightTablePosition.y; // Relative to table top

      if (rightColumnYRelative < 20) {
        coordinate.endY = rightTablePosition.y + 10;
      }
      else if (rightColumnYRelative >= rightTablePosition.height - 20) {
        coordinate.endY = rightTablePosition.y + rightTablePosition.height - 3;
      }
      else {
        coordinate.endY = rightTablePosition.y + rightColumnYRelative + 10;
      }
    } else {
      // Fallback: use table center if column positions not found
      coordinate.startY = leftTablePosition.y + (leftTablePosition.height / 2);
      coordinate.endY = rightTablePosition.y + (rightTablePosition.height / 2);
    }

    // Calculate step points based on direction
    if (coordinate.startX <= coordinate.endX) {
      // Left to right
      coordinate.step1X = coordinate.startX + (leftTablePosition.width / 2) + 20;
      coordinate.step1Y = coordinate.startY;
      coordinate.step2X = coordinate.endX - (rightTablePosition.width / 2) - 20;
      coordinate.step2Y = coordinate.endY;
    } else {
      // Right to left
      coordinate.step1X = coordinate.startX - (leftTablePosition.width / 2) - 20;
      coordinate.step1Y = coordinate.startY;
      coordinate.step2X = coordinate.endX + (rightTablePosition.width / 2) + 20;
      coordinate.step2Y = coordinate.endY;
    }

    // Calculate midpoint for diamond/arrow
    coordinate.midX = coordinate.step1X + (coordinate.step2X - coordinate.step1X) / 2;
    coordinate.midY = coordinate.step1Y + (coordinate.step2Y - coordinate.step1Y) / 2;

    const logoRadius = 8;
    const joinMethod = join.JoinMethod || 'INNER JOIN';

    // Generate diamond/arrow shapes based on join method and direction
    if (joinMethod === 'INNER JOIN') {
      // Diamond shape
      coordinate.logoPath1 = `M ${coordinate.midX} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY} ` +
        `L ${coordinate.midX} ${coordinate.midY - logoRadius} ` +
        `L ${coordinate.midX - logoRadius} ${coordinate.midY} Z`;
      coordinate.logoPath2 = '';
    } else if ((joinMethod === 'LEFT OUTER JOIN' && coordinate.startX <= coordinate.endX) ||
      (joinMethod === 'RIGHT OUTER JOIN' && coordinate.startX > coordinate.endX)) {
      // Left arrow (pointing left)
      coordinate.logoPath1 = `M ${coordinate.midX - logoRadius} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY} ` +
        `L ${coordinate.midX} ${coordinate.midY - logoRadius} ` +
        `L ${coordinate.midX - logoRadius} ${coordinate.midY - logoRadius} Z`;
      coordinate.logoPath2 = `M ${coordinate.midX} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY} ` +
        `L ${coordinate.midX} ${coordinate.midY - logoRadius} ` +
        `L ${coordinate.midX - logoRadius} ${coordinate.midY} Z`;
    } else if ((joinMethod === 'RIGHT OUTER JOIN' && coordinate.startX <= coordinate.endX) ||
      (joinMethod === 'LEFT OUTER JOIN' && coordinate.startX > coordinate.endX)) {
      // Right arrow (pointing right)
      coordinate.logoPath1 = `M ${coordinate.midX} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY - logoRadius} ` +
        `L ${coordinate.midX} ${coordinate.midY - logoRadius} ` +
        `L ${coordinate.midX - logoRadius} ${coordinate.midY} Z`;
      coordinate.logoPath2 = `M ${coordinate.midX} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY} ` +
        `L ${coordinate.midX} ${coordinate.midY - logoRadius} ` +
        `L ${coordinate.midX - logoRadius} ${coordinate.midY} Z`;
    } else if (joinMethod === 'FULL OUTER JOIN') {
      // Full outer join - square with diamond
      coordinate.logoPath1 = `M ${coordinate.midX - logoRadius} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY - logoRadius} ` +
        `L ${coordinate.midX - logoRadius} ${coordinate.midY - logoRadius} Z`;
      coordinate.logoPath2 = `M ${coordinate.midX} ${coordinate.midY + logoRadius} ` +
        `L ${coordinate.midX + logoRadius} ${coordinate.midY} ` +
        `L ${coordinate.midX} ${coordinate.midY - logoRadius} ` +
        `L ${coordinate.midX - logoRadius} ${coordinate.midY} Z`;
    }

    return coordinate;
  }, [viewDto.DictTables, getTablePosition, getColumnPosition, TABLE_COLUMN_DELIMITER]);

  if (!isOpen) return null;

  return (
    <>
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10001]">
        <div
          className={`${theme.mainContentSection} ${isFullscreen ? 'rounded-none' : 'rounded-lg'} shadow-xl border flex flex-col overflow-hidden`}
          style={isFullscreen
            ? { width: '100vw', height: '100vh', position: 'fixed', top: 0, left: 0, zIndex: 10001 }
            : { width: '90vw', height: '90vh', maxWidth: '1600px', maxHeight: '900px' }
          }
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className={`flex items-center justify-between px-4 py-2 ${theme.mainContentSection} ${isFullscreen ? 'rounded-none' : 'rounded-t-lg'}`}>
            <h3 className={`text-lg font-semibold ${theme.title}`}>
              Database View Design: {viewNameValue || 'New View'}
            </h3>
            <div className="flex items-center gap-2">
              <button
                onClick={refresh}
                className="px-2 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center gap-1"
                title="Refresh"
              >
                <i className="fa-solid fa-rotate"></i>
              </button>
              <button
                onClick={executeQuery}
                className="px-2 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500 flex items-center gap-1"
                title="Query Execute"
              >
                <i className="fa-solid fa-bolt"></i>
              </button>
              {!isEmbeddedByOtherPage && (
                <button
                  onClick={save}
                  className="px-2 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600 flex items-center gap-1"
                  title="Save"
                >
                  <i className="fa-solid fa-save"></i>
                </button>
              )}
              {isEmbeddedByOtherPage && onQueryBuilt && (
                <button
                  onClick={() => {
                    onQueryBuilt(queryText);
                    onClose();
                  }}
                  className="px-2 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600 flex items-center gap-1"
                  title="Use This Query"
                >
                  <i className="fa-solid fa-check"></i>
                </button>
              )}
              <div className="ml-4 flex items-center gap-1">
                <button
                  onClick={() => setIsFullscreen(!isFullscreen)}
                  className="text-xs hover:text-gray-600 px-0.5 w-5 h-5 flex items-center justify-center"
                  title={isFullscreen ? "Exit Fullscreen" : "Fullscreen"}
                >
                  <i className={`fa-solid ${isFullscreen ? 'fa-compress' : 'fa-expand'}`}></i>
                </button>
                <button
                  onClick={onClose}
                  className="text-2xl hover:text-gray-600 px-2 w-9 h-9 flex items-center justify-center"
                  title="Close"
                >
                  &times;
                </button>
              </div>
            </div>
          </div>

          {/* Body */}
          <div className="h-1 px-2 pb-2 flex-auto overflow-hidden flex main-content-area">
            {/* Left Panel - Available Tables */}
            <div
              className={`flex flex-col border ${theme.mainContentSection}`}
              style={{ width: `${leftPanelWidth}px`, minWidth: '200px' }}
            >
              <div className={`px-2 py-1 border-b ${theme.mainContentSection}`}>
                <div className="flex items-center justify-between">
                  <h4 className={`text-xs font-semibold ${theme.title}`}>Dragging Table To Edit Panel</h4>
                  <button
                    onClick={() => loadAvailableTableData(true)}
                    className="w-6 h-5 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                    title="Reload Tables From Database"
                  >
                    <i className="fa-solid fa-database" style={{ fontSize: '11px' }}></i>
                    <i className="fa-solid fa-rotate" style={{ fontSize: '10px', position: 'relative', left: '1px', top: '3px' }}></i>
                  </button>
                </div>
              </div>
              <div className="flex-auto h-1 overflow-hidden p-2">
                <FlexGrid
                  ref={flexAvailableTablesRef}
                  itemsSource={availableTablesCV}
                  isReadOnly={true}
                  selectionMode="None"
                  style={{ height: '100%', borderLeft: 'none', borderRight: 'none', borderTop: 'none', borderBottom: 'none' }}
                >
                  <FlexGridFilter />
                  <FlexGridColumn width="*" header="Available Tables" binding="uiDisplayName">
                    <FlexGridCellTemplate
                      cellType="Cell"
                      template={(cell: any) => {
                        if (!cell.item) return <div></div>;
                        return (
                          <div
                            className="flex items-center justify-between p-1 cursor-move hover:bg-gray-100"
                            draggable
                            onDragStart={(e) => handleTableDragStart(e, cell.item)}
                            title={cell.item.uiDisplayName}
                          >
                            <span className="flex-auto w-1 truncate">{cell.item.Name}</span>
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                previewTableData(cell.item);
                              }}
                              className="p-1 text-green-600 hover:bg-green-100"
                              title="Preview Table Data"
                            >
                              <i className="fa-solid fa-eye"></i>
                            </button>
                          </div>
                        );
                      }}
                    />
                  </FlexGridColumn>
                </FlexGrid>
              </div>
            </div>

            {/* Horizontal Resizer */}
            <div
              onMouseDown={handleHorizontalResizeStart}
              className="w-1 cursor-col-resize hover:bg-blue-400 transition-colors flex-shrink-0"
              style={{ backgroundColor: isResizingHorizontal ? '#60a5fa' : 'transparent' }}
            >
              <div className="w-full h-full flex items-center justify-center">
                <div className="w-1 h-12 border-l border-r border-gray-300"></div>
              </div>
            </div>

            {/* Main Area - Diagram and Right Panel */}
            <div className="flex-auto w-1 flex overflow-hidden">
              {/* Diagram Container */}
              <div
                ref={diagramContainerRef}
                className={`relative flex-auto w-1 overflow-auto border ${theme.mainContentSection}`}
                onDrop={handleDropToDiagram}
                onDragOver={handleDragOverDiagram}
                onClick={(e) => {
                  // Deselect all tables when clicking on diagram
                  if (e.target === diagramContainerRef.current) {
                    setViewDto(prev => {
                      const updated = { ...prev };
                      Object.keys(updated.DictTables).forEach(key => {
                        updated.DictTables[key].isSelected = false;
                      });
                      return updated;
                    });
                  }
                }}
              >
                <div className="relative" style={{ width: '10000px', height: '10000px' }}>
                  {/* SVG for join lines */}
                  <svg className="absolute inset-0" style={{ width: '100%', height: '100%' }}>
                    {viewDto.Joins && viewDto.Joins
                      .filter((join: any) => {
                        // Filter out joins that reference non-existent tables
                        if (!join.JoinConditionList || join.JoinConditionList.length === 0) return false;
                        const existingTableNames = Object.keys(viewDto.DictTables || {}).map(name => name.toLowerCase());
                        return join.JoinConditionList.some((condition: any) => {
                          const leftTable = condition.LeftSideTable?.toLowerCase();
                          const rightTable = condition.RightSideTable?.toLowerCase();
                          return leftTable && rightTable && 
                                 existingTableNames.includes(leftTable) && 
                                 existingTableNames.includes(rightTable);
                        });
                      })
                      .map((join: any, joinIndex: number) => {
                        // Filter join conditions to only include those with existing tables
                        const existingTableNames = Object.keys(viewDto.DictTables || {}).map(name => name.toLowerCase());
                        const validJoinConditions = join.JoinConditionList?.filter((condition: any) => {
                          const leftTable = condition.LeftSideTable?.toLowerCase();
                          const rightTable = condition.RightSideTable?.toLowerCase();
                          return leftTable && rightTable && 
                                 existingTableNames.includes(leftTable) && 
                                 existingTableNames.includes(rightTable);
                        }) || [];
                        
                        if (validJoinConditions.length === 0) return null;
                        
                        return (
                          <g key={joinIndex} style={{ pointerEvents: 'none' }}>
                            {validJoinConditions.map((line: any, lineIndex: number) => {
                          const coords = getLineCoordinate(join, line);
                          return (
                            <g key={lineIndex} style={{ pointerEvents: 'all' }}>
                              <path
                                className={`connection-line ${line.isSelected ? 'connection-line-selected' : ''}`}
                                d={`M ${coords.startX} ${coords.startY} L ${coords.step1X} ${coords.step1Y} L ${coords.step2X} ${coords.step2Y} L ${coords.endX} ${coords.endY}`}
                                stroke={line.isSelected ? '#3b82f6' : '#d3d3d3'}
                                strokeWidth="2"
                                fill="none"
                                style={{ pointerEvents: 'none' }}
                              />
                              {/* Clickable area for the line itself (wider transparent path) */}
                              <path
                                d={`M ${coords.startX} ${coords.startY} L ${coords.step1X} ${coords.step1Y} L ${coords.step2X} ${coords.step2Y} L ${coords.endX} ${coords.endY}`}
                                stroke="transparent"
                                strokeWidth="12"
                                fill="none"
                                style={{ cursor: 'pointer' }}
                                onClick={(e) => handleLineSelected(e, line, join)}
                              />
                              {coords.logoPath1 && (
                                <path
                                  className={`connection-logo ${line.isSelected ? 'connection-logo-selected' : ''}`}
                                  d={coords.logoPath1}
                                  stroke={line.isSelected ? '#3b82f6' : '#d3d3d3'}
                                  strokeWidth="2"
                                  fill={line.isSelected ? '#93c5fd' : '#f0f0f0'}
                                  fillOpacity="1"
                                  style={{ cursor: 'pointer' }}
                                  onClick={(e) => handleLineSelected(e, line, join)}
                                />
                              )}
                              {coords.logoPath2 && (
                                <path
                                  className={`connection-logo ${line.isSelected ? 'connection-logo-selected' : ''}`}
                                  d={coords.logoPath2}
                                  stroke={line.isSelected ? '#3b82f6' : '#d3d3d3'}
                                  strokeWidth="2"
                                  fill={line.isSelected ? '#93c5fd' : '#f0f0f0'}
                                  fillOpacity="1"
                                  style={{ cursor: 'pointer' }}
                                  onClick={(e) => handleLineSelected(e, line, join)}
                                />
                              )}
                            </g>
                          );
                        })}
                          </g>
                        );
                      })
                      .filter((item: any) => item !== null)
                    }
                  </svg>

                  {/* Table boxes */}
                  {Object.entries(viewDto.DictTables || {}).map(([uniqTableOrAliasName, tableObj]: [string, any]) => (
                    <div
                      key={uniqTableOrAliasName}
                      id={uniqTableOrAliasName}
                      className="absolute bg-white border"
                      style={{
                        left: `${tableObj.PositionX || 0}px`,
                        top: `${tableObj.PositionY || 0}px`,
                        width: `${tableObj.Width || 200}px`,
                        height: `${tableObj.Height || 200}px`,
                        minWidth: '100px',
                        minHeight: '50px',
                        border: tableObj.isSelected ? '2px solid gray' : '1px solid gray',
                        boxSizing: 'border-box',
                        display: 'flex',
                        flexDirection: 'column',
                        overflow: 'hidden'
                      }}
                      onClick={(e) => {
                        e.stopPropagation();
                        setViewDto(prev => {
                          const updated = { ...prev };
                          Object.keys(updated.DictTables).forEach(key => {
                            updated.DictTables[key].isSelected = key === uniqTableOrAliasName;
                          });
                          return updated;
                        });
                      }}
                    >
                      {/* Table header */}
                      <div className={`flex items-center justify-between px-2 py-1 border-b ${theme.mainContentSection} select-none`} style={{ height: '45px', minHeight: '45px' }}>
                        <label
                          className="table-drag-handle text-xs font-semibold truncate flex-auto w-1 cursor-move select-none"
                          title={tableObj.UniqTableOrAliasName}
                          onMouseDown={(e) => {
                            if (e.button === 0) {
                              handleTableBoxDragStart(e, uniqTableOrAliasName);
                            }
                          }}
                        >
                          {tableObj.UniqTableOrAliasName}
                        </label>
                        <div className="flex items-center gap-1">
                          <div className="relative">
                            <button
                              onClick={(e) => handleTableContextMenuClick(e, uniqTableOrAliasName)}
                              className="text-xs px-1"
                              title="More Options"
                            >
                              ▼
                            </button>
                            {showTableContextMenu === uniqTableOrAliasName && (
                              <div
                                className={`absolute right-0 mt-1 w-56 rounded-[4px] shadow-lg ${theme.mainContentSection} border border-gray-300 z-50`}
                                style={{
                                  position: 'fixed',
                                  left: `${tableContextMenuPosition.x}px`,
                                  top: `${tableContextMenuPosition.y}px`,
                                  transform: 'translateX(-100%)'
                                }}
                                onClick={(e) => e.stopPropagation()}
                              >
                                <div className="py-1" role="menu">
                                  <button
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      previewTableData({ Name: tableObj.TableName, SchemaOwner: tableObj.SchemaOwner });
                                      setShowTableContextMenu(null);
                                    }}
                                    className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                                    role="menuitem"
                                  >
                                    <i className="fa-solid fa-eye mr-2"></i>
                                    <span>Preview Table Data</span>
                                  </button>
                                  <button
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      checkAllTableColumns(uniqTableOrAliasName, true);
                                      setShowTableContextMenu(null);
                                    }}
                                    className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                                    role="menuitem"
                                  >
                                    <i className="fa-solid fa-check-square mr-2"></i>
                                    <span>Select All Columns</span>
                                  </button>
                                  <button
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      uncheckAllTableColumns(uniqTableOrAliasName);
                                      setShowTableContextMenu(null);
                                    }}
                                    className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                                    role="menuitem"
                                  >
                                    <i className="fa-solid fa-square mr-2"></i>
                                    <span>Unselect All Columns</span>
                                  </button>
                                  <div className="border-t my-1"></div>
                                  {/* Add Reference (Parent) Tables */}
                                  {tableObj.FKRefTables && tableObj.FKRefTables.length > 0 && (
                                    <div className="relative">
                                      <button
                                        onClick={(e) => {
                                          e.stopPropagation();
                                          setShowFkRefSubmenu(showFkRefSubmenu === uniqTableOrAliasName ? null : uniqTableOrAliasName);
                                        }}
                                        className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                                        role="menuitem"
                                      >
                                        <i className="fa-solid fa-plus-square mr-2"></i>
                                        <span>Add Reference (Parent) Tables</span>
                                        <i className="fa-solid fa-chevron-right ml-auto"></i>
                                      </button>
                                      {showFkRefSubmenu === uniqTableOrAliasName && (
                                        <div
                                          className={`absolute left-full top-0 ml-1 min-w-64 rounded-[4px] shadow-lg ${theme.mainContentSection} border border-gray-300 z-50`}
                                         
                                          onClick={(e) => e.stopPropagation()}
                                        >
                                          <div className="py-1" role="menu">
                                            {tableObj.FKRefTables.map((fkObj: any, index: number) => {
                                              const isTableAdded = viewDto.DictTables[fkObj.ParentTableName] !== undefined;
                                              return (
                                                <button
                                                  key={index}
                                                  onClick={(e) => {
                                                    e.stopPropagation();
                                                    addFkRefTable(fkObj, false, uniqTableOrAliasName, addJoinConditionLine);
                                                    setShowFkRefSubmenu(null);
                                                    setShowTableContextMenu(null);
                                                  }}
                                                  className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700 text-left"
                                                  role="menuitem"
                                                >
                                                  <i className="fa-solid fa-database mr-2 flex-shrink-0"></i>
                                                  <span className="flex-auto w-1 truncate">{fkObj.ParentTableName}</span>
                                                  {isTableAdded && <i className="fa-solid fa-check ml-2 flex-shrink-0"></i>}
                                                </button>
                                              );
                                            })}
                                          </div>
                                        </div>
                                      )}
                                    </div>
                                  )}
                                  {/* Add Referenced (Child) Tables */}
                                  {tableObj.FKRefedTables && tableObj.FKRefedTables.length > 0 && (
                                    <div className="relative">
                                      <button
                                        onClick={(e) => {
                                          e.stopPropagation();
                                          setShowFkRefedSubmenu(showFkRefedSubmenu === uniqTableOrAliasName ? null : uniqTableOrAliasName);
                                        }}
                                        className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                                        role="menuitem"
                                      >
                                        <i className="fa-solid fa-plus-square mr-2"></i>
                                        <span>Add Referenced (Child) Tables</span>
                                        <i className="fa-solid fa-chevron-right ml-auto"></i>
                                      </button>
                                      {showFkRefedSubmenu === uniqTableOrAliasName && (
                                        <div
                                          className={`absolute left-full top-0 ml-1 min-w-64 rounded-[4px] shadow-lg ${theme.mainContentSection} border border-gray-300 z-50`}
                                          
                                          onClick={(e) => e.stopPropagation()}
                                        >
                                          <div className="py-1" role="menu">
                                            {tableObj.FKRefedTables.map((fkObj: any, index: number) => {
                                              const isTableAdded = viewDto.DictTables[fkObj.ChildTableName] !== undefined;
                                              return (
                                                <button
                                                  key={index}
                                                  onClick={(e) => {
                                                    e.stopPropagation();
                                                    addFkRefTable(fkObj, true, uniqTableOrAliasName, addJoinConditionLine);
                                                    setShowFkRefedSubmenu(null);
                                                    setShowTableContextMenu(null);
                                                  }}
                                                  className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700 text-left"
                                                  role="menuitem"
                                                >
                                                  <i className="fa-solid fa-database mr-2 flex-shrink-0"></i>
                                                  <span className="flex-auto w-1 truncate">{fkObj.ChildTableName}</span>
                                                  {isTableAdded && <i className="fa-solid fa-check ml-2 flex-shrink-0"></i>}
                                                </button>
                                              );
                                            })}
                                          </div>
                                        </div>
                                      )}
                                    </div>
                                  )}
                                  <div className="border-t my-1"></div>
                                  <button
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      openTableDesign(tableObj);
                                      setShowTableContextMenu(null);
                                    }}
                                    className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                                    role="menuitem"
                                  >
                                    <i className="fa-solid fa-pencil mr-2"></i>
                                    <span>Alter Table</span>
                                  </button>
                                </div>
                              </div>
                            )}
                          </div>
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              deleteOneTable(uniqTableOrAliasName);
                            }}
                            className="text-lg"
                            title="Delete Table"
                          >
                            &times;
                          </button>
                        </div>
                      </div>
                      {/* Table columns */}
                      <div className="dbViewTableContainer overflow-y-auto overflow-x-hidden" style={{ height: 'calc(100% - 45px)', paddingBottom: '2px', boxSizing: 'border-box' }}>
                        {viewDto.DictAllColumns[uniqTableOrAliasName.toLowerCase()] && Object.entries(viewDto.DictAllColumns[uniqTableOrAliasName.toLowerCase()]).map(([columnName, isSelected]: [string, any]) => {
                          const columnId = `${uniqTableOrAliasName}${TABLE_COLUMN_DELIMITER}${columnName}`;
                          return (
                            <div key={columnName} className="flex items-center p-1 hover:bg-gray-100" style={{ height: '18px' }}>
                              <input
                                type="checkbox"
                                checked={isSelected || false}
                                onChange={(e) => {
                                  setViewDto(prev => {
                                    const updated = { ...prev };
                                    const tableKey = uniqTableOrAliasName.toLowerCase();
                                    if (!updated.DictAllColumns[tableKey]) {
                                      updated.DictAllColumns[tableKey] = {};
                                    }
                                    updated.DictAllColumns[tableKey][columnName] = e.target.checked;
                                    // Update ref synchronously with the new state
                                    viewDtoRef.current = updated;
                                    return updated;
                                  });
                                  diagramChanged();
                                }}
                                className="mr-1"
                              />
                              <div
                                id={columnId}
                                className="text-xs truncate flex-auto w-1 cursor-pointer"
                                draggable
                                onDragStart={(e) => handleColumnDragStart(e, uniqTableOrAliasName, columnName)}
                                onDrag={(e) => handleColumnDrag(e)}
                                onDragEnd={handleColumnDragEnd}
                                onDrop={(e) => handleColumnDrop(e, uniqTableOrAliasName, columnName)}
                                onDragOver={handleColumnDragOver}
                                title="Drag to another column to create a join"
                              >
                                {columnName}
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Diagram Resizer */}
              <div
                onMouseDown={handleDiagramResizeStart}
                className="w-1 cursor-col-resize hover:bg-blue-400 transition-colors flex-shrink-0"
                style={{ backgroundColor: isResizingDiagram ? '#60a5fa' : 'transparent' }}
              >
                <div className="w-full h-full flex items-center justify-center">
                  <div className="w-1 h-12 border-l border-r border-gray-300"></div>
                </div>
              </div>

              {/* Right Panel */}
              <div className="flex flex-col overflow-hidden" style={{ width: `${rightPanelWidth}px` }}>
                {/* View Name */}
                <div className="px-2 py-2 flex items-center gap-2">
                  <label className="text-xs font-semibold w-20">View Name:</label>
                  <input
                    type="text"
                    value={viewNameValue}
                    onChange={(e) => setViewNameValue(e.target.value)}
                    disabled={!isNewView}
                    className={`flex-auto w-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                    placeholder="Enter view name"
                  />
                  {!viewNameValue && (
                    <span className="text-xs text-red-500">View name is required</span>
                  )}
                </div>
                <div className="w-full h-1 flex-auto flex flex-col right-panel-container">
                  {/* Selected Columns */}
                  <div className="flex flex-col" style={{ height: `${selectedColumnsHeight}%`, minHeight: '70px' }}>
                    <div className={`px-2 py-1 ${theme.mainContentSection}`}>
                      <h4 className={`text-xs font-semibold ${theme.title}`}>Selected Columns</h4>
                    </div>
                    <div className="flex-auto h-1 overflow-hidden">
                      {selectedColumnsListCV && (
                        <FlexGrid
                          ref={flexSelectedColumnsRef}
                          itemsSource={selectedColumnsListCV}
                          isReadOnly={false}
                          selectionMode="Row"
                          style={{ height: '100%', fontSize: '11px' }}
                          cellEditEnded={(sender: any, e: any) => {
                            // Handle alias or other column edits (matching AngularJS selectedColumnGridChanged)
                            // sender is the actual Wijmo control in cellEditEnded event
                            const col = sender.columns[e.col];
                            if (col && col.binding === 'ColumnAlias') {
                              // The CollectionView is already updated by Wijmo when cell is edited
                              // Get the updated SelectedColumnsList from the CollectionView's sourceCollection
                              const cv = sender.itemsSource;
                              const updatedSelectedColumnsList: any[] = [];
                              if (cv && cv.sourceCollection) {
                                // sourceCollection is the original array that the CollectionView wraps
                                for (let i = 0; i < cv.sourceCollection.length; i++) {
                                  updatedSelectedColumnsList.push({ ...cv.sourceCollection[i] });
                                }
                              } else if (cv && cv.items) {
                                // Fallback to items if sourceCollection is not available
                                for (let i = 0; i < cv.items.length; i++) {
                                  updatedSelectedColumnsList.push({ ...cv.items[i] });
                                }
                              }
                              
                              // Update viewDto with the updated SelectedColumnsList from CollectionView
                              const updatedViewDto = { ...viewDtoRef.current };
                              updatedViewDto.SelectedColumnsList = updatedSelectedColumnsList;
                              
                              // Update both state and ref synchronously (matching checkbox change pattern)
                              viewDtoRef.current = updatedViewDto;
                              setViewDto(updatedViewDto);
                              
                              // Trigger query update (matching AngularJS: updateDatabaseViewSelectedColumns(true))
                              setIsSelectedColumnChanged(true);
                              isSelectedColumnChangedRef.current = true;
                              
                              // Debounce the API call
                              if (debounceTimeoutRef.current) {
                                clearTimeout(debounceTimeoutRef.current);
                              }
                              debounceTimeoutRef.current = setTimeout(() => {
                                updateDatabaseViewSelectedColumns(true);
                                debounceTimeoutRef.current = null;
                              }, 300);
                            }
                          }}
                        >
                          <FlexGridFilter />
                          <FlexGridColumn header="Sort" binding="SortOrder" width={50} dataType="Number" />
                          <FlexGridColumn header="Name" binding="ColumnName" width={150} isReadOnly={true} />
                          <FlexGridColumn header="Alias" binding="ColumnAlias" width={150} />
                          <FlexGridColumn header="Table" binding="UniqTableOrAliasName" width={150} isReadOnly={true} />
                          <FlexGridColumn width="*" header="" binding="" isReadOnly={true} />
                        </FlexGrid>
                      )}
                    </div>
                  </div>

                  {/* Vertical Resizer */}
                  <div
                    onMouseDown={handleVerticalResizeStart}
                    className="h-1 cursor-row-resize hover:bg-blue-400 transition-colors flex-shrink-0"
                    style={{ backgroundColor: isResizingVertical ? '#60a5fa' : 'transparent' }}
                  >
                    <div className="h-full w-full flex items-center justify-center">
                      <div className="h-1 w-12 border-t border-b border-gray-300"></div>
                    </div>
                  </div>

                  {/* Query Text and Results */}
                  <div className="flex-auto h-1 flex flex-col overflow-hidden queryTextAndResultContainer">
                    {/* Query Text */}
                    <div className="flex flex-col min-h-[100px]" style={{ height: `${queryTextHeight}%` }}>
                      <div className={`px-2 py-1 ${theme.mainContentSection} flex items-center justify-between`}>
                        <h4 className={`text-xs font-semibold ${theme.title}`}>Query Text</h4>
                        <div className="flex items-center gap-1">
                          <button
                            onClick={executeQuery}
                            className="px-2 h-5 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500"
                            title="Execute"
                          >
                            <i className="fa-solid fa-bolt"></i>
                          </button>
                          <button
                            onClick={() => {
                              if (queryTextAreaRef.current) {
                                navigator.clipboard.writeText(queryText);
                                showInfo('Query text copied to clipboard');
                              }
                            }}
                            className="px-2 h-5 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                            title="Copy Query Text"
                          >
                            <i className="fa-solid fa-copy"></i>
                          </button>
                        </div>
                      </div>
                      <textarea
                        ref={queryTextAreaRef}
                        value={queryText}
                        onChange={(e) => {
                          setQueryText(e.target.value);
                          handleQueryChanged();
                        }}
                        className={`flex-auto h-1 w-full p-2 border rounded font-mono text-xs ${theme.inputBox} focus:outline-none`}
                        placeholder="Enter SQL query here..."
                        style={{ resize: 'none' }}
                      />
                    </div>

                    {/* Query Text/Result Resizer */}
                    <div
                      onMouseDown={handleQueryTextResizeStart}
                      className="h-1 cursor-row-resize hover:bg-blue-400 transition-colors flex-shrink-0"
                      style={{ backgroundColor: isResizingQueryText ? '#60a5fa' : 'transparent' }}
                    >
                      <div className="h-full w-full flex items-center justify-center">
                        <div className="h-1 w-12 border-t border-b border-gray-300"></div>
                      </div>
                    </div>

                    {/* Query Result */}
                    <div className="flex-auto h-1 flex flex-col min-h-[100px]">
                      <div className={`px-2 py-1 ${theme.mainContentSection} flex items-center justify-between`}>
                        <h4 className={`text-xs font-semibold ${theme.title}`}>
                          Query Result ({queryResult.length} Rows)
                        </h4>
                      </div>
                      <div className="flex-auto h-1 overflow-hidden border">
                        {queryResultCV && queryResult.length > 0 && (
                          <FlexGrid
                            ref={flexQueryResultRef}
                            itemsSource={queryResultCV}
                            isReadOnly={true}
                            style={{ height: '100%', borderLeft: 'none', borderRight: 'none', borderTop: 'none', borderBottom: 'none' }}
                            itemsSourceChanged={(s, e) => {
                              // Format date columns (matching AngularJS flexItemsSourceChanged)
                              const flex = flexQueryResultRef.current?.control || s;
                              setTimeout(() => {
                                if (flex && flex.columns) {
                                  for (let i = 0; i < flex.columns.length; i++) {
                                    const col = flex.columns[i];
                                    if (col.dataType === 4) { // Date Type
                                      col.format = "G";
                                    }
                                  }
                                }
                              }, 0);
                            }}
                          >
                            <FlexGridFilter />
                          </FlexGrid>
                        )}
                        {queryResult.length === 0 && (
                          <div className="flex items-center justify-center h-full text-gray-500 text-xs">
                            No query results. Click "Execute Query" to run the query.
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </div>

              </div>
            </div>
          </div>
        </div>
      </div >

      {/* Drag Helper Tooltip */}
      {
        dragHelperVisible && draggingColumn && (
          <div
            className="fixed z-[10000] pointer-events-none"
            style={{
              left: `${dragHelperPosition.x + 10}px`,
              top: `${dragHelperPosition.y + 10}px`,
              opacity: 0.8
            }}
          >
            <div className="font-semibold bg-white border border-gray-300 p-2 shadow-lg whitespace-nowrap">
              Join: Drag and drop onto a table column
            </div>
          </div>
        )
      }

      {/* Join Context Menu */}
      {
        showJoinContextMenu && currentConditionLine && currentJoin && (
          <div
            id="join-context-menu"
            className={`fixed z-[10000] w-64 rounded-[4px] shadow-lg ${theme.mainContentSection} border border-gray-300`}
            style={{
              left: `${joinContextMenuPosition.x}px`,
              top: `${joinContextMenuPosition.y}px`
            }}
            onClick={(e) => e.stopPropagation()}
            onMouseDown={(e) => e.stopPropagation()}
          >
            <div className="py-1" role="menu">
              <button
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  console.log('Remove button clicked');
                  removeJoinConditionLines();
                }}
                className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                role="menuitem"
                type="button"
              >
                <i className="fa-solid fa-trash mr-2"></i>
                <span>Remove</span>
              </button>
              <div className="border-t my-1"></div>
              <button
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  console.log('Select All Rows From Left Table clicked');
                  updateJoinMethod(true);
                }}
                className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700 whitespace-nowrap"
                role="menuitem"
                type="button"
              >
                <span>Select All Rows From {currentConditionLine.LeftSideTable}</span>
              </button>
              <button
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  console.log('Select All Rows From Right Table clicked');
                  updateJoinMethod(false);
                }}
                className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700 whitespace-nowrap"
                role="menuitem"
                type="button"
              >
                <span>Select All Rows From {currentConditionLine.RightSideTable}</span>
              </button>
              <div className="border-t my-1"></div>
              <div className="px-4 py-2 text-xs text-gray-600">
                <div className="font-semibold">Condition:</div>
                <div className="text-[10px] mt-1 italic whitespace-normal">
                  {currentConditionLine.LeftSideTable && currentConditionLine.LeftSideColumn &&
                    currentConditionLine.RightSideTable && currentConditionLine.RightSideColumn
                    ? `[${currentConditionLine.LeftSideTable}].[${currentConditionLine.LeftSideColumn}] = [${currentConditionLine.RightSideTable}].[${currentConditionLine.RightSideColumn}]`
                    : currentJoin.JoinConditionDisplay || 'N/A'}
                </div>
              </div>
            </div>
          </div>
        )
      }

      {/* Preview Popup */}
      {
        previewTableInfo && (
          <TableDataPreview
            isOpen={showPreviewPopup}
            onClose={() => {
              setShowPreviewPopup(false);
              setPreviewTableInfo(null);
            }}
            tableName={previewTableInfo.tableName}
            dataSourceRegisterId={previewTableInfo.dataSourceRegisterId}
            schemaOwner={previewTableInfo.schemaOwner}
            recordLimit={100}
          />
        )
      }

      {/* Table Design Popup */}
      {
        tableDesignInfo && (
          <MetaDataTableDesign
            isOpen={showTableDesignPopup}
            onClose={() => {
              setShowTableDesignPopup(false);
              setTableDesignInfo(null);
            }}
            onSave={() => {
              // Refresh after table design
              loadViewData();
            }}
            tableName={tableDesignInfo.tableName}
            dataSourceRegisterId={tableDesignInfo.dataSourceRegisterId}
            schemaOwner={tableDesignInfo.schemaOwner}
            applicationId={tableDesignInfo.applicationId}
          />
        )
      }
    </>
  );
};

export default MetaDataViewDesign;
