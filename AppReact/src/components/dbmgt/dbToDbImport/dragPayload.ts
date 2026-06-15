export const DB2DB_DRAG_MIME = 'application/json';

export type Db2DbDragPayload =
  | { type: 'source'; names: string[] }
  | { type: 'tableColumns'; fromTableUiId: string; columnNames: string[] };

export function setDb2DbDragData(e: React.DragEvent, payload: Db2DbDragPayload): void {
  e.dataTransfer.setData(DB2DB_DRAG_MIME, JSON.stringify(payload));
  e.dataTransfer.effectAllowed = 'copy';
}

export function readDb2DbDragData(e: React.DragEvent): Db2DbDragPayload | null {
  try {
    const raw = e.dataTransfer.getData(DB2DB_DRAG_MIME);
    if (!raw) return null;
    const o = JSON.parse(raw);
    if (o?.type === 'source' && Array.isArray(o.names)) return o;
    if (o?.type === 'tableColumns' && o.fromTableUiId) return o;
    return null;
  } catch {
    return null;
  }
}
