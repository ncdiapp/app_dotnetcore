import { getDictViewColumnValue, buildLinkTargetTabTitle } from './linkTargetTabTitle';
import type { AppDispatch } from '../redux/store';
import { getDataModelFromCache, setDataModelToCache } from '../redux/features/ui/navigation/tabnavSlice';

const FORM_GROUP_SESSION_STORAGE_PREFIX = 'appai_tfg_session:';

export const API_PARAMETER_PREFIX = {
  prefix: 'api__',
  split: '|',
};

/** Matches APP.Components.Dto EmAppLinkTargetActionType (AppEnums.cs). */
export const EmAppLinkTargetActionType = {
  Edit: 1,
  Create: 2,
  Delete: 3,
  Preview: 5,
  CallExternalMethod: 6,
  Report: 7,
  EditUserLogin: 11,
  EditOnPopup: 12,
  CreateFromExistingItem: 13,
  CopyEvent: 14,
  CutEvent: 15,
  PasteEvent: 16,
  ExecuteTransactionCommand: 17,
};

export const EmAppTransactionTemplateItemType = {
  MainItem: 1,
  TemplateHeader: 2,
};

/** Template header items render inside TransactionFormGroup, not on search row context menu. */
export function isTemplateHeaderLinkTarget(linkTarget: any): boolean {
  return linkTarget?.OtherSettingsDto?.TemplateItemType === EmAppTransactionTemplateItemType.TemplateHeader;
}

export function filterRowContextMenuFormLinkTargets(list: any[] | null | undefined): any[] {
  if (!Array.isArray(list)) return [];
  return list.filter((lt) => !isTemplateHeaderLinkTarget(lt));
}

export function filterRowContextMenuLinkedSearches(list: any[] | null | undefined): any[] {
  if (!Array.isArray(list)) return [];
  return list.filter((ls) => !isTemplateHeaderLinkTarget(ls));
}

export const EmAppLinkTargetSourceColumnType = {
  SearchViewField: 1,
  SearchViewSpecialProperty: 2,
};

export type TransactionFormGroupSessionData = {
  viewDto: any;
  selecedDataRow: any;
  linkTargetDto: any;
  searchResultRowList: any[];
};

export type TransactionFormGroupOpenParam2 = {
  callingFromScopeName?: string;
  linkTargetId?: number | null;
  linkTargetTransactionId?: number | null;
  targetPkValue?: string | null;
  tabTitle?: string;
  formGroupSessionKey?: string;
  openFrom?: string | null;
  /** Inline fallback when session cache is unavailable (e.g. page refresh). */
  viewDto?: any;
  linkTargetDto?: any;
  selecedDataRow?: any;
  searchResultRowList?: any[];
};

export function persistFormGroupSession(sessionKey: string, sessionData: TransactionFormGroupSessionData): void {
  if (!sessionKey) return;
  try {
    sessionStorage.setItem(FORM_GROUP_SESSION_STORAGE_PREFIX + sessionKey, JSON.stringify(sessionData));
  } catch {
    // Quota or private mode — Redux cache still works until refresh.
  }
}

export function loadFormGroupSession(sessionKey: string): TransactionFormGroupSessionData | null {
  if (!sessionKey) return null;
  try {
    const raw = sessionStorage.getItem(FORM_GROUP_SESSION_STORAGE_PREFIX + sessionKey);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as TransactionFormGroupSessionData;
    return parsed?.viewDto ? parsed : null;
  } catch {
    return null;
  }
}

/** Redux (in-memory) first, then sessionStorage (survives refresh / tab remount). */
export function resolveFormGroupSession(sessionKey: string): TransactionFormGroupSessionData | null {
  if (!sessionKey) return null;
  const cached = getDataModelFromCache(sessionKey);
  if (cached?.viewDto) return cached as TransactionFormGroupSessionData;
  return loadFormGroupSession(sessionKey);
}

export function cacheFormGroupSession(
  dispatch: AppDispatch,
  sessionKey: string,
  sessionData: TransactionFormGroupSessionData,
): void {
  if (!sessionKey) return;
  dispatch(setDataModelToCache({ dataModelKey: sessionKey, dataModel: sessionData }));
  persistFormGroupSession(sessionKey, sessionData);
}

