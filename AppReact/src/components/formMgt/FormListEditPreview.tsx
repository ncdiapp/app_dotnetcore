import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useTheme } from '../../redux/hooks/useTheme';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { dynamicLayoutService } from '../../webapi/dynamiclayoutsvc';
import appHelper from '../../helper/appHelper';
import AppSearch, { type AppSearchHandle } from '../search/AppSearch';
import FormMasterDetail from './FormMasterDetail';
import FormListEdit from './FormListEdit';
import { EmbeddedLinkedPopupFrame } from './EmbeddedLinkedPopupFrame';
import {
    applyLinkTargetMasterDataSelectionToRow,
    isLinkedSearchGridSingleSelection,
    isLinkedSearchPopupConfirmClose,
    type MasterDataPickerContext,
} from './linkedSearchUtils';
import { buildLinkTargetTabTitle } from '../../utils/linkTargetTabTitle';

interface FormListEditPreviewProps {
    transactionId: number;
}

function applyLinkedSearchResultToListRowPreview(result: any, linkedSearch: any, rowItem: any, unitFields: any[]) {
    if (!result || !rowItem) return;
    const viewMappings = Array.isArray(linkedSearch?.AppTransactionUnitSearchViewFieldMappingList)
        ? linkedSearch.AppTransactionUnitSearchViewFieldMappingList
        : [];
    const fieldIdToDbName = new Map<string, string>();
    unitFields.forEach((f: any) => {
        if (f?.Id != null && f?.DataBaseFieldName) fieldIdToDbName.set(String(f.Id), String(f.DataBaseFieldName));
    });
    const dictViewValues = result?.DictViewColumnIDKeyValue ?? {};
    rowItem.DictOneToOneFields = { ...(rowItem.DictOneToOneFields ?? {}) };
    viewMappings.forEach((m: any) => {
        const searchViewFieldId = m?.SearchViewFieldId != null ? String(m.SearchViewFieldId) : '';
        const transactionFieldId = m?.TransactionFieldId != null ? String(m.TransactionFieldId) : '';
        const dbFieldName = fieldIdToDbName.get(transactionFieldId);
        if (!searchViewFieldId || !dbFieldName) return;
        const value =
            dictViewValues?.[searchViewFieldId] ?? dictViewValues?.[Number(searchViewFieldId)] ?? null;
        rowItem.DictOneToOneFields[dbFieldName] = value;
    });
    rowItem.IsDirty = true;
}

