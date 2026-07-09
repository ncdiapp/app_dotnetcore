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

  useEffect(() => { onChangeRef.current = onChange; }, [onChange]);
  useEffect(() => { activeRef.current = active; }, [active]);
  useEffect(() => { htmlRef.current = html; }, [html]);

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
