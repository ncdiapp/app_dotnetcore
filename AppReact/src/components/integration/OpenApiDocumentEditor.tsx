import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { addTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { useApiServerRoot } from '../../redux/hooks/useApiServerRoot';
import { openApiDocumentService, OpenApiDocumentMember, OpenApiSelectableApi } from '../../webapi/openApiDocumentSvc';
import OpenApiAddApisDialog from './OpenApiAddApisDialog';

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
  const [members, setMembers] = useState<OpenApiDocumentMember[]>([]);
  const [memberView, setMemberView] = useState<CollectionView<any>>(() => new CollectionView<any>([]));
  const [pickerOpen, setPickerOpen] = useState(false);
  const [selectable, setSelectable] = useState<OpenApiSelectableApi[]>([]);
  const [skips, setSkips] = useState<string>('');

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
      bindMembers(doc.Members || []);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [bindMembers, dispatch, errorMessage]);

  useEffect(() => {
    if (param && Number(param) > 0) load(Number(param));
  }, [param, load]);

  const publicUrl = status === 'Published' && code && serverRoot
    ? `${serverRoot.replace(/\/$/, '')}/openapi-doc/${code}`
    : '';

  const save = useCallback(async (): Promise<number> => {
    const saved = await openApiDocumentService.save({
      Id: id,
      Name: name,
      Code: code,
      Version: version,
      Description: description,
      ActionCodes: members.map((m) => m.ActionCode || '').filter(Boolean),
    });
    setId(saved.Id);
    setCode(saved.Code || code);
    setVersion(saved.Version || version);
    setStatus(saved.Status || status);
    bindMembers(saved.Members || []);
    if (!param) {
      const path = `/openapi-doc-editor/${saved.Id}`;
      dispatch(addTab({ tabPath: path, label: `OpenAPI: ${saved.Name || ''}`, isClosable: true }));
      navigate(path);
    }
    return saved.Id;
  }, [bindMembers, code, description, dispatch, id, members, name, navigate, param, status, version]);

  const onSave = async () => {
    dispatch(setIsBusy());
    try {
      await save();
      errorMessage.showInfo('Saved.');
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const onRegenerate = async () => {
    dispatch(setIsBusy());
    try {
      const documentId = await save();
      const result = await openApiDocumentService.regenerate(documentId);
      const doc = result.Document;
      setVersion(doc.Version || version);
      setStatus(doc.Status || status);
      bindMembers(doc.Members || members);
      const lines = (result.Skipped || []).map((s) => `${s.ActionCode || '(blank)'}: ${s.Reason}`);
      setSkips(lines.join('\n'));
      errorMessage.showInfo(lines.length ? 'Regenerated with skipped APIs.' : 'Regenerated.');
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
    const grid = memberView;
    const current = grid.currentItem as OpenApiDocumentMember | null;
    if (!current?.ActionCode) return;
    bindMembers(members.filter((m) => m.ActionCode !== current.ActionCode));
  };

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>{name || 'OpenAPI Document'}</div>
        <div className="flex items-center gap-2">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={openPicker}>Add APIs</button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={removeSelected}>Remove</button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onSave}>Save</button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onRegenerate}>Regenerate</button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onDownload}>Download</button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onCopy}>Copy URL</button>
        </div>
      </div>
      <div className={`px-3 py-2 grid grid-cols-2 gap-2 ${theme.mainContentSection}`}>
        <label className="flex items-center">
          <span className={`w-32 text-xs ${theme.label}`}>Name</span>
          <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} value={name} onChange={(e) => setName(e.target.value)} />
        </label>
        <label className="flex items-center">
          <span className={`w-32 text-xs ${theme.label}`}>Code</span>
          <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} value={code} disabled={id > 0} onChange={(e) => setCode(e.target.value)} />
        </label>
        <label className="flex items-center">
          <span className={`w-32 text-xs ${theme.label}`}>Version</span>
          <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} value={version} onChange={(e) => setVersion(e.target.value)} />
        </label>
        <label className="flex items-center">
          <span className={`w-32 text-xs ${theme.label}`}>Status</span>
          <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} value={status} readOnly />
        </label>
        <label className="flex items-center col-span-2">
          <span className={`w-32 text-xs ${theme.label}`}>Description</span>
          <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>
        <label className="flex items-center col-span-2">
          <span className={`w-32 text-xs ${theme.label}`}>Public URL</span>
          <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} value={publicUrl} readOnly />
        </label>
      </div>
      <div className={`h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid itemsSource={memberView} autoGenerateColumns={false} isReadOnly selectionMode="Row" className="w-full h-full">
          <FlexGridColumn binding="ActionCode" header="ActionCode" width={240} />
          <FlexGridColumn binding="HttpMethod" header="Http Method" width={120} />
          <FlexGridColumn binding="Source" header="Source" width={200} />
          <FlexGridColumn binding="Description" header="Description" width={280} />
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>
      {skips && (
        <pre className={`max-h-24 overflow-auto px-3 py-2 text-xs ${theme.label}`}>{skips}</pre>
      )}
      {pickerOpen && (
        <OpenApiAddApisDialog apis={selectable} onApply={applyApis} onClose={() => setPickerOpen(false)} />
      )}
    </div>
  );
};

export default OpenApiDocumentEditor;
