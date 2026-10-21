import React, { useMemo } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { parsePageViews, type DataRenderEvent } from '../../webapi/genericAgentSvc';
import { DataRenderPanel } from './dataRender';

interface Props {
    event: DataRenderEvent;
    disabled?: boolean;
    onAction?: (actionId: string, selectionJson?: string) => void;
}

/** Child panels must not inherit page SpecJson; omit rather than assign undefined (TS excess checks). */
function toChildEvent(
    event: DataRenderEvent,
    patch: Partial<DataRenderEvent> & Pick<DataRenderEvent, 'Ui'>,
): DataRenderEvent {
    const { SpecJson: _omitSpec, MetaJson: _omitMeta, ...rest } = event;
    return { ...rest, ...patch };
}

/**
 * Show-only Result page: stack grid + cards from one DataJson (like a simple dashboard layout,
 * without KPI/analysis). Used when layout=page.
 */
const InteractivePagePanel: React.FC<Props> = ({ event, disabled, onAction }) => {
    const { theme } = useTheme();
    const { views, cardsPerRow, title } = useMemo(
        () => parsePageViews(event.SpecJson),
        [event.SpecJson],
    );
    const pageTitle = title || event.Title || 'Result';

    return (
        <div className="w-full h-full overflow-auto flex flex-col gap-3 p-1">
            {pageTitle && (
                <h2 className={`text-base font-semibold m-0 px-1 ${theme.label}`}>{pageTitle}</h2>
            )}
            {views.includes('table') && (
                <DataRenderPanel
                    event={toChildEvent(event, {
                        Ui: 'grid',
                        Layout: 'table',
                        Title: `${pageTitle} — Grid`,
                    })}
                    disabled={disabled}
                    onAction={onAction}
                />
            )}
            {views.includes('cards') && (
                <DataRenderPanel
                    event={toChildEvent(event, {
                        Ui: 'card',
                        Layout: 'cards',
                        Title: `${pageTitle} — Cards`,
                        MetaJson: JSON.stringify({ cardsPerRow }),
                    })}
                    disabled={disabled}
                    onAction={onAction}
                />
            )}
        </div>
    );
};

export default InteractivePagePanel;
