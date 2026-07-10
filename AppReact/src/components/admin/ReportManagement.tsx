import React, { useCallback, useEffect, useRef, useState } from 'react';
import { CollectionView } from '@mescius/wijmo';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { adminSvc } from '../../webapi/adminsvc';
import { clampContextMenuPosition } from '../../hooks/useClampedContextMenuPosition';
import ReportTemplateDesigner from './ReportTemplateDesigner';
import DataSourceEditor, { DataSourceDef, serializeConfig } from './DataSourceEditor';

const CONTEXT_MENU_W = 180;
const CONTEXT_MENU_H = 110;
const DS_API = 1;

interface CreateForm {
  ReportName: string;
  Description: string;
  IsActive: boolean;
}

const emptyForm = (): CreateForm => ({
  ReportName: '',
  Description: '',
  IsActive: true,
});

interface ContextMenuState {
  visible: boolean;
  x: number;
  y: number;
  item: any | null;
}

// ─────────────────────────────────────────────────────────────────────────────

const ReportManagement: React.FC = () => {
  const { theme, t } = useTheme();
  const dispatch = useDispatch();
  const { showError } = useErrorMessage();

  const [reports, setReports]         = useState<CollectionView>(() => new CollectionView<any>([]));
  const [selectedReport, setSelectedReport] = useState<any>(null);
  const [designerOpen, setDesignerOpen]     = useState(false);
  const [designerReportId, setDesignerReportId] = useState<number | null>(null);
  const [contextMenu, setContextMenu]       = useState<ContextMenuState>({ visible: false, x: 0, y: 0, item: null });

  // Create modal state
  const [createOpen, setCreateOpen]   = useState(false);
  const [form, setForm]               = useState<CreateForm>(emptyForm());
  const [formSources, setFormSources] = useState<DataSourceDef[]>([{ name: 'header', type: 'sp', value: '' }]);
  const [formBusy, setFormBusy]       = useState(false);
  const [formError, setFormError]     = useState<string | null>(null);

  // Delete confirm state
  const [deleteTarget, setDeleteTarget] = useState<any | null>(null);
  const [deleteBusy, setDeleteBusy]     = useState(false);

  const flexGridRef = useRef<any>(null);

  // ── Data ──────────────────────────────────────────────────────────────────

  const load = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.retrieveAllAppReportEntityDto();
      setReports(new CollectionView<any>(Array.isArray(data) ? data : []));
    } catch (e: any) {
      showError(e?.message ?? 'Failed to load reports');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  useEffect(() => { load(); }, [load]);

  // ── Context menu ──────────────────────────────────────────────────────────

  const openCtx = (e: React.MouseEvent, item: any) => {
    e.preventDefault();
    e.stopPropagation();
    const { x, y } = clampContextMenuPosition(e.clientX, e.clientY, CONTEXT_MENU_W, CONTEXT_MENU_H);
    setContextMenu({ visible: true, x, y, item });
  };
  const closeCtx = () => setContextMenu(c => ({ ...c, visible: false }));

  // ── Create ────────────────────────────────────────────────────────────────

  const openCreate = () => {
    setForm(emptyForm());
    setFormSources([{ name: 'header', type: 'sp', value: '' }]);
    setFormError(null);
    setCreateOpen(true);
  };

  const handleFormChange = (field: keyof CreateForm, value: any) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleCreate = async () => {
    if (!form.ReportName.trim()) { setFormError('Report Name is required.'); return; }
    setFormBusy(true);
    setFormError(null);
    try {
      const activeSources = formSources.filter(s => s.value.trim());
      const primarySp     = activeSources.find(s => s.type === 'sp')?.value ?? '';
      const hasApi        = activeSources.some(s => s.type === 'api');
      const configJson    = activeSources.length > 0 ? serializeConfig(activeSources, null) : null;

      const dto = {
        ReportName:       form.ReportName.trim(),
        Description:      form.Description.trim() || null,
        ReportFileName:   primarySp || null,
        IsActive:         form.IsActive,
        ReportEngineType: hasApi ? 1 : 0,
        // Embed data sources in the template sub-object right at creation
        ReportTemplate: activeSources.length > 0 ? {
          ReportId:         0,  // will be set by server
          DataSpName:       primarySp || null,
          ExtraParamConfig: configJson,
          PageSize:         'A4',
          Orientation:      'portrait',
          MarginMm:         15,
        } : undefined,
      };
      await adminSvc.saveOneAppReportEntityDto(dto);
      setCreateOpen(false);
      await load();
    } catch (e: any) {
      setFormError(e?.message ?? 'Save failed.');
    } finally {
      setFormBusy(false);
    }
  };

  // ── Delete ────────────────────────────────────────────────────────────────

  const confirmDelete = (item: any) => {
    setDeleteTarget(item);
    closeCtx();
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleteBusy(true);
    try {
      await adminSvc.deleteOneAppReportEntityDto(String(deleteTarget.Id));
      setDeleteTarget(null);
      await load();
    } catch (e: any) {
      showError(e?.message ?? 'Delete failed.');
    } finally {
      setDeleteBusy(false);
    }
  };

  // ── Designer ──────────────────────────────────────────────────────────────

  const openDesigner = (item: any) => {
    setSelectedReport(item);
    setDesignerReportId(item?.Id ?? null);
    setDesignerOpen(true);
    closeCtx();
  };

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden"
         onClick={closeCtx}>

      {/* Header bar */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Report Management</div>
        <div className="flex gap-2">
          <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`} onClick={openCreate}>
            <i className="fa-solid fa-plus mr-1" />Create Report
          </button>
          <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => load()}>
            <i className="fa-solid fa-rotate mr-1" />Refresh
          </button>
        </div>
      </div>

      {/* Grid */}
      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          ref={flexGridRef}
          className="w-full h-full"
          itemsSource={reports}
          isReadOnly
          headersVisibility="Column"
          selectionMode="Row"
          selectionChanged={(s: any) => {
            const flex = s?.control ?? s;
            const row = flex.selection?.row;
            if (row != null && row >= 0) setSelectedReport(flex.rows[row]?.dataItem ?? null);
          }}
        >
          <FlexGridColumn header="ID"          binding="Id"             width={70} />
          <FlexGridColumn header="Name"        binding="ReportName"     width={200} />
          <FlexGridColumn header="Description" binding="Description"    width={220} />
          <FlexGridColumn header="Data Source" binding="ReportEngineType" width={130}>
            <FlexGridCellTemplate cellType="Cell" template={(cell: any) => (
              <span className={cell.item?.ReportEngineType === DS_API ? 'text-purple-500' : 'text-blue-500'}>
                {cell.item?.ReportEngineType === DS_API
                  ? <><i className="fa-solid fa-network-wired mr-1" />API</>
                  : <><i className="fa-solid fa-database mr-1" />SP</>}
              </span>
            )} />
          </FlexGridColumn>
          <FlexGridColumn header="SP / API Path" binding="ReportFileName" width={200} />
          <FlexGridColumn header="Active"      binding="IsActive"       width={70} />
          <FlexGridColumn header="Template"    binding="Id"             width={90}>
            <FlexGridCellTemplate cellType="Cell" template={(cell: any) => (
              <span className={cell.item?.hasTemplate ? 'text-green-600' : 'text-gray-400'}>
                {cell.item?.hasTemplate ? 'Yes' : '—'}
              </span>
            )} />
          </FlexGridColumn>
          <FlexGridColumn header="Actions" binding="" width={120}>
            <FlexGridCellTemplate cellType="Cell" template={(cell: any) => (
              <div className="flex gap-1">
                <button
                  className={`px-2 py-0.5 text-xs rounded-[4px] ${theme.button_secondary}`}
                  onClick={e => { e.stopPropagation(); openDesigner(cell.item); }}
                  title="Design template"
                >
                  <i className="fa-solid fa-pen-to-square mr-1" />Design
                </button>
                <button
                  className="px-2 py-0.5 text-xs rounded-[4px] text-red-500 border border-red-300 hover:bg-red-50"
                  onClick={e => { e.stopPropagation(); confirmDelete(cell.item); }}
                  title="Delete report"
                >
                  <i className="fa-solid fa-trash" />
                </button>
              </div>
            )} />
          </FlexGridColumn>
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>

      {/* Context menu */}
      {contextMenu.visible && (
        <div
          className={`fixed z-50 shadow-lg rounded-[4px] py-1 text-sm ${theme.contextMenu}`}
          style={{ left: contextMenu.x, top: contextMenu.y, minWidth: CONTEXT_MENU_W }}
          onClick={e => e.stopPropagation()}
        >
          <button className="w-full text-left px-3 py-1.5 hover:bg-blue-600 hover:text-white"
            onClick={() => contextMenu.item && openDesigner(contextMenu.item)}>
            <i className="fa-solid fa-pen-to-square mr-2" />Design Template
          </button>
          <hr className="my-0.5 border-gray-200" />
          <button className="w-full text-left px-3 py-1.5 text-red-500 hover:bg-red-600 hover:text-white"
            onClick={() => contextMenu.item && confirmDelete(contextMenu.item)}>
            <i className="fa-solid fa-trash mr-2" />Delete
          </button>
        </div>
      )}

      {/* ── Create Report modal ─────────────────────────────────────────── */}
      {createOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
             onClick={() => setCreateOpen(false)}>
          <div
            className={`w-[480px] max-h-[90vh] flex flex-col rounded-lg shadow-2xl overflow-hidden border ${t('border_mainContentSection')} ${theme.mainContentSection}`}
            onClick={e => e.stopPropagation()}
          >
            {/* Modal header */}
            <div className={`flex-shrink-0 flex items-center justify-between px-4 py-3 border-b ${t('border_mainContentSection')}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>
                <i className="fa-solid fa-plus mr-2 text-blue-500" />Create New Report
              </span>
              <button onClick={() => setCreateOpen(false)} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                <i className="fa-solid fa-xmark" />
              </button>
            </div>

            {/* Form body */}
            <div className="flex-auto overflow-y-auto px-4 py-4 space-y-3">

              {/* Report Name */}
              <div className="flex items-center">
                <label className={`w-36 text-xs font-medium ${theme.label}`}>
                  Report Name <span className="text-red-500">*</span>
                </label>
                <input
                  className={`flex-auto h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                  value={form.ReportName}
                  onChange={e => handleFormChange('ReportName', e.target.value)}
                  placeholder="e.g. Style Summary Report"
                  autoFocus
                />
              </div>

              {/* Description */}
              <div className="flex items-start">
                <label className={`w-36 text-xs font-medium mt-1 ${theme.label}`}>Description</label>
                <textarea
                  className={`flex-auto h-16 px-2 py-1 text-xs border rounded-[4px] resize-none ${theme.inputBox}`}
                  value={form.Description}
                  onChange={e => handleFormChange('Description', e.target.value)}
                  placeholder="Optional description"
                />
              </div>

              {/* Data Sources */}
              <div>
                <label className={`block text-xs font-medium mb-2 ${theme.label}`}>
                  Data Sources
                  <span className={`ml-2 font-normal opacity-60`}>— add SP or API endpoints</span>
                </label>
                <DataSourceEditor
                  sources={formSources}
                  onChange={setFormSources}
                />
              </div>

              {/* Is Active */}
              <div className="flex items-center">
                <label className={`w-36 text-xs font-medium ${theme.label}`}>Active</label>
                <label className={`flex items-center gap-1.5 text-xs cursor-pointer ${theme.label}`}>
                  <input
                    type="checkbox"
                    checked={form.IsActive}
                    onChange={e => handleFormChange('IsActive', e.target.checked)}
                  />
                  Enable this report
                </label>
              </div>

              {formError && (
                <p className="text-xs text-red-500 ml-36">{formError}</p>
              )}
            </div>

            {/* Modal footer */}
            <div className={`flex-shrink-0 flex justify-end gap-2 px-4 py-3 border-t ${t('border_mainContentSection')}`}>
              <button onClick={() => setCreateOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
              <button onClick={handleCreate} disabled={formBusy} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}>
                {formBusy
                  ? <><i className="fa-solid fa-spinner fa-spin mr-1" />Saving…</>
                  : <><i className="fa-solid fa-plus mr-1" />Create</>}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Delete confirm modal ────────────────────────────────────────── */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
             onClick={() => setDeleteTarget(null)}>
          <div
            className={`w-[400px] rounded-lg shadow-2xl overflow-hidden border ${t('border_mainContentSection')} ${theme.mainContentSection}`}
            onClick={e => e.stopPropagation()}
          >
            <div className={`flex items-center gap-2 px-4 py-3 border-b ${t('border_mainContentSection')}`}>
              <i className="fa-solid fa-triangle-exclamation text-red-500" />
              <span className={`text-sm font-semibold ${theme.title}`}>Delete Report</span>
            </div>
            <div className="px-4 py-5">
              <p className={`text-sm ${theme.label}`}>
                Are you sure you want to delete <strong>"{deleteTarget.ReportName}"</strong> (ID {deleteTarget.Id})?
              </p>
              <p className={`text-xs mt-2 ${theme.label} opacity-70`}>
                This will also remove the associated HTML template. This action cannot be undone.
              </p>
            </div>
            <div className={`flex justify-end gap-2 px-4 py-3 border-t ${t('border_mainContentSection')}`}>
              <button onClick={() => setDeleteTarget(null)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
              <button onClick={handleDelete} disabled={deleteBusy}
                className="px-3 py-1.5 text-sm rounded-[4px] bg-red-500 hover:bg-red-600 text-white">
                {deleteBusy
                  ? <><i className="fa-solid fa-spinner fa-spin mr-1" />Deleting…</>
                  : <><i className="fa-solid fa-trash mr-1" />Delete</>}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Template designer overlay */}
      {designerOpen && designerReportId != null && (
        <div className="fixed inset-0 z-40 flex">
          <div className={`w-full h-full overflow-hidden flex flex-col border-0`}>
            <ReportTemplateDesigner
              reportId={designerReportId}
              mainReferenceId={0}
              initialView="design"
              onSaved={() => load()}
              onClose={() => { setDesignerOpen(false); setDesignerReportId(null); }}
            />
          </div>
        </div>
      )}
    </div>
  );
};

export default ReportManagement;
