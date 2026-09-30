import React, { useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { adaptToCard, adaptToChart, adaptToGrid } from './adapters';
import { DataRenderCard } from './DataRenderCard';
import { DataRenderChartRecharts } from './DataRenderChartRecharts';
import { DataRenderChartWijmo } from './DataRenderChartWijmo';
import { DataRenderGridAg } from './DataRenderGridAg';
import { DataRenderGridWijmo } from './DataRenderGridWijmo';
import { DATA_RENDER_ENGINE } from './engine';
import type { DataRenderEvent } from './types';
import { parseActions } from './types';

interface Props {
    event: DataRenderEvent;
    disabled?: boolean;
    onAction?: (actionId: string, selectionJson?: string) => void;
}

export const DataRenderPanel: React.FC<Props> = ({ event, disabled, onAction }) => {
    const { theme } = useTheme();
    const [selectedRow, setSelectedRow] = useState<Record<string, unknown> | null>(null);
    const ui = (event.Ui || 'grid').toLowerCase();
    const actions = useMemo(() => parseActions(event.ActionsJson), [event.ActionsJson]);
    const btn = `px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`;
    const useWijmo = DATA_RENDER_ENGINE === 'wijmo';

    const body = useMemo(() => {
        if (ui === 'card') {
            const props = adaptToCard({
                dataJson: event.DataJson,
                metaJson: event.MetaJson,
                title: event.Title,
            });
            return <DataRenderCard {...props} />;
        }
        if (ui === 'chart') {
            const props = adaptToChart({
                dataJson: event.DataJson,
                chartConfigJson: event.ChartConfigJson,
                metaJson: event.MetaJson,
                title: event.Title,
            });
            return useWijmo
                ? <DataRenderChartWijmo {...props} />
                : <DataRenderChartRecharts {...props} />;
        }
        const props = adaptToGrid({
            dataJson: event.DataJson,
            columnsJson: event.ColumnsJson,
            metaJson: event.MetaJson,
            title: event.Title,
        });
        const Grid = useWijmo ? DataRenderGridWijmo : DataRenderGridAg;
        return (
            <Grid
                {...props}
                onRowSelected={row => setSelectedRow(row)}
            />
        );
    }, [event, ui, useWijmo]);

    const handleAction = (actionId: string) => {
        if (disabled || !onAction) return;
        const selectionJson = selectedRow ? JSON.stringify(selectedRow) : undefined;
        onAction(actionId, selectionJson);
    };

    return (
        <div className={`my-2 mx-2 border rounded-[4px] p-2 ${theme.mainContentSection}`}>
            <div className="flex items-center justify-between gap-2 mb-2 flex-wrap">
                <div className={`text-xs ${theme.label}`}>
                    <span className="font-medium uppercase tracking-wide opacity-70 mr-2">{ui}</span>
                    {event.Title && <span className="font-medium">{event.Title}</span>}
                    {event.Truncated && (
                        <span className="ml-2 opacity-70">(truncated)</span>
                    )}
                </div>
                {actions.length > 0 && (
                    <div className="flex flex-wrap gap-1">
                        {actions.map(a => (
                            <button
                                key={a.Id}
                                type="button"
                                className={btn}
                                disabled={disabled}
                                onClick={() => handleAction(a.Id)}
                            >
                                {a.Label || a.Id}
                            </button>
                        ))}
                    </div>
                )}
            </div>
            {body}
        </div>
    );
};
