import React, { useState, useEffect, useRef } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import FormSettingToolbox from './FormDesign/FormSettingToolbox';
import FieldSettingToolbox from './FormDesign/FieldSettingToolbox';
import AddFieldToolbox from './FormDesign/AddFieldToolbox';
import FormLayoutDesignArea from './FormDesign/FormLayoutDesignArea';
import TableContainerDialog from './FormDesign/TableContainerDialog';
import TransactionFormPreview from '../transaction/ApplicationFormBuilder/TransactionFormPreview';
import appHelper from '../../helper/appHelper';

interface FormDesignProps {
  formId: number | null;
  transactionId: number | null;
  applicationId: string | null;
  onRefresh?: () => void;
  /** When provided, "Design Data Model" button switches to Data Model Design (like left menu). */
  onSwitchToDataModelDesign?: () => void;
  /** When true, auto-run resetFormLayout(4, true) on first effective formId (Angular isAutoDesignFormLayout). When false, open as blank. Cleared via onConsumedInitialAutoLayout. */
  initialIsAutoDesignFormLayout?: boolean | null;
  /** Called after initial auto/blank layout has been applied so parent can clear the flag. */
  onConsumedInitialAutoLayout?: () => void;
  /** After first save (or auto-save) assigns a real FormId; parent should store it like Angular controllerModel.formId. */
  onFormIdResolved?: (newFormId: number) => void;
  /** Select this layout field when Form Design opens (runtime master-detail gear → Design Layout). */
  initialFieldDesignFocus?: {
    transactionFieldId: number;
    layoutItemId?: string | number;
    isGrid?: boolean;
  } | null;
}

