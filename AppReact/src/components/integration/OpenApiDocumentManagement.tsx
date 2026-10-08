import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { addTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { adminSvc } from '../../webapi/adminsvc';
import { useApiServerRoot } from '../../redux/hooks/useApiServerRoot';
import { openApiDocumentService, OpenApiDocument } from '../../webapi/openApiDocumentSvc';
import { clampContextMenuPosition, useRefineContextMenuPosition } from '../../hooks/useClampedContextMenuPosition';

const CONTEXT_MENU_ESTIMATED_WIDTH = 180;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 220;

function publicUrl(root: string, code?: string, status?: string): string {
  if (status !== 'Published' || !code || !root) return '';
  return `${root.replace(/\/$/, '')}/openapi-doc/${code}`;
}

const OpenApiDocumentManagement: React.FC = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const serverRoot = useApiServerRoot();

  const [collectionView, setCollectionView] = useState<CollectionView<any>>(() => new CollectionView<any>([]));
  const [userDataMap, setUserDataMap] = useState<DataMap | null>(null);
  const [contextMenuOpen, setContextMenuOpen] = useState(false);
  const [contextMenuPos, setContextMenuPos] = useState({ x: 0, y: 0 });
  const [selectedRow, setSelectedRow] = useState<OpenApiDocument | null>(null);
  const contextMenuRef = useRef<HTMLDivElement | null>(null);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const [rows, usersRaw] = await Promise.all([
        openApiDocumentService.list(),
        adminSvc.getCurrentTenantUserDtoList(false).catch(() => []),
      ]);
      const users = Array.isArray(usersRaw) ? usersRaw : [];
      const mapSource = users
        .filter((u: any) => u?.Id != null)
        .map((u: any) => ({ Id: u.Id, Name: u.UserName || u.LoginName || String(u.Id) }));
      setUserDataMap(mapSource.length ? new DataMap(mapSource, 'Id', 'Name') : null);
      const list = (rows || []).map((row) => ({
        ...row,
        PublicUrl: publicUrl(serverRoot, row.Code, row.Status),
        LastGenerated: row.LastGenerated ? new Date(row.LastGenerated) : null,
        AppCreatedDate: row.AppCreatedDate ? new Date(row.AppCreatedDate) : null,
        AppModifiedDate: row.AppModifiedDate ? new Date(row.AppModifiedDate) : null,
      }));
      const cv = new CollectionView<any>(list);
      cv.sortDescriptions.push(new SortDescription('Name', true));
      setCollectionView(cv);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, errorMessage, serverRoot]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const openEditor = useCallback((row?: OpenApiDocument | null) => {
    const path = row?.Id ? `/openapi-doc-editor/${row.Id}` : '/openapi-doc-editor';
    const label = row?.Name ? `OpenAPI: ${row.Name}` : 'New OpenAPI Document';
    dispatch(addTab({ tabPath: path, label, isClosable: true }));
    navigate(path);
  }, [dispatch, navigate]);

  const closeContextMenu = useCallback(() => {
    setContextMenuOpen(false);
    setSelectedRow(null);
  }, []);

  const openContextMenu = useCallback((e: React.MouseEvent, row: OpenApiDocument) => {
    e.stopPropagation();
    setSelectedRow(row);
    const pos = clampContextMenuPosition(e.clientX - 20, e.clientY - 5, CONTEXT_MENU_ESTIMATED_WIDTH, CONTEXT_MENU_ESTIMATED_HEIGHT);
    setContextMenuPos(pos);
    setContextMenuOpen(true);
  }, []);

  useEffect(() => {
    if (!contextMenuOpen) return;
    const handler = () => closeContextMenu();
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, [contextMenuOpen, closeContextMenu]);

  useRefineContextMenuPosition(contextMenuOpen, contextMenuRef, setContextMenuPos);

  const copyUrl = useCallback(async (row: OpenApiDocument | null) => {
    const url = publicUrl(serverRoot, row?.Code, row?.Status);
    if (!url) {
      errorMessage.showError('Publish the document before copying its URL.');
      return;
    }
    await navigator.clipboard.writeText(url);
    errorMessage.showInfo('Public URL copied.');
  }, [errorMessage, serverRoot]);

  const download = useCallback(async (row: OpenApiDocument | null) => {
    if (!row?.Id) return;
    const blob = await openApiDocumentService.download(row.Id);
    const href = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = href;
    link.download = `${row.Code || 'openapi'}.json`;
    link.click();
    URL.revokeObjectURL(href);
  }, []);

  const regenerate = useCallback(async (row: OpenApiDocument | null) => {
    if (!row?.Id) return;
    dispatch(setIsBusy());
    try {
      const result = await openApiDocumentService.regenerate(row.Id);
      const skipped = result.Skipped || [];
      if (skipped.length) {
        errorMessage.showInfo(skipped.map((s) => `${s.ActionCode || '(blank)'}: ${s.Reason}`).join('\n'));
      } else {
        errorMessage.showInfo('Regenerated.');
      }
      await loadData();
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, errorMessage, loadData]);

  const publish = useCallback(async (row: OpenApiDocument | null) => {
    if (!row?.Id) return;
    const next = row.Status !== 'Published';
    dispatch(setIsBusy());
    try {
      await openApiDocumentService.setPublished(row.Id, next);
      await loadData();
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, errorMessage, loadData]);

  const remove = useCallback(async (row: OpenApiDocument | null) => {
    if (!row?.Id) return;
    const confirmed = await showConfirm(`Please confirm to delete:\n${row.Name || row.Code}`, { title: 'Delete OpenAPI Document' });
    if (!confirmed) return;
    dispatch(setIsBusy());
    try {
      await openApiDocumentService.remove(row.Id);
      await loadData();
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, errorMessage, loadData, showConfirm]);

  const menuRow = selectedRow;
  const publishLabel = menuRow?.Status === 'Published' ? 'Unpublish' : 'Publish';

  const columns = useMemo(() => userDataMap, [userDataMap]);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>OpenAPI Documents Management</div>
        <div className="flex items-center space-x-2">
          <button type="button" onClick={loadData} className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white bg-blue-400 hover:bg-blue-500" title="Refresh">
            <i className="fa-solid fa-rotate" aria-hidden />
          </button>
          <button type="button" onClick={() => openEditor(null)} className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white bg-green-500 hover:bg-green-600" title="Create">
            <i className="fa-solid fa-plus" aria-hidden />
          </button>
        </div>
      </div>
      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          itemsSource={collectionView}
          autoGenerateColumns={false}
          selectionMode="Row"
          isReadOnly
          headersVisibility="Column"
          initialized={(flex: wjGrid.FlexGrid) => {
            flexRef.current = flex;
            flex.hostElement.addEventListener('dblclick', () => {
              const row = flex.selection?.row;
              if (row == null || row < 0) return;
              const item = flex.rows[row]?.dataItem as OpenApiDocument | undefined;
              if (item) openEditor(item);
            });
          }}
          className="w-full h-full !border-0"
        >
          <FlexGridColumn header="Actions" width={90} isReadOnly>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: any) => (
                <div className="flex justify-center w-full">
                  <button type="button" className={theme.menu_default} title="More Options" onClick={(e) => openContextMenu(e, ctx.item)}>
                    <i className="fa-solid fa-ellipsis" aria-hidden />
                  </button>
                </div>
              )}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="Id" header="Id" width={70} />
          <FlexGridColumn binding="Name" header="Name" width={220} />
          <FlexGridColumn binding="Code" header="Code" width={160} />
          <FlexGridColumn binding="Version" header="Version" width={90} />
          <FlexGridColumn binding="ApiCount" header="API Count" width={90} />
          <FlexGridColumn binding="Status" header="Status" width={100} />
          <FlexGridColumn binding="PublicUrl" header="Public URL" width={280} />
          <FlexGridColumn binding="LastGenerated" header="Last Generated" width={160} dataType="Date" format="yyyy-MM-dd HH:mm" />
          <FlexGridColumn binding="Description" header="Description" width={180} />
          <FlexGridColumn binding="AppCreatedByID" header="Created By" width={140} dataMap={columns ?? undefined} />
          <FlexGridColumn binding="AppCreatedDate" header="Created Date" width={150} dataType="Date" format="yyyy-MM-dd HH:mm" />
          <FlexGridColumn binding="AppModifiedByID" header="Modified By" width={140} dataMap={columns ?? undefined} />
          <FlexGridColumn binding="AppModifiedDate" header="Modified Date" width={150} dataType="Date" format="yyyy-MM-dd HH:mm" />
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>
      {contextMenuOpen && menuRow && (
        <div ref={contextMenuRef} className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[160px]`} style={{ left: contextMenuPos.x, top: contextMenuPos.y }} onClick={(e) => e.stopPropagation()}>
          <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu}`} onClick={() => { openEditor(menuRow); closeContextMenu(); }}>Open</button>
          <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu}`} onClick={() => { closeContextMenu(); regenerate(menuRow); }}>Regenerate</button>
          <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu}`} onClick={() => { closeContextMenu(); copyUrl(menuRow); }}>Copy URL</button>
          <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu}`} onClick={() => { closeContextMenu(); download(menuRow); }}>Download</button>
          <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu}`} onClick={() => { closeContextMenu(); publish(menuRow); }}>{publishLabel}</button>
          <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu}`} onClick={() => { closeContextMenu(); remove(menuRow); }}>Delete</button>
        </div>
      )}
    </div>
  );
};

export default OpenApiDocumentManagement;
