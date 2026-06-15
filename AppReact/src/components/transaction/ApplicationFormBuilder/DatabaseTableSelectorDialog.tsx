import React, { useState, useEffect } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';

interface DatabaseTableSelectorDialogProps {
    isOpen: boolean;
    dataSourceRegisterId: number | null;
    applicationId: string | null;
    onClose: () => void;
    onSelect: (tableName: string, schemaOwner: string) => void;
}

const DatabaseTableSelectorDialog: React.FC<DatabaseTableSelectorDialogProps> = ({
    isOpen,
    dataSourceRegisterId,
    applicationId,
    onSelect,
    onClose
}) => {
    const { theme } = useTheme();
    const dispatch = useDispatch();
    const [allTables, setAllTables] = useState<any[]>([]); // Store all tables from server
    const [tables, setTables] = useState<any[]>([]); // Filtered tables to display
    const [schemaOwners, setSchemaOwners] = useState<string[]>([]);
    const [selectedSchemaOwner, setSelectedSchemaOwner] = useState<string>('');
    const [searchText, setSearchText] = useState('');
    const [loading, setLoading] = useState(false);

    // Load schema owners
    useEffect(() => {
        if (isOpen && dataSourceRegisterId !== null) {
            const loadSchemaOwners = async () => {
                try {
                    const owners = await schemaMetadataService.getDataBaseSchemaOwnerList(dataSourceRegisterId);
                    setSchemaOwners(owners || []);
                    if (owners && owners.length > 0) {
                        setSelectedSchemaOwner(owners[0]);
                    }
                } catch (error: any) {
                    console.error('Failed to load schema owners:', error);
                }
            };
            loadSchemaOwners();
        }
    }, [isOpen, dataSourceRegisterId]);

    // Load all tables when dialog opens or dataSourceRegisterId changes (cached)
    useEffect(() => {
        if (isOpen && dataSourceRegisterId !== null) {
            const loadTables = async () => {
                setLoading(true);
                dispatch(setIsBusy());
                try {
                    // Load all tables from cache first, fetch if not cached
                    const tableList = await schemaMetadataService.getDataSourceTableAndViewListFromCache(
                        dataSourceRegisterId,
                        null,
                        applicationId ? parseInt(applicationId) : null
                    );
                    setAllTables(tableList || []);
                } catch (error: any) {
                    console.error('Failed to load tables:', error);
                    setAllTables([]);
                } finally {
                    setLoading(false);
                    dispatch(setIsNotBusy());
                }
            };
            loadTables();
        } else {
            setAllTables([]);
            setTables([]);
        }
    }, [isOpen, dataSourceRegisterId, applicationId, dispatch]);

    // Filter tables client-side when schema owner or search text changes
    useEffect(() => {
        let filtered = [...allTables];
        
        // Filter by schema owner
        if (selectedSchemaOwner) {
            filtered = filtered.filter((t: any) => t.SchemaOwner === selectedSchemaOwner);
        }
        
        // Filter by search text
        if (searchText.trim()) {
            const searchLower = searchText.toLowerCase();
            filtered = filtered.filter((table: any) =>
                (table.Name || '').toLowerCase().includes(searchLower) ||
                (table.SchemaOwner || '').toLowerCase().includes(searchLower)
            );
        }
        
        setTables(filtered);
    }, [allTables, selectedSchemaOwner, searchText]);


    const handleSelect = (table: any) => {
        onSelect(table.Name, table.SchemaOwner || selectedSchemaOwner || '');
        onClose();
    };

    const handleBackdropClick = (e: React.MouseEvent) => {
        e.stopPropagation();
    };

    if (!isOpen) return null;

    return (
        <div
            className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[9999]"
            onClick={handleBackdropClick}
        >
            <div
                className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
                style={{ width: '600px', maxWidth: '90vw', height: '70vh', maxHeight: '600px' }}
                onClick={(e) => e.stopPropagation()}
            >
                {/* Header */}
                <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
                    <h3 className={`text-base font-semibold ${theme.title}`}>
                        Select Database Table
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

                {/* Filters */}
                <div className={`px-4 py-3 border-b ${theme.mainContentSection} space-y-2`}>
                    {schemaOwners.length > 0 && (
                        <div className="flex items-center gap-2">
                            <label className={`text-xs w-24 ${theme.label}`}>Schema Owner:</label>
                            <select
                                value={selectedSchemaOwner}
                                onChange={(e) => setSelectedSchemaOwner(e.target.value)}
                                className={`flex-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                            >
                                <option value="">All Schemas</option>
                                {schemaOwners.map((owner) => (
                                    <option key={owner} value={owner}>
                                        {owner}
                                    </option>
                                ))}
                            </select>
                        </div>
                    )}
                    <div className="flex items-center gap-2">
                        <label className={`text-xs w-24 ${theme.label}`}>Search:</label>
                        <input
                            type="text"
                            value={searchText}
                            onChange={(e) => setSearchText(e.target.value)}
                            placeholder="Search table name..."
                            className={`flex-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                        />
                    </div>
                </div>

                {/* Table List */}
                <div className="flex-1 overflow-y-auto px-4 py-2">
                    {loading ? (
                        <div className={`text-center py-8 ${theme.label}`}>
                            <i className="fa fa-spinner fa-spin mr-2"></i>
                            Loading tables...
                        </div>
                    ) : tables.length === 0 ? (
                        <div className={`text-center py-8 ${theme.label}`}>
                            {searchText ? 'No tables found matching your search' : 'No tables available'}
                        </div>
                    ) : (
                        <div className="space-y-1">
                            {tables.map((table: any, index: number) => (
                                <button
                                    key={table.Name || index}
                                    onClick={() => handleSelect(table)}
                                    className={`w-full text-left px-3 py-2 rounded border ${theme.button_default} hover:shadow-sm transition-all duration-200`}
                                >
                                    <div className="flex items-center justify-between">
                                        <div className="flex-1">
                                            <div className={`font-semibold text-sm ${theme.title}`}>
                                                {table.Name}
                                            </div>
                                            {table.SchemaOwner && (
                                                <div className={`text-xs ${theme.label}`}>
                                                    Schema: {table.SchemaOwner}
                                                </div>
                                            )}
                                        </div>
                                        <i className="fa fa-chevron-right text-gray-400"></i>
                                    </div>
                                </button>
                            ))}
                        </div>
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

export default DatabaseTableSelectorDialog;
