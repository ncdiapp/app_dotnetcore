import React, { useEffect, useRef } from 'react';
import 'grapesjs/dist/css/grapes.min.css';

// eslint-disable-next-line @typescript-eslint/no-require-imports
const grapesjs = require('grapesjs');
// eslint-disable-next-line @typescript-eslint/no-require-imports
const grapesjsNewsletter = require('grapesjs-preset-newsletter');

interface TokenDescriptor {
  Token: string;
  ResultSet: string;
  Field: string;
  IsList: boolean;
  InsideEach: boolean;
}

interface ReportBlock {
  label: string;
  icon: string;
  color: string;
  html: string;
}

interface GrapeJsEditorProps {
  html: string;
  tokens: TokenDescriptor[];
  blocks: ReportBlock[];
  isDark: boolean;
  /** When false, GrapeJS will not sync incoming html changes and will not emit onChange.
   *  Prevents feedback loop with Monaco when the user is in Code mode. */
  active: boolean;
  onChange: (html: string) => void;
  onEditorReady?: (editor: any) => void;
}

// Strip leading <style> block from HTML string to separate CSS from body content
function splitStyleAndBody(html: string): { css: string; body: string } {
  const styleMatch = html.match(/^<style>([\s\S]*?)<\/style>\s*/i);
  if (styleMatch) {
    return { css: styleMatch[1], body: html.slice(styleMatch[0].length) };
  }
  return { css: '', body: html };
}

