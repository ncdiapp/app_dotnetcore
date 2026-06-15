import React, { useMemo } from 'react';
import ReactQuill from 'react-quill';
import 'react-quill/dist/quill.snow.css';
import { useTheme } from '../../redux/hooks/useTheme';

const FONT_OPTIONS = [
  'arial',
  'calibri',
  'comic-sans',
  'courier-new',
  'georgia',
  'helvetica',
  'impact',
  'lucida',
  'roboto',
  'tahoma',
  'times-new-roman',
  'trebuchet-ms',
  'verdana',
  'monospace',
] as const;

const SIZE_OPTIONS = [
  '10px',
  '11px',
  '12px',
  '14px',
  '16px',
  '18px',
  '20px',
  '24px',
  '28px',
  '32px',
  '36px',
  '48px',
] as const;

// Quill: expand font/size options (whitelist).
// Must be registered before editor mounts; safe to do at module load.
// eslint-disable-next-line @typescript-eslint/no-var-requires
const Quill = (ReactQuill as any).Quill;
if (Quill) {
  const Font = Quill.import('formats/font');
  Font.whitelist = [...FONT_OPTIONS];
  Quill.register(Font, true);

  const Size = Quill.import('attributors/style/size');
  Size.whitelist = [...SIZE_OPTIONS];
  Quill.register(Size, true);
}

type RichTextEditorProps = {
  value: string;
  onChange: (html: string) => void;
  className?: string;
  readOnly?: boolean;
};

const DEFAULT_MODULES = {
  toolbar: [
    [{ font: [...FONT_OPTIONS] }, { size: [...SIZE_OPTIONS] }],
    ['bold', 'italic', 'underline', 'strike'],
    [{ color: [] }, { background: [] }],
    [{ align: [] }],
    [{ list: 'ordered' }, { list: 'bullet' }, { indent: '-1' }, { indent: '+1' }],
    ['link', 'image'],
  ],
};

const DEFAULT_FORMATS = [
  'font',
  'size',
  'bold',
  'italic',
  'underline',
  'strike',
  'color',
  'background',
  'align',
  'list',
  'bullet',
  'indent',
  'link',
  'image',
];

