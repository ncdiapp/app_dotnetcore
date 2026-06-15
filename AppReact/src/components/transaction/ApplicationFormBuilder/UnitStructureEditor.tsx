import React, { useState, useCallback, useRef, useEffect, useMemo } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

interface UnitStructureEditorProps {
    units: any[];
    transactionOrganizedType?: number | null;
    onUnitSelect?: (unit: any) => void;
    onUnitEdit?: (unit: any) => void;
    onUnitDelete?: (unit: any) => void;
    onUnitNavigate?: (unit: any, usageType: number) => void;
    onAddUnit?: (level: number, parentUnitId?: number | null) => void;
    selectedUnitId?: number | null;
    isPhysicalModelTableCreated?: boolean;
    /** When false (API/Temp DTO), hide Edit Table / Create DDL - match Angular */
    showDatabaseTableActions?: boolean;
    isReadOnly?: boolean;
    dictCurrentPKOrFKLinkToParentKeyGuidMap?: { [key: string]: string };
    onFieldLinkChange?: (unit: any, field: any, parentPkField: any | null, isRemove: boolean) => void;
    onEditTable?: (unit: any) => void;
}

const COLUMN_WIDTH = 320;
const UNIT_CARD_HEIGHT = 220;

// Helper function to generate unique field ID for DOM element
const getFieldId = (unit: any, field: any): string => {
    if (unit.SchemaOwner && unit.DataBaseTableName && field.DataBaseFieldName) {
        // Match AngularJS: SchemaOwner + delimiter + DataBaseTableName + delimiter + DataBaseFieldName
        return `${unit.SchemaOwner}_${unit.DataBaseTableName}_${field.DataBaseFieldName}`.replace(/\s+/g, '');
    }
    return `field_${field.Id || field.RowIdentityGuid || `temp_${Date.now()}_${Math.random()}`}`;
};

// Helper function to generate unique unit card ID
const getUnitCardId = (unit: any): string => {
    return `unitCard_${unit.Id || unit.uiId || `temp_${Date.now()}_${Math.random()}`}`;
};

interface UnitCardProps {
    unit: any;
    level: number;
    onSelect: (unit: any) => void;
    onEdit: (unit: any) => void;
    onDelete: (unit: any) => void;
    onToggleExpand: (unit: any) => void;
    selectedUnitId?: number | null;
    theme: any;
    isPhysicalModelTableCreated?: boolean;
    showDatabaseTableActions?: boolean;
    isReadOnly?: boolean;
    onFieldLinkChange?: (unit: any, field: any, parentPkField: any | null, isRemove: boolean) => void;
    onEditTable?: (unit: any) => void;
    onOpenNavigateMenu?: (e: React.MouseEvent, unit: any) => void;
    parentUnit?: any;
    allUnits?: any[];
}

