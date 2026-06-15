import React, { useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

interface AddUnitDialogProps {
    isOpen: boolean;
    level: number;
    parentUnitId?: number | null;
    onClose: () => void;
    onAddExistingTable: () => void;
    onAddNewTable: () => void;
    onAddVirtual: () => void;
    onAddQuery: () => void;
    isPhysicalModelTableCreated: boolean;
    /** When false (e.g. API or Temp DTO), hide Add Existing Table / Add New Table options */
    showDatabaseTableActions?: boolean;
    isReadOnly: boolean;
}

const AddUnitDialog: React.FC<AddUnitDialogProps> = ({
    isOpen,
    level,
    parentUnitId,
    onClose,
    onAddExistingTable,
    onAddNewTable,
    onAddVirtual,
    onAddQuery,
    isPhysicalModelTableCreated,
    showDatabaseTableActions = true,
    isReadOnly
}) => {
    const { theme } = useTheme();

    if (!isOpen) return null;

    const handleBackdropClick = (e: React.MouseEvent) => {
        e.stopPropagation();
    };

    return (
        <div
            className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[9999]"
            onClick={handleBackdropClick}
        >
            <div
                className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
                style={{ width: '500px', maxWidth: '90vw' }}
                onClick={(e) => e.stopPropagation()}
            >
                {/* Header */}
                <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
                    <h3 className={`text-base font-semibold ${theme.title}`}>
                        Add Unit (Level {level})
                    </h3>
                    <button
                        onClick={onClose}
                        className={`p-1 ${theme.button_default} rounded-[4px] transition-all duration-200 hover:shadow-sm active:scale-95`}
                    >
                        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                            <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                        </svg>
                    </button>
                </div>

                {/* Body */}
                <div className="px-4 py-4 space-y-2">
                    {/* Add Existing Table Unit - hidden for API/Temp DTO (showDatabaseTableActions false) */}
                    {(level === 1 || Boolean(isPhysicalModelTableCreated)) && showDatabaseTableActions && (
                        <button
                            onClick={() => {
                                onAddExistingTable();
                                onClose();
                            }}
                            className={`w-full px-4 py-3 text-left ${theme.button_default} rounded-[4px] border hover:shadow-sm active:scale-95 transition-all duration-200 flex items-center gap-2`}
                        >
                            <i className="fa fa-database text-lg"></i>
                            <div className="flex-1">
                                <div className={`font-semibold ${theme.title}`}>Add Existing Table Unit</div>
                                <div className={`text-xs ${theme.label}`}>Add a unit from an existing database table</div>
                            </div>
                        </button>
                    )}

                    {/* Add New Table Unit - hidden for API/Temp DTO */}
                    {((level === 1 && !isReadOnly) || (Boolean(isPhysicalModelTableCreated) && !isReadOnly)) && showDatabaseTableActions && (
                        <button
                            onClick={() => {
                                onAddNewTable();
                                onClose();
                            }}
                            className={`w-full px-4 py-3 text-left ${theme.button_default} rounded-[4px] border hover:shadow-sm active:scale-95 transition-all duration-200 flex items-center gap-2`}
                        >
                            <i className="fa fa-table text-lg"></i>
                            <div className="flex-1">
                                <div className={`font-semibold ${theme.title}`}>Add New Table Unit</div>
                                <div className={`text-xs ${theme.label}`}>Create a new database table and add it as a unit</div>
                            </div>
                        </button>
                    )}

                    {!isReadOnly && (
                        <button
                            onClick={() => {
                                onAddVirtual();
                                onClose();
                            }}
                            className={`w-full px-4 py-3 text-left ${theme.button_default} rounded-[4px] border hover:shadow-sm active:scale-95 transition-all duration-200 flex items-center gap-2`}
                        >
                            <i className="fa fa-cube text-lg"></i>
                            <div className="flex-1">
                                <div className={`font-semibold ${theme.title}`}>Add Virtual Unit</div>
                                <div className={`text-xs ${theme.label}`}>Add a virtual unit without a database table</div>
                            </div>
                        </button>
                    )}

                    {isReadOnly && (
                        <button
                            onClick={() => {
                                onAddQuery();
                                onClose();
                            }}
                            className={`w-full px-4 py-3 text-left ${theme.button_default} rounded-[4px] border hover:shadow-sm active:scale-95 transition-all duration-200 flex items-center gap-2`}
                        >
                            <i className="fa fa-search text-lg"></i>
                            <div className="flex-1">
                                <div className={`font-semibold ${theme.title}`}>Add Query Unit</div>
                                <div className={`text-xs ${theme.label}`}>Add a read-only unit based on a query</div>
                            </div>
                        </button>
                    )}
                </div>

                {/* Footer */}
                <div className={`px-4 py-3 border-t ${theme.mainContentSection} flex justify-end`}>
                    <button
                        onClick={onClose}
                        className={`px-4 py-2 ${theme.button_default} rounded-[4px] transition-all duration-200 border hover:shadow-sm active:scale-95 text-sm font-medium`}
                    >
                        Cancel
                    </button>
                </div>
            </div>
        </div>
    );
};

export default AddUnitDialog;
