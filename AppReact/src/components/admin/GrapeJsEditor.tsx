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
  useEffect(() => {
    // Set initial value once; React will never override it because it's not in the style prop.
    if (overlayRef.current) overlayRef.current.style.pointerEvents = 'none';

    const enable  = () => { if (overlayRef.current) overlayRef.current.style.pointerEvents = 'auto'; };
    const disable = () => { if (overlayRef.current) overlayRef.current.style.pointerEvents = 'none'; };
    document.addEventListener('dragstart', enable);
    document.addEventListener('dragend',   disable);
    document.addEventListener('drop',      disable);
    return () => {
      document.removeEventListener('dragstart', enable);
      document.removeEventListener('dragend',   disable);
      document.removeEventListener('drop',      disable);
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

    editor.on('load', () => {
      markEditable(editor.DomComponents.getComponents());
      setupCanvasListeners();
      const iframeEl: HTMLIFrameElement | undefined = editor.Canvas.getFrameEl?.();
      iframeEl?.addEventListener('load', setupCanvasListeners);
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

  // ── Overlay: parent-frame drag capture ────────────────────────────────────
  // The overlay is always mounted; pointer-events is toggled via direct DOM ref.
  // On dragover: use iframe.contentDocument.elementFromPoint to highlight the cell.
  // On drop: same lookup → GrapeJS getModelFromEl → replace cell content with token.

  const getCanvasIframe = (): HTMLIFrameElement | null =>
    containerRef.current?.querySelector('iframe') ?? null;

  const clearHighlight = (iframeDoc: Document) => {
    iframeDoc.querySelectorAll('[data-dh]').forEach(el => {
      (el as HTMLElement).style.outline = '';
      (el as HTMLElement).style.backgroundColor = '';
      (el as HTMLElement).removeAttribute('data-dh');
    });
  };

  const handleOverlayDragOver = (e: React.DragEvent) => {
    const types = Array.from(e.dataTransfer.types);
    if (!types.includes('text/plain') && !types.includes('application/x-report-block')) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = 'copy';

    const iframe = getCanvasIframe();
    if (!iframe?.contentDocument) return;
    const r = iframe.getBoundingClientRect();
    const target = walkUpToTextTag(
      iframe.contentDocument.elementFromPoint(e.clientX - r.left, e.clientY - r.top) as HTMLElement,
      iframe.contentDocument.body,
    );
    clearHighlight(iframe.contentDocument);
    if (target) {
      target.style.outline = '2px dashed #0d6efd';
      target.style.backgroundColor = 'rgba(13,110,253,0.08)';
      target.setAttribute('data-dh', '1');
    }
  };

  const handleOverlayDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (overlayRef.current) overlayRef.current.style.pointerEvents = 'none';

    const iframe = getCanvasIframe();
    if (iframe?.contentDocument) clearHighlight(iframe.contentDocument);

    const text = e.dataTransfer.getData('text/plain');
    if (!text) return;
    const isBlock = Array.from(e.dataTransfer.types).includes('application/x-report-block');

    const editor = gjsRef.current;
    if (!editor) return;

    if (isBlock) {
      editor.setComponents((editor.getHtml() ?? '') + '\n' + text);
      return;
    }

    if (!text.includes('{{')) return;

    let comp: any = null;

    if (iframe?.contentDocument) {
      const r = iframe.getBoundingClientRect();
      const el = walkUpToTextTag(
        iframe.contentDocument.elementFromPoint(e.clientX - r.left, e.clientY - r.top) as HTMLElement,
        iframe.contentDocument.body,
      );
      if (el) comp = editor.Canvas.getModelFromEl?.(el);
    }

    if (!comp) comp = editor.getSelected?.();
    if (comp) { comp.components().reset(); comp.append(text); }
  };

  const handleOverlayDragLeave = (e: React.DragEvent) => {
    if (e.relatedTarget && overlayRef.current?.contains(e.relatedTarget as Node)) return;
    const iframe = getCanvasIframe();
    if (iframe?.contentDocument) clearHighlight(iframe.contentDocument);
  };

  return (
    <div className="relative w-full h-full" style={{ minHeight: 0 }}>
      <div ref={containerRef} className="w-full h-full" />
      {/* Overlay: pointer-events controlled ONLY via overlayRef.current.style — NOT via
          the React style prop. If pointerEvents appeared in the style prop, React would
          re-apply 'none' on every parent re-render and race with enable(). */}
      <div
        ref={overlayRef}
        className="absolute inset-0"
        style={{ zIndex: 9999, cursor: 'copy' }}
        onDragOver={handleOverlayDragOver}
        onDrop={handleOverlayDrop}
        onDragLeave={handleOverlayDragLeave}
      />
    </div>
  );
};

export default GrapeJsEditor;
