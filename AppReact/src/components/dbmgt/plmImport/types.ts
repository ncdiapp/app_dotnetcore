import type { PlmImportSessionDto, PlmImportJobDto, PlmTableExportPlanItemDto } from '../../../webapi/plmMigrationSvc';

export type PlmImportStepCode = 'Connect' | 'Entity' | 'Template' | 'OtherData';

export interface PlmImportWizardState {
  session: PlmImportSessionDto | null;
  currentStepCode: PlmImportStepCode;
  targetCompanyId: number | null;
  saasApplicationId: number | null;
  plmConnectionString: string;
  connectionTested: boolean;
  systemDefineComplete: boolean;
}

/** Entity step UI persisted across main app tab switches. */
export interface PlmImportEntityStepUiState {
  activeTab: 'system' | 'user';
  planItems: PlmTableExportPlanItemDto[];
  activeJob: PlmImportJobDto | null;
  isExporting: boolean;
}

export interface PlmImportPageCache {
  wizardState: PlmImportWizardState;
  entityStepUi: PlmImportEntityStepUiState;
}

export const createInitialEntityStepUi = (): PlmImportEntityStepUiState => ({
  activeTab: 'system',
  planItems: [],
  activeJob: null,
  isExporting: false,
});

export const PLM_IMPORT_PAGE_CACHE_SUFFIX = '-PlmDataImportManagement';

export const PLM_IMPORT_STEP_ORDER: PlmImportStepCode[] = [
  'Connect',
  'Entity',
  'Template',
  'OtherData',
];
