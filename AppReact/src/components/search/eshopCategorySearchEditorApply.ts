/** Apply helpers mirroring Angular `eshopCategorySearchEditorCtrl.js` (category tree / card / filter). */

export const EmAppControlTypeTextBox = 2;
export const EmAppControlTypeDDL = 1;

export type LevelSetting = {
  Id: string | null;
  Name: string | null;
  Sort: string | null;
  EntityId: number | null;
};

export function findMaxSearchViewFieldSort(fieldList: any[] | undefined | null): number {
  if (!Array.isArray(fieldList) || fieldList.length === 0) return 0;
  let m = 0;
  for (const f of fieldList) {
    const s = Number(f?.Sort);
    if (Number.isFinite(s) && s > m) m = s;
  }
  return m;
}

export function prepareNewViewField(searchViewDto: any): any {
  const list = searchViewDto?.AppSearchViewFieldList ?? [];
  return {
    Sort: findMaxSearchViewFieldSort(list) + 1,
    DisplayText: '',
    IsVisible: true,
    IsGroupBy: false,
    IsDescOrder: false,
    SysTableFiledPath: ' ',
    ControlType: EmAppControlTypeTextBox,
    EntityId: null,
    DataType: null,
    ProductDetaiMapTransFiledId: null,
    IsMapToChartX: false,
    IsMapToChartY: false,
    IsFileFoderId: false,
    IsTransRootId: false,
    IsUserDefined1: false,
    IsUserDefined2: false,
    IsUserDefined3: false,
    IsUserDefined4: false,
  };
}

export function parseCategoryTreeLevelsFromView(searchViewDto: any): { levels: number; lv: LevelSetting[] } {
  const lv: LevelSetting[] = [];
  for (let i = 0; i < 5; i++) {
    lv.push({ Id: null, Name: null, Sort: null, EntityId: null });
  }
  let maxLevel = 1;
  const fields = searchViewDto?.AppSearchViewFieldList ?? [];
  for (const vf of fields) {
    const tl = Number(vf?.TreeLevel);
    if (!tl || tl < 1 || tl > 5) continue;
    const idx = tl - 1;
    maxLevel = Math.max(maxLevel, tl);
    if (vf.IsTreeNodeId) lv[idx].Id = vf.SysTableFiledPath ?? null;
    if (vf.IsTreeNodeDisplay) {
      lv[idx].Name = vf.SysTableFiledPath ?? null;
      lv[idx].EntityId = vf.EntityId ?? null;
    }
    if (vf.OrderByLevel === 1) lv[idx].Sort = vf.SysTableFiledPath ?? null;
  }
  return { levels: Math.min(5, Math.max(1, maxLevel)), lv };
}

export function validateCategoryTreeLevels(levels: number, levelRows: LevelSetting[]): string | null {
  for (let i = 1; i <= levels; i++) {
    const levelObj = levelRows[i - 1];
    if (!levelObj?.Id || !levelObj?.Name) {
      return `Level ${i} category must have Id and Display fields selected.`;
    }
  }
  return null;
}

