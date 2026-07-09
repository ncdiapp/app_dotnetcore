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

    // Apply canvas background based on app theme
    try {
      const canvasDoc = editor.Canvas.getDocument();
      if (canvasDoc?.body) {
        canvasDoc.body.style.background = isDark ? '#1e1e1e' : '#ffffff';
      }
    } catch {
      // canvas may not be ready yet — harmless
    }

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

    // ── Make text-container elements editable ───────────────────────────────
    // Newsletter preset does not set editable:true on td/th/p/headings.
    const EDITABLE_TAGS = new Set(['td', 'th', 'p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'span', 'li', 'a', 'div', 'label']);
    const markEditable = (comps: any) => {
      comps?.each?.((c: any) => {
        if (EDITABLE_TAGS.has((c.get('tagName') ?? '').toLowerCase())) {
          c.set('editable', true);
        }
        markEditable(c.components?.());
      });
    };
    editor.on('load', () => markEditable(editor.DomComponents.getComponents()));
    editor.on('component:add', (c: any) => {
      if (EDITABLE_TAGS.has((c.get('tagName') ?? '').toLowerCase())) c.set('editable', true);
      markEditable(c.components?.());
    });

    // ── Direct double-click-to-edit ──────────────────────────────────────────
    // GrapeJS RTE only fires on dblclick for an already-selected component.
    // We intercept dblclick on the canvas, select the deepest matching text
    // component, then re-dispatch dblclick so GrapeJS's own handler activates.
    const TEXT_TAGS = new Set(['TD','TH','P','H1','H2','H3','H4','H5','H6','SPAN','LI','A','LABEL','DIV']);
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

    try {
      editor.Canvas.getDocument().addEventListener('dblclick', (e: Event) => {
        if (editGuard) return;
        let el: HTMLElement | null = e.target as HTMLElement;
        while (el && el.tagName !== 'BODY') {
          if (TEXT_TAGS.has(el.tagName)) break;
          el = el.parentElement;
        }
        if (!el || el.tagName === 'BODY') return;

        const comp = findDeepComp(editor.DomComponents.getComponents(), el);
        if (!comp) return;
        comp.set('editable', true);
        editor.select(comp);

        // Re-fire dblclick after GrapeJS processes the selection so its RTE activates
        editGuard = true;
        setTimeout(() => {
          const viewEl: HTMLElement | undefined = comp.view?.el ?? comp.getView?.()?.el;
          viewEl?.dispatchEvent(new MouseEvent('dblclick', { bubbles: true, cancelable: true }));
          setTimeout(() => { editGuard = false; }, 150);
        }, 50);
      }, true);
    } catch { /* canvas may not be ready */ }

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