const FormListEditPreview: React.FC<FormListEditPreviewProps> = ({ transactionId }) => {
    const { theme } = useTheme();
    const dispatch = useDispatch();
    const { showError } = useErrorMessage();
    const transactionOrganizedTypeEnum = useEnumValues('EmTransactionOrganizedType');

    const [unitNavMenuState, setUnitNavMenuState] = useState<{
        x: number;
        y: number;
        rowItem: any;
    } | null>(null);
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
        hostListRow?: any;
    } | null>(null);
    const previewLinkedSearchRef = useRef<AppSearchHandle | null>(null);
    const previewLinkTargetMdRef = useRef<AppSearchHandle | null>(null);
    const [fetchedRootLinkTargets, setFetchedRootLinkTargets] = useState<any[]>([]);
    const [fetchedRootLinkedSearches, setFetchedRootLinkedSearches] = useState<any[]>([]);

    const [dataModel, setDataModel] = useState<{
        currentFormData: any;
        currentFormStructure: any;
        transactionExDto: any;
        listDataSource: CollectionView | null;
        errorMessages: { error: string[]; warning: string[]; message: string[] };
        isLoading: boolean;
        dictFieldEntityDataMap: Record<string, any>;
    }>({
        currentFormData: null,
        currentFormStructure: null,
        transactionExDto: null,
        listDataSource: null,
        errorMessages: { error: [], warning: [], message: [] },
        isLoading: false,
        dictFieldEntityDataMap: {},
    });

    const blankRowRef = useRef<any>(null);
    const flexGridRef = useRef<any>(null);

    const markChange = useCallback(() => {
        setDataModel((prev) => {
            if (!prev.currentFormData) return prev;
            return {
                ...prev,
                currentFormData: { ...prev.currentFormData, IsDirty: true },
            };
        });
    }, []);

    const loadData = useCallback(
        async (_isRefresh = false) => {
            if (!transactionId) return;
            try {
                dispatch(setIsBusy());
                setDataModel((prev) => ({ ...prev, isLoading: true }));

                const [formStructureData, listDataResponse, transactionExDto] = await Promise.all([
                    appTransactionService.getFormStructure(transactionId),
                    appTransactionService.getListEditFormData(transactionId),
                    dynamicLayoutService.getTransactionForm(transactionId).catch(() => null),
                ]);

                const listData = listDataResponse?.ListData ?? [];
                const blankRow = listDataResponse?.EditCloneAppChildDataDto ?? null;
                blankRowRef.current = blankRow;

                const dictFieldEntityDataMap: Record<string, any> = {};
                const dictEntityDataSource = formStructureData?.DictStandAloneEntityDataSource ?? {};
                const dictFiledIDMappingEntityID = formStructureData?.DictStandAloneFiledIDMappingEntityID ?? {};
                for (const fieldId in dictFiledIDMappingEntityID) {
                    const entityId = dictFiledIDMappingEntityID[fieldId];
                    const entityData = dictEntityDataSource[entityId];
                    if (entityData && Array.isArray(entityData)) {
                        dictFieldEntityDataMap[fieldId] = {
                            itemsSource: entityData,
                            valueMember: 'Id',
                            displayMember: 'Display',
                        };
                    }
                }

                const cv = new CollectionView(listData);
                cv.sortDescriptions.clear();

                const currentFormData = {
                    ...listDataResponse,
                    TransactionId: transactionId,
                    ListData: listData,
                    IsDirty: false,
                    IsMassUpdate: listDataResponse?.IsMassUpdate ?? false,
                    MassUpdateViewId: listDataResponse?.MassUpdateViewId,
                };

                setDataModel({
                    currentFormData,
                    currentFormStructure: formStructureData,
                    transactionExDto: transactionExDto ?? null,
                    listDataSource: cv,
                    errorMessages: { error: [], warning: [], message: [] },
                    isLoading: false,
                    dictFieldEntityDataMap,
                });
            } catch (err) {
                appHelper.debugLog('FormListEditPreview loadData error', err);
                showError('Failed to load list edit data: ' + (err as Error).message);
                setDataModel((prev) => ({ ...prev, isLoading: false }));
            } finally {
                dispatch(setIsNotBusy());
            }
        },
        [transactionId, dispatch, showError]
    );

    useEffect(() => {
        if (transactionId) {
            loadData();
        }
    }, [transactionId, loadData]);

    const addChildNew = useCallback(() => {
        const blankRow = blankRowRef.current;
        const cv = dataModel.listDataSource;
        if (!cv || !dataModel.currentFormData) return;

        const newItem = blankRow
            ? JSON.parse(JSON.stringify(blankRow))
            : { UIId: appHelper.guid(), DictOneToOneFields: {}, DictOneToManyFields: {} };
        newItem.UIId = newItem.UIId || appHelper.guid();
        if (!newItem.DictOneToOneFields) newItem.DictOneToOneFields = {};
        if (newItem.DictOneToOneFields && typeof newItem.DictOneToOneFields === 'object') {
            newItem.DictOneToOneFields['GUID'] = appHelper.guid();
        }
        if (newItem.DictOneToManyFields && typeof newItem.DictOneToManyFields === 'object') {
            const copy: Record<string, any[]> = {};
            for (const k of Object.keys(newItem.DictOneToManyFields)) {
                copy[k] = Array.isArray(newItem.DictOneToManyFields[k]) ? [...newItem.DictOneToManyFields[k]] : [];
            }
            newItem.DictOneToManyFields = copy;
        }
        (cv as any).sourceCollection.push(newItem);
        cv.refresh();

        setDataModel((prev) => ({
            ...prev,
            currentFormData: prev.currentFormData
                ? {
                      ...prev.currentFormData,
                      IsDirty: true,
                      ListData: (cv as any).sourceCollection ?? prev.currentFormData.ListData,
                  }
                : prev.currentFormData,
        }));
    }, [dataModel.listDataSource, dataModel.currentFormData]);

    const deleteChild = useCallback(() => {
        const flex = (flexGridRef.current as any)?.control ?? flexGridRef.current;
        const cv = dataModel.listDataSource;
        if (!flex || !cv || !dataModel.currentFormData) return;

        const rowIndex = flex.selection?.row ?? -1;
        if (rowIndex < 0 || rowIndex >= flex.rows?.length) return;

        const row = flex.rows[rowIndex];
        const dataItem = row?.dataItem;
        if (!dataItem) return;

        const src = (cv as any).sourceCollection;
        if (Array.isArray(src)) src.splice(rowIndex, 1);
        cv.refresh();

        setDataModel((prev) => ({
            ...prev,
            currentFormData: prev.currentFormData
                ? {
                      ...prev.currentFormData,
                      IsDirty: true,
                      ListData: (cv as any).sourceCollection ?? prev.currentFormData.ListData,
                  }
                : prev.currentFormData,
        }));
    }, [dataModel.listDataSource, dataModel.currentFormData]);

    const saveFormData = useCallback(async () => {
        if (!dataModel.currentFormData || !dataModel.listDataSource) return;

        try {
            dispatch(setIsBusy());

            const src = (dataModel.listDataSource as any).sourceCollection ?? [];
            const dirtyItems = src.filter((item: any) => item.IsDirty);

            const saveDto = {
                ...dataModel.currentFormData,
                ListData: src,
                ChangedItems: dirtyItems,
            };

            const result = await appTransactionService.saveListEditFormData(saveDto);

            if (!result?.IsSuccessful) {
                const errors = result?.ValidationResult?.Items ?? [];
                const errorMessages = errors.map((e: any) => e.Message || 'Validation error');
                setDataModel((prev) => ({
                    ...prev,
                    errorMessages: {
                        error: errorMessages,
                        warning: [],
                        message: [],
                    },
                }));
                return;
            }

            await loadData(true);
        } catch (err) {
            appHelper.debugLog('FormListEditPreview saveFormData error', err);
            showError('Failed to save list edit data: ' + (err as Error).message);
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [dataModel.currentFormData, dataModel.listDataSource, dispatch, loadData, showError]);

    const rootUnit =
        dataModel.transactionExDto?.AppTransactionUnitList?.[0] ??
        dataModel.currentFormStructure?.AppTransactionUnitList?.[0];

    const linkTargets = (rootUnit?.AppFormLinkTargetList?.length ? rootUnit.AppFormLinkTargetList : fetchedRootLinkTargets) || [];
    const linkedSearches =
        (rootUnit?.AppTransactionUnitLinkedSearchList?.length
            ? rootUnit.AppTransactionUnitLinkedSearchList
            : fetchedRootLinkedSearches) || [];
    const hasUnitNavigation = linkTargets.length > 0 || linkedSearches.length > 0;

    useEffect(() => {
        let cancelled = false;
        const rootUnitId = rootUnit?.Id != null ? String(rootUnit.Id) : '';
        if (!rootUnitId) return;

        Promise.all([
            appTransactionService.retrieveOneTransactionUnitLinkTargetList(rootUnitId).catch(() => []),
            appTransactionService.retrieveOneAppTransactionUnitLinkedSearchList(rootUnitId).catch(() => []),
        ]).then(([ltList, lsList]) => {
            if (cancelled) return;
            setFetchedRootLinkTargets(Array.isArray(ltList) ? ltList : []);
            setFetchedRootLinkedSearches(Array.isArray(lsList) ? lsList : []);
        });

        return () => {
            cancelled = true;
        };
    }, [rootUnit?.Id]);

    const fieldsFromUnit = (rootUnit?.AppTransactionFieldList ?? [])
        .filter((f: any) => f.IsFormLayoutVisible !== false)
        .sort((a: any, b: any) => (a.SortOrder ?? 0) - (b.SortOrder ?? 0));

    const fieldsFromStructure = ((): any[] => {
        const st = dataModel.currentFormStructure;
        if (!st?.DictTransactionUnitIdFiledNameFiledID) return [];
        const unitIds = Object.keys(st.DictTransactionUnitIdFiledNameFiledID || {})
            .map(Number)
            .filter(Boolean);
        const unitId = unitIds[0];
        if (unitId == null) return [];
        const nameToId = st.DictTransactionUnitIdFiledNameFiledID[unitId];
        const idToDisplay = st.DictTransactionUnitIdFieldIdFieldDisplayName?.[unitId];
        if (!nameToId || typeof nameToId !== 'object') return [];
        return Object.entries(nameToId).map(([dataBaseFieldName, id]) => ({
            Id: id,
            DataBaseFieldName: dataBaseFieldName,
            DisplayName: (idToDisplay && (idToDisplay as Record<number, string>)[Number(id)]) ?? dataBaseFieldName,
            SortOrder: 0,
            Width: 100,
        }));
    })();

    const fields = fieldsFromUnit.length > 0 ? fieldsFromUnit : fieldsFromStructure;

    const isReadOnly = rootUnit?.IsReadOnly === true;
    const isAllowAccess = dataModel.transactionExDto?.IsAllowAccess !== false;

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
        // NAVI button behavior: always use in-page DIV popup (never open a new tab).
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

    const executeUnitLinkTarget = (linkTarget: any, rowItem: any) => {
        if (!linkTarget || !rowItem) return;

        const rootDictOneToOneFields = dataModel.currentFormData?.DictOneToOneFields ?? {};
        const rowDictOneToOneFields = rowItem?.DictOneToOneFields ?? {};

        // Angular: SourceConditionColumn gates whether this menu item can execute.
        if (linkTarget?.SourceConditionColumn) {
            const v = rowDictOneToOneFields?.[linkTarget.SourceConditionColumn];
            if (v === false || v === 0 || v === '0' || v === 'false' || v === 'False' || v == null) return;
        }

        const usageType = Number(linkTarget?.LinkTargetUsageType ?? 0);
        const tabTitleBase = linkTarget.NavigationActionName ?? 'Action';

        if (usageType === 5) {
            // System Defined Page
            const paramId = linkTarget.SourceColumn1 ? rowDictOneToOneFields?.[linkTarget.SourceColumn1] : null;

            const resolveParamBySourceColumn = (sourceColumn: any, rootFallback: any) => {
                if (!sourceColumn) return null;
                let colName = String(sourceColumn);
                if (colName.indexOf('RootUnit.') >= 0) colName = colName.substring(9).trim();
                if (String(sourceColumn).indexOf('RootUnit.') >= 0) return rootFallback?.[colName] ?? null;
                return rowDictOneToOneFields?.[colName] ?? null;
            };

            const param1 = resolveParamBySourceColumn(linkTarget.SourceColumn2, rootDictOneToOneFields);
            const param2 = resolveParamBySourceColumn(linkTarget.SourceColumn3, rootDictOneToOneFields);

            const routeCode = normalizeMainPrefixRouteCode(linkTarget.LinkTargetUrlOrRouteCode ?? '');

            const paramObj: any = {};
            const routeLower = routeCode.toLowerCase();
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

            const tabTitle = buildLinkTargetTabTitle(
                tabTitleBase,
                Boolean(linkTarget.RowDisplayDbField),
                linkTarget.RowDisplayDbField ? rowDictOneToOneFields?.[linkTarget.RowDisplayDbField] : undefined,
                paramId ?? undefined
            );

            openInTabOrPopup({
                routeBasePath: routeCode,
                title: tabTitle,
                paramObj,
                isPopup: Boolean(linkTarget.IsPopup),
                popupWidth: linkTarget.PopupWidth ?? null,
                popupHeight: linkTarget.PopupHeight ?? null,
                showConfirmClose: routeLower === 'masterdatamanagement',
                pickerContext: routeLower === 'masterdatamanagement' ? { linkTarget, hostRow: rowItem } : undefined,
            });
            return;
        }

        // Regular transaction link target (FormMasterDetail / FormListEdit)
        const targetPkValue = linkTarget.SourceColumn1 ? rowDictOneToOneFields?.[linkTarget.SourceColumn1] : null;

        const linkTargetValueMapping: Record<string, any> = {};
        if (linkTarget.SourceColumn2 && linkTarget.TargetColumn2) {
            let dbColumnName = linkTarget.SourceColumn2;
            const fromRoot = String(dbColumnName).indexOf('RootUnit.') >= 0;
            if (fromRoot) dbColumnName = dbColumnName.substring(9).trim();
            linkTargetValueMapping[linkTarget.TargetColumn2] = fromRoot
                ? rootDictOneToOneFields?.[dbColumnName]
                : rowDictOneToOneFields?.[dbColumnName];
        }
        if (linkTarget.SourceColumn3 && linkTarget.TargetColumn3) {
            let dbColumnName = linkTarget.SourceColumn3;
            const fromRoot = String(dbColumnName).indexOf('RootUnit.') >= 0;
            if (fromRoot) dbColumnName = dbColumnName.substring(9).trim();
            linkTargetValueMapping[linkTarget.TargetColumn3] = fromRoot
                ? rootDictOneToOneFields?.[dbColumnName]
                : rowDictOneToOneFields?.[dbColumnName];
        }

        const tabTitle = buildLinkTargetTabTitle(
            tabTitleBase,
            Boolean(linkTarget.RowDisplayDbField),
            linkTarget.RowDisplayDbField ? rowDictOneToOneFields?.[linkTarget.RowDisplayDbField] : undefined,
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
                    srcTransactionRid: dataModel.currentFormData?.RootPrimaryKeyValue ?? null,
                };
            }
            openInTabOrPopup({
                routeBasePath,
                title: tabTitleBase,
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

    const executeLinkedSearch = (linkedSearch: any, rowItem: any) => {
        if (!linkedSearch) return;

        const rootDictOneToOneFields = dataModel.currentFormData?.DictOneToOneFields ?? {};
        const siblingOneToOne = dataModel.currentFormData?.DictSiblingOneToOneFields ?? {};
        const rootUnitId = dataModel.currentFormData?.RootUnitId ?? rootUnit?.Id ?? null;

        const rowDictOneToOneFields = rowItem?.DictOneToOneFields ?? {};

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

            if (sourceUnitId == null) {
                formValue = rootDictOneToOneFields?.[dataBaseFieldName];
            } else if (rootUnitId != null && String(sourceUnitId) === String(rootUnitId)) {
                formValue = rootDictOneToOneFields?.[dataBaseFieldName];
            } else if (rootUnit?.Id != null && String(sourceUnitId) === String(rootUnit.Id)) {
                // In list-edit, the grid unit is typically the "current" unit.
                formValue = rowDictOneToOneFields?.[dataBaseFieldName];
            } else if (siblingOneToOne?.[sourceUnitId]) {
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
            hostListRow: rowItem,
        });
    };

    if (!transactionId) {
        return (
            <div className={`w-full h-full flex items-center justify-center p-4 ${theme.mainContentSection}`}>
                <div className={`text-sm ${theme.label}`}>No transaction selected.</div>
            </div>
        );
    }

    if (dataModel.currentFormStructure && !dataModel.isLoading && isAllowAccess === false) {
        return (
            <div className={`w-full h-full flex items-center justify-center p-4 ${theme.mainContentSection}`}>
                <div className="text-red-600">
                    Data Model &quot;
                    {dataModel.transactionExDto?.TransactionName ??
                        dataModel.currentFormStructure?.TransactionName ??
                        'List Edit'}
                    &quot;: Access Denied
                </div>
            </div>
        );
    }

    return (
        <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.default}`}>
            <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
                <div className={`text-md font-semibold ${theme.title}`}>
                    {dataModel.transactionExDto?.TransactionName ??
                        dataModel.currentFormStructure?.TransactionName ??
                        'List Edit'}
                </div>
                <div className="flex items-center gap-2">
                    {!isReadOnly && (
                        <>
                            <button
                                type="button"
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={addChildNew}
                                title="Add row"
                            >
                                <i className="fa-solid fa-plus-circle mr-1" aria-hidden /> Add
                            </button>
                            <button
                                type="button"
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={deleteChild}
                                title="Delete selected row"
                            >
                                <i className="fa-solid fa-minus-circle mr-1" aria-hidden /> Delete
                            </button>
                        </>
                    )}
                    {!isReadOnly && (
                        <button
                            type="button"
                            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                            onClick={saveFormData}
                            disabled={!dataModel.currentFormData?.IsDirty}
                            title="Save"
                        >
                            <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save
                        </button>
                    )}
                    <button
                        type="button"
                        className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                        onClick={() => loadData(true)}
                        title="Refresh"
                    >
                        <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
                    </button>
                </div>
            </div>

            {(dataModel.errorMessages?.error?.length > 0 ||
                dataModel.errorMessages?.warning?.length > 0) && (
                <div className={`px-3 py-1 text-xs ${theme.label}`}>
                    {dataModel.errorMessages.error?.map((e: string, i: number) => (
                        <div key={`err-${i}`} className="text-red-600">
                            {e}
                        </div>
                    ))}
                    {dataModel.errorMessages.warning?.map((w: string, i: number) => (
                        <div key={`warn-${i}`} className="text-amber-600">
                            {w}
                        </div>
                    ))}
                </div>
            )}

            <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection} px-2 pb-2`}>
                {dataModel.isLoading && (
                    <div className="flex items-center justify-center h-full">
                        <div className={`text-sm ${theme.label}`}>Loading...</div>
                    </div>
                )}

                {!dataModel.isLoading && dataModel.listDataSource && (
                    <div className="w-full h-full min-h-[200px]">
                        <FlexGrid
                            ref={flexGridRef}
                            itemsSource={dataModel.listDataSource}
                            selectionMode="Row"
                            allowSorting={true}
                            isReadOnly={isReadOnly}
                            headersVisibility="Column"
                            cellEditEnded={(s: any, e: any) => {
                                const row = s.rows[e.row];
                                const item = row?.dataItem;
                                if (item) {
                                    item.IsDirty = true;
                                    markChange();
                                }
                            }}
                            className="w-full h-full"
                        >
                            <FlexGridFilter />
                            {fields.map((field: any) => {
                                const binding = `DictOneToOneFields.${field.DataBaseFieldName}`;
                                const header = field.DisplayName ?? field.DataBaseFieldName;
                                const colWidth = field.Width ?? field.DisplayWidth ?? 100;
                                return (
                                    <FlexGridColumn
                                        key={field.Id ?? field.DataBaseFieldName}
                                        binding={binding}
                                        header={header}
                                        width={typeof colWidth === 'number' ? colWidth : 100}
                                    />
                                );
                            })}
                            {hasUnitNavigation && !isReadOnly && (
                                <FlexGridColumn header="" binding="" width={150} isReadOnly={true}>
                                    <FlexGridCellTemplate
                                        cellType="Cell"
                                        template={(cell: any) => {
                                            const rowItem = cell?.item?._originalData ?? cell?.item;
                                            return (
                                                <div className="w-full h-full flex items-center justify-center">
                                                    <button
                                                        type="button"
                                                        className={`${theme.button_default} w-9 h-7 rounded-[4px] text-xs flex items-center justify-center flex-shrink-0`}
                                                        title="Navigate"
                                                        onMouseDown={(e) => e.stopPropagation()}
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect();
                                                            setUnitNavMenuState({ x: rect.right, y: rect.top, rowItem });
                                                        }}
                                                    >
                                                        <i className="fa-solid fa-route mr-1" aria-hidden />
                                                        Nav
                                                    </button>
                                                </div>
                                            );
                                        }}
                                    />
                                </FlexGridColumn>
                            )}
                            <FlexGridColumn
                                header=""
                                binding=""
                                width="*"
                                allowSorting={false}
                                isReadOnly={true}
                            />
                        </FlexGrid>
                    </div>
                )}

                {!dataModel.isLoading && !dataModel.listDataSource && dataModel.currentFormStructure && (
                    <div className="flex items-center justify-center h-full">
                        <div className={`text-sm ${theme.label}`}>No list data.</div>
                    </div>
                )}
            </div>

            {unitNavMenuState && (
                <>
                    <div
                        className="fixed inset-0 z-[10010]"
                        onClick={() => setUnitNavMenuState(null)}
                        aria-hidden
                    />
                    <div
                        className={`fixed z-[10011] rounded shadow-lg border py-1 ${theme.mainContentSection}`}
                        style={{ left: unitNavMenuState.x, top: unitNavMenuState.y, minWidth: 240 }}
                        onClick={(e) => e.stopPropagation()}
                    >
                        <div className={`px-3 py-2 text-xs font-semibold ${theme.title}`}>Unit Navigation</div>
                        <div className="py-1">
                            {linkTargets.map((lt: any) => (
                                <button
                                    key={lt.Id ?? lt.NavigationActionName}
                                    type="button"
                                    className={`w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5 ${theme.contextMenu}`}
                                    onClick={() => {
                                        const currentRowItem = unitNavMenuState.rowItem;
                                        setUnitNavMenuState(null);
                                        executeUnitLinkTarget(lt, currentRowItem);
                                    }}
                                >
                                    <i className="fa-solid fa-file-lines opacity-70" aria-hidden />
                                    {lt.NavigationActionName}
                                </button>
                            ))}

                            {linkedSearches.map((ls: any) => (
                                <button
                                    key={ls.Id ?? ls.Name}
                                    type="button"
                                    className={`w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5 ${theme.contextMenu}`}
                                    onClick={() => {
                                        const currentRowItem = unitNavMenuState.rowItem;
                                        setUnitNavMenuState(null);
                                        executeLinkedSearch(ls, currentRowItem);
                                    }}
                                >
                                    <i className="fa-solid fa-magnifying-glass opacity-70" aria-hidden />
                                    {ls.Name}
                                </button>
                            ))}
                        </div>
                    </div>
                </>
            )}

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
                                    const row = searchPopupState.hostListRow;
                                    const sel = previewLinkedSearchRef.current?.getSelectedResults?.() ?? [];
                                    if (ls && row && sel.length > 0) {
                                        applyLinkedSearchResultToListRowPreview(sel[0], ls, row, fields);
                                        markChange();
                                        const cv = dataModel.listDataSource;
                                        if (cv && typeof (cv as any).refresh === 'function') (cv as any).refresh();
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
                    <AppSearch ref={previewLinkedSearchRef} embeddedParamObj={searchPopupState.paramObj} />
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
                                    const sel = previewLinkTargetMdRef.current?.getSelectedResults?.() ?? [];
                                    const pc = linkTargetPopupState.pickerContext;
                                    if (pc && sel.length > 0) {
                                        applyLinkTargetMasterDataSelectionToRow(pc.linkTarget, sel[0], pc.hostRow);
                                        markChange();
                                        const cv = dataModel.listDataSource;
                                        if (cv && typeof (cv as any).refresh === 'function') (cv as any).refresh();
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
                            return <AppSearch ref={previewLinkTargetMdRef} embeddedParamObj={paramObj} />;
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
                            <div className="p-3 text-xs">
                                Unsupported popup target: {String(linkTargetPopupState.routeBasePath || '')}
                            </div>
                        );
                    })()}
                </EmbeddedLinkedPopupFrame>
            )}

        </div>
    );
};

export default FormListEditPreview;

