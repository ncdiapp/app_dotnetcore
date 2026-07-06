import type { PlmImportStepCode } from './types';
import { normalizePlmImportStepCode } from './types';

export interface PlmImportStepDefinition {
  code: PlmImportStepCode;
  label: string;
  description: string;
  icon: string;
}

export const PLM_IMPORT_STEPS: PlmImportStepDefinition[] = [
  {
    code: 'Connect',
    label: 'Connect & Discover',
    description: 'Select application, connect to PLM, and discover data sources.',
    icon: 'fa-solid fa-plug',
  },
  {
    code: 'Entity',
    label: 'Entity Import',
    description: 'System Define first, then User Define entities.',
    icon: 'fa-solid fa-table',
  },
  {
    code: 'DwBlueprint',
    label: 'Transaction From DW Blueprint',
    description: 'Apply PlmDw_ImportBlueprint.json to create transactions, forms, and search.',
    icon: 'fa-solid fa-diagram-project',
  },
  {
    code: 'FolderImport',
    label: 'Folder Import',
    description: 'Import PLM folder structures into APP folders.',
    icon: 'fa-solid fa-folder-tree',
  },
  {
    code: 'ImageImport',
    label: 'Image Import',
    description: 'Import PLM tblSketch binary data into AppFile (original/regular/thumbnail).',
    icon: 'fa-solid fa-images',
  },
];

export const getStepIndex = (code: PlmImportStepCode): number =>
  PLM_IMPORT_STEPS.findIndex((s) => s.code === normalizePlmImportStepCode(code));

export const getStepByCode = (code: PlmImportStepCode): PlmImportStepDefinition | undefined =>
  PLM_IMPORT_STEPS.find((s) => s.code === code);
