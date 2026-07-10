import React, { Suspense, useCallback, useEffect, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { JsonCodeEditor } from '../common/JsonCodeEditor';
import { appReportSvc } from '../../webapi/appReportSvc';
import DataSourceEditor, {
  DataSourceDef,
  parseSourcesFromConfig,
  serializeConfig,
} from './DataSourceEditor';
import { RootState } from '../../redux/store';

import type { GrapeJsEditorHandle } from './GrapeJsEditor';

const GrapeJsEditor = React.lazy(() => import('./GrapeJsEditor'));

interface Props {
  reportId: number;
  mainReferenceId?: number;
  onSaved?: () => void;
  onClose?: () => void;
}

interface TokenDescriptor {
  Token: string;
  ResultSet: string;
  Field: string;
  IsList: boolean;
  InsideEach: boolean;
}

const PAGE_SIZES = ['A4', 'Letter', 'A3', 'Legal'];
const ORIENTATIONS = ['portrait', 'landscape'];

// ── Pre-built HTML snippets ──────────────────────────────────────────────────
const REPORT_BLOCKS = [
  {
    label: 'Report Header',
    icon: 'fa-solid fa-heading',
    color: 'text-blue-500',
    html: `<div style="margin-bottom:24px;padding-bottom:12px;border-bottom:2px solid #1d4ed8">
  <h1 style="color:#1d4ed8;font-size:22px;font-family:Arial,sans-serif;margin:0 0 4px 0">{{header.Title}}</h1>
  <div style="color:#555;font-size:12px;font-family:Arial,sans-serif">
    <strong>{{header.Field1}}</strong> &nbsp;|&nbsp; {{header.Field2}} &nbsp;|&nbsp; {{header.Field3}}
  </div>
</div>`,
  },
  {
    label: 'Info Grid (key-value)',
    icon: 'fa-solid fa-table-cells',
    color: 'text-green-500',
    html: `<table style="width:100%;border-collapse:collapse;font-family:Arial,sans-serif;font-size:12px;margin-bottom:16px">
  <tr>
    <td style="width:140px;color:#555;padding:4px 8px 4px 0;font-weight:bold">Field Label</td>
    <td style="padding:4px 0;color:#222">{{header.FieldName}}</td>
    <td style="width:140px;color:#555;padding:4px 8px 4px 16px;font-weight:bold">Field Label</td>
    <td style="padding:4px 0;color:#222">{{header.FieldName}}</td>
  </tr>
  <tr>
    <td style="width:140px;color:#555;padding:4px 8px 4px 0;font-weight:bold">Field Label</td>
    <td style="padding:4px 0;color:#222">{{header.FieldName}}</td>
    <td style="width:140px;color:#555;padding:4px 8px 4px 16px;font-weight:bold">Field Label</td>
    <td style="padding:4px 0;color:#222">{{header.FieldName}}</td>
  </tr>
</table>`,
  },
  {
    label: 'Data Table',
    icon: 'fa-solid fa-table',
    color: 'text-purple-500',
    html: `<h3 style="font-family:Arial,sans-serif;font-size:14px;color:#1d4ed8;margin:16px 0 6px 0">Section Title</h3>
<table style="width:100%;border-collapse:collapse;font-family:Arial,sans-serif;font-size:12px;margin-bottom:16px">
  <thead>
    <tr>
      <th style="background:#1d4ed8;color:white;padding:6px 10px;text-align:left">Column 1</th>
      <th style="background:#1d4ed8;color:white;padding:6px 10px;text-align:left">Column 2</th>
      <th style="background:#1d4ed8;color:white;padding:6px 10px;text-align:left">Column 3</th>
    </tr>
  </thead>
  <tbody>
    {{#each header_rs1}}
    <tr>
      <td style="padding:5px 10px;border-bottom:1px solid #e5e7eb">{{Field1}}</td>
      <td style="padding:5px 10px;border-bottom:1px solid #e5e7eb">{{Field2}}</td>
      <td style="padding:5px 10px;border-bottom:1px solid #e5e7eb">{{Field3}}</td>
    </tr>
    {{/each}}
  </tbody>
</table>`,
  },
  {
    label: 'Section Title',
    icon: 'fa-solid fa-minus',
    color: 'text-orange-400',
    html: `<div style="margin:20px 0 8px 0;padding-bottom:4px;border-bottom:1px solid #e5e7eb">
  <h3 style="font-family:Arial,sans-serif;font-size:14px;font-weight:bold;color:#374151;margin:0">Section Title</h3>
</div>`,
  },
  {
    label: 'Text Paragraph',
    icon: 'fa-solid fa-align-left',
    color: 'text-gray-500',
    html: `<p style="font-family:Arial,sans-serif;font-size:12px;color:#374151;margin:8px 0;line-height:1.6">
  {{header.Description}}
</p>`,
  },
  {
    label: 'Image',
    icon: 'fa-solid fa-image',
    color: 'text-pink-400',
    html: `<div style="text-align:center;margin:12px 0">
  <img src="{{header.ImageUrl}}" alt="" style="max-width:200px;max-height:200px;border:1px solid #e5e7eb" />
</div>`,
  },
  {
    label: 'Page Styles',
    icon: 'fa-solid fa-palette',
    color: 'text-indigo-500',
    html: `<style>
  body { font-family: Arial, sans-serif; color: #222; padding: 0; margin: 0; }
  h1 { color: #1d4ed8; font-size: 22px; }
  h3 { color: #374151; font-size: 14px; }
  table { border-collapse: collapse; width: 100%; }
  th { background: #1d4ed8; color: white; padding: 6px 10px; text-align: left; }
  td { padding: 5px 10px; border-bottom: 1px solid #e5e7eb; font-size: 12px; }
  tr:nth-child(even) td { background: #f8faff; }
</style>`,
  },
];

const STARTER_TEMPLATES: { label: string; icon: string; html: string; isAbsolute?: boolean; tip?: string }[] = [
  {
    label: 'Style Summary',
    icon: '👗',
    html: `<style>
  body{font-family:Arial,sans-serif;color:#222;padding:24px}
  h1{color:#1d4ed8;font-size:22px;border-bottom:2px solid #1d4ed8;padding-bottom:8px;margin-bottom:4px}
  .meta{font-size:11px;color:#555;margin:4px 0 16px}
  h3{font-size:13px;color:#374151;border-bottom:1px solid #e5e7eb;padding-bottom:3px;margin:16px 0 6px}
  table{border-collapse:collapse;width:100%;font-size:12px}
  th{background:#1d4ed8;color:white;padding:6px 10px;text-align:left}
  td{padding:5px 10px;border-bottom:1px solid #e5e7eb}
  tr:nth-child(even) td{background:#f0f4ff}
</style>
<h1>{{header.StyleNumber}}</h1>
<div class="meta">
  <strong>{{header.Brand}}</strong> &nbsp;|&nbsp; Season: {{header.Season}} &nbsp;|&nbsp; Gender: {{header.Gender}} &nbsp;|&nbsp; Printed: {{header.PrintedAt}}
</div>
<p style="font-size:12px;color:#444;margin:0 0 16px">{{header.Description}}</p>

<h3>Fabric Composition</h3>
<table>
  <tr><th>Pos</th><th>Code</th><th>Description</th><th>Colour</th></tr>
  {{#each header_rs1}}<tr><td>{{Position}}</td><td>{{Code}}</td><td>{{Description}}</td><td>{{Colour}}</td></tr>{{/each}}
</table>`,
  },
  {
    label: 'Order Detail',
    icon: '📦',
    html: `<style>
  body{font-family:Arial,sans-serif;color:#222;padding:24px}
  h1{color:#1d4ed8;font-size:22px;margin:0 0 4px}
  .info-grid{display:grid;grid-template-columns:1fr 1fr;gap:4px 24px;font-size:12px;margin:12px 0 20px}
  .info-grid label{color:#555;font-weight:bold}
  h3{font-size:13px;color:#374151;border-bottom:1px solid #e5e7eb;padding-bottom:3px;margin:16px 0 6px}
  table{border-collapse:collapse;width:100%;font-size:12px}
  th{background:#1d4ed8;color:white;padding:6px 10px;text-align:left}
  td{padding:5px 10px;border-bottom:1px solid #e5e7eb}
  .total{font-weight:bold;background:#f0f4ff}
</style>
<h1>Order: {{header.OrderNumber}}</h1>
<div class="info-grid">
  <div><label>Customer:</label> {{header.CustomerName}}</div>
  <div><label>Date:</label> {{header.OrderDate}}</div>
  <div><label>Status:</label> {{header.Status}}</div>
  <div><label>Currency:</label> {{header.Currency}}</div>
</div>

<h3>Order Lines</h3>
<table>
  <tr><th>SKU</th><th>Description</th><th>Qty</th><th>Unit Price</th><th>Total</th></tr>
  {{#each header_rs1}}<tr><td>{{Sku}}</td><td>{{Description}}</td><td>{{Quantity}}</td><td>{{UnitPrice}}</td><td>{{Total}}</td></tr>{{/each}}
  <tr class="total"><td colspan="4" style="text-align:right;padding:6px 10px">Order Total:</td><td>{{header.OrderTotal}}</td></tr>
</table>`,
  },
  {
    label: 'Simple List',
    icon: '📋',
    html: `<style>
  body{font-family:Arial,sans-serif;color:#222;padding:24px}
  h1{color:#1d4ed8;font-size:20px;border-bottom:2px solid #1d4ed8;padding-bottom:6px}
  .meta{font-size:11px;color:#888;margin:4px 0 20px}
  table{border-collapse:collapse;width:100%;font-size:12px}
  th{background:#374151;color:white;padding:6px 10px;text-align:left}
  td{padding:5px 10px;border-bottom:1px solid #e5e7eb}
  tr:hover td{background:#f9fafb}
</style>
<h1>{{header.Title}}</h1>
<div class="meta">Generated: {{header.PrintedAt}}</div>
<table>
  <tr><th>Field 1</th><th>Field 2</th><th>Field 3</th></tr>
  {{#each header_rs1}}<tr><td>{{Field1}}</td><td>{{Field2}}</td><td>{{Field3}}</td></tr>{{/each}}
</table>`,
  },

  // ── Canva-style fixed-layout templates ────────────────────────────────────
  {
    label: 'Cover Page',
    icon: '🎨',
    isAbsolute: true,
    tip: 'Fixed-size canvas (760×1050 px). Elements are pixel-positioned — great for decorative covers, posters, and slides. Not suitable for repeating data rows.',
    html: `<style>*{box-sizing:border-box;margin:0;padding:0}body{font-family:Arial,sans-serif}</style>
<div style="position:relative;width:760px;height:1050px;background:#0f172a;overflow:hidden">
  <div style="position:absolute;top:0;left:0;width:280px;height:100%;background:#1e3a8a"></div>
  <div style="position:absolute;top:-120px;right:-120px;width:420px;height:420px;border-radius:50%;background:#2563eb;opacity:.12"></div>
  <div style="position:absolute;bottom:-80px;left:160px;width:260px;height:260px;border-radius:50%;background:#3b82f6;opacity:.15"></div>
  <div style="position:absolute;top:56px;left:32px">
    <div style="color:#93c5fd;font-size:9px;letter-spacing:3px;text-transform:uppercase;margin-bottom:8px">Report</div>
    <div style="color:white;font-size:22px;font-weight:bold">{{header.Brand}}</div>
  </div>
  <div style="position:absolute;top:180px;left:32px;width:220px">
    <div style="color:#93c5fd;font-size:9px;letter-spacing:2px;text-transform:uppercase;margin-bottom:8px">Style</div>
    <div style="color:white;font-size:38px;font-weight:900;line-height:1.1">{{header.StyleNumber}}</div>
    <div style="color:#cbd5e1;font-size:11px;margin-top:10px;line-height:1.5">{{header.Description}}</div>
  </div>
  <div style="position:absolute;bottom:80px;left:32px">
    <table style="border-collapse:collapse;color:white;font-size:11px">
      <tr><td style="color:#93c5fd;padding-right:16px;padding-bottom:6px">Season</td><td style="padding-bottom:6px">{{header.Season}}</td></tr>
      <tr><td style="color:#93c5fd;padding-right:16px;padding-bottom:6px">Gender</td><td style="padding-bottom:6px">{{header.Gender}}</td></tr>
      <tr><td style="color:#93c5fd;padding-right:16px">Country</td><td>{{header.Country}}</td></tr>
    </table>
  </div>
  <div style="position:absolute;top:56px;left:312px;width:400px;height:320px;background:#1e293b;border-radius:8px;overflow:hidden;display:flex;align-items:center;justify-content:center">
    <img src="{{header.ImageUrl}}" style="max-width:380px;max-height:300px;object-fit:contain" alt="" onerror="this.style.display='none'"/>
  </div>
  <div style="position:absolute;top:400px;left:312px;width:188px;height:100px;background:#1e293b;border-radius:8px;padding:16px">
    <div style="color:#64748b;font-size:9px;text-transform:uppercase;letter-spacing:1px">Supplier</div>
    <div style="color:#e2e8f0;font-size:13px;font-weight:bold;margin-top:6px">{{header.Supplier}}</div>
  </div>
  <div style="position:absolute;top:400px;left:514px;width:188px;height:100px;background:#1e293b;border-radius:8px;padding:16px">
    <div style="color:#64748b;font-size:9px;text-transform:uppercase;letter-spacing:1px">Category</div>
    <div style="color:#e2e8f0;font-size:13px;font-weight:bold;margin-top:6px">{{header.Category}}</div>
  </div>
  <div style="position:absolute;top:514px;left:312px;width:188px;height:100px;background:#1e293b;border-radius:8px;padding:16px">
    <div style="color:#64748b;font-size:9px;text-transform:uppercase;letter-spacing:1px">Status</div>
    <div style="color:#e2e8f0;font-size:13px;font-weight:bold;margin-top:6px">{{header.Status}}</div>
  </div>
  <div style="position:absolute;top:514px;left:514px;width:188px;height:100px;background:#1e293b;border-radius:8px;padding:16px">
    <div style="color:#64748b;font-size:9px;text-transform:uppercase;letter-spacing:1px">Colour</div>
    <div style="color:#e2e8f0;font-size:13px;font-weight:bold;margin-top:6px">{{header.Colour}}</div>
  </div>
  <div style="position:absolute;bottom:32px;left:312px;right:40px;display:flex;justify-content:space-between">
    <div style="color:#475569;font-size:10px">{{header.PrintedAt}}</div>
    <div style="color:#475569;font-size:10px">Confidential</div>
  </div>
</div>`,
  },
  {
    label: 'Hang Tag',
    icon: '🏷️',
    isAbsolute: true,
    tip: 'Fixed-size label (240×360 px per tag). Each tag is pixel-positioned — ideal for product labels, hang tags, and badges printed on label paper.',
    html: `<style>*{box-sizing:border-box;margin:0;padding:0}body{font-family:Arial,sans-serif}</style>
<div style="position:relative;width:240px;height:360px;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;background:white;box-shadow:0 2px 8px rgba(0,0,0,.08)">
  <div style="position:absolute;top:0;left:0;right:0;height:110px;background:#1d4ed8"></div>
  <div style="position:absolute;top:10px;left:50%;transform:translateX(-50%);width:18px;height:18px;border-radius:50%;background:rgba(255,255,255,.3);border:2px solid rgba(255,255,255,.6)"></div>
  <div style="position:absolute;top:36px;left:0;right:0;text-align:center;color:white;font-size:10px;font-weight:bold;letter-spacing:3px;text-transform:uppercase">{{header.Brand}}</div>
  <div style="position:absolute;top:72px;left:50%;transform:translateX(-50%);width:96px;height:96px;border-radius:50%;background:white;border:3px solid white;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.15)">
    <img src="{{header.ImageUrl}}" style="width:100%;height:100%;object-fit:cover" alt="" onerror="this.style.display='none'"/>
  </div>
  <div style="position:absolute;top:184px;left:0;right:0;text-align:center">
    <div style="font-size:16px;font-weight:bold;color:#111">{{header.StyleNumber}}</div>
    <div style="font-size:10px;color:#6b7280;margin-top:3px">{{header.Description}}</div>
  </div>
  <div style="position:absolute;top:238px;left:20px;right:20px;height:1px;background:#f0f0f0"></div>
  <div style="position:absolute;top:252px;left:0;right:0;display:flex;justify-content:space-around;text-align:center">
    <div>
      <div style="font-size:8px;color:#9ca3af;text-transform:uppercase;letter-spacing:.5px">Season</div>
      <div style="font-size:10px;font-weight:600;color:#374151;margin-top:2px">{{header.Season}}</div>
    </div>
    <div>
      <div style="font-size:8px;color:#9ca3af;text-transform:uppercase;letter-spacing:.5px">Gender</div>
      <div style="font-size:10px;font-weight:600;color:#374151;margin-top:2px">{{header.Gender}}</div>
    </div>
    <div>
      <div style="font-size:8px;color:#9ca3af;text-transform:uppercase;letter-spacing:.5px">Colour</div>
      <div style="font-size:10px;font-weight:600;color:#374151;margin-top:2px">{{header.Colour}}</div>
    </div>
  </div>
  <div style="position:absolute;bottom:14px;left:0;right:0;text-align:center">
    <div style="font-size:9px;color:#9ca3af;font-family:monospace;letter-spacing:5px">{{header.Barcode}}</div>
  </div>
</div>`,
  },
];

// ── Page Style ───────────────────────────────────────────────────────────────

interface PageStyleValues {
  fontFamily: string;
  fontSize: number;
  textColor: string;
  primaryColor: string;
  altRowColor: string;
  borderColor: string;
}

const DEFAULT_PAGE_STYLE: PageStyleValues = {
  fontFamily: 'Arial, sans-serif',
  fontSize: 12,
  textColor: '#222222',
  primaryColor: '#1d4ed8',
  altRowColor: '#f8faff',
  borderColor: '#e5e7eb',
};

const FONT_FAMILIES = [
  { label: 'Arial',           value: 'Arial, sans-serif' },
  { label: 'Helvetica Neue',  value: "'Helvetica Neue', Helvetica, sans-serif" },
  { label: 'Calibri',         value: "Calibri, 'Gill Sans', sans-serif" },
  { label: 'Trebuchet MS',    value: "'Trebuchet MS', sans-serif" },
  { label: 'Georgia',         value: 'Georgia, serif' },
  { label: 'Times New Roman', value: "'Times New Roman', Times, serif" },
];

function generatePageCss(s: PageStyleValues): string {
  const h1Size = Math.round(s.fontSize * 1.75);
  const h2Size = Math.round(s.fontSize * 1.4);
  const h3Size = Math.round(s.fontSize * 1.15);
  return [
    `body{font-family:${s.fontFamily};font-size:${s.fontSize}px;color:${s.textColor};padding:0;margin:0}`,
    `h1{color:${s.primaryColor};font-size:${h1Size}px}`,
    `h2{color:${s.primaryColor};font-size:${h2Size}px}`,
    `h3{color:${s.textColor};font-size:${h3Size}px}`,
    `table{border-collapse:collapse;width:100%}`,
    `th{background:${s.primaryColor};color:white;padding:6px 10px;text-align:left;font-size:${s.fontSize}px}`,
    `td{padding:5px 10px;border-bottom:1px solid ${s.borderColor};font-size:${s.fontSize}px}`,
    `tr:nth-child(even) td{background:${s.altRowColor}}`,
  ].join('\n');
}

// ────────────────────────────────────────────────────────────────────────────

const ReportTemplateDesigner: React.FC<Props> = ({ reportId, mainReferenceId = 0, onSaved, onClose }) => {
  const { theme, t } = useTheme();

  const [report, setReport]                 = useState<any>(null);
  const [templateHtml, setTemplateHtml]     = useState('');
  const [dataSources, setDataSources]       = useState<DataSourceDef[]>([{ name: 'header', type: 'sp', value: '' }]);
  const [pageSize, setPageSize]             = useState('A4');
  const [orientation, setOrientation]       = useState('portrait');
  const [marginMm, setMarginMm]             = useState(15);
  const [extraParamConfig, setExtraParamConfig] = useState('');

  const [tokens, setTokens]                 = useState<TokenDescriptor[]>([]);
  const [previewHtml, setPreviewHtml]       = useState('');
  const [previewRefId, setPreviewRefId]     = useState(mainReferenceId.toString());
  const [loading, setLoading]               = useState(true);
  const [saving, setSaving]                 = useState(false);
  const [tokenLoading, setTokenLoading]     = useState(false);
  const [error, setError]                   = useState<string | null>(null);
  const [leftTab, setLeftTab]               = useState<'blocks' | 'tokens'>('blocks');
  const [dsModalOpen, setDsModalOpen]       = useState(false);
  const [pdfPreviewing, setPdfPreviewing]   = useState(false);
  const [viewMode, setViewMode]             = useState<'split' | 'visual' | 'design'>('split');
  const [draggingToken, setDraggingToken]   = useState<string | null>(null);
  const [dropIndicatorY, setDropIndicatorY] = useState<number | null>(null);
  const [pointerDragPos, setPointerDragPos] = useState<{x:number;y:number}|null>(null);
  const [gjsSelectedPath, setGjsSelectedPath] = useState<string[]>([]);
  const [pageStyle, setPageStyle]             = useState<PageStyleValues>(DEFAULT_PAGE_STYLE);
  const [stylesPanelOpen, setStylesPanelOpen] = useState(false);
  const [galleryOpen, setGalleryOpen]       = useState(false);
  const [tableWizardOpen, setTableWizardOpen] = useState(false);
  const [wizardSrcIdx, setWizardSrcIdx]     = useState(0);
  const [wizardCols, setWizardCols]         = useState<{ field: string; header: string; selected: boolean }[]>([]);

  const currentThemeId = useSelector((state: RootState) => state.theme.currentThemeId);
  const monacoTheme = currentThemeId === 'dark' ? 'vs-dark' : 'vs';

  const editorRef              = useRef<any>(null);
  const gjsEditorRef           = useRef<any>(null);
  const gjsEditorComponentRef  = useRef<GrapeJsEditorHandle>(null);
  const iframeRef              = useRef<HTMLIFrameElement>(null);
  const previewTimer           = useRef<number | null>(null);
  const tokensRef              = useRef<TokenDescriptor[]>([]);
  const completionDisposable   = useRef<any>(null);

  useEffect(() => { tokensRef.current = tokens; }, [tokens]);

  // Load report on mount
  useEffect(() => {
    appReportSvc.getReportTemplate(reportId)
      .then(data => {
        setReport(data);
        const tmpl = data?.ReportTemplate;
        const html = tmpl?.TemplateHtml ?? '';
        setTemplateHtml(html);
        setDataSources(parseSourcesFromConfig(tmpl?.ExtraParamConfig, tmpl?.DataSpName ?? data?.ReportFileName));
        setPageSize(tmpl?.PageSize ?? 'A4');
        setOrientation(tmpl?.Orientation ?? 'portrait');
        setMarginMm(tmpl?.MarginMm ?? 15);
        setExtraParamConfig(tmpl?.ExtraParamConfig ?? '');
        if (html) triggerPreview(html, mainReferenceId);
      })
      .catch(() => setError('Failed to load report'))
      .finally(() => setLoading(false));
  }, [reportId]); // eslint-disable-line react-hooks/exhaustive-deps

  const triggerPreview = useCallback((html: string, refId: number) => {
    if (!html) return;
    const primarySp = dataSources.find(s => s.type === 'sp')?.value;
    const liveConfig = serializeConfig(dataSources, extraParamConfig);
    appReportSvc.previewHtml({
      reportId,
      mainReferenceId: refId,
      templateHtmlOverride: html,
      dataSpNameOverride: primarySp || undefined,
      extraParamConfigOverride: liveConfig || undefined,
    }).then(setPreviewHtml).catch(() => setPreviewHtml(html));
  }, [reportId, dataSources, extraParamConfig]);

  const schedulePreview = useCallback((html: string) => {
    if (previewTimer.current) window.clearTimeout(previewTimer.current);
    previewTimer.current = window.setTimeout(() => {
      triggerPreview(html, Number(previewRefId) || 0);
    }, 700) as unknown as number;
  }, [triggerPreview, previewRefId]);

  const handleHtmlChange = (v: string) => {
    setTemplateHtml(v);
    schedulePreview(v);
  };

  const refreshTokens = useCallback(() => {
    const hasSource = dataSources.some(s =>
      (s.type === 'sp' && s.value.trim()) ||
      (s.type === 'api' && (s.value.trim() || s.sampleJson?.trim()))
    );
    if (!hasSource) return;
    setTokenLoading(true);
    // Pass current (unsaved) config so sampleJson for API sources is included
    const liveConfig = JSON.stringify({ dataSources });
    const refId = Number(previewRefId) || 0;
    appReportSvc.getTokensFromConfig(liveConfig, undefined, refId)
      .then(setTokens)
      .catch(() => setTokens([]))
      .finally(() => setTokenLoading(false));
  }, [dataSources, previewRefId]);

  const insertAtCursor = (text: string) => {
    if (viewMode === 'design' && gjsEditorRef.current) {
      const gjsEditor = gjsEditorRef.current;

      // <style> blocks → open the Style Panel (CSS is managed there, not via raw insertion)
      const styleMatch = text.trim().match(/^<style>([\s\S]*?)<\/style>$/i);
      if (styleMatch) {
        setStylesPanelOpen(true);
        return;
      }

      // Rich block HTML (starts with a tag) → append to canvas body, not into a selected cell
      if (text.trim().startsWith('<')) {
        gjsEditor.DomComponents?.getWrapper?.()?.append(text);
        return;
      }

      // Token ({{...}}) → replace selected cell content
      const sel = gjsEditor.getSelected?.();
      if (sel) {
        sel.components().reset();
        sel.append(text);
      } else {
        navigator.clipboard?.writeText(text).catch(() => {});
      }
      return;
    }
    const editor = editorRef.current;
    if (editor) {
      const sel = editor.getSelection();
      if (sel) {
        editor.executeEdits('insert', [{ range: sel, text, forceMoveMarkers: true }]);
      } else {
        const model = editor.getModel();
        const lastLine = model?.getLineCount() ?? 1;
        const lastCol  = model?.getLineMaxColumn(lastLine) ?? 1;
        editor.executeEdits('insert', [{ range: { startLineNumber: lastLine, startColumn: lastCol, endLineNumber: lastLine, endColumn: lastCol }, text, forceMoveMarkers: true }]);
      }
      editor.focus();
      schedulePreview(editor.getValue());
    } else {
      const next = templateHtml + '\n' + text;
      setTemplateHtml(next);
      schedulePreview(next);
    }
  };

  const applyPageStyle = (values: PageStyleValues) => {
    setPageStyle(values);
    gjsEditorRef.current?.setStyle?.(generatePageCss(values));
  };

  const applyStarterTemplate = (html: string) => {
    setTemplateHtml(html);
    schedulePreview(html);
  };

  const handleHtmlPreview = () => {
    if (!previewHtml) return;
    const blob = new Blob([previewHtml], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 15000);
  };

  // Export a self-contained HTML artifact — wraps the rendered preview in a full
  // <!DOCTYPE html> document so it opens standalone in any browser (same approach
  // Claude uses for artifacts: pure HTML blob downloaded directly from the browser).
  const handleHtmlExport = () => {
    const exportHtml = previewHtml || templateHtml;
    if (!exportHtml) return;
    const reportName = report?.ReportName ?? 'report';
    const fullHtml = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${reportName}</title>
<style>
  * { box-sizing: border-box; }
  @page { size: ${pageSize} ${orientation}; margin: ${marginMm}mm; }
  body { margin: 0; padding: ${marginMm}mm; font-family: Arial, sans-serif; }
</style>
</head>
<body>
${exportHtml}
</body>
</html>`;
    const blob = new Blob([fullHtml], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${reportName.replace(/[^a-z0-9_-]/gi, '_')}.html`;
    a.click();
    URL.revokeObjectURL(url);
  };

  // Drop a dragged token/block onto the visual preview iframe.
  // Uses elementFromPoint on the iframe document (sandbox=allow-same-origin) to find
  // the rendered element at the drop position, then matches it by tag+index back to
  // the source HTML and inserts the text after that element's closing tag.
  const insertTokenAtDropPosition = useCallback((token: string, clientX: number, clientY: number) => {
    const iframe = iframeRef.current;
    const iframeDoc = iframe?.contentDocument;

    const fallback = (html: string) => {
      const next = html + '\n' + token;
      setTemplateHtml(next);
      schedulePreview(next);
    };

    if (!iframeDoc || !templateHtml) { fallback(templateHtml); return; }

    const iframeRect = iframe!.getBoundingClientRect();
    const x = clientX - iframeRect.left;
    const y = clientY - iframeRect.top;

    const el = iframeDoc.elementFromPoint(x, y);
    if (!el) { fallback(templateHtml); return; }

    // Walk up to a meaningful block-level ancestor
    const BLOCKS = new Set(['H1','H2','H3','H4','H5','H6','P','TABLE','UL','OL','SECTION','ARTICLE','DIV']);
    let target: Element = el;
    while (target.parentElement && !BLOCKS.has(target.tagName) && target.tagName !== 'BODY') {
      target = target.parentElement;
    }
    if (target.tagName === 'BODY' || target.tagName === 'HTML') { fallback(templateHtml); return; }

    const tagName = target.tagName.toLowerCase();
    const siblings = Array.from(iframeDoc.querySelectorAll(tagName));
    const elIdx = siblings.indexOf(target as HTMLElement);

    const html = templateHtml;
    const re = new RegExp(`<${tagName}[\\s>/]`, 'gi');
    const htmlMatches = Array.from(html.matchAll(re));

    const safeIdx = Math.max(0, Math.min(elIdx, htmlMatches.length - 1));
    if (htmlMatches.length === 0) { fallback(html); return; }

    const openPos = htmlMatches[safeIdx].index!;
    const closeTag = `</${tagName}>`;
    const closePos = html.indexOf(closeTag, openPos);
    const insertPos = closePos !== -1 ? closePos + closeTag.length : openPos + htmlMatches[safeIdx][0].length;

    const next = html.slice(0, insertPos) + '\n' + token + html.slice(insertPos);
    setTemplateHtml(next);
    schedulePreview(next);
  }, [templateHtml, schedulePreview]);

  const handlePdfPreview = async () => {
    setPdfPreviewing(true);
    setError(null);
    try {
      const primarySp = dataSources.find(s => s.type === 'sp')?.value;
      const liveConfig = serializeConfig(dataSources, extraParamConfig);
      const blobUrl = await appReportSvc.previewPdf({
        reportId,
        mainReferenceId: Number(previewRefId) || 0,
        templateHtmlOverride: templateHtml,
        dataSpNameOverride: primarySp || undefined,
        extraParamConfigOverride: liveConfig || undefined,
        pageSize,
        orientation,
        marginMm,
      });
      window.open(blobUrl, '_blank');
      setTimeout(() => URL.revokeObjectURL(blobUrl), 60000);
    } catch {
      setError('PDF preview failed.');
    } finally {
      setPdfPreviewing(false);
    }
  };

  const handleSave = async () => {
    // Read directly from Monaco model — bypasses debounce so Save always
    // captures the latest keystrokes, not a potentially-stale React state copy.
    const currentHtml = editorRef.current?.getValue() ?? templateHtml;
    setSaving(true);
    setError(null);
    try {
      const newConfig   = serializeConfig(dataSources, extraParamConfig);
      const primarySpName = dataSources.find(s => s.type === 'sp')?.value ?? '';
      await appReportSvc.saveReportTemplate({
        ...report,
        ReportTemplate: {
          ReportId:         reportId,
          TemplateHtml:     currentHtml,
          DataSpName:       primarySpName,  // primary SP for legacy display
          PageSize:         pageSize,
          Orientation:      orientation,
          MarginMm:         marginMm,
          ExtraParamConfig: newConfig || null,
        },
      });
      setTemplateHtml(currentHtml);  // sync state to what was actually saved
      onSaved?.();
    } catch {
      setError('Save failed.');
    } finally {
      setSaving(false);
    }
  };

  // Group tokens by their data source name
  const groupedTokens = React.useMemo(() => {
    const groups: { name: string; type: string; toks: TokenDescriptor[] }[] = [];
    const sortedSrcs = [...dataSources].sort((a, b) => b.name.length - a.name.length);
    for (const tok of tokens) {
      let srcName = tok.ResultSet;
      for (const src of sortedSrcs) {
        if (tok.ResultSet === src.name || tok.ResultSet.startsWith(src.name + '_')) {
          srcName = src.name; break;
        }
      }
      let grp = groups.find(g => g.name === srcName);
      if (!grp) {
        const srcType = dataSources.find(s => s.name === srcName)?.type ?? 'sp';
        grp = { name: srcName, type: srcType, toks: [] };
        groups.push(grp);
      }
      grp.toks.push(tok);
    }
    // Sort: scalars first (A-Z), then #each marker, then children (A-Z)
    for (const grp of groups) {
      grp.toks.sort((a, b) => {
        const pri = (t: TokenDescriptor) => t.IsList && !t.InsideEach ? 1 : t.InsideEach ? 2 : 0;
        const pa = pri(a), pb = pri(b);
        if (pa !== pb) return pa - pb;
        return a.Field.localeCompare(b.Field);
      });
    }
    return groups;
  }, [tokens, dataSources]);

  if (loading) return <div className={`p-4 text-sm ${theme.label}`}>Loading…</div>;

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>

      {/* ── Toolbar ── */}
      <div className={`flex items-center gap-2 px-3 py-2 flex-shrink-0 border-b ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
        <span className={`text-sm font-semibold ${theme.title} mr-1 shrink-0`}>
          {report?.ReportName ?? 'Report Designer'}
        </span>
        {/* Data Sources popup trigger */}
        <button
          onClick={() => setDsModalOpen(true)}
          className={`h-7 px-2 text-xs border rounded-[4px] shrink-0 flex items-center gap-1 ${theme.inputBox}`}
          title="Configure data sources"
        >
          <i className="fa-solid fa-database text-blue-400 mr-1" />
          {dataSources.filter(s => s.value).length} Source{dataSources.filter(s => s.value).length !== 1 ? 's' : ''}
          <i className="fa-solid fa-pen-to-square text-[9px] ml-0.5 text-blue-400" />
        </button>
        <label className={`text-xs ${theme.label} shrink-0`}>Size:</label>
        <select className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} value={pageSize} onChange={e => setPageSize(e.target.value)}>
          {PAGE_SIZES.map(s => <option key={s}>{s}</option>)}
        </select>
        <select className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} value={orientation} onChange={e => setOrientation(e.target.value)}>
          {ORIENTATIONS.map(o => <option key={o}>{o}</option>)}
        </select>
        <label className={`text-xs ${theme.label} shrink-0`}>Margin:</label>
        <input type="number" className={`h-7 w-14 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} value={marginMm} onChange={e => setMarginMm(Number(e.target.value))} />
        <div className="flex-auto" />
        {error && <span className="text-xs text-red-500 shrink-0">{error}</span>}
        <button
          onClick={handleHtmlPreview}
          disabled={!previewHtml}
          title="Open rendered HTML in new tab"
          className={`px-3 py-1.5 text-sm rounded-[4px] shrink-0 disabled:opacity-40 disabled:cursor-not-allowed ${theme.button_default}`}
        >
          <i className="fa-solid fa-globe mr-1" />HTML Preview
        </button>
        <button
          onClick={handlePdfPreview}
          disabled={pdfPreviewing || !templateHtml}
          title="Generate PDF and open in new tab"
          className={`px-3 py-1.5 text-sm rounded-[4px] shrink-0 disabled:opacity-40 disabled:cursor-not-allowed ${theme.button_default}`}
        >
          {pdfPreviewing
            ? <><i className="fa-solid fa-spinner fa-spin mr-1" />Generating…</>
            : <><i className="fa-solid fa-file-pdf mr-1" />PDF Preview</>}
        </button>
        <button onClick={handleSave} disabled={saving} className={`px-3 py-1.5 text-sm rounded-[4px] shrink-0 ${theme.button_default}`}>
          {saving ? <><i className="fa-solid fa-spinner fa-spin mr-1" />Saving…</> : <><i className="fa-solid fa-floppy-disk mr-1" />Save</>}
        </button>
        {onClose && (
          <button onClick={onClose} className={`px-2 py-1.5 text-sm rounded-[4px] shrink-0 ${theme.button_default}`}>
            <i className="fa-solid fa-xmark" />
          </button>
        )}
      </div>

      {/* ── Data Sources Modal ── */}
      {dsModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
             onClick={() => setDsModalOpen(false)}>
          <div
            className={`w-[560px] max-h-[80vh] flex flex-col rounded-lg shadow-2xl border ${t('border_mainContentSection')} ${theme.mainContentSection}`}
            onClick={e => e.stopPropagation()}
          >
            {/* Header */}
            <div className={`flex-shrink-0 flex items-center justify-between px-4 py-3 border-b ${t('border_mainContentSection')}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>
                <i className="fa-solid fa-database mr-2 text-blue-500" />Data Sources
              </span>
              <button onClick={() => setDsModalOpen(false)} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                <i className="fa-solid fa-xmark" />
              </button>
            </div>
            {/* Body */}
            <div className="flex-auto overflow-y-auto px-4 py-4">
              <p className={`text-xs mb-3 opacity-60 ${theme.label}`}>
                SP: RS0 → <code>{'{{name.Field}}'}</code> · RS1 → <code>{'{{#each name_rs1}}'}</code>&nbsp;&nbsp;
                API: primitives → <code>{'{{name.Field}}'}</code> · array "Lines" → <code>{'{{#each name_Lines}}'}</code>
              </p>
              <DataSourceEditor
                sources={dataSources}
                onChange={setDataSources}
              />
            </div>
            {/* Footer */}
            <div className={`flex-shrink-0 flex justify-end gap-2 px-4 py-3 border-t ${t('border_mainContentSection')}`}>
              <button onClick={() => setDsModalOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
              <button
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}
                onClick={() => { refreshTokens(); setLeftTab('tokens'); setDsModalOpen(false); }}
              >
                <i className="fa-solid fa-check mr-1" />Apply &amp; Load Tokens
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Template Gallery Modal ── */}
      {galleryOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
             onClick={() => setGalleryOpen(false)}>
          <div
            className={`w-[680px] max-h-[80vh] flex flex-col rounded-lg shadow-2xl border ${t('border_mainContentSection')} ${theme.mainContentSection}`}
            onClick={e => e.stopPropagation()}
          >
            {/* Header */}
            <div className={`flex-shrink-0 flex items-center justify-between px-4 py-3 border-b ${t('border_mainContentSection')}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>
                <i className="fa-solid fa-layer-group mr-2 text-blue-500" />Choose a Starter Template
              </span>
              <button onClick={() => setGalleryOpen(false)} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                <i className="fa-solid fa-xmark" />
              </button>
            </div>
            {/* Layout approach note */}
            <div className="flex-shrink-0 flex items-start gap-2 mx-4 mt-4 px-3 py-2.5 rounded-[4px] bg-blue-50 border border-blue-200 text-blue-700">
              <i className="fa-solid fa-circle-info text-blue-400 mt-0.5 shrink-0" />
              <p className="text-xs leading-relaxed">
                <strong className="text-green-700">Flow layout</strong> — tables and text reflow automatically as data length varies. Use for data reports with repeating rows.&nbsp;
                <strong className="text-purple-700">Fixed layout</strong> — pixel-positioned canvas (Canva-style). Use for covers, labels, and posters where the design must be pixel-perfect and data length is predictable.
              </p>
            </div>
            {/* Card grid */}
            <div className="flex-auto overflow-y-auto p-4 grid grid-cols-2 gap-4">
              {STARTER_TEMPLATES.map(tmpl => (
                <button
                  key={tmpl.label}
                  onClick={() => { applyStarterTemplate(tmpl.html); setGalleryOpen(false); }}
                  title={tmpl.tip ?? `${tmpl.label} — ${tmpl.isAbsolute ? 'fixed-size pixel canvas (Canva-style)' : 'CSS flow layout, content reflows with real data'}`}
                  className={`flex flex-col rounded-lg border-2 overflow-hidden text-left transition-colors ${tmpl.isAbsolute ? 'hover:border-purple-400' : 'hover:border-blue-400'} ${t('border_mainContentSection')}`}
                >
                  {/* Scaled HTML thumbnail */}
                  <div className="relative w-full overflow-hidden bg-white" style={{ height: '160px' }}>
                    <iframe
                      srcDoc={tmpl.html}
                      title={tmpl.label}
                      className="absolute top-0 left-0 border-none origin-top-left"
                      style={{ width: '600px', height: '500px', transform: 'scale(0.48)', pointerEvents: 'none' }}
                      sandbox="allow-scripts"
                    />
                  </div>
                  {/* Label */}
                  <div className={`px-3 py-2 text-xs font-medium flex items-center gap-1.5 ${theme.label}`}>
                    <span>{tmpl.icon}</span>
                    <span className="flex-auto">{tmpl.label}</span>
                    {tmpl.isAbsolute
                      ? <span className="text-[9px] px-1.5 py-0.5 rounded font-semibold bg-purple-100 text-purple-600 shrink-0">Fixed</span>
                      : <span className="text-[9px] px-1.5 py-0.5 rounded font-semibold bg-green-100 text-green-700 shrink-0">Flow</span>}
                  </div>
                </button>
              ))}
              {/* Blank option */}
              <button
                onClick={() => { applyStarterTemplate(''); setGalleryOpen(false); }}
                title="Start with an empty canvas — write your own HTML using CSS flow layout"
                className={`flex flex-col rounded-lg border-2 overflow-hidden text-left transition-colors hover:border-blue-400 ${t('border_mainContentSection')}`}
              >
                <div className={`relative w-full flex items-center justify-center ${theme.mainContentSection}`} style={{ height: '160px' }}>
                  <div className="flex flex-col items-center gap-2 opacity-40">
                    <i className="fa-solid fa-file text-4xl" />
                    <span className="text-xs">Blank canvas</span>
                  </div>
                </div>
                <div className={`px-3 py-2 text-xs font-medium flex items-center gap-1.5 ${theme.label}`}>
                  <span>📄</span>Start blank
                </div>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Table Wizard Modal ── */}
      {tableWizardOpen && (() => {
        const listGroups = groupedTokens.filter(g => g.toks.some(tok => tok.IsList && !tok.InsideEach));
        const safeIdx = Math.min(wizardSrcIdx, Math.max(0, listGroups.length - 1));
        const selectedGrp = listGroups[safeIdx];
        const eachToken = selectedGrp?.toks.find(t => t.IsList && !t.InsideEach);
        const selectedCols = wizardCols.filter(c => c.selected);

        const generateHtml = () => {
          if (!eachToken || selectedCols.length === 0) return '';
          const headers = selectedCols.map(c => `      <th style="background:#1d4ed8;color:white;padding:6px 10px;text-align:left">${c.header}</th>`).join('\n');
          const cells = selectedCols.map(c => `      <td style="padding:5px 10px;border-bottom:1px solid #e5e7eb">{{${c.field}}}</td>`).join('\n');
          return `<table style="width:100%;border-collapse:collapse;font-family:Arial,sans-serif;font-size:12px;margin-bottom:16px">
  <thead>
    <tr>
${headers}
    </tr>
  </thead>
  <tbody>
    ${eachToken.Token}
    <tr>
${cells}
    </tr>
    {{/each}}
  </tbody>
</table>`;
        };

        return (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
               onClick={() => setTableWizardOpen(false)}>
            <div
              className={`w-[520px] max-h-[80vh] flex flex-col rounded-lg shadow-2xl border ${t('border_mainContentSection')} ${theme.mainContentSection}`}
              onClick={e => e.stopPropagation()}
            >
              {/* Header */}
              <div className={`flex-shrink-0 flex items-center justify-between px-4 py-3 border-b ${t('border_mainContentSection')}`}>
                <span className={`text-sm font-semibold ${theme.title}`}>
                  <i className="fa-solid fa-table mr-2 text-purple-500" />Table Wizard
                </span>
                <button onClick={() => setTableWizardOpen(false)} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                  <i className="fa-solid fa-xmark" />
                </button>
              </div>
              {/* Body */}
              <div className="flex-auto overflow-y-auto px-4 py-4 space-y-4">
                {listGroups.length === 0 ? (
                  <p className={`text-xs ${theme.label} opacity-70`}>
                    No list tokens found. Add a data source with repeating rows and click "Apply &amp; Load Tokens" first.
                  </p>
                ) : (
                  <>
                    {/* Step 1: data source */}
                    <div>
                      <p className={`text-xs font-semibold mb-1 ${theme.label}`}>1. Select data source</p>
                      <select
                        className={`h-7 w-full px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                        value={safeIdx}
                        onChange={e => {
                          const idx = Number(e.target.value);
                          setWizardSrcIdx(idx);
                          const grp = listGroups[idx];
                          setWizardCols(grp ? grp.toks.filter(tok => !tok.IsList || tok.InsideEach).map(tok => ({ field: tok.Field, header: tok.Field, selected: true })) : []);
                        }}
                      >
                        {listGroups.map((g, i) => {
                          const each = g.toks.find(t => t.IsList && !t.InsideEach);
                          return <option key={g.name} value={i}>{g.name} ({each?.Token ?? ''})</option>;
                        })}
                      </select>
                    </div>

                    {/* Step 2: columns */}
                    {wizardCols.length > 0 && (
                      <div>
                        <p className={`text-xs font-semibold mb-1 ${theme.label}`}>2. Choose columns</p>
                        <div className="space-y-1 max-h-44 overflow-y-auto pr-1">
                          {wizardCols.map((col, i) => (
                            <div key={col.field} className={`flex items-center gap-2 px-2 py-1 rounded border ${t('border_mainContentSection')}`}>
                              <input
                                type="checkbox"
                                checked={col.selected}
                                onChange={e => setWizardCols(prev => prev.map((c, j) => j === i ? { ...c, selected: e.target.checked } : c))}
                                className="shrink-0"
                              />
                              <span className={`text-xs w-24 shrink-0 ${theme.label}`}>{col.field}</span>
                              <input
                                type="text"
                                value={col.header}
                                onChange={e => setWizardCols(prev => prev.map((c, j) => j === i ? { ...c, header: e.target.value } : c))}
                                className={`h-6 flex-auto px-1.5 text-xs border rounded-[4px] ${theme.inputBox}`}
                                placeholder="Column header"
                              />
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Preview */}
                    {selectedCols.length > 0 && eachToken && (
                      <div>
                        <p className={`text-xs font-semibold mb-1 ${theme.label}`}>3. Preview</p>
                        <pre className={`text-[10px] p-2 rounded border overflow-x-auto ${t('border_mainContentSection')} ${theme.mainContentSection}`} style={{ maxHeight: 120 }}>
                          {generateHtml()}
                        </pre>
                      </div>
                    )}
                  </>
                )}
              </div>
              {/* Footer */}
              <div className={`flex-shrink-0 flex justify-end gap-2 px-4 py-3 border-t ${t('border_mainContentSection')}`}>
                <button onClick={() => setTableWizardOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                  Cancel
                </button>
                <button
                  disabled={selectedCols.length === 0 || !eachToken}
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary} disabled:opacity-40 disabled:cursor-not-allowed`}
                  onClick={() => { insertAtCursor(generateHtml()); setTableWizardOpen(false); }}
                >
                  <i className="fa-solid fa-check mr-1" />Insert Table
                </button>
              </div>
            </div>
          </div>
        );
      })()}

      {/* ── Main body ── */}
      <div className="flex h-1 flex-auto overflow-hidden">

        {/* ── Left panel: Blocks / Tokens ── */}
        <div className={`w-52 shrink-0 flex flex-col border-r overflow-hidden ${t('border_mainContentSection')} ${theme.mainContentSection}`}>

          {/* Tab strip */}
          <div className={`flex border-b shrink-0 ${t('border_mainContentSection')}`}>
            {(['blocks', 'tokens'] as const).map(tab => (
              <button
                key={tab}
                onClick={() => {
                  setLeftTab(tab);
                  if (tab === 'tokens') refreshTokens();
                }}
                className={`flex-1 py-1.5 text-xs font-medium capitalize transition-colors
                  ${leftTab === tab
                    ? 'border-b-2 border-blue-500 text-blue-600'
                    : `${theme.label} hover:opacity-80`}`}
              >
                {tab === 'blocks' ? '⊞ Blocks' : '⟨⟩ Tokens'}
              </button>
            ))}
          </div>

          <div className="h-1 flex-auto overflow-y-auto">

            {/* ── Blocks tab ── */}
            {leftTab === 'blocks' && (
              <div className="p-2 space-y-3">

                {/* Starter templates — gallery trigger */}
                <div>
                  <p className={`text-[10px] font-semibold uppercase tracking-wider mb-1.5 ${theme.label} opacity-60`}>Start from template</p>
                  <button
                    onClick={() => setGalleryOpen(true)}
                    className={`w-full text-left px-2 py-1.5 text-xs rounded-[4px] border ${t('border_mainContentSection')} ${theme.contextMenu} hover:border-blue-400 transition-colors flex items-center gap-1.5`}
                  >
                    <i className="fa-solid fa-layer-group text-blue-400" />
                    Browse templates…
                  </button>
                </div>

                {/* Insert blocks */}
                <div>
                  <p className={`text-[10px] font-semibold uppercase tracking-wider mb-1.5 ${theme.label} opacity-60`}>Insert block</p>
                  {/* Table wizard shortcut */}
                  <button
                    onClick={() => {
                      const listGroups = groupedTokens.filter(g => g.toks.some(tok => tok.IsList && !tok.InsideEach));
                      const idx = Math.min(wizardSrcIdx, Math.max(0, listGroups.length - 1));
                      const grp = listGroups[idx];
                      setWizardSrcIdx(idx);
                      setWizardCols(grp ? grp.toks.filter(tok => !tok.IsList || tok.InsideEach).map(tok => ({ field: tok.Field, header: tok.Field, selected: true })) : []);
                      setTableWizardOpen(true);
                    }}
                    className={`w-full mb-1 text-left px-2 py-1.5 text-xs rounded-[4px] border ${t('border_mainContentSection')} ${theme.contextMenu} hover:border-blue-400 transition-colors flex items-center gap-1.5`}
                    title="Generate a data table from a token list"
                  >
                    <i className="fa-solid fa-table text-purple-400" />
                    Insert Table from data…
                  </button>
                  <div className="space-y-1">
                    {REPORT_BLOCKS.map(block => (
                      <button
                        key={block.label}
                        draggable
                        onDragStart={e => {
                          e.dataTransfer.setData('text/plain', block.html);
                          e.dataTransfer.setData('application/x-report-block', '1');
                          e.dataTransfer.effectAllowed = 'copy';
                          setDraggingToken(block.html);
                        }}
                        onDragEnd={() => { setDraggingToken(null); setDropIndicatorY(null); }}
                        onClick={() => insertAtCursor(block.html)}
                        title="Design mode: click to insert onto canvas · Code mode: click to insert at cursor or drag onto editor"
                        className={`w-full text-left px-2 py-1.5 text-xs rounded-[4px] border ${t('border_mainContentSection')} ${theme.contextMenu} hover:border-blue-400 transition-colors cursor-grab active:cursor-grabbing flex items-center gap-1.5`}
                      >
                        <span className="text-gray-300 text-[10px] select-none shrink-0">⠿</span>
                        <i className={`${block.icon} ${block.color} w-3 shrink-0`} />
                        {block.label}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* ── Tokens tab ── */}
            {leftTab === 'tokens' && (
              <div>
                {/* Header */}
                <div className={`flex items-center justify-between px-2 py-1.5 sticky top-0 z-10 border-b ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
                  <span className={`text-[10px] font-semibold uppercase tracking-wider opacity-60 ${theme.label}`}>
                    Tokens
                  </span>
                  <div className="flex items-center gap-1">
                    <input
                      type="number"
                      className={`h-6 w-16 px-1 text-[10px] border rounded-[4px] ${theme.inputBox}`}
                      value={previewRefId}
                      onChange={e => setPreviewRefId(e.target.value)}
                      placeholder="ID"
                      title="Sample record ID used when discovering tokens from stored procedures"
                    />
                    <button
                      onClick={() => setDsModalOpen(true)}
                      className={`text-xs px-1.5 py-0.5 rounded ${theme.button_default}`}
                      title="Edit data sources"
                    >
                      <i className="fa-solid fa-database" />
                    </button>
                    <button onClick={refreshTokens} disabled={tokenLoading} className={`text-xs px-1.5 py-0.5 rounded ${theme.button_default}`} title="Reload tokens">
                      <i className={`fa-solid fa-rotate ${tokenLoading ? 'fa-spin' : ''}`} />
                    </button>
                  </div>
                </div>

                {tokens.length === 0 ? (
                  <div className={`px-3 py-4 text-xs text-center ${theme.label} opacity-70`}>
                    <i className="fa-solid fa-database block text-2xl mb-2 opacity-30" />
                    {dataSources.some(s => s.value.trim())
                      ? <>Set record ID if needed, then click <i className="fa-solid fa-rotate" /> to load tokens</>
                      : <>Click <i className="fa-solid fa-database" /> to add a data source</>}
                  </div>
                ) : (
                  <>
                  {!draggingToken && (
                    <div className={`mx-2 my-1.5 px-2 py-1.5 rounded-[4px] text-[10px] border border-blue-200 bg-blue-50 text-blue-600`}>
                      <div className="flex items-center gap-1 font-semibold mb-0.5">
                        <i className="fa-solid fa-hand-pointer text-[9px]" /> Click token → inserts at cursor
                      </div>
                      <div className="flex items-center gap-1">
                        <i className="fa-solid fa-up-down-left-right text-[9px]" /> Drag token → drop onto any line in the code editor
                      </div>
                    </div>
                  )}
                  {groupedTokens.map(grp => (
                    <div key={grp.name}>
                      {/* Source group header */}
                      <div className={`flex items-center gap-1 px-2 py-1 text-[10px] font-bold uppercase tracking-wider sticky top-[33px] z-10 ${theme.mainContentSection} border-b ${t('border_mainContentSection')}`}>
                        <i className={`fa-solid ${grp.type === 'api' ? 'fa-network-wired text-purple-500' : 'fa-database text-blue-500'} text-[9px]`} />
                        <span className={grp.type === 'api' ? 'text-purple-500' : 'text-blue-500'}>{grp.name}</span>
                      </div>
                      {/* Tokens in this source */}
                      <div className="space-y-0">
                        {grp.toks.map((tok, i) => (
                          <button
                            key={i}
                            draggable
                            onDragStart={e => {
                              if (viewMode === 'design') { e.preventDefault(); return; }
                              e.dataTransfer.setData('text/plain', tok.Token);
                              e.dataTransfer.effectAllowed = 'copy';
                              setDraggingToken(tok.Token);
                            }}
                            onDragEnd={() => { setDraggingToken(null); setDropIndicatorY(null); }}
                            onPointerDown={e => {
                              (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
                              setDraggingToken(tok.Token);
                              if (viewMode === 'design') setPointerDragPos({ x: e.clientX, y: e.clientY });
                            }}
                            onPointerMove={e => {
                              if (draggingToken && viewMode === 'design')
                                setPointerDragPos({ x: e.clientX, y: e.clientY });
                            }}
                            onPointerUp={e => {
                              const token = tok.Token;
                              (e.currentTarget as HTMLElement).releasePointerCapture(e.pointerId);
                              setPointerDragPos(null);
                              if (viewMode === 'design' && token.includes('{{')) {
                                setDraggingToken(null);
                                gjsEditorComponentRef.current?.dropToken(token, e.clientX, e.clientY);
                                return;
                              }
                              setDraggingToken(null);
                            }}
                            onClick={() => insertAtCursor(tok.Token)}
                            title={`${tok.Token}\nDesign mode: drag over canvas cell and release to insert\nCode mode: click to insert at cursor · drag onto any line`}
                            className={`w-full text-left px-2 py-1 text-xs truncate cursor-grab active:cursor-grabbing border-b ${t('border_mainContentSection')} ${theme.contextMenu} hover:border-blue-400 flex items-center gap-1`}
                          >
                            <span className="text-gray-300 text-[10px] shrink-0 select-none">⠿</span>
                            {tok.InsideEach
                              ? <span className="text-gray-400 text-[10px]">↳</span>
                              : tok.IsList
                                ? <i className="fa-solid fa-list text-orange-400 text-[9px]" />
                                : <i className="fa-solid fa-tag text-green-400 text-[9px]" />}
                            <span className={`truncate ${tok.InsideEach ? 'opacity-70' : ''}`}>
                              {tok.Field === '*' ? `#each ${tok.ResultSet}` : tok.Field}
                            </span>
                          </button>
                        ))}
                      </div>
                    </div>
                  ))}
                  </>
                )}
              </div>
            )}
          </div>
        </div>

        {/* ── Right side: Code mode OR Visual mode (never both) ── */}
        <div className="flex-auto flex flex-col overflow-hidden">

          {/* Shared mode toggle strip */}
          <div className={`flex items-center gap-2 px-3 py-1 shrink-0 border-b ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
            {viewMode === 'visual' ? (
              /* Visual bar — Ref ID + Refresh on left, toggle on right */
              <>
                <i className="fa-solid fa-eye text-blue-400 text-xs shrink-0" />
                <span className={`text-xs font-medium ${theme.label} shrink-0`}>
                  Preview — {pageSize} {orientation}
                </span>
                <span className={`text-xs ${theme.label} opacity-70`}>Preview record #</span>
                <input
                  type="number"
                  className={`h-6 w-20 px-1.5 text-xs border rounded-[4px] ${theme.inputBox}`}
                  value={previewRefId}
                  onChange={e => setPreviewRefId(e.target.value)}
                  placeholder="e.g. 42"
                  title="Enter any record ID — the preview will fetch live data from your data sources using that record"
                />
                <button
                  onClick={() => triggerPreview(templateHtml, Number(previewRefId) || 0)}
                  className={`h-6 px-2 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-rotate mr-1" />Refresh
                </button>
              </>
            ) : viewMode === 'design' ? (
              /* Design bar — breadcrumb + parent selector */
              <>
                <i className="fa-solid fa-pen-ruler text-purple-400 text-xs shrink-0" />
                {gjsSelectedPath.length === 0 ? (
                  <span className={`text-xs opacity-40 ${theme.label}`}>
                    Click any element to select it
                  </span>
                ) : (
                  <span className={`text-xs opacity-40 ${theme.label} shrink-0`}>Click blocks to insert · select a cell then click a token</span>
                )}
                {gjsSelectedPath.length > 0 && (
                  <>
                    {/* Clickable breadcrumb — each tag navigates to that ancestor */}
                    <div className="flex items-center gap-0.5 overflow-x-auto max-w-[420px]">
                      {gjsSelectedPath.map((tag, i) => (
                        <React.Fragment key={i}>
                          {i > 0 && <span className="text-gray-400 text-[10px] shrink-0 select-none">›</span>}
                          <button
                            title={i === gjsSelectedPath.length - 1 ? `Selected: ${tag}` : `Click to select ${tag}`}
                            onClick={() => {
                              const ed = gjsEditorRef.current;
                              let cur = ed?.getSelected?.();
                              const stepsUp = gjsSelectedPath.length - 1 - i;
                              for (let s = 0; s < stepsUp; s++) cur = cur?.parent?.();
                              if (cur && ed) ed.select(cur);
                            }}
                            className={`px-1.5 py-0.5 rounded text-[10px] font-mono shrink-0 transition-colors ${
                              i === gjsSelectedPath.length - 1
                                ? 'bg-purple-100 text-purple-700 font-bold'
                                : 'text-gray-500 hover:bg-gray-100 hover:text-gray-700'
                            }`}
                          >
                            {tag}
                          </button>
                        </React.Fragment>
                      ))}
                    </div>
                    {/* Go up one level */}
                    <button
                      title="Select parent element (↑)"
                      onClick={() => {
                        const ed = gjsEditorRef.current;
                        const sel = ed?.getSelected?.();
                        const parent = sel?.parent?.();
                        if (parent?.get?.('tagName') && ed) ed.select(parent);
                      }}
                      className={`h-5 px-1.5 text-[10px] rounded shrink-0 ${theme.button_default}`}
                    >
                      ↑
                    </button>
                  </>
                )}
                {/* Styles button — always visible in Design mode */}
                <button
                  onClick={() => setStylesPanelOpen(v => !v)}
                  title="Edit page-level styles (font, colors, table theme)"
                  className={`h-6 px-2 text-[10px] rounded shrink-0 flex items-center gap-1 ml-1 ${stylesPanelOpen ? 'bg-purple-500 text-white' : theme.button_default}`}
                >
                  <i className="fa-solid fa-palette" /> Styles
                </button>
              </>
            ) : (
              /* Code bar — label on left */
              <>
                <i className="fa-solid fa-code text-gray-400 text-xs shrink-0" />
                <span className={`text-xs font-medium ${theme.label}`}>HTML / CSS</span>
                <span className={`text-xs opacity-40 ${theme.label}`}>
                  — <i className="fa-solid fa-hand-pointer mr-0.5" />click token to insert at cursor
                  &nbsp;·&nbsp;
                  <i className="fa-solid fa-up-down-left-right mr-0.5" />drag token onto editor
                  &nbsp;·&nbsp;
                  type <kbd className="px-0.5 py-px text-[9px] border rounded">{'{{' }</kbd> for autocomplete
                </span>
              </>
            )}
            <div className="flex-auto" />
            {/* Toggle pill */}
            <div className="flex shrink-0 rounded-[4px] overflow-hidden border border-gray-300">
              <button
                onClick={() => setViewMode('design')}
                title="GrapeJS WYSIWYG visual editor"
                className={`h-6 px-2.5 text-xs transition-colors ${viewMode === 'design' ? 'bg-blue-500 text-white' : theme.button_default}`}
              >
                <i className="fa-solid fa-pen-ruler mr-1" />Design
              </button>
              <button
                onClick={() => {
                  setViewMode('split');
                  requestAnimationFrame(() => editorRef.current?.layout());
                }}
                title="Full-screen HTML/CSS editor"
                className={`h-6 px-2.5 text-xs transition-colors ${viewMode === 'split' ? 'bg-blue-500 text-white' : theme.button_default}`}
              >
                <i className="fa-solid fa-code mr-1" />Code
              </button>
              <button
                onClick={() => setViewMode('visual')}
                title="Visual preview — drag tokens onto the layout"
                className={`h-6 px-2.5 text-xs transition-colors ${viewMode === 'visual' ? 'bg-blue-500 text-white' : theme.button_default}`}
              >
                <i className="fa-solid fa-paintbrush mr-1" />Visual
              </button>
            </div>
          </div>

          {/* ── CODE mode: full-height Monaco editor — always mounted, hidden in visual mode ── */}
          <div
            className="relative h-1 flex-auto overflow-hidden"
            style={{ display: viewMode === 'split' ? 'flex' : 'none' }}
          >
            {/* Drag-drop overlay — intercepts drops when user drags a token onto the code editor */}
            {draggingToken !== null && (
              <div
                className="absolute inset-0 z-20 flex flex-col items-center justify-center"
                style={{
                  background: 'rgba(59,130,246,0.08)',
                  border: '2px dashed #3b82f6',
                  cursor: 'copy',
                }}
                onDragOver={e => { e.preventDefault(); e.dataTransfer.dropEffect = 'copy'; }}
                onDrop={e => {
                  e.preventDefault();
                  const token = draggingToken;
                  setDraggingToken(null);
                  setDropIndicatorY(null);
                  if (token) insertAtCursor(token);
                }}
              >
                <i className="fa-solid fa-down-long text-blue-400 text-2xl mb-2" />
                <span className="text-sm font-semibold text-blue-500">Drop here to insert</span>
                <span className="text-xs text-blue-400 mt-1 opacity-80">Token will be inserted at cursor position</span>
              </div>
            )}

            {/* Static hint — shown only when idle in Code mode */}
            {draggingToken === null && viewMode === 'split' && (
              <div className="absolute bottom-3 right-3 z-10 pointer-events-none">
                <span className="inline-flex items-center gap-1.5 text-[10px] px-2 py-1 rounded border border-blue-200 bg-white/90 text-blue-500 shadow-sm">
                  <i className="fa-solid fa-up-down-left-right text-[9px]" />
                  Drag tokens from left panel · drop onto any line
                </span>
              </div>
            )}

            <JsonCodeEditor
              language="html"
              value={templateHtml}
              onChange={handleHtmlChange}
              debounceMs={300}
              monacoTheme={monacoTheme}
              enableTriggerSuggestions
              onMount={(editor, monacoInstance) => {
                editorRef.current = editor;

                // Register {{token}} autocomplete — reads latest tokens via ref on each invocation
                completionDisposable.current?.dispose();
                completionDisposable.current = monacoInstance.languages.registerCompletionItemProvider('html', {
                  triggerCharacters: ['{'],
                  provideCompletionItems: (model: any, position: any) => {
                    const textBefore = model.getValueInRange({
                      startLineNumber: position.lineNumber, startColumn: 1,
                      endLineNumber: position.lineNumber, endColumn: position.column,
                    });
                    if (!textBefore.endsWith('{{')) return { suggestions: [] };
                    return {
                      suggestions: tokensRef.current.map((tok: TokenDescriptor) => ({
                        label: tok.Token,
                        kind: monacoInstance.languages.CompletionItemKind.Variable,
                        insertText: tok.Token.slice(2), // '{{' already typed
                        detail: `${tok.ResultSet} · ${tok.Field}`,
                        documentation: tok.IsList ? 'List token — wrap in #each' : 'Scalar field token',
                        range: {
                          startLineNumber: position.lineNumber, startColumn: position.column,
                          endLineNumber: position.lineNumber, endColumn: position.column,
                        },
                      })),
                    };
                  },
                });

                // Wire native drag-drop — use capture phase so Monaco's internal handlers don't block ours
                const domNode = editor.getDomNode();
                if (domNode) {
                  domNode.addEventListener('dragover', (e: Event) => {
                    const de = e as DragEvent;
                    const text = de.dataTransfer?.types?.includes('text/plain');
                    if (!text) return;
                    de.preventDefault();
                    de.stopPropagation();
                    if (de.dataTransfer) de.dataTransfer.dropEffect = 'copy';
                  }, true); // capture
                  domNode.addEventListener('drop', (e: Event) => {
                    const de = e as DragEvent;
                    const text = de.dataTransfer?.getData('text/plain');
                    if (!text) return; // not our drag — let Monaco handle it
                    de.preventDefault();
                    de.stopPropagation();
                    const target = editor.getTargetAtClientPoint(de.clientX, de.clientY);
                    if (target?.position) {
                      const { lineNumber, column } = target.position;
                      const range = new monacoInstance.Range(lineNumber, column, lineNumber, column);
                      editor.executeEdits('drag-drop', [{ range, text: text + '\n', forceMoveMarkers: true }]);
                    } else {
                      const model = editor.getModel();
                      const lastLine = model?.getLineCount() ?? 1;
                      const lastCol  = model?.getLineMaxColumn(lastLine) ?? 1;
                      const range = new monacoInstance.Range(lastLine, lastCol, lastLine, lastCol);
                      editor.executeEdits('drag-drop', [{ range, text: '\n' + text, forceMoveMarkers: true }]);
                    }
                    editor.focus();
                    schedulePreview(editor.getValue());
                    setDraggingToken(null);
                  }, true); // capture
                }
              }}
            />
          </div>

          {/* ── VISUAL mode: full-height preview iframe + drag-drop overlay — always mounted, hidden in code mode ── */}
          <div className="relative h-1 flex-auto overflow-hidden bg-white"
               style={{ display: viewMode === 'visual' ? 'flex' : 'none' }}>
              <iframe
                ref={iframeRef}
                title="report-preview"
                srcDoc={previewHtml || `<div style="padding:32px;text-align:center;font-family:Arial,sans-serif;color:#9ca3af">
                  <div style="font-size:48px;margin-bottom:12px">📄</div>
                  <p style="font-size:14px;margin:0 0 8px">Select a template from the left panel, or insert blocks to start designing</p>
                  <p style="font-size:12px">Switch to Visual mode and drag tokens onto the layout</p>
                </div>`}
                style={{ width: '100%', height: '100%', border: 'none' }}
                sandbox="allow-same-origin"
              />

              {/* Transparent overlay — captures drag events over the iframe */}
              {draggingToken !== null && (
                <div
                  className="absolute inset-0"
                  style={{ cursor: 'crosshair', zIndex: 20, background: 'rgba(59,130,246,0.04)' }}
                  onDragOver={e => {
                    e.preventDefault();
                    e.dataTransfer.dropEffect = 'copy';
                    const rect = e.currentTarget.getBoundingClientRect();
                    setDropIndicatorY(e.clientY - rect.top);
                  }}
                  onDragLeave={() => setDropIndicatorY(null)}
                  onDrop={e => {
                    e.preventDefault();
                    insertTokenAtDropPosition(draggingToken, e.clientX, e.clientY);
                    setDraggingToken(null);
                    setDropIndicatorY(null);
                  }}
                />
              )}

              {/* Blue drop-position line */}
              {draggingToken !== null && dropIndicatorY !== null && (
                <div
                  className="absolute left-0 right-0 pointer-events-none"
                  style={{ top: dropIndicatorY, height: 2, background: '#3b82f6', boxShadow: '0 0 6px rgba(59,130,246,0.5)', zIndex: 21 }}
                >
                  <span className="absolute right-2 -top-5 text-[10px] font-medium text-white bg-blue-500 px-1.5 py-0.5 rounded shadow">
                    Drop to insert
                  </span>
                </div>
              )}

              {/* Idle hint */}
              {draggingToken === null && (
                <div className="absolute bottom-3 right-3 pointer-events-none">
                  <span className="text-[10px] px-2 py-1 rounded border border-blue-200 bg-white/90 text-blue-500 shadow-sm">
                    <i className="fa-solid fa-hand mr-1" />Drag tokens or blocks from the left panel
                  </span>
                </div>
              )}
            </div>

          {/* ── DESIGN mode: GrapeJS WYSIWYG — always mounted, hidden when not in design mode ── */}
          <div
            className="h-1 flex-auto overflow-hidden relative"
            style={{ display: viewMode === 'design' ? 'flex' : 'none' }}
          >
            <Suspense fallback={<div className={`w-full h-full flex items-center justify-center text-xs ${theme.label} opacity-60`}><i className="fa-solid fa-spinner fa-spin mr-2" />Loading visual editor…</div>}>
              <GrapeJsEditor
                ref={gjsEditorComponentRef as any}
                html={templateHtml}
                isDark={currentThemeId === 'dark'}
                active={viewMode === 'design'}
                onChange={html => { setTemplateHtml(html); schedulePreview(html); }}
                onEditorReady={ed => {
                  gjsEditorRef.current = ed;
                  const buildPath = () => {
                    const sel = ed.getSelected?.();
                    if (!sel) { setGjsSelectedPath([]); return; }
                    const path: string[] = [];
                    let cur = sel;
                    while (cur) {
                      const tag = (cur.get?.('tagName') ?? '').toUpperCase();
                      if (tag && tag !== 'BODY') path.unshift(tag);
                      const parent = cur.parent?.();
                      if (!parent?.get?.('tagName')) break;
                      cur = parent;
                    }
                    setGjsSelectedPath(path);
                  };
                  ed.on('component:selected', buildPath);
                  ed.on('component:deselected', () => setGjsSelectedPath([]));
                }}
              />
            </Suspense>

            {/* ── Style Panel ── floats over canvas, anchored top-right */}
            {stylesPanelOpen && (
              <div
                className={`absolute top-2 right-2 z-40 w-60 rounded-lg shadow-2xl border flex flex-col overflow-auto ${t('border_mainContentSection')} ${theme.mainContentSection}`}
                style={{ maxHeight: 'calc(100% - 16px)' }}
              >
                {/* Header */}
                <div className={`flex items-center justify-between px-3 py-2 border-b shrink-0 ${t('border_mainContentSection')}`}>
                  <span className={`text-xs font-semibold ${theme.title}`}>
                    <i className="fa-solid fa-palette mr-1.5 text-purple-400" />Page Style
                  </span>
                  <button onClick={() => setStylesPanelOpen(false)} className={`px-1.5 py-0.5 text-xs rounded ${theme.button_default}`}>
                    <i className="fa-solid fa-xmark" />
                  </button>
                </div>

                <div className="p-3 space-y-4 overflow-y-auto">
                  {/* Typography */}
                  <div>
                    <p className={`text-[10px] font-semibold uppercase tracking-wider mb-2 ${theme.label} opacity-60`}>Typography</p>
                    <div className="space-y-2">
                      <div className="flex items-center gap-2">
                        <label className={`text-xs w-14 shrink-0 ${theme.label}`}>Font</label>
                        <select
                          className={`flex-auto h-6 px-1.5 text-[10px] border rounded-[4px] ${theme.inputBox}`}
                          value={pageStyle.fontFamily}
                          onChange={e => applyPageStyle({ ...pageStyle, fontFamily: e.target.value })}
                        >
                          {FONT_FAMILIES.map(f => <option key={f.value} value={f.value}>{f.label}</option>)}
                        </select>
                      </div>
                      <div className="flex items-center gap-2">
                        <label className={`text-xs w-14 shrink-0 ${theme.label}`}>Size</label>
                        <input
                          type="number" min={8} max={24}
                          className={`w-14 h-6 px-1.5 text-xs border rounded-[4px] ${theme.inputBox}`}
                          value={pageStyle.fontSize}
                          onChange={e => applyPageStyle({ ...pageStyle, fontSize: Number(e.target.value) })}
                        />
                        <span className={`text-[10px] ${theme.label} opacity-50`}>px</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <label className={`text-xs w-14 shrink-0 ${theme.label}`}>Text</label>
                        <input type="color"
                          className="w-8 h-6 rounded border cursor-pointer p-0"
                          value={pageStyle.textColor}
                          onChange={e => applyPageStyle({ ...pageStyle, textColor: e.target.value })}
                        />
                        <span className={`text-[10px] font-mono ${theme.label} opacity-60`}>{pageStyle.textColor}</span>
                      </div>
                    </div>
                  </div>

                  {/* Colors */}
                  <div>
                    <p className={`text-[10px] font-semibold uppercase tracking-wider mb-2 ${theme.label} opacity-60`}>Colors</p>
                    <div className="space-y-2">
                      {([
                        { key: 'primaryColor', label: 'Primary', hint: 'headings · table headers' },
                        { key: 'altRowColor',  label: 'Alt row',  hint: 'even table rows' },
                        { key: 'borderColor',  label: 'Border',   hint: 'table cell borders' },
                      ] as { key: keyof PageStyleValues; label: string; hint: string }[]).map(({ key, label, hint }) => (
                        <div key={key} className="flex items-center gap-2">
                          <label className={`text-xs w-14 shrink-0 ${theme.label}`} title={hint}>{label}</label>
                          <input type="color"
                            className="w-8 h-6 rounded border cursor-pointer p-0 shrink-0"
                            value={pageStyle[key] as string}
                            onChange={e => applyPageStyle({ ...pageStyle, [key]: e.target.value })}
                          />
                          <span className={`text-[10px] font-mono ${theme.label} opacity-60`}>{pageStyle[key]}</span>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Preview swatches */}
                  <div>
                    <p className={`text-[10px] font-semibold uppercase tracking-wider mb-2 ${theme.label} opacity-60`}>Preview</p>
                    <div className="rounded border overflow-hidden text-[10px]" style={{ borderColor: pageStyle.borderColor }}>
                      <div className="flex">
                        <div className="px-2 py-1 font-bold text-white shrink-0 w-1/2" style={{ background: pageStyle.primaryColor }}>Col A</div>
                        <div className="px-2 py-1 font-bold text-white w-1/2" style={{ background: pageStyle.primaryColor }}>Col B</div>
                      </div>
                      <div className="flex" style={{ borderTop: `1px solid ${pageStyle.borderColor}` }}>
                        <div className="px-2 py-1 w-1/2" style={{ color: pageStyle.textColor, fontFamily: pageStyle.fontFamily }}>Row 1</div>
                        <div className="px-2 py-1 w-1/2" style={{ color: pageStyle.textColor, fontFamily: pageStyle.fontFamily }}>Data</div>
                      </div>
                      <div className="flex" style={{ background: pageStyle.altRowColor, borderTop: `1px solid ${pageStyle.borderColor}` }}>
                        <div className="px-2 py-1 w-1/2" style={{ color: pageStyle.textColor, fontFamily: pageStyle.fontFamily }}>Row 2</div>
                        <div className="px-2 py-1 w-1/2" style={{ color: pageStyle.textColor, fontFamily: pageStyle.fontFamily }}>Data</div>
                      </div>
                    </div>
                  </div>

                  {/* Reset */}
                  <button
                    onClick={() => applyPageStyle(DEFAULT_PAGE_STYLE)}
                    className={`w-full text-xs px-2 py-1.5 rounded-[4px] border ${t('border_mainContentSection')} ${theme.contextMenu} hover:border-blue-400`}
                  >
                    <i className="fa-solid fa-rotate-left mr-1 opacity-60" />Reset to defaults
                  </button>
                </div>
              </div>
            )}
          </div>

        </div>
      </div>

      {/* Floating pointer-drag chip — follows cursor in Design mode when dragging a token.
          Uses position:fixed so it escapes any overflow:hidden ancestor. */}
      {pointerDragPos && viewMode === 'design' && draggingToken && (
        <div
          style={{
            position: 'fixed',
            left: pointerDragPos.x + 14,
            top: pointerDragPos.y - 10,
            zIndex: 9999,
            pointerEvents: 'none',
          }}
          className="px-2 py-1 bg-blue-600 text-white text-xs rounded shadow-lg select-none font-mono"
        >
          {draggingToken}
        </div>
      )}
    </div>
  );
};

export default ReportTemplateDesigner;