export function applyCategoryTreeSetting(searchViewDto: any, levels: number, levelRows: LevelSetting[]): string | null {
  if (!searchViewDto) return 'Missing search view.';
  searchViewDto.AppSearchViewFieldList = searchViewDto.AppSearchViewFieldList ?? [];
  searchViewDto.DeletedItemsIds = searchViewDto.DeletedItemsIds ?? [];
  searchViewDto.IsModified = true;

  for (const vf of searchViewDto.AppSearchViewFieldList) {
    delete vf.isKeep;
  }

  const v = validateCategoryTreeLevels(levels, levelRows);
  if (v) return v;

  for (let i = 1; i <= levels; i++) {
    const levelObj = levelRows[i - 1];

    let isIdFieldExist = false;
    let isNameFieldExist = false;
    let isSortFieldExist = false;

    for (const viewFieldDto of searchViewDto.AppSearchViewFieldList) {
      if (!viewFieldDto?.Id) continue;
      if (viewFieldDto.SysTableFiledPath === levelObj.Id && viewFieldDto.IsTreeNodeId) {
        viewFieldDto.isKeep = true;
        viewFieldDto.TreeLevel = i;
        viewFieldDto.IsModified = true;
        isIdFieldExist = true;
      } else if (viewFieldDto.SysTableFiledPath === levelObj.Name && viewFieldDto.IsTreeNodeDisplay) {
        viewFieldDto.isKeep = true;
        viewFieldDto.TreeLevel = i;
        isNameFieldExist = true;
        viewFieldDto.IsModified = true;
        if (levelObj.EntityId) {
          viewFieldDto.ControlType = EmAppControlTypeDDL;
          viewFieldDto.EntityId = levelObj.EntityId;
        } else {
          viewFieldDto.ControlType = EmAppControlTypeTextBox;
          viewFieldDto.EntityId = null;
        }
      } else if (viewFieldDto.SysTableFiledPath === levelObj.Sort && viewFieldDto.OrderByLevel === 1) {
        viewFieldDto.isKeep = true;
        viewFieldDto.TreeLevel = i;
        viewFieldDto.IsModified = true;
        isSortFieldExist = true;
      }
    }

    if (!isIdFieldExist) {
      const newField = prepareNewViewField(searchViewDto);
      searchViewDto.AppSearchViewFieldList.push(newField);
      newField.IsModified = true;
      newField.isKeep = true;
      newField.TreeLevel = i;
      newField.IsTreeNodeId = true;
      newField.SysTableFiledPath = levelObj.Id;
    }

    if (!isNameFieldExist) {
      const newField = prepareNewViewField(searchViewDto);
      searchViewDto.AppSearchViewFieldList.push(newField);
      newField.IsModified = true;
      newField.isKeep = true;
      newField.TreeLevel = i;
      newField.IsTreeNodeDisplay = true;
      newField.SysTableFiledPath = levelObj.Name;
      if (levelObj.EntityId) {
        newField.ControlType = EmAppControlTypeDDL;
        newField.EntityId = levelObj.EntityId;
      }
    }

    if (!isSortFieldExist && levelObj.Sort) {
      const newField = prepareNewViewField(searchViewDto);
      searchViewDto.AppSearchViewFieldList.push(newField);
      newField.IsModified = true;
      newField.isKeep = true;
      newField.TreeLevel = i;
      newField.OrderByLevel = 1;
      newField.SysTableFiledPath = levelObj.Sort;
    }
  }

  const needToRemovFieldList: any[] = [];
  for (const viewFieldDto of searchViewDto.AppSearchViewFieldList) {
    viewFieldDto.DisplayText = viewFieldDto.SysTableFiledPath;
    if (viewFieldDto.Id && !viewFieldDto.isKeep) {
      needToRemovFieldList.push(viewFieldDto);
    }
  }

  for (const deleteField of needToRemovFieldList) {
    if (deleteField.Id) {
      deleteField.IsModified = true;
      searchViewDto.DeletedItemsIds.push(deleteField.Id);
      const index = searchViewDto.AppSearchViewFieldList.indexOf(deleteField);
      if (index >= 0) searchViewDto.AppSearchViewFieldList.splice(index, 1);
    }
  }

  return null;
}

export type CardMappingLevel = {
  ViewFieldId: number | null;
  SearchFieldName: string | null;
};

export function applyEshopCardSearchViewSetting(
  categorySearchViewDto: any,
  eshopCardSearchExDto: any,
  levels: CardMappingLevel[],
  rootGroupKeyField: string | null,
): string | null {
  if (!categorySearchViewDto) return 'Category view is missing.';
  if (!eshopCardSearchExDto) return 'Eshop card search is missing.';

  categorySearchViewDto.OtherSettingsDto = categorySearchViewDto.OtherSettingsDto ?? {};
  categorySearchViewDto.IsModified = true;
  const mappingObj: any = {
    UiId: `ui_${Date.now()}_${Math.random().toString(36).slice(2, 9)}`,
    LinkTargetSearchId: eshopCardSearchExDto.Id,
  };
  for (let i = 1; i <= 5; i++) {
    const lo = levels[i - 1];
    if (lo) {
      mappingObj[`SourceViewColumnId${i}`] = lo.ViewFieldId;
      mappingObj[`TargetSearchFieldName${i}`] = lo.SearchFieldName;
    }
  }
  categorySearchViewDto.OtherSettingsDto.EshopCategorySearchMapping = mappingObj;

  const cardView = eshopCardSearchExDto.DefaultSearchViewExDto;
  if (!cardView?.AppSearchViewFieldList) return null;

  const fieldList = cardView.AppSearchViewFieldList;
  let isRootGroupKeyFieldAssigned = false;
  for (const viewFieldDto of fieldList) {
    if (!viewFieldDto) continue;
    viewFieldDto.IsGroupBy = false;
    if (rootGroupKeyField && viewFieldDto.SysTableFiledPath === rootGroupKeyField) {
      viewFieldDto.IsGroupBy = true;
      isRootGroupKeyFieldAssigned = true;
    }
  }

  if (!isRootGroupKeyFieldAssigned && rootGroupKeyField) {
    const vf = prepareNewViewField(cardView);
    fieldList.push(vf);
    vf.IsModified = true;
    vf.IsGroupBy = true;
    vf.SysTableFiledPath = rootGroupKeyField;
  }

  return null;
}

export type FilterOptionRow = {
  Sort: number;
  Label: string;
  Id: string | null;
  Name: string | null;
  EntityId: number | null;
};

