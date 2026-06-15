import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

class DynamicLayoutService {
  /**
   * Gets transaction form structure with caching support.
   * This endpoint returns AppTransactionExDto with:
   * - Cached base structure (TransactionOrganizedType, form layout) - same for all users
   * - Fresh user-specific security data (IsAllowAccess, field visibility) - NOT cached
   * 
   * @param transactionId - The transaction ID
   * @param transGroupId - Optional transaction group ID
   * @param rootPkId - Optional root primary key ID
   * @param isPrint - Optional print flag ("true" or "false")
   * @param opennedFormAutoExecuteCommandId - Optional auto-execute command ID
   * @param isPreview - Optional preview flag
   * @returns Promise<AppTransactionExDto> - The transaction form structure with security
   */
  async getTransactionForm(
    transactionId: number,
    transGroupId?: number,
    rootPkId?: string,
    isPrint?: string,
    opennedFormAutoExecuteCommandId?: number,
    isPreview?: string
  ): Promise<any> {
    const params = new URLSearchParams();
    
    // Always include all parameter keys, even if empty/undefined
    params.append('transactionId', transactionId.toString());
    params.append('transGroupId', transGroupId !== undefined && transGroupId !== null ? transGroupId.toString() : '');
    params.append('rootPkId', rootPkId || '');
    params.append('isPrint', isPrint || '');
    params.append('opennedFormAutoExecuteCommandId', opennedFormAutoExecuteCommandId !== undefined && opennedFormAutoExecuteCommandId !== null ? opennedFormAutoExecuteCommandId.toString() : '');
    params.append('isPreview', isPreview || '');

    // Use relative BASE_URL so CRA dev proxy is applied (avoids cross-origin/CORS in dev).
    const url = `${endpoints.BASE_URL}/webapi/DynamicLayout/TransactionForm?${params.toString()}`;
    
    const response = await fetch(url, {
      headers: getHeaders()
    });
    
    if (!response.ok) {
      throw new Error(`Failed to get transaction form structure: ${response.statusText}`);
    }
    
    return response.json();
  }
}

export const dynamicLayoutService = new DynamicLayoutService();

