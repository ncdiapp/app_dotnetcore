import type { AppDispatch } from '../redux/store';
import {
  buildFormGroupOpenPayload,
  cacheFormGroupSession as cacheFormGroupSessionInternal,
  EmAppLinkTargetActionType,
  EmAppTransactionTemplateItemType,
  isTemplateHeaderLinkTarget,
} from './transactionFormGroupHelper';
import { getDictViewColumnValue } from './linkTargetTabTitle';

export { buildFormGroupOpenPayload, EmAppLinkTargetActionType };

export function cacheFormGroupSession(dispatch: AppDispatch, sessionKey: string, sessionData: any) {
  cacheFormGroupSessionInternal(dispatch, sessionKey, sessionData);
}

export function resolveFolderIdFieldName(viewDto: any): string | null {
  const fields = viewDto?.AppSearchViewFieldList ?? viewDto?.Columns ?? [];
  const folderField = fields.find((f: any) => f.IsFileFoderId);
  return folderField?.SysTableFiledPath ?? folderField?.Name ?? null;
}

function resolveFolderIdViewField(viewDto: any): any | null {
  const fields = viewDto?.AppSearchViewFieldList ?? viewDto?.Columns ?? [];
  return fields.find((f: any) => f.IsFileFoderId) ?? null;
}

/** Template folder nav views usually only define Main Item Edit targets; Create reuses the first main item. */
export function resolveFolderNavigationCreateLinkTarget(viewDto: any): any | null {
  const list = Array.isArray(viewDto?.AppFormLinkTargetList) ? viewDto.AppFormLinkTargetList : [];

  const explicitCreate = list.find(
    (lt: any) =>
      (lt.ActionType === EmAppLinkTargetActionType.Create ||
        lt.ActionType === EmAppLinkTargetActionType.CreateFromExistingItem) &&
      !isTemplateHeaderLinkTarget(lt),
  );
  if (explicitCreate) return adaptLinkTargetForFolderNavigationCreate(explicitCreate, viewDto);

  const headerItems = list
    .filter(
      (lt: any) =>
        lt?.OtherSettingsDto?.TemplateItemType === EmAppTransactionTemplateItemType.TemplateHeader,
    )
    .sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0));

  const mainItems = list
    .filter(
      (lt: any) =>
        lt?.OtherSettingsDto?.TemplateItemType === EmAppTransactionTemplateItemType.MainItem &&
        !isTemplateHeaderLinkTarget(lt),
    )
    .sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0));

  const seedTarget =
    headerItems[0] ??
    mainItems[0] ??
    list.find(
      (lt: any) =>
        lt.ActionType === EmAppLinkTargetActionType.Edit && !isTemplateHeaderLinkTarget(lt),
    );

  if (!seedTarget) return null;

  return adaptLinkTargetForFolderNavigationCreate(seedTarget, viewDto);
}

/** Open a template main item as New with only FolderId defaulted (folder navigation Create). */
export function adaptLinkTargetForFolderNavigationCreate(linkTarget: any, viewDto: any): any {
  const folderFieldName = resolveFolderIdFieldName(viewDto);
  const folderField = resolveFolderIdViewField(viewDto);
  const folderViewColumnId = folderField?.Id ?? folderField?.SearchViewFieldID ?? null;

  return {
    ...linkTarget,
    ActionType: EmAppLinkTargetActionType.Create,
    NavigationActionName: linkTarget?.NavigationActionName || 'Create',
    SourceViewColumnId1: folderViewColumnId,
    SourceViewColumnId2: null,
    SourceViewColumnId3: null,
    TargetColumn1: folderFieldName ?? linkTarget?.TargetColumn1 ?? null,
    TargetColumn2: null,
    TargetColumn3: null,
  };
}

export function buildFolderNavigationFormGroupCreatePayload(
  viewDto: any,
  folderId: number | string,
  searchResultRowList: any[] = [],
) {
  const seedTarget = resolveFolderNavigationCreateLinkTarget(viewDto);
  if (!seedTarget) return null;

  const folderFieldName = resolveFolderIdFieldName(viewDto);
  if (!folderFieldName) return null;

  const folderField = resolveFolderIdViewField(viewDto);
  const folderViewColumnId = folderField?.Id ?? folderField?.SearchViewFieldID ?? null;
  const createTarget = adaptLinkTargetForFolderNavigationCreate(seedTarget, viewDto);

  const dictDefaults: Record<string | number, unknown> = {};
  const emptyRow = { DictViewColumnIDKeyValue: {} as Record<string, unknown> };
  if (folderViewColumnId != null) {
    dictDefaults[folderViewColumnId] = folderId;
    emptyRow.DictViewColumnIDKeyValue[folderViewColumnId] = folderId;
  }

  const payload = buildFormGroupOpenPayload(
    createTarget,
    emptyRow,
    viewDto,
    searchResultRowList,
    dictDefaults,
  );

  return {
    ...payload,
    param2: {
      ...payload.param2,
      targetPkValue: null,
      openFrom: 'folderNavigation',
    },
    sessionData: {
      ...payload.sessionData,
      linkTargetDto: createTarget,
      selecedDataRow: emptyRow,
    },
  };
}

export function openFolderNavigationFormMasterDetail(
  addTabAndNavigate: (route: string, title: string, param: any, closable?: boolean) => void,
  transactionId: number | string,
  viewDto: any,
  folderId: number | string | null,
  actionLabel: string,
  row?: any,
  linkTarget?: any,
) {
  const folderFieldName = resolveFolderIdFieldName(viewDto);
  const param2: Record<string, unknown> = { linkTargetValueMapping: {} as Record<string, unknown> };
  let param1: string | null = null;

  if (actionLabel === 'Create' && folderFieldName && folderId != null) {
    (param2.linkTargetValueMapping as Record<string, unknown>)[folderFieldName] = folderId;
  } else if (linkTarget && row) {
    if (linkTarget.SourceViewColumnId1) {
      param1 = getDictViewColumnValue(row.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId1) ?? null;
    }
  }

  addTabAndNavigate('FormMasterDetail', actionLabel, {
    id: linkTarget?.LinkTargetTransactionId ?? transactionId,
    param1,
    param2: JSON.stringify(param2),
    preserveLinkTabTitle: true,
  });
}
