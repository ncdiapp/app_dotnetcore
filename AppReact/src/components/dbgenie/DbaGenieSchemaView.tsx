import React, { useState, useCallback, useMemo } from 'react';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useAlert } from '../../hooks/useAlert';
import Confirm from '../common/Confirm';
import {
    dbGenieService,
    DbGenieTableMetadataDto,
} from '../../webapi/dbgeniesvc';
import { DbaGenieSessionState } from './DbaGenie';

interface DbaGenieSchemaViewProps {
    sessionState: DbaGenieSessionState;
    onSessionStateChange: (updates: Partial<DbaGenieSessionState>) => void;
}

const DbaGenieSchemaView: React.FC<DbaGenieSchemaViewProps> = ({
    sessionState,
    onSessionStateChange,
}) => {
    const dispatch = useDispatch();
    const { theme } = useTheme();
    const errorMessage = useErrorMessage();
    const { confirmState, showConfirm, closeConfirm } = useAlert();

    const [selectedTableIndex, setSelectedTableIndex] = useState<number>(0);
    const [showScript, setShowScript] = useState(true);

    const tables = sessionState.extractedSchema?.Tables || [];
    const generatedScript = sessionState.extractedSchema?.GeneratedScript || '';
    const selectedTable = tables[selectedTableIndex];

    // Regenerate script
    const handleRegenerateScript = useCallback(async () => {
        if (!sessionState.extractedSchema || tables.length === 0) return;

        dispatch(setIsBusy());
        try {
            const result = await dbGenieService.generateCreateScript(sessionState.extractedSchema, 'dbo');
            if (result.IsSuccessful && result.Object) {
                onSessionStateChange({
                    extractedSchema: {
                        ...sessionState.extractedSchema,
                        GeneratedScript: result.Object,
                    },
                });
                errorMessage.showInfo('Script regenerated successfully');
            } else {
                const errMsg = result.ValidationResult?.Items?.[0]?.Message || 'Failed to regenerate script';
                errorMessage.showError(errMsg);
            }
        } catch (err) {
            errorMessage.showError('Error regenerating script: ' + (err as Error).message);
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [sessionState.extractedSchema, tables, dispatch, errorMessage, onSessionStateChange]);

    // Execute script
    const handleExecuteScript = useCallback(async () => {
        if (!generatedScript) return;

        const confirmed = await showConfirm(
            'This will create the tables in your database. Are you sure you want to proceed?',
            {
                title: 'Execute CREATE Script',
                confirmLabel: 'Execute',
                cancelLabel: 'Cancel',
            }
        );

        if (!confirmed) return;

        dispatch(setIsBusy());
        try {
            const result = await dbGenieService.executeSQL(
                generatedScript,
                undefined,
                true,
                true
            );

            if (result.IsSuccessful && result.Object?.IsSuccess) {
                errorMessage.showInfo('Tables created successfully');
            } else {
                const errMsg = result.Object?.Error || result.ValidationResult?.Items?.[0]?.Message || 'Failed to execute script';
                errorMessage.showError(errMsg);
            }
        } catch (err) {
            errorMessage.showError('Error executing script: ' + (err as Error).message);
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [generatedScript, dispatch, errorMessage, showConfirm]);

    // Copy script to clipboard
    const handleCopyScript = useCallback(() => {
        if (!generatedScript) return;
        navigator.clipboard.writeText(generatedScript);
        errorMessage.showInfo('Script copied to clipboard');
    }, [generatedScript, errorMessage]);

    // Export to file
    const handleExportScript = useCallback(() => {
        if (!generatedScript) return;

        const blob = new Blob([generatedScript], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'create_tables.sql';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);

        errorMessage.showInfo('Script exported successfully');
    }, [generatedScript, errorMessage]);

    // Render column type display
    const getColumnTypeDisplay = (column: any) => {
        let type = column.DataType || 'NVARCHAR';
        if (column.Length) {
            type += `(${column.Length === -1 ? 'MAX' : column.Length})`;
        } else if (column.Precision && column.Scale) {
            type += `(${column.Precision},${column.Scale})`;
        }
        return type;
    };

    if (tables.length === 0) {
        return (
            <div className="w-full h-full flex flex-col items-center justify-center p-8">
                <i className="fa-solid fa-diagram-project text-6xl text-gray-400 mb-4"></i>
                <h3 className={`text-lg font-medium mb-2 ${theme.title}`}>No Schema Extracted</h3>
                <p className={`text-sm text-center max-w-md ${theme.label}`}>
                    Go to the Dashboard to extract a database schema from your requirements document or text.
                </p>
            </div>
        );
    }

    return (
        <div className="w-full h-full flex flex-col">
            {/* Header */}
            <div className={`flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700`}>
                <div className={`text-md font-semibold ${theme.title}`}>
                    <i className="fa-solid fa-diagram-project mr-2 text-green-500"></i>
                    Extracted Schema ({tables.length} tables)
                </div>
                <div className="flex items-center gap-2">
                    <button
                        onClick={() => setShowScript(!showScript)}
                        className={`px-2 py-1 text-xs rounded ${showScript ? 'bg-blue-500 hover:bg-blue-600 text-white' : theme.button_default}`}
                    >
                        <i className={`fa-solid fa-code mr-1`}></i>
                        {showScript ? 'Hide Script' : 'Show Script'}
                    </button>
                </div>
            </div>

            {/* Main content */}
            <div className="flex-auto flex overflow-hidden">
                {/* Table list */}
                <div className={`w-48 flex-none border-r overflow-y-auto border-gray-200 dark:border-gray-700`}>
                    <div className="p-2">
                        <div className={`text-xs font-medium mb-2 px-2 ${theme.label}`}>Tables</div>
                        {tables.map((table, index) => (
                            <div
                                key={index}
                                onClick={() => setSelectedTableIndex(index)}
                                className={`px-3 py-2 rounded cursor-pointer text-sm mb-1 ${
                                    selectedTableIndex === index
                                        ? 'bg-blue-500 text-white'
                                        : `hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-900 dark:text-gray-100`
                                }`}
                            >
                                <i className="fa-solid fa-table mr-2"></i>
                                {table.Name}
                            </div>
                        ))}
                    </div>
                </div>

                {/* Table details and script */}
                <div className="flex-auto flex flex-col overflow-hidden">
                    {/* Table details */}
                    <div className="flex-auto overflow-auto p-4">
                        {selectedTable && (
                            <div>
                                {/* Table header */}
                                <div className="mb-4">
                                    <h3 className={`text-lg font-medium ${theme.title}`}>
                                        <i className="fa-solid fa-table mr-2 text-blue-500"></i>
                                        {selectedTable.Name}
                                    </h3>
                                    {selectedTable.Description && (
                                        <p className={`text-sm mt-1 ${theme.label}`}>{selectedTable.Description}</p>
                                    )}
                                </div>

                                {/* Columns */}
                                <div className={`rounded-md overflow-hidden border border-gray-200 dark:border-gray-700`}>
                                    <table className="w-full text-sm">
                                        <thead className="bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300">
                                            <tr>
                                                <th className="px-3 py-2 text-left">Column</th>
                                                <th className="px-3 py-2 text-left">Type</th>
                                                <th className="px-3 py-2 text-center w-16">PK</th>
                                                <th className="px-3 py-2 text-center w-16">Null</th>
                                                <th className="px-3 py-2 text-center w-16">Auto</th>
                                                <th className="px-3 py-2 text-left">Default</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {selectedTable.Columns.map((column, colIndex) => (
                                                <tr key={colIndex} className={`border-t border-gray-200 dark:border-gray-700`}>
                                                    <td className="px-3 py-2 font-medium">{column.Name}</td>
                                                    <td className="px-3 py-2 font-mono text-xs">{getColumnTypeDisplay(column)}</td>
                                                    <td className="px-3 py-2 text-center">
                                                        {column.IsPrimaryKey && <i className="fa-solid fa-key text-yellow-500"></i>}
                                                    </td>
                                                    <td className="px-3 py-2 text-center">
                                                        {column.IsNullable ? (
                                                            <span className="text-gray-400">Yes</span>
                                                        ) : (
                                                            <span className="text-red-500">No</span>
                                                        )}
                                                    </td>
                                                    <td className="px-3 py-2 text-center">
                                                        {column.IsAutoIncrement && <i className="fa-solid fa-arrow-up-1-9 text-blue-500"></i>}
                                                    </td>
                                                    <td className="px-3 py-2 text-xs opacity-70">{column.DefaultValue || '-'}</td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>

                                {/* Relationships */}
                                {selectedTable.Relationships && selectedTable.Relationships.length > 0 && (
                                    <div className="mt-4">
                                        <h4 className={`text-sm font-medium mb-2 ${theme.title}`}>
                                            <i className="fa-solid fa-link mr-2"></i>
                                            Relationships
                                        </h4>
                                        <div className={`rounded-md border p-3 bg-white dark:bg-gray-900/50 border-gray-200 dark:border-gray-700`}>
                                            {selectedTable.Relationships.map((rel, relIndex) => (
                                                <div key={relIndex} className={`text-sm py-1 ${relIndex > 0 ? 'border-t border-gray-200 dark:border-gray-700' : ''}`}>
                                                    <span className="font-mono">{rel.ForeignKeyColumn}</span>
                                                    <span className={`mx-2 ${theme.label}`}>→</span>
                                                    <span className="font-mono">{rel.TargetTable}.{rel.ReferencedColumn || 'Id'}</span>
                                                    <span className="ml-2 text-xs px-1 rounded bg-gray-200 dark:bg-gray-700">{rel.Type}</span>
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}
                    </div>

                    {/* Script panel */}
                    {showScript && (
                        <div className={`h-64 flex-none border-t border-gray-200 dark:border-gray-700`}>
                            <div className="h-full flex flex-col">
                                {/* Script header */}
                                <div className={`flex items-center justify-between px-4 py-2 border-b border-gray-200 dark:border-gray-700`}>
                                    <span className={`text-sm font-medium ${theme.title}`}>
                                        <i className="fa-solid fa-code mr-2"></i>
                                        Generated CREATE Script
                                    </span>
                                    <div className="flex items-center gap-2">
                                        <button
                                            onClick={handleRegenerateScript}
                                            className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                            title="Regenerate script"
                                        >
                                            <i className="fa-solid fa-rotate"></i>
                                        </button>
                                        <button
                                            onClick={handleCopyScript}
                                            className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                            title="Copy to clipboard"
                                        >
                                            <i className="fa-solid fa-copy"></i>
                                        </button>
                                        <button
                                            onClick={handleExportScript}
                                            className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                            title="Export to file"
                                        >
                                            <i className="fa-solid fa-download"></i>
                                        </button>
                                        <button
                                            onClick={handleExecuteScript}
                                            disabled={!generatedScript}
                                            className={`px-2 py-1 text-xs rounded bg-blue-500 hover:bg-blue-600 text-white disabled:opacity-50`}
                                            title="Execute script"
                                        >
                                            <i className="fa-solid fa-play mr-1"></i>
                                            Execute
                                        </button>
                                    </div>
                                </div>

                                {/* Script content */}
                                <div className="flex-auto overflow-auto p-4 bg-gray-50 dark:bg-gray-900">
                                    <pre className="text-xs font-mono whitespace-pre-wrap">{generatedScript}</pre>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </div>

            {/* Confirm dialog */}
            <Confirm
                isOpen={confirmState.isOpen}
                title={confirmState.title || 'Confirm'}
                message={confirmState.message}
                confirmLabel={confirmState.confirmLabel}
                cancelLabel={confirmState.cancelLabel}
                onConfirm={() => confirmState.onConfirm?.()}
                onCancel={closeConfirm}
            />
        </div>
    );
};

export default DbaGenieSchemaView;
