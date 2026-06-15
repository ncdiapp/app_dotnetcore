import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class AppTransactionService {
  
  async retrieveOneAppTransactionUnitExDto(unitId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppTransactionUnitExDto?unitId=${unitId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction unit');
    return response.json();
  }

  async retrieveAllAppTransactions(isSystemBuiltIn: any, transactionOrganizedType: any, isIncludeWorkflow: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllAppTransactions?isSystemBuitIn=${isSystemBuiltIn ?? ''}&transactionOrganizedType=${transactionOrganizedType ?? ''}&isIncludeWorkflow=${isIncludeWorkflow ?? ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all transactions');
    return response.json();
  }

  async getCurrentUserAvailableTransactions(isSystemBuiltIn: any, transactionType: any, includeReadonlyTransactions: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GetCurrentUserAvailableTransactions?isSystemBuitIn=${isSystemBuiltIn ?? ''}&transactionType=${transactionType ?? ''}&includeReadonlyTransactions=${includeReadonlyTransactions ?? ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get available transactions');
    return response.json();
  }

  async getOneAppTransactionData(id: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppTransaction?Id=${id}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction');
    return response.json();
  }

  /** Same as Angular `appTransactionSvc.RetrieveOneAppTransactionFieldExDto` — Transaction Field Editor load. */
  async retrieveOneAppTransactionFieldExDto(
    transFieldId: number | string,
    layoutItemId: number | string | null | undefined
  ): Promise<any> {
    const lid = layoutItemId != null && layoutItemId !== '' ? layoutItemId : '';
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppTransactionFieldExDto?transFieldId=${transFieldId}&layoutItemId=${lid}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to retrieve transaction field');
    return response.json();
  }

  /** Same as Angular `appTransactionSvc.SaveAppTransactionFieldExDto` — Transaction Field Editor save. */
  async saveAppTransactionFieldExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppTransactionFieldExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save transaction field');
    return response.json();
  }

  async convertTransactionFieldDataRetrieveMappingStringToDict(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ConvertTransactionFieldDataRetrieveMappingStringToDict`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to convert transaction field mapping');
    return response.json();
  }

  async convertBackTransactionFieldDataRetrieveMappingDictToString(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ConvertBackTransactionFieldDataRetrieveMappingDictToString`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to convert back transaction field mapping');
    return response.json();
  }

  async getOneHierarchyTransaction(transactionId: string, isNewTransaction: boolean, newTransactionType: string, newFolderTransactionUsageType: string, newTransactionDataSourceFrom: string, isESitePageDesign: boolean, rootWorkflowTransactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GetOneHierarchyTransaction?Id=${transactionId}&isNewTransaction=${isNewTransaction}&newTransactionType=${newTransactionType}&newFolderTransactionUsageType=${newFolderTransactionUsageType}&newTransactionDataSourceFrom=${newTransactionDataSourceFrom}&isESitePageDesign=${isESitePageDesign}&rootWorkflowTransactionId=${rootWorkflowTransactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get hierarchy transaction');
    return response.json();
  }

  /** Angular `appTransactionSvc.ConvertMasterDetaiFormDataToNotificationDto` — seed message from form row. */
  async convertMasterDetaiFormDataToNotificationDto(transactionId: string, transactionRid: string): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/ConvertMasterDetaiFormDataToNotificationDto?transactionId=${transactionId || ''}&transactionRid=${transactionRid || ''}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to convert form data to notification');
    return response.json();
  }

  async prepareNewWorkflowAutomation(dataSourceFrom: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/PrepareNewWorkflowAutomation?dataSourceFrom=${dataSourceFrom}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to prepare workflow automation');
    return response.json();
  }

  async saveOneWorkflowAutomation(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveOneWorkflowAutomation`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save workflow automation');
    return response.json();
  }

  async saveAsOneWorkflowAutomation(workflowTransactionId: number, newWorkflowSurffix: string, newWorkflowName: string, applicationId: string): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/SaveAsOneWorkflowAutomation?workflowTransactionId=${workflowTransactionId}&newWorkflowSurffix=${newWorkflowSurffix || ''}&newWorkflowName=${newWorkflowName || ''}&applicationId=${applicationId || ''}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to save as new workflow automation');
    return response.json();
  }

  async syncronizeWorkflowCommandNodeTreeFromActionList(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SyncronizeWorkflowCommandNodeTreeFromActionList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to sync workflow command node tree');
    return response.json();
  }

  /** Angular: DeubgWorkflowOneRootChildCommand — test-run one root-level workflow child task. */
  async deubgWorkflowOneRootChildCommand(
    workflowTransactionId: number,
    debugWorkflowRootChildCommandId: number,
    debugKey: string,
  ): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/DeubgWorkflowOneRootChildCommand?workflowTransactionId=${workflowTransactionId}&debugWorkflowRootChildCommandId=${debugWorkflowRootChildCommandId}&debugKey=${encodeURIComponent(debugKey || '')}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to debug workflow command');
    return response.json();
  }

  async createOneTransactionCommand(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/CreateOneTransactionCommand`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to create transaction command');
    return response.json();
  }

  async saveOneTransactionCommandActionList(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveOneTransactionCommandActionList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save transaction commands');
    return response.json();
  }

  async retrieveAllAppTransactionGroupDto(applicationId: number | string | null): Promise<any[]> {
    const q = applicationId != null ? `?applicationId=${applicationId}` : '';
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllAppTransactionGroupDto${q}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction groups');
    const data = await response.json();
    return Array.isArray(data) ? data : data?.ObjectList ?? data?.Object ?? [];
  }

  async retrieveOneAppTransactionGroupExDto(groupId: number | string | null): Promise<any> {
    if (groupId == null || groupId === '') return null;
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppTransactionGroupExDto?groupId=${groupId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction group');
    return response.json();
  }

  async saveAppTransactionGroupExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppTransactionGroupExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save transaction group');
    return response.json();
  }

  async deleteOneAppTransactionGroup(groupId: number | string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/DeleteOneAppTransactionGroup?groupId=${groupId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete transaction group');
    return response.json();
  }

  async saveOneAppTransaction(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppTransaction`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      let errorMessage = `Failed to save transaction: ${response.status} ${response.statusText}`;
      try {
        const errorData = await response.json();
        if (errorData?.message) {
          errorMessage = errorData.message;
        } else if (errorData?.ExceptionMessage) {
          errorMessage = errorData.ExceptionMessage;
        } else if (typeof errorData === 'string') {
          errorMessage = errorData;
        } else if (errorData) {
          errorMessage = JSON.stringify(errorData);
        }
      } catch (e) {
        // If response is not JSON, try to get text
        try {
          const errorText = await response.text();
          if (errorText) {
            errorMessage = errorText;
          }
        } catch (textError) {
          // Keep the default error message
        }
      }
      const error = new Error(errorMessage);
      (error as any).status = response.status;
      (error as any).response = response;
      throw error;
    }
    return response.json();
  }

  async generateUnitsFromSelectedApiNodes(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GenerateUnitsFromSelectedApiNodes`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to generate units from API nodes');
    return response.json();
  }

  async synchronizeTransactionUnitFieldsWithApiNodes(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SynchronizeTransactionUnitFieldsWithApiNodes`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to synchronize fields with API nodes');
    return response.json();
  }

  async saveAsAppTransaction(orgTransactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAsAppTransaction?orgTranscactionId=${orgTransactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to save as new transaction');
    return response.json();
  }

  async deleteOneAppTransaction(id: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/DeleteOneAppTransaction?Id=${id}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete transaction');
    return response.json();
  }

  async retrieveOneTransactionUnitLinkTargetList(transactionUnitId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneTransactionUnitLinkTargetList?transactionUnitId=${transactionUnitId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve unit link targets');
    return response.json();
  }

  async saveOneTransactionUnitLinkTargetList(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveOneTransactionUnitLinkTargetList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save unit link targets');
    return response.json();
  }

  async getTransactionDataSourceList(transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GetTransactionDataSourceList?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get data source list');
    return response.json();
  }

  async retrieveOneAppTransactionUnitLinkedSearchList(transactionUnitId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppTransactionUnitLinkedSearchList?transactionUnitId=${transactionUnitId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve unit linked searches');
    return response.json();
  }

  async retrieveOneAppTransactionUnitLinkedSearchExDto(transactionUnitLinkedSearchId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppTransactionUnitLinkedSearchExDto?transactionUnitLinkedSearchId=${transactionUnitLinkedSearchId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve unit linked search');
    return response.json();
  }

  async saveAppTransactionUnitLinkedSearchExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppTransactionUnitLinkedSearchExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save unit linked search');
    return response.json();
  }

  async deleteAppTransactionUnitLinkedSearch(transactionUnitLinkedSearchId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/DeleteAppTransactionUnitLinkedSearch?transactionUnitLinkedSearchId=${transactionUnitLinkedSearchId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete unit linked search');
    return response.json();
  }

  async retrieveAppTransactionFieldAggFunctionSetDto(fieldId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAppTransactionFieldAggFunctionSetDto?fieldId=${fieldId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve field aggregation function');
    return response.json();
  }

  async saveAppTransactionFieldAggFunctionSetDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppTransactionFieldAggFunctionSetDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save field aggregation function');
    return response.json();
  }

  async retrieveAppTransactionUnitFormulaSetDto(unitId: string, transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAppTransactionUnitFormulaSetDto?unitId=${unitId}&transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve unit formula set');
    return response.json();
  }

  async retrieveAppTransactionUnitFormulaSetDtoList(transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAppTransactionUnitFormulaSetDtoList?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve unit formula sets');
    return response.json();
  }

  async retrieveAppSearchViewFormulaSetDto(searchViewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAppSearchViewFormulaSetDto?searchViewId=${searchViewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve search view formula set');
    return response.json();
  }

  async saveAppSearchViewFormulaSetDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppSearchViewFormulaSetDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save search view formula set');
    return response.json();
  }

  async saveAppTransactionUnitFormulaSetDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppTransactionUnitFormulaSetDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save unit formula set');
    return response.json();
  }

  async saveAppTransactionFormulas(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppTransactionFormulas`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save transaction formulas');
    return response.json();
  }

  async validateOneFormulaDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ValidateOneFormulaDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to validate formula');
    return response.json();
  }

  // Data Load Settings (Transaction Data Load Management)
  async retrievOneAppTransactionDataLoadDto(transactionId: string | number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrievOneAppTransactionDataLoadDto?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction data load list');
    return response.json();
  }

  async retrievOneAppTransactionUnitDataLoadDto(transactionUnitId: string | number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrievOneAppTransactionUnitDataLoadDto?transactionUnitId=${transactionUnitId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve unit data load list');
    return response.json();
  }

  async saveAllAppTransactionDataLoadDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAllAppTransactionDataLoadDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save data load list');
    return response.json();
  }

  async retrieveOneAppTransactionDataLoadExDto(dataLoadId: string | number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppTransactionDataLoadExDto?dataLoadId=${dataLoadId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve data load');
    return response.json();
  }

  async saveAppTransactionDataLoadExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppTransactionDataLoadExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save data load');
    return response.json();
  }

  async deleteAppTransactionDataLoad(dataLoadId: string | number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/DeleteAppTransactionDataLoad?dataLoadId=${dataLoadId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete data load');
    return response.json();
  }

  /** Delete Flow (TransactionUnitDeleteFlowEditor). */
  async retrieveDeleteFlowListByTransactionUnitId(transactionUnitId: string | number): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveDeleteFlowListByTransactionUnitId?transactionUnitId=${transactionUnitId}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to retrieve delete flow list');
    return response.json();
  }

  async saveAllAppTransactionUnitDeleteFlowExDto(data: {
    TransactionUnitId: number;
    DeletedItemIds: number[];
    TransactionUnitDeleteFlowSet: any[];
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAllAppTransactionUnitDeleteFlowExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save delete flow');
    return response.json();
  }

  // Form data methods
  async getFormStructure(transactionId: number, transGroupId?: number): Promise<any> {
    const transGroupValue = transGroupId != null ? transGroupId : '';
    const url = `${endpoints.BASE_URL}/webapi/AppTransaction/GetFormStructure?transactionId=${transactionId}&transGroupId=${transGroupValue}`;
    const response = await fetch(url, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get form structure');
    return response.json();
  }

  async getFormData(data: { transactionId: number; rootPrimaryKeyValue?: string; transGroupId?: number; autoExecuteCommandId?: number; selectDataRow?: any }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GetFormData`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to get form data');
    return response.json();
  }

  /** List-edit form: load list data for a transaction (TransactionOrganizedType = List). */
  async getListEditFormData(transactionId: number | string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GetListEditFormData?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get list edit form data');
    return response.json();
  }

  /** List-edit form: save list data (AppListDataDto). Also used for mass-update (MassUpdateAppListDataDto). */
  async saveListEditFormData(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveListEditFormData`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save list edit form data');
    return response.json();
  }

  async saveDataModelData(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveDataModelData`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save form data');
    return response.json();
  }

  /** Save transaction form (master-detail). Sends full AppMasterDetailDto to SaveTransactionData. Use this for form save, not saveDataModelData. */
  async saveTransactionData(appformDataDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveTransactionData`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(appformDataDto)
    });
    if (!response.ok) throw new Error('Failed to save transaction data');
    return response.json();
  }

  /**
   * Master-detail runtime: Save As (duplicate) current row.
   * Angular: appTransactionSvc.saveAsMasterDetailTransactionData(currentFormData)
   * WebAPI: POST /webapi/AppTransaction/SaveAsMasterDetailTransactionData
   */
  async saveAsMasterDetailTransactionData(appformDataDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAsMasterDetailTransactionData`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(appformDataDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to save as transaction data');
    return response.json();
  }

  /** Master-detail runtime: run validation + calculation without saving. */
  async validateAndCalculateTransactionData(rootAppformDataDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ValidateAndCalculateTransactionData`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(rootAppformDataDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to validate and calculate transaction data');
    return response.json();
  }

  /**
   * Master-detail runtime: execute a server-side command for current form data.
   * Angular: appTransactionSvc.ExcuteTransactionCommonad(currentFormData)
   * WebAPI: POST /webapi/AppTransaction/ExcuteTransactionCommonad
   */
  async excuteTransactionCommonad(appformDataDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ExcuteTransactionCommonad`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(appformDataDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to execute transaction command');
    return response.json();
  }

  /**
   * Workflow automation runtime: execute workflow root command.
   * Angular: appTransactionSvc.ExecuteWorkflowRootCommonad(currentFormData)
   * WebAPI: POST /webapi/AppTransaction/ExecuteWorkflowRootCommonad
   */
  async executeWorkflowRootCommonad(appformDataDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ExecuteWorkflowRootCommonad`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(appformDataDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to execute workflow command');
    return response.json();
  }

  /**
   * Angular: appTransactionSvc.GetWorkflowAutomationRuntimeProgressData(transactionId, rootPrimaryKeyValue)
   */
  async getWorkflowAutomationRuntimeProgressData(
    transactionId: string | number,
    rootPrimaryKeyValue: string | number,
  ): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/GetWorkflowAutomationRuntimeProgressData?transactionId=${transactionId ?? ''}&rootPrimaryKeyValue=${encodeURIComponent(String(rootPrimaryKeyValue ?? ''))}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to load workflow execution progress');
    return response.json();
  }

  /**
   * Angular: appTransactionSvc.ForceStopWorkflowByBatchNumber(batchNumber, workflowAutomationId)
   */
  async forceStopWorkflowByBatchNumber(
    batchNumber: string,
    workflowAutomationId: string | number,
  ): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/ForceStopWorkflowByBatchNumber?batchNumber=${encodeURIComponent(batchNumber ?? '')}&workflowAutomationId=${workflowAutomationId ?? ''}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to force stop workflow');
    return response.json();
  }

  /** Master-detail form: create a new form DTO (Angular getNewFormData). */
  async getNewFormData(transactionId: number | string, isConfigTestRun: boolean = false): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/GetNewFormData?transactionId=${transactionId}&isConfigTestRun=${isConfigTestRun}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to get new form data');
    return response.json();
  }

  /** Angular appTransactionSvc.RetrieveTransactionDefaultNavigationDto */
  async retrieveTransactionDefaultNavigationDto(
    transactionId: number | string,
    isFolderNavigation = false,
  ): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveTransactionDefaultNavigationDto?transactionId=${transactionId}&isFolderNavigation=${isFolderNavigation}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to retrieve transaction default navigation');
    return response.json();
  }

  async buildAdvancedDBViewDtoFromTransaction(transactionId: number | string, isOnlyUseRootTable = true): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/BuildAdvancedDBViewDtoFromTransaction?transactionId=${transactionId}&isOnlyUseRootTable=${isOnlyUseRootTable}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to build database view from transaction');
    return response.json();
  }

  async retrieveFolderViewByTransactionId(transactionId: number | string): Promise<any[]> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveFolderViewBytransactionId?transactionId=${transactionId}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to retrieve folder view navigation list');
    const data = await response.json();
    return Array.isArray(data) ? data : [];
  }

  async retrieveQuickSearchByTransactionId(transactionId: number | string): Promise<any[]> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveQuickSearchBytransactionId?transactionId=${transactionId}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to retrieve quick search navigation list');
    const data = await response.json();
    return Array.isArray(data) ? data : [];
  }

  async saveFolderViewNavigationListExDto(payload: {
    TransactionId: number;
    TransactionNavigationExDtoSet: any[];
    DeletedItemIds?: number[];
    MgtRootFolderId?: number | null;
    IsEnableFolderSecurity?: boolean;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveFolderViewNavigationListExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        TransactionId: payload.TransactionId,
        TransactionNavigationExDtoSet: payload.TransactionNavigationExDtoSet,
        DeletedItemIds: payload.DeletedItemIds ?? [],
        MgtRootFolderId: payload.MgtRootFolderId ?? null,
        IsEnableFolderSecurity: payload.IsEnableFolderSecurity ?? false,
      }),
    });
    if (!response.ok) throw new Error('Failed to save folder view navigation');
    return response.json();
  }

  async saveQuickSearchNavigationListExDto(payload: {
    TransactionId: number;
    TransactionNavigationExDtoSet: any[];
    DeletedItemIds?: number[];
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveQuickSearchNavigationListExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        TransactionId: payload.TransactionId,
        TransactionNavigationExDtoSet: payload.TransactionNavigationExDtoSet,
        DeletedItemIds: payload.DeletedItemIds ?? [],
      }),
    });
    if (!response.ok) throw new Error('Failed to save quick search navigation');
    return response.json();
  }

  async retrieveAllRootFolderDtoList(folderType = 1): Promise<any[]> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllRootFolderDtoList?folderType=${folderType}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to retrieve root folder list');
    const data = await response.json();
    return Array.isArray(data) ? data : [];
  }

  async retrieveTemplateFolderNavigationConfig(templateSearchId: number | string): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveTemplateFolderNavigationConfig?templateSearchId=${templateSearchId}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to retrieve template folder navigation config');
    return response.json();
  }

  async saveTemplateFolderNavigationConfig(config: {
    TemplateSearchId: number;
    HostTransactionId: number;
    RootFolderId: number;
    SearchViewId?: number | null;
    IsEnableFolderSecurity?: boolean;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveTemplateFolderNavigationConfig`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(config),
    });
    if (!response.ok) throw new Error('Failed to save template folder navigation config');
    return response.json();
  }

  async resolveFolderNavigationRuntimeContext(transactionId: number | string): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/ResolveFolderNavigationRuntimeContext?transactionId=${transactionId}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to resolve folder navigation runtime context');
    return response.json();
  }

  async retrieveSaasApplicationTransactionList(applicationId: string | number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveSaasApplicationTransactionList?applicationId=${applicationId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve application transaction list');
    return response.json();
  }

  async retrieveSaasApplicationFormList(applicationId: string | number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveSaasApplicationFormList?applicationId=${applicationId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve application form list');
    return response.json();
  }

  async retrieveSaasApplicationWorkflowAutomationList(applicationId?: string | number | null): Promise<any[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveSaasApplicationWorkflowAutomationList?applicationId=${applicationId ?? ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve workflow automation list');
    const data = await response.json();
    return Array.isArray(data) ? data : [];
  }

  async retrieveSaasApplicationSearchList(applicationId: string | number | null): Promise<any[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveSaasApplicationSearchList?applicationId=${applicationId ?? ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve application search list');
    const data = await response.json();
    return Array.isArray(data) ? data : [];
  }

  /** List-edit transactions for a table (entity). Returns object like { [transactionId]: displayName }. */
  async retrieveListEditTransactionsBySchemaOwnerTableName(
    tableName: string,
    schemaOwner: string,
    dataSourceFrom: number | null
  ): Promise<Record<number, string> | null> {
    const ds = dataSourceFrom != null ? `&dataSourceFrom=${dataSourceFrom}` : '';
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveListEditTransactionsBySchemaOwnerTableName?tableName=${encodeURIComponent(tableName || '')}&schemaOwner=${encodeURIComponent(schemaOwner || '')}${ds}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to retrieve list edit transactions');
    return response.json();
  }

  /** Create default list-edit transaction for a table. Returns OperationCallResult with Object (AppTransactionExDto). */
  async createDefaultListTransactionFromTableName(
    tableName: string,
    dataSourceRegisterId: number | null,
    schemaOwner: string,
    saasApplicationId: number | string | null
  ): Promise<any> {
    const appId = saasApplicationId != null ? `&saasApplicationId=${saasApplicationId}` : '';
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/CreateDefaultListTransactionFromTableName?tableName=${encodeURIComponent(tableName || '')}&dataSourceRegisterId=${dataSourceRegisterId ?? ''}&schemaOwner=${encodeURIComponent(schemaOwner || '')}${appId}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to create default list transaction');
    return response.json();
  }

  async CreateHierarchyTransactionFromTables(setupDto: {
    MasterTableName: string;
    /** Each child can have its own list of grandchild table names */
    ChildTables: Array<{
      TableName: string;
      GrandChildTableNames?: string[];
    }>;
    DataSourceRegisterId?: number | null;
    SchemaOwner?: string;
    TransactionName?: string;
    SaasApplicationId?: number | null;
  }): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/CreateHierarchyTransactionFromTables`,
      {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(setupDto),
      }
    );
    if (!response.ok) throw new Error('Failed to create hierarchy transaction');
    return response.json();
  }

  async convertDbSchemaOwnerTableNameToTransactionUnitExDto(converterDto: {
    TableName: string;
    SchemaOwner: string;
    ParentUnit?: any;
    TransactionId: number;
    DataSourceRegisterId?: number | null;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ConvertDbSchemaOwnerTableNameToTransactionUnitExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(converterDto)
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to convert table to unit: ${errorText}`);
    }
    return response.json();
  }

  async convertTableColumnsToTransactionFieldExDtoList(converterDto: {
    SchemaOwner: string;
    TableName: string;
    ParentUnit?: any;
    TransactionId: number;
    NeedToAddDbColumns: any[];
    DataSourceRegisterId?: number | null;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ConvertTableColumnsToTransactionFieldExDtoList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(converterDto)
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to convert table columns to transaction fields: ${errorText}`);
    }
    return response.json();
  }

  // Form Design methods
  async retrieveOneAppFormFlexLayoutExDto(formId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppFormFlexLayoutExDto?formId=${formId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve form layout');
    return response.json();
  }

  async getNewFlexFormExDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GetNewFlexFormExDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get new form template');
    return response.json();
  }

  async saveAppFormFlexLayoutExDto(formData: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAppFormFlexLayoutExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(formData)
    });
    if (!response.ok) throw new Error('Failed to save form layout');
    return response.json();
  }

  async resetFormLayout(formId: number, resetToLayoutType: number, needToGenerateDefaultLayout: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/ResetFormLayout?formId=${formId}&resetToLayoutType=${resetToLayoutType}&needToGenerateDefaultLayout=${needToGenerateDefaultLayout}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to reset form layout');
    return response.json();
  }

  // Conditional Lock or Hide (Data Model Condition Lock)
  async retrieveAllAppConditionalActionListByTransactionId(transactionId: string | number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllAppConditionalActionListByTransactionId?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve conditional action list');
    return response.json();
  }

  async saveAllAppConditionalActionExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAllAppConditionalActionExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save conditional actions');
    return response.json();
  }

  // Internal Data Transfer Data Model
  async retrieveAllAppTransactionDataTransferSettingExDto(transactionId: string | number, destinationTransactionId?: string | number | null): Promise<any> {
    const dest = destinationTransactionId !== undefined && destinationTransactionId !== null ? `&destinationTransactionId=${destinationTransactionId}` : `&destinationTransactionId=${transactionId}`;
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllAppTransactionDataTransferSettingExDto?transactionId=${transactionId}${dest}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve data transfer settings');
    return response.json();
  }

  async retrieveOneAppTransactionDataTransferSettingExDto(settingId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneAppTransactionDataTransferSettingExDto?settingId=${settingId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve data transfer setting');
    return response.json();
  }

  async saveOneAppTransactionDataTransferSettingExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveOneAppTransactionDataTransferSettingExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save data transfer setting');
    return response.json();
  }

  /** Angular: appTransactionSvc.GenerateCommandCallApiOperationSetting */
  async generateCommandCallApiOperationSetting(commandDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GenerateCommandCallApiOperationSetting`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(commandDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to generate Call API operation setting');
    return response.json();
  }

  async deleteOneAppTransactionDataTransferSettingExDto(settingId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/DeleteOneAppTransactionDataTransferSettingExDto?settingId=${settingId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete data transfer setting');
    return response.json();
  }

  // Report (Transaction Report Editor)
  async retrieveAllAppTranscationReportListByTransactionId(transactionId: string | number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllAppTranscationReportListByTransactionId?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction report list');
    return response.json();
  }

  async saveAllAppTranscationReportExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveAllAppTranscationReportExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save transaction reports');
    return response.json();
  }

  /**
   * Used by SystemObjectSecurityEditor (non–partner-type): available roles/users for an object.
   * Matches Angular: webapi/AppTransaction/RetrieveSecurityObjectAvailableOrganizationAllRoleAndUser.
   * On 404 popup uses GetOrganizationGroupList + GetCompanyUserDtoList fallback.
   */
  async retrieveSecurityObjectAvailableOrganizationAllRoleAndUser(objId: string | number, objType: string | number, actionCode?: string | number | null): Promise<any> {
    const ac = actionCode != null ? String(actionCode) : 'null';
    const url = `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveSecurityObjectAvailableOrganizationAllRoleAndUser?objId=${objId}&objType=${objType}&actionCode=${ac}`;
    const response = await fetch(url, { headers: getHeaders() });
    if (!response.ok) throw new Error(`Failed to retrieve security object available role and user (${response.status})`);
    return response.json();
  }

  /** Move files to target folder (paste cut files). Matches Angular: appTransactionSvc.UpdateAppFileFolder. */
  async updateAppFileFolder(data: { TargetFolderId: number; FileIdList: number[] }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/UpdateAppFileFolder`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to move files to folder');
    return response.json();
  }

  /** Excel table import settings for Drag & Drop Post Process. Matches Angular: RetrieveAllExcelTableImportSettingDto. */
  async retrieveAllExcelTableImportSettingDto(isFlatSingleTableImport = false): Promise<any[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllExcelTableImportSettingDto?isFlatSingleTableImport=${isFlatSingleTableImport}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve Excel table import settings');
    const data = await response.json();
    return Array.isArray(data) ? data : data?.ObjectList ?? [];
  }

  /** DB-to-DB table import settings. Matches Angular: RetrieveAllDbToDbTableImportSettingDto. */
  async retrieveAllDbToDbTableImportSettingDto(): Promise<any[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllDbToDbTableImportSettingDto`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve DB-to-DB table import settings');
    const data = await response.json();
    return Array.isArray(data) ? data : data?.ObjectList ?? [];
  }

  /** DB-to-DB table import settings for an application. Matches Angular: RetrieveSaasApplicationDbToDbTableImportSettingList. */
  async retrieveSaasApplicationDbToDbTableImportSettingList(applicationId: string | number | null): Promise<any[]> {
    const appId = applicationId != null && applicationId !== '' ? applicationId : '';
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveSaasApplicationDbToDbTableImportSettingList?applicationId=${appId}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to retrieve application DB-to-DB import settings');
    const data = await response.json();
    return Array.isArray(data) ? data : data?.ObjectList ?? [];
  }

  /** Entity import settings for Drag & Drop Post Process. Matches Angular: RetrieveAllEntityImportSettingDto. */
  async retrieveAllEntityImportSettingDto(): Promise<any[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllEntityImportSettingDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve entity import settings');
    const data = await response.json();
    return Array.isArray(data) ? data : data?.ObjectList ?? [];
  }

  /** ER Diagram: list all. Matches Angular: RetrieveAllErDiagramDto. */
  async RetrieveAllErDiagramDto(): Promise<any[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAllErDiagramDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve ER diagrams');
    const data = await response.json();
    return Array.isArray(data) ? data : data?.ObjectList ?? [];
  }

  /** ER Diagram: get one. Matches Angular: RetrieveOneErDiagramExDto. */
  async RetrieveOneErDiagramExDto(dataSetId: number | null): Promise<any> {
    if (dataSetId == null) return null;
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveOneErDiagramExDto?dataSetId=${dataSetId}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to retrieve ER diagram');
    return response.json();
  }

  /** ER Diagram: save (create or update). Matches Angular: SaveOneErDiagramExDto. */
  async SaveOneErDiagramExDto(aAppDataSetExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/SaveOneErDiagramExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppDataSetExDto)
    });
    if (!response.ok) throw new Error('Failed to save ER diagram');
    return response.json();
  }

  /** ER Diagram: delete. Matches Angular: DeleteOneErDiagram. */
  async DeleteOneErDiagram(dataSetId: number | null): Promise<any> {
    if (dataSetId == null) return null;
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppTransaction/DeleteOneErDiagram?dataSetId=${dataSetId}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to delete ER diagram');
    return response.json();
  }

  /**
   * Runtime DDL/AutoComplete datasource.
   * Matches Angular: appTransactionSvc.RetrieveAutoCompleteDDLEntityItemSource(currentFormData).
   * Server expects an AppMasterDetailDto payload with:
   * - CurrentAutoCompleteFieldId
   * - CurrentAutoCompleteFieldQueryText
   * - (optional) CurrentEditRowDto
   */
  async RetrieveAutoCompleteDDLEntityItemSource(rootAppformDataDto: any): Promise<any[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/RetrieveAutoCompleteDDLEntityItemSource`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(rootAppformDataDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to retrieve DDL item source');
    const data = await response.json();
    return Array.isArray(data) ? data : data?.ObjectList ?? data?.Object ?? [];
  }

  /** Root unit cascading (DDL parent changed). Matches Angular: appTransactionSvc.GetRootUnitFieldTriggerCascadingDataSource(currentFormData). */
  async GetRootUnitFieldTriggerCascadingDataSource(rootAppformDataDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GetRootUnitFieldTriggerCascadingDataSource`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(rootAppformDataDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to retrieve cascading datasource (root)');
    return response.json();
  }

  /** Child/grandchild unit cascading (grid row parent changed). Matches Angular: appTransactionSvc.GetChildOrGrandChildUnitFieldTriggerCascadingDataSource({ AppChildDataDto, MasterDetailDataDto }). */
  async GetChildOrGrandChildUnitFieldTriggerCascadingDataSource(data: { AppChildDataDto: any; MasterDetailDataDto: any }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/GetChildOrGrandChildUnitFieldTriggerCascadingDataSource`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data ?? null),
    });
    if (!response.ok) throw new Error('Failed to retrieve cascading datasource (child/grandchild)');
    return response.json();
  }

  /** Angular appTransactionSvc.CreateWorkflowWindowsSchedulerTask */
  async createWorkflowWindowsSchedulerTask(appWinSchedulerTaskDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppTransaction/CreateWorkflowWindowsSchedulerTask`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(appWinSchedulerTaskDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to create Windows scheduler task');
    return response.json();
  }
}

export const appTransactionService = new AppTransactionService(); 