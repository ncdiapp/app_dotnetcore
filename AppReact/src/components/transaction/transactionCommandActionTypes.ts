/**
 * Operation type Display strings — copied exactly from AngularJS:
 * - Master-detail & list-edit: PlmApplication/Scripts1x/mgtCtrl/Transaction/transactionCommandActionEditorCtrl.js
 * - Workflow automation: PlmApplication/Scripts1x/mgtCtrl/Transaction/workflowAutomationEditorCtrl.js
 *
 * Angular sorts the CollectionView by Display ascending (SortDescription('Display', true)); use sortCommandActionTypesByDisplayLikeAngular() for dropdowns.
 */

export type TransactionCommandActionTypeOption = { Id: number; Display: string };

/** transactionCommandActionEditorCtrl.js — `masterDetailCommandActionTypeList` (Master–Detail transactions) */
export const ANGULAR_MASTER_DETAIL_COMMAND_ACTION_TYPE_LIST: TransactionCommandActionTypeOption[] = [
  { Id: 40, Display: 'Call Plugin Web Api' },
  { Id: 42, Display: 'Execute SQL Statement' },
  { Id: 43, Display: 'Execute Data Transfer' },
  { Id: 205, Display: 'Validate Required Fields' },
  { Id: 44, Display: 'Exec DataModel All Formula' },
  { Id: 45, Display: 'Execute All DataLoad' },
  { Id: 46, Display: 'Generate Matrix' },
  { Id: 49, Display: 'Save' },
  { Id: 50, Display: 'Refresh' },
  { Id: 57, Display: 'Execute Commnad Formula' },
  { Id: 59, Display: 'Call API Operation' },
  { Id: 60, Display: 'Send Message: To Form Field UserId' },
  { Id: 61, Display: 'Send Message: To Form Field Email Address' },
  { Id: 62, Display: 'Send Message: To Form Field Partner' },
  { Id: 63, Display: 'Send SMS: To Form Field Phone Number' },
  { Id: 66, Display: 'Import To Database Table From Json' },
  { Id: 67, Display: 'Import To Database Table From Excel' },
  { Id: 76, Display: 'Import To Database Tables From Multiple Json Files (in a given folder and subfolders)' },
  { Id: 77, Display: 'Import To Database Tables From Multiple Excel Files (in a given folder and subfolders)' },
  { Id: 68, Display: 'Execute External Exe Process' },
  { Id: 70, Display: 'Create User' },
  { Id: 71, Display: 'Import To Database Table From Rest-Api Import Setting' },
  { Id: 72, Display: 'Import To Database Table From DB-To-DB Import Setting' },
  { Id: 73, Display: 'Download File To Server Folder' },
  { Id: 81, Display: 'Convert From Xml To Json' },
  { Id: 82, Display: 'Convert Back From Json To Xml' },
  { Id: 200, Display: 'Composition Command' },
  { Id: 47, Display: 'UI: Save As' },
  { Id: 48, Display: 'UI: Print' },
  { Id: 74, Display: 'UI: Print From Message Template' },
  { Id: 51, Display: 'UI: Open Form Creation Window' },
  { Id: 53, Display: 'UI: Navigate To Search' },
  { Id: 54, Display: 'UI: Close Form Window' },
  { Id: 55, Display: 'UI: Open Form Edit Window' },
];

/** transactionCommandActionEditorCtrl.js — `listEditCommandActionTypeList` (List Edit transactions, EmTransactionOrganizedType.List) */
export const ANGULAR_LIST_EDIT_COMMAND_ACTION_TYPE_LIST: TransactionCommandActionTypeOption[] = [
  { Id: 43, Display: 'Execute Data Transfer' },
  { Id: 49, Display: 'Save' },
  { Id: 50, Display: 'Refresh' },
  { Id: 200, Display: 'Composition Command' },
];

/** workflowAutomationEditorCtrl.js — `masterDetailCommandActionTypeList` (workflow automation editor) */
export const ANGULAR_WORKFLOW_AUTOMATION_COMMAND_ACTION_TYPE_LIST: TransactionCommandActionTypeOption[] = [
  { Id: 42, Display: 'Execute SQL Statement' },
  { Id: 49, Display: 'Save' },
  { Id: 50, Display: 'Refresh' },
  { Id: 57, Display: 'Execute Commnad Formula' },
  { Id: 59, Display: 'Call API Operation' },
  { Id: 60, Display: 'Send Message: To Form Field UserId' },
  { Id: 61, Display: 'Send Message: To Form Field Email Address' },
  { Id: 62, Display: 'Send Message: To Form Field Partner' },
  { Id: 63, Display: 'Send SMS: To Form Field Phone Number' },
  { Id: 66, Display: 'Import To Database Table From Json' },
  { Id: 67, Display: 'Import To Database Table From Excel' },
  { Id: 76, Display: 'Import To Database Tables From Multiple Json Files (in a given folder and subfolders)' },
  { Id: 77, Display: 'Import To Database Tables From Multiple Excel Files (in a given folder and subfolders)' },
  { Id: 68, Display: 'Execute External Exe Process' },
  { Id: 71, Display: 'Import To Database Table From Rest-Api Import Setting' },
  { Id: 72, Display: 'Import To Database Table From DB-To-DB Import Setting' },
  { Id: 73, Display: 'Download File To Server Folder' },
  { Id: 81, Display: 'Convert From Xml To Json' },
  { Id: 82, Display: 'Convert Back From Json To Xml' },
  { Id: 200, Display: 'Composition Command' },
];