const RichTextEditor: React.FC<RichTextEditorProps> = ({ value, onChange, className, readOnly }) => {
  const { theme } = useTheme();

  const css = useMemo(() => {
    const p: any = (theme as any)?.param ?? {};
    const bg = p.bg_input_box || '';
    const text = p.text_input_box || p.wijmo_grid_default_text_color || '';
    const border = p.border_input_box || '';
    const title = p.text_title || p.text_mainContentSection || '';
    const label = p.text_label || title || '';
    const headerBg = p.bg_mainContentSection || bg || '';

    const fontLabel: Record<(typeof FONT_OPTIONS)[number], string> = {
      arial: 'Arial',
      calibri: 'Calibri',
      'comic-sans': 'Comic Sans',
      'courier-new': 'Courier New',
      georgia: 'Georgia',
      helvetica: 'Helvetica',
      impact: 'Impact',
      lucida: 'Lucida',
      roboto: 'Roboto',
      tahoma: 'Tahoma',
      'times-new-roman': 'Times New Roman',
      'trebuchet-ms': 'Trebuchet MS',
      verdana: 'Verdana',
      monospace: 'Monospace',
    };

    const fontFamilyCss: Record<(typeof FONT_OPTIONS)[number], string> = {
      arial: 'Arial, Helvetica, sans-serif',
      calibri: 'Calibri, Arial, sans-serif',
      'comic-sans': '"Comic Sans MS", "Comic Sans", cursive',
      'courier-new': '"Courier New", Courier, monospace',
      georgia: 'Georgia, serif',
      helvetica: 'Helvetica, Arial, sans-serif',
      impact: 'Impact, Haettenschweiler, "Arial Narrow Bold", sans-serif',
      lucida: '"Lucida Sans Unicode", "Lucida Grande", sans-serif',
      roboto: 'Roboto, Arial, sans-serif',
      tahoma: 'Tahoma, Arial, sans-serif',
      'times-new-roman': '"Times New Roman", Times, serif',
      'trebuchet-ms': '"Trebuchet MS", Arial, sans-serif',
      verdana: 'Verdana, Arial, sans-serif',
      monospace: 'monospace',
    };

    const fontPickerCss = FONT_OPTIONS.map((f) => {
      const labelText = fontLabel[f];
      const fam = fontFamilyCss[f];
      // Show readable labels in picker + preview the font.
      return `
        .appai-rte .ql-snow .ql-picker.ql-font .ql-picker-item[data-value="${f}"]::before,
        .appai-rte .ql-snow .ql-picker.ql-font .ql-picker-label[data-value="${f}"]::before { content: "${labelText}"; }
        .appai-rte .ql-font-${f} { font-family: ${fam}; }
      `;
    }).join('\n');

    const sizePickerCss = SIZE_OPTIONS.map((s) => {
      const labelText = s.replace('px', '');
      return `
        .appai-rte .ql-snow .ql-picker.ql-size .ql-picker-item[data-value="${s}"]::before,
        .appai-rte .ql-snow .ql-picker.ql-size .ql-picker-label[data-value="${s}"]::before { content: "${labelText}px"; }
      `;
    }).join('\n');

    // No hardcoded colors: only apply rules when theme tokens exist.
    return `
      .appai-rte .ql-toolbar,
      .appai-rte .ql-container {
        box-sizing: border-box;
      }

      /* Critical: keep Quill inside flex container (no overflow past parent) */
      .appai-rte .ql-toolbar.ql-snow {
        flex: 0 0 auto;
      }
      .appai-rte .ql-container.ql-snow {
        flex: 1 1 auto;
        min-height: 0;
        height: auto;
      }
      .appai-rte .ql-editor {
        min-height: 0;
        height: 100%;
      }

      .appai-rte .ql-toolbar.ql-snow {
        ${headerBg ? `background: ${headerBg} !important;` : ''}
        ${border ? `border: 1px solid ${border} !important;` : ''}
        border-radius: 4px 4px 0 0;
      }
      .appai-rte .ql-container.ql-snow {
        ${border ? `border: 1px solid ${border} !important;` : ''}
        border-top: none !important;
        border-radius: 0 0 4px 4px;
      }
      .appai-rte .ql-editor {
        ${bg ? `background: ${bg} !important;` : ''}
        ${text ? `color: ${text} !important;` : ''}
        font-size: 12px;
      }
      .appai-rte .ql-editor.ql-blank::before {
        ${label ? `color: ${label} !important;` : ''}
        opacity: 0.8;
        font-style: normal;
      }
      .appai-rte .ql-toolbar button,
      .appai-rte .ql-toolbar .ql-picker-label,
      .appai-rte .ql-toolbar .ql-picker-item {
        ${title ? `color: ${title} !important;` : ''}
      }
      .appai-rte .ql-toolbar .ql-stroke {
        ${title ? `stroke: ${title} !important;` : ''}
      }
      .appai-rte .ql-toolbar .ql-fill {
        ${title ? `fill: ${title} !important;` : ''}
      }

      /* Quill picker labels for custom font/size options */
      ${fontPickerCss}
      ${sizePickerCss}
    `;
  }, [theme]);

  return (
    <div className={`appai-rte w-full h-full min-h-0 flex flex-col ${className || ''}`}>
      <style>{css}</style>
      <div className="w-full min-h-0 flex-auto overflow-hidden">
        <ReactQuill
          theme="snow"
          value={value}
          onChange={onChange}
          modules={DEFAULT_MODULES as any}
          formats={DEFAULT_FORMATS}
          readOnly={!!readOnly}
          className="h-full w-full flex flex-col min-h-0"
        />
      </div>
    </div>
  );
};

export default RichTextEditor;

