/** Angular parity: transactionFormDataTransferSettingEditorCtrl.js (FromDataModelToApi mapping). */

export type TransactionFieldLookup = {
  Id: number;
  Display: string;
  ShortDisplay: string;
  Display2: string;
  UnitId: number;
};

export type ApiParamMappingRow = {
  targetApiParamName: string;
  SourceFiledId: number | null;
};

export function buildSourceTransactionFieldLookups(srcTransactionData: any): {
  allSrcTransactionFieldList: TransactionFieldLookup[];
  allSrcTransactionInputParamList: { Id: string; Display: string }[];
  unitList: any[];
} {
  const allSrcTransactionFieldList: TransactionFieldLookup[] = [];
  const allSrcTransactionInputParamList: { Id: string; Display: string }[] = [];
  const unitList = srcTransactionData?.AppTransactionUnitList || [];

  unitList.forEach((unit: any) => {
    (unit.AppTransactionFieldList || []).forEach((field: any) => {
      const shortDisplay = field.DataBaseFieldName || field.DisplayName || '';
      allSrcTransactionFieldList.push({
        Id: field.Id,
        Display: `${unit.UnitDisplayName} : ${shortDisplay}`,
        ShortDisplay: shortDisplay,
        Display2: `${shortDisplay} (${unit.UnitDisplayName})`,
        UnitId: unit.Id,
      });
    });
  });

  (srcTransactionData?.ApiInputParameterList || []).forEach((apiInputParam: string) => {
    if (apiInputParam) {
      allSrcTransactionInputParamList.push({ Id: apiInputParam, Display: apiInputParam });
    }
  });

  return { allSrcTransactionFieldList, allSrcTransactionInputParamList, unitList };
}

function findMappingByInputParamName(mappingData: any[], paramName: string) {
  return mappingData.find((x) => x.Name === paramName) ?? null;
}

function findMappingByJsonPath(mappingData: any[], jsonPropertyPathName: string, isResponseMapping: boolean) {
  return (
    mappingData.find(
      (x) => x.JsonPropertyPathName === jsonPropertyPathName && x.IsBlankTargetField === (isResponseMapping || false),
    ) ?? null
  );
}

function processTreeNodeMappings(
  nodeDto: any,
  mappingData: any[],
  isResponseMapping: boolean,
  hasSrcApiInputParams: boolean,
) {
  if ((!nodeDto.IsArray && !nodeDto.IsObject) || isResponseMapping) {
    if (nodeDto.AbsolutePath) {
      nodeDto.TargetFiledId = null;
      const existing = findMappingByJsonPath(mappingData, nodeDto.AbsolutePath, isResponseMapping);
      if (existing) {
        if (isResponseMapping) {
          nodeDto.TargetFiledId = existing.TargetFiledId || null;
          nodeDto.SourceFiledId = existing.SourceFiledId || null;
        } else {
          nodeDto.SourceFiledId = existing.SourceFiledId || null;
        }
        if (hasSrcApiInputParams) {
          nodeDto.mapToInputParam = existing.Name || '';
        }
      }
    }
  }
  if (nodeDto.Children) {
    nodeDto.Children.forEach((child: any) =>
      processTreeNodeMappings(child, mappingData, isResponseMapping, hasSrcApiInputParams),
    );
  }
}

