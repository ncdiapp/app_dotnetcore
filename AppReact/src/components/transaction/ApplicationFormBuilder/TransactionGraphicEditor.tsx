import React, { useState, useEffect, useMemo, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import UnitStructureEditor from './UnitStructureEditor';
import AddUnitDialog from './AddUnitDialog';
import DatabaseTableSelectorDialog from './DatabaseTableSelectorDialog';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';
import { useAlertConfirm } from '../../../components/common/AlertConfirmProvider';
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import MetaDataTableDesign from '../metaDataTableDesign';
import MetaDataViewDesign from '../metaDataViewDesign';
import TransactionUnitEditor from './TransactionUnitEditor';
import TransactionFormPreview from './TransactionFormPreview';
import TransactionUnitLinkTargetEditor from './TransactionUnitLinkTargetEditor';
import TransactionUnitLinkedSearchManagementDialog from './TransactionUnitLinkedSearchManagementDialog';
import TransactionDataTransferSettingMgt from './TransactionDataTransferSettingMgt';
import { adminSvc } from '../../../webapi/adminsvc';
import { setUserMenu } from '../../../redux/features/admin/userSessionSlice';
import { integrationService } from '../../../webapi/integrationsvc';
import { prettyPrintJsonForDisplay } from '../../../helper/integrationPayloadHelper';

interface TransactionGraphicEditorProps {
    transactionId?: number | null;
    applicationId: string | null;
    transactionType?: number | null;
    isCreateNewItem?: boolean;
    dataSourceRegisterId?: number | null;
    isCreateDtoDataModel?: boolean;
    isCreateApiDataModel?: boolean;
    isCreateDataModelView?: boolean;
    erDiagramId?: number | null;
    onRefresh?: () => void;
    /** When provided, Design Form button/dropdown can switch to Form section. isAutoDesignFormLayout: true = Auto Design Form, false = Design On Blank Form, undefined = just open Form (when FormId exists). */
    onOpenFormDesign?: (isAutoDesignFormLayout?: boolean) => void;
    /** Parent stores real transaction id after first save (route props stay null for new transactions). */
    onTransactionIdResolved?: (transactionId: number) => void;
    /** Parent stores FormId when save assigns or creates one. */
    onFormIdResolved?: (formId: number) => void;
    /** Open unit editor after load (Angular TransactionGraphicEditor route param2.needToEditUnitId). */
    initialNeedToEditUnitId?: number | null;
}

// Local enum definitions (matching AngularJS)
const EmAppDockPosition = {
    Right: 2,
    Bottom: 3,
};

const EmAppConversationGroupByType = {
    'Group By All User': 1,
    'Group By Supplier User': 2,
    'Group By Customer User': 3,
    'Group By Employee User': 4,
    'Group By Supplier Name': 5,
    'Group By Customer Name': 6,
};

// Helper function to convert enum values to dropdown options
const enumToOptions = (enumValues: Record<string, number> | null): Array<{ Id: number; Display: string }> => {
    if (!enumValues) return [];
    return Object.entries(enumValues).map(([key, value]) => ({
        Id: value,
        Display: key
    }));
};

// Path = indices from root, e.g. "0", "0-0", "0-1" (backend DTO has no uiId, use index path so checkboxes work)
function cloneTreeAndSetSelectedByPath(nodes: any[], pathSegments: number[], selected: boolean): any[] {
    if (!nodes?.length) return nodes || [];
    const [head, ...rest] = pathSegments;
    if (head == null || head < 0 || head >= nodes.length) return nodes;
    return nodes.map((n: any, i: number) => {
        const isTarget = i === head && rest.length === 0;
        const children = i === head && n.Children?.length && rest.length > 0
            ? cloneTreeAndSetSelectedByPath(n.Children, rest, selected)
            : n.Children;
        return {
            ...n,
            IsSelected: isTarget ? selected : n.IsSelected,
            Children: children
        };
    });
}
function cloneTreeAndSetSelected(nodes: any[], pathKey: string, selected: boolean): any[] {
    const pathSegments = pathKey.split('-').map((s) => parseInt(s, 10)).filter((n) => !Number.isNaN(n));
    return cloneTreeAndSetSelectedByPath(nodes || [], pathSegments, selected);
}

// API Data Structure tree (Angular: wj-flex-grid child-items-path="Children", binding Display + IsSelected)
const ApiDataStructureTree: React.FC<{
    nodes: any[];
    theme: any;
    parentPath?: string;
    onNodeSelectChange?: (pathKey: string, checked: boolean) => void;
}> = ({ nodes, theme, parentPath = '', onNodeSelectChange }) => {
    if (!nodes?.length) return null;
    return (
        <ul className="list-none pl-0 text-xs space-y-0.5">
            {nodes.map((node: any, index: number) => {
                const pathKey = parentPath ? `${parentPath}-${index}` : String(index);
                return (
                    <li key={pathKey} className="pl-2">
                        <div className="flex items-center gap-2 py-0.5">
                            {(node.IsArray || node.IsObject) ? (
                                <input
                                    type="checkbox"
                                    checked={Boolean(node.IsSelected)}
                                    onChange={(e) => {
                                        const checked = e.target.checked;
                                        if (onNodeSelectChange) {
                                            onNodeSelectChange(pathKey, checked);
                                        } else {
                                            node.IsSelected = checked;
                                        }
                                    }}
                                    className="rounded shrink-0 cursor-pointer"
                                />
                            ) : (
                                <span className="w-4 shrink-0" />
                            )}
                            <span className={node.IsArray || node.IsObject ? 'font-semibold' : ''} title={node.Display}>
                                {node.Display || node.NodeName || '—'}
                            </span>
                        </div>
                        {node.Children?.length > 0 && (
                            <div className="pl-4 border-l border-gray-300 dark:border-gray-600 ml-1">
                                <ApiDataStructureTree nodes={node.Children} theme={theme} parentPath={pathKey} onNodeSelectChange={onNodeSelectChange} />
                            </div>
                        )}
                    </li>
                );
            })}
        </ul>
    );
};

// Collapsible Section Component
interface CollapsibleSectionProps {
    title: string;
    isCollapsed: boolean;
    onToggle: () => void;
    children: React.ReactNode;
    defaultCollapsed?: boolean;
    collapsedLabel?: string;
}

const CollapsibleSection: React.FC<CollapsibleSectionProps> = ({
    title,
    isCollapsed,
    onToggle,
    children,
    defaultCollapsed = false,
    collapsedLabel
}) => {
    const { theme } = useTheme();
    const shouldShowBody = defaultCollapsed ? isCollapsed : !isCollapsed;
    const isCurrentlyCollapsed = defaultCollapsed ? !isCollapsed : isCollapsed;
    const contentRef = React.useRef<HTMLDivElement>(null);
    const [height, setHeight] = React.useState<number | string>('auto');

    // Calculate height for smooth animation
    React.useEffect(() => {
        if (contentRef.current) {
            if (shouldShowBody) {
                // Expanding: measure and set height
                const scrollHeight = contentRef.current.scrollHeight;
                setHeight(scrollHeight);
                // After animation, allow auto height for dynamic content
                const timer = setTimeout(() => {
                    setHeight('auto');
                }, 300);
                return () => clearTimeout(timer);
            } else {
                // Collapsing: capture current height first
                const currentHeight = contentRef.current.scrollHeight;
                setHeight(currentHeight);
                // Force reflow, then animate to 0
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => {
                        setHeight(0);
                    });
                });
            }
        }
    }, [shouldShowBody]);

    return (
        <div className={`min-h-9 border rounded overflow-hidden ${theme.mainContentSection}`}>
            <div
                className={`flex items-center justify-between px-3 py-2 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors ${theme.mainContentSection} border-b`}
                onClick={onToggle}
            >
                <div className="flex items-center gap-2">
                    <div className={`text-sm font-semibold ${theme.title}`}>
                        {title}
                    </div>
                    {isCurrentlyCollapsed && collapsedLabel && (
                        <span className={`text-xs ${theme.label} italic`}>{collapsedLabel}</span>
                    )}
                </div>
                <div className="flex items-center">
                    <i
                        className={`fa-solid ${shouldShowBody ? 'fa-chevron-up' : 'fa-chevron-down'} text-xs ${theme.label} transition-transform duration-300`}
                    ></i>
                </div>
            </div>
            <div
                ref={contentRef}
                className="overflow-hidden transition-all duration-300 ease-in-out"
                style={{
                    height: typeof height === 'number' ? `${height}px` : height,
                    opacity: shouldShowBody ? 1 : 0,
                    transform: shouldShowBody ? 'translateY(0)' : 'translateY(-8px)',
                    marginBottom: shouldShowBody ? '0' : '-8px'
                }}
            >
                <div className="p-4">
                    {children}
                </div>
            </div>
        </div>
    );
};

