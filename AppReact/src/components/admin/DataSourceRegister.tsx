import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { DataMap } from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch, useSelector } from 'react-redux';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { RootState } from '../../redux/store';
import { isMasterSysAdminFromContext } from '../../helper/adminPermissionHelper';

type DataSourceRegisterItem = any;
type ValidationResult = any;
type CompanyItem = { Id: number; Code: string; ShortName: string };

type OperationCallResult<T> = {
    IsSuccessful?: boolean;
    ObjectList?: T[];
    ValidationResult?: ValidationResult;
};

const ALL_COMPANIES_ID = -1;

const extractValidationMessages = (validationResult: ValidationResult): string[] => {
    if (!validationResult) return [];

    const collect = (items: any[]) =>
        items.map((item) => item?.ErrorMessage || item?.Message || item?.Description || '').filter(Boolean);

    if (Array.isArray(validationResult)) return collect(validationResult);
    if (Array.isArray(validationResult?.Items)) return collect(validationResult.Items);
    if (Array.isArray(validationResult?.Errors)) return collect(validationResult.Errors);
    if (typeof validationResult === 'string') return [validationResult];
    return [];
};

const DataSourceRegister: React.FC = () => {
    const dispatch = useDispatch();
    const { theme } = useTheme();
    const errorMessage = useErrorMessage();
    const enumMap = useEnumValues('EmAppDataServerType');
    const userContext = useSelector((state: RootState) => state.userSession.userContext);
    const isSysAdmin = isMasterSysAdminFromContext(userContext);

    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [allDatasources, setAllDatasources] = useState<DataSourceRegisterItem[]>([]);
    const [companies, setCompanies] = useState<CompanyItem[]>([]);
    const [selectedCompanyId, setSelectedCompanyId] = useState<number>(ALL_COMPANIES_ID);
    const [deletedItemIds, setDeletedItemIds] = useState<number[]>([]);
    const [validationMessages, setValidationMessages] = useState<string[]>([]);

    const flexRef = useRef<wjGrid.FlexGrid | null>(null);

    // Single stable CollectionView — never replaced, only its source/filter mutated.
    // Wijmo's change-notification system keeps the grid in sync without any React rebind.
    const [cv] = useState<CollectionView>(() => new CollectionView<any>([]));

    // Replace CV source when data loads or reloads.
    useEffect(() => {
        cv.sourceCollection = allDatasources;
    }, [cv, allDatasources]);

    // SysAdmin only: filter grid by selected company in the left panel.
    // Tenant users receive tenant-scoped data from the server — no client-side data filter.
    useEffect(() => {
        if (!isSysAdmin) {
            cv.filter = null;
        } else {
            cv.filter =
                selectedCompanyId === ALL_COMPANIES_ID
                    ? null
                    : (item: any) => Number(item.DataSourceOwnerCompanyId) === selectedCompanyId;
        }
        cv.refresh();
    }, [cv, selectedCompanyId, isSysAdmin]);

    const dataSourceTypeOptions = useMemo(() => {
        if (!enumMap) return [];
        return Object.entries(enumMap).map(([key, value]) => ({ id: value, display: key }));
    }, [enumMap]);

    const dataSourceTypeMap = useMemo(
        () => new DataMap(dataSourceTypeOptions, 'id', 'display'),
        [dataSourceTypeOptions],
    );

    const companyMap = useMemo(() => {
        const opts = companies.map((c) => ({ id: c.Id, display: c.ShortName || c.Code || String(c.Id) }));
        return new DataMap(opts, 'id', 'display');
    }, [companies]);

    const loadData = useCallback(async () => {
        setIsLoading(true);
        dispatch(setIsBusy());
        try {
            const [dataSourceList, companyList] = await Promise.all([
                adminSvc.retrieveAllAppDataSourceRegisterExDto(),
                isSysAdmin
                    ? adminSvc.retrieveAllSaasCompanyDtoList()
                    : adminSvc.retrieveCurrentUserCompanyExDto().then((company) =>
                        company?.Id != null ? [company] : []),
            ]);

            const normalizedList = Array.isArray(dataSourceList) ? [...dataSourceList] : [];
            const normalizedCompanies = Array.isArray(companyList) ? companyList : [];

            setAllDatasources(normalizedList);
            setCompanies(normalizedCompanies);

            if (!isSysAdmin && normalizedCompanies[0]?.Id != null) {
                setSelectedCompanyId(normalizedCompanies[0].Id);
            }

            setDeletedItemIds([]);
            setValidationMessages([]);
        } catch (error) {
            errorMessage.showError(error instanceof Error ? error.message : String(error));
        } finally {
            setIsLoading(false);
            dispatch(setIsNotBusy());
        }
    }, [dispatch, errorMessage, isSysAdmin]);

    useEffect(() => {
        loadData();
    }, [loadData]);

    const handleAddRow = () => {
        const enumDefault = dataSourceTypeOptions[0]?.id;
        const ownerCompanyId = isSysAdmin
            ? (selectedCompanyId !== ALL_COMPANIES_ID ? selectedCompanyId : null)
            : (selectedCompanyId !== ALL_COMPANIES_ID ? selectedCompanyId : companies[0]?.Id ?? null);
        const newRow: DataSourceRegisterItem = {
            Id: null,
            DataSourceName: '',
            Description: '',
            DataSourceType: enumDefault,
            ConnectionString: '',
            DatabaseName: '',
            IsCompanyMasterDb: false,
            DataSourceOwnerCompanyId: ownerCompanyId,
        };

        const updated = [...allDatasources, newRow];
        setAllDatasources(updated);

        setTimeout(() => {
            const flex = flexRef.current;
            if (flex) {
                const lastRowIndex = flex.rows.length - 1;
                if (lastRowIndex >= 0) {
                    flex.select(new wjGrid.CellRange(lastRowIndex, 0), true);
                    flex.scrollIntoView(lastRowIndex, 0);
                }
            }
        }, 0);
    };

    const handleDeleteRow = () => {
        const flex = flexRef.current;
        if (!flex) return;

        const selectedRowIndex = flex.selection?.row;
        if (selectedRowIndex == null || selectedRowIndex < 0) {
            errorMessage.showWarning('Select a row to remove.');
            return;
        }

        const selectedItem = flex.rows[selectedRowIndex]?.dataItem;
        if (!selectedItem) return;

        if (selectedItem.Id && selectedItem.IsCompanyMasterDb) {
            errorMessage.showWarning('You cannot remove a Company Master Db.');
            return;
        }

        // eslint-disable-next-line no-restricted-globals
        if (!confirm(`Confirm to remove: ${selectedItem.DataSourceName || 'Datasource'} (Id = ${selectedItem.Id ?? 'new'})`)) {
            return;
        }

        if (selectedItem.Id) {
            setDeletedItemIds((prev) => [...prev, selectedItem.Id]);
        }

        setAllDatasources((prev) => prev.filter((item) => item !== selectedItem));
    };

    const handleSave = async () => {

        setIsSaving(true);
        dispatch(setIsBusy());
        try {
            const payload = {
                DataSourceRegisterSet: allDatasources,
                DeletedItemIds: deletedItemIds,
            };

            const response: OperationCallResult<DataSourceRegisterItem> =
                await adminSvc.saveAllAppDataSourceRegisterExDto(payload);

            const messages = extractValidationMessages(response?.ValidationResult);
            setValidationMessages(messages);

            if (response?.IsSuccessful) {
                errorMessage.showInfo('Data sources saved successfully.');
                const refreshedList = response?.ObjectList ? [...response.ObjectList] : [];
                setAllDatasources(refreshedList);
                setDeletedItemIds([]);
            } else if (messages.length) {
                messages.forEach((msg) => errorMessage.showError(msg));
            } else {
                errorMessage.showError('Failed to save data sources.');
            }
        } catch (error) {
            errorMessage.showError(error instanceof Error ? error.message : String(error));
        } finally {
            setIsSaving(false);
            dispatch(setIsNotBusy());
        }
    };

    const dsCountForCompany = (companyId: number) =>
        allDatasources.filter((ds) => Number(ds.DataSourceOwnerCompanyId) === companyId).length;

    const selectedCompanyLabel = companies.find((c) => c.Id === selectedCompanyId)?.ShortName
        ?? companies.find((c) => c.Id === selectedCompanyId)?.Code
        ?? '';

    return (
        <div className="w-full h-full flex flex-col gap-2 rounded-t-md rounded-b-md overflow-hidden">
            {/* Header */}
            <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
                <div className={`text-md font-semibold ${theme.title}`}>Data Source Register</div>
                <div className="flex items-center space-x-2">
                    <button type="button" onClick={loadData} disabled={isLoading || isSaving}
                        className="w-8 h-6 inline-flex items-center justify-center rounded text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-500 hover:bg-blue-600">
                        <i className="fa-solid fa-rotate" />
                    </button>
                    <button type="button" onClick={handleAddRow} disabled={isLoading || isSaving}
                        className="w-8 h-6 inline-flex items-center justify-center rounded text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600">
                        <i className="fa-solid fa-plus" />
                    </button>
                    <button type="button" onClick={handleDeleteRow} disabled={isLoading || isSaving}
                        className="w-8 h-6 inline-flex items-center justify-center rounded text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-rose-500 hover:bg-rose-600">
                        <i className="fa-solid fa-trash" />
                    </button>
                    <button type="button" onClick={handleSave} disabled={isLoading || isSaving}
                        className="w-8 h-6 inline-flex items-center justify-center rounded text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-orange-500 hover:bg-orange-600">
                        <i className="fa-solid fa-floppy-disk" />
                    </button>
                </div>
            </div>

            {/* Body: company panel + grid */}
            <div className="h-1 flex-auto flex flex-row gap-2 overflow-hidden">

                {/* Left company panel (SysAdmin only) */}
                {isSysAdmin && (
                <div className={`w-52 flex-none flex flex-col overflow-hidden ${theme.mainContentSection}`}>
                    <div className="py-2 px-3 text-sm font-semibold">Companies</div>
                    <div className="h-1 flex-auto overflow-y-auto">
                        {/* All Companies */}
                        <div
                            onClick={() => setSelectedCompanyId(ALL_COMPANIES_ID)}
                            className={`cursor-pointer flex items-center justify-between px-3 py-2 text-xs rounded-[4px] mx-1 mb-0.5 ${
                                selectedCompanyId === ALL_COMPANIES_ID ? theme.sideBar_menu_active : theme.sideBar_menu
                            }`}
                        >
                            <span>All Companies</span>
                            <span className="text-[10px] px-1.5 py-0.5 rounded-full bg-gray-200 text-gray-600">
                                {allDatasources.length}
                            </span>
                        </div>

                        {/* Company list */}
                        {companies.map((company) => (
                            <div
                                key={company.Id}
                                onClick={() => setSelectedCompanyId(company.Id)}
                                className={`cursor-pointer flex items-center justify-between px-3 py-2 text-xs rounded-[4px] mx-1 mb-0.5 ${
                                    selectedCompanyId === company.Id ? theme.sideBar_menu_active : theme.sideBar_menu
                                }`}
                            >
                                <span className="truncate mr-1">{company.ShortName || company.Code}</span>
                                <span className="text-[10px] px-1.5 py-0.5 rounded-full flex-none bg-gray-200 text-gray-600">
                                    {dsCountForCompany(company.Id)}
                                </span>
                            </div>
                        ))}
                    </div>
                </div>
                )}

                {/* Right datasource grid */}
                <section className={`flex-auto flex flex-col overflow-hidden ${theme.mainContentSection}`}>
                    <div className="flex items-center py-2 px-3 text-sm font-semibold gap-2">
                        <span>Datasource</span>
                        {(isSysAdmin ? selectedCompanyId !== ALL_COMPANIES_ID : selectedCompanyLabel) && (
                            <span className={`text-xs font-normal ${theme.label}`}>
                                — {selectedCompanyLabel}
                            </span>
                        )}
                    </div>
                    <div className="h-1 flex-auto w-full p-3 pt-0">
                        <FlexGrid
                            itemsSource={cv}
                            autoGenerateColumns={false}
                            allowDelete={false}
                            selectionMode="Row"
                            initialized={(flex: wjGrid.FlexGrid) => { flexRef.current = flex; }}
                            className="w-full h-full"
                        >
                            <FlexGridColumn binding="Id" header="ID" width={60} isReadOnly format="d" />
                            <FlexGridColumn
                                binding="DataSourceOwnerCompanyId"
                                header="Company"
                                width={140}
                                dataMap={companyMap}
                                isReadOnly={!isSysAdmin}
                            />
                            <FlexGridColumn binding="DataSourceName" header="Datasource Name" width={160} />
                            <FlexGridColumn binding="Description" header="Description" width={180} />
                            <FlexGridColumn
                                binding="DataSourceType"
                                header="Type"
                                width={130}
                                dataMap={dataSourceTypeMap}
                            />
                            <FlexGridColumn binding="ConnectionString" header="Connection String" width="*" />
                            <FlexGridColumn binding="DatabaseName" header="Database Name" width={160} />
                            <FlexGridColumn
                                binding="IsCompanyMasterDb"
                                header="Master DB"
                                width={90}
                                dataType="Boolean"
                                isReadOnly
                            />
                        </FlexGrid>
                    </div>
                </section>
            </div>

            {validationMessages.length > 0 && (
                <div className="rounded border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
                    <div className="font-semibold">Validation</div>
                    <ul className="mt-1 list-disc pl-5">
                        {validationMessages.map((msg, index) => (
                            <li key={`${msg}-${index}`}>{msg}</li>
                        ))}
                    </ul>
                </div>
            )}
        </div>
    );
};

export default DataSourceRegister;