/** Merged Id → Display for grid/type column (master-detail list is superset for Application Builder). */
const DISPLAY_BY_ACTION_TYPE_ID: Map<number, string> = (() => {
  const m = new Map<number, string>();
  for (const row of ANGULAR_MASTER_DETAIL_COMMAND_ACTION_TYPE_LIST) {
    m.set(row.Id, row.Display);
  }
  for (const row of ANGULAR_WORKFLOW_AUTOMATION_COMMAND_ACTION_TYPE_LIST) {
    if (!m.has(row.Id)) m.set(row.Id, row.Display);
  }
  for (const row of ANGULAR_LIST_EDIT_COMMAND_ACTION_TYPE_LIST) {
    if (!m.has(row.Id)) m.set(row.Id, row.Display);
  }
  return m;
})();

/** Matches Angular CollectionView SortDescription('Display', true). */
export function sortCommandActionTypesByDisplayLikeAngular(
  list: readonly TransactionCommandActionTypeOption[]
): TransactionCommandActionTypeOption[] {
  return [...list].sort((a, b) => a.Display.localeCompare(b.Display));
}

export function getTransactionCommandActionTypeDisplay(actionType: number | null | undefined): string {
  if (actionType == null) return '';
  return DISPLAY_BY_ACTION_TYPE_ID.get(actionType) ?? String(actionType);
}

/** Application Form Builder Operation Task: dropdown source (Angular parity). */
export function getApplicationFormBuilderCommandActionTypeOptions(
  transactionOrganizedType: number | null | undefined
): TransactionCommandActionTypeOption[] {
  const list =
    transactionOrganizedType === EmTransactionOrganizedType.List
      ? ANGULAR_LIST_EDIT_COMMAND_ACTION_TYPE_LIST
      : ANGULAR_MASTER_DETAIL_COMMAND_ACTION_TYPE_LIST;
  return sortCommandActionTypesByDisplayLikeAngular(list);
}

/** EmTransactionOrganizedType — match Angular EnumApp.EmTransactionOrganizedType */
export const EmTransactionOrganizedType = {
  MasterDetail: 1,
  List: 3,
} as const;

/** Pre-sorted workflow automation dropdown options (Angular SortDescription 'Display'). */
export const WORKFLOW_AUTOMATION_COMMAND_ACTION_TYPE_OPTIONS_SORTED = sortCommandActionTypesByDisplayLikeAngular(
  ANGULAR_WORKFLOW_AUTOMATION_COMMAND_ACTION_TYPE_LIST
);

/**
 * transactionCommandSingleEditorPopupCtrl when directivOutercontrol.rootWorkflowTransactionData (isCallFromWorkflow).
 * Embedded command editor inside Workflow Automation Editor right panel.
 */
export const ANGULAR_WORKFLOW_EMBEDDED_COMMAND_ACTION_TYPE_LIST: TransactionCommandActionTypeOption[] = [
  { Id: 40, Display: 'Call Plugin Web Api' },
  { Id: 42, Display: 'Execute SQL Statement' },
  { Id: 43, Display: 'Execute Data Transfer' },
  { Id: 205, Display: 'Validate Required Fields' },
  { Id: 44, Display: 'Exec DataModel All Formula' },
  { Id: 45, Display: 'Execute All DataLoad' },
  { Id: 46, Display: 'Generate Matrix' },
  { Id: 49, Display: 'Save' },
  { Id: 50, Display: 'Refresh' },
  { Id: 57, Display: 'Execute Commnad Formula' },
  { Id: 59, Display: 'Call API Operation' },
  { Id: 60, Display: 'Send Message: To Form Field UserId' },
  { Id: 61, Display: 'Send Message: To Form Field Email Address' },
  { Id: 62, Display: 'Send Message: To Form Field Partner' },
  { Id: 63, Display: 'Send SMS: To Form Field Phone Number' },
  { Id: 66, Display: 'Import To Database Table From Json' },
  { Id: 67, Display: 'Import To Database Table From Excel' },
  { Id: 76, Display: 'Import To Database Tables From Multiple Json Files (in a given folder and subfolders)' },
  { Id: 77, Display: 'Import To Database Tables From Multiple Excel Files (in a given folder and subfolders)' },
  { Id: 68, Display: 'Execute External Exe Process' },
  { Id: 70, Display: 'Create User' },
  { Id: 71, Display: 'Import To Database Table From Rest-Api Import Setting' },
  { Id: 72, Display: 'Import To Database Table From DB-To-DB Import Setting' },
  { Id: 73, Display: 'Download File To Server Folder' },
  { Id: 81, Display: 'Convert From Xml To Json' },
  { Id: 82, Display: 'Convert Back From Json To Xml' },
  { Id: 200, Display: 'Composition Command' },
];

export function getWorkflowEmbeddedCommandActionTypeOptions(): TransactionCommandActionTypeOption[] {
  return sortCommandActionTypesByDisplayLikeAngular(ANGULAR_WORKFLOW_EMBEDDED_COMMAND_ACTION_TYPE_LIST);
}
