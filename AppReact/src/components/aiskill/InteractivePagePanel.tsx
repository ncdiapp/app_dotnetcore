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
    const { SpecJson: _omitSpec, MetaJson: _omitMeta, Html: _h, HtmlPath: _hp, HtmlUrl: _hu, ...rest } = event;
    return { ...rest, ...patch };
}

/**
 * Interactive Result page: Wijmo grid + React cards, stacked with collapse chrome.
 * Grid pane: max-height min(80vh, 800px). Cards: no max-height (outer panel scrolls).
 */
const InteractivePagePanel: React.FC<Props> = ({ event, disabled, onAction }) => {
    const { theme } = useTheme();
    const { views, cardsPerRow, title, sectionTitles } = useMemo(
        () => parsePageViews(event.SpecJson),
        [event.SpecJson],
    );
    const pageTitle = title || event.Title || 'Result';
    const sectionCount = (views.includes('table') ? 1 : 0) + (views.includes('cards') ? 1 : 0);
    const useCollapse = sectionCount >= 2;
    const tableSectionTitle =
        sectionTitles?.table || sectionTitles?.grid || `${pageTitle} — Table view`;
    const cardsSectionTitle =
        sectionTitles?.cards || sectionTitles?.card || `${pageTitle} — Card view`;

    return (
        <div className="w-full h-full min-w-0 max-w-full overflow-auto flex flex-col gap-2 p-1 box-border">
            {pageTitle && (
                <h2 className={`text-base font-semibold m-0 px-1 shrink-0 ${theme.label}`}>{pageTitle}</h2>
            )}
            {views.includes('table') && (
                <PageSection title={tableSectionTitle} collapse={useCollapse}>
                    <div
                        className="min-w-0 max-w-full overflow-auto border border-gray-200 rounded-[4px]"
                        style={{ maxHeight: 'min(80vh, 800px)' }}
                    >
                        <DataRenderPanel
                            event={toChildEvent(event, {
                                Ui: 'grid',
                                Layout: 'table',
                                Title: tableSectionTitle,
                            })}
                            disabled={disabled}
                            onAction={onAction}
                        />
                    </div>
                </PageSection>
            )}
            {views.includes('cards') && (
                <PageSection title={cardsSectionTitle} collapse={useCollapse}>
                    <div className="min-w-0 max-w-full overflow-x-auto">
                        <DataRenderPanel
                            event={toChildEvent(event, {
                                Ui: 'card',
                                Layout: 'cards',
                                Title: cardsSectionTitle,
                                MetaJson: JSON.stringify({ cardsPerRow }),
                            })}
                            disabled={disabled}
                            onAction={onAction}
                        />
                    </div>
                </PageSection>
            )}
        </div>
    );
};

const PageSection: React.FC<{
    title: string;
    collapse: boolean;
    children: React.ReactNode;
}> = ({ title, collapse, children }) => {
    if (!collapse) {
        return <div className="min-w-0 max-w-full mb-2">{children}</div>;
    }
    return (
        <details open className="min-w-0 max-w-full mb-2 rounded-[4px] border border-gray-300 bg-white">
            <summary className="cursor-pointer select-none px-3 py-2 text-sm font-semibold list-none">
                {title}
            </summary>
            <div className="px-3 pb-3 min-w-0 max-w-full overflow-hidden">{children}</div>
        </details>
    );
};

export default InteractivePagePanel;
