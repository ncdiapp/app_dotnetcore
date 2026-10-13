import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { updateActiveTabPath, updateCurrentTabLabel } from '../../redux/features/ui/navigation/tabnavSlice';
import { useApiServerRoot } from '../../redux/hooks/useApiServerRoot';
import { openApiDocumentService, OpenApiDocumentMember, OpenApiSelectableApi } from '../../webapi/openApiDocumentSvc';
import OpenApiAddApisDialog from './OpenApiAddApisDialog';
import { openOpenApiSwaggerView } from './openApiSwaggerView';

function repairCode(code: string, name: string): string {
  const source = code.trim() ? code : name;
  return source
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

const OpenApiDocumentEditor: React.FC = () => {
  const { param } = useParams<{ param: string }>();
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const serverRoot = useApiServerRoot();

  const [id, setId] = useState<number>(param ? Number(param) : 0);
  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [version, setVersion] = useState('1.0.0');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState('Draft');
  const [savedStatus, setSavedStatus] = useState('Draft');
  const [members, setMembers] = useState<OpenApiDocumentMember[]>([]);
  const [memberView, setMemberView] = useState<CollectionView<any>>(() => new CollectionView<any>([]));
  const [pickerOpen, setPickerOpen] = useState(false);
  const [selectable, setSelectable] = useState<OpenApiSelectableApi[]>([]);
  const [skips, setSkips] = useState<string>('');
  const [openApiJson, setOpenApiJson] = useState('');
  const memberGridRef = useRef<any>(null);
  const skipNextRouteLoadRef = useRef(false);

  const bindMembers = useCallback((rows: OpenApiDocumentMember[]) => {
    const sorted = [...rows].sort((a, b) => (a.ActionCode || '').localeCompare(b.ActionCode || ''));
    setMembers(sorted);
    const cv = new CollectionView<any>(sorted);
    cv.sortDescriptions.push(new SortDescription('ActionCode', true));
    setMemberView(cv);
  }, []);

  const load = useCallback(async (documentId: number) => {
    if (!documentId) return;
    dispatch(setIsBusy());
    try {
      const doc = await openApiDocumentService.get(documentId);
      setId(doc.Id);
      setName(doc.Name || '');
      setCode(doc.Code || '');
      setVersion(doc.Version || '1.0.0');
      setDescription(doc.Description || '');
      setStatus(doc.Status || 'Draft');
      setSavedStatus(doc.Status || 'Draft');
      setOpenApiJson(doc.OpenApiJson || '');
      bindMembers(doc.Members || []);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [bindMembers, dispatch, errorMessage]);

  useEffect(() => {
    if (skipNextRouteLoadRef.current) {
      skipNextRouteLoadRef.current = false;
      return;
    }
    if (param && Number(param) > 0) load(Number(param));
  }, [param, load]);

  const publicUrl = savedStatus === 'Published' && code && serverRoot
    ? `${serverRoot.replace(/\/$/, '')}/openapi-doc/${code}`
    : '';

  const save = useCallback(async (preserveStatus = false): Promise<number> => {
    const nextCode = repairCode(code, name);
    if (!nextCode) throw new Error('Code could not be made from the name. Use letters or digits.');
    setCode(nextCode);
    const saved = await openApiDocumentService.save({
      Id: id,
      Name: name,
      Code: nextCode,
      Version: version,
      Description: description,
      ActionCodes: members.map((m) => m.ActionCode || '').filter(Boolean),
    });
    setId(saved.Id);
    setCode(saved.Code || code);
    setVersion(saved.Version || version);
    if (!preserveStatus) setStatus(saved.Status || status);
    bindMembers(saved.Members || []);
    const path = `/openapi-doc-editor/${saved.Id}`;
    dispatch(updateCurrentTabLabel(`OpenAPI: ${saved.Name || ''}`));
    if (!param || String(saved.Id) !== String(param)) {
      skipNextRouteLoadRef.current = true;
      dispatch(updateActiveTabPath(path));
      navigate(path, { replace: true });
    }
    return saved.Id;
  }, [bindMembers, code, description, dispatch, id, members, name, navigate, param, status, version]);

  const onSaveAndGenerate = async () => {
    const wantPublished = status === 'Published';
    dispatch(setIsBusy());
    try {
      const documentId = await save(true);
      const result = await openApiDocumentService.regenerate(documentId);
      const doc = result.Document;
      setVersion(doc.Version || version);
      setOpenApiJson(doc.OpenApiJson || '');
      bindMembers(doc.Members || members);
      const lines = (result.Skipped || []).map((s) => `${s.ActionCode || '(blank)'}: ${s.Reason}`);
      setSkips(lines.join('\n'));
      try {
        const publishedDoc = await openApiDocumentService.setPublished(documentId, wantPublished);
        const nextStatus = publishedDoc.Status || (wantPublished ? 'Published' : 'Draft');
        setStatus(nextStatus);
        setSavedStatus(nextStatus);
      } catch (error) {
        setStatus(doc.Status || 'Draft');
        throw error;
      }
      errorMessage.showInfo(lines.length ? 'Saved. Some APIs were skipped.' : 'Saved.');
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const onDownload = async () => {
    if (!id) {
      errorMessage.showError('Save the document first.');
      return;
    }
    const blob = await openApiDocumentService.download(id);
    const href = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = href;
    link.download = `${code || 'openapi'}.json`;
    link.click();
    URL.revokeObjectURL(href);
  };

  const onCopy = async () => {
    if (!publicUrl) {
      errorMessage.showError('Publish the document before copying its URL.');
      return;
    }
    await navigator.clipboard.writeText(publicUrl);
    errorMessage.showInfo('Public URL copied.');
  };

  const openPicker = async () => {
    dispatch(setIsBusy());
    try {
      const rows = await openApiDocumentService.listSelectableApis();
      const taken = new Set(members.map((m) => (m.ActionCode || '').toLowerCase()));
      setSelectable(rows.filter((r) => r.ActionCode && !taken.has(r.ActionCode.toLowerCase())));
      setPickerOpen(true);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const applyApis = (codes: string[]) => {
    const byCode = new Map(selectable.map((r) => [r.ActionCode || '', r]));
    const added: OpenApiDocumentMember[] = codes.map((actionCode) => {
      const row = byCode.get(actionCode);
      return {
        ActionCode: actionCode,
        HttpMethod: row?.HttpMethod || '',
        Source: row?.ProviderKind === 'app' ? (row.ApiType || '') : (row?.ProviderName || ''),
        Description: row?.Description || '',
      };
    });
    bindMembers([...members, ...added]);
    setPickerOpen(false);
  };

  const removeSelected = () => {
    const selected = (memberGridRef.current?.selectedItems || []) as OpenApiDocumentMember[];
    const codes = new Set(selected.map((row) => row?.ActionCode).filter(Boolean));
    if (!codes.size) {
      errorMessage.showInfo('Select one or more APIs to delete.');
      return;
    }
    bindMembers(members.filter((m) => !codes.has(m.ActionCode)));
  };

  const previewText = (() => {
    if (!openApiJson) return '';
    try {
      const doc = JSON.parse(openApiJson);
      if (serverRoot) doc.servers = [{ url: `${serverRoot.replace(/\/$/, '')}/webapi` }];
      return JSON.stringify(doc, null, 2);
    } catch {
      return openApiJson;
    }
  })();

  const listButton = `h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] border text-xs ${theme.button_default}`;
  const field = 'w-[200px] px-2 py-1 border rounded-[4px] text-xs';
  const mini = `w-6 h-5 inline-flex items-center justify-center rounded-[4px] border text-[11px] ${theme.button_default}`;
  const published = status === 'Published';
  const showPublishedUrl = savedStatus === 'Published';

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className="flex items-center gap-4">
          <div className={`text-md font-semibold ${theme.title}`}>{name || 'OpenAPI Document'}</div>
          <div className={`text-xs ${theme.label}`}>Version {version || '1.0.0'}</div>
        </div>
        <div className="flex items-center space-x-2">
          <button type="button" onClick={() => { if (id) load(id); }} disabled={!id}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500" title="Refresh">
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={onSaveAndGenerate}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600" title="Save & Generate OpenAPI Doc">
            <i className="fa-solid fa-floppy-disk" aria-hidden /> Save &amp; Generate OpenAPI Doc
          </button>
          <button
            type="button"
            onClick={onDownload}
            disabled={!id || !openApiJson}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-amber-500 hover:bg-amber-600"
            title="Download JSON"
          >
            <i className="fa-solid fa-download" aria-hidden /> Download JSON
          </button>
          <button
            type="button"
            onClick={() => openOpenApiSwaggerView(serverRoot, { Id: id, Code: code, Status: savedStatus })}
            disabled={!id || !openApiJson || !serverRoot}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-violet-500 hover:bg-violet-600"
            title="View Swagger"
          >
            <i className="fa-solid fa-eye" aria-hidden /> View Swagger
          </button>
        </div>
      </div>
      <div className={`px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className="grid w-full grid-cols-[max-content_max-content_minmax(0,1fr)_max-content] gap-x-6 gap-y-2 items-center">
          <label className="flex items-center gap-2">
            <span className={`w-28 text-xs ${theme.label}`}>Name</span>
            <input className={field} value={name} onChange={(e) => setName(e.target.value)} />
          </label>
          <label className="flex items-center gap-2">
            <span className={`w-28 text-xs ${theme.label}`}>Code</span>
            <input className={field} value={code} onChange={(e) => setCode(e.target.value)} />
          </label>
          <label className="flex items-center gap-2 min-w-0">
            <span className={`w-28 text-xs shrink-0 ${theme.label}`}>Description</span>
            <input className={`w-1 flex-auto min-w-0 px-2 py-1 border rounded-[4px] text-xs`} value={description} onChange={(e) => setDescription(e.target.value)} />
          </label>
          <label className="flex items-center gap-1">
            <input type="checkbox" className="h-3.5 w-3.5" checked={published} onChange={(e) => setStatus(e.target.checked ? 'Published' : 'Draft')} />
            <span className={`text-xs ${theme.label}`}>Is Published</span>
          </label>
          {showPublishedUrl && (
            <>
              <div className="col-span-3 flex items-center gap-2 min-w-0">
                <span className={`w-28 text-xs shrink-0 ${theme.label}`}>Published URL</span>
                <div className={`w-1 flex-auto min-w-0 px-2 py-1 border rounded-[4px] text-xs truncate ${theme.inputBox}`} title={publicUrl}>
                  {publicUrl || ''}
                </div>
              </div>
              <div className="flex items-center gap-2">
                <button type="button" className={mini} onClick={onCopy} title="Copy URL">
                  <i className="fa-solid fa-copy" aria-hidden />
                </button>
                <button type="button" className={mini} onClick={onDownload} title="Download">
                  <i className="fa-solid fa-download" aria-hidden />
                </button>
              </div>
            </>
          )}
        </div>
      </div>
      <div className="h-1 flex-auto flex gap-1 min-h-0">
        <div className="w-1 flex-auto flex flex-col min-w-0 min-h-0">
          <div className={`flex items-center gap-2 px-3 py-2 ${theme.mainContentSection}`}>
            <span className={`w-28 shrink-0 text-xs font-semibold whitespace-nowrap ${theme.title}`}>Selected API List</span>
            <button type="button" className={listButton} onClick={openPicker}>Add APIs</button>
            <button type="button" className={listButton} onClick={removeSelected}>Delete APIs</button>
          </div>
          <div className={`h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
            <FlexGrid itemsSource={memberView} autoGenerateColumns={false} isReadOnly selectionMode="ListBox" initialized={(flex: any) => { memberGridRef.current = flex; }} className="w-full h-full">
              <FlexGridColumn binding="ActionCode" header="ActionCode" width={240} />
              <FlexGridColumn binding="HttpMethod" header="Http Method" width={120} />
              <FlexGridColumn binding="Source" header="Source" width={200} />
              <FlexGridColumn binding="Description" header="Description" width={280} />
              <FlexGridColumn header="" binding="" width="*" />
            </FlexGrid>
          </div>
        </div>
        <div className="w-1 flex-auto flex flex-col min-w-0 min-h-0">
          <div className={`px-3 py-2 ${theme.mainContentSection}`}>
            <span className={`text-xs font-semibold ${theme.title}`}>OpenAPI JSON</span>
          </div>
          <pre className={`h-1 flex-auto overflow-auto px-3 py-2 text-xs ${theme.label} ${theme.mainContentSection}`}>{previewText || 'Save & Generate OpenAPI Doc creates the JSON.'}</pre>
        </div>
      </div>
      {skips && (
        <pre className={`max-h-24 overflow-auto px-3 py-2 text-xs ${theme.label} ${theme.mainContentSection}`}>{skips}</pre>
      )}
      {pickerOpen && (
        <OpenApiAddApisDialog apis={selectable} onApply={applyApis} onClose={() => setPickerOpen(false)} />
      )}
    </div>
  );
};

export default OpenApiDocumentEditor;
