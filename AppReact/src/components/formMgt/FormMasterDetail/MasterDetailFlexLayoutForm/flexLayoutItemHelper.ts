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

/** Skip hidden fields and empty LayoutRow shells so they do not reserve row space. */
export function shouldRenderRuntimeLayoutItem(layoutItem: any): boolean {
  if (!layoutItem) return false;

  const fieldDto = layoutItem.ForeignAppTransactionFieldExDto ?? layoutItem.foreignAppTransactionFieldExDto;
  if (fieldDto?.IsFormLayoutVisible === false) {
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
