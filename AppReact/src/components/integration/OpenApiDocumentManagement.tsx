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

  const columns = useMemo(() => userDataMap, [userDataMap]);
  const actionButtonClass = `w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`;

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>OpenAPI Documents Management</div>
        <div className="flex items-center space-x-2">
          <button
            type="button"
            onClick={loadData}
            className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
          </button>
          <button
            type="button"
            onClick={() => openEditor(null)}
            className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white bg-green-500 hover:bg-green-600"
            title="Create"
          >
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
          <FlexGridColumn header="Actions" width={80} isReadOnly>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: any) => {
                const item = ctx.item as OpenApiDocument | undefined;
                if (!item) return null;
                return (
                  <div className="flex justify-center items-center gap-1 w-full">
                    <button type="button" className={actionButtonClass} title="Edit" onClick={(e) => { e.stopPropagation(); openEditor(item); }}>
                      <i className="fa-solid fa-pen-to-square" aria-hidden />
                    </button>
                    <button type="button" className={actionButtonClass} title="Delete" onClick={(e) => { e.stopPropagation(); remove(item); }}>
                      <i className="fa-solid fa-trash" aria-hidden />
                    </button>
                  </div>
                );
              }}
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
    </div>
  );
};

export default OpenApiDocumentManagement;
