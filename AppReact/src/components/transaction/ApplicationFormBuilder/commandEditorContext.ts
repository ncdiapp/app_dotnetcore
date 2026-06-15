/** Where CommandEditor is hosted — controls visibility (Angular directivOutercontrol / isCallFromWorkflow). */

export type CommandEditorHostContext = 'applicationFormBuilder' | 'workflowAutomation';

export function isWorkflowAutomationEditorContext(ctx: CommandEditorHostContext): boolean {
  return ctx === 'workflowAutomation';
}
