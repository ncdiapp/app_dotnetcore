import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useDispatch } from 'react-redux';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../../../webapi/adminsvc';
import { appTransactionService } from '../../../../webapi/apptransactionsvc';
import ApplicationFormBuilder from '../../ApplicationFormBuilder';
import { PopupModalOverlay } from '../../../formMgt/PopupModalOverlay';
import { ChildCommandEditorPopup } from './ChildCommandEditorPopup';
import { buildCommandDisplayName, resolveIsBatchCommand } from './childCommandGridHelpers';

export const EmAppTransactionCommandTypeCompositionCommand = 200;

type OperationTypeOption = { Id: any; Display: string };

export function CompositionCommandSection(props: {
  action: any;
  hierarchy: any;
  applicationId: string | null;
  transactionId: number | null;
  transactionName?: string | null;
  rootLevelSwitchConditionTransFieldLookUpList: any[];
  rootLevelTransFieldLookUpList: any[];
  rootLevelConditionTransFieldLookUpList: any[];
  transactionFieldLookUpList: any[];
  rootLevelAllFieldLookUpList: any[];
  operationTypeOptions: OperationTypeOption[];
  onMarkChange: () => void;
  onCommandAdded?: (commandDto: any) => void;
}) {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showValidationMessages } = useErrorMessage();
  const {
    action,
    hierarchy,
    applicationId,
    transactionId,
    transactionName,
    rootLevelSwitchConditionTransFieldLookUpList,
    rootLevelTransFieldLookUpList,
    rootLevelConditionTransFieldLookUpList,
    transactionFieldLookUpList,
    rootLevelAllFieldLookUpList,
    operationTypeOptions,
    onMarkChange,
    onCommandAdded,
  } = props;

  const actionType = Number(action?.ActionType);
  const show = !!action && actionType === EmAppTransactionCommandTypeCompositionCommand;

  const [childActionCV] = useState(() => new CollectionView<any>([]));
  const [availableChildActions, setAvailableChildActions] = useState<any[]>([]);
  const [childActionDataMap, setChildActionDataMap] = useState<DataMap | null>(null);
  const [childActionConditionDataMap, setChildActionConditionDataMap] = useState<DataMap | null>(null);
  const [rootLevelTransFieldDataMap, setRootLevelTransFieldDataMap] = useState<DataMap | null>(null);
  const [childUnitDataMap, setChildUnitDataMap] = useState<DataMap | null>(null);
  const [transactionList, setTransactionList] = useState<any[]>([]);

  const [addExistingDropdownOpen, setAddExistingDropdownOpen] = useState(false);
  const [internalSelectorOpen, setInternalSelectorOpen] = useState(false);
  const [externalSelectorOpen, setExternalSelectorOpen] = useState(false);
  const [externalTransactionId, setExternalTransactionId] = useState<string | number | null>(null);
  const [externalCommandId, setExternalCommandId] = useState<string | number | null>(null);
  const [externalCommandList, setExternalCommandList] = useState<any[]>([]);

  const [childEditorCommandId, setChildEditorCommandId] = useState<number | null>(null);
  const [externalFormBuilderOpen, setExternalFormBuilderOpen] = useState(false);
  const [externalFormBuilderParams, setExternalFormBuilderParams] = useState<{
    applicationId: string | null;
    transactionId: number;
    initialSelectedCommandId: number;
    transactionType: number | null;
    modelName: string | null;
  } | null>(null);

  const gridRef = useRef<any>(null);
  const addExistingDropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!show) return;
    action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
    action.ActionAttribute.ChildActionList = action.ActionAttribute.ChildActionList || [];
    childActionCV.sourceCollection = action.ActionAttribute.ChildActionList;
    childActionCV.sortDescriptions.clear();
    childActionCV.sortDescriptions.push(new SortDescription('Sort', true));
    childActionCV.refresh();
  }, [action, childActionCV, show]);

  useEffect(() => {
    if (!show) return;
    const list = (hierarchy?.CommandActionList || []).filter((c: any) => Number(c?.Id) !== Number(action?.Id));
    const normalized = list
      .map((c: any) => {
        const displayName = buildCommandDisplayName(c);
        return { ...c, displayName, Display: displayName };
      })
      .sort((a: any, b: any) => String(a.displayName).localeCompare(String(b.displayName)));
    setAvailableChildActions(normalized);
    setChildActionDataMap(new DataMap(normalized, 'Id', 'displayName'));

    setRootLevelTransFieldDataMap(new DataMap(rootLevelTransFieldLookUpList || [], 'Id', 'Display'));

    const childUnits: any[] = [];
    (hierarchy?.AppTransactionUnitList || []).forEach((u: any) => {
      (u.Children || []).forEach((cu: any) => {
        childUnits.push({ Id: cu.Id, ShortDisplay: `${cu.DataBaseTableName || cu.UnitDisplayName}(${cu.Id})` });
      });
    });
    setChildUnitDataMap(new DataMap(childUnits, 'Id', 'ShortDisplay'));
  }, [action?.Id, hierarchy, rootLevelTransFieldLookUpList, show]);

  const getCommandDataModelDisplay = useCallback(
    (externalTransactionId: any) => {
      if (externalTransactionId) {
        const t = transactionList.find((x: any) => Number(x.Id) === Number(externalTransactionId));
        return t?.TransactionName ?? t?.Name ?? String(externalTransactionId);
      }
      return transactionName ?? hierarchy?.TransactionName ?? hierarchy?.Name ?? '';
    },
    [hierarchy, transactionList, transactionName],
  );

  const loadChildConditionDataMap = useCallback(async () => {
    if (!show) return;
    const switchFieldId = action?.ActionAttribute?.ChildCommandsSwitchConditionFieldId;
    if (!switchFieldId) {
      setChildActionConditionDataMap(null);
      return;
    }
    const field = (rootLevelSwitchConditionTransFieldLookUpList || []).find(
      (f: any) => Number(f.Id) === Number(switchFieldId),
    );
    const entityId = field?.EntityId;
    if (!entityId) {
      setChildActionConditionDataMap(null);
      return;
    }
    try {
      const lookupItems = await adminSvc.getLookupItemListByEntityInfoId(String(entityId));
      setChildActionConditionDataMap(new DataMap(lookupItems || [], 'Id', 'Display'));
    } catch (e: any) {
      setChildActionConditionDataMap(null);
      showError(e?.message || 'Failed to load switch condition lookup items');
    }
  }, [action?.ActionAttribute?.ChildCommandsSwitchConditionFieldId, rootLevelSwitchConditionTransFieldLookUpList, show, showError]);

  useEffect(() => {
    if (!show) return;
    void loadChildConditionDataMap();
    (action?.ActionAttribute?.ChildActionList || []).forEach((c: any) => {
      c.PredictValue = null;
    });
  }, [action?.ActionAttribute?.ChildCommandsSwitchConditionFieldId, loadChildConditionDataMap, show]);

  const addChildAction = useCallback(
    (commandDto?: any, externalTxId?: string | number) => {
      if (!show) return null;
      action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
      action.ActionAttribute.ChildActionList = action.ActionAttribute.ChildActionList || [];
      const maxSort = Math.max(0, ...action.ActionAttribute.ChildActionList.map((x: any) => Number(x.Sort) || 0));
      const child: any = { Sort: maxSort + 1, CommandId: null };
      if (commandDto) {
        child.CommandId = commandDto.Id ?? null;
        child.CommandDisplay = commandDto.Display ?? commandDto.displayName ?? commandDto.Name ?? '';
        child.IsBatchCommand = resolveIsBatchCommand(commandDto, hierarchy, externalTxId);
      }
      if (externalTxId != null) {
        child.ExternalTransactionId = externalTxId;
        if (commandDto) {
          child.CommandDisplay = 'External: ' + (commandDto.Name || '');
          child.IsBatchCommand = resolveIsBatchCommand(commandDto, hierarchy, externalTxId);
        }
      }
      action.ActionAttribute.ChildActionList.push(child);
      childActionCV.refresh();
      onMarkChange();
      return child;
    },
    [action, childActionCV, hierarchy, onMarkChange, show],
  );

  const removeSelectedChild = useCallback(() => {
    if (!show) return;
    const flex = gridRef.current?.control ?? gridRef.current;
    const rowIndex = flex?.selection?.row;
    if (typeof rowIndex !== 'number' || rowIndex < 0) return;
    const item = flex?.rows?.[rowIndex]?.dataItem;
    if (!item) return;
    const list = action?.ActionAttribute?.ChildActionList || [];
    const idx = list.indexOf(item);
    if (idx >= 0) {
      list.splice(idx, 1);
      childActionCV.refresh();
      onMarkChange();
    }
  }, [action, childActionCV, onMarkChange, show]);

  const createNewChildAction = useCallback(async () => {
    if (!show || !hierarchy?.Id || transactionId == null) return;
    const list = hierarchy.CommandActionList || [];
    const maxOrder = Math.max(0, ...list.map((c: any) => Number(c.ActionFlowOrder) || 0));
    const newAction: any = {
      CommandTransactionId: hierarchy.Id,
      ActionGuid: 'guid-' + Math.random().toString(36).slice(2),
      ActionFlowOrder: maxOrder + 1,
      Name: '_ChildCommand' + (maxOrder + 1),
      NextTransactionId: null,
      ActionType: 42,
      NotificationDestinationUserIdtransactionFiledId: null,
      NotificationDestinationRoleIdtransactionFiledId: null,
      DataLoadId: null,
      CommandConditionTransactionFieldId: null,
      ActionAttribute: {
        LinkToUI: false,
        IsLogCommandStartEnd: false,
        IsLogErrorDetails: true,
        SqlStatement: '',
        ChildActionList: [],
      },
    };
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.createOneTransactionCommand(newAction);
      if (!result?.IsSuccessful || !result?.Object) {
        showValidationMessages(result?.ValidationResult);
        return;
      }
      const commandExDto = result.Object;
      commandExDto.Display = buildCommandDisplayName(commandExDto);
      if (!hierarchy.CommandActionList) hierarchy.CommandActionList = [];
      hierarchy.CommandActionList.push(commandExDto);
      onCommandAdded?.(commandExDto);
      addChildAction(commandExDto);
    } catch (e: any) {
      showError(e?.message || 'Failed to create child command');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    addChildAction,
    dispatch,
    hierarchy,
    onCommandAdded,
    show,
    showError,
    showValidationMessages,
    transactionId,
  ]);

  const loadTransactionList = useCallback(async () => {
    try {
      const list = await appTransactionService.retrieveAllAppTransactions(false, '', true);
      setTransactionList(Array.isArray(list) ? list : []);
    } catch (_) {
      setTransactionList([]);
    }
  }, []);

  const loadExternalCommandList = useCallback(async (txId: string | number) => {
    try {
      const data = await appTransactionService.getOneHierarchyTransaction(String(txId), false, '', '', '', false, '');
      setExternalCommandList(data?.CommandActionList || []);
    } catch (_) {
      setExternalCommandList([]);
    }
  }, []);

  const openInternalSelector = useCallback(() => {
    setAddExistingDropdownOpen(false);
    setInternalSelectorOpen(true);
  }, []);

  const openExternalSelector = useCallback(() => {
    setAddExistingDropdownOpen(false);
    setExternalTransactionId(null);
    setExternalCommandId(null);
    setExternalCommandList([]);
    setExternalSelectorOpen(true);
  }, []);

  const applyInternalCommand = useCallback(
    (commandDto: any) => {
      if (!commandDto) return;
      addChildAction(commandDto);
      setInternalSelectorOpen(false);
    },
    [addChildAction],
  );

  const applyExternalCommand = useCallback(() => {
    if (externalTransactionId == null || externalCommandId == null) return;
    const externalCommandDto = externalCommandList.find(
      (c: any) => c.Id === externalCommandId || c.Id === Number(externalCommandId),
    );
    if (!externalCommandDto) return;
    const child = addChildAction(externalCommandDto, externalTransactionId);
    if (child) {
      child.CommandId = externalCommandDto.Id;
      child.ExternalTransactionId = externalTransactionId;
      child.CommandDisplay = 'External: ' + (externalCommandDto.Name || '');
    }
    setExternalSelectorOpen(false);
    setExternalTransactionId(null);
    setExternalCommandId(null);
    setExternalCommandList([]);
  }, [addChildAction, externalCommandId, externalCommandList, externalTransactionId]);

  const openChildCommandEditor = useCallback(
    (row: any) => {
      if (!row?.CommandId) return;
      if (row.ExternalTransactionId) {
        void (async () => {
          try {
            const data = await appTransactionService.getOneHierarchyTransaction(
              String(row.ExternalTransactionId),
              false,
              '',
              '',
              '',
              false,
              '',
            );
            setExternalFormBuilderParams({
              applicationId: data?.SaasApplicationId ?? applicationId,
              transactionId: Number(row.ExternalTransactionId),
              initialSelectedCommandId: Number(row.CommandId),
              transactionType: data?.TransactionOrganizedType ?? null,
              modelName: data?.TransactionName ?? null,
            });
            setExternalFormBuilderOpen(true);
          } catch (e: any) {
            showError(e?.message || 'Failed to open external command editor');
          }
        })();
        return;
      }
      setChildEditorCommandId(Number(row.CommandId));
    },
    [applicationId, showError],
  );

  const handleChildCommandApplied = useCallback(
    (updatedCommand: any, row: any) => {
      if (row) {
        row.CommandDisplay = buildCommandDisplayName(updatedCommand);
        row.IsBatchCommand = resolveIsBatchCommand(updatedCommand, hierarchy, row.ExternalTransactionId);
        childActionCV.refresh();
      }
      onMarkChange();
    },
    [childActionCV, hierarchy, onMarkChange],
  );

  useEffect(() => {
    if (!show) return;
    const hasExternal = (action?.ActionAttribute?.ChildActionList || []).some((c: any) => c?.ExternalTransactionId);
    if ((externalSelectorOpen || hasExternal) && transactionList.length === 0) void loadTransactionList();
  }, [action?.ActionAttribute?.ChildActionList, externalSelectorOpen, loadTransactionList, show, transactionList.length]);

  useEffect(() => {
    if (externalSelectorOpen && externalTransactionId != null) void loadExternalCommandList(externalTransactionId);
    else setExternalCommandList([]);
  }, [externalSelectorOpen, externalTransactionId, loadExternalCommandList]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (addExistingDropdownRef.current && !addExistingDropdownRef.current.contains(e.target as Node)) {
        setAddExistingDropdownOpen(false);
      }
    };
    if (addExistingDropdownOpen) document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, [addExistingDropdownOpen]);

  if (!show) return null;

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Child Commands Switch Condition Field</label>
        <select
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
          value={action?.ActionAttribute?.ChildCommandsSwitchConditionFieldId ?? ''}
          onChange={(e) => {
            action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
            action.ActionAttribute.ChildCommandsSwitchConditionFieldId = e.target.value ? Number(e.target.value) : null;
            onMarkChange();
          }}
        >
          <option value="">(None)</option>
          {(rootLevelSwitchConditionTransFieldLookUpList || []).map((f: any) => (
            <option key={String(f.Id)} value={String(f.Id)}>
              {f.ShortDisplay ?? f.Display}
            </option>
          ))}
        </select>
      </div>

      <div className="flex items-center gap-2 flex-wrap mt-2 mb-1">
        <div className={`text-xs font-semibold ${theme.title} shrink-0`}>Child Commands</div>
        <div className="flex items-center gap-2 flex-wrap">
          <div className="relative inline-block" ref={addExistingDropdownRef}>
            <button
              type="button"
              onClick={() => setAddExistingDropdownOpen((v) => !v)}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            >
              <i className="fa-solid fa-plus mr-1" aria-hidden /> Add Existing Command <span className="caret" />
            </button>
            {addExistingDropdownOpen && (
              <ul
                className={`absolute left-0 top-full mt-1 py-1 min-w-[220px] rounded shadow z-50 ${theme.mainContentSection} border border-gray-200`}
              >
                <li>
                  <button
                    type="button"
                    className="w-full text-left px-3 py-1.5 text-xs hover:bg-gray-100"
                    onClick={openInternalSelector}
                  >
                    From Current Data Model
                  </button>
                </li>
                <li>
                  <button
                    type="button"
                    className="w-full text-left px-3 py-1.5 text-xs hover:bg-gray-100"
                    onClick={openExternalSelector}
                  >
                    From External Data Model
                  </button>
                </li>
              </ul>
            )}
          </div>
          <button type="button" onClick={() => void createNewChildAction()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-plus mr-1" aria-hidden /> Create New
          </button>
          <button type="button" onClick={removeSelectedChild} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-minus mr-1" aria-hidden /> Remove
          </button>
        </div>
      </div>

      <div className="w-full h-[260px] mt-2">
        <FlexGrid
          ref={gridRef}
          itemsSource={childActionCV}
          selectionMode="ListBox"
          headersVisibility="Column"
          className="w-full h-full"
          cellEditEnded={() => onMarkChange()}
        >
          <FlexGridFilter />
          <FlexGridColumn header="" width={50} isReadOnly>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(cell: any) => {
                const item = cell.item;
                if (!item?.CommandId) return null;
                return (
                  <div className="w-full text-center">
                    <button
                      type="button"
                      className={`px-1.5 py-0.5 text-xs rounded-[4px] ${theme.button_default}`}
                      title="Edit child command"
                      onClick={() => openChildCommandEditor(item)}
                    >
                      <i className="fa-solid fa-pencil" aria-hidden />
                    </button>
                  </div>
                );
              }}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="Sort" header="Sort" width={80} />
          <FlexGridColumn binding="CommandId" header="Id" width={120} dataMap={childActionDataMap ?? undefined} visible={false} />
          <FlexGridColumn binding="CommandDisplay" header="Child Task Name" width={360} isReadOnly />
          <FlexGridColumn header="Data Model" width={160} isReadOnly>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(cell: any) => <span>{getCommandDataModelDisplay(cell.item?.ExternalTransactionId)}</span>}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="IsBatchCommand" header="Is Batch Command" width={150} isReadOnly dataType="Boolean" />
          {action?.ActionAttribute?.ChildCommandsSwitchConditionFieldId ? (
            <FlexGridColumn
              binding="PredictValue"
              header="Child Task Switch Condition"
              width={200}
              dataMap={childActionConditionDataMap ?? undefined}
              dataType="Number"
              isRequired={false}
            />
          ) : null}
          <FlexGridColumn
            binding="ChangeTriggerRootLevelFieldId"
            header="Root Level Change Trigger Field"
            width={200}
            dataMap={rootLevelTransFieldDataMap ?? undefined}
          />
          <FlexGridColumn
            binding="ChangeTriggerChildGridUnitId"
            header="Change Trigger Grid"
            width={160}
            dataMap={childUnitDataMap ?? undefined}
          />
          <FlexGridColumn binding="IsSkip" header="Is Skip" width={90} dataType="Boolean" />
          <FlexGridColumn binding="IsGoToNextCommandWithError" header="Go To Next Task With Error" width={230} dataType="Boolean" />
          <FlexGridColumn header="" binding="" width="*" isReadOnly />
        </FlexGrid>
      </div>

      {internalSelectorOpen && (
        <PopupModalOverlay backdropClassName="bg-black/30" onBackdropClick={() => setInternalSelectorOpen(false)}>
          <div
            className={`rounded shadow-lg p-4 max-w-2xl w-full max-h-[80vh] flex flex-col ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-2">
              <span className={`text-md font-semibold ${theme.title}`}>Available Child Commands</span>
              <button type="button" onClick={() => setInternalSelectorOpen(false)} className="text-lg leading-none">
                &times;
              </button>
            </div>
            <div className="flex-1 min-h-0 overflow-auto border border-gray-200 rounded mb-3">
              {availableChildActions.length === 0 ? (
                <p className={`p-3 text-sm ${theme.label}`}>No other commands in this data model.</p>
              ) : (
                <ul className="py-1">
                  {availableChildActions.map((cmd: any) => (
                    <li key={cmd.Id}>
                      <button
                        type="button"
                        className="w-full text-left px-3 py-2 text-sm hover:bg-gray-100 border-b border-gray-100 last:border-0 overflow-hidden text-ellipsis whitespace-nowrap"
                        onClick={() => applyInternalCommand(cmd)}
                      >
                        {cmd.displayName || cmd.Name}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <div className="flex justify-end">
              <button type="button" onClick={() => setInternalSelectorOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
            </div>
          </div>
        </PopupModalOverlay>
      )}

      {externalSelectorOpen && (
        <PopupModalOverlay backdropClassName="bg-black/30" onBackdropClick={() => setExternalSelectorOpen(false)}>
          <div
            className={`rounded shadow-lg p-4 max-w-md w-full ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-3">
              <span className={`text-md font-semibold ${theme.title}`}>Select Command From External Data Model</span>
              <button type="button" onClick={() => setExternalSelectorOpen(false)} className="text-lg leading-none">
                &times;
              </button>
            </div>
            <div className="space-y-3 mb-4">
              <div className="flex items-center gap-2">
                <label className={`w-28 text-xs ${theme.label} shrink-0`}>Data Model</label>
                <select
                  value={externalTransactionId ?? ''}
                  onChange={(e) => setExternalTransactionId(e.target.value ? Number(e.target.value) : null)}
                  className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                >
                  <option value="">-- Select --</option>
                  {transactionList.map((t: any) => (
                    <option key={t.Id} value={t.Id}>
                      {t.TransactionName ?? t.Name ?? t.Id}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-2">
                <label className={`w-28 text-xs ${theme.label} shrink-0`}>Command</label>
                <select
                  value={externalCommandId ?? ''}
                  onChange={(e) => setExternalCommandId(e.target.value ? Number(e.target.value) : null)}
                  className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  disabled={!externalTransactionId}
                >
                  <option value="">-- Select --</option>
                  {externalCommandList.map((c: any) => (
                    <option key={c.Id} value={c.Id}>
                      {c.Name ?? c.Id}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button type="button" onClick={() => setExternalSelectorOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
              <button
                type="button"
                onClick={applyExternalCommand}
                disabled={!externalTransactionId || !externalCommandId}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50`}
              >
                Apply
              </button>
            </div>
          </div>
        </PopupModalOverlay>
      )}

      {childEditorCommandId != null ? (
        <ChildCommandEditorPopup
          commandId={childEditorCommandId}
          hierarchy={hierarchy}
          applicationId={applicationId}
          transactionId={transactionId}
          operationTypeOptions={operationTypeOptions}
          rootLevelTransFieldLookUpList={rootLevelTransFieldLookUpList}
          rootLevelConditionTransFieldLookUpList={rootLevelConditionTransFieldLookUpList}
          rootLevelSwitchConditionTransFieldLookUpList={rootLevelSwitchConditionTransFieldLookUpList}
          transactionFieldLookUpList={transactionFieldLookUpList}
          rootLevelAllFieldLookUpList={rootLevelAllFieldLookUpList}
          onClose={() => setChildEditorCommandId(null)}
          onApplied={(updatedCommand) => {
            const row = (action?.ActionAttribute?.ChildActionList || []).find(
              (c: any) => Number(c?.CommandId) === childEditorCommandId,
            );
            handleChildCommandApplied(updatedCommand, row);
            setChildEditorCommandId(null);
          }}
        />
      ) : null}

      {externalFormBuilderOpen && externalFormBuilderParams && (
        <ApplicationFormBuilder
          isOpen={externalFormBuilderOpen}
          onClose={() => {
            setExternalFormBuilderOpen(false);
            setExternalFormBuilderParams(null);
          }}
          applicationId={externalFormBuilderParams.applicationId}
          defaultSectionCode="TransactionCommandActionSetting"
          isCreateNewItem={false}
          transactionId={externalFormBuilderParams.transactionId}
          transactionType={externalFormBuilderParams.transactionType}
          modelName={externalFormBuilderParams.modelName}
          initialSelectedCommandId={externalFormBuilderParams.initialSelectedCommandId}
        />
      )}
    </>
  );
}
