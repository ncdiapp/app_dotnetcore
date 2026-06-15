/**
 * Unit Linked Search Editor (create/edit one linked search)
 * Ported from Angular: TransactionUnitLinkedSearchEditor
 */
import React, { useState, useEffect, useCallback } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { adminSvc } from '../../../webapi/adminsvc';

export interface TransactionUnitLinkedSearchEditorProps {
  isOpen: boolean;
  transactionUnitId: number;
  linkedSearchId?: number;
  unitDisplayName?: string | null;
  onClose: () => void;
  onSaved: () => void;
}

const ACTION_OPTIONS = [
  { Id: 3, Display: 'Open Search' },
  { Id: 1, Display: 'Add Grid Row(s) By Search' },
  { Id: 2, Display: 'Update Form Field By Search' },
];

const TransactionUnitLinkedSearchEditor: React.FC<TransactionUnitLinkedSearchEditorProps> = ({
  isOpen,
  transactionUnitId,
  linkedSearchId,
  unitDisplayName,
  onClose,
  onSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages } = useErrorMessage();
  const dispatch = useDispatch();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [data, setData] = useState<any>({
    TransactionUnitId: transactionUnitId,
    Name: '',
    Sort: 0,
    Action: 3,
    SearchId: null,
    SearchSaveId: null,
    SearchViewId: null,
    IsSingleSelectedRow: false,
    AppTransactionUnitSearchFieldMappingList: [],
    AppTransactionUnitSearchViewFieldMappingList: [],
  });
  const [searchList, setSearchList] = useState<any[]>([]);
  const [searchSavedList, setSearchSavedList] = useState<any[]>([]);
  const [searchViewList, setSearchViewList] = useState<any[]>([]);

  const loadLookups = useCallback(async () => {
    try {
      const mass = await adminSvc.getMassEntitiesLookupItem('AppSearch|AppSearchSaved|AppSearchView');
      if (mass) {
        setSearchList(Array.isArray(mass['AppSearch']) ? mass['AppSearch'] : []);
        setSearchSavedList(Array.isArray(mass['AppSearchSaved']) ? mass['AppSearchSaved'] : []);
        setSearchViewList(Array.isArray(mass['AppSearchView']) ? mass['AppSearchView'] : []);
      }
    } catch (_) {
      setSearchList([]);
      setSearchSavedList([]);
      setSearchViewList([]);
    }
  }, []);

  const loadData = useCallback(async () => {
    if (!isOpen) return;
    setLoading(true);
    dispatch(setIsBusy());
    try {
      await loadLookups();
      if (linkedSearchId) {
        const dto = await appTransactionService.retrieveOneAppTransactionUnitLinkedSearchExDto(String(linkedSearchId));
        setData({
          ...dto,
          TransactionUnitId: transactionUnitId,
          AppTransactionUnitSearchFieldMappingList: dto?.AppTransactionUnitSearchFieldMappingList ?? [],
          AppTransactionUnitSearchViewFieldMappingList: dto?.AppTransactionUnitSearchViewFieldMappingList ?? [],
        });
      } else {
        setData({
          TransactionUnitId: transactionUnitId,
          Name: '',
          Sort: 0,
          Action: 3,
          SearchId: null,
          SearchSaveId: null,
          SearchViewId: null,
          IsSingleSelectedRow: false,
          AppTransactionUnitSearchFieldMappingList: [],
          AppTransactionUnitSearchViewFieldMappingList: [],
        });
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load');
      onClose();
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [isOpen, linkedSearchId, transactionUnitId, dispatch, showError, onClose, loadLookups]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleSave = useCallback(async () => {
    setSaving(true);
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.saveAppTransactionUnitLinkedSearchExDto(data);
      if (result?.ValidationResult) showValidationMessages(result.ValidationResult, true);
      if (result?.IsSuccessful) {
        onSaved();
        onClose();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
      setSaving(false);
    }
  }, [data, dispatch, showError, showValidationMessages, onSaved, onClose]);

  const update = useCallback((key: string, value: any) => {
    setData((prev: any) => ({ ...prev, [key]: value }));
  }, []);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[10001] flex items-center justify-center bg-black bg-opacity-50">
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: 480, maxHeight: '90vh' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>
            {linkedSearchId ? 'Edit' : 'New'} Linked Search {unitDisplayName ? `: ${unitDisplayName}` : ''}
          </div>
          <button type="button" onClick={onClose} className="text-xl leading-none px-2">&times;</button>
        </div>
        <div className="p-4 overflow-y-auto space-y-3">
          {loading ? (
            <div className={theme.label}>Loading...</div>
          ) : (
            <>
              <div className="flex items-center gap-2">
                <label className={`w-24 text-xs ${theme.label}`}>Name</label>
                <input
                  type="text"
                  value={data.Name ?? ''}
                  onChange={(e) => update('Name', e.target.value)}
                  className={`flex-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                />
              </div>
              <div className="flex items-center gap-2">
                <label className={`w-24 text-xs ${theme.label}`}>Sort</label>
                <input
                  type="number"
                  value={data.Sort ?? 0}
                  onChange={(e) => update('Sort', parseInt(e.target.value, 10) || 0)}
                  className={`w-20 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                />
              </div>
              <div className="flex items-center gap-2">
                <label className={`w-24 text-xs ${theme.label}`}>Action</label>
                <select
                  value={data.Action ?? 3}
                  onChange={(e) => update('Action', parseInt(e.target.value, 10))}
                  className={`flex-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                >
                  {ACTION_OPTIONS.map((o) => (
                    <option key={o.Id} value={o.Id}>{o.Display}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-2">
                <label className={`w-24 text-xs ${theme.label}`}>Search</label>
                <select
                  value={data.SearchId ?? ''}
                  onChange={(e) => update('SearchId', e.target.value ? Number(e.target.value) : null)}
                  className={`flex-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                >
                  <option value="">-- Select --</option>
                  {searchList.map((s: any) => (
                    <option key={s.Id} value={s.Id}>{s.Display ?? s.Id}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-2">
                <label className={`w-24 text-xs ${theme.label}`}>Saved Search</label>
                <select
                  value={data.SearchSaveId ?? ''}
                  onChange={(e) => update('SearchSaveId', e.target.value ? Number(e.target.value) : null)}
                  className={`flex-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                >
                  <option value="">-- Select --</option>
                  {searchSavedList.map((s: any) => (
                    <option key={s.Id} value={s.Id}>{s.Display ?? s.Id}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-2">
                <label className={`w-24 text-xs ${theme.label}`}>Search View</label>
                <select
                  value={data.SearchViewId ?? ''}
                  onChange={(e) => update('SearchViewId', e.target.value ? Number(e.target.value) : null)}
                  className={`flex-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                >
                  <option value="">-- Select --</option>
                  {searchViewList.map((s: any) => (
                    <option key={s.Id} value={s.Id}>{s.Display ?? s.Id}</option>
                  ))}
                </select>
              </div>
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={Boolean(data.IsSingleSelectedRow)}
                  onChange={(e) => update('IsSingleSelectedRow', e.target.checked)}
                />
                <span className={`text-xs ${theme.label}`}>Single row selection</span>
              </label>
              {/* Field mapping summary – mappings are preserved on save when editing */}
              {((data.AppTransactionUnitSearchFieldMappingList?.length) || (data.AppTransactionUnitSearchViewFieldMappingList?.length)) ? (
                <div className={`text-xs ${theme.label} mt-2 pt-2 border-t`}>
                  Field mappings: Search {data.AppTransactionUnitSearchFieldMappingList?.length ?? 0}, Search view {data.AppTransactionUnitSearchViewFieldMappingList?.length ?? 0}.
                  <span className="block mt-0.5 opacity-80">Edit in Angular editor to change mappings.</span>
                </div>
              ) : null}
            </>
          )}
        </div>
        <div className={`px-4 py-3 border-t flex justify-end gap-2 ${theme.mainContentSection}`}>
          <button type="button" onClick={onClose} className={`px-3 py-1 text-xs rounded ${theme.button_default}`}>
            Cancel
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={loading || saving}
            className={`px-3 py-1 text-xs rounded ${theme.button_default} disabled:opacity-50`}
          >
            Save
          </button>
        </div>
      </div>
    </div>
  );
};

export default TransactionUnitLinkedSearchEditor;
