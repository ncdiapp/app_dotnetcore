import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

class AppReportService {

  async getLinkedReports(transactionId: number | string): Promise<any[]> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllAppTranscationReportListByTransactionId?transactionId=${transactionId}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to load linked reports');
    return response.json();
  }

  async getReportTemplate(reportId: number): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppReportExDto?reportId=${reportId}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to load report template');
    return response.json();
  }

  async saveReportTemplate(dto: any): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/Administration/SaveOneAppReportEntityDto`,
      { method: 'POST', headers: getHeaders(), body: JSON.stringify(dto) }
    );
    if (!response.ok) throw new Error('Failed to save report template');
    return response.json();
  }

  async getTokens(reportId: number, mainReferenceId = 0, masterReferenceId?: number): Promise<any[]> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppReport/GetTokens?reportId=${reportId}&mainReferenceId=${mainReferenceId}${masterReferenceId != null ? `&masterReferenceId=${masterReferenceId}` : ''}`,
      { headers: getHeaders() }
    );
    if (!response.ok) return [];
    return response.json();
  }

  /** Discover tokens from the designer's current (unsaved) config — includes sampleJson for API sources. */
  async getTokensFromConfig(
    extraParamConfig: string,
    dataSpName?: string,
    mainReferenceId = 0,
    masterReferenceId?: number,
  ): Promise<any[]> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppReport/GetTokensFromConfig`,
      {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ extraParamConfig, dataSpName, mainReferenceId, masterReferenceId }),
      }
    );
    if (!response.ok) return [];
    return response.json();
  }

  async previewHtml(payload: {
    reportId: number;
    mainReferenceId: number;
    masterReferenceId?: number;
    templateHtmlOverride?: string;
    dataSpNameOverride?: string;
    extraParamConfigOverride?: string;
    extraParams?: Record<string, string>;
  }): Promise<string> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppReport/PreviewHtml`,
      { method: 'POST', headers: getHeaders(), body: JSON.stringify(payload) }
    );
    if (!response.ok) throw new Error('Preview failed');
    return response.text();
  }

  async previewPdf(payload: {
    reportId: number;
    mainReferenceId: number;
    masterReferenceId?: number;
    templateHtmlOverride?: string;
    dataSpNameOverride?: string;
    extraParamConfigOverride?: string;
    pageSize?: string;
    orientation?: string;
    marginMm?: number;
    extraParams?: Record<string, string>;
  }): Promise<string> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppReport/PreviewPdf`,
      { method: 'POST', headers: getHeaders(), body: JSON.stringify(payload) }
    );
    if (!response.ok) throw new Error('PDF preview failed');
    const blob = await response.blob();
    return URL.createObjectURL(blob);
  }

  async generatePdf(payload: {
    mainReferenceId: number;
    masterReferenceId?: number;
    sections: Array<{ reportId: number; referenceId?: number }>;
    pageSize?: string;
    orientation?: string;
    marginMm?: number;
    extraParams?: Record<string, string>;
  }): Promise<void> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppReport/GeneratePdf`,
      { method: 'POST', headers: getHeaders(), body: JSON.stringify(payload) }
    );
    if (!response.ok) throw new Error('PDF generation failed');
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'report.pdf';
    a.click();
    URL.revokeObjectURL(url);
  }
}

export const appReportSvc = new AppReportService();
