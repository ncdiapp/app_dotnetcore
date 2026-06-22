import React, { useCallback, useEffect, useState, useRef } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, PropertyGroupDescription, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { setUserMenu } from '../../../redux/features/admin/userSessionSlice';
import { useDispatch } from 'react-redux';
import { adminSvc } from '../../../webapi/adminsvc';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';
import ApplicationFormBuilder from '../../transaction/ApplicationFormBuilder';
import TransactionFolderNavigationQuickBuilder from '../../transaction/TransactionFolderNavigationQuickBuilder';
import { useAlertConfirm } from '../../common/AlertConfirmProvider';
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../../hooks/useClampedContextMenuPosition';

const SEARCH_MENU_ESTIMATED_WIDTH = 200;
const SEARCH_MENU_ESTIMATED_HEIGHT = 400;
const FOLDER_MENU_ESTIMATED_WIDTH = 240;
const FOLDER_MENU_ESTIMATED_HEIGHT = 180;
const TRANSACTION_MENU_ESTIMATED_WIDTH = 240;
const TRANSACTION_MENU_ESTIMATED_HEIGHT = 480;

// Transaction Organized Type (matching enum)
const TransactionOrganizedType = {
  MasterDetail: 1, // Form Model
  List: 3 // List Edit Model
};

interface DataModelDesignProps {
  menuId: string | null;
}

