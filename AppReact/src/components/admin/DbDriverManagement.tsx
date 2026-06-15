import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';

type TableDataDto = {
    ColumnList?: Array<{ ColumnName?: string; Name?: string } | string>;
    DataRowList?: Record<string, any>[];
};

type ServerSettingDto = {
    InstalledDbDriver?: TableDataDto;
};

const buildDriverColumns = (tableData?: TableDataDto) => {
    const rows = tableData?.DataRowList ?? [];
    if (!rows.length) return [];

    if (Array.isArray(tableData?.ColumnList) && tableData!.ColumnList!.length > 0) {
        return tableData!.ColumnList!.map((col: any) => col?.ColumnName || col?.Name || col).filter(Boolean);
    }

    const keys = new Set<string>();
    rows.forEach((row) => Object.keys(row || {}).forEach((k) => keys.add(k)));
    return Array.from(keys);
};

const DbDriverManagement: React.FC = () => {
    const dispatch = useDispatch();
    const { theme } = useTheme();
    const errorMessage = useErrorMessage();

    const [isLoading, setIsLoading] = useState(false);
    const [serverSettings, setServerSettings] = useState<ServerSettingDto | null>(null);

    const driverRows = useMemo(() => serverSettings?.InstalledDbDriver?.DataRowList ?? [], [serverSettings]);
    const driverColumns = useMemo(() => buildDriverColumns(serverSettings?.InstalledDbDriver), [serverSettings]);

    const loadData = useCallback(async () => {
        setIsLoading(true);
        dispatch(setIsBusy());
        try {
            const result = await adminSvc.checkServerSetting();
            setServerSettings(result);
        } catch (error) {
            errorMessage.showError(error instanceof Error ? error.message : String(error));
        } finally {
            setIsLoading(false);
            dispatch(setIsNotBusy());
        }
    }, [dispatch, errorMessage]);

    useEffect(() => {
        loadData();
    }, [loadData]);

    return (
        <div className="w-full h-full flex flex-col gap-2 rounded-t-md rounded-b-md overflow-hidden">
            <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
                <div className={`text-md font-semibold ${theme.title}`}>Database Drivers</div>
                <button
                    type="button"
                    onClick={loadData}
                    disabled={isLoading}
                    className="w-8 h-6 inline-flex items-center justify-center rounded text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-500 hover:bg-blue-600"
                >
                    <i className="fa-solid fa-rotate" />
                </button>
            </div>

            <div className={`h-1 flex-auto flex flex-col overflow-hidden ${theme.mainContentSection}`}>
                <div className="flex items-center justify-between py-2 px-3 text-sm font-semibold">
                    Available Database Drivers
                </div>
                <div className="h-1 flex-auto w-full p-3 pt-0">
                    <FlexGrid
                        itemsSource={driverRows}
                        isReadOnly={true}
                        autoGenerateColumns={false}
                        className="w-full h-full"
                    >
                        {driverColumns.map((col) => (
                            <FlexGridColumn key={col} header={col} binding={col} width="*" />
                        ))}
                    </FlexGrid>
                </div>
            </div>
        </div>
    );
};

export default DbDriverManagement;
