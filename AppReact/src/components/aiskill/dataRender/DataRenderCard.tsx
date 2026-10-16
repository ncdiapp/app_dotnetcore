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
    const { theme } = useTheme();
    const fieldGrid = fieldColumns === 2 ? 'grid-cols-1 sm:grid-cols-2' : 'grid-cols-1';

    return (
        <div className={`min-w-0 overflow-hidden border rounded-[8px] px-3 py-2.5 ${theme.mainContentSection}`}>
            {fields.length === 0 ? (
                <div className={`text-sm ${theme.label}`}>No fields.</div>
            ) : (
                <dl className={`grid ${fieldGrid} gap-x-4 gap-y-1.5 min-w-0`}>
                    {fields.map((f, i) => {
                        const isImage = imageUrlKind(f.value) != null;
                        if (isImage) {
                            return (
                                <div key={`${f.label}-${i}`} className={`min-w-0 overflow-hidden flex flex-col gap-1 ${fieldColumns === 2 ? 'sm:col-span-2' : ''}`}>
                                    <dt className={`text-xs break-words ${theme.label}`}>{f.label}</dt>
                                    <dd className="min-w-0">
                                        <DataRenderImage url={f.value} alt={f.label} size="card" />
                                    </dd>
                                </div>
                            );
                        }
                        return (
                            <div key={`${f.label}-${i}`} className="min-w-0 overflow-hidden flex items-start gap-3">
                                <dt className={`w-28 shrink-0 text-xs pt-0.5 break-words ${theme.label}`}>{f.label}</dt>
                                <dd className={`min-w-0 flex-auto text-sm break-all ${theme.title}`}>{f.value || '—'}</dd>
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
        <div className="min-w-0 flex flex-col gap-2">
            {(title || status) && (
                <div className="flex items-start justify-between gap-3 min-w-0">
                    {title && <div className={`min-w-0 text-sm font-semibold break-words ${theme.title}`}>{title}</div>}
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
