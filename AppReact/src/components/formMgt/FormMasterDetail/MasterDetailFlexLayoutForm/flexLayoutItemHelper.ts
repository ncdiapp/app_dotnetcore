/** Stable EmAppFormLayoutItemType values (fallback when enum dictionary not loaded yet). */
export const FLEX_LAYOUT_ITEM_TYPE = {
  LayoutRow: 101,
  Section: 102,
  TabContainer: 107,
} as const;

/** Read WidgetDisplayType from layout item (PascalCase or camelCase API). */
export function getLayoutWidgetDisplayType(layoutItem: any): number | undefined {
  const da = layoutItem?.DomAttribute ?? layoutItem?.domAttribute;
  const raw = da?.WidgetDisplayType ?? da?.widgetDisplayType;
  if (raw == null || raw === '') return undefined;
  const n = Number(raw);
  return Number.isFinite(n) ? n : undefined;
}

export function resolveLayoutRowType(layoutItemTypeEnum: Record<string, number> | null | undefined): number {
  return layoutItemTypeEnum?.LayoutRow ?? FLEX_LAYOUT_ITEM_TYPE.LayoutRow;
}

export function resolveSectionType(layoutItemTypeEnum: Record<string, number> | null | undefined): number {
  return layoutItemTypeEnum?.Section ?? FLEX_LAYOUT_ITEM_TYPE.Section;
}

export function resolveTabContainerType(layoutItemTypeEnum: Record<string, number> | null | undefined): number {
  return layoutItemTypeEnum?.TabContainer ?? FLEX_LAYOUT_ITEM_TYPE.TabContainer;
}

/** True when DomAttribute.IsTab is set (legacy auto-gen tabs may omit this). */
export function hasLayoutItemIsTabFlag(layoutItem: any): boolean {
  const da = layoutItem?.DomAttribute ?? layoutItem?.domAttribute;
  return !!(da?.IsTab ?? da?.isTab);
}

/**
 * Direct children of a TabContainer are tab items even when IsTab was never persisted
 * (e.g. AppFormFlexLayoutBL.AppendNewTab historically omitted IsTab).
 */
export function isLayoutTabItem(
  layoutItem: any,
  parentLayoutItem: any | null | undefined,
  layoutItemTypeEnum?: Record<string, number> | null
): boolean {
  if (!layoutItem) return false;
  if (hasLayoutItemIsTabFlag(layoutItem)) return true;
  if (!parentLayoutItem) return false;
  const tabContainerType = resolveTabContainerType(layoutItemTypeEnum);
  return getLayoutWidgetDisplayType(parentLayoutItem) === tabContainerType;
}

/** Walk form layout tree and set IsTab=true on every direct child of TabContainer. */
export function normalizeTabItemsIsTabFlag(
  rootItems: any[] | null | undefined,
  layoutItemTypeEnum?: Record<string, number> | null
): boolean {
  if (!Array.isArray(rootItems) || rootItems.length === 0) return false;
  const tabContainerType = resolveTabContainerType(layoutItemTypeEnum);
  let changed = false;

  const walk = (items: any[]) => {
    for (const item of items) {
      if (!item) continue;
      const children = item.AppFormLayoutItem_List ?? item.appFormLayoutItem_List;
      if (!Array.isArray(children) || children.length === 0) continue;

      if (getLayoutWidgetDisplayType(item) === tabContainerType) {
        for (const tab of children) {
          if (!tab) continue;
          if (!tab.DomAttribute) tab.DomAttribute = {};
          if (!tab.DomAttribute.IsTab) {
            tab.DomAttribute.IsTab = true;
            changed = true;
          }
        }
      }
      walk(children);
    }
  };

  walk(rootItems);
  return changed;
}

export function isLayoutRowItem(layoutItem: any, layoutRowType?: number | null): boolean {
  const rowType = layoutRowType ?? FLEX_LAYOUT_ITEM_TYPE.LayoutRow;
  return getLayoutWidgetDisplayType(layoutItem) === rowType;
}

export function getLayoutColSpan(layoutItem: any): number {
  const da = layoutItem?.DomAttribute ?? layoutItem?.domAttribute;
  const raw = da?.ColSpanValue ?? da?.colSpanValue;
  const n = Number(raw);
  return Number.isFinite(n) && n > 0 ? n : 24;
}

function getChildLayoutItems(layoutItem: any): any[] {
  const raw = layoutItem?.AppFormLayoutItem_List ?? layoutItem?.appFormLayoutItem_List;
  return Array.isArray(raw) ? raw : [];
}

/** Parse nullable bool from API (bool, 0/1, "true"/"false"). Returns null when unset. */
export function normalizeOptionalBool(value: unknown): boolean | null {
  if (value == null || value === '') return null;
  if (value === true || value === 1 || value === '1') return true;
  if (value === false || value === 0 || value === '0') return false;
  const s = String(value).trim().toLowerCase();
  if (s === 'true' || s === 'yes') return true;
  if (s === 'false' || s === 'no') return false;
  return Boolean(value);
}

/**
 * Runtime visibility for transaction fields (grid columns + form layout).
 * Design-time IsVisible is copied to IsFormLayoutVisible during security load on DictAllTransactionField;
 * unit.AppTransactionFieldList often only has IsVisible set.
 */
export function isRuntimeTransactionFieldVisible(field: any): boolean {
  if (!field) return false;

  const layoutVisible = normalizeOptionalBool(field.IsFormLayoutVisible);
  if (layoutVisible === false) return false;
  if (layoutVisible === true) return true;

  const configVisible = normalizeOptionalBool(field.IsVisible);
  if (configVisible === false) return false;
  return true;
}

/** Merge security/runtime flags from DictAllTransactionField when present. */
export function enrichTransactionFieldFromDict(field: any, dictAllTransactionField?: Record<string | number, any> | null): any {
  if (!field || !dictAllTransactionField) return field;
  const id = field.Id ?? field.id;
  if (id == null) return field;
  const fromDict = dictAllTransactionField[id] ?? dictAllTransactionField[Number(id)] ?? dictAllTransactionField[String(id)];
  if (!fromDict) return field;
  return {
    ...field,
    IsVisible: fromDict.IsVisible ?? field.IsVisible,
    IsFormLayoutVisible: fromDict.IsFormLayoutVisible ?? field.IsFormLayoutVisible,
    IsFormLayoutReadOnly: fromDict.IsFormLayoutReadOnly ?? field.IsFormLayoutReadOnly,
    IsReadonly: fromDict.IsReadonly ?? fromDict.IsReadOnly ?? field.IsReadonly ?? field.IsReadOnly,
  };
}

/** Skip hidden fields and empty LayoutRow shells so they do not reserve row space. */
export function shouldRenderRuntimeLayoutItem(layoutItem: any): boolean {
  if (!layoutItem) return false;

  const fieldDto = layoutItem.ForeignAppTransactionFieldExDto ?? layoutItem.foreignAppTransactionFieldExDto;
  if (fieldDto && !isRuntimeTransactionFieldVisible(fieldDto)) {
    return false;
  }

  const displayType = getLayoutWidgetDisplayType(layoutItem);
  if (displayType === FLEX_LAYOUT_ITEM_TYPE.LayoutRow) {
    return getChildLayoutItems(layoutItem).some((child) => shouldRenderRuntimeLayoutItem(child));
  }

  const da = layoutItem.DomAttribute ?? layoutItem.domAttribute ?? {};
  const visibleExpression = da.VisibleExpression ?? da.visibleExpression;
  if (visibleExpression === 'false') {
    return false;
  }

  return true;
}
