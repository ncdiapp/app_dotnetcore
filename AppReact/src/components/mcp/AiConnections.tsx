import React, { useCallback, useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { oauthSvc, OAuthConnectionRow } from '../../webapi/oauthSvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';

type Props = {
  /** 'company': a company admin ends any connection of the company's users. 'mine': a user ends their own. */
  mode: 'company' | 'mine';
};

const formatDate = (value?: string | null) => (value ? new Date(value).toLocaleString() : '');

// The AI applications (Claude, ChatGPT, ...) connected to AppAI. Ending a connection stops it at once: its tokens are
// revoked, so the application must ask the user to sign in again.
const AiConnections: React.FC<Props> = ({ mode }) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();

  const [cv] = useState(() => new CollectionView<OAuthConnectionRow>([]));
  const [selected, setSelected] = useState<OAuthConnectionRow | null>(null);

  const showError = useCallback((e: unknown) => errorMessage.showError(e instanceof Error ? e.message : String(e)), [errorMessage]);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const rows = mode === 'company' ? await oauthSvc.companyConnections() : await oauthSvc.myConnections();
      cv.sourceCollection = Array.isArray(rows) ? rows : [];
      cv.refresh();
      setSelected(null);
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [mode, dispatch, cv, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const onSelectionChanged = useCallback((s: any) => {
    const flex = s?.control ?? s;
    const row = flex.selection?.row;
    setSelected(row != null && row >= 0 ? ((flex.rows[row]?.dataItem as OAuthConnectionRow | undefined) ?? null) : null);
  }, []);

  const disconnect = useCallback(async () => {
    if (!selected) return;
    const who = mode === 'company' && selected.UserName ? ` for ${selected.UserName}` : '';
    if (!window.confirm(`Disconnect ${selected.ClientName}${who}? It will have to ask for sign-in again.`)) return;
    dispatch(setIsBusy());
    try {
      if (mode === 'company') await oauthSvc.revokeCompanyConnection(selected.AuthorizationId);
      else await oauthSvc.revokeMyConnection(selected.AuthorizationId);
      await loadData();
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [selected, mode, dispatch, loadData, showError]);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>{mode === 'company' ? 'AI Connections' : 'My AI Connections'}</div>
        <div className="flex items-center gap-2">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={loadData} title="Refresh">
            <i className="fa-solid fa-rotate" aria-hidden="true" />
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            disabled={!selected}
            onClick={disconnect}
            title="Disconnect the selected application"
          >
            <i className="fa-solid fa-link-slash" aria-hidden="true" /> Disconnect
          </button>
        </div>
      </div>
      <div className={`px-3 pb-2 text-xs ${theme.label}`}>
        {mode === 'company'
          ? 'AI applications your users connected. A connection acts with that user’s own permissions.'
          : 'AI applications you connected. Disconnect any you no longer use.'}
      </div>

      <div className="w-full h-1 flex-auto overflow-hidden">
        <FlexGrid
          className="w-full h-full"
          itemsSource={cv}
          selectionMode="Row"
          headersVisibility="Column"
          isReadOnly={true}
          selectionChanged={onSelectionChanged}
        >
          <FlexGridColumn binding="ClientName" header="Application" width={220} />
          {mode === 'company' && <FlexGridColumn binding="UserName" header="User" width={200} />}
          <FlexGridColumn header="Connected" width={200}>
            <FlexGridCellTemplate cellType="Cell" template={(cell: any) => (cell.item ? formatDate(cell.item.CreatedUtc) : '')} />
          </FlexGridColumn>
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>
    </div>
  );
};

export default AiConnections;