const DataModelDesign: React.FC<DataModelDesignProps> = ({ menuId }) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo, showWarning } = useErrorMessage();
  const dispatch = useDispatch();
  const { showConfirm } = useAlertConfirm();
  const { addTabAndNavigate } = useTabNavigation();
  
  const [, setTransactionList] = useState<any[]>([]);
  const [transactionCV, setTransactionCV] = useState<CollectionView | null>(null);
  const [dataSourceRegisterList, setDataSourceRegisterList] = useState<any[]>([]);
  const [searchDataMap, setSearchDataMap] = useState<DataMap | null>(null);
  const [searchViewDataMap, setSearchViewDataMap] = useState<DataMap | null>(null);
  const [datasourceRegisterDataMap, setDatasourceRegisterDataMap] = useState<DataMap | null>(null);
  const [searchList, setSearchList] = useState<any[]>([]);
  const [searchViewList, setSearchViewList] = useState<any[]>([]);
  
  // Context menu state
  const [showSearchMenu, setShowSearchMenu] = useState<{ visible: boolean; x: number; y: number; item: any }>({ visible: false, x: 0, y: 0, item: null });
  const [showFolderMenu, setShowFolderMenu] = useState<{ visible: boolean; x: number; y: number; item: any }>({ visible: false, x: 0, y: 0, item: null });
  const [showCreateMenu, setShowCreateMenu] = useState(false);
  const [showDataSourceSubmenu, setShowDataSourceSubmenu] = useState<string | null>(null);
  const [showTransactionContextMenu, setShowTransactionContextMenu] = useState<{ visible: boolean; x: number; y: number; item: any }>({ visible: false, x: 0, y: 0, item: null });
  const createMenuRef = useRef<HTMLDivElement>(null);
  const searchMenuRef = useRef<HTMLDivElement | null>(null);
  const folderMenuRef = useRef<HTMLDivElement | null>(null);
  const transactionContextMenuRef = useRef<HTMLDivElement | null>(null);
  
  // ApplicationFormBuilder popup state
  const [showFormBuilder, setShowFormBuilder] = useState(false);
  const [formBuilderParams, setFormBuilderParams] = useState<{
    defaultSectionCode: string;
    isCreateNewItem: boolean;
    transactionId?: number | null;
    transactionType: number | null;
    dataSourceRegisterId: number | null;
    isCreateDtoDataModel: boolean;
    isCreateApiDataModel: boolean;
    isCreateDataModelView: boolean;
    modelName: string | null;
  } | null>(null);
  
  const flexGridRef = useRef<any>(null);
  const [showFolderQuickBuilder, setShowFolderQuickBuilder] = useState(false);
  const [folderQuickBuilderTransactionId, setFolderQuickBuilderTransactionId] = useState<number | null>(null);

  // Transaction Organized Type display mapping
  const transactionOrganizedTypeMap: Record<number, string> = {
    1: '1. Form Model',
    3: '2. List Edit Model',
    7: '3. Excel Import Draft'
  };

  const getTransactionOrganizedTypeDisplay = (type: number): string => {
    return transactionOrganizedTypeMap[type] || `Type ${type}`;
  };

  // Load data source registers
  useEffect(() => {
    const loadDataSources = async () => {
      try {
        const dataSources = await adminSvc.getDataSourceRegisterList(false);
        const filtered = dataSources.filter((ds: any) => ds.Id !== 2147483647);
        setDataSourceRegisterList(filtered);
        
        const dataMap = new DataMap(filtered, 'Id', 'DataSourceName');
        setDatasourceRegisterDataMap(dataMap);
      } catch (error) {
        console.error('Failed to load data sources:', error);
      }
    };
    
    if (menuId) {
      loadDataSources();
    }
  }, [menuId]);

  // Load search and search view data maps
  useEffect(() => {
    const loadSearchData = async () => {
      try {
        const massEntityData = await adminSvc.getMassEntitiesLookupItem('AppSearch|AppSearchView');
        const searches = massEntityData['AppSearch'] || [];
        const searchViews = massEntityData['AppSearchView'] || [];
        
        setSearchList(searches);
        setSearchViewList(searchViews);
        setSearchDataMap(new DataMap(searches, 'Id', 'Display'));
        setSearchViewDataMap(new DataMap(searchViews, 'Id', 'Display'));
      } catch (error) {
        console.error('Failed to load search data:', error);
      }
    };
    
    if (menuId) {
      loadSearchData();
    }
  }, [menuId]);

  const loadTransactionList = useCallback(async (manageBusy = true) => {
    if (!menuId) return;

    try {
      if (manageBusy) {
        dispatch(setIsBusy());
      }

      const transactionDtoList = await appTransactionService.retrieveSaasApplicationTransactionList(menuId);

      const transactionsWithDisplay = transactionDtoList.map((t: any) => ({
        ...t,
        TransactionOrganizedTypeDisplay: getTransactionOrganizedTypeDisplay(t.TransactionOrganizedType)
      }));

      const cv = new CollectionView(transactionsWithDisplay);
      cv.groupDescriptions.push(new PropertyGroupDescription('TransactionOrganizedTypeDisplay'));
      cv.sortDescriptions.push(new SortDescription('TransactionOrganizedType', true));
      cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
      cv.sortDescriptions.push(new SortDescription('Id', false));

      setTransactionList(transactionsWithDisplay);
      setTransactionCV(cv);
    } catch (error: any) {
      showError(error.message || 'Failed to load transaction list');
    } finally {
      if (manageBusy) {
        dispatch(setIsNotBusy());
      }
    }
  }, [menuId, dispatch, showError]);

  // Load transaction list
  useEffect(() => {
    loadTransactionList();
  }, [loadTransactionList]);

  // Close context menus when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (showSearchMenu.visible) {
        setShowSearchMenu({ visible: false, x: 0, y: 0, item: null });
      }
      if (showFolderMenu.visible) {
        setShowFolderMenu({ visible: false, x: 0, y: 0, item: null });
      }
      if (showCreateMenu && createMenuRef.current && !createMenuRef.current.contains(e.target as Node)) {
        setShowCreateMenu(false);
      }
    };
    
    if (showSearchMenu.visible || showFolderMenu.visible || showCreateMenu) {
      document.addEventListener('click', handleClickOutside);
      return () => {
        document.removeEventListener('click', handleClickOutside);
      };
    }
  }, [showSearchMenu.visible, showFolderMenu.visible, showCreateMenu]);

  const handleRefresh = () => {
    if (menuId) {
      appTransactionService.retrieveSaasApplicationTransactionList(menuId)
        .then(transactionDtoList => {
          const transactionsWithDisplay = transactionDtoList.map((t: any) => ({
            ...t,
            TransactionOrganizedTypeDisplay: getTransactionOrganizedTypeDisplay(t.TransactionOrganizedType)
          }));
          
          const cv = new CollectionView(transactionsWithDisplay);
          cv.groupDescriptions.push(new PropertyGroupDescription('TransactionOrganizedTypeDisplay'));
          cv.sortDescriptions.push(new SortDescription('TransactionOrganizedType', true));
          cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
          cv.sortDescriptions.push(new SortDescription('Id', false));
          setTransactionList(transactionsWithDisplay);
          setTransactionCV(cv);
        })
        .catch((error: any) => showError(error.message || 'Failed to refresh'));
    }
  };

  // Create menu handlers
  const handleCreateTransaction = (transactionType: number, isCreateDtoDataModel?: boolean, dataSourceRegisterId?: number, isCreateApiDataModel?: boolean, isCreateDataModelView?: boolean) => {
    setShowCreateMenu(false);
    setShowDataSourceSubmenu(null);
    
    // Open ApplicationFormBuilder popup with parameters
    // Matching AngularJS: openFormBuilder(EmFormBuilderSectionCode.Transaction, ...)
    setFormBuilderParams({
      defaultSectionCode: 'TransactionGraphicEditor',
      isCreateNewItem: true,
      transactionType: transactionType,
      dataSourceRegisterId: dataSourceRegisterId || null,
      isCreateDtoDataModel: isCreateDtoDataModel || false,
      isCreateApiDataModel: isCreateApiDataModel || false,
      isCreateDataModelView: isCreateDataModelView || false,
      modelName: null
    });
    setShowFormBuilder(true);
  };

  const handleCreateFormFromScratch = () => {
    setShowCreateMenu(false);
    
    // TODO: Navigate to form builder
    // Similar to AngularJS: openFormBuilder(EmFormBuilderSectionCode.Form, ...)
    console.log('Create Form From Scratch');
    showError('Form builder not yet implemented. This will open the form designer.');
  };

  const handleCreateFromExcel = (dataSourceRegisterId?: number) => {
    setShowCreateMenu(false);
    setShowDataSourceSubmenu(null);
    
    // TODO: Open file uploader for Excel import
    // Similar to AngularJS: openImportFileUploader($event, dataSourceRegisterId, transactionType)
    console.log('Create From Excel', { dataSourceRegisterId });
    showError('Excel import not yet implemented. This will open the file uploader.');
  };

  // Transaction context menu handlers
  const handleEditTransaction = (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.Id || !menuId) return;
    
    const modelName = item.TransactionName || item.Name || 'Form Builder';
    addTabAndNavigate('application-form-builder', modelName, {
      id: menuId,
      defaultSectionCode: 'TransactionGraphicEditor',
      isCreateNewItem: false,
      transactionId: item.Id,
      transactionType: item.TransactionOrganizedType || null,
      dataSourceRegisterId: null,
      isCreateDtoDataModel: false,
      isCreateApiDataModel: false,
      isCreateDataModelView: false,
      modelName: item.TransactionName || item.Name || null
    }, true);
  };

  const handleSaveAsTransaction = async (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.Id) return;
    
    const confirmed = await showConfirm(`Please Confirm To Save As: ${item.TransactionName || item.Name}`, {
      title: 'Confirm'
    });
    
    if (!confirmed) return;
    
    try {
      dispatch(setIsBusy());
      const result = await appTransactionService.saveAsAppTransaction(item.Id.toString());
      
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      
      if (result?.Object) {
        const transactionDto = result.Object;
        // Open the new transaction in ApplicationFormBuilder
        setFormBuilderParams({
          defaultSectionCode: 'TransactionGraphicEditor',
          isCreateNewItem: false,
          transactionId: transactionDto.Id,
          transactionType: transactionDto.TransactionOrganizedType || null,
          dataSourceRegisterId: null,
          isCreateDtoDataModel: false,
          isCreateApiDataModel: false,
          isCreateDataModelView: false,
          modelName: transactionDto.Name || transactionDto.TransactionName || null
        });
        setShowFormBuilder(true);
        handleRefresh();
      }
    } catch (error: any) {
      showError(error.message || 'Failed to save as new transaction');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleDeleteTransaction = async (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.Id) return;
    
    const confirmed = await showConfirm(`Please Confirm To Delete: ${item.TransactionName || item.Name}`, {
      title: 'Confirm'
    });
    
    if (!confirmed) return;
    
    try {
      dispatch(setIsBusy());
      const result = await appTransactionService.deleteOneAppTransaction(item.Id.toString());
      
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      
      if (result?.ValidationResult?.IsValid) {
        handleRefresh();
      }
    } catch (error: any) {
      showError(error.message || 'Failed to delete transaction');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleUpdateDataFromExcel = (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.OtherOptions) return;
    
    const importSettingId = item.OtherOptions.TransactionDataUpdateImportSettingId;
    if (importSettingId) {
      // TODO: Open file uploader for updating from Excel
      // Similar to AngularJS: openImportFileUploader(null, item.DataSourceFrom, null, true, importSettingId)
      showError('Update from Excel not yet implemented.');
    }
  };

  const handleEditConsumeAPIs = (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.Id) return;
    
    // Open ApplicationFormBuilder with Consume API section
    setFormBuilderParams({
      defaultSectionCode: 'TransactionApiSetting_Consume',
      isCreateNewItem: false,
      transactionId: item.Id,
      transactionType: item.TransactionOrganizedType || null,
      dataSourceRegisterId: null,
      isCreateDtoDataModel: false,
      isCreateApiDataModel: false,
      isCreateDataModelView: false,
      modelName: item.TransactionName || item.Name || null
    });
    setShowFormBuilder(true);
  };

  const handleEditProvideAPIs = (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.Id) return;
    
    // Open ApplicationFormBuilder with Provide API section
    setFormBuilderParams({
      defaultSectionCode: 'TransactionApiSetting_Provide',
      isCreateNewItem: false,
      transactionId: item.Id,
      transactionType: item.TransactionOrganizedType || null,
      dataSourceRegisterId: null,
      isCreateDtoDataModel: false,
      isCreateApiDataModel: false,
      isCreateDataModelView: false,
      modelName: item.TransactionName || item.Name || null
    });
    setShowFormBuilder(true);
  };

  const handleAddToAppMenu = async (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.Id) return;
    
    try {
      dispatch(setIsBusy());
      const result = await schemaMetadataService.quickGenerateTransactionDefaultSearchNavigation(item.Id);

      if (result?.ValidationResult?.Items && result.ValidationResult.Items.length > 0) {
        const lastItem = result.ValidationResult.Items[result.ValidationResult.Items.length - 1];
        showValidationMessages({ Items: [lastItem] }, true);
      } else if (result?.IsSuccessful) {
        showInfo('Transaction added to app menu successfully');
      }

      if (result?.IsSuccessful) {
        await loadTransactionList(false);
        try {
          const userMenu = await adminSvc.retrieveUserTreeMenu();
          dispatch(setUserMenu(userMenu));
        } catch {
          // Non-blocking — transaction list already refreshed.
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to add to app menu');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleViewImportSetting = (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.OtherOptions) return;
    
    const importSettingId = item.OtherOptions.TransactionDataUpdateImportSettingId || item.OtherOptions.ImportSettingId;
    if (importSettingId) {
      const label = `Import Setting ${importSettingId}`;
      addTabAndNavigate('rest-api-import-editor', label, { id: String(importSettingId) }, true);
    }
  };

  const handleDeleteImportSetting = async (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.OtherOptions) return;
    
    const importSettingId = item.OtherOptions.TransactionDataUpdateImportSettingId || item.OtherOptions.ImportSettingId;
    if (!importSettingId) return;
    
    const confirmed = await showConfirm('Please Confirm To Delete This Import Setting.', {
      title: 'Confirm'
    });
    
    if (!confirmed) return;
    
    try {
      dispatch(setIsBusy());
      // TODO: Call API to delete import setting
      // Similar to AngularJS: searchSvc.deleteOneAppDataSetEntityDto(importSettingId)
      showError('Delete import setting not yet implemented.');
    } catch (error: any) {
      showError(error.message || 'Failed to delete import setting');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleImportFromLibrary = () => {
    // Matching AngularJS: openApplicationDataModelSelectorPopup - opens popup to select data models from library to import
    showInfo('Import From Library is not yet implemented. It will open a selector to import data models from the application library.');
  };

  const handleCreateImportSettingForTransaction = (item: any) => {
    setShowTransactionContextMenu({ visible: false, x: 0, y: 0, item: null });
    
    if (!item || !item.Id) return;
    
    // TODO: Open file uploader for creating import setting
    // Similar to AngularJS: openImportFileUploader(null, item.DataSourceFrom, null, null, null, item.Id)
    showError('Create import setting not yet implemented.');
  };

  // Render menu cell template for transaction grid
  const transactionMenuCellTemplate = (cell: any) => {
    if (!cell.item) return null;
    
    return (
      <div className="flex items-center justify-center w-full">
        <button
          className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
          title="More Options"
          onClick={(e) => {
            e.stopPropagation();
            const rect = e.currentTarget.getBoundingClientRect();
            const { x, y } = clampContextMenuPosition(
              rect.right,
              rect.top,
              TRANSACTION_MENU_ESTIMATED_WIDTH,
              TRANSACTION_MENU_ESTIMATED_HEIGHT
            );
            setShowTransactionContextMenu({
              visible: true,
              x,
              y,
              item: cell.item
            });
          }}
        >
          <i className="fa-solid fa-pencil text-xs"></i>
          <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5"></i>
        </button>
      </div>
    );
  };

  // Render Default Search cell
  const defaultSearchCellTemplate = (cell: any) => {
    if (!cell.item) return null;
    const item = cell.item;
    
    if (item.TransactionOrganizedType === TransactionOrganizedType.MasterDetail && 
        !item.OtherOptions?.IsApiIntegrationTransaction) {
      const searchDisplay = searchDataMap?.getDisplayValue(item.DefaultNavigationSearchId) || '';
      
      return (
        <div className="flex items-center w-full text-left">
          <button
            className={`${theme.menu_default} w-8 h-6 flex items-center justify-center flex-none`}
            title="Default Search Options"
            onClick={(e) => {
              e.stopPropagation();
              const rect = e.currentTarget.getBoundingClientRect();
              const { x, y } = clampContextMenuPosition(
                rect.right,
                rect.top,
                SEARCH_MENU_ESTIMATED_WIDTH,
                SEARCH_MENU_ESTIMATED_HEIGHT
              );
              setShowSearchMenu({
                visible: true,
                x,
                y,
                item: item
              });
            }}
          >
            <i className="fa-solid fa-magnifying-glass text-xs"></i>
            <i className="fa-solid fa-ellipsis-vertical text-[8px] ml-0.5"></i>
          </button>
          <div className="flex-auto w-1 overflow-hidden px-2 text-xs truncate">
            {searchDisplay}
          </div>
        </div>
      );
    }
    return null;
  };

  const reloadTransactionGrid = handleRefresh;

  const handleEditFolderTreeView = (item: any) => {
    setShowFolderMenu({ visible: false, x: 0, y: 0, item: null });
    if (!item?.DefaultNavigationFolderViewId) {
      showWarning('No folder tree view configured for this transaction.');
      return;
    }
    addTabAndNavigate('search-view-editor', 'Edit Folder Tree View', { id: item.DefaultNavigationFolderViewId }, true);
  };

  const handleSetupFolderTreeNavigation = (item: any) => {
    setShowFolderMenu({ visible: false, x: 0, y: 0, item: null });
    if (!item?.Id) return;
    setFolderQuickBuilderTransactionId(item.Id);
    setShowFolderQuickBuilder(true);
  };

  const handleOpenTransactionNavigationEditor = (item: any) => {
    setShowFolderMenu({ visible: false, x: 0, y: 0, item: null });
    if (!item?.Id) return;
    addTabAndNavigate('transaction-navigation-editor', 'Transaction Navigation Editor', { id: item.Id }, true);
  };

  // Render Folder Tree cell
  const folderTreeCellTemplate = (cell: any) => {
    if (!cell.item) return null;
    const item = cell.item;
    
    if (item.TransactionOrganizedType === TransactionOrganizedType.MasterDetail && 
        !item.OtherOptions?.IsApiIntegrationTransaction) {
      const folderDisplay = searchViewDataMap?.getDisplayValue(item.DefaultNavigationFolderViewId) || '';
      
      return (
        <div className="flex items-center w-full text-left">
          <button
            className={`${theme.menu_default} w-8 h-6 flex items-center justify-center flex-none`}
            title="Folder Tree Options"
            onClick={(e) => {
              e.stopPropagation();
              const rect = e.currentTarget.getBoundingClientRect();
              const { x, y } = clampContextMenuPosition(
                rect.right,
                rect.top,
                FOLDER_MENU_ESTIMATED_WIDTH,
                FOLDER_MENU_ESTIMATED_HEIGHT
              );
              setShowFolderMenu({
                visible: true,
                x,
                y,
                item: item
              });
            }}
          >
            <i className="fa-solid fa-pencil text-xs"></i>
            <i className="fa-regular fa-folder text-[10px] ml-0.5"></i>
          </button>
          <div className="flex-auto w-1 overflow-hidden px-2 text-xs truncate">
            {folderDisplay}
          </div>
        </div>
      );
    }
    return null;
  };

  useRefineContextMenuField(showSearchMenu.visible, searchMenuRef, setShowSearchMenu);
  useRefineContextMenuField(showFolderMenu.visible, folderMenuRef, setShowFolderMenu);
  useRefineContextMenuField(showTransactionContextMenu.visible, transactionContextMenuRef, setShowTransactionContextMenu);

  return (
    <div className="h-full flex flex-col gap-1">
      {/* Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Data Model Design
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={handleRefresh}
            className="px-3 py-1 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center gap-1"
            title="Refresh"
          >
            <i className="fa-solid fa-rotate"></i>
            <span>Refresh</span>
          </button>
          <div className="relative" ref={createMenuRef}>
            <button
              onClick={(e) => {
                e.stopPropagation();
                setShowCreateMenu(!showCreateMenu);
              }}
              className="px-3 py-1 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600 flex items-center gap-1"
              title="Create"
            >
              <i className="fa-solid fa-plus"></i>
              <span>Create</span>
              <i className="fa-solid fa-caret-down text-[10px]"></i>
            </button>
            {showCreateMenu && (
              <div
                className={`absolute right-0 mt-1 min-w-max rounded-[4px] shadow-lg ${theme.mainContentSection} border z-50`}
                onClick={(e) => e.stopPropagation()}
              >
                <div className="py-1" role="menu">
                  {/* Create Data Model Hierarchy From Database */}
                  {dataSourceRegisterList.length > 1 ? (
                    <div className="relative">
                      <button
                        className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center justify-between whitespace-nowrap`}
                        onMouseEnter={() => setShowDataSourceSubmenu('database')}
                        onMouseLeave={() => setShowDataSourceSubmenu(null)}
                      >
                        <div className="flex items-center">
                          <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                          <span>Create Data Model Hairarchy From Database</span>
                        </div>
                        <i className="fa-solid fa-angle-right text-[10px] flex-shrink-0"></i>
                      </button>
                      {showDataSourceSubmenu === 'database' && (
                        <div
                          className={`absolute left-full top-0 ml-1 w-64 rounded-[4px] shadow-lg ${theme.mainContentSection} border z-50`}
                          onMouseEnter={() => setShowDataSourceSubmenu('database')}
                          onMouseLeave={() => setShowDataSourceSubmenu(null)}
                        >
                          <div className="py-1">
                            {dataSourceRegisterList.map((ds: any) => (
                              <button
                                key={ds.Id}
                                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                                onClick={() => handleCreateTransaction(TransactionOrganizedType.MasterDetail, false, ds.Id)}
                              >
                                <span>On {ds.DataSourceName} ({ds.Id})</span>
                              </button>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ) : (
                    <button
                      className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                      onClick={() => handleCreateTransaction(TransactionOrganizedType.MasterDetail, false, dataSourceRegisterList[0]?.Id)}
                    >
                      <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                      <span>Create Data Model Hairarchy From Database</span>
                    </button>
                  )}

                  {/* Create Data Model Hierarchy From Excel */}
                  {dataSourceRegisterList.length > 1 ? (
                    <div className="relative">
                      <button
                        className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center justify-between whitespace-nowrap`}
                        onMouseEnter={() => setShowDataSourceSubmenu('excel')}
                        onMouseLeave={() => setShowDataSourceSubmenu(null)}
                      >
                        <div className="flex items-center">
                          <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                          <span>Create Data Model Hairarchy From Excel</span>
                        </div>
                        <i className="fa-solid fa-angle-right text-[10px] flex-shrink-0"></i>
                      </button>
                      {showDataSourceSubmenu === 'excel' && (
                        <div
                          className={`absolute left-full top-0 ml-1 w-64 rounded-[4px] shadow-lg ${theme.mainContentSection} border z-50`}
                          onMouseEnter={() => setShowDataSourceSubmenu('excel')}
                          onMouseLeave={() => setShowDataSourceSubmenu(null)}
                        >
                          <div className="py-1">
                            {dataSourceRegisterList.map((ds: any) => (
                              <button
                                key={ds.Id}
                                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                                onClick={() => handleCreateFromExcel(ds.Id)}
                              >
                                <span>On {ds.DataSourceName} ({ds.Id})</span>
                              </button>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ) : (
                    <button
                      className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                      onClick={() => handleCreateFromExcel(dataSourceRegisterList[0]?.Id)}
                    >
                      <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                      <span>Create Data Model Hairarchy From Excel</span>
                    </button>
                  )}

                  {/* Create Data Model From Form (Domain Model) */}
                  <button
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={handleCreateFormFromScratch}
                  >
                    <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                    <span>Create Data Model From Form (Domain Model)</span>
                  </button>

                  {/* Create Temp Data Transfer Object */}
                  <button
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => handleCreateTransaction(TransactionOrganizedType.MasterDetail, true)}
                  >
                    <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                    <span>Create Temp Data Transfer Object</span>
                  </button>

                  {/* Create Data Model Hierarchy From Rest API */}
                  <button
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => handleCreateTransaction(TransactionOrganizedType.MasterDetail, true, undefined, true)}
                  >
                    <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                    <span>Create Data Model Hairarchy From Rest API</span>
                  </button>

                  {/* Create Hierarchy Data Model View */}
                  {dataSourceRegisterList.length > 1 ? (
                    <div className="relative">
                      <button
                        className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center justify-between whitespace-nowrap`}
                        onMouseEnter={() => setShowDataSourceSubmenu('view')}
                        onMouseLeave={() => setShowDataSourceSubmenu(null)}
                      >
                        <div className="flex items-center">
                          <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                          <span>Create Hairarchy Data Model View</span>
                        </div>
                        <i className="fa-solid fa-angle-right text-[10px] flex-shrink-0"></i>
                      </button>
                      {showDataSourceSubmenu === 'view' && (
                        <div
                          className={`absolute left-full top-0 ml-1 w-64 rounded-[4px] shadow-lg ${theme.mainContentSection} border z-50`}
                          onMouseEnter={() => setShowDataSourceSubmenu('view')}
                          onMouseLeave={() => setShowDataSourceSubmenu(null)}
                        >
                          <div className="py-1">
                            {dataSourceRegisterList.map((ds: any) => (
                              <button
                                key={ds.Id}
                                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                                onClick={() => handleCreateTransaction(TransactionOrganizedType.MasterDetail, false, ds.Id, false, true)}
                              >
                                <span>On {ds.DataSourceName} ({ds.Id})</span>
                              </button>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ) : (
                    <button
                      className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                      onClick={() => handleCreateTransaction(TransactionOrganizedType.MasterDetail, false, undefined, false, true)}
                    >
                      <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                      <span>Create Hairarchy Data Model View</span>
                    </button>
                  )}

                  {/* Divider */}
                  <div className="h-px my-1 border-t border-gray-300"></div>

                  {/* Create List Edit From Database */}
                  {dataSourceRegisterList.length > 1 ? (
                    <div className="relative">
                      <button
                        className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center justify-between whitespace-nowrap`}
                        onMouseEnter={() => setShowDataSourceSubmenu('list-db')}
                        onMouseLeave={() => setShowDataSourceSubmenu(null)}
                      >
                        <div className="flex items-center">
                          <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                          <span>Create List Edit From Database</span>
                        </div>
                        <i className="fa-solid fa-angle-right text-[10px] flex-shrink-0"></i>
                      </button>
                      {showDataSourceSubmenu === 'list-db' && (
                        <div
                          className={`absolute left-full top-0 ml-1 w-64 rounded-[4px] shadow-lg ${theme.mainContentSection} border z-50`}
                          onMouseEnter={() => setShowDataSourceSubmenu('list-db')}
                          onMouseLeave={() => setShowDataSourceSubmenu(null)}
                        >
                          <div className="py-1">
                            {dataSourceRegisterList.map((ds: any) => (
                              <button
                                key={ds.Id}
                                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                                onClick={() => handleCreateTransaction(TransactionOrganizedType.List, false, ds.Id)}
                              >
                                <span>On {ds.DataSourceName} ({ds.Id})</span>
                              </button>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ) : (
                    <button
                      className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                      onClick={() => handleCreateTransaction(TransactionOrganizedType.List)}
                    >
                      <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                      <span>Create List Edit From Database</span>
                    </button>
                  )}

                  {/* Create List Edit From Rest API */}
                  <button
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => handleCreateTransaction(TransactionOrganizedType.List, true, undefined, true)}
                  >
                    <i className="fa-solid fa-plus mr-2 flex-shrink-0"></i>
                    <span>Create List Edit From Rest API</span>
                  </button>
                </div>
              </div>
            )}
          </div>
          <button
            onClick={handleImportFromLibrary}
            className="px-3 py-1 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600 flex items-center gap-1"
            title="Import data models from library"
          >
            <i className="fa-solid fa-plus-circle"></i>
            <span>Import From Library</span>
          </button>
        </div>
      </div>

      {/* FlexGrid */}
      <div className={`w-full h-[200px] ${theme.mainContentSection} flex-auto overflow-hidden`}>
        {transactionCV && (
          <FlexGrid
            ref={flexGridRef}
            itemsSource={transactionCV}
            autoGenerateColumns={false}
            selectionMode="Row"
            isReadOnly={true}
            allowDelete={false}
            showGroups={true}
            groupHeaderFormat="<span style='font-weight:600;'>{value}</span>"
            style={{ width: '100%', height: '100%', borderLeft: 'none', borderRight: 'none', borderTop: 'none', borderBottom: 'none' }}
          >
            <FlexGridFilter />
            
            <FlexGridColumn width={60} header="Actions">
              <FlexGridCellTemplate cellType="Cell" template={transactionMenuCellTemplate} />
            </FlexGridColumn>
            
            <FlexGridColumn binding="Id" header="ID" width={100} format="d" />
            <FlexGridColumn binding="TransactionName" header="Name" width={300} />
            <FlexGridColumn binding="AppModifiedDate" header="Modified Date" width={150} dataType="Date" format="d" />
            
            <FlexGridColumn binding="DefaultNavigationSearchId" header="Default Search" width={200}>
              <FlexGridCellTemplate cellType="Cell" template={defaultSearchCellTemplate} />
            </FlexGridColumn>
            
            <FlexGridColumn binding="DefaultNavigationFolderViewId" header="Folder Tree" width={200}>
              <FlexGridCellTemplate cellType="Cell" template={folderTreeCellTemplate} />
            </FlexGridColumn>
            
            <FlexGridColumn binding="CreatedFromDisplay" header="Created From" width={150} />
            
            {dataSourceRegisterList.length > 0 && (
              <FlexGridColumn 
                binding="DataSourceFrom" 
                header="Data Source From" 
                width={200}
                dataMap={datasourceRegisterDataMap || undefined}
              />
            )}
            
            <FlexGridColumn binding="Description" header="Description" width="*" />
          </FlexGrid>
        )}
      </div>
      
      {/* Search Context Menu */}
      {showSearchMenu.visible && (
        <div
          ref={searchMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[200px]`}
          style={{
            left: showSearchMenu.x,
            top: showSearchMenu.y,
          }}
          onClick={(e) => e.stopPropagation()}
        >
          {searchList.map((search: any) => (
            <button
              key={search.Id}
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
              onClick={() => {
                // TODO: Handle search selection
                setShowSearchMenu({ visible: false, x: 0, y: 0, item: null });
              }}
            >
              <i className="fa-solid fa-magnifying-glass mr-2"></i>
              <span>{search.Display}</span>
            </button>
          ))}
        </div>
      )}
      
      {/* Folder Context Menu */}
      {showFolderMenu.visible && showFolderMenu.item && (
        <div
          ref={folderMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[240px]`}
          style={{
            left: showFolderMenu.x,
            top: showFolderMenu.y,
          }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
            onClick={() => handleEditFolderTreeView(showFolderMenu.item)}
          >
            <i className="fa-solid fa-pen-to-square" />
            <span>Edit Folder Tree View</span>
          </button>
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
            onClick={() => handleSetupFolderTreeNavigation(showFolderMenu.item)}
          >
            <i className="fa-solid fa-plus-circle" />
            <span>Setup Folder Tree View &amp; Add To App Menu</span>
          </button>
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
            onClick={() => handleOpenTransactionNavigationEditor(showFolderMenu.item)}
          >
            <i className="fa-solid fa-folder-tree" />
            <span>Transaction Navigation Editor</span>
          </button>
        </div>
      )}

      {/* Transaction Row Context Menu */}
      {showTransactionContextMenu.visible && showTransactionContextMenu.item && (
        <div
          ref={transactionContextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{
            left: showTransactionContextMenu.x,
            top: showTransactionContextMenu.y,
          }}
          onClick={(e) => e.stopPropagation()}
        >
          <div className="py-1" role="menu">
            {showTransactionContextMenu.item.Id && (
              <>
                {/* Edit */}
                <button
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                  onClick={() => handleEditTransaction(showTransactionContextMenu.item)}
                >
                  <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-edit"></i></span>
                  <span>Edit</span>
                </button>

                {/* Save As */}
                <button
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                  onClick={() => handleSaveAsTransaction(showTransactionContextMenu.item)}
                >
                  <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-copy"></i></span>
                  <span>Save As</span>
                </button>

                {/* Delete */}
                <button
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                  onClick={() => handleDeleteTransaction(showTransactionContextMenu.item)}
                >
                  <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-trash"></i></span>
                  <span>Delete</span>
                </button>
              </>
            )}

            {/* Divider - if has ImportSetting */}
            {showTransactionContextMenu.item.Id && 
             (showTransactionContextMenu.item.OtherOptions?.TransactionDataUpdateImportSettingId || 
              showTransactionContextMenu.item.OtherOptions?.ImportSettingId) && (
              <div className="h-px my-1 border-t border-gray-300"></div>
            )}

            {/* Import Setting related items */}
            {(showTransactionContextMenu.item.OtherOptions?.TransactionDataUpdateImportSettingId || 
              showTransactionContextMenu.item.OtherOptions?.ImportSettingId) && (
              <>
                {/* Open Import Setting */}
                <button
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                  onClick={() => handleViewImportSetting(showTransactionContextMenu.item)}
                >
                  <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-edit"></i></span>
                  <span>
                    Open Import Setting
                    {showTransactionContextMenu.item.OtherOptions?.IsDraft && ' (Draft)'}
                  </span>
                </button>

                {/* Update Data From Excel */}
                {!showTransactionContextMenu.item.OtherOptions?.IsDraft && 
                 showTransactionContextMenu.item.OtherOptions?.TransactionDataUpdateImportSettingId && (
                  <button
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                    onClick={() => handleUpdateDataFromExcel(showTransactionContextMenu.item)}
                  >
                    <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-file-excel"></i></span>
                    <span>Update Data Model Data From Excel</span>
                  </button>
                )}

                {/* Delete Import Setting */}
                <button
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                  onClick={() => handleDeleteImportSetting(showTransactionContextMenu.item)}
                >
                  <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-trash"></i></span>
                  <span>Delete Import Setting</span>
                </button>
              </>
            )}

            {/* Update Data Model Data From Excel - for transactions without import setting */}
            {showTransactionContextMenu.item.Id && 
             (showTransactionContextMenu.item.TransactionOrganizedType === TransactionOrganizedType.MasterDetail || 
              showTransactionContextMenu.item.TransactionOrganizedType === 8) && 
             !showTransactionContextMenu.item.OtherOptions?.IsApiIntegrationTransaction && 
             !(showTransactionContextMenu.item.OtherOptions?.TransactionDataUpdateImportSettingId || 
               showTransactionContextMenu.item.OtherOptions?.ImportSettingId) && (
              <button
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                onClick={() => handleCreateImportSettingForTransaction(showTransactionContextMenu.item)}
              >
                <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-file-excel"></i></span>
                <span>Update Data Model Data From Excel</span>
              </button>
            )}

            {/* Divider - before API mapping */}
            {showTransactionContextMenu.item.Id && 
             !(showTransactionContextMenu.item.OtherOptions?.ImportSettingId && 
               showTransactionContextMenu.item.OtherOptions?.IsDraft) && (
              <div className="h-px my-1 border-t border-gray-300"></div>
            )}

            {/* API Mapping */}
            {showTransactionContextMenu.item.Id && 
             !(showTransactionContextMenu.item.OtherOptions?.ImportSettingId && 
               showTransactionContextMenu.item.OtherOptions?.IsDraft) && (
              <>
                {/* Consume API Mapping */}
                <button
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                  onClick={() => handleEditConsumeAPIs(showTransactionContextMenu.item)}
                >
                  <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-edit"></i></span>
                  <span>Consume API Mapping - Data Model Consumes APIs From 3rd Part</span>
                </button>

                {/* Provide API Mapping */}
                <button
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                  onClick={() => handleEditProvideAPIs(showTransactionContextMenu.item)}
                >
                  <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-edit"></i></span>
                  <span>Provide API Mapping - Data Model Provides APIs To 3rd Part</span>
                </button>
              </>
            )}

            {/* Divider - before Add To App Menu */}
            {showTransactionContextMenu.item.Id && 
             (showTransactionContextMenu.item.TransactionOrganizedType === TransactionOrganizedType.MasterDetail || 
              showTransactionContextMenu.item.TransactionOrganizedType === 8) && 
             !showTransactionContextMenu.item.OtherOptions?.IsApiIntegrationTransaction && (
              <div className="h-px my-1 border-t border-gray-300"></div>
            )}

            {/* Add To App Menu */}
            {showTransactionContextMenu.item.Id && 
             (showTransactionContextMenu.item.TransactionOrganizedType === TransactionOrganizedType.MasterDetail || 
              showTransactionContextMenu.item.TransactionOrganizedType === 8) && 
             !showTransactionContextMenu.item.OtherOptions?.IsApiIntegrationTransaction && (
              <button
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
                onClick={() => handleAddToAppMenu(showTransactionContextMenu.item)}
              >
                <span className="inline-flex w-5 flex-shrink-0 items-center justify-center" aria-hidden><i className="fa-solid fa-bars"></i></span>
                <span>Add To App Menu</span>
              </button>
            )}
          </div>
        </div>
      )}

      {/* ApplicationFormBuilder Popup */}
      {showFolderQuickBuilder && folderQuickBuilderTransactionId != null && (
        <TransactionFolderNavigationQuickBuilder
          isOpen={showFolderQuickBuilder}
          onClose={() => {
            setShowFolderQuickBuilder(false);
            setFolderQuickBuilderTransactionId(null);
          }}
          onSaved={reloadTransactionGrid}
          transactionId={folderQuickBuilderTransactionId}
        />
      )}

      {showFormBuilder && formBuilderParams && (
        <ApplicationFormBuilder
          isOpen={showFormBuilder}
          onClose={() => {
            setShowFormBuilder(false);
            setFormBuilderParams(null);
          }}
          onRefresh={handleRefresh}
          applicationId={menuId}
          defaultSectionCode={formBuilderParams.defaultSectionCode}
          isCreateNewItem={formBuilderParams.isCreateNewItem}
          transactionId={formBuilderParams.transactionId || null}
          transactionType={formBuilderParams.transactionType}
          dataSourceRegisterId={formBuilderParams.dataSourceRegisterId}
          isCreateDtoDataModel={formBuilderParams.isCreateDtoDataModel}
          isCreateApiDataModel={formBuilderParams.isCreateApiDataModel}
          isCreateDataModelView={formBuilderParams.isCreateDataModelView}
          modelName={formBuilderParams.modelName}
        />
      )}
    </div>
  );
};

export default DataModelDesign;

