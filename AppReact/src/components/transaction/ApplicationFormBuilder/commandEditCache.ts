/** In-memory draft cache for operation-task commands while switching selection (not persisted). */

export type CommandEditCache = Map<number, any>;

export function cloneCommand(cmd: any): any {
  return JSON.parse(JSON.stringify(cmd));
}

/** Copy all fields from source onto target (same object identity in the command list). */
export function copyCommandInPlace(target: any, source: any): void {
  if (!target || !source) return;
  const cloned = cloneCommand(source);
  Object.keys(target).forEach((k) => {
    if (!(k in cloned)) delete target[k];
  });
  Object.assign(target, cloned);
}

export function stashCommand(cache: CommandEditCache, cmd: any): void {
  if (cmd?.Id == null) return;
  cache.set(Number(cmd.Id), cloneCommand(cmd));
}

export function restoreCommand(cache: CommandEditCache, cmd: any): boolean {
  if (cmd?.Id == null) return false;
  const cached = cache.get(Number(cmd.Id));
  if (!cached) return false;
  copyCommandInPlace(cmd, cached);
  return true;
}

export function clearCommandEditCache(cache: CommandEditCache): void {
  cache.clear();
}

export function removeCommandFromCache(cache: CommandEditCache, commandId: number | null | undefined): void {
  if (commandId == null) return;
  cache.delete(Number(commandId));
}