const FormDesign: React.FC<FormDesignProps> = ({
  formId,
  transactionId,
  applicationId,
  onRefresh,
  onSwitchToDataModelDesign,
  initialIsAutoDesignFormLayout,
  onConsumedInitialAutoLayout,
  onFormIdResolved,
  initialFieldDesignFocus = null,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo, showValidationMessages } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const dispatch = useDispatch();
  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');

  const [formData, setFormData] = useState<any>(null);
  const [transactionData, setTransactionData] = useState<any>(null);
  const [currentLayoutItem, setCurrentLayoutItem] = useState<any>(null);
  const [isModified, setIsModified] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  // Track the TransactionFieldId of the item being edited to prevent useEffect from interfering
  const editingFieldIdRef = useRef<number | null>(null);
  const [draggingLayoutItemId, setDraggingLayoutItemId] = useState<number | null>(null);
  const [currentCutLayoutItem, setCurrentCutLayoutItem] = useState<any>(null);
  // Track hovered layout item (max 1) - like AngularJS currentBorderInsertButtonId
  const [currentHoveredLayoutItemHostId, setCurrentHoveredLayoutItemHostId] = useState<string | number | null>(null);
  // Track if mouse is over design panel - show container headers only when hovering
  const [isMouseOverDesignPanel, setIsMouseOverDesignPanel] = useState<boolean>(false);
  // CRITICAL: Track active tabs at FormDesign level to persist across component re-renders
  const [activeTabs, setActiveTabs] = useState<Record<string, number | string>>({});
  // Store current drag data for drop handlers
  // Row item context menu state
  const [rowItemContextMenu, setRowItemContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    item: any;
  }>({
    visible: false,
    x: 0,
    y: 0,
    item: null
  });
  
  // Container context menu state
  const [containerContextMenu, setContainerContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    item: any;
  }>({
    visible: false,
    x: 0,
    y: 0,
    item: null
  });
  const [currentDragData, setCurrentDragData] = useState<{
    itemType?: number;
    transactionFieldId?: number;
    gridTransactionUnitId?: number;
    commandActionId?: number;
    linkedSearchId?: number;
    layoutItemUiId?: number;
  } | null>(null);
  // Table Container dialog state
  const [showTableContainerDialog, setShowTableContainerDialog] = useState<boolean>(false);
  const [pendingTableContainer, setPendingTableContainer] = useState<any>(null);
  // Reset Layout dropdown
  const [resetLayoutDropdownOpen, setResetLayoutDropdownOpen] = useState<boolean>(false);
  const resetLayoutDropdownRef = useRef<HTMLDivElement>(null);
  // Run Test popup (runtime form, no data)
  const [showRunTestPopup, setShowRunTestPopup] = useState<boolean>(false);
  // Track if we already ran initial auto/blank layout (Design Form dropdown)
  const hasConsumedInitialAutoLayoutRef = useRef<boolean>(false);
  const consumedInitialFieldDesignFocusRef = useRef<boolean>(false);

  useEffect(() => {
    consumedInitialFieldDesignFocusRef.current = false;
  }, [initialFieldDesignFocus]);

  // Process transaction data (similar to AngularJS initializeFormData)
  const processTransactionData = (appTransactionData: any) => {
    if (!appTransactionData) return;

    // Build dictionaries and lists similar to AngularJS version
    const dictTransactionFieldIdAndDto: Record<number, any> = {};
    const dictChildGridTransactionUnitIdAndDto: Record<number, any> = {};
    const dictUnitIdAndDto: Record<number, any> = {};
    const dictCommandActionIdAndDto: Record<number, any> = {};
    const rootLevelUnitFieldList: any[] = [];
    const childLevelUnitList: any[] = [];
    const siblingUnitList: any[] = [];
    const dictRootLevelUnitLinkedSearchIdAndDto: Record<number, any> = {};
    const dictTransfieldIdAndFormulaDto: Record<number, any> = {};

    // Process CommandActionList
    if (appTransactionData.CommandActionList) {
      appTransactionData.CommandActionList.forEach((cmd: any) => {
        dictCommandActionIdAndDto[cmd.Id] = cmd;
      });
    }

    // Process AppTransactionUnitList
    if (appTransactionData.AppTransactionUnitList && appTransactionData.AppTransactionUnitList.length > 0) {
      appTransactionData.AppTransactionUnitList.forEach((unit: any) => {
        dictUnitIdAndDto[unit.Id] = unit;

        // Process root level unit fields
        if (unit.AppTransactionFieldList) {
          unit.AppTransactionFieldList.forEach((field: any) => {
            dictTransactionFieldIdAndDto[field.Id] = field;
            rootLevelUnitFieldList.push(field);
          });
        }

        // Process sibling units
        if (unit.IsMasterSiblingUnit) {
          siblingUnitList.push(unit);
        }

        // Process linked searches
        if (unit.AppTransactionUnitLinkedSearchList) {
          unit.AppTransactionUnitLinkedSearchList.forEach((linkedSearch: any) => {
            dictRootLevelUnitLinkedSearchIdAndDto[linkedSearch.Id] = linkedSearch;
          });
        }

        // Process child units
        if (unit.Children) {
          unit.Children.forEach((childUnit: any) => {
            dictUnitIdAndDto[childUnit.Id] = childUnit;
            dictChildGridTransactionUnitIdAndDto[childUnit.Id] = childUnit;
            childLevelUnitList.push(childUnit);

            // Process child unit fields
            if (childUnit.AppTransactionFieldList) {
              childUnit.AppTransactionFieldList.forEach((field: any) => {
                dictTransactionFieldIdAndDto[field.Id] = field;
              });
            }

            // Process grandchild units
            if (childUnit.Children) {
              childUnit.Children.forEach((grandchildUnit: any) => {
                dictUnitIdAndDto[grandchildUnit.Id] = grandchildUnit;
                if (grandchildUnit.AppTransactionFieldList) {
                  grandchildUnit.AppTransactionFieldList.forEach((field: any) => {
                    dictTransactionFieldIdAndDto[field.Id] = field;
                  });
                }
              });
            }
          });
        }
      });
    }

    // Build formula dictionary (like AngularJS dictTransfieldIdAndFormulaDto)
    // Source: AppTransactionData.DictUnitldIdAndFormulaSetDto[unitId].ListAppTransactionUnitFormula
    const dictUnitldIdAndFormulaSetDto = appTransactionData?.DictUnitldIdAndFormulaSetDto;
    if (dictUnitldIdAndFormulaSetDto) {
      Object.values(dictUnitldIdAndFormulaSetDto as Record<string, any>).forEach((formularSetDto: any) => {
        const list = formularSetDto?.ListAppTransactionUnitFormula || [];
        list.forEach((formulaDto: any) => {
          const assignTo = formulaDto?.AssignToTransFieldId;
          if (!assignTo) return;

          // Keep same behavior as Angular: formulaDto.displayText is text after '='
          const expr = formulaDto?.FormulaExpression || '';
          let displayText = expr;
          const idxEq = expr.indexOf('=');
          if (idxEq >= 0 && expr.length > idxEq) {
            displayText = expr.substring(idxEq + 1);
          }
          formulaDto.displayText = displayText;

          dictTransfieldIdAndFormulaDto[assignTo] = formulaDto;
        });
      });
    }

    // Set transaction data with processed dictionaries
    setTransactionData({
      AppTransactionData: appTransactionData,
      dictTransactionFieldIdAndDto,
      dictChildGridTransactionUnitIdAndDto,
      dictUnitIdAndDto,
      dictCommandActionIdAndDto,
      rootLevelUnitFieldList,
      childLevelUnitList,
      siblingUnitList,
      dictRootLevelUnitLinkedSearchIdAndDto,
      dictTransfieldIdAndFormulaDto
    });
  };

  // Load transaction data from API if not already in form
  const loadTransactionData = async (transId: number) => {
    try {
      const result = await appTransactionService.getOneHierarchyTransaction(
        transId.toString(),
        false, // isNewTransaction
        '', // newTransactionType
        '', // newFolderTransactionUsageType
        '', // newTransactionDataSourceFrom
        false, // isESitePageDesign
        '' // rootWorkflowTransactionId
      );
      if (result) {
        processTransactionData(result);
      }
    } catch (error: any) {
      console.error('Failed to load transaction data:', error);
    }
  };

  /** Flex SaveAppFormFlexLayoutExDto(new) does not set AppTransaction.FormId; Angular CreateNewTranactionForm does. Link FormId via SaveAppTransaction (same as DB update in AppFormBL.CreateNewTranactionForm). */
  const persistTransactionFormId = async (transId: number, newFormId: number) => {
    const hierarchy = await appTransactionService.getOneHierarchyTransaction(
      transId.toString(),
      false,
      '',
      '',
      '',
      false,
      ''
    );
    if (!hierarchy) {
      throw new Error('Could not load transaction to link FormId');
    }
    hierarchy.FormId = newFormId;
    hierarchy.IsModified = true;
    if (!hierarchy.DictDeletedItemsIds) {
      hierarchy.DictDeletedItemsIds = {};
    }
    if (!hierarchy.DictDeletedItemsIds.AppTransactionUnitList) {
      hierarchy.DictDeletedItemsIds.AppTransactionUnitList = [];
    }
    const saveResult = await appTransactionService.saveOneAppTransaction(hierarchy);
    if (saveResult?.ValidationResult) {
      showValidationMessages(saveResult.ValidationResult, true);
    }
    if (!saveResult?.IsSuccessful) {
      throw new Error('Failed to save transaction FormId');
    }
  };

  // Global drag handler to track current drag data
  useEffect(() => {
    const handleGlobalDragStart = (e: DragEvent) => {
      // Helper function to find element with drag attributes (may be parent of target)
      // Note: e.target might be Text node or other non-Element types, so we need to check
      const findElementWithDragAttributes = (node: EventTarget | null): HTMLElement | null => {
        if (!node) return null;
        
        // If node is not an Element (e.g., Text node), find the parent Element
        let element: Element | null = null;
        if (node instanceof Element) {
          element = node;
        } else if (node instanceof Node && node.parentElement) {
          element = node.parentElement;
        } else {
          return null;
        }
        
        // Check if current element has drag attributes
        if (element.hasAttribute('data-drag-type') || 
            element.hasAttribute('data-drag-transaction-field-id') ||
            element.hasAttribute('data-drag-grid-transaction-unit-id') ||
            element.hasAttribute('data-drag-command-action-id') ||
            element.hasAttribute('data-drag-linked-search-id') ||
            element.hasAttribute('data-dragging-layout-item-ui-id')) {
          return element as HTMLElement;
        }
        
        // Check parent elements (up to 3 levels)
        let current = element.parentElement;
        let depth = 0;
        while (current && depth < 3) {
          if (current.hasAttribute('data-drag-type') || 
              current.hasAttribute('data-drag-transaction-field-id') ||
              current.hasAttribute('data-drag-grid-transaction-unit-id') ||
              current.hasAttribute('data-drag-command-action-id') ||
              current.hasAttribute('data-drag-linked-search-id') ||
              current.hasAttribute('data-dragging-layout-item-ui-id')) {
            return current as HTMLElement;
          }
          current = current.parentElement;
          depth++;
        }
        
        return null;
      };
      
      const elementWithAttributes = findElementWithDragAttributes(e.target);
      
      if (elementWithAttributes) {
        const dragType = elementWithAttributes.getAttribute('data-drag-type');
        const dragFieldId = elementWithAttributes.getAttribute('data-drag-transaction-field-id');
        const dragGridUnitId = elementWithAttributes.getAttribute('data-drag-grid-transaction-unit-id');
        const dragCommandActionId = elementWithAttributes.getAttribute('data-drag-command-action-id');
        const dragLinkedSearchId = elementWithAttributes.getAttribute('data-drag-linked-search-id');
        const dragLayoutItemUiId = elementWithAttributes.getAttribute('data-dragging-layout-item-ui-id');
        
        if (dragType || dragFieldId || dragGridUnitId || dragCommandActionId || dragLinkedSearchId || dragLayoutItemUiId) {
          const dragData = {
            itemType: dragType ? parseInt(dragType) : undefined,
            transactionFieldId: dragFieldId ? parseInt(dragFieldId) : undefined,
            gridTransactionUnitId: dragGridUnitId ? parseInt(dragGridUnitId) : undefined,
            commandActionId: dragCommandActionId ? parseInt(dragCommandActionId) : undefined,
            linkedSearchId: dragLinkedSearchId ? parseInt(dragLinkedSearchId) : undefined,
            layoutItemUiId: dragLayoutItemUiId ? parseFloat(dragLayoutItemUiId) : undefined,
          };
          
          appHelper.debugLog('Global drag start - setting currentDragData:', dragData);
          setCurrentDragData(dragData);
        }
      }
    };

    const handleGlobalDragEnd = () => {
      setCurrentDragData(null);
    };

    document.addEventListener('dragstart', handleGlobalDragStart);
    document.addEventListener('dragend', handleGlobalDragEnd);

    return () => {
      document.removeEventListener('dragstart', handleGlobalDragStart);
      document.removeEventListener('dragend', handleGlobalDragEnd);
    };
  }, []);

  // Load form data
  useEffect(() => {
    loadFormData();
  }, [formId, transactionId, applicationId]);

  // Design Form dropdown: Auto Design Form (resetFormLayout(4, true)) or Design On Blank (just consume flag)
  useEffect(() => {
    if (initialIsAutoDesignFormLayout == null || hasConsumedInitialAutoLayoutRef.current) return;
    const effectiveFormId = formId ?? formData?.Id;
    if (initialIsAutoDesignFormLayout === false) {
      hasConsumedInitialAutoLayoutRef.current = true;
      onConsumedInitialAutoLayout?.();
      return;
    }
    if (initialIsAutoDesignFormLayout === true && effectiveFormId) {
      hasConsumedInitialAutoLayoutRef.current = true;
      (async () => {
        try {
          dispatch(setIsBusy());
          const result = await appTransactionService.resetFormLayout(effectiveFormId, 4, true);
          if (result?.IsSuccessful) {
            await loadFormData();
            showInfo('Auto design form layout applied');
          } else if (result?.ValidationResult) {
            showValidationMessages(result.ValidationResult, true);
          }
        } catch (error: any) {
          showError(error.message || 'Failed to apply auto design layout');
        } finally {
          dispatch(setIsNotBusy());
          onConsumedInitialAutoLayout?.();
        }
      })();
    }
  }, [initialIsAutoDesignFormLayout, formId, formData?.Id]);

  // Close Reset Layout dropdown on outside click
  useEffect(() => {
    if (!resetLayoutDropdownOpen) return;
    const handleClickOutside = (e: MouseEvent) => {
      if (resetLayoutDropdownRef.current && !resetLayoutDropdownRef.current.contains(e.target as Node)) {
        setResetLayoutDropdownOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [resetLayoutDropdownOpen]);

  const loadFormData = async () => {
    appHelper.debugLog('========== loadFormData START ==========');
    appHelper.debugLog('loadFormData called with:', { formId, transactionId, applicationId });
    appHelper.debugLog('Current formData before load:', {
      hasFormData: !!formData,
      rowCount: formData?.AppFormLayoutItemList?.length || 0,
      formDataId: formData?.Id
    });
    
    try {
      dispatch(setIsBusy());
      setIsLoading(true);

      let actualFormId = formId;

      // If formId is empty but transactionId exists, try to get formId from transaction
      if (!actualFormId && transactionId) {
        try {
          const transResult = await appTransactionService.getOneHierarchyTransaction(
            transactionId.toString(),
            false, // isNewTransaction
            '', // newTransactionType
            '', // newFolderTransactionUsageType
            '', // newTransactionDataSourceFrom
            false, // isESitePageDesign
            '' // rootWorkflowTransactionId
          );
          
          if (transResult) {          
            actualFormId = transResult.FormId || null;
            
            // Process transaction data if we have it
            processTransactionData(transResult);
          }
        } catch (error: any) {
          console.warn('Failed to load transaction to get formId:', error);
          // Continue to create new form if transaction load fails
        }
      }

      if (actualFormId) {
        // Load existing form
        const form = await appTransactionService.retrieveOneAppFormFlexLayoutExDto(actualFormId);
        if (form) {
          // Set default values like AngularJS version
          form.DefaultDeviceType = form.DefaultDeviceType || 2;
          form.DefaultNbColumns = form.DefaultNbColumns || 3;
          form.DefaultWidth = form.DefaultWidth || 1000;

          // Initialize placeholders for all rows (like AngularJS line 438-441)
          // Process each layout row in AppFormLayoutItemList
          if (form.AppFormLayoutItemList && form.AppFormLayoutItemList.length > 0) {
            form.AppFormLayoutItemList.forEach((layoutRow: any) => {
              initLayoutItemAndChildItems(layoutRow, undefined, form);
            });
          } else {
            // If form has no rows, create an initial row with placeholder (like AngularJS)
            form.AppFormLayoutItemList = form.AppFormLayoutItemList || [];
            appendNewLayoutRow(undefined, form);
          }

          // Create a deep copy to ensure React detects the changes
          // This is important because we mutated the form object during initialization
          appHelper.debugLog('Before JSON.parse(JSON.stringify), form row count:', form.AppFormLayoutItemList?.length || 0);
          const formWithPlaceholders = JSON.parse(JSON.stringify(form));
          appHelper.debugLog('After JSON.parse(JSON.stringify), formWithPlaceholders row count:', formWithPlaceholders.AppFormLayoutItemList?.length || 0);
          appHelper.debugLog('Calling setFormData in loadFormData');
          appHelper.debugLog('Stack trace for loadFormData setFormData:', new Error().stack);
          setFormData(formWithPlaceholders);
          setIsModified(false);
          appHelper.debugLog('loadFormData: setFormData called, formData will be updated');
          appHelper.debugLog('========== loadFormData END (existing form) ==========');
          
          // Check if form already has AssociatedTransactionExDto (like AngularJS version)
          if (form.AssociatedTransactionId && form.AssociatedTransactionExDto) {
            // Use the transaction data already in the form
            processTransactionData(form.AssociatedTransactionExDto);
          } else if (form.AssociatedTransactionId && !transactionData) {
            // Load transaction data separately if not included in form and not already loaded
            await loadTransactionData(form.AssociatedTransactionId);
          }
        }
      } else {
        // Create new form
        const newForm = await appTransactionService.getNewFlexFormExDto();
        if (newForm) {
          if (applicationId) {
            newForm.SaasApplicationId = parseInt(applicationId);
          }
          if (transactionId) {
            newForm.AssociatedTransactionId = transactionId;
            // Only load transaction data if not already loaded
            if (!transactionData) {
              await loadTransactionData(transactionId);
            }
          }
          // Set default values
          newForm.DefaultDeviceType = newForm.DefaultDeviceType || 2;
          newForm.DefaultNbColumns = newForm.DefaultNbColumns || 3;
          newForm.DefaultWidth = newForm.DefaultWidth || 1000;
          
          // Initialize AppFormLayoutItemList if empty
          newForm.AppFormLayoutItemList = newForm.AppFormLayoutItemList || [];
          
          // Create initial row with placeholder if form is empty
          if (newForm.AppFormLayoutItemList.length === 0) {
            appendNewLayoutRow(undefined, newForm);
          } else {
            // Initialize placeholders for existing rows
            newForm.AppFormLayoutItemList.forEach((layoutRow: any) => {
              initLayoutItemAndChildItems(layoutRow, undefined, newForm);
            });
          }
          
          // Create a deep copy to ensure React detects the changes
          // This is important because we mutated the form object during initialization
          appHelper.debugLog('Before JSON.parse(JSON.stringify) for new form, newForm row count:', newForm.AppFormLayoutItemList?.length || 0);
          const newFormWithPlaceholders = JSON.parse(JSON.stringify(newForm));
          appHelper.debugLog('After JSON.parse(JSON.stringify) for new form, newFormWithPlaceholders row count:', newFormWithPlaceholders.AppFormLayoutItemList?.length || 0);

          // When opening Form Design from Transaction with empty formId, auto-save the form first
          // so we immediately have a real FormId (matching Angular behavior).
          if (!formId && transactionId) {
            try {
              const newFormToSave = removeAllNewItemAddButtons(newFormWithPlaceholders);
              const saveResult = await appTransactionService.saveAppFormFlexLayoutExDto(newFormToSave);
              if (saveResult?.ValidationResult) {
                showValidationMessages(saveResult.ValidationResult, true);
              }
              if (saveResult?.IsSuccessful && saveResult?.Object?.Id) {
                const savedFormId = saveResult.Object.Id as number;
                try {
                  await persistTransactionFormId(transactionId, savedFormId);
                  await loadTransactionData(transactionId);
                  onFormIdResolved?.(savedFormId);
                  onRefresh?.();
                } catch (linkErr: any) {
                  showError(linkErr?.message || 'Form was saved but transaction FormId could not be linked');
                }
                const savedForm = await appTransactionService.retrieveOneAppFormFlexLayoutExDto(savedFormId);
                if (savedForm) {
                  savedForm.DefaultDeviceType = savedForm.DefaultDeviceType || 2;
                  savedForm.DefaultNbColumns = savedForm.DefaultNbColumns || 3;
                  savedForm.DefaultWidth = savedForm.DefaultWidth || 1000;
                  if (savedForm.AppFormLayoutItemList && savedForm.AppFormLayoutItemList.length > 0) {
                    savedForm.AppFormLayoutItemList.forEach((layoutRow: any) => {
                      initLayoutItemAndChildItems(layoutRow, undefined, savedForm);
                    });
                  } else {
                    savedForm.AppFormLayoutItemList = savedForm.AppFormLayoutItemList || [];
                    appendNewLayoutRow(undefined, savedForm);
                  }
                  const savedFormWithPlaceholders = JSON.parse(JSON.stringify(savedForm));
                  setFormData(savedFormWithPlaceholders);
                  setIsModified(false);
                  appHelper.debugLog('========== loadFormData END (new form auto-saved) ==========');
                  return;
                }
              }
            } catch (e: any) {
              // If auto-save fails, fall back to unsaved in-memory form so user can still work.
              console.warn('Auto-save new form on open failed:', e);
            }
          }

          appHelper.debugLog('Calling setFormData in loadFormData (new form)');
          appHelper.debugLog('Stack trace for loadFormData setFormData (new form):', new Error().stack);
          setFormData(newFormWithPlaceholders);
          setIsModified(false);
          appHelper.debugLog('loadFormData: setFormData called (new form), formData will be updated');
          appHelper.debugLog('========== loadFormData END (new form) ==========');
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to load form data');
    } finally {
      dispatch(setIsNotBusy());
      setIsLoading(false);
    }
  };

  // Helper function to remove all NewItemAddButton placeholders before saving (like AngularJS)
  const removeAllNewItemAddButtons = (formDataRef: any): any => {
    if (!formDataRef) return formDataRef;

    const cleaned = { ...formDataRef };

    const removeFromItem = (item: any): any => {
      if (item.AppFormLayoutItem_List) {
        item.AppFormLayoutItem_List = item.AppFormLayoutItem_List
          .filter((childItem: any) => 
            !(childItem.DomAttribute && childItem.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton)
          )
          .map((childItem: any) => removeFromItem(childItem));
      }
      return item;
    };

    if (cleaned.AppFormLayoutItemList) {
      cleaned.AppFormLayoutItemList = cleaned.AppFormLayoutItemList.map((item: any) => removeFromItem(item));
    }

    return cleaned;
  };

  // Helper function to find unit by field ID or unit ID
  const findUnitByFieldOrUnitId = (fieldId?: number, unitId?: number): any => {
    if (!transactionData?.AppTransactionData?.AppTransactionUnitList) return null;

    const getFieldIdFromUnitField = (f: any): number | undefined => {
      const id = f?.Id ?? f?.TransactionFieldId ?? f?.FieldId;
      if (id === null || id === undefined) return undefined;
      const n = typeof id === 'number' ? id : parseInt(id.toString(), 10);
      return Number.isFinite(n) ? n : undefined;
    };
    
    // Search in all units (including children)
    const searchInUnits = (units: any[]): any => {
      for (const unit of units) {
        // Check if this unit contains the field
        if (fieldId && unit.AppTransactionFieldList) {
          if (unit.AppTransactionFieldList.some((f: any) => getFieldIdFromUnitField(f) === fieldId)) {
            return unit;
          }
        }
        // Check if this is the unit we're looking for
        if (unitId && unit.Id === unitId) {
          return unit;
        }
        // Recursively search in children
        if (unit.Children && unit.Children.length > 0) {
          const found = searchInUnits(unit.Children);
          if (found) return found;
        }
      }
      return null;
    };
    
    return searchInUnits(transactionData.AppTransactionData.AppTransactionUnitList);
  };

  // Helper function to mark unit as modified
  const markUnitAsModified = (fieldId?: number, unitId?: number) => {
    if (!transactionData?.AppTransactionData) return;
    
    const unit = findUnitByFieldOrUnitId(fieldId, unitId);
    if (unit) {
      unit.IsModified = true;
      // Also mark the transaction as modified
      if (transactionData.AppTransactionData.AssociatedTransactionExDto) {
        transactionData.AppTransactionData.AssociatedTransactionExDto.IsModified = true;
      }
      // Update transactionData state to ensure React detects the change
      setTransactionData({ ...transactionData });
    }
  };

  // Handle save
  const handleSave = async () => {
    if (!formData) return;

    try {
      // Show global busy loader immediately
      dispatch(setIsBusy());
      
      // Allow React to update UI and show busy loader
      await Promise.resolve();
      
      // Remove NewItemAddButton placeholders before saving (like AngularJS)
      const formDataToSave = removeAllNewItemAddButtons(formData);
      
      // CRITICAL: Sync field property changes from dictionaries to AssociatedTransactionExDto.AppTransactionUnitList
      // The dictionaries point to transactionData.AppTransactionData, but we need to update formData.AssociatedTransactionExDto
      if (formDataToSave.AssociatedTransactionExDto?.AppTransactionUnitList && 
          transactionData?.dictTransactionFieldIdAndDto) {
        
        // Recursive function to find and update fields in units
        const syncFieldInUnits = (units: any[]): void => {
          if (!units) return;
          
          units.forEach((unit: any) => {
            // Update fields in this unit
            if (unit.AppTransactionFieldList) {
              unit.AppTransactionFieldList.forEach((field: any) => {
                const fieldId = field.Id;
                const dictFieldDto = transactionData.dictTransactionFieldIdAndDto[fieldId];
                
                if (dictFieldDto && dictFieldDto.IsModified) {
                  // Copy modified properties from dictionary to formData field
                  if (dictFieldDto.DisplayName !== undefined) {
                    field.DisplayName = dictFieldDto.DisplayName;
                    field.LabelDisplayBinding = dictFieldDto.DisplayName;
                  }
                  if (dictFieldDto.ControlType !== undefined) {
                    field.ControlType = dictFieldDto.ControlType;
                  }
                  // Copy all other modified properties
                  Object.keys(dictFieldDto).forEach(key => {
                    if (key !== 'IsModified' && dictFieldDto[key] !== undefined) {
                      field[key] = dictFieldDto[key];
                    }
                  });
                  field.IsModified = true;
                  unit.IsModified = true;
                }
              });
            }
            
            // Recursively process child units
            if (unit.Children && unit.Children.length > 0) {
              syncFieldInUnits(unit.Children);
            }
          });
        };
        
        // Sync all fields in all units
        syncFieldInUnits(formDataToSave.AssociatedTransactionExDto.AppTransactionUnitList);
      }
      
      // Also sync to DictAllTransactionField if it exists (backend may read from there)
      if (formDataToSave.AssociatedTransactionExDto?.DictAllTransactionField && 
          transactionData?.dictTransactionFieldIdAndDto) {
        Object.keys(transactionData.dictTransactionFieldIdAndDto).forEach((fieldIdStr: string) => {
          const fieldId = parseInt(fieldIdStr, 10);
          const fieldDto = transactionData.dictTransactionFieldIdAndDto[fieldId];
          const fieldInDict = formDataToSave.AssociatedTransactionExDto.DictAllTransactionField[fieldId];
          
          if (fieldInDict && fieldDto && fieldDto.IsModified) {
            // Update DictAllTransactionField with latest values from dictionary
            if (fieldDto.DisplayName !== undefined) {
              fieldInDict.DisplayName = fieldDto.DisplayName;
              fieldInDict.LabelDisplayBinding = fieldDto.DisplayName;
            }
            if (fieldDto.ControlType !== undefined) {
              fieldInDict.ControlType = fieldDto.ControlType;
            }
            // Copy other modified properties
            Object.keys(fieldDto).forEach(key => {
              if (fieldDto[key] !== undefined && key !== 'IsModified') {
                fieldInDict[key] = fieldDto[key];
              }
            });
            fieldInDict.IsModified = true;
          }
        });
      }
      
      // Set AssociatedTransactionExDto.IsModified = true if it exists
      if (formDataToSave.AssociatedTransactionExDto) {
        formDataToSave.AssociatedTransactionExDto.IsModified = true;
      }

      // Save transaction formulas if any unit formula sets were modified (Angular saves separately)
      const dictUnitldIdAndFormulaSetDto = transactionData?.AppTransactionData?.DictUnitldIdAndFormulaSetDto;
      if (dictUnitldIdAndFormulaSetDto) {
        const modifiedFormulaSets = Object.values(dictUnitldIdAndFormulaSetDto as Record<string, any>).filter(
          (dto: any) => dto?.IsModified
        );
        if (modifiedFormulaSets.length > 0) {
          const formulaSaveResult = await appTransactionService.saveAppTransactionFormulas(modifiedFormulaSets);
          if (formulaSaveResult?.ValidationResult) {
            showValidationMessages(formulaSaveResult.ValidationResult, true);
          }
          if (formulaSaveResult && formulaSaveResult.IsSuccessful === false) {
            throw new Error('Failed to save transaction formulas');
          }

          // Best-effort: clear modified flags locally
          modifiedFormulaSets.forEach((dto: any) => {
            dto.IsModified = false;
          });
        }
      }
      
      // Save to backend - this is the main async operation
      const result = await appTransactionService.saveAppFormFlexLayoutExDto(formDataToSave);

      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }

      if (result?.IsSuccessful && result?.Object) {
        // Re-initialize placeholders after loading saved form
        if (result.Object.AppFormLayoutItemList && result.Object.AppFormLayoutItemList.length > 0) {
          result.Object.AppFormLayoutItemList.forEach((layoutRow: any) => {
            initLayoutItemAndChildItems(layoutRow, undefined, result.Object);
          });
        }

        setFormData(result.Object);
        setIsModified(false);
        showInfo('Form saved successfully');

        const assocId = result.Object.AssociatedTransactionId ?? transactionId;
        if (!formId && result.Object.Id && assocId) {
          try {
            await persistTransactionFormId(assocId, result.Object.Id);
            await loadTransactionData(assocId);
            onFormIdResolved?.(result.Object.Id);
          } catch (linkErr: any) {
            showError(linkErr?.message || 'Form was saved but transaction FormId could not be linked');
          }
        } else if (result.Object.AssociatedTransactionId && !transactionData) {
          await loadTransactionData(result.Object.AssociatedTransactionId);
        }

        if (onRefresh) {
          onRefresh();
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to save form');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Handle refresh
  const handleRefresh = async () => {
    if (isModified) {
      const confirmed = await showConfirm(
        'You have unsaved changes. Do you want to discard them and reload?',
        {
          title: 'Confirm Refresh',
          confirmLabel: 'Discard & Reload',
          cancelLabel: 'Cancel'
        }
      );
      if (confirmed) {
        setIsModified(false);
        setCurrentLayoutItem(null); // Clear current selection
        editingFieldIdRef.current = null; // Clear editing flag
        loadFormData();
      }
    } else {
      setCurrentLayoutItem(null); // Clear current selection
      editingFieldIdRef.current = null; // Clear editing flag
      loadFormData();
    }
  };

  // Handle reset layout (Angular formDesignFlexCtrl: resetFormLayout(resetToLayoutType, needToGenerateDefaultLayout))
  // resetToLayoutType: 4 = Flex (EmAppFormLayoutType), 1 = Grid
  const handleResetLayout = async (resetToLayoutType: number, needToGenerateDefaultLayout: boolean = false) => {
    const effectiveFormId = formId ?? formData?.Id;
    if (!effectiveFormId) {
      showError('Form must be saved before resetting layout');
      return;
    }

    const confirmed = await showConfirm(
      'Please Confirm To Reset The Form Layout',
      {
        title: 'Confirm Reset Layout',
        confirmLabel: 'Reset',
        cancelLabel: 'Cancel'
      }
    );

    if (confirmed) {
      try {
        dispatch(setIsBusy());
        const result = await appTransactionService.resetFormLayout(effectiveFormId, resetToLayoutType, needToGenerateDefaultLayout);
        
        if (result?.IsSuccessful) {
          await loadFormData();
          showInfo('Layout reset successfully');
        } else if (result?.ValidationResult) {
          showValidationMessages(result.ValidationResult, true);
        }
      } catch (error: any) {
        showError(error.message || 'Failed to reset layout');
      } finally {
        dispatch(setIsNotBusy());
      }
    }
  };

  // Handle run test (preview form in popup - runtime form with no data)
  const handleRunTest = async () => {
    if (!formData?.AssociatedTransactionId) {
      showError('Form must be associated with a transaction to preview');
      return;
    }

    if (isModified) {
      const confirmed = await showConfirm(
        'You have unsaved changes. Save before preview?',
        {
          title: 'Save Before Preview',
          confirmLabel: 'Save & Preview',
          cancelLabel: 'Preview Without Saving'
        }
      );
      if (confirmed) {
        await handleSave();
      }
    }
    setShowRunTestPopup(true);
  };

  // Handle form data change
  const handleFormDataChange = (updatedFormData: any) => {
    setFormData(updatedFormData);
    setIsModified(true);
  };

  // Handle layout item selection (like AngularJS setCurrentLayoutItem)
  // CRITICAL: Ensure ForeignAppTransactionFieldExDto always points to dictionary field DTO
  const handleLayoutItemSelect = (layoutItem: any) => {
    if (!layoutItem || !formData) {
      setCurrentLayoutItem(layoutItem);
      // Clear editing flag when deselecting
      editingFieldIdRef.current = null;
      return;
    }
    
    // Find the item from the latest formData using CurrentHostId or Id
    // This ensures we get the most up-to-date version with all recent changes
    const hostId = layoutItem.CurrentHostId;
    const itemId = layoutItem.Id;
    let itemToSelect = layoutItem;
    
    // For Tab items, use the passed layoutItem directly to avoid finding wrong tab
    // (findLayoutItemByHostId might find the first tab in TabContainer instead of the correct one)
    // CRITICAL: For Tab items, we must use the exact layoutItem passed in, not search in formData
    // because Tabs are nested inside TabContainer, and findLayoutItemByHostId might return the wrong tab
    if (layoutItem.DomAttribute?.IsTab) {
      itemToSelect = layoutItem;
      // Don't try to find it in formData - use it directly to preserve the exact tab instance
    } else if (hostId) {
      const foundItem = findLayoutItemByHostId(hostId, undefined, formData);
      if (foundItem) {
        itemToSelect = foundItem;
      }
    } else if (itemId) {
      // Fallback: try to find by Id if CurrentHostId is not available
      const foundItem = findLayoutItemByHostId(itemId, undefined, formData);
      if (foundItem) {
        itemToSelect = foundItem;
      }
    }
    
    // CRITICAL: Ensure ForeignAppTransactionFieldExDto points to dictionary field DTO
    // This ensures synchronization between layoutItem and dictionary
    if (itemToSelect.TransactionFieldId && transactionData?.dictTransactionFieldIdAndDto?.[itemToSelect.TransactionFieldId]) {
      const fieldDto = transactionData.dictTransactionFieldIdAndDto[itemToSelect.TransactionFieldId];
      itemToSelect.ForeignAppTransactionFieldExDto = fieldDto;
    }
    
    // CRITICAL: Clear editing flag when selecting a new item
    // This allows useEffect to sync properly after selection
    editingFieldIdRef.current = null;
    
    setCurrentLayoutItem(itemToSelect);
  };

  // Sync currentLayoutItem with formData when formData changes
  // This ensures that if the selected item is updated in formData, currentLayoutItem is also updated
  // CRITICAL: Ensure ForeignAppTransactionFieldExDto always points to dictionary field DTO
  // CRITICAL: Don't sync if user is currently editing a field (to prevent switching to wrong field)
  React.useEffect(() => {
    appHelper.debugLog('========== useEffect (formData sync) START ==========');
    appHelper.debugLog('useEffect triggered, formData changed:', {
      hasCurrentLayoutItem: !!currentLayoutItem,
      hasFormData: !!formData,
      currentLayoutItemHostId: currentLayoutItem?.CurrentHostId,
      formDataRowCount: formData?.AppFormLayoutItemList?.length || 0,
      formDataId: formData?.Id,
      formDataName: formData?.Name,
      editingFieldId: editingFieldIdRef.current,
      stackTrace: new Error().stack?.split('\n').slice(1, 4).join('\n')
    });
    
    if (!currentLayoutItem || !formData) {
      appHelper.debugLog('useEffect: Early return - no currentLayoutItem or formData');
      return;
    }
    
    // Don't sync if user is currently editing a field
    if (editingFieldIdRef.current !== null) {
      appHelper.debugLog('useEffect: Early return - user is editing field:', editingFieldIdRef.current);
      return;
    }
    
    // CRITICAL: For Tab items, don't sync from formData because it might find the wrong tab
    // Tabs are nested inside TabContainer, and findLayoutItemByHostId might return the first tab
    // instead of the currently selected tab. We should preserve the exact tab instance.
    if (currentLayoutItem.DomAttribute?.IsTab) {
      appHelper.debugLog('useEffect: Early return - Tab item, preserving exact instance');
      return;
    }
    
    const hostId = currentLayoutItem.CurrentHostId;
    if (!hostId) {
      appHelper.debugLog('useEffect: Early return - no hostId');
      return;
    }
    
    const foundItem = findLayoutItemByHostId(hostId, undefined, formData);
    if (foundItem) {
      appHelper.debugLog('useEffect: Found item in formData:', {
        hostId,
        foundItemId: foundItem.Id,
        foundItemTransactionFieldId: foundItem.TransactionFieldId,
        hasForeignAppTransactionFieldExDto: !!foundItem.ForeignAppTransactionFieldExDto,
        currentLayoutItemForeignAppTransactionFieldExDto: !!currentLayoutItem.ForeignAppTransactionFieldExDto
      });
      
      // CRITICAL: Ensure foundItem.ForeignAppTransactionFieldExDto points to dictionary field DTO
      if (foundItem.TransactionFieldId && transactionData?.dictTransactionFieldIdAndDto?.[foundItem.TransactionFieldId]) {
        const fieldDto = transactionData.dictTransactionFieldIdAndDto[foundItem.TransactionFieldId];
        const beforeUpdate = foundItem.ForeignAppTransactionFieldExDto;
        appHelper.debugLog('useEffect: Updating ForeignAppTransactionFieldExDto:', {
          transactionFieldId: foundItem.TransactionFieldId,
          beforeUpdate: beforeUpdate ? { hasValue: true, displayName: beforeUpdate?.DisplayName } : null,
          fieldDto: fieldDto ? { hasValue: true, displayName: fieldDto?.DisplayName } : null,
          isSameReference: beforeUpdate === fieldDto
        });
        foundItem.ForeignAppTransactionFieldExDto = fieldDto;
        appHelper.debugLog('useEffect: After update, foundItem.ForeignAppTransactionFieldExDto:', {
          hasValue: !!foundItem.ForeignAppTransactionFieldExDto,
          displayName: foundItem.ForeignAppTransactionFieldExDto?.DisplayName,
          isSameReference: foundItem.ForeignAppTransactionFieldExDto === fieldDto
        });
      }
      
      // Check if ForeignAppTransactionFieldExDto.DisplayName or other key properties changed
      // This prevents unnecessary updates while ensuring we have the latest data
      const currentDisplayName = currentLayoutItem.ForeignAppTransactionFieldExDto?.DisplayName;
      const foundDisplayName = foundItem.ForeignAppTransactionFieldExDto?.DisplayName;
      
      appHelper.debugLog('useEffect: Comparing properties:', {
        currentDisplayName,
        foundDisplayName,
        displayNameChanged: currentDisplayName !== foundDisplayName,
        referenceChanged: foundItem.ForeignAppTransactionFieldExDto !== currentLayoutItem.ForeignAppTransactionFieldExDto
      });
      
      if (currentDisplayName !== foundDisplayName || 
          foundItem.ForeignAppTransactionFieldExDto !== currentLayoutItem.ForeignAppTransactionFieldExDto) {
        appHelper.debugLog('useEffect: Calling setCurrentLayoutItem with foundItem');
        setCurrentLayoutItem(foundItem);
      } else {
        appHelper.debugLog('useEffect: No update needed');
      }
    } else {
      appHelper.debugLog('useEffect: Item not found in formData for hostId:', hostId);
    }
    
    appHelper.debugLog('========== useEffect (formData sync) END ==========');
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formData]); // Only depend on formData - currentLayoutItem is intentionally excluded to avoid loops

  // Runtime master-detail gear: jump to field layout item in designer (Angular: TransactionFieldEditor from form).
  useEffect(() => {
    if (!initialFieldDesignFocus || !formData || isLoading) return;
    if (consumedInitialFieldDesignFocusRef.current) return;

    const findLayoutItemForRuntimeFieldFocus = (fd: any, focus: NonNullable<typeof initialFieldDesignFocus>) => {
      const match = (item: any) => {
        if (
          focus.layoutItemId != null &&
          focus.layoutItemId !== '' &&
          item?.Id != null &&
          String(item.Id) === String(focus.layoutItemId)
        ) {
          return true;
        }
        if (
          item?.TransactionFieldId != null &&
          Number(item.TransactionFieldId) === Number(focus.transactionFieldId)
        ) {
          return true;
        }
        return false;
      };
      const walk = (items: any[]): any => {
        for (const item of items || []) {
          if (match(item)) return item;
          if (item.AppFormLayoutItem_List?.length) {
            const inner = walk(item.AppFormLayoutItem_List);
            if (inner) return inner;
          }
        }
        return null;
      };
      return walk(fd?.AppFormLayoutItemList || []);
    };

    const found = findLayoutItemForRuntimeFieldFocus(formData, initialFieldDesignFocus);
    if (found) {
      consumedInitialFieldDesignFocusRef.current = true;
      editingFieldIdRef.current = null;
      setCurrentLayoutItem(found);
    }
  }, [formData, isLoading, initialFieldDesignFocus]);

  // Handle layout item hover (like AngularJS mouseoverLayoutItem)
  const handleLayoutItemHover = (hostId: string | number | null) => {
    // Only log if hostId changes to avoid too many logs
    if (hostId !== currentHoveredLayoutItemHostId) {
      appHelper.debugLog('handleLayoutItemHover:', {
        hostId,
        previousHostId: currentHoveredLayoutItemHostId,
        formDataRowCount: formData?.AppFormLayoutItemList?.length || 0
      });
    }
    setCurrentHoveredLayoutItemHostId(hostId);
  };

  // Handle open row item context menu
  const handleOpenRowItemContextMenu = (event: React.MouseEvent, layoutItem: any) => {
    event.preventDefault();
    event.stopPropagation();

    const { x, y } = appHelper.clampMenuPositionToViewport({
      x: event.clientX,
      y: event.clientY,
      menuWidth: 170,
      menuHeight: 220,
      margin: 8
    });

    setRowItemContextMenu({
      visible: true,
      x,
      y,
      item: layoutItem
    });
  };

  // Handle open container context menu
  const handleOpenContainerContextMenu = (event: React.MouseEvent, layoutItem: any) => {
    event.preventDefault();
    event.stopPropagation();

    const { x, y } = appHelper.clampMenuPositionToViewport({
      x: event.clientX,
      y: event.clientY,
      menuWidth: 190,
      menuHeight: 240,
      margin: 8
    });

    setContainerContextMenu({
      visible: true,
      x,
      y,
      item: layoutItem
    });
  };

  // Handle close container context menu
  const handleCloseContainerContextMenu = () => {
    setContainerContextMenu({
      visible: false,
      x: 0,
      y: 0,
      item: null
    });
  };

  // Handle add tab to Tab Container
  const handleAddTabToContainer = (tabContainer: any) => {
    if (!tabContainer || !formData) return;
    
    // Find the next tab number
    const existingTabs = tabContainer.AppFormLayoutItem_List || [];
    const tabNumbers = existingTabs
      .map((tab: any) => {
        const name = tab.DomAttribute?.DisplayName || '';
        const match = name.match(/^Tab(\d+)$/);
        return match ? parseInt(match[1]) : 0;
      })
      .filter((n: number) => n > 0);
    
    const nextTabNumber = tabNumbers.length > 0 
      ? Math.max(...tabNumbers) + 1 
      : existingTabs.length + 1;
    
    const newTabName = `Tab${nextTabNumber}`;
    
    // Append new tab using existing function
    appendNewLayoutTab(tabContainer, newTabName, formData);
    
    setFormData({ ...formData });
    setIsModified(true);
  };

  // Handle close row item context menu
  const handleCloseRowItemContextMenu = () => {
    setRowItemContextMenu({
      visible: false,
      x: 0,
      y: 0,
      item: null
    });
  };

  // Handle justify entire row - evenly distribute colSpan across all items
  const handleJustifyEntireRow = () => {
    if (!rowItemContextMenu.item || !formData) return;
    const layoutItem = rowItemContextMenu.item;
    const parentRow = findLayoutItemByHostId(layoutItem.ParentHostId);
    if (parentRow && parentRow.AppFormLayoutItem_List) {
      // Get all non-placeholder items
      const items = parentRow.AppFormLayoutItem_List.filter(
        (item: any) => item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton
      );
      
      if (items.length > 0) {
        // Calculate equal colSpan for each item (24 columns total)
        const newColSpan = Math.floor(24 / items.length);
        const remainder = 24 % items.length;
        
        // Distribute colSpan evenly, with remainder going to the last item(s)
        items.forEach((item: any, index: number) => {
          if (item.DomAttribute) {
            // Last item(s) get the remainder
            if (index >= items.length - remainder) {
              item.DomAttribute.ColSpanValue = newColSpan + 1;
            } else {
              item.DomAttribute.ColSpanValue = newColSpan;
            }
          }
        });
        
        // Ensure placeholder button is updated/removed if row is full
        ensureNewItemAddButton(parentRow);
        
        setFormData({ ...formData });
        setIsModified(true);
      }
    }
    handleCloseRowItemContextMenu();
  };

  // Handle justify left - set current item's colSpan to fill remaining space on the right
  // Like AngularJS: calculates remaining space to the right of current item, sets current item's colSpan to fill it
  // This makes the item expand to fill the remaining space on the right, effectively "justifying left" (item stays left, fills right)
  const handleJustifyLeft = () => {
    if (!rowItemContextMenu.item || !formData) return;
    const layoutItem = rowItemContextMenu.item;
    const parentRow = findLayoutItemByHostId(layoutItem.ParentHostId);
    if (parentRow && parentRow.AppFormLayoutItem_List) {
      // Get all non-placeholder, non-space items
      const allItems = parentRow.AppFormLayoutItem_List.filter(
        (item: any) => item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton &&
                       item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.Space
      );
      
      // Find the index of current item in the filtered list
      const currentItemIndex = allItems.findIndex((item: any) => 
        item.CurrentHostId === layoutItem.CurrentHostId ||
        (item.Id && layoutItem.Id && item.Id === layoutItem.Id)
      );
      
      if (currentItemIndex >= 0) {
        // Calculate colSpan of all items to the right of current item (excluding current item)
        const rightItemsColSpan = allItems.slice(currentItemIndex + 1).reduce((sum: number, item: any) => {
          return sum + (item.DomAttribute?.ColSpanValue || 0);
        }, 0);
        
        // Calculate colSpan of all items to the left of current item (excluding current item)
        const leftItemsColSpan = allItems.slice(0, currentItemIndex).reduce((sum: number, item: any) => {
          return sum + (item.DomAttribute?.ColSpanValue || 0);
        }, 0);
        
        // Calculate remaining space: 24 - leftItemsColSpan - rightItemsColSpan
        // Current item should fill this remaining space
        const remainingColSpan = 24 - leftItemsColSpan - rightItemsColSpan;
        
        // Set current item's colSpan to fill the remaining space
        if (layoutItem.DomAttribute) {
          layoutItem.DomAttribute.ColSpanValue = remainingColSpan;
        }
        
        // Remove placeholder buttons (AngularJS version does not add placeholder after justify)
        removeNewItemAddButton(parentRow);
        
        setFormData({ ...formData });
        setIsModified(true);
      }
    }
    handleCloseRowItemContextMenu();
  };

  // Handle justify right - set current item's colSpan to fill remaining space on the right
  // Like AngularJS: calculates remaining space to the right of current item, sets current item's colSpan to fill it
  const handleJustifyRight = () => {
    if (!rowItemContextMenu.item || !formData) return;
    const layoutItem = rowItemContextMenu.item;
    const parentRow = findLayoutItemByHostId(layoutItem.ParentHostId);
    if (parentRow && parentRow.AppFormLayoutItem_List) {
      // Get all non-placeholder, non-space items
      const allItems = parentRow.AppFormLayoutItem_List.filter(
        (item: any) => item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton &&
                       item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.Space
      );
      
      // Find the index of current item in the filtered list
      const currentItemIndex = allItems.findIndex((item: any) => 
        item.CurrentHostId === layoutItem.CurrentHostId ||
        (item.Id && layoutItem.Id && item.Id === layoutItem.Id)
      );
      
      if (currentItemIndex >= 0) {
        // Calculate colSpan of all items to the left of current item (including current item itself)
        const leftItemsColSpan = allItems.slice(0, currentItemIndex + 1).reduce((sum: number, item: any) => {
          return sum + (item.DomAttribute?.ColSpanValue || 0);
        }, 0);
        
        // Calculate remaining space on the right
        // Current item should fill: remaining space = 24 - (leftItemsColSpan - currentItemColSpan)
        // Which simplifies to: currentItemColSpan + remainingSpace = 24 - (leftItemsColSpan - currentItemColSpan)
        // Or: newColSpan = 24 - (leftItemsColSpan - currentItemColSpan) = 24 - leftItemsColSpan + currentItemColSpan
        const currentItemColSpan = layoutItem.DomAttribute?.ColSpanValue || 0;
        const leftItemsWithoutCurrentColSpan = leftItemsColSpan - currentItemColSpan;
        const remainingColSpan = 24 - leftItemsWithoutCurrentColSpan;
        
        // Set current item's colSpan to fill the remaining space on the right
        if (layoutItem.DomAttribute) {
          layoutItem.DomAttribute.ColSpanValue = remainingColSpan;
        }
        
        // Remove placeholder buttons (AngularJS version does not add placeholder after justify)
        removeNewItemAddButton(parentRow);
        
        setFormData({ ...formData });
        setIsModified(true);
      }
    }
    handleCloseRowItemContextMenu();
  };

  // Handle justify entire row for container (like AngularJS justifyCurrentLayoutRow)
  // Works on the LayoutRow that contains the container
  const handleJustifyEntireRowForContainer = (containerItem: any) => {
    if (!containerItem || !formData) return;
    
    // CRITICAL: If the item is a Tab (IsTab: true), it's inside a Tab Container
    // We need to find the Tab Container first, then find its parent LayoutRow
    let actualContainerItem = containerItem;
    if (containerItem.DomAttribute?.IsTab) {
      // This is a Tab inside a Tab Container, find the Tab Container
      const tabContainer = findLayoutItemByHostId(containerItem.ParentHostId);
      if (tabContainer && tabContainer.DomAttribute?.WidgetDisplayType === layoutItemTypeEnum?.TabContainer) {
        actualContainerItem = tabContainer;
      } else {
        // If we can't find the Tab Container, this is an error - return early
        console.error('handleJustifyEntireRowForContainer: Could not find Tab Container for Tab item');
        return;
      }
    }
    
    // Verify that actualContainerItem is a container type (Section, TabContainer, or TableContainer)
    const displayType = actualContainerItem.DomAttribute?.WidgetDisplayType;
    const isContainerType = displayType === layoutItemTypeEnum?.Section ||
                            displayType === layoutItemTypeEnum?.TabContainer ||
                            displayType === layoutItemTypeEnum?.TableContainer;
    
    if (!isContainerType) {
      console.error('handleJustifyEntireRowForContainer: Item is not a container type', {
        displayType,
        itemId: actualContainerItem.Id,
        itemCurrentHostId: actualContainerItem.CurrentHostId
      });
      return;
    }
    
    // Find the parent LayoutRow that contains this container
    const parentRow = findLayoutItemByHostId(actualContainerItem.ParentHostId);
    if (!parentRow || !parentRow.AppFormLayoutItem_List) {
      console.error('handleJustifyEntireRowForContainer: Could not find parent LayoutRow', {
        containerItemId: actualContainerItem.Id,
        containerItemCurrentHostId: actualContainerItem.CurrentHostId,
        parentHostId: actualContainerItem.ParentHostId
      });
      return;
    }
    
    // Get all non-placeholder items in the parent LayoutRow (like AngularJS getLayoutRowChildItemInfo)
    const items = parentRow.AppFormLayoutItem_List.filter(
      (item: any) => item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton
    );
    
    if (items.length > 0) {
      // Calculate equal colSpan for each item (24 columns total)
      const newColSpan = Math.floor(24 / items.length);
      const remainder = 24 % items.length;
      
      // Distribute colSpan evenly, with remainder going to the last item(s)
      items.forEach((item: any, index: number) => {
        if (item.DomAttribute) {
          // Last item(s) get the remainder
          if (index >= items.length - remainder) {
            item.DomAttribute.ColSpanValue = newColSpan + 1;
          } else {
            item.DomAttribute.ColSpanValue = newColSpan;
          }
        }
      });
      
      // Ensure placeholder button is updated/removed if row is full
      ensureNewItemAddButton(parentRow);
      
      setFormData({ ...formData });
      setIsModified(true);
    }
  };

  // Handle justify left for container (like AngularJS justifyCurrentLayoutRow with LeftSideItems option)
  const handleJustifyLeftForContainer = (containerItem: any) => {
    if (!containerItem || !formData) return;
    const parentRow = findLayoutItemByHostId(containerItem.ParentHostId);
    if (parentRow && parentRow.AppFormLayoutItem_List) {
      // Get all non-placeholder, non-space items
      const allItems = parentRow.AppFormLayoutItem_List.filter(
        (item: any) => item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton &&
                       item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.Space
      );
      
      // Find the index of current container in the filtered list
      const currentItemIndex = allItems.findIndex((item: any) => 
        item.CurrentHostId === containerItem.CurrentHostId ||
        (item.Id && containerItem.Id && item.Id === containerItem.Id)
      );
      
      if (currentItemIndex >= 0) {
        // Calculate colSpan of all items to the right of current item (excluding current item)
        const rightItemsColSpan = allItems.slice(currentItemIndex + 1).reduce((sum: number, item: any) => {
          return sum + (item.DomAttribute?.ColSpanValue || 0);
        }, 0);
        
        // Calculate colSpan of all items to the left of current item (excluding current item)
        const leftItemsColSpan = allItems.slice(0, currentItemIndex).reduce((sum: number, item: any) => {
          return sum + (item.DomAttribute?.ColSpanValue || 0);
        }, 0);
        
        // Calculate remaining space: 24 - leftItemsColSpan - rightItemsColSpan
        // Current item should fill this remaining space
        const remainingColSpan = 24 - leftItemsColSpan - rightItemsColSpan;
        
        // Set current item's colSpan to fill the remaining space
        if (containerItem.DomAttribute) {
          containerItem.DomAttribute.ColSpanValue = remainingColSpan;
        }
        
        // Remove placeholder buttons (AngularJS version does not add placeholder after justify)
        removeNewItemAddButton(parentRow);
        
        setFormData({ ...formData });
        setIsModified(true);
      }
    }
  };

  // Handle justify right for container (like AngularJS justifyCurrentLayoutRow with RightSideItems option)
  const handleJustifyRightForContainer = (containerItem: any) => {
    if (!containerItem || !formData) return;
    const parentRow = findLayoutItemByHostId(containerItem.ParentHostId);
    if (parentRow && parentRow.AppFormLayoutItem_List) {
      // Get all non-placeholder, non-space items
      const allItems = parentRow.AppFormLayoutItem_List.filter(
        (item: any) => item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton &&
                       item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.Space
      );
      
      // Find the index of current container in the filtered list
      const currentItemIndex = allItems.findIndex((item: any) => 
        item.CurrentHostId === containerItem.CurrentHostId ||
        (item.Id && containerItem.Id && item.Id === containerItem.Id)
      );
      
      if (currentItemIndex >= 0) {
        // Calculate colSpan of all items to the left of current item (including current item itself)
        const leftItemsColSpan = allItems.slice(0, currentItemIndex + 1).reduce((sum: number, item: any) => {
          return sum + (item.DomAttribute?.ColSpanValue || 0);
        }, 0);
        
        // Calculate remaining space on the right
        const currentItemColSpan = containerItem.DomAttribute?.ColSpanValue || 0;
        const leftItemsWithoutCurrentColSpan = leftItemsColSpan - currentItemColSpan;
        const remainingColSpan = 24 - leftItemsWithoutCurrentColSpan;
        
        // Set current item's colSpan to fill the remaining space on the right
        if (containerItem.DomAttribute) {
          containerItem.DomAttribute.ColSpanValue = remainingColSpan;
        }
        
        // Remove placeholder buttons (AngularJS version does not add placeholder after justify)
        removeNewItemAddButton(parentRow);
        
        setFormData({ ...formData });
        setIsModified(true);
      }
    }
  };

  // Handle cut row item
  const handleCutRowItem = () => {
    if (!rowItemContextMenu.item) return;
    setCurrentCutLayoutItem(rowItemContextMenu.item);
    handleCloseRowItemContextMenu();
  };

  // Handle delete row item
  const handleDeleteRowItem = () => {
    if (!rowItemContextMenu.item || !formData) return;
    const layoutItem = rowItemContextMenu.item;
    const parentRow = findLayoutItemByHostId(layoutItem.ParentHostId);
    
    if (parentRow && parentRow.AppFormLayoutItem_List) {
      // Use CurrentHostId or Id to find the item (more reliable than indexOf with object reference)
      const itemToDelete = parentRow.AppFormLayoutItem_List.find((item: any) => 
        item.CurrentHostId === layoutItem.CurrentHostId || 
        (item.Id && layoutItem.Id && item.Id === layoutItem.Id)
      );
      
      if (itemToDelete) {
        // Remove the item from the array
        parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List.filter((item: any) => 
          item.CurrentHostId !== layoutItem.CurrentHostId && 
          !(item.Id && layoutItem.Id && item.Id === layoutItem.Id)
        );
        
        // Check if row is now empty (excluding placeholder buttons)
        const remainingItems = parentRow.AppFormLayoutItem_List.filter((item: any) => 
          item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton
        );
        
        if (remainingItems.length === 0) {
          // Delete empty row
          if (parentRow.ParentHostId) {
            const parentSection = findLayoutItemByHostId(parentRow.ParentHostId);
            if (parentSection && parentSection.AppFormLayoutItem_List) {
              // Use CurrentHostId to find and remove the row
              parentSection.AppFormLayoutItem_List = parentSection.AppFormLayoutItem_List.filter((item: any) => 
                item.CurrentHostId !== parentRow.CurrentHostId
              );
            }
          } else {
            // Root level row
            if (formData.AppFormLayoutItemList) {
              // Use CurrentHostId to find and remove the row
              formData.AppFormLayoutItemList = formData.AppFormLayoutItemList.filter((item: any) => 
                item.CurrentHostId !== parentRow.CurrentHostId
              );
            }
          }
        } else {
          // Add placeholder if needed
          ensureNewItemAddButton(parentRow);
        }
        
        // Create a new formData object to trigger React re-render
        setFormData({ ...formData });
        setIsModified(true);
        setCurrentLayoutItem(null);
      } else {
        console.warn('handleDeleteRowItem: Item not found in parent row', {
          layoutItemHostId: layoutItem.CurrentHostId,
          parentRowHostId: parentRow.CurrentHostId,
          itemsInRow: parentRow.AppFormLayoutItem_List.length
        });
      }
    }
    handleCloseRowItemContextMenu();
  };

  // Handle mouse move on design panel (like AngularJS mouseMoveOnDesignPanel)
  // Clear hover when mouse moves away from layout items
  const handleDesignPanelMouseMove = (event: React.MouseEvent) => {
    const target = event.target as HTMLElement;
    // Check if mouse is over a layout item container or hover area
    if (!target.closest('.LayoutItemContainer') && !target.closest('.LayoutItemHoverArea')) {
      setCurrentHoveredLayoutItemHostId(null);
    }
  };

  // Helper function to find parent row of a layout item
  const findParentRow = (layoutItem: any, searchInRows?: any[]): any => {
    const rowsToSearch = searchInRows || formData?.AppFormLayoutItemList || [];
    
    for (const row of rowsToSearch) {
      // Check if item is directly in this row
      if (row.AppFormLayoutItem_List && row.AppFormLayoutItem_List.some((item: any) => 
        item.CurrentHostId === layoutItem.CurrentHostId || item.Id === layoutItem.Id
      )) {
        return row;
      }
      
      // Recursively search in child items (sections, etc.)
      if (row.AppFormLayoutItem_List) {
        for (const childItem of row.AppFormLayoutItem_List) {
          if (childItem.AppFormLayoutItem_List) {
            const found = findParentRow(layoutItem, [childItem]);
            if (found) return found;
          }
        }
      }
    }
    
    return null;
  };

  // Helper function to update layout item in formData recursively
  const updateLayoutItemInFormData = (formDataRef: any, updatedItem: any): any => {
    if (!formDataRef || !updatedItem) return formDataRef;

    // Create a deep copy
    const updated = { ...formDataRef };

    // Recursive function to update item in nested structure
    const updateItemRecursively = (items: any[]): any[] => {
      return items.map((item: any) => {
        // Check if this is the item to update
        // IMPORTANT:
        // - Always prefer matching by CurrentHostId (unique per layout item instance)
        // - Only match by Id when BOTH sides have a real Id
        //   (avoid "undefined === undefined" accidentally matching the wrong item)
        const isSameByHostId =
          !!item.CurrentHostId &&
          !!updatedItem.CurrentHostId &&
          item.CurrentHostId === updatedItem.CurrentHostId;
        const isSameById =
          item.Id !== null &&
          item.Id !== undefined &&
          updatedItem.Id !== null &&
          updatedItem.Id !== undefined &&
          item.Id === updatedItem.Id;

        if (isSameByHostId || isSameById) {
          // Create a new object with all properties from updatedItem
          // This ensures React detects the change and re-renders
          const newItem = {
            ...updatedItem,
            // Ensure nested objects are also new references for layout structure
            DomAttribute: updatedItem.DomAttribute ? { ...updatedItem.DomAttribute } : undefined,
            // CRITICAL: Preserve DTO references (don't copy them)
            // These should point to the same objects in AssociatedTransactionExDto/dictionaries
            ForeignAppTransactionFieldExDto: updatedItem.ForeignAppTransactionFieldExDto,
            ForeignAppTransactionUnitExDto: updatedItem.ForeignAppTransactionUnitExDto,
            ForeignAppCommandActionExDto: updatedItem.ForeignAppCommandActionExDto,
            ForeignAppLinkedSearchExDto: updatedItem.ForeignAppLinkedSearchExDto,
            // Recursively copy child items if they exist
            AppFormLayoutItem_List: updatedItem.AppFormLayoutItem_List ? 
              updatedItem.AppFormLayoutItem_List.map((child: any) => ({ ...child })) : undefined
          };
          return newItem;
        }
        
        // Recursively update in child items (for nested structures like Section -> Row -> Item)
        if (item.AppFormLayoutItem_List && item.AppFormLayoutItem_List.length > 0) {
          const updatedChildList = updateItemRecursively(item.AppFormLayoutItem_List);
          // Check if any child was updated
          if (updatedChildList.some((child: any, idx: number) => 
            child !== item.AppFormLayoutItem_List[idx]
          )) {
            return { ...item, AppFormLayoutItem_List: updatedChildList };
          }
        }
        
        return item;
      });
    };

    // Update in AppFormLayoutItemList
    if (updated.AppFormLayoutItemList) {
      updated.AppFormLayoutItemList = updateItemRecursively(updated.AppFormLayoutItemList);
    }

    return updated;
  };

  // Handle layout item change
  const handleLayoutItemChange = (updatedLayoutItem: any) => {
    appHelper.debugLog('========== handleLayoutItemChange START ==========');
    appHelper.debugLog('handleLayoutItemChange called with:', {
      hasFormData: !!formData,
      hasUpdatedLayoutItem: !!updatedLayoutItem,
      updatedLayoutItemCurrentHostId: updatedLayoutItem?.CurrentHostId,
      updatedLayoutItemTransactionFieldId: updatedLayoutItem?.TransactionFieldId,
      currentEditingFieldId: editingFieldIdRef.current,
      formDataRowCount: formData?.AppFormLayoutItemList?.length || 0,
      stackTrace: new Error().stack?.split('\n').slice(1, 5).join('\n')
    });
    
    if (!formData || !updatedLayoutItem) {
      appHelper.debugLog('handleLayoutItemChange: Early return - no formData or updatedLayoutItem');
      return;
    }
    
    // CRITICAL: Check if this is a duplicate call from convertPlaceholderToItem
    // If we just converted a placeholder and set currentLayoutItem, and handleLayoutItemChange
    // is called immediately after with the same item, it's likely a false trigger (e.g., from ComboBox initialization)
    // We should skip the update if the item hasn't actually changed
    if (updatedLayoutItem.CurrentHostId && currentLayoutItem?.CurrentHostId === updatedLayoutItem.CurrentHostId) {
      // Find the existing item in formData to compare
      const existingItem = findLayoutItemByHostId(updatedLayoutItem.CurrentHostId, undefined, formData);
      if (existingItem) {
        // Compare key properties to see if anything actually changed
        const controlTypeChanged = (updatedLayoutItem.DomAttribute?.ControlType !== existingItem.DomAttribute?.ControlType) ||
                                   (updatedLayoutItem.ForeignAppTransactionFieldExDto?.ControlType !== existingItem.ForeignAppTransactionFieldExDto?.ControlType);
        const displayNameChanged = (updatedLayoutItem.ForeignAppTransactionFieldExDto?.DisplayName !== existingItem.ForeignAppTransactionFieldExDto?.DisplayName);
        // DomAttribute.DisplayName is used by Section/Stack, Tab, Content, Space, containers, etc.
        // Previously only Tab/Content/Space were checked — Section Label Text edits were discarded (felt read-only).
        const domAttributeDisplayNameChanged =
          updatedLayoutItem.DomAttribute?.DisplayName !== existingItem.DomAttribute?.DisplayName;
        const colSpanChanged = (updatedLayoutItem.DomAttribute?.ColSpanValue !== existingItem.DomAttribute?.ColSpanValue);
        const heightChanged = (updatedLayoutItem.DomAttribute?.HeightValue !== existingItem.DomAttribute?.HeightValue);
        const backgroundColorChanged = (updatedLayoutItem.DomAttribute?.BackgroundColor !== existingItem.DomAttribute?.BackgroundColor);
        const textColorChanged = (updatedLayoutItem.DomAttribute?.TextColor !== existingItem.DomAttribute?.TextColor);
        const formulaChanged = (updatedLayoutItem.DomAttribute?.Formula !== existingItem.DomAttribute?.Formula);
        const visibleExpressionChanged = (updatedLayoutItem.DomAttribute?.VisibleExpression !== existingItem.DomAttribute?.VisibleExpression);
        const workflowTriggerChanged = (updatedLayoutItem.DomAttribute?.WorkflowTrigger !== existingItem.DomAttribute?.WorkflowTrigger);
        const isCollapsibleChanged =
          updatedLayoutItem.DomAttribute?.IsCollapsible !== existingItem.DomAttribute?.IsCollapsible;
        const isDefaultCollapsedChanged =
          updatedLayoutItem.DomAttribute?.IsDefaultCollapsed !== existingItem.DomAttribute?.IsDefaultCollapsed;
        const defaultNbColumnsChanged =
          updatedLayoutItem.DomAttribute?.DefaultNbColumns !== existingItem.DomAttribute?.DefaultNbColumns;
        
        const hasActualChange = controlTypeChanged || displayNameChanged || domAttributeDisplayNameChanged || colSpanChanged || heightChanged || 
                                backgroundColorChanged || textColorChanged || formulaChanged || 
                                visibleExpressionChanged || workflowTriggerChanged ||
                                isCollapsibleChanged || isDefaultCollapsedChanged || defaultNbColumnsChanged;
        
        if (!hasActualChange) {
          appHelper.debugLog('handleLayoutItemChange: Early return - no actual changes detected (likely false trigger from ComboBox initialization)');
          return;
        }
        
        appHelper.debugLog('handleLayoutItemChange: Changes detected:', {
          controlTypeChanged,
          displayNameChanged,
          domAttributeDisplayNameChanged,
          colSpanChanged,
          heightChanged,
          backgroundColorChanged,
          textColorChanged,
          formulaChanged,
          visibleExpressionChanged,
          workflowTriggerChanged,
          isCollapsibleChanged,
          isDefaultCollapsedChanged,
          defaultNbColumnsChanged
        });
      }
    }

    // CRITICAL: Use CurrentHostId or TransactionFieldId to identify the correct field
    // This ensures we update the correct field even if currentLayoutItem has changed
    const fieldId = updatedLayoutItem.TransactionFieldId;
    
    // If TransactionFieldId is missing, try to find it from currentLayoutItem or formData
    let actualFieldId = fieldId;
    if (!actualFieldId && currentLayoutItem?.TransactionFieldId) {
      // Use currentLayoutItem's TransactionFieldId if updatedLayoutItem doesn't have it
      actualFieldId = currentLayoutItem.TransactionFieldId;
      updatedLayoutItem.TransactionFieldId = actualFieldId;
    }
    
    // If still no fieldId, try to find it from formData using CurrentHostId
    if (!actualFieldId && updatedLayoutItem.CurrentHostId) {
      const foundItem = findLayoutItemByHostId(updatedLayoutItem.CurrentHostId, undefined, formData);
      if (foundItem?.TransactionFieldId) {
        actualFieldId = foundItem.TransactionFieldId;
        updatedLayoutItem.TransactionFieldId = actualFieldId;
      }
    }

    // CRITICAL: Set editing flag to prevent useEffect from interfering
    if (actualFieldId) {
      editingFieldIdRef.current = actualFieldId;
    }

    setCurrentLayoutItem(updatedLayoutItem);
    
    // CRITICAL: If this is a field and ForeignAppTransactionFieldExDto properties changed,
    // update the field DTO in the dictionary (which points to AssociatedTransactionExDto)
    // This ensures changes are persisted when saving
    if (actualFieldId && updatedLayoutItem.ForeignAppTransactionFieldExDto) {
      const fieldDto = transactionData?.dictTransactionFieldIdAndDto?.[actualFieldId];
      
      if (fieldDto) {
        // Update the field DTO in dictionary with changes from layoutItem
        // The dictionary points to the same object in AssociatedTransactionExDto
        const incomingDto = updatedLayoutItem.ForeignAppTransactionFieldExDto;
        
        // Update DisplayName
        if (incomingDto.DisplayName !== undefined && incomingDto.DisplayName !== fieldDto.DisplayName) {
          fieldDto.DisplayName = incomingDto.DisplayName;
          fieldDto.LabelDisplayBinding = incomingDto.DisplayName;
          fieldDto.IsModified = true;
        }
        
        // Update ControlType
        if (incomingDto.ControlType !== undefined && incomingDto.ControlType !== fieldDto.ControlType) {
          fieldDto.ControlType = incomingDto.ControlType;
          fieldDto.IsModified = true;
        }
        
        // Update other properties
        Object.keys(incomingDto).forEach(key => {
          if (key !== 'IsModified' && key !== 'DisplayName' && key !== 'ControlType' && 
              incomingDto[key] !== undefined && incomingDto[key] !== fieldDto[key]) {
            fieldDto[key] = incomingDto[key];
            fieldDto.IsModified = true;
          }
        });
        
        // CRITICAL: Ensure updatedLayoutItem.ForeignAppTransactionFieldExDto points to dictionary field DTO
        // This ensures they stay synchronized
        updatedLayoutItem.ForeignAppTransactionFieldExDto = fieldDto;
        
        // Find and update the unit that contains this field
        const unitDto = findUnitByFieldOrUnitId(actualFieldId);
        if (unitDto) {
          unitDto.IsModified = true;
        }

        // CRITICAL: Grid column editing uses a pseudo layout item. In some cases the unit's
        // AppTransactionFieldList holds a different field object reference than the dictionary.
        // Sync the changed field back into the unit list so Grid placeholder reflects edits immediately.
        if (updatedLayoutItem.__isGridColumn === true && updatedLayoutItem.GridTransactionUnitId) {
          const gridUnitDto =
            findUnitByFieldOrUnitId(undefined, updatedLayoutItem.GridTransactionUnitId) || unitDto;
          if (gridUnitDto?.AppTransactionFieldList) {
            const getFieldIdFromUnitField = (f: any): number | undefined => {
              const id = f?.Id ?? f?.TransactionFieldId ?? f?.FieldId;
              if (id === null || id === undefined) return undefined;
              const n = typeof id === 'number' ? id : parseInt(id.toString(), 10);
              return Number.isFinite(n) ? n : undefined;
            };
            const unitField = gridUnitDto.AppTransactionFieldList.find(
              (f: any) => getFieldIdFromUnitField(f) === actualFieldId
            );
            if (unitField) {
              Object.assign(unitField, fieldDto);
            }
            // Force state update so children that only receive AppTransactionData also re-render
            if (transactionData?.AppTransactionData) {
              setTransactionData({
                ...transactionData,
                AppTransactionData: { ...transactionData.AppTransactionData }
              });
            } else {
              setTransactionData({ ...transactionData });
            }
          }
        }
        
        // Mark transaction as modified
        if (formData.AssociatedTransactionExDto) {
          formData.AssociatedTransactionExDto.IsModified = true;
        }
      }
    }
    
    // Mark unit as modified if this is a field or grid unit
    if (updatedLayoutItem.TransactionFieldId) {
      // This is a field - mark its unit as modified
      markUnitAsModified(updatedLayoutItem.TransactionFieldId);
    } else if (updatedLayoutItem.GridTransactionUnitId) {
      // This is a grid unit - mark the unit as modified
      markUnitAsModified(undefined, updatedLayoutItem.GridTransactionUnitId);
    }
    
    // Update the layout item in formData
    const updatedFormData = updateLayoutItemInFormData(formData, updatedLayoutItem);
    
    // CRITICAL: Clear editing flag after update is complete
    // Use setTimeout to ensure this happens after React state updates
    setTimeout(() => {
      appHelper.debugLog('handleLayoutItemChange: Clearing editingFieldIdRef in setTimeout');
      editingFieldIdRef.current = null;
    }, 0);
    
    appHelper.debugLog('handleLayoutItemChange: setFormData called, formData will be updated');
    appHelper.debugLog('========== handleLayoutItemChange END ==========');
    
    // Note: We don't need to call appendNewLayoutRow here because:
    // 1. Section is already initialized with a LayoutRow when created (in handleAddLayoutItem)
    // 2. initLayoutItemAndChildItems also ensures Section has a LayoutRow
    // 3. Calling appendNewLayoutRow here would add duplicate rows and placeholders when modifying Section properties
    
    // If colSpan changed, update NewItemAddButton in parent row
    // For containers (Section), their col span affects the parent row, so we need to update placeholder buttons
    // IMPORTANT: Find parent row from UPDATED formData to get the row with updated col span values
    const findUpdatedParentRow = (items: any[], targetItem: any): any => {
      for (const item of items) {
        // Check if target item is directly in this row
        if (item.AppFormLayoutItem_List && item.AppFormLayoutItem_List.some((child: any) => 
          child.CurrentHostId === targetItem.CurrentHostId || child.Id === targetItem.Id
        )) {
          return item;
        }
        // Recursively search in child items (sections, etc.)
        if (item.AppFormLayoutItem_List) {
          for (const childItem of item.AppFormLayoutItem_List) {
            if (childItem.AppFormLayoutItem_List) {
              const found = findUpdatedParentRow([childItem], targetItem);
              if (found) return found;
            }
          }
        }
      }
      return null;
    };
    
    const parentRow = findUpdatedParentRow(updatedFormData.AppFormLayoutItemList || [], updatedLayoutItem);
    if (parentRow) {
      // Pass updatedFormData to ensureNewItemAddButton so it uses the updated col span values
      ensureNewItemAddButton(parentRow, updatedFormData);
      
      // Ensure only one placeholder exists
      const placeholderCountAfter = parentRow.AppFormLayoutItem_List?.filter((item: any) => 
        item.DomAttribute && item.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
      ).length || 0;
      
      if (placeholderCountAfter > 1) {
        // Force remove all but one
        let foundFirst = false;
        parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List.filter((item: any) => {
          const isPlaceholder = item.DomAttribute && item.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton;
          if (isPlaceholder) {
            if (!foundFirst) {
              foundFirst = true;
              return true; // Keep the first one
            }
            return false; // Remove the rest
          }
          return true;
        });
      }
    }

    setFormData(updatedFormData);
    setIsModified(true);
  };

  // Helper function to find parent container (Section, TabContainer, etc.) of a layout item
  const findParentContainer = (layoutItem: any, searchInItems?: any[]): any => {
    if (!layoutItem || !layoutItem.ParentHostId) return null;
    
    const itemsToSearch = searchInItems || formData?.AppFormLayoutItemList || [];
    
    const findRecursively = (items: any[]): any => {
      for (const item of items) {
        // Check if this is the parent
        if (item.CurrentHostId === layoutItem.ParentHostId || item.Id === layoutItem.ParentHostId) {
          return item;
        }
        
        // Recursively search in child items
        if (item.AppFormLayoutItem_List && item.AppFormLayoutItem_List.length > 0) {
          const found = findRecursively(item.AppFormLayoutItem_List);
          if (found) return found;
        }
      }
      return null;
    };
    
    return findRecursively(itemsToSearch);
  };

  // Handle remove layout item
  const handleRemoveLayoutItem = (layoutItemToRemove: any) => {
    if (!formData || !layoutItemToRemove) return;

    // Special handling for Tab items (IsTab: true)
    const isTab = layoutItemToRemove.DomAttribute?.IsTab;
    if (isTab) {
      // Find parent TabContainer
      const parentTabContainer = findParentContainer(layoutItemToRemove);
      
      if (parentTabContainer && parentTabContainer.AppFormLayoutItem_List) {
        const remainingTabs = parentTabContainer.AppFormLayoutItem_List.filter((item: any) => 
          item.CurrentHostId !== layoutItemToRemove.CurrentHostId && item.Id !== layoutItemToRemove.Id
        );
        
        // If only one tab remains, don't allow deletion (or delete the entire TabContainer)
        // For now, we'll prevent deletion if it's the last tab
        if (remainingTabs.length === 0) {
          // If this is the last tab, delete the entire TabContainer instead
          const tabContainerParent = findParentContainer(parentTabContainer);
          if (tabContainerParent && tabContainerParent.AppFormLayoutItem_List) {
            tabContainerParent.AppFormLayoutItem_List = tabContainerParent.AppFormLayoutItem_List.filter((item: any) => 
              item.CurrentHostId !== parentTabContainer.CurrentHostId && item.Id !== parentTabContainer.Id
            );
            // Ensure placeholder exists after removal
            ensureNewItemAddButton(tabContainerParent);
          } else {
            // TabContainer is top-level
            formData.AppFormLayoutItemList = formData.AppFormLayoutItemList.filter((item: any) => 
              item.CurrentHostId !== parentTabContainer.CurrentHostId && item.Id !== parentTabContainer.Id
            );
          }
        } else {
          // Remove the tab from TabContainer
          parentTabContainer.AppFormLayoutItem_List = remainingTabs;
          
          // Update active tab if the deleted tab was active
          // Note: activeTabs state is managed in OneLayoutItemDesign, so we'll handle it there
          // For now, just remove the tab - active tab update will be handled by component re-render
        }
      }
    } else {
      // Regular item removal logic
      // Find parent row
      const parentRow = findParentRow(layoutItemToRemove);
      
      if (parentRow && parentRow.AppFormLayoutItem_List) {
        // Remove the item
        parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List.filter((item: any) => 
          item.CurrentHostId !== layoutItemToRemove.CurrentHostId && item.Id !== layoutItemToRemove.Id
        );

        // Ensure NewItemAddButton exists after removal
        ensureNewItemAddButton(parentRow);
      } else {
        // Item might be a top-level row
        formData.AppFormLayoutItemList = formData.AppFormLayoutItemList.filter((item: any) => 
          item.CurrentHostId !== layoutItemToRemove.CurrentHostId && item.Id !== layoutItemToRemove.Id
        );
      }
    }

    // Clear selection if removed item was selected
    if (currentLayoutItem && 
        (currentLayoutItem.CurrentHostId === layoutItemToRemove.CurrentHostId || 
         currentLayoutItem.Id === layoutItemToRemove.Id)) {
      setCurrentLayoutItem(null);
    }

    setFormData({ ...formData });
    setIsModified(true);
  };

  // Helper function to get layout row child items col span info (like AngularJS getLayoutRowChildItemsColSpanInfo)
  const getLayoutRowChildItemsColSpanInfo = (layoutRow: any) => {
    if (layoutRow && layoutRow.DomAttribute && 
        layoutRow.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.LayoutRow) {
      layoutRow.AppFormLayoutItem_List = layoutRow.AppFormLayoutItem_List || [];
      let colSpanInfo: any = {};
      let totalColSpan = 0;
      let maxColSpan = 0;
      let maxColSpanLayoutItem = null;

      layoutRow.AppFormLayoutItem_List.forEach((aLayoutItem: any) => {
        // Exclude placeholder buttons (NewItemAddButton) from col span calculation
        if (aLayoutItem.DomAttribute && 
            aLayoutItem.DomAttribute.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton &&
            aLayoutItem.DomAttribute.ColSpanValue) {
          const colSpan = parseInt(aLayoutItem.DomAttribute.ColSpanValue || 0);
          totalColSpan += colSpan;
          if (colSpan > maxColSpan) {
            maxColSpan = colSpan;
            maxColSpanLayoutItem = aLayoutItem;
          }
        }
      });

      colSpanInfo.totalColSpan = totalColSpan;
      colSpanInfo.maxColSpan = maxColSpan;
      colSpanInfo.maxColSpanLayoutItem = maxColSpanLayoutItem;
      colSpanInfo.totalChildItem = layoutRow.AppFormLayoutItem_List.length;
      return colSpanInfo;
    }

    return null;
  };

  // Helper function to find layout item by CurrentHostId (like AngularJS dictUiIdAndLayoutItem lookup)
  const findLayoutItemByHostId = (hostId: any, searchInItems?: any[], formDataRef?: any): any => {
    if (!hostId) return null;
    
    // Use provided searchInItems, or formDataRef, or formData from state
    const itemsToSearch = searchInItems || formDataRef?.AppFormLayoutItemList || formData?.AppFormLayoutItemList || [];
    
    for (const item of itemsToSearch) {
      if (item.CurrentHostId === hostId) {
        return item;
      }
      
      // Recursively search in child items
      if (item.AppFormLayoutItem_List && item.AppFormLayoutItem_List.length > 0) {
        const found = findLayoutItemByHostId(hostId, item.AppFormLayoutItem_List, formDataRef);
        if (found) return found;
      }
    }
    
    return null;
  };

  // Helper function to get DefaultNbColumns for a row (checks parent Section first, then formData)
  const getDefaultNbColumnsForRow = (parentRow: any, formDataRef?: any): number => {
    let defaultNbColumns = formDataRef?.DefaultNbColumns || formData?.DefaultNbColumns || 3;

    // Check parent section for DefaultNbColumns (like insertNewItemAddButton)
    if (parentRow?.ParentHostId) {
      const parentSection = findLayoutItemByHostId(parentRow.ParentHostId, undefined, formDataRef || formData);
      if (parentSection?.DomAttribute?.DefaultNbColumns) {
        defaultNbColumns = parseInt(parentSection.DomAttribute.DefaultNbColumns) || defaultNbColumns;
      }
    }

    return defaultNbColumns || 2;
  };

  // Handle insert placeholder at specific index (called from insert boundary button click)
  const handleInsertPlaceholderAtIndex = (parentRow: any, insertAtIndex: number) => {
    appHelper.debugLog('handleInsertPlaceholderAtIndex called:', { parentRow: parentRow?.CurrentHostId, insertAtIndex });
    if (!formData || !parentRow) {
      appHelper.debugLog('handleInsertPlaceholderAtIndex: Missing formData or parentRow');
      return;
    }
    
    // Use insertNewItemAddButton to insert placeholder at the specified index
    const newPlaceholder = insertNewItemAddButton(parentRow, formData, insertAtIndex, false);
    
    if (newPlaceholder) {
      appHelper.debugLog('Placeholder inserted successfully:', newPlaceholder.CurrentHostId);
      setFormData({ ...formData });
      setIsModified(true);
    } else {
      appHelper.debugLog('handleInsertPlaceholderAtIndex: Failed to insert placeholder');
    }
  };

  // Handle insert row at specific index (called from horizontal boundary button click)
  const handleInsertRowAtIndex = (insertAtIndex: number) => {
    appHelper.debugLog('handleInsertRowAtIndex called:', insertAtIndex);
    if (!formData) {
      appHelper.debugLog('handleInsertRowAtIndex: Missing formData');
      return;
    }
    
    // Adjust sort orders of existing rows
    formData.AppFormLayoutItemList = formData.AppFormLayoutItemList || [];
    formData.AppFormLayoutItemList.forEach((row: any) => {
      if ((row.FlowOrGridLayoutSortOrder || 0) >= insertAtIndex) {
        row.FlowOrGridLayoutSortOrder = (row.FlowOrGridLayoutSortOrder || 0) + 1;
      }
    });
    
    // Create a new row with the specified sort order
    const newRow: any = {
      FlowOrGridLayoutSortOrder: insertAtIndex,
      AppFormLayoutItem_List: [],
      DomAttribute: {
        WidgetDisplayType: layoutItemTypeEnum?.LayoutRow || 0,
      },
      CurrentHostId: appHelper.randomId(),
    };
    
    // Insert placeholder button in the new row
    insertNewItemAddButton(newRow, formData, undefined, false);
    
    // Add to form data
    formData.AppFormLayoutItemList.push(newRow);
    
    // Sort by FlowOrGridLayoutSortOrder
    formData.AppFormLayoutItemList.sort((a: any, b: any) => 
      (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
    );
    
    setFormData({ ...formData });
    setIsModified(true);
  };

  // Handle insert row at specific index in Section (called from Section horizontal boundary button click)
  const handleInsertRowInSectionAtIndex = (parentSection: any, insertAtIndex: number) => {
    appHelper.debugLog('handleInsertRowInSectionAtIndex called:', { sectionId: parentSection?.CurrentHostId, insertAtIndex });
    if (!formData || !parentSection) {
      appHelper.debugLog('handleInsertRowInSectionAtIndex: Missing formData or parentSection');
      return;
    }
    
    // Ensure parentSection has AppFormLayoutItem_List
    parentSection.AppFormLayoutItem_List = parentSection.AppFormLayoutItem_List || [];
    
    // Adjust sort orders of existing rows in the section
    parentSection.AppFormLayoutItem_List.forEach((row: any) => {
      if ((row.FlowOrGridLayoutSortOrder || 0) >= insertAtIndex) {
        row.FlowOrGridLayoutSortOrder = (row.FlowOrGridLayoutSortOrder || 0) + 1;
      }
    });
    
    // Create a new row with the specified sort order
    const newRow: any = {
      FlowOrGridLayoutSortOrder: insertAtIndex,
      AppFormLayoutItem_List: [],
      DomAttribute: {
        WidgetDisplayType: layoutItemTypeEnum?.LayoutRow || 0,
      },
      CurrentHostId: appHelper.randomId(),
      ParentHostId: parentSection.CurrentHostId,
    };
    
    // Insert placeholder button in the new row
    insertNewItemAddButton(newRow, formData, undefined, false);
    
    // Add to section's row list
    parentSection.AppFormLayoutItem_List.push(newRow);
    
    // Sort by FlowOrGridLayoutSortOrder
    parentSection.AppFormLayoutItem_List.sort((a: any, b: any) => 
      (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
    );
    
    setFormData({ ...formData });
    setIsModified(true);
  };

  // Handle drop on horizontal row boundary
  const handleDropToRowBoundary = (
    event: React.DragEvent,
    insertAtIndex: number
  ) => {
    event.preventDefault();
    event.stopPropagation();

    if (!formData) return;

    // Get drag data (same as handleDropToInsertBoundary)
    let dragTypeData = event.dataTransfer.getData('application/drag-type');
    let dragFieldIdData = event.dataTransfer.getData('application/drag-transaction-field-id');
    let dragGridUnitIdData = event.dataTransfer.getData('application/drag-grid-transaction-unit-id');
    let dragCommandActionIdData = event.dataTransfer.getData('application/drag-command-action-id');
    let dragLinkedSearchIdData = event.dataTransfer.getData('application/drag-linked-search-id');
    let dragLayoutItemUiIdData = event.dataTransfer.getData('application/drag-layout-item-ui-id');
    
    // Try to parse from text/plain as fallback (include layoutItemUiId — host ids are often non-numeric)
    if (event.dataTransfer.getData('text/plain')) {
      try {
        const jsonData = JSON.parse(event.dataTransfer.getData('text/plain'));
        if (!dragTypeData) dragTypeData = jsonData.type?.toString() || '';
        if (!dragFieldIdData) dragFieldIdData = jsonData.transactionFieldId?.toString() || '';
        if (!dragGridUnitIdData) dragGridUnitIdData = jsonData.gridTransactionUnitId?.toString() || '';
        if (!dragCommandActionIdData) dragCommandActionIdData = jsonData.commandActionId?.toString() || '';
        if (!dragLinkedSearchIdData) dragLinkedSearchIdData = jsonData.linkedSearchId?.toString() || '';
        if (!dragLayoutItemUiIdData && jsonData.layoutItemUiId != null) {
          dragLayoutItemUiIdData = String(jsonData.layoutItemUiId);
        }
      } catch (e) {
        console.warn('Failed to parse drag data from text/plain:', e);
      }
    }

    // Fallback to currentDragData state if dataTransfer is empty
    if (!dragTypeData && currentDragData) {
      dragTypeData = currentDragData.itemType?.toString() || '';
      dragFieldIdData = currentDragData.transactionFieldId?.toString() || '';
      dragGridUnitIdData = currentDragData.gridTransactionUnitId?.toString() || '';
      dragCommandActionIdData = currentDragData.commandActionId?.toString() || '';
      dragLinkedSearchIdData = currentDragData.linkedSearchId?.toString() || '';
    }
    if (!dragLayoutItemUiIdData?.trim() && currentDragData?.layoutItemUiId != null) {
      dragLayoutItemUiIdData = String(currentDragData.layoutItemUiId);
    }

    const safeParseInt = (value: string | undefined): number | undefined => {
      if (!value || value.trim() === '') return undefined;
      const parsed = parseInt(value, 10);
      return isNaN(parsed) ? undefined : parsed;
    };

    const itemType = safeParseInt(dragTypeData);
    const transactionFieldId = safeParseInt(dragFieldIdData);
    const gridTransactionUnitId = safeParseInt(dragGridUnitIdData);
    const commandActionId = safeParseInt(dragCommandActionIdData);
    const linkedSearchId = safeParseInt(dragLinkedSearchIdData);
    const layoutItemUiIdRaw = dragLayoutItemUiIdData?.trim() || undefined;
    const layoutItemUiId: string | number | undefined = layoutItemUiIdRaw
      ? (safeParseInt(layoutItemUiIdRaw) ?? layoutItemUiIdRaw)
      : undefined;

    if (layoutItemUiId != null && layoutItemUiId !== '') {
      // Dragging an existing layout item - move it to new row
      const draggedItem = findLayoutItemByHostId(layoutItemUiId);
      if (draggedItem && draggedItem.ParentHostId) {
        const sourceRow = findLayoutItemByHostId(draggedItem.ParentHostId);
        if (sourceRow && sourceRow.AppFormLayoutItem_List) {
          // Remove from source
          const sourceIndex = sourceRow.AppFormLayoutItem_List.findIndex((item: any) => 
            item.CurrentHostId === draggedItem.CurrentHostId ||
            (item.Id && draggedItem.Id && item.Id === draggedItem.Id)
          );
          if (sourceIndex >= 0) {
            const deletedItemColSpan = draggedItem.DomAttribute?.ColSpanValue || 0;
            sourceRow.AppFormLayoutItem_List.splice(sourceIndex, 1);
            cleanupSourceLayoutRowAfterItemRemoved(sourceRow, deletedItemColSpan);
            draggedItem.UIGridLayoutParentID = null;

            // Create new row if needed
            let targetRow = formData.AppFormLayoutItemList[insertAtIndex];
            if (!targetRow) {
              targetRow = appendNewLayoutRow(undefined, formData);
              if (targetRow) {
                targetRow.FlowOrGridLayoutSortOrder = insertAtIndex;
              }
            }
            
            if (targetRow && targetRow.AppFormLayoutItem_List) {
              // Update dragged item properties
              draggedItem.ParentHostId = targetRow.CurrentHostId;
              draggedItem.FlowOrGridLayoutSortOrder = 0;
              
              // Insert into target row
              targetRow.AppFormLayoutItem_List.push(draggedItem);

              const colSpanInfoT = getLayoutRowChildItemsColSpanInfo(targetRow);
              if (colSpanInfoT && colSpanInfoT.totalColSpan > 0 && colSpanInfoT.totalColSpan < 24) {
                ensureNewItemAddButton(targetRow, formData);
              }
              if (targetRow.ParentHostId) {
                const section = findLayoutItemByHostId(targetRow.ParentHostId);
                if (section?.AppFormLayoutItem_List) {
                  const maxSort = Math.max(...section.AppFormLayoutItem_List.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
                  if (maxSort === targetRow.FlowOrGridLayoutSortOrder) appendNewLayoutRow(section);
                }
              } else if (formData?.AppFormLayoutItemList) {
                const maxSort = Math.max(...formData.AppFormLayoutItemList.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
                if (maxSort === targetRow.FlowOrGridLayoutSortOrder) appendNewLayoutRow();
              }

              setFormData({ ...formData });
              setIsModified(true);
            }
          }
        }
      }
    } else if (itemType !== undefined) {
      // Dragging a new item from toolbox - create new row and add item
      formData.AppFormLayoutItemList = formData.AppFormLayoutItemList || [];
      
      // Adjust sort orders
      formData.AppFormLayoutItemList.forEach((row: any) => {
        if ((row.FlowOrGridLayoutSortOrder || 0) >= insertAtIndex) {
          row.FlowOrGridLayoutSortOrder = (row.FlowOrGridLayoutSortOrder || 0) + 1;
        }
      });
      
      // Create new row
      const newRow: any = {
        FlowOrGridLayoutSortOrder: insertAtIndex,
        AppFormLayoutItem_List: [],
        DomAttribute: {
          WidgetDisplayType: layoutItemTypeEnum?.LayoutRow || 0,
        },
        CurrentHostId: appHelper.randomId(),
      };
      
      // Add to form data first
      formData.AppFormLayoutItemList.push(newRow);
      formData.AppFormLayoutItemList.sort((a: any, b: any) => 
        (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
      );
      
      // Add item to new row using handleAddLayoutItem with targetRowIndex
      // We need to find the row after sorting
      const targetRow = formData.AppFormLayoutItemList.find((row: any) => 
        row.CurrentHostId === newRow.CurrentHostId
      );
      
      if (targetRow) {
        // Create the item directly in the new row
        const layoutItemType = layoutItemTypeEnum?.[Object.keys(layoutItemTypeEnum).find(key => 
          layoutItemTypeEnum[key] === itemType
        ) as string];
        
        // Calculate ColSpanValue based on DefaultNbColumns (check Section first, then formData)
        const defaultNbColumns = getDefaultNbColumnsForRow(targetRow, formData);
        const defaultColSpan = Math.round(24 / defaultNbColumns);
        
        const newItem: any = {
          FlowOrGridLayoutSortOrder: 0,
          DomElementTag: layoutItemType || 'Field',
          DisplayTitle: layoutItemType || 'Field',
          DomAttribute: {
            WidgetDisplayType: itemType,
            ColSpanValue: defaultColSpan, // Use calculated colSpan based on DefaultNbColumns
          },
          CurrentHostId: appHelper.randomId(),
          ParentHostId: targetRow.CurrentHostId,
        };
        
        // Set field/unit/linked search properties
        if (transactionFieldId && transactionData?.dictTransactionFieldIdAndDto?.[transactionFieldId]) {
          const field = transactionData.dictTransactionFieldIdAndDto[transactionFieldId];
          // CRITICAL: Use direct reference to dictionary field DTO (not deep copy)
          // This ensures LayoutItem.ForeignAppTransactionFieldExDto always points to the same object in dictionary
          // Changes to dictionary will automatically reflect in layoutItem
          newItem.ForeignAppTransactionFieldExDto = field;
          newItem.DomAttribute.IsBindingToDataField = true;
          // Don't set DomAttribute.DisplayName for field types - use ForeignAppTransactionFieldExDto.DisplayName instead
        }
        
        if (gridTransactionUnitId && transactionData?.dictChildGridTransactionUnitIdAndDto) {
          const unit = transactionData.dictChildGridTransactionUnitIdAndDto[gridTransactionUnitId];
          if (unit) {
            newItem.ForeignAppTransactionUnitExDto = unit;
            newItem.DomAttribute.IsBindingToDataField = true;
            newItem.DomAttribute.DisplayName = unit.DisplayName;
          }
        }
        
        if (commandActionId && transactionData?.dictCommandActionIdAndDto) {
          const command = transactionData.dictCommandActionIdAndDto[commandActionId];
          if (command) {
            newItem.ForeignAppCommandActionExDto = command;
            newItem.DomAttribute.DisplayName = command.DisplayName;
          }
        }
        
        if (linkedSearchId && transactionData?.dictRootLevelUnitLinkedSearchIdAndDto) {
          const linkedSearch = transactionData.dictRootLevelUnitLinkedSearchIdAndDto[linkedSearchId];
          if (linkedSearch) {
            newItem.ForeignAppTransactionUnitLinkedSearchExDto = linkedSearch;
            newItem.DomAttribute.DisplayName = linkedSearch.DisplayName;
          }
        }
        
        targetRow.AppFormLayoutItem_List.push(newItem);
        
        // Ensure placeholder button exists
        insertNewItemAddButton(targetRow, formData, undefined, false);
        
        setFormData({ ...formData });
        setIsModified(true);
      }
    }
  };

  // Handle drop on insert boundary (called when dragging item to insert boundary)
  const handleDropToInsertBoundary = (
    event: React.DragEvent,
    parentRow: any,
    insertAtIndex: number
  ) => {
    event.preventDefault();
    event.stopPropagation();

    if (!formData || !parentRow) return;

    // Get drag data from dataTransfer (same as onDropToNewItemButton)
    let dragTypeData = event.dataTransfer.getData('application/drag-type');
    let dragFieldIdData = event.dataTransfer.getData('application/drag-transaction-field-id');
    let dragGridUnitIdData = event.dataTransfer.getData('application/drag-grid-transaction-unit-id');
    let dragCommandActionIdData = event.dataTransfer.getData('application/drag-command-action-id');
    let dragLinkedSearchIdData = event.dataTransfer.getData('application/drag-linked-search-id');
    let dragLayoutItemUiIdData = event.dataTransfer.getData('application/drag-layout-item-ui-id');
    
    // Try to parse from text/plain as fallback (form field drag sets JSON with layoutItemUiId)
    const textPlain = event.dataTransfer.getData('text/plain');
    if (textPlain) {
      try {
        const jsonData = JSON.parse(textPlain);
        if (!dragTypeData) {
          dragTypeData = jsonData.type?.toString();
          dragFieldIdData = jsonData.transactionFieldId?.toString();
          dragGridUnitIdData = jsonData.gridTransactionUnitId?.toString();
          dragCommandActionIdData = jsonData.commandActionId?.toString();
          dragLinkedSearchIdData = jsonData.linkedSearchId?.toString();
        }
        if (!dragLayoutItemUiIdData && jsonData.layoutItemUiId != null) {
          dragLayoutItemUiIdData = String(jsonData.layoutItemUiId);
        }
      } catch (e) {
        console.warn('Failed to parse drag data from text/plain:', e);
      }
    }

    // Fallback to currentDragData state if dataTransfer is empty
    if (!dragTypeData && currentDragData) {
      dragTypeData = currentDragData.itemType?.toString() || '';
      dragFieldIdData = currentDragData.transactionFieldId?.toString() || '';
      dragGridUnitIdData = currentDragData.gridTransactionUnitId?.toString() || '';
      dragCommandActionIdData = currentDragData.commandActionId?.toString() || '';
      dragLinkedSearchIdData = currentDragData.linkedSearchId?.toString() || '';
    }

    const safeParseInt = (value: string | undefined): number | undefined => {
      if (!value || value === '') return undefined;
      const parsed = parseInt(value, 10);
      return isNaN(parsed) ? undefined : parsed;
    };

    const itemType = safeParseInt(dragTypeData);
    const transactionFieldId = safeParseInt(dragFieldIdData);
    const gridTransactionUnitId = safeParseInt(dragGridUnitIdData);
    const commandActionId = safeParseInt(dragCommandActionIdData);
    const linkedSearchId = safeParseInt(dragLinkedSearchIdData);
    // CurrentHostId can be string (e.g. 'MBWA29') or number; support both for move-existing-item
    const layoutItemUiIdRaw = dragLayoutItemUiIdData?.trim() || undefined;
    const layoutItemUiId: string | number | undefined = layoutItemUiIdRaw
      ? (safeParseInt(layoutItemUiIdRaw) ?? layoutItemUiIdRaw)
      : undefined;

    if (layoutItemUiId != null && layoutItemUiId !== '') {
      // Dragging an existing layout item - move it to new position (align with toolbox-from-left logic, including isSection)
      const draggedItem = findLayoutItemByHostId(layoutItemUiId);
      if (draggedItem && draggedItem.ParentHostId) {
        const sourceRow = findLayoutItemByHostId(draggedItem.ParentHostId);
        if (sourceRow && sourceRow.AppFormLayoutItem_List) {
          const sourceIndex = sourceRow.AppFormLayoutItem_List.findIndex((item: any) =>
            item.CurrentHostId === draggedItem.CurrentHostId ||
            (item.Id && draggedItem.Id && item.Id === draggedItem.Id)
          );
          if (sourceIndex >= 0) {
            const deletedItemColSpan = draggedItem.DomAttribute?.ColSpanValue || 0;
            sourceRow.AppFormLayoutItem_List.splice(sourceIndex, 1);
            cleanupSourceLayoutRowAfterItemRemoved(sourceRow, deletedItemColSpan);
            draggedItem.UIGridLayoutParentID = null;

            const isSection = parentRow.DomAttribute &&
              parentRow.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.Section;

            if (isSection) {
              // Target is Section (contains rows): create new row at insertAtIndex and put item in it (same as toolbox)
              parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List || [];
              parentRow.AppFormLayoutItem_List.forEach((row: any) => {
                if ((row.FlowOrGridLayoutSortOrder || 0) >= insertAtIndex) {
                  row.FlowOrGridLayoutSortOrder = (row.FlowOrGridLayoutSortOrder || 0) + 1;
                }
              });
              const newRow: any = {
                FlowOrGridLayoutSortOrder: insertAtIndex,
                AppFormLayoutItem_List: [],
                DomAttribute: { WidgetDisplayType: layoutItemTypeEnum?.LayoutRow || 0 },
                CurrentHostId: appHelper.randomId(),
                ParentHostId: parentRow.CurrentHostId,
              };
              parentRow.AppFormLayoutItem_List.push(newRow);
              parentRow.AppFormLayoutItem_List.sort((a: any, b: any) =>
                (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
              );
              draggedItem.ParentHostId = newRow.CurrentHostId;
              draggedItem.FlowOrGridLayoutSortOrder = 0;
              newRow.AppFormLayoutItem_List.push(draggedItem);

              const colSpanInfoNew = getLayoutRowChildItemsColSpanInfo(newRow);
              if (colSpanInfoNew && colSpanInfoNew.totalColSpan > 0 && colSpanInfoNew.totalColSpan < 24) {
                ensureNewItemAddButton(newRow, formData);
              }
              if (parentRow.AppFormLayoutItem_List) {
                const maxSort = Math.max(...parentRow.AppFormLayoutItem_List.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
                if (maxSort === newRow.FlowOrGridLayoutSortOrder) appendNewLayoutRow(parentRow);
              }
            } else {
              // Target is LayoutRow (contains items): insert item into parentRow (same as toolbox)
              parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List || [];
              parentRow.AppFormLayoutItem_List.forEach((item: any) => {
                if ((item.FlowOrGridLayoutSortOrder || 0) >= insertAtIndex) {
                  item.FlowOrGridLayoutSortOrder = (item.FlowOrGridLayoutSortOrder || 0) + 1;
                }
              });
              draggedItem.ParentHostId = parentRow.CurrentHostId;
              draggedItem.FlowOrGridLayoutSortOrder = insertAtIndex;
              parentRow.AppFormLayoutItem_List.push(draggedItem);
              parentRow.AppFormLayoutItem_List.sort((a: any, b: any) =>
                (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
              );

              const colSpanInfoRow = getLayoutRowChildItemsColSpanInfo(parentRow);
              if (colSpanInfoRow && colSpanInfoRow.totalColSpan > 0 && colSpanInfoRow.totalColSpan < 24) {
                ensureNewItemAddButton(parentRow, formData);
              }
              if (parentRow.ParentHostId) {
                const section = findLayoutItemByHostId(parentRow.ParentHostId);
                if (section?.AppFormLayoutItem_List) {
                  const maxSort = Math.max(...section.AppFormLayoutItem_List.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
                  if (maxSort === parentRow.FlowOrGridLayoutSortOrder) appendNewLayoutRow(section);
                }
              } else if (formData?.AppFormLayoutItemList) {
                const maxSort = Math.max(...formData.AppFormLayoutItemList.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
                if (maxSort === parentRow.FlowOrGridLayoutSortOrder) appendNewLayoutRow();
              }
            }

            setFormData({ ...formData });
            setIsModified(true);
          }
        }
      }
    } else if (itemType !== undefined) {
      // Dragging a new item from toolbox - insert at index
      // Check if parentRow is a Section (contains LayoutRows, not items)
      const isSection = parentRow.DomAttribute && 
                        parentRow.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.Section;
      
      if (isSection) {
        // For Section, we need to create a new row first, then add item to it
        parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List || [];
        
        // Adjust sort orders of existing rows
        parentRow.AppFormLayoutItem_List.forEach((row: any) => {
          if ((row.FlowOrGridLayoutSortOrder || 0) >= insertAtIndex) {
            row.FlowOrGridLayoutSortOrder = (row.FlowOrGridLayoutSortOrder || 0) + 1;
          }
        });
        
        // Create new row with correct sort order
        const newRow: any = {
          FlowOrGridLayoutSortOrder: insertAtIndex,
          AppFormLayoutItem_List: [],
          DomAttribute: {
            WidgetDisplayType: layoutItemTypeEnum?.LayoutRow || 0,
          },
          CurrentHostId: appHelper.randomId(),
          ParentHostId: parentRow.CurrentHostId,
        };
        
        // Add row to Section
        parentRow.AppFormLayoutItem_List.push(newRow);
        parentRow.AppFormLayoutItem_List.sort((a: any, b: any) => 
          (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
        );
        
        // Now add item to the new row
        const layoutItemType = layoutItemTypeEnum?.[Object.keys(layoutItemTypeEnum).find(key => 
          layoutItemTypeEnum[key] === itemType
        ) as string];
        
        // Calculate ColSpanValue based on DefaultNbColumns (check Section first, then formData)
        const defaultNbColumns = getDefaultNbColumnsForRow(newRow, formData);
        const defaultColSpan = Math.round(24 / defaultNbColumns);
        
        const newItem: any = {
          FlowOrGridLayoutSortOrder: 0,
          DomElementTag: layoutItemType || 'Field',
          DisplayTitle: layoutItemType || 'Field',
          DomAttribute: {
            WidgetDisplayType: itemType,
            ColSpanValue: defaultColSpan, // Use calculated colSpan based on DefaultNbColumns
          },
          CurrentHostId: appHelper.randomId(),
          ParentHostId: newRow.CurrentHostId,
        };
        
        // Set field-specific properties
        if (transactionFieldId && transactionData?.dictTransactionFieldIdAndDto) {
          const field = transactionData.dictTransactionFieldIdAndDto[transactionFieldId];
          if (field) {
            newItem.ForeignAppTransactionFieldExDto = field;
            newItem.DomAttribute.IsBindingToDataField = true;
          }
        }
        
        if (gridTransactionUnitId && transactionData?.dictChildGridTransactionUnitIdAndDto) {
          const unit = transactionData.dictChildGridTransactionUnitIdAndDto[gridTransactionUnitId];
          if (unit) {
            newItem.ForeignAppTransactionUnitExDto = unit;
            newItem.DomAttribute.IsBindingToDataField = true;
            newItem.DomAttribute.DisplayName = unit.DisplayName;
          }
        }
        
        if (commandActionId && transactionData?.AppTransactionData?.CommandActionList) {
          const command = transactionData.AppTransactionData.CommandActionList.find((cmd: any) => cmd.Id === commandActionId);
          if (command) {
            newItem.DomAttribute.DisplayName = command.Name;
          }
        }
        
        if (linkedSearchId) {
          // Find linked search from transaction data
          if (transactionData?.AppTransactionData?.AppTransactionUnitList) {
            for (const unit of transactionData.AppTransactionData.AppTransactionUnitList) {
              if (unit.AppTransactionUnitLinkedSearchList) {
                const linkedSearch = unit.AppTransactionUnitLinkedSearchList.find((ls: any) => ls.Id === linkedSearchId);
                if (linkedSearch) {
                  newItem.DomAttribute.DisplayName = linkedSearch.Name;
                  break;
                }
              }
            }
          }
        }
        
        // Add item to new row
        newRow.AppFormLayoutItem_List.push(newItem);
        
        setFormData({ ...formData });
        setIsModified(true);
      } else {
        // For regular LayoutRow, insert item directly into parentRow
        parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List || [];
        
        // Adjust sort orders of existing items
        parentRow.AppFormLayoutItem_List.forEach((item: any) => {
          if ((item.FlowOrGridLayoutSortOrder || 0) >= insertAtIndex) {
            item.FlowOrGridLayoutSortOrder = (item.FlowOrGridLayoutSortOrder || 0) + 1;
          }
        });
        
        // Remove existing NewItemAddButton before adding new item
        const placeholderIndex = parentRow.AppFormLayoutItem_List.findIndex((item: any) => 
          item.DomAttribute?.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
        );
        if (placeholderIndex >= 0) {
          parentRow.AppFormLayoutItem_List.splice(placeholderIndex, 1);
        }
        
        // Create new layout item
        const layoutItemType = layoutItemTypeEnum?.[Object.keys(layoutItemTypeEnum).find(key => 
          layoutItemTypeEnum[key] === itemType
        ) as string];
        
        // Calculate ColSpanValue based on DefaultNbColumns (check Section first, then formData)
        const defaultNbColumns = getDefaultNbColumnsForRow(parentRow, formData);
        const defaultColSpan = Math.round(24 / defaultNbColumns);
        
        const newItem: any = {
          FlowOrGridLayoutSortOrder: insertAtIndex,
          DomElementTag: layoutItemType || 'Field',
          DisplayTitle: layoutItemType || 'Field',
          DomAttribute: {
            WidgetDisplayType: itemType,
            ColSpanValue: defaultColSpan, // Use calculated colSpan based on DefaultNbColumns
          },
          CurrentHostId: appHelper.randomId(),
          ParentHostId: parentRow.CurrentHostId,
        };
        
        // Set field-specific properties
        if (transactionFieldId && transactionData?.dictTransactionFieldIdAndDto) {
          const field = transactionData.dictTransactionFieldIdAndDto[transactionFieldId];
          if (field) {
            // CRITICAL: Use direct reference to dictionary field DTO (not deep copy)
            newItem.ForeignAppTransactionFieldExDto = field;
            newItem.DomAttribute.IsBindingToDataField = true;
          }
        }
        
        if (gridTransactionUnitId && transactionData?.dictChildGridTransactionUnitIdAndDto) {
          const unit = transactionData.dictChildGridTransactionUnitIdAndDto[gridTransactionUnitId];
          if (unit) {
            newItem.ForeignAppTransactionUnitExDto = unit;
            newItem.DomAttribute.IsBindingToDataField = true;
            newItem.DomAttribute.DisplayName = unit.DisplayName;
          }
        }
        
        if (commandActionId && transactionData?.AppTransactionData?.CommandActionList) {
          const command = transactionData.AppTransactionData.CommandActionList.find((cmd: any) => cmd.Id === commandActionId);
          if (command) {
            newItem.DomAttribute.DisplayName = command.Name;
          }
        }
        
        if (linkedSearchId) {
          // Find linked search from transaction data
          if (transactionData?.AppTransactionData?.AppTransactionUnitList) {
            for (const unit of transactionData.AppTransactionData.AppTransactionUnitList) {
              if (unit.AppTransactionUnitLinkedSearchList) {
                const linkedSearch = unit.AppTransactionUnitLinkedSearchList.find((ls: any) => ls.Id === linkedSearchId);
                if (linkedSearch) {
                  newItem.DomAttribute.DisplayName = linkedSearch.Name;
                  break;
                }
              }
            }
          }
        }
        
        // Insert item into parentRow
        parentRow.AppFormLayoutItem_List.push(newItem);
        parentRow.AppFormLayoutItem_List.sort((a: any, b: any) => 
          (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
        );
        
        setFormData({ ...formData });
        setIsModified(true);
      }
    }
  };

  // Helper function to insert new item add button (placeholder) - like AngularJS insertNewItemAddButton
  const insertNewItemAddButton = (parentRow: any, formDataRef: any, insertAtIndex?: number, isAllowShrinkOtherItems: boolean = false) => {
    if (parentRow && parentRow.DomAttribute &&
        parentRow.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.LayoutRow) {
      parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List || [];
      
      // CRITICAL: Check if placeholder already exists - if so, don't add another one
      // EXCEPTION: If insertAtIndex is explicitly provided (from boundary click), allow inserting at specific position
      const existingPlaceholders = parentRow.AppFormLayoutItem_List.filter((item: any) => 
        item.DomAttribute && item.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
      );
      if (existingPlaceholders.length > 0 && insertAtIndex === undefined) {
        // Only block if insertAtIndex is not specified (default behavior: only one placeholder at end)
        // If insertAtIndex is specified, allow inserting at specific position even if placeholder exists
        return null;
      }

      let defaultNbColumns = formDataRef?.DefaultNbColumns || 3;

      // Check parent section for DefaultNbColumns (like AngularJS line 695-702)
      if (parentRow.ParentHostId && formDataRef) {
        const parentSection = findLayoutItemByHostId(parentRow.ParentHostId, undefined, formDataRef);
        if (parentSection) {
          if (parentSection.DomAttribute && parentSection.DomAttribute.DefaultNbColumns) {
            defaultNbColumns = parseInt(parentSection.DomAttribute.DefaultNbColumns) || defaultNbColumns;
          }
        }
      }

      defaultNbColumns = defaultNbColumns || 2;

      let colSpanInfo = getLayoutRowChildItemsColSpanInfo(parentRow);

      if (colSpanInfo) {
        let totalColSpan = colSpanInfo.totalColSpan;
        let maxColSpan = colSpanInfo.maxColSpan;
        let maxColSpanLayoutItem = colSpanInfo.maxColSpanLayoutItem;

        if (totalColSpan >= 0 && totalColSpan <= 24) {
          let newAddBtnColSpan = 0;

          if (totalColSpan === 0) {
            newAddBtnColSpan = Math.round(24 / defaultNbColumns);
          } else if (totalColSpan >= 0 && totalColSpan <= 23) {
            if ((24 - totalColSpan) > Math.round(24 / defaultNbColumns)) {
              newAddBtnColSpan = Math.round(24 / defaultNbColumns);
            } else {
              newAddBtnColSpan = 24 - totalColSpan;
            }
          } else if (totalColSpan === 24) {
            // If insertAtIndex is explicitly provided (from boundary click), allow inserting even when totalColSpan is 24
            if (insertAtIndex !== undefined) {
              // When inserting at specific index, shrink the largest item to make room
              if (maxColSpanLayoutItem && maxColSpan) {
                newAddBtnColSpan = Math.max(1, Math.round(maxColSpan / 2)); // At least 1, or half of maxColSpan
                maxColSpanLayoutItem.DomAttribute.ColSpanValue = maxColSpanLayoutItem.DomAttribute.ColSpanValue - newAddBtnColSpan;
              } else {
                // Fallback: use default column span
                newAddBtnColSpan = Math.round(24 / defaultNbColumns);
              }
            } else if (isAllowShrinkOtherItems) {
              if (maxColSpanLayoutItem && maxColSpan) {
                newAddBtnColSpan = maxColSpan / 2;
                maxColSpanLayoutItem.DomAttribute.ColSpanValue = maxColSpanLayoutItem.DomAttribute.ColSpanValue - newAddBtnColSpan;
              }
            }
          }

          if (newAddBtnColSpan > 0) {
            let maxItemSort = 0;

            parentRow.AppFormLayoutItem_List.forEach((layoutItem: any) => {
              if (layoutItem.FlowOrGridLayoutSortOrder) {
                if (layoutItem.FlowOrGridLayoutSortOrder > maxItemSort) {
                  maxItemSort = layoutItem.FlowOrGridLayoutSortOrder;
                }

                if (insertAtIndex !== undefined && layoutItem.FlowOrGridLayoutSortOrder >= insertAtIndex) {
                  layoutItem.FlowOrGridLayoutSortOrder += 1;
                }
              }
            });

            if (insertAtIndex === undefined) {
              insertAtIndex = maxItemSort + 1;
            }

            let NewItemAddButton: any = {
              FlowOrGridLayoutSortOrder: insertAtIndex,
              DomElementTag: 'Add',
              DisplayTitle: 'Add',
              DomAttribute: {
                ColSpanValue: newAddBtnColSpan,
                DisplayName: '+',
                WidgetDisplayType: layoutItemTypeEnum?.NewItemAddButton || 0,
              },
              CurrentHostId: appHelper.randomId(), // Generate unique ID
              ParentHostId: parentRow.CurrentHostId,
            };

            parentRow.AppFormLayoutItem_List.push(NewItemAddButton);

            return NewItemAddButton;
          }
        }
      }
    }

    return null;
  };

  // Helper function to append new LayoutTab to TabContainer (like AngularJS appendNewLayoutTab)
  const appendNewLayoutTab = (tabContainer: any, defaultTabName?: string, formDataRef?: any) => {
    if (!tabContainer) {
      return null;
    }

    const tabList = tabContainer.AppFormLayoutItem_List = tabContainer.AppFormLayoutItem_List || [];
    const currentForm = formDataRef || formData;

    // Find max sort order (like AngularJS appHelper.findMaxValueFromArray)
    const maxSort = tabList.length > 0
      ? Math.max(...tabList.map((item: any) => item.FlowOrGridLayoutSortOrder || 0))
      : 0;

    const newLayoutTab: any = {
      FlowOrGridLayoutSortOrder: maxSort + 1,
      AppFormLayoutItem_List: [],
      DomAttribute: {
        WidgetDisplayType: layoutItemTypeEnum?.Section || 0,
        DisplayName: defaultTabName || 'New Tab',
        IsBindingToDataField: false,
        DefaultNbColumns: 2,
        IsTab: true,
        BackgroundColor: '#ffffff',
        TextColor: '#000000',
      },
      CurrentHostId: appHelper.randomId(),
      ParentHostId: tabContainer.CurrentHostId,
    };

    // Add Stack Container (Section) inside the Tab
    const stackContainer: any = {
      FlowOrGridLayoutSortOrder: 1,
      AppFormLayoutItem_List: [],
      DomAttribute: {
        WidgetDisplayType: layoutItemTypeEnum?.Section || 0,
        DisplayName: 'Stack Container',
        IsBindingToDataField: false,
        DefaultNbColumns: 2,
        IsTab: false, // This is a regular Section, not a Tab
        BackgroundColor: '#ffffff',
        TextColor: '#000000',
      },
      CurrentHostId: appHelper.randomId(),
      ParentHostId: newLayoutTab.CurrentHostId,
    };

    // Add LayoutRow to Stack Container (this will create one LayoutRow with one Placeholder)
    appendNewLayoutRow(stackContainer, currentForm);
    
    // Initialize the LayoutRow to ensure it's properly set up
    const stackContainerLayoutRow = stackContainer.AppFormLayoutItem_List && stackContainer.AppFormLayoutItem_List.length > 0
      ? stackContainer.AppFormLayoutItem_List[0]
      : null;
    
    if (stackContainerLayoutRow) {
      // Initialize the LayoutRow, but skip adding placeholder since appendNewLayoutRow already added one
      // We only need to set up the LayoutRow's properties, not add another placeholder
      if (!stackContainerLayoutRow.CurrentHostId) {
        stackContainerLayoutRow.CurrentHostId = appHelper.randomId();
      }
      stackContainerLayoutRow.ParentHostId = stackContainer.CurrentHostId;
      stackContainerLayoutRow.AppFormLayoutItem_List = stackContainerLayoutRow.AppFormLayoutItem_List || [];
      
      // Ensure the placeholder fills the entire Stack Container (24 columns)
      const placeholder = stackContainerLayoutRow.AppFormLayoutItem_List.find((item: any) => 
        item.DomAttribute && item.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
      );
      if (placeholder) {
        placeholder.DomAttribute.ColSpanValue = 24; // Fill entire width
      }
      
      // Don't call initLayoutItemAndChildItems on LayoutRow because it would try to add another placeholder
      // The placeholder was already added by appendNewLayoutRow
    }
    
    // Set Stack Container properties (but don't call initLayoutItemAndChildItems on Stack Container itself,
    // because it would try to add another LayoutRow since initLayoutItemAndChildItems checks if Section has LayoutRow)
    if (!stackContainer.CurrentHostId) {
      stackContainer.CurrentHostId = appHelper.randomId();
    }
    stackContainer.ParentHostId = newLayoutTab.CurrentHostId;
    stackContainer.AppFormLayoutItem_List = stackContainer.AppFormLayoutItem_List || [];

    // Add Stack Container to Tab
    newLayoutTab.AppFormLayoutItem_List.push(stackContainer);

    // Initialize the tab (but don't add LayoutRow directly to Tab, as it now has Stack Container)
    // Also, don't recursively initialize Stack Container and its LayoutRow, as they are already set up
    // We only need to set up the Tab's properties
    if (!newLayoutTab.CurrentHostId) {
      newLayoutTab.CurrentHostId = appHelper.randomId();
    }
    newLayoutTab.ParentHostId = tabContainer.CurrentHostId;
    newLayoutTab.AppFormLayoutItem_List = newLayoutTab.AppFormLayoutItem_List || [];
    
    // Don't call initLayoutItemAndChildItems on Tab because it would recursively process Stack Container
    // and its LayoutRow, which might add another placeholder

    tabList.push(newLayoutTab);

    return newLayoutTab;
  };

  // Helper function to append new LayoutRow (like AngularJS appendNewLayoutRow)
  // If parentSection is provided, appends to parentSection.AppFormLayoutItem_List
  // If parentSection is null/undefined, appends to formData.AppFormLayoutItemList (top level)
  const appendNewLayoutRow = (parentSection?: any, formDataRef?: any) => {
    let layoutRowList = null;
    let currentForm = formDataRef || formData;

    if (!currentForm) {
      return null;
    }

    if (!parentSection) {
      // Append to top-level form layout item list
      layoutRowList = currentForm.AppFormLayoutItemList = currentForm.AppFormLayoutItemList || [];
    } else {
      // Append to parent section's layout item list
      layoutRowList = parentSection.AppFormLayoutItem_List = parentSection.AppFormLayoutItem_List || [];
    }

    // Find max sort order (like AngularJS appHelper.findMaxValueFromArray)
    const maxSort = layoutRowList.length > 0
      ? Math.max(...layoutRowList.map((item: any) => item.FlowOrGridLayoutSortOrder || 0))
      : 0;

    const newLayoutRow: any = {
      FlowOrGridLayoutSortOrder: maxSort + 1,
      AppFormLayoutItem_List: [],
      DomAttribute: {
        WidgetDisplayType: layoutItemTypeEnum?.LayoutRow || 0,
      },
      CurrentHostId: appHelper.randomId(),
      ParentHostId: parentSection ? parentSection.CurrentHostId : null,
    };

    // Call insertNewItemAddButton BEFORE pushing (like AngularJS)
    const newItemButton = insertNewItemAddButton(newLayoutRow, currentForm, undefined, false);
    
    layoutRowList.push(newLayoutRow);

    // Note: AngularJS also updates dictUiIdAndLayoutItem, but we don't need that in React

    return newItemButton;
  };

  // Recursive function to initialize layout items and add placeholders - like AngularJS initLayoutItemAndChildItems
  // This function processes a single layoutItem (can be a LayoutRow, Section, etc.) and recursively processes its children
  const initLayoutItemAndChildItems = (layoutItem: any, parentItem?: any, formDataRef?: any) => {
    if (!layoutItem) return;

    // Ensure CurrentHostId exists
    if (!layoutItem.CurrentHostId) {
      layoutItem.CurrentHostId = appHelper.randomId();
    }

    if (parentItem) {
      layoutItem.ParentHostId = parentItem.CurrentHostId;
    }

    // IMPORTANT: If ForeignAppTransactionFieldExDto is missing but we have TransactionFieldId, rebuild it from transactionData
    // ForeignAppTransactionFieldExDto.DisplayName is the single source of truth for display names
    if (layoutItem.TransactionFieldId && layoutItem.DomAttribute?.IsBindingToDataField) {
      // CRITICAL: Ensure ForeignAppTransactionFieldExDto always points to dictionary field DTO
      // If missing or not pointing to dictionary, set it to dictionary reference
      if (transactionData?.dictTransactionFieldIdAndDto?.[layoutItem.TransactionFieldId]) {
        const fieldDto = transactionData.dictTransactionFieldIdAndDto[layoutItem.TransactionFieldId];
        // Always use direct reference to dictionary (not deep copy)
        // This ensures synchronization between layoutItem and dictionary
        layoutItem.ForeignAppTransactionFieldExDto = fieldDto;
      }
    }

    layoutItem.AppFormLayoutItem_List = layoutItem.AppFormLayoutItem_List || [];

    // Recursively process child items first (like AngularJS line 795-799)
    if (layoutItem.AppFormLayoutItem_List.length > 0) {
      layoutItem.AppFormLayoutItem_List.forEach((childLayoutItem: any) => {
        initLayoutItemAndChildItems(childLayoutItem, layoutItem, formDataRef);
      });
    }

    // Add placeholder to LayoutRow items (like AngularJS line 831-837)
    if (layoutItem.DomAttribute && 
        layoutItem.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.LayoutRow) {
      let colSpanInfo = getLayoutRowChildItemsColSpanInfo(layoutItem);
      if (colSpanInfo) {
        if (colSpanInfo.totalColSpan >= 0 && colSpanInfo.totalColSpan <= 23) {
          insertNewItemAddButton(layoutItem, formDataRef);
        }
      }
    }

    // Append new LayoutRow to Section if it doesn't have one (like AngularJS)
    // BUT: Don't add LayoutRow for Tab items (IsTab: true) - they are handled in appendNewLayoutTab
    if (layoutItem.DomAttribute && 
        layoutItem.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.Section &&
        !layoutItem.DomAttribute.IsTab) {
      appendNewLayoutRow(layoutItem, formDataRef);
    }

    // Set activeTab for TabContainer (like AngularJS)
    if (layoutItem.DomAttribute && 
        layoutItem.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.TabContainer) {
      if (layoutItem.AppFormLayoutItem_List && layoutItem.AppFormLayoutItem_List.length > 0) {
        // Set activeTab to the first item in the list
        layoutItem.activeTab = layoutItem.AppFormLayoutItem_List[0];
      }
    }
  };

  // Helper function to remove NewItemAddButton from a row (like AngularJS)
  const removeNewItemAddButton = (parentRow: any) => {
    if (parentRow && parentRow.AppFormLayoutItem_List) {
      parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List.filter((item: any) => 
        !(item.DomAttribute && item.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton)
      );
    }
  };

  // Helper function to ensure NewItemAddButton exists in LayoutRow (like AngularJS)
  const ensureNewItemAddButton = (parentRow: any, formDataRef?: any) => {
    if (parentRow && parentRow.DomAttribute && 
        parentRow.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.LayoutRow) {
      // CRITICAL: Remove ALL existing NewItemAddButton first - do this before calculating col span
      removeNewItemAddButton(parentRow);
      
      // Verify removal was successful - check again after removal
      const placeholderCountAfter = parentRow.AppFormLayoutItem_List?.filter((item: any) => 
        item.DomAttribute && item.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
      ).length || 0;
      
      if (placeholderCountAfter > 0) {
        // Force remove again - this should not happen, but just in case
        parentRow.AppFormLayoutItem_List = parentRow.AppFormLayoutItem_List.filter((item: any) => 
          !(item.DomAttribute && item.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton)
        );
      }
      
      // Then add it back if needed
      // Use the provided formDataRef or fall back to formData
      const dataToUse = formDataRef || formData;
      
      // IMPORTANT: Get col span info from the parentRow reference (which should be from updatedFormData)
      // The parentRow passed in should already have the updated col span values
      // After removing placeholder, recalculate col span info
      let colSpanInfo = getLayoutRowChildItemsColSpanInfo(parentRow);
      if (colSpanInfo) {
        if (colSpanInfo.totalColSpan >= 0 && colSpanInfo.totalColSpan <= 23) {
          // Final check: ensure no placeholder exists before inserting
          const finalPlaceholderCheck = parentRow.AppFormLayoutItem_List?.some((item: any) => 
            item.DomAttribute && item.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
          );
          if (!finalPlaceholderCheck) {
            insertNewItemAddButton(parentRow, dataToUse);
          }
        }
      }
    }
  };

  /** After removing an item from a LayoutRow: drop empty rows (matches placeholder-drop / save), or merge col span into placeholder. */
  const cleanupSourceLayoutRowAfterItemRemoved = (srcLayoutRow: any, deletedItemColSpan: number) => {
    if (!srcLayoutRow?.AppFormLayoutItem_List) return;

    const remainingItems = srcLayoutRow.AppFormLayoutItem_List.filter((item: any) =>
      item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton
    );
    if (remainingItems.length === 0) {
      if (srcLayoutRow.ParentHostId) {
        const parentSection = findLayoutItemByHostId(srcLayoutRow.ParentHostId);
        if (parentSection?.AppFormLayoutItem_List) {
          const rowIndex = parentSection.AppFormLayoutItem_List.indexOf(srcLayoutRow);
          if (rowIndex >= 0) parentSection.AppFormLayoutItem_List.splice(rowIndex, 1);
        }
      } else if (formData?.AppFormLayoutItemList) {
        const rowIndex = formData.AppFormLayoutItemList.indexOf(srcLayoutRow);
        if (rowIndex >= 0) formData.AppFormLayoutItemList.splice(rowIndex, 1);
      }
    } else {
      const placeholderButtons = srcLayoutRow.AppFormLayoutItem_List.filter((item: any) =>
        item.DomAttribute?.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
      );
      if (placeholderButtons.length > 0) {
        placeholderButtons[0].DomAttribute = placeholderButtons[0].DomAttribute || {};
        placeholderButtons[0].DomAttribute.ColSpanValue =
          (placeholderButtons[0].DomAttribute.ColSpanValue || 0) + deletedItemColSpan;
      } else {
        ensureNewItemAddButton(srcLayoutRow);
      }
    }
  };

  // Helper function to find or create target row for adding items
  const findOrCreateTargetRow = (targetRowIndex?: number): any => {
    if (!formData) return null;

    formData.AppFormLayoutItemList = formData.AppFormLayoutItemList || [];

    // If targetRowIndex is specified and valid, use that row
    if (targetRowIndex !== undefined && targetRowIndex >= 0 && targetRowIndex < formData.AppFormLayoutItemList.length) {
      return formData.AppFormLayoutItemList[targetRowIndex];
    }

    // Find the last LayoutRow or create a new one
    let targetRow = formData.AppFormLayoutItemList.find((row: any) => 
      row.DomAttribute && row.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.LayoutRow
    );

    if (!targetRow) {
      // Create a new LayoutRow
      const maxSortOrder = formData.AppFormLayoutItemList.length > 0
        ? Math.max(...formData.AppFormLayoutItemList.map((r: any) => r.FlowOrGridLayoutSortOrder || 0))
        : 0;

      targetRow = {
        FlowOrGridLayoutSortOrder: maxSortOrder + 1,
        DomElementTag: 'LayoutRow',
        DisplayTitle: 'Layout Row',
        DomAttribute: {
          WidgetDisplayType: layoutItemTypeEnum?.LayoutRow || 0,
        },
        CurrentHostId: appHelper.randomId(),
        AppFormLayoutItem_List: [],
      };

      formData.AppFormLayoutItemList.push(targetRow);
    }

    return targetRow;
  };

  // Helper function to convert placeholder button to actual layout item (like AngularJS convertBlankLayoutItemToControl)
  // Returns true if conversion was completed, false if dialog was opened (for TableContainer)
  const convertBlankLayoutItemToControl = (
    layoutItem: any,
    itemType: number,
    transactionFieldId?: number,
    gridTransactionUnitId?: number,
    commandActionId?: number,
    linkedSearchId?: number
  ): boolean => {
    if (!layoutItem || !layoutItem.DomAttribute || 
        layoutItem.DomAttribute.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton) {
      return false;
    }

    // If adding TableContainer, open dialog and return false (conversion will happen in dialog's onConfirm)
    if (itemType === layoutItemTypeEnum?.TableContainer) {
      // Store the placeholder layoutItem for later conversion
      setPendingTableContainer(layoutItem);
      setShowTableContainerDialog(true);
      return false; // Indicate that conversion was deferred
    }

    // Preserve the ColSpanValue from the placeholder
    const preservedColSpanValue = layoutItem.DomAttribute.ColSpanValue;
    
    // Convert the placeholder to the actual item type
    layoutItem.DomAttribute.WidgetDisplayType = itemType;
    layoutItem.DomAttribute.BackgroundColor = '#ffffff';
    layoutItem.DomAttribute.TextColor = '#000000';
    layoutItem.TransactionFieldId = transactionFieldId ?? null;
    layoutItem.GridTransactionUnitId = gridTransactionUnitId ?? null;
    
    // Preserve the ColSpanValue (don't override it)
    if (preservedColSpanValue) {
      layoutItem.DomAttribute.ColSpanValue = preservedColSpanValue;
    } else {
      // If no ColSpanValue was set, use default based on form settings
      const defaultColSpan = formData?.DefaultNbColumns ? Math.round(24 / formData.DefaultNbColumns) : 8;
      layoutItem.DomAttribute.ColSpanValue = defaultColSpan;
    }

    // Set default height values based on item type
    if (itemType === layoutItemTypeEnum?.Grid ||
        itemType === layoutItemTypeEnum?.Image ||
        itemType === layoutItemTypeEnum?.Video ||
        itemType === layoutItemTypeEnum?.SearchAndView) {
      layoutItem.DomAttribute.HeightValue = 400;
    } else if (itemType === layoutItemTypeEnum?.Space) {
      layoutItem.DomAttribute.HeightValue = 40;
    } else if (itemType === layoutItemTypeEnum?.Memo) {
      layoutItem.DomAttribute.HeightValue = 150;
    } else if (itemType === layoutItemTypeEnum?.Content) {
      layoutItem.DomAttribute.HeightValue = null;
    } else if (itemType === layoutItemTypeEnum?.Section) {
      layoutItem.DomAttribute.DefaultNbColumns = 1;
    } else if (itemType === layoutItemTypeEnum?.CommandActionButton) {
      layoutItem.DomAttribute.HeightValue = null;
    } else if (itemType === layoutItemTypeEnum?.HtmlContentContainer) {
      layoutItem.DomAttribute.HeightValue = 400;
    }

    // Handle specific item types
    if (itemType === layoutItemTypeEnum?.Grid) {
      layoutItem.DomAttribute.TranscationUnitLevel = 2;
      layoutItem.DomAttribute.IsBindingToDataField = true;
      layoutItem.AppFormLayoutItem_List = [];

      if (gridTransactionUnitId && transactionData?.dictChildGridTransactionUnitIdAndDto?.[gridTransactionUnitId]) {
        const gridUnitDto = transactionData.dictChildGridTransactionUnitIdAndDto[gridTransactionUnitId];
        layoutItem.DomAttribute.DisplayName = gridUnitDto.UnitDisplayName;
      } else {
        layoutItem.DomAttribute.DisplayName = 'New Grid';
      }
    } else if (itemType === layoutItemTypeEnum?.Section) {
      layoutItem.DomAttribute.DisplayName = 'Stack Container';
      layoutItem.AppFormLayoutItem_List = [];
      layoutItem.DomAttribute.IsBindingToDataField = false;
      appendNewLayoutRow(layoutItem);
    } else if (itemType === layoutItemTypeEnum?.TabContainer) {
      layoutItem.DomAttribute.DisplayName = 'Tab Container';
      layoutItem.AppFormLayoutItem_List = [];
      layoutItem.DomAttribute.IsBindingToDataField = false;
      
      // Create two default tabs (like AngularJS line 1443-1446)
      const newLayoutTab1 = appendNewLayoutTab(layoutItem, 'Tab1', formData);
      if (newLayoutTab1) {
        layoutItem.activeTab = newLayoutTab1;
      }
      appendNewLayoutTab(layoutItem, 'Tab2', formData);
    } else if (itemType === layoutItemTypeEnum?.TableContainer) {
      // TableContainer conversion is handled in dialog's onConfirm callback
      // This should not be reached in normal flow
      layoutItem.DomAttribute.DisplayName = 'Table Container';
      layoutItem.AppFormLayoutItem_List = [];
      layoutItem.DomAttribute.IsBindingToDataField = false;
    } else if (itemType === layoutItemTypeEnum?.Content) {
      layoutItem.DomAttribute.DisplayName = 'Literal Content';
      layoutItem.AppFormLayoutItem_List = [];
      layoutItem.DomAttribute.IsBindingToDataField = false;
      appendNewLayoutRow(layoutItem);
    } else if (itemType === layoutItemTypeEnum?.Space) {
      layoutItem.DomAttribute.DisplayName = '';
      layoutItem.AppFormLayoutItem_List = [];
      layoutItem.DomAttribute.IsBindingToDataField = false;
      appendNewLayoutRow(layoutItem);
    } else if (itemType === layoutItemTypeEnum?.CommandActionButton) {
      if (commandActionId && transactionData?.dictCommandActionIdAndDto?.[commandActionId]) {
        const _commandActionDto = transactionData.dictCommandActionIdAndDto[commandActionId];
        layoutItem.DomAttribute.CommandActionId = commandActionId || null;
        layoutItem.DomAttribute.DisplayName = '';
        layoutItem.AppFormLayoutItem_List = [];
        layoutItem.DomAttribute.IsBindingToDataField = false;
        appendNewLayoutRow(layoutItem);
      }
    } else if (itemType === layoutItemTypeEnum?.LinkedSearch) {
      if (linkedSearchId && transactionData?.dictRootLevelUnitLinkedSearchIdAndDto?.[linkedSearchId]) {
        const linkedSearchDto = transactionData.dictRootLevelUnitLinkedSearchIdAndDto[linkedSearchId];
        layoutItem.DomAttribute.LinkedSearchId = linkedSearchId || null;
        layoutItem.DomAttribute.DisplayName = linkedSearchDto.Name || '';
        layoutItem.AppFormLayoutItem_List = [];
        layoutItem.DomAttribute.IsBindingToDataField = false;
        layoutItem.DomAttribute.HeightValue = 300;
        appendNewLayoutRow(layoutItem);
      }
    } else {
      layoutItem.DomAttribute.IsBindingToDataField = true;
      layoutItem.DomAttribute.TranscationUnitLevel = 1;

      if (transactionFieldId && transactionData?.dictTransactionFieldIdAndDto?.[transactionFieldId]) {
        const transFieldDto = transactionData.dictTransactionFieldIdAndDto[transactionFieldId];
        appHelper.debugLog('convertBlankLayoutItemToControl: Setting ForeignAppTransactionFieldExDto', {
          transactionFieldId,
          fieldDto: transFieldDto,
          fieldDtoDisplayName: transFieldDto?.DisplayName,
          fieldDtoId: transFieldDto?.Id,
          beforeAssignment: {
            hasForeignAppTransactionFieldExDto: !!layoutItem.ForeignAppTransactionFieldExDto,
            currentDisplayName: layoutItem.ForeignAppTransactionFieldExDto?.DisplayName
          }
        });
        
        layoutItem.TransactionFieldId = transactionFieldId;
        // CRITICAL: Use direct reference to dictionary field DTO (not deep copy)
        // This ensures LayoutItem.ForeignAppTransactionFieldExDto always points to the same object in dictionary
        layoutItem.ForeignAppTransactionFieldExDto = transFieldDto;
        // Don't set DomAttribute.DisplayName for field types - use ForeignAppTransactionFieldExDto.DisplayName instead
        layoutItem.DomAttribute.DataType = transFieldDto.DataType;
        
        appHelper.debugLog('Field data set in convertBlankLayoutItemToControl:', {
          transactionFieldId,
          fieldDto: transFieldDto,
          displayName: layoutItem.ForeignAppTransactionFieldExDto?.DisplayName,
          hasForeignAppTransactionFieldExDto: !!layoutItem.ForeignAppTransactionFieldExDto,
          afterAssignment: {
            hasForeignAppTransactionFieldExDto: !!layoutItem.ForeignAppTransactionFieldExDto,
            currentDisplayName: layoutItem.ForeignAppTransactionFieldExDto?.DisplayName,
            isSameReference: layoutItem.ForeignAppTransactionFieldExDto === transFieldDto
          }
        });
        
        // Check if field belongs to a child grid unit
        const fieldUnitDto = transactionData?.dictChildGridTransactionUnitIdAndDto?.[transFieldDto.TransactionUnitId];
        if (fieldUnitDto && fieldUnitDto.ParentTransactionUnitId) {
          layoutItem.GridTransactionUnitId = transFieldDto.TransactionUnitId;
          layoutItem.DomAttribute.TranscationUnitLevel = 2;
        }
      } else {
        console.warn('Cannot set field data - missing transactionFieldId or field not found:', {
          transactionFieldId,
          hasTransactionData: !!transactionData,
          hasDict: !!transactionData?.dictTransactionFieldIdAndDto,
          fieldInDict: transactionFieldId ? !!transactionData?.dictTransactionFieldIdAndDto?.[transactionFieldId] : false
        });
        layoutItem.DomAttribute.DisplayName = 'New Field';
        // TODO: Implement getDefaultDataTypeByWidgetDisplayType
      }
    }

    appHelper.debugLog('convertBlankLayoutItemToControl: Before afterConvertBlankLayoutItemToControl_appendNewButtonAndRow');
    appHelper.debugLog('convertBlankLayoutItemToControl: Current formData row count:', formData?.AppFormLayoutItemList?.length || 0);
    
    // Call afterConvertBlankLayoutItemToControl_appendNewButtonAndRow (like AngularJS)
    afterConvertBlankLayoutItemToControl_appendNewButtonAndRow(layoutItem);
    
    appHelper.debugLog('convertBlankLayoutItemToControl: After afterConvertBlankLayoutItemToControl_appendNewButtonAndRow');
    appHelper.debugLog('convertBlankLayoutItemToControl: Current formData row count:', formData?.AppFormLayoutItemList?.length || 0);
    appHelper.debugLog('convertBlankLayoutItemToControl: LayoutItem after conversion:', {
      CurrentHostId: layoutItem.CurrentHostId,
      TransactionFieldId: layoutItem.TransactionFieldId,
      hasForeignAppTransactionFieldExDto: !!layoutItem.ForeignAppTransactionFieldExDto,
      displayName: layoutItem.ForeignAppTransactionFieldExDto?.DisplayName
    });
    
    // NOTE: State update is handled by the caller (convertPlaceholderToItem) to avoid duplicate updates
    // and ensure we use the latest formData state
    return true; // Indicate that conversion was completed
  };

  // After converting placeholder, ensure placeholder button exists and add new row if needed (like AngularJS)
  const afterConvertBlankLayoutItemToControl_appendNewButtonAndRow = (layoutItem: any) => {
    appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow START:', {
      layoutItemHostId: layoutItem.CurrentHostId,
      parentHostId: layoutItem.ParentHostId,
      formDataRowCount: formData?.AppFormLayoutItemList?.length || 0
    });
    
    if (!layoutItem.ParentHostId) {
      appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow: No ParentHostId, returning');
      return;
    }

    const parentRow = findLayoutItemByHostId(layoutItem.ParentHostId);
    if (!parentRow) {
      appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow: ParentRow not found, returning');
      return;
    }
    
    appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow: Found parentRow:', {
      parentRowHostId: parentRow.CurrentHostId,
      parentRowItemCount: parentRow.AppFormLayoutItem_List?.length || 0
    });

    // Ensure placeholder button exists if there's space
    let colSpanInfo = getLayoutRowChildItemsColSpanInfo(parentRow);
    if (colSpanInfo) {
      let totalColSpan = colSpanInfo.totalColSpan;
      if (totalColSpan > 0 && totalColSpan < 24) {
        ensureNewItemAddButton(parentRow);
      }
    }

    // If this row is the last row in its container, add a new row
    if (parentRow.ParentHostId) {
      // Row belongs to a section
      const section = findLayoutItemByHostId(parentRow.ParentHostId);
      if (section && section.AppFormLayoutItem_List) {
        const maxSort = Math.max(...section.AppFormLayoutItem_List.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
        appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow: Section row check:', {
          maxSort,
          parentRowSort: parentRow.FlowOrGridLayoutSortOrder,
          isLastRow: maxSort === parentRow.FlowOrGridLayoutSortOrder
        });
        if (maxSort === parentRow.FlowOrGridLayoutSortOrder) {
          appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow: Appending new row to section');
          appendNewLayoutRow(section);
        }
      }
    } else {
      // Is form root row
      if (formData && formData.AppFormLayoutItemList) {
        const maxSort = Math.max(...formData.AppFormLayoutItemList.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
        appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow: Root row check:', {
          maxSort,
          parentRowSort: parentRow.FlowOrGridLayoutSortOrder,
          isLastRow: maxSort === parentRow.FlowOrGridLayoutSortOrder,
          formDataRowCount: formData.AppFormLayoutItemList.length
        });
        if (maxSort === parentRow.FlowOrGridLayoutSortOrder) {
          appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow: Appending new root row');
          appendNewLayoutRow();
        }
      }
    }
    
    appHelper.debugLog('afterConvertBlankLayoutItemToControl_appendNewButtonAndRow END:', {
      formDataRowCount: formData?.AppFormLayoutItemList?.length || 0
    });
  };

  // Move an existing layout item to a placeholder (shared by drop and paste). Returns true if moved.
  const moveLayoutItemToPlaceholder = (itemToMove: any, placeholderButton: any): boolean => {
    if (!placeholderButton?.ParentHostId || !itemToMove?.ParentHostId) return false;

    const srcLayoutRow = findLayoutItemByHostId(itemToMove.ParentHostId);
    if (!srcLayoutRow?.AppFormLayoutItem_List) return false;

    const itemToDelete = srcLayoutRow.AppFormLayoutItem_List.find((item: any) =>
      item.CurrentHostId === itemToMove.CurrentHostId ||
      (item.Id && itemToMove.Id && item.Id === itemToMove.Id)
    );
    if (!itemToDelete) return false;

    const deleteIndex = srcLayoutRow.AppFormLayoutItem_List.indexOf(itemToDelete);
    if (deleteIndex < 0) return false;

    const deletedItemColSpan = itemToMove.DomAttribute?.ColSpanValue || 0;
    srcLayoutRow.AppFormLayoutItem_List.splice(deleteIndex, 1);

    cleanupSourceLayoutRowAfterItemRemoved(srcLayoutRow, deletedItemColSpan);

    const targetLayoutRow = findLayoutItemByHostId(placeholderButton.ParentHostId);
    if (!targetLayoutRow?.AppFormLayoutItem_List) return false;

    const phIndex = targetLayoutRow.AppFormLayoutItem_List.findIndex((item: any) =>
      item.CurrentHostId === placeholderButton.CurrentHostId ||
      (item.Id && placeholderButton.Id && item.Id === placeholderButton.Id)
    );
    if (phIndex < 0) return false;

    const placeholderColSpan = placeholderButton.DomAttribute?.ColSpanValue || 0;
    itemToMove.UIGridLayoutParentID = null;
    itemToMove.ParentHostId = placeholderButton.ParentHostId;
    itemToMove.FlowOrGridLayoutSortOrder = placeholderButton.FlowOrGridLayoutSortOrder ?? 0;
    if (!itemToMove.DomAttribute) itemToMove.DomAttribute = {};
    itemToMove.DomAttribute.ColSpanValue = placeholderColSpan;

    targetLayoutRow.AppFormLayoutItem_List.splice(phIndex, 1);
    targetLayoutRow.AppFormLayoutItem_List.splice(phIndex, 0, itemToMove);

    const colSpanInfo = getLayoutRowChildItemsColSpanInfo(targetLayoutRow);
    if (colSpanInfo && colSpanInfo.totalColSpan > 0 && colSpanInfo.totalColSpan < 24) {
      ensureNewItemAddButton(targetLayoutRow);
    }
    if (targetLayoutRow.ParentHostId) {
      const section = findLayoutItemByHostId(targetLayoutRow.ParentHostId);
      if (section?.AppFormLayoutItem_List) {
        const maxSort = Math.max(...section.AppFormLayoutItem_List.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
        if (maxSort === targetLayoutRow.FlowOrGridLayoutSortOrder) appendNewLayoutRow(section);
      }
    } else if (formData?.AppFormLayoutItemList) {
      const maxSort = Math.max(...formData.AppFormLayoutItemList.map((r: any) => r.FlowOrGridLayoutSortOrder || 0));
      if (maxSort === targetLayoutRow.FlowOrGridLayoutSortOrder) appendNewLayoutRow();
    }

    setFormData({ ...formData });
    setIsModified(true);
    return true;
  };

  const pasteToNewItemButton = (btnLayoutItem: any) => {
    if (!btnLayoutItem || !currentCutLayoutItem) return;
    const ok = moveLayoutItemToPlaceholder(currentCutLayoutItem, btnLayoutItem);
    if (ok) {
      setCurrentLayoutItem(null);
      setCurrentCutLayoutItem(null);
    }
  };

  // Shared function to convert placeholder to actual layout item (used by both drag-drop and click)
  const convertPlaceholderToItem = (
    placeholderItem: any,
    itemType: number,
    transactionFieldId?: number,
    gridTransactionUnitId?: number,
    commandActionId?: number,
    linkedSearchId?: number
  ) => {
    appHelper.debugLog('========== convertPlaceholderToItem START ==========');
    appHelper.debugLog('convertPlaceholderToItem called with:', {
      placeholderItem: placeholderItem ? {
        CurrentHostId: placeholderItem.CurrentHostId,
        Id: placeholderItem.Id,
        WidgetDisplayType: placeholderItem.DomAttribute?.WidgetDisplayType
      } : null,
      itemType,
      transactionFieldId,
      gridTransactionUnitId,
      commandActionId,
      linkedSearchId
    });
    appHelper.debugLog('Current formData before conversion:', {
      hasFormData: !!formData,
      rowCount: formData?.AppFormLayoutItemList?.length || 0,
      formDataId: formData?.Id,
      formDataName: formData?.Name
    });

    if (!placeholderItem || 
        !placeholderItem.DomAttribute || 
        placeholderItem.DomAttribute.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton) {
      console.warn('[DEBUG] convertPlaceholderToItem: Invalid placeholder item');
      return false;
    }

    // Find the actual layout item in formData by CurrentHostId to ensure we're modifying the real object
    const actualLayoutItem = findLayoutItemByHostId(placeholderItem.CurrentHostId);
    if (!actualLayoutItem || 
        !actualLayoutItem.DomAttribute || 
        actualLayoutItem.DomAttribute.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton) {
      console.warn('[DEBUG] convertPlaceholderToItem: Placeholder not found in formData');
      return false;
    }

    appHelper.debugLog('Converting placeholder to item:', {
      placeholderHostId: actualLayoutItem.CurrentHostId,
      itemType,
      transactionFieldId,
      gridTransactionUnitId,
      commandActionId,
      linkedSearchId,
      fieldDto: transactionFieldId ? transactionData?.dictTransactionFieldIdAndDto?.[transactionFieldId] : null
    });

    // Convert the placeholder to the clicked item type
    // This will also call afterConvertBlankLayoutItemToControl_appendNewButtonAndRow
    // which modifies nested objects (adds placeholder buttons, etc.)
    appHelper.debugLog('Before convertBlankLayoutItemToControl, formData row count:', formData?.AppFormLayoutItemList?.length || 0);
    const conversionCompleted = convertBlankLayoutItemToControl(
      actualLayoutItem,
      itemType,
      transactionFieldId,
      gridTransactionUnitId,
      commandActionId,
      linkedSearchId
    );
    appHelper.debugLog('After convertBlankLayoutItemToControl, conversionCompleted:', conversionCompleted);
    appHelper.debugLog('After convertBlankLayoutItemToControl, formData row count:', formData?.AppFormLayoutItemList?.length || 0);
    
    // If conversion was deferred (e.g., TableContainer dialog opened), don't update state yet
    if (!conversionCompleted) {
      appHelper.debugLog('convertPlaceholderToItem: Conversion deferred (dialog opened), returning true without state update');
      return true; // Return true to indicate the operation was handled (dialog opened)
    }

    // CRITICAL: Force React to detect ALL changes by creating new array references
    // Since convertBlankLayoutItemToControl and afterConvertBlankLayoutItemToControl_appendNewButtonAndRow
    // modify nested objects (like adding placeholder buttons), we need to ensure React detects these changes
    // Use functional update to ensure we're working with the latest formData state
    // IMPORTANT: We clone arrays but preserve DTO references (ForeignAppTransactionFieldExDto, etc.)
    // to avoid breaking data binding and React key conflicts
    appHelper.debugLog('Before setFormData, formData snapshot:', {
      rowCount: formData?.AppFormLayoutItemList?.length || 0,
      firstRowItemCount: formData?.AppFormLayoutItemList?.[0]?.AppFormLayoutItem_List?.length || 0,
      placeholderCount: formData?.AppFormLayoutItemList?.flatMap((row: any) => 
        row.AppFormLayoutItem_List?.filter((item: any) => 
          item.DomAttribute?.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
        ) || []
      ).length || 0
    });
    
    setFormData((prevFormData: any) => {
      appHelper.debugLog('setFormData callback called, prevFormData:', {
        hasPrevFormData: !!prevFormData,
        rowCount: prevFormData?.AppFormLayoutItemList?.length || 0,
        prevFormDataId: prevFormData?.Id,
        prevFormDataName: prevFormData?.Name
      });
      
      if (!prevFormData) {
        console.warn('[DEBUG] convertPlaceholderToItem: prevFormData is null');
        return prevFormData;
      }
      
      // Helper function to clone layout items recursively, preserving DTO references
      const cloneLayoutItem = (item: any): any => {
        if (!item) return item;
        
        return {
          ...item,
          // Clone DomAttribute
          DomAttribute: item.DomAttribute ? { ...item.DomAttribute } : undefined,
          // CRITICAL: Preserve DTO references (don't clone them)
          // These should point to the same objects in transactionData dictionaries
          ForeignAppTransactionFieldExDto: item.ForeignAppTransactionFieldExDto,
          ForeignAppTransactionUnitExDto: item.ForeignAppTransactionUnitExDto,
          ForeignAppCommandActionExDto: item.ForeignAppCommandActionExDto,
          ForeignAppTransactionUnitLinkedSearchExDto: item.ForeignAppTransactionUnitLinkedSearchExDto,
          // Recursively clone child items array
          AppFormLayoutItem_List: item.AppFormLayoutItem_List ? 
            item.AppFormLayoutItem_List.map(cloneLayoutItem) : undefined
        };
      };
      
      // Clone AppFormLayoutItemList with all nested arrays
      const newFormData = {
        ...prevFormData,
        AppFormLayoutItemList: prevFormData.AppFormLayoutItemList ? 
          prevFormData.AppFormLayoutItemList.map(cloneLayoutItem) : []
      };
      
      appHelper.debugLog('setFormData returning newFormData:', {
        rowCount: newFormData.AppFormLayoutItemList?.length || 0,
        firstRowItemCount: newFormData.AppFormLayoutItemList?.[0]?.AppFormLayoutItem_List?.length || 0,
        placeholderCount: newFormData.AppFormLayoutItemList?.flatMap((row: any) => 
          row.AppFormLayoutItem_List?.filter((item: any) => 
            item.DomAttribute?.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
          ) || []
        ).length || 0,
        newFormDataId: newFormData.Id,
        newFormDataName: newFormData.Name,
        stackTrace: new Error().stack?.split('\n').slice(1, 4).join('\n')
      });
      
      return newFormData;
    });
    
    appHelper.debugLog('After setFormData call (async), formData may not be updated yet');
    appHelper.debugLog('Stack trace for convertPlaceholderToItem setFormData:', new Error().stack);
    
    // Update currentLayoutItem to the converted item (after formData update)
    appHelper.debugLog('Before setCurrentLayoutItem, actualLayoutItem:', {
      CurrentHostId: actualLayoutItem.CurrentHostId,
      TransactionFieldId: actualLayoutItem.TransactionFieldId,
      hasForeignAppTransactionFieldExDto: !!actualLayoutItem.ForeignAppTransactionFieldExDto,
      editingFieldIdRef: editingFieldIdRef.current
    });
    
    // CRITICAL: Clear editingFieldIdRef before setCurrentLayoutItem to prevent handleLayoutItemChange from being triggered incorrectly
    // The editingFieldIdRef should only be set when user explicitly edits a field in FieldSettingToolbox
    const previousEditingFieldId = editingFieldIdRef.current;
    editingFieldIdRef.current = null;
    appHelper.debugLog('Cleared editingFieldIdRef before setCurrentLayoutItem:', {
      previousEditingFieldId,
      newEditingFieldId: editingFieldIdRef.current
    });
    
    setCurrentLayoutItem({ ...actualLayoutItem });
    setIsModified(true);
    
    appHelper.debugLog('After setCurrentLayoutItem, currentLayoutItem should be updated');
    appHelper.debugLog('========== convertPlaceholderToItem END (returning true) ==========');
    return true;
  };

  // Drop handler for placeholder button (like AngularJS onDropToNewItemButton)
  const onDropToNewItemButton = (
    event: React.DragEvent,
    placeholderHostId: string | number
  ) => {
    appHelper.debugLog('========== onDropToNewItemButton START ==========');
    appHelper.debugLog('onDropToNewItemButton called with placeholderHostId:', placeholderHostId);
    appHelper.debugLog('Current formData before drop:', {
      hasFormData: !!formData,
      rowCount: formData?.AppFormLayoutItemList?.length || 0,
      formDataId: formData?.Id,
      placeholderCount: formData?.AppFormLayoutItemList?.flatMap((row: any) => 
        row.AppFormLayoutItem_List?.filter((item: any) => 
          item.DomAttribute?.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
        ) || []
      ).length || 0
    });
    appHelper.debugLog('currentDragData:', currentDragData);
    
    event.preventDefault();
    event.stopPropagation();

    // Find the placeholder button by CurrentHostId
    const placeholderButton = findLayoutItemByHostId(placeholderHostId);
    if (!placeholderButton) {
      console.warn('Placeholder button not found for hostId:', placeholderHostId);
      return;
    }

    // Get drag data from dataTransfer (preferred) or fallback to currentDragData state
    // Note: dataTransfer.getData may return empty string, so we need to check for that
    let dragTypeData = event.dataTransfer.getData('application/drag-type');
    let dragFieldIdData = event.dataTransfer.getData('application/drag-transaction-field-id');
    let dragGridUnitIdData = event.dataTransfer.getData('application/drag-grid-transaction-unit-id');
    let dragCommandActionIdData = event.dataTransfer.getData('application/drag-command-action-id');
    let dragLinkedSearchIdData = event.dataTransfer.getData('application/drag-linked-search-id');
    let dragLayoutItemUiIdData = event.dataTransfer.getData('application/drag-layout-item-ui-id');
    
    // Try to parse from text/plain as fallback (JSON format)
    if ((!dragTypeData || dragTypeData.trim() === '') && (!dragFieldIdData || dragFieldIdData.trim() === '')) {
      try {
        const textData = event.dataTransfer.getData('text/plain');
        if (textData && textData.trim() !== '') {
          const parsedData = JSON.parse(textData);
          if (parsedData.type) {
            dragTypeData = parsedData.type.toString();
          }
          if (parsedData.transactionFieldId) {
            dragFieldIdData = parsedData.transactionFieldId.toString();
          }
          if (parsedData.gridTransactionUnitId) {
            dragGridUnitIdData = parsedData.gridTransactionUnitId.toString();
          }
          if (parsedData.commandActionId) {
            dragCommandActionIdData = parsedData.commandActionId.toString();
          }
          if (parsedData.linkedSearchId) {
            dragLinkedSearchIdData = parsedData.linkedSearchId.toString();
          }
          if (parsedData.layoutItemUiId != null) {
            dragLayoutItemUiIdData = parsedData.layoutItemUiId.toString();
          }
          appHelper.debugLog('Parsed drag data from text/plain:', parsedData);
        }
      } catch (err) {
        console.warn('Failed to parse text/plain drag data:', err);
      }
    }
    
    appHelper.debugLog('DataTransfer data:', {
      dragTypeData,
      dragFieldIdData,
      dragGridUnitIdData,
      dragCommandActionIdData,
      dragLinkedSearchIdData,
      dragLayoutItemUiIdData,
      availableTypes: Array.from(event.dataTransfer.types)
    });
    
    // Helper function to safely parse data, checking for empty strings
    const safeParseInt = (value: string | null | undefined, fallback?: number): number | undefined => {
      if (!value || value.trim() === '') return fallback;
      const parsed = parseInt(value);
      return isNaN(parsed) ? fallback : parsed;
    };
    
    // CurrentHostId is string (e.g. 'MBWA29') - use as-is for findLayoutItemByHostId, do not parse as number
    const draggingLayoutItemHostId: string | number | null =
      (dragLayoutItemUiIdData && dragLayoutItemUiIdData.trim() !== '')
        ? dragLayoutItemUiIdData
        : (currentDragData?.layoutItemUiId != null ? currentDragData.layoutItemUiId : null);

    const draggingItemType = safeParseInt(dragTypeData, currentDragData?.itemType ?? undefined) ?? null;
    const draggingTransactionFieldId = safeParseInt(dragFieldIdData, currentDragData?.transactionFieldId) ?? undefined;
    const draggingGridTransactionUnitId = safeParseInt(dragGridUnitIdData, currentDragData?.gridTransactionUnitId) ?? undefined;
    const draggingCommandActionId = safeParseInt(dragCommandActionIdData, currentDragData?.commandActionId) ?? undefined;
    const draggingLinkedSearchId = safeParseInt(dragLinkedSearchIdData, currentDragData?.linkedSearchId) ?? undefined;

    appHelper.debugLog('Parsed drag data:', {
      draggingItemType,
      draggingTransactionFieldId,
      draggingLayoutItemHostId
    });

    // If moving existing layout item to placeholder: move immediately (no cut/paste state - setState is async)
    if (draggingLayoutItemHostId) {
      const draggingLayoutItem = findLayoutItemByHostId(draggingLayoutItemHostId);
      if (draggingLayoutItem && placeholderButton) {
        appHelper.debugLog('Moving existing layout item to placeholder:', draggingLayoutItem);
        const ok = moveLayoutItemToPlaceholder(draggingLayoutItem, placeholderButton);
        if (ok) {
          setCurrentDragData(null);
          return;
        }
      }
    }

    // Determine item type from drag data
    let finalItemType = draggingItemType;
    if (!finalItemType && draggingTransactionFieldId && transactionData?.dictTransactionFieldIdAndDto?.[draggingTransactionFieldId]) {
      const transFieldDto = transactionData.dictTransactionFieldIdAndDto[draggingTransactionFieldId];
      finalItemType = transFieldDto.ControlType;
      appHelper.debugLog('Determined item type from transaction field:', finalItemType);
    } else if (!finalItemType && draggingGridTransactionUnitId) {
      finalItemType = layoutItemTypeEnum?.Grid ?? null;
      appHelper.debugLog('Determined item type as Grid');
    } else if (!finalItemType && draggingCommandActionId) {
      finalItemType = layoutItemTypeEnum?.CommandActionButton ?? null;
      appHelper.debugLog('Determined item type as CommandActionButton');
    } else if (!finalItemType && draggingLinkedSearchId) {
      finalItemType = layoutItemTypeEnum?.LinkedSearch ?? null;
      appHelper.debugLog('Determined item type as LinkedSearch');
    }

    appHelper.debugLog('Final item type:', finalItemType);
    appHelper.debugLog('Transaction data available:', !!transactionData);

    if (finalItemType && typeof finalItemType === 'number') {
      appHelper.debugLog('Calling convertPlaceholderToItem with:', {
        placeholderButton: placeholderButton ? {
          CurrentHostId: placeholderButton.CurrentHostId,
          Id: placeholderButton.Id
        } : null,
        finalItemType,
        draggingTransactionFieldId,
        draggingGridTransactionUnitId,
        draggingCommandActionId,
        draggingLinkedSearchId
      });
      
      // Use shared function to convert placeholder
      const success = convertPlaceholderToItem(
        placeholderButton,
        finalItemType,
        draggingTransactionFieldId,
        draggingGridTransactionUnitId,
        draggingCommandActionId,
        draggingLinkedSearchId
      );
      
      appHelper.debugLog('convertPlaceholderToItem returned:', success);
      appHelper.debugLog('After convertPlaceholderToItem, formData:', {
        hasFormData: !!formData,
        rowCount: formData?.AppFormLayoutItemList?.length || 0,
        placeholderCount: formData?.AppFormLayoutItemList?.flatMap((row: any) => 
          row.AppFormLayoutItem_List?.filter((item: any) => 
            item.DomAttribute?.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
          ) || []
        ).length || 0
      });
      
      if (success) {
        // Clear drag data after successful drop
        setCurrentDragData(null);
        appHelper.debugLog('Drag data cleared');
      }
      appHelper.debugLog('========== onDropToNewItemButton END ==========');
    } else {
      console.warn('Cannot determine item type from drag data. Drag data:', {
        draggingItemType,
        draggingTransactionFieldId,
        draggingGridTransactionUnitId,
        draggingCommandActionId,
        draggingLinkedSearchId,
        currentDragData
      });
    }
  };

  // Handle add layout item
  const handleAddLayoutItem = (
    itemType: number,
    transactionFieldId?: number,
    gridTransactionUnitId?: number,
    commandActionId?: number,
    linkedSearchId?: number,
    targetRowIndex?: number,
    insertAtIndex?: number
  ) => {
    if (!formData) return;

    // If currentLayoutItem is a placeholder (NewItemAddButton), convert it instead of adding new item
    if (currentLayoutItem && 
        currentLayoutItem.DomAttribute && 
        currentLayoutItem.DomAttribute.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton) {
      // Use shared function to convert placeholder
      const success = convertPlaceholderToItem(
        currentLayoutItem,
        itemType,
        transactionFieldId,
        gridTransactionUnitId,
        commandActionId,
        linkedSearchId
      );
      if (success) {
        return;
      }
    }

    const layoutItemType = layoutItemTypeEnum?.[Object.keys(layoutItemTypeEnum).find(key => 
      layoutItemTypeEnum[key] === itemType
    ) as string];

    // If adding a Section or TabContainer, add as a new row
    if (itemType === layoutItemTypeEnum?.Section || 
        itemType === layoutItemTypeEnum?.TabContainer) {
      const maxSortOrder = formData.AppFormLayoutItemList.length > 0
        ? Math.max(...formData.AppFormLayoutItemList.map((r: any) => r.FlowOrGridLayoutSortOrder || 0))
        : 0;

      const newItem: any = {
        FlowOrGridLayoutSortOrder: maxSortOrder + 1,
        DomElementTag: layoutItemType || 'Section',
        DisplayTitle: layoutItemType || 'Section',
        DomAttribute: {
          WidgetDisplayType: itemType,
        },
        CurrentHostId: appHelper.randomId(),
        AppFormLayoutItem_List: [],
      };

      // If it's a Section, use appendNewLayoutRow to ensure it has a LayoutRow
      if (itemType === layoutItemTypeEnum?.Section) {
        // Initialize the section first
        initLayoutItemAndChildItems(newItem, undefined, formData);
        // appendNewLayoutRow is called inside initLayoutItemAndChildItems, but we can also call it explicitly here
        appendNewLayoutRow(newItem);
      } else {
        // For TabContainer, just initialize
        initLayoutItemAndChildItems(newItem, undefined, formData);
      }

      formData.AppFormLayoutItemList.push(newItem);
      setFormData({ ...formData });
      setIsModified(true);
      return;
    }


    // For other items, add to a LayoutRow
    const targetRow = findOrCreateTargetRow(targetRowIndex);
    if (!targetRow) return;

    // Remove existing NewItemAddButton before adding new item
    removeNewItemAddButton(targetRow);

    // Get max sort order in the row
    const maxItemSort = targetRow.AppFormLayoutItem_List.length > 0
      ? Math.max(...targetRow.AppFormLayoutItem_List.map((item: any) => item.FlowOrGridLayoutSortOrder || 0))
      : 0;

    // Create new layout item
    const newItem: any = {
      FlowOrGridLayoutSortOrder: insertAtIndex !== undefined ? insertAtIndex : maxItemSort + 1,
      DomElementTag: layoutItemType || 'Field',
      DisplayTitle: layoutItemType || 'Field',
      DomAttribute: {
        WidgetDisplayType: itemType,
        ColSpanValue: formData.DefaultNbColumns ? Math.round(24 / formData.DefaultNbColumns) : 8,
      },
      CurrentHostId: appHelper.randomId(),
      ParentHostId: targetRow.CurrentHostId,
    };

    // Set field-specific properties
    if (transactionFieldId && transactionData?.dictTransactionFieldIdAndDto) {
      const field = transactionData.dictTransactionFieldIdAndDto[transactionFieldId];
      if (field) {
        // CRITICAL: Use direct reference to dictionary field DTO (not deep copy)
        // This ensures LayoutItem.ForeignAppTransactionFieldExDto always points to the same object in dictionary
        newItem.ForeignAppTransactionFieldExDto = field;
        newItem.DomAttribute.IsBindingToDataField = true;
        // Don't set DomAttribute.DisplayName for field types - use ForeignAppTransactionFieldExDto.DisplayName instead
      }
    }

    if (gridTransactionUnitId && transactionData?.dictChildGridTransactionUnitIdAndDto) {
      const unit = transactionData.dictChildGridTransactionUnitIdAndDto[gridTransactionUnitId];
      if (unit) {
        newItem.ForeignAppTransactionUnitExDto = unit;
        newItem.DomAttribute.IsBindingToDataField = true;
        newItem.DomAttribute.DisplayName = unit.DisplayName;
      }
    }

    if (commandActionId && transactionData?.dictCommandActionIdAndDto) {
      const command = transactionData.dictCommandActionIdAndDto[commandActionId];
      if (command) {
        newItem.ForeignAppCommandActionExDto = command;
        newItem.DomAttribute.DisplayName = command.DisplayName;
      }
    }

    if (linkedSearchId && transactionData?.dictRootLevelUnitLinkedSearchIdAndDto) {
      const linkedSearch = transactionData.dictRootLevelUnitLinkedSearchIdAndDto[linkedSearchId];
      if (linkedSearch) {
        newItem.ForeignAppTransactionUnitLinkedSearchExDto = linkedSearch;
        newItem.DomAttribute.DisplayName = linkedSearch.DisplayName;
      }
    }

    // Adjust sort orders if inserting at specific index
    if (insertAtIndex !== undefined) {
      targetRow.AppFormLayoutItem_List.forEach((item: any) => {
        if (item.FlowOrGridLayoutSortOrder >= insertAtIndex) {
          item.FlowOrGridLayoutSortOrder += 1;
        }
      });
    }

    targetRow.AppFormLayoutItem_List.push(newItem);

    // Ensure NewItemAddButton exists after adding item
    ensureNewItemAddButton(targetRow);

    setFormData({ ...formData });
    setIsModified(true);
  };

  if (isLoading) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <div className={`text-sm ${theme.label}`}>Loading form design...</div>
      </div>
    );
  }

  if (!formData) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <div className={`text-sm ${theme.label}`}>No form data available</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col">
      {/* Top Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection} border-b`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Flex Form Design: {formData.Name || 'New Form'}
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleRefresh}
            className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
            title="Refresh"
          >
            <i className="fa fa-refresh"></i>
            <span>Refresh</span>
          </button>

          <button
            onClick={handleSave}
            disabled={!isModified}
            className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1`}
            title="Save"
          >
            <i className="fa fa-save"></i>
            <span>Save</span>
          </button>

          {(formId || formData?.Id) && (
            <div className="relative" ref={resetLayoutDropdownRef}>
              <button
                type="button"
                onClick={() => setResetLayoutDropdownOpen((v) => !v)}
                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1 ${resetLayoutDropdownOpen ? 'ring-1 ring-blue-400' : ''}`}
                title="Reset Layout"
              >
                <i className="fa fa-eraser" aria-hidden="true"></i>
                <span>Reset Layout</span>
                <i className="fa fa-caret-down text-[10px]"></i>
              </button>
              {resetLayoutDropdownOpen && (
                <div
                  className={`absolute right-0 top-full mt-1 min-w-[200px] py-1 rounded shadow-lg border z-50 ${theme.mainContentSection}`}
                  role="menu"
                >
                  <button
                    type="button"
                    className={`w-full text-left px-3 py-2 text-xs hover:bg-opacity-80 ${theme.button_default}`}
                    onClick={() => {
                      handleResetLayout(4, true);
                      setResetLayoutDropdownOpen(false);
                    }}
                  >
                    Reset & Auto Design Layout
                  </button>
                  <button
                    type="button"
                    className={`w-full text-left px-3 py-2 text-xs hover:bg-opacity-80 ${theme.button_default}`}
                    onClick={() => {
                      handleResetLayout(4, false);
                      setResetLayoutDropdownOpen(false);
                    }}
                  >
                    Reset To Blank Layout
                  </button>
                </div>
              )}
            </div>
          )}

          {formData.AssociatedTransactionId && (
            <button
              type="button"
              onClick={handleRunTest}
              className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
              title="Run Test"
            >
              <i className="fa fa-eye" aria-hidden="true"></i>
              <span>Run Test</span>
            </button>
          )}

          {!formData.IsPhysicalModelTableCreated && !formData.IsApiIntegrationTransaction && (
            <button
              type="button"
              className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
              title="Release Form"
            >
              <i className="fa fa-globe"></i>
              <span>Release Form</span>
            </button>
          )}

          {formData.IsPhysicalModelTableCreated && onSwitchToDataModelDesign && (
            <button
              type="button"
              onClick={onSwitchToDataModelDesign}
              className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
              title="Design Data Model"
            >
              <i className="fa fa-diagram-project"></i>
              <span>Design Data Model</span>
            </button>
          )}
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 overflow-hidden flex">
        {/* Left Toolbox (350px) */}
        <div className="w-[350px] flex-none overflow-y-auto p-2 border-r" style={{ backgroundColor: theme.param.bg_tab || '#f5f5f5' }}>
          {/* Form Setting Toolbox */}
          {formData && (
            <FormSettingToolbox
              formData={formData}
              onFormDataChange={handleFormDataChange}
            />
          )}

          {/* Field Setting Toolbox */}
          <FieldSettingToolbox
            currentLayoutItem={currentLayoutItem}
            formData={formData}
            transactionData={transactionData}
            onLayoutItemChange={handleLayoutItemChange}
          />

          {/* Add Field Toolbox */}
          <AddFieldToolbox
            formData={formData}
            transactionData={transactionData}
            onAddLayoutItem={handleAddLayoutItem}
          />
        </div>

        {/* Right Design Panel */}
        <div className="flex-1 overflow-auto p-2" style={{ backgroundColor: '#f9f9f9' }}>
          <div 
            className="mx-auto bg-white border shadow-sm"
            style={{ 
              width: formData.DefaultWidth ? `${formData.DefaultWidth}px` : '800px',
              minHeight: '100%',
              backgroundImage: 'url(/img/backGroundGridLight.png)',
              padding: '10px'
            }}
            onDragOver={(e) => {
              e.preventDefault();
              e.dataTransfer.dropEffect = 'move';
            }}
            onDrop={(e) => {
              e.preventDefault();
              const el = document.elementFromPoint(e.clientX, e.clientY);
              const placeholder = el?.closest?.('[id^="NewItemButton_"]') as HTMLElement | null;
              if (placeholder?.id) {
                const hostId = placeholder.id.replace(/^NewItemButton_/, '');
                onDropToNewItemButton(e, hostId);
                e.stopPropagation();
                return;
              }
              const boundary = el?.closest?.('.InsertBoundary') || el?.closest?.('.InsertBoundaryButton');
              const boundaryDiv = boundary?.closest?.('.InsertBoundary') as HTMLElement | null;
              const parentHostId = boundaryDiv?.getAttribute?.('data-parent-host-id');
              const insertIndexStr = boundaryDiv?.getAttribute?.('data-insert-index');
              if (parentHostId != null && insertIndexStr != null && insertIndexStr !== '') {
                const parentRow = findLayoutItemByHostId(parentHostId);
                const insertIndex = parseInt(insertIndexStr, 10);
                if (parentRow && !isNaN(insertIndex)) {
                  handleDropToInsertBoundary(e, parentRow, insertIndex);
                  e.stopPropagation();
                  return;
                }
              }
              e.stopPropagation();
            }}
          >
            {/* Form Layout Design Area */}
            <FormLayoutDesignArea
              formData={formData}
              transactionData={transactionData}
              currentLayoutItem={currentLayoutItem}
              draggingLayoutItemId={draggingLayoutItemId}
              currentCutLayoutItem={currentCutLayoutItem}
              currentHoveredLayoutItemHostId={currentHoveredLayoutItemHostId}
              isMouseOverDesignPanel={isMouseOverDesignPanel}
              onLayoutItemSelect={handleLayoutItemSelect}
              onLayoutItemChange={handleLayoutItemChange}
              onAddLayoutItem={handleAddLayoutItem}
              onDropToNewItemButton={onDropToNewItemButton}
              onInsertPlaceholderAtIndex={handleInsertPlaceholderAtIndex}
              onDropToInsertBoundary={handleDropToInsertBoundary}
              onResolveInsertBoundaryDrop={(ev, parentHostId, insertIndex) => {
                const parentRow = findLayoutItemByHostId(parentHostId);
                if (parentRow) handleDropToInsertBoundary(ev, parentRow, insertIndex);
              }}
              onInsertRowAtIndex={handleInsertRowAtIndex}
              onDropToRowBoundary={handleDropToRowBoundary}
              onInsertRowInSectionAtIndex={handleInsertRowInSectionAtIndex}
              onPasteToNewItemButton={pasteToNewItemButton}
              onLayoutItemHover={handleLayoutItemHover}
              onDesignPanelMouseMove={handleDesignPanelMouseMove}
              onDesignPanelMouseEnter={() => setIsMouseOverDesignPanel(true)}
              onDesignPanelMouseLeave={() => setIsMouseOverDesignPanel(false)}
              onDragStart={(layoutItemId: number) => setDraggingLayoutItemId(layoutItemId)}
              onDragEnd={() => setDraggingLayoutItemId(null)}
              onOpenRowItemContextMenu={handleOpenRowItemContextMenu}
              onOpenContainerContextMenu={handleOpenContainerContextMenu}
              onAddTabToContainer={handleAddTabToContainer}
              activeTabs={activeTabs}
              onSetActiveTab={(tabContainerId: string, tabId: number | string) => {
                setActiveTabs(prev => ({
                  ...prev,
                  [tabContainerId]: String(tabId)
                }));
              }}
            />
          </div>
        </div>
      </div>

      {/* Row Item Context Menu */}
      {rowItemContextMenu.visible && rowItemContextMenu.item && (
        <>
          {/* Backdrop to close menu on click outside */}
          <div
            className="fixed inset-0 z-40"
            onClick={handleCloseRowItemContextMenu}
            onContextMenu={(e) => {
              e.preventDefault();
              handleCloseRowItemContextMenu();
            }}
          />
          {/* Context Menu */}
          <div
            className={`fixed ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
            style={{
              left: rowItemContextMenu.x,
              top: rowItemContextMenu.y,
              zIndex: 10000, // Ensure context menu is above INSERT BORDER (z-index 200-300)
              pointerEvents: 'auto',
            }}
            onClick={(e) => e.stopPropagation()}
            onMouseDown={(e) => e.stopPropagation()}
            onMouseUp={(e) => e.stopPropagation()}
            onMouseMove={(e) => e.stopPropagation()}
          >
            <button
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-gray-100`}
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                handleJustifyEntireRow();
              }}
              onMouseDown={(e) => e.stopPropagation()}
              onMouseUp={(e) => e.stopPropagation()}
            >
              <i className="fa fa-align-justify mr-2"></i>
              Justify Entire Row
            </button>
            <button
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-gray-100`}
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                handleJustifyLeft();
              }}
              onMouseDown={(e) => e.stopPropagation()}
              onMouseUp={(e) => e.stopPropagation()}
            >
              <i className="fa fa-align-left mr-2"></i>
              Justify Left
            </button>
            <button
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-gray-100`}
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                handleJustifyRight();
              }}
              onMouseDown={(e) => e.stopPropagation()}
              onMouseUp={(e) => e.stopPropagation()}
            >
              <i className="fa fa-align-right mr-2"></i>
              Justify Right
            </button>
            <div className="border-t my-1"></div>
            <button
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-gray-100`}
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                handleCutRowItem();
              }}
              onMouseDown={(e) => e.stopPropagation()}
              onMouseUp={(e) => e.stopPropagation()}
            >
              <i className="fa fa-cut mr-2"></i>
              Cut
            </button>
            <button
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-red-100 text-red-600`}
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                handleDeleteRowItem();
              }}
              onMouseDown={(e) => e.stopPropagation()}
              onMouseUp={(e) => e.stopPropagation()}
            >
              <i className="fa fa-trash mr-2"></i>
              Delete
            </button>
          </div>
        </>
      )}

      {/* Container Context Menu */}
      {containerContextMenu.visible && containerContextMenu.item && (() => {
        const containerItem = containerContextMenu.item;
        const isTab = containerItem.DomAttribute?.IsTab;
        const hasParentRow = containerItem.ParentHostId !== null && containerItem.ParentHostId !== undefined;
        const displayType = containerItem.DomAttribute?.WidgetDisplayType;
        const _isContainerType = displayType === layoutItemTypeEnum?.Section ||
                                displayType === layoutItemTypeEnum?.TabContainer ||
                                displayType === layoutItemTypeEnum?.TableContainer;
        
        // For containers in a LayoutRow, show justify options (like AngularJS)
        // For Tab items (IsTab = true), only show Delete Tab
        if (isTab) {
          // Tab item menu (simplified - only Delete Tab)
          return (
            <>
              <div
                className="fixed inset-0 z-40"
                onClick={handleCloseContainerContextMenu}
                onContextMenu={(e) => {
                  e.preventDefault();
                  handleCloseContainerContextMenu();
                }}
              />
              <div
                className={`fixed ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
                style={{
                  left: containerContextMenu.x,
                  top: containerContextMenu.y,
                  zIndex: 10000, // Ensure context menu is above INSERT BORDER (z-index 200-300)
                  pointerEvents: 'auto',
                }}
                onClick={(e) => e.stopPropagation()}
                onMouseDown={(e) => e.stopPropagation()}
                onMouseUp={(e) => e.stopPropagation()}
                onMouseMove={(e) => e.stopPropagation()}
              >
                <button
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-red-100 text-red-600`}
                  onClick={(e) => {
                    e.stopPropagation();
                    e.preventDefault();
                    if (containerContextMenu.item) {
                      handleRemoveLayoutItem(containerContextMenu.item);
                    }
                    handleCloseContainerContextMenu();
                  }}
                  onMouseDown={(e) => e.stopPropagation()}
                  onMouseUp={(e) => e.stopPropagation()}
                >
                  <i className="fa fa-trash mr-2"></i>
                  Delete Tab
                </button>
              </div>
            </>
          );
        }
        
        // Container menu (Section, TabContainer, TableContainer)
        return (
          <>
            <div
              className="fixed inset-0 z-40"
              onClick={handleCloseContainerContextMenu}
              onContextMenu={(e) => {
                e.preventDefault();
                handleCloseContainerContextMenu();
              }}
            />
            <div
              className={`fixed ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
              style={{
                left: containerContextMenu.x,
                top: containerContextMenu.y,
                zIndex: 10000, // Ensure context menu is above INSERT BORDER (z-index 200-300)
                pointerEvents: 'auto',
              }}
              onClick={(e) => e.stopPropagation()}
              onMouseDown={(e) => e.stopPropagation()}
              onMouseUp={(e) => e.stopPropagation()}
              onMouseMove={(e) => e.stopPropagation()}
            >
              {/* Justify options - only show if container is in a LayoutRow (like AngularJS) */}
              {hasParentRow && (
                <>
                  <button
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-gray-100`}
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      if (containerContextMenu.item) {
                        handleJustifyEntireRowForContainer(containerContextMenu.item);
                      }
                      handleCloseContainerContextMenu();
                    }}
                    onMouseDown={(e) => e.stopPropagation()}
                    onMouseUp={(e) => e.stopPropagation()}
                  >
                    <i className="fa fa-arrows-h mr-2"></i>
                    Justify Entire Row
                  </button>
                  <button
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-gray-100`}
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      if (containerContextMenu.item) {
                        handleJustifyLeftForContainer(containerContextMenu.item);
                      }
                      handleCloseContainerContextMenu();
                    }}
                    onMouseDown={(e) => e.stopPropagation()}
                    onMouseUp={(e) => e.stopPropagation()}
                  >
                    <i className="fa fa-arrow-left mr-2"></i>
                    Justify Left
                  </button>
                  <button
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-gray-100`}
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      if (containerContextMenu.item) {
                        handleJustifyRightForContainer(containerContextMenu.item);
                      }
                      handleCloseContainerContextMenu();
                    }}
                    onMouseDown={(e) => e.stopPropagation()}
                    onMouseUp={(e) => e.stopPropagation()}
                  >
                    <i className="fa fa-arrow-right mr-2"></i>
                    Justify Right
                  </button>
                  <div className="border-t my-1"></div>
                </>
              )}
              
              <button
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-gray-100`}
                onClick={(e) => {
                  e.stopPropagation();
                  e.preventDefault();
                  if (containerContextMenu.item) {
                    handleLayoutItemSelect(containerContextMenu.item);
                  }
                  handleCloseContainerContextMenu();
                }}
                onMouseDown={(e) => e.stopPropagation()}
                onMouseUp={(e) => e.stopPropagation()}
              >
                <i className="fa fa-edit mr-2"></i>
                Edit Container
              </button>
              <div className="border-t my-1"></div>
              <button
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-red-100 text-red-600`}
                onClick={(e) => {
                  e.stopPropagation();
                  e.preventDefault();
                  if (containerContextMenu.item) {
                    handleRemoveLayoutItem(containerContextMenu.item);
                  }
                  handleCloseContainerContextMenu();
                }}
                onMouseDown={(e) => e.stopPropagation()}
                onMouseUp={(e) => e.stopPropagation()}
              >
                <i className="fa fa-trash mr-2"></i>
                Delete Container
              </button>
            </div>
          </>
        );
      })()}

      {/* Table Container Dialog */}
      <TableContainerDialog
        isOpen={showTableContainerDialog}
        onClose={() => {
          // User cancelled - just close dialog, no changes needed
          setShowTableContainerDialog(false);
          setPendingTableContainer(null);
        }}
        onConfirm={(rows: number, columns: number) => {
          // Convert the placeholder to TableContainer and create internal structure (like AngularJS applyTableContainerLayoutOption)
          if (!formData || !pendingTableContainer) return;

          // Preserve the ColSpanValue from the placeholder
          const preservedColSpanValue = pendingTableContainer.DomAttribute?.ColSpanValue;

          // Convert placeholder to Section (Stack Container) - like AngularJS applyTableContainerLayoutOption
          // NOTE: AngularJS converts to Section, not TableContainer!
          pendingTableContainer.DomAttribute.WidgetDisplayType = layoutItemTypeEnum?.Section || 0;
          pendingTableContainer.DomAttribute.BackgroundColor = '#ffffff';
          pendingTableContainer.DomAttribute.TextColor = '#000000';
          pendingTableContainer.DomAttribute.DisplayName = 'Stack Container';
          pendingTableContainer.AppFormLayoutItem_List = [];
          pendingTableContainer.DomAttribute.IsBindingToDataField = false;
          pendingTableContainer.DomAttribute.DefaultNbColumns = columns;

          // Preserve ColSpanValue if it was set
          if (preservedColSpanValue) {
            pendingTableContainer.DomAttribute.ColSpanValue = preservedColSpanValue;
          }

          // Create rows and columns structure (like AngularJS applyTableContainerLayoutOption)
          for (let row = 1; row <= rows; row++) {
            const newLayoutRow: any = {
              FlowOrGridLayoutSortOrder: row,
              AppFormLayoutItem_List: [],
              DomAttribute: {
                WidgetDisplayType: layoutItemTypeEnum?.LayoutRow || 0,
              },
              CurrentHostId: appHelper.randomId(),
              ParentHostId: pendingTableContainer.CurrentHostId,
            };

            // Create columns - each cell is a Section (Stack Container) created from insertNewItemAddButton
            for (let column = 1; column <= columns; column++) {
              // Insert a placeholder button first
              const newCell = insertNewItemAddButton(newLayoutRow, formData, undefined, false);
              
              // Convert the placeholder to Section (Stack Container) - like AngularJS line 3719-3725
              newCell.DomAttribute.WidgetDisplayType = layoutItemTypeEnum?.Section || 0;
              newCell.DomAttribute.BackgroundColor = '#ffffff';
              newCell.DomAttribute.TextColor = '#000000';
              newCell.DomAttribute.DisplayName = 'Stack Container';
              newCell.AppFormLayoutItem_List = [];
              newCell.DomAttribute.IsBindingToDataField = false;
              newCell.DomAttribute.DefaultNbColumns = 1;
            }

            pendingTableContainer.AppFormLayoutItem_List.push(newLayoutRow);
          }

          // Initialize the table container with new structure
          initLayoutItemAndChildItems(pendingTableContainer, undefined, formData);
          
          // Call afterConvertBlankLayoutItemToControl_appendNewButtonAndRow (like AngularJS)
          afterConvertBlankLayoutItemToControl_appendNewButtonAndRow(pendingTableContainer);

          // Update form data (like convertPlaceholderToItem does)
          setFormData((prevFormData: any) => {
            if (!prevFormData) return prevFormData;
            
            // Helper function to clone layout items recursively, preserving DTO references
            const cloneLayoutItem = (item: any): any => {
              if (!item) return item;
              
              return {
                ...item,
                DomAttribute: item.DomAttribute ? { ...item.DomAttribute } : undefined,
                ForeignAppTransactionFieldExDto: item.ForeignAppTransactionFieldExDto,
                ForeignAppTransactionUnitExDto: item.ForeignAppTransactionUnitExDto,
                ForeignAppCommandActionExDto: item.ForeignAppCommandActionExDto,
                ForeignAppTransactionUnitLinkedSearchExDto: item.ForeignAppTransactionUnitLinkedSearchExDto,
                AppFormLayoutItem_List: item.AppFormLayoutItem_List ? 
                  item.AppFormLayoutItem_List.map(cloneLayoutItem) : undefined
              };
            };
            
            return {
              ...prevFormData,
              AppFormLayoutItemList: prevFormData.AppFormLayoutItemList ? 
                prevFormData.AppFormLayoutItemList.map(cloneLayoutItem) : []
            };
          });
          
          // Update currentLayoutItem to the converted item
          setCurrentLayoutItem({ ...pendingTableContainer });
          setIsModified(true);

          // Clear pending state
          setPendingTableContainer(null);
          setShowTableContainerDialog(false);
        }}
      />

      {/* Run Test popup: runtime form with no data */}
      {showRunTestPopup && formData?.AssociatedTransactionId && (
        <TransactionFormPreview
          isOpen={showRunTestPopup}
          onClose={() => setShowRunTestPopup(false)}
          transactionId={formData.AssociatedTransactionId}
          transactionType={transactionData?.AppTransactionData?.TransactionOrganizedType ?? 0}
          transactionName={transactionData?.AppTransactionData?.TransactionName || formData?.Name || 'Form'}
        />
      )}
    </div>
  );
};

export default FormDesign;
