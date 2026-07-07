import React, { useState, useEffect } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { appReportSvc } from '../../webapi/appReportSvc';

interface Props {
  transactionId: number | string;
  mainReferenceId: number;
  masterReferenceId?: number;
  onClose: () => void;
}

interface ReportItem {
  ReportId: number;
  ReportDisplayName: string;
  ReportTemplate?: {
    PageSize?: string;
    Orientation?: string;
    MarginMm?: number;
  };
}

const TabPrintDialog: React.FC<Props> = ({ transactionId, mainReferenceId, masterReferenceId, onClose }) => {
  const { theme, t } = useTheme();
  const [reports, setReports] = useState<ReportItem[]>([]);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    appReportSvc.getLinkedReports(transactionId)
      .then(data => {
        const items: ReportItem[] = (data || []).map((d: any) => ({
          ReportId: d.AppReport?.ReportId ?? d.ReportId,
          ReportDisplayName: d.AppReport?.ReportName ?? d.ReportName ?? `Report ${d.ReportId}`,
          ReportTemplate: d.AppReport?.ReportTemplate,
        }));
        setReports(items);
        if (items.length > 0) setSelected(new Set([items[0].ReportId]));
      })
      .catch(() => setError('Failed to load reports'))
      .finally(() => setLoading(false));
  }, [transactionId]);

  const toggle = (id: number) => {
    setSelected(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

  const firstSelected = reports.find(r => selected.has(r.ReportId));
  const pageSize   = firstSelected?.ReportTemplate?.PageSize   ?? 'A4';
  const orientation = firstSelected?.ReportTemplate?.Orientation ?? 'portrait';
  const marginMm   = firstSelected?.ReportTemplate?.MarginMm   ?? 15;

  const handlePrintInBrowser = () => {
    if (selected.size === 0) return;
    const ids = Array.from(selected).join(',');
    const param = btoa(JSON.stringify({ id: transactionId, reportIds: ids, mainRef: mainReferenceId }));
    window.open(`/FormMasterDetailPrint/${encodeURIComponent(param)}`, '_blank');
    onClose();
  };

  const handleSaveAsPdf = async () => {
    if (selected.size === 0) return;
    setGenerating(true);
    setError(null);
    try {
      await appReportSvc.generatePdf({
        mainReferenceId,
        masterReferenceId,
        sections: Array.from(selected).map(reportId => ({ reportId, referenceId: mainReferenceId })),
        pageSize,
        orientation,
        marginMm,
      });
      onClose();
    } catch {
      setError('PDF generation failed. Please try again.');
    } finally {
      setGenerating(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className={`w-[480px] rounded-[4px] shadow-xl flex flex-col ${theme.mainContentSection}`}>

        {/* Header */}
        <div className={`flex items-center justify-between px-4 py-3 border-b ${t('border_mainContentSection')}`}>
          <span className={`font-medium text-sm ${theme.label}`}>Print / Save as PDF</span>
          <button onClick={onClose} className={`text-xs px-2 py-1 rounded ${theme.button_default}`}>
            <i className="fa-solid fa-xmark" />
          </button>
        </div>

        {/* Report list */}
        <div className="px-4 py-3 h-1 flex-auto overflow-y-auto">
          {loading && <p className={`text-xs ${theme.label}`}>Loading reports…</p>}
          {!loading && reports.length === 0 && (
            <p className={`text-xs ${theme.label}`}>No reports linked to this record.</p>
          )}
          {reports.map(r => (
            <label key={r.ReportId} className={`flex items-center gap-2 py-1.5 cursor-pointer text-sm ${theme.label}`}>
              <input
                type="checkbox"
                checked={selected.has(r.ReportId)}
                onChange={() => toggle(r.ReportId)}
                className="accent-blue-500"
              />
              {r.ReportDisplayName}
            </label>
          ))}
        </div>

        {/* Error */}
        {error && (
          <p className="px-4 text-xs text-red-500">{error}</p>
        )}

        {/* Footer */}
        <div className={`flex justify-end gap-2 px-4 py-3 border-t ${t('border_mainContentSection')}`}>
          <button
            onClick={handlePrintInBrowser}
            disabled={selected.size === 0}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            <i className="fa-solid fa-print mr-1" />
            Print in Browser
          </button>
          <button
            onClick={handleSaveAsPdf}
            disabled={selected.size === 0 || generating}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            {generating
              ? <><i className="fa-solid fa-spinner fa-spin mr-1" />Generating…</>
              : <><i className="fa-solid fa-file-pdf mr-1" />Save as PDF</>
            }
          </button>
        </div>
      </div>
    </div>
  );
};

export default TabPrintDialog;
