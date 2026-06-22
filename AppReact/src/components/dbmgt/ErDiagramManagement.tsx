import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { clampContextMenuPosition, useRefineContextMenuPosition } from '../../hooks/useClampedContextMenuPosition';

const CONTEXT_MENU_ESTIMATED_WIDTH = 200;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 150;

interface ErDiagramItem {
  Id: number;
  Name: string;
  Description?: string;
  DataSourceFrom?: number;
  AppModifiedDate?: string;
  AppCreatedDate?: string;
}

interface DataSourceRegisterItem {
  Id: number;
  DataSourceName: string;
}

const ErDiagramManagement: React.FC = () => {
  const { theme, t } = useTheme();
  const dispatch = useDispatch();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();

  const [list, setList] = useState<ErDiagramItem[]>([]);
  const [listCV, setListCV] = useState<CollectionView | null>(null);
  const [dataSourceRegisterList, setDataSourceRegisterList] = useState<DataSourceRegisterItem[]>([]);
  const [selectedItem, setSelectedItem] = useState<ErDiagramItem | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [contextMenuVisible, setContextMenuVisible] = useState(false);
  const [contextMenuPosition, setContextMenuPosition] = useState({ x: 0, y: 0 });

  const flexGridRef = useRef<wjGrid.FlexGrid | null>(null);
  const dataSourceDataMapRef = useRef<DataMap | null>(null);
  const contextMenuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (dataSourceRegisterList.length > 0) {
      dataSourceDataMapRef.current = new DataMap(dataSourceRegisterList, 'Id', 'DataSourceName');
    }
  }, [dataSourceRegisterList]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const [diagrams, dsList] = await Promise.all([
        appTransactionService.RetrieveAllErDiagramDto(),
        adminSvc.getDataSourceRegisterList(false)
      ]);
      const safeList = Array.isArray(diagrams) ? diagrams : [];
      setList(safeList);
      const cv = new CollectionView(safeList);
      cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
      cv.sortDescriptions.push(new SortDescription('Id', false));
      setListCV(cv);
      setDataSourceRegisterList(Array.isArray(dsList) ? dsList : []);
    } catch (err) {
      showError('Failed to load ER diagrams: ' + (err instanceof Error ? err.message : String(err)));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  useEffect(() => {
    loadData();
  }, []);

  const handleRefresh = () => {
    loadData();
  };

  const handleSelectionChanged = (grid: wjGrid.FlexGrid) => {
    const rowIndex = grid.selection?.row ?? -1;
    if (rowIndex >= 0 && grid.rows[rowIndex]) {
      setSelectedItem(grid.rows[rowIndex].dataItem as ErDiagramItem);
    } else {
      setSelectedItem(null);
    }
  };

  const openEditor = (item: ErDiagramItem | null) => {
    if (item) {
      const label = item.Name || `ER Diagram ${item.Id}`;
      addTabAndNavigate('er-diagram-editor', label, { id: item.Id }, true);
    } else {
      addTabAndNavigate('er-diagram-editor', 'New ER Diagram', {}, true);
    }
    setContextMenuVisible(false);
  };

  const handleEdit = (item?: ErDiagramItem) => {
    const target = item ?? selectedItem;
    if (!target) {
      showWarning('Please select an ER diagram to edit');
      return;
    }
    openEditor(target);
  };

  const handleRowDoubleClick = (grid: wjGrid.FlexGrid) => {
    const rowIndex = grid.selection?.row ?? -1;
    if (rowIndex >= 0 && grid.rows[rowIndex]) {
      const item = grid.rows[rowIndex].dataItem as ErDiagramItem;
      if (item) openEditor(item);
    }
  };

  const handleDelete = async (item?: ErDiagramItem) => {
    const target = item ?? selectedItem;
    if (!target?.Id) {
      showWarning('Please select an ER diagram to delete');
      return;
    }
    const confirmed = window.confirm(`Delete ER diagram: ${target.Name} (Id: ${target.Id})?`);
    if (!confirmed) return;
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.DeleteOneErDiagram(target.Id);
      if (result?.ValidationResult?.IsValid !== false) {
        showInfo('ER diagram deleted');
        await loadData();
      } else {
        const msg = result?.ValidationResult?.Items?.[0]?.Message ?? result?.ValidationResult?.ErrorMessage ?? 'Delete failed';
        showError(msg);
      }
    } catch (err) {
      showError('Failed to delete: ' + (err instanceof Error ? err.message : String(err)));
    } finally {
      dispatch(setIsNotBusy());
      setContextMenuVisible(false);
    }
  };

  const handleContextMenu = (e: React.MouseEvent, dataItem: ErDiagramItem) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedItem(dataItem);
    const { x, y } = clampContextMenuPosition(
      e.clientX,
      e.clientY,
      CONTEXT_MENU_ESTIMATED_WIDTH,
      CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setContextMenuPosition({ x, y });
    setContextMenuVisible(true);
  };

  useEffect(() => {
    const handleClickOutside = () => setContextMenuVisible(false);
    if (contextMenuVisible) {
      document.addEventListener('click', handleClickOutside);
      return () => document.removeEventListener('click', handleClickOutside);
    }
  }, [contextMenuVisible]);

  useRefineContextMenuPosition(contextMenuVisible, contextMenuRef, setContextMenuPosition);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>ER Diagrams</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isLoading}
            className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1.5 disabled:opacity-60 ${theme.button_default}`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            <span>Refresh</span>
          </button>
          <button
            type="button"
            onClick={() => openEditor(null)}
            disabled={isLoading}
            className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}
            title="Create new ER diagram"
          >
            <i className="fa-solid fa-plus" aria-hidden />
            <span>Create</span>
          </button>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          itemsSource={listCV ?? undefined}
          autoGenerateColumns={false}
          selectionMode="Row"
          headersVisibility="Column"
          isReadOnly={true}
          initialized={(grid: wjGrid.FlexGrid) => {
            flexGridRef.current = grid;
            grid.hostElement.addEventListener('dblclick', () => handleRowDoubleClick(grid));
          }}
          selectionChanged={(grid: wjGrid.FlexGrid) => handleSelectionChanged(grid)}
          className="w-full h-full"
        >
          <FlexGridColumn isReadOnly width={60} header="Actions">
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: any) => (
                <div className="flex items-center justify-center w-full">
                  <button
                    type="button"
                    className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                    title="More options"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleContextMenu(e, ctx.item as ErDiagramItem);
                    }}
                  >
                    <i className="fa-solid fa-pencil text-xs" aria-hidden />
                    <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                  </button>
                </div>
              )}
            />
          </FlexGridColumn>
          <FlexGridColumn header="Id" binding="Id" width={70} />
          <FlexGridColumn header="Name" binding="Name" width={200} />
          <FlexGridColumn header="Description" binding="Description" width={250} />
          <FlexGridColumn
            header="Data Source"
            binding="DataSourceFrom"
            width={150}
            dataMap={dataSourceDataMapRef.current ?? undefined}
          />
          <FlexGridColumn header="Modified" binding="AppModifiedDate" width={120} />
          <FlexGridColumn header="Created" binding="AppCreatedDate" width={120} />
          <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
        </FlexGrid>
      </div>

      {contextMenuVisible && selectedItem && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 py-1 min-w-[200px] rounded-[4px] shadow-lg border ${t('border_default')} ${theme.mainContentSection}`}
          style={{ left: contextMenuPosition.x, top: contextMenuPosition.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`}
            onClick={() => handleEdit()}
          >
            <i className="fa-solid fa-pen-to-square" aria-hidden />
            Edit
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`}
            onClick={() => handleDelete()}
          >
            <i className="fa-solid fa-trash" aria-hidden />
            Delete
          </button>
        </div>
      )}
    </div>
  );
};

export default ErDiagramManagement;
