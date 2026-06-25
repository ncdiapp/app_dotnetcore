import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { useParams, useNavigate } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { updateCurrentTabLabel, updateActiveTabPath, getDataModelFromCache, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { buildRoutePathFromParamObj, getReactPathForRouteCode } from '../../helper/navigationHelper';
import { useTabDataAutoCache } from '../../redux/hooks/useTabNavigation';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useTheme } from '../../redux/hooks/useTheme';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import MasterDetailEditLayoutForm from './FormMasterDetail/MasterDetailEditLayoutForm';
import { dynamicLayoutService } from '../../webapi/dynamiclayoutsvc';
import { EmbeddedLinkedPopupFrame } from './EmbeddedLinkedPopupFrame';
import WorkflowExecutionMonitor from '../workflow/WorkflowExecutionMonitor';
import appHelper from '../../helper/appHelper';
import { applyLinkTargetValueMappingToFormData } from './FormMasterDetail/MasterDetailFlexLayoutForm/FormControls/formDataBindingHelper';

/** When provided, form runs in embedded mode (e.g. User Editor "User Detail Info" tab) without route params. */
export type FormMasterDetailEmbeddedProps = {
    embeddedTransactionId: number;
    embeddedRootPrimaryKeyValue: string | number | null;
    embeddedParam2?: Record<string, unknown>;
};

export type FormMasterDetailActionApi = {
    save: () => Promise<void>;
    refresh: () => void;
    reload: () => void;
};

export type TemplateHeaderEmbeddedForm = {
    key: string;
    transactionId: number;
    rootPrimaryKeyValue: string | number | null;
    param2?: Record<string, unknown>;
};

export type FormMasterDetailSaveState = {
    isDirty: boolean;
    isNewForm: boolean;
};

type FormMasterDetailProps = {
    embedded?: FormMasterDetailEmbeddedProps | null;
    /** Template form-group header transactions rendered inside TemplateHeaderContainer (menu stays on main). */
    templateHeaderForms?: TemplateHeaderEmbeddedForm[];
    /** Optional: expose Save/Refresh/Reload for a parent toolbar (e.g. My Profile Details tab). */
    onFormActionApiReady?: (api: FormMasterDetailActionApi) => void;
    /** Notify parent when dirty/new state changes (template header forms report to main toolbar). */
    onFormSaveStateChange?: (state: FormMasterDetailSaveState) => void;
    /** When set, overrides field-setting buttons visibility (template header forms follow the main toolbar toggle). */
    forceEnableFormConfigButtons?: boolean;
};

type TemplateHeaderEmbeddedFormProps = {
    hdr: TemplateHeaderEmbeddedForm;
    onApiReady: (key: string, api: FormMasterDetailActionApi) => void;
    onApiUnmount: (key: string) => void;
    onSaveStateChange: (key: string, state: FormMasterDetailSaveState) => void;
    forceEnableFormConfigButtons?: boolean;
};

const TemplateHeaderEmbeddedForm: React.FC<TemplateHeaderEmbeddedFormProps> = ({
    hdr,
    onApiReady,
    onApiUnmount,
    onSaveStateChange,
    forceEnableFormConfigButtons,
}) => {
    const handleFormActionApiReady = useCallback(
        (api: FormMasterDetailActionApi) => {
            onApiReady(hdr.key, api);
        },
        [hdr.key, onApiReady],
    );

    const handleFormSaveStateChange = useCallback(
        (state: FormMasterDetailSaveState) => {
            onSaveStateChange(hdr.key, state);
        },
        [hdr.key, onSaveStateChange],
    );

    useEffect(() => () => onApiUnmount(hdr.key), [hdr.key, onApiUnmount]);

    return (
        <FormMasterDetail
            embedded={{
                embeddedTransactionId: hdr.transactionId,
                embeddedRootPrimaryKeyValue: hdr.rootPrimaryKeyValue,
                embeddedParam2: hdr.param2,
            }}
            onFormActionApiReady={handleFormActionApiReady}
            onFormSaveStateChange={handleFormSaveStateChange}
            forceEnableFormConfigButtons={forceEnableFormConfigButtons}
        />
    );
};

