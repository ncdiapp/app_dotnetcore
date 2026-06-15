/**
 * Create Windows Scheduler Task – migrated from WorkflowAutomationEditor.cshtml popup
 * + workflowAutomationEditorCtrl openWindowsSchedulerTaskCreatorPopup / createWorkflowWindowsSchedulerTask.
 */

import React from 'react';
import { createPortal } from 'react-dom';

export const WORKFLOW_SCHEDULE_TYPE_OPTIONS = [
  'MINUTE',
  'HOURLY',
  'DAILY',
  'WEEKLY',
  'MONTHLY',
  'ONCE',
] as const;

export const WORKFLOW_WEEKDAY_OPTIONS = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'] as const;

export const WORKFLOW_MONTH_DAY_OPTIONS = Array.from({ length: 31 }, (_, i) => String(i + 1));

export type WorkflowSchedulerTaskDto = {
  TransactionId: number;
  TaskName: string;
  StartTime: string;
  ScheduleType: string;
  RepeatEvery: string | number;
  Days?: string;
};

function repeatEveryUnitLabel(scheduleType: string): string {
  switch (scheduleType) {
    case 'MINUTE':
      return 'Minute(s)';
    case 'HOURLY':
      return 'Hour(s)';
    case 'DAILY':
      return 'Day(s)';
    case 'WEEKLY':
      return 'Week(s)';
    case 'MONTHLY':
      return 'Month(s)';
    default:
      return '';
  }
}

type ThemeTokens = {
  mainContentSection: string;
  title: string;
  label: string;
  inputBox: string;
  button_default: string;
};

export type WorkflowWindowsSchedulerTaskPopupProps = {
  isOpen: boolean;
  task: WorkflowSchedulerTaskDto | null;
  theme: ThemeTokens;
  borderClass: string;
  onClose: () => void;
  onChange: (patch: Partial<WorkflowSchedulerTaskDto>) => void;
  onSave: () => void;
};

export default function WorkflowWindowsSchedulerTaskPopup({
  isOpen,
  task,
  theme,
  borderClass,
  onClose,
  onChange,
  onSave,
}: WorkflowWindowsSchedulerTaskPopupProps) {
  if (!isOpen || !task || typeof document === 'undefined') return null;

  const unitLabel = repeatEveryUnitLabel(task.ScheduleType);
  const showRepeatEvery = !!task.ScheduleType;
  const showWeekday = task.ScheduleType === 'WEEKLY';
  const showMonthDay = task.ScheduleType === 'MONTHLY';

  return createPortal(
    <div
      className="fixed inset-0 flex items-center justify-center bg-black/40 px-4"
      style={{ zIndex: 10050 }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="workflow-windows-scheduler-title"
      onClick={onClose}
    >
      <div
        className={`flex w-full max-w-[420px] flex-col overflow-hidden rounded-md border shadow-2xl ${theme.mainContentSection} ${borderClass}`}
        onClick={(e) => e.stopPropagation()}
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div
          className={`flex shrink-0 items-center justify-between border-b px-3 py-2.5 ${theme.mainContentSection} ${borderClass}`}
        >
          <span id="workflow-windows-scheduler-title" className={`text-sm font-semibold ${theme.title}`}>
            Create Windows Scheduler Task
          </span>
          <button
            type="button"
            className={`shrink-0 rounded-[4px] p-1.5 ${theme.button_default}`}
            onClick={onClose}
            aria-label="Close"
          >
            <i className="fa-solid fa-xmark" aria-hidden />
          </button>
        </div>

        <div className="flex flex-col gap-3 px-3 py-3">
          <div className="min-w-0">
            <label className={`mb-1 block text-xs ${theme.label}`}>Task Name</label>
            <input
              type="text"
              autoComplete="off"
              value={task.TaskName}
              onChange={(e) => onChange({ TaskName: e.target.value })}
              className={`h-7 w-full px-2 text-xs border box-border ${theme.inputBox}`}
            />
          </div>

          <div className="min-w-0">
            <label className={`mb-1 block text-xs ${theme.label}`}>Repeat Type</label>
            <select
              value={task.ScheduleType}
              onChange={(e) => {
                const scheduleType = e.target.value;
                const patch: Partial<WorkflowSchedulerTaskDto> = { ScheduleType: scheduleType };
                if (scheduleType === 'WEEKLY' && !task.Days) patch.Days = 'MON';
                if (scheduleType === 'MONTHLY' && !task.Days) patch.Days = '1';
                onChange(patch);
              }}
              className={`h-7 w-full px-2 text-xs border box-border ${theme.inputBox}`}
            >
              {WORKFLOW_SCHEDULE_TYPE_OPTIONS.map((opt) => (
                <option key={opt} value={opt}>
                  {opt}
                </option>
              ))}
            </select>
          </div>

          {showRepeatEvery ? (
            <div className="min-w-0">
              <label className={`mb-1 block text-xs ${theme.label}`}>Repeat Every</label>
              <div className="flex items-center gap-2">
                <input
                  type="text"
                  autoComplete="off"
                  value={task.RepeatEvery}
                  onChange={(e) => onChange({ RepeatEvery: e.target.value })}
                  className={`h-7 w-1 min-w-0 flex-auto px-2 text-xs border box-border ${theme.inputBox}`}
                />
                {unitLabel ? <span className={`shrink-0 text-xs ${theme.label}`}>{unitLabel}</span> : null}
              </div>
            </div>
          ) : null}

          {showWeekday ? (
            <div className="min-w-0">
              <label className={`mb-1 block text-xs ${theme.label}`}>On Weekday</label>
              <select
                value={task.Days ?? 'MON'}
                onChange={(e) => onChange({ Days: e.target.value })}
                className={`h-7 w-full px-2 text-xs border box-border ${theme.inputBox}`}
              >
                {WORKFLOW_WEEKDAY_OPTIONS.map((d) => (
                  <option key={d} value={d}>
                    {d}
                  </option>
                ))}
              </select>
            </div>
          ) : null}

          {showMonthDay ? (
            <div className="min-w-0">
              <label className={`mb-1 block text-xs ${theme.label}`}>On Date of Month</label>
              <select
                value={task.Days ?? '1'}
                onChange={(e) => onChange({ Days: e.target.value })}
                className={`h-7 w-full px-2 text-xs border box-border ${theme.inputBox}`}
              >
                {WORKFLOW_MONTH_DAY_OPTIONS.map((d) => (
                  <option key={d} value={d}>
                    {d}
                  </option>
                ))}
              </select>
            </div>
          ) : null}

          <div className="min-w-0">
            <label className={`mb-1 block text-xs ${theme.label}`}>Start Time (example: 09:00)</label>
            <input
              type="text"
              autoComplete="off"
              value={task.StartTime}
              onChange={(e) => onChange({ StartTime: e.target.value })}
              className={`h-7 w-full px-2 text-xs border box-border ${theme.inputBox}`}
            />
          </div>
        </div>

        <div
          className={`flex shrink-0 items-center justify-end gap-2 border-t px-3 py-2 ${theme.mainContentSection} ${borderClass}`}
        >
          <button
            type="button"
            className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
            onClick={onClose}
          >
            Cancel
          </button>
          <button
            type="button"
            className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
            onClick={onSave}
          >
            Save
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
