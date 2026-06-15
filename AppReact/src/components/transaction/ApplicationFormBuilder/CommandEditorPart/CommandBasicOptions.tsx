import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import type { CommandEditorHostContext } from '../commandEditorContext';
import { isWorkflowAutomationEditorContext } from '../commandEditorContext';

type OperationTypeOption = { Id: any; Display: string };

export function CommandBasicOptions(props: {
  action: any;
  operationTypeOptions: OperationTypeOption[];
  rootLevelConditionTransFieldLookUpList: any[];
  hostContext?: CommandEditorHostContext;
  onMarkChange: () => void;
  onRefreshGridCell: () => void;
  onFlowOrderChanged: () => void;
}) {
  const { theme } = useTheme();
  const {
    action,
    operationTypeOptions,
    rootLevelConditionTransFieldLookUpList,
    hostContext = 'applicationFormBuilder',
    onMarkChange,
    onRefreshGridCell,
    onFlowOrderChanged,
  } = props;
  const hideFlowOrder = isWorkflowAutomationEditorContext(hostContext);

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Operation Task Name</label>
        <input
          type="text"
          autoComplete="off"
          value={action.Name ?? ''}
          onChange={(e) => {
            action.Name = e.target.value;
            onMarkChange();
            onRefreshGridCell();
          }}
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
        />
      </div>

      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Operation Type</label>
        <select
          value={action.ActionType ?? 42}
          onChange={(e) => {
            action.ActionType = Number(e.target.value);
            onMarkChange();
            onRefreshGridCell();
          }}
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
        >
          {operationTypeOptions.map((opt) => (
            <option key={String(opt.Id)} value={opt.Id}>
              {opt.Display}
            </option>
          ))}
        </select>
      </div>

      {!hideFlowOrder ? (
        <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>Flow Order</label>
          <div className="w-72">
            <input
              type="number"
              value={action.ActionFlowOrder ?? ''}
              onChange={(e) => {
                action.ActionFlowOrder = e.target.value === '' ? null : Number(e.target.value);
                onMarkChange();
                onFlowOrderChanged();
              }}
              className={`w-24 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            />
          </div>
        </div>
      ) : null}

      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Execute Condition Field</label>
        <select
          value={action.CommandConditionTransactionFieldId ?? ''}
          onChange={(e) => {
            action.CommandConditionTransactionFieldId = e.target.value ? Number(e.target.value) : null;
            onMarkChange();
          }}
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
        >
          <option value="">No Condition (The task will execute)</option>
          {rootLevelConditionTransFieldLookUpList.map((f: any) => (
            <option key={String(f.Id)} value={f.Id}>
              {f.ShortDisplay ?? f.Display}
            </option>
          ))}
        </select>
      </div>

      <div className="mt-2 border-t pt-3" />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-x-10 gap-y-1 py-1">
        <label className="flex items-center gap-2 text-xs">
          <input
            type="checkbox"
            checked={!!action.ActionAttribute?.LinkToUI}
            onChange={(e) => {
              action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
              action.ActionAttribute.LinkToUI = e.target.checked;
              onMarkChange();
              onRefreshGridCell();
            }}
          />
          <span className={theme.label}>Is Public Task (Link To UI)</span>
        </label>

        {!!action.ActionAttribute?.LinkToUI && (
          <>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={!!action.ActionAttribute?.IsShowOnTopMenu}
                onChange={(e) => {
                  action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                  action.ActionAttribute.IsShowOnTopMenu = e.target.checked;
                  onMarkChange();
                  onRefreshGridCell();
                }}
              />
              <span className={theme.label}>Visible on Form Menu</span>
            </label>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={!!action.ActionAttribute?.IsShowOnSearchViewEventOptionMenu}
                onChange={(e) => {
                  action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                  action.ActionAttribute.IsShowOnSearchViewEventOptionMenu = e.target.checked;
                  onMarkChange();
                }}
              />
              <span className={theme.label}>Visible on Calendar/Scheduler/Gantt View Menu</span>
            </label>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={!!action.ActionAttribute?.IsAutoExecuteOnFormOpen}
                onChange={(e) => {
                  action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                  action.ActionAttribute.IsAutoExecuteOnFormOpen = e.target.checked;
                  onMarkChange();
                }}
              />
              <span className={theme.label}>Auto Execute On Form Open</span>
            </label>
          </>
        )}
      </div>
    </>
  );
}

