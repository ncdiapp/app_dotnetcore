import React, { useState, useEffect } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import OneLayoutRow from './MasterDetailFlexLayoutForm/OneLayoutRow';
import WorkflowBatchLogSearch from './WorkflowBatchLogSearch';
import { useFormMasterDetailRuntimeConfig } from './formMasterDetailRuntimeConfig';

interface MasterDetailFlexLayoutFormProps {
  controllerModel: any;
  dataModel: any;
  formStructureData: any;
  transactionExDto?: any;
  onDataModelChange: (dataModel: any) => void;
}

const MasterDetailFlexLayoutForm: React.FC<MasterDetailFlexLayoutFormProps> = ({
  controllerModel,
  dataModel,
  formStructureData,
  transactionExDto,
  onDataModelChange
}) => {
  const { theme } = useTheme();
  const runtimeFieldConfig = useFormMasterDetailRuntimeConfig();
  const _layoutTypeEnum = useEnumValues('EmAppFormLayoutType');
  const transactionScopeEnum = useEnumValues('EmAppTransactionScopeUsage');
  const [isLoading, setIsLoading] = useState(true);

  // Get form DTO from transactionExDto or formStructureData
  // Priority: transactionExDto.ForeignAppFormExDto > formStructureData.ForeignAppFormExDto > formStructureData
  const formDto = transactionExDto?.ForeignAppFormExDto 
    || formStructureData?.ForeignAppFormExDto 
    || (formStructureData?.LayoutType ? formStructureData : null);
  
  // Check if data is still loading
  useEffect(() => {
    // If we have transactionExDto or formStructureData, we're not loading
    if (transactionExDto || formStructureData) {
      setIsLoading(false);
    } else {
      setIsLoading(true);
    }
  }, [transactionExDto, formStructureData]);
  
  // Get root transaction unit
  const rootUnit = transactionExDto?.AppTransactionUnitList?.[0] || formStructureData?.AppTransactionUnitList?.[0];
  
  // Check if workflow automation
  const isWorkflow = transactionExDto?.BusinessScopeId === transactionScopeEnum?.WorkflowAutomation;
  
  // Get form width (default 800px)
  let formWidth = 800;
  if (formDto?.DefaultWidth) {
    const parsedWidth = parseInt(formDto.DefaultWidth.trim());
    if (!isNaN(parsedWidth)) {
      formWidth = parsedWidth;
    }
  }
  
  // Get layout items list, ordered by FlowOrGridLayoutSortOrder
  // Try multiple possible locations for layout items
  let layoutItems: any[] = [];
  
  // Check formDto first (most common location)
  if (formDto?.AppFormLayoutItemList && Array.isArray(formDto.AppFormLayoutItemList)) {
    layoutItems = [...formDto.AppFormLayoutItemList];
  } 
  // Check formStructureData
  else if (formStructureData?.AppFormLayoutItemList && Array.isArray(formStructureData.AppFormLayoutItemList)) {
    layoutItems = [...formStructureData.AppFormLayoutItemList];
  } 
  // Check transactionExDto directly
  else if (transactionExDto?.ForeignAppFormExDto?.AppFormLayoutItemList && Array.isArray(transactionExDto.ForeignAppFormExDto.AppFormLayoutItemList)) {
    layoutItems = [...transactionExDto.ForeignAppFormExDto.AppFormLayoutItemList];
  }
  // Check if formDto itself is an array (edge case)
  else if (Array.isArray(formDto)) {
    layoutItems = [...formDto];
  }
  
  // Filter out null/undefined items and sort by FlowOrGridLayoutSortOrder
  layoutItems = layoutItems.filter(item => item != null);
  
  if (layoutItems.length > 0) {
    layoutItems.sort((a: any, b: any) => 
      (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
    );
  }
  
  // Get grand child edit mode
  const grandChildEditMode = transactionExDto?.EmGrandChildEditMode || 1; // Default to SubGrid

  // Debug logging
  if (process.env.NODE_ENV === 'development') {
    console.log('MasterDetailFlexLayoutForm Debug:', {
      hasTransactionExDto: !!transactionExDto,
      hasFormStructureData: !!formStructureData,
      hasFormDto: !!formDto,
      formDtoKeys: formDto ? Object.keys(formDto) : [],
      layoutItemsCount: layoutItems.length,
      formDtoAppFormLayoutItemList: formDto?.AppFormLayoutItemList ? `Array(${formDto.AppFormLayoutItemList.length})` : 'not found',
      formStructureDataAppFormLayoutItemList: formStructureData?.AppFormLayoutItemList ? `Array(${formStructureData.AppFormLayoutItemList.length})` : 'not found',
      transactionExDtoForeignAppFormExDto: transactionExDto?.ForeignAppFormExDto ? 'exists' : 'not found',
      transactionExDtoKeys: transactionExDto ? Object.keys(transactionExDto) : [],
      formStructureDataKeys: formStructureData ? Object.keys(formStructureData) : []
    });
    
    // Log full formDto structure if no layout items found
    if (layoutItems.length === 0 && formDto) {
      console.log('FormDto full structure:', JSON.stringify(formDto, null, 2));
    }
  }

  // Show loading state
  if (isLoading) {
    return (
      <div className="w-full h-full p-4 text-gray-500">
        Loading form layout...
      </div>
    );
  }

  if (!formDto) {
    return (
      <div className="w-full h-full p-4 text-gray-500">
        <div>No form DTO available.</div>
        <div className="text-xs mt-2">
          Debug: transactionExDto={transactionExDto ? 'exists' : 'null'}, 
          formStructureData={formStructureData ? 'exists' : 'null'}
        </div>
      </div>
    );
  }

  if (!layoutItems || layoutItems.length === 0) {
    return (
      <div className="w-full h-full p-4 text-gray-500">
        <div>No layout items available.</div>
        <div className="text-xs mt-2">
          <div>Form DTO exists: {formDto ? 'Yes' : 'No'}</div>
          <div>Layout items count: {layoutItems.length}</div>
          <div>Form DTO keys: {formDto ? Object.keys(formDto).join(', ') : 'N/A'}</div>
          {formDto && (
            <details className="mt-2">
              <summary className="cursor-pointer text-blue-500">View Form DTO</summary>
              <pre className="text-xs mt-1 p-2 bg-gray-100 dark:bg-gray-800 overflow-auto max-h-64">
                {JSON.stringify(formDto, null, 2)}
              </pre>
            </details>
          )}
        </div>
      </div>
    );
  }

  return (
    <>
    <div
      className="FormFlex relative flex w-full min-w-0 flex-row items-start gap-1"
      style={{
        width: `${formWidth}px`,
        maxWidth: '100%',
        position: 'relative',
      }}
    >
      <div className="w-1 min-w-0 flex-auto">
        {layoutItems.map((layoutRowExDto: any, index: number) => {
          const rowWithMode = {
            ...layoutRowExDto,
            GrandChildEditMode: grandChildEditMode,
          };

          return (
            <OneLayoutRow
              key={layoutRowExDto.Id || `row-${index}`}
              layoutRowExDto={rowWithMode}
              controllerModel={controllerModel}
              dataModel={dataModel}
              onDataModelChange={onDataModelChange}
              transactionExDto={transactionExDto}
              RowComponent={OneLayoutRow}
            />
          );
        })}
      </div>

      {controllerModel.isEnableFormConfigButtons && (
        <button
          type="button"
          className={`flex h-7 w-7 shrink-0 items-center justify-center self-start rounded-[4px] text-xs ${theme.button_default}`}
          title={
            rootUnit && transactionExDto?.AppTransactionUnitList?.length === 1
              ? 'Edit Root Unit'
              : 'Edit Root Level Units'
          }
          onClick={(e) => {
            const tx = transactionExDto || formStructureData;
            const list = tx?.AppTransactionUnitList || [];
            if (list.length === 1) {
              runtimeFieldConfig.openTransactionUnitEditor(e, Number(list[0].Id));
            } else if (list.length > 1) {
              runtimeFieldConfig.openRootUnitsContextMenu(e);
            }
          }}
        >
          <i className="fa-solid fa-gear" aria-hidden />
        </button>
      )}
      
    </div>

    {isWorkflow ? <WorkflowBatchLogSearch controllerModel={controllerModel} /> : null}
    </>
  );
};

export default MasterDetailFlexLayoutForm;