const UnitCard: React.FC<UnitCardProps> = ({
    unit,
    level,
    onSelect,
    onEdit,
    onDelete,
    onToggleExpand,
    selectedUnitId,
    theme,
    isPhysicalModelTableCreated = false,
    showDatabaseTableActions = true,
    isReadOnly = false,
    onFieldLinkChange,
    onEditTable,
    onOpenNavigateMenu,
    parentUnit,
    allUnits = []
}) => {
    const menuRef = useRef<HTMLDivElement | null>(null);
    const [fieldContextMenu, setFieldContextMenu] = useState<{
        isOpen: boolean;
        field: any;
        x: number;
        y: number;
        parentPkFields: any[];
    } | null>(null);

    const isSelected = selectedUnitId === unit.Id;
    const hasFields = unit.AppTransactionFieldList && Array.isArray(unit.AppTransactionFieldList) && unit.AppTransactionFieldList.length > 0;
    const unitCardId = getUnitCardId(unit);

    // Find parent unit to get masterKeyList
    const getParentUnit = useCallback((): any | null => {
        if (level === 1 && unit.IsMasterSiblingUnit) {
            // For sibling units, parent is the first root unit
            return allUnits.find((u: any) => u.level === 1 && !u.IsMasterSiblingUnit) || allUnits[0];
        } else if (level > 1) {
            // First try the passed parentUnit prop
            if (parentUnit) {
                return parentUnit;
            }
            // For level 2, parent is level 1 unit (Master unit)
            if (level === 2) {
                // Find the master unit (level 1, not sibling)
                const masterUnit = allUnits.find((u: any) => u.level === 1 && !u.IsMasterSiblingUnit) || allUnits[0];
                if (masterUnit) return masterUnit;
                // Fallback: find any level 1 unit that has this unit as a child
                return allUnits.find((u: any) => 
                    u.level === 1 && 
                    (u.Children?.some((c: any) => c.Id === unit.Id) || u.Id === unit.parent?.Id)
                );
            }
            // For level 3, parent is level 2 unit
            if (level === 3) {
                // Find the level 2 unit that contains this level 3 unit
                for (const level1Unit of allUnits) {
                    if (level1Unit.Children) {
                        const level2Unit = level1Unit.Children.find((c: any) => 
                            c.Id === unit.Id || 
                            c.Id === unit.parent?.Id || 
                            c.Children?.some((gc: any) => gc.Id === unit.Id)
                        );
                        if (level2Unit) return level2Unit;
                    }
                }
            }
        }
        return null;
    }, [level, unit, parentUnit, allUnits]);

    // Build masterKeyList from fields if it doesn't exist
    const buildMasterKeyList = useCallback((aUnit: any): any[] => {
        if (aUnit.masterKeyList && Array.isArray(aUnit.masterKeyList) && aUnit.masterKeyList.length > 0) {
            return aUnit.masterKeyList;
        }
        // Build from primary key fields
        if (aUnit.AppTransactionFieldList && Array.isArray(aUnit.AppTransactionFieldList)) {
            return aUnit.AppTransactionFieldList
                .filter((f: any) => f.IsPrimaryKey)
                .map((f: any) => ({
                    RowIdentityGuid: f.RowIdentityGuid,
                    DataBaseFieldName: f.DataBaseFieldName,
                    Id: f.Id,
                    DisplayName: f.DisplayName || f.FieldName
                }));
        }
        return [];
    }, []);

    const handleFieldMenuClick = useCallback((e: React.MouseEvent, field: any) => {
        e.stopPropagation();
        e.preventDefault();
        
        // Close any other open menu first
        if (globalFieldContextMenuRef) {
            globalFieldContextMenuRef = null;
        }
        
        const foundParentUnit = getParentUnit();
        let parentPkFields: any[] = [];
        
        if (foundParentUnit) {
            // Build masterKeyList if it doesn't exist
            parentPkFields = buildMasterKeyList(foundParentUnit);
        }

        // Always show menu, even if no parent PK fields (might still have "Remove Link" option)
        setFieldContextMenu({
            isOpen: true,
            field,
            x: e.clientX,
            y: e.clientY,
            parentPkFields
        });
    }, [getParentUnit, buildMasterKeyList]);

    const handleLinkToParentKey = useCallback((parentPkField: any) => {
        if (onFieldLinkChange && fieldContextMenu) {
            onFieldLinkChange(unit, fieldContextMenu.field, parentPkField, false);
        }
        setFieldContextMenu(null);
        globalFieldContextMenuRef = null;
    }, [onFieldLinkChange, fieldContextMenu, unit]);

    const handleRemoveLink = useCallback(() => {
        if (onFieldLinkChange && fieldContextMenu) {
            onFieldLinkChange(unit, fieldContextMenu.field, null, true);
        }
        setFieldContextMenu(null);
        globalFieldContextMenuRef = null;
    }, [onFieldLinkChange, fieldContextMenu, unit]);

    // Close menu when clicking outside or pressing Escape
    useEffect(() => {
        const handleClickOutside = (e: MouseEvent) => {
            if (fieldContextMenu?.isOpen && menuRef.current) {
                const target = e.target as HTMLElement;
                
                // Don't close if clicking on the menu itself
                if (menuRef.current.contains(target)) {
                    return;
                }
                
                // Don't close if clicking on another field menu trigger (will open new menu)
                if (target.closest('.cursor-pointer.hover\\:text-blue-600')) {
                    return;
                }
                
                // Close the menu
                setFieldContextMenu(null);
                globalFieldContextMenuRef = null;
            }
        };

        const handleEscape = (e: KeyboardEvent) => {
            if (e.key === 'Escape' && fieldContextMenu?.isOpen) {
                setFieldContextMenu(null);
                globalFieldContextMenuRef = null;
            }
        };

        if (fieldContextMenu?.isOpen) {
            // Update global ref
            if (menuRef.current) {
                globalFieldContextMenuRef = menuRef.current;
            }
            
            // Use setTimeout to avoid immediate closure from the click that opened it
            const timeoutId = setTimeout(() => {
                document.addEventListener('mousedown', handleClickOutside, true);
                document.addEventListener('click', handleClickOutside, true);
                document.addEventListener('keydown', handleEscape);
            }, 100);
            
            return () => {
                clearTimeout(timeoutId);
                document.removeEventListener('mousedown', handleClickOutside, true);
                document.removeEventListener('click', handleClickOutside, true);
                document.removeEventListener('keydown', handleEscape);
            };
        } else {
            globalFieldContextMenuRef = null;
        }
    }, [fieldContextMenu]);

    return (
        <div
            id={unitCardId}
            className={`relative border-b border-dashed border-gray-300`}
            style={{
                width: `${COLUMN_WIDTH}px`,
                height: `${UNIT_CARD_HEIGHT}px`,
                padding: '10px'
            }}
            onClick={() => onSelect(unit)}
        >
            <div
                className={`relative border ${isSelected ? 'border-blue-500' : 'border-gray-300'} ${theme.mainContentSection}`}
                style={{
                    margin: '10px',
                    width: 'calc(100% - 20px)',
                    height: 'calc(100% - 20px)'
                }}
            >
                {/* Action Buttons - Top (matching AngularJS UnitEditButton: 21px x 21px) */}
                <div className="absolute top-0.5 right-0.5 flex items-center gap-1 z-10" style={{ margin: '0px 4px' }}>
                    <button
                        onClick={(e) => {
                            e.stopPropagation();
                            onEdit(unit);
                        }}
                        className={`rounded-full border flex items-center justify-center ${theme.button_default}`}
                        style={{ width: '21px', height: '21px', fontSize: '9px' }}
                        title="Edit Unit"
                    >
                        <i className="fa fa-pencil"></i>
                    </button>
                    <button
                        onClick={(e) => {
                            e.stopPropagation();
                            if (onOpenNavigateMenu) onOpenNavigateMenu(e, unit);
                        }}
                        className={`rounded-full border flex items-center justify-center ${theme.button_default}`}
                        style={{ width: '21px', height: '21px', fontSize: '10px' }}
                        title="Navigation"
                    >
                        <i className="fa-solid fa-location-arrow" style={{ position: 'relative', top: '-1px' }}></i>
                    </button>
                    {/* Angular: IsPhysicalModelTableCreated && !IsVirtualUnit || IsReadOnly; hide Edit Table for API/DTO via showDatabaseTableActions */}
                    {((showDatabaseTableActions && !unit.IsVirtualUnit) || isReadOnly) && (
                        <button
                            onClick={(e) => {
                                e.stopPropagation();
                                if (onEditTable) {
                                    onEditTable(unit);
                                }
                            }}
                            className={`rounded-full border flex items-center justify-center ${theme.button_default}`}
                            style={{ width: '21px', height: '21px', fontSize: '9px' }}
                            title={isReadOnly ? 'Edit Query' : 'Edit Table'}
                        >
                            <i className="fa fa-database"></i>
                        </button>
                    )}
                    {!isReadOnly && (
                        <button
                            onClick={(e) => {
                                e.stopPropagation();
                                onDelete(unit);
                            }}
                            className={`rounded-full border flex items-center justify-center ${theme.button_default}`}
                            style={{ width: '21px', height: '21px', fontSize: '9px' }}
                            title="Remove Unit"
                        >
                            <i className="fa fa-trash"></i>
                        </button>
                    )}
                </div>

                {/* Header */}
                <div className={`${theme.mainContentSection} border-b h-[25px] flex items-center overflow-hidden`}>
                    <label
                        className="flex-1 cursor-move font-semibold text-xs px-2 truncate"
                        title={`DB Table: ${unit.DataBaseTableName || ''}`}
                    >
                        {unit.UnitDisplayName || unit.UnitName || `Unit ${unit.Id}`}
                    </label>
                </div>

                {/* Fields List */}
                <div className={`overflow-x-hidden overflow-y-auto ${theme.mainContentSection}`} style={{ height: 'calc(100% - 25px)' }}>
                    {hasFields ? (
                        unit.AppTransactionFieldList.map((field: any, index: number) => {
                            const fieldId = getFieldId(unit, field);
                            return (
                                <div 
                                    key={field.Id || index} 
                                    id={fieldId}
                                    className="h-[18px] flex items-center text-[10px]"
                                >
                                    <div className="px-1">
                                        <input
                                            type="checkbox"
                                            checked
                                            disabled
                                            className="w-3 h-3"
                                        />
                                    </div>
                                    <div
                                        className={`px-1 ${field.IsPrimaryKey ? theme.title : theme.label}`}
                                        title="Primary Key"
                                    >
                                        <i className="fa fa-key text-[9px]"></i>
                                    </div>
                                    {level > 0 && (
                                        <div
                                            className={`px-1 ${field.IsLinkToParentPrimaryKey ? theme.title : theme.label}`}
                                            title="Foreign Key"
                                        >
                                            <i className="fa fa-link text-[10px]"></i>
                                        </div>
                                    )}
                                    <div className="flex-1 truncate" title={field.DisplayName || field.FieldName}>
                                        {field.DisplayName || field.FieldName}
                                    </div>
                                    {level > 0 && (
                                        <div 
                                            className="px-1 cursor-pointer hover:text-blue-600" 
                                            title="Column Options"
                                            onClick={(e) => handleFieldMenuClick(e, field)}
                                        >
                                            <span className="text-[9px]">▼</span>
                                        </div>
                                    )}
                                </div>
                            );
                        })
                    ) : (
                        <div className={`text-center py-4 text-xs ${theme.label}`}>
                            No fields defined
                        </div>
                    )}
                </div>

                {/* Field Context Menu */}
                {fieldContextMenu?.isOpen && (
                    <div
                        ref={menuRef}
                        className="fixed bg-white border border-gray-300 rounded shadow-lg py-1 min-w-[200px]"
                        style={{
                            left: `${fieldContextMenu.x}px`,
                            top: `${fieldContextMenu.y}px`,
                            zIndex: 10000
                        }}
                        onClick={(e) => e.stopPropagation()}
                        onMouseDown={(e) => e.stopPropagation()}
                    >
                        {fieldContextMenu.parentPkFields.length > 0 && (
                            <>
                                {fieldContextMenu.parentPkFields.length === 1 ? (
                                    <button
                                        onClick={() => handleLinkToParentKey(fieldContextMenu.parentPkFields[0])}
                                        className="w-full text-left px-3 py-1 text-xs hover:bg-gray-100"
                                    >
                                        Link To Parent Key: {fieldContextMenu.parentPkFields[0].DataBaseFieldName}
                                    </button>
                                ) : (
                                    <>
                                        <div className="px-3 py-1 text-xs text-gray-500 border-b">
                                            Link To Parent Key <i className="fa fa-angle-right"></i>
                                        </div>
                                        {fieldContextMenu.parentPkFields.map((pkField: any, idx: number) => (
                                            <button
                                                key={idx}
                                                onClick={() => handleLinkToParentKey(pkField)}
                                                className="w-full text-left px-5 py-1 text-xs hover:bg-gray-100"
                                            >
                                                {pkField.DataBaseFieldName}
                                            </button>
                                        ))}
                                    </>
                                )}
                            </>
                        )}
                        {fieldContextMenu.field.IsLinkToParentPrimaryKey && (
                            <>
                                {fieldContextMenu.parentPkFields.length > 0 && <div className="border-t my-1"></div>}
                                <button
                                    onClick={handleRemoveLink}
                                    className="w-full text-left px-3 py-1 text-xs hover:bg-gray-100 text-red-600"
                                >
                                    Remove Link
                                </button>
                            </>
                        )}
                        {fieldContextMenu.parentPkFields.length === 0 && !fieldContextMenu.field.IsLinkToParentPrimaryKey && (
                            <div className="px-3 py-1 text-xs text-gray-400">
                                No parent key available
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
};

// Global ref to track open field context menu element (ensure only one is open at a time)
let globalFieldContextMenuRef: HTMLDivElement | null = null;

const UnitStructureEditor: React.FC<UnitStructureEditorProps> = ({
    units,
    transactionOrganizedType,
    onUnitSelect,
    onUnitEdit,
    onUnitDelete,
    onUnitNavigate,
    onAddUnit,
    selectedUnitId,
    isPhysicalModelTableCreated = false,
    showDatabaseTableActions = true,
    isReadOnly = false,
    dictCurrentPKOrFKLinkToParentKeyGuidMap = {},
    onFieldLinkChange,
    onEditTable
}) => {
    const { theme } = useTheme();
    const containerRef = useRef<HTMLDivElement>(null);
    const [expandedUnits, setExpandedUnits] = useState<Set<number>>(new Set());
    const [joinLines, setJoinLines] = useState<any[]>([]);
    const [navigateMenu, setNavigateMenu] = useState<{
        isOpen: boolean;
        unit: any | null;
        x: number;
        y: number;
    }>({ isOpen: false, unit: null, x: 0, y: 0 });

    // Determine number of columns based on transaction type
    // TransactionOrganizedType: 1 = MasterDetail (3 columns), 3 = List (2 columns)
    const maxColumns = transactionOrganizedType === 1 ? 3 : 2;
    const isMasterDetail = transactionOrganizedType === 1;

    const toggleExpand = useCallback((unit: any) => {
        if (!unit.Id) return;
        setExpandedUnits(prev => {
            const newSet = new Set(prev);
            if (newSet.has(unit.Id)) {
                newSet.delete(unit.Id);
            } else {
                newSet.add(unit.Id);
            }
            return newSet;
        });
    }, []);

    const expandAll = useCallback(() => {
        const allUnitIds = new Set<number>();
        const collectUnitIds = (unitList: any[]) => {
            unitList.forEach(unit => {
                if (unit.Id) {
                    allUnitIds.add(unit.Id);
                }
                if (unit.Children && unit.Children.length > 0) {
                    collectUnitIds(unit.Children);
                }
            });
        };
        collectUnitIds(units);
        setExpandedUnits(allUnitIds);
    }, [units]);

    const collapseAll = useCallback(() => {
        setExpandedUnits(new Set());
    }, []);

    const handleUnitSelect = useCallback((unit: any) => {
        if (onUnitSelect) {
            onUnitSelect(unit);
        }
    }, [onUnitSelect]);

    const handleUnitEdit = useCallback((unit: any) => {
        if (onUnitEdit) {
            onUnitEdit(unit);
        }
    }, [onUnitEdit]);

    const handleUnitDelete = useCallback((unit: any) => {
        if (onUnitDelete) {
            onUnitDelete(unit);
        }
    }, [onUnitDelete]);

    const handleOpenNavigateMenu = useCallback((e: React.MouseEvent, unit: any) => {
        e.stopPropagation();
        e.preventDefault();
        setNavigateMenu({ isOpen: true, unit, x: e.clientX, y: e.clientY });
    }, []);

    const handleNavigateAction = useCallback(
        (usageType: number) => {
            if (navigateMenu.unit && onUnitNavigate) {
                onUnitNavigate(navigateMenu.unit, usageType);
            }
            setNavigateMenu({ isOpen: false, unit: null, x: 0, y: 0 });
        },
        [navigateMenu.unit, onUnitNavigate]
    );

    const handleAddUnit = useCallback((level: number, parentUnitId?: number | null) => {
        if (onAddUnit) {
            onAddUnit(level, parentUnitId);
        }
    }, [onAddUnit]);

    // Get master units (level 1)
    const masterUnits = units || [];
    // Get child units (level 2) - from first master unit
    const childUnits = masterUnits.length > 0 && masterUnits[0].Children ? masterUnits[0].Children : [];

    // Column labels based on transaction type
    const getColumnLabels = () => {
        if (isMasterDetail) {
            return [
                'Master Units Container (Fields)',
                'Child Units Container (Grids)',
                'Grand Child Units Container (Sub-grid)'
            ];
        } else {
            return [
                'Master Units Container (Grid)',
                'Child Units Container (Sub-grid)'
            ];
        }
    };

    const columnLabels = getColumnLabels();

    // Check if adding unit is allowed on a specific level (matching AngularJS isAllowAddingUnitOnLevel)
    const isAllowAddingUnitOnLevel = useCallback((level: number): boolean => {
        if (level === 1) {
            // For List type (TransactionOrganizedType === 3), if root unit exists, don't allow adding more
            if (transactionOrganizedType === 3) { // List type
                const rootUnit = masterUnits[0];
                if (rootUnit) {
                    return false; // List type only allows one root unit
                }
            }
        }
        return true;
    }, [transactionOrganizedType, masterUnits]);

    // Use ref to track previous dictCurrentPKOrFKLinkToParentKeyGuidMap to prevent infinite loops
    const dictRef = useRef(dictCurrentPKOrFKLinkToParentKeyGuidMap);
    const prevDictStringRef = useRef<string>('');
    
    // Only update if the object actually changed (deep comparison via JSON)
    const currentDictString = JSON.stringify(dictCurrentPKOrFKLinkToParentKeyGuidMap);
    if (currentDictString !== prevDictStringRef.current) {
        dictRef.current = dictCurrentPKOrFKLinkToParentKeyGuidMap;
        prevDictStringRef.current = currentDictString;
    }
    
    const stableDictCurrentPKOrFKLinkToParentKeyGuidMap = dictRef.current;

    // Match Angular transactionEditorCtrl.getLineCoodinate: fixed X lanes per target unit level.
    const getLineXCoordinates = (rightUnitLevel: number): { startX: number; endX: number; midX: number } => {
        if (rightUnitLevel === 1) {
            // Sibling units in the same column: vertical connector on the left edge
            return { startX: 20, endX: 20, midX: 10 };
        }
        if (rightUnitLevel === 2) {
            return { startX: 300, endX: 340, midX: 320 };
        }
        if (rightUnitLevel === 3) {
            return { startX: 620, endX: 660, midX: 640 };
        }
        return { startX: 300, endX: 340, midX: 320 };
    };

    // Calculate line coordinate from one field to another field
    const calculateLineCoordinate = useCallback((line: any): any => {
        const coord: any = {
            startX: 0, startY: 0, step1X: 0, step1Y: 0, step2X: 0, step2Y: 0, endX: 0, endY: 0, midX: 0
        };

        const fromUnit = line.fromUnit;
        const toUnit = line.toUnit;
        const fromField = line.fromField;
        const toField = line.toField;

        if (!containerRef.current) return coord;

        // Get unit card elements
        const fromUnitCard = document.getElementById(getUnitCardId(fromUnit));
        const toUnitCard = document.getElementById(getUnitCardId(toUnit));
        
        if (!fromUnitCard || !toUnitCard) return coord;

        // Get field elements
        const fromFieldEl = document.getElementById(getFieldId(fromUnit, fromField));
        const toFieldEl = document.getElementById(getFieldId(toUnit, toField));
        
        if (!fromFieldEl || !toFieldEl) return coord;

        // Get positions relative to container
        const containerRect = containerRef.current.getBoundingClientRect();
        const fromCardRect = fromUnitCard.getBoundingClientRect();
        const toCardRect = toUnitCard.getBoundingClientRect();
        const fromFieldRect = fromFieldEl.getBoundingClientRect();
        const toFieldRect = toFieldEl.getBoundingClientRect();

        // Calculate positions relative to SVG container (accounting for scroll)
        const fromCardPos = {
            x: fromCardRect.left - containerRect.left + containerRef.current.scrollLeft,
            y: fromCardRect.top - containerRect.top + containerRef.current.scrollTop,
            width: fromCardRect.width,
            height: fromCardRect.height
        };

        const toCardPos = {
            x: toCardRect.left - containerRect.left + containerRef.current.scrollLeft,
            y: toCardRect.top - containerRect.top + containerRef.current.scrollTop,
            width: toCardRect.width,
            height: toCardRect.height
        };

        const fromFieldY = fromFieldRect.top - containerRect.top + containerRef.current.scrollTop;
        const toFieldY = toFieldRect.top - containerRect.top + containerRef.current.scrollTop;

        const rightUnitLevel = line.toUnitLevel ?? toUnit?.level ?? (toUnit?.IsMasterSiblingUnit ? 1 : 2);
        const lineX = getLineXCoordinates(rightUnitLevel);
        coord.startX = lineX.startX;
        coord.endX = lineX.endX;
        coord.midX = lineX.midX;

        // Calculate Y positions (align with field center)
        if (fromFieldY < fromCardPos.y + 20) {
            coord.startY = fromCardPos.y + 25;
        } else if (fromFieldY >= fromCardPos.y + fromCardPos.height - 20) {
            coord.startY = fromCardPos.y + fromCardPos.height + 8;
        } else {
            coord.startY = fromFieldY + 9; // Center of 18px field row
        }

        if (toFieldY < toCardPos.y + 20) {
            coord.endY = toCardPos.y + 25;
        } else if (toFieldY >= toCardPos.y + toCardPos.height - 20) {
            coord.endY = toCardPos.y + toCardPos.height + 8;
        } else {
            coord.endY = toFieldY + 9; // Center of 18px field row
        }

        // L-shaped path: start -> step1 (horizontal) -> step2 (vertical) -> end
        coord.step1X = coord.midX;
        coord.step1Y = coord.startY;
        coord.step2X = coord.midX;
        coord.step2Y = coord.endY;

        return coord;
    }, []);

    // Calculate join lines: from one unit's field to another unit's field
    useEffect(() => {
        const calculateJoinLines = () => {
            const lines: any[] = [];
            
            if (!units || units.length === 0 || !stableDictCurrentPKOrFKLinkToParentKeyGuidMap || Object.keys(stableDictCurrentPKOrFKLinkToParentKeyGuidMap).length === 0) {
                setJoinLines([]);
                return;
            }

            const rootUnit = units[0];
            if (!rootUnit) {
                setJoinLines([]);
                return;
            }

            // Get root unit's primary key fields
            const rootMasterFieldList = rootUnit.AppTransactionFieldList?.filter((f: any) => f.IsPrimaryKey) || [];

            if (rootMasterFieldList.length === 0) {
                setJoinLines([]);
                return;
            }

            // Helper to get unit identifier (Id or uiId for new units)
            const getUnitIdentifier = (unit: any): string | number | undefined => {
                return unit.Id || unit.uiId;
            };

            // Helper to check if two units are different
            const areDifferentUnits = (unit1: any, unit2: any): boolean => {
                const id1 = getUnitIdentifier(unit1);
                const id2 = getUnitIdentifier(unit2);
                // If both have identifiers, compare them
                if (id1 !== undefined && id2 !== undefined) {
                    return id1 !== id2;
                }
                // If one or both don't have identifiers, compare by reference
                return unit1 !== unit2;
            };

            // Process all units to find foreign key relationships
            units.forEach((aUnit: any) => {
                if (aUnit.IsMasterSiblingUnit) {
                    // Master sibling units - link to root unit
                    aUnit.AppTransactionFieldList?.forEach((aTransField: any) => {
                        if (aTransField.RowIdentityGuid && dictCurrentPKOrFKLinkToParentKeyGuidMap[aTransField.RowIdentityGuid]) {
                            const parentGuid = dictCurrentPKOrFKLinkToParentKeyGuidMap[aTransField.RowIdentityGuid];
                            const parentPkField = rootMasterFieldList.find((f: any) => f.RowIdentityGuid === parentGuid);
                            if (parentPkField && areDifferentUnits(rootUnit, aUnit)) {
                                lines.push({
                                    fromUnit: rootUnit,
                                    toUnit: aUnit,
                                    fromField: parentPkField,
                                    toField: aTransField,
                                    toUnitLevel: 1
                                });
                            }
                        }
                    });
                } else {
                    // Child units - link to root unit
                    aUnit.Children?.forEach((aChildUnit: any) => {
                        aChildUnit.AppTransactionFieldList?.forEach((aTransField: any) => {
                            if (aTransField.RowIdentityGuid && dictCurrentPKOrFKLinkToParentKeyGuidMap[aTransField.RowIdentityGuid]) {
                                const parentGuid = dictCurrentPKOrFKLinkToParentKeyGuidMap[aTransField.RowIdentityGuid];
                                const parentPkField = rootMasterFieldList.find((f: any) => f.RowIdentityGuid === parentGuid);
                                if (parentPkField && areDifferentUnits(rootUnit, aChildUnit)) {
                                    lines.push({
                                        fromUnit: rootUnit,
                                        toUnit: aChildUnit,
                                        fromField: parentPkField,
                                        toField: aTransField,
                                        toUnitLevel: 2
                                    });
                                }
                            }
                        });

                        // Grandchild units - link to child unit
                        const childPkFieldList = aChildUnit.AppTransactionFieldList?.filter((f: any) => f.IsPrimaryKey) || [];
                        if (childPkFieldList.length > 0) {
                            aChildUnit.Children?.forEach((aGrandchildUnit: any) => {
                                aGrandchildUnit.AppTransactionFieldList?.forEach((aTransField: any) => {
                                    if (aTransField.RowIdentityGuid && dictCurrentPKOrFKLinkToParentKeyGuidMap[aTransField.RowIdentityGuid]) {
                                        const parentGuid = dictCurrentPKOrFKLinkToParentKeyGuidMap[aTransField.RowIdentityGuid];
                                        const parentPkField = childPkFieldList.find((f: any) => f.RowIdentityGuid === parentGuid);
                                        if (parentPkField && areDifferentUnits(aChildUnit, aGrandchildUnit)) {
                                            lines.push({
                                                fromUnit: aChildUnit,
                                                toUnit: aGrandchildUnit,
                                                fromField: parentPkField,
                                                toField: aTransField,
                                                toUnitLevel: 3
                                            });
                                        }
                                    }
                                });
                            });
                        }
                    });
                }
            });

            // Calculate coordinates after DOM is ready
            // Use longer timeout for new units that might not be rendered yet
            setTimeout(() => {
                const linesWithCoords = lines.map(line => {
                    const coord = calculateLineCoordinate(line);
                    return { ...line, coordinate: coord };
                });
                setJoinLines(linesWithCoords);
            }, 200);
        };

        calculateJoinLines();
    }, [units, stableDictCurrentPKOrFKLinkToParentKeyGuidMap, calculateLineCoordinate]); // Removed expandedUnits - it's not used in the calculation

    // Recalculate on scroll
    useEffect(() => {
        const handleScroll = () => {
            setJoinLines(prevLines => {
                return prevLines.map(line => {
                    const coord = calculateLineCoordinate(line);
                    return { ...line, coordinate: coord };
                });
            });
        };

        const container = containerRef.current;
        if (container) {
            container.addEventListener('scroll', handleScroll);
            return () => container.removeEventListener('scroll', handleScroll);
        }
    }, [calculateLineCoordinate]); // Removed joinLines.length - we only need to set up the listener once

    return (
        <div className="flex flex-col h-full relative">
            {/* Navigate context menu (Unit Navigation button) */}
            {navigateMenu.isOpen && (
                <>
                    <div
                        className="fixed inset-0 z-[10000]"
                        onClick={() => setNavigateMenu({ isOpen: false, unit: null, x: 0, y: 0 })}
                        aria-hidden
                    />
                    <div
                        className={`fixed z-[10001] rounded shadow-lg border py-1 ${theme.mainContentSection}`}
                        style={{ left: navigateMenu.x, top: navigateMenu.y, minWidth: 220 }}
                    >
                        <button
                            type="button"
                            className="w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5"
                            onClick={() => handleNavigateAction(101)}
                        >
                            <span className="w-4 inline-flex items-center justify-center">
                                <i className="fa-solid fa-file-lines" />
                            </span>
                            Navigate To Data Model
                        </button>
                        <button
                            type="button"
                            className="w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5"
                            onClick={() => handleNavigateAction(102)}
                        >
                            <span className="w-4 inline-flex items-center justify-center">
                                <i className="fa-solid fa-magnifying-glass" />
                            </span>
                            Navigate To Search
                        </button>
                        <button
                            type="button"
                            className="w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-black/5"
                            onClick={() => handleNavigateAction(5)}
                        >
                            <span className="w-4 inline-flex items-center justify-center">
                                <i className="fa-solid fa-file" />
                            </span>
                            Navigate To Any Page
                        </button>
                    </div>
                </>
            )}

            {/* Column Headers */}
            <div className="absolute top-0 left-0 right-0 h-[25px] flex" style={{ zIndex: 10 }}>
                {columnLabels.map((label, index) => (
                    <div
                        key={index}
                        className="font-semibold text-xs px-2 pl-8 pt-1"
                        style={{
                            width: `${COLUMN_WIDTH}px`
                        }}
                    >
                        {label}
                    </div>
                ))}
            </div>

            {/* Main Container with Columns */}
            <div
                ref={containerRef}
                className="flex-auto h-1 overflow-auto border border-dashed border-gray-300 mt-[25px]"
            >
                <div className="relative" style={{ minHeight: '1000px', width: `${maxColumns * COLUMN_WIDTH}px` }}>
                    {/* SVG for connection lines */}
                    <svg className="absolute top-0 left-0 w-full h-full pointer-events-none" style={{ zIndex: 1 }}>
                        {joinLines.map((line, index) => {
                            const coord = line.coordinate;
                            if (!coord || coord.startX === undefined) return null;
                            
                            const { startX, startY, step1X, step1Y, step2X, step2Y, endX, endY } = coord;
                            
                            return (
                                <g key={index}>
                                    <path
                                        d={`M ${startX} ${startY} L ${step1X} ${step1Y} L ${step2X} ${step2Y} L ${endX} ${endY}`}
                                        stroke="#666"
                                        strokeWidth="1"
                                        fill="none"
                                    />
                                    <circle
                                        cx={startX + 3.3}
                                        cy={startY}
                                        r="3.3"
                                        fill="gray"
                                    />
                                </g>
                            );
                        })}
                    </svg>

                    {/* Column 1: Master Units */}
                    <div
                        className="absolute top-0 left-0 border-r border-dashed border-gray-300"
                        style={{
                            width: `${COLUMN_WIDTH}px`,
                            height: '100%'
                        }}
                    >
                        {masterUnits.map((unit: any, index: number) => (
                            <UnitCard
                                key={unit.Id || `master-${index}`}
                                unit={{ ...unit, isContainerCollapsed: !expandedUnits.has(unit.Id) }}
                                level={1}
                                onSelect={handleUnitSelect}
                                onEdit={handleUnitEdit}
                                onDelete={handleUnitDelete}
                                onOpenNavigateMenu={handleOpenNavigateMenu}
                                onToggleExpand={toggleExpand}
                                selectedUnitId={selectedUnitId}
                                onFieldLinkChange={onFieldLinkChange}
                                onEditTable={onEditTable}
                                allUnits={units}
                                theme={theme}
                                isPhysicalModelTableCreated={isPhysicalModelTableCreated}
                                showDatabaseTableActions={showDatabaseTableActions}
                                isReadOnly={isReadOnly}
                            />
                        ))}

                        {/* Add Unit Button for Level 1 */}
                        {isAllowAddingUnitOnLevel(1) && (
                            <div className="p-1">
                                <button
                                    onClick={() => handleAddUnit(1, null)}
                                    className={`w-full px-2 py-1 text-xs ${theme.button_default} rounded border`}
                                >
                                    <i className="fa fa-plus mr-1"></i>
                                    Add Unit
                                </button>
                            </div>
                        )}
                    </div>

                    {/* Column 2: Child Units */}
                    {maxColumns >= 2 && (
                        <div
                            className="absolute top-0 border-r border-dashed border-gray-300"
                            style={{
                                left: `${COLUMN_WIDTH}px`,
                                width: `${COLUMN_WIDTH}px`,
                                height: '100%'
                            }}
                        >
                            {childUnits.map((unit: any, index: number) => (
                                <UnitCard
                                    key={unit.Id || `child-${index}`}
                                    unit={{ ...unit, isContainerCollapsed: !expandedUnits.has(unit.Id) }}
                                    level={2}
                                    onSelect={handleUnitSelect}
                                    onEdit={handleUnitEdit}
                                    onDelete={handleUnitDelete}
                                    onOpenNavigateMenu={handleOpenNavigateMenu}
                                    onToggleExpand={toggleExpand}
                                    selectedUnitId={selectedUnitId}
                                    theme={theme}
                                    isPhysicalModelTableCreated={isPhysicalModelTableCreated}
                                    isReadOnly={isReadOnly}
                                    onFieldLinkChange={onFieldLinkChange}
                                    onEditTable={onEditTable}
                                    parentUnit={masterUnits[0]}
                                    allUnits={units}
                                    showDatabaseTableActions={showDatabaseTableActions}
                                />
                            ))}

                            {/* Add Unit Button for Level 2 */}
                            {masterUnits.length > 0 && masterUnits[0] && (
                                <div className="p-1">
                                    <button
                                        onClick={() => handleAddUnit(2, masterUnits[0]?.Id)}
                                        className={`w-full px-2 py-1 text-xs ${theme.button_default} rounded border`}
                                    >
                                        <i className="fa fa-plus mr-1"></i>
                                        Add Unit
                                    </button>
                                </div>
                            )}
                        </div>
                    )}

                    {/* Column 3: Grandchild Units (only for MasterDetail) */}
                    {maxColumns >= 3 && isMasterDetail && (
                        <div
                            className="absolute top-0 border-r border-dashed border-gray-300"
                            style={{
                                left: `${COLUMN_WIDTH * 2}px`,
                                width: `${COLUMN_WIDTH}px`,
                                height: '100%'
                            }}
                        >
                            {childUnits.map((childUnit: any, childIndex: number) => {
                                const grandchildUnits = childUnit.Children || [];
                                return (
                                    <div key={`grandchild-container-${childUnit.Id || childIndex}`}>
                                        {grandchildUnits.map((grandchildUnit: any, grandchildIndex: number) => (
                                            <UnitCard
                                                key={grandchildUnit.Id || `grandchild-${grandchildIndex}`}
                                                unit={{ ...grandchildUnit, isContainerCollapsed: !expandedUnits.has(grandchildUnit.Id) }}
                                                level={3}
                                                onSelect={handleUnitSelect}
                                                onEdit={handleUnitEdit}
                                                onDelete={handleUnitDelete}
                                                onOpenNavigateMenu={handleOpenNavigateMenu}
                                                onToggleExpand={toggleExpand}
                                                selectedUnitId={selectedUnitId}
                                                onFieldLinkChange={onFieldLinkChange}
                                                onEditTable={onEditTable}
                                                parentUnit={childUnit}
                                                allUnits={units}
                                                theme={theme}
                                                isPhysicalModelTableCreated={isPhysicalModelTableCreated}
                                                showDatabaseTableActions={showDatabaseTableActions}
                                                isReadOnly={isReadOnly}
                                            />
                                        ))}
                                        {/* Add Unit Button for Level 3 */}
                                        <div className="p-1">
                                            <button
                                                onClick={() => handleAddUnit(3, childUnit.Id)}
                                                className={`w-full px-2 py-1 text-xs ${theme.button_default} rounded border`}
                                            >
                                                <i className="fa fa-plus mr-1"></i>
                                                Add Unit
                                            </button>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </div>
            </div>

           
        </div>
    );
};

export default UnitStructureEditor;
