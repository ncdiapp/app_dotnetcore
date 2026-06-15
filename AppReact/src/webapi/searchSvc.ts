import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

class SearchService {
  
  // Data Set Entity Operations
  async retrieveAllAppDataSetEntityDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveAllAppDataSetEntityDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all app data set entities');
    return response.json();
  }

  async retrieveQueryColumnList(dataSetId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveQueryColumnList?dataSetId=${dataSetId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve query column list');
    return response.json();
  }

  async retrieveDataSetQueryColumnList(aAppDataSetExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveDataSetQueryColumnList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppDataSetExDto)
    });
    if (!response.ok) throw new Error('Failed to retrieve data set query column list');
    return response.json();
  }

  async retrieveOneAppDataSetExDto(dataSetId: string, isGetDbDiagram: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveOneAppDataSetExDto?dataSetId=${dataSetId}&isGetDbDiagram=${isGetDbDiagram}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app data set');
    return response.json();
  }

  async saveOneAppDataSetEntityDto(aAppDataSetExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveOneAppDataSetEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppDataSetExDto)
    });
    if (!response.ok) throw new Error('Failed to save app data set entity');
    return response.json();
  }

  async deleteOneAppDataSetEntityDto(dataSetId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/DeleteOneAppDataSetEntityDto?dataSetId=${dataSetId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app data set entity');
    return response.json();
  }

  // Data Set Extract View Operations
  async retrieveExtractDataSetList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveExtractDataSetList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve extract data set list');
    return response.json();
  }

  async retrieveOneExtractAppDataSetExDto(extractDataSetId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveOneExtractAppDataSetExDto?extractDataSetId=${extractDataSetId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve extract app data set');
    return response.json();
  }

  async saveOneExtractAppDataSetExDto(aAppDataSetExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveOneExtractAppDataSetExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppDataSetExDto)
    });
    if (!response.ok) throw new Error('Failed to save extract app data set');
    return response.json();
  }

  async deleteOneExtractAppDataSetExDto(extractDataSetId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/DeleteOneExtractAppDataSetExDto?extractDataSetId=${extractDataSetId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete extract app data set');
    return response.json();
  }

  // Search Operations
  async retrieveStatciSearchAvailableViewWithSameQueryBL(dataSetId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveStatciSearchAvailableViewWithSameQueryBL?dataSetId=${dataSetId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve static search available views');
    return response.json();
  }

  async retrieveOneAppSearchExDto(searchId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveOneAppSearchExDto?searchId=${searchId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app search');
    return response.json();
  }

  async retrieveAllAppSearchDto(searchUsageType?: number | null): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveAllAppSearchDto?searchUsageType=${searchUsageType ?? ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all app searches');
    return response.json();
  }

  async saveAppSearchExDto(aAppSearchExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveAppSearchExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppSearchExDto)
    });
    if (!response.ok) throw new Error('Failed to save app search');
    return response.json();
  }

  async saveEshopCategorySearchExDto(aAppSearchExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveEshopCategorySearchExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppSearchExDto)
    });
    if (!response.ok) throw new Error('Failed to save eshop category search');
    return response.json();
  }

  async saveAsSearch(searchId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveAsSearch?searchId=${searchId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to save as search');
    return response.json();
  }

  async deleteAppSearch(searchId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/DeleteAppSearch?searchId=${searchId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app search');
    return response.json();
  }

  // Search View Operations
  async retrieveOneAppSearchViewExDto(searchViewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveOneAppSearchViewExDto?searchViewId=${searchViewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app search view');
    return response.json();
  }

  async retrieveAllAppSearchViewDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveAllAppSearchViewDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all app search views');
    return response.json();
  }

  async retrieveAllSearchViewDtoByViewType(viewType: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveAllSearchViewDtoByViewType?viewType=${viewType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve search views by type');
    return response.json();
  }

  async saveAppSearchViewExDto(aAppSearchViewExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveAppSearchViewExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppSearchViewExDto)
    });
    if (!response.ok) throw new Error('Failed to save app search view');
    return response.json();
  }

  async saveAsSearchView(searchViewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveAsSearchView?searchViewId=${searchViewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to save as search view');
    return response.json();
  }

  async deleteAppSearchView(searchViewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/DeleteAppSearchView?searchViewId=${searchViewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app search view');
    return response.json();
  }

  // Search View Link Target Operations
  async retrieveOneSearchViewLinkTargetList(searchViewId: string, usageType: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveOneSearchViewLinkTargetList?searchViewId=${searchViewId}&usageType=${usageType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve search view link target list');
    return response.json();
  }

  async saveOneSearchViewLinkTargetList(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveOneSearchViewLinkTargetList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save search view link target list');
    return response.json();
  }

  async retrieveOneAppViewLinkedSeaechOrUrlExDto(searchViewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveOneAppViewLinkedSeaechOrUrlExDto?searchViewId=${searchViewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app view linked search or URL');
    return response.json();
  }

  async saveAllAppViewLinkedSeaechOrUrlEntityDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveAllAppViewLinkedSeaechOrUrlEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app view linked search or URL entities');
    return response.json();
  }

  // Field Mapping Operations
  async retrieveAppViewFiledSearchFiledMappingBySearchViewId(searchViewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveAppViewFiledSearchFiledMappingBySearchViewId?searchViewId=${searchViewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app view field search field mapping');
    return response.json();
  }

  async saveAllAppViewFiledSearchFiledMappingExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveAllAppViewFiledSearchFiledMappingExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app view field search field mapping');
    return response.json();
  }

  async createNewSearchViewForm(searchViewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/CreateNewSearchViewForm?searchViewId=${searchViewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to create new search view form');
    return response.json();
  }

  async retrieveAllAppSearchFieldDtoList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveAllAppSearchFieldDtoList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all app search field DTOs');
    return response.json();
  }

  // Execute AppSearch Search And View Operations
  async retrieveDefaultSearch(searchUsageType: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/RetrieveDefaultSearch?searchUsageType=${searchUsageType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve default search');
    return response.json();
  }

  async retrieveOneSearch(searchId: string, isSavedSearch: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/RetrieveOneSearch?searchId=${searchId}&isSavedSearch=${isSavedSearch}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve one search');
    return response.json();
  }

  async processSearchResult(searchViewExternalUriDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/ProcessSearchResult`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchViewExternalUriDto)
    });
    if (!response.ok) throw new Error('Failed to process search result');
    return response.json();
  }

  async retrieveSearchResult(searchDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/retrieveSearchResult`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDto)
    });
    if (!response.ok) throw new Error('Failed to retrieve search result');
    return response.json();
  }

  async fullTextSearch(keywords: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/FullTextSearch`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(keywords)
    });
    if (!response.ok) throw new Error('Failed to perform full text search');
    return response.json();
  }

  async fullTextFileSearch(keywords: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/FullTextFileSearch?keywords=${encodeURIComponent(keywords)}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to perform full text file search');
    return response.json();
  }

  async retrieveSearchesByUsageType(emSearchUsageType: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/RetrieveSearchesByUsageType?emSearchUsageType=${emSearchUsageType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve searches by usage type');
    return response.json();
  }

  async retrieveUserViewsBySearchDefinition(searchDefinition: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/RetrieveUserViewsBySearchDefinition`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDefinition)
    });
    if (!response.ok) throw new Error('Failed to retrieve user views by search definition');
    return response.json();
  }

  async retrieveViewDictEntityLookupItemDto(viewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/RetrieveViewDictEntityLookupItemDto?viewId=${viewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve view dict entity lookup item DTO');
    return response.json();
  }

  async retrieveOneReferenceViewDto(viewId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/RetrieveOneReferenceViewDto?viewId=${viewId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve one reference view DTO');
    return response.json();
  }

  // Saved Search Operations
  async saveCriteriaPreset(searchDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/SaveCriteriaPreset`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDto)
    });
    if (!response.ok) throw new Error('Failed to save criteria preset');
    return response.json();
  }

  async saveCriteriaPresetAs(searchDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/SaveCriteriaPresetAs`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDto)
    });
    if (!response.ok) throw new Error('Failed to save criteria preset as');
    return response.json();
  }

  async deleteCriteriaPreset(searchDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/DeleteCriteriaPreset`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDto)
    });
    if (!response.ok) throw new Error('Failed to delete criteria preset');
    return response.json();
  }

  async setAsDefaultCriteriaPreset(searchDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/SetAsDefaultCriteriaPreset`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDto)
    });
    if (!response.ok) throw new Error('Failed to set as default criteria preset');
    return response.json();
  }

  async changeSearchAutoExecute(searchDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/ChangeSearchAutoExecute`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDto)
    });
    if (!response.ok) throw new Error('Failed to change search auto execute');
    return response.json();
  }

  async saveMassUpdateResult(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/SaveMassUpdateResult`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save mass update result');
    return response.json();
  }

  async cascadingSearchCriteriaValueChanged(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/CascadingSearchCriteriaValueChanged`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to handle cascading search criteria value change');
    return response.json();
  }

  async retrieveSearchApiSettings(searchId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearch/RetrieveSearchApiSettings?searchId=${searchId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve search API settings');
    return response.json();
  }

  async setSearchForPublicAccess(searchId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SetSearchForPublicAccess?searchId=${searchId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to set search for public access');
    return response.json();
  }

  // Search Field Operations
  async retrieveOneAppSearchFieldExDto(searchFieldId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveOneAppSearchFieldExDto?searchFieldId=${searchFieldId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app search field');
    return response.json();
  }

  async saveAppSearchFieldExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveAppSearchFieldExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app search field');
    return response.json();
  }

  async retrieveOneAppSearchViewFieldExDto(searchViewFieldId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/RetrieveOneAppSearchViewFieldExDto?searchViewFieldId=${searchViewFieldId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app search view field');
    return response.json();
  }

  async saveAppSearchViewFieldExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/SaveAppSearchViewFieldExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app search view field');
    return response.json();
  }

  async generateQueryEntityFromDataSetField(datasetId: string, datasetFieldName: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppSearchViewConfig/GenerateQueryEntityFromDataSetField?datasetId=${datasetId}&datasetFieldName=${encodeURIComponent(datasetFieldName)}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to generate query entity from data set field');
    return response.json();
  }

}

export const searchSvc = new SearchService(); 