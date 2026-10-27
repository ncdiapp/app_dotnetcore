import React from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { DataRenderImage } from './DataRenderImage';
import { cellImageKind } from './imageValue';

interface CardField {
    label: string;
    value: string;
    role?: string;
}

interface CardProps {
    fields: CardField[];
}

function statusClass(value: string, themeOk: string): string {
    const v = (value || '').toLowerCase();
    if (v.includes('drop') || v.includes('reject')) return 'bg-red-100 text-red-800';
    if (v === 'hold') return 'bg-gray-100 text-gray-600';
    if (v.includes('develop')) return 'bg-blue-100 text-blue-800';
    if (v.includes('approv')) return 'bg-green-100 text-green-800';
    return themeOk;
}

/** MCP-like card: hero image + role-typed fields (idLink, title, statusPill, meta). */
export const DataRenderCard: React.FC<CardProps> = ({ fields }) => {
    const { theme, t } = useTheme();
    const hero = fields.find(f =>
        (f.role || '').toLowerCase() === 'primaryimage'
        || cellImageKind(f.value, f.label) != null
        || cellImageKind(f.value, f.role) != null
    );
    const rest = fields.filter(f => f !== hero);

    return (
        <div
            className={`min-w-[220px] overflow-hidden border rounded-[8px] bg-white flex flex-col ${theme.mainContentSection}`}
            style={{ contentVisibility: 'auto', containIntrinsicSize: '220px 360px' }}
        >
            {hero && (cellImageKind(hero.value, hero.label) != null
                || cellImageKind(hero.value, hero.role) != null
                || (hero.role || '').toLowerCase() === 'primaryimage') && (
                <div className="bg-gray-100 w-full max-h-[200px] aspect-square overflow-hidden relative">
                    <DataRenderImage
                        url={hero.value}
                        alt={hero.label}
                        size="card"
                        kind={cellImageKind(hero.value, hero.label) || cellImageKind(hero.value, hero.role) || 'external'}
                    />
                </div>
            )}
            <div className="px-3.5 py-3 flex flex-col gap-1.5 min-w-0">
                {rest.length === 0 && !hero ? (
                    <div className={`text-xs ${t('text_label')}`}>No fields.</div>
                ) : (
                    rest.map((f, i) => {
                        const role = (f.role || 'body').toLowerCase();
                        const imgKind = cellImageKind(f.value, f.label) || cellImageKind(f.value, f.role);
                        if (imgKind != null) {
                            return (
                                <div key={`${f.label}-${i}`} className="min-w-0">
                                    <DataRenderImage url={f.value} alt={f.label} size="compact" kind={imgKind} />
                                </div>
                            );
                        }
                        if (role === 'idlink') {
                            return (
                                <div key={`${f.label}-${i}`} className="text-[13px] font-bold text-blue-600 break-words">
                                    {f.value || '—'}
                                </div>
                            );
                        }
                        if (role === 'title') {
                            return (
                                <div key={`${f.label}-${i}`} className={`text-xs font-medium break-words ${t('text_default')}`}>
                                    {f.value || '—'}
                                </div>
                            );
                        }
                        if (role === 'statuspill') {
                            return (
                                <span
                                    key={`${f.label}-${i}`}
                                    className={`self-start text-[11px] font-semibold px-2 py-0.5 rounded-full ${statusClass(f.value, theme.button_default)}`}
                                >
                                    {f.value || '—'}
                                </span>
                            );
                        }
                        if (role === 'colorswatch') {
                            return (
                                <div key={`${f.label}-${i}`} className={`flex items-center gap-1.5 text-xs ${t('text_default')}`}>
                                    <span className="w-2.5 h-2.5 rounded-full bg-blue-400 border border-gray-300 shrink-0" />
                                    {f.value || '—'}
                                </div>
                            );
                        }
                        if (role === 'meta') {
                            return (
                                <div key={`${f.label}-${i}`} className={`text-[11px] break-words ${t('text_label')}`}>
                                    {f.value || '—'}
                                </div>
                            );
                        }
                        return (
                            <div key={`${f.label}-${i}`} className="min-w-0 overflow-hidden flex items-baseline gap-3">
                                <dt className={`w-28 shrink-0 text-[11px] leading-4 font-normal break-words ${t('text_label')}`}>{f.label}</dt>
                                <dd className={`min-w-0 w-1 flex-auto text-xs leading-4 font-normal break-words ${t('text_default')}`}>{f.value || '—'}</dd>
                            </div>
                        );
                    })
                )}
            </div>
        </div>
    );
};

interface ListProps {
    title?: string;
    status?: string;
    cards: Array<Array<{ label: string; value: string; role?: string }>>;
    cardsPerRow?: number;
}

export const DataRenderCardList: React.FC<ListProps> = ({ title, status, cards, cardsPerRow = 1 }) => {
    const { theme } = useTheme();
    // cardsPerRow <= 0 → responsive auto-fill (min 220px), same as agent-cards.css.
    const columns = cardsPerRow > 0 ? Math.min(6, Math.max(1, cardsPerRow)) : 0;

    return (
        <div className="min-w-0 flex flex-col gap-3 agent-cards-board rounded-[6px] p-3" style={{ background: '#e8ecf1' }}>
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
                    style={{
                        gridTemplateColumns: columns > 0
                            ? `repeat(${columns}, minmax(0, 1fr))`
                            : 'repeat(auto-fill, minmax(220px, 1fr))',
                    }}
                >
                    {cards.map((fields, i) => (
                        <DataRenderCard key={i} fields={fields} />
                    ))}
                </div>
            )}
        </div>
    );
};
