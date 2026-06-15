import { endpoints } from './endpoints';

interface TenantInfo {
  isFound: boolean;
  companyName?: string;
  domainToken?: string;
  customDomain?: string;
}

class TenantService {
  async GetTenantInfo(): Promise<TenantInfo> {
    try {
      const response = await fetch(`${endpoints.BASE_URL}/webapi/Tenant/Info`);
      if (!response.ok) return { isFound: false };
      return response.json();
    } catch {
      return { isFound: false };
    }
  }
}

export const tenantSvc = new TenantService();
export type { TenantInfo };
