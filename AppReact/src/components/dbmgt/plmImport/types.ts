import type {
  PlmImportSessionDto,
  PlmImportJobDto,
  PlmTableExportPlanItemDto,
  PlmSystemDefineEntityPreviewItemDto,
  PlmUserDefineEntityPreviewItemDto,
  PlmDwImportBlueprintDto,
  PlmDwBlueprintPreviewItemDto,
  PlmDwBlueprintExecuteResultDto,
} from '../../../webapi/plmMigrationSvc';

export type PlmImportStepCode =
  | 'Connect'
  | 'Entity'
  | 'DwBlueprint'
  | 'FolderImport'
  | 'ColorImport'
  | 'ImageImport'
  /** @deprecated legacy step code (replaced by ImageImport) */
  | 'OtherData';

/** Legacy session/cache step code from before DW Blueprint step. */
export const normalizePlmImportStepCode = (code: string | undefined | null): PlmImportStepCode => {
  if (code === 'Template') return 'DwBlueprint';
  if (code === 'ImageImport') return 'ImageImport';
  if (code === 'OtherData') return 'ImageImport';
  if (code === 'FolderImport') return 'FolderImport';
  if (code === 'ColorImport') return 'ColorImport';
  if (code === 'Connect' || code === 'Entity' || code === 'DwBlueprint') {
    return code;
  }
  return 'Connect';
};

export const PLM_DEFAULT_TABLE_PREFIX = 'Plm_';
/** Hardcoded suffix appended to TablePrefix for User Define wide entity tables. */
export const PLM_ENTITY_WIDE_SUFFIX = 'Entity_';

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

export const deriveEntityWideTablePrefix = (tablePrefix: string | undefined | null): string =>
  normalizePlmImportTablePrefix(tablePrefix, PLM_DEFAULT_TABLE_PREFIX) + PLM_ENTITY_WIDE_SUFFIX;

export interface PlmImportWizardState {
  session: PlmImportSessionDto | null;
  currentStepCode: PlmImportStepCode;
  targetCompanyId: number | null;
  saasApplicationId: number | null;
  plmConnectionString: string;
  tablePrefix: string;
  connectionTested: boolean;
  systemDefineTablesComplete: boolean;
  systemDefineEntitiesComplete: boolean;
  userDefineEntitiesComplete: boolean;
  templatesComplete: boolean;
}

export type PlmSystemDefineWorkflowStep = 1 | 2;

/** @deprecated Legacy cache — no longer used in UI */
export const normalizeViewingWorkflowStep = (step: number | undefined): PlmSystemDefineWorkflowStep =>
  step === 2 ? 2 : 1;

/** Entity step UI persisted across main app tab switches. */
export interface PlmImportEntityStepUiState {
  systemSectionExpanded: boolean;
  userSectionExpanded: boolean;
  planItems: PlmTableExportPlanItemDto[];
  entityPlanItems: PlmSystemDefineEntityPreviewItemDto[];
  userDefinePlanItems: PlmUserDefineEntityPreviewItemDto[];
  activeJob: PlmImportJobDto | null;
  isExporting: boolean;
  isEntityImporting: boolean;
  isUserDefineImporting: boolean;
}

/** DW Blueprint step UI persisted across main app tab switches. */
export interface PlmImportDwBlueprintStepUiState {
  blueprint: PlmDwImportBlueprintDto | null;
  blueprintFileName: string | null;
  blueprintJsonText: string | null;
  previewItems: PlmDwBlueprintPreviewItemDto[];
  validationErrors: string[];
  validationWarnings: string[];
  lastExecuteResult: PlmDwBlueprintExecuteResultDto | null;
  isValidating: boolean;
  isPreviewing: boolean;
  isExecuting: boolean;
  includeTransactionGroup: boolean;
  includeSearchView: boolean;
  includeNavigation: boolean;
}

export interface PlmImportPageCache {
  wizardState: PlmImportWizardState;
  entityStepUi: PlmImportEntityStepUiState;
  dwBlueprintStepUi: PlmImportDwBlueprintStepUiState;
  /** @deprecated Legacy cache key — migrated to dwBlueprintStepUi on read */
  templateStepUi?: PlmImportDwBlueprintStepUiState;
}

export const createInitialEntityStepUi = (): PlmImportEntityStepUiState => ({
  systemSectionExpanded: true,
  userSectionExpanded: true,
  planItems: [],
  entityPlanItems: [],
  userDefinePlanItems: [],
  activeJob: null,
  isExporting: false,
  isEntityImporting: false,
  isUserDefineImporting: false,
});

export const createInitialDwBlueprintStepUi = (): PlmImportDwBlueprintStepUiState => ({
  blueprint: null,
  blueprintFileName: null,
  blueprintJsonText: null,
  previewItems: [],
  validationErrors: [],
  validationWarnings: [],
  lastExecuteResult: null,
  isValidating: false,
  isPreviewing: false,
  isExecuting: false,
  includeTransactionGroup: true,
  includeSearchView: true,
  includeNavigation: true,
});

export const buildPlmImportStepStateJson = (state: Pick<
  PlmImportWizardState,
  | 'connectionTested'
  | 'systemDefineTablesComplete'
  | 'systemDefineEntitiesComplete'
  | 'userDefineEntitiesComplete'
  | 'templatesComplete'
  | 'tablePrefix'
>): string => JSON.stringify({
  connectionTested: state.connectionTested,
  systemDefineTablesComplete: state.systemDefineTablesComplete,
  systemDefineEntitiesComplete: state.systemDefineEntitiesComplete,
  userDefineEntitiesComplete: state.userDefineEntitiesComplete,
  templatesComplete: state.templatesComplete,
  systemDefineComplete: state.systemDefineTablesComplete,
  tablePrefix: normalizePlmImportTablePrefix(state.tablePrefix, PLM_DEFAULT_TABLE_PREFIX),
});

export const PLM_IMPORT_PAGE_CACHE_SUFFIX = '-PlmDataImportManagement';

export const PLM_IMPORT_STEP_ORDER: PlmImportStepCode[] = [
  'Connect',
  'Entity',
  'DwBlueprint',
  'FolderImport',
  'ColorImport',
  'ImageImport',
];
