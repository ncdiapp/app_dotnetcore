import React from 'react';
import { useTheme } from '../../redux/hooks/useTheme';

const FOLLOWUPS_FENCE = /```followups\s*\n([\s\S]*?)```/i;
const FOLLOWUPS_FENCE_OPEN = /```followups\b/i;

/** Split assistant content into display markdown + optional followup chip labels. */
export function splitFollowups(content: string, isStreaming?: boolean): {
    body: string;
    followups: string[];
} {
    const raw = content ?? '';
    if (isStreaming && FOLLOWUPS_FENCE_OPEN.test(raw) && !FOLLOWUPS_FENCE.test(raw)) {
        // Incomplete fence while streaming — hide the partial block from the body.
        const idx = raw.search(FOLLOWUPS_FENCE_OPEN);
        return { body: idx >= 0 ? raw.slice(0, idx).trimEnd() : raw, followups: [] };
    }
    const m = raw.match(FOLLOWUPS_FENCE);
    if (!m) return { body: raw, followups: [] };
    let followups: string[] = [];
    try {
        const parsed = JSON.parse(m[1].trim()) as unknown;
        if (Array.isArray(parsed)) {
            followups = parsed
                .map(x => {
                    if (typeof x === 'string') return x.trim();
                    if (x && typeof x === 'object' && typeof (x as { text?: string }).text === 'string') {
                        return String((x as { text: string }).text).trim();
                    }
                    return '';
                })
                .filter(Boolean)
                .slice(0, 5);
        }
    } catch {
        followups = [];
    }
    const body = raw.replace(FOLLOWUPS_FENCE, '').trimEnd();
    return { body, followups };
}

const escapeHtml = (s: string) =>
    s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

/** Inline: **bold**, *italic*, `code` */
const formatInline = (text: string): string => {
    let s = escapeHtml(text);
    s = s.replace(/`([^`]+)`/g, '<code class="agent-md-code">$1</code>');
    s = s.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    s = s.replace(/(^|[^*])\*([^*]+)\*(?!\*)/g, '$1<em>$2</em>');
    return s;
};

type Block =
    | { type: 'p'; text: string }
    | { type: 'h'; level: number; text: string }
    | { type: 'ul'; items: string[] }
    | { type: 'ol'; items: string[] }
    | { type: 'table'; headers: string[]; rows: string[][] }
    | { type: 'pre'; text: string };

