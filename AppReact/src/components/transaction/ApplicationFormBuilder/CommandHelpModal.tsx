import React, { useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

export type CommandHelpSectionId =
  | 'overview'
  | 'shared'
  | 'types'
  | 'composition'
  | 'agent'
  | 'all';

const SECTION_HINTS: { id: CommandHelpSectionId; label: string; match: RegExp }[] = [
  { id: 'overview', label: 'Concepts', match: /^## 1\. Concepts/m },
  { id: 'shared', label: 'Shared settings', match: /^## 2\. Shared settings/m },
  { id: 'composition', label: 'Master → child', match: /^## 3\. Master/m },
  { id: 'types', label: 'Per-type reference', match: /^## 5\. Per-type/m },
  { id: 'agent', label: 'Configurator agent', match: /^## 6\. Configurator/m },
];

function extractSection(md: string, sectionId: CommandHelpSectionId): string {
  if (sectionId === 'all' || !md) return md;
  const hint = SECTION_HINTS.find((h) => h.id === sectionId);
  if (!hint) return md;
  const start = md.search(hint.match);
  if (start < 0) return md;
  const rest = md.slice(start + 1);
  const next = rest.search(/^## /m);
  if (next < 0) return md.slice(start);
  return md.slice(start, start + 1 + next);
}

/** Very small markdown → React-ish text (headers + paragraphs + tables as pre). */
function SimpleMarkdownView({ text, themeLabel }: { text: string; themeLabel: string }) {
  const blocks = useMemo(() => text.split(/\n\n+/), [text]);
  return (
    <div className={`text-xs leading-relaxed space-y-3 ${themeLabel}`}>
      {blocks.map((block, i) => {
        const t = block.trim();
        if (!t) return null;
        if (t.startsWith('# ')) {
          return (
            <h2 key={i} className="text-base font-semibold">
              {t.replace(/^#\s+/, '')}
            </h2>
          );
        }
        if (t.startsWith('## ')) {
          return (
            <h3 key={i} className="text-sm font-semibold mt-2">
              {t.replace(/^##\s+/, '')}
            </h3>
          );
        }
        if (t.startsWith('### ')) {
          return (
            <h4 key={i} className="text-xs font-semibold mt-1">
              {t.replace(/^###\s+/, '')}
            </h4>
          );
        }
        if (t.startsWith('#### ')) {
          return (
            <h4 key={i} className="text-xs font-semibold opacity-90">
              {t.replace(/^####\s+/, '')}
            </h4>
          );
        }
        if (t.includes('|') && t.includes('\n')) {
          return (
            <pre
              key={i}
              className="overflow-x-auto whitespace-pre text-[11px] p-2 border rounded bg-black/5 dark:bg-white/5"
            >
              {t}
            </pre>
          );
        }
        if (t.startsWith('- ') || t.startsWith('|')) {
          return (
            <pre key={i} className="whitespace-pre-wrap text-[11px]">
              {t}
            </pre>
          );
        }
        return (
          <p key={i} className="whitespace-pre-wrap">
            {t}
          </p>
        );
      })}
    </div>
  );
}

export function CommandHelpModal(props: {
  open: boolean;
  initialSection?: CommandHelpSectionId;
  onClose: () => void;
  /** Optional: post a fixed prompt into Configurator (host supplies handler). */
  onAskConfigurator?: (prompt: string) => void;
  askPrompt?: string;
}) {
  const { theme } = useTheme();
  const { open, initialSection = 'overview', onClose, onAskConfigurator, askPrompt } = props;
  const [md, setMd] = useState('');
  const [loadError, setLoadError] = useState<string | null>(null);
  const [section, setSection] = useState<CommandHelpSectionId>(initialSection);

  useEffect(() => {
    if (!open) return;
    setSection(initialSection);
  }, [open, initialSection]);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    setLoadError(null);
    fetch(`${process.env.PUBLIC_URL || ''}/help/commands-user-guide.md`, { cache: 'no-cache' })
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.text();
      })
      .then((t) => {
        if (!cancelled) setMd(t);
      })
      .catch((e) => {
        if (!cancelled) setLoadError(e?.message || 'Failed to load help');
      });
    return () => {
      cancelled = true;
    };
  }, [open]);

  const body = useMemo(() => extractSection(md, section), [md, section]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 flex items-center justify-center bg-black/40 px-4"
      style={{ zIndex: 15000 }}
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        className={`w-full max-w-3xl max-h-[85vh] flex flex-col border rounded-[4px] shadow-xl overflow-hidden ${theme.mainContentSection}`}
        role="dialog"
        aria-modal="true"
        aria-label="Data Model Operation help"
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b shrink-0`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Data Model Operation — Help</div>
          <button
            type="button"
            onClick={onClose}
            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
            aria-label="Close"
          >
            <i className="fa-solid fa-xmark" />
          </button>
        </div>

        <div className="px-3 py-2 flex flex-wrap gap-1.5 border-b shrink-0">
          <button
            type="button"
            onClick={() => setSection('all')}
            className={`px-2 py-1 text-xs rounded-[4px] border ${
              section === 'all' ? theme.button_default : theme.inputBox
            }`}
          >
            Full guide
          </button>
          {SECTION_HINTS.map((h) => (
            <button
              key={h.id}
              type="button"
              onClick={() => setSection(h.id)}
              className={`px-2 py-1 text-xs rounded-[4px] border ${
                section === h.id ? theme.button_default : theme.inputBox
              }`}
            >
              {h.label}
            </button>
          ))}
          {onAskConfigurator && askPrompt ? (
            <button
              type="button"
              onClick={() => {
                onAskConfigurator(askPrompt);
                onClose();
              }}
              className={`px-2 py-1 text-xs rounded-[4px] ml-auto ${theme.button_default}`}
              title="Send this help topic to Configurator chat"
            >
              <i className="fa-solid fa-robot mr-1" />
              Ask Configurator
            </button>
          ) : null}
        </div>

        <div className="min-h-0 h-1 flex-auto overflow-auto px-4 py-3">
          {loadError ? (
            <p className={`text-xs ${theme.label}`}>Could not load help: {loadError}</p>
          ) : !md ? (
            <p className={`text-xs opacity-70 ${theme.label}`}>Loading…</p>
          ) : (
            <SimpleMarkdownView text={body} themeLabel={theme.label} />
          )}
        </div>
      </div>
    </div>
  );
}