export function applyEshopCardViewFilterSetting(cardSearchViewDto: any, optionList: FilterOptionRow[]): void {
  if (!cardSearchViewDto) return;
  cardSearchViewDto.AppSearchViewFieldList = cardSearchViewDto.AppSearchViewFieldList ?? [];
  cardSearchViewDto.DeletedItemsIds = cardSearchViewDto.DeletedItemsIds ?? [];
  cardSearchViewDto.IsModified = true;

  const needToRemovFieldList: any[] = [];
  for (const viewFieldDto of cardSearchViewDto.AppSearchViewFieldList) {
    if (viewFieldDto?.Id && viewFieldDto.TreeLevel) {
      needToRemovFieldList.push(viewFieldDto);
    }
  }
  for (const deleteField of needToRemovFieldList) {
    if (deleteField.Id) {
      deleteField.IsModified = true;
      cardSearchViewDto.DeletedItemsIds.push(deleteField.Id);
      const index = cardSearchViewDto.AppSearchViewFieldList.indexOf(deleteField);
      if (index >= 0) cardSearchViewDto.AppSearchViewFieldList.splice(index, 1);
    }
  }

  const sorted = [...optionList].sort((a, b) => (a.Sort || 0) - (b.Sort || 0));
  for (const optionObj of sorted) {
    if (!optionObj.Sort || !optionObj.Id) continue;

    const newSearchViewField = prepareNewViewField(cardSearchViewDto);
    newSearchViewField.IsModified = true;
    newSearchViewField.IsTreeNodeId = true;
    newSearchViewField.TreeLevel = optionObj.Sort;
    newSearchViewField.SysTableFiledPath = optionObj.Id;
    cardSearchViewDto.AppSearchViewFieldList.push(newSearchViewField);
    newSearchViewField.IsVisible = false;

    if (!optionObj.Name) {
      newSearchViewField.IsTreeNodeDisplay = true;
      newSearchViewField.DisplayText = optionObj.Label || newSearchViewField.SysTableFiledPath;
      newSearchViewField.EntityId = optionObj.EntityId ?? null;
      if (newSearchViewField.EntityId) {
        newSearchViewField.ControlType = EmAppControlTypeDDL;
      }
    } else if (optionObj.Name === optionObj.Id) {
      newSearchViewField.IsTreeNodeDisplay = true;
      newSearchViewField.DisplayText = optionObj.Label || newSearchViewField.SysTableFiledPath;
      newSearchViewField.EntityId = optionObj.EntityId ?? null;
      if (newSearchViewField.EntityId) {
        newSearchViewField.ControlType = EmAppControlTypeDDL;
      }
    } else {
      const newDisplayField = prepareNewViewField(cardSearchViewDto);
      newDisplayField.IsModified = true;
      newDisplayField.TreeLevel = optionObj.Sort;
      newDisplayField.IsTreeNodeDisplay = true;
      newDisplayField.SysTableFiledPath = optionObj.Name;
      newDisplayField.DisplayText = optionObj.Label || newDisplayField.SysTableFiledPath;
      newDisplayField.IsVisible = false;
      newDisplayField.EntityId = optionObj.EntityId ?? null;
      if (newDisplayField.EntityId) {
        newDisplayField.ControlType = EmAppControlTypeDDL;
      }
      cardSearchViewDto.AppSearchViewFieldList.push(newDisplayField);
    }
  }
}

export function buildFilterOptionListFromCardView(
  cardSearchViewDto: any,
  _dataSetColumns: { Id: string }[],
): FilterOptionRow[] {
  const fieldList = cardSearchViewDto?.AppSearchViewFieldList ?? [];
  const dict: Record<number, FilterOptionRow> = {};
  for (const viewFieldDto of fieldList) {
    if (!viewFieldDto?.TreeLevel || !(viewFieldDto.IsTreeNodeId || viewFieldDto.IsTreeNodeDisplay)) continue;
    const n = Number(viewFieldDto.TreeLevel);
    if (!dict[n]) {
      dict[n] = {
        Sort: n,
        Label: '',
        Id: null,
        Name: null,
        EntityId: null,
      };
    }
    const o = dict[n];
    if (viewFieldDto.IsTreeNodeId) o.Id = viewFieldDto.SysTableFiledPath ?? null;
    if (viewFieldDto.IsTreeNodeDisplay) {
      o.Label = viewFieldDto.DisplayText || viewFieldDto.SysTableFiledPath || '';
      o.Name = viewFieldDto.SysTableFiledPath ?? null;
      o.EntityId = viewFieldDto.EntityId ?? null;
    }
  }
  const list = Object.keys(dict)
    .map((k) => dict[Number(k)])
    .sort((a, b) => a.Sort - b.Sort);
  if (list.length > 0) return list;
  return [
    {
      Sort: 1,
      Label: '',
      Id: null,
      Name: null,
      EntityId: null,
    },
  ];
}

export function findMaxOptionSort(optionList: FilterOptionRow[]): number {
  let m = 0;
  for (const o of optionList) {
    if (Number.isFinite(o.Sort) && o.Sort > m) m = o.Sort;
  }
  return m;
}