const parseBlocks = (md: string): Block[] => {
    const lines = (md || '').replace(/\r\n/g, '\n').split('\n');
    const blocks: Block[] = [];
    let i = 0;
    while (i < lines.length) {
        const line = lines[i];
        if (!line.trim()) { i++; continue; }

        // fenced code (non-followups already stripped)
        if (line.trim().startsWith('```')) {
            const lang = line.trim().slice(3).trim();
            i++;
            const buf: string[] = [];
            while (i < lines.length && !lines[i].trim().startsWith('```')) {
                buf.push(lines[i]);
                i++;
            }
            if (i < lines.length) i++; // closing fence
            if (lang.toLowerCase() !== 'followups') {
                blocks.push({ type: 'pre', text: buf.join('\n') });
            }
            continue;
        }

        // table
        if (line.includes('|') && i + 1 < lines.length && /^\s*\|?[\s-:|]+\|?\s*$/.test(lines[i + 1])) {
            const splitRow = (r: string) =>
                r.replace(/^\s*\|/, '').replace(/\|\s*$/, '').split('|').map(c => c.trim());
            const headers = splitRow(line);
            i += 2; // skip header + separator
            const rows: string[][] = [];
            while (i < lines.length && lines[i].includes('|') && lines[i].trim()) {
                rows.push(splitRow(lines[i]));
                i++;
            }
            blocks.push({ type: 'table', headers, rows });
            continue;
        }

        // heading
        const hm = line.match(/^(#{1,3})\s+(.+)$/);
        if (hm) {
            blocks.push({ type: 'h', level: hm[1].length, text: hm[2].trim() });
            i++;
            continue;
        }

        // unordered list
        if (/^\s*[-*]\s+/.test(line)) {
            const items: string[] = [];
            while (i < lines.length && /^\s*[-*]\s+/.test(lines[i])) {
                items.push(lines[i].replace(/^\s*[-*]\s+/, ''));
                i++;
            }
            blocks.push({ type: 'ul', items });
            continue;
        }

        // ordered list
        if (/^\s*\d+\.\s+/.test(line)) {
            const items: string[] = [];
            while (i < lines.length && /^\s*\d+\.\s+/.test(lines[i])) {
                items.push(lines[i].replace(/^\s*\d+\.\s+/, ''));
                i++;
            }
            blocks.push({ type: 'ol', items });
            continue;
        }

        // paragraph (merge consecutive non-empty non-special lines)
        const para: string[] = [line];
        i++;
        while (
            i < lines.length
            && lines[i].trim()
            && !lines[i].trim().startsWith('```')
            && !/^(#{1,3})\s+/.test(lines[i])
            && !/^\s*[-*]\s+/.test(lines[i])
            && !/^\s*\d+\.\s+/.test(lines[i])
            && !(lines[i].includes('|') && i + 1 < lines.length && /^\s*\|?[\s-:|]+\|?\s*$/.test(lines[i + 1]))
        ) {
            para.push(lines[i]);
            i++;
        }
        blocks.push({ type: 'p', text: para.join('\n') });
    }
    return blocks;
};

export const FollowupChips: React.FC<{
    items: string[];
    disabled?: boolean;
    onSelect: (text: string) => void;
}> = ({ items, disabled, onSelect }) => {
    const { theme, t } = useTheme();
    if (!items.length) return null;
    const borderCls = t('border_mainContentSection');
    return (
        <div className="mt-2 flex flex-wrap gap-2">
            {items.map((q, i) => (
                <button
                    key={`${i}-${q.slice(0, 24)}`}
                    type="button"
                    disabled={disabled}
                    className={`text-left text-xs px-3 py-2 rounded-xl border transition-opacity ${borderCls} ${theme.button_default} ${disabled ? 'opacity-50 cursor-not-allowed' : 'hover:opacity-90'}`}
                    onClick={() => onSelect(q)}
                    title={q}
                >
                    {q}
                </button>
            ))}
        </div>
    );
};

/** Lightweight markdown for assistant chat bubbles (no extra npm deps). */
export const AgentMarkdown: React.FC<{
    content: string;
    className?: string;
}> = ({ content, className }) => {
    const { theme, t } = useTheme();
    const borderCls = t('border_mainContentSection');
    const blocks = parseBlocks(content);

    return (
        <div className={`agent-md text-sm leading-relaxed ${className || ''}`}>
            {blocks.map((b, idx) => {
                if (b.type === 'p') {
                    return (
                        <p
                            key={idx}
                            className={`mb-2 last:mb-0 whitespace-pre-wrap ${theme.label}`}
                            dangerouslySetInnerHTML={{ __html: formatInline(b.text) }}
                        />
                    );
                }
                if (b.type === 'h') {
                    const size = b.level === 1 ? 'text-base' : b.level === 2 ? 'text-sm' : 'text-sm';
                    return (
                        <div
                            key={idx}
                            className={`mb-2 font-semibold ${size} ${theme.title}`}
                            dangerouslySetInnerHTML={{ __html: formatInline(b.text) }}
                        />
                    );
                }
                if (b.type === 'ul') {
                    return (
                        <ul key={idx} className={`mb-2 pl-4 list-disc space-y-1 ${theme.label}`}>
                            {b.items.map((it, j) => (
                                <li key={j} dangerouslySetInnerHTML={{ __html: formatInline(it) }} />
                            ))}
                        </ul>
                    );
                }
                if (b.type === 'ol') {
                    return (
                        <ol key={idx} className={`mb-2 pl-4 list-decimal space-y-1 ${theme.label}`}>
                            {b.items.map((it, j) => (
                                <li key={j} dangerouslySetInnerHTML={{ __html: formatInline(it) }} />
                            ))}
                        </ol>
                    );
                }
                if (b.type === 'pre') {
                    return (
                        <pre
                            key={idx}
                            className={`mb-2 p-2 text-xs overflow-x-auto rounded-[4px] border whitespace-pre-wrap ${borderCls} ${theme.mainContentSection} ${theme.label}`}
                        >
                            {b.text}
                        </pre>
                    );
                }
                // table
                return (
                    <div key={idx} className={`mb-3 overflow-x-auto rounded-[4px] border ${borderCls}`}>
                        <table className="w-full border-collapse text-xs">
                            <thead className={theme.mainContentSection}>
                                <tr>
                                    {b.headers.map((h, hi) => (
                                        <th
                                            key={hi}
                                            className={`px-2.5 py-2 text-left font-medium border-b ${borderCls} ${theme.title} whitespace-nowrap`}
                                            dangerouslySetInnerHTML={{ __html: formatInline(h) }}
                                        />
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {b.rows.map((row, ri) => (
                                    <tr key={ri} className={`border-b last:border-b-0 ${borderCls}`}>
                                        {row.map((cell, ci) => (
                                            <td
                                                key={ci}
                                                className={`px-2.5 py-2 ${theme.label} ${/^-?\$?[\d,.]+%?$/.test(cell.trim()) ? 'text-right' : 'text-left'}`}
                                                dangerouslySetInnerHTML={{ __html: formatInline(cell) }}
                                            />
                                        ))}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                );
            })}
            <style>{`
                .agent-md .agent-md-code {
                    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
                    font-size: 0.85em;
                    padding: 0.1em 0.35em;
                    border-radius: 3px;
                    opacity: 0.95;
                }
            `}</style>
        </div>
    );
};
