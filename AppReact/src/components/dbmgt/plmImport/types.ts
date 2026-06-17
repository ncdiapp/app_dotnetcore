import type {
  PlmImportSessionDto,
  PlmImportJobDto,
  PlmTableExportPlanItemDto,
  PlmSystemDefineEntityPreviewItemDto,
  PlmUserDefineEntityPreviewItemDto,
} from '../../../webapi/plmMigrationSvc';

export type PlmImportStepCode = 'Connect' | 'Entity' | 'Template' | 'OtherData';

export const PLM_DEFAULT_TABLE_PREFIX = 'Plm_';
export const PLM_DEFAULT_ENTITY_WIDE_TABLE_PREFIX = 'Plm_entity_';

export const normalizePlmImportTablePrefix = (
  value: string | undefined | null,
  fallback: string,
): string => {
  const trimmed = (value ?? '').trim();
  if (!trimmed) return fallback;
  const sanitized = trimmed.replace(/[^A-Za-z0-9_]/g, '');
  if (!sanitized) return fallback;
  return sanitized.length <= 30 ? sanitized : sanitized.slice(0, 30);
};

export interface PlmImportWizardState {
  session: PlmImportSessionDto | null;
  currentStepCode: PlmImportStepCode;
  targetCompanyId: number | null;
  saasApplicationId: number | null;
  plmConnectionString: string;
  tablePrefix: string;
  entityWideTablePrefix: string;
  connectionTested: boolean;
  systemDefineTablesComplete: boolean;
  systemDefineEntitiesComplete: boolean;
  userDefineEntitiesComplete: boolean;
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
  userDefinePlanItems: PlmUserDefineEntityPreviewItemDto[];
  activeJob: PlmImportJobDto | null;
  isExporting: boolean;
  isEntityImporting: boolean;
  isUserDefineImporting: boolean;
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
  userDefinePlanItems: [],
  activeJob: null,
  isExporting: false,
  isEntityImporting: false,
  isUserDefineImporting: false,
});

export const buildPlmImportStepStateJson = (state: Pick<
  PlmImportWizardState,
  | 'connectionTested'
  | 'systemDefineTablesComplete'
  | 'systemDefineEntitiesComplete'
  | 'userDefineEntitiesComplete'
  | 'tablePrefix'
  | 'entityWideTablePrefix'
>): string => JSON.stringify({
  connectionTested: state.connectionTested,
  systemDefineTablesComplete: state.systemDefineTablesComplete,
  systemDefineEntitiesComplete: state.systemDefineEntitiesComplete,
  userDefineEntitiesComplete: state.userDefineEntitiesComplete,
  systemDefineComplete: state.systemDefineTablesComplete,
  tablePrefix: normalizePlmImportTablePrefix(state.tablePrefix, PLM_DEFAULT_TABLE_PREFIX),
  entityWideTablePrefix: normalizePlmImportTablePrefix(
    state.entityWideTablePrefix,
    PLM_DEFAULT_ENTITY_WIDE_TABLE_PREFIX,
  ),
});

export const PLM_IMPORT_PAGE_CACHE_SUFFIX = '-PlmDataImportManagement';

export const PLM_IMPORT_STEP_ORDER: PlmImportStepCode[] = [
  'Connect',
  'Entity',
  'Template',
  'OtherData',
];
