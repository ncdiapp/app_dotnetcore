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
  active: boolean;
  onChange: (html: string) => void;
  onEditorReady?: (editor: any) => void;
}

function splitStyleAndBody(html: string): { css: string; body: string } {
  const styleMatch = html.match(/^<style>([\s\S]*?)<\/style>\s*/i);
  if (styleMatch) {
    return { css: styleMatch[1], body: html.slice(styleMatch[0].length) };
  }
  return { css: '', body: html };
}

const TEXT_TAGS = new Set(['TD','TH','P','H1','H2','H3','H4','H5','H6','SPAN','LI','A','LABEL']);

function walkUpToTextTag(el: HTMLElement | null, stopEl: HTMLElement | null): HTMLElement | null {
  let cur = el;
  while (cur && cur !== stopEl) {
    if (TEXT_TAGS.has(cur.tagName)) return cur;
    cur = cur.parentElement;
  }
  return null;
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
  const containerRef = useRef<HTMLDivElement>(null);
  const overlayRef   = useRef<HTMLDivElement>(null);
  const gjsRef       = useRef<any>(null);
  const onChangeRef  = useRef(onChange);
  const activeRef    = useRef(active);

  onChangeRef.current = onChange;
  activeRef.current   = active;

  // Toggle overlay pointer-events via direct DOM ref — never via React style prop.
  // If pointerEvents were in the JSX style prop, React would re-apply 'none' on every
  // re-render (e.g. when the parent's draggingToken state changes), racing with enable().
  // By keeping pointerEvents OUT of the style prop, React never touches it.
  // Browsers route drag events directly to <iframe> elements regardless of any covering div
  // with pointer-events:auto (iframe drag is special-cased by the browser's hit-test).
  // The fix: attach dragover/drop on the <iframe> element itself (in the parent document).
  // We also keep a subtle visual hint on the overlay during drag, but don't rely on it for events.
  useEffect(() => {
    if (overlayRef.current) {
      overlayRef.current.style.pointerEvents = 'none';
      overlayRef.current.style.background = '';
    }

    const showHint  = () => { if (overlayRef.current) overlayRef.current.style.background = 'rgba(59,130,246,0.08)'; };
    const hideHint  = () => { if (overlayRef.current) overlayRef.current.style.background = ''; };
    document.addEventListener('dragstart', showHint);
    document.addEventListener('dragend',   hideHint);
    document.addEventListener('drop',      hideHint);
    return () => {
      document.removeEventListener('dragstart', showHint);
      document.removeEventListener('dragend',   hideHint);
      document.removeEventListener('drop',      hideHint);
    };
  }, []);

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
      panels: { defaults: [] },
      deviceManager: { devices: [] },
    });

    blocks.forEach(block => {
      const slug = block.label.toLowerCase().replace(/\s+/g, '-');
      editor.BlockManager.add(slug, { label: block.label, category: 'Report Blocks', content: block.html });
    });

    tokens.forEach(tok => {
      const id = `tok-${tok.Token.replace(/[^a-z0-9]/gi, '_')}`;
      const label = tok.Field === '*' ? `#each ${tok.ResultSet}` : tok.Field;
      editor.BlockManager.add(id, { label, category: tok.ResultSet, content: `<span data-token="true">${tok.Token}</span>` });
    });

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

    // ── dblclick inline editing ────────────────────────────────────────────────
    let canvasListenersAbort: AbortController | null = null;
    let editGuard = false;

    const setupCanvasListeners = () => {
      const canvasDoc = editor.Canvas.getDocument?.();
      if (!canvasDoc) return;
      canvasListenersAbort?.abort();
      canvasListenersAbort = new AbortController();
      const { signal } = canvasListenersAbort;
      editGuard = false;

      try { if (canvasDoc.body) canvasDoc.body.style.background = isDark ? '#1e1e1e' : '#ffffff'; } catch { /* ignore */ }

      canvasDoc.addEventListener('dblclick', (e: Event) => {
        if (editGuard) return;
        const el = walkUpToTextTag(e.target as HTMLElement, canvasDoc.body);
        if (!el) return;
        e.stopPropagation();

        const snapshot = el.innerHTML;
        el.setAttribute('contenteditable', 'true');
        el.style.outline = '2px solid #0d6efd';
        el.style.outlineOffset = '2px';
        el.style.cursor = 'text';
        el.focus();

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
          const comp = editor.Canvas.getModelFromEl?.(el);
          if (comp) { comp.components().reset(); comp.append(newHtml); }
        };
        el.addEventListener('blur', () => finish(false), { once: true });
        el.addEventListener('keydown', (ke: KeyboardEvent) => {
          if (ke.key === 'Escape') { ke.stopPropagation(); finish(true); }
          else if (ke.key === 'Enter' && !ke.shiftKey) { ke.preventDefault(); finish(false); }
        }, { once: true });
      }, { capture: true, signal });
    };

    // Browsers send drag events directly to <iframe> elements even when a covering div
    // has pointer-events:auto. Attaching to the iframe element in the parent document
    // is the only reliable way to intercept drops onto the GrapeJS canvas.
    const setupIframeDragDrop = (iframeEl: HTMLIFrameElement) => {
      const onDragOver = (e: DragEvent) => {
        if (!Array.from(e.dataTransfer?.types ?? []).includes('text/plain')) return;
        e.preventDefault();
        if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';
      };
      const onDrop = (e: DragEvent) => {
        e.preventDefault();
        const text = e.dataTransfer?.getData('text/plain') ?? '';
        if (!text.includes('{{')) return;

        // Prefer the cell under the cursor; fall back to the currently selected component.
        let comp: any = null;
        try {
          const r = iframeEl.getBoundingClientRect();
          const iframeDoc = iframeEl.contentDocument;
          if (iframeDoc) {
            const el = walkUpToTextTag(
              iframeDoc.elementFromPoint(e.clientX - r.left, e.clientY - r.top) as HTMLElement,
              iframeDoc.body,
            );
            if (el) comp = editor.Canvas.getModelFromEl?.(el);
          }
        } catch { /* cross-origin or sandbox — fall through to getSelected */ }

        if (!comp) comp = editor.getSelected?.();
        if (comp) { comp.components().reset(); comp.append(text); }
      };
      iframeEl.addEventListener('dragover', onDragOver);
      iframeEl.addEventListener('drop', onDrop);
    };

    editor.on('load', () => {
      markEditable(editor.DomComponents.getComponents());
      setupCanvasListeners();
      const iframeEl: HTMLIFrameElement | undefined = editor.Canvas.getFrameEl?.();
      if (iframeEl) {
        iframeEl.addEventListener('load', setupCanvasListeners);
        setupIframeDragDrop(iframeEl);
      }
    });

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
      canvasListenersAbort?.abort();
      try { editor.destroy(); } catch { /* ignore */ }
      gjsRef.current = null;
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

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
    <div className="relative w-full h-full" style={{ minHeight: 0 }}>
      <div ref={containerRef} className="w-full h-full" />
      {/* Visual-only hint overlay — no pointer-events; drag events go to the iframe element directly */}
      <div ref={overlayRef} className="absolute inset-0" style={{ pointerEvents: 'none', zIndex: 1 }} />
    </div>
  );
};

export default GrapeJsEditor;