const TransactionGraphicEditor: React.FC<TransactionGraphicEditorProps> = ({
    transactionId,
    applicationId,
    transactionType,
    isCreateNewItem = false,
    dataSourceRegisterId,
    isCreateDtoDataModel = false,
    isCreateApiDataModel = false,
    isCreateDataModelView = false,
    erDiagramId = null,
    onRefresh,
    onOpenFormDesign,
    onTransactionIdResolved,
    onFormIdResolved,
    initialNeedToEditUnitId = null,
}) => {
    const { theme } = useTheme();
    const { showError, showValidationMessages, showInfo } = useErrorMessage();
    const { showConfirm } = useAlertConfirm();
    const dispatch = useDispatch();
    const { addTabAndNavigate } = useTabNavigation();

    // Collapsible sections state
    const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());
    // Design Form dropdown open state (when !FormId)
    const [designFormDropdownOpen, setDesignFormDropdownOpen] = useState(false);
    const designFormDropdownRef = useRef<HTMLDivElement>(null);
    const [linkTargetDropdownOpen, setLinkTargetDropdownOpen] = useState(false);
    const [apiSettingsDropdownOpen, setApiSettingsDropdownOpen] = useState(false);
    const [apiSettingPopup, setApiSettingPopup] = useState<{ isOpen: boolean; mode: 'Consume' | 'Provide' }>({
        isOpen: false,
        mode: 'Consume',
    });

    // Preview popup state
    const [previewPopup, setPreviewPopup] = useState<{
        isOpen: boolean;
        transactionId: number | null;
        transactionType: number | null;
        transactionName: string | null;
    }>({
        isOpen: false,
        transactionId: null,
        transactionType: null,
        transactionName: null
    });

    // Close Design Form dropdown on outside click
    useEffect(() => {
        if (!designFormDropdownOpen) return;
        const handleClick = (e: MouseEvent) => {
            if (designFormDropdownRef.current && !designFormDropdownRef.current.contains(e.target as Node)) {
                setDesignFormDropdownOpen(false);
            }
        };
        document.addEventListener('click', handleClick);
        return () => document.removeEventListener('click', handleClick);
    }, [designFormDropdownOpen]);

    // Close API Settings dropdown on outside click
    useEffect(() => {
        if (!apiSettingsDropdownOpen) return;
        const handleClick = () => setApiSettingsDropdownOpen(false);
        document.addEventListener('click', handleClick);
        return () => document.removeEventListener('click', handleClick);
    }, [apiSettingsDropdownOpen]);

    // Get enum values from Redux
    const emGrandChildEditModeEnum = useEnumValues('EmAppGrandChildEditMode');
    const emTransactionOrganizedTypeEnum = useEnumValues('EmTransactionOrganizedType');
    const emAppTransactionCrudTypeEnum = useEnumValues('EmAppTransactionCrudType');

    // Convert enums to options
    const grandChildEditModeOptions = useMemo(() => enumToOptions(emGrandChildEditModeEnum), [emGrandChildEditModeEnum]);
    // Use local enum definitions for Dock Position and Conversation Group By Type (matching AngularJS)
    const conversationDockPositionOptions = useMemo(() => enumToOptions(EmAppDockPosition), []);
    const conversationGroupByTypeOptions = useMemo(() => enumToOptions(EmAppConversationGroupByType), []);
    const transactionOrganizedTypeOptions = useMemo(() => {
        if (!emTransactionOrganizedTypeEnum) return [];
        return Object.entries(emTransactionOrganizedTypeEnum).map(([key, value]) => ({
            Id: value,
            Display: `${value}. ${key}`
        }));
    }, [emTransactionOrganizedTypeEnum]);
    const apiCrudTypeOptions = useMemo(() => {
        if (!emAppTransactionCrudTypeEnum) return [];
        return Object.entries(emAppTransactionCrudTypeEnum).map(([key, value]) => ({
            Id: value,
            Display: key
        }));
    }, [emAppTransactionCrudTypeEnum]);

    const [transactionData, setTransactionData] = useState<any>(null);
    const [isModified, setIsModified] = useState(false);
    const [allTransactionFields, setAllTransactionFields] = useState<Array<{ Id: number; longDisplay: string }>>([]);
    const [selectedUnitId, setSelectedUnitId] = useState<number | null>(null);
    const [addUnitDialog, setAddUnitDialog] = useState<{ isOpen: boolean; level: number; parentUnitId?: number | null }>({
        isOpen: false,
        level: 1,
        parentUnitId: null
    });
    const [tableSelectorDialog, setTableSelectorDialog] = useState<{ isOpen: boolean; level: number; parentUnitId?: number | null }>({
        isOpen: false,
        level: 1,
        parentUnitId: null
    });
    const [tableDesignDialog, setTableDesignDialog] = useState<{ isOpen: boolean; tableName: string | null; schemaOwner: string | null }>({
        isOpen: false,
        tableName: null,
        schemaOwner: null
    });
    const [newTableUnitDialog, setNewTableUnitDialog] = useState<{ isOpen: boolean; level: number; parentUnitId?: number | null }>({
        isOpen: false,
        level: 1,
        parentUnitId: null
    });
    const [newTableUnitSaved, setNewTableUnitSaved] = useState<{ tableName: string; schemaOwner: string } | null>(null);
    const newTableUnitSavedRef = useRef<{ tableName: string; schemaOwner: string } | null>(null);
    const [viewDesignDialog, setViewDesignDialog] = useState<{ isOpen: boolean; viewName: string | null; schemaOwner: string | null }>({
        isOpen: false,
        viewName: null,
        schemaOwner: null
    });
    const [unitEditorDialog, setUnitEditorDialog] = useState<{ isOpen: boolean; unit: any | null }>({
        isOpen: false,
        unit: null
    });
    const [unitNavigateDialog, setUnitNavigateDialog] = useState<{
        unitId: number | null;
        unitDisplayName: string;
        linkTargetEditor: { usageType: number; title: string } | null;
        linkedSearchOpen: boolean;
    }>({ unitId: null, unitDisplayName: '', linkTargetEditor: null, linkedSearchOpen: false });
    // API Data Model (Block B): integration list and operation Id -> display name (e.g. "Weather . GetCurrentWeather")
    const [integrationSettingList, setIntegrationSettingList] = useState<any[]>([]);
    const [dictApiOperationDisplay, setDictApiOperationDisplay] = useState<Record<number, string>>({});
    const [apiJsonPreviewOpen, setApiJsonPreviewOpen] = useState(false);
    const [apiJsonPreviewData, setApiJsonPreviewData] = useState<string | object | null>(null);
    const [apiJsonPreviewLoading, setApiJsonPreviewLoading] = useState(false);
    const [formData, setFormData] = useState({
        TransactionName: '',
        TransactionOrganizedType: null as number | null,
        EmGrandChildEditMode: null as number | null,
        PreSaveValidationMethod: '',
        IsForPublicAcesss: false,
        TransactionFileStorageRootFolderId: null as number | null,
        TransactionFileStorageRootFolderName: '',
        // MasterDetail specific properties
        IsNeedToSetCriticalPathTrackFlow: false,
        IsExclusiveForOwner: false,
        IsNeedToSetComunication: false,
        // New transaction defaults match Angular transactionEditorCtrl (IsShowSaveButton false for view-only model)
        IsShowSaveButton: !isCreateDataModelView,
        IsShowCalculateButton: true,
        IsShowPrintButton: false,
        // Communication properties
        ConversationBoxDockPosition: null as number | null,
        CommunicationGroupByType: null as number | null,
        CommunicationToUserIdTransField: null as number | null,
        IsOpenCommunicationByDefault: false,
        // API properties (physical table block: Enable Publish Data To API)
        IsEnableSaveByApiCall: false,
        SaveByApiCallDataTransferId: null as number | null,
        // API integration block (Rest API data model): Provider API, CRUD type, Save/Delete by API
        FolderUsageType: null as number | null,
        ApiIntegrationTransactionCrudType: null as number | null,
        IsEnableDeleteByApi: false,
        DeleteDataTransferId: null as number | null
    });

    // Toggle section collapse
    const toggleSection = (sectionId: string) => {
        setCollapsedSections(prev => {
            const newSet = new Set(prev);
            if (newSet.has(sectionId)) {
                newSet.delete(sectionId);
            } else {
                newSet.add(sectionId);
            }
            return newSet;
        });
    };

    const isSectionCollapsed = (sectionId: string) => {
        return collapsedSections.has(sectionId);
    };

    const loadNewTransactionData = async () => {
        try {
            dispatch(setIsBusy());
            // Call API with isNewTransaction=true to get default settings from server
            const newTransactionType = transactionType ? transactionType.toString() : '1'; // Default to MasterDetail (1)
            const data = await appTransactionService.getOneHierarchyTransaction(
                '0', // Use '0' for new transaction
                true, // isNewTransaction
                newTransactionType,
                '', // newFolderTransactionUsageType
                dataSourceRegisterId ? dataSourceRegisterId.toString() : '', // newTransactionDataSourceFrom
                false, // isESitePageDesign
                '' // rootWorkflowTransactionId
            );

            if (data) {
                // Initialize DictDeletedItemsIds (matching AngularJS behavior)
                if (!data.DictDeletedItemsIds) {
                    data.DictDeletedItemsIds = {};
                }
                if (!data.DictDeletedItemsIds.AppTransactionUnitList) {
                    data.DictDeletedItemsIds.AppTransactionUnitList = [];
                }
                
                // Set SaasApplicationID from applicationId prop (matching AngularJS line 307)
                // $scope.dataModel.AppTransactionData.SaasApplicationId = $scope.controllerModel.saasApplicationId || null;
                if (applicationId) {
                    data.SaasApplicationId = parseInt(applicationId);
                } else {
                    data.SaasApplicationId = null;
                }
                
                // When creating from database (dataSourceRegisterId is provided and not DTO model), 
                // ensure IsPhysicalModelTableCreated is set to true
                // This matches AngularJS behavior (line 369): 
                // if (!isCreateDtoDataModel) { IsPhysicalModelTableCreated = true; }
                if (dataSourceRegisterId && !isCreateDtoDataModel && !data.IsPhysicalModelTableCreated) {
                    data.IsPhysicalModelTableCreated = true;
                }
                
                // If creating DTO model, ensure IsPhysicalModelTableCreated is false
                // This matches AngularJS behavior (line 363)
                if (isCreateDtoDataModel) {
                    data.IsPhysicalModelTableCreated = false;
                }
                
                // When creating from Rest API, set IsApiIntegrationTransaction and no physical table
                if (isCreateApiDataModel) {
                    data.OtherOptions = data.OtherOptions || {};
                    data.OtherOptions.IsApiIntegrationTransaction = true;
                    data.IsPhysicalModelTableCreated = false;
                }

                // When generating from ER diagram, set ErDiagramId for table drag-and-drop
                if (erDiagramId) {
                    data.OtherOptions = data.OtherOptions || {};
                    data.OtherOptions.ErDiagramId = erDiagramId;
                }

                // Match Angular transactionEditorCtrl new-transaction defaults (API may return false)
                data.IsShowSaveButton = isCreateDataModelView ? false : true;
                data.IsShowCalculateButton = true;

                setTransactionData(data);

                setFormData({
                    TransactionName: data.TransactionName || '',
                    TransactionOrganizedType: data.TransactionOrganizedType || transactionType || null,
                    EmGrandChildEditMode: data.EmGrandChildEditMode || null,
                    PreSaveValidationMethod: data.PreSaveValidationMethod || '',
                    IsForPublicAcesss: data.IsForPublicAcesss || false,
                    TransactionFileStorageRootFolderId: data.TransactionFileStorageRootFolderId || null,
                    TransactionFileStorageRootFolderName: data.TransactionFileStorageRootFolderName || '',
                    IsNeedToSetCriticalPathTrackFlow: data.IsNeedToSetCriticalPathTrackFlow || false,
                    IsExclusiveForOwner: data.IsExclusiveForOwner || false,
                    IsNeedToSetComunication: data.IsNeedToSetComunication || false,
                    IsShowSaveButton: data.IsShowSaveButton,
                    IsShowCalculateButton: data.IsShowCalculateButton,
                    IsShowPrintButton: data.IsShowPrintButton || false,
                    ConversationBoxDockPosition: data.ConversationBoxDockPosition || null,
                    CommunicationGroupByType: data.OtherOptions?.CommunicationGroupByType || null,
                    CommunicationToUserIdTransField: data.OtherOptions?.CommunicationToUserIdTransField || null,
                    IsOpenCommunicationByDefault: data.OtherOptions?.IsOpenCommunicationByDefault || false,
                    IsEnableSaveByApiCall: data.OtherOptions?.IsEnableSaveByApiCall || false,
                    SaveByApiCallDataTransferId: data.OtherOptions?.SaveByApiCallDataTransferId || null,
                    FolderUsageType: data.FolderUsageType ?? null,
                    ApiIntegrationTransactionCrudType: data.OtherOptions?.ApiIntegrationTransactionCrudType ?? null,
                    IsEnableDeleteByApi: data.OtherOptions?.IsEnableDeleteByApi || false,
                    DeleteDataTransferId: data.OtherOptions?.DeleteDataTransferId ?? null
                });
                setIsModified(true);
            }
        } catch (error: any) {
            showError(error.message || 'Failed to load new transaction defaults');
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    // Load transaction data
    useEffect(() => {
        if (transactionId) {
            loadTransactionData();
        } else if (isCreateNewItem) {
            // Load default settings from server for new transaction
            loadNewTransactionData();
        }
    }, [transactionId, isCreateNewItem, transactionType, dataSourceRegisterId, erDiagramId]);

    // Load integration list for API Data Model (Block B) - Provider API Data Source dropdown (match Angular loadIntegrationSettingListOnDemand)
    const isApiIntegrationMode = isCreateApiDataModel || Boolean(transactionData?.OtherOptions?.IsApiIntegrationTransaction);
    useEffect(() => {
        if (!isApiIntegrationMode) return;
        integrationService.retrieveAllAppIntegrationSettingDto(false).then((list: any[]) => {
            if (!list?.length) return;
            setIntegrationSettingList(list);
            const dict: Record<number, string> = {};
            list.forEach((integration: any) => {
                const params = integration.AppIntergrationSettingParameterList || integration.AppIntegrationSettingParameterList || [];
                params.forEach((apiOp: any) => {
                    const display = integration.Name ? `${integration.Name} . ${apiOp.ActionCode || apiOp.Id}` : (apiOp.ActionCode || String(apiOp.Id));
                    dict[apiOp.Id] = display;
                });
            });
            setDictApiOperationDisplay(dict);
        }).catch(() => {});
    }, [isApiIntegrationMode]);

    const loadTransactionData = async (idOverride?: number | null) => {
        const loadId = idOverride ?? transactionId ?? transactionData?.Id;
        if (!loadId) return;

        try {
            dispatch(setIsBusy());
            const data = await appTransactionService.getOneHierarchyTransaction(
                loadId.toString(),
                false,
                '',
                '',
                '',
                false,
                ''
            );

            if (data) {
                // Initialize DictDeletedItemsIds (matching AngularJS behavior)
                if (!data.DictDeletedItemsIds) {
                    data.DictDeletedItemsIds = {};
                }
                if (!data.DictDeletedItemsIds.AppTransactionUnitList) {
                    data.DictDeletedItemsIds.AppTransactionUnitList = [];
                }
                
                // Initialize DictDeletedItemsIds for each unit's field list
                if (data.AppTransactionUnitList && Array.isArray(data.AppTransactionUnitList)) {
                    data.AppTransactionUnitList.forEach((unit: any) => {
                        if (unit.Id) {
                            const fieldListKey = `AppTransactionFieldList_${unit.Id}`;
                            if (!data.DictDeletedItemsIds[fieldListKey]) {
                                data.DictDeletedItemsIds[fieldListKey] = [];
                            }
                            
                            // Process child units
                            if (unit.Children && Array.isArray(unit.Children)) {
                                unit.Children.forEach((childUnit: any) => {
                                    if (childUnit.Id) {
                                        const childFieldListKey = `AppTransactionFieldList_${childUnit.Id}`;
                                        if (!data.DictDeletedItemsIds[childFieldListKey]) {
                                            data.DictDeletedItemsIds[childFieldListKey] = [];
                                        }
                                        
                                        // Process grandchild units
                                        if (childUnit.Children && Array.isArray(childUnit.Children)) {
                                            childUnit.Children.forEach((grandchildUnit: any) => {
                                                if (grandchildUnit.Id) {
                                                    const grandchildFieldListKey = `AppTransactionFieldList_${grandchildUnit.Id}`;
                                                    if (!data.DictDeletedItemsIds[grandchildFieldListKey]) {
                                                        data.DictDeletedItemsIds[grandchildFieldListKey] = [];
                                                    }
                                                }
                                            });
                                        }
                                    }
                                });
                            }
                        }
                    });
                }
                
                setTransactionData(data);

                // Load all transaction fields for Communication User List Field dropdown
                if (data.AppTransactionUnitList && data.AppTransactionUnitList.length > 0) {
                    const allFields: Array<{ Id: number; longDisplay: string }> = [];
                    data.AppTransactionUnitList.forEach((unit: any) => {
                        if (unit.AppTransactionFieldList) {
                            unit.AppTransactionFieldList.forEach((field: any) => {
                                allFields.push({
                                    Id: field.Id,
                                    longDisplay: field.longDisplay || field.DisplayName || field.FieldName
                                });
                            });
                        }
                    });
                    setAllTransactionFields(allFields);
                }

                setFormData({
                    TransactionName: data.TransactionName || '',
                    TransactionOrganizedType: data.TransactionOrganizedType || null,
                    EmGrandChildEditMode: data.EmGrandChildEditMode || null,
                    PreSaveValidationMethod: data.PreSaveValidationMethod || '',
                    IsForPublicAcesss: data.IsForPublicAcesss || false,
                    TransactionFileStorageRootFolderId: data.TransactionFileStorageRootFolderId || null,
                    TransactionFileStorageRootFolderName: data.TransactionFileStorageRootFolderName || '',
                    IsNeedToSetCriticalPathTrackFlow: data.IsNeedToSetCriticalPathTrackFlow || false,
                    IsExclusiveForOwner: data.IsExclusiveForOwner || false,
                    IsNeedToSetComunication: data.IsNeedToSetComunication || false,
                    IsShowSaveButton: data.IsShowSaveButton || false,
                    IsShowCalculateButton: data.IsShowCalculateButton || false,
                    IsShowPrintButton: data.IsShowPrintButton || false,
                    ConversationBoxDockPosition: data.ConversationBoxDockPosition || null,
                    CommunicationGroupByType: data.OtherOptions?.CommunicationGroupByType || null,
                    CommunicationToUserIdTransField: data.OtherOptions?.CommunicationToUserIdTransField || null,
                    IsOpenCommunicationByDefault: data.OtherOptions?.IsOpenCommunicationByDefault || false,
                    IsEnableSaveByApiCall: data.OtherOptions?.IsEnableSaveByApiCall || false,
                    SaveByApiCallDataTransferId: data.OtherOptions?.SaveByApiCallDataTransferId || null,
                    FolderUsageType: data.FolderUsageType ?? null,
                    ApiIntegrationTransactionCrudType: data.OtherOptions?.ApiIntegrationTransactionCrudType ?? null,
                    IsEnableDeleteByApi: data.OtherOptions?.IsEnableDeleteByApi || false,
                    DeleteDataTransferId: data.OtherOptions?.DeleteDataTransferId ?? null
                });
                setIsModified(data.IsModified || false);
            }
        } catch (error: any) {
            showError(error.message || 'Failed to load transaction data');
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleRefresh = () => {
        if (transactionId) {
            loadTransactionData();
        }
        if (onRefresh) {
            onRefresh();
        }
    };

    // Prepare one unit's save data (matching AngularJS prepareOneUnitSaveData)
    const prepareOneUnitSaveData = (aUnit: any, appTransactionData: any) => {
        if (!aUnit) return;

        // Propagate fields removed in the Unit Editor into DictDeletedItemsIds so the backend
        // (AppTransactionController → AppTransactionFieldList.DeletedItemIds) actually deletes them.
        if (aUnit.Id && Array.isArray(aUnit.DeletedFieldIdList) && aUnit.DeletedFieldIdList.length > 0) {
            if (!appTransactionData.DictDeletedItemsIds) {
                appTransactionData.DictDeletedItemsIds = {};
            }
            const key = `AppTransactionFieldList_${aUnit.Id}`;
            const existing = appTransactionData.DictDeletedItemsIds[key] || [];
            appTransactionData.DictDeletedItemsIds[key] = Array.from(
                new Set([...existing, ...aUnit.DeletedFieldIdList])
            );
        }
        // Strip the temp tracking prop so it isn't sent as an unknown field to the backend.
        if ('DeletedFieldIdList' in aUnit) {
            delete aUnit.DeletedFieldIdList;
        }

        if (aUnit.AppTransactionFieldList && Array.isArray(aUnit.AppTransactionFieldList)) {
            aUnit.AppTransactionFieldList.forEach((transField: any) => {
                if (transField) {
                    // Mark new fields
                    if (!transField.Id) {
                        transField.IsNew = true;
                        aUnit.IsModified = true;
                    }
                    
                    // Handle ParentPKFieldGuid
                    if (transField.ParentPKFieldGuid) {
                        transField.IsLinkToParentPrimaryKey = true;
                        
                        if (transField.RowIdentityGuid) {
                            if (!appTransactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap) {
                                appTransactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap = {};
                            }
                            appTransactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap[transField.RowIdentityGuid] = transField.ParentPKFieldGuid;
                        }
                    } else {
                        transField.IsLinkToParentPrimaryKey = false;
                    }
                }
            });
        }
    };

    // Prepare save data (matching AngularJS prepareSaveData)
    const prepareSaveData = (appTransactionData: any) => {
        if (!appTransactionData) return;
        
        // Initialize DictCurrentPKOrFKLinkToParentKeyGuidMap if not exists
        if (!appTransactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap) {
            appTransactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap = {};
        }
        
        // Ensure DictDeletedItemsIds is initialized
        if (!appTransactionData.DictDeletedItemsIds) {
            appTransactionData.DictDeletedItemsIds = {};
        }
        if (!appTransactionData.DictDeletedItemsIds.AppTransactionUnitList) {
            appTransactionData.DictDeletedItemsIds.AppTransactionUnitList = [];
        }
        
        // Process database field names if needed (matching AngularJS logic)
        // Note: This logic is commented out in AngularJS but kept for reference
        // if (!appTransactionData.IsPhysicalModelTableCreated && !appTransactionData.OtherOptions?.IsApiIntegrationTransaction) {
        //     // Process field database names
        // }
        
        // Process all units (parent, child, grandchild)
        if (appTransactionData.AppTransactionUnitList && Array.isArray(appTransactionData.AppTransactionUnitList)) {
            appTransactionData.AppTransactionUnitList.forEach((aUnit: any) => {
                prepareOneUnitSaveData(aUnit, appTransactionData);
                
                // Process child units
                if (aUnit.Children && Array.isArray(aUnit.Children)) {
                    aUnit.Children.forEach((aChildUnit: any) => {
                        if (aChildUnit) {
                            prepareOneUnitSaveData(aChildUnit, appTransactionData);
                            
                            // Process grandchild units
                            if (aChildUnit.Children && Array.isArray(aChildUnit.Children)) {
                                aChildUnit.Children.forEach((aGrandchildUnit: any) => {
                                    if (aGrandchildUnit) {
                                        prepareOneUnitSaveData(aGrandchildUnit, appTransactionData);
                                    }
                                });
                            }
                        }
                    });
                }
            });
        }
    };

    const notifyParentOfSavedIds = (savedObject: any) => {
        if (savedObject?.Id) {
            onTransactionIdResolved?.(savedObject.Id);
        }
        if (savedObject?.FormId) {
            onFormIdResolved?.(savedObject.FormId);
        }
    };

    const handleSave = async (): Promise<number | null> => {
        try {
            dispatch(setIsBusy());

            const dataToSave = {
                ...transactionData,
                ...formData,
                OtherOptions: {
                    ...transactionData?.OtherOptions,
                    CommunicationGroupByType: formData.CommunicationGroupByType,
                    CommunicationToUserIdTransField: formData.CommunicationToUserIdTransField,
                    IsOpenCommunicationByDefault: formData.IsOpenCommunicationByDefault,
                    IsEnableSaveByApiCall: formData.IsEnableSaveByApiCall,
                    SaveByApiCallDataTransferId: formData.SaveByApiCallDataTransferId,
                    ApiIntegrationTransactionCrudType: formData.ApiIntegrationTransactionCrudType,
                    IsEnableDeleteByApi: formData.IsEnableDeleteByApi,
                    DeleteDataTransferId: formData.DeleteDataTransferId
                },
                IsModified: true
            };

            // Ensure DictDeletedItemsIds is initialized
            if (!dataToSave.DictDeletedItemsIds) {
                dataToSave.DictDeletedItemsIds = {};
            }
            if (!dataToSave.DictDeletedItemsIds.AppTransactionUnitList) {
                dataToSave.DictDeletedItemsIds.AppTransactionUnitList = [];
            }

            // Prepare save data (matching AngularJS prepareSaveData)
            prepareSaveData(dataToSave);

            const result = await appTransactionService.saveOneAppTransaction(dataToSave);

            if (result?.ValidationResult) {
                showValidationMessages(result.ValidationResult, true);
            }

            if (result?.IsSuccessful && result?.Object) {
                setIsModified(false);
                notifyParentOfSavedIds(result.Object);

                // Update transactionData with the saved object (including Id)
                // This ensures transactionData.Id is immediately available after save
                if (result.Object.Id) {
                    setTransactionData((prev: any) => ({
                        ...prev,
                        ...result.Object,
                        Id: result.Object.Id
                    }));
                }

                const savedId = result.Object.Id ?? transactionId ?? transactionData?.Id;
                if (savedId) {
                    await loadTransactionData(savedId);
                }

                // Return the saved transaction Id
                return result.Object.Id || null;
            }
            
            return null;
        } catch (error: any) {
            showError(error.message || 'Failed to save transaction');
            return null;
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleFieldChange = (field: string, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        setIsModified(true);
    };

    // Unit structure editor handlers
    const handleUnitSelect = (unit: any) => {
        setSelectedUnitId(unit?.Id || null);
        // TODO: Open unit editor or show unit details
    };

    const handleUnitEdit = (unit: any) => {
        if (!unit) return;
        setSelectedUnitId(unit?.Id || null);
        setUnitEditorDialog({
            isOpen: true,
            unit: unit
        });
    };

    const handleUnitNavigate = (unit: any, usageType: number) => {
        const unitId = unit?.Id != null ? Number(unit.Id) : null;
        if (!unitId) {
            showInfo('Please save transaction/unit first.');
            return;
        }

        const unitDisplayName = unit?.UnitDisplayName || unit?.UnitName || `Unit ${unitId}`;

        // 102 = Unit Navigate To Search (Linked Search Management)
        if (usageType === 102) {
            setUnitNavigateDialog({ unitId, unitDisplayName, linkTargetEditor: null, linkedSearchOpen: true });
            return;
        }

        const title =
            usageType === 101 ? 'Unit Navigate To Data Model' : usageType === 5 ? 'Unit Navigate To Any Page' : 'Unit Navigate';
        setUnitNavigateDialog({ unitId, unitDisplayName, linkTargetEditor: { usageType, title }, linkedSearchOpen: false });
    };

    const findUnitById = (unitList: any[], unitId: number | null): any | null => {
        if (!unitId) return null;
        for (const u of unitList || []) {
            if (u?.Id != null && Number(u.Id) === Number(unitId)) return u;
            const children = u?.Children || [];
            if (children?.length) {
                const found = findUnitById(children, unitId);
                if (found) return found;
            }
        }
        return null;
    };

    const initialUnitEditConsumedRef = useRef<number | null>(null);
    useEffect(() => {
        if (!initialNeedToEditUnitId || !transactionData?.AppTransactionUnitList?.length) return;
        if (initialUnitEditConsumedRef.current === initialNeedToEditUnitId) return;
        const unit = findUnitById(transactionData.AppTransactionUnitList, initialNeedToEditUnitId);
        if (unit) {
            initialUnitEditConsumedRef.current = initialNeedToEditUnitId;
            handleUnitEdit(unit);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps -- one-shot open when hierarchy loads
    }, [transactionData, initialNeedToEditUnitId]);

    const handleCloseUnitEditor = () => {
        setUnitEditorDialog({ isOpen: false, unit: null });
    };

    const handleUnitEditorSave = async (updatedUnit: any, saveToServer: boolean = true) => {
        if (!transactionData) return;

        // Helper function to update unit in list
        const updateUnitInList = (unitList: any[], targetId: number, updatedData: any): any[] => {
            return unitList.map((u: any) => {
                if (u.Id === targetId) {
                    return { ...u, ...updatedData };
                }
                if (u.Children) {
                    return { ...u, Children: updateUnitInList(u.Children, targetId, updatedData) };
                }
                return u;
            });
        };

        // Update unit in transaction data state
        let updatedUnits: any[];
        if (updatedUnit.Id) {
            updatedUnits = updateUnitInList(transactionData.AppTransactionUnitList || [], updatedUnit.Id, updatedUnit);
        } else {
            // New unit - update in place
            updatedUnits = (transactionData.AppTransactionUnitList || []).map((u: any) => 
                u === updatedUnit ? { ...u, ...updatedUnit } : u
            );
        }

        // Update local state
        const updatedTransactionData = {
            ...transactionData,
            AppTransactionUnitList: updatedUnits
        };
        setTransactionData(updatedTransactionData);
        setIsModified(true);

        // Only save to server if saveToServer is true
        if (!saveToServer) {
            return;
        }

        // Save the entire transaction after updating the unit
        try {
            dispatch(setIsBusy());

            const dataToSave = {
                ...updatedTransactionData,
                ...formData,
                OtherOptions: {
                    ...updatedTransactionData?.OtherOptions,
                    CommunicationGroupByType: formData.CommunicationGroupByType,
                    CommunicationToUserIdTransField: formData.CommunicationToUserIdTransField,
                    IsOpenCommunicationByDefault: formData.IsOpenCommunicationByDefault,
                    IsEnableSaveByApiCall: formData.IsEnableSaveByApiCall,
                    SaveByApiCallDataTransferId: formData.SaveByApiCallDataTransferId,
                    ApiIntegrationTransactionCrudType: formData.ApiIntegrationTransactionCrudType,
                    IsEnableDeleteByApi: formData.IsEnableDeleteByApi,
                    DeleteDataTransferId: formData.DeleteDataTransferId
                },
                IsModified: true
            };

            // Ensure DictDeletedItemsIds is initialized
            if (!dataToSave.DictDeletedItemsIds) {
                dataToSave.DictDeletedItemsIds = {};
            }
            if (!dataToSave.DictDeletedItemsIds.AppTransactionUnitList) {
                dataToSave.DictDeletedItemsIds.AppTransactionUnitList = [];
            }

            // Prepare save data (matching AngularJS prepareSaveData)
            prepareSaveData(dataToSave);

            const result = await appTransactionService.saveOneAppTransaction(dataToSave);

            if (result?.ValidationResult) {
                showValidationMessages(result.ValidationResult, true);
            }

            if (result?.IsSuccessful && result?.Object) {
                setIsModified(false);
                notifyParentOfSavedIds(result.Object);
                if (result.Object.Id) {
                    setTransactionData((prev: any) => ({
                        ...prev,
                        ...result.Object,
                        Id: result.Object.Id
                    }));
                }
                const savedId = result.Object.Id ?? transactionId ?? updatedTransactionData?.Id;
                if (savedId) {
                    await loadTransactionData(savedId);
                }
            }
        } catch (error: any) {
            showError(error.message || 'Failed to save transaction');
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleUnitDelete = async (unit: any) => {
        if (!unit.Id) {
            // New unit (no Id): remove from local tree immediately (no need to save first)
            if (transactionData?.AppTransactionUnitList) {
                const matchKey = unit?.uiId ?? unit;
                const removeNewUnitFromTree = (list: any[]): any[] => {
                    return (list || [])
                        .filter((u: any) => (u?.uiId ?? u) !== matchKey)
                        .map((u: any) => ({
                            ...u,
                            Children: u.Children ? removeNewUnitFromTree(u.Children) : u.Children
                        }));
                };

                const updatedUnits = removeNewUnitFromTree(transactionData.AppTransactionUnitList);
                setTransactionData({ ...transactionData, AppTransactionUnitList: updatedUnits });
                setIsModified(true);
                setSelectedUnitId(null);
            }
            return;
        }

        // Existing unit - show confirmation dialog
        const confirmed = await showConfirm(
            `Are you sure you want to delete unit "${unit.UnitDisplayName || unit.UnitName}"?`,
            {
                title: 'Delete Unit',
                confirmLabel: 'Delete',
                cancelLabel: 'Cancel',
                confirmButtonStyle: 'bg-red-500 hover:bg-red-600 text-white'
            }
        );

        if (confirmed && transactionData) {
            const updatedData = { ...transactionData };
            
            // Initialize DictDeletedItemsIds if needed
            if (!updatedData.DictDeletedItemsIds) {
                updatedData.DictDeletedItemsIds = {};
            }
            if (!updatedData.DictDeletedItemsIds.AppTransactionUnitList) {
                updatedData.DictDeletedItemsIds.AppTransactionUnitList = [];
            }

            // Add unit ID to deleted items
            if (unit.Id && !updatedData.DictDeletedItemsIds.AppTransactionUnitList.includes(unit.Id)) {
                updatedData.DictDeletedItemsIds.AppTransactionUnitList.push(unit.Id);
            }

            // Also add child and grandchild units to deleted items
            const addChildrenToDeleted = (unitToDelete: any) => {
                if (unitToDelete.Children && Array.isArray(unitToDelete.Children)) {
                    unitToDelete.Children.forEach((child: any) => {
                        if (child.Id && !updatedData.DictDeletedItemsIds.AppTransactionUnitList.includes(child.Id)) {
                            updatedData.DictDeletedItemsIds.AppTransactionUnitList.push(child.Id);
                        }
                        if (child.Children && Array.isArray(child.Children)) {
                            child.Children.forEach((grandchild: any) => {
                                if (grandchild.Id && !updatedData.DictDeletedItemsIds.AppTransactionUnitList.includes(grandchild.Id)) {
                                    updatedData.DictDeletedItemsIds.AppTransactionUnitList.push(grandchild.Id);
                                }
                            });
                        }
                    });
                }
            };

            addChildrenToDeleted(unit);

            // Remove from local state (for immediate UI update)
            const removeUnitFromList = (unitList: any[], targetId: number): any[] => {
                return unitList.filter(u => u.Id !== targetId).map(u => ({
                    ...u,
                    Children: u.Children ? removeUnitFromList(u.Children, targetId) : []
                }));
            };

            updatedData.AppTransactionUnitList = removeUnitFromList(updatedData.AppTransactionUnitList, unit.Id);
            setTransactionData(updatedData);
        setIsModified(true);
        setSelectedUnitId(null);
        }
    };

    const handleEditTable = (unit: any) => {
        if (!transactionData) {
            showError('Transaction data not loaded');
            return;
        }

        const dataSourceId = transactionData.DataSourceFrom || dataSourceRegisterId;
        if (!dataSourceId) {
            showError('Data source not available');
            return;
        }

        // Check if it's a read-only transaction (query/view units)
        // Matching AngularJS: if AppTransactionData.IsReadOnly, open query editor (view design)
        if (transactionData.IsReadOnly) {
            // Open view design for query units
            if (unit.DataBaseTableName) {
                setViewDesignDialog({
                    isOpen: true,
                    viewName: unit.DataBaseTableName,
                    schemaOwner: unit.SchemaOwner || null
                });
            } else {
                showError('View name not available');
            }
        } else {
            // Open table design for regular units
            if (unit.DataBaseTableName && unit.SchemaOwner) {
                setTableDesignDialog({
                    isOpen: true,
                    tableName: unit.DataBaseTableName,
                    schemaOwner: unit.SchemaOwner
                });
            } else {
                showError('Table name or schema owner not available');
            }
        }
    };

    const handleCloseTableDesign = () => {
        setTableDesignDialog({ isOpen: false, tableName: null, schemaOwner: null });
        // Refresh transaction data after table design changes
        if (transactionId) {
            loadTransactionData();
        }
    };

    const handleCloseViewDesign = () => {
        setViewDesignDialog({ isOpen: false, viewName: null, schemaOwner: null });
        // Refresh transaction data after view design changes
        if (transactionId) {
            loadTransactionData();
        }
    };

    const handleAddUnit = (level: number, parentUnitId?: number | null) => {
        setAddUnitDialog({
            isOpen: true,
            level,
            parentUnitId: parentUnitId || null
        });
    };

    const handleCloseAddUnitDialog = () => {
        setAddUnitDialog({ isOpen: false, level: 1, parentUnitId: null });
    };

    const handleAddExistingTableUnit = () => {
        setTableSelectorDialog({
            isOpen: true,
            level: addUnitDialog.level,
            parentUnitId: addUnitDialog.parentUnitId
        });
    };

    const handleCloseTableSelectorDialog = () => {
        setTableSelectorDialog({ isOpen: false, level: 1, parentUnitId: null });
    };

    const addUnitFromTable = async (tableName: string, schemaOwner: string, level: number, parentUnitId?: number | null) => {
        if (!transactionData) {
            showError('Transaction data not loaded');
            return;
        }

        // Check if unit with this table already exists
        const checkUnitExists = (unitList: any[]): boolean => {
            for (const unit of unitList) {
                if (unit.DataBaseTableName === tableName && unit.SchemaOwner === schemaOwner) {
                    return true;
                }
                if (unit.Children && checkUnitExists(unit.Children)) {
                    return true;
                }
            }
            return false;
        };

        if (checkUnitExists(transactionData.AppTransactionUnitList || [])) {
            showError(`TransactionUnit With Table '${tableName}' Already Exists.`);
            return;
        }

        dispatch(setIsBusy());
        try {
            // Determine parent unit and if adding sibling
            const rootUnit = transactionData.AppTransactionUnitList?.[0];
            let isAddingSiblingUnit = false;
            let parentUnit = null;

            if (level === 1) {
                if (rootUnit && transactionData.TransactionOrganizedType === 1) {
                    // MasterDetail: adding sibling
                    isAddingSiblingUnit = true;
                    parentUnit = rootUnit;
                }
            } else if (level === 2) {
                parentUnit = rootUnit;
            } else if (level === 3) {
                // Find parent child unit
                parentUnit = rootUnit?.Children?.find((u: any) => u.Id === parentUnitId);
            }

            // Convert table to unit
            const converterDto = {
                TableName: tableName,
                SchemaOwner: schemaOwner,
                ParentUnit: parentUnit,
                TransactionId: transactionData.Id,
                DataSourceRegisterId: transactionData.DataSourceFrom || dataSourceRegisterId
            };

            const newUnit = await appTransactionService.convertDbSchemaOwnerTableNameToTransactionUnitExDto(converterDto);

            if (newUnit) {
                // Get table schema for additional data
                const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
                    tableName,
                    transactionData.DataSourceFrom || dataSourceRegisterId,
                    schemaOwner
                );

                // Initialize unit properties
                newUnit.uiId = `unit_${Date.now()}_${Math.random()}`;
                newUnit.MaxRowCount = 800;
                newUnit.masterKeyList = [];
                newUnit.Children = [];

                // Process fields and build masterKeyList
                if (tableData?.Columns && newUnit.AppTransactionFieldList) {
                    tableData.Columns.forEach((columnObj: any) => {
                        const transactionField = newUnit.AppTransactionFieldList.find(
                            (f: any) => f.DataBaseFieldName === columnObj.Name
                        );
                        if (transactionField) {
                            if (columnObj.IsPrimaryKey || columnObj.IsUniqueKey || transactionField.IsPrimaryKey) {
                                newUnit.masterKeyList.push({
                                    RowIdentityGuid: transactionField.RowIdentityGuid,
                                    DataBaseFieldName: transactionField.DataBaseFieldName,
                                    Id: transactionField.Id
                                });
                            }
                        }
                    });
                }

                // Update DictCurrentPKOrFKLinkToParentKeyGuidMap
                if (newUnit.AppTransactionFieldList) {
                    newUnit.AppTransactionFieldList.forEach((field: any) => {
                        if (field.ParentPKFieldGuid && field.RowIdentityGuid) {
                            if (!transactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap) {
                                transactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap = {};
                            }
                            transactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap[field.RowIdentityGuid] = field.ParentPKFieldGuid;
                        }
                    });
                }

                // Add unit to appropriate location
                if (isAddingSiblingUnit && transactionData.TransactionOrganizedType === 1) {
                    // MasterDetail: add as sibling
                    if (transactionData.AppTransactionUnitList.length > 0) {
                        newUnit.level = 1;
                        newUnit.IsMasterSiblingUnit = true;
                        setTransactionData({
                            ...transactionData,
                            AppTransactionUnitList: [...transactionData.AppTransactionUnitList, newUnit]
                        });
                    } else {
                        showError('Please Add Root Unit First.');
                        return;
                    }
                } else {
                    // Add as child
                    if (level === 1) {
                        // Root unit
                        newUnit.level = 1;
                        setTransactionData({
                            ...transactionData,
                            AppTransactionUnitList: [...(transactionData.AppTransactionUnitList || []), newUnit]
                        });
                    } else if (level === 2) {
                        // Child unit
                        if (rootUnit) {
                            newUnit.level = 2;
                            if (!rootUnit.Children) {
                                rootUnit.Children = [];
                            }
                            rootUnit.Children.push(newUnit);
                            setTransactionData({ ...transactionData });
                        }
                    } else if (level === 3) {
                        // Grandchild unit
                        if (parentUnit) {
                            newUnit.level = 3;
                            if (!parentUnit.Children) {
                                parentUnit.Children = [];
                            }
                            parentUnit.Children.push(newUnit);
                            setTransactionData({ ...transactionData });
                        }
                    }
                }

                setIsModified(true);
                showInfo(`Unit added from table '${tableName}' successfully`);
            }
        } catch (error: any) {
            showError(error.message || 'Failed to add unit from table');
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleTableSelected = async (tableName: string, schemaOwner: string) => {
        await addUnitFromTable(tableName, schemaOwner, tableSelectorDialog.level, tableSelectorDialog.parentUnitId);
    };

    const handleAddNewTableUnit = () => {
        if (!transactionData) {
            showError('Transaction data not loaded');
            return;
        }
        const dataSourceId = transactionData.DataSourceFrom || dataSourceRegisterId;
        if (!dataSourceId) {
            showError('Data source not available');
            return;
        }
        setNewTableUnitDialog({
            isOpen: true,
            level: addUnitDialog.level,
            parentUnitId: addUnitDialog.parentUnitId
        });
        setNewTableUnitSaved(null);
        newTableUnitSavedRef.current = null;
    };

    const handleCloseNewTableUnitDialog = () => {
        // Apply only when user closes, and only if a new table was created successfully.
        const saved = newTableUnitSavedRef.current;
        if (saved) {
            addUnitFromTable(saved.tableName, saved.schemaOwner, newTableUnitDialog.level, newTableUnitDialog.parentUnitId);
        }
        setNewTableUnitDialog({ isOpen: false, level: 1, parentUnitId: null });
        setNewTableUnitSaved(null);
        newTableUnitSavedRef.current = null;
    };

    const handleAddVirtualUnit = () => {
        if (!transactionData) {
            showError('Transaction data not loaded');
            return;
        }

        const newUnit = {
            TransactionId: transactionData.Id,
            UnitDisplayName: 'New Virtual Unit',
            AppTransactionFieldList: [],
            Children: [],
            IsSynchToDatabaseTable: false,
            IsReadOnly: false,
            IsMasterSiblingUnit: addUnitDialog.level === 1 && transactionData.TransactionOrganizedType === 1,
            level: addUnitDialog.level,
            uiId: `temp_${Date.now()}_${Math.random()}`,
            IsPrimaryKeyIdentityInsert: true,
            MaxRowCount: 800
        };

        // Add unit to appropriate location
        if (addUnitDialog.level === 1) {
            // Add as master unit or sibling
            if (transactionData.TransactionOrganizedType === 1 && transactionData.AppTransactionUnitList?.length > 0) {
                // MasterDetail: add as sibling
                newUnit.IsMasterSiblingUnit = true;
                setTransactionData({
                    ...transactionData,
                    AppTransactionUnitList: [...(transactionData.AppTransactionUnitList || []), newUnit]
                });
            } else {
                // List or first unit: add as root
                setTransactionData({
                    ...transactionData,
                    AppTransactionUnitList: [...(transactionData.AppTransactionUnitList || []), newUnit]
                });
            }
        } else if (addUnitDialog.level === 2) {
            // Add as child unit
            const rootUnit = transactionData.AppTransactionUnitList?.[0];
            if (rootUnit) {
                if (!rootUnit.Children) {
                    rootUnit.Children = [];
                }
                rootUnit.Children.push(newUnit);
                setTransactionData({ ...transactionData });
            }
        } else if (addUnitDialog.level === 3) {
            // Add as grandchild unit
            const rootUnit = transactionData.AppTransactionUnitList?.[0];
            const parentUnit = rootUnit?.Children?.find((u: any) => u.Id === addUnitDialog.parentUnitId);
            if (parentUnit) {
                if (!parentUnit.Children) {
                    parentUnit.Children = [];
                }
                parentUnit.Children.push(newUnit);
                setTransactionData({ ...transactionData });
            }
        }

        setIsModified(true);
        showInfo('Virtual unit added successfully');
    };

    const handleAddQueryUnit = () => {
        if (!transactionData) {
            showError('Transaction data not loaded');
            return;
        }

        const newUnit = {
            TransactionId: transactionData.Id,
            UnitDisplayName: 'New Query Unit',
            AppTransactionFieldList: [],
            Children: [],
            IsSynchToDatabaseTable: false,
            IsReadOnly: true, // Query units are read-only
            IsMasterSiblingUnit: false,
            level: addUnitDialog.level,
            uiId: `temp_${Date.now()}_${Math.random()}`,
            IsPrimaryKeyIdentityInsert: true,
            MaxRowCount: 800,
            IsDisableAddButton: true,
            IsDisableDeleteButton: true
        };

        // Add unit to appropriate location (same logic as virtual unit)
        if (addUnitDialog.level === 1) {
            setTransactionData({
                ...transactionData,
                AppTransactionUnitList: [...(transactionData.AppTransactionUnitList || []), newUnit]
            });
        } else if (addUnitDialog.level === 2) {
            const rootUnit = transactionData.AppTransactionUnitList?.[0];
            if (rootUnit) {
                if (!rootUnit.Children) {
                    rootUnit.Children = [];
                }
                rootUnit.Children.push(newUnit);
                setTransactionData({ ...transactionData });
            }
        } else if (addUnitDialog.level === 3) {
            const rootUnit = transactionData.AppTransactionUnitList?.[0];
            const parentUnit = rootUnit?.Children?.find((u: any) => u.Id === addUnitDialog.parentUnitId);
            if (parentUnit) {
                if (!parentUnit.Children) {
                    parentUnit.Children = [];
                }
                parentUnit.Children.push(newUnit);
                setTransactionData({ ...transactionData });
            }
        }

        setIsModified(true);
        showInfo('Query unit added successfully');
    };

    // Handle Add To App Menu - generates default search navigation
    const handleAddToAppMenu = async () => {
        try {
            dispatch(setIsBusy());
            
            // First save the transaction if there are unsaved changes (matching AngularJS behavior)
            let savedTransactionId: number | null = null;
            if (isModified) {
                savedTransactionId = await handleSave();
            }

            // Use saved transaction Id if available, otherwise use existing transactionData.Id
            const transactionIdToUse = savedTransactionId || transactionData?.Id;
            
            if (!transactionIdToUse) {
                showError('Transaction must be saved before adding to app menu');
                return;
            }

            // Call API to generate default search navigation
            const result = await schemaMetadataService.quickGenerateTransactionDefaultSearchNavigation(transactionIdToUse);

            // Only show the last message from server's ValidationResult if it exists
            if (result?.ValidationResult?.Items && result.ValidationResult.Items.length > 0) {
                // Get the last item (most recent message from server)
                const lastItem = result.ValidationResult.Items[result.ValidationResult.Items.length - 1];
                
                // Create a ValidationResult with only the last item
                const lastMessageResult = {
                    Items: [lastItem]
                };
                
                showValidationMessages(lastMessageResult, true);
            } else if (result?.IsSuccessful) {
                // Only show manual success message if server didn't send any messages
                showInfo('Transaction added to app menu successfully');
            }

            if (result?.IsSuccessful) {
                // Refresh transaction data to get updated DefaultNavigationMenuId and FormId
                // Always refresh using transactionIdToUse (works for both new and existing transactions)
                const currentTransactionId = transactionIdToUse;
                if (currentTransactionId) {
                    try {
                        const savedData = await appTransactionService.getOneHierarchyTransaction(
                            currentTransactionId.toString(),
                            false,
                            '',
                            '',
                            '',
                            false,
                            ''
                        );
                        
                        if (savedData) {
                            // Initialize DictDeletedItemsIds (matching AngularJS behavior)
                            if (!savedData.DictDeletedItemsIds) {
                                savedData.DictDeletedItemsIds = {};
                            }
                            if (!savedData.DictDeletedItemsIds.AppTransactionUnitList) {
                                savedData.DictDeletedItemsIds.AppTransactionUnitList = [];
                            }
                            
                            // Initialize DictDeletedItemsIds for each unit's field list
                            if (savedData.AppTransactionUnitList && Array.isArray(savedData.AppTransactionUnitList)) {
                                savedData.AppTransactionUnitList.forEach((unit: any) => {
                                    if (unit.Id) {
                                        const fieldListKey = `AppTransactionFieldList_${unit.Id}`;
                                        if (!savedData.DictDeletedItemsIds[fieldListKey]) {
                                            savedData.DictDeletedItemsIds[fieldListKey] = [];
                                        }
                                        
                                        // Process child units
                                        if (unit.Children && Array.isArray(unit.Children)) {
                                            unit.Children.forEach((childUnit: any) => {
                                                if (childUnit.Id) {
                                                    const childFieldListKey = `AppTransactionFieldList_${childUnit.Id}`;
                                                    if (!savedData.DictDeletedItemsIds[childFieldListKey]) {
                                                        savedData.DictDeletedItemsIds[childFieldListKey] = [];
                                                    }
                                                    
                                                    // Process grandchild units
                                                    if (childUnit.Children && Array.isArray(childUnit.Children)) {
                                                        childUnit.Children.forEach((grandchildUnit: any) => {
                                                            if (grandchildUnit.Id) {
                                                                const grandchildFieldListKey = `AppTransactionFieldList_${grandchildUnit.Id}`;
                                                                if (!savedData.DictDeletedItemsIds[grandchildFieldListKey]) {
                                                                    savedData.DictDeletedItemsIds[grandchildFieldListKey] = [];
                                                                }
                                                            }
                                                        });
                                                    }
                                                }
                                            });
                                        }
                                    }
                                });
                            }
                            
                            setTransactionData(savedData);
                            
                            // Load all transaction fields for Communication User List Field dropdown
                            if (savedData.AppTransactionUnitList && savedData.AppTransactionUnitList.length > 0) {
                                const allFields: Array<{ Id: number; longDisplay: string }> = [];
                                savedData.AppTransactionUnitList.forEach((unit: any) => {
                                    if (unit.AppTransactionFieldList) {
                                        unit.AppTransactionFieldList.forEach((field: any) => {
                                            allFields.push({
                                                Id: field.Id,
                                                longDisplay: field.longDisplay || field.DisplayName || field.FieldName
                                            });
                                        });
                                    }
                                });
                                setAllTransactionFields(allFields);
                            }
                            
                            // Update formData with latest values
                            setFormData({
                                TransactionName: savedData.TransactionName || '',
                                TransactionOrganizedType: savedData.TransactionOrganizedType || null,
                                EmGrandChildEditMode: savedData.EmGrandChildEditMode || null,
                                PreSaveValidationMethod: savedData.PreSaveValidationMethod || '',
                                IsForPublicAcesss: savedData.IsForPublicAcesss || false,
                                TransactionFileStorageRootFolderId: savedData.TransactionFileStorageRootFolderId || null,
                                TransactionFileStorageRootFolderName: savedData.TransactionFileStorageRootFolderName || '',
                                IsNeedToSetCriticalPathTrackFlow: savedData.IsNeedToSetCriticalPathTrackFlow || false,
                                IsExclusiveForOwner: savedData.IsExclusiveForOwner || false,
                                IsNeedToSetComunication: savedData.IsNeedToSetComunication || false,
                                IsShowSaveButton: savedData.IsShowSaveButton || false,
                                IsShowCalculateButton: savedData.IsShowCalculateButton || false,
                                IsShowPrintButton: savedData.IsShowPrintButton || false,
                                ConversationBoxDockPosition: savedData.ConversationBoxDockPosition || null,
                                CommunicationGroupByType: savedData.OtherOptions?.CommunicationGroupByType || null,
                                CommunicationToUserIdTransField: savedData.OtherOptions?.CommunicationToUserIdTransField || null,
                                IsOpenCommunicationByDefault: savedData.OtherOptions?.IsOpenCommunicationByDefault || false,
                                IsEnableSaveByApiCall: savedData.OtherOptions?.IsEnableSaveByApiCall || false,
                                SaveByApiCallDataTransferId: savedData.OtherOptions?.SaveByApiCallDataTransferId || null,
                                FolderUsageType: savedData.FolderUsageType ?? null,
                                ApiIntegrationTransactionCrudType: savedData.OtherOptions?.ApiIntegrationTransactionCrudType ?? null,
                                IsEnableDeleteByApi: savedData.OtherOptions?.IsEnableDeleteByApi || false,
                                DeleteDataTransferId: savedData.OtherOptions?.DeleteDataTransferId ?? null
                            });
                            setIsModified(savedData.IsModified || false);
                        }
                    } catch (refreshError: any) {
                        console.error('Error refreshing transaction data after adding to menu:', refreshError);
                        // Don't show error to user, just log it
                    }
                } else if (transactionId) {
                    // Fallback to original method if transactionId prop exists
                    loadTransactionData();
                }
                
                // Refresh left menu by reloading user menu from server
                try {
                    const userMenu = await adminSvc.retrieveUserTreeMenu();
                    dispatch(setUserMenu(userMenu));
                } catch (menuError: any) {
                    console.error('Error refreshing user menu:', menuError);
                    // Don't show error to user, just log it
                }
                
                // Trigger menu refresh if onRefresh is available
                if (onRefresh) {
                    onRefresh();
                }
            } else if (!result?.ValidationResult?.Items || result.ValidationResult.Items.length === 0) {
                // Only show error if server didn't send any validation messages
                showError('Failed to add transaction to app menu');
            }
        } catch (error: any) {
            showError(error.message || 'Failed to add transaction to app menu');
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    // Handle Add List/FolderList Transaction To Main Menu (Angular: AddListTransactionToMainMenu)
    const handleAddListTransactionToMainMenu = async () => {
        const type = formData.TransactionOrganizedType ?? transactionData?.TransactionOrganizedType;
        const isListOrFolderList = type === 3 || type === 5;
        if (!transactionData?.Id || !isListOrFolderList) return;

        const confirmed = await showConfirm(
            'Do you want to add a main menu shortcut for this transaction?'
        );
        if (!confirmed) return;

        try {
            dispatch(setIsBusy());
            let idToUse = transactionData.Id;
            if (isModified) {
                const savedId = await handleSave();
                if (savedId != null) idToUse = savedId;
            }
            const menuName = formData.TransactionName || transactionData?.TransactionName || '';
            const result = await adminSvc.addListTransactionToMainMenu(
                String(idToUse),
                menuName
            );

            if (result?.ValidationResult?.Items && result.ValidationResult.Items.length > 0) {
                showValidationMessages(result.ValidationResult, true);
            } else if (result?.IsSuccessful) {
                showInfo('Main menu shortcut added successfully');
            }

            if (result?.IsSuccessful) {
                try {
                    const userMenu = await adminSvc.retrieveUserTreeMenu();
                    dispatch(setUserMenu(userMenu));
                } catch (menuError: any) {
                    console.error('Error refreshing user menu:', menuError);
                }
                if (onRefresh) onRefresh();
            } else if (!result?.ValidationResult?.Items || result.ValidationResult.Items.length === 0) {
                showError('Failed to add transaction to menu');
            }
        } catch (error: any) {
            showError(error?.message || 'Failed to add transaction to menu');
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    // Handle Run Test - opens transaction form in preview mode in a semi-full screen popup
    const handleRunTest = () => {
        if (!transactionData || !transactionData.Id) {
            showError('Transaction must be saved before running test');
            return;
        }

        const transactionType = formData.TransactionOrganizedType || transactionData.TransactionOrganizedType;
        const transactionName = formData.TransactionName || transactionData.TransactionName || 'Transaction';

        if (transactionType === 1 || transactionType === 3 || transactionType === 5) {
            // Open in semi-full screen popup
            setPreviewPopup({
                isOpen: true,
                transactionId: transactionData.Id,
                transactionType: transactionType,
                transactionName: transactionName
            });
        } else {
            showError('Transaction type not supported for preview');
        }
    };

    const handleClosePreview = () => {
        setPreviewPopup({
            isOpen: false,
            transactionId: null,
            transactionType: null,
            transactionName: null
        });
    };

    // Open API JSON preview: use existing data if present, else fetch by selected API Data Source (so preview works before save)
    const handleOpenApiJsonPreview = () => {
        setApiJsonPreviewOpen(true);
        const existing = transactionData?.BaseApiConfigDto?.JsonSampleData;
        if (existing) {
            setApiJsonPreviewData(null);
            setApiJsonPreviewLoading(false);
            return;
        }
        if (formData.FolderUsageType == null) {
            setApiJsonPreviewData(null);
            setApiJsonPreviewLoading(false);
            return;
        }
        setApiJsonPreviewLoading(true);
        setApiJsonPreviewData(null);
        integrationService
            .retrieveOneAppIntegrationSettingParameterExDto(String(formData.FolderUsageType), true)
            .then((res: any) => {
                setApiJsonPreviewData(res?.JsonSampleData ?? null);
            })
            .catch(() => {
                showError('Failed to load API JSON sample data');
            })
            .finally(() => {
                setApiJsonPreviewLoading(false);
            });
    };

    const handleCloseApiJsonPreview = () => {
        setApiJsonPreviewOpen(false);
        setApiJsonPreviewData(null);
    };

    // Edit Operation: open API operation editor in new tab (Angular: IntegrationSettingParameterEditor)
    const handleEditApiOperation = () => {
        if (formData.FolderUsageType == null) return;
        addTabAndNavigate('/third-party-api-editor', 'Edit Operation', { id: formData.FolderUsageType }, true);
    };

    const handleApiStructureNodeSelectChange = (pathKey: string, checked: boolean) => {
        if (!transactionData?.ApiDataStructure) return;
        setTransactionData((prev: any) => ({
            ...prev,
            ApiDataStructure: cloneTreeAndSetSelected(prev.ApiDataStructure || [], pathKey, checked)
        }));
        setIsModified(true);
    };

    // Add Selected Node As Units: call backend to generate units from selected API nodes (Angular: addSelectedApiNodesAsUnits)
    const handleAddSelectedApiNodesAsUnits = async () => {
        if (!transactionData?.ApiDataStructure) {
            showError('No API Data Structure loaded. Select Provider API Data Source and save, or open an existing API Data Model.');
            return;
        }
        try {
            dispatch(setIsBusy());
            const dataToSend = {
                ...transactionData,
                ...formData,
                OtherOptions: {
                    ...transactionData?.OtherOptions,
                    CommunicationGroupByType: formData.CommunicationGroupByType,
                    CommunicationToUserIdTransField: formData.CommunicationToUserIdTransField,
                    IsOpenCommunicationByDefault: formData.IsOpenCommunicationByDefault,
                    IsEnableSaveByApiCall: formData.IsEnableSaveByApiCall,
                    SaveByApiCallDataTransferId: formData.SaveByApiCallDataTransferId,
                    ApiIntegrationTransactionCrudType: formData.ApiIntegrationTransactionCrudType,
                    IsEnableDeleteByApi: formData.IsEnableDeleteByApi,
                    DeleteDataTransferId: formData.DeleteDataTransferId
                }
            };
            prepareSaveData(dataToSend);
            const result = await appTransactionService.generateUnitsFromSelectedApiNodes(dataToSend);
            if (result?.ValidationResult && !result.ValidationResult.IsValid) {
                showValidationMessages(result.ValidationResult, true);
            }
            if (result?.IsSuccessful && result?.Object) {
                const data = result.Object;
                if (!data.DictDeletedItemsIds) data.DictDeletedItemsIds = {};
                if (!data.DictDeletedItemsIds.AppTransactionUnitList) data.DictDeletedItemsIds.AppTransactionUnitList = [];
                if (data.AppTransactionUnitList?.length) {
                    data.AppTransactionUnitList.forEach((unit: any) => {
                        if (unit.Id && !data.DictDeletedItemsIds[`AppTransactionFieldList_${unit.Id}`]) {
                            data.DictDeletedItemsIds[`AppTransactionFieldList_${unit.Id}`] = [];
                        }
                        unit.Children?.forEach((child: any) => {
                            if (child.Id && !data.DictDeletedItemsIds[`AppTransactionFieldList_${child.Id}`]) {
                                data.DictDeletedItemsIds[`AppTransactionFieldList_${child.Id}`] = [];
                            }
                            child.Children?.forEach((gc: any) => {
                                if (gc.Id && !data.DictDeletedItemsIds[`AppTransactionFieldList_${gc.Id}`]) {
                                    data.DictDeletedItemsIds[`AppTransactionFieldList_${gc.Id}`] = [];
                                }
                            });
                        });
                    });
                }
                setTransactionData(data);
                setFormData({
                    TransactionName: data.TransactionName || '',
                    TransactionOrganizedType: data.TransactionOrganizedType ?? null,
                    EmGrandChildEditMode: data.EmGrandChildEditMode ?? null,
                    PreSaveValidationMethod: data.PreSaveValidationMethod || '',
                    IsForPublicAcesss: data.IsForPublicAcesss || false,
                    TransactionFileStorageRootFolderId: data.TransactionFileStorageRootFolderId ?? null,
                    TransactionFileStorageRootFolderName: data.TransactionFileStorageRootFolderName || '',
                    IsNeedToSetCriticalPathTrackFlow: data.IsNeedToSetCriticalPathTrackFlow || false,
                    IsExclusiveForOwner: data.IsExclusiveForOwner || false,
                    IsNeedToSetComunication: data.IsNeedToSetComunication || false,
                    IsShowSaveButton: data.IsShowSaveButton || false,
                    IsShowCalculateButton: data.IsShowCalculateButton || false,
                    IsShowPrintButton: data.IsShowPrintButton || false,
                    ConversationBoxDockPosition: data.ConversationBoxDockPosition ?? null,
                    CommunicationGroupByType: data.OtherOptions?.CommunicationGroupByType ?? null,
                    CommunicationToUserIdTransField: data.OtherOptions?.CommunicationToUserIdTransField ?? null,
                    IsOpenCommunicationByDefault: data.OtherOptions?.IsOpenCommunicationByDefault || false,
                    IsEnableSaveByApiCall: data.OtherOptions?.IsEnableSaveByApiCall || false,
                    SaveByApiCallDataTransferId: data.OtherOptions?.SaveByApiCallDataTransferId ?? null,
                    FolderUsageType: data.FolderUsageType ?? null,
                    ApiIntegrationTransactionCrudType: data.OtherOptions?.ApiIntegrationTransactionCrudType ?? null,
                    IsEnableDeleteByApi: data.OtherOptions?.IsEnableDeleteByApi || false,
                    DeleteDataTransferId: data.OtherOptions?.DeleteDataTransferId ?? null
                });
                setIsModified(true);
            }
        } catch (error: any) {
            showError(error?.message || 'Failed to add selected nodes as units');
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleFieldLinkChange = (unit: any, field: any, parentPkField: any | null, isRemove: boolean) => {
        if (!transactionData) {
            showError('Transaction data not loaded');
            return;
        }

        // Create a new DictCurrentPKOrFKLinkToParentKeyGuidMap object to ensure React detects the change
        const newDict = { ...(transactionData.DictCurrentPKOrFKLinkToParentKeyGuidMap || {}) };

        if (isRemove) {
            // Remove link
            field.IsLinkToParentPrimaryKey = false;
            field.ParentPKFieldGuid = null;
            
            // Remove from DictCurrentPKOrFKLinkToParentKeyGuidMap
            if (field.RowIdentityGuid) {
                delete newDict[field.RowIdentityGuid];
            }
        } else if (parentPkField) {
            // Add link
            field.IsLinkToParentPrimaryKey = true;
            field.ParentPKFieldGuid = parentPkField.RowIdentityGuid || parentPkField.Id;
            
            // Add to DictCurrentPKOrFKLinkToParentKeyGuidMap
            if (field.RowIdentityGuid) {
                newDict[field.RowIdentityGuid] = field.ParentPKFieldGuid;
            }
        }

        // Update transaction data with new DictCurrentPKOrFKLinkToParentKeyGuidMap
        setTransactionData({ 
            ...transactionData,
            DictCurrentPKOrFKLinkToParentKeyGuidMap: newDict
        });
        setIsModified(true);
    };

    // Determine isPhysicalModelTableCreated
    // Matching AngularJS logic (line 362-370):
    // - If isCreateDtoDataModel or isCreateApiDataModel: no physical DB table
    // - Else (creating from database): IsPhysicalModelTableCreated = true
    // - Otherwise: use value from transactionData
    const isPhysicalModelTableCreated = React.useMemo(() => {
        // Priority 1: If creating DTO or API model, no physical table
        if (isCreateNewItem && (isCreateDtoDataModel || isCreateApiDataModel)) {
            return false;
        }
        // Priority 2: If creating new item (not DTO/API), from database
        if (isCreateNewItem && !isCreateDtoDataModel) {
            return true;
        }
        // Priority 3: Use value from transactionData if it exists and is a boolean
        if (transactionData?.IsPhysicalModelTableCreated !== undefined && transactionData?.IsPhysicalModelTableCreated !== null) {
            return Boolean(transactionData.IsPhysicalModelTableCreated);
        }
        // Priority 4: Default to false
        return false;
    }, [isCreateNewItem, isCreateDtoDataModel, isCreateApiDataModel, transactionData]);
    
    // Show Main Properties for both branches: physical table (Block A) or non-physical e.g. Rest API (Block B)
    const shouldShowMainProperties = isCreateNewItem || !!transactionData;
    const isMasterDetail = formData.TransactionOrganizedType === 1;
    // Include creation-type prop so UI hides/shows correctly before first save
    const isApiIntegrationTransaction = Boolean(
        transactionData?.OtherOptions?.IsApiIntegrationTransaction || isCreateApiDataModel
    );
    // Show "Add from table" / "Create DDL" only when physical table and not API model
    const showDatabaseTableActions = isPhysicalModelTableCreated && !isApiIntegrationTransaction;

    return (
        <div className="h-full flex flex-col gap-1">
            {/* Toolbar */}
            <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
                <div className="flex items-center gap-2">
                    <div className={`text-md font-semibold ${theme.title}`}>
                        Data Model Design


                    </div>
                    {formData.TransactionName && (
                        <div className={`text-md font-semibold ${theme.title}`}>
                            : <span className="ml-2">{formData.TransactionName}</span>
                        </div>
                    )}
                </div>
                <div className="flex items-center space-x-2">
                    <button
                        onClick={handleRefresh}
                        className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                        title="Refresh"
                    >
                        <i className="fa-solid fa-rotate"></i>
                        <span>Refresh</span>
                    </button>

                    <button
                        onClick={handleSave}
                        disabled={(!transactionData && !isCreateNewItem) || (transactionData?.Id && !isModified)}
                        className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1 disabled:opacity-50 disabled:cursor-not-allowed`}
                        title="Save"
                    >
                        <i className="fa-solid fa-floppy-disk"></i>
                        <span>Save</span>
                    </button>

                    {/* Design Form - Angular: MasterDetail && (IsPhysicalModelTableCreated || IsApiIntegrationTransaction) */}
                    {isMasterDetail && (isPhysicalModelTableCreated || isApiIntegrationTransaction) && transactionData?.FormId && (
                        <button
                            onClick={() => onOpenFormDesign?.()}
                            className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                            title="Design Form"
                        >
                            <i className="fa-regular fa-file"></i>
                            <span>Design Form</span>
                        </button>
                    )}
                    {/* Design Form dropdown when no FormId - Angular: Auto Design Form / Design On Blank Form */}
                    {isMasterDetail && (isPhysicalModelTableCreated || isApiIntegrationTransaction) && !transactionData?.FormId && (
                        <div className="relative" ref={designFormDropdownRef}>
                            <button
                                onClick={() => setDesignFormDropdownOpen((v) => !v)}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                title="Design Form"
                            >
                                <i className="fa-regular fa-file"></i>
                                <span>Design Form</span>
                                <i className="fa-solid fa-caret-down text-[10px]"></i>
                            </button>
                            {designFormDropdownOpen && (
                                <div className={`absolute left-0 top-full mt-1 min-w-[180px] py-1 rounded shadow-lg z-50 border ${theme.mainContentSection}`}>
                                    <button
                                        type="button"
                                        onClick={() => { onOpenFormDesign?.(true); setDesignFormDropdownOpen(false); }}
                                        className={`w-full text-left px-3 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700 ${theme.label}`}
                                    >
                                        Auto Design Form
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => { onOpenFormDesign?.(false); setDesignFormDropdownOpen(false); }}
                                        className={`w-full text-left px-3 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700 ${theme.label}`}
                                    >
                                        Design On Blank Form
                                    </button>
                                </div>
                            )}
                        </div>
                    )}

                    {/* API Settings dropdown */}
                    <div className="relative">
                        <button
                            type="button"
                            onClick={(e) => { e.stopPropagation(); setApiSettingsDropdownOpen((v) => !v); }}
                            className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                            title="API Settings"
                        >
                            <i className="fa-solid fa-gears"></i>
                            <span>API Settings</span>
                            <i className="fa-solid fa-caret-down text-[10px]"></i>
                        </button>
                        {apiSettingsDropdownOpen && (
                            <>
                                <div className="fixed inset-0 z-[9998]" onClick={() => setApiSettingsDropdownOpen(false)} aria-hidden />
                                <div className={`absolute left-0 top-full mt-1 py-1 min-w-[260px] rounded shadow-lg border z-[9999] ${theme.mainContentSection}`}>
                                    <button
                                        type="button"
                                        className="w-full text-left px-3 py-2 text-xs hover:bg-black/5"
                                        onClick={() => {
                                            setApiSettingsDropdownOpen(false);
                                            setApiSettingPopup({ isOpen: true, mode: 'Consume' });
                                        }}
                                    >
                                        Edit Consume API: Call 3rd Part API
                                    </button>
                                    <button
                                        type="button"
                                        className="w-full text-left px-3 py-2 text-xs hover:bg-black/5"
                                        onClick={() => {
                                            setApiSettingsDropdownOpen(false);
                                            setApiSettingPopup({ isOpen: true, mode: 'Provide' });
                                        }}
                                    >
                                        Edit Provide API: Provide API To 3rd Part
                                    </button>
                                </div>
                            </>
                        )}
                    </div>

                    {/* Link To Other Pages dropdown (Default: Root Master Unit) */}
                    <div className="relative">
                        <button
                            type="button"
                            onClick={() => setLinkTargetDropdownOpen((o) => !o)}
                            className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                            title="Link To Other Pages"
                        >
                            <i className="fa-solid fa-clipboard"></i>
                            <span>Link To Other Pages</span>
                            <i className="fa-solid fa-caret-down text-[10px]"></i>
                        </button>
                        {linkTargetDropdownOpen && (
                            <>
                                <div className="fixed inset-0 z-[9998]" onClick={() => setLinkTargetDropdownOpen(false)} aria-hidden />
                                <div className={`absolute right-0 top-full mt-1 py-1 min-w-[220px] rounded shadow-lg border z-[9999] ${theme.mainContentSection}`}>
                                    <button
                                        type="button"
                                        className="w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5"
                                        onClick={() => {
                                            setLinkTargetDropdownOpen(false);
                                            const unit = transactionData?.AppTransactionUnitList?.[0] ?? null;
                                            if (!unit) return showInfo('Root unit not available.');
                                            handleUnitNavigate(unit, 101);
                                        }}
                                    >
                                        <span className="w-4 inline-flex items-center justify-center">
                                            <i className="fa-solid fa-file-lines" />
                                        </span>
                                        Navigate To Data Model
                                    </button>
                                    <button
                                        type="button"
                                        className="w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5"
                                        onClick={() => {
                                            setLinkTargetDropdownOpen(false);
                                            const unit = transactionData?.AppTransactionUnitList?.[0] ?? null;
                                            if (!unit) return showInfo('Root unit not available.');
                                            handleUnitNavigate(unit, 102);
                                        }}
                                    >
                                        <span className="w-4 inline-flex items-center justify-center">
                                            <i className="fa-solid fa-magnifying-glass" />
                                        </span>
                                        Navigate To Search
                                    </button>
                                    <button
                                        type="button"
                                        className="w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5"
                                        onClick={() => {
                                            setLinkTargetDropdownOpen(false);
                                            const unit = transactionData?.AppTransactionUnitList?.[0] ?? null;
                                            if (!unit) return showInfo('Root unit not available.');
                                            handleUnitNavigate(unit, 5);
                                        }}
                                    >
                                        <span className="w-4 inline-flex items-center justify-center">
                                            <i className="fa-solid fa-file" />
                                        </span>
                                        Navigate To Any Page
                                    </button>
                                </div>
                            </>
                        )}
                    </div>

                    {/* Add To App Menu - Angular: !DefaultNavigationMenuId && MasterDetail && !IsApiIntegrationTransaction */}
                    {isMasterDetail &&
                        !isApiIntegrationTransaction &&
                        !transactionData?.DefaultNavigationMenuId && (
                            <button
                                onClick={handleAddToAppMenu}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                title="Add To App Menu"
                            >
                                <i className="fa-solid fa-list-ul"></i>
                                <span>Add To App Menu</span>
                            </button>
                        )}

                    {/* Add To Main Menu - List/FolderList (Angular: AddListTransactionToMainMenu) */}
                    {transactionData?.Id &&
                        ((formData.TransactionOrganizedType ?? transactionData?.TransactionOrganizedType) === 3 ||
                            (formData.TransactionOrganizedType ?? transactionData?.TransactionOrganizedType) === 5) && (
                            <button
                                onClick={handleAddListTransactionToMainMenu}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                title="Add To Main Menu"
                            >
                                <i className="fa-solid fa-plus" aria-hidden />
                                <i className="fa-solid fa-bars text-[10px] relative -left-1 top-0.5" aria-hidden />
                                <span>Add To Main Menu</span>
                            </button>
                        )}

                    {/* Edit Navigation Search - Angular: DefaultNavigationSearchId && MasterDetail && !unitEditPopup */}
                    {isMasterDetail &&
                        !isApiIntegrationTransaction &&
                        transactionData?.DefaultNavigationSearchId && (
                            <div className="relative">
                                <button
                                    className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                    title="Edit Navigation Search"
                                >
                                    <i className="fa-solid fa-list-ul"></i>
                                    <span>Edit Navigation Search</span>
                                    <i className="fa-solid fa-caret-down text-[10px]"></i>
                                </button>
                                {/* TODO: dropdown Edit Navigation Search / Reset Search Dataset */}
                            </div>
                        )}

                    {/* Run Test - Angular: Id && (List || MasterDetail); no FormId required for List */}
                    {transactionData?.Id &&
                        (formData.TransactionOrganizedType === 1 || formData.TransactionOrganizedType === 3) && (
                            <button
                                onClick={handleRunTest}
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                title="Run Test"
                            >
                                <i className="fa-solid fa-eye"></i>
                                <span>Run Test</span>
                            </button>
                        )}
                </div>
            </div>

            {/* Content Area */}
            <div className={`h-1 flex-auto overflow-hidden flex flex-col p-2 gap-2 ${theme.mainContentSection}`}>
                {/* Properties Configuration Section - Collapsed by default, compact */}
                {shouldShowMainProperties && (
                    <div className="flex-none">
                        <CollapsibleSection
                            title="Main Properties"
                            isCollapsed={isSectionCollapsed('properties')}
                            onToggle={() => toggleSection('properties')}
                            defaultCollapsed={false}
                            collapsedLabel="click to expand"
                        >
                            <div className="max-h-[400px] overflow-y-auto">
                                <div className="flex gap-3 overflow-x-auto">
                                    {/* Block A: IsPhysicalModelTableCreated - full config */}
                                    {isPhysicalModelTableCreated ? (
                                    <>
                                    {/* Section 1: Data Model Configuration - Two Columns */}
                                    <div className={`p-3 border rounded-lg ${theme.mainContentSection} flex-none min-w-[400px]`}>
                                        <div className="flex gap-4">
                                            <div className="flex-1 space-y-2">
                                                <div className="flex items-center">
                                                    <div className={`text-xs w-[140px] ${theme.label}`}>
                                                        Data Model Name
                                                    </div>
                                                    <div>
                                                        <input
                                                            type="text"
                                                            value={formData.TransactionName}
                                                            onChange={(e) => handleFieldChange('TransactionName', e.target.value)}
                                                            className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                            maxLength={200}
                                                        />
                                                    </div>
                                                </div>
                        <div className="flex items-center">
                          <div className={`text-xs w-[140px] ${theme.label}`}>
                            Data Model Type
                          </div>
                          <div>
                            <select
                              value={formData.TransactionOrganizedType || ''}
                              disabled
                              className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                            >
                              {transactionOrganizedTypeOptions.map(opt => (
                                <option key={opt.Id} value={opt.Id}>
                                  {opt.Display}
                                </option>
                              ))}
                            </select>
                          </div>
                        </div>
                        <div className="flex items-center">
                          <div className={`text-xs w-[140px] ${theme.label}`}>
                            File Storage Folder
                          </div>
                          <div>
                            <div className="relative" style={{ width: '200px' }}>
                              <input
                                type="text"
                                value={formData.TransactionFileStorageRootFolderName}
                                readOnly
                                className={`w-full px-2 py-1 pr-[35px] text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                              />
                              <button
                                className={`absolute top-0 right-0 p-1 bg-transparent hover:bg-transparent border-none hover:border-none ${theme.button_default} rounded-[4px] text-xs`}
                                title="Select Folder"
                                style={{ padding: '4px' }}
                              >
                                <i className="fa-regular fa-folder text-xs"></i>
                              </button>
                              {formData.TransactionFileStorageRootFolderId && (
                                <button
                                  onClick={() => handleFieldChange('TransactionFileStorageRootFolderId', null)}
                                  className={`absolute top-0 right-[19px] p-1 bg-transparent hover:bg-transparent border-none hover:border-none${theme.button_default} rounded-[4px] text-xs`}
                                  title="Clear"
                                  style={{ padding: '4px' }}
                                >
                                  <i className="fa-solid fa-times text-[10px]"></i>
                                </button>
                              )}
                            </div>
                          </div>
                        </div>
                                            </div>
                                            {/* Column 2: Edit Mode & Validation */}
                                            <div className="flex-1 space-y-2">
                                                <div className="flex items-center">
                                                    <div className={`text-xs w-[160px] ${theme.label}`}>
                                                        Grandchild Edit Mode
                                                    </div>
                                                    <div>
                                                        <select
                                                            value={formData.EmGrandChildEditMode || ''}
                                                            onChange={(e) => handleFieldChange('EmGrandChildEditMode', e.target.value ? parseInt(e.target.value) : null)}
                                                            className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                        >
                                                            <option value="">Select...</option>
                                                            {grandChildEditModeOptions.map(opt => (
                                                                <option key={opt.Id} value={opt.Id}>
                                                                    {opt.Display}
                                                                </option>
                                                            ))}
                                                        </select>
                                                    </div>
                                                </div>
                                                <div className="flex items-center">
                                                    <div className={`text-xs w-[160px] ${theme.label}`}>
                                                        PreSave Validation Method
                                                    </div>
                                                    <div>
                                                        <input
                                                            type="text"
                                                            value={formData.PreSaveValidationMethod}
                                                            onChange={(e) => handleFieldChange('PreSaveValidationMethod', e.target.value)}
                                                            className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                            minLength={5}
                                                            maxLength={200}
                                                        />
                                                    </div>
                                                </div>
                                                {!isApiIntegrationTransaction && (
                                                    <div className="flex items-center">
                                                        <div className={`text-xs w-[160px] ${theme.label}`}>
                                                            Enable Publish Data To API
                                                        </div>
                                                        <div>
                                                            <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                                <input
                                                                    type="checkbox"
                                                                    checked={formData.IsEnableSaveByApiCall}
                                                                    onChange={(e) => handleFieldChange('IsEnableSaveByApiCall', e.target.checked)}
                                                                    className="rounded"
                                                                />
                                                                {formData.IsEnableSaveByApiCall && (
                                                                    <span className="ml-2">
                                                                        {formData.SaveByApiCallDataTransferId && transactionData?.ConsumApiDataModelSaveSettingDto ? (
                                                                            <button
                                                                                type="button"
                                                                                onClick={() => {
                                                                                    // TODO: Open API save setting popup
                                                                                }}
                                                                                className={`text-xs underline ${theme.label} bg-transparent border-none cursor-pointer p-0`}
                                                                                style={{ paddingLeft: '10px', textDecoration: 'underline' }}
                                                                            >
                                                                                <i className="fa-solid fa-pencil"></i> Click To Config The Post Setting
                                                                            </button>
                                                                        ) : (
                                                                            <button
                                                                                type="button"
                                                                                onClick={() => {
                                                                                    // TODO: Show dropdown menu for "Start To Generate Setting"
                                                                                }}
                                                                                className={`text-xs underline ${theme.label} bg-transparent border-none cursor-pointer p-0`}
                                                                                style={{ paddingLeft: '10px', textDecoration: 'underline' }}
                                                                            >
                                                                                Start To Generate Setting
                                                                            </button>
                                                                        )}
                                                                    </span>
                                                                )}
                                                            </label>
                                                        </div>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    </div>

                                    {/* Section 2: Feature Enablement and Button Visibility - Multiple Columns */}
                                    <div className={`p-3 border rounded-lg ${theme.mainContentSection} flex-none min-w-[350px]`}>
                                        <div className="flex gap-4">
                                            {/* Column 1: Feature Enablement */}
                                            <div className="flex-1 space-y-2">
                                                {isMasterDetail && (
                                                    <>
                                                        <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                            <input
                                                                type="checkbox"
                                                                checked={formData.IsNeedToSetCriticalPathTrackFlow}
                                                                onChange={(e) => handleFieldChange('IsNeedToSetCriticalPathTrackFlow', e.target.checked)}
                                                                className="rounded"
                                                            />
                                                            <span>Enable CPF Flow</span>
                                                        </label>
                                                        <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                            <input
                                                                type="checkbox"
                                                                checked={formData.IsExclusiveForOwner}
                                                                onChange={(e) => handleFieldChange('IsExclusiveForOwner', e.target.checked)}
                                                                className="rounded"
                                                            />
                                                            <span>Is Exclusive For Owner</span>
                                                        </label>
                                                        <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                            <input
                                                                type="checkbox"
                                                                checked={formData.IsForPublicAcesss}
                                                                onChange={(e) => handleFieldChange('IsForPublicAcesss', e.target.checked)}
                                                                className="rounded"
                                                            />
                                                            <span>Is For Public Acesss</span>
                                                        </label>
                                                    </>
                                                )}
                                            </div>
                                            {/* Column 2: Button Visibility */}
                                            {isMasterDetail && (
                                                <div className="flex-1 space-y-2">
                                                    <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                        <input
                                                            type="checkbox"
                                                            checked={formData.IsShowSaveButton}
                                                            onChange={(e) => handleFieldChange('IsShowSaveButton', e.target.checked)}
                                                            className="rounded"
                                                        />
                                                        <span>Is Show Save Button</span>
                                                    </label>
                                                    <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                        <input
                                                            type="checkbox"
                                                            checked={formData.IsShowCalculateButton}
                                                            onChange={(e) => handleFieldChange('IsShowCalculateButton', e.target.checked)}
                                                            className="rounded"
                                                        />
                                                        <span>Is Show Calculate Button</span>
                                                    </label>
                                                    <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                        <input
                                                            type="checkbox"
                                                            checked={formData.IsShowPrintButton}
                                                            onChange={(e) => handleFieldChange('IsShowPrintButton', e.target.checked)}
                                                            className="rounded"
                                                        />
                                                        <span>Is Show Print Button</span>
                                                    </label>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {/* Section 3: Communication Settings - MasterDetail only */}
                                    {isMasterDetail && (
                                    <div className={`p-3 border rounded-lg ${theme.mainContentSection} flex-none ${formData.IsNeedToSetComunication ? 'min-w-[400px]' : 'min-w-[200px]'}`}>
                                        <div className={`flex gap-4 ${formData.IsNeedToSetComunication ? '' : 'justify-start'}`}>
                                            {/* Column 1: Communication Toggles */}
                                            <div className={formData.IsNeedToSetComunication ? 'flex-1 space-y-2' : 'space-y-2'}>
                                                <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                    <input
                                                        type="checkbox"
                                                        checked={formData.IsNeedToSetComunication}
                                                        onChange={(e) => handleFieldChange('IsNeedToSetComunication', e.target.checked)}
                                                        className="rounded"
                                                    />
                                                    <span>Enable Communication</span>
                                                </label>
                                                {formData.IsNeedToSetComunication && (
                                                    <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                        <input
                                                            type="checkbox"
                                                            checked={formData.IsOpenCommunicationByDefault}
                                                            onChange={(e) => handleFieldChange('IsOpenCommunicationByDefault', e.target.checked)}
                                                            className="rounded"
                                                        />
                                                        <span>Open Communication By Default</span>
                                                    </label>
                                                )}
                                            </div>
                                            {/* Column 2: Communication Dropdowns */}
                                            {formData.IsNeedToSetComunication && (
                                                <div className="flex-1 space-y-2">
                                                    <div className="flex items-center">
                                                        <div className={`text-xs w-[140px] ${theme.label}`}>
                                                            Dock Position
                                                        </div>
                                                        <div>
                                                            <select
                                                                value={formData.ConversationBoxDockPosition || ''}
                                                                onChange={(e) => handleFieldChange('ConversationBoxDockPosition', e.target.value ? parseInt(e.target.value) : null)}
                                                                className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                            >
                                                                <option value="">Select...</option>
                                                                {conversationDockPositionOptions.map(opt => (
                                                                    <option key={opt.Id} value={opt.Id}>
                                                                        {opt.Display}
                                                                    </option>
                                                                ))}
                                                            </select>
                                                        </div>
                                                    </div>
                                                    <div className="flex items-center">
                                                        <div className={`text-xs w-[140px] ${theme.label}`}>
                                                            Group By User
                                                        </div>
                                                        <div>
                                                            <select
                                                                value={formData.CommunicationGroupByType || ''}
                                                                onChange={(e) => handleFieldChange('CommunicationGroupByType', e.target.value ? parseInt(e.target.value) : null)}
                                                                className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                            >
                                                                <option value="">Select...</option>
                                                                {conversationGroupByTypeOptions.map(opt => (
                                                                    <option key={opt.Id} value={opt.Id}>
                                                                        {opt.Display}
                                                                    </option>
                                                                ))}
                                                            </select>
                                                        </div>
                                                    </div>
                                                    <div className="flex items-center">
                                                        <div className={`text-xs w-[140px] ${theme.label}`}>
                                                            User List Field
                                                        </div>
                                                        <div>
                                                            <select
                                                                value={formData.CommunicationToUserIdTransField || ''}
                                                                onChange={(e) => handleFieldChange('CommunicationToUserIdTransField', e.target.value ? parseInt(e.target.value) : null)}
                                                                className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                            >
                                                                <option value="">Select...</option>
                                                                {allTransactionFields.map(field => (
                                                                    <option key={field.Id} value={field.Id}>
                                                                        {field.longDisplay}
                                                                    </option>
                                                                ))}
                                                            </select>
                                                        </div>
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                    )}
                                    </>
                                    ) : (
                                    /* Block B: !IsPhysicalModelTableCreated - horizontal layout like Angular (display:flex; column-gap:20px) */
                                    <div className={`p-3 border rounded-lg ${theme.mainContentSection} flex-none`}>
                                        <div className="flex gap-5 flex-wrap" style={{ columnGap: 20 }}>
                                            {/* Column 1: Name, Type, Provider API, API JSON Data */}
                                            <div className="flex flex-col gap-2 min-w-0">
                                                <div className="flex items-center gap-2">
                                                    <div className={`text-xs w-[180px] shrink-0 ${theme.label}`}>Data Model Name</div>
                                                    <input
                                                        type="text"
                                                        value={formData.TransactionName}
                                                        onChange={(e) => handleFieldChange('TransactionName', e.target.value)}
                                                        className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                        maxLength={200}
                                                    />
                                                </div>
                                                <div className="flex items-center gap-2">
                                                    <div className={`text-xs w-[180px] shrink-0 ${theme.label}`}>Data Model Type</div>
                                                    <select
                                                        value={formData.TransactionOrganizedType || ''}
                                                        disabled
                                                        className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                    >
                                                        {transactionOrganizedTypeOptions.map(opt => (
                                                            <option key={opt.Id} value={opt.Id}>{opt.Display}</option>
                                                        ))}
                                                    </select>
                                                </div>
                                                {isApiIntegrationTransaction && (
                                                    <>
                                                        <div className="flex items-center gap-2">
                                                            <div className={`text-xs w-[180px] shrink-0 ${theme.label}`}>Provider API Data Source</div>
                                                            <div className="flex items-center gap-1 w-[200px] min-w-0">
                                                                <select
                                                                    value={formData.FolderUsageType ?? ''}
                                                                    onChange={(e) => handleFieldChange('FolderUsageType', e.target.value ? parseInt(e.target.value) : null)}
                                                                    className={`flex-auto min-w-0 h-7 px-2 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                                    title={formData.FolderUsageType != null ? dictApiOperationDisplay[formData.FolderUsageType] : undefined}
                                                                >
                                                                    <option value="">Select...</option>
                                                                    {integrationSettingList.map((integration: any) => {
                                                                        const params = integration.AppIntergrationSettingParameterList || integration.AppIntegrationSettingParameterList || [];
                                                                        if (!params.length) return null;
                                                                        return (
                                                                            <optgroup key={integration.Id || integration.Name} label={integration.Name || 'API'}>
                                                                                {params.map((apiOp: any) => (
                                                                                    <option key={apiOp.Id} value={apiOp.Id}>
                                                                                        {dictApiOperationDisplay[apiOp.Id] ?? `${integration.Name || ''} . ${apiOp.ActionCode || apiOp.Id}`}
                                                                                    </option>
                                                                                ))}
                                                                            </optgroup>
                                                                        );
                                                                    })}
                                                                </select>
                                                                {formData.FolderUsageType != null && (
                                                                    <>
                                                                        <button
                                                                            type="button"
                                                                            onClick={handleOpenApiJsonPreview}
                                                                            className={`flex-none p-1 text-xs rounded ${theme.button_default}`}
                                                                            title="Preview API JSON Data"
                                                                        >
                                                                            <i className="fa-solid fa-eye"></i>
                                                                        </button>
                                                                        <button
                                                                            type="button"
                                                                            onClick={handleEditApiOperation}
                                                                            className={`flex-none p-1 text-xs rounded ${theme.button_default}`}
                                                                            title="Edit Operation"
                                                                        >
                                                                            <i className="fa-solid fa-pencil"></i>
                                                                        </button>
                                                                    </>
                                                                )}
                                                            </div>
                                                        </div>

                                                    </>
                                                )}
                                            </div>
                                            {/* Column 2: API CRUD Type, Need Save, Need Delete (like Angular second div) */}
                                            {isApiIntegrationTransaction && (
                                                <div className="flex flex-col gap-2 min-w-0">
                                                    {formData.FolderUsageType != null && (
                                                        <div className="flex items-center gap-2">
                                                            <div className={`text-xs w-[180px] shrink-0 ${theme.label}`}>API CRUD Type</div>
                                                            <select
                                                                value={formData.ApiIntegrationTransactionCrudType ?? ''}
                                                                onChange={(e) => handleFieldChange('ApiIntegrationTransactionCrudType', e.target.value ? parseInt(e.target.value) : null)}
                                                                className={`w-[200px] px-2 py-1 text-xs border rounded ${theme.inputBox || theme.mainContentSection}`}
                                                            >
                                                                <option value="">Select...</option>
                                                                {apiCrudTypeOptions.map(opt => (
                                                                    <option key={opt.Id} value={opt.Id}>{opt.Display}</option>
                                                                ))}
                                                            </select>
                                                        </div>
                                                    )}
                                                    {formData.ApiIntegrationTransactionCrudType === 2 && (
                                                        <>
                                                            <div className="flex items-center gap-2">
                                                                <div className={`text-xs w-[180px] shrink-0 ${theme.label}`}>Need Save With Provider API</div>
                                                                <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                                    <input
                                                                        type="checkbox"
                                                                        checked={formData.IsEnableSaveByApiCall}
                                                                        onChange={(e) => handleFieldChange('IsEnableSaveByApiCall', e.target.checked)}
                                                                        className="rounded"
                                                                    />
                                                                    {formData.IsEnableSaveByApiCall && (
                                                                        <button type="button" onClick={() => { /* TODO: Config */ }} className={`text-xs underline ${theme.label} bg-transparent border-none cursor-pointer p-0`}>Config</button>
                                                                    )}
                                                                </label>
                                                            </div>
                                                            <div className="flex items-center gap-2">
                                                                <div className={`text-xs w-[180px] shrink-0 ${theme.label}`}>Need Delete With Provider API</div>
                                                                <label className="flex items-center gap-1.5 text-xs h-[26px] whitespace-nowrap">
                                                                    <input
                                                                        type="checkbox"
                                                                        checked={formData.IsEnableDeleteByApi}
                                                                        onChange={(e) => handleFieldChange('IsEnableDeleteByApi', e.target.checked)}
                                                                        className="rounded"
                                                                    />
                                                                    {formData.IsEnableDeleteByApi && (
                                                                        <button type="button" onClick={() => { /* TODO: Config */ }} className={`text-xs underline ${theme.label} bg-transparent border-none cursor-pointer p-0`}>Config</button>
                                                                    )}
                                                                </label>
                                                            </div>
                                                        </>
                                                    )}
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                    )}
                                </div>
                            </div>
                        </CollapsibleSection>
                    </div>
                )}

                {/* Unit Structure Section - Takes most of the screen space */}
                <div
                    className="h-1 flex-auto overflow-hidden flex flex-col cursor-pointer"
                    onClick={() => {
                        // Auto collapse Main Properties when Unit Structure section is clicked
                        if (!isSectionCollapsed('properties')) {
                            toggleSection('properties');
                        }
                    }}
                >
                    <div
                        className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection} border border-b-0 rounded-t flex-none`}

                    >
                        <div className={`text-sm font-semibold ${theme.title}`}>
                            Unit Structure
                        </div>
                        <div className="flex items-center space-x-2" onClick={(e) => e.stopPropagation()}>
                            {transactionData?.ApiDataStructure && (
                                <button
                                    className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                    title="Synchronize All Unit Fields With API Nodes"
                                >
                                    <i className="fa-solid fa-recycle"></i>
                                    <span>Synchronize All Unit Fields With API Nodes</span>
                                </button>
                            )}

                            <button
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                title="Expand All"
                            >
                                <i className="fa-regular fa-plus-square"></i>
                                <span>Expand All</span>
                            </button>

                            <button
                                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95 flex items-center gap-1`}
                                title="Collapse All"
                            >
                                <i className="fa-regular fa-minus-square"></i>
                                <span>Collapse All</span>
                            </button>
                        </div>
                    </div>

                    {/* Diagram Container - Takes remaining space */}
                    <div
                        className={`h-1 flex-auto w-full flex flex-col ${theme.mainContentSection}`}
                        style={{ position: 'relative' }}
                    >
                        {transactionData?.OtherOptions?.ErDiagramId && (
                            <div
                                className="absolute left-0 top-0 bottom-0 w-[390px] border-r p-2"
                                onClick={(e) => e.stopPropagation()}
                            >
                                <div className={`text-xs mb-2 ${theme.label}`}>
                                    Drag & Drop - Add Table To Unit Container
                                </div>
                                {/* Table list for drag and drop would go here */}
                                <div className="text-xs text-gray-400">
                                    Table list to be implemented
                                </div>
                            </div>
                        )}

                        {/* API Data Structure left panel (Angular: ng-if="IsApiIntegrationTransaction", same position as ER panel) */}
                        {isApiIntegrationTransaction && !transactionData?.OtherOptions?.ErDiagramId && (
                            <div
                                className="absolute left-0 top-0 bottom-0 w-[390px] border border-[rgb(214,212,244)] flex flex-col overflow-hidden"
                                onClick={(e) => e.stopPropagation()}
                            >
                                <div className={`flex items-center justify-between px-2 py-1.5 border-b ${theme.mainContentSection}`}>
                                    <span className={`text-xs font-medium ${theme.label}`}>Api Data Structure:</span>
                                    <button
                                        type="button"
                                        onClick={handleAddSelectedApiNodesAsUnits}
                                        className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                    >
                                        <i className="fa-solid fa-plus mr-1"></i>
                                        Add Selected Node As Units
                                    </button>
                                </div>
                                <div className="flex-1 min-h-0 overflow-auto p-2">
                                    {transactionData?.ApiDataStructure && transactionData.ApiDataStructure.length > 0 ? (
                                        <ApiDataStructureTree
                                            nodes={transactionData.ApiDataStructure}
                                            theme={theme}
                                            onNodeSelectChange={handleApiStructureNodeSelectChange}
                                        />
                                    ) : (
                                        <div className={`text-xs ${theme.label}`}>
                                            Select Provider API Data Source and save to load API structure, or open an existing API Data Model.
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}

                        <div className={`flex-auto h-1 overflow-hidden ${(transactionData?.OtherOptions?.ErDiagramId || isApiIntegrationTransaction) ? 'ml-[390px]' : ''}`}>
                            <div className="h-full border rounded-b overflow-hidden">
                                <UnitStructureEditor
                                    units={transactionData?.AppTransactionUnitList || []}
                                    transactionOrganizedType={transactionData?.TransactionOrganizedType}
                                    dictCurrentPKOrFKLinkToParentKeyGuidMap={transactionData?.DictCurrentPKOrFKLinkToParentKeyGuidMap}
                                    onUnitSelect={handleUnitSelect}
                                    onUnitEdit={handleUnitEdit}
                                    onUnitDelete={handleUnitDelete}
                                    onUnitNavigate={handleUnitNavigate}
                                    onAddUnit={handleAddUnit}
                                    selectedUnitId={selectedUnitId}
                                    isPhysicalModelTableCreated={transactionData?.IsPhysicalModelTableCreated || false}
                                    showDatabaseTableActions={showDatabaseTableActions}
                                    isReadOnly={transactionData?.IsReadOnly || false}
                                    onFieldLinkChange={handleFieldLinkChange}
                                    onEditTable={handleEditTable}
                                />
                                        </div>
                                </div>
                                </div>
                        </div>
                    </div>

            {/* Add Unit Dialog */}
            <AddUnitDialog
                isOpen={addUnitDialog.isOpen}
                level={addUnitDialog.level}
                parentUnitId={addUnitDialog.parentUnitId}
                onClose={handleCloseAddUnitDialog}
                onAddExistingTable={handleAddExistingTableUnit}
                onAddNewTable={handleAddNewTableUnit}
                onAddVirtual={handleAddVirtualUnit}
                onAddQuery={handleAddQueryUnit}
                isPhysicalModelTableCreated={Boolean(isPhysicalModelTableCreated)}
                showDatabaseTableActions={showDatabaseTableActions}
                isReadOnly={Boolean(transactionData?.IsReadOnly || false)}
            />

            {/* Database Table Selector Dialog */}
            <DatabaseTableSelectorDialog
                isOpen={tableSelectorDialog.isOpen}
                dataSourceRegisterId={transactionData?.DataSourceFrom || dataSourceRegisterId}
                applicationId={applicationId}
                onClose={handleCloseTableSelectorDialog}
                onSelect={handleTableSelected}
            />

            {/* Table Design Dialog */}
            <MetaDataTableDesign
                isOpen={tableDesignDialog.isOpen}
                onClose={handleCloseTableDesign}
                tableName={tableDesignDialog.tableName}
                dataSourceRegisterId={transactionData?.DataSourceFrom || dataSourceRegisterId}
                schemaOwner={tableDesignDialog.schemaOwner}
                applicationId={applicationId ? parseInt(applicationId) : null}
            />

            {/* New Table Designer (full screen) -> add as unit after save */}
            <MetaDataTableDesign
                isOpen={newTableUnitDialog.isOpen}
                onClose={handleCloseNewTableUnitDialog}
                onSave={(tableData: any, isNewTable: boolean) => {
                    if (!isNewTable) return;
                    const name = tableData?.Name;
                    const owner = tableData?.SchemaOwner;
                    if (!name || !owner) return;
                    const saved = { tableName: name, schemaOwner: owner };
                    newTableUnitSavedRef.current = saved;
                    setNewTableUnitSaved(saved);
                }}
                tableName={null}
                dataSourceRegisterId={transactionData?.DataSourceFrom || dataSourceRegisterId}
                schemaOwner={null}
                applicationId={applicationId ? parseInt(applicationId) : null}
                defaultFullscreen={true}
                closeOnSave={false}
            />

            {/* View Design Dialog */}
            <MetaDataViewDesign
                isOpen={viewDesignDialog.isOpen}
                onClose={handleCloseViewDesign}
                viewName={viewDesignDialog.viewName}
                dataSourceRegisterId={transactionData?.DataSourceFrom || dataSourceRegisterId}
                schemaOwner={viewDesignDialog.schemaOwner}
                applicationId={applicationId ? parseInt(applicationId) : null}
                isNewView={false}
            />

            {/* Unit Editor Dialog */}
            <TransactionUnitEditor
                isOpen={unitEditorDialog.isOpen}
                unit={unitEditorDialog.unit}
                transactionData={transactionData}
                dataSourceRegisterId={transactionData?.DataSourceFrom || dataSourceRegisterId}
                applicationId={applicationId}
                onClose={handleCloseUnitEditor}
                onSave={handleUnitEditorSave}
                onEditTable={handleEditTable}
            />

            {/* Unit Navigate To Data Model / Any Page */}
            {unitNavigateDialog.linkTargetEditor && unitNavigateDialog.unitId && (
                <TransactionUnitLinkTargetEditor
                    isOpen={true}
                    transactionUnitId={unitNavigateDialog.unitId}
                    unitDisplayName={unitNavigateDialog.unitDisplayName}
                    linkTargetUsageType={unitNavigateDialog.linkTargetEditor.usageType}
                    title={unitNavigateDialog.linkTargetEditor.title}
                    onClose={() =>
                        setUnitNavigateDialog({ unitId: null, unitDisplayName: '', linkTargetEditor: null, linkedSearchOpen: false })
                    }
                />
            )}

            {/* Unit Navigate To Search */}
            {unitNavigateDialog.linkedSearchOpen && unitNavigateDialog.unitId && (
                <TransactionUnitLinkedSearchManagementDialog
                    isOpen={true}
                    transactionUnitId={unitNavigateDialog.unitId}
                    unitDisplayName={unitNavigateDialog.unitDisplayName}
                    onClose={() =>
                        setUnitNavigateDialog({ unitId: null, unitDisplayName: '', linkTargetEditor: null, linkedSearchOpen: false })
                    }
                />
            )}

            {/* Form Preview Popup - Similar to TableDataPreview */}
            {previewPopup.isOpen && previewPopup.transactionId && (
                <TransactionFormPreview
                    isOpen={previewPopup.isOpen}
                    onClose={handleClosePreview}
                    transactionId={previewPopup.transactionId}
                    transactionType={previewPopup.transactionType || 1}
                    transactionName={previewPopup.transactionName || 'Transaction'}
                />
            )}

            {/* API JSON Data Preview Modal - supports preview before save by fetching from selected API operation */}
            {apiJsonPreviewOpen && (
                <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/50" onClick={(e) => e.stopPropagation()}>
                    <div className={`w-full max-w-3xl mx-4 flex flex-col rounded-lg shadow-xl ${theme.mainContentSection}`} style={{ minHeight: 600, height: 600, maxHeight: '90vh' }} onClick={(e) => e.stopPropagation()}>
                        <div className="flex-none flex items-center justify-between px-3 py-2 border-b">
                            <span className={`text-sm font-semibold ${theme.title}`}>Preview API JSON Data</span>
                            <button type="button" onClick={handleCloseApiJsonPreview} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>Close</button>
                        </div>
                        <div className="flex-1 min-h-0 overflow-auto p-3">
                            {apiJsonPreviewLoading ? (
                                <p className={`text-xs ${theme.label}`}>Loading API JSON sample data…</p>
                            ) : (() => {
                                const data = transactionData?.BaseApiConfigDto?.JsonSampleData ?? apiJsonPreviewData;
                                return data ? (
                                    <pre className={`text-xs p-3 rounded overflow-auto min-h-0 whitespace-pre-wrap break-words ${theme.inputBox || 'bg-gray-100 dark:bg-gray-800'}`}>
                                        {prettyPrintJsonForDisplay(data)}
                                    </pre>
                                ) : (
                                    <p className={`text-xs ${theme.label}`}>No API JSON data available for the selected API Data Source.</p>
                                );
                            })()}
                        </div>
                    </div>
                </div>
            )}

            {/* API Settings Popup (Angular-style DIV popup) */}
            {apiSettingPopup.isOpen && (
                <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/50" onClick={() => setApiSettingPopup((p) => ({ ...p, isOpen: false }))}>
                    <div
                        className={`w-full max-w-6xl mx-4 flex flex-col rounded-lg shadow-xl ${theme.mainContentSection}`}
                        style={{ minHeight: 700, height: 700, maxHeight: '90vh' }}
                        onClick={(e) => e.stopPropagation()}
                    >
                        <div className="flex-none flex items-center justify-between px-3 py-2 border-b">
                            <span className={`text-sm font-semibold ${theme.title}`}>
                                {apiSettingPopup.mode === 'Consume' ? 'Consume APIs (Call 3rd Part API)' : 'Provide APIs (Provide API To 3rd Part)'}
                            </span>
                            <button
                                type="button"
                                onClick={() => setApiSettingPopup((p) => ({ ...p, isOpen: false }))}
                                className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                            >
                                Close
                            </button>
                        </div>
                        <div className="flex-1 min-h-0 overflow-hidden p-2">
                            <TransactionDataTransferSettingMgt
                                transactionId={transactionId ?? null}
                                transactionName={formData?.TransactionName || ''}
                                filterTransferTypeIds={apiSettingPopup.mode === 'Consume' ? [3] : [4]}
                                titleOverride={apiSettingPopup.mode === 'Consume' ? 'Consume APIs (Call 3rd Part API)' : 'Provide APIs (Provide API To 3rd Part)'}
                                onRefresh={onRefresh}
                            />
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default TransactionGraphicEditor;
