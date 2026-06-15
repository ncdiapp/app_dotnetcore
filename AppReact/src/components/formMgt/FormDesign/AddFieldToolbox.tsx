import React, { useState, useMemo } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import appHelper from '../../../helper/appHelper';

interface AddFieldToolboxProps {
  formData: any;
  transactionData: any;
  onAddLayoutItem: (itemType: number, transactionFieldId?: number, gridTransactionUnitId?: number, commandActionId?: number, linkedSearchId?: number) => void;
}

const AddFieldToolbox: React.FC<AddFieldToolboxProps> = ({
  formData,
  transactionData,
  onAddLayoutItem
}) => {
  const { theme } = useTheme();
  const [isUICollapsed, setIsUICollapsed] = useState<boolean>(false);
  const [isDataModelCollapsed, setIsDataModelCollapsed] = useState<boolean>(false);
  const [fieldFilterText, setFieldFilterText] = useState<string>('');
  const [showPrivateCommands, setShowPrivateCommands] = useState<boolean>(false);

  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');
  const isPhysicalModelTableCreated = formData?.IsPhysicalModelTableCreated;
  const isApiIntegrationTransaction = formData?.IsApiIntegrationTransaction;
  const layoutButtonIconClass = 'fa fa-fw text-[12px]';

  // Use processed data from transactionData if available, otherwise fallback to raw data
  const rootLevelUnitFieldList = transactionData?.rootLevelUnitFieldList || 
    transactionData?.AppTransactionData?.AppTransactionUnitList?.[0]?.AppTransactionFieldList || [];
  const childLevelUnitList = transactionData?.childLevelUnitList || 
    transactionData?.AppTransactionData?.AppTransactionUnitList?.[0]?.Children || [];

  // Filter fields based on filter text
  const filteredFields = useMemo(() => {
    if (!rootLevelUnitFieldList || rootLevelUnitFieldList.length === 0) {
      return [];
    }
    if (!fieldFilterText) return rootLevelUnitFieldList;
    const filter = fieldFilterText.toLowerCase();
    return rootLevelUnitFieldList.filter((field: any) => 
      (field.DisplayName || '').toLowerCase().includes(filter) ||
      (field.DataBaseFieldName || '').toLowerCase().includes(filter)
    );
  }, [rootLevelUnitFieldList, fieldFilterText]);
  const commandActionList = transactionData?.AppTransactionData?.CommandActionList || [];
  const linkedSearchList = useMemo(() => {
    if (!transactionData?.AppTransactionData?.AppTransactionUnitList) return [];
    const searches: any[] = [];
    transactionData.AppTransactionData.AppTransactionUnitList.forEach((unit: any) => {
      if (unit.AppTransactionUnitLinkedSearchList) {
        searches.push(...unit.AppTransactionUnitLinkedSearchList);
      }
    });
    return searches;
  }, [transactionData]);

  return (
    <>
      {/* Add UI Element section - always show */}
      <div className="w-full mb-4">
        <div 
          className={`flex items-center justify-between px-2 py-2 cursor-pointer ${theme.mainContentSection} border rounded-t`}
          onClick={() => setIsUICollapsed(!isUICollapsed)}
        >
          <span className={`text-sm font-semibold ${theme.title}`}>Add UI Element</span>
          <i 
            className={`fa ${isUICollapsed ? 'fa-chevron-circle-down' : 'fa-chevron-circle-up'} text-gray-500`}
          ></i>
        </div>
        
        {!isUICollapsed && (
          <div className={`p-3 border-l border-r border-b rounded-b ${theme.mainContentSection}`}>
            {/* Layout Element */}
            <div className="mb-4">
              <div className={`text-xs font-semibold ${theme.title} mb-2`}>Layout Element</div>
              <div className="grid grid-cols-2 gap-2">
                <button
                  className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1 cursor-move`}
                  draggable
                  onDragStart={(e) => {
                    e.dataTransfer.effectAllowed = 'copyMove';
                    const dragData = {
                      type: layoutItemTypeEnum?.TableContainer || 0
                    };
                    e.dataTransfer.setData('text/plain', JSON.stringify(dragData));
                    const buttonElement = e.currentTarget as HTMLElement;
                    buttonElement.setAttribute('data-drag-type', (layoutItemTypeEnum?.TableContainer || 0).toString());
                  }}
                  onClick={() => layoutItemTypeEnum?.TableContainer && onAddLayoutItem(layoutItemTypeEnum.TableContainer)}
                  title="Table Container (Drag to add)"
                >
                  <i className={`${layoutButtonIconClass} fa-table`}></i>
                  <span>Table Container</span>
                </button>
                <button
                  className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1 cursor-move`}
                  draggable
                  onDragStart={(e) => {
                    e.dataTransfer.effectAllowed = 'copyMove';
                    const dragData = {
                      type: layoutItemTypeEnum?.TabContainer || 0
                    };
                    e.dataTransfer.setData('text/plain', JSON.stringify(dragData));
                    const buttonElement = e.currentTarget as HTMLElement;
                    buttonElement.setAttribute('data-drag-type', (layoutItemTypeEnum?.TabContainer || 0).toString());
                  }}
                  onClick={() => layoutItemTypeEnum?.TabContainer && onAddLayoutItem(layoutItemTypeEnum.TabContainer)}
                  title="Tab Container (Drag to add)"
                >
                  <i className={`${layoutButtonIconClass} fa-clone`}></i>
                  <span>Tab Container</span>
                </button>
                <button
                  className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1 cursor-move`}
                  draggable
                  onDragStart={(e) => {
                    e.dataTransfer.effectAllowed = 'copyMove';
                    const dragData = {
                      type: layoutItemTypeEnum?.Section || 0
                    };
                    e.dataTransfer.setData('text/plain', JSON.stringify(dragData));
                    const buttonElement = e.currentTarget as HTMLElement;
                    buttonElement.setAttribute('data-drag-type', (layoutItemTypeEnum?.Section || 0).toString());
                  }}
                  onClick={() => layoutItemTypeEnum?.Section && onAddLayoutItem(layoutItemTypeEnum.Section)}
                  title="Stack Container (Drag to add)"
                >
                  <i className={`${layoutButtonIconClass} fa-layer-group`}></i>
                  <span className="truncate overflow-hidden whitespace-nowrap">Stack Container</span>
                </button>
                <button
                  className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1 cursor-move`}
                  draggable
                  onDragStart={(e) => {
                    e.dataTransfer.effectAllowed = 'copyMove';
                    const dragData = {
                      type: layoutItemTypeEnum?.Space || 0
                    };
                    e.dataTransfer.setData('text/plain', JSON.stringify(dragData));
                    const buttonElement = e.currentTarget as HTMLElement;
                    buttonElement.setAttribute('data-drag-type', (layoutItemTypeEnum?.Space || 0).toString());
                  }}
                  onClick={() => layoutItemTypeEnum?.Space && onAddLayoutItem(layoutItemTypeEnum.Space)}
                  title="Space (Drag to add)"
                >
                  <i className={`${layoutButtonIconClass} fa-arrows-h`}></i>
                  <span>Space</span>
                </button>
                <button
                  className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1 cursor-move`}
                  draggable
                  onDragStart={(e) => {
                    e.dataTransfer.effectAllowed = 'copyMove';
                    const dragData = {
                      type: layoutItemTypeEnum?.Content || 0
                    };
                    e.dataTransfer.setData('text/plain', JSON.stringify(dragData));
                    const buttonElement = e.currentTarget as HTMLElement;
                    buttonElement.setAttribute('data-drag-type', (layoutItemTypeEnum?.Content || 0).toString());
                  }}
                  onClick={() => layoutItemTypeEnum?.Content && onAddLayoutItem(layoutItemTypeEnum.Content)}
                  title="Literal Content (Drag to add)"
                >
                  <span className={`${layoutButtonIconClass} inline-flex items-center justify-center`} style={{ fontWeight: 'bold', fontFamily: 'serif' }}>T</span>
                  <span>Literal Content</span>
                </button>
              </div>
            </div>

            {/* Input Element - only show when form is not published */}
            {!isPhysicalModelTableCreated && !isApiIntegrationTransaction && (
              <div>
                <div className={`text-xs font-semibold ${theme.title} mb-2`}>Input Element</div>
                <div className="grid grid-cols-2 gap-2">
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.TextBox && onAddLayoutItem(layoutItemTypeEnum.TextBox)}
                    title="Textbox"
                  >
                    <i className="fa fa-font"></i>
                    <span>Textbox</span>
                  </button>
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.Memo && onAddLayoutItem(layoutItemTypeEnum.Memo)}
                    title="Textarea"
                  >
                    <i className="fa fa-font"></i>
                    <span>Textarea</span>
                  </button>
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.DDL && onAddLayoutItem(layoutItemTypeEnum.DDL)}
                    title="Dropdown List"
                  >
                    <i className="fa fa-caret-square-o-down"></i>
                    <span>Dropdown List</span>
                  </button>
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.Label && onAddLayoutItem(layoutItemTypeEnum.Label)}
                    title="Label"
                  >
                    <i className="fa fa-font"></i>
                    <span>Label</span>
                  </button>
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.Integer && onAddLayoutItem(layoutItemTypeEnum.Integer)}
                    title="Integer"
                  >
                    <i className="fa fa-superscript"></i>
                    <span>Integer</span>
                  </button>
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.Numeric && onAddLayoutItem(layoutItemTypeEnum.Numeric)}
                    title="Decimal"
                  >
                    <i className="fa fa-superscript"></i>
                    <span>Decimal</span>
                  </button>
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.Date && onAddLayoutItem(layoutItemTypeEnum.Date)}
                    title="Date"
                  >
                    <i className="fa fa-calendar"></i>
                    <span>Date</span>
                  </button>
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.CheckBox && onAddLayoutItem(layoutItemTypeEnum.CheckBox)}
                    title="Checkbox"
                  >
                    <i className="fa fa-check"></i>
                    <span>Checkbox</span>
                  </button>
                  <button
                    className={`px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.Grid && onAddLayoutItem(layoutItemTypeEnum.Grid)}
                    title="Data Grid"
                  >
                    <i className="fa fa-table"></i>
                    <span>Data Grid</span>
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Add Data Model Field section (shown when form is published) */}
      {(isPhysicalModelTableCreated || isApiIntegrationTransaction) && (
        <div className="w-full mb-4">
      <div 
        className={`flex items-center justify-between px-2 py-2 cursor-pointer ${theme.mainContentSection} border rounded-t`}
        onClick={() => setIsDataModelCollapsed(!isDataModelCollapsed)}
      >
        <span className={`text-sm font-semibold ${theme.title}`}>Add Data Model Field</span>
        <i 
          className={`fa ${isDataModelCollapsed ? 'fa-chevron-circle-down' : 'fa-chevron-circle-up'} text-gray-500`}
        ></i>
      </div>
      
      {!isDataModelCollapsed && (
        <div className={`p-3 border-l border-r border-b rounded-b ${theme.mainContentSection}`}>
          {/* Standard Field */}
          {rootLevelUnitFieldList.length > 0 && (
            <div className="mb-4">
              <div className={`text-xs font-semibold ${theme.title} mb-1`}>Standard Field</div>
              {transactionData?.AppTransactionData?.AppTransactionUnitList?.[0]?.DataBaseTableName && (
                <div className={`text-xs text-gray-500 mb-2`}>
                  From table: {transactionData.AppTransactionData.AppTransactionUnitList[0].DataBaseTableName}
                </div>
              )}
              
              {/* Field Filter */}
              <div className="relative mb-2">
                <input
                  type="text"
                  className={`w-full px-2 py-1 pl-6 text-xs border rounded ${theme.inputBox}`}
                  placeholder="Field Filter"
                  value={fieldFilterText}
                  onChange={(e) => setFieldFilterText(e.target.value)}
                />
                <i className="fa fa-filter absolute left-2 top-2 text-gray-400 text-xs"></i>
              </div>

              {/* Field List */}
              <div className="grid grid-cols-2 gap-2 max-h-60 overflow-y-auto">
                {filteredFields.map((field: any) => (
                  <button
                    key={field.Id}
                    className={`px-2 py-1.5 text-xs border rounded ${theme.button_default} hover:shadow-sm text-left truncate cursor-move`}
                    draggable
                    onDragStart={(e) => {
                      appHelper.debugLog('Drag start on field:', field.DisplayName || field.DataBaseFieldName, 'Field ID:', field.Id, 'ControlType:', field.ControlType);
                      e.dataTransfer.effectAllowed = 'copyMove';
                      
                      // Store drag data in multiple formats for compatibility
                      // Use JSON format in text/plain as fallback
                      const dragData = {
                        type: field.ControlType,
                        transactionFieldId: field.Id
                      };
                      e.dataTransfer.setData('text/plain', JSON.stringify(dragData));
                      
                      // Also try custom MIME types
                      try {
                        e.dataTransfer.setData('application/drag-type', field.ControlType.toString());
                        e.dataTransfer.setData('application/drag-transaction-field-id', field.Id.toString());
                      } catch (err) {
                        console.warn('Failed to set custom MIME type data:', err);
                      }
                      
                      // Use currentTarget to ensure we get the button element, not a child element
                      const buttonElement = e.currentTarget as HTMLElement;
                      buttonElement.setAttribute('data-drag-type', field.ControlType.toString());
                      buttonElement.setAttribute('data-drag-transaction-field-id', field.Id.toString());
                      appHelper.debugLog('Drag data set on element:', {
                        type: field.ControlType,
                        fieldId: field.Id,
                        element: buttonElement,
                        hasAttribute: buttonElement.hasAttribute('data-drag-type'),
                        dataTransferTypes: Array.from(e.dataTransfer.types)
                      });
                    }}
                    onClick={() => onAddLayoutItem(field.ControlType, field.Id)}
                    title={`Table column: ${field.DataBaseFieldName} (Drag to add)`}
                  >
                    {field.DisplayName || field.DataBaseFieldName}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Data Grid */}
          {childLevelUnitList.length > 0 && (
            <div className="mb-4">
              <div className={`text-xs font-semibold ${theme.title} mb-2`}>Data Grid</div>
              <div className="space-y-2">
                {childLevelUnitList.map((childUnit: any) => (
                  <button
                    key={childUnit.Id}
                    className={`w-full px-2 py-2 text-xs border rounded ${theme.button_default} hover:shadow-sm text-left`}
                    onClick={() => layoutItemTypeEnum?.Grid && onAddLayoutItem(layoutItemTypeEnum.Grid, undefined, childUnit.Id)}
                    title={childUnit.UnitDisplayName}
                  >
                    <div className="flex items-center gap-1">
                      <i className="fa fa-table"></i>
                      <span>{childUnit.UnitDisplayName}</span>
                    </div>
                    <div className="text-[10px] text-gray-500 mt-1 ml-5">
                      From table: {childUnit.DataBaseTableName}
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Command Button */}
          {commandActionList.length > 0 && (
            <div className="mb-4">
              <div className={`text-xs font-semibold ${theme.title} mb-2 flex items-center gap-2`}>
                Command Button
                <label className="flex items-center gap-1 text-[10px] font-normal">
                  <input
                    type="checkbox"
                    checked={showPrivateCommands}
                    onChange={(e) => setShowPrivateCommands(e.target.checked)}
                    className="w-3 h-3"
                  />
                  <span>Show Private Commands</span>
                </label>
              </div>
              <div className="space-y-1">
                {commandActionList
                  .filter((cmd: any) => cmd.ActionAttribute?.LinkToUI || showPrivateCommands)
                  .map((commandAction: any) => (
                    <button
                      key={commandAction.Id}
                      className={`w-full px-2 py-1.5 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                      onClick={() => layoutItemTypeEnum?.CommandActionButton && onAddLayoutItem(layoutItemTypeEnum.CommandActionButton, undefined, undefined, commandAction.Id)}
                      title={commandAction.Name}
                    >
                      <i className="fa fa-play-circle"></i>
                      <span>{commandAction.Name}</span>
                    </button>
                  ))}
              </div>
            </div>
          )}

          {/* Linked Search */}
          {linkedSearchList.length > 0 && (
            <div>
              <div className={`text-xs font-semibold ${theme.title} mb-2`}>Linked Search (On Root Level Unit)</div>
              <div className="space-y-1">
                {linkedSearchList.map((linkedSearch: any) => (
                  <button
                    key={linkedSearch.Id}
                    className={`w-full px-2 py-1.5 text-xs border rounded ${theme.button_default} hover:shadow-sm flex items-center gap-1`}
                    onClick={() => layoutItemTypeEnum?.LinkedSearch && onAddLayoutItem(layoutItemTypeEnum.LinkedSearch, undefined, undefined, undefined, linkedSearch.Id)}
                    title={linkedSearch.Name}
                  >
                    <i className="fa fa-play-circle"></i>
                    <span>{linkedSearch.Name}</span>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
        </div>
      )}
    </>
  );
};

export default AddFieldToolbox;
