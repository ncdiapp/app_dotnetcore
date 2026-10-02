import React, { useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { adaptToCard, adaptToChart, adaptToGrid } from './adapters';
import { DataRenderCard } from './DataRenderCard';
import { DataRenderChartRecharts } from './DataRenderChartRecharts';
import { DataRenderChartWijmo } from './DataRenderChartWijmo';
import { DataRenderGridAg } from './DataRenderGridAg';
import { DataRenderGridWijmo } from './DataRenderGridWijmo';
import { DataRenderKpiRow } from './DataRenderKpiRow';
import { DATA_RENDER_ENGINE } from './engine';
import type { DataRenderAction, DataRenderBlock, DataRenderEvent } from './types';
import { parseActions, parseActionsFromUnknown, parseBlocks, parseJsonSafe } from './types';

interface Props {
    event: DataRenderEvent; 
    disabled?: boolean;
    onAction?: (actionId: string, selectionJson?: string) => void;
}

function BlockActions(props: {
    actions: DataRenderAction[];
    disabled?: boolean;
    btn: string;
    onClick: (id: string) => void;
}) {
    if (!props.actions.length) return null;
    return (
        <div className="flex flex-wrap gap-1 mt-2">
            {props.actions.map(a => (
                <button
                    key={a.Id}
                    type="button"
                    className={props.btn}
                    disabled={props.disabled}
                    onClick={() => props.onClick(a.Id)}
                >
                    {a.Label || a.Id}
                </button>
            ))}
        </div>
    );
}

function DashboardBlockView(props: {
    block: DataRenderBlock;
    disabled?: boolean;
    btn: string;
    useWijmo: boolean;
    onAction?: (actionId: string, selectionJson?: string) => void;
}) {
    const { theme, t } = useTheme();
    const { block } = props;
    const [selectedRow, setSelectedRow] = useState<Record<string, unknown> | null>(null);
    const blockActions = useMemo(() => parseActionsFromUnknown(block.actions), [block.actions]);

    const fire = (actionId: string, selectionJson?: string) => {
        if (props.disabled || !props.onAction) return;
        const payload = {
            blockId: block.id,
            blockType: block.type,
            ...(selectionJson ? { selection: parseJsonSafe(selectionJson, null) } : {}),
        };
        props.onAction(actionId, JSON.stringify(payload));
    };

    let body: React.ReactNode = null;
    if (block.type === 'markdown') {
        body = (
            <div className={`text-sm whitespace-pre-wrap break-words ${theme.label}`}>
                {block.content || ''}
            </div>
        );
    } else if (block.type === 'kpi') {
        body = <DataRenderKpiRow title={block.title} items={block.items ?? []} />;
    } else if (block.type === 'chart') {
        const chartProps = adaptToChart({
            dataJson: JSON.stringify(block.data ?? []),
            chartConfigJson: JSON.stringify(block.chartConfig ?? {}),
            metaJson: JSON.stringify(block.meta ?? {}),
            title: block.title,
        });
        body = props.useWijmo
            ? <DataRenderChartWijmo {...chartProps} />
            : <DataRenderChartRecharts {...chartProps} />;
    } else if (block.type === 'grid') {
        const gridProps = adaptToGrid({
            dataJson: JSON.stringify(block.data ?? []),
            columnsJson: block.columns ? JSON.stringify(block.columns) : undefined,
            metaJson: JSON.stringify(block.meta ?? {}),
            title: block.title,
        });
        const Grid = props.useWijmo ? DataRenderGridWijmo : DataRenderGridAg;
        body = (
            <Grid
                {...gridProps}
                onRowSelected={row => setSelectedRow(row)}
            />
        );
    } else if (block.type === 'card') {
        const cardProps = adaptToCard({
            dataJson: JSON.stringify(block.data ?? {}),
            metaJson: JSON.stringify(block.meta ?? {}),
            title: block.title,
        });
        body = <DataRenderCard {...cardProps} />;
    }

    return (
        <div className={`rounded-[4px] border p-2 ${t('border_mainContentSection')}`}>
            {block.title && block.type !== 'kpi' && block.type !== 'chart' && block.type !== 'grid' && block.type !== 'card' && (
                <div className={`text-xs font-medium mb-1 ${theme.label}`}>{block.title}</div>
            )}
            {body}
            <BlockActions
                actions={blockActions}
                disabled={props.disabled}
                btn={props.btn}
                onClick={id => fire(id, selectedRow ? JSON.stringify(selectedRow) : undefined)}
            />
        </div>
    );
}

export const DataRenderPanel: React.FC<Props> = ({ event, disabled, onAction }) => {
    const { theme, t } = useTheme();
    const [selectedRow, setSelectedRow] = useState<Record<string, unknown> | null>(null);
    const ui = (event.Ui || 'grid').toLowerCase();
    const actions = useMemo(() => parseActions(event.ActionsJson), [event.ActionsJson]);
    const blocks = useMemo(() => parseBlocks(event.BlocksJson), [event.BlocksJson]);
    const btn = `px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`;
    const useWijmo = DATA_RENDER_ENGINE === 'wijmo';

    const body = useMemo(() => {
        if (ui === 'kpi_dashboard') {
            if (blocks.length === 0) {
                return <div className={`text-xs ${theme.label}`}>No dashboard blocks.</div>;
            }
            return (
                <div className="flex flex-col gap-3">
                    {blocks.map(b => (
                        <DashboardBlockView
                            key={b.id}
                            block={b}
                            disabled={disabled}
                            btn={btn}
                            useWijmo={useWijmo}
                            onAction={onAction}
                        />
                    ))}
                </div>
            );
        }
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
    }, [event, ui, useWijmo, blocks, disabled, btn, onAction, theme.label]);

    const handleAction = (actionId: string) => {
        if (disabled || !onAction) return;
        const selectionJson = selectedRow ? JSON.stringify(selectedRow) : undefined;
        onAction(actionId, selectionJson);
    };

    return (
        <div className={`my-2 mx-2 rounded-[4px] p-2 border ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
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
