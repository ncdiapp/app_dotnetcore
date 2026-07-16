import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useDispatch } from 'react-redux';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../../../webapi/apptransactionsvc';
import {
  EmAppTransactionCommandTypeSendMessageToTransFieldEmailAddress,
  EmAppTransactionCommandTypeSendMessageToTransFieldPartnerId,
} from './SendMessageSections';
import { ChildCommandEditorPopup } from './ChildCommandEditorPopup';
import { buildCommandDisplayName, resolveIsBatchCommand } from './childCommandGridHelpers';
import appHelper from '../../../../helper/appHelper';

type OperationTypeOption = { Id: any; Display: string };

export function MessageLinkCommandsSection(props: {
  action: any;
  hierarchy: any;
  transactionId: number | null;
  applicationId: string | null;
  transactionName?: string | null;
  rootLevelTransFieldLookUpList: any[];
  rootLevelConditionTransFieldLookUpList: any[];
  rootLevelSwitchConditionTransFieldLookUpList: any[];
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
    transactionId,
    applicationId,
    transactionName,
    rootLevelTransFieldLookUpList,
    rootLevelConditionTransFieldLookUpList,
    rootLevelSwitchConditionTransFieldLookUpList,
    transactionFieldLookUpList,
    rootLevelAllFieldLookUpList,
    operationTypeOptions,
    onMarkChange,
    onCommandAdded,
  } = props;

  const actionType = Number(action?.ActionType);
  const show =
    !!action &&
    (actionType === EmAppTransactionCommandTypeSendMessageToTransFieldEmailAddress ||
      actionType === EmAppTransactionCommandTypeSendMessageToTransFieldPartnerId);

  const [childActionCV] = useState(() => new CollectionView<any>([]));
  const [childActionDataMap, setChildActionDataMap] = useState<DataMap | null>(null);
  const [rootLevelTransFieldDataMap, setRootLevelTransFieldDataMap] = useState<DataMap | null>(null);
  const [childUnitDataMap, setChildUnitDataMap] = useState<DataMap | null>(null);
  const gridRef = useRef<any>(null);
  const [childEditorCommandId, setChildEditorCommandId] = useState<number | null>(null);

  useEffect(() => {
    if (!show) return;
    action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
    action.ActionAttribute.ChildActionList = action.ActionAttribute.ChildActionList || [];
    childActionCV.sourceCollection = action.ActionAttribute.ChildActionList;
    childActionCV.sortDescriptions.clear();
    childActionCV.sortDescriptions.push(new SortDescription('Sort', true));
    childActionCV.refresh();

    const list = (hierarchy?.CommandActionList || []).filter((c: any) => Number(c?.Id) !== Number(action?.Id));
    const normalized = list
      .map((c: any) => {
        const displayName = buildCommandDisplayName(c);
        return { ...c, displayName, Display: displayName };
      })
      .sort((a: any, b: any) => String(a.displayName).localeCompare(String(b.displayName)));
    setChildActionDataMap(new DataMap(normalized, 'Id', 'displayName'));
    setRootLevelTransFieldDataMap(new DataMap(rootLevelTransFieldLookUpList || [], 'Id', 'Display'));

    const childUnits: any[] = [];
    (hierarchy?.AppTransactionUnitList || []).forEach((u: any) => {
      (u.Children || []).forEach((cu: any) => {
        childUnits.push({ Id: cu.Id, ShortDisplay: `${cu.DataBaseTableName || cu.UnitDisplayName}(${cu.Id})` });
      });
    });
    setChildUnitDataMap(new DataMap(childUnits, 'Id', 'ShortDisplay'));
  }, [action, childActionCV, hierarchy, rootLevelTransFieldLookUpList, show]);

  const syncChildRowFromCommand = useCallback(
    (row: any, commandId: number | null) => {
      if (!commandId) {
        row.CommandDisplay = '';
        row.IsBatchCommand = false;
        return;
      }
      const cmd = (hierarchy?.CommandActionList || []).find((c: any) => Number(c?.Id) === Number(commandId));
      if (cmd) {
        row.CommandDisplay = buildCommandDisplayName(cmd);
        row.IsBatchCommand = resolveIsBatchCommand(cmd, hierarchy);
      }
    },
    [hierarchy],
  );

  const addChildAction = useCallback(
    (commandDto?: any) => {
      if (!show) return null;
      action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
      action.ActionAttribute.ChildActionList = action.ActionAttribute.ChildActionList || [];
      const maxSort = Math.max(0, ...action.ActionAttribute.ChildActionList.map((x: any) => Number(x.Sort) || 0));
      const child: any = { Sort: maxSort + 1, CommandId: null, CommandDisplay: '', IsBatchCommand: false };
      if (commandDto) {
        child.CommandId = commandDto.Id ?? null;
        child.CommandDisplay = buildCommandDisplayName(commandDto);
        child.IsBatchCommand = resolveIsBatchCommand(commandDto, hierarchy);
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
      ActionGuid: appHelper.guid(),
      ActionFlowOrder: maxOrder + 1,
      Name: '_ChildCommand' + (maxOrder + 1),
      NextTransactionId: null,
      ActionType: 42,
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

  const onGridCellEditEnded = useCallback(() => {
    const flex = gridRef.current?.control ?? gridRef.current;
    const rowIndex = flex?.selection?.row;
    if (typeof rowIndex === 'number' && rowIndex >= 0) {
      const row = flex?.rows?.[rowIndex]?.dataItem;
      if (row) syncChildRowFromCommand(row, row.CommandId != null ? Number(row.CommandId) : null);
    }
    onMarkChange();
  }, [onMarkChange, syncChildRowFromCommand]);

  const openChildCommandEditor = useCallback((row: any) => {
    if (!row?.CommandId) return;
    setChildEditorCommandId(Number(row.CommandId));
  }, []);

  if (!show) return null;

  const dataModelLabel = transactionName ?? hierarchy?.TransactionName ?? hierarchy?.Name ?? '';

  return (
    <>
      <div className="flex items-center gap-2 flex-wrap mt-2 mb-1">
        <div className={`text-xs font-semibold ${theme.title} shrink-0`}>Message Link Commands</div>
        <div className="flex items-center gap-2 flex-wrap">
          <button type="button" onClick={() => addChildAction()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-plus mr-1" aria-hidden /> Add Existing
          </button>
          <button type="button" onClick={() => void createNewChildAction()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-plus mr-1" aria-hidden /> Create New
          </button>
          <button type="button" onClick={removeSelectedChild} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-minus mr-1" aria-hidden /> Remove
          </button>
        </div>
      </div>

      <div className="w-full h-[200px]">
        <FlexGrid
          ref={gridRef}
          itemsSource={childActionCV}
          selectionMode="ListBox"
          headersVisibility="Column"
          className="w-full h-full"
          cellEditEnded={onGridCellEditEnded}
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
          <FlexGridColumn binding="Sort" header="Sort" width={70} />
          <FlexGridColumn
            binding="CommandId"
            header="Child Task Name"
            width={320}
            dataMap={childActionDataMap ?? undefined}
            isRequired={false}
          />
          <FlexGridColumn header="Data Model" width={160} isReadOnly>
            <FlexGridCellTemplate cellType="Cell" template={() => <span>{dataModelLabel}</span>} />
          </FlexGridColumn>
          <FlexGridColumn binding="IsBatchCommand" header="Is Batch Command" width={140} isReadOnly dataType="Boolean" />
          <FlexGridColumn
            binding="ChangeTriggerRootLevelFieldId"
            header="Root Level Change Trigger Field"
            width={200}
            dataMap={rootLevelTransFieldDataMap ?? undefined}
            isRequired={false}
          />
          <FlexGridColumn
            binding="ChangeTriggerChildGridUnitId"
            header="Change Trigger Grid"
            width={160}
            dataMap={childUnitDataMap ?? undefined}
            isRequired={false}
          />
          <FlexGridColumn binding="IsSkip" header="Is Skip" width={90} dataType="Boolean" isRequired={false} />
          <FlexGridColumn binding="IsGoToNextCommandWithError" header="Go To Next Task With Error" width={200} dataType="Boolean" isRequired={false} />
          <FlexGridColumn binding="" header="" width="*" isReadOnly />
        </FlexGrid>
      </div>

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
            if (row) {
              row.CommandDisplay = buildCommandDisplayName(updatedCommand);
              row.IsBatchCommand = resolveIsBatchCommand(updatedCommand, hierarchy);
              childActionCV.refresh();
            }
            onMarkChange();
            setChildEditorCommandId(null);
          }}
        />
      ) : null}
    </>
  );
}
