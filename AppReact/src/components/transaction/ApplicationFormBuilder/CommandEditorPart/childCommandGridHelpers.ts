/** Shared helpers for composition / message-link child command grids. */

export const SYS_BUILTIN_ACTION_TYPES = new Set([44, 46, 47, 48, 49, 50, 54]);

export function buildCommandDisplayName(command: any): string {
  const typeString = SYS_BUILTIN_ACTION_TYPES.has(Number(command?.ActionType)) ? 'Sys Built-in' : 'User Defined';
  return `${typeString}: ${command?.Name ?? ''}${command?.Description ? `: ${command.Description}` : ''}`;
}

export function resolveIsBatchCommand(commandDto: any, hierarchy: any, externalTransactionId?: any): boolean {
  if (!commandDto?.Id) return false;
  if (externalTransactionId) {
    return !!commandDto?.ActionAttribute?.IsBatchCommand;
  }
  const cmd = (hierarchy?.CommandActionList || []).find((c: any) => Number(c?.Id) === Number(commandDto.Id));
  return !!cmd?.ActionAttribute?.IsBatchCommand;
}
