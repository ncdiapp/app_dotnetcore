import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import TemplateControlLayout, {
    filterCardsHtmlByRows,
    parseCardsHtmlFromSpec,
} from '../TemplateControlLayout';
import { adaptToChart, adaptToGrid, parseRows } from './adapters';
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
        // Agent Result cards are TemplateContent/cardsHtml only — block cards unused here.
        body = (
            <div className={`text-xs ${theme.label}`}>
                Card block requires TemplateContent layout (cardsHtml).
            </div>
        );
    }

    const framed = block.type !== 'card';
    return (
        <div className={framed ? `rounded-[4px] border p-3 ${t('border_mainContentSection')}` : ''}>
            {block.title && framed && block.type !== 'kpi' && block.type !== 'chart' && block.type !== 'grid' && (
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
    // Keep latest onAction without forcing grid/card body remount when chat re-renders.
    const onActionRef = useRef(onAction);
    useEffect(() => { onActionRef.current = onAction; }, [onAction]);
    const fireAction = useMemo(
        () => (actionId: string, selectionJson?: string) => onActionRef.current?.(actionId, selectionJson),
        [],
    );
    // Persisted ui_render often stores Layout in Ui ("cards"); accept both.
    const uiRaw = (event.Ui || event.Layout || 'grid').toLowerCase();
    const ui = uiRaw === 'cards' ? 'card'
        : uiRaw === 'kpi' || uiRaw === 'dashboard' ? 'kpi_dashboard'
        : uiRaw;
    const actions = useMemo(() => parseActions(event.ActionsJson), [event.ActionsJson]);
    const blocks = useMemo(() => parseBlocks(event.BlocksJson), [event.BlocksJson]);
    const btn = useMemo(
        () => `px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`,
        [theme.button_default],
    );
    const useWijmo = DATA_RENDER_ENGINE === 'wijmo';
    const labelClass = theme.label;

    // Stable adapt — chat keystrokes must not rebuild rowData arrays.
    const gridProps = useMemo(
        () => adaptToGrid({
            dataJson: event.DataJson,
            columnsJson: event.ColumnsJson,
            metaJson: event.MetaJson,
            title: undefined,
        }),
        [event.DataJson, event.ColumnsJson, event.MetaJson],
    );
    const chartProps = useMemo(
        () => adaptToChart({
            dataJson: event.DataJson,
            chartConfigJson: event.ChartConfigJson,
            metaJson: event.MetaJson,
            title: event.Title,
        }),
        [event.DataJson, event.ChartConfigJson, event.MetaJson, event.Title],
    );
    const cardsHtml = useMemo(() => {
        if (ui !== 'card') return null;
        const full = parseCardsHtmlFromSpec(event.SpecJson);
        return full ? filterCardsHtmlByRows(full, parseRows(event.DataJson)) : null;
    }, [ui, event.SpecJson, event.DataJson]);

    const onRowSelected = useCallback((row: Record<string, unknown>) => {
        setSelectedRow(row);
    }, []);

    const body = useMemo(() => {
        if (ui === 'kpi_dashboard') {
            if (blocks.length === 0) {
                return <div className={`text-xs ${labelClass}`}>No dashboard blocks.</div>;
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
                            onAction={fireAction}
                        />
                    ))}
                </div>
            );
        }
        if (ui === 'card') {
            // Agent Result cards = TemplateContent / cardsHtml only (not React DataRenderCard).
            if (cardsHtml) return <TemplateControlLayout html={cardsHtml} />;
            return (
                <div className={`text-xs ${labelClass}`}>
                    Card Result requires TemplateContent (cardsHtml). Re-run render_interactive with polish.
                </div>
            );
        }
        if (ui === 'chart') {
            return useWijmo
                ? <DataRenderChartWijmo {...chartProps} />
                : <DataRenderChartRecharts {...chartProps} />;
        }
        // Single-control Result (grid only): fill remaining height — no ~420px cap.
        const Grid = useWijmo ? DataRenderGridWijmo : DataRenderGridAg;
        return (
            <Grid
                {...gridProps}
                fillHeight
                onRowSelected={onRowSelected}
            />
        );
    }, [ui, useWijmo, blocks, disabled, btn, fireAction, labelClass, cardsHtml, chartProps, gridProps, onRowSelected]);

    const handleAction = (actionId: string) => {
        if (disabled || !onAction) return;
        const selectionJson = selectedRow ? JSON.stringify(selectedRow) : undefined;
        onAction(actionId, selectionJson);
    };

    const cardOnly = ui === 'card';
    const gridOnly = ui === 'grid' || ui === 'table';
    // ResultChrome already shows the title — keep a slim toolbar only for actions / truncated.
    const showPanelHeader = !cardOnly && (actions.length > 0 || !!event.Truncated);
    return (
        <div className={
            cardOnly
                ? 'h-full min-h-0 overflow-auto'
                : `h-full min-h-0 flex flex-col rounded-[4px] border ${t('border_mainContentSection')} ${theme.mainContentSection} ${gridOnly ? 'p-2' : 'p-4'}`
        }>
            {showPanelHeader && (
            <div className="flex items-center justify-between gap-2 mb-2 flex-wrap shrink-0">
                <div className={`text-sm font-semibold ${theme.title}`}>
                    {event.Truncated && (
                        <span className={`text-xs font-normal ${theme.label}`}>(truncated)</span>
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
            )}
            <div className={`min-h-0 min-w-0 ${gridOnly || cardOnly ? 'h-1 flex-auto' : ''}`}>
            {body}
            </div>
            {cardOnly && actions.length > 0 && (
                <div className="flex flex-wrap gap-1 mt-2">
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
    );
};
