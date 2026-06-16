import type { PlmImportSessionDto } from '../../../webapi/plmMigrationSvc';

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

export const PLM_IMPORT_STEP_ORDER: PlmImportStepCode[] = [
  'Connect',
  'Entity',
  'Template',
  'OtherData',
];
