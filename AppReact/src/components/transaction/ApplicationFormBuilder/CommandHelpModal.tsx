import React, { useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { AgentMarkdown } from '../../aiskill/AgentMarkdown';

export type CommandHelpSectionId =
  | 'overview'
  | 'types'
  | 'shared'
  | 'composition'
  | 'howto'
  | 'practices'
  | 'all';

const SECTION_HINTS: { id: CommandHelpSectionId; label: string; match: RegExp }[] = [
  { id: 'overview', label: 'Concepts', match: /^## 1\. Concepts/m },
  { id: 'types', label: 'Operation types', match: /^## 2\. Finding the right/m },
  { id: 'shared', label: 'Shared settings', match: /^## 3\. Shared settings/m },
  { id: 'composition', label: 'Composition & links', match: /^## 4\. Composition/m },
  { id: 'howto', label: 'I want to…', match: /^## 5\. Quick/m },
  { id: 'practices', label: 'Good practices', match: /^## 6\. Good practices/m },
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

export function CommandHelpModal(props: {
  open: boolean;
  initialSection?: CommandHelpSectionId;
  onClose: () => void;
  onAskConfigurator?: (prompt: string) => void;
  askPrompt?: string;
}) {
  const { theme } = useTheme();
  const { open, initialSection = 'overview', onClose, onAskConfigurator, askPrompt } = props;
  const [md, setMd] = useState('');
  const [loadError, setLoadError] = useState<string | null>(null);
  const [section, setSection] = useState<CommandHelpSectionId>(initialSection);
  const [maximized, setMaximized] = useState(false);

  useEffect(() => {
    if (!open) return;
    setSection(initialSection);
    setMaximized(false);
  }, [open, initialSection]);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    setLoadError(null);
    fetch(`${process.env.PUBLIC_URL || ''}/help/commands-user-help.md`, { cache: 'no-cache' })
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
      className={`fixed inset-0 flex items-center justify-center bg-black/40 ${maximized ? '' : 'px-4 py-4'}`}
      style={{ zIndex: 15000 }}
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        className={`flex flex-col border shadow-xl overflow-hidden ${theme.mainContentSection} ${
          maximized ? 'h-full w-full rounded-none' : 'w-full max-w-4xl rounded-[4px]'
        }`}
        style={
          maximized
            ? { width: '100%', height: '100%', maxWidth: '100vw', maxHeight: '100vh' }
            : { width: '100%', height: 'min(820px, 90vh)', maxWidth: '56rem', maxHeight: '90vh' }
        }
        role="dialog"
        aria-modal="true"
        aria-label="Data Model Operation help"
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-3 py-2 border-b shrink-0">
          <div className={`text-sm font-semibold ${theme.title}`}>Data Model Operation — Help</div>
          <div className="flex items-center gap-1 shrink-0">
            <button
              type="button"
              onClick={() => setMaximized((m) => !m)}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
              title={maximized ? 'Exit full screen' : 'Full screen'}
              aria-label={maximized ? 'Exit full screen' : 'Full screen'}
            >
              <i className={`fa-solid ${maximized ? 'fa-compress' : 'fa-expand'}`} aria-hidden />
            </button>
            <button
              type="button"
              onClick={onClose}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          </div>
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
            <AgentMarkdown content={body} className="text-xs leading-relaxed max-w-none" />
          )}
        </div>
      </div>
    </div>
  );
}