export function repairFormGroupSession(
  session: TransactionFormGroupSessionData,
  param2: TransactionFormGroupOpenParam2,
): TransactionFormGroupSessionData {
  let { viewDto, selecedDataRow, linkTargetDto, searchResultRowList } = session;

  if (!linkTargetDto && param2.linkTargetId != null && Array.isArray(viewDto?.AppFormLinkTargetList)) {
    linkTargetDto =
      viewDto.AppFormLinkTargetList.find((lt: any) => lt.Id === param2.linkTargetId) ?? linkTargetDto;
  }

  if (!selecedDataRow && param2.targetPkValue != null && Array.isArray(searchResultRowList)) {
    const pk = String(param2.targetPkValue);
    selecedDataRow =
      searchResultRowList.find((row: any) => {
        const dict = row?.DictViewColumnIDKeyValue || {};
        return Object.values(dict).some((v) => v != null && String(v) === pk);
      }) ?? selecedDataRow;
  }

  return { viewDto, selecedDataRow, linkTargetDto, searchResultRowList: searchResultRowList || [] };
}

export function normalizeLinkTargetActionType(value: unknown): number | null {
  if (value == null || value === '') return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

export function isFormGroupOpenAction(actionType: unknown): boolean {
  const action = normalizeLinkTargetActionType(actionType);
  if (action == null) return false;
  return (
    action === EmAppLinkTargetActionType.Edit ||
    action === EmAppLinkTargetActionType.Preview ||
    action === EmAppLinkTargetActionType.Create ||
    action === EmAppLinkTargetActionType.CreateFromExistingItem ||
    action === EmAppLinkTargetActionType.EditOnPopup
  );
}

export function isCreateLikeLinkTarget(linkTarget: any): boolean {
  const action = normalizeLinkTargetActionType(linkTarget?.ActionType);
  return (
    action === EmAppLinkTargetActionType.Create ||
    action === EmAppLinkTargetActionType.CreateFromExistingItem ||
    action === EmAppLinkTargetActionType.PasteEvent
  );
}

function isFormGroupCandidateLinkTarget(linkTarget: any): boolean {
  const action = normalizeLinkTargetActionType(linkTarget?.ActionType);
  if (action == null) return false;
  return (
    action === EmAppLinkTargetActionType.Edit ||
    action === EmAppLinkTargetActionType.EditOnPopup ||
    action === EmAppLinkTargetActionType.Preview ||
    action === EmAppLinkTargetActionType.Create
  );
}

/** View is a data-model template with form-group navigation (Angular FormGroupLinkTargetList). */
export function hasDataModelTemplateFormGroup(viewDto: any): boolean {
  if (!viewDto) return false;

  const fromDto = viewDto.FormGroupLinkTargetList;
  if (Array.isArray(fromDto) && fromDto.length > 0) return true;

  const list = viewDto.AppFormLinkTargetList;
  if (Array.isArray(list)) {
    const templateTargets = list.filter((lt) => lt?.OtherSettingsDto?.TemplateItemType != null);
    if (templateTargets.some((lt) => isFormGroupOpenAction(lt.ActionType))) {
      return true;
    }
    if (list.filter((lt) => isFormGroupCandidateLinkTarget(lt)).length >= 1) {
      return true;
    }
  }

  const linked = viewDto.AppViewLinkedSeaechOrUrlDtoList;
  if (Array.isArray(linked) && linked.some((lt) => lt?.OtherSettingsDto?.TemplateItemType != null)) {
    return true;
  }

  return false;
}

export function getFormGroupLinkTargetList(viewDto: any): any[] {
  const fromDto = viewDto?.FormGroupLinkTargetList;
  if (Array.isArray(fromDto) && fromDto.length > 0) {
    return [...fromDto].sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0));
  }
  const list = viewDto?.AppFormLinkTargetList;
  if (!Array.isArray(list)) return [];

  const templateItems = list.filter((o) => o?.OtherSettingsDto?.TemplateItemType != null);
  if (templateItems.length > 0) {
    return templateItems
      .filter((o) => isFormGroupCandidateLinkTarget(o))
      .sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0));
  }

  return list
    .filter((o) => isFormGroupCandidateLinkTarget(o))
    .sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0));
}

