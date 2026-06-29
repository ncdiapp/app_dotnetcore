import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';
import { adminSvc } from '../../../webapi/adminsvc';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { DataMap } from '@mescius/wijmo.grid';
import { useAlertConfirm } from '../../../components/common/AlertConfirmProvider';
import TableColumnSelectorDialog from './TableColumnSelectorDialog';
import TransactionUnitDataLoadDialog from './TransactionUnitDataLoadDialog';
import TransactionUnitDeleteFlowEditor from './TransactionUnitDeleteFlowEditor';
import TransactionUnitLinkTargetEditor from './TransactionUnitLinkTargetEditor';
import TransactionUnitLinkedSearchManagementDialog from './TransactionUnitLinkedSearchManagementDialog';
import UnitAdvancedOptionsPopup from './UnitAdvancedOptionsPopup';
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import EntityDataPreview from '../../admin/EntityListOfValue/EntityDataPreview';
import AppEntityInfoEdit from '../../admin/EntityListOfValue/AppEntityInfoEdit';
import SimpleValueListEntityEdit from '../../admin/EntityListOfValue/SimpleValueListEntityEdit';
import MetaDataViewDesign from '../metaDataViewDesign';
import SystemObjectSecurityEditorPopup from '../../admin/CompanySecuritySetting/SystemObjectSecurityEditorPopup';
import FlexGridAddOn from '../../common/FlexGridAddOn';

interface TransactionUnitEditorProps {
    isOpen: boolean;
    unit: any | null;
    transactionData: any | null;
    dataSourceRegisterId: number | null;
    applicationId: string | null;
    onClose: () => void;
    onSave?: (updatedUnit: any, saveToServer?: boolean) => void;
    onEditTable?: (unit: any) => void;
}

