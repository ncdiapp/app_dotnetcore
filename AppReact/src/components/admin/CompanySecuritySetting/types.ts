/** Tab indices matching Angular CompanyManagement tabs */
export const TAB = {
  CompanyInfo: 0,
  User: 1,
  Role: 2,
  Privilege: 3,
  ContactGroup: 4,
  ApplicationMenu: 5,
  MenuRole: 6,
  Dashboard: 7,
  IntegrationToken: 8,
  // SysAdmin-only: company-scoped user management from master DB
  TenantAdminUsers: 9,
} as const;

export type CompanyDto = {
  Id?: number;
  Code?: string;
  ShortName?: string;
  FullName?: string;
  IsModified?: boolean;
  OtherSettingsDto?: Record<string, any>;
  [key: string]: any;
};