export function shouldOpenAsFormGroup(linkTarget: any, viewDto: any): boolean {
  if (!linkTarget || isTemplateHeaderLinkTarget(linkTarget)) return false;
  if (!hasDataModelTemplateFormGroup(viewDto)) return false;
  return isFormGroupOpenAction(linkTarget.ActionType);
}

export function resolveLinkTargetTargetPkValue(
  linkTarget: any,
  selecedDataRow: any,
  dictLinkTargetParameterDefaultValue: Record<string | number, unknown> = {},
): { targetPkValue: string | null; linkTargetValueMapping: Record<string, unknown> } {
  const row = selecedDataRow || { DictViewColumnIDKeyValue: {} };
  const dict = row.DictViewColumnIDKeyValue || {};
  let targetPkValue: string | null = null;
  const linkTargetValueMapping: Record<string, unknown> = {};

  const isCreateLike =
    linkTarget?.ActionType === EmAppLinkTargetActionType.Create ||
    linkTarget?.ActionType === EmAppLinkTargetActionType.CreateFromExistingItem ||
    linkTarget?.ActionType === EmAppLinkTargetActionType.PasteEvent;

  const appendApiPk = (column: string, value: unknown) => {
    if (value == null || value === '' || !column) return;
    const segment = `${column}:${value}`;
    if (!targetPkValue) {
      targetPkValue = `${API_PARAMETER_PREFIX.prefix}${segment}`;
    } else {
      targetPkValue += `${API_PARAMETER_PREFIX.split}${segment}`;
    }
  };

  if (linkTarget?.SourceViewColumnId1) {
    if (isCreateLike) {
      if (linkTarget.TargetColumn1) {
        linkTargetValueMapping[linkTarget.TargetColumn1] =
          getDictViewColumnValue(dict, linkTarget.SourceViewColumnId1) ??
          dictLinkTargetParameterDefaultValue[linkTarget.SourceViewColumnId1];
      }
    } else if (linkTarget.OtherSettingsDto?.IsLinkToComsumeApiTransaction) {
      const v = getDictViewColumnValue(dict, linkTarget.SourceViewColumnId1);
      if (v && linkTarget.TargetColumn1) appendApiPk(linkTarget.TargetColumn1, v);
    } else {
      targetPkValue = getDictViewColumnValue(dict, linkTarget.SourceViewColumnId1) ?? null;
    }
  }

  if (linkTarget?.SourceViewColumnId2 && linkTarget?.TargetColumn2) {
    if (isCreateLike) {
      linkTargetValueMapping[linkTarget.TargetColumn2] =
        getDictViewColumnValue(dict, linkTarget.SourceViewColumnId2) ??
        dictLinkTargetParameterDefaultValue[linkTarget.SourceViewColumnId2];
    } else if (linkTarget.OtherSettingsDto?.IsLinkToComsumeApiTransaction) {
      const v = getDictViewColumnValue(dict, linkTarget.SourceViewColumnId2);
      if (v) appendApiPk(linkTarget.TargetColumn2, v);
    }
  }

  if (linkTarget?.SourceViewColumnId3 && linkTarget?.TargetColumn3) {
    if (isCreateLike) {
      linkTargetValueMapping[linkTarget.TargetColumn3] =
        getDictViewColumnValue(dict, linkTarget.SourceViewColumnId3) ??
        dictLinkTargetParameterDefaultValue[linkTarget.SourceViewColumnId3];
    } else if (linkTarget.OtherSettingsDto?.IsLinkToComsumeApiTransaction) {
      const v = getDictViewColumnValue(dict, linkTarget.SourceViewColumnId3);
      if (v) appendApiPk(linkTarget.TargetColumn3, v);
    }
  }

  return { targetPkValue, linkTargetValueMapping };
}

