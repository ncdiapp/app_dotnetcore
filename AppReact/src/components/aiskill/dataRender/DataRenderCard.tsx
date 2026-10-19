import React from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { DataRenderImage } from './DataRenderImage';
import { imageUrlKind } from './imageValue';

interface CardProps {
    fields: Array<{ label: string; value: string }>;
    /** Two field columns when the card spans the row. One column when several cards share a row. */
    fieldColumns?: 1 | 2;
}

export const DataRenderCard: React.FC<CardProps> = ({ fields, fieldColumns = 2 }) => {
    const { theme, t } = useTheme();
    const stacked = fieldColumns === 1;
    const fieldGrid = stacked ? 'grid-cols-1' : 'grid-cols-1 sm:grid-cols-2';

    return (
        <div className={`min-w-0 overflow-hidden border rounded-[8px] px-3.5 py-3 ${theme.mainContentSection}`}>
            {fields.length === 0 ? (
                <div className={`text-xs ${t('text_label')}`}>No fields.</div>
            ) : (
                <dl className={`grid ${fieldGrid} gap-x-6 gap-y-2.5 min-w-0`}>
                    {fields.map((f, i) => {
                        const isImage = imageUrlKind(f.value) != null;
                        if (isImage) {
                            return (
                                <div key={`${f.label}-${i}`} className={`min-w-0 overflow-hidden flex flex-col gap-1.5 ${stacked ? '' : 'sm:col-span-2'}`}>
                                    <dt className={`text-[11px] leading-4 font-normal break-words ${t('text_label')}`}>{f.label}</dt>
                                    <dd className="min-w-0">
                                        <DataRenderImage url={f.value} alt={f.label} size={stacked ? 'compact' : 'card'} />
                                    </dd>
                                </div>
                            );
                        }
                        if (stacked) {
                            return (
                                <div key={`${f.label}-${i}`} className="min-w-0 overflow-hidden flex flex-col gap-0.5">
                                    <dt className={`text-[11px] leading-4 font-normal break-words ${t('text_label')}`}>{f.label}</dt>
                                    <dd className={`min-w-0 text-xs leading-4 font-normal break-words ${t('text_default')}`}>{f.value || '—'}</dd>
                                </div>
                            );
                        }
                        return (
                            <div key={`${f.label}-${i}`} className="min-w-0 overflow-hidden flex items-baseline gap-3">
                                <dt className={`w-28 shrink-0 text-[11px] leading-4 font-normal break-words ${t('text_label')}`}>{f.label}</dt>
                                <dd className={`min-w-0 w-1 flex-auto text-xs leading-4 font-normal break-words ${t('text_default')}`}>{f.value || '—'}</dd>
                            </div>
                        );
                    })}
                </dl>
            )}
        </div>
    );
};

interface ListProps {
    title?: string;
    status?: string;
    cards: Array<Array<{ label: string; value: string }>>;
    cardsPerRow?: number;
}

export const DataRenderCardList: React.FC<ListProps> = ({ title, status, cards, cardsPerRow = 1 }) => {
    const { theme } = useTheme();
    const columns = Math.min(6, Math.max(1, cardsPerRow || 1));

    return (
            <div className="min-w-0 flex flex-col gap-3">
            {(title || status) && (
                <div className="flex items-start justify-between gap-3 min-w-0">
                    {title && <div className={`min-w-0 text-sm font-medium break-words ${theme.title}`}>{title}</div>}
                    {status && (
                        <span className={`shrink-0 text-xs px-2 py-0.5 rounded border ${theme.button_default}`}>
                            {status}
                        </span>
                    )}
                </div>
            )}
            {cards.length === 0 ? (
                <div className={`text-sm ${theme.label}`}>No records.</div>
            ) : (
                <div
                    className="grid gap-3 min-w-0"
                    style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}
                >
                    {cards.map((fields, i) => (
                        <DataRenderCard
                            key={i}
                            fields={fields}
                            fieldColumns={columns === 1 ? 2 : 1}
                        />
                    ))}
                </div>
            )}
        </div>
    );
};
