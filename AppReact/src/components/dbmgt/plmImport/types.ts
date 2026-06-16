import type {
  PlmImportSessionDto,
  PlmImportJobDto,
  PlmTableExportPlanItemDto,
  PlmSystemDefineEntityPreviewItemDto,
} from '../../../webapi/plmMigrationSvc';

export type PlmImportStepCode = 'Connect' | 'Entity' | 'Template' | 'OtherData';

export interface PlmImportWizardState {
  session: PlmImportSessionDto | null;
  currentStepCode: PlmImportStepCode;
  targetCompanyId: number | null;
  saasApplicationId: number | null;
  plmConnectionString: string;
  connectionTested: boolean;
  systemDefineTablesComplete: boolean;
  systemDefineEntitiesComplete: boolean;
}

export type PlmSystemDefineWorkflowStep = 1 | 2;

/** Map legacy 4-step cache values (3/4) to the 2-step workflow. */
export const normalizeViewingWorkflowStep = (step: number | undefined): PlmSystemDefineWorkflowStep =>
  step === 2 ? 2 : 1;

/** Entity step UI persisted across main app tab switches. */
export interface PlmImportEntityStepUiState {
  activeTab: 'system' | 'user';
  /** Which workflow step result panel is shown (1 = tables, 2 = entities). */
  viewingWorkflowStep: PlmSystemDefineWorkflowStep;
  planItems: PlmTableExportPlanItemDto[];
  entityPlanItems: PlmSystemDefineEntityPreviewItemDto[];
  activeJob: PlmImportJobDto | null;
  isExporting: boolean;
  isEntityImporting: boolean;
}

export interface PlmImportPageCache {
  wizardState: PlmImportWizardState;
  entityStepUi: PlmImportEntityStepUiState;
}

export const createInitialEntityStepUi = (): PlmImportEntityStepUiState => ({
  activeTab: 'system',
  viewingWorkflowStep: 1,
  planItems: [],
  entityPlanItems: [],
  activeJob: null,
  isExporting: false,
  isEntityImporting: false,
});

export const buildPlmImportStepStateJson = (state: Pick<
  PlmImportWizardState,
  'connectionTested' | 'systemDefineTablesComplete' | 'systemDefineEntitiesComplete'
>): string => JSON.stringify({
  connectionTested: state.connectionTested,
  systemDefineTablesComplete: state.systemDefineTablesComplete,
  systemDefineEntitiesComplete: state.systemDefineEntitiesComplete,
  systemDefineComplete: state.systemDefineTablesComplete,
});

export const PLM_IMPORT_PAGE_CACHE_SUFFIX = '-PlmDataImportManagement';

export const PLM_IMPORT_STEP_ORDER: PlmImportStepCode[] = [
  'Connect',
  'Entity',
  'Template',
  'OtherData',
];