export function buildTemplateItemLists(viewDto: any, clickedLinkTarget: any): {
  linkTargetList: any[];
  templateHeaderList: any[];
} {
  let linkTargetList: any[] = [];
  let templateHeaderList: any[] = [];

  if (clickedLinkTarget?.OtherSettingsDto?.TemplateItemType) {
    const formGroupList = getFormGroupLinkTargetList(viewDto);
    formGroupList.forEach((lt: any) => {
      const display = lt.NavigationActionName || '';
      const item = { ...lt, display };
      if (lt.OtherSettingsDto?.TemplateItemType === EmAppTransactionTemplateItemType.TemplateHeader) {
        templateHeaderList.push({ ...item, isTemplateHeader: true });
      } else if (lt.OtherSettingsDto?.TemplateItemType === EmAppTransactionTemplateItemType.MainItem) {
        linkTargetList.push(item);
      }
    });

    const linkedList = viewDto?.AppViewLinkedSeaechOrUrlDtoList;
    if (Array.isArray(linkedList)) {
      linkedList.forEach((lt: any) => {
        const display = lt.DisplayText || '';
        const item = { ...lt, display };
        if (lt.OtherSettingsDto?.TemplateItemType === EmAppTransactionTemplateItemType.TemplateHeader) {
          templateHeaderList.push({ ...item, isTemplateHeader: true });
        } else if (lt.OtherSettingsDto?.TemplateItemType === EmAppTransactionTemplateItemType.MainItem) {
          linkTargetList.push(item);
        }
      });
    }

    linkTargetList.sort((a, b) => (a.Sort || 0) - (b.Sort || 0));
    templateHeaderList.sort((a, b) => (a.Sort || 0) - (b.Sort || 0));
  } else {
    linkTargetList = [
      {
        ...clickedLinkTarget,
        display: clickedLinkTarget.NavigationActionName || clickedLinkTarget.DisplayText || '',
      },
    ];
  }

  return { linkTargetList, templateHeaderList };
}

export function findLinkTargetInList(dto: any, list: any[]): any | null {
  if (!dto || !Array.isArray(list)) return null;
  for (const item of list) {
    if (item.Id !== dto.Id) continue;
    if (dto.LinkTargetTransactionId && dto.LinkTargetTransactionId === item.LinkTargetTransactionId) {
      return item;
    }
    if (dto.LinkTargetSearchId && dto.LinkTargetSearchId === item.LinkTargetSearchId) {
      return item;
    }
  }
  return null;
}

export function buildLinkedSearchCriteriaDict(linkTarget: any, selecedDataRow: any): Record<number, unknown> {
  const dictCreteriaIdValue: Record<number, unknown> = {};
  const row = selecedDataRow || { DictViewColumnIDKeyValue: {} };

  const readSource = (sourceColumnId: number, targetFieldId: number) => {
    if (!sourceColumnId || !targetFieldId) return;
    if (linkTarget.SourceColumnType === EmAppLinkTargetSourceColumnType.SearchViewSpecialProperty) {
      dictCreteriaIdValue[targetFieldId] = row[sourceColumnId];
    } else {
      dictCreteriaIdValue[targetFieldId] = getDictViewColumnValue(
        row.DictViewColumnIDKeyValue,
        sourceColumnId,
      );
    }
  };

  readSource(linkTarget.SourceViewColumnId1, linkTarget.TargetSearchFieldId1);
  readSource(linkTarget.SourceViewColumnId2, linkTarget.TargetSearchFieldId2);
  readSource(linkTarget.SourceViewColumnId3, linkTarget.TargetSearchFieldId3);

  return dictCreteriaIdValue;
}

export function buildFormGroupTabTitle(linkTarget: any, selecedDataRow: any, targetPkValue: string | null): string {
  const displayKeyValue = linkTarget?.RowDisplayViewColumnId
    ? getDictViewColumnValue(selecedDataRow?.DictViewColumnIDKeyValue, linkTarget.RowDisplayViewColumnId)
    : undefined;
  const pkForTitle =
    targetPkValue && !String(targetPkValue).startsWith(API_PARAMETER_PREFIX.prefix)
      ? targetPkValue
      : getDictViewColumnValue(
          selecedDataRow?.DictViewColumnIDKeyValue,
          linkTarget?.SourceViewColumnId1,
        );

  let tabTitle = buildLinkTargetTabTitle(
    linkTarget?.NavigationActionName,
    Boolean(linkTarget?.RowDisplayViewColumnId),
    displayKeyValue,
    pkForTitle,
  );

  if (targetPkValue && !String(targetPkValue).startsWith(API_PARAMETER_PREFIX.prefix)) {
    tabTitle += ` (${targetPkValue})`;
  }

  return tabTitle;
}

