/** Helpers for Data Transfer Setting Full Editor (types 1–4). */

import { buildSourceTransactionFieldLookups, type TransactionFieldLookup } from './CommandEditorPart/callApiOperation/dataTransferFromDataModelToApiHelpers';

export const EmAppTransactionDataTransferType = {
  FromDataModelToMessageTemplate: 1,
  FromDataModelToDataModel: 2,
  FromApiToDataModel: 3,
  FromDataModelToApi: 4,
} as const;

export type DataTransferMappingRow = {
  /** Existing MappingId; omitted for new rows (full-replace save strips Ids). */
  Id?: number | null;
  SourceFiledId?: number | null;
  TargetFiledId?: number | null;
  Name?: string;
  JsonPropertyPathName?: string;
  IsBlankTargetField?: boolean | null;
  MappingUnitId?: number | null;
  TransactionId?: number | null;
};

export function transferTypeDisplay(typeId: number): string {
  switch (typeId) {
    case EmAppTransactionDataTransferType.FromDataModelToMessageTemplate:
      return 'From Data Model To Message Template';
    case EmAppTransactionDataTransferType.FromDataModelToDataModel:
      return 'From Data Model To Data Model';
    case EmAppTransactionDataTransferType.FromApiToDataModel:
      return 'From Api To Data Model';
    case EmAppTransactionDataTransferType.FromDataModelToApi:
      return 'From Data Model To Api';
    default:
      return `Type ${typeId}`;
  }
}

export function buildFieldLookupsFromTransaction(transactionData: any): TransactionFieldLookup[] {
  if (!transactionData) return [];
  return buildSourceTransactionFieldLookups(transactionData).allSrcTransactionFieldList;
}

/** Map API DataTransferMappingList / AppTransactionSaveAsMappingList into grid rows. */
export function mappingsFromSetting(settingData: any): DataTransferMappingRow[] {
  const list =
    settingData?.DataTransferMappingList ||
    settingData?.AppTransactionSaveAsMappingList ||
    [];
  const arr = Array.isArray(list) ? list : [];
  return arr
    .filter((m: any) => !m?.IsBlankTargetField)
    .map((m: any) => ({
      Id: m.Id ?? null,
      SourceFiledId: m.SourceFiledId ?? null,
      TargetFiledId: m.TargetFiledId ?? null,
      Name: m.Name ?? '',
      JsonPropertyPathName: m.JsonPropertyPathName ?? '',
      IsBlankTargetField: m.IsBlankTargetField ?? false,
      MappingUnitId: m.MappingUnitId ?? null,
      TransactionId: m.TransactionId ?? null,
    }));
}

/**
 * Angular parity: when SRC is a Data Model, pre-fill one mapping row per source field
 * (all units). Existing saved mappings supply TargetFiledId / Name / Id.
 */
export function buildAutoSourceFieldMappings(
  srcFields: TransactionFieldLookup[],
  existing: DataTransferMappingRow[] | null | undefined,
  srcTransactionId: number | null,
): DataTransferMappingRow[] {
  const bySourceId = new Map<number, DataTransferMappingRow>();
  (existing || []).forEach((m) => {
    if (m.SourceFiledId != null) bySourceId.set(Number(m.SourceFiledId), m);
  });

  return (srcFields || []).map((f) => {
    const prev = bySourceId.get(Number(f.Id));
    return {
      Id: prev?.Id ?? null,
      SourceFiledId: f.Id,
      TargetFiledId: prev?.TargetFiledId ?? null,
      Name: prev?.Name ?? '',
      JsonPropertyPathName: prev?.JsonPropertyPathName ?? '',
      IsBlankTargetField: false,
      MappingUnitId: prev?.MappingUnitId ?? f.UnitId ?? null,
      TransactionId: prev?.TransactionId ?? srcTransactionId,
    };
  });
}

/** Persist only rows the user has mapped (target or token filled). */
export function filterMappingsForSave(
  transferTypeId: number,
  mappings: DataTransferMappingRow[],
): DataTransferMappingRow[] {
  if (transferTypeId === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate) {
    return mappings.filter((m) => m.SourceFiledId != null && String(m.Name || '').trim() !== '');
  }
  if (transferTypeId === EmAppTransactionDataTransferType.FromDataModelToDataModel) {
    return mappings.filter((m) => m.SourceFiledId != null && m.TargetFiledId != null);
  }
  if (transferTypeId === EmAppTransactionDataTransferType.FromApiToDataModel) {
    return mappings.filter(
      (m) => String(m.JsonPropertyPathName || '').trim() !== '' && m.TargetFiledId != null,
    );
  }
  return mappings;
}

/**
 * Full-replace save payload: strip mapping Ids so BL treats all as new and deletes prior rows.
 * (See AppTransactionDataTransferSettingBL.ProcessDirtyDto replaceAllMappings.)
 */
export function prepareFullReplaceMappingSavePayload(
  settingData: any,
  header: {
    Description: string;
    TransactionId: number | null;
    DestinationTransactionId: number | null;
    InternalCode: string;
    TransferTypeId: number;
  },
  mappings: DataTransferMappingRow[],
): any {
  const srcTxId = header.TransactionId ?? settingData?.TransactionId ?? null;
  return {
    ...settingData,
    Description: header.Description,
    TransactionId: header.TransactionId,
    DestinationTransactionId: header.DestinationTransactionId,
    InternalCode: header.InternalCode,
    TransferTypeId: header.TransferTypeId,
    IsModified: settingData?.Id != null,
    IsNew: settingData?.Id == null,
    AppTransactionSaveAsMappingList: mappings.map((m) => ({
      // No Id → IsNew → full replace path
      DataTransferSettingId: settingData?.Id ?? null,
      TransactionId: srcTxId,
      SourceFiledId: m.SourceFiledId ?? null,
      TargetFiledId: m.TargetFiledId ?? null,
      Name: m.Name ?? '',
      JsonPropertyPathName: m.JsonPropertyPathName ?? '',
      IsBlankTargetField: m.IsBlankTargetField ?? false,
      MappingUnitId: m.MappingUnitId ?? null,
    })),
  };
}

export { type TransactionFieldLookup };