const GrapeJsEditor: React.FC<GrapeJsEditorProps> = ({
  html,
  tokens,
  blocks,
  isDark,
  active,
  onChange,
  onEditorReady,
}) => {
  const containerRef  = useRef<HTMLDivElement>(null);
  const gjsRef        = useRef<any>(null);
  const onChangeRef   = useRef(onChange);
  const activeRef     = useRef(active);
  const htmlRef       = useRef(html);

  // Update refs synchronously during render so event handlers always see the latest values
  // without waiting for an async useEffect tick (which could miss a state transition window).
  onChangeRef.current = onChange;
  activeRef.current   = active;
  htmlRef.current     = html;

  // Initialize GrapeJS once on mount
  useEffect(() => {
    if (!containerRef.current) return;

    const { css, body } = splitStyleAndBody(html);

    const editor = grapesjs.init({
      container: containerRef.current,
      fromElement: false,
      storageManager: false,
      height: '100%',
      width: '100%',
      components: body,
      style: css,
      plugins: [grapesjsNewsletter],
      pluginsOpts: { [grapesjsNewsletter]: {} },
      // Keep GrapeJS panels minimal — left panel is handled by our app's own panel
      panels: { defaults: [] },
      deviceManager: { devices: [] },
    });

    // Register our REPORT_BLOCKS as GrapeJS blocks
    blocks.forEach(block => {
      const slug = block.label.toLowerCase().replace(/\s+/g, '-');
      editor.BlockManager.add(slug, {
        label: block.label,
        category: 'Report Blocks',
        content: block.html,
      });
    });

    // Register each token as a draggable block (users can drag from GrapeJS block panel)
    tokens.forEach(tok => {
      const id = `tok-${tok.Token.replace(/[^a-z0-9]/gi, '_')}`;
      const label = tok.Field === '*' ? `#each ${tok.ResultSet}` : tok.Field;
      editor.BlockManager.add(id, {
        label,
        category: tok.ResultSet,
        content: `<span data-token="true">${tok.Token}</span>`,
      });
    });

    // ── Mark text-container elements as editable ────────────────────────────
    const EDITABLE_TAGS = new Set(['td', 'th', 'p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'span', 'li', 'a', 'div', 'label']);
    const markEditable = (comps: any) => {
      comps?.each?.((c: any) => {
        if (EDITABLE_TAGS.has((c.get('tagName') ?? '').toLowerCase())) c.set('editable', true);
        markEditable(c.components?.());
      });
    };
    editor.on('component:add', (c: any) => {
      if (EDITABLE_TAGS.has((c.get('tagName') ?? '').toLowerCase())) c.set('editable', true);
      markEditable(c.components?.());
    });

    // ── Canvas ready: wire up dblclick-to-edit ──────────────────────────────
    // IMPORTANT: editor.Canvas.getDocument() returns undefined immediately after
    // grapesjs.init() — the iframe hasn't loaded yet. We must wait for 'load'.
    // Approach: make the DOM element contenteditable directly (GrapeJS's RTE only
    // works for 'text' component types; td/th/p from newsletter preset are generic).
    editor.on('load', () => {
      // Mark all existing text containers editable now that the model is ready
      markEditable(editor.DomComponents.getComponents());

      // Apply canvas background
      try {
        const canvasDoc = editor.Canvas.getDocument();
        if (canvasDoc?.body) canvasDoc.body.style.background = isDark ? '#1e1e1e' : '#ffffff';
      } catch { /* ignore */ }

      // Attach dblclick-to-contenteditable handler to the now-ready canvas document
      const canvasDoc = editor.Canvas.getDocument?.();
      if (!canvasDoc) return;

      const TEXT_TAGS = new Set(['TD','TH','P','H1','H2','H3','H4','H5','H6','SPAN','LI','A','LABEL']);
      let editGuard = false;

      const findDeepComp = (comps: any, el: HTMLElement): any => {
        let hit: any = null;
        comps?.each?.((c: any) => {
          if (hit) return;
          if (c.view?.el === el) { hit = c; return; }
          const child = findDeepComp(c.components?.(), el);
          if (child) hit = child;
        });
        return hit;
      };

      canvasDoc.addEventListener('dblclick', (e: Event) => {
        if (editGuard) return;
        let el: HTMLElement | null = e.target as HTMLElement;
        while (el && el.tagName !== 'BODY') {
          if (TEXT_TAGS.has(el.tagName)) break;
          el = el.parentElement;
        }
        if (!el || el.tagName === 'BODY') return;

        e.stopPropagation();

        const snapshot = el.innerHTML;
        el.setAttribute('contenteditable', 'true');
        el.style.outline = '2px solid #0d6efd';
        el.style.outlineOffset = '2px';
        el.style.cursor = 'text';
        el.focus();

        // Select all existing content so first keystroke replaces it
        try {
          const range = canvasDoc.createRange();
          range.selectNodeContents(el);
          const sel = canvasDoc.defaultView?.getSelection();
          sel?.removeAllRanges();
          sel?.addRange(range);
        } catch { /* ignore */ }

        editGuard = true;

        const finish = (cancel = false) => {
          if (!el) return;
          const newHtml = el.innerHTML;
          el.removeAttribute('contenteditable');
          el.style.outline = '';
          el.style.outlineOffset = '';
          el.style.cursor = '';
          editGuard = false;

          if (cancel) { el.innerHTML = snapshot; return; }
          if (newHtml === snapshot) return;

          // Push edited HTML back into GrapeJS model.
          // comp.components().reset() + comp.append() already fire GrapeJS's
          // internal change events which increment changesCount naturally —
          // no need to trigger it manually (that causes UndoManager crash).
          const comp = findDeepComp(editor.DomComponents.getComponents(), el);
          if (comp) {
            comp.components().reset();
            comp.append(newHtml);
          }
        };

        el.addEventListener('blur', () => finish(false), { once: true });
        el.addEventListener('keydown', (ke: KeyboardEvent) => {
          if (ke.key === 'Escape') { ke.stopPropagation(); finish(true); }
          else if (ke.key === 'Enter' && !ke.shiftKey) { ke.preventDefault(); finish(false); }
        }, { once: true });
      }, true);

      // ── Drag-drop from left panel: tokens onto cells, blocks into canvas ──────
      // Tokens  (text/plain = "{{header.Field}}") → replace target cell content.
      // Blocks  (text/plain = full HTML, marker = application/x-report-block)
      //         → insert as new sibling element after the drop position.
      let dragOverEl: HTMLElement | null = null;

      const clearDragHighlight = () => {
        if (dragOverEl) {
          dragOverEl.style.outline = '';
          dragOverEl.style.backgroundColor = '';
          dragOverEl = null;
        }
      };

      const dropTargetFor = (target: EventTarget | null): HTMLElement | null => {
        let el = target as HTMLElement | null;
        while (el && el.tagName !== 'BODY') {
          if (TEXT_TAGS.has(el.tagName)) return el;
          el = el.parentElement;
        }
        return null;
      };

      // Find the direct child of <body> that contains (or is) `el`
      const topLevelElFor = (target: EventTarget | null): HTMLElement | null => {
        let el = target as HTMLElement | null;
        while (el && el.parentElement && el.parentElement !== canvasDoc.body) {
          el = el.parentElement;
        }
        return el && el !== canvasDoc.body ? el : null;
      };

      canvasDoc.addEventListener('dragover', (e: Event) => {
        const de = e as DragEvent;
        const types = de.dataTransfer?.types ?? [];
        const isBlock = types.includes('application/x-report-block');
        const isToken = !isBlock && types.includes('text/plain');
        if (!isBlock && !isToken) return;

        de.preventDefault();
        de.dataTransfer!.dropEffect = 'copy';

        if (isToken) {
          // Highlight target cell for token drops
          const el = dropTargetFor(de.target);
          if (el !== dragOverEl) {
            clearDragHighlight();
            if (el) {
              el.style.outline = '2px dashed #0d6efd';
              el.style.backgroundColor = 'rgba(13,110,253,0.08)';
              dragOverEl = el;
            }
          }
        } else {
          // Highlight top-level block for block drops
          const el = topLevelElFor(de.target);
          if (el !== dragOverEl) {
            clearDragHighlight();
            if (el) {
              el.style.outline = '2px solid #16a34a';
              el.style.backgroundColor = 'rgba(22,163,74,0.06)';
              dragOverEl = el;
            }
          }
        }
      }, true);

      canvasDoc.addEventListener('dragleave', (e: Event) => {
        const de = e as DragEvent;
        const related = de.relatedTarget as HTMLElement | null;
        if (!related || !canvasDoc.body?.contains(related)) clearDragHighlight();
      }, true);

      canvasDoc.addEventListener('drop', (e: Event) => {
        const de = e as DragEvent;
        clearDragHighlight();

        const text = de.dataTransfer?.getData('text/plain') ?? '';
        if (!text) return;

        const isBlock = de.dataTransfer?.types.includes('application/x-report-block') ?? false;

        de.preventDefault();
        de.stopPropagation();

        if (isBlock) {
          // Insert block HTML as a new element after the drop target
          const topEl = topLevelElFor(de.target);
          const wrapper = editor.getWrapper();
          if (topEl) {
            // Find this element's position in the wrapper's component children
            let insertAt = -1;
            wrapper?.components()?.each((c: any, idx: number) => {
              if (c.view?.el === topEl) insertAt = idx;
            });
            if (insertAt >= 0) {
              wrapper?.components().add(text, { at: insertAt + 1 });
            } else {
              wrapper?.append(text);
            }
          } else {
            wrapper?.append(text);
          }
          return;
        }

        // Token drop → replace target cell content
        if (!text.includes('{{')) return;
        const el = dropTargetFor(de.target);
        if (!el) return;
        const comp = findDeepComp(editor.DomComponents.getComponents(), el);
        if (comp) {
          comp.components().reset();
          comp.append(text);
        }
      }, true);
    });

    // Emit combined HTML+CSS whenever the content changes — but only when Design mode is active
    editor.on('change:changesCount', () => {
      if (!activeRef.current) return;
      const css = editor.getCss();
      const body = editor.getHtml();
      const combined = css ? `<style>${css}</style>\n${body}` : body;
      onChangeRef.current(combined);
    });

    onEditorReady?.(editor);
    gjsRef.current = editor;

    return () => {
      try { editor.destroy(); } catch { /* ignore */ }
      gjsRef.current = null;
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Sync HTML into GrapeJS only when Design mode becomes active or html changes while active.
  // Skipping this sync in Code mode prevents GrapeJS from reformatting Monaco's HTML.
  useEffect(() => {
    const ed = gjsRef.current;
    if (!ed || !active) return;
    const { css, body } = splitStyleAndBody(html);
    const currentBody = ed.getHtml() ?? '';
    if (currentBody.trim() !== body.trim()) {
      ed.setComponents(body);
      if (css) ed.setStyle(css);
    }
  }, [html, active]);

  return (
    <div
      ref={containerRef}
      className="w-full h-full"
      style={{ minHeight: 0 }}
    />
  );
};

export default GrapeJsEditor;
