import React, { useCallback, useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { oauthSvc, OAuthClientRow } from '../../webapi/oauthSvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';

// System administrators only: the OAuth clients (AI applications) that may ask users to sign in. Claude and ChatGPT
// register themselves automatically; register one here for an application that cannot, then type its client id into
// that application's connector settings ("use your own OAuth client").
const OAuthClients: React.FC = () => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();

  const [cv] = useState(() => new CollectionView<OAuthClientRow>([]));
  const [selected, setSelected] = useState<OAuthClientRow | null>(null);
  const [name, setName] = useState('');
  const [redirects, setRedirects] = useState('');
  const [createdId, setCreatedId] = useState<string | null>(null);

  const showError = useCallback((e: unknown) => errorMessage.showError(e instanceof Error ? e.message : String(e)), [errorMessage]);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const rows = await oauthSvc.clients();
      cv.sourceCollection = Array.isArray(rows) ? rows : [];
      cv.refresh();
      setSelected(null);
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, cv, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const onSelectionChanged = useCallback((s: any) => {
    const flex = s?.control ?? s;
    const row = flex.selection?.row;
    setSelected(row != null && row >= 0 ? ((flex.rows[row]?.dataItem as OAuthClientRow | undefined) ?? null) : null);
  }, []);

  const create = useCallback(async () => {
    const uris = redirects.split('\n').map((u) => u.trim()).filter(Boolean);
    dispatch(setIsBusy());
    try {
      setCreatedId(await oauthSvc.createClient(name.trim(), uris));
      setName('');
      setRedirects('');
      await loadData();
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [name, redirects, dispatch, loadData, showError]);

  const remove = useCallback(async () => {
    if (!selected) return;
    if (!window.confirm(`Delete ${selected.Name}? Every connection made through it is ended.`)) return;
    dispatch(setIsBusy());
    try {
      await oauthSvc.deleteClient(selected.ClientId);
      setCreatedId(null);
      await loadData();
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [selected, dispatch, loadData, showError]);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>OAuth Clients</div>
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          disabled={!selected}
          onClick={remove}
          title="Delete the selected client"
        >
          <i className="fa-solid fa-trash" aria-hidden="true" /> Delete
        </button>
      </div>

      <div className="w-full h-[200px] flex-auto flex overflow-hidden">
        <div className="w-1 flex-auto h-full overflow-hidden">
          <FlexGrid
            className="w-full h-full"
            itemsSource={cv}
            selectionMode="Row"
            headersVisibility="Column"
            isReadOnly={true}
            selectionChanged={onSelectionChanged}
          >
            <FlexGridColumn binding="Name" header="Application" width={220} />
            <FlexGridColumn binding="ClientId" header="Client id" width={300} />
            <FlexGridColumn header="Registered" width={110}>
              <FlexGridCellTemplate cellType="Cell" template={(cell: any) => (cell.item ? (cell.item.IsDynamic ? 'Automatic' : 'By admin') : '')} />
            </FlexGridColumn>
            <FlexGridColumn header="Redirect addresses" width={320}>
              <FlexGridCellTemplate cellType="Cell" template={(cell: any) => (cell.item ? (cell.item.RedirectUris ?? []).join(', ') : '')} />
            </FlexGridColumn>
            <FlexGridColumn header="" binding="" width="*" />
          </FlexGrid>
        </div>

        <div className={`w-[340px] h-full shrink-0 overflow-auto border-l px-4 py-3 ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Register a client</div>
          <div className="flex flex-col gap-2 pt-2">
            <label className={`text-xs ${theme.label}`}>Application name</label>
            <input
              type="text"
              maxLength={100}
              className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
            <label className={`text-xs ${theme.label}`}>Redirect addresses (one per line, https or http://localhost)</label>
            <textarea
              rows={4}
              className={`w-full px-2 py-1 text-xs border ${theme.inputBox}`}
              value={redirects}
              onChange={(e) => setRedirects(e.target.value)}
            />
            <div>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                disabled={!name.trim() || !redirects.trim()}
                onClick={create}
              >
                <i className="fa-solid fa-plus" aria-hidden="true" /> Register
              </button>
            </div>
            {createdId && (
              <div className={`text-xs break-all ${theme.label}`}>
                Client id: <b>{createdId}</b>
                <br />
                Enter it in the application&apos;s connector settings. It is a public client: no secret is needed.
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default OAuthClients;