export function buildFormGroupOpenPayload(
  linkTarget: any,
  dataItem: any,
  viewDto: any,
  searchResultRowList: any[] = [],
  dictLinkTargetParameterDefaultValue: Record<string | number, unknown> = {},
): { tabTitle: string; sessionKey: string; param2: TransactionFormGroupOpenParam2; sessionData: TransactionFormGroupSessionData } {
  const { targetPkValue } = resolveLinkTargetTargetPkValue(
    linkTarget,
    dataItem,
    dictLinkTargetParameterDefaultValue,
  );
  const tabTitle = buildFormGroupTabTitle(linkTarget, dataItem, targetPkValue);
  const sessionKey = `tfg_${linkTarget.Id}_${targetPkValue || dataItem?.Id || ''}`;

  const sessionData: TransactionFormGroupSessionData = {
    viewDto,
    selecedDataRow: dataItem,
    linkTargetDto: linkTarget,
    searchResultRowList: searchResultRowList || [],
  };

  const param2: TransactionFormGroupOpenParam2 = {
    callingFromScopeName: 'searchScope',
    linkTargetId: linkTarget.Id ?? null,
    linkTargetTransactionId: linkTarget.LinkTargetTransactionId ?? null,
    targetPkValue: targetPkValue ?? null,
    tabTitle,
    formGroupSessionKey: sessionKey,
  };

  return { tabTitle, sessionKey, param2, sessionData };
}

export function buildEmbeddedFormParam2(
  linkTarget: any,
  row: any,
  options: { isHeader: boolean; openFrom?: string | null },
): {
  transactionId: number | null;
  rootPrimaryKeyValue: string | null;
  param2: Record<string, unknown>;
} | null {
  const { targetPkValue, linkTargetValueMapping } = resolveLinkTargetTargetPkValue(linkTarget, row);
  const transactionId = linkTarget?.LinkTargetTransactionId != null
    ? Number(linkTarget.LinkTargetTransactionId)
    : null;

  if (transactionId == null || Number.isNaN(transactionId)) {
    return null;
  }

  const isCreateLike = isCreateLikeLinkTarget(linkTarget);
  if (!isCreateLike && !targetPkValue) {
    return null;
  }
  // Create-like actions may open a blank new record without source row mapping.

  const transGroupId = linkTarget.LinkTargetTransactionGroupId ?? null;
  const label = linkTarget.display || linkTarget.NavigationActionName || 'Template Header';

  const param2: Record<string, unknown> = {
    linkTargetValueMapping,
    openFrom: options.openFrom ?? null,
    opennedFormAutoExecuteCommandId: linkTarget.OpennedFormAutoExecuteCommandId ?? null,
    transGroupId,
    LinkTargetTransactionGroupId: transGroupId,
    isEmbeddedByOtherPage: true,
    isTemplateHeader: options.isHeader,
    TemplateHeaderName: label,
    selecedDataRow: row,
  };

  if (options.isHeader) {
    param2.isHideHeaderAndFooter = true;
  }

  if (linkTarget.ActionType === EmAppLinkTargetActionType.Preview) {
    param2.isPreview = true;
    param2.isPrint = true;
  }

  return {
    transactionId,
    rootPrimaryKeyValue: targetPkValue != null ? String(targetPkValue) : null,
    param2,
  };
}

export function parseRouteParam2(paramObj: any): TransactionFormGroupOpenParam2 {
  if (!paramObj?.param2) return {};
  if (typeof paramObj.param2 === 'object') return paramObj.param2;
  try {
    return JSON.parse(paramObj.param2);
  } catch {
    return {};
  }
}
