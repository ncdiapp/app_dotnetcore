import type { DataRenderBlock, DataRenderEvent } from './types';
import { parseBlocks } from './types';

export const INLINE_ROW_MAX = 8;

const countRows = (event: DataRenderEvent): number => {
    if (typeof event.RowCount === 'number' && event.RowCount > 0) return event.RowCount;
    try {
        const data = JSON.parse(event.DataJson || '[]');
        if (Array.isArray(data)) return data.length;
    } catch {
        /* not a row array */
    }
    return 0;
};

const blockRows = (block: DataRenderBlock): number => {
    if (Array.isArray(block.data)) return block.data.length;
    return 0;
};

export interface PlacedRender {
    inline?: DataRenderEvent;
    panel?: DataRenderEvent;
}

/** Short KPI and tables of at most 8 rows stay in the thread. Larger grids, charts, and cards open in the panel. */
export function placeRender(event: DataRenderEvent): PlacedRender {
    const ui = (event.Ui || 'grid').toLowerCase();
    if (ui === 'kpi_dashboard') {
        const blocks = parseBlocks(event.BlocksJson);
        const small: DataRenderBlock[] = [];
        const large: DataRenderBlock[] = [];
        blocks.forEach(block => {
            const rows = blockRows(block);
            if (block.type === 'markdown' || block.type === 'kpi' || rows <= INLINE_ROW_MAX)
                small.push(block);
            else
                large.push(block);
        });
        return {
            inline: small.length ? { ...event, BlocksJson: JSON.stringify(small) } : undefined,
            panel: large.length
                ? { ...event, BlocksJson: JSON.stringify(large), RowCount: large.reduce((n, b) => n + blockRows(b), 0) }
                : undefined,
        };
    }
    if (countRows(event) > INLINE_ROW_MAX) return { panel: event };
    return { inline: event };
}

export function renderKey(event: DataRenderEvent, index: number): string {
    return event.RenderId || `${event.Timestamp || 'render'}-${index}`;
}