export function buildFromDataModelToApiMappingState(settingData: any, srcTransactionData: any) {
  settingData.orgAppTransactionSaveAsMappingList = settingData.AppTransactionSaveAsMappingList || [];
  const orgMappings = settingData.orgAppTransactionSaveAsMappingList as any[];

  const nameMappings = orgMappings.filter((m) => m.Name);
  const payloadMappings = orgMappings.filter((m) => !m.IsBlankTargetField);
  const responseMappings = orgMappings.filter((m) => m.IsBlankTargetField);

  const targetApiParamMappingList: ApiParamMappingRow[] = [];
  (settingData.TargetApiInputParameterList || []).forEach((paramName: string) => {
    if (!paramName) return;
    const existing = findMappingByInputParamName(nameMappings, paramName);
    targetApiParamMappingList.push({
      targetApiParamName: paramName,
      SourceFiledId: existing?.SourceFiledId ?? null,
    });
  });

  const hasSrcApiInputParams = (srcTransactionData?.ApiInputParameterList || []).length > 0;
  const payloadRoots = settingData.TargetApiPayloadDataStructure
    ? JSON.parse(JSON.stringify(settingData.TargetApiPayloadDataStructure))
    : null;
  const responseRoots = settingData.TargetApiResponseDataStructure
    ? JSON.parse(JSON.stringify(settingData.TargetApiResponseDataStructure))
    : null;

  if (payloadRoots) {
    payloadRoots.forEach((node: any) => processTreeNodeMappings(node, payloadMappings, false, hasSrcApiInputParams));
  }
  if (responseRoots) {
    responseRoots.forEach((node: any) => processTreeNodeMappings(node, responseMappings, true, hasSrcApiInputParams));
  }

  return { targetApiParamMappingList, payloadRoots, responseRoots, hasSrcApiInputParams };
}

function prepareSaveProcessOneNode(
  nodeDto: any,
  settingData: any,
  srcTransactionId: number,
  isResponseMapping: boolean,
) {
  if ((!nodeDto.IsArray && !nodeDto.IsObject) || isResponseMapping) {
    if (nodeDto.AbsolutePath) {
      if (
        (nodeDto.SourceFiledId || nodeDto.TargetFiledId || nodeDto.mapToInputParam) &&
        !(isResponseMapping && nodeDto.SourceFiledId && !nodeDto.TargetFiledId)
      ) {
        settingData.AppTransactionSaveAsMappingList.push({
          MappingUnitId: null,
          SourceFiledId: nodeDto.SourceFiledId || null,
          TargetFiledId: nodeDto.TargetFiledId || null,
          IsBlankTargetField: isResponseMapping || false,
          TransactionId: srcTransactionId,
          JsonPropertyPathName: nodeDto.AbsolutePath,
          Name: nodeDto.mapToInputParam || '',
        });
      }
    }
  }
  if (nodeDto.Children) {
    nodeDto.Children.forEach((child: any) =>
      prepareSaveProcessOneNode(child, settingData, srcTransactionId, isResponseMapping),
    );
  }
}

export function prepareFromDataModelToApiSavePayload(
  settingData: any,
  srcTransactionId: number,
  targetApiParamMappingList: ApiParamMappingRow[],
  payloadRoots: any[] | null,
  responseRoots: any[] | null,
) {
  const toSave = { ...settingData, AppTransactionSaveAsMappingList: [] as any[] };

  targetApiParamMappingList.forEach((paramMapping) => {
    if (paramMapping.SourceFiledId) {
      toSave.AppTransactionSaveAsMappingList.push({
        MappingUnitId: null,
        SourceFiledId: paramMapping.SourceFiledId || null,
        TargetFiledId: null,
        IsBlankTargetField: false,
        TransactionId: srcTransactionId,
        JsonPropertyPathName: '',
        Name: paramMapping.targetApiParamName || '',
      });
    }
  });

  if (payloadRoots) {
    payloadRoots.forEach((node) => prepareSaveProcessOneNode(node, toSave, srcTransactionId, false));
  }
  if (responseRoots) {
    responseRoots.forEach((node) => prepareSaveProcessOneNode(node, toSave, srcTransactionId, true));
  }

  return toSave;
}

export function buildIntegrationApiOperationDict(integrationSettingList: any[]) {
  const dict: Record<number, any> = {};
  (integrationSettingList || []).forEach((integrationSettingDto) => {
    const params =
      integrationSettingDto.AppIntergrationSettingParameterList ||
      integrationSettingDto.AppIntegrationSettingParameterList ||
      [];
    params.forEach((apiOperationDto: any) => {
      apiOperationDto.actionDisplay = integrationSettingDto.Name
        ? `${integrationSettingDto.Name} . ${apiOperationDto.ActionCode}`
        : apiOperationDto.ActionCode;
      dict[apiOperationDto.Id] = apiOperationDto;
    });
  });
  return dict;
}
