import React, { forwardRef, useEffect, useImperativeHandle, useRef } from 'react';
import 'grapesjs/dist/css/grapes.min.css';

// eslint-disable-next-line @typescript-eslint/no-require-imports
const grapesjs = require('grapesjs');
// eslint-disable-next-line @typescript-eslint/no-require-imports
const grapesjsNewsletter = require('grapesjs-preset-newsletter');

interface GrapeJsEditorProps {
  html: string;
  isDark: boolean;
  active: boolean;
  onChange: (html: string) => void;
  onEditorReady?: (editor: any) => void;
}

export type GrapeJsEditorHandle = {
  dropToken: (token: string, clientX: number, clientY: number) => void;
};

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

const GrapeJsEditor = forwardRef<GrapeJsEditorHandle, GrapeJsEditorProps>(({
  html,
  isDark,
  active,
  onChange,
  onEditorReady,
}, ref) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const overlayRef   = useRef<HTMLDivElement>(null);
  const gjsRef       = useRef<any>(null);
  const onChangeRef  = useRef(onChange);
  const activeRef    = useRef(active);

  onChangeRef.current = onChange;
  activeRef.current   = active;

  // Expose dropToken to parent via ref — called from pointer-event handlers
  // on token buttons. Pointer events are NOT intercepted by iframes (unlike HTML5 DnD),
  // so the parent captures pointerup with client coordinates and calls this to insert.
  useImperativeHandle(ref, () => ({
    dropToken: (token: string, clientX: number, clientY: number) => {
      const editor = gjsRef.current;
      if (!editor) return;

      let comp: any = null;
      try {
        const iframes = Array.from(containerRef.current?.querySelectorAll<HTMLIFrameElement>('iframe') ?? []);
        const iframe = iframes
          .map(f => ({ f, r: f.getBoundingClientRect() }))
          .filter(({ r }) => r.width > 0 && r.height > 0)
          .sort((a, b) => (b.r.width * b.r.height) - (a.r.width * a.r.height))[0]?.f ?? null;

        if (iframe) {
          const r = iframe.getBoundingClientRect();
          const iDoc = iframe.contentDocument;
          if (iDoc) {
            const el = walkUpToTextTag(
              iDoc.elementFromPoint(clientX - r.left, clientY - r.top) as HTMLElement,
              iDoc.body,
            );
            if (el) comp = editor.Canvas.getModelFromEl?.(el);
          }
        }
      } catch { /* cross-origin guard */ }

      if (!comp) comp = editor.getSelected?.();
      if (comp) { comp.components().reset(); comp.append(token); }
    },
  }));

  // Blue-tint hint overlay — visual feedback only, no DnD logic needed here anymore.
  // Pointer events stay with the source button via setPointerCapture so no overlay wiring needed.
  useEffect(() => {
    if (overlayRef.current) {
      overlayRef.current.style.pointerEvents = 'none';
      overlayRef.current.style.background = '';
    }

    const showHint = () => {
      if (overlayRef.current) overlayRef.current.style.background = 'rgba(59,130,246,0.08)';
    };
    const hideHint = () => { if (overlayRef.current) overlayRef.current.style.background = ''; };
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
      pluginsOpts: { [grapesjsNewsletter]: { showBlocksOnLoad: 0 } },
      panels: { defaults: [] },
      deviceManager: { devices: [] },
    });

    // Blocks and tokens are managed by the left panel in ReportTemplateDesigner.
    // Do not register them with GrapeJS BlockManager — keeps the native blocks panel empty.

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
      const iframeEl = editor.Canvas.getFrameEl?.() as HTMLIFrameElement | undefined;
      iframeEl?.addEventListener('load', setupCanvasListeners);

      // Move outline/undo/redo from the now-hidden options panel into the right-side views panel.
      // Must happen before removeButton so the button definitions are still available.
      const moveToViews = new Set(['sw-visibility', 'undo', 'redo']);
      editor.Panels.getPanel('options')?.get('buttons')?.each((btn: any) => {
        if (moveToViews.has(btn.get('id'))) {
          editor.Panels.addButton('views', {
            id:         btn.get('id'),
            className:  btn.get('className') || '',
            command:    btn.get('command'),
            active:     btn.get('active'),
            label:      btn.get('label') || '',
            attributes: btn.get('attributes') || {},
          });
        }
      });

      // Remove the remaining redundant buttons from the (now hidden) options panel.
      ['preview', 'fullscreen', 'export-template', 'gjs-open-import-template', 'gjs-toggle-images', 'canvas-clear']
        .forEach(id => editor.Panels.removeButton('options', id));
      // Keep 'open-blocks' so GrapeJS can enforce radio-like exclusivity (SM vs Blocks)
      editor.Panels.removeButton('views', 'open-layers');
      ['set-device-desktop', 'set-device-tablet', 'set-device-mobile']
        .forEach(id => editor.Panels.removeButton('devices-c', id));

      // Open Style Manager as the default active right panel
      editor.runCommand('open-sm');

      // GrapeJS may set inline style="top:40px !important" on the canvas after init.
      // Inline !important beats author-stylesheet !important, so we must use
      // style.setProperty('prop', 'val', 'important') — the last JS write wins.
      // Double-rAF ensures we run after any GrapeJS post-load frame callbacks.
      requestAnimationFrame(() => requestAnimationFrame(() => {
        const canvasEl = (editor.Canvas.getElement?.() as HTMLElement | undefined)
                      ?? containerRef.current?.querySelector<HTMLElement>('.gjs-cv-canvas');
        const optEl    = (editor.Panels.getPanel('options')?.get?.('el') as HTMLElement | undefined)
                      ?? containerRef.current?.querySelector<HTMLElement>('.gjs-pn-options');
        const devEl    = (editor.Panels.getPanel('devices-c')?.get?.('el') as HTMLElement | undefined)
                      ?? containerRef.current?.querySelector<HTMLElement>('.gjs-pn-devices-c');
        const cmdEl    = (editor.Panels.getPanel('commands')?.get?.('el') as HTMLElement | undefined)
                      ?? containerRef.current?.querySelector<HTMLElement>('.gjs-pn-commands');

        canvasEl?.style.setProperty('top',    '0px', 'important');
        canvasEl?.style.setProperty('height', '100%', 'important');
        optEl?.style.setProperty('display', 'none', 'important');
        devEl?.style.setProperty('display', 'none', 'important');
        cmdEl?.style.setProperty('display', 'none', 'important');
      }));
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
      {/* Hide the top toolbar panels; expand canvas to fill freed vertical space.
          Rendered before useEffect so GrapeJS measures correct heights on init. */}
      <style>{`
        /* ── Layout overrides ── */
        .gjs-pn-options          { display: none !important; }
        .gjs-pn-devices-c        { display: none !important; }
        .gjs-pn-commands         { display: none !important; }
        .gjs-cv-canvas           { top: 0 !important; height: 100% !important; }
        .gjs-frame-wrapper__head { display: none !important; }
        .gjs-frame-wrapper       { margin-top: 0 !important; padding-top: 0 !important; }
        .gjs-canvas__frames      { top: 0 !important; }

        /* ── Light panel theme ── */
        /* Panel backgrounds */
        .gjs-pn-views,
        .gjs-pn-views-container,
        .gjs-one-bg { background-color: #f5f6f7 !important; }
        .gjs-two-bg  { background-color: #ebedf0 !important; }
        .gjs-three-bg { background-color: #dde1e7 !important; }
        .gjs-four-bg  { background-color: #f0f1f3 !important; }

        /* Panel text */
        .gjs-color-main,
        .gjs-pn-views .gjs-pn-btn,
        .gjs-label,
        .gjs-sm-sector__title,
        .gjs-sm-label,
        .gjs-clm-header,
        .gjs-traits-cs .gjs-label,
        .gjs-field { color: #333 !important; }

        /* Panel button icons (SVG/icon font) */
        .gjs-pn-views .gjs-pn-btn       { color: #555 !important; }
        .gjs-pn-views .gjs-pn-btn.gjs-pn-active { color: #1a73e8 !important; }

        /* Style Manager sectors */
        .gjs-sm-sector .gjs-sm-sector-title,
        .gjs-sm-sector__title { background: #ebedf0 !important; color: #333 !important; border-color: #dde1e7 !important; }
        .gjs-sm-properties    { background: #f5f6f7 !important; }

        /* Trait Manager */
        .gjs-trt-header  { color: #333 !important; background: #ebedf0 !important; }
        .gjs-trt-traits .gjs-label,
        .gjs-trt-trait .gjs-label,
        .gjs-label { color: #333 !important; }
        .gjs-trt-trait__wrp { border-color: #dde1e7 !important; }

        /* Fields & inputs inside panels */
        .gjs-field,
        .gjs-field select,
        .gjs-field input,
        .gjs-field textarea {
          background: #fff !important;
          color: #333 !important;
          border-color: #c4c8ce !important;
        }
        .gjs-field input::placeholder,
        .gjs-field textarea::placeholder { color: #aaa !important; opacity: 1 !important; }

        /* Block manager */
        .gjs-block-categories  { background: #f5f6f7 !important; }
        .gjs-block-category .gjs-title { background: #ebedf0 !important; color: #333 !important; border-color: #dde1e7 !important; }
        .gjs-block {
          background: #fff !important;
          color: #333 !important;
          border-color: #dde1e7 !important;
        }
        .gjs-block:hover       { background: #e8eaed !important; border-color: #adb5bd !important; }
        .gjs-block__media svg,
        .gjs-block__media i    { color: #555 !important; fill: #555 !important; }
        .gjs-block__label      { color: #444 !important; }

        /* Class manager */
        .gjs-clm-header,
        .gjs-clm-title         { background: #ebedf0 !important; color: #333 !important; }
        .gjs-clm-tags-field    { background: #fff !important; border-color: #c4c8ce !important; }
        .gjs-clm-tag           { background: #dde1e7 !important; color: #333 !important; }
        .gjs-clm-sels-info     { color: #555 !important; }
      `}</style>
      <div ref={containerRef} className="w-full h-full" />
      <div ref={overlayRef} className="absolute inset-0" style={{ pointerEvents: 'none', zIndex: 1 }} />
    </div>
  );
});

GrapeJsEditor.displayName = 'GrapeJsEditor';

export default GrapeJsEditor;
