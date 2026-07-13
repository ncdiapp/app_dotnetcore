import React, { useState, useEffect, useRef, useMemo } from 'react';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { dynamicLayoutService } from '../../../webapi/dynamiclayoutsvc';
import MasterDetailEditLayoutForm from '../../formMgt/FormMasterDetail/MasterDetailEditLayoutForm';
import FormListEdit from '../../formMgt/FormListEdit';

interface TransactionFormPreviewProps {
  isOpen: boolean;
  onClose: () => void;
  transactionId: number;
  transactionType: number;
  transactionName: string;
}

const TransactionFormPreview: React.FC<TransactionFormPreviewProps> = ({
  isOpen,
  onClose,
  transactionId,
  transactionType,
  transactionName
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError } = useErrorMessage();
  const [isFullscreen, setIsFullscreen] = useState<boolean>(false);

  // Controller model state
  const [controllerModel] = useState<any>({
    transactionId: transactionId,
    rootPrimaryKeyValue: null, // Preview mode - no root primary key
    formRequestMode: 'New',
    uiId: `form_preview_${Date.now()}`,
    isEnableFormConfigButtons: false,
    isAutoReopenCurrentFormAfterChange: false,
    param2Obj: { isPreview: true },
    isPreview: true,
    isConfigTestRun: true,
    isFormImportTemplate: false,
    isFilePropertyEdit: false,
    opennedFormAutoExecuteCommandId: null,
    isTemplateHeader: false,
    TemplateHeaderName: '',
    isNeedExecuteDataloadOnOpenNewForm: false,
    isNeedPreSaveNewFormData: false,
  });

  // Data model state
  const [dataModel, setDataModel] = useState<any>({
    rootIdInCache: null,
    doing_async: false,
    currentFormData: null,
    currentFormStructure: null,
    isTransactionForm: true,
    isHideCilldFormAdvancedMenus: true,
    dictChildTransactionUnitIdDataSource: {},
    dictGrandChildTransactionUnitIdDataSource: {},
    dictFieldEntityDataSource: {},
    dictFieldEntityDataMap: {},
    dictChangedUnitId: {},
    isLoading: true,
  });

  // Form structure data ref
  const formStructureDataRef = useRef<any>(null);
  const transactionExDtoRef = useRef<any>(null);

  // Load dynamic layout from server
  const loadDynamicLayout = async (): Promise<any> => {
    if (!transactionId) return null;

    try {
      const transactionExDto = await dynamicLayoutService.getTransactionForm(
        transactionId,
        undefined, // transGroupId
        undefined, // rootPkIdValue
        undefined, // isPrint
        undefined, // opennedFormAutoExecuteCommandId
        'true' // isPreview
      );

      return transactionExDto;
    } catch (error) {
      console.error('Error loading dynamic layout:', error);
      showError((error as Error).message);
      return null;
    }
  };

  // Load form structure from server
  const loadFormStructure = async (): Promise<any> => {
    if (!transactionId) return null;

    try {
      const formStructure = await appTransactionService.getFormStructure(transactionId);
      return formStructure;
    } catch (error) {
      console.error('Error loading form structure:', error);
      showError((error as Error).message);
      return null;
    }
  };

  // Prepare new form data (for preview)
  const prepareNewFormData = (formStructureData: any) => {
    if (!formStructureData) return;

    const newFormData: any = {
      IsDirty: false,
      IsShowSaveButton: false, // Preview mode - hide save button
      DictOneToOneFields: {},
      DictOneToManyFields: {},
      DictSiblingOneToOneFields: {},
      RootUnitId: formStructureData.RootUnitId,
    };

    setDataModel((prev: any) => ({
      ...prev,
      currentFormData: newFormData,
      doing_async: false,
      isLoading: false
    }));
  };

  // Main data loading function
  const loadDataFromServer = async () => {
    if (!transactionId) return;

    try {
      dispatch(setIsBusy());
      setDataModel((prev: any) => ({ ...prev, isLoading: true }));

      // Load dynamic layout
      const transactionExDto = await loadDynamicLayout();
      if (transactionExDto) {
        transactionExDtoRef.current = transactionExDto;
      }

      // Load form structure
      const formStructureData = await loadFormStructure();

      if (formStructureData && transactionExDto) {
        // Merge the form structure data with dynamic layout data
        const mergedFormStructure = {
          ...formStructureData,
          TransactionOrganizedType: transactionExDto.TransactionOrganizedType || formStructureData.TransactionOrganizedType,
          ForeignAppFormExDto: transactionExDto.ForeignAppFormExDto || formStructureData.ForeignAppFormExDto,
          TransactionId: transactionExDto.Id || formStructureData.TransactionId,
          TransactionName: transactionExDto.TransactionName || formStructureData.TransactionName,
          RootUnitId: transactionExDto.RootUnitId || formStructureData.RootUnitId,
          IsAllowAccess: transactionExDto.IsAllowAccess,
        };

        formStructureDataRef.current = mergedFormStructure;
        setDataModel((prev: any) => ({
          ...prev,
          currentFormStructure: mergedFormStructure
        }));

        // Prepare new form data for preview
        prepareNewFormData(mergedFormStructure);
      } else if (formStructureData) {
        // Fallback if no transactionExDto
        formStructureDataRef.current = formStructureData;
        setDataModel((prev: any) => ({
          ...prev,
          currentFormStructure: formStructureData
        }));
        prepareNewFormData(formStructureData);
      }
    } catch (error) {
      console.error('Error loading data from server:', error);
      showError((error as Error).message);
    } finally {
      dispatch(setIsNotBusy());
      setDataModel((prev: any) => ({ ...prev, isLoading: false }));
    }
  };

  // Load data when component opens
  useEffect(() => {
    if (isOpen && transactionId) {
      loadDataFromServer();
    }
  }, [isOpen, transactionId]);

  // Check if access is allowed
  const isAllowAccess = useMemo(() => {
    return transactionExDtoRef.current?.IsAllowAccess ?? 
           dataModel.currentFormStructure?.IsAllowAccess ?? 
           true;
  }, [dataModel.currentFormStructure]);

  // Determine if we have form structure data
  const hasFormStructure = !!(dataModel.currentFormStructure || formStructureDataRef.current);

  const toggleFullscreen = () => {
    setIsFullscreen(!isFullscreen);
  };

  if (!isOpen) return null;

  const overlayZIndex = 10500; // Above Form Design INSERT ROW/border (200-300) and context menus (10000)

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center"
      style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, zIndex: overlayZIndex }}
      onClick={(e) => {
        if (e.target === e.currentTarget) {
          onClose();
        }
      }}
    >
      <div
        className={`${theme.mainContentSection} shadow-xl border flex flex-col overflow-hidden ${isFullscreen ? 'rounded-none' : 'rounded-lg'}`}
        style={isFullscreen
          ? { width: '100vw', height: '100vh', position: 'fixed', top: 0, left: 0, zIndex: overlayZIndex + 1 }
          : { width: '90vw', height: '90vh', maxWidth: '1400px', maxHeight: '800px' }
        }
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection} ${isFullscreen ? 'rounded-none' : 'rounded-t-lg'}`}>
          <h3 className={`text-lg font-semibold ${theme.title}`}>
            Form Preview: {transactionName}
          </h3>
          <div className="flex items-center gap-2">
            <button
              onClick={toggleFullscreen}
              className="text-xs hover:text-gray-600 px-0.5 w-5 h-5 flex items-center justify-center"
              title={isFullscreen ? "Exit Fullscreen" : "Fullscreen"}
            >
              <i className={`fa-solid ${isFullscreen ? 'fa-compress' : 'fa-expand'}`}></i>
            </button>
            <button
              onClick={onClose}
              className="text-2xl hover:text-gray-600 px-2 w-9 h-9 flex items-center justify-center"
              title="Close"
            >
              &times;
            </button>
          </div>
        </div>

        {/* Body - Form content directly rendered (similar to TableDataPreview structure) */}
        <div className="h-1 flex-auto overflow-hidden">
          {transactionType === 1 ? (
            <div className={`w-full h-full flex ${theme.default}`} style={{ position: 'relative' }}>
              {/* Show loading state */}
              {dataModel.isLoading && (
                <div className="flex items-center justify-center h-full w-full">
                  <div className="text-gray-500">Loading form data...</div>
                </div>
              )}

              {/* Show access denied */}
              {!isAllowAccess && !dataModel.isLoading && (
                <div className="flex items-center justify-center h-full w-full">
                  <div className="text-red-500 p-5">
                    Data Model "{dataModel.currentFormStructure?.TransactionName || 'Unknown'}": Access Denied
                  </div>
                </div>
              )}

              {/* Show form when data is loaded */}
              {!dataModel.isLoading && isAllowAccess && hasFormStructure && (
                <div className="flex-auto h-full overflow-auto w-full">
                  <MasterDetailEditLayoutForm
                    controllerModel={controllerModel}
                    dataModel={dataModel}
                    formStructureData={formStructureDataRef.current}
                    transactionExDto={transactionExDtoRef.current}
                    onDataModelChange={setDataModel}
                  />
                </div>
              )}

              {/* Show message when no form structure and not loading */}
              {!dataModel.isLoading && isAllowAccess && !hasFormStructure && !dataModel.doing_async && (
                <div className="flex items-center justify-center h-full w-full">
                  <div className="text-gray-500">No form structure available.</div>
                </div>
              )}
            </div>
          ) : transactionType === 3 ? (
            <div className={`w-full h-full flex ${theme.default}`}>
              <FormListEdit
                embedded={{
                  embeddedTransactionId: transactionId,
                }}
              />
            </div>
          ) : (
            <div className="w-full h-full flex items-center justify-center">
              <div className={`text-lg ${theme.label}`}>
                Form Folder List Edit preview will be implemented here
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default TransactionFormPreview;
