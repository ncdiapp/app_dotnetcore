import { getTransactionCommandActionTypeDisplay } from '../transactionCommandActionTypes';

/** Prefab user message for Configurator when asking about Data Model Operations / a type. */
export function buildCommandHelpAskPrompt(actionType?: number | null): string {
  const id = actionType != null && Number.isFinite(Number(actionType)) ? Number(actionType) : null;
  if (id == null) {
    return [
      'Using AgentStarter file commands-knowledge.md (Data Model Operation guide),',
      'summarize the Operation Type taxonomy and shared Public / Batch / Log / Result settings.',
      'When creating operations, remind me of correct upsert_command actionType ints',
      '(EmAppTransactionCommandType — never Link Target 1=Edit). Do not execute any operation.',
    ].join(' ');
  }
  const display = getTransactionCommandActionTypeDisplay(id) || `ActionType ${id}`;
  return [
    `Using AgentStarter file commands-knowledge.md, explain how to configure Operation Type`,
    `"${display}" (actionType=${id}): editor fields, ActionAttribute / optionsJson, and runtime behavior.`,
    'Give a correct upsert_command sketch. Do not execute the operation.',
  ].join(' ');
}
