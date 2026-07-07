import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { JsonCodeEditor } from '../common/JsonCodeEditor';
import { appReportSvc } from '../../webapi/appReportSvc';
import DataSourceEditor, {
  DataSourceDef,
  parseSourcesFromConfig,
  serializeConfig,
} from './DataSourceEditor';

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
    {{#each rs1}}
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

const STARTER_TEMPLATES: { label: string; icon: string; html: string }[] = [
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
  {{#each rs1}}<tr><td>{{Position}}</td><td>{{Code}}</td><td>{{Description}}</td><td>{{Colour}}</td></tr>{{/each}}
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
  {{#each rs1}}<tr><td>{{Sku}}</td><td>{{Description}}</td><td>{{Quantity}}</td><td>{{UnitPrice}}</td><td>{{Total}}</td></tr>{{/each}}
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
  {{#each rs1}}<tr><td>{{Field1}}</td><td>{{Field2}}</td><td>{{Field3}}</td></tr>{{/each}}
</table>`,
  },
];

// ────────────────────────────────────────────────────────────────────────────

const ReportTemplateDesigner: React.FC<Props> = ({ reportId, mainReferenceId = 0, onSaved, onClose }) => {
  const { theme, t } = useTheme();

  const [report, setReport]                 = useState<any>(null);
  const [templateHtml, setTemplateHtml]     = useState('');
  const [dataSources, setDataSources]       = useState<DataSourceDef[]>([{ alias: 'main', type: 'sp', value: '' }]);
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
  const [dsExpanded, setDsExpanded]         = useState(false);
  const [pdfPreviewing, setPdfPreviewing]   = useState(false);

  const editorRef    = useRef<any>(null);
  const previewTimer = useRef<number | null>(null);

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
    appReportSvc.previewHtml({
      reportId,
      mainReferenceId: refId,
      templateHtmlOverride: html,
      dataSpNameOverride: primarySp || undefined,
    }).then(setPreviewHtml).catch(() => setPreviewHtml(html));
  }, [reportId, dataSources]);

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
    const hasSp = dataSources.some(s => s.type === 'sp' && s.value.trim());
    if (!hasSp) return;
    setTokenLoading(true);
    appReportSvc.getTokens(reportId)
      .then(setTokens)
      .catch(() => setTokens([]))
      .finally(() => setTokenLoading(false));
  }, [reportId, dataSources]);

  const insertAtCursor = (text: string) => {
    if (editorRef.current) {
      const editor = editorRef.current;
      const sel = editor.getSelection();
      editor.executeEdits('insert', [{ range: sel, text, forceMoveMarkers: true }]);
      editor.focus();
    } else {
      setTemplateHtml(prev => prev + '\n' + text);
    }
    schedulePreview(templateHtml + '\n' + text);
  };

  const applyStarterTemplate = (html: string) => {
    setTemplateHtml(html);
    schedulePreview(html);
  };

  const handlePdfPreview = async () => {
    setPdfPreviewing(true);
    setError(null);
    try {
      const primarySp = dataSources.find(s => s.type === 'sp')?.value;
      const blobUrl = await appReportSvc.previewPdf({
        reportId,
        mainReferenceId: Number(previewRefId) || 0,
        templateHtmlOverride: templateHtml,
        dataSpNameOverride: primarySp || undefined,
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
    setSaving(true);
    setError(null);
    try {
      const newConfig   = serializeConfig(dataSources, extraParamConfig);
      const primarySpName = dataSources.find(s => s.type === 'sp')?.value ?? '';
      await appReportSvc.saveReportTemplate({
        ...report,
        ReportTemplate: {
          ReportId:         reportId,
          TemplateHtml:     templateHtml,
          DataSpName:       primarySpName,  // primary SP for legacy display
          PageSize:         pageSize,
          Orientation:      orientation,
          MarginMm:         marginMm,
          ExtraParamConfig: newConfig || null,
        },
      });
      onSaved?.();
    } catch {
      setError('Save failed.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className={`p-4 text-sm ${theme.label}`}>Loading…</div>;

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>

      {/* ── Toolbar ── */}
      <div className={`flex items-center gap-2 px-3 py-2 flex-shrink-0 border-b ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
        <span className={`text-sm font-semibold ${theme.title} mr-1 shrink-0`}>
          {report?.ReportName ?? 'Report Designer'}
        </span>
        {/* Data Sources toggle */}
        <button
          onClick={() => setDsExpanded(v => !v)}
          className={`h-7 px-2 text-xs border rounded-[4px] shrink-0 flex items-center gap-1 ${theme.inputBox}`}
          title="Configure data sources"
        >
          <i className="fa-solid fa-database text-blue-400 mr-1" />
          {dataSources.filter(s => s.value).length} Source{dataSources.filter(s => s.value).length !== 1 ? 's' : ''}
          <i className={`fa-solid fa-chevron-${dsExpanded ? 'up' : 'down'} text-[9px] ml-0.5`} />
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
          onClick={handlePdfPreview}
          disabled={pdfPreviewing || !templateHtml}
          title="Generate PDF and open in new tab"
          className={`px-3 py-1.5 text-sm rounded-[4px] shrink-0 border border-red-300 text-red-500
            hover:bg-red-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors`}
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

      {/* ── Data Sources Panel (expandable) ── */}
      {dsExpanded && (
        <div className={`px-3 py-3 border-b flex-shrink-0 ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
          <div className={`text-xs font-medium mb-2 ${theme.title}`}>
            <i className="fa-solid fa-database mr-1.5 text-blue-400" />Data Sources
            <span className={`ml-2 font-normal opacity-60 ${theme.label}`}>
              Primary → {'{{header.…}}'}, {'{{rs1}}'} &nbsp;·&nbsp; Additional → {'{{alias.…}}'}, {'{{#each alias_rs1}}'}</span>
          </div>
          <DataSourceEditor
            sources={dataSources}
            onChange={srcs => { setDataSources(srcs); }}
            compact
          />
          <button
            className={`mt-2 px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`}
            onClick={() => { refreshTokens(); setDsExpanded(false); }}
          >
            <i className="fa-solid fa-check mr-1" />Apply & Load Tokens
          </button>
        </div>
      )}

      {/* ── Main body ── */}
      <div className="flex h-1 flex-auto overflow-hidden">

        {/* ── Left panel: Blocks / Tokens ── */}
        <div className={`w-52 shrink-0 flex flex-col border-r overflow-hidden ${t('border_mainContentSection')} ${theme.mainContentSection}`}>

          {/* Tab strip */}
          <div className={`flex border-b shrink-0 ${t('border_mainContentSection')}`}>
            {(['blocks', 'tokens'] as const).map(tab => (
              <button
                key={tab}
                onClick={() => setLeftTab(tab)}
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

                {/* Starter templates */}
                <div>
                  <p className={`text-[10px] font-semibold uppercase tracking-wider mb-1.5 ${theme.label} opacity-60`}>Start from template</p>
                  <div className="space-y-1">
                    {STARTER_TEMPLATES.map(tmpl => (
                      <button
                        key={tmpl.label}
                        onClick={() => applyStarterTemplate(tmpl.html)}
                        className={`w-full text-left px-2 py-1.5 text-xs rounded-[4px] border ${t('border_mainContentSection')} ${theme.contextMenu} hover:border-blue-400 transition-colors`}
                      >
                        <span className="mr-1.5">{tmpl.icon}</span>{tmpl.label}
                      </button>
                    ))}
                  </div>
                </div>

                {/* Insert blocks */}
                <div>
                  <p className={`text-[10px] font-semibold uppercase tracking-wider mb-1.5 ${theme.label} opacity-60`}>Insert block</p>
                  <div className="space-y-1">
                    {REPORT_BLOCKS.map(block => (
                      <button
                        key={block.label}
                        onClick={() => insertAtCursor(block.html)}
                        className={`w-full text-left px-2 py-1.5 text-xs rounded-[4px] border ${t('border_mainContentSection')} ${theme.contextMenu} hover:border-blue-400 transition-colors`}
                      >
                        <i className={`${block.icon} ${block.color} mr-1.5 w-3`} />
                        {block.label}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* ── Tokens tab ── */}
            {leftTab === 'tokens' && (
              <div className="p-1">
                <div className="flex items-center justify-between px-1 py-1 mb-1">
                  <span className={`text-xs ${theme.label}`}>SP tokens</span>
                  <button onClick={refreshTokens} disabled={tokenLoading} className={`text-xs px-1.5 py-0.5 rounded ${theme.button_default}`} title="Load from SP">
                    <i className={`fa-solid fa-rotate ${tokenLoading ? 'fa-spin' : ''}`} />
                  </button>
                </div>
                {tokens.length === 0 ? (
                  <p className={`text-xs ${theme.label} px-2 py-1 opacity-70`}>
                    {dataSources.some(s => s.value.trim()) ? 'Click ↻ to load' : 'Configure a source first'}
                  </p>
                ) : (
                  <div className="space-y-0.5">
                    {tokens.map((tok, i) => (
                      <button key={i} onClick={() => insertAtCursor(tok.Token)}
                        title={`${tok.ResultSet}.${tok.Field}`}
                        className={`w-full text-left px-2 py-1 text-xs rounded truncate ${theme.contextMenu}`}>
                        {tok.InsideEach ? <span className="opacity-50 mr-1">↳</span>
                          : tok.IsList ? <i className="fa-solid fa-list mr-1 text-blue-400" />
                          : <i className="fa-solid fa-tag mr-1 text-green-400" />}
                        {tok.Field === '*' ? `#each ${tok.ResultSet}` : tok.Field}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* ── Right side: Preview (top 55%) + Code editor (bottom 45%) ── */}
        <div className="flex-auto flex flex-col overflow-hidden">

          {/* Preview area */}
          <div className="flex flex-col" style={{ height: '55%' }}>
            <div className={`flex items-center gap-2 px-3 py-1 text-xs shrink-0 border-b ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
              <span className={`font-medium ${theme.label}`}>
                <i className="fa-solid fa-eye mr-1 text-blue-400" />Preview — {pageSize} {orientation}
              </span>
              <div className="flex items-center gap-1 ml-2">
                <span className={`${theme.label} opacity-70`}>Ref ID:</span>
                <input
                  type="number"
                  className={`h-6 w-20 px-1.5 text-xs border rounded-[4px] ${theme.inputBox}`}
                  value={previewRefId}
                  onChange={e => setPreviewRefId(e.target.value)}
                  placeholder="42"
                />
                <button
                  onClick={() => triggerPreview(templateHtml, Number(previewRefId) || 0)}
                  className={`h-6 px-2 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-rotate mr-1" />Refresh
                </button>
              </div>
            </div>
            <div className="h-1 flex-auto overflow-auto bg-white">
              <iframe
                title="report-preview"
                srcDoc={previewHtml || `<div style="padding:32px;text-align:center;font-family:Arial,sans-serif;color:#9ca3af">
                  <div style="font-size:48px;margin-bottom:12px">📄</div>
                  <p style="font-size:14px;margin:0 0 8px">Select a template from the left panel, or insert blocks to start designing</p>
                  <p style="font-size:12px">Preview updates automatically as you type</p>
                </div>`}
                style={{ width: '100%', height: '100%', border: 'none' }}
                sandbox="allow-same-origin"
              />
            </div>
          </div>

          {/* Code editor area */}
          <div className="flex flex-col" style={{ height: '45%' }}>
            <div className={`flex items-center px-3 py-1 text-xs shrink-0 border-t border-b ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
              <i className="fa-solid fa-code mr-1.5 text-gray-400" />
              <span className={`font-medium ${theme.label}`}>HTML / CSS</span>
              <span className={`ml-2 opacity-50 ${theme.label}`}>— click blocks above to insert, or type directly</span>
            </div>
            <div className="h-1 flex-auto overflow-hidden">
              <JsonCodeEditor
                language="html"
                value={templateHtml}
                onChange={handleHtmlChange}
                debounceMs={300}
                onMount={(editor) => { editorRef.current = editor; }}
              />
            </div>
          </div>

        </div>
      </div>
    </div>
  );
};

export default ReportTemplateDesigner;
