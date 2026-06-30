import React, { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../../../redux/store';
import { useNavigate } from 'react-router-dom';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { buildRoutePathFromParamObj, getReactPathForRouteCode } from '../../../helper/navigationHelper';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import ApplicationFormBuilder from '../../transaction/ApplicationFormBuilder';
import { isAdminUserFromContext, isEnableConfigurationModeForUser } from '../../../helper/adminPermissionHelper';
import AppSearch, { type AppSearchHandle } from '../../search/AppSearch';
import FormMasterDetail from '../FormMasterDetail';
import FormListEdit from '../FormListEdit';
import appHelper from '../../../helper/appHelper';
import { buildLinkTargetTabTitle } from '../../../utils/linkTargetTabTitle';
import { EmbeddedLinkedPopupFrame } from '../EmbeddedLinkedPopupFrame';
import WorkflowAutomationEditor from '../../workflow/WorkflowAutomationEditor';
import {
  applyLinkTargetMasterDataSelectionToRow,
  isLinkedSearchGridSingleSelection,
  isLinkedSearchPopupConfirmClose,
  type MasterDataPickerContext,
} from '../linkedSearchUtils';
import type { FormMasterDetailRuntimeConfigApi } from './formMasterDetailRuntimeConfig';
import RuntimeFieldSettingDrawer from './RuntimeFieldSettingDrawer';
import { QRCodeCanvas } from 'qrcode.react';
import { addTab, closeTab } from '../../../redux/features/ui/navigation/tabnavSlice';
import { store } from '../../../redux/store';

function buildFieldErrorKey(rootUnitId: any, fieldDto: any): string {
  const unitId = fieldDto?.TransactionUnitId ?? null;
  const db = fieldDto?.DataBaseFieldName ?? fieldDto?.FieldName ?? '';
  if (!db) return '';
  if (rootUnitId != null && unitId != null && String(unitId) !== String(rootUnitId)) return `${String(unitId)}.${String(db)}`;
  return String(db);
}

function isEmptyRequiredValue(v: any): boolean {
  // Treat 0 / false as valid values.
  if (v === 0 || v === false) return false;
  if (v == null) return true;
  if (typeof v === 'string') return v.trim() === '';
  return false;
}

function needValidateField(fieldDto: any): boolean {
  // Skip fields the save pipeline auto-populates, so a brand-new row doesn't fail "required"
  // validation for a value the user can never enter:
  //   - IsPrimaryKey: identity / generated on first save (root PK comes from RootPrimaryKeyValue).
  //   - IsLinkToParentPrimaryKey: child/grandchild FK set during the parent→child save cascade.
  const truthy = (v: any) =>
    v === true || v === 1 || v === '1' || (typeof v === 'string' && v.toLowerCase() === 'true');
  if (truthy(fieldDto?.IsPrimaryKey)) return false;
  if (truthy(fieldDto?.IsLinkToParentPrimaryKey)) return false;
  return true;
}

function findTransactionUnitById(units: any[] | undefined, unitId: number): any | null {
  for (const u of units || []) {
    if (u?.Id != null && Number(u.Id) === Number(unitId)) return u;
    const nested = findTransactionUnitById(u?.Children, unitId);
    if (nested) return nested;
  }
  return null;
}

/** Returns new root row — never mutate `rootFormData` (may be frozen / read-only DictOneToOneFields). */
function applyLinkedSearchResultToRootRow(result: any, linkedSearch: any, rootFormData: any, unitFields: any[]) {
  if (!result || !rootFormData) return null;
  const viewMappings = Array.isArray(linkedSearch?.AppTransactionUnitSearchViewFieldMappingList)
    ? linkedSearch.AppTransactionUnitSearchViewFieldMappingList
    : [];
  const fieldIdToDbName = new Map<string, string>();
  unitFields.forEach((f: any) => {
    if (f?.Id != null && f?.DataBaseFieldName) fieldIdToDbName.set(String(f.Id), String(f.DataBaseFieldName));
  });
  const dictViewValues = result?.DictViewColumnIDKeyValue ?? {};
  const nextDict = { ...(rootFormData.DictOneToOneFields ?? {}) };
  viewMappings.forEach((m: any) => {
    const searchViewFieldId = m?.SearchViewFieldId != null ? String(m.SearchViewFieldId) : '';
    const transactionFieldId = m?.TransactionFieldId != null ? String(m.TransactionFieldId) : '';
    const dbFieldName = fieldIdToDbName.get(transactionFieldId);
    if (!searchViewFieldId || !dbFieldName) return;
    const value =
      dictViewValues?.[searchViewFieldId] ?? dictViewValues?.[Number(searchViewFieldId)] ?? null;
    nextDict[dbFieldName] = value;
  });
  return {
    ...rootFormData,
    DictOneToOneFields: nextDict,
    IsDirty: true,
  };
}

interface FormMainMenusProps {
  controllerModel: any;
  dataModel: any;
  formStructureData: any;
  transactionExDto?: any;
  /** Layout variant for the main menu buttons. */
  layoutVariant?: 'toolbar' | 'mobilePopup';
  /** Called after successful save with server DTO (Angular: update rootPrimaryKeyValue from data.Object). */
  onSave?: (savedFormData: any) => void;
  /** Workflow Start Run: apply server FormData in place (no route reload — keeps monitor popup open). */
  onApplyWorkflowCommandFormData?: (serverFormData: any) => void;
  /** Open workflow execution monitor popup (hosted on FormMasterDetail). */
  onOpenWorkflowExecutionMonitor?: () => void;
  onDelete?: () => void;
  onRefresh?: () => void;
  /** Reload current form like reopening it: re-fetch layout + data (Angular: refresh/reopen). */
  onReloadCurrentForm?: () => void;
  /** Expose save/refresh actions to a parent (e.g. My Profile toolbar). */
  onFormActionApiReady?: (api: { save: () => Promise<void>; refresh: () => void; reload: () => void }) => void;
  /** Template form-group header forms: run their actions before main (Angular: shared top menu). */
  additionalFormActionApis?: Array<{ save: () => Promise<void>; refresh: () => void; reload: () => void }>;
  /** Template header forms have unsaved changes or are new — enable main toolbar Save. */
  additionalFormCanSave?: boolean;
  additionalFormIsDirty?: boolean;
  onCalculate?: () => void;
  onPrint?: () => void;
  onDataModelChange?: (dataModel: any) => void;
  onToggleFormConfigButtons?: () => void;
  /** Register field/unit gear actions for the form body (MasterDetailFlexLayoutForm). */
  onRuntimeConfigApiReady?: (api: FormMasterDetailRuntimeConfigApi) => void;
}

const FormMainMenus: React.FC<FormMainMenusProps> = ({
  controllerModel,
  dataModel,
  formStructureData,
  transactionExDto,
  layoutVariant = 'toolbar',
  onSave,
  onApplyWorkflowCommandFormData,
  onOpenWorkflowExecutionMonitor,
  onDelete,
  onRefresh,
  onReloadCurrentForm,
  onFormActionApiReady,
  additionalFormActionApis,
  additionalFormCanSave = false,
  additionalFormIsDirty = false,
  onCalculate,
  onPrint,
  onDataModelChange,
  onToggleFormConfigButtons,
  onRuntimeConfigApiReady,
}) => {
  const { theme, t } = useTheme();
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { showError } = useErrorMessage();
  const isMobilePopup = layoutVariant === 'mobilePopup';
  const [configMenuOpen, setConfigMenuOpen] = useState(false);
  const [reportMenuOpen, setReportMenuOpen] = useState(false);
  const configMenuRef = useRef<HTMLDivElement | null>(null);
  const reportMenuRef = useRef<HTMLDivElement | null>(null);
  const unitContextMenuRef = useRef<HTMLDivElement | null>(null);
  const rootUnitsMenuRef = useRef<HTMLDivElement | null>(null);
  const [linkTargetPopupState, setLinkTargetPopupState] = useState<{
    title: string;
    routeBasePath: string;
    paramObj: any;
    width?: number | null;
    height?: number | null;
    popupZIndex: number;
    showConfirmClose?: boolean;
    pickerContext?: MasterDataPickerContext | null;
  } | null>(null);
  const [searchPopupState, setSearchPopupState] = useState<{
    title: string;
    width?: number | null;
    height?: number | null;
    paramObj: any;
    popupZIndex: number;
    showConfirmClose?: boolean;
    linkedSearch?: any;
  } | null>(null);
  const mainMenuLinkedSearchRef = useRef<AppSearchHandle | null>(null);
  const mainMenuLinkTargetMdRef = useRef<AppSearchHandle | null>(null);

  const [configBuilderOpen, setConfigBuilderOpen] = useState(false);
  const [builderMountKey, setBuilderMountKey] = useState(0);
  const [configBuilderProps, setConfigBuilderProps] = useState<{
    defaultSectionCode: string;
    applicationId: string | null;
    transactionId: number | null;
    transactionType: number | null;
    workflowId: number | null;
    formLayoutType: number | null;
    formId: number | null;
    modelName: string | null;
    reloadOnClose: boolean;
    initialNeedToEditUnitId?: number | null;
    initialFormDesignFieldFocus?: {
      transactionFieldId: number;
      layoutItemId?: string | number;
      isGrid?: boolean;
    } | null;
  } | null>(null);

  const [workflowEditorPopup, setWorkflowEditorPopup] = useState<{
    popupZIndex: number;
    transactionId: number;
    saasApplicationId: string | number | null;
    modelName: string;
  } | null>(null);

  const [unitContextMenu, setUnitContextMenu] = useState<{
    x: number;
    y: number;
    unitId: number;
    unitDisplayName: string;
    layoutItemId: string | number;
    fields: { id: number; display: string }[];
  } | null>(null);

  const [rootUnitsMenu, setRootUnitsMenu] = useState<{ x: number; y: number } | null>(null);

  const [fieldSettingDrawer, setFieldSettingDrawer] = useState<{
    transactionFieldId: number;
    layoutItemId?: string | number;
    isGrid?: boolean;
    /** Gear button rect — popup positions beside field (viewport coordinates). */
    anchorRect: { top: number; left: number; right: number; bottom: number; width: number; height: number };
  } | null>(null);

  const [refreshConfirmOpen, setRefreshConfirmOpen] = useState(false);
  const [refreshConfirmTitle, setRefreshConfirmTitle] = useState('Refresh current form?');
  const [refreshConfirmMessage, setRefreshConfirmMessage] = useState(
    'Settings have been updated. Do you want to reload the current form to make the changes take effect?'
  );
  const [shareLinkOpen, setShareLinkOpen] = useState(false);
  const [shareLinkCopied, setShareLinkCopied] = useState(false);
  const [commandMenuOpen, setCommandMenuOpen] = useState(false);
  const commandMenuRef = useRef<HTMLDivElement | null>(null);
  const commandMenuButtonRef = useRef<HTMLButtonElement | null>(null);

  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const { tabs, activeTabKey } = useSelector((state: RootState) => state.tabnav);
  // Match Angular: tenant EnableConfigurationMode + admin; default on for admin when unset.
  const enableConfigurationMode = isEnableConfigurationModeForUser(userContext);
  const isAdminUser = isAdminUserFromContext(userContext);
  
  // Get enum values
  const transactionOrganizedTypeEnum = useEnumValues('EmTransactionOrganizedType');
  const transactionActionCodeEnum = useEnumValues('EmAppSysTransactionActionCode');
  const transactionScopeEnum = useEnumValues('EmAppTransactionScopeUsage');
  
  // Get transaction data
  const transactionDto = transactionExDto || formStructureData;
  const transactionName = transactionDto?.TransactionName || formStructureData?.TransactionName;
  
  // Check if MasterDetail form
  const isMasterDetailForm = transactionDto?.TransactionOrganizedType === transactionOrganizedTypeEnum?.MasterDetail;

  const isWorkflowAutomation =
    transactionDto?.BusinessScopeId != null &&
    transactionScopeEnum?.WorkflowAutomation != null &&
    transactionDto.BusinessScopeId === transactionScopeEnum.WorkflowAutomation;
  
  // Check if read-only
  const isReadOnly = transactionDto?.IsReadOnly === true;
  
  // Check restrictions
  const restrictedActions = transactionDto?.RestrictedTransactionUserActionList || [];
  const isSaveRestricted = transactionActionCodeEnum && restrictedActions.includes(transactionActionCodeEnum.Save);
  const isDeleteRestricted = transactionActionCodeEnum && restrictedActions.includes(transactionActionCodeEnum.Delete);
  const isRefreshRestricted = transactionActionCodeEnum && restrictedActions.includes(transactionActionCodeEnum.Refresh);
  const isCalculateRestricted = transactionActionCodeEnum && restrictedActions.includes(transactionActionCodeEnum.CalculationAndValidation);
  const isPrintRestricted = transactionActionCodeEnum && restrictedActions.includes(transactionActionCodeEnum.Print);
  
  // Check if form is locked
  const isLocked = dataModel.currentFormData?.IsLockTransaction === true;
  
  // Check if form is dirty
  const isDirty = dataModel.currentFormData?.IsDirty === true;
  const isNewForm =
    dataModel.currentFormData?.IsNew === true ||
    controllerModel?.formRequestMode === 'New' ||
    controllerModel?.rootPrimaryKeyValue == null ||
    String(controllerModel?.rootPrimaryKeyValue ?? '').trim() === '';
  
  // Check button visibility flags (file property: at least Save and Refresh needed)
  const isShowSaveButton = transactionDto?.IsShowSaveButton === true || controllerModel.isFilePropertyEdit;
  const isShowDeleteButton = transactionDto?.IsShowDeleteButton === true;
  const isShowCalculateButton = transactionDto?.IsShowCalculateButton === true;
  const isShowPrintButton = transactionDto?.IsShowPrintButton === true;
  
  // Get root unit for link targets
  const rootUnit = transactionDto?.AppTransactionUnitList?.[0];
  const linkTargets = rootUnit?.AppFormLinkTargetList || [];
  const linkedSearches = rootUnit?.AppTransactionUnitLinkedSearchList || [];
  
  // Get command actions
  const commandActions = transactionDto?.CommandActionList?.filter((cmd: any) => 
    cmd.ActionAttribute?.LinkToUI === true && 
    cmd.ActionAttribute?.IsShowOnTopMenu === true
  ).sort((a: any, b: any) => (a.ActionFlowOrder || 0) - (b.ActionFlowOrder || 0)) || [];
  
  // Get reports
  const reports = transactionDto?.ForeignAppTransactionReportExDtoList || [];
  const hasMultipleReports = reports.length > 1;
  
  // Handle Save: use SaveTransactionData with full AppMasterDetailDto (same as Angular formMasterDetailCtrl)
  const handleSave = async () => {
    // Angular parity: new form can always be saved (first save generates RootPrimaryKeyValue)
    if (isLocked) return;

    try {
      if (additionalFormActionApis?.length) {
        for (const api of additionalFormActionApis) {
          await api.save();
        }
      }

      if (!isDirty && !isNewForm) return;
      if (!dataModel.currentFormData) return;

      // UI required validation (before calling API)
      const formData = dataModel.currentFormData;
      const rootUnitId = formData?.RootUnitId ?? null;
      const tx = transactionExDto || formStructureData;
      const errors: Record<string, string> = {};
      const visited = new Set<string>();
      const oneToManyDict = (formData?.DictOneToManyFields ?? {}) as Record<string, any>;

      const unitDisplay = (unit: any) =>
        unit?.DisplayName || unit?.UnitDisplayName || unit?.TransactionUnitDisplayName || unit?.UnitName || unit?.TableName || '';

      const validateUnit = (unit: any) => {
        if (!unit?.Id) return;
        const unitIdStr = String(unit.Id);
        const unitFields = Array.isArray(unit.AppTransactionFieldList) ? unit.AppTransactionFieldList : [];
        const required = unitFields.filter((f: any) => f && f.IsFormLayoutVisible !== false && f.IsAllowEmpty === false && needValidateField(f));
        if (required.length === 0) return;

        const rows = oneToManyDict?.[unitIdStr];
        const isRowUnit = Array.isArray(rows);

        if (isRowUnit) {
          // Child/grandchild: validate per row only if there are rows.
          if (rows.length === 0) return;
          rows.forEach((row: any, rowIndex: number) => {
            required.forEach((f: any) => {
              const db = f.DataBaseFieldName;
              const v = row?.DictOneToOneFields?.[db];
              if (isEmptyRequiredValue(v)) {
                const name = f.DisplayName || f.LabelDisplayBinding || db || 'Field';
                const uName = unitDisplay(unit);
                const msg = uName ? `${uName}: ${name} is empty (row ${rowIndex + 1}).` : `${name} is empty (row ${rowIndex + 1}).`;
                const key = `${unitIdStr}[${rowIndex}].${String(db)}`;
                if (!visited.has(msg)) {
                  errors[key] = msg;
                  visited.add(msg);
                }
              }
            });
          });
          return;
        }

        // Root/sibling units: validate one-to-one values.
        required.forEach((f: any) => {
          const key = buildFieldErrorKey(rootUnitId, f);
          if (!key) return;
          const unitId = f.TransactionUnitId ?? unit.Id;
          const db = f.DataBaseFieldName;
          const isSibling = rootUnitId != null && unitId != null && String(unitId) !== String(rootUnitId);
          const v = isSibling
            ? formData?.DictSiblingOneToOneFields?.[String(unitId)]?.[db]
            : formData?.DictOneToOneFields?.[db];
          if (isEmptyRequiredValue(v)) {
            const name = f.DisplayName || f.LabelDisplayBinding || db || 'Field';
            const msg = `${name} is empty.`;
            if (!visited.has(msg)) {
              errors[key] = msg;
              visited.add(msg);
            }
          }
        });
      };

      const walkUnits = (unit: any) => {
        if (!unit) return;
        validateUnit(unit);
        const children = Array.isArray(unit.Children) ? unit.Children : [];
        children.forEach(walkUnits);
      };

      walkUnits(tx?.AppTransactionUnitList?.[0]);

      if (Object.keys(errors).length > 0) {
        onDataModelChange?.({ ...dataModel, uiValidationErrors: errors });
        const first = Object.values(errors)[0];
        if (first) showError(first);
        return;
      } else if (dataModel?.uiValidationErrors && Object.keys(dataModel.uiValidationErrors).length > 0) {
        onDataModelChange?.({ ...dataModel, uiValidationErrors: {} });
      }

      dispatch(setIsBusy());
      const result = await appTransactionService.saveTransactionData(dataModel.currentFormData);

      const errorItems = result?.ValidationResult?.Items?.filter((item: any) => item.ItemType === 1) || [];
      if (errorItems.length > 0) {
        showError(errorItems.map((e: any) => e.LocalizedMessage || e.Message || e.Key).join('\n'));
        return;
      }

      // After save, use the response only to update root PK / URL and trigger a SINGLE reload.
      // handleAfterSave (onSave) clears caches and reloads the form exactly once; also calling
      // onReloadCurrentForm here would load the form twice (duplicate TransactionForm/GetFormData).
      if (onSave && result?.Object) {
        onSave(result.Object);
      } else {
        (onReloadCurrentForm ?? onRefresh)?.();
      }
    } catch (error) {
      console.error('Error saving form:', error);
      showError('Failed to save form: ' + (error as Error).message);
    } finally {
      dispatch(setIsNotBusy());
    }
  };
  
  // Handle Delete
  const handleDelete = async () => {
    if (isLocked || !controllerModel.rootPrimaryKeyValue) return;
    
    if (!window.confirm('Are you sure you want to delete this record?')) {
      return;
    }
    
    try {
      dispatch(setIsBusy());
      // TODO: Implement delete API call
      // await appTransactionService.deleteFormData(...);

      if (onDelete) onDelete();
    } catch (error) {
      console.error('Error deleting form:', error);
      showError('Failed to delete record: ' + (error as Error).message);
    } finally {
      dispatch(setIsNotBusy());
    }
  };
  
  // Handle Refresh: only refresh form data when onRefresh is provided (never full page reload)
  const handleRefresh = () => {
    additionalFormActionApis?.forEach((api) => api.refresh());
    if (onRefresh) {
      onRefresh();
    }
  };

  // Expose action API to parent callers.
  // In embedded mode the parent toolbar must call the *latest* save/refresh logic,
  // so keep refs updated and expose stable wrappers.
  const saveRef = useRef(handleSave);
  const refreshRef = useRef(handleRefresh);
  const reloadRef = useRef<() => void>(() => {
    additionalFormActionApis?.forEach((api) => api.reload());
    (onReloadCurrentForm ?? onRefresh)?.();
  });

  useEffect(() => {
    saveRef.current = handleSave;
    refreshRef.current = handleRefresh;
    reloadRef.current = () => {
      additionalFormActionApis?.forEach((api) => api.reload());
      (onReloadCurrentForm ?? onRefresh)?.();
    };
  });

  useEffect(() => {
    if (!onFormActionApiReady) return;
    onFormActionApiReady({
      save: async () => await saveRef.current(),
      refresh: () => refreshRef.current(),
      reload: () => reloadRef.current(),
    });
  }, [onFormActionApiReady]);
  
  // Handle Calculate
  const handleCalculate = async () => {
    if (isLocked) return;
    if (!dataModel.currentFormData) return;

    // If parent provided a custom calculate hook, run it.
    if (onCalculate) return onCalculate();

    try {
      dispatch(setIsBusy());
      const result = await appTransactionService.validateAndCalculateTransactionData(dataModel.currentFormData);

      const items = result?.ValidationResult?.Items ?? [];
      const errorItems = items.filter((item: any) => item?.ItemType === 1);
      if (errorItems.length > 0) {
        showError(errorItems.map((e: any) => e?.LocalizedMessage || e?.Message || e?.Key).join('\n'));
      }

      if (result?.Object) {
        onDataModelChange?.({
          ...dataModel,
          currentFormData: { ...result.Object, IsDirty: true },
        });
      }
    } catch (error) {
      console.error('Error calculating form:', error);
      showError('Failed to calculate: ' + (error as Error).message);
    } finally {
      dispatch(setIsNotBusy());
    }
  };
  
  // Handle Print
  const handlePrint = () => {
    if (onPrint) {
      onPrint();
    } else {
      if (!controllerModel?.transactionId) return;
      if (isNewForm) {
        showError('Please save the form before printing.');
        return;
      }
      if (!controllerModel?.rootPrimaryKeyValue) {
        showError('No record selected for printing.');
        return;
      }

      const transactionId = Number(controllerModel.transactionId);
      const rootPk = String(controllerModel.rootPrimaryKeyValue);

      // React-only print page (legacy OpenWindow/FormMasterDetailPrint has been dropped).
      // This route is standalone in a new window and renders a minimal, read-only print layout.
      const routeBase = getReactPathForRouteCode('FormMasterDetailPrint');
      const urlPath = buildRoutePathFromParamObj(routeBase, { id: transactionId, param1: rootPk });
      const url = `${window.location.origin}${urlPath}`;

      try {
        window.open(url, '_blank', 'noopener,noreferrer');
      } catch (e) {
        showError('Failed to open print window: ' + ((e as Error)?.message || String(e)));
      }
    }
  };

  const buildStandaloneFormUrl = useCallback((): string | null => {
    if (!controllerModel?.transactionId) return null;
    if (isNewForm) return null;
    if (!controllerModel?.rootPrimaryKeyValue) return null;

    const transactionId = Number(controllerModel.transactionId);
    const rootPk = String(controllerModel.rootPrimaryKeyValue);
    const baseParam2 = controllerModel?.param2Obj ?? {};
    const paramObj = {
      id: transactionId,
      param1: rootPk,
      param2: { ...(typeof baseParam2 === 'object' ? baseParam2 : {}), isNavigatedFromTab: false, isStandalone: true },
    };
    const routeBase = getReactPathForRouteCode('FormMasterDetailStandalone');
    const urlPath = buildRoutePathFromParamObj(routeBase, paramObj);
    return `${window.location.origin}${urlPath}`;
  }, [controllerModel?.param2Obj, controllerModel?.rootPrimaryKeyValue, controllerModel?.transactionId, isNewForm]);

  const openShareLinkPopup = useCallback(() => {
    const url = buildStandaloneFormUrl();
    if (!url) {
      showError(isNewForm ? 'Please save the form before generating a link.' : 'No record selected to generate link.');
      return;
    }
    setShareLinkCopied(false);
    setShareLinkOpen(true);
  }, [buildStandaloneFormUrl, isNewForm, showError]);

  const copyShareLink = useCallback(async () => {
    const url = buildStandaloneFormUrl();
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      setShareLinkCopied(true);
      setTimeout(() => setShareLinkCopied(false), 1200);
    } catch {
      try {
        const ta = document.createElement('textarea');
        ta.value = url;
        ta.style.position = 'fixed';
        ta.style.left = '-9999px';
        ta.style.top = '0';
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        document.execCommand('copy');
        document.body.removeChild(ta);
        setShareLinkCopied(true);
        setTimeout(() => setShareLinkCopied(false), 1200);
      } catch {
        showError('Failed to copy URL.');
      }
    }
  }, [buildStandaloneFormUrl, showError]);
  
  const workflowStartTime = dataModel.currentFormData?.DictOneToOneFields?.StartTime;
  const workflowHasStarted = workflowStartTime != null && workflowStartTime !== '';
  const workflowHasRootPk =
    controllerModel?.rootPrimaryKeyValue != null && String(controllerModel.rootPrimaryKeyValue).trim() !== '';

  const isWorkflowCommandConditionMet = useCallback(
    (cmd: any): boolean => {
      const fieldId = cmd?.CommandConditionTransactionFieldId;
      if (!fieldId) return true;
      const dictAll = transactionDto?.DictAllTransactionField;
      const fieldMeta =
        dictAll?.[fieldId] ??
        dictAll?.[String(fieldId)] ??
        (Array.isArray(dictAll)
          ? dictAll.find((f: any) => Number(f?.Id) === Number(fieldId))
          : null);
      const dbName = fieldMeta?.DataBaseFieldName ?? cmd?.CommandConditionFieldDbFieldName;
      if (!dbName) return true;
      return !!dataModel.currentFormData?.DictOneToOneFields?.[dbName];
    },
    [dataModel.currentFormData?.DictOneToOneFields, transactionDto?.DictAllTransactionField],
  );

  // Handle Command Action
  const handleCommandAction = (commandId: number, actionType: number) => {
    if (isLocked) return;
    // Angular parity for "UI:*" actions (see transactionCommandActionEditorCtrl.js list):
    // 47 Save As, 48 Print, 49 Save, 50 Refresh, 51 Open Form Creation Window,
    // 53 Navigate To Search, 54 Close Form Window, 55 Open Form Edit Window, 74 Print From Message Template.
    if (actionType === 49) return void handleSave();
    if (actionType === 50) return void handleRefresh();
    if (actionType === 48) return void handlePrint();
    if (actionType === 53) {
      // Angular formMasterDetailCtrl.commandActionButtonClicked:
      // commandType == EmAppTransactionCommandType.OpenLinkedSearch => excecuteOpenLinkedSearch(null, LinkedSearchId)
      const cmdDto = (transactionDto?.CommandActionList || []).find((x: any) => Number(x?.Id) === Number(commandId));
      const aa = cmdDto?.ActionAttribute ?? cmdDto?.actionAttribute ?? null;
      const linkedSearchId =
        aa?.LinkedSearchId ??
        aa?.linkedSearchId ??
        aa?.LinkedSearchID ??
        aa?.linkedSearchID ??
        cmdDto?.LinkedSearchId ??
        cmdDto?.linkedSearchId ??
        null;
      if (!linkedSearchId) {
        showError('Navigate to Search failed: missing LinkedSearchId.');
        return;
      }

      const linkedSearch = (linkedSearches || []).find((x: any) => Number(x?.Id) === Number(linkedSearchId));
      if (!linkedSearch) {
        showError(`Navigate to Search failed: linked search "${String(linkedSearchId)}" not found on this form.`);
        return;
      }

      handleLinkedSearch(linkedSearch, String(rootUnit?.Id ?? ''));
      return;
    }
    if (actionType === 47) {
      // Angular formMasterDetailCtrl.saveAsFormData:
      // appTransactionSvc.saveAsMasterDetailTransactionData(currentFormData) then open main.FormMasterDetail with returned PK.
      (async () => {
        if (!dataModel.currentFormData) return;
        try {
          dispatch(setIsBusy());
          const result = await appTransactionService.saveAsMasterDetailTransactionData(dataModel.currentFormData);

          const errorItems = result?.ValidationResult?.Items?.filter((item: any) => item?.ItemType === 1) || [];
          if (errorItems.length > 0) {
            showError(errorItems.map((e: any) => e?.LocalizedMessage || e?.Message || e?.Key).join('\n'));
            return;
          }

          const obj = result?.Object;
          const tid = obj?.TransactionId ?? controllerModel?.transactionId ?? null;
          const rootPk = obj?.RootPrimaryKeyValue ?? null;
          if (!tid || rootPk == null || String(rootPk) === '') {
            showError('Save As failed: missing target record id.');
            return;
          }

          const routeBase = getReactPathForRouteCode('FormMasterDetail');
          const tabPath = buildRoutePathFromParamObj(routeBase, { id: Number(tid), param1: String(rootPk) });
          dispatch(addTab({ tabPath, label: 'Save As', isClosable: true }));
          navigate(tabPath);
        } catch (e) {
          showError('Save As failed: ' + ((e as Error)?.message || String(e)));
        } finally {
          dispatch(setIsNotBusy());
        }
      })();
      return;
    }
    if (actionType === 54) {
      // Close current form window/tab:
      // - If this form is opened in a standalone browser window, try window.close()
      // - Otherwise close the current app tab (tabnav)
      try {
        const isStandalone = Boolean(controllerModel?.param2Obj?.isStandalone);
        if (isStandalone || (typeof window !== 'undefined' && (window.opener || window.name))) {
          window.close();
          return;
        }
      } catch {
        // ignore and fall back to tab close
      }

      const key = activeTabKey || tabs.find((x) => x.isActive)?.tabKey || null;
      if (!key) return;
      dispatch(closeTab(key));
      // Navigate to the newly active tab path after state updates.
      setTimeout(() => {
        try {
          const s = store.getState();
          const next = s.tabnav.tabs.find((x) => x.isActive) ?? s.tabnav.tabs[s.tabnav.tabs.length - 1];
          if (next?.path) navigate(next.path);
        } catch {
          // ignore
        }
      }, 0);
      return;
    }
    // Non-UI commands: execute via server (Angular: ExcuteTransactionCommonad / ExecuteWorkflowRootCommonad)
    (async () => {
      if (!dataModel.currentFormData) return;

      // Angular: open monitor popup immediately when starting workflow (not after API returns).
      if (isWorkflowAutomation) {
        onOpenWorkflowExecutionMonitor?.();
      }

      try {
        if (!isWorkflowAutomation) {
          dispatch(setIsBusy());
        }

        const nextFormData = { ...dataModel.currentFormData, TransactionCommandId: commandId };
        const result = isWorkflowAutomation
          ? await appTransactionService.executeWorkflowRootCommonad(nextFormData)
          : await appTransactionService.excuteTransactionCommonad(nextFormData);

        const errorItems = result?.ValidationResult?.Items?.filter((item: any) => item?.ItemType === 1) || [];
        if (errorItems.length > 0) {
          showError(errorItems.map((e: any) => e?.LocalizedMessage || e?.Message || e?.Key).join('\\n'));
          return;
        }

        const obj = result?.Object ?? null;
        const serverFormData = obj?.FormData ?? null;
        if (serverFormData) {
          if (isWorkflowAutomation) {
            onApplyWorkflowCommandFormData?.(serverFormData);
          } else {
            onDataModelChange?.({ ...dataModel, currentFormData: serverFormData });
            onSave?.(serverFormData);
          }
        }

        // Composition command: open requested forms (subset of Angular executeServerCommandCallBack)
        const childList = obj?.ChildCommandResultDtoList ?? null;
        if (Array.isArray(childList)) {
          childList.forEach((child: any) => {
            if (child?.IsNeedToOpenTransactionForm && child?.TransactionId && child?.TransactionRId) {
              const routeBase = getReactPathForRouteCode('FormMasterDetail');
              const tabPath = buildRoutePathFromParamObj(routeBase, {
                id: Number(child.TransactionId),
                param1: String(child.TransactionRId),
              });
              dispatch(
                addTab({
                  tabPath,
                  label: String(child.FormTitleDisplay || child.TransactionRId || 'Open Form'),
                  isClosable: true,
                })
              );
            }
          });
        }
      } catch (e) {
        showError('Command execution failed: ' + ((e as Error)?.message || String(e)));
      } finally {
        if (!isWorkflowAutomation) {
          dispatch(setIsNotBusy());
        }
      }
    })();
  };
  
  // Handle Link Target
  const handleLinkTarget = (linkTarget: any) => {
    if (isLocked) return;

    const rootDictOneToOneFields = dataModel.currentFormData?.DictOneToOneFields ?? {};

    // Angular: SourceConditionColumn gates whether this menu item can execute.
    if (linkTarget?.SourceConditionColumn) {
      const v = rootDictOneToOneFields?.[linkTarget.SourceConditionColumn];
      if (v === false || v === 0 || v === '0' || v === 'false' || v === 'False' || v == null) {
        return;
      }
    }

    const normalizeMainPrefixRouteCode = (rc: string) => (rc || '').replace(/^main\./, '');

    const openInTabOrPopup = (opts: {
      routeBasePath: string;
      title: string;
      paramObj: any;
      isPopup: boolean;
      popupWidth?: number | null;
      popupHeight?: number | null;
      showConfirmClose?: boolean;
      pickerContext?: MasterDataPickerContext | null;
    }) => {
      // Unit navigation behavior: always open as in-page DIV popup.
      setLinkTargetPopupState({
        title: opts.title,
        routeBasePath: opts.routeBasePath,
        paramObj: opts.paramObj,
        width: opts.popupWidth ?? null,
        height: opts.popupHeight ?? null,
        popupZIndex: appHelper.getNextPopupZIndex(),
        showConfirmClose: opts.showConfirmClose === true,
        pickerContext: opts.pickerContext ?? undefined,
      });
    };

    // TransactionUnitLinkTargetEditor: ids
    const LINK_TARGET_ACTION = {
      CreateBlank: 2,
      CreateFromExistingItem: 13,
      Edit: 1,
      Preview: 5,
      Delete: 3,
    };

    const usageType = Number(linkTarget?.LinkTargetUsageType ?? 0);

    if (usageType === 5) {
      // System Defined Page
      const paramId = linkTarget.SourceColumn1 ? rootDictOneToOneFields?.[linkTarget.SourceColumn1] : null;
      const param1 = (() => {
        if (!linkTarget.SourceColumn2) return null;
        let dbColumnName = linkTarget.SourceColumn2;
        if (dbColumnName.indexOf('RootUnit.') >= 0) dbColumnName = dbColumnName.substring(9).trim();
        return rootDictOneToOneFields?.[dbColumnName] ?? null;
      })();
      const param2 = (() => {
        if (!linkTarget.SourceColumn3) return null;
        let dbColumnName = linkTarget.SourceColumn3;
        if (dbColumnName.indexOf('RootUnit.') >= 0) dbColumnName = dbColumnName.substring(9).trim();
        return rootDictOneToOneFields?.[dbColumnName] ?? null;
      })();

      const tabTitle = buildLinkTargetTabTitle(
        linkTarget.NavigationActionName,
        Boolean(linkTarget.RowDisplayDbField),
        linkTarget.RowDisplayDbField ? rootDictOneToOneFields?.[linkTarget.RowDisplayDbField] : undefined,
        paramId ?? undefined
      );
      let routeCode = normalizeMainPrefixRouteCode(linkTarget.LinkTargetUrlOrRouteCode ?? '');

      const paramObj: any = {};
      const routeLower = routeCode.toLowerCase();
      // AppSearch route expects `searchId`, not `id`.
      if (routeLower === 'masterdatamanagement') {
        paramObj.searchId = paramId ?? null;
        paramObj.initialViewId = param1 ?? null;
        paramObj.param2 = param2 ?? null;
        paramObj.isLinkedSearch = true;
        paramObj.isSingleSelection = true;
      } else {
        if (paramId != null) paramObj.id = paramId;
        if (param1 != null) paramObj.param1 = param1;
        if (param2 != null) paramObj.param2 = param2;
      }

      const hostRow = dataModel.currentFormData;
      openInTabOrPopup({
        routeBasePath: routeCode,
        title: tabTitle,
        paramObj,
        isPopup: Boolean(linkTarget.IsPopup),
        popupWidth: linkTarget.PopupWidth ?? null,
        popupHeight: linkTarget.PopupHeight ?? null,
        showConfirmClose: routeLower === 'masterdatamanagement',
        pickerContext: routeLower === 'masterdatamanagement' && hostRow ? { linkTarget, hostRow } : undefined,
      });

      return;
    }

    // Regular transaction link target (FormMasterDetail / FormListEdit)
    const targetPkValue = linkTarget.SourceColumn1 ? rootDictOneToOneFields?.[linkTarget.SourceColumn1] : null;

    const linkTargetValueMapping: Record<string, any> = {};
    if (linkTarget.SourceColumn2 && linkTarget.TargetColumn2) {
      let dbColumnName = linkTarget.SourceColumn2;
      if (dbColumnName.indexOf('RootUnit.') >= 0) dbColumnName = dbColumnName.substring(9).trim();
      linkTargetValueMapping[linkTarget.TargetColumn2] = rootDictOneToOneFields?.[dbColumnName];
    }
    if (linkTarget.SourceColumn3 && linkTarget.TargetColumn3) {
      let dbColumnName = linkTarget.SourceColumn3;
      if (dbColumnName.indexOf('RootUnit.') >= 0) dbColumnName = dbColumnName.substring(9).trim();
      linkTargetValueMapping[linkTarget.TargetColumn3] = rootDictOneToOneFields?.[dbColumnName];
    }

    const tabTitle = buildLinkTargetTabTitle(
      linkTarget.NavigationActionName,
      Boolean(linkTarget.RowDisplayDbField),
      linkTarget.RowDisplayDbField ? rootDictOneToOneFields?.[linkTarget.RowDisplayDbField] : undefined,
      targetPkValue ?? undefined
    );

    const targetOrganizedType = linkTarget?.TransactionOrganizedType ?? linkTarget?.LinkTargetTransactionOrganizedType;
    const isTargetListTransaction =
      transactionOrganizedTypeEnum?.List != null &&
      targetOrganizedType != null &&
      Number(targetOrganizedType) === Number(transactionOrganizedTypeEnum.List);

    const routeBasePath = isTargetListTransaction ? 'FormListEdit' : 'FormMasterDetail';

    if (linkTarget.ActionType === LINK_TARGET_ACTION.Edit) {
      if (!targetPkValue) return;
      const param2Obj = { linkTargetValueMapping };
      openInTabOrPopup({
        routeBasePath,
        title: tabTitle,
        paramObj: { id: linkTarget.LinkTargetTransactionId, param1: targetPkValue, param2: JSON.stringify(param2Obj) },
        isPopup: Boolean(linkTarget.IsPopup),
        popupWidth: linkTarget.PopupWidth ?? null,
        popupHeight: linkTarget.PopupHeight ?? null,
        showConfirmClose: false,
      });
      return;
    }

    if (linkTarget.ActionType === LINK_TARGET_ACTION.Preview) {
      if (!targetPkValue) return;
      const param2Obj = { linkTargetValueMapping, isPreview: true, isPrint: true };
      openInTabOrPopup({
        routeBasePath,
        title: tabTitle,
        paramObj: { id: linkTarget.LinkTargetTransactionId, param1: targetPkValue, param2: JSON.stringify(param2Obj) },
        isPopup: Boolean(linkTarget.IsPopup),
        popupWidth: linkTarget.PopupWidth ?? null,
        popupHeight: linkTarget.PopupHeight ?? null,
        showConfirmClose: false,
      });
      return;
    }

    if (linkTarget.ActionType === LINK_TARGET_ACTION.CreateBlank || linkTarget.ActionType === LINK_TARGET_ACTION.CreateFromExistingItem) {
      if (!linkTarget.LinkTargetTransactionId) return;
      const param2Obj: any = {};
      if (linkTarget.ActionType === LINK_TARGET_ACTION.CreateFromExistingItem) {
        param2Obj.linkTargetValueMapping = linkTargetValueMapping;
      }
      if (linkTarget.DataTransferSettingId) {
        param2Obj.newFormLinkTargetPreLoadSettingObj = {
          dataTransferSettingId: linkTarget.DataTransferSettingId,
          srcTransactionRid: controllerModel?.rootPrimaryKeyValue ?? null,
        };
      }
      openInTabOrPopup({
        routeBasePath,
        title: linkTarget.NavigationActionName,
        paramObj: { id: linkTarget.LinkTargetTransactionId, param1: null, param2: JSON.stringify(param2Obj) },
        isPopup: Boolean(linkTarget.IsPopup),
        popupWidth: linkTarget.PopupWidth ?? null,
        popupHeight: linkTarget.PopupHeight ?? null,
        showConfirmClose: false,
      });
      return;
    }

    if (linkTarget.ActionType === LINK_TARGET_ACTION.Delete) {
      showError('Delete action is not implemented for unit navigation yet.');
      return;
    }

    showError('This unit navigation action is not implemented yet.');
  };
  
  // Handle Linked Search
  const handleLinkedSearch = (linkedSearch: any, _unitId: string) => {
    if (isLocked) return;

    const rootDictOneToOneFields = dataModel.currentFormData?.DictOneToOneFields ?? {};
    const siblingOneToOne = dataModel.currentFormData?.DictSiblingOneToOneFields ?? {};
    const rootUnitId = dataModel.currentFormData?.RootUnitId ?? rootUnit?.Id ?? null;

    const targetSearchIdRaw = linkedSearch?.SearchSaveId ?? linkedSearch?.SearchId ?? null;
    const isSavedSearch = linkedSearch?.SearchSaveId != null && linkedSearch?.SearchSaveId !== '';
    const initialViewId = linkedSearch?.SearchViewId ?? null;

    const dictCreteriaIdValue: Record<string, any> = {};
    const mappingList = Array.isArray(linkedSearch?.AppTransactionUnitSearchFieldMappingList)
      ? linkedSearch.AppTransactionUnitSearchFieldMappingList
      : [];

    for (const mapping of mappingList) {
      const searchFieldId = mapping?.SearchFieldId;
      const dataBaseFieldName = mapping?.DataBaseFieldName;
      if (searchFieldId == null || !dataBaseFieldName) continue;

      const sourceUnitId = mapping?.SourceTransactionUnitId;
      let formValue: any = null;

      if (sourceUnitId == null || (rootUnitId != null && String(sourceUnitId) === String(rootUnitId))) {
        formValue = rootDictOneToOneFields?.[dataBaseFieldName];
      } else if (sourceUnitId != null && siblingOneToOne?.[sourceUnitId]) {
        formValue = siblingOneToOne?.[sourceUnitId]?.[dataBaseFieldName];
      } else {
        formValue = rootDictOneToOneFields?.[dataBaseFieldName];
      }

      dictCreteriaIdValue[searchFieldId] = formValue;
    }

    const tabTitle = linkedSearch?.Name ?? 'Linked Search';
    const routeBasePath = 'MasterDataManagement';
    const paramObj = {
      searchId: targetSearchIdRaw,
      isSavedSearch,
      initialViewId,
      isShowCriterias: true,
      isLinkedSearch: true,
      isSingleSelection: isLinkedSearchGridSingleSelection(linkedSearch),
      dictCreteriaIdValue,
    };

    setSearchPopupState({
      title: tabTitle,
      width: linkedSearch.PopupWidth ?? null,
      height: linkedSearch.PopupHeight ?? null,
      paramObj,
      popupZIndex: appHelper.getNextPopupZIndex(),
      showConfirmClose: isLinkedSearchPopupConfirmClose(linkedSearch),
      linkedSearch,
    });
  };
  
  // Handle Report
  const handleReport = (_reportId: number) => {
    // TODO: Implement report opening
  };
  
  // Render Configuration Menu (Admin only)
  const renderConfigurationMenu = () => {
    const toBooleanish = (v: any): boolean => {
      if (v === true || v === 1 || v === '1') return true;
      if (v === false || v === 0 || v === '0' || v == null) return false;
      if (typeof v === 'string') return v.toLowerCase() === 'true';
      return Boolean(v);
    };

    const isDraft = toBooleanish(dataModel?.currentFormStructure?.IsDraft ?? dataModel?.currentFormStructure?.isDraft);

    if (!enableConfigurationMode || !isAdminUser || isDraft) return null;

    const hideEnableFieldSettingButtons = false;
    const hideEditCommands = false;

    return (
      <div className={`relative ${isMobilePopup ? 'w-full' : ''}`} ref={configMenuRef}>
        <button
          className={
            isMobilePopup
              ? 'w-full px-3 h-8 bg-gray-400 text-white rounded-[4px] text-sm hover:bg-gray-500 flex items-center justify-start gap-2'
              : 'px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1'
          }
          onClick={() => setConfigMenuOpen(!configMenuOpen)}
          title="Configuration"
        >
          <i className="fa fa-gears" aria-hidden="true"></i>
          <span>Configuration</span>
        </button>

        {configMenuOpen && (
          <div
            className={`absolute top-full ${isMobilePopup ? 'left-0 right-0' : 'left-0'} z-50 mt-1 ${
              isMobilePopup ? 'min-w-0 w-full' : 'min-w-[17rem]'
            } max-w-[min(22rem,calc(100vw-16px))] rounded border shadow-lg ${theme.mainContentSection} ${t('border_mainContentSection')}`}
          >
            <div className="py-1">
              {!hideEnableFieldSettingButtons && (
                <>
                  <button
                    type="button"
                    className={`flex w-full min-w-0 items-center justify-between gap-2 px-3 py-2 text-left text-sm ${theme.contextMenu}`}
                    onClick={() => {
                      onToggleFormConfigButtons?.();
                      setConfigMenuOpen(false);
                    }}
                  >
                    <span className="min-w-0 flex-1 truncate">Enable Field Setting Buttons</span>
                    <span className={`shrink-0 opacity-70 ${theme.label}`} style={{ position: 'relative', top: 1 }}>
                      {controllerModel?.isEnableFormConfigButtons ? (
                        <span className="fa fa-toggle-on" style={{ fontSize: 15 }} />
                      ) : (
                        <span className="fa fa-toggle-off" style={{ fontSize: 15 }} />
                      )}
                    </span>
                  </button>

                  <div className={`border-t my-1 ${t('border_mainContentSection')}`} />
                </>
              )}

              {isWorkflowAutomation ? (
                <button
                  type="button"
                  className={`w-full min-w-0 px-3 py-2 text-left text-sm whitespace-nowrap ${theme.contextMenu}`}
                  onClick={() => {
                    void openWorkflowEditor();
                  }}
                >
                  Edit Workflow
                </button>
              ) : (
                <>
                  <button
                    type="button"
                    className={`w-full min-w-0 px-3 py-2 text-left text-sm whitespace-nowrap ${theme.contextMenu}`}
                    onClick={() => {
                      // Angular: OpenConfiguration(null, true) => defaultSectionCode 'Transaction'
                      openTransactionBuilder('Transaction', true);
                      setConfigMenuOpen(false);
                    }}
                  >
                    Edit Data Model
                  </button>
                  <button
                    type="button"
                    className={`w-full min-w-0 px-3 py-2 text-left text-sm whitespace-nowrap ${theme.contextMenu}`}
                    onClick={() => {
                      // Angular: OpenFormEditor() => OpenConfiguration('Form', true)
                      openTransactionBuilder('Form', true);
                      setConfigMenuOpen(false);
                    }}
                  >
                    Design Layout
                  </button>
                  {!hideEditCommands && (
                    <button
                      type="button"
                      className={`w-full min-w-0 px-3 py-2 text-left text-sm whitespace-nowrap ${theme.contextMenu}`}
                      onClick={() => {
                        // Angular: OpenCommandEditor() => OpenConfiguration('TransactionCommandActionSetting', false)
                        openTransactionBuilder('TransactionCommandActionSetting', false);
                        setConfigMenuOpen(false);
                      }}
                    >
                      Edit Commands
                    </button>
                  )}
                </>
              )}

              <div className={`border-t my-1 ${t('border_mainContentSection')}`} />

              <button
                type="button"
                className={`w-full min-w-0 px-3 py-2 text-left text-sm whitespace-nowrap ${theme.contextMenu}`}
                onClick={() => {
                  (onReloadCurrentForm ?? onRefresh)?.();
                  setConfigMenuOpen(false);
                }}
              >
                Reload Current Form
              </button>
            </div>
          </div>
        )}
      </div>
    );
  };

  // Auto-close dropdowns when clicking elsewhere.
  useEffect(() => {
    const onMouseDown = (e: MouseEvent) => {
      const target = e.target as Node;

      if (configMenuOpen && configMenuRef.current && !configMenuRef.current.contains(target)) {
        setConfigMenuOpen(false);
      }

      if (reportMenuOpen && reportMenuRef.current && !reportMenuRef.current.contains(target)) {
        setReportMenuOpen(false);
      }

      if (
        unitContextMenu &&
        unitContextMenuRef.current &&
        !unitContextMenuRef.current.contains(target)
      ) {
        setUnitContextMenu(null);
      }
      if (
        rootUnitsMenu &&
        rootUnitsMenuRef.current &&
        !rootUnitsMenuRef.current.contains(target)
      ) {
        setRootUnitsMenu(null);
      }
    };

    if (!configMenuOpen && !reportMenuOpen && !unitContextMenu && !rootUnitsMenu) return;

    document.addEventListener('mousedown', onMouseDown);
    return () => document.removeEventListener('mousedown', onMouseDown);
  }, [configMenuOpen, reportMenuOpen, unitContextMenu, rootUnitsMenu]);

  useEffect(() => {
    if (!commandMenuOpen) return;
    const onMouseDown = (e: MouseEvent) => {
      const target = e.target as Node | null;
      if (!target) return;
      if (commandMenuRef.current && commandMenuRef.current.contains(target)) return;
      setCommandMenuOpen(false);
    };
    document.addEventListener('mousedown', onMouseDown);
    return () => document.removeEventListener('mousedown', onMouseDown);
  }, [commandMenuOpen]);

  const openTransactionBuilder = useCallback(
    async (
      defaultSectionCode: string,
      reloadOnClose: boolean,
      extras?: {
        initialNeedToEditUnitId?: number | null;
        initialFormDesignFieldFocus?: {
          transactionFieldId: number;
          layoutItemId?: string | number;
          isGrid?: boolean;
        } | null;
      }
    ) => {
      const tid = controllerModel?.transactionId;
      if (!tid) return;
      try {
        dispatch(setIsBusy());
        const transactionData = await appTransactionService.getOneHierarchyTransaction(
          String(tid),
          false,
          '',
          '',
          '',
          false,
          ''
        );

        const applicationId =
          transactionData?.SaasApplicationId ?? transactionData?.saasApplicationId ?? null;

        setConfigBuilderProps({
          defaultSectionCode,
          applicationId: applicationId != null ? String(applicationId) : null,
          transactionId: tid,
          transactionType: transactionData?.TransactionOrganizedType ?? transactionDto?.TransactionOrganizedType ?? null,
          workflowId: transactionData?.MasterWorkflowId ?? null,
          formLayoutType:
            transactionData?.FormLayoutType ??
            transactionData?.ForeignAppFormExDto?.LayoutType ??
            null,
          formId: transactionData?.FormId ?? null,
          modelName: String(tid),
          reloadOnClose,
          initialNeedToEditUnitId: extras?.initialNeedToEditUnitId ?? null,
          initialFormDesignFieldFocus: extras?.initialFormDesignFieldFocus ?? null,
        });
        setBuilderMountKey((k) => k + 1);
        setConfigBuilderOpen(true);
      } catch (err) {
        showError('Failed to open Application Builder: ' + ((err as Error)?.message || String(err)));
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [controllerModel?.transactionId, transactionDto?.TransactionOrganizedType, dispatch, showError]
  );

  /** Angular formMasterDetailCtrl.openWorkflowEditor — popup WorkflowAutomationEditor, then autoReopenCurrentFormPopup on close. */
  const closeWorkflowEditorPopup = useCallback(() => {
    setWorkflowEditorPopup(null);
    setRefreshConfirmTitle('Refresh current form?');
    setRefreshConfirmMessage('Configuration has been changed. Do you want to reload current form?');
    setRefreshConfirmOpen(true);
  }, []);

  const openWorkflowEditor = useCallback(async () => {
    const tid = controllerModel?.transactionId;
    if (tid == null) {
      showError('No workflow transaction id.');
      return;
    }
    setConfigMenuOpen(false);
    try {
      dispatch(setIsBusy());
      const transactionData = await appTransactionService.getOneHierarchyTransaction(
        String(tid),
        false,
        '',
        '',
        '',
        false,
        ''
      );
      if (!transactionData) {
        showError('Failed to load workflow transaction.');
        return;
      }
      const modelName =
        transactionData.TransactionName ||
        transactionDto?.TransactionName ||
        'Workflow Automation Editor';
      const saasApplicationId =
        transactionData.SaasApplicationId ?? transactionData.saasApplicationId ?? null;
      setWorkflowEditorPopup({
        popupZIndex: appHelper.getNextPopupZIndex(),
        transactionId: Number(tid),
        saasApplicationId,
        modelName: String(modelName),
      });
    } catch (err) {
      showError('Failed to open Workflow Editor: ' + ((err as Error)?.message || String(err)));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [controllerModel?.transactionId, transactionDto?.TransactionName, dispatch, showError]);

  const openRuntimeTransactionFieldEditor = useCallback(
    (
      e: React.MouseEvent,
      transFieldId: number,
      _transFieldDisplayName: string,
      layoutItemId: string | number,
      isGrid = false
    ) => {
      e.preventDefault();
      e.stopPropagation();
      setUnitContextMenu(null);
      setRootUnitsMenu(null);
      const el = e.currentTarget as HTMLElement;
      const r = el.getBoundingClientRect();
      setFieldSettingDrawer({
        transactionFieldId: Number(transFieldId),
        layoutItemId,
        isGrid,
        anchorRect: {
          top: r.top,
          left: r.left,
          right: r.right,
          bottom: r.bottom,
          width: r.width,
          height: r.height,
        },
      });
    },
    []
  );

  const openRuntimeTransactionUnitEditor = useCallback(
    async (e: React.MouseEvent, transUnitId: number) => {
      e.preventDefault();
      e.stopPropagation();
      setUnitContextMenu(null);
      setRootUnitsMenu(null);
      await openTransactionBuilder('Transaction', true, {
        initialNeedToEditUnitId: Number(transUnitId),
        initialFormDesignFieldFocus: null,
      });
    },
    [openTransactionBuilder]
  );

  const openRuntimeUnitContextMenu = useCallback(
    (e: React.MouseEvent, unitId: number, unitDisplayName: string, layoutItemId: string | number) => {
      e.preventDefault();
      e.stopPropagation();
      const st = dataModel?.currentFormStructure;
      const dictFromStructure = st?.DictTransactionUnitIdFieldIdFieldDisplayName?.[unitId] as
        | Record<string, string>
        | undefined;
      const tx = transactionExDto || formStructureData;
      const unit = findTransactionUnitById(tx?.AppTransactionUnitList, unitId);
      let fields: { id: number; display: string }[] = [];
      if (dictFromStructure && typeof dictFromStructure === 'object') {
        fields = Object.keys(dictFromStructure).map((fid) => ({
          id: Number(fid),
          display: String(dictFromStructure[fid as keyof typeof dictFromStructure] ?? fid),
        }));
      } else if (unit?.AppTransactionFieldList?.length) {
        fields = unit.AppTransactionFieldList.map((f: any) => ({
          id: Number(f.Id),
          display: String(f.DisplayName || f.LabelDisplayBinding || f.DataBaseFieldName || f.Id),
        }));
      }
      const pos = appHelper.clampMenuPositionToViewport({
        x: e.clientX,
        y: e.clientY,
        menuWidth: 300,
        menuHeight: Math.min(420, 72 + fields.length * 28),
        margin: 8,
      });
      setUnitContextMenu({
        x: pos.x,
        y: pos.y,
        unitId,
        unitDisplayName: unitDisplayName || unit?.UnitDisplayName || `Unit ${unitId}`,
        layoutItemId,
        fields,
      });
      setRootUnitsMenu(null);
    },
    [dataModel?.currentFormStructure, transactionExDto, formStructureData]
  );

  const openRuntimeRootUnitsContextMenu = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setUnitContextMenu(null);
    const pos = appHelper.clampMenuPositionToViewport({
      x: e.clientX,
      y: e.clientY,
      menuWidth: 320,
      menuHeight: 360,
      margin: 8,
    });
    setRootUnitsMenu({ x: pos.x, y: pos.y });
  }, []);

  useEffect(() => {
    if (!onRuntimeConfigApiReady) return;
    const api: FormMasterDetailRuntimeConfigApi = {
      openTransactionFieldEditor: openRuntimeTransactionFieldEditor,
      openTransactionUnitEditor: openRuntimeTransactionUnitEditor,
      openUnitContextMenu: openRuntimeUnitContextMenu,
      openRootUnitsContextMenu: openRuntimeRootUnitsContextMenu,
    };
    onRuntimeConfigApiReady(api);
  }, [
    onRuntimeConfigApiReady,
    openRuntimeTransactionFieldEditor,
    openRuntimeTransactionUnitEditor,
    openRuntimeUnitContextMenu,
    openRuntimeRootUnitsContextMenu,
  ]);
  
  // Render Save Button
  const renderSaveButton = () => {
    if (isReadOnly || isSaveRestricted || !isShowSaveButton) return null;
    if (controllerModel.isPreview || controllerModel.isFormImportTemplate) return null;
    if (isLocked) return null;

    const canSave = isDirty || isNewForm || additionalFormCanSave;
    
    return (
      <button
        className={
          isMobilePopup
            ? `w-full px-3 h-8 rounded-[4px] text-sm flex items-center justify-start gap-2 ${
                canSave
                  ? 'bg-green-500 text-white hover:bg-green-600'
                  : 'bg-gray-300 text-gray-500 cursor-not-allowed'
              }`
            : `px-2 h-6 rounded-[4px] text-xs flex items-center gap-1 ${
                canSave
                  ? 'bg-green-500 text-white hover:bg-green-600'
                  : 'bg-gray-300 text-gray-500 cursor-not-allowed'
              }`
        }
        title="Save"
        disabled={!canSave}
        onClick={handleSave}
      >
        {(isDirty || additionalFormIsDirty) && <span className="text-yellow-300">*</span>}
        <i className="fa fa-save"></i>
        <span>Save</span>
      </button>
    );
  };
  
  // Render Delete Button
  const renderDeleteButton = () => {
    if (controllerModel.isFilePropertyEdit) return null;
    if (isReadOnly || isDeleteRestricted || !isShowDeleteButton) return null;
    if (controllerModel.isPreview || controllerModel.isFormImportTemplate) return null;
    if (isLocked || !controllerModel.rootPrimaryKeyValue) return null;
    
    return (
      <button
        className={
          isMobilePopup
            ? 'w-full px-3 h-8 bg-red-400 text-white rounded-[4px] text-sm hover:bg-red-500 flex items-center justify-start gap-2'
            : 'px-2 h-6 bg-red-400 text-white rounded-[4px] text-xs hover:bg-red-500 flex items-center gap-1'
        }
        title="Delete"
        onClick={handleDelete}
      >
        <i className="fa fa-trash"></i>
        <span>Delete</span>
      </button>
    );
  };
  
  // Render Refresh Button
  const renderRefreshButton = () => {
    if (isRefreshRestricted) return null;
    
    return (
      <button
        className={
          isMobilePopup
            ? 'w-full px-3 h-8 bg-indigo-500 text-white rounded-[4px] text-sm hover:bg-indigo-600 flex items-center justify-start gap-2'
            : 'px-2 h-6 bg-indigo-500 text-white rounded-[4px] text-xs hover:bg-indigo-600 flex items-center gap-1'
        }
        title="Refresh"
        onClick={handleRefresh}
      >
        <i className="fa fa-refresh"></i>
        <span>Refresh</span>
      </button>
    );
  };
  
  // Render Calculation Button
  const renderCalculationButton = () => {
    if (!isMasterDetailForm || isCalculateRestricted || !isShowCalculateButton) return null;
    if (controllerModel.isFilePropertyEdit || isLocked) return null;
    
    return (
      <button
        className={
          isMobilePopup
            ? 'w-full px-3 h-8 bg-purple-500 text-white rounded-[4px] text-sm hover:bg-purple-600 flex items-center justify-start gap-2'
            : 'px-2 h-6 bg-purple-500 text-white rounded-[4px] text-xs hover:bg-purple-600 flex items-center gap-1'
        }
        title="Calculation"
        onClick={handleCalculate}
      >
        <i className="fa-solid fa-calculator" aria-hidden />
        <span>Calculation</span>
      </button>
    );
  };
  
  // Render Print Button
  const renderPrintButton = () => {
    if (controllerModel.isFilePropertyEdit) return null;
    if (!isMasterDetailForm || isPrintRestricted || !isShowPrintButton) return null;
    if (controllerModel.isPreview) return null;
    
    return (
      <button
        className={
          isMobilePopup
            ? 'w-full px-3 h-8 bg-teal-500 text-white rounded-[4px] text-sm hover:bg-teal-600 flex items-center justify-start gap-2'
            : 'px-2 h-6 bg-teal-500 text-white rounded-[4px] text-xs hover:bg-teal-600 flex items-center gap-1'
        }
        title="Print"
        onClick={handlePrint}
      >
        <i className="fa fa-print"></i>
        <span>Print</span>
      </button>
    );
  };
  
  // Render Import & Close Button
  const renderImportCloseButton = () => {
    if (!controllerModel.isFormImportTemplate || isLocked) return null;
    
    return (
      <button
        className={
          isMobilePopup
            ? 'w-full px-3 h-8 bg-blue-400 text-white rounded-[4px] text-sm hover:bg-blue-500 flex items-center justify-start gap-2'
            : 'px-2 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center gap-1'
        }
        title="Import & Close"
        onClick={() => {
          // TODO: Implement import and close
        }}
      >
        <i className="fa fa-arrow-circle-down"></i>
        <span>Import & Close</span>
      </button>
    );
  };
  
  // Render Link Target Buttons
  const renderLinkTargets = () => {
    if (controllerModel.isFilePropertyEdit) return null;
    if (!rootUnit || isLocked) return null;
    
    return linkTargets.map((linkTarget: any) => {
      // TODO: Check restrictions and conditions
      return (
        <button
          key={linkTarget.Id}
          className={
            isMobilePopup
              ? 'w-full px-3 h-8 bg-gray-400 text-white rounded-[4px] text-sm hover:bg-gray-500 flex items-center justify-start gap-2'
              : 'px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1'
          }
          title={linkTarget.NavigationActionName}
          onClick={() => handleLinkTarget(linkTarget)}
        >
          <i className="fa fa-files-o"></i>
          <span>{linkTarget.NavigationActionName}</span>
        </button>
      );
    });
  };
  
  // Render Search Buttons
  const renderSearchButtons = () => {
    if (controllerModel.isFilePropertyEdit) return null;
    const buttons = [];
    
    // Default Search
    if (transactionDto?.DefaultTransactionSearchId && !isLocked) {
      buttons.push(
        <button
          key="default-search"
          className={
            isMobilePopup
              ? 'w-full px-3 h-8 bg-gray-400 text-white rounded-[4px] text-sm hover:bg-gray-500 flex items-center justify-start gap-2'
              : 'px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1'
          }
          title="Search"
          onClick={() => {
            // TODO: Open default search
          }}
        >
          <i className="fa fa-search"></i>
          <span>Search</span>
        </button>
      );
    }
    
    // Linked Searches
    linkedSearches.forEach((linkedSearch: any) => {
      if (linkedSearch.Action !== 1 && !isLocked) { // Not ViewSearchResult
        buttons.push(
          <button
            key={`linked-search-${linkedSearch.Id}`}
            className={
              isMobilePopup
                ? 'w-full px-3 h-8 bg-gray-400 text-white rounded-[4px] text-sm hover:bg-gray-500 flex items-center justify-start gap-2'
                : 'px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1'
            }
            title={linkedSearch.Name}
            onClick={() => handleLinkedSearch(linkedSearch, rootUnit?.Id)}
          >
            <i className="fa fa-search"></i>
            <span>{linkedSearch.Name}</span>
          </button>
        );
      }
    });
    
    return buttons;
  };
  
  // Render Report Menu
  const renderReportMenu = () => {
    if (controllerModel.isFilePropertyEdit) return null;
    if (!isMasterDetailForm || !hasMultipleReports) return null;
    
    return (
      <div className={`relative ${isMobilePopup ? 'w-full' : ''}`} ref={reportMenuRef}>
        <button
          className={
            isMobilePopup
              ? 'w-full px-3 h-8 bg-gray-400 text-white rounded-[4px] text-sm hover:bg-gray-500 flex items-center justify-start gap-2'
              : 'px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1'
          }
          onClick={() => setReportMenuOpen(!reportMenuOpen)}
        >
          <i className="fa fa-line-chart"></i>
          <span>Report</span>
          <i className="fa fa-caret-down ml-1"></i>
        </button>
        
        {reportMenuOpen && (
          <div
            className={`absolute top-full ${isMobilePopup ? 'left-0 right-0' : 'left-0'} mt-1 border rounded shadow-lg z-50 ${
              isMobilePopup ? 'min-w-0 w-full' : 'min-w-48'
            } ${theme.mainContentSection} ${t('border_mainContentSection')}`}
          >
            <div className="py-1">
              {reports.map((report: any) => (
                <button
                  key={report.ReportId}
                  className={`w-full text-left px-4 py-2 text-sm ${theme.contextMenu}`}
                  onClick={() => {
                    handleReport(report.ReportId);
                    setReportMenuOpen(false);
                  }}
                >
                  {report.ReportDisplayName}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  };
  
  const renderWorkflowMonitorButton = () => {
    if (!isWorkflowAutomation || !workflowHasRootPk || !workflowHasStarted) return null;
    return (
      <button
        type="button"
        className={
          isMobilePopup
            ? 'w-full px-3 h-8 bg-gray-400 text-white rounded-[4px] text-sm hover:bg-gray-500 flex items-center justify-start gap-2'
            : 'px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1'
        }
        title="Monitor Workflow Latest Execution"
        onClick={() => onOpenWorkflowExecutionMonitor?.()}
      >
        <i className="fa-solid fa-magnifying-glass" aria-hidden />
        <span>Monitor Workflow Latest Execution</span>
      </button>
    );
  };

  // Render Command Buttons
  const renderCommandButtons = () => {
    if (controllerModel.isFilePropertyEdit) return null;
    if (isLocked) return null;
    
    const restrictedCommands = transactionDto?.RestrictedTransactionCommandIdList || [];

    const allowed = commandActions
      .filter((cmd: any) => !restrictedCommands.includes(cmd.Id))
      .map((cmd: any) => {
        const conditionMet = isWorkflowCommandConditionMet(cmd);
        return {
          ...cmd,
          _label: isWorkflowAutomation ? `Start Run: ${cmd.Name || ''}` : cmd.Name,
          _isDisabled: !conditionMet,
          _isVisible:
            !isWorkflowAutomation ||
            (workflowHasRootPk && !workflowHasStarted && conditionMet),
        };
      })
      .filter((cmd: any) => cmd._isVisible);

    if (allowed.length === 0) return null;

    // If commands > 3, collapse into dropdown menu.
    if (!isMobilePopup && allowed.length > 3) {
      return (
        <div className="relative">
          <button
            type="button"
            ref={commandMenuButtonRef}
            className={`px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1`}
            title="Command"
            onClick={() => setCommandMenuOpen((p) => !p)}
          >
            <i className="fa fa-files-o" aria-hidden />
            <span>Command</span>
            <i className="fa fa-caret-down" aria-hidden />
          </button>

          {commandMenuOpen && typeof document !== 'undefined'
            ? createPortal(
                <div
                  ref={commandMenuRef}
                  className={`fixed z-[5000] mt-2 w-auto min-w-max max-w-none rounded-[4px] shadow-lg ${theme.mainContentSection} ring-1 ring-black ring-opacity-5 max-h-96 overflow-y-auto whitespace-nowrap`}
                  style={(() => {
                    const r = commandMenuButtonRef.current?.getBoundingClientRect?.();
                    if (!r) return { right: 12, top: 64 };
                    const top = Math.round(r.bottom + 8);
                    const right = Math.max(8, Math.round(window.innerWidth - r.right));
                    return { top, right };
                  })()}
                  role="menu"
                  onMouseDown={(e) => e.stopPropagation()}
                >
                  <div className="py-1" role="menu" aria-orientation="vertical">
                    {allowed.map((cmd: any) => (
                      <button
                        key={cmd.Id}
                        type="button"
                        className={`flex w-full items-center px-4 py-2 text-sm whitespace-nowrap ${theme.button_default} hover:bg-gray-100 dark:hover:bg-gray-700`}
                        disabled={cmd._isDisabled}
                        onClick={() => {
                          setCommandMenuOpen(false);
                          handleCommandAction(cmd.Id, cmd.ActionType);
                        }}
                        role="menuitem"
                        title={cmd._label || cmd.Name}
                      >
                        <span className="whitespace-nowrap">{cmd._label || cmd.Name}</span>
                      </button>
                    ))}
                  </div>
                </div>,
                document.body
              )
            : null}
        </div>
      );
    }

    // Otherwise, render as individual buttons (mobile: always).
    return allowed.map((cmd: any) => {
      const isDisabled = cmd._isDisabled === true;
      return (
        <button
          key={cmd.Id}
          className={
            isMobilePopup
              ? `w-full px-3 h-8 rounded-[4px] text-sm flex items-center justify-start gap-2 ${
                  isDisabled ? 'bg-gray-300 text-gray-500 cursor-not-allowed' : 'bg-gray-400 text-white hover:bg-gray-500'
                }`
              : `px-2 h-6 rounded-[4px] text-xs flex items-center gap-1 ${
                  isDisabled ? 'bg-gray-300 text-gray-500 cursor-not-allowed' : 'bg-gray-400 text-white hover:bg-gray-500'
                }`
          }
          title={cmd._label || cmd.Name}
          disabled={isDisabled}
          onClick={() => handleCommandAction(cmd.Id, cmd.ActionType)}
        >
          <i className="fa fa-files-o" aria-hidden />
          <span>{cmd._label || cmd.Name}</span>
        </button>
      );
    });
  };
  
  // Render URL Link Button
  const renderUrlLinkButton = () => {
    if (controllerModel.isFilePropertyEdit) return null;
    if (!controllerModel.rootPrimaryKeyValue) return null;
    
    return (
      <button
        className={
          isMobilePopup
            ? 'w-full px-3 h-8 bg-cyan-500 text-white rounded-[4px] text-sm hover:bg-cyan-600 flex items-center justify-start gap-2'
            : 'px-2 h-6 bg-cyan-500 text-white rounded-[4px] text-xs hover:bg-cyan-600 flex items-center gap-1'
        }
        title="URL Link"
        onClick={openShareLinkPopup}
      >
        <i className="fa fa-external-link"></i>
        <span>URL Link</span>
      </button>
    );
  };

  return (
    <>
      <div
        className={
          isMobilePopup
            ? 'w-full flex flex-col items-stretch px-0 gap-2'
            : 'h-full flex items-center px-2 gap-2 flex-wrap'
        }
      >
      {/* {transactionName && (
        <span className="text-sm font-semibold mr-2 text-gray-700 dark:text-gray-300">
          {transactionName}
        </span>
      )} */}
      
      {/* Divider */}
      {/* <div className="w-px h-6 bg-gray-300 dark:bg-gray-600"></div> */}
      
      {/* Configuration Menu */}
      {renderConfigurationMenu()}

      {configBuilderOpen && configBuilderProps && typeof document !== 'undefined' && createPortal(
        <ApplicationFormBuilder
          key={`afb-${builderMountKey}`}
          isOpen={configBuilderOpen}
          onClose={() => {
            const needConfirm = configBuilderProps?.reloadOnClose === true;
            const section = String(configBuilderProps?.defaultSectionCode ?? '').toLowerCase();
            setConfigBuilderOpen(false);
            if (needConfirm) {
              setRefreshConfirmTitle('Refresh current form?');
              setRefreshConfirmMessage(
                section === 'form'
                  ? 'Form layout has been updated. Do you want to reload the current form to make the changes take effect?'
                  : 'Data model has been updated. Do you want to reload the current form to make the changes take effect?'
              );
              setRefreshConfirmOpen(true);
            }
          }}
          onRefresh={undefined}
          applicationId={configBuilderProps.applicationId}
          defaultSectionCode={configBuilderProps.defaultSectionCode}
          isCreateNewItem={false}
          formId={configBuilderProps.formId}
          transactionId={configBuilderProps.transactionId}
          workflowId={configBuilderProps.workflowId}
          transactionType={configBuilderProps.transactionType}
          formLayoutType={configBuilderProps.formLayoutType}
          modelName={configBuilderProps.modelName}
          initialNeedToEditUnitId={configBuilderProps.initialNeedToEditUnitId ?? null}
          initialFormDesignFieldFocus={configBuilderProps.initialFormDesignFieldFocus ?? null}
        />,
        document.body
      )}

      {unitContextMenu && typeof document !== 'undefined'
        ? createPortal(
            <div
              ref={unitContextMenuRef}
              className={`fixed z-[5000] min-w-[260px] max-w-[90vw] max-h-[420px] overflow-auto rounded-md border shadow-xl py-1 ${theme.mainContentSection} ${t('border_mainContentSection')}`}
              style={{ left: unitContextMenu.x, top: unitContextMenu.y }}
              role="menu"
              onMouseDown={(e) => e.stopPropagation()}
            >
              <button
                type="button"
                className={`w-full text-left px-3 py-2 text-sm ${theme.contextMenu}`}
                onClick={(e) => {
                  openRuntimeTransactionUnitEditor(e, unitContextMenu.unitId);
                }}
              >
                Unit Setting: {unitContextMenu.unitDisplayName} ({unitContextMenu.unitId})
              </button>
              <div className={`border-t ${t('border_mainContentSection')}`} />
              {unitContextMenu.fields.map((f) => (
                <button
                  key={f.id}
                  type="button"
                  className={`w-full text-left px-3 py-1.5 text-sm ${theme.contextMenu}`}
                  onClick={(e) => {
                    openRuntimeTransactionFieldEditor(
                      e,
                      f.id,
                      f.display,
                      unitContextMenu.layoutItemId,
                      true
                    );
                  }}
                >
                  {f.display} ({f.id})
                </button>
              ))}
            </div>,
            document.body
          )
        : null}

      {rootUnitsMenu && typeof document !== 'undefined'
        ? createPortal(
            <div
              ref={rootUnitsMenuRef}
              className={`fixed z-[5000] min-w-[260px] max-w-[90vw] max-h-[360px] overflow-auto rounded-md border shadow-xl py-1 ${theme.mainContentSection} ${t('border_mainContentSection')}`}
              style={{ left: rootUnitsMenu.x, top: rootUnitsMenu.y }}
              role="menu"
              onMouseDown={(e) => e.stopPropagation()}
            >
              {(transactionDto?.AppTransactionUnitList || []).map((u: any) => (
                <button
                  key={u.Id}
                  type="button"
                  className={`w-full text-left px-3 py-2 text-sm ${theme.contextMenu}`}
                  onClick={(e) => {
                    openRuntimeTransactionUnitEditor(e, Number(u.Id));
                    setRootUnitsMenu(null);
                  }}
                >
                  Unit Setting: {u.UnitDisplayName || u.UnitName || u.Id} ({u.Id})
                </button>
              ))}
            </div>,
            document.body
          )
        : null}

      {fieldSettingDrawer && controllerModel?.transactionId ? (
        <RuntimeFieldSettingDrawer
          isOpen
          transactionId={Number(controllerModel.transactionId)}
          transactionFieldId={fieldSettingDrawer.transactionFieldId}
          layoutItemId={fieldSettingDrawer.layoutItemId}
          isGridField={fieldSettingDrawer.isGrid === true}
          anchorRect={fieldSettingDrawer.anchorRect}
          onClose={() => setFieldSettingDrawer(null)}
          onSaved={() => (onReloadCurrentForm ?? onRefresh)?.()}
        />
      ) : null}

      {refreshConfirmOpen && typeof document !== 'undefined'
        ? createPortal(
            <div
              className="fixed inset-0 flex items-center justify-center"
              style={{ zIndex: appHelper.getNextPopupZIndex() }}
              role="dialog"
              aria-modal="true"
              aria-labelledby="form-refresh-confirm-title"
            >
              <div className="absolute inset-0 bg-black/40" aria-hidden />
              <div
                className={`relative z-10 w-[520px] max-w-[95vw] overflow-hidden rounded-md border shadow-xl ${theme.mainContentSection}`}
                onMouseDown={(e) => e.stopPropagation()}
              >
                <div className={`flex items-center justify-between border-b px-3 py-2 ${theme.mainContentSection}`}>
                  <div id="form-refresh-confirm-title" className={`text-sm font-semibold ${theme.title}`}>
                    {refreshConfirmTitle}
                  </div>
                  <button
                    type="button"
                    className={`rounded-[4px] p-1.5 ${theme.button_default}`}
                    onClick={() => setRefreshConfirmOpen(false)}
                    title="Close"
                    aria-label="Close"
                  >
                    <i className="fa-solid fa-xmark" aria-hidden />
                  </button>
                </div>
                <div className="px-3 py-3 text-sm">
                  <div className={`${theme.label}`}>{refreshConfirmMessage}</div>
                </div>
                <div className={`flex items-center justify-end gap-2 border-t px-3 py-2 ${theme.mainContentSection}`}>
                  <button
                    type="button"
                    className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                    onClick={() => {
                      setRefreshConfirmOpen(false);
                      (onReloadCurrentForm ?? onRefresh)?.();
                    }}
                  >
                    Yes
                  </button>
                  <button
                    type="button"
                    className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                    onClick={() => setRefreshConfirmOpen(false)}
                  >
                    No
                  </button>
                </div>
              </div>
            </div>,
            document.body
          )
        : null}
      
      {/* Import & Close (if template import) */}
      {renderImportCloseButton()}
      
      {/* Calculation (MasterDetail only) */}
      {renderCalculationButton()}
      
      {/* Save Button */}
      {renderSaveButton()}
      
      {/* Delete Button */}
      {renderDeleteButton()}
      
      {/* Refresh Button */}
      {renderRefreshButton()}
      
      {/* Print Button (MasterDetail only) */}
      {renderPrintButton()}
      
      {/* Link Target Buttons */}
      {renderLinkTargets()}
      
      {/* Search Buttons */}
      {renderSearchButtons()}
      
      {/* Report Menu (if multiple reports) */}
      {renderReportMenu()}
      
      {/* Command Buttons */}
      {renderCommandButtons()}

      {/* Workflow: Monitor latest execution (Angular _FormMainMenus when StartTime is set) */}
      {renderWorkflowMonitorButton()}
      
      {/* URL Link */}
      {renderUrlLinkButton()}
      </div>

      {shareLinkOpen && typeof document !== 'undefined'
        ? createPortal(
            <div
              className="fixed inset-0 flex items-center justify-center"
              style={{ zIndex: appHelper.getNextPopupZIndex() }}
              role="dialog"
              aria-modal="true"
              aria-label="Standalone form link"
              onMouseDown={() => setShareLinkOpen(false)}
            >
              <div className="absolute inset-0 bg-black/40" aria-hidden />
              <div
                className={`relative z-10 w-[640px] max-w-[95vw] overflow-hidden rounded-md border shadow-xl ${theme.mainContentSection}`}
                onMouseDown={(e) => e.stopPropagation()}
              >
                <div className={`flex items-center justify-between border-b px-3 py-2 ${theme.mainContentSection}`}>
                  <div className={`text-sm font-semibold ${theme.title}`}>Standalone Form Link</div>
                  <button
                    type="button"
                    className={`rounded-[4px] p-1.5 ${theme.button_default}`}
                    onClick={() => setShareLinkOpen(false)}
                    title="Close"
                    aria-label="Close"
                  >
                    <i className="fa-solid fa-xmark" aria-hidden />
                  </button>
                </div>

                <div className="px-4 py-4">
                  <div className="grid grid-cols-1 gap-5 items-start">
                    <div>
                      <div className="flex items-center justify-between gap-3 mb-1">
                        <div className={`text-xs font-semibold ${theme.label}`}>URL</div>
                        <div className="flex items-center gap-2">
                          <button
                            type="button"
                            className={`rounded-[4px] px-3 h-8 text-xs ${theme.button_default}`}
                            onClick={copyShareLink}
                            title="Copy URL"
                          >
                            {shareLinkCopied ? 'Copied' : 'Copy'}
                          </button>
                        </div>
                      </div>
                      {(() => {
                        const url = buildStandaloneFormUrl() ?? '';
                        return url ? (
                          <a
                            href={url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className={`block text-xs underline text-blue-700 break-all`}
                            title={url}
                          >
                            {url}
                          </a>
                        ) : (
                          <div className={`text-xs ${theme.label}`}>No URL.</div>
                        );
                      })()}
                    </div>

                    <div className="flex flex-col items-center pt-2">
                      <div className="bg-white p-5 rounded border shadow-sm">
                        <QRCodeCanvas value={buildStandaloneFormUrl() ?? ''} size={280} includeMargin />
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>,
            document.body
          )
        : null}

      {searchPopupState && (
        <EmbeddedLinkedPopupFrame
          zIndex={searchPopupState.popupZIndex}
          title={searchPopupState.title || 'Open Search'}
          frameInstanceKey={searchPopupState.popupZIndex}
          toolbarLeading={
            searchPopupState.showConfirmClose ? (
              <button
                type="button"
                className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                onClick={() => {
                  const ls = searchPopupState.linkedSearch;
                  const root = dataModel?.currentFormData;
                  const sel = mainMenuLinkedSearchRef.current?.getSelectedResults?.() ?? [];
                  const unitFields = rootUnit?.AppTransactionFieldList ?? [];
                  if (ls && root && sel.length > 0) {
                    const nextRoot = applyLinkedSearchResultToRootRow(sel[0], ls, root, unitFields);
                    if (nextRoot) {
                      onDataModelChange?.({
                        ...dataModel,
                        currentFormData: nextRoot,
                      });
                    }
                  }
                  setSearchPopupState(null);
                }}
              >
                <i className="fa-solid fa-check mr-1" aria-hidden /> Confirm &amp; Close
              </button>
            ) : undefined
          }
          toolbarTrailing={
            <button
              type="button"
              className={`rounded-[4px] p-1.5 ${theme.button_default}`}
              onClick={() => setSearchPopupState(null)}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          <AppSearch ref={mainMenuLinkedSearchRef} embeddedParamObj={searchPopupState.paramObj} />
        </EmbeddedLinkedPopupFrame>
      )}

      {linkTargetPopupState && (
        <EmbeddedLinkedPopupFrame
          zIndex={linkTargetPopupState.popupZIndex}
          title={linkTargetPopupState.title || 'Navigate'}
          frameInstanceKey={linkTargetPopupState.popupZIndex}
          toolbarLeading={
            linkTargetPopupState.pickerContext ? (
              <button
                type="button"
                className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                onClick={() => {
                  const sel = mainMenuLinkTargetMdRef.current?.getSelectedResults?.() ?? [];
                  const pc = linkTargetPopupState.pickerContext;
                  const root = dataModel?.currentFormData;
                  if (pc && root && sel.length > 0) {
                    applyLinkTargetMasterDataSelectionToRow(pc.linkTarget, sel[0], root);
                    onDataModelChange?.({
                      ...dataModel,
                      currentFormData: { ...root, IsDirty: true },
                    });
                  }
                  setLinkTargetPopupState(null);
                }}
              >
                <i className="fa-solid fa-check mr-1" aria-hidden /> Confirm &amp; Close
              </button>
            ) : undefined
          }
          toolbarTrailing={
            <button
              type="button"
              className={`rounded-[4px] p-1.5 ${theme.button_default}`}
              onClick={() => setLinkTargetPopupState(null)}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          {(() => {
            const route = String(linkTargetPopupState.routeBasePath || '').toLowerCase();
            const paramObj = linkTargetPopupState.paramObj ?? {};
            const parsedParam2 =
              typeof paramObj?.param2 === 'string'
                ? (() => {
                    try {
                      return JSON.parse(paramObj.param2);
                    } catch {
                      return {};
                    }
                  })()
                : (paramObj?.param2 ?? {});

            if (route === 'masterdatamanagement') {
              return <AppSearch ref={mainMenuLinkTargetMdRef} embeddedParamObj={paramObj} />;
            }
            if (route === 'formmasterdetail') {
              const embeddedTransactionId = Number(paramObj?.id ?? 0);
              if (!embeddedTransactionId) return <div className="p-3 text-xs">Invalid navigation target.</div>;
              return (
                <FormMasterDetail
                  embedded={{
                    embeddedTransactionId,
                    embeddedRootPrimaryKeyValue: paramObj?.param1 ?? null,
                    embeddedParam2: parsedParam2,
                  }}
                />
              );
            }
            if (route === 'formlistedit') {
              const embeddedTransactionId = Number(paramObj?.id ?? 0);
              if (!embeddedTransactionId) return <div className="p-3 text-xs">Invalid navigation target.</div>;
              return (
                <FormListEdit
                  embedded={{
                    embeddedTransactionId,
                    embeddedParam2: parsedParam2,
                  }}
                />
              );
            }
            return (
              <div className="p-3 text-xs">Unsupported popup target: {String(linkTargetPopupState.routeBasePath || '')}</div>
            );
          })()}
        </EmbeddedLinkedPopupFrame>
      )}

      {workflowEditorPopup && (
        <EmbeddedLinkedPopupFrame
          zIndex={workflowEditorPopup.popupZIndex}
          title={workflowEditorPopup.modelName || 'Workflow Automation Editor'}
          frameInstanceKey={workflowEditorPopup.popupZIndex}
          defaultWidth="96vw"
          defaultHeight="92vh"
          toolbarTrailing={
            <button
              type="button"
              className={`rounded-[4px] p-1.5 ${theme.button_default}`}
              onClick={closeWorkflowEditorPopup}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          <div className="w-full h-full overflow-hidden">
            <WorkflowAutomationEditor
              embedded={{
                transactionId: workflowEditorPopup.transactionId,
                saasApplicationId: workflowEditorPopup.saasApplicationId,
                modelName: workflowEditorPopup.modelName,
                onClose: closeWorkflowEditorPopup,
              }}
            />
          </div>
        </EmbeddedLinkedPopupFrame>
      )}
    </>
  );
};

export default FormMainMenus;
