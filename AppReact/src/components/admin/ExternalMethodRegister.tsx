import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';

type ExternalMethodItem = any;
type PluginMethodCandidate = ExternalMethodItem;

type ValidationResult = any;
type OperationCallResult<T> = {
  IsSuccessful?: boolean;
  ObjectList?: T[];
  ValidationResult?: ValidationResult;
};

const methodKey = (item: { AssemblyName?: string; TypeName?: string; MethodName?: string }) =>
  `${item.AssemblyName || ''}|${item.TypeName || ''}|${item.MethodName || ''}`.toLowerCase();

const extractValidationMessages = (validationResult: ValidationResult): string[] => {
  if (!validationResult) return [];

  const collect = (items: any[]) =>
    items.map((item) => item?.ErrorMessage || item?.Message || item?.Description || '').filter(Boolean);

  if (Array.isArray(validationResult)) return collect(validationResult);
  if (Array.isArray(validationResult?.Items)) return collect(validationResult.Items);
  if (Array.isArray(validationResult?.Errors)) return collect(validationResult.Errors);
  if (typeof validationResult === 'string') return [validationResult];
  return [];
};

const ExternalMethodRegister: React.FC = () => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();

  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [registeredMethods, setRegisteredMethods] = useState<ExternalMethodItem[]>([]);
  const [availableMethods, setAvailableMethods] = useState<PluginMethodCandidate[]>([]);
  const [deletedItemIds, setDeletedItemIds] = useState<(number | string)[]>([]);
  const [filterText, setFilterText] = useState('');

  const registerFlexRef = useRef<wjGrid.FlexGrid | null>(null);
  const availableFlexRef = useRef<wjGrid.FlexGrid | null>(null);

  const [registerCv] = useState(() => new CollectionView<any>([]));
  const [availableCv] = useState(() => new CollectionView<any>([]));

  const registeredKeys = useMemo(
    () => new Set(registeredMethods.map((m) => methodKey(m))),
    [registeredMethods],
  );

  const filteredAvailable = useMemo(() => {
      const q = filterText.trim().toLowerCase();
    return availableMethods.filter((m) => {
      if (registeredKeys.has(methodKey(m))) return false;
      if (!q) return true;
      const hay = [
        m.MethodDisplayName,
        m.MethodName,
        m.AssemblyName,
        m.TypeName,
        m.InputParameterList,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();
      return hay.includes(q);
    });
  }, [availableMethods, registeredKeys, filterText]);

  useEffect(() => {
    registerCv.sourceCollection = registeredMethods;
  }, [registerCv, registeredMethods]);

  useEffect(() => {
    availableCv.sourceCollection = filteredAvailable;
  }, [availableCv, filteredAvailable]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const [registerList, discovered] = await Promise.all([
        adminSvc.retrieveAllAppExternalMethodRegisterExDto(),
        adminSvc.discoverExternalPluginMethods(),
      ]);

      setRegisteredMethods(Array.isArray(registerList) ? [...registerList] : []);
      setAvailableMethods(Array.isArray(discovered) ? [...discovered] : []);
      setDeletedItemIds([]);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, errorMessage]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const scrollRegisterToLast = () => {
    setTimeout(() => {
      const flex = registerFlexRef.current;
      if (!flex) return;
      const lastRowIndex = flex.rows.length - 1;
      if (lastRowIndex >= 0) {
        flex.select(new wjGrid.CellRange(lastRowIndex, 0), true);
        flex.scrollIntoView(lastRowIndex, 0);
      }
    }, 0);
  };

  const handleAddBlankRow = () => {
    const newRow: ExternalMethodItem = {
      Id: null,
      MethodDisplayName: '',
      AssemblyName: '',
      TypeName: '',
      MethodName: '',
      InputParameterList: 'AppMasterDetailDto',
    };
    setRegisteredMethods((prev) => [...prev, newRow]);
    scrollRegisterToLast();
  };

  const handleAddFromCandidate = useCallback((candidate?: PluginMethodCandidate | null) => {
    if (!candidate) {
      const flex = availableFlexRef.current;
      const row = flex?.selection?.row;
      candidate = row != null && row >= 0 ? flex?.rows[row]?.dataItem : null;
    }
    if (!candidate) {
      errorMessage.showWarning('Select a discovered method first.');
      return;
    }

    const key = methodKey(candidate);
    setRegisteredMethods((prev) => {
      if (prev.some((m) => methodKey(m) === key)) {
        errorMessage.showWarning('That method is already in the register list.');
        return prev;
      }
      const newRow: ExternalMethodItem = {
        Id: null,
        MethodDisplayName: candidate!.MethodDisplayName || candidate!.MethodName || '',
        AssemblyName: candidate!.AssemblyName || '',
        TypeName: candidate!.TypeName || '',
        MethodName: candidate!.MethodName || '',
        InputParameterList: candidate!.InputParameterList || 'AppMasterDetailDto',
      };
      return [...prev, newRow];
    });
    scrollRegisterToLast();
  }, [errorMessage]);

  // Keep latest adder for one-time FlexGrid dblclick listener.
  const addFromCandidateRef = useRef(handleAddFromCandidate);
  addFromCandidateRef.current = handleAddFromCandidate;

  const handleDeleteRow = () => {
    const flex = registerFlexRef.current;
    if (!flex) return;

    const selectedRowIndex = flex.selection?.row;
    if (selectedRowIndex == null || selectedRowIndex < 0) {
      errorMessage.showWarning('Select a registered method to remove.');
      return;
    }

    const selectedItem = flex.rows[selectedRowIndex]?.dataItem;
    if (!selectedItem) return;

    if (
      !window.confirm(
        `Confirm to remove: ${selectedItem.MethodDisplayName || selectedItem.MethodName || 'Method'} (Id = ${selectedItem.Id ?? 'new'})`,
      )
    ) {
      return;
    }

    if (selectedItem.Id != null) {
      setDeletedItemIds((prev) => [...prev, selectedItem.Id]);
    }
    setRegisteredMethods((prev) => prev.filter((item) => item !== selectedItem));
  };

  const handleSave = async () => {
    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const payload = {
        InternalItems: registeredMethods,
        DeletedItemIds: deletedItemIds,
      };

      const response: OperationCallResult<ExternalMethodItem> =
        await adminSvc.saveAllAppExternalMethodRegisterExDto(payload);

      const messages = extractValidationMessages(response?.ValidationResult);

      if (response?.IsSuccessful) {
        errorMessage.showInfo('External methods saved successfully.');
        const refreshedList = response?.ObjectList ? [...response.ObjectList] : [];
        setRegisteredMethods(refreshedList);
        setDeletedItemIds([]);
      } else if (messages.length) {
        messages.forEach((msg) => errorMessage.showError(msg));
      } else {
        errorMessage.showError('Failed to save external methods.');
      }
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsSaving(false);
      dispatch(setIsNotBusy());
    }
  };

  const busy = isLoading || isSaving;

  return (
    <div className="w-full h-full flex flex-col gap-2 rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>External Method Register</div>
        <div className="flex items-center space-x-2">
          <button
            type="button"
            title="Refresh"
            onClick={loadData}
            disabled={busy}
            className={`w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs ${theme.button_default} disabled:cursor-not-allowed disabled:opacity-60`}
          >
            <i className="fa-solid fa-rotate" />
          </button>
          <button
            type="button"
            title="Add blank row"
            onClick={handleAddBlankRow}
            disabled={busy}
            className={`w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs ${theme.button_default} disabled:cursor-not-allowed disabled:opacity-60`}
          >
            <i className="fa-solid fa-plus" />
          </button>
          <button
            type="button"
            title="Remove selected registered method"
            onClick={handleDeleteRow}
            disabled={busy}
            className={`w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs ${theme.button_default} disabled:cursor-not-allowed disabled:opacity-60`}
          >
            <i className="fa-solid fa-trash" />
          </button>
          <button
            type="button"
            title="Save"
            onClick={handleSave}
            disabled={busy}
            className={`w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs ${theme.button_default} disabled:cursor-not-allowed disabled:opacity-60`}
          >
            <i className="fa-solid fa-floppy-disk" />
          </button>
        </div>
      </div>

      <div className="h-1 flex-auto flex flex-row gap-2 overflow-hidden">
        <section className={`w-[42%] flex-none flex flex-col overflow-hidden ${theme.mainContentSection}`}>
          <div className="flex items-center justify-between gap-2 py-2 px-3">
            <div className="text-sm font-semibold">Available Methods (from DLL)</div>
            <button
              type="button"
              title="Add selected method to register"
              onClick={() => handleAddFromCandidate()}
              disabled={busy}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:cursor-not-allowed disabled:opacity-60`}
            >
              Add Selected
            </button>
          </div>
          <div className="px-3 pb-2">
            <input
              type="text"
              autoComplete="off"
              placeholder="Filter methods..."
              value={filterText}
              onChange={(e) => setFilterText(e.target.value)}
              className={`w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            />
          </div>
          <div className="h-1 flex-auto w-full px-3 pb-3">
            <FlexGrid
              itemsSource={availableCv}
              autoGenerateColumns={false}
              allowDelete={false}
              selectionMode="Row"
              isReadOnly
              initialized={(flex: wjGrid.FlexGrid) => {
                availableFlexRef.current = flex;
                flex.hostElement.addEventListener('dblclick', () => {
                  const row = flex.selection?.row;
                  if (row == null || row < 0) return;
                  const item = flex.rows[row]?.dataItem as PluginMethodCandidate | undefined;
                  if (item) addFromCandidateRef.current(item);
                });
              }}
              className="w-full h-full"
            >
              <FlexGridColumn binding="MethodDisplayName" header="Display Name" width={150} />
              <FlexGridColumn binding="MethodName" header="Method" width={140} />
              <FlexGridColumn binding="AssemblyName" header="Assembly" width={110} />
              <FlexGridColumn binding="InputParameterList" header="Input" width={140} />
              <FlexGridColumn binding="TypeName" header="Type" width="*" />
            </FlexGrid>
          </div>
        </section>

        <section className={`w-1 flex-auto flex flex-col overflow-hidden ${theme.mainContentSection}`}>
          <div className="py-2 px-3 text-sm font-semibold">Registered Methods (AppExternalMethodRegister)</div>
          <div className="h-1 flex-auto w-full p-3 pt-0">
            <FlexGrid
              itemsSource={registerCv}
              autoGenerateColumns={false}
              allowDelete={false}
              selectionMode="Row"
              initialized={(flex: wjGrid.FlexGrid) => {
                registerFlexRef.current = flex;
              }}
              className="w-full h-full"
            >
              <FlexGridColumn binding="Id" header="ID" width={60} isReadOnly format="d" />
              <FlexGridColumn binding="MethodDisplayName" header="Display Name" width={180} />
              <FlexGridColumn binding="AssemblyName" header="Assembly Name" width={130} />
              <FlexGridColumn binding="TypeName" header="Type Name" width={220} />
              <FlexGridColumn binding="MethodName" header="Method Name" width={160} />
              <FlexGridColumn binding="InputParameterList" header="Input Parameters" width={160} />
              <FlexGridColumn header="" binding="" width="*" />
            </FlexGrid>
          </div>
        </section>
      </div>
    </div>
  );
};

export default ExternalMethodRegister;
