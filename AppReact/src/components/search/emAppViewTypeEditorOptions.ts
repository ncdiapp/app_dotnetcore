/**
 * Search View Editor — View Type dropdown options (Id + user-facing Display).
 * Source: legacy Angular `PlmApplication/Scripts1x/mgtCtrl/Search/searchViewEditorCtrl.js`
 * (`initialDataModel` → `dataModel.appViewTypeList`).
 *
 * Order matches Angular. Backend enum `EmAppViewType` also defines values not listed here
 * (e.g. WorkflowView); those fall back to enum member names via `resolveAppViewTypeDisplay`.
 */
export const APP_VIEW_TYPE_EDITOR_OPTIONS: ReadonlyArray<{ Id: number; Display: string }> = [
  { Id: 1, Display: "Grid View" },
  { Id: 2, Display: "Card View" },
  { Id: 7, Display: "Chart View" },
  { Id: 5, Display: "Pivot View" },
  { Id: 25, Display: "Cluster Analysis View" },
  { Id: 17, Display: "Google Map View" },
  { Id: 15, Display: "Gantt View" },
  { Id: 16, Display: "Scheduler View" },
  { Id: 6, Display: "Calendar View" },
  // { Id: 23, Display: "Hierarchy Master Detail View (Show Child View Details on Each Row)" },
  // { Id: 18, Display: "Recursive Data Set Tree View (Folder Tree Like)" },
  { Id: 8, Display: "E-Shop - Flat Data Set Tree View (Category Tree)" },
  { Id: 9, Display: "E-Shop - Item List View (With Group and Filter Options)" },
  { Id: 10, Display: "E-Shop - Item Detail View (Show Item Details On Popup)" },
  // { Id: 14, Display: "Date Picker - Select Available Date on Calendar" },
  // { Id: 19, Display: "Date Time Picker - Select Available Date and Time on Calendar" },
];

const EDITOR_DISPLAY_BY_ID = new Map<number, string>(
  APP_VIEW_TYPE_EDITOR_OPTIONS.map((o) => [o.Id, o.Display])
);

/** Same UX label as Id 9 in Angular editor; C# enum uses 21 for `EShopOrderListView`. */
const ESHOP_ORDER_LIST_DISPLAY = "E-Shop - Item List View (With Group and Filter Options)";

/**
 * Friendly label for a view type: Angular View Editor wording when defined, else enum key from session dictionary.
 */
export function resolveAppViewTypeDisplay(
  viewTypeId: number | null | undefined,
  emAppViewType: Record<string, number> | null | undefined
): string {
  if (viewTypeId == null) return "—";
  const fromEditor = EDITOR_DISPLAY_BY_ID.get(viewTypeId);
  if (fromEditor) return fromEditor;
  if (viewTypeId === 21) return ESHOP_ORDER_LIST_DISPLAY;
  if (emAppViewType) {
    const pair = Object.entries(emAppViewType).find(([, id]) => id === viewTypeId);
    if (pair) return pair[0];
  }
  return String(viewTypeId);
}
