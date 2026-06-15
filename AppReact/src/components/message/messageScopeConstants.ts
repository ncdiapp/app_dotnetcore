/**
 * Sidebar scope list mirrors Angular `messageManagementCtrl` (`messageScopeTypeList` /
 * `businessPartnerMessageScopeTypeList`). Ids align with `EmAppMessgaeScopeType`.
 */
export const MESSAGE_SCOPE_SIDEBAR_FULL = [
  { Id: 10, Display: 'Company Message', SortOrder: 0 },
  { Id: 1, Display: 'Generic Message', SortOrder: 1 },
  { Id: 2, Display: 'Workflow Message', SortOrder: 2 },
  { Id: 5, Display: 'Form Message', SortOrder: 5 },
  { Id: 6, Display: 'Project Message', SortOrder: 6 },
  { Id: 7, Display: 'Task Message', SortOrder: 7 },
  { Id: 99, Display: 'Message Template', SortOrder: 99 },
];

export const MESSAGE_SCOPE_SIDEBAR_BUSINESS_PARTNER = [
  { Id: 1, Display: 'Generic Message', SortOrder: 1 },
  { Id: 2, Display: 'Workflow Message', SortOrder: 2 },
  { Id: 5, Display: 'Form Message', SortOrder: 5 },
  { Id: 6, Display: 'Project Message', SortOrder: 6 },
  { Id: 7, Display: 'Task Message', SortOrder: 7 },
];

export const MESSAGE_TEMPLATE_SCOPE_TYPE = 99;