const TransactionUnitEditor: React.FC<TransactionUnitEditorProps> = ({
    isOpen,
    unit,
    transactionData,
    dataSourceRegisterId,
    applicationId,
    onClose,
    onSave,
    onEditTable
}) => {
    const { theme } = useTheme();
    const { showError, showInfo } = useErrorMessage();
    const { showConfirm } = useAlertConfirm();
    const dispatch = useDispatch();
    const { addTabAndNavigate } = useTabNavigation();
    const [unitData, setUnitData] = useState<any>(null);
    const [fieldCollectionView, setFieldCollectionView] = useState<CollectionView | null>(null);
    const [isModified, setIsModified] = useState(false);
    const isModifiedRef = React.useRef(false);
    const originalUnitDataRef = React.useRef<any>(null);
    const flexGridRef = React.useRef<any>(null);
    const [fieldGridControl, setFieldGridControl] = useState<any>(null);
    const [tableColumnSelectorDialog, setTableColumnSelectorDialog] = useState<{ isOpen: boolean }>({ isOpen: false });
    const [isDataLoadDialogOpen, setIsDataLoadDialogOpen] = useState(false);
    const [linkTargetEditor, setLinkTargetEditor] = useState<{ usageType: number; title: string } | null>(null);
    const [linkedSearchManagementOpen, setLinkedSearchManagementOpen] = useState(false);
    const [deleteFlowEditorOpen, setDeleteFlowEditorOpen] = useState(false);
    const [moreUnitOptionsOpen, setMoreUnitOptionsOpen] = useState(false);
    const [datasourceSelectionField, setDatasourceSelectionField] = useState<any | null>(null);
    const [datasourceSelectorState, setDatasourceSelectorState] = useState<{
        activeTab: 1 | 2 | 3;
        entityList: Array<{ Id: number; EntityCode?: string; Name?: string }>;
        selectedEntityId: number | null;
        selectedEntityDisplay: string;
        isEnableDependentColumns: boolean;
        dependentMappings: CollectionView | null;
        currentUnitFieldDataMap: DataMap | null;
        entityDbColumnDataMap: DataMap | null;
        // Tab 2: Current Data Model Unit Datasource
        foreignUnitList: Array<{ Id: number; Display: string }>;
        selectedForeignUnitId: number | null;
        selectedForeignUnitDisplay: string;
        foreignUnitFieldDataMap: DataMap | null;
        // Tab 3: Query Datasource
        selectionQueryText: string;
        conditionQueryText: string;
        conditionParameterList: Array<{ parameterName: string; transactionFieldId: number | null }>;
        conditionParameterCV: CollectionView | null;
        parameterTransFieldDataMap: DataMap | null;
    } | null>(null);
    const [entityDataPreviewOpen, setEntityDataPreviewOpen] = useState<{
        entityId: number;
        entityCode?: string;
    } | null>(null);
    const [entityEditPopup, setEntityEditPopup] = useState<{
        route: 'simple-value-list-entity-edit' | 'entity-info-edit';
        param: string;
        title?: string;
    } | null>(null);
    const [isQueryBuilderOpen, setIsQueryBuilderOpen] = useState(false);
    const [fieldSecurityPopup, setFieldSecurityPopup] = useState<{
        open: boolean;
        fieldId: number | null;
        display: string;
    }>({ open: false, fieldId: null, display: '' });
    const [parentDataSourceMappingState, setParentDataSourceMappingState] = useState<{
        isOpen: boolean;
        field: any | null;
        currentFieldName: string;
        parentFieldId: number | null;
        relationTable: string;
        relationTableSchemaOwner: string | null;
        relationParentKey: string;
        relationChildKey: string;
        parentFieldOptions: Array<{ Id: number; Display: string }>;
        relationTableOptions: Array<{ schemaOwner: string | null; name: string; display: string }>;
        relationTableColumnOptions: Array<{ name: string }>;
    }>({
        isOpen: false,
        field: null,
        currentFieldName: '',
        parentFieldId: null,
        relationTable: '',
        relationTableSchemaOwner: null,
        relationParentKey: '',
        relationChildKey: '',
        parentFieldOptions: [],
        relationTableOptions: [],
        relationTableColumnOptions: []
    });
    const [linkTargetDropdownOpen, setLinkTargetDropdownOpen] = useState(false);

    // Matrix Grid Foreignkey popup (Angular: MatrixFkPopup / OpenMatrixFkPopup / assignMatrixFkMapping)
    const [matrixFkPopupState, setMatrixFkPopupState] = useState<{
        isOpen: boolean;
        field: any | null;
        currentFieldName: string;
        sourceUnitId: number | null;
        matrixForeignKeyFieldId: number | null;
        sourceUnitOptions: Array<{ Id: number; Display: string }>;
        sourceFieldOptions: Array<{ Id: number; Display: string }>;
    }>({
        isOpen: false,
        field: null,
        currentFieldName: '',
        sourceUnitId: null,
        matrixForeignKeyFieldId: null,
        sourceUnitOptions: [],
        sourceFieldOptions: []
    });

    // Enum values
    const emAppDataType = useEnumValues('EmAppDataType');
    const emAppTransactionGridDisplayType = useEnumValues('EmAppTransactionGridDisplayType');
    const emAppDataFieldStoreMode = useEnumValues('EmAppDataFieldStoreMode');
    const emAppControlType = useEnumValues('EmAppControlType');
    const emSystemTokenFieldEnum = useEnumValues('EmBLFiledMappingSystemTokenField');
    const emAppAggregationFunctionType = useEnumValues('EmAppAggregationFunctionType');

    // Data maps for dropdowns - store both the list and the DataMap
    const gridDisplayTypeOptions = React.useMemo(() => {
        if (!emAppTransactionGridDisplayType) return [];
        // useEnumValues returns Record<string, number> for enums: { "RegularGrid": 1, ... }
        const list: Array<{ Display: string; Value: number }> = [];
        Object.entries(emAppTransactionGridDisplayType).forEach(([key, value]: [string, any]) => {
            // Skip numeric keys (reverse mapping)
            if (!isNaN(Number(key))) return;
            if (typeof value === 'number') {
                list.push({ Display: key, Value: value });
            }
        });
        list.sort((a, b) => a.Value - b.Value);
        return list;
    }, [emAppTransactionGridDisplayType]);

    const gridDisplayTypeDataMap = React.useMemo(() => {
        if (gridDisplayTypeOptions.length === 0) return null;
        // DataMap(selectedValuePath, displayMemberPath)
        return new DataMap(gridDisplayTypeOptions, 'Value', 'Display');
    }, [gridDisplayTypeOptions]);

    // DataType DataMap - always create, even if enum not loaded yet
    // useEnumValues returns Record<string, number> format: { "String": 1, "Number": 2 }
    const dataTypeDataMap = React.useMemo(() => {
        if (!emAppDataType) {
            // Return empty DataMap if enum not loaded yet
            return new DataMap([], 'Display', 'Value');
        }

        const dataTypeList: Array<{ Display: string; Value: number }> = [];

        Object.entries(emAppDataType).forEach(([key, value]: [string, any]) => {
            // Skip numeric keys (these are reverse mappings)
            if (!isNaN(Number(key))) {
                return;
            }

            // value is the numeric enum value, key is the display name
            if (typeof value === 'number') {
                dataTypeList.push({
                    Display: key,
                    Value: value
                });
            }
        });

        // Sort by value
        dataTypeList.sort((a, b) => a.Value - b.Value);

        // DataMap constructor: new DataMap(itemsSource, selectedValuePath, displayMemberPath)
        // selectedValuePath: the property to use as the value (stored in the cell)
        // displayMemberPath: the property to display in the dropdown
        return new DataMap(dataTypeList, 'Value', 'Display');
    }, [emAppDataType]);

    // ControlType (UIDisplayType) DataMap - always create, even if enum not loaded yet
    // useEnumValues returns Record<string, number> format: { "Text": 1, "Number": 2 }
    const controlTypeDataMap = React.useMemo(() => {
        if (!emAppControlType) {
            // Return empty DataMap if enum not loaded yet
            return new DataMap([], 'Display', 'Value');
        }

        const typeList: Array<{ Display: string; Value: number }> = [];

        Object.entries(emAppControlType).forEach(([key, value]: [string, any]) => {
            // Skip numeric keys (these are reverse mappings)
            if (!isNaN(Number(key))) {
                return;
            }

            // value is the numeric enum value, key is the display name
            if (typeof value === 'number') {
                typeList.push({
                    Display: key,
                    Value: value
                });
            }
        });

        // Sort by value
        typeList.sort((a, b) => a.Value - b.Value);

        // DataMap constructor: new DataMap(itemsSource, selectedValuePath, displayMemberPath)
        // selectedValuePath: the property to use as the value (stored in the cell)
        // displayMemberPath: the property to display in the dropdown
        return new DataMap(typeList, 'Value', 'Display');
    }, [emAppControlType]);

    // ControlType ids for datasource-capable fields (matches Angular getTransactionFieldDatasourceType)
    const controlTypeIdsForDatasource = React.useMemo(() => {
        if (!emAppControlType) {
            return {
                DDL: 1,
                AutoComplete: undefined as number | undefined,
                SearchAbleDDL: undefined as number | undefined,
                RadioButtons: undefined as number | undefined,
                Progress: undefined as number | undefined
            };
        }
        return {
            DDL: emAppControlType.DDL ?? 1,
            AutoComplete: emAppControlType.AutoComplete,
            SearchAbleDDL: (emAppControlType as any).SearchAbleDDL,
            RadioButtons: (emAppControlType as any).RadioButtons,
            Progress: (emAppControlType as any).Progress
        };
    }, [emAppControlType]);

    // Field store mode DataMap
    const fieldStoreModeDataMap = React.useMemo(() => {
        if (!emAppDataFieldStoreMode) {
            return new DataMap([], 'Value', 'Display');
        }

        const modeList: Array<{ Display: string; Value: number }> = [];

        Object.entries(emAppDataFieldStoreMode).forEach(([key, value]: [string, any]) => {
            if (!isNaN(Number(key))) {
                return;
            }

            if (typeof value === 'number') {
                modeList.push({
                    Display: key,
                    Value: value
                });
            }
        });

        modeList.sort((a, b) => a.Value - b.Value);

        return new DataMap(modeList, 'Value', 'Display');
    }, [emAppDataFieldStoreMode]);

    // System token field DataMap
    const systemTokenFieldDataMap = React.useMemo(() => {
        if (!emSystemTokenFieldEnum) {
            return new DataMap([], 'Value', 'Display');
        }

        const tokenList: Array<{ Display: string; Value: number }> = [];

        Object.entries(emSystemTokenFieldEnum).forEach(([key, value]: [string, any]) => {
            if (!isNaN(Number(key))) {
                return;
            }

            if (typeof value === 'number') {
                tokenList.push({
                    Display: key,
                    Value: value
                });
            }
        });

        tokenList.sort((a, b) => a.Value - b.Value);

        return new DataMap(tokenList, 'Value', 'Display');
    }, [emSystemTokenFieldEnum]);

    // Command list DataMap (Angular: data.CommandActionList -> DataMap(Id, Name))
    const commandActionDataMap = React.useMemo(() => {
        const list = Array.isArray(transactionData?.CommandActionList) ? transactionData.CommandActionList : [];
        const items = list
            .filter((c: any) => c?.Id != null)
            .map((c: any) => ({
                Id: Number(c.Id),
                Name: c.Name ?? c.Display ?? String(c.Id)
            }));
        return new DataMap(items, 'Id', 'Name');
    }, [transactionData]);

    // Aggregation type DataMap (for pivot columns)
    const aggregationTypeDataMap = React.useMemo(() => {
        if (!emAppAggregationFunctionType) {
            return new DataMap([], 'Value', 'Display');
        }

        const aggList: Array<{ Display: string; Value: number }> = [];

        Object.entries(emAppAggregationFunctionType).forEach(([key, value]: [string, any]) => {
            if (!isNaN(Number(key))) {
                return;
            }

            if (typeof value === 'number') {
                aggList.push({
                    Display: key,
                    Value: value
                });
            }
        });

        aggList.sort((a, b) => a.Value - b.Value);

        return new DataMap(aggList, 'Value', 'Display');
    }, [emAppAggregationFunctionType]);

    // Load unit data when opened
    useEffect(() => {
        const loadUnitData = async () => {
            if (!isOpen || !unit) return;

            // Helper function to find unit in transactionData (which may have unsaved changes)
            const findUnitInTransactionData = (unitList: any[], targetId: number | null, targetUnit: any): any | null => {
                for (const u of unitList) {
                    if (targetId && u.Id === targetId) {
                        return u;
                    }
                    if (!targetId && u === targetUnit) {
                        return u;
                    }
                    if (u.Children) {
                        const found = findUnitInTransactionData(u.Children, targetId, targetUnit);
                        if (found) return found;
                    }
                }
                return null;
            };

            // Check if unit exists in transactionData (which may have unsaved changes)
            let unitFromTransactionData: any = null;
            if (transactionData && transactionData.AppTransactionUnitList) {
                unitFromTransactionData = findUnitInTransactionData(
                    transactionData.AppTransactionUnitList,
                    unit.Id || null,
                    unit
                );
            }

            let loadedData: any = null;

            // If unit exists in transactionData, use it (preserves unsaved changes)
            // Otherwise, load from server for existing units
            if (unitFromTransactionData) {
                // Use unit from transactionData to preserve any unsaved changes
                loadedData = { ...unitFromTransactionData };
                setUnitData(loadedData);
                setIsModified(false);
                isModifiedRef.current = false;
            } else if (unit.Id) {
                // Existing unit not found in transactionData - load from server
                dispatch(setIsBusy());
                try {
                    const unitExDto = await appTransactionService.retrieveOneAppTransactionUnitExDto(unit.Id.toString());
                    loadedData = unitExDto;
                    setUnitData(unitExDto);
                    setIsModified(false);
                    isModifiedRef.current = false;
                } catch (error: any) {
                    showError(error.message || 'Failed to load unit data');
                    // Fallback to local unit data
                    loadedData = { ...unit };
                    setUnitData(loadedData);
                    setIsModified(false);
                    isModifiedRef.current = false;
                } finally {
                    dispatch(setIsNotBusy());
                }
            } else {
                // New unit - use local data
                loadedData = { ...unit };
                setUnitData(loadedData);
                setIsModified(false);
                isModifiedRef.current = false;
            }

            // Store original data for comparison after data is loaded
            if (loadedData) {
                originalUnitDataRef.current = JSON.parse(JSON.stringify(loadedData));
            }
        };

        loadUnitData();
    }, [isOpen, unit, transactionData, dispatch, showError]);

    // Initialize field collection view
    useEffect(() => {
        if (unitData && unitData.AppTransactionFieldList) {
            const cv = new CollectionView(unitData.AppTransactionFieldList);
            cv.sortDescriptions.push(new SortDescription('SortOrder', true));
            setFieldCollectionView(cv);
        } else {
            setFieldCollectionView(null);
        }
    }, [unitData]);

    // Angular initialTransactionField: populate ParentPKFieldGuid from DictCurrentPKOrFKLinkToParentKeyGuidMap (by RowIdentityGuid)
    useEffect(() => {
        if (!unitData?.AppTransactionFieldList?.length) return;
        const dict = transactionData?.DictCurrentPKOrFKLinkToParentKeyGuidMap ?? null;
        if (!dict) return;

        const need = unitData.AppTransactionFieldList.some(
            (f: any) =>
                f?.RowIdentityGuid &&
                dict[f.RowIdentityGuid] &&
                (f.ParentPKFieldGuid == null || f.ParentPKFieldGuid === '')
        );
        if (!need) return;

        const updated = unitData.AppTransactionFieldList.map((f: any) => {
            if (!f?.RowIdentityGuid) return f;
            const mapped = dict[f.RowIdentityGuid];
            if (!mapped) return f;
            if (f.ParentPKFieldGuid != null && f.ParentPKFieldGuid !== '') return f;
            return {
                ...f,
                ParentPKFieldGuid: mapped,
                IsLinkToParentPrimaryKey: true
            };
        });

        setUnitData({ ...unitData, AppTransactionFieldList: updated });
    }, [unitData, transactionData]);

    // Ensure EntityIdSelector text is populated after reload (Standalone: EntityId; Tab 2: DdlForeignUnitId)
    useEffect(() => {
        const ensureEntityDisplay = async () => {
            if (!unitData || !unitData.AppTransactionFieldList || unitData.AppTransactionFieldList.length === 0) {
                return;
            }

            const needEntityPopulate = unitData.AppTransactionFieldList.some(
                (f: any) => f.EntityId && !f.EntityIdSelector
            );
            const needForeignUnitPopulate = transactionData && unitData.AppTransactionFieldList.some(
                (f: any) => f.DdlForeignUnitId != null && f.DdlForeignUnitId !== '' && !f.EntityIdSelector
            );
            const needQueryPopulate = unitData.AppTransactionFieldList.some(
                (f: any) => f.DdlQueryText && !f.EntityIdSelector
            );
            if (!needEntityPopulate && !needForeignUnitPopulate && !needQueryPopulate) return;

            const collectUnits = (units: any[]): any[] => {
                const result: any[] = [];
                const visit = (list: any[]) => {
                    for (const u of list || []) {
                        result.push(u);
                        if (u.Children?.length) visit(u.Children);
                    }
                };
                visit(units);
                return result;
            };
            const findUnitById = (list: any[], id: number | null): any | null => {
                if (!id) return null;
                for (const u of list || []) {
                    if (u.Id === id) return u;
                    if (u.Children?.length) {
                        const found = findUnitById(u.Children, id);
                        if (found) return found;
                    }
                }
                return null;
            };

            let updatedFields = unitData.AppTransactionFieldList;

            if (needForeignUnitPopulate && transactionData?.AppTransactionUnitList) {
                const allUnits = collectUnits(transactionData.AppTransactionUnitList);
                updatedFields = updatedFields.map((f: any) => {
                    if (f.DdlForeignUnitId != null && f.DdlForeignUnitId !== '' && !f.EntityIdSelector) {
                        const uid = Number(f.DdlForeignUnitId);
                        const u = findUnitById(transactionData.AppTransactionUnitList, uid) || allUnits.find((x: any) => x.Id === uid);
                        const display = u ? (u.UnitDisplayName || u.UnitName || String(uid)) : String(uid);
                        return {
                            ...f,
                            EntityIdSelector: `Unit: ${display} (${uid})`,
                            EntityDisplay: `Unit: ${display} (${uid})`
                        };
                    }
                    return f;
                });
            }

            if (needQueryPopulate) {
                updatedFields = updatedFields.map((f: any) => {
                    if (f.DdlQueryText && !f.EntityIdSelector) {
                        return {
                            ...f,
                            EntityIdSelector: 'Query',
                            EntityDisplay: 'Query'
                        };
                    }
                    return f;
                });
            }

            if (needEntityPopulate) {
                try {
                    const entityList = await adminSvc.retrieveAllAppEntityInfoDto(false);
                    const safeList: Array<{ Id: number; EntityCode?: string; Name?: string }> = Array.isArray(entityList)
                        ? entityList
                        : [];
                    if (safeList.length > 0) {
                        const dict = new Map<number, { Id: number; EntityCode?: string; Name?: string }>();
                        safeList.forEach((e) => dict.set(e.Id, e));
                        updatedFields = updatedFields.map((f: any) => {
                            if (f.EntityId && !f.EntityIdSelector) {
                                const match = dict.get(f.EntityId);
                                const display = match?.EntityCode || match?.Name || String(f.EntityId);
                                return { ...f, EntityIdSelector: display, EntityDisplay: display };
                            }
                            return f;
                        });
                    }
                } catch {
                    // Ignore display-only failures
                }
            }

            if (updatedFields !== unitData.AppTransactionFieldList) {
                setUnitData({ ...unitData, AppTransactionFieldList: updatedFields });
            }
        };

        ensureEntityDisplay();
    }, [unitData, transactionData]);

    // Find parent unit for foreign key map
    const getParentUnit = useCallback((): any | null => {
        if (!transactionData || !unitData) return null;

        // Sibling unit's parent is root master unit (Angular: rootUnit)
        if (unitData.IsMasterSiblingUnit) {
            return transactionData.AppTransactionUnitList?.[0] ?? null;
        }

        // If API/tree already provides parent reference, prefer it (Angular: currentEditUnit.parent)
        if (unitData.parent) return unitData.parent;

        // Find parent from transaction data tree by Id or uiId (supports unsaved units)
        const findParent = (units: any[]): any | null => {
            for (const u of units || []) {
                const children = u?.Children || [];
                if (Array.isArray(children) && children.length > 0) {
                    const found = children.find(
                        (c: any) =>
                            (unitData?.Id != null && c?.Id != null && Number(c.Id) === Number(unitData.Id)) ||
                            (unitData?.uiId != null && c?.uiId != null && String(c.uiId) === String(unitData.uiId))
                    );
                    if (found) return u;
                    const childParent = findParent(children);
                    if (childParent) return childParent;
                }
            }
            return null;
        };
        return findParent(transactionData.AppTransactionUnitList || []);
    }, [transactionData, unitData]);

    const parentUnit = getParentUnit();
    const parentPkFields = parentUnit?.masterKeyList || parentUnit?.AppTransactionFieldList?.filter((f: any) => f.IsPrimaryKey) || [];
    const [resolvedParentPkFields, setResolvedParentPkFields] = useState<any[]>([]);
    // Ensure parentUnit's PK fields are available for FK DataMap (Angular uses parentUnit.masterKeyList)
    useEffect(() => {
        let cancelled = false;
        const run = async () => {
            if (!isOpen || !unitData || !transactionData) return;
            if (!transactionData.IsPhysicalModelTableCreated) {
                setResolvedParentPkFields([]);
                return;
            }
            const isRootMasterUnit = !unitData.IsMasterSiblingUnit && !parentUnit;
            if (isRootMasterUnit) {
                setResolvedParentPkFields([]);
                return;
            }
            if (!parentUnit) {
                setResolvedParentPkFields([]);
                return;
            }

            // Prefer masterKeyList if present; otherwise build from AppTransactionFieldList (PK)
            if (Array.isArray(parentUnit.masterKeyList) && parentUnit.masterKeyList.length > 0) {
                // Still try to load full parent unit to match Angular behavior (parent PK list comes from parent.AppTransactionFieldList)
                // Fall back to masterKeyList immediately for faster UX.
                setResolvedParentPkFields(parentUnit.masterKeyList);
            }
            if (Array.isArray(parentUnit.AppTransactionFieldList) && parentUnit.AppTransactionFieldList.length > 0) {
                const built = parentUnit.AppTransactionFieldList
                    .filter((f: any) => f?.IsPrimaryKey)
                    .map((f: any) => ({
                        RowIdentityGuid: f.RowIdentityGuid,
                        DataBaseFieldName: f.DataBaseFieldName,
                        Id: f.Id
                    }))
                    .filter((k: any) => k.RowIdentityGuid);
                setResolvedParentPkFields(built);
            }

            if (parentUnit.Id == null) {
                setResolvedParentPkFields([]);
                return;
            }

            try {
                const full = await appTransactionService.retrieveOneAppTransactionUnitExDto(String(parentUnit.Id));
                if (cancelled) return;
                // Match Angular: build PK list from parent unit's AppTransactionFieldList(IsPrimaryKey)
                const list = Array.isArray(full?.AppTransactionFieldList)
                    ? full.AppTransactionFieldList
                        .filter((f: any) => f?.IsPrimaryKey)
                        .map((f: any) => ({
                            RowIdentityGuid: f.RowIdentityGuid,
                            DataBaseFieldName: f.DataBaseFieldName,
                            Id: f.Id
                        }))
                        // keep items even if RowIdentityGuid missing; DataMap normalizer will fall back to Id
                        .filter((k: any) => k.RowIdentityGuid || k.Id != null)
                    : (Array.isArray(full?.masterKeyList) ? full.masterKeyList : []);
                setResolvedParentPkFields(list);
            } catch {
                if (!cancelled) {
                    setResolvedParentPkFields([]);
                }
            }
        };
        run();
        return () => {
            cancelled = true;
        };
    }, [isOpen, unitData, transactionData, parentUnit]);

    // Ensure Parent Data Source Mapping display text is populated after reload (Angular shows "Cascading From: <Table> . <Field>")
    useEffect(() => {
        let cancelled = false;
        const ensureParentMappingDisplay = async () => {
            if (!unitData?.AppTransactionFieldList?.length) return;
            const need = unitData.AppTransactionFieldList.some(
                (f: any) => f?.DdlparentLevelId != null && f.DdlparentLevelId !== '' && !f.DdlparentLevelIdSelector
            );
            if (!need) return;

            let changed = false;
            const unitList = transactionData?.AppTransactionUnitList ?? [];
            const findParentOfUnit = (list: any[], childId: number | null): any | null => {
                if (!childId) return null;
                for (const u of list || []) {
                    if (u.Children) {
                        const found = u.Children.find((c: any) => c.Id === childId);
                        if (found) return u;
                        const inner = findParentOfUnit(u.Children, childId);
                        if (inner) return inner;
                    }
                }
                return null;
            };

            // Pre-load parent units needed for display so we don't use await inside map()
            const parentUnitById = new Map<number, any>();
            const ensureUnitFieldsLoaded = async (u: any | null): Promise<any | null> => {
                if (!u) return null;
                if (u.AppTransactionFieldList?.length) return u;
                if (u.Id == null) return u;
                try {
                    const full = await appTransactionService.retrieveOneAppTransactionUnitExDto(String(u.Id));
                    return full || u;
                } catch {
                    return u;
                }
            };

            const parentIdsToLoad = new Set<number>();
            unitData.AppTransactionFieldList.forEach((f: any) => {
                if (f?.DdlparentLevelId == null || f.DdlparentLevelId === '' || f.DdlparentLevelIdSelector) return;
                const fieldUnitId = f.TransactionUnitId != null ? Number(f.TransactionUnitId) : null;
                const currentEditUnit = fieldUnitId != null ? findUnitByIdInTree(unitList, fieldUnitId) : null;
                const unitForField = currentEditUnit ?? unitData;
                const p = unitForField?.Id != null ? findParentOfUnit(unitList, Number(unitForField.Id)) : null;
                if (p?.Id != null && (!p.AppTransactionFieldList || p.AppTransactionFieldList.length === 0)) {
                    parentIdsToLoad.add(Number(p.Id));
                }
            });

            for (const pid of Array.from(parentIdsToLoad)) {
                if (cancelled) return;
                const pRef = findUnitByIdInTree(unitList, pid);
                const full = await ensureUnitFieldsLoaded(pRef);
                if (full?.Id != null) parentUnitById.set(Number(full.Id), full);
            }

            const newList = unitData.AppTransactionFieldList.map((f: any) => {
                if (f?.DdlparentLevelId == null || f.DdlparentLevelId === '' || f.DdlparentLevelIdSelector) return f;
                const pid = Number(f.DdlparentLevelId);

                // Resolve current unit by field.TransactionUnitId (Angular: dictTransactionUnitldAndObj[item.TransactionUnitId])
                const fieldUnitId = f.TransactionUnitId != null ? Number(f.TransactionUnitId) : null;
                const currentEditUnit = fieldUnitId != null ? findUnitByIdInTree(unitList, fieldUnitId) : null;
                const unitForField = currentEditUnit ?? unitData;
                const parentRef: any = unitForField?.Id != null ? findParentOfUnit(unitList, Number(unitForField.Id)) : null;
                const resolvedParentUnit: any =
                    parentRef?.Id != null ? parentUnitById.get(Number(parentRef.Id)) || parentRef : parentRef;

                const parentFieldFromParent =
                    resolvedParentUnit?.AppTransactionFieldList?.find((x: any) => Number(x.Id) === pid) || null;
                const parentFieldFromCurrent =
                    unitForField?.AppTransactionFieldList?.find((x: any) => Number(x.Id) === pid) || null;
                const parentField = parentFieldFromParent || parentFieldFromCurrent;
                const tableName = parentFieldFromParent
                    ? resolvedParentUnit?.DataBaseTableName
                    : unitForField?.DataBaseTableName;
                const fieldName = parentField?.DataBaseFieldName ?? parentField?.DisplayName ?? '';
                const display = tableName && fieldName ? `${tableName} . ${fieldName}` : '';

                // Important: never set empty string here, otherwise refresh can cause an update loop
                const label = display && display.length > 0 ? `Cascading From: ${display}` : `Cascading From: (${pid})`;
                changed = true;
                return { ...f, DdlparentLevelIdSelector: label };
            });

            if (!cancelled && changed) {
                setUnitData({ ...unitData, AppTransactionFieldList: newList });
            }
        };

        ensureParentMappingDisplay();
        return () => {
            cancelled = true;
        };
    }, [unitData, parentUnit]);

    // Collect all units from transaction tree (optionally by level). Used for Tab 2 foreign unit list.
    const collectUnitsFromTree = useCallback((units: any[], level?: number): any[] => {
        const result: any[] = [];
        const visit = (list: any[]) => {
            for (const u of list || []) {
                if (level == null || u.level === level) result.push(u);
                if (u.Children?.length) visit(u.Children);
            }
        };
        visit(units);
        return result;
    }, []);

    // Find unit by Id in transaction tree. Used to get foreign unit for Tab 2 field DataMap.
    const findUnitByIdInTree = useCallback((unitList: any[], targetId: number | null): any | null => {
        if (!targetId) return null;
        for (const u of unitList || []) {
            if (u.Id === targetId) return u;
            if (u.Children?.length) {
                const found = findUnitByIdInTree(u.Children, targetId);
                if (found) return found;
            }
        }
        return null;
    }, []);

    // Foreign key data map (Angular: parentUnit.masterKeyList => DataMap(RowIdentityGuid, DataBaseFieldName))
    const foreignKeyDataMap = React.useMemo(() => {
        const list = resolvedParentPkFields?.length ? resolvedParentPkFields : parentPkFields;
        if (!list || list.length === 0) return null;

        const items = list
            .map((k: any) => ({
                RowIdentityGuid: k?.RowIdentityGuid ?? null,
                DataBaseFieldName: k?.DataBaseFieldName ?? k?.DisplayName ?? k?.FieldName ?? ''
            }))
            .filter((k: any) => k.RowIdentityGuid != null && String(k.RowIdentityGuid).length > 0);

        if (items.length === 0) return null;

        // Angular behavior: always match by RowIdentityGuid.
        return new DataMap(items, 'RowIdentityGuid', 'DataBaseFieldName');
    }, [resolvedParentPkFields, parentPkFields]);

    // Derived values (used in callbacks)
    const isPhysicalModelTableCreated = transactionData?.IsPhysicalModelTableCreated || false;
    const isReadOnly = transactionData?.IsReadOnly || false;

    const handleUnitPropertyChange = (property: string, value: any) => {
        if (!unitData) return;
        setUnitData({ ...unitData, [property]: value });
        setIsModified(true);
        isModifiedRef.current = true;
    };

    const handleFieldChange = (field: any, property: string, value: any) => {
        if (!fieldCollectionView) return;
        const index = fieldCollectionView.sourceCollection.indexOf(field);
        if (index >= 0) {
            field[property] = value;
            fieldCollectionView.refresh();
            setIsModified(true);
            isModifiedRef.current = true;
        }
    };

    // Build foreign unit field DataMap from a unit's AppTransactionFieldList (Tab 2: value = DataBaseFieldName, display = label).
    const buildForeignUnitFieldDataMap = useCallback((foreignUnit: any): DataMap => {
        const list = (foreignUnit?.AppTransactionFieldList ?? [])
            .filter((f: any) => f?.DataBaseFieldName != null)
            .map((f: any) => ({
                DataBaseFieldName: String(f.DataBaseFieldName),
                Display: `${f.DisplayName ?? f.DataBaseFieldName ?? ''} (${f.Id ?? ''})`.trim()
            }));
        return new DataMap(list, 'DataBaseFieldName', 'Display');
    }, []);

    // Datasource selector (Standalone + Tab 2 Foreign Unit + Tab 3 Query) helpers
    const handleOpenDatasourceSelector = async (field: any) => {
        if (!field) return;
        setDatasourceSelectionField(field);

        try {
            dispatch(setIsBusy());
            const entityList = await adminSvc.retrieveAllAppEntityInfoDto(false);
            const safeList: Array<{ Id: number; EntityCode?: string; Name?: string }> = Array.isArray(entityList)
                ? entityList
                : [];

            // Current unit field DataMap: DisplayName + Id + DbName
            const unitFieldList = (unitData?.AppTransactionFieldList ?? [])
                .filter((f: any) => f?.Id != null)
                .map((f: any) => ({
                    Id: Number(f.Id),
                    Display: `${f.DisplayName ?? f.DataBaseFieldName ?? ''} ${f.Id} | ${f.DataBaseFieldName ?? ''}`.trim()
                }));
            const currentUnitFieldDataMap = new DataMap(unitFieldList, 'Id', 'Display');

            // Tab 2: Foreign unit list (same-level units, exclude current)
            const allSameLevel = collectUnitsFromTree(
                transactionData?.AppTransactionUnitList ?? [],
                unitData?.level
            );
            const currentUnitId = unitData?.Id != null ? Number(unitData.Id) : null;
            const foreignUnitList: Array<{ Id: number; Display: string }> = allSameLevel
                .filter((u: any) => u.Id != null && Number(u.Id) !== currentUnitId)
                .map((u: any) => ({
                    Id: Number(u.Id),
                    Display: u.UnitDisplayName || u.UnitName || String(u.Id)
                }));

            // Determine initial tab based on Angular getTransactionFieldDatasourceType:
            // - If DdlQueryText has value => Query Datasource (Tab 3)
            // - Else if DdlForeignUnitId exists => Current Transaction Unit Datasource (Tab 2)
            // - Else => Standalone Datasource (Tab 1)
            const hasQuery = typeof field.DdlQueryText === 'string' && field.DdlQueryText.trim().length > 0;
            const hasForeignUnit = field.DdlForeignUnitId != null && field.DdlForeignUnitId !== '';
            const initialTab: 1 | 2 | 3 = hasQuery ? 3 : hasForeignUnit ? 2 : 1;
            let selectedForeignUnitId: number | null = hasForeignUnit ? Number(field.DdlForeignUnitId) : null;
            let selectedForeignUnitDisplay = field.DdlForeignUnitDisplay || '';
            let foreignUnitFieldDataMap: DataMap = new DataMap([], 'DataBaseFieldName', 'Display');

            if (initialTab === 2 && selectedForeignUnitId != null) {
                let foreignUnit = findUnitByIdInTree(transactionData?.AppTransactionUnitList ?? [], selectedForeignUnitId);
                if (!foreignUnit && selectedForeignUnitId) {
                    try {
                        foreignUnit = await appTransactionService.retrieveOneAppTransactionUnitExDto(String(selectedForeignUnitId));
                    } catch {
                        // keep empty map
                    }
                }
                if (foreignUnit) {
                    selectedForeignUnitDisplay = foreignUnit.UnitDisplayName || foreignUnit.UnitName || String(selectedForeignUnitId);
                    foreignUnitFieldDataMap = buildForeignUnitFieldDataMap(foreignUnit);
                }
            }

            // Entity DB column DataMap (Tab 1: populated after entity selection, or if EntityId already exists)
            let entityDbColumnDataMap: DataMap | null = null;
            const entityIdToLoad = initialTab === 1 ? (field.EntityId ?? null) : null;
            if (entityIdToLoad) {
                try {
                    const entityDto = await adminSvc.retrieveOneAppEntityInfoExDto(String(entityIdToLoad), true);
                    const tableName = entityDto?.TableName ?? null;
                    const schemaOwner = entityDto?.SchemaOwner ?? null;
                    const dsId = entityDto?.DataSourceFrom ?? transactionData?.DataSourceFrom ?? dataSourceRegisterId ?? null;
                    if (tableName && dsId) {
                        const tableSchema = await schemaMetadataService.getOneDatabaseTableSchema(
                            tableName,
                            dsId,
                            schemaOwner
                        );
                        const cols: Array<{ Name: string }> = Array.isArray(tableSchema?.Columns)
                            ? tableSchema.Columns.map((c: any) => ({ Name: String(c.Name) }))
                            : [];
                        entityDbColumnDataMap = new DataMap(cols, 'Name', 'Name');
                    }
                } catch {
                    entityDbColumnDataMap = new DataMap([], 'Name', 'Name');
                }
            }
            if (!entityDbColumnDataMap) {
                entityDbColumnDataMap = new DataMap([], 'Name', 'Name');
            }

            // Rebuild dependent mapping list from unit fields (same for all tabs)
            const ddlFieldIdRaw = field?.Id ?? null;
            const ddlFieldId = ddlFieldIdRaw != null ? Number(ddlFieldIdRaw) : null;
            const existingMappings: any[] = (unitData?.AppTransactionFieldList ?? [])
                .filter((f: any) => {
                    if (ddlFieldId == null) return false;
                    if (f?.MasterEntityFieldlId == null) return false;
                    return Number(f.MasterEntityFieldlId) === ddlFieldId;
                })
                .filter((f: any) => (f?.InnerEntitySubscribeFiled || f?.InnerEntityLabelSubscribeFiled) && f?.Id != null)
                .map((f: any) => ({
                    DependentField: Number(f.Id),
                    SourceColumn: f.InnerEntitySubscribeFiled || '',
                    LabelSourceColumn: f.InnerEntityLabelSubscribeFiled || ''
                }));
            const mappingCv = new CollectionView(existingMappings);

            // Tab 3: Query Datasource - reconstruct selection/condition text and parameters
            let selectionQueryText = '';
            let conditionQueryText = '';
            const conditionParameterList: Array<{ parameterName: string; transactionFieldId: number | null }> = [];
            let parameterTransFieldItems: Array<{ Id: number; Display: string }> = [];

            if (initialTab === 3) {
                const queryText: string = field.DdlQueryText || '';
                if (queryText) {
                    const whereIndex = queryText.toLowerCase().indexOf(' where ');
                    if (whereIndex > 0) {
                        selectionQueryText = queryText.substring(0, whereIndex).trim();
                        conditionQueryText = queryText.substring(whereIndex).trim();
                    } else {
                        selectionQueryText = queryText.trim();
                    }
                }

                const parameterText: string = field.WhereClauseExpress || '';
                if (parameterText) {
                    const transIdTextList = String(parameterText).split('|');
                    transIdTextList.forEach((transIdText) => {
                        if (transIdText && transIdText.trim() && parseInt(transIdText.trim(), 10)) {
                            const transactionFieldId = parseInt(transIdText.trim(), 10);
                            const parameterName = `@p${conditionParameterList.length}`;
                            conditionParameterList.push({ parameterName, transactionFieldId });
                        }
                    });
                }

                // Build parameterTransFieldList from grandparent, parent, and current unit fields (same as Angular)
                const currentEditUnit = unitData;
                if (currentEditUnit) {
                    const buildLookupFromUnit = (aUnit: any | null | undefined) => {
                        if (!aUnit || !aUnit.AppTransactionFieldList) return;
                        (aUnit.AppTransactionFieldList as any[]).forEach((aField: any) => {
                            if (aField && aField.Id != null) {
                                parameterTransFieldItems.push({
                                    Id: Number(aField.Id),
                                    Display: `[${aUnit.UnitDisplayName || aUnit.UnitName || ''}] . [${aField.DataBaseFieldName ?? ''}] (${aField.Id})`
                                });
                            }
                        });
                    };

                    const parent = parentUnit;
                    if (parent) {
                        if ((parent as any).parent) {
                            buildLookupFromUnit((parent as any).parent);
                        }
                        buildLookupFromUnit(parent);
                    }
                    buildLookupFromUnit(currentEditUnit);
                }
            }

            const conditionParameterCV = new CollectionView(conditionParameterList);
            const parameterTransFieldDataMap = new DataMap(parameterTransFieldItems, 'Id', 'Display');

            setDatasourceSelectorState({
                activeTab: initialTab,
                entityList: safeList,
                selectedEntityId: field.EntityId || null,
                selectedEntityDisplay: field.EntityDisplay || '',
                isEnableDependentColumns: existingMappings.length > 0,
                dependentMappings: mappingCv,
                currentUnitFieldDataMap,
                entityDbColumnDataMap,
                foreignUnitList,
                selectedForeignUnitId,
                selectedForeignUnitDisplay,
                foreignUnitFieldDataMap,
                selectionQueryText,
                conditionQueryText,
                conditionParameterList,
                conditionParameterCV,
                parameterTransFieldDataMap
            });
        } catch (error: any) {
            showError(error?.message || 'Failed to load datasource list');
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleDatasourcePopupClose = () => {
        setDatasourceSelectionField(null);
        setDatasourceSelectorState(null);
    };

    const handleAddDependentMappingRow = () => {
        if (!datasourceSelectorState || !datasourceSelectorState.dependentMappings) return;
        const cv = datasourceSelectorState.dependentMappings;
        cv.sourceCollection.push({
            DependentField: '',
            LabelSourceColumn: '',
            SourceColumn: ''
        });
        cv.refresh();
        setDatasourceSelectorState({ ...datasourceSelectorState, dependentMappings: cv });
    };

    const handleRemoveDependentMappingRow = () => {
        if (!datasourceSelectorState || !datasourceSelectorState.dependentMappings) return;
        const cv = datasourceSelectorState.dependentMappings;
        if (cv.sourceCollection.length > 0) {
            cv.sourceCollection.pop();
            cv.refresh();
            setDatasourceSelectorState({ ...datasourceSelectorState, dependentMappings: cv });
        }
    };

    const handleDatasourceSelected = () => {
        if (!datasourceSelectionField || !datasourceSelectorState) {
            handleDatasourcePopupClose();
            return;
        }

        const {
            activeTab,
            selectedEntityId,
            selectedEntityDisplay,
            isEnableDependentColumns,
            dependentMappings,
            selectedForeignUnitId,
            selectedForeignUnitDisplay,
            selectionQueryText,
            conditionQueryText,
            conditionParameterList
        } =
            datasourceSelectorState;

        if (activeTab === 3) {
            // Tab 3: Query Datasource
            const selectionText = (selectionQueryText || '').trim();
            const conditionText = (conditionQueryText || '').trim();
            const fullQuery = `${selectionText} ${conditionText}`.trim();

            datasourceSelectionField.DdlQueryText = fullQuery || null;
            datasourceSelectionField.WhereClauseExpress = '';
            datasourceSelectionField.DdlForeignUnitId = null;
            datasourceSelectionField.DdlForeignUnitDisplay = null;
            datasourceSelectionField.EntityId = null;
            datasourceSelectionField.EntityDisplay = 'Query';
            datasourceSelectionField.EntityIdSelector = 'Query';

            if (conditionParameterList && conditionParameterList.length > 0) {
                const ids = conditionParameterList
                    .filter((p) => p.transactionFieldId)
                    .map((p) => String(p.transactionFieldId));
                datasourceSelectionField.WhereClauseExpress = ids.join('|');
            }
        } else if (activeTab === 2) {
            // Tab 2: Current Data Model Unit Datasource
            datasourceSelectionField.DdlForeignUnitId = selectedForeignUnitId ?? null;
            datasourceSelectionField.DdlForeignUnitDisplay = selectedForeignUnitDisplay || '';
            const unitDisplay = selectedForeignUnitDisplay ? `Unit: ${selectedForeignUnitDisplay} (${selectedForeignUnitId ?? ''})` : '';
            datasourceSelectionField.EntityIdSelector = unitDisplay;
            datasourceSelectionField.EntityDisplay = unitDisplay;
            datasourceSelectionField.EntityId = null;
            datasourceSelectionField.DdlQueryText = null;
            datasourceSelectionField.WhereClauseExpress = null;
        } else {
            // Tab 1: Standalone Datasource
            datasourceSelectionField.EntityId = selectedEntityId || null;
            datasourceSelectionField.EntityDisplay = selectedEntityDisplay || '';
            datasourceSelectionField.EntityIdSelector = selectedEntityDisplay || '';
            datasourceSelectionField.DdlForeignUnitId = null;
            datasourceSelectionField.DdlForeignUnitDisplay = null;
            datasourceSelectionField.DdlQueryText = null;
            datasourceSelectionField.WhereClauseExpress = null;
        }

        // Apply dependent mappings to unit fields (same for Tab 1 and Tab 2)
        const ddlFieldIdRaw = datasourceSelectionField.Id ?? null;
        const ddlFieldId = ddlFieldIdRaw != null ? Number(ddlFieldIdRaw) : null;
        if (ddlFieldId != null && unitData?.AppTransactionFieldList) {
            const mappings = dependentMappings ? [...dependentMappings.sourceCollection] : [];
            unitData.AppTransactionFieldList.forEach((transField: any) => {
                if (transField.MasterEntityFieldlId != null && Number(transField.MasterEntityFieldlId) === ddlFieldId) {
                    transField.MasterEntityFieldlId = null;
                    transField.InnerEntitySubscribeFiled = '';
                    transField.InnerEntityLabelSubscribeFiled = '';
                }

                const m = mappings.find((x: any) => Number(x.DependentField) === Number(transField.Id));
                if (m && (m.SourceColumn || m.LabelSourceColumn) && m.DependentField) {
                    transField.MasterEntityFieldlId = ddlFieldId;
                    transField.InnerEntitySubscribeFiled = m.SourceColumn || '';
                    transField.InnerEntityLabelSubscribeFiled = m.LabelSourceColumn || '';
                }
            });
        }

        if (fieldCollectionView) {
            fieldCollectionView.refresh();
        }
        setIsModified(true);
        isModifiedRef.current = true;

        handleDatasourcePopupClose();
    };

    const handleOpenEntityDataPreview = (field: any) => {
        const entityId = field?.EntityId ?? null;
        if (!entityId) return;
        setEntityDataPreviewOpen({
            entityId: Number(entityId),
            entityCode: field?.EntityIdSelector || field?.EntityDisplay || undefined
        });
    };

    // Transaction Field Security (Angular: editTransactionFieldSecurity -> embedded SystemObjectSecurityEditor)
    const handleOpenTransactionFieldSecurity = (field: any) => {
        const fieldId = field?.Id != null ? Number(field.Id) : null;
        if (!fieldId) {
            showInfo('Please save the data model first. Security editing requires a saved Transaction Field.');
            return;
        }
        const display = field?.DataBaseFieldName || field?.DisplayName || '';
        setFieldSecurityPopup({ open: true, fieldId, display });
    };

    const handleOpenParentDataSourceMapping = async (field: any) => {
        if (!field) return;
        const currentFieldName = field.DisplayName || field.DataBaseFieldName || '';
        const ctlIds = controlTypeIdsForDatasource;
        const allowed: number[] = [
            ctlIds.DDL,
            ctlIds.AutoComplete!,
            ctlIds.SearchAbleDDL!
        ].filter((v) => typeof v === 'number') as number[];

        const parentFields: Array<{ Id: number; Display: string }> = [];
        const relationTableOptions: Array<{ schemaOwner: string | null; name: string; display: string }> = [];
        const unitList = transactionData?.AppTransactionUnitList ?? [];

        // Resolve current edit unit by field (Angular: dictTransactionUnitldAndObj[item.TransactionUnitId])
        const fieldUnitId = field.TransactionUnitId != null ? Number(field.TransactionUnitId) : null;
        const currentEditUnit = fieldUnitId != null
            ? findUnitByIdInTree(unitList, fieldUnitId)
            : null;
        const unitForField = currentEditUnit ?? unitData;

        // Resolve parent of the field's unit from tree (Angular: currentEditUnit.parent)
        const findParentOfUnit = (list: any[], childId: number | null): any | null => {
            if (!childId) return null;
            for (const u of list || []) {
                if (u.Children) {
                    const found = u.Children.find((c: any) => c.Id === childId);
                    if (found) return u;
                    const inner = findParentOfUnit(u.Children, childId);
                    if (inner) return inner;
                }
            }
            return null;
        };
        const resolvedParentUnitRef = unitForField?.Id != null
            ? findParentOfUnit(unitList, Number(unitForField.Id))
            : null;

        const ensureUnitFieldsLoaded = async (aUnit: any | null | undefined): Promise<any | null> => {
            if (!aUnit || aUnit.AppTransactionFieldList?.length) return aUnit || null;
            if (!aUnit.Id) return aUnit || null;
            try {
                const fullUnit = await appTransactionService.retrieveOneAppTransactionUnitExDto(String(aUnit.Id));
                return fullUnit || aUnit;
            } catch {
                return aUnit || null;
            }
        };

        const pushFromUnit = (aUnit: any | null | undefined) => {
            if (!aUnit || !aUnit.AppTransactionFieldList) return;
            (aUnit.AppTransactionFieldList as any[]).forEach((pf: any) => {
                if (
                    pf &&
                    pf.Id != null &&
                    Number(pf.Id) !== Number(field.Id) &&
                    allowed.includes(Number(pf.ControlType))
                ) {
                    parentFields.push({
                        Id: Number(pf.Id),
                        Display: `[${aUnit.DataBaseTableName || ''}] . [${pf.DataBaseFieldName ?? ''}]`
                    });
                }
            });
        };

        const resolvedParentUnit = await ensureUnitFieldsLoaded(resolvedParentUnitRef);
        const resolvedCurrentUnit = await ensureUnitFieldsLoaded(unitForField);

        if (resolvedParentUnit) {
            pushFromUnit(resolvedParentUnit);
        }
        pushFromUnit(resolvedCurrentUnit);

        // Load available relation tables from schema (Angular: databaseSchemaData)
        try {
            const dsId = transactionData?.DataSourceFrom ?? dataSourceRegisterId ?? null;
            if (dsId != null) {
                const tableList = await schemaMetadataService.getDataSourceTableAndViewListFromCache(dsId, null, null);
                const arr = Array.isArray(tableList) ? tableList : [];
                arr.forEach((t: any) => {
                    const schemaOwner = t.SchemaOwner || t.schemaOwner || null;
                    const name = t.Name || t.name || '';
                    if (!name) return;
                    const display =
                        schemaOwner && schemaOwner.length > 0 ? `${schemaOwner}.${name}` : name;
                    relationTableOptions.push({
                        schemaOwner,
                        name,
                        display
                    });
                });
            }
        } catch {
            // ignore schema load errors; user can still type manually
        }

        // If there is an existing relation table, try to pre-load its columns
        let relationTableColumnOptions: Array<{ name: string }> = [];
        const existingSchemaOwner = field.CascadingRelationTableSchemaOwner || null;
        const existingTableName = field.CascadingRelationTable || '';
        if (existingTableName) {
            try {
                const dsId = transactionData?.DataSourceFrom ?? dataSourceRegisterId ?? null;
                const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
                    existingTableName,
                    dsId,
                    existingSchemaOwner
                );
                const cols = Array.isArray(tableData?.Columns) ? tableData.Columns : [];
                relationTableColumnOptions = cols
                    .filter((c: any) => c?.Name)
                    .map((c: any) => ({ name: String(c.Name) }));
            } catch {
                relationTableColumnOptions = [];
            }
        }

        setParentDataSourceMappingState({
            isOpen: true,
            field,
            currentFieldName,
            parentFieldId: field.DdlparentLevelId ?? null,
            relationTable: field.CascadingRelationTable || '',
            relationTableSchemaOwner: field.CascadingRelationTableSchemaOwner || null,
            relationParentKey: field.CascadingRelationTableParentKeyField || '',
            relationChildKey: field.CascadingRelationTableChildKeyField || '',
            parentFieldOptions: parentFields,
            relationTableOptions,
            relationTableColumnOptions
        });
    };

    const handleApplyParentDataSourceMapping = () => {
        const { field, parentFieldId, relationTable, relationTableSchemaOwner, relationParentKey, relationChildKey, parentFieldOptions } = parentDataSourceMappingState;
        if (!field || !unitData) {
            setParentDataSourceMappingState((prev) => ({ ...prev, isOpen: false }));
            return;
        }

        field.DdlparentLevelId = parentFieldId || null;
        field.CascadingRelationTable = relationTable || null;
        field.CascadingRelationTableSchemaOwner = relationTable ? (relationTableSchemaOwner || null) : null;
        field.CascadingRelationTableParentKeyField = relationParentKey || null;
        field.CascadingRelationTableChildKeyField = relationChildKey || null;

        const selectedOption = parentFieldId != null && parentFieldOptions?.length
            ? parentFieldOptions.find((o) => Number(o.Id) === Number(parentFieldId))
            : null;
        const parentName = selectedOption?.Display ?? (parentFieldId != null ? String(parentFieldId) : '');
        field.DdlparentLevelIdSelector = parentName ? `Depend On DDL Field: ${parentName}` : '';

        if (fieldCollectionView) {
            fieldCollectionView.refresh();
        }
        setIsModified(true);
        isModifiedRef.current = true;

        setParentDataSourceMappingState((prev) => ({ ...prev, isOpen: false }));
    };

    const handleClearParentDataSourceMapping = () => {
        const { field } = parentDataSourceMappingState;
        if (field) {
            field.DdlparentLevelId = null;
            field.CascadingRelationTable = null;
            field.CascadingRelationTableSchemaOwner = null;
            field.CascadingRelationTableParentKeyField = null;
            field.CascadingRelationTableChildKeyField = null;
            field.DdlparentLevelIdSelector = '';
        }
        if (fieldCollectionView) {
            fieldCollectionView.refresh();
        }
        setIsModified(true);
        isModifiedRef.current = true;
        setParentDataSourceMappingState((prev) => ({ ...prev, isOpen: false }));
    };

    // ── Matrix Grid Foreignkey popup ─────────────────────────────────────────
    // Build a lookup of every transaction field in the form by Id (Angular: dictTransactionFieldIdAndObj).
    const collectAllUnits = useCallback((list: any[]): any[] => {
        const out: any[] = [];
        const visit = (arr: any[]) => {
            (arr || []).forEach((u: any) => {
                out.push(u);
                if (u?.Children?.length) visit(u.Children);
            });
        };
        visit(list || []);
        return out;
    }, []);

    const allFieldsById = useMemo(() => {
        const dict: Record<string, any> = {};
        const units = collectAllUnits(transactionData?.AppTransactionUnitList ?? []);
        units.forEach((u: any) => {
            (u?.AppTransactionFieldList ?? []).forEach((f: any) => {
                if (f?.Id != null) dict[String(f.Id)] = f;
            });
        });
        // include the currently edited unit's (possibly unsaved) fields
        (unitData?.AppTransactionFieldList ?? []).forEach((f: any) => {
            if (f?.Id != null) dict[String(f.Id)] = f;
        });
        return dict;
    }, [transactionData, unitData, collectAllUnits]);

    // Angular getDdlColumnNameById: resolve a field id to its display column name.
    const getDdlColumnNameById = useCallback((fieldId: any): string => {
        if (fieldId == null) return '';
        const f = allFieldsById[String(fieldId)];
        if (!f) return String(fieldId);
        return f.DisplayName || f.DataBaseFieldName || String(fieldId);
    }, [allFieldsById]);

    // Source-unit options = root unit's direct children (Angular: AppTransactionUnitList[0].Children).
    const matrixFkSourceUnitOptions = useMemo(() => {
        const root = transactionData?.AppTransactionUnitList?.[0] ?? null;
        const children: any[] = root?.Children ?? [];
        return children
            .filter((u: any) => u?.Id != null)
            .map((u: any) => ({
                Id: Number(u.Id),
                Display: u.UnitDisplayName || u.DataBaseTableName || `Unit ${u.Id}`
            }));
    }, [transactionData]);

    const buildMatrixFkSourceFieldOptions = useCallback((sourceUnitId: number | null): Array<{ Id: number; Display: string }> => {
        if (sourceUnitId == null) return [];
        const units = collectAllUnits(transactionData?.AppTransactionUnitList ?? []);
        const unit = units.find((u: any) => Number(u?.Id) === Number(sourceUnitId));
        return (unit?.AppTransactionFieldList ?? [])
            .filter((f: any) => f?.Id != null)
            .map((f: any) => ({
                Id: Number(f.Id),
                Display: f.DisplayName || f.DataBaseFieldName || String(f.Id)
            }));
    }, [transactionData, collectAllUnits]);

    const handleOpenMatrixFkPopup = useCallback((field: any) => {
        if (!field || !field.Id) return;
        // Angular: only DDL / AutoComplete / SearchAbleDDL fields with a unit can map a matrix FK.
        const ctl = Number(field.ControlType);
        const allowed: number[] = [
            controlTypeIdsForDatasource.DDL,
            controlTypeIdsForDatasource.AutoComplete!,
            controlTypeIdsForDatasource.SearchAbleDDL!
        ].filter((v) => typeof v === 'number') as number[];
        if (!allowed.includes(ctl)) return;

        // Pre-select the source unit from the existing FK field mapping, if any.
        let sourceUnitId: number | null = null;
        if (field.MatrixForeignKeyFieldId) {
            const fkField = allFieldsById[String(field.MatrixForeignKeyFieldId)];
            if (fkField?.TransactionUnitId != null) sourceUnitId = Number(fkField.TransactionUnitId);
        }

        setMatrixFkPopupState({
            isOpen: true,
            field,
            currentFieldName: field.DisplayName || field.DataBaseFieldName || '',
            sourceUnitId,
            matrixForeignKeyFieldId: field.MatrixForeignKeyFieldId ?? null,
            sourceUnitOptions: matrixFkSourceUnitOptions,
            sourceFieldOptions: buildMatrixFkSourceFieldOptions(sourceUnitId)
        });
    }, [controlTypeIdsForDatasource, allFieldsById, matrixFkSourceUnitOptions, buildMatrixFkSourceFieldOptions]);

    const handleMatrixFkSourceUnitChange = useCallback((sourceUnitId: number | null) => {
        setMatrixFkPopupState((prev) => ({
            ...prev,
            sourceUnitId,
            // changing the source unit resets the field choice (Angular matrixFkPopupSourceUnitChangeChanged)
            matrixForeignKeyFieldId: null,
            sourceFieldOptions: buildMatrixFkSourceFieldOptions(sourceUnitId)
        }));
    }, [buildMatrixFkSourceFieldOptions]);

    const handleApplyMatrixFkMapping = useCallback(() => {
        const { field, matrixForeignKeyFieldId } = matrixFkPopupState;
        if (field) {
            field.MatrixForeignKeyFieldId = matrixForeignKeyFieldId ?? null;
            field.IsModified = true;
            field.MatrixForeignKeyFieldIdSelector = matrixForeignKeyFieldId
                ? getDdlColumnNameById(matrixForeignKeyFieldId)
                : '';
            if (fieldCollectionView) fieldCollectionView.refresh();
            setIsModified(true);
            isModifiedRef.current = true;
        }
        setMatrixFkPopupState((prev) => ({ ...prev, isOpen: false, field: null }));
    }, [matrixFkPopupState, getDdlColumnNameById, fieldCollectionView]);

    const handleClearMatrixFkMapping = useCallback(() => {
        const { field } = matrixFkPopupState;
        if (field) {
            field.MatrixForeignKeyFieldId = null;
            field.IsModified = true;
            field.MatrixForeignKeyFieldIdSelector = '';
            if (fieldCollectionView) fieldCollectionView.refresh();
            setIsModified(true);
            isModifiedRef.current = true;
        }
        setMatrixFkPopupState((prev) => ({ ...prev, isOpen: false, field: null }));
    }, [matrixFkPopupState, fieldCollectionView]);

    const handleCloseMatrixFkPopup = useCallback(() => {
        setMatrixFkPopupState((prev) => ({ ...prev, isOpen: false, field: null }));
    }, []);

    const handleCellEditEnded = (sender: any, e: any) => {
        // Mark unit as modified when any cell is edited
        setIsModified(true);
        isModifiedRef.current = true;
    };

    // Helper to get max sort order
    const getMaxSortOrder = useCallback((): number => {
        if (!unitData || !unitData.AppTransactionFieldList || unitData.AppTransactionFieldList.length === 0) {
            return 0;
        }
        return Math.max(...unitData.AppTransactionFieldList.map((f: any) => f.SortOrder || 0));
    }, [unitData]);

    // Helper to generate unique ID
    const generateGuid = () => {
        return `field_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    };

    // Add Temp Field
    const handleAddTempField = useCallback(() => {
        if (!unitData || !fieldCollectionView) return;

        const maxSortOrder = getMaxSortOrder();
        const newField = {
            uiId: generateGuid(),
            RowIdentityGuid: generateGuid(),
            SortOrder: Math.ceil(maxSortOrder / 10.0) * 10 + 10,
            DisplayName: `Field${Math.ceil(maxSortOrder / 10.0) * 10 + 10}`,
            DataBaseFieldName: '',
            ControlType: 2, // TextBox (default)
            DataType: 1, // String (default)
            DisplayWidth: 100,
            Nbdecimal: 0,
            IsVisible: true,
            IsReadonly: false,
            IsGroupBy: false,
            IsGridUseAvailableEntitySource: false,
            IsNeedLog: false,
            IsLogicalDisplay: false,
            IsChangeTrigerNotification: false,
            IsAllowEmpty: true,
            IsConvertToUpperCase: false,
            IsPrimaryKey: false,
            IsLinkToParentPrimaryKey: false,
            IsTempVariable: true,
            IsStoreToExtendTable: false
        };

        const updatedFields = [...(unitData.AppTransactionFieldList || []), newField];
        setUnitData({ ...unitData, AppTransactionFieldList: updatedFields });
        setIsModified(true);
        isModifiedRef.current = true;
    }, [unitData, fieldCollectionView, getMaxSortOrder]);

    // Add Table Extend Field
    const handleAddTableExtendField = useCallback(() => {
        if (!unitData || !fieldCollectionView) return;

        const maxSortOrder = getMaxSortOrder();
        const newField = {
            uiId: generateGuid(),
            RowIdentityGuid: generateGuid(),
            SortOrder: Math.ceil(maxSortOrder / 10.0) * 10 + 10,
            DisplayName: `Field${Math.ceil(maxSortOrder / 10.0) * 10 + 10}`,
            DataBaseFieldName: '',
            ControlType: 2, // TextBox
            DataType: 1, // String
            DisplayWidth: 100,
            Nbdecimal: 0,
            IsVisible: true,
            IsReadonly: false,
            IsGroupBy: false,
            IsGridUseAvailableEntitySource: false,
            IsNeedLog: false,
            IsLogicalDisplay: false,
            IsChangeTrigerNotification: false,
            IsAllowEmpty: true,
            IsConvertToUpperCase: false,
            IsPrimaryKey: false,
            IsLinkToParentPrimaryKey: false,
            IsTempVariable: false,
            IsStoreToExtendTable: true
        };

        const updatedFields = [...(unitData.AppTransactionFieldList || []), newField];
        setUnitData({ ...unitData, AppTransactionFieldList: updatedFields });
        setIsModified(true);
        isModifiedRef.current = true;
    }, [unitData, fieldCollectionView, getMaxSortOrder]);

    // Remove Field
    const handleRemoveField = useCallback(() => {
        if (!unitData || !flexGridRef.current) return;

        const grid = flexGridRef.current.control;
        if (!grid || !grid.selectedRows || grid.selectedRows.length === 0) {
            showInfo('Please select one or more fields to remove');
            return;
        }

        const selectedFields: any[] = [];
        grid.selectedRows.forEach((row: any) => {
            if (row.dataItem) {
                selectedFields.push(row.dataItem);
            }
        });

        if (selectedFields.length === 0) {
            showInfo('Please select one or more fields to remove');
            return;
        }

        // Remove selected fields
        const updatedFields = (unitData.AppTransactionFieldList || []).filter(
            (field: any) => !selectedFields.some((sf: any) =>
                (sf.Id && field.Id && sf.Id === field.Id) ||
                (sf.RowIdentityGuid && field.RowIdentityGuid && sf.RowIdentityGuid === field.RowIdentityGuid) ||
                (sf === field)
            )
        );

        // Track DB-persisted fields (with an Id) so the save can delete them server-side
        // (DictDeletedItemsIds["AppTransactionFieldList_<unitId>"]). Without this, the backend
        // upserts the remaining list and the removed rows come back on reload.
        const removedIds = selectedFields.map((f: any) => f.Id).filter((id: any) => id != null);
        const existingDeleted = Array.isArray(unitData.DeletedFieldIdList) ? unitData.DeletedFieldIdList : [];
        const nextDeleted = Array.from(new Set([...existingDeleted, ...removedIds]));

        setUnitData({ ...unitData, AppTransactionFieldList: updatedFields, DeletedFieldIdList: nextDeleted });
        setIsModified(true);
        isModifiedRef.current = true;
    }, [unitData, showInfo]);

    // Remove All Fields not in Table/Query
    const handleRemoveFieldsNotInTableOrQuery = useCallback(async () => {
        if (!unitData || !transactionData) return;

        try {
            dispatch(setIsBusy());

            if (isPhysicalModelTableCreated && unitData.DataBaseTableName) {
                // Remove fields not in table
                const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
                    unitData.DataBaseTableName,
                    transactionData.DataSourceFrom || dataSourceRegisterId,
                    unitData.SchemaOwner || null
                );

                if (tableData && tableData.Columns) {
                    const tableColumnNames = tableData.Columns.map((col: any) => col.Name);
                    const keepField = (field: any) => {
                        // Keep temp variables and fields that exist in table
                        if (field.IsTempVariable) return true;
                        if (!field.DataBaseFieldName) return true;
                        return tableColumnNames.includes(field.DataBaseFieldName);
                    };
                    const updatedFields = (unitData.AppTransactionFieldList || []).filter(keepField);

                    // Track DB-persisted removed fields so the save deletes them server-side.
                    const removedIds = (unitData.AppTransactionFieldList || [])
                        .filter((f: any) => !keepField(f))
                        .map((f: any) => f.Id)
                        .filter((id: any) => id != null);
                    const existingDeleted = Array.isArray(unitData.DeletedFieldIdList) ? unitData.DeletedFieldIdList : [];
                    const nextDeleted = Array.from(new Set([...existingDeleted, ...removedIds]));

                    setUnitData({ ...unitData, AppTransactionFieldList: updatedFields, DeletedFieldIdList: nextDeleted });
                    setIsModified(true);
                    isModifiedRef.current = true;
                }
            } else if (isReadOnly && unitData.DataSourceQuery) {
                // Remove fields not in query - TODO: implement query column retrieval
                showInfo('Remove fields not in query - to be implemented');
            }
        } catch (error: any) {
            showError(error.message || 'Failed to remove fields');
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [unitData, transactionData, isPhysicalModelTableCreated, isReadOnly, dataSourceRegisterId, dispatch, showError, showInfo]);

    // Add Existing Table Field
    const handleAddExistingTableField = useCallback(() => {
        if (!unitData || !unitData.DataBaseTableName) {
            showError('Unit must have a database table name');
            return;
        }
        setTableColumnSelectorDialog({ isOpen: true });
    }, [unitData, showError]);

    // Handle table column selection
    const handleTableColumnsSelected = useCallback(async (selectedColumns: any[]) => {
        if (!unitData || !transactionData || selectedColumns.length === 0) return;

        try {
            dispatch(setIsBusy());

            // Check for duplicates
            const existingFieldNames = new Set(
                (unitData.AppTransactionFieldList || [])
                    .filter((f: any) => f.DataBaseFieldName && !f.IsTempVariable)
                    .map((f: any) => f.DataBaseFieldName)
            );

            const needToAddColumns = selectedColumns.filter((col: any) =>
                !existingFieldNames.has(col.Name)
            );

            const duplicateNames = selectedColumns
                .filter((col: any) => existingFieldNames.has(col.Name))
                .map((col: any) => col.Name);

            if (duplicateNames.length > 0) {
                showInfo(`The following fields already exist and will not be added: ${duplicateNames.join(', ')}`);
            }

            if (needToAddColumns.length === 0) {
                setTableColumnSelectorDialog({ isOpen: false });
                return;
            }

            // Get parent unit for converter (do not rely on unitData.level; may be undefined)
            const parentUnitForConverter = getParentUnit();

            // Convert table columns to transaction fields
            const converterDto = {
                SchemaOwner: unitData.SchemaOwner || '',
                TableName: unitData.DataBaseTableName,
                ParentUnit: parentUnitForConverter,
                TransactionId: transactionData.Id,
                NeedToAddDbColumns: needToAddColumns,
                DataSourceRegisterId: transactionData.DataSourceFrom || dataSourceRegisterId
            };

            const transFieldList = await appTransactionService.convertTableColumnsToTransactionFieldExDtoList(converterDto);

            if (transFieldList && transFieldList.length > 0) {
                const maxSortOrder = getMaxSortOrder();
                let nextSortOrder = Math.ceil(maxSortOrder / 10.0) * 10 + 10;

                const newFields = transFieldList.map((fieldDto: any) => {
                    const newField = {
                        ...fieldDto,
                        SortOrder: nextSortOrder,
                        uiId: generateGuid()
                    };
                    nextSortOrder += 10;
                    return newField;
                });

                const updatedFields = [...(unitData.AppTransactionFieldList || []), ...newFields];
                setUnitData({ ...unitData, AppTransactionFieldList: updatedFields });
                setIsModified(true);
                isModifiedRef.current = true;

                // Update DictCurrentPKOrFKLinkToParentKeyGuidMap if needed
                if (transactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap) {
                    newFields.forEach((field: any) => {
                        if (field.ParentPKFieldGuid && field.RowIdentityGuid) {
                            transactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap[field.RowIdentityGuid] = field.ParentPKFieldGuid;
                        }
                    });
                }
            }

            setTableColumnSelectorDialog({ isOpen: false });
        } catch (error: any) {
            showError(error.message || 'Failed to add table fields');
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [unitData, transactionData, dataSourceRegisterId, dispatch, showError, showInfo, getMaxSortOrder]);

    const handleSave = () => {
        if (!unitData) return;

        if (onSave) {
            onSave(unitData, true);
        }

        setIsModified(false);
        isModifiedRef.current = false;
        originalUnitDataRef.current = JSON.parse(JSON.stringify(unitData));
    };

    const handleSaveAndClose = () => {
        handleSave();
        // Close after save
        onClose();
    };

    const handleClose = async () => {
        // Wait 300ms to allow cell editing to complete and isModified to update
        await new Promise(resolve => setTimeout(resolve, 300));

        // Check if modified after the delay - use ref to get the latest value
        // Also compare current data with original data as a fallback
        const hasChanges = isModifiedRef.current || isModified;

        // Additional check: compare current unitData with original
        let dataChanged = false;
        if (unitData && originalUnitDataRef.current) {
            dataChanged = JSON.stringify(unitData) !== JSON.stringify(originalUnitDataRef.current);
        }

        if (!hasChanges && !dataChanged) {
            onClose();
            return;
        }

        // Unsaved changes: close without prompting (popup disabled). Update local state only.
        if (unitData && onSave) {
            onSave(unitData, false); // false = don't save to server, just update local state
        }
        setIsModified(false);
        isModifiedRef.current = false;
        originalUnitDataRef.current = JSON.parse(JSON.stringify(unitData));
        onClose();
    };

    const handleBackdropClick = (e: React.MouseEvent) => {
        // Prevent closing on backdrop click
        e.stopPropagation();
    };

    if (!isOpen || !unitData) return null;

    const isMasterDetail = transactionData?.TransactionOrganizedType === 1;
    const isSiblingUnit = Boolean(unitData.IsMasterSiblingUnit);
    const hasParent = !isSiblingUnit && Boolean(parentUnit);
    const isRootMasterUnit = !isSiblingUnit && !parentUnit;
    const isTopLevelUnit = isSiblingUnit || isRootMasterUnit;

    const canOpenDataLoad =
        isMasterDetail &&
        !isSiblingUnit &&
        Boolean(transactionData?.Id) &&
        Boolean(unitData.Id);

    const handleOpenDataLoad = () => {
        if (!canOpenDataLoad) {
            showInfo('Data Load is only available for child units in a Master-Detail data model that has been saved.');
            return;
        }
        setIsDataLoadDialogOpen(true);
    };

    const handleCreateDdlEntity = () => {
        const dataSourceFrom = transactionData?.DataSourceFrom ?? dataSourceRegisterId ?? null;
        const saasApplicationId =
            transactionData?.SaasApplicationId ??
            (applicationId ? Number(applicationId) || null : null);

        if (!dataSourceFrom) {
            showError('Cannot open DDL Entity editor: data source is not available. Please save the data model first.');
            return;
        }

        addTabAndNavigate('entity-info-edit', 'New Entity', {
            id: null,
            param1: dataSourceFrom,
            param2: { applicationId: saasApplicationId },
        });
    };

    return (
        <div
            className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[9999]"
            onClick={handleBackdropClick}
        >
            <div
                className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
                style={{ width: '95vw', height: '95vh' }}
                onClick={(e) => e.stopPropagation()}
            >
                {/* Header */}
                <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
                    <h3 className={`text-base font-semibold ${theme.title}`}>
                        Edit Unit: {unitData.UnitDisplayName || unitData.UnitName || 'Unit'}
                    </h3>
                    <div className="flex items-center gap-2">
                        {/* Save - hide if transaction is new (no id); unit save requires saved transaction */}
                        {transactionData?.Id && (
                            <button
                                onClick={handleSave}
                                disabled={!isModified}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1`}
                                title="Save Unit"
                            >
                                <i className="fa-solid fa-floppy-disk"></i>
                                <span>Save</span>
                            </button>
                        )}

                        {/* Link To Other Pages - dropdown: Navigate to Data Model / Search / Any Page */}
                        <div className="relative">
                            <button
                                type="button"
                                onClick={() => setLinkTargetDropdownOpen((o) => !o)}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                title="Link To Other Pages"
                            >
                                <i className="fa-solid fa-clipboard"></i>
                                <span>Link To Other Pages</span>
                                <i className="fa-solid fa-caret-down text-[10px] ml-0.5"></i>
                            </button>
                            {linkTargetDropdownOpen && (
                                <>
                                    <div
                                        className="fixed inset-0 z-[9998]"
                                        onClick={() => setLinkTargetDropdownOpen(false)}
                                        aria-hidden
                                    />
                                    <div
                                        className={`absolute right-0 top-full mt-1 py-1 min-w-[220px] rounded shadow-lg border z-[9999] ${theme.mainContentSection}`}
                                    >
                                        <button
                                            type="button"
                                            className={`w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5`}
                                            onClick={() => {
                                                setLinkTargetEditor({ usageType: 101, title: 'Unit Navigate To Data Model' });
                                                setLinkTargetDropdownOpen(false);
                                            }}
                                        >
                                            <span className="w-4 inline-flex items-center justify-center">
                                                <i className="fa-solid fa-file-lines"></i>
                                            </span>
                                            Unit Navigate To Data Model
                                        </button>
                                        <button
                                            type="button"
                                            className={`w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5`}
                                            onClick={() => {
                                                setLinkedSearchManagementOpen(true);
                                                setLinkTargetDropdownOpen(false);
                                            }}
                                        >
                                            <span className="w-4 inline-flex items-center justify-center">
                                                <i className="fa-solid fa-magnifying-glass"></i>
                                            </span>
                                            Unit Navigate To Search
                                        </button>
                                        <button
                                            type="button"
                                            className={`w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5`}
                                            onClick={() => {
                                                setLinkTargetEditor({ usageType: 5, title: 'Unit Navigate To Any Page' });
                                                setLinkTargetDropdownOpen(false);
                                            }}
                                        >
                                            <span className="w-4 inline-flex items-center justify-center">
                                                <i className="fa-solid fa-file"></i>
                                            </span>
                                            Unit Navigate To Any Page
                                        </button>
                                    </div>
                                </>
                            )}
                        </div>

                        {/* Data Load - only relevant for master-detail and non-sibling units */}
                        {isMasterDetail && !isSiblingUnit && (
                            <button
                                type="button"
                                onClick={handleOpenDataLoad}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                disabled={!canOpenDataLoad}
                                title="Data Load"
                            >
                                Data Load
                            </button>
                        )}

                        {/* Delete Flow - only relevant for master-detail and non-sibling units */}
                        {isMasterDetail && !isSiblingUnit && (
                            <button
                                type="button"
                                onClick={() => setDeleteFlowEditorOpen(true)}
                                disabled={!unitData?.Id}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed`}
                                title="Delete Flow"
                            >
                                Delete Flow
                            </button>
                        )}

                        {/* Create DDL Entity */}
                        <button
                            type="button"
                            onClick={handleCreateDdlEntity}
                            className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                            title="Create DDL Entity"
                        >
                            Create DDL Entity
                        </button>

                        {/* More Unit Options */}
                        <button
                            type="button"
                            onClick={() => setMoreUnitOptionsOpen(true)}
                            className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                            title="More Unit Options"
                        >
                            <i className="fa fa-cogs"></i>
                            <span>More Unit Options</span>
                        </button>

                        {/* Close */}
                        <button
                            onClick={handleClose}
                            className={`p-1 ${theme.button_default} rounded-[4px] transition-all duration-200 hover:shadow-sm active:scale-95`}
                            title="Close"
                        >
                            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                <path
                                    fillRule="evenodd"
                                    d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                                    clipRule="evenodd"
                                />
                            </svg>
                        </button>
                    </div>
                </div>

                {/* Unit Properties (layout aligned to Angular) */}
                <div className={`px-4 py-3 border-b ${theme.mainContentSection}`}>
                    <div className="flex flex-col gap-2">
                        <div className="flex items-center gap-6 flex-wrap">
                            <div className="flex items-center">
                                <div className={`w-32 text-xs ${theme.label} mr-2`}>Unit Display Name</div>
                                <input
                                    type="text"
                                    autoComplete="off"
                                    value={unitData.UnitDisplayName || ''}
                                    onChange={(e) => handleUnitPropertyChange('UnitDisplayName', e.target.value)}
                                    className={`h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none w-[260px]`}
                                />
                            </div>

                            {isPhysicalModelTableCreated && !unitData.IsVirtualUnit && (
                                <div className="flex items-center">
                                    <div className={`w-32 text-xs ${theme.label} mr-2`}>Database Table Name</div>
                                    <input
                                        type="text"
                                        autoComplete="off"
                                        value={unitData.DataBaseTableName || ''}
                                        onChange={(e) => handleUnitPropertyChange('DataBaseTableName', e.target.value)}
                                        className={`h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none w-[260px]`}
                                    />
                                </div>
                            )}

                            {isPhysicalModelTableCreated && !unitData.IsVirtualUnit && (
                                <div className="flex items-center">
                                    <div className={`w-32 text-xs ${theme.label} mr-2`}>DB View Base Table</div>
                                    <input
                                        type="text"
                                        autoComplete="off"
                                        value={unitData.BaseDataBaseTableName || ''}
                                        onChange={(e) => handleUnitPropertyChange('BaseDataBaseTableName', e.target.value)}
                                        className={`h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none w-[260px]`}
                                    />
                                </div>
                            )}


                        </div>

                        <div className="flex items-center gap-6 flex-wrap">
                            {hasParent && (
                                <div className="flex items-center">
                                    <div className={`w-32 text-xs ${theme.label} mr-2`}>Grid Display Type</div>
                                    <select
                                        value={unitData.EmGridViewDisplayType || 1}
                                        onChange={(e) => handleUnitPropertyChange('EmGridViewDisplayType', parseInt(e.target.value) || 1)}
                                        className={`h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none w-[260px]`}
                                    >
                                        {gridDisplayTypeOptions.map((item: any) => (
                                            <option key={item.Value} value={item.Value}>
                                                {item.Display}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                            )}

                            {hasParent && (unitData.EmGridViewDisplayType ?? 1) === 1 && (
                                <div className="flex items-center">
                                    <div className={`text-xs ${theme.label} w-[250px] mr-2`}>Edit Row On Popup When Width Less Than</div>
                                    <input
                                        type="number"
                                        value={unitData.MaxRowCount || 800}
                                        onChange={(e) => handleUnitPropertyChange('MaxRowCount', parseInt(e.target.value) || 800)}
                                        className={`h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none w-[138px]`}
                                    />
                                    <div className={`ml-2 text-xs ${theme.label}`}>px</div>
                                </div>
                            )}

                            <div className="flex items-center gap-6 flex-wrap justify-end pl-10">
                                {hasParent && (
                                    <label className="flex items-center gap-2">
                                        <input
                                            type="checkbox"
                                            checked={unitData.IsReadOnly || false}
                                            onChange={(e) => handleUnitPropertyChange('IsReadOnly', e.target.checked)}
                                            className="w-4 h-4"
                                        />
                                        <span className={`text-xs ${theme.label}`}>Is Read-Only</span>
                                    </label>
                                )}

                                {/* Is Matrix Grid: available for RegularGrid (1) and PivotEditGrid (3) so a grid can be both
                                    a Cartesian matrix (server GenerateMatrix) and a pivot-edit view at the same time. */}
                                {hasParent && ((unitData.EmGridViewDisplayType ?? 1) === 1 || unitData.EmGridViewDisplayType === 3) && !unitData.IsVirtualUnit && (
                                    <label className="flex items-center gap-2">
                                        <input
                                            type="checkbox"
                                            checked={unitData.IsMatrixUnit || false}
                                            onChange={(e) => handleUnitPropertyChange('IsMatrixUnit', e.target.checked)}
                                            className="w-4 h-4"
                                        />
                                        <span className={`text-xs ${theme.label}`}>Is Matrix Grid</span>
                                    </label>
                                )}

                                {hasParent && (unitData.EmGridViewDisplayType ?? 1) === 1 && (
                                    <label className="flex items-center gap-2">
                                        <input
                                            type="checkbox"
                                            checked={unitData.IsUsedForLoadingAvailableSource || false}
                                            onChange={(e) =>
                                                handleUnitPropertyChange('IsUsedForLoadingAvailableSource', e.target.checked)
                                            }
                                            className="w-4 h-4"
                                        />
                                        <span className={`text-xs ${theme.label}`}>Is Used For Loading Available Source</span>
                                    </label>
                                )}



                                {unitData.IsVirtualUnit && (
                                    <label className="flex items-center gap-2 opacity-80">
                                        <input
                                            type="checkbox"
                                            checked={unitData.IsVirtualUnit || false}
                                            disabled
                                            className="w-4 h-4"
                                        />
                                        <span className={`text-xs ${theme.label}`}>Is Virtual Unit</span>
                                    </label>
                                )}
                            </div>
                        </div>

                        {unitData.EmGridViewDisplayType === 2 && (
                            <div className="flex items-center gap-6 flex-wrap">
                                <div className="flex items-center">
                                    <div className={`w-32 text-xs ${theme.label} mr-2`}>Tree Grid Key Field</div>
                                    <select
                                        value={unitData.TreeViewKeyField || ''}
                                        onChange={(e) => handleUnitPropertyChange('TreeViewKeyField', e.target.value)}
                                        className={`h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none w-[260px]`}
                                    >
                                        <option value="">None</option>
                                        {unitData.AppTransactionFieldList?.map((field: any) => (
                                            <option key={field.DataBaseFieldName} value={field.DataBaseFieldName}>
                                                {field.DataBaseFieldName}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                                <div className="flex items-center">
                                    <div className={`w-32 text-xs ${theme.label} mr-2`}>Tree Grid Parent Key Field</div>
                                    <select
                                        value={unitData.TreeViewParentKeyField || ''}
                                        onChange={(e) => handleUnitPropertyChange('TreeViewParentKeyField', e.target.value)}
                                        className={`h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none w-[260px]`}
                                    >
                                        <option value="">None</option>
                                        {unitData.AppTransactionFieldList?.map((field: any) => (
                                            <option key={field.DataBaseFieldName} value={field.DataBaseFieldName}>
                                                {field.DataBaseFieldName}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                            </div>
                        )}
                    </div>
                </div>

                {/* Fields Grid */}
                <div className="flex-1 overflow-hidden flex flex-col">
                    <div className={`px-4 py-2 border-b ${theme.mainContentSection} flex items-center justify-between`}>
                        <div className="flex items-center gap-2">
                            <div className={`text-sm font-semibold ${theme.title}`}>Unit Fields</div>
                            <FlexGridAddOn
                                grid={fieldGridControl}
                                gridRef={flexGridRef}
                                defaultFrozenColumns={3}
                                storageKey="txnUnitFieldGrid"
                                title="Freeze / Show / Hide columns"
                            />
                        </div>
                        <div className="flex items-center gap-2 flex-wrap">
                            {/* Alter Table / Edit Query Button */}
                            {isPhysicalModelTableCreated && unitData?.DataBaseTableName && !isReadOnly && onEditTable && (
                                <button
                                    onClick={() => onEditTable(unitData)}
                                    className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                    title="Alter Table"
                                >
                                    <i className="fa fa-database mr-1"></i>
                                    Alter Table
                                </button>
                            )}
                            {isReadOnly && onEditTable && (
                                <button
                                    onClick={() => onEditTable(unitData)}
                                    className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                    title="Edit Query"
                                >
                                    <i className="fa fa-database mr-1"></i>
                                    Edit Query
                                </button>
                            )}

                            {/* Add Table Existing Field */}
                            {isPhysicalModelTableCreated && unitData?.DataBaseTableName && (
                                <button
                                    onClick={handleAddExistingTableField}
                                    className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                    title="Add Table Existing Field"
                                >
                                    <i className="fa fa-plus mr-1"></i>
                                    <i className="fa fa-database mr-1"></i>
                                    Add Table Existing Field
                                </button>
                            )}

                            {/* Add Table Extend Field */}
                            {isPhysicalModelTableCreated && unitData?.DataBaseTableName && (
                                <button
                                    onClick={handleAddTableExtendField}
                                    className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                    title="Add Table Extend Field"
                                >
                                    <i className="fa fa-plus mr-1"></i>
                                    Add Table Extend Field
                                </button>
                            )}

                            {/* Add Temp Field */}
                            <button
                                onClick={handleAddTempField}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                title="Add Temp Field"
                            >
                                <i className="fa fa-plus mr-1"></i>
                                Add Temp Field
                            </button>

                            {/* Remove Field */}
                            <button
                                onClick={handleRemoveField}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                title="Remove Field"
                            >
                                <i className="fa fa-trash mr-1"></i>
                                Remove Field
                            </button>

                            {/* Remove All Fields not in Table/Query */}
                            {isPhysicalModelTableCreated && unitData?.DataBaseTableName && (
                                <button
                                    onClick={handleRemoveFieldsNotInTableOrQuery}
                                    className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                    title="Remove All Fields not in Table"
                                >
                                    <i className="fa fa-trash mr-1"></i>
                                    Remove All Fields not in Table
                                </button>
                            )}
                            {isReadOnly && (
                                <button
                                    onClick={handleRemoveFieldsNotInTableOrQuery}
                                    className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                                    title="Remove All Fields not in Query"
                                >
                                    <i className="fa fa-trash mr-1"></i>
                                    Remove All Fields not in Query
                                </button>
                            )}

                        </div>
                    </div>
                    <div className="flex-1 overflow-hidden">
                        {fieldCollectionView ? (
                            <FlexGrid
                                ref={flexGridRef}
                                itemsSource={fieldCollectionView}
                                isReadOnly={false}
                                selectionMode="ListBox"
                                style={{ height: '100%', border: 'none' }}
                                cellEditEnded={handleCellEditEnded}
                                initialized={(g: any) => setFieldGridControl(g)}
                            >
                                <FlexGridFilter />
                                <FlexGridCellTemplate
                                    cellType="RowHeader"
                                    template={(cell: any) => (
                                        <div style={{ padding: '0px 5px' }}>
                                            {cell.grid?.selectedRows?.indexOf(cell.row) >= 0 && (
                                                <i className="fa fa-check" style={{ color: '#808080' }} />
                                            )}
                                        </div>
                                    )}
                                />
                                {/* Sort */}
                                <FlexGridColumn binding="SortOrder" header="Sort" width={60} dataType="Number" />
                                {/* Name / DbName / Json Property Name */}
                                <FlexGridColumn binding="DisplayName" header="Name" width={200} />
                                <FlexGridColumn
                                    binding="DataBaseFieldName"
                                    header="DbName"
                                    width={200}
                                    isReadOnly={true}
                                    visible={Boolean(isPhysicalModelTableCreated)}
                                />
                                <FlexGridColumn
                                    binding="DataBaseFieldName"
                                    header="Json Property Name"
                                    width={200}
                                    isReadOnly={false}
                                    visible={Boolean(transactionData?.OtherOptions?.IsApiIntegrationTransaction)}
                                />
                                {/* Primary key / object key */}
                                <FlexGridColumn
                                    binding="IsPrimaryKey"
                                    header="IsPrimaryKey"
                                    width={100}
                                    dataType="Boolean"
                                    visible={Boolean(isPhysicalModelTableCreated)}
                                />
                                <FlexGridColumn
                                    binding="IsPrimaryKey"
                                    header="IsObjectKey"
                                    width={100}
                                    dataType="Boolean"
                                    visible={Boolean(!isPhysicalModelTableCreated)}
                                />
                                {/* Parent PK link (show for all units except root master unit) */}
                                <FlexGridColumn
                                    binding="ParentPKFieldGuid"
                                    header="Link To Parent Primary Key"
                                    width={180}
                                    isRequired={false}
                                    dataMap={foreignKeyDataMap || undefined}
                                    dataType="String"
                                    visible={Boolean(isPhysicalModelTableCreated && !isRootMasterUnit)}
                                />
                                <FlexGridColumn
                                    binding="IsLinkToParentPrimaryKey"
                                    header="Is Link To Parent Primary Key"
                                    width={180}
                                    dataType="Boolean"
                                    isReadOnly={true}
                                    visible={false}
                                />
                                {/* Is Row Sequence Key (maps to IsConvertToUpperCase in Angular) */}
                                <FlexGridColumn
                                    binding="IsConvertToUpperCase"
                                    header="Is Row Sequence Key"
                                    width={200}
                                    dataType="Boolean"
                                />
                                {/* Mapping To Available Source Unit */}
                                <FlexGridColumn
                                    binding="MappingToAvailableSourceUnitPopupOpener"
                                    header="Mapping To Available Source Unit"
                                    isReadOnly={true}
                                    isContentHtml={true}
                                    width={250}
                                    visible={Boolean(unitData.EmGridViewDisplayType === 5 || unitData.EmGridViewDisplayType === 6)}
                                />
                                <FlexGridColumn
                                    binding="MappingToAvailableSourceUnitTransactionFieldId"
                                    header="Mapping To Available Source Unit Field"
                                    isReadOnly={false}
                                    visible={false}
                                    width={250}
                                />
                                {/* Data Store Mode */}
                                <FlexGridColumn
                                    binding="FieldStoreMode"
                                    header="Data Store Mode"
                                    width={150}
                                    isReadOnly={true}
                                    isRequired={false}
                                    dataMap={fieldStoreModeDataMap}
                                    dataType="Number"
                                />
                                {/* DataType / Nbdecimal / UIDisplayType */}
                                <FlexGridColumn
                                    binding="DataType"
                                    header="DataType"
                                    isRequired={false}
                                    dataMap={dataTypeDataMap}
                                    dataType="Number"
                                />
                                <FlexGridColumn binding="Nbdecimal" header="Nbdecimal" dataType="Number" />
                                <FlexGridColumn
                                    binding="ControlType"
                                    header="UIDisplayType"
                                    width={100}
                                    isRequired={false}
                                    dataMap={controlTypeDataMap}
                                    dataType="Number"
                                />
                                {/* Datasource-related columns */}
                                <FlexGridColumn
                                    binding="EntityIdSelector"
                                    header="Datasource"
                                    isReadOnly={true}
                                    width={200}
                                    isContentHtml={true}
                                >
                                    <FlexGridCellTemplate
                                        cellType="Cell"
                                        template={(cell: any) => {
                                            const item = cell.item;
                                            if (!item) return null;
                                            const ctl = Number(item.ControlType);
                                            const allowedTypes: number[] = [
                                                controlTypeIdsForDatasource.DDL,
                                                controlTypeIdsForDatasource.AutoComplete!,
                                                controlTypeIdsForDatasource.SearchAbleDDL!,
                                                controlTypeIdsForDatasource.RadioButtons!,
                                                controlTypeIdsForDatasource.Progress!
                                            ].filter((v) => typeof v === 'number') as number[];

                                            const isDatasourceCapable = allowedTypes.includes(ctl);
                                            if (!isDatasourceCapable) {
                                                // Types that do not allow datasource selector: show nothing
                                                return <span className="text-xs text-gray-500"> </span>;
                                            }

                                            const text = item.EntityIdSelector || '';
                                            const hasEntity = Boolean(item.EntityId);
                                            return (
                                                <div className="flex items-center gap-1">
                                                    <button
                                                        type="button"
                                                        className={`px-1 py-0.5 text-[10px] rounded ${theme.button_default}`}
                                                        onClick={() => handleOpenDatasourceSelector(item)}
                                                        title="Select Datasource"
                                                    >
                                                        <i className="fa fa-plus" />
                                                    </button>
                                                    {hasEntity && (
                                                        <button
                                                            type="button"
                                                            className={`px-1 py-0.5 text-[10px] rounded ${theme.button_default}`}
                                                            onClick={() => handleOpenEntityDataPreview(item)}
                                                            title="Entity Data Preview"
                                                        >
                                                            <i className="fa fa-database" />
                                                        </button>
                                                    )}
                                                    <span className="text-xs truncate" title={text}>
                                                        {text}
                                                    </span>
                                                </div>
                                            );
                                        }}
                                    />
                                </FlexGridColumn>
                                <FlexGridColumn
                                    binding="DdlparentLevelIdSelector"
                                    header="Parent Data Source Mapping"
                                    isReadOnly={true}
                                    width={250}
                                    isContentHtml={true}
                                    visible={Boolean(!unitData.IsUsedForLoadingAvailableSource)}
                                >
                                    <FlexGridCellTemplate
                                        cellType="Cell"
                                        template={(cell: any) => {
                                            const item = cell.item;
                                            if (!item) return null;
                                            const pid = item.DdlparentLevelId != null && item.DdlparentLevelId !== '' ? Number(item.DdlparentLevelId) : null;
                                            const parentFieldFromParent =
                                                pid != null
                                                    ? parentUnit?.AppTransactionFieldList?.find((f: any) => Number(f.Id) === pid) || null
                                                    : null;
                                            const parentFieldFromCurrent =
                                                pid != null
                                                    ? unitData?.AppTransactionFieldList?.find((f: any) => Number(f.Id) === pid) || null
                                                    : null;
                                            const parentField = parentFieldFromParent || parentFieldFromCurrent;
                                            const tableName = parentFieldFromParent ? parentUnit?.DataBaseTableName : unitData?.DataBaseTableName;
                                            const fieldName = parentField?.DataBaseFieldName ?? parentField?.DisplayName ?? '';
                                            const fallbackText = pid != null && tableName && fieldName ? `Cascading From: ${tableName} . ${fieldName}` : '';
                                            const displayText = item.DdlparentLevelIdSelector || fallbackText || '';
                                            // Angular: column always present when !IsUsedForLoadingAvailableSource;
                                            // button is only meaningful for DDL/AutoComplete/SearchAbleDDL, but it still shows even without parent mapping yet.
                                            const ctl = Number(item.ControlType);
                                            const allowed: number[] = [
                                                controlTypeIdsForDatasource.DDL,
                                                controlTypeIdsForDatasource.AutoComplete!,
                                                controlTypeIdsForDatasource.SearchAbleDDL!,
                                            ].filter((v) => typeof v === 'number') as number[];
                                            if (!allowed.includes(ctl)) {
                                                return <span className="text-xs text-gray-500"> </span>;
                                            }
                                            return (
                                                <div className="flex items-center gap-1">
                                                    <button
                                                        type="button"
                                                        className={`px-1 py-0.5 text-[10px] rounded ${theme.button_default}`}
                                                        onClick={() => handleOpenParentDataSourceMapping(item)}
                                                        title="Parent Data Source Mapping"
                                                    >
                                                        <i className="fa fa-plus" />
                                                    </button>
                                                    <span className="text-xs truncate" title={displayText}>
                                                        {displayText}
                                                    </span>
                                                </div>
                                            );
                                        }}
                                    />
                                </FlexGridColumn>
                                <FlexGridColumn
                                    binding="AvailableSourceFilterByFieldSelector"
                                    header="Available Source Filter By"
                                    isReadOnly={true}
                                    width={250}
                                    isContentHtml={true}
                                    visible={Boolean(unitData.IsUsedForLoadingAvailableSource)}
                                />
                                {/* Code Auto Generate */}
                                <FlexGridColumn
                                    binding="CodeAutoGenerationSetting"
                                    header="Code Auto Generate"
                                    isReadOnly={true}
                                    width={150}
                                    isContentHtml={true}
                                    visible={Boolean(isTopLevelUnit)}
                                />
                                <FlexGridColumn
                                    binding="AutoIncrementLastId"
                                    header="AutoIncrementLastId"
                                    isReadOnly={false}
                                    width={150}
                                    visible={false}
                                />
                                {/* Data Retrieve Mapping */}
                                <FlexGridColumn
                                    binding="DataRetrieveMappingEditor"
                                    header="Data Retrieve Mapping"
                                    isReadOnly={true}
                                    width={150}
                                    isContentHtml={true}
                                    visible={false}
                                />
                                {/* Current user filter */}
                                <FlexGridColumn
                                    binding="IsFilterByCurrentUser"
                                    header="Is Filter By Current User"
                                    isReadOnly={false}
                                    width={150}
                                    dataType="Boolean"
                                />
                                {/* Grouping / available entity source */}
                                <FlexGridColumn binding="IsGroupBy" header="IsGroupBy" isReadOnly={false} width={90} dataType="Boolean" />
                                <FlexGridColumn
                                    binding="GroupByLevel"
                                    header="Group Level"
                                    isReadOnly={false}
                                    width={100}
                                    dataType="Number"
                                />
                                <FlexGridColumn
                                    binding="IsGridUseAvailableEntitySource"
                                    header="Is Use Available Entity Source"
                                    isReadOnly={false}
                                    width={160}
                                    dataType="Boolean"
                                />
                                {/* Internal code / validation */}
                                <FlexGridColumn
                                    binding="InternalCode"
                                    header="InternalCode"
                                    isReadOnly={false}
                                    visible={false}
                                />
                                <FlexGridColumn
                                    binding="NeedValidator"
                                    header="NeedValidator"
                                    dataType="Boolean"
                                    isReadOnly={false}
                                />
                                <FlexGridColumn
                                    binding="ValidatorType"
                                    header="ValidatorType"
                                    isReadOnly={true}
                                    dataType="Number"
                                    width={120}
                                />
                                {/* Empty / length */}
                                <FlexGridColumn binding="IsAllowEmpty" header="IsAllowEmpty" dataType="Boolean" />
                                <FlexGridColumn
                                    binding="MaxCharLegnth"
                                    header="Legnth"
                                    isReadOnly={false}
                                    dataType="Number"
                                />
                                {/* Uniqueness / logging / logical display */}
                                <FlexGridColumn
                                    binding="IsUnique"
                                    header="Is Data Transfer Unique Logical Key"
                                    isReadOnly={false}
                                    dataType="Boolean"
                                    width={140}
                                />
                                <FlexGridColumn
                                    binding="IsLogicalDisplay"
                                    header="Is Logical Display"
                                    isReadOnly={false}
                                    dataType="Boolean"
                                    width={140}
                                />
                                <FlexGridColumn
                                    binding="IsNeedLog"
                                    header="IsNeedLog"
                                    isReadOnly={false}
                                    dataType="Boolean"
                                />
                                <FlexGridColumn
                                    binding="IsChangeTrigerNotification"
                                    header="IsChangeTrigerNotification"
                                    isReadOnly={false}
                                    dataType="Boolean"
                                    width={160}
                                />
                                {/* Tooltip / security */}
                                <FlexGridColumn binding="ToolTip" header="ToolTip" isReadOnly={false} />
                                <FlexGridColumn
                                    binding="FieldSecurityOpener"
                                    header="Security"
                                    isReadOnly={true}
                                    isContentHtml={true}
                                    width={100}
                                >
                                    <FlexGridCellTemplate
                                        cellType="Cell"
                                        template={(cell: any) => {
                                            const item = cell.item;
                                            if (!item) return null;
                                            const isDisabled = !item.Id;
                                            return (
                                                <div className="flex items-center justify-center">
                                                    <button
                                                        type="button"
                                                        disabled={isDisabled}
                                                        className={`px-2 py-0.5 text-[10px] rounded ${theme.button_default} ${isDisabled ? 'opacity-50 cursor-not-allowed' : ''}`}
                                                        title={isDisabled ? 'Save transaction first' : 'Edit Field Security'}
                                                        onClick={() => handleOpenTransactionFieldSecurity(item)}
                                                    >
                                                        <i className="fa-solid fa-shield-halved" aria-hidden />
                                                    </button>
                                                </div>
                                            );
                                        }}
                                    />
                                </FlexGridColumn>
                                {/* Matrix FK — click to assign the matrix foreign-key field (Angular OpenMatrixFkPopup) */}
                                <FlexGridColumn
                                    binding="MatrixForeignKeyFieldIdSelector"
                                    header="Matrix ForeignKey Field"
                                    isReadOnly={true}
                                    width={180}
                                    isContentHtml={true}
                                    visible={Boolean(unitData.IsMatrixUnit || unitData.EmGridViewDisplayType === 3 || unitData.EmGridViewDisplayType === 4)}
                                >
                                    <FlexGridCellTemplate
                                        cellType="Cell"
                                        template={(cell: any) => {
                                            const item = cell.item;
                                            if (!item) return null;
                                            const ctl = Number(item.ControlType);
                                            const allowed: number[] = [
                                                controlTypeIdsForDatasource.DDL,
                                                controlTypeIdsForDatasource.AutoComplete!,
                                                controlTypeIdsForDatasource.SearchAbleDDL!,
                                            ].filter((v) => typeof v === 'number') as number[];
                                            if (!allowed.includes(ctl)) {
                                                return <span className="text-xs text-gray-500"> </span>;
                                            }
                                            const fkName = item.MatrixForeignKeyFieldId
                                                ? getDdlColumnNameById(item.MatrixForeignKeyFieldId)
                                                : '';
                                            return (
                                                <div className="flex items-center gap-1">
                                                    <button
                                                        type="button"
                                                        className={`px-1 py-0.5 text-[10px] rounded ${theme.button_default}`}
                                                        onClick={() => handleOpenMatrixFkPopup(item)}
                                                        title="Matrix Grid Foreignkey"
                                                    >
                                                        <i className="fa-solid fa-link" />
                                                    </button>
                                                    <span className="text-xs truncate" title={fkName}>
                                                        {fkName}
                                                    </span>
                                                </div>
                                            );
                                        }}
                                    />
                                </FlexGridColumn>
                                {/* Pivot columns */}
                                <FlexGridColumn
                                    binding="IsPivotRow"
                                    header="Is Pivot Row"
                                    isReadOnly={false}
                                    dataType="Boolean"
                                    visible={Boolean(unitData.EmGridViewDisplayType === 3 || unitData.EmGridViewDisplayType === 4)}
                                />
                                <FlexGridColumn
                                    binding="IsPivotColumn"
                                    header="Is Pivot Column"
                                    isReadOnly={false}
                                    dataType="Boolean"
                                    visible={Boolean(unitData.EmGridViewDisplayType === 3 || unitData.EmGridViewDisplayType === 4)}
                                />
                                <FlexGridColumn
                                    binding="IsPivotValue"
                                    header="Is Pivot Value"
                                    isReadOnly={false}
                                    dataType="Boolean"
                                    visible={Boolean(unitData.EmGridViewDisplayType === 3 || unitData.EmGridViewDisplayType === 4)}
                                />
                                <FlexGridColumn
                                    binding="PivotAggregationType"
                                    header="Pivot Aggregation Type"
                                    isReadOnly={false}
                                    isRequired={false}
                                    dataType="Number"
                                    dataMap={aggregationTypeDataMap}
                                    width={180}
                                    visible={Boolean(unitData.EmGridViewDisplayType === 3 || unitData.EmGridViewDisplayType === 4)}
                                />
                                {/* System token / trigger actions / Google address mapping */}
                                <FlexGridColumn
                                    binding="MappingEmSystemTokenField"
                                    header="MappingEmSystemTokenField"
                                    isReadOnly={false}
                                    isRequired={false}
                                    dataMap={systemTokenFieldDataMap}
                                    dataType="Number"
                                    width={200}
                                />
                                <FlexGridColumn
                                    binding="ControlTypeParam1"
                                    header="On UI Change Trigger To Command"
                                    isReadOnly={false}
                                    width={300}
                                    isRequired={false}
                                    dataMap={commandActionDataMap}
                                    dataType="Number"
                                />
                                <FlexGridColumn
                                    binding="ControlTypeParam2"
                                    header="Save Map Location Data To Field DbName"
                                    isReadOnly={false}
                                    width={250}
                                />
                                <FlexGridColumn
                                    binding="GoogleAddressFieldsMapping"
                                    header="Google Address Fields Mapping"
                                    isReadOnly={true}
                                    width={250}
                                    isContentHtml={true}
                                />
                                {/* Owner exclusive / readonly / visible / default / width */}
                                <FlexGridColumn
                                    binding="IsFieldExclusiveForOwner"
                                    header="IsFieldExclusiveForOwner"
                                    isReadOnly={false}
                                    dataType="Boolean"
                                    width={200}
                                />
                                <FlexGridColumn binding="IsReadonly" header="IsReadonly" dataType="Boolean" />
                                <FlexGridColumn binding="IsVisible" header="IsVisible" dataType="Boolean" />
                                <FlexGridColumn binding="DefaultValue" header="DefaultValue" width={150} />
                                <FlexGridColumn binding="DisplayWidth" header="Width" dataType="Number" />
                                {/* Spacer */}
                                <FlexGridColumn binding="" header="" isReadOnly={true} width={"*"} />
                            </FlexGrid>
                        ) : (
                            <div className={`text-center py-8 ${theme.label}`}>
                                No fields available
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {/* Table Column Selector Dialog */}
            {tableColumnSelectorDialog.isOpen && unitData && (
                <TableColumnSelectorDialog
                    isOpen={tableColumnSelectorDialog.isOpen}
                    tableName={unitData.DataBaseTableName || ''}
                    schemaOwner={unitData.SchemaOwner || null}
                    dataSourceRegisterId={transactionData?.DataSourceFrom || dataSourceRegisterId}
                    lockedColumnNames={(unitData.AppTransactionFieldList || [])
                        .filter((f: any) => f.DataBaseFieldName && !f.IsTempVariable)
                        .map((f: any) => f.DataBaseFieldName)}
                    onClose={() => setTableColumnSelectorDialog({ isOpen: false })}
                    onSelect={handleTableColumnsSelected}
                />
            )}

            {/* Unit-scoped Data Load Settings dialog */}
            {isDataLoadDialogOpen && transactionData?.Id && unitData?.Id && (
                <TransactionUnitDataLoadDialog
                    isOpen={isDataLoadDialogOpen}
                    transactionId={Number(transactionData.Id)}
                    transactionUnitId={Number(unitData.Id)}
                    transactionName={transactionData.TransactionName || null}
                    onClose={() => setIsDataLoadDialogOpen(false)}
                />
            )}

            {/* Unit Navigate To Search - Linked Search Management */}
            {linkedSearchManagementOpen && unitData?.Id && (
                <TransactionUnitLinkedSearchManagementDialog
                    isOpen={true}
                    transactionUnitId={Number(unitData.Id)}
                    unitDisplayName={unitData.UnitDisplayName || unitData.UnitName}
                    onClose={() => setLinkedSearchManagementOpen(false)}
                />
            )}

            {/* Link To Other Pages editor (Form / Search / System Page) */}
            {linkTargetEditor && unitData?.Id && (
                <TransactionUnitLinkTargetEditor
                    isOpen={true}
                    transactionUnitId={Number(unitData.Id)}
                    unitDisplayName={unitData.UnitDisplayName || unitData.UnitName}
                    linkTargetUsageType={linkTargetEditor.usageType}
                    title={linkTargetEditor.title}
                    onClose={() => setLinkTargetEditor(null)}
                />
            )}

            {/* Delete Flow editor */}
            {deleteFlowEditorOpen && unitData?.Id && (
                <TransactionUnitDeleteFlowEditor
                    isOpen={true}
                    transactionUnitId={Number(unitData.Id)}
                    dataSourceRegisterId={transactionData?.DataSourceFrom ?? dataSourceRegisterId}
                    unitDisplayName={unitData.UnitDisplayName || unitData.UnitName}
                    applicationId={applicationId}
                    onClose={() => setDeleteFlowEditorOpen(false)}
                />
            )}

            {/* More Unit Options popup */}
            {moreUnitOptionsOpen && unitData && (
                <UnitAdvancedOptionsPopup
                    isOpen={true}
                    unitDisplayName={unitData.UnitDisplayName || unitData.UnitName}
                    unitData={unitData}
                    transactionOrganizedType={transactionData?.TransactionOrganizedType}
                    hasParent={hasParent}
                    onClose={() => setMoreUnitOptionsOpen(false)}
                    onPropertyChange={handleUnitPropertyChange}
                />
            )}

            {/* Datasource Selector popup for unit field - Standalone Datasource slice */}
            {datasourceSelectionField && datasourceSelectorState && (
                <div className="fixed inset-0 z-[10000] flex items-center justify-center bg-black/50">
                    <div
                        className={`max-w-full max-h-[80vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}
                        style={{
                            width:
                                datasourceSelectorState.activeTab === 3
                                    ? '600px'
                                    : datasourceSelectorState.isEnableDependentColumns
                                        ? '900px'
                                        : '420px',
                            maxWidth: '95vw',
                            height: datasourceSelectorState.activeTab === 3 ? '600px' : undefined,
                            maxHeight: datasourceSelectorState.activeTab === 3 ? '90vh' : undefined
                        }}
                    >
                        <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`}>
                            <span className="text-md font-semibold">Datasource Selector</span>
                            <button
                                type="button"
                                onClick={handleDatasourcePopupClose}
                                className={`p-1 rounded ${theme.button_default}`}
                                aria-label="Close"
                            >
                                <i className="fa-solid fa-times" aria-hidden />
                            </button>
                        </div>
                        <div className="flex-1 min-h-0 overflow-auto p-4 space-y-4 text-xs">
                            <div>
                                <div className={theme.label}>Datasource Type</div>
                                <select
                                    className={`w-full mt-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                    value={datasourceSelectorState.activeTab}
                                    onChange={(e) => {
                                        const tab = Number(e.target.value) as 1 | 2 | 3;
                                        setDatasourceSelectorState((prev) => {
                                            if (!prev) return prev;
                                            const base = { ...prev, activeTab: tab };
                                            if (tab === 1) {
                                                return {
                                                    ...base,
                                                    selectedForeignUnitId: null,
                                                    selectedForeignUnitDisplay: '',
                                                    foreignUnitFieldDataMap: new DataMap([], 'DataBaseFieldName', 'Display'),
                                                    selectionQueryText: '',
                                                    conditionQueryText: '',
                                                    conditionParameterList: [],
                                                    conditionParameterCV: new CollectionView<any>([]),
                                                    parameterTransFieldDataMap: new DataMap([], 'Id', 'Display')
                                                };
                                            }
                                            if (tab === 2) {
                                                return {
                                                    ...base,
                                                    selectedEntityId: null,
                                                    selectedEntityDisplay: '',
                                                    selectionQueryText: '',
                                                    conditionQueryText: '',
                                                    conditionParameterList: [],
                                                    conditionParameterCV: new CollectionView<any>([]),
                                                    parameterTransFieldDataMap: new DataMap([], 'Id', 'Display')
                                                };
                                            }
                                            // tab === 3
                                            return {
                                                ...base,
                                                selectedEntityId: null,
                                                selectedEntityDisplay: '',
                                                selectedForeignUnitId: null,
                                                selectedForeignUnitDisplay: '',
                                                foreignUnitFieldDataMap: new DataMap([], 'DataBaseFieldName', 'Display')
                                            };
                                        });
                                    }}
                                >
                                    <option value={1}>Standalone Datasource</option>
                                    <option value={2}>Current Data Model Unit Datasource</option>
                                    <option value={3}>Query Datasource</option>
                                </select>
                            </div>

                            {datasourceSelectorState.activeTab === 1 && (
                                <>
                                    <div>
                                        <div className={theme.label}>DDL Entity</div>
                                        <select
                                            className={`w-full mt-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                            value={datasourceSelectorState.selectedEntityId ?? ''}
                                            onChange={(e) => {
                                                const val = e.target.value ? Number(e.target.value) : null;
                                                const found = datasourceSelectorState.entityList.find(
                                                    (x) => x.Id === val
                                                );
                                                const nextDisplay =
                                                    found?.EntityCode ||
                                                    found?.Name ||
                                                    (val != null ? String(val) : '');
                                                setDatasourceSelectorState({
                                                    ...datasourceSelectorState,
                                                    selectedEntityId: val,
                                                    selectedEntityDisplay: nextDisplay
                                                });

                                                // Load entity db columns datamap for dependent mapping grid
                                                (async () => {
                                                    if (!val) {
                                                        setDatasourceSelectorState((prev) =>
                                                            prev
                                                                ? { ...prev, entityDbColumnDataMap: new DataMap([], 'Name', 'Name') }
                                                                : prev
                                                        );
                                                        return;
                                                    }
                                                    try {
                                                        dispatch(setIsBusy());
                                                        const entityDto = await adminSvc.retrieveOneAppEntityInfoExDto(String(val), true);
                                                        const tableName = entityDto?.TableName ?? null;
                                                        const schemaOwner = entityDto?.SchemaOwner ?? null;
                                                        const dsId =
                                                            entityDto?.DataSourceFrom ??
                                                            transactionData?.DataSourceFrom ??
                                                            dataSourceRegisterId ??
                                                            null;
                                                        if (tableName && dsId) {
                                                            const tableSchema = await schemaMetadataService.getOneDatabaseTableSchema(
                                                                tableName,
                                                                dsId,
                                                                schemaOwner
                                                            );
                                                            const cols: Array<{ Name: string }> = Array.isArray(tableSchema?.Columns)
                                                                ? tableSchema.Columns.map((c: any) => ({ Name: String(c.Name) }))
                                                                : [];
                                                            const dm = new DataMap(cols, 'Name', 'Name');
                                                            setDatasourceSelectorState((prev) => (prev ? { ...prev, entityDbColumnDataMap: dm } : prev));
                                                        } else {
                                                            setDatasourceSelectorState((prev) =>
                                                                prev
                                                                    ? { ...prev, entityDbColumnDataMap: new DataMap([], 'Name', 'Name') }
                                                                    : prev
                                                            );
                                                        }
                                                    } catch (err: any) {
                                                        showError(err?.message || 'Failed to load entity columns');
                                                        setDatasourceSelectorState((prev) =>
                                                            prev
                                                                ? { ...prev, entityDbColumnDataMap: new DataMap([], 'Name', 'Name') }
                                                                : prev
                                                        );
                                                    } finally {
                                                        dispatch(setIsNotBusy());
                                                    }
                                                })();
                                            }}
                                        >
                                            <option value="">(None)</option>
                                            {datasourceSelectorState.entityList.map((e) => (
                                                <option key={e.Id} value={e.Id}>
                                                    {e.EntityCode || e.Name || e.Id}
                                                </option>
                                            ))}
                                        </select>
                                    </div>

                                    <div className="flex items-center gap-2">
                                        <span className={theme.label}>Enable Dependent Columns</span>
                                        <button
                                            type="button"
                                            className="px-2 py-1 text-xs"
                                            onClick={() =>
                                                setDatasourceSelectorState({
                                                    ...datasourceSelectorState,
                                                    isEnableDependentColumns:
                                                        !datasourceSelectorState.isEnableDependentColumns
                                                })
                                            }
                                        >
                                            <i
                                                className={`fa ${datasourceSelectorState.isEnableDependentColumns
                                                        ? 'fa-toggle-on text-green-600'
                                                        : 'fa-toggle-off text-gray-400'
                                                    } text-lg`}
                                            />
                                        </button>
                                    </div>

                                    {datasourceSelectorState.isEnableDependentColumns && (
                                        <div className="mt-2 border rounded p-2">
                                            <div className="flex items-center justify-between mb-2">
                                                <span className={`text-xs font-semibold ${theme.title}`}>
                                                    Dependent Field Mapping
                                                </span>
                                                <div className="flex gap-1">
                                                    <button
                                                        type="button"
                                                        className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                                        onClick={handleAddDependentMappingRow}
                                                    >
                                                        <i className="fa fa-plus mr-1" />
                                                        Add
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                                        onClick={handleRemoveDependentMappingRow}
                                                    >
                                                        <i className="fa fa-minus mr-1" />
                                                        Remove
                                                    </button>
                                                </div>
                                            </div>
                                            <div className="h-48">
                                                {datasourceSelectorState.dependentMappings ? (
                                                    <FlexGrid
                                                        itemsSource={datasourceSelectorState.dependentMappings}
                                                        headersVisibility="Column"
                                                        selectionMode="Row"
                                                        className="w-full h-full"
                                                    >
                                                        <FlexGridColumn
                                                            binding="DependentField"
                                                            header="Current Unit Field"
                                                            width="*"
                                                            isRequired={false}
                                                            dataMap={datasourceSelectorState.currentUnitFieldDataMap ?? undefined}
                                                            dataType="Number"
                                                        />
                                                        <FlexGridColumn
                                                            binding="LabelSourceColumn"
                                                            header="Field Label Map To DB Column"
                                                            width="*"
                                                            isRequired={false}
                                                            dataMap={datasourceSelectorState.entityDbColumnDataMap ?? undefined}
                                                            dataType="String"
                                                        />
                                                        <FlexGridColumn
                                                            binding="SourceColumn"
                                                            header="Field Value Map To DB Column"
                                                            width="*"
                                                            isRequired={false}
                                                            dataMap={datasourceSelectorState.entityDbColumnDataMap ?? undefined}
                                                            dataType="String"
                                                        />
                                                    </FlexGrid>
                                                ) : (
                                                    <div className={`text-xs ${theme.label}`}>No mappings.</div>
                                                )}
                                            </div>
                                        </div>
                                    )}
                                </>
                            )}

                            {datasourceSelectorState.activeTab === 2 && (
                                <>
                                    <div>
                                        <div className={theme.label}>Foreign Unit</div>
                                        <select
                                            className={`w-full mt-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                            value={datasourceSelectorState.selectedForeignUnitId ?? ''}
                                            onChange={async (e) => {
                                                const val = e.target.value ? Number(e.target.value) : null;
                                                const found = datasourceSelectorState.foreignUnitList.find((x) => x.Id === val);
                                                const nextDisplay = found?.Display ?? (val != null ? String(val) : '');
                                                setDatasourceSelectorState({
                                                    ...datasourceSelectorState,
                                                    selectedForeignUnitId: val,
                                                    selectedForeignUnitDisplay: nextDisplay,
                                                    foreignUnitFieldDataMap: new DataMap([], 'DataBaseFieldName', 'Display')
                                                });
                                                if (!val) return;
                                                try {
                                                    dispatch(setIsBusy());
                                                    let foreignUnit = findUnitByIdInTree(transactionData?.AppTransactionUnitList ?? [], val);
                                                    if (!foreignUnit) {
                                                        foreignUnit = await appTransactionService.retrieveOneAppTransactionUnitExDto(String(val));
                                                    }
                                                    const dm = foreignUnit ? buildForeignUnitFieldDataMap(foreignUnit) : new DataMap([], 'DataBaseFieldName', 'Display');
                                                    setDatasourceSelectorState((prev) =>
                                                        prev ? { ...prev, foreignUnitFieldDataMap: dm } : prev
                                                    );
                                                } catch (err: any) {
                                                    showError(err?.message || 'Failed to load foreign unit fields');
                                                    setDatasourceSelectorState((prev) =>
                                                        prev ? { ...prev, foreignUnitFieldDataMap: new DataMap([], 'DataBaseFieldName', 'Display') } : prev
                                                    );
                                                } finally {
                                                    dispatch(setIsNotBusy());
                                                }
                                            }}
                                        >
                                            <option value="">(None)</option>
                                            {datasourceSelectorState.foreignUnitList.map((u) => (
                                                <option key={u.Id} value={u.Id}>
                                                    {u.Display}
                                                </option>
                                            ))}
                                        </select>
                                        {datasourceSelectorState.foreignUnitList.length === 0 && (
                                            <div className={`text-xs mt-1 ${theme.label}`}>
                                                No other units at the same level in this transaction. Add a sibling unit first.
                                            </div>
                                        )}
                                    </div>

                                    <div className="flex items-center gap-2">
                                        <span className={theme.label}>Enable Dependent Columns</span>
                                        <button
                                            type="button"
                                            className="px-2 py-1 text-xs"
                                            onClick={() =>
                                                setDatasourceSelectorState({
                                                    ...datasourceSelectorState,
                                                    isEnableDependentColumns:
                                                        !datasourceSelectorState.isEnableDependentColumns
                                                })
                                            }
                                        >
                                            <i
                                                className={`fa ${datasourceSelectorState.isEnableDependentColumns
                                                        ? 'fa-toggle-on text-green-600'
                                                        : 'fa-toggle-off text-gray-400'
                                                    } text-lg`}
                                            />
                                        </button>
                                    </div>

                                    {datasourceSelectorState.isEnableDependentColumns && (
                                        <div className="mt-2 border rounded p-2">
                                            <div className="flex items-center justify-between mb-2">
                                                <span className={`text-xs font-semibold ${theme.title}`}>
                                                    Dependent Field Mapping
                                                </span>
                                                <div className="flex gap-1">
                                                    <button
                                                        type="button"
                                                        className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                                        onClick={handleAddDependentMappingRow}
                                                    >
                                                        <i className="fa fa-plus mr-1" />
                                                        Add
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                                        onClick={handleRemoveDependentMappingRow}
                                                    >
                                                        <i className="fa fa-minus mr-1" />
                                                        Remove
                                                    </button>
                                                </div>
                                            </div>
                                            <div className="h-48">
                                                {datasourceSelectorState.dependentMappings ? (
                                                    <FlexGrid
                                                        itemsSource={datasourceSelectorState.dependentMappings}
                                                        headersVisibility="Column"
                                                        selectionMode="Row"
                                                        className="w-full h-full"
                                                    >
                                                        <FlexGridColumn
                                                            binding="DependentField"
                                                            header="Current Unit Field"
                                                            width="*"
                                                            isRequired={false}
                                                            dataMap={datasourceSelectorState.currentUnitFieldDataMap ?? undefined}
                                                            dataType="Number"
                                                        />
                                                        <FlexGridColumn
                                                            binding="SourceColumn"
                                                            header="Equal To Source Unit Column"
                                                            width="*"
                                                            isRequired={false}
                                                            dataMap={datasourceSelectorState.foreignUnitFieldDataMap ?? undefined}
                                                            dataType="String"
                                                        />
                                                    </FlexGrid>
                                                ) : (
                                                    <div className={`text-xs ${theme.label}`}>No mappings.</div>
                                                )}
                                            </div>
                                        </div>
                                    )}
                                </>
                            )}

                            {datasourceSelectorState.activeTab === 3 && (
                                <>
                                    {/* Selection Query with Query Builder button on the right */}
                                    <div className="flex items-center justify-between">
                                        <div className={theme.label}>Selection Query</div>
                                        <button
                                            type="button"
                                            className={`px-2 py-1 text-xs rounded ${theme.button_default} flex items-center gap-1`}
                                            onClick={() => setIsQueryBuilderOpen(true)}
                                        >
                                            <i className="fa fa-cogs" />
                                            <span>Query Builder</span>
                                        </button>
                                    </div>
                                    <textarea
                                        className={`w-full mt-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                        style={{ minHeight: '140px' }}
                                        value={datasourceSelectorState.selectionQueryText}
                                        onChange={(e) =>
                                            setDatasourceSelectorState({
                                                ...datasourceSelectorState,
                                                selectionQueryText: e.target.value
                                            })
                                        }
                                    />

                                    {/* Bottom row: Condition Query (left) and Parameter List (right) */}
                                    <div className="mt-3 flex gap-4">
                                        <div className="flex-1 min-w-[45%] flex flex-col">
                                            <div className={theme.label}>Condition Query</div>
                                            <textarea
                                                className={`w-full mt-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                                style={{ flex: 1, minHeight: '140px' }}
                                                value={datasourceSelectorState.conditionQueryText}
                                                onChange={(e) =>
                                                    setDatasourceSelectorState({
                                                        ...datasourceSelectorState,
                                                        conditionQueryText: e.target.value
                                                    })
                                                }
                                            />
                                        </div>
                                        <div className="flex-1 min-w-[45%]">
                                            <div className="mt-0 border rounded p-2 h-full flex flex-col">
                                                <div className="flex items-center justify-between mb-2">
                                                    <span className={`text-xs font-semibold ${theme.title}`}>Parameter List</span>
                                                    <div className="flex gap-1">
                                                        <button
                                                            type="button"
                                                            className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                                            onClick={() => {
                                                                if (!datasourceSelectorState.conditionParameterCV) return;
                                                                setDatasourceSelectorState((prev) => {
                                                                    if (!prev) return prev;
                                                                    const list = [
                                                                        ...prev.conditionParameterList,
                                                                        {
                                                                            parameterName: `@p${prev.conditionParameterList.length}`,
                                                                            transactionFieldId: null
                                                                        }
                                                                    ];
                                                                    const cv = new CollectionView<any>(list);
                                                                    return {
                                                                        ...prev,
                                                                        conditionParameterList: list,
                                                                        conditionParameterCV: cv
                                                                    };
                                                                });
                                                            }}
                                                        >
                                                            <i className="fa fa-plus mr-1" />
                                                            Add
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                                            onClick={() => {
                                                                if (!datasourceSelectorState.conditionParameterCV) return;
                                                                setDatasourceSelectorState((prev) => {
                                                                    if (!prev) return prev;
                                                                    const list = [...prev.conditionParameterList];
                                                                    list.pop();
                                                                    const cv = new CollectionView<any>(list);
                                                                    return {
                                                                        ...prev,
                                                                        conditionParameterList: list,
                                                                        conditionParameterCV: cv
                                                                    };
                                                                });
                                                            }}
                                                        >
                                                            <i className="fa fa-minus mr-1" />
                                                            Remove
                                                        </button>
                                                    </div>
                                                </div>
                                                <div className="flex-1 min-h-0">
                                                    {datasourceSelectorState.conditionParameterCV ? (
                                                        <FlexGrid
                                                            itemsSource={datasourceSelectorState.conditionParameterCV}
                                                            headersVisibility="Column"
                                                            selectionMode="Row"
                                                            className="w-full h-full"
                                                        >
                                                            <FlexGridColumn binding="parameterName" header="Parameter" width={120} isReadOnly />
                                                            <FlexGridColumn
                                                                binding="transactionFieldId"
                                                                header="Data Model Field"
                                                                width="*"
                                                                isRequired={false}
                                                                dataMap={datasourceSelectorState.parameterTransFieldDataMap ?? undefined}
                                                                dataType="Number"
                                                            />
                                                        </FlexGrid>
                                                    ) : (
                                                        <div className={`text-xs ${theme.label}`}>No parameters.</div>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </>
                            )}
                        </div>
                        <div className={`flex justify-end gap-2 px-4 py-2 border-t ${theme.mainContentSection}`}>
                            <button
                                type="button"
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={handleDatasourcePopupClose}
                            >
                                Close
                            </button>
                            <button
                                type="button"
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={handleDatasourceSelected}
                            >
                                Ok
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Query Builder popup (MetaDataViewDesign, reusable) */}
            {isQueryBuilderOpen && (
                <MetaDataViewDesign
                    isOpen={true}
                    onClose={() => setIsQueryBuilderOpen(false)}
                    dataSourceRegisterId={transactionData?.DataSourceFrom || dataSourceRegisterId}
                    schemaOwner={null}
                    applicationId={applicationId ? Number(applicationId) : null}
                    isNewView={false}
                    isEmbeddedByOtherPage
                    initialQueryText={datasourceSelectorState?.selectionQueryText || ''}
                    onQueryBuilt={(queryText) => {
                        setDatasourceSelectorState((prev) =>
                            prev
                                ? {
                                    ...prev,
                                    selectionQueryText: queryText
                                }
                                : prev
                        );
                    }}
                />
            )}

            {/* Transaction Field Security popup (embedded SystemObjectSecurityEditor) */}
            <SystemObjectSecurityEditorPopup
                open={fieldSecurityPopup.open}
                onClose={() => setFieldSecurityPopup({ open: false, fieldId: null, display: '' })}
                securityObjId={fieldSecurityPopup.fieldId}
                securityObjType={2} // EmAppSecuritySysObjType.TransactionField
                param2={{ securityObjDisplay: fieldSecurityPopup.display }}
            />

            {/* Entity Data Preview popup (DIV modal, not a tab) */}
            {entityDataPreviewOpen && (
                <div className="fixed inset-0 z-[10001] flex items-center justify-center bg-black/50">
                    <div
                        className={`${theme.mainContentSection} rounded-md shadow-lg border flex flex-col overflow-hidden`}
                        style={{ width: '780px', height: '560px', maxWidth: '95vw', maxHeight: '90vh' }}
                    >
                        <div className="flex-1 min-h-0 overflow-hidden">
                            <EntityDataPreview
                                entityId={entityDataPreviewOpen.entityId}
                                title="Entity Data Preview"
                                asPopup
                                onClose={() => setEntityDataPreviewOpen(null)}
                                onOpenEditDataPopup={({ route, entityId, title }) => {
                                    const paramObj = { id: entityId };
                                    setEntityEditPopup({
                                        route,
                                        title,
                                        param: encodeURIComponent(JSON.stringify(paramObj))
                                    });
                                }}
                            />
                        </div>
                    </div>
                </div>
            )}

            {/* Parent Data Source Mapping popup */}
            {parentDataSourceMappingState.isOpen && (
                <div className="fixed inset-0 z-[10030] flex items-center justify-center bg-black/50">
                    <div
                        className={`${theme.mainContentSection} rounded-md shadow-lg border flex flex-col overflow-hidden`}
                        style={{ width: '480px', height: '330px', maxWidth: '95vw' }}
                    >
                        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
                            <div className={`text-sm font-semibold ${theme.title}`}>Parent Data Source Mapping</div>
                            <button
                                type="button"
                                onClick={() =>
                                    setParentDataSourceMappingState((prev) => ({
                                        ...prev,
                                        isOpen: false
                                    }))
                                }
                                className={`p-1 rounded ${theme.button_default}`}
                                aria-label="Close"
                            >
                                <i className="fa-solid fa-times" aria-hidden />
                            </button>
                        </div>
                        <div className="flex-1 min-h-0 overflow-auto p-3 text-xs space-y-2">
                            <div className="flex items-center">
                                <span className="w-32">Current Field</span>
                                <div className="flex-1 truncate" title={parentDataSourceMappingState.currentFieldName}>
                                    {parentDataSourceMappingState.currentFieldName}
                                </div>
                            </div>
                            <div className="flex items-center">
                                <span className="w-32">Parent Field</span>
                                <select
                                    className={`flex-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                    value={parentDataSourceMappingState.parentFieldId ?? ''}
                                    onChange={(e) => {
                                        const val = e.target.value ? Number(e.target.value) : null;
                                        setParentDataSourceMappingState((prev) => ({
                                            ...prev,
                                            parentFieldId: val
                                        }));
                                    }}
                                >
                                    <option value="">(None)</option>
                                    {parentDataSourceMappingState.parentFieldOptions.map((pf) => (
                                        <option key={pf.Id} value={pf.Id}>
                                            {pf.Display}
                                        </option>
                                    ))}
                                </select>
                            </div>
                            <div className="flex items-center">
                                <span className="w-32">Relation Table</span>
                                {/** Use schemaOwner+table as option value so duplicate table names across schemas are handled correctly. */}
                                <select
                                    className={`flex-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                    value={
                                        parentDataSourceMappingState.relationTable
                                            ? `${parentDataSourceMappingState.relationTableSchemaOwner || ''}::${parentDataSourceMappingState.relationTable}`
                                            : ''
                                    }
                                    onChange={async (e) => {
                                        const raw = e.target.value;
                                        const [schemaOwnerPart, tableNamePart] = raw.split('::');
                                        const tableName = tableNamePart || '';
                                        const selectedSchemaOwner = tableName ? (schemaOwnerPart || null) : null;
                                        let cols: Array<{ name: string }> = [];
                                        if (tableName) {
                                            try {
                                                const selected = parentDataSourceMappingState.relationTableOptions.find(
                                                    (t) => t.name === tableName && (t.schemaOwner || '') === (selectedSchemaOwner || '')
                                                );
                                                const schemaOwner = selected?.schemaOwner || null;
                                                const dsId = transactionData?.DataSourceFrom ?? dataSourceRegisterId ?? null;
                                                const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
                                                    tableName,
                                                    dsId,
                                                    schemaOwner
                                                );
                                                const colArr = Array.isArray(tableData?.Columns) ? tableData.Columns : [];
                                                cols = colArr
                                                    .filter((c: any) => c?.Name)
                                                    .map((c: any) => ({ name: String(c.Name) }));
                                            } catch {
                                                cols = [];
                                            }
                                        }
                                        setParentDataSourceMappingState((prev) => ({
                                            ...prev,
                                            relationTable: tableName,
                                            relationTableSchemaOwner: selectedSchemaOwner,
                                            relationParentKey: '',
                                            relationChildKey: '',
                                            relationTableColumnOptions: cols
                                        }));
                                    }}
                                >
                                    <option value="">(None)</option>
                                    {parentDataSourceMappingState.relationTableOptions.map((t) => (
                                        <option key={`${t.schemaOwner || ''}.${t.name}`} value={`${t.schemaOwner || ''}::${t.name}`}>
                                            {t.display}
                                        </option>
                                    ))}
                                </select>
                            </div>
                            <div className="flex items-center">
                                <span className="w-32">Relation Parent Key</span>
                                <select
                                    className={`flex-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                    value={parentDataSourceMappingState.relationParentKey}
                                    onChange={(e) =>
                                        setParentDataSourceMappingState((prev) => ({
                                            ...prev,
                                            relationParentKey: e.target.value
                                        }))
                                    }
                                >
                                    <option value="">(None)</option>
                                    {parentDataSourceMappingState.relationTableColumnOptions.map((c) => (
                                        <option key={c.name} value={c.name}>
                                            {c.name}
                                        </option>
                                    ))}
                                </select>
                            </div>
                            <div className="flex items-center">
                                <span className="w-32">Relation Child Key</span>
                                <select
                                    className={`flex-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                    value={parentDataSourceMappingState.relationChildKey}
                                    onChange={(e) =>
                                        setParentDataSourceMappingState((prev) => ({
                                            ...prev,
                                            relationChildKey: e.target.value
                                        }))
                                    }
                                >
                                    <option value="">(None)</option>
                                    {parentDataSourceMappingState.relationTableColumnOptions.map((c) => (
                                        <option key={c.name} value={c.name}>
                                            {c.name}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>
                        <div className={`flex justify-end gap-2 px-3 py-2 border-t ${theme.mainContentSection}`}>
                            <button
                                type="button"
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={handleClearParentDataSourceMapping}
                            >
                                Clear
                            </button>
                            <button
                                type="button"
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={handleApplyParentDataSourceMapping}
                            >
                                Ok
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Matrix Grid Foreignkey popup (Angular: MatrixFkPopup) */}
            {matrixFkPopupState.isOpen && (
                <div className="fixed inset-0 z-[10030] flex items-center justify-center bg-black/50">
                    <div
                        className={`${theme.mainContentSection} rounded-md shadow-lg border flex flex-col overflow-hidden`}
                        style={{ width: '420px', maxWidth: '95vw' }}
                    >
                        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
                            <div className={`text-sm font-semibold ${theme.title}`}>Matrix Grid Foreignkey</div>
                            <button
                                type="button"
                                onClick={handleCloseMatrixFkPopup}
                                className={`p-1 rounded ${theme.button_default}`}
                                aria-label="Close"
                            >
                                <i className="fa-solid fa-times" aria-hidden />
                            </button>
                        </div>
                        <div className="flex-1 min-h-0 overflow-auto p-3 text-xs space-y-2">
                            <div className="flex items-center">
                                <span className="w-32">Current Field</span>
                                <div className="flex-1 truncate" title={matrixFkPopupState.currentFieldName}>
                                    {matrixFkPopupState.currentFieldName}
                                </div>
                            </div>
                            <div className="flex items-center">
                                <span className="w-32">Foreign Unit</span>
                                <select
                                    className={`flex-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                    value={matrixFkPopupState.sourceUnitId ?? ''}
                                    onChange={(e) =>
                                        handleMatrixFkSourceUnitChange(e.target.value ? Number(e.target.value) : null)
                                    }
                                >
                                    <option value="">(None)</option>
                                    {matrixFkPopupState.sourceUnitOptions.map((u) => (
                                        <option key={u.Id} value={u.Id}>
                                            {u.Display}
                                        </option>
                                    ))}
                                </select>
                            </div>
                            <div className="flex items-center">
                                <span className="w-32">Foreignkey Field</span>
                                <select
                                    className={`flex-1 px-2 py-1 border rounded ${theme.inputBox}`}
                                    value={matrixFkPopupState.matrixForeignKeyFieldId ?? ''}
                                    onChange={(e) =>
                                        setMatrixFkPopupState((prev) => ({
                                            ...prev,
                                            matrixForeignKeyFieldId: e.target.value ? Number(e.target.value) : null
                                        }))
                                    }
                                >
                                    <option value="">(None)</option>
                                    {matrixFkPopupState.sourceFieldOptions.map((f) => (
                                        <option key={f.Id} value={f.Id}>
                                            {f.Display}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>
                        <div className={`flex justify-end gap-2 px-3 py-2 border-t ${theme.mainContentSection}`}>
                            <button
                                type="button"
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={handleClearMatrixFkMapping}
                            >
                                Clear
                            </button>
                            <button
                                type="button"
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={handleApplyMatrixFkMapping}
                            >
                                Ok
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Entity Edit popup opened from EntityDataPreview.Edit Data */}
            {entityEditPopup && (
                <div className="fixed inset-0 z-[10002] flex items-center justify-center bg-black/50">
                    <div
                        className={`${theme.mainContentSection} rounded-md shadow-lg border flex flex-col overflow-hidden`}
                        style={{ width: '1100px', height: '760px', maxWidth: '97vw', maxHeight: '95vh' }}
                    >
                        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
                            <div className={`text-sm font-semibold ${theme.title}`}>{entityEditPopup.title || 'Edit Data'}</div>
                            <button
                                type="button"
                                onClick={() => setEntityEditPopup(null)}
                                className={`p-1 rounded ${theme.button_default}`}
                                aria-label="Close"
                            >
                                <i className="fa-solid fa-times" aria-hidden />
                            </button>
                        </div>
                        <div className="flex-1 min-h-0 overflow-auto">
                            {entityEditPopup.route === 'entity-info-edit' ? (
                                <AppEntityInfoEdit paramOverride={entityEditPopup.param} />
                            ) : (
                                <SimpleValueListEntityEdit paramOverride={entityEditPopup.param} />
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default TransactionUnitEditor;