const FormMasterDetail: React.FC<FormMasterDetailProps> = ({
    embedded = null,
    templateHeaderForms = [],
    onFormActionApiReady,
    onFormSaveStateChange,
    forceEnableFormConfigButtons,
}) => {
    const { theme } = useTheme();
    const dispatch = useDispatch();
    const navigate = useNavigate();
    const { showError } = useErrorMessage();

    const { param } = useParams<{ param: string }>();

    const isEmbedded = embedded != null && embedded.embeddedTransactionId != null;

    const [paramObj, setParamObj] = useState<any>(() => {
        if (isEmbedded && embedded) {
            return {
                id: embedded.embeddedTransactionId,
                param1: embedded.embeddedRootPrimaryKeyValue ?? undefined,
                param2: embedded.embeddedParam2 ?? {},
            };
        }
        if (param) {
            try {
                const decodedParam = decodeURIComponent(param);
                return JSON.parse(decodedParam);
            } catch (error) {
                console.error('Error parsing param JSON:', error);
                return {};
            }
        }
        return {};
    });

    useEffect(() => {
        if (isEmbedded && embedded) {
            setParamObj({
                id: embedded.embeddedTransactionId,
                param1: embedded.embeddedRootPrimaryKeyValue ?? undefined,
                param2: embedded.embeddedParam2 ?? {},
            });
            return;
        }
        if (param) {
            try {
                const decodedParam = decodeURIComponent(param);
                const parsed = JSON.parse(decodedParam);
                setParamObj(parsed);
            } catch (error) {
                console.error('Error parsing param JSON:', error);
                showError('Invalid URL parameters. ' + error);
                setParamObj({});
            }
        } else {
            setParamObj({});
        }
    }, [param, showError, isEmbedded, embedded?.embeddedTransactionId, embedded?.embeddedRootPrimaryKeyValue, embedded?.embeddedParam2]);

    const transactionId = paramObj.id ? Number(paramObj.id) : null;
    const rootPrimaryKeyValue = paramObj.param1 ?? null;

    const param2Obj = (() => {
        if (!paramObj.param2) return {};
        if (typeof paramObj.param2 === 'string') {
            try {
                return JSON.parse(paramObj.param2);
            } catch {
                return {};
            }
        }
        return paramObj.param2;
    })();

    // Controller model state
    const [controllerModel, setControllerModel] = useState<any>({
        transactionId: transactionId,
        rootPrimaryKeyValue: rootPrimaryKeyValue,
        formRequestMode: rootPrimaryKeyValue ? 'Edit' : 'New',
        uiId: `form_${Date.now()}`,
        // Angular: param2Obj.isEnableFormConfigButtons || false
        isEnableFormConfigButtons: param2Obj?.isEnableFormConfigButtons ?? false,
        isAutoReopenCurrentFormAfterChange: true,
        param2Obj: param2Obj,
        isPreview: param2Obj.isPreview || false,
        isPrint: param2Obj.isPrint || false,
        isConfigTestRun: param2Obj.isPreview || false,
        isFormImportTemplate: param2Obj.isFormImportTemplate || false,
        isFilePropertyEdit: param2Obj.isFilePropertyEdit || false,
        opennedFormAutoExecuteCommandId: param2Obj.opennedFormAutoExecuteCommandId || null,
        isTemplateHeader: param2Obj.isTemplateHeader || false,
        TemplateHeaderName: param2Obj.TemplateHeaderName || '',
        isNeedExecuteDataloadOnOpenNewForm: param2Obj.isNeedExecuteDataloadOnOpenNewForm !== false,
        isNeedPreSaveNewFormData: param2Obj.isNeedPreSaveNewFormData || false,
    });

    const toggleFormConfigButtons = useCallback(() => {
        setControllerModel((prev: any) => ({
            ...prev,
            isEnableFormConfigButtons: !(prev?.isEnableFormConfigButtons ?? false),
        }));
    }, []);

    // Keep controllerModel identity in sync with the live transaction so toolbar actions
    // (Configuration, Edit Workflow, Print, URL Link) target the CURRENTLY selected transaction,
    // not the first one. In a transaction form group the embedded <FormMasterDetail> instance is
    // reused (no key) when switching sidebar items, so transactionId/rootPrimaryKeyValue change via
    // props without a remount and controllerModel would otherwise stay pinned to the first one.
    useEffect(() => {
        setControllerModel((prev: any) => {
            if (
                prev?.transactionId === transactionId &&
                prev?.rootPrimaryKeyValue === rootPrimaryKeyValue
            ) {
                return prev;
            }
            return {
                ...prev,
                transactionId,
                rootPrimaryKeyValue,
                formRequestMode: rootPrimaryKeyValue ? 'Edit' : 'New',
            };
        });
    }, [transactionId, rootPrimaryKeyValue]);

    // Template header forms follow the main form's "Enable Field Setting Buttons" toggle.
    useEffect(() => {
        if (forceEnableFormConfigButtons === undefined) return;
        setControllerModel((prev: any) => {
            if (prev?.isEnableFormConfigButtons === forceEnableFormConfigButtons) return prev;
            return { ...prev, isEnableFormConfigButtons: forceEnableFormConfigButtons };
        });
    }, [forceEnableFormConfigButtons]);

    // Data model state
    const [dataModel, setDataModel] = useState<any>({
        rootIdInCache: rootPrimaryKeyValue,
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
        isLoading: false,
    });

    // Form structure data ref (similar to _FormStructureData in AngularJS)
    const formStructureDataRef = useRef<any>(null);

    // Cache for DynamicLayout data (per transactionId)
    // This ensures we only call the API once per transactionId per user session
    const dynamicLayoutCacheRef = useRef<Map<string, any>>(new Map());

    // Store transactionExDto from DynamicLayout (loaded once per transactionId)
    const transactionExDtoRef = useRef<any>(null);

    const lastLoadedKeyRef = useRef<string | null>(null);

    const [workflowMonitorPopup, setWorkflowMonitorPopup] = useState<{
        popupZIndex: number;
        workflowTransactionId: number;
        rootPrimaryKeyValue: string | number;
    } | null>(null);

    const [templateHeaderContainerEl, setTemplateHeaderContainerEl] = useState<HTMLElement | null>(null);
    const headerFormApisRef = useRef<Map<string, FormMasterDetailActionApi>>(new Map());
    const headerSaveStateRef = useRef<Map<string, FormMasterDetailSaveState>>(new Map());
    const [headerApiVersion, setHeaderApiVersion] = useState(0);
    const [headerSaveStateVersion, setHeaderSaveStateVersion] = useState(0);

    const handleTemplateHeaderContainerReady = useCallback((element: HTMLElement | null) => {
        setTemplateHeaderContainerEl(element);
    }, []);

    const registerHeaderFormApi = useCallback((key: string, api: FormMasterDetailActionApi) => {
        headerFormApisRef.current.set(key, api);
        setHeaderApiVersion((v) => v + 1);
    }, []);

    const unregisterHeaderFormApi = useCallback((key: string) => {
        let changed = false;
        if (headerFormApisRef.current.delete(key)) changed = true;
        if (headerSaveStateRef.current.delete(key)) changed = true;
        if (changed) {
            setHeaderApiVersion((v) => v + 1);
            setHeaderSaveStateVersion((v) => v + 1);
        }
    }, []);

    const handleHeaderSaveStateChange = useCallback((key: string, state: FormMasterDetailSaveState) => {
        const prev = headerSaveStateRef.current.get(key);
        if (prev?.isDirty === state.isDirty && prev?.isNewForm === state.isNewForm) return;
        headerSaveStateRef.current.set(key, state);
        setHeaderSaveStateVersion((v) => v + 1);
    }, []);

    useEffect(() => {
        headerFormApisRef.current.clear();
        headerSaveStateRef.current.clear();
        setHeaderApiVersion((v) => v + 1);
        setHeaderSaveStateVersion((v) => v + 1);
    }, [templateHeaderForms]);

    const additionalFormActionApis = useMemo(
        () => Array.from(headerFormApisRef.current.values()),
        [headerApiVersion, templateHeaderForms],
    );

    const additionalFormCanSave = useMemo(() => {
        return Array.from(headerSaveStateRef.current.values()).some(
            (state) => state.isDirty || state.isNewForm,
        );
    }, [headerSaveStateVersion]);

    const additionalFormIsDirty = useMemo(() => {
        return Array.from(headerSaveStateRef.current.values()).some((state) => state.isDirty);
    }, [headerSaveStateVersion]);

    useEffect(() => {
        if (!onFormSaveStateChange) return;
        const isDirty = dataModel.currentFormData?.IsDirty === true;
        const isNewForm =
            dataModel.currentFormData?.IsNew === true ||
            controllerModel?.formRequestMode === 'New' ||
            controllerModel?.rootPrimaryKeyValue == null ||
            String(controllerModel?.rootPrimaryKeyValue ?? '').trim() === '';
        onFormSaveStateChange({ isDirty, isNewForm });
    }, [
        onFormSaveStateChange,
        dataModel.currentFormData?.IsDirty,
        dataModel.currentFormData?.IsNew,
        controllerModel?.formRequestMode,
        controllerModel?.rootPrimaryKeyValue,
    ]);

    const buildFieldEntityMaps = useCallback((formData: any, formStructureData: any, currentDataModel: any) => {
        const dictFieldEntityDataSource = { ...(currentDataModel?.dictFieldEntityDataSource ?? {}) };
        const dictFieldEntityDataMap = { ...(currentDataModel?.dictFieldEntityDataMap ?? {}) };

        const dictEntityDataSource = formStructureData?.DictStandAloneEntityDataSource ?? {};
        const dictFieldIdMappingEntityId = formStructureData?.DictStandAloneFiledIDMappingEntityID ?? {};
        const dictCascadingFieldDataSource = formData?.DictCascadingFiledDataSource ?? {};
        const dictAutoCompleteFieldDataSource = formData?.DictAutoCompleteFieldDataSource ?? {};
        const dictForeignUnitMasterFieldLookup = formData?.DictForeignUnitMasterFieldlIdLookupItem ?? {};

        Object.keys(dictFieldIdMappingEntityId).forEach((fieldId: string) => {
            const entityId = dictFieldIdMappingEntityId[fieldId];
            const standaloneItems = entityId != null ? (dictEntityDataSource?.[entityId] ?? []) : [];
            const itemSource = dictCascadingFieldDataSource?.[fieldId]
                ?? dictAutoCompleteFieldDataSource?.[fieldId]
                ?? standaloneItems
                ?? [];

            dictFieldEntityDataSource[fieldId] = Array.isArray(itemSource) ? itemSource : [];
            dictFieldEntityDataMap[fieldId] = {
                itemsSource: dictFieldEntityDataSource[fieldId],
                valueMember: 'Id',
                displayMember: 'Display',
            };
        });

        Object.keys(dictForeignUnitMasterFieldLookup).forEach((unitId: string) => {
            const dictFieldItems = dictForeignUnitMasterFieldLookup[unitId] ?? {};
            Object.keys(dictFieldItems).forEach((fieldId: string) => {
                const itemSource = dictFieldItems[fieldId] ?? [];
                dictFieldEntityDataSource[fieldId] = Array.isArray(itemSource) ? itemSource : [];
                dictFieldEntityDataMap[fieldId] = {
                    itemsSource: dictFieldEntityDataSource[fieldId],
                    valueMember: 'Id',
                    displayMember: 'Display',
                };
            });
        });

        return { dictFieldEntityDataSource, dictFieldEntityDataMap };
    }, []);

    const reportFormLoadError = useCallback((message: string) => {
        if (param2Obj?.isEmbeddedByOtherPage) {
            appHelper.debugLog('[FormMasterDetail embedded]', message);
            return;
        }
        showError(message);
    }, [param2Obj?.isEmbeddedByOtherPage, showError]);

    // Load dynamic layout from server (cached per transactionId + root PK)
    const loadDynamicLayout = async (): Promise<any> => {
        if (!transactionId) return null;

        const layoutCacheKey = `${transactionId}-${rootPrimaryKeyValue ?? ''}`;
        if (dynamicLayoutCacheRef.current.has(layoutCacheKey)) {
            const cachedData = dynamicLayoutCacheRef.current.get(layoutCacheKey);
            appHelper.debugLog(`Using cached DynamicLayout for ${layoutCacheKey}`);
            return cachedData;
        }

        try {
            appHelper.debugLog(`Loading DynamicLayout from API for ${layoutCacheKey}`);
            const transGroupId = controllerModel.param2Obj?.transGroupId ?? controllerModel.param2Obj?.LinkTargetTransactionGroupId;
            const transactionExDto = await dynamicLayoutService.getTransactionForm(
                transactionId,
                transGroupId != null && transGroupId !== '' ? Number(transGroupId) : undefined,
                rootPrimaryKeyValue != null ? String(rootPrimaryKeyValue) : undefined,
                controllerModel?.param2Obj?.isPrint ? 'true' : undefined,
                controllerModel.opennedFormAutoExecuteCommandId,
                controllerModel.isPreview ? 'true' : undefined
            );

            dynamicLayoutCacheRef.current.set(layoutCacheKey, transactionExDto);

            return transactionExDto;
        } catch (error) {
            console.error('Error loading dynamic layout:', error);
            reportFormLoadError((error as Error).message);
            return null;
        }
    };

    // Load form structure from server
    const loadFormStructure = async (): Promise<any> => {
        if (!transactionId) return null;

        try {
            const transGroupId = controllerModel.param2Obj?.transGroupId ?? controllerModel.param2Obj?.LinkTargetTransactionGroupId;
            const formStructure = await appTransactionService.getFormStructure(
                transactionId,
                transGroupId != null && transGroupId !== '' ? Number(transGroupId) : undefined,
            );
            formStructureDataRef.current = formStructure;
            setDataModel((prev: any) => ({
                ...prev,
                currentFormStructure: formStructure
            }));
            return formStructure;
        } catch (error) {
            console.error('Error loading form structure:', error);
            reportFormLoadError((error as Error).message);
            return null;
        }
    };

    // Load form data from server
    const loadFormData = async (formStructureData: any) => {
        if (!transactionId || !formStructureData) return;

        try {
            dispatch(setIsBusy());
            setDataModel((prev: any) => ({ ...prev, doing_async: true, isLoading: true }));

            const isNew = rootPrimaryKeyValue == null || String(rootPrimaryKeyValue).trim() === '';

            // Angular parity:
            // - New form uses GetNewFormData (properly initialized DTO: TransactionId/defaults/etc.)
            // - Edit form uses GetFormData
            const transGroupId = controllerModel.param2Obj?.transGroupId ?? controllerModel.param2Obj?.LinkTargetTransactionGroupId;
            const rawFormData = isNew
                ? await appTransactionService.getNewFormData(transactionId, controllerModel.isConfigTestRun === true)
                : await appTransactionService.getFormData({
                    transactionId: transactionId,
                    rootPrimaryKeyValue: rootPrimaryKeyValue != null ? String(rootPrimaryKeyValue) : undefined,
                    transGroupId: transGroupId != null && transGroupId !== '' ? Number(transGroupId) : undefined,
                    autoExecuteCommandId: controllerModel.opennedFormAutoExecuteCommandId || null,
                    selectDataRow: param2Obj?.selecedDataRow ?? null,
                });

            let formData = {
                ...(rawFormData ?? {}),
                TransactionId: rawFormData?.TransactionId ?? transactionId,
                IsNew: rawFormData?.IsNew ?? isNew,
                IsShowSaveButton: rawFormData?.IsShowSaveButton ?? true,
            };

            const linkTargetValueMapping = param2Obj?.linkTargetValueMapping;
            if (
                isNew &&
                linkTargetValueMapping &&
                typeof linkTargetValueMapping === 'object' &&
                Object.keys(linkTargetValueMapping).length > 0
            ) {
                formData = applyLinkTargetValueMappingToFormData(
                    formData,
                    transactionExDtoRef.current,
                    linkTargetValueMapping as Record<string, unknown>,
                );
            }

            const { dictFieldEntityDataSource, dictFieldEntityDataMap } = buildFieldEntityMaps(formData, formStructureData, dataModel);
            setDataModel((prev: any) => ({
                ...prev,
                currentFormData: formData,
                dictFieldEntityDataSource,
                dictFieldEntityDataMap,
                doing_async: false,
                isLoading: false
            }));

            // Update tab label if we have display name (skip when embedded in another page, e.g. User Editor).
            // Skip when opened from search "link to data model" so "Open: {pk}" / title column is not replaced by TransactionName.
            if (
                !param2Obj.isEmbeddedByOtherPage &&
                !paramObj.preserveLinkTabTitle &&
                (formData?.Display || formData?.TransactionName)
            ) {
                dispatch(updateCurrentTabLabel(formData.Display || formData.TransactionName));
            }

        } catch (error) {
            console.error('Error loading form data:', error);
            showError('Failed to load form data: ' + (error as Error).message);
            setDataModel((prev: any) => ({ ...prev, doing_async: false, isLoading: false }));
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    // Prepare new form data (for new records)
    const prepareNewFormData = (formStructureData: any) => {
        if (!formStructureData) return;

        // Create empty form data structure based on form structure
        const newFormData: any = {
            IsDirty: false,
            IsShowSaveButton: true,
            TransactionId: transactionId ?? undefined,
            IsNew: true,
            DictOneToOneFields: {},
            DictOneToManyFields: {},
            DictSiblingOneToOneFields: {},
            RootUnitId: formStructureData.RootUnitId,
        };

        const { dictFieldEntityDataSource, dictFieldEntityDataMap } = buildFieldEntityMaps(newFormData, formStructureData, dataModel);
        setDataModel((prev: any) => ({
            ...prev,
            currentFormData: newFormData,
            dictFieldEntityDataSource,
            dictFieldEntityDataMap,
            doing_async: false,
            isLoading: false
        }));

        // Update tab label (skip when embedded)
        if (!param2Obj.isEmbeddedByOtherPage && formStructureData.TransactionName) {
            dispatch(updateCurrentTabLabel(`New ${formStructureData.TransactionName}`));
        }
    };

    // Main data loading function (can be called multiple times without re-loading DynamicLayout)
    const loadDataFromServer = async () => {
        if (!transactionId) return;

        try {
            dispatch(setIsBusy());
            setDataModel((prev: any) => ({ ...prev, isLoading: true }));

            // Ensure DynamicLayout is loaded first (has ForeignAppFormExDto.AppFormLayoutItemList)
            // Without it, form structure from getFormStructure lacks layout items → blank form
            let transactionExDto = transactionExDtoRef.current;
            if (!transactionExDto) {
                transactionExDto = await loadDynamicLayout();
                if (transactionExDto) {
                    transactionExDtoRef.current = transactionExDto;
                }
            }

            if (transactionExDto) {
                // Extract TransactionOrganizedType and form structure from AppTransactionExDto
                const transactionOrganizedType = transactionExDto.TransactionOrganizedType;
                const foreignAppFormExDto = transactionExDto.ForeignAppFormExDto;

                // Also load form structure from existing API (for compatibility)
                const formStructureData = await loadFormStructure();

                // Merge the form structure data with dynamic layout data
                if (formStructureData) {
                    const mergedFormStructure = {
                        ...formStructureData,
                        TransactionOrganizedType: transactionOrganizedType || formStructureData.TransactionOrganizedType,
                        ForeignAppFormExDto: foreignAppFormExDto || formStructureData.ForeignAppFormExDto,
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
                } else {
                    // If formStructureData is not available, use transactionExDto data
                    const enhancedFormStructure = {
                        TransactionOrganizedType: transactionOrganizedType,
                        ForeignAppFormExDto: foreignAppFormExDto,
                        TransactionId: transactionExDto.Id,
                        TransactionName: transactionExDto.TransactionName,
                        RootUnitId: transactionExDto.RootUnitId,
                        IsAllowAccess: transactionExDto.IsAllowAccess,
                    };

                    formStructureDataRef.current = enhancedFormStructure;
                    setDataModel((prev: any) => ({
                        ...prev,
                        currentFormStructure: enhancedFormStructure
                    }));
                }

                // Load form data (new vs edit is handled inside loadFormData for Angular parity)
                await loadFormData(formStructureDataRef.current);
            } else {
                // Fallback to original method if DynamicLayout is not loaded yet
                const formStructureData = await loadFormStructure();
                if (!formStructureData) return;

                // Load form data (new vs edit is handled inside loadFormData for Angular parity)
                await loadFormData(formStructureData);
            }
        } catch (error) {
            console.error('Error loading data from server:', error);
            showError((error as Error).message);
        } finally {
            dispatch(setIsNotBusy());
            setDataModel((prev: any) => ({ ...prev, isLoading: false }));
        }
    };

    const handleRefresh = useCallback(() => {
        loadDataFromServer();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [transactionId, rootPrimaryKeyValue]);

    const handleReloadCurrentFormInner = useCallback(() => {
        if (!transactionId) return;
        // Clear cached dynamic layout so layout changes are picked up.
        try {
            const layoutCacheKey = `${transactionId}-${rootPrimaryKeyValue ?? ''}`;
            dynamicLayoutCacheRef.current.delete(layoutCacheKey);
        } catch {
            // ignore
        }
        transactionExDtoRef.current = null;
        formStructureDataRef.current = null;
        lastLoadedKeyRef.current = null;

        setDataModel((prev: any) => ({
            ...prev,
            currentFormStructure: null,
            currentFormData: null,
            dictChildTransactionUnitIdDataSource: {},
            dictGrandChildTransactionUnitIdDataSource: {},
            dictFieldEntityDataSource: {},
            dictFieldEntityDataMap: {},
            dictChangedUnitId: {},
            isLoading: true,
            doing_async: true,
        }));
        loadDataFromServer();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [transactionId, rootPrimaryKeyValue]);

    const handleReloadCurrentForm = handleReloadCurrentFormInner;

    // Angular formMasterDetailCtrl saveFormData: controllerModel.rootPrimaryKeyValue = data.Object.RootPrimaryKeyValue
    const handleAfterSave = useCallback(
        (savedFormData: any) => {
            if (!savedFormData) return;
            let rootPk = savedFormData.RootPrimaryKeyValue;
            if (savedFormData.DataTransferFromMasterDetailDto?.RootPrimaryKeyValue != null) {
                rootPk = savedFormData.DataTransferFromMasterDetailDto.RootPrimaryKeyValue;
            }
            if (rootPk === undefined || rootPk === null || rootPk === '') {
                return;
            }

            setControllerModel((prev: any) => ({
                ...prev,
                rootPrimaryKeyValue: rootPk,
                formRequestMode: 'Edit',
            }));
            setDataModel((prev: any) => ({
                ...prev,
                rootIdInCache: rootPk,
            }));

            setParamObj((prev: any) => {
                const next = { ...prev, param1: rootPk };
                if (!isEmbedded) {
                    const reactPath = getReactPathForRouteCode('FormMasterDetail');
                    const newPath = buildRoutePathFromParamObj(reactPath, {
                        ...next,
                        isNavigatedFromTab: prev.isNavigatedFromTab !== false,
                    });
                    dispatch(updateActiveTabPath(newPath));
                    navigate(newPath, { replace: true });
                }
                return next;
            });
        },
        [isEmbedded, dispatch, navigate]
    );

    /** Workflow Start Run: Angular executeServerCommandCallBack initLoadData — update form in place, no route reload. */
    const applyWorkflowCommandFormData = useCallback(
        (serverFormData: any) => {
            if (!serverFormData) return;
            let rootPk = serverFormData.RootPrimaryKeyValue;
            if (serverFormData.DataTransferFromMasterDetailDto?.RootPrimaryKeyValue != null) {
                rootPk = serverFormData.DataTransferFromMasterDetailDto.RootPrimaryKeyValue;
            }

            setControllerModel((prev: any) => ({
                ...prev,
                rootPrimaryKeyValue: rootPk ?? prev.rootPrimaryKeyValue,
                formRequestMode: rootPk != null && String(rootPk).trim() !== '' ? 'Edit' : prev.formRequestMode,
            }));

            setDataModel((prev: any) => ({
                ...prev,
                currentFormData: serverFormData,
                rootIdInCache: rootPk ?? prev.rootIdInCache,
            }));

            if (rootPk != null && String(rootPk).trim() !== '' && transactionId != null) {
                lastLoadedKeyRef.current = `${transactionId}-${rootPk}`;
            }

            setWorkflowMonitorPopup((prev) =>
                prev
                    ? {
                          ...prev,
                          rootPrimaryKeyValue: rootPk ?? prev.rootPrimaryKeyValue,
                      }
                    : prev,
            );
        },
        [transactionId],
    );

    const openWorkflowExecutionMonitor = useCallback(() => {
        const tid = controllerModel?.transactionId ?? transactionId;
        const pk = controllerModel?.rootPrimaryKeyValue ?? rootPrimaryKeyValue;
        if (tid == null || pk == null || String(pk).trim() === '') return;
        setWorkflowMonitorPopup({
            popupZIndex: appHelper.getNextPopupZIndex(),
            workflowTransactionId: Number(tid),
            rootPrimaryKeyValue: pk,
        });
    }, [controllerModel?.rootPrimaryKeyValue, controllerModel?.transactionId, rootPrimaryKeyValue, transactionId]);

    // Load DynamicLayout once when transactionId changes (cached per transactionId)
    useEffect(() => {
        if (!transactionId) return;

        const loadDynamicLayoutOnce = async () => {
            const transactionExDto = await loadDynamicLayout();
            if (transactionExDto) {
                transactionExDtoRef.current = transactionExDto;

                // Extract and update form structure with TransactionOrganizedType
                const transactionOrganizedType = transactionExDto.TransactionOrganizedType;
                const foreignAppFormExDto = transactionExDto.ForeignAppFormExDto;

                if (formStructureDataRef.current) {
                    const enhancedFormStructure = {
                        ...formStructureDataRef.current,
                        TransactionOrganizedType: transactionOrganizedType,
                        ForeignAppFormExDto: foreignAppFormExDto,
                        TransactionId: transactionExDto.Id,
                        TransactionName: transactionExDto.TransactionName,
                        RootUnitId: transactionExDto.RootUnitId,
                        IsAllowAccess: transactionExDto.IsAllowAccess,
                    };
                    formStructureDataRef.current = enhancedFormStructure;
                    setDataModel((prev: any) => ({
                        ...prev,
                        currentFormStructure: enhancedFormStructure
                    }));
                }
            }
        };

        loadDynamicLayoutOnce();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [transactionId]);

    // Load data from cache or server
    useEffect(() => {


        // Check cache first

        let isAllowLoadTabCache = false;

        if (isAllowLoadTabCache) {
            let cachedDataModel = null;
            let dataModelKey = null;

            if (paramObj.isNavigatedFromTab) {
                dataModelKey = getCurrentActiveTab()?.tabKey || null;
            }

            if (dataModelKey) {
                cachedDataModel = getDataModelFromCache(dataModelKey);
                if (cachedDataModel) {
                    // Only use cache if it's for the same transactionId and rootPrimaryKeyValue
                    if (cachedDataModel.controllerModel?.transactionId === transactionId &&
                        cachedDataModel.controllerModel?.rootPrimaryKeyValue === rootPrimaryKeyValue) {
                        setControllerModel(cachedDataModel.controllerModel);
                        setDataModel(cachedDataModel.dataModel);
                        formStructureDataRef.current = cachedDataModel.formStructureData;
                        // Restore transactionExDto from cache if available
                        if (cachedDataModel.transactionExDto) {
                            transactionExDtoRef.current = cachedDataModel.transactionExDto;
                        }
                        lastLoadedKeyRef.current = `${transactionId}-${rootPrimaryKeyValue}`;
                        return;
                    }
                } 
            }

            const loadKey = `${transactionId}-${rootPrimaryKeyValue}`;
            // Avoid re-loading when we already have data for this key (e.g. effect re-run after field change)
            if (lastLoadedKeyRef.current === loadKey) {
                return;
            }
            lastLoadedKeyRef.current = loadKey;
        }
        // Load from server (will use transactionExDtoRef if already loaded)
        loadDataFromServer();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [transactionId, rootPrimaryKeyValue, paramObj.isNavigatedFromTab]);

    // Use a ref to store cache data and keep it in sync with the latest loaded form state.
    // This snapshot is what we persist into the tab cache so that switching tabs restores
    // the most recent in-memory state (matching Angular's $scope behavior).
    const cacheDataRef = useRef<any>({});
    const lastCachedTransactionIdRef = useRef<number | null>(null);
    const lastCachedRootPkRef = useRef<string | null>(null);

    useEffect(() => {
        const isDataLoaded = !dataModel.isLoading && !dataModel.doing_async;
        const hasData = dataModel.currentFormData || dataModel.currentFormStructure;
        const transactionChanged = lastCachedTransactionIdRef.current !== controllerModel.transactionId;
        const rootPkChanged = lastCachedRootPkRef.current !== controllerModel.rootPrimaryKeyValue;

        if (isDataLoaded && hasData) {
            cacheDataRef.current = {
                controllerModel,
                dataModel,
                formStructureData: formStructureDataRef.current,
                transactionExDto: transactionExDtoRef.current
            };
            // Track current form identity so diagnostics can detect stale cache issues if needed
            lastCachedTransactionIdRef.current = controllerModel.transactionId;
            lastCachedRootPkRef.current = controllerModel.rootPrimaryKeyValue;
        }
    }, [
        controllerModel.transactionId,
        controllerModel.rootPrimaryKeyValue,
        dataModel.isLoading,
        dataModel.doing_async,
        dataModel.currentFormData,
        dataModel.currentFormStructure
    ]);

    // Auto-cache data for tab navigation (skip when embedded in another page, e.g. TransactionFormGroup)
    useTabDataAutoCache(isEmbedded || param2Obj?.isEmbeddedByOtherPage ? null : cacheDataRef.current);




    // Check if access is allowed - get from transactionExDtoRef (standalone, not in dataModel)
    // Note: useMemo dependency on ref.current won't trigger re-renders, but that's OK since ref persists
    const isAllowAccess = useMemo(() => {
        return transactionExDtoRef.current?.IsAllowAccess ??
            dataModel.currentFormStructure?.IsAllowAccess ??
            true; // Default to true if not available
    }, [dataModel.currentFormStructure]); // Only depend on dataModel, ref.current is stable

    // Determine if we have form structure data
    const hasFormStructure = !!(dataModel.currentFormStructure || formStructureDataRef.current);

    // Auto-print when opened in print mode (Angular: IsLoadingPrintForm / print view).
    // Only run once per mount to avoid repeat prints on re-render.
    const hasAutoPrintedRef = useRef(false);
    useEffect(() => {
        const isPrintMode = Boolean(controllerModel?.param2Obj?.isPrint);
        if (!isPrintMode) return;
        if (hasAutoPrintedRef.current) return;
        const isLoaded = !dataModel.isLoading && !dataModel.doing_async && hasFormStructure;
        if (!isLoaded) return;
        hasAutoPrintedRef.current = true;
        // Let layout settle before printing to avoid empty / partial output.
        setTimeout(() => {
            try {
                window.print();
            } catch {
                // ignore
            }
        }, 300);
    }, [controllerModel?.param2Obj?.isPrint, dataModel.isLoading, dataModel.doing_async, hasFormStructure]);




    // Get form layout type (reserved for layout switching)
    const _formLayoutType = useMemo(() => {
        return dataModel.currentFormStructure?.ForeignAppFormExDto?.LayoutType ||
            formStructureDataRef.current?.ForeignAppFormExDto?.LayoutType;
    }, [dataModel.currentFormStructure]);

    // Handle main body click (for closing popups, etc.)
    const handleMainBodyClick = (e: React.MouseEvent) => {
        // TODO: Implement main body click logic
    };

    // Handle main body keydown
    const handleMainBodyKeyDown = (e: React.KeyboardEvent) => {
        // TODO: Implement keyboard shortcuts
    };

    if (!isAllowAccess) {
        return (
            <div className={`w-full h-full flex items-center justify-center ${theme.default}`}>
                <div className="text-red-500 p-5">
                    Data Model "{dataModel.currentFormStructure?.TransactionName || 'Unknown'}": Access Denied
                </div>
            </div>
        );
    }

    return (
        <div
            className={`w-full h-full flex ${theme.default}`}
            style={{ position: 'relative' }}
            onMouseDown={handleMainBodyClick}
            onKeyDown={handleMainBodyKeyDown}
        >
            {/* Main Form Container - no left border when embedded in file properties */}
            <div
                className={`flex-auto h-full overflow-auto ${theme.mainContentSection} ${controllerModel?.isFilePropertyEdit ? 'border-l-0' : ''}`}
                style={{ flex: '1 0' }}
            >
                {/* Show loading state */}
                {dataModel.isLoading && (
                    <div className="flex items-center justify-center h-full">
                        <div className="text-gray-500">Loading form data...</div>
                    </div>
                )}

                {/* Show form when data is loaded */}
                {!dataModel.isLoading && hasFormStructure && (
                    <MasterDetailEditLayoutForm
                        controllerModel={controllerModel}
                        dataModel={dataModel}
                        formStructureData={formStructureDataRef.current}
                        transactionExDto={transactionExDtoRef.current}
                        onDataModelChange={setDataModel}
                        onRefresh={handleRefresh}
                        onReloadCurrentForm={handleReloadCurrentForm}
                        onSave={handleAfterSave}
                        onApplyWorkflowCommandFormData={applyWorkflowCommandFormData}
                        onOpenWorkflowExecutionMonitor={openWorkflowExecutionMonitor}
                        onToggleFormConfigButtons={toggleFormConfigButtons}
                        onFormActionApiReady={onFormActionApiReady}
                        additionalFormActionApis={additionalFormActionApis}
                        additionalFormCanSave={additionalFormCanSave}
                        additionalFormIsDirty={additionalFormIsDirty}
                        onTemplateHeaderContainerReady={handleTemplateHeaderContainerReady}
                    />
                )}

                {templateHeaderContainerEl && templateHeaderForms.length > 0 && templateHeaderForms.map((hdr) =>
                    createPortal(
                        <TemplateHeaderEmbeddedForm
                            key={hdr.key}
                            hdr={hdr}
                            onApiReady={registerHeaderFormApi}
                            onApiUnmount={unregisterHeaderFormApi}
                            onSaveStateChange={handleHeaderSaveStateChange}
                            forceEnableFormConfigButtons={controllerModel.isEnableFormConfigButtons}
                        />,
                        templateHeaderContainerEl,
                        hdr.key,
                    ),
                )}

                {/* Show message when no form structure and not loading */}
                {!dataModel.isLoading && !hasFormStructure && !dataModel.doing_async && (
                    <div className="flex items-center justify-center h-full">
                        <div className="text-gray-500">No form structure available.</div>
                    </div>
                )}
            </div>

            {/* Right Side Box (for conversation/workflow) - TODO: Implement */}
            {/* This will be implemented later based on AngularJS logic */}

            {workflowMonitorPopup && (
                <EmbeddedLinkedPopupFrame
                    zIndex={workflowMonitorPopup.popupZIndex}
                    title="Workflow Execution Monitor"
                    frameInstanceKey={workflowMonitorPopup.popupZIndex}
                    defaultWidth="min(1100px, 95vw)"
                    defaultHeight="min(720px, 90vh)"
                    toolbarTrailing={
                        <button
                            type="button"
                            className={`rounded-[4px] p-1.5 ${theme.button_default}`}
                            onClick={() => setWorkflowMonitorPopup(null)}
                            title="Close"
                            aria-label="Close"
                        >
                            <i className="fa-solid fa-xmark" aria-hidden />
                        </button>
                    }
                >
                    <WorkflowExecutionMonitor
                        embedded={{
                            workflowTransactionId: workflowMonitorPopup.workflowTransactionId,
                            rootPrimaryKeyValue: workflowMonitorPopup.rootPrimaryKeyValue,
                        }}
                    />
                </EmbeddedLinkedPopupFrame>
            )}
        </div>
    );
};

export default FormMasterDetail;
