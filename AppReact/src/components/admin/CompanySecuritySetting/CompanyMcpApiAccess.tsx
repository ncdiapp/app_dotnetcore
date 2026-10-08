import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { mcpManagementSvc, McpExposableApi, McpSecurityGroup } from '../../../webapi/mcpManagementSvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';

const apiKey = (a: McpExposableApi) => `${a.AppSource}\n${a.OperationId}`;

// A GET that looks like a change is common in this API; make the admin notice before exposing it.
const LOOKS_LIKE_A_CHANGE = /(delete|remove|save|update|create|add|set|reset|clear|import)/i;

// Admin screen: which APIs external MCP users (Claude Desktop, ChatGPT Desktop, ...) may use, per security group.
// An API is reachable only when it is enabled here AND the user belongs to a group ticked for it.
const CompanyMcpApiAccess: React.FC = () => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();

  const [cv] = useState(() => new CollectionView<McpExposableApi>([]));
  const [groups, setGroups] = useState<McpSecurityGroup[]>([]);
  const [search, setSearch] = useState('');
  const [onlyExposed, setOnlyExposed] = useState(false);
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [apis, setApis] = useState<McpExposableApi[]>([]);
  const [editEnabled, setEditEnabled] = useState(false);
  const [editGroupIds, setEditGroupIds] = useState<number[]>([]);

  const selected = useMemo(() => apis.find((a) => apiKey(a) === selectedKey) ?? null, [apis, selectedKey]);

  const showError = useCallback((e: unknown) => errorMessage.showError(e instanceof Error ? e.message : String(e)), [errorMessage]);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const [list, groupList] = await Promise.all([mcpManagementSvc.getExposableApis(), mcpManagementSvc.getSecurityGroups()]);
      setApis(Array.isArray(list) ? list : []);
      setGroups(Array.isArray(groupList) ? groupList : []);
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Keep the grid's collection in step with the data and the filters.
  useEffect(() => {
    const text = search.trim().toLowerCase();
    cv.sourceCollection = apis;
    cv.filter = (a: McpExposableApi) =>
      (!onlyExposed || (a.IsEnabled && a.GroupIds.length > 0)) &&
      (!text || `${a.OperationId} ${a.Summary ?? ''} ${a.ApiPath ?? ''} ${a.Tag ?? ''}`.toLowerCase().includes(text));
    cv.refresh();
  }, [apis, search, onlyExposed, cv]);

  // Load the editor fields whenever another API is selected or the data is reloaded.
  useEffect(() => {
    setEditEnabled(selected?.IsEnabled ?? false);
    setEditGroupIds(selected?.GroupIds ?? []);
  }, [selected]);

  const onSelectionChanged = useCallback((s: any) => {
    const flex = s?.control ?? s;
    const row = flex.selection?.row;
    const item = row != null && row >= 0 ? (flex.rows[row]?.dataItem as McpExposableApi | undefined) : undefined;
    setSelectedKey(item ? apiKey(item) : null);
  }, []);

  const toggleGroup = useCallback((groupId: number) => {
    setEditGroupIds((prev) => (prev.includes(groupId) ? prev.filter((g) => g !== groupId) : [...prev, groupId]));
  }, []);

  const isDirty =
    !!selected &&
    (editEnabled !== selected.IsEnabled ||
      editGroupIds.length !== selected.GroupIds.length ||
      editGroupIds.some((g) => !selected.GroupIds.includes(g)));

  const save = useCallback(async () => {
    if (!selected) return;
    dispatch(setIsBusy());
    try {
      await mcpManagementSvc.saveExposedApi({
        AppSource: selected.AppSource,
        OperationId: selected.OperationId,
        IsEnabled: editEnabled,
        GroupIds: editGroupIds,
      });
      await loadData();
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [selected, editEnabled, editGroupIds, dispatch, loadData, showError]);

  const remove = useCallback(async () => {
    if (!selected || selected.ExposedApiId <= 0) return;
    if (!window.confirm('Remove this API from MCP access? External users will lose access to it.')) return;
    dispatch(setIsBusy());
    try {
      await mcpManagementSvc.deleteExposedApi(selected.ExposedApiId);
      await loadData();
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [selected, dispatch, loadData, showError]);

  const refreshCatalog = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      await mcpManagementSvc.refreshApiCatalog();
      await loadData();
    } catch (e) {
      showError(e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, loadData, showError]);

  const groupNames = useCallback(
    (ids: number[]) => ids.map((id) => groups.find((g) => g.GroupId === id)?.GroupName ?? `#${id}`).join(', '),
    [groups]
  );

  const changeWarning = selected && selected.HttpMethod?.toUpperCase() === 'GET' && LOOKS_LIKE_A_CHANGE.test(selected.OperationId);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>MCP API Access</div>
        <div className="flex items-center gap-2">
          <input
            type="text"
            placeholder="Search operations..."
            className={`w-56 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <label className={`flex items-center gap-1 text-xs ${theme.label}`}>
            <input type="checkbox" checked={onlyExposed} onChange={(e) => setOnlyExposed(e.target.checked)} />
            Exposed only
          </label>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={refreshCatalog} title="Re-read the API specs">
            <i className="fa-solid fa-rotate" aria-hidden="true" />
          </button>
        </div>
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
            <FlexGridColumn header="" width={34}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) =>
                  cell.item?.IsEnabled && cell.item.GroupIds.length > 0 ? (
                    <i className="fa-solid fa-circle-check text-green-600" title="Exposed" aria-hidden="true" />
                  ) : null
                }
              />
            </FlexGridColumn>
            <FlexGridColumn binding="HttpMethod" header="Method" width={70} />
            <FlexGridColumn binding="OperationId" header="Operation" width={300} />
            <FlexGridColumn binding="Summary" header="Summary" width={220} />
            <FlexGridColumn header="Groups" width={200}>
              <FlexGridCellTemplate cellType="Cell" template={(cell: any) => (cell.item ? groupNames(cell.item.GroupIds) : '')} />
            </FlexGridColumn>
            <FlexGridColumn binding="AppSource" header="Source" width={80} />
            <FlexGridColumn header="" binding="" width="*" />
          </FlexGrid>
        </div>

        <div className={`w-[340px] h-full shrink-0 overflow-auto border-l px-4 py-3 ${theme.mainContentSection}`}>
          {!selected && <div className={`text-xs ${theme.label}`}>Select an operation to control who can call it through MCP.</div>}

          {selected && (
            <div className="flex flex-col gap-2">
              <div className={`text-sm font-semibold break-all ${theme.title}`}>{selected.OperationId}</div>
              <div className={`text-xs break-all ${theme.label}`}>
                {selected.HttpMethod} {selected.ApiPath}
              </div>
              {selected.Summary && <div className={`text-xs ${theme.label}`}>{selected.Summary}</div>}

              {!selected.InSpec && (
                <div className="text-xs text-red-600">This operation is no longer in the API spec. Remove it.</div>
              )}
              {selected.InSpec && !selected.ForwardsCallerToken && (
                <div className="text-xs text-orange-600">
                  Source "{selected.AppSource}" does not forward the caller&apos;s token (ForwardCallerToken is off), so calls to it are made
                  without the user&apos;s identity.
                </div>
              )}
              {changeWarning && (
                <div className="text-xs text-orange-600">The name suggests this changes data, but it is a GET. Check before exposing it.</div>
              )}

              <label className={`flex items-center gap-2 text-xs ${theme.label}`}>
                <input type="checkbox" checked={editEnabled} onChange={(e) => setEditEnabled(e.target.checked)} />
                Enabled for MCP
              </label>

              <div className={`text-xs font-semibold ${theme.title}`}>Security groups allowed to call it</div>
              {groups.length === 0 && <div className={`text-xs ${theme.label}`}>No security groups found.</div>}
              <div className="flex flex-col gap-1">
                {groups.map((g) => (
                  <label key={g.GroupId} className={`flex items-center gap-2 text-xs ${theme.label}`}>
                    <input type="checkbox" checked={editGroupIds.includes(g.GroupId)} onChange={() => toggleGroup(g.GroupId)} />
                    {g.GroupName}
                  </label>
                ))}
              </div>
              {editEnabled && editGroupIds.length === 0 && (
                <div className="text-xs text-orange-600">No group selected: nobody can call this yet.</div>
              )}

              <div className="flex items-center gap-2 pt-2">
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  disabled={!isDirty}
                  onClick={save}
                  title="Save"
                >
                  <i className="fa-solid fa-floppy-disk" aria-hidden="true" /> Save
                </button>
                {selected.ExposedApiId > 0 && (
                  <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={remove} title="Remove">
                    <i className="fa-solid fa-trash" aria-hidden="true" /> Remove
                  </button>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default CompanyMcpApiAccess;
