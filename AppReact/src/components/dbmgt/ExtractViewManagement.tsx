import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { searchSvc } from '../../webapi/searchSvc';

// Aggregation function options
const aggFunctionOptions = [
  { Id: '', Display: '(None)' },
  { Id: 'SUM', Display: 'Sum' },
  { Id: 'COUNT', Display: 'Count' },
  { Id: 'AVG', Display: 'Average' },
  { Id: 'MIN', Display: 'Min' },
  { Id: 'MAX', Display: 'Max' },
  { Id: 'FIRST', Display: 'First' },
  { Id: 'LAST', Display: 'Last' }
];

interface ExtractViewField {
  Id?: number;
  DataSetId?: number;
  DbfiledName?: string;
  Description?: string;
  IsGroup?: boolean;
  AggFunction?: string;
  IsModified?: boolean;
}

interface ExtractView {
  Id?: number;
  Name: string;
  Description?: string;
  BaseDataSetId?: number;
  AppDateSetDataExtractViewList?: ExtractViewField[];
  DeletedItemsIds?: number[];
}

interface BaseDataSet {
  Id: number;
  Name: string;
}

const ExtractViewManagement: React.FC = () => {
  const { theme, t } = useTheme();
  const dispatch = useDispatch();
  const { showError, showInfo, showWarning } = useErrorMessage();

  // State
  const [extractViews, setExtractViews] = useState<ExtractView[]>([]);
  const [extractViewsCV, setExtractViewsCV] = useState<CollectionView | null>(null);
  const [baseDataSets, setBaseDataSets] = useState<BaseDataSet[]>([]);
  const [currentExtractView, setCurrentExtractView] = useState<ExtractView | null>(null);
  const [currentFieldsCV, setCurrentFieldsCV] = useState<CollectionView | null>(null);
  const [dataSetColumns, setDataSetColumns] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isModified, setIsModified] = useState(false);

  // New extract view dialog state
  const [showNewDialog, setShowNewDialog] = useState(false);
  const [newExtractView, setNewExtractView] = useState<ExtractView>({
    Name: '',
    Description: '',
    BaseDataSetId: undefined
  });

  // Refs
  const extractViewsGridRef = useRef<wjGrid.FlexGrid | null>(null);
  const fieldsGridRef = useRef<wjGrid.FlexGrid | null>(null);
  const baseDataSetDataMapRef = useRef<DataMap | null>(null);
  const aggFunctionDataMapRef = useRef<DataMap | null>(null);
  const columnsDataMapRef = useRef<DataMap | null>(null);

  // Initialize DataMaps
  useEffect(() => {
    aggFunctionDataMapRef.current = new DataMap(aggFunctionOptions, 'Id', 'Display');
  }, []);

  useEffect(() => {
    if (baseDataSets.length > 0) {
      baseDataSetDataMapRef.current = new DataMap(baseDataSets, 'Id', 'Name');
    }
  }, [baseDataSets]);

  useEffect(() => {
    if (dataSetColumns.length > 0) {
      columnsDataMapRef.current = new DataMap(dataSetColumns, 'Id', 'Id');
    }
  }, [dataSetColumns]);

  // Load data
  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      // Load all datasets (to get base datasets)
      const allDataSets = await searchSvc.retrieveAllAppDataSetEntityDto();
      const baseSets = Array.isArray(allDataSets)
        ? allDataSets.filter((ds: any) => !ds.BaseDataSetId)
        : [];
      setBaseDataSets(baseSets);

      // Load extract views
      const extractViewList = await searchSvc.retrieveExtractDataSetList();
      setExtractViews(Array.isArray(extractViewList) ? extractViewList : []);
      setExtractViewsCV(new CollectionView(Array.isArray(extractViewList) ? extractViewList : []));
    } catch (error) {
      showError('Failed to load data: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  };

  // Handle extract view selection
  const handleSelectExtractView = async (extractView: ExtractView) => {
    if (!extractView?.Id || !extractView?.BaseDataSetId) return;

    dispatch(setIsBusy());
    try {
      // Load full extract view details
      const fullExtractView = await searchSvc.retrieveOneExtractAppDataSetExDto(extractView.Id.toString());

      // Load column list for the base dataset
      const columns = await searchSvc.retrieveQueryColumnList(fullExtractView.BaseDataSetId.toString());
      setDataSetColumns(Array.isArray(columns) ? columns : []);

      // Set current extract view
      setCurrentExtractView({
        ...fullExtractView,
        DeletedItemsIds: []
      });
      setCurrentFieldsCV(new CollectionView(fullExtractView.AppDateSetDataExtractViewList || []));
      setIsModified(false);
    } catch (error) {
      showError('Failed to load extract view: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Handle extract view grid selection change
  const handleExtractViewSelectionChanged = (grid: wjGrid.FlexGrid) => {
    const sel = grid.selection;
    if (sel.row >= 0 && grid.rows[sel.row]) {
      const selectedView = grid.rows[sel.row].dataItem;
      handleSelectExtractView(selectedView);
    }
  };

  // Add new field
  const handleAddField = () => {
    if (!currentExtractView) return;

    const newField: ExtractViewField = {
      DataSetId: currentExtractView.Id,
      DbfiledName: '',
      Description: '',
      IsGroup: false,
      AggFunction: ''
    };

    const updatedFields = [...(currentExtractView.AppDateSetDataExtractViewList || []), newField];
    setCurrentExtractView({
      ...currentExtractView,
      AppDateSetDataExtractViewList: updatedFields
    });
    setCurrentFieldsCV(new CollectionView(updatedFields));
    setIsModified(true);
  };

  // Remove field
  const handleRemoveField = () => {
    if (!fieldsGridRef.current || !currentExtractView) return;

    const grid = fieldsGridRef.current;
    const sel = grid.selection;
    if (sel.row < 0) {
      showWarning('Please select a field to remove');
      return;
    }

    const selectedField = grid.rows[sel.row]?.dataItem;
    if (!selectedField) return;

    // Track deleted IDs for existing fields
    const deletedIds = [...(currentExtractView.DeletedItemsIds || [])];
    if (selectedField.Id) {
      deletedIds.push(selectedField.Id);
    }

    const updatedFields = (currentExtractView.AppDateSetDataExtractViewList || [])
      .filter((f: ExtractViewField) => f !== selectedField);

    setCurrentExtractView({
      ...currentExtractView,
      AppDateSetDataExtractViewList: updatedFields,
      DeletedItemsIds: deletedIds
    });
    setCurrentFieldsCV(new CollectionView(updatedFields));
    setIsModified(true);
  };

  // Save current extract view
  const handleSaveExtractView = async () => {
    if (!currentExtractView) return;

    dispatch(setIsBusy());
    try {
      const result = await searchSvc.saveOneExtractAppDataSetExDto(currentExtractView);

      if (result?.IsSuccessful && result?.Object) {
        showInfo('Extract view saved successfully');
        setIsModified(false);
        await loadData();
        // Re-select the current view
        handleSelectExtractView(result.Object);
      } else {
        const errorMsg = result?.ValidationResult?.Items?.[0]?.ErrorMessage || 'Failed to save extract view';
        showError(errorMsg);
      }
    } catch (error) {
      showError('Failed to save extract view: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Delete current extract view
  const handleDeleteExtractView = async () => {
    if (!currentExtractView?.Id) return;

    const confirmed = window.confirm(`Confirm to delete: ${currentExtractView.Name}`);
    if (!confirmed) return;

    dispatch(setIsBusy());
    try {
      const result = await searchSvc.deleteOneExtractAppDataSetExDto(currentExtractView.Id.toString());

      if (result?.IsSuccessful) {
        showInfo('Extract view deleted successfully');
        setCurrentExtractView(null);
        setCurrentFieldsCV(null);
        await loadData();
      } else {
        const errorMsg = result?.ValidationResult?.Items?.[0]?.ErrorMessage || 'Failed to delete extract view';
        showError(errorMsg);
      }
    } catch (error) {
      showError('Failed to delete extract view: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Create new extract view
  const handleCreateExtractView = async () => {
    if (!newExtractView.Name?.trim()) {
      showWarning('Please enter a name for the extract view');
      return;
    }
    if (!newExtractView.BaseDataSetId) {
      showWarning('Please select a base dataset');
      return;
    }

    dispatch(setIsBusy());
    try {
      const result = await searchSvc.saveOneExtractAppDataSetExDto(newExtractView);

      if (result?.IsSuccessful && result?.Object) {
        showInfo('Extract view created successfully');
        setShowNewDialog(false);
        setNewExtractView({ Name: '', Description: '', BaseDataSetId: undefined });
        await loadData();
        // Select the newly created view
        handleSelectExtractView(result.Object);
      } else {
        const errorMsg = result?.ValidationResult?.Items?.[0]?.ErrorMessage || 'Failed to create extract view';
        showError(errorMsg);
      }
    } catch (error) {
      showError('Failed to create extract view: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Handle field cell edit
  const handleFieldCellEditEnded = (grid: wjGrid.FlexGrid) => {
    setIsModified(true);
    // Update the current extract view with the modified fields
    if (currentExtractView && currentFieldsCV) {
      setCurrentExtractView({
        ...currentExtractView,
        AppDateSetDataExtractViewList: currentFieldsCV.items
      });
    }
  };

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Extract View Management
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={() => loadData()}
            disabled={isLoading}
            className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate"></i>
          </button>
          <button
            onClick={() => setShowNewDialog(true)}
            disabled={isLoading}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} flex items-center gap-1`}
          >
            <i className="fa-solid fa-plus"></i>
            <span>New</span>
          </button>
        </div>
      </div>

      {/* Main content */}
      <div className={`w-full h-1 flex-auto overflow-hidden flex gap-1`}>
        {/* Left panel - Extract views list */}
        <div className={`w-[300px] flex flex-col ${theme.mainContentSection}`}>
          <div className={`px-3 py-2 border-b font-semibold text-sm ${t('border_default')} ${theme.title}`}>
            Extract Views ({extractViews.length})
          </div>
          <div className="h-1 flex-auto overflow-hidden">
            <FlexGrid
              itemsSource={extractViewsCV || undefined}
              autoGenerateColumns={false}
              selectionMode="Row"
              headersVisibility="Column"
              isReadOnly={true}
              initialized={(grid: wjGrid.FlexGrid) => { extractViewsGridRef.current = grid; }}
              selectionChanged={handleExtractViewSelectionChanged}
              className="w-full h-full"
            >
              <FlexGridColumn header="Name" binding="Name" width="*" />
              <FlexGridColumn
                header="Base Dataset"
                binding="BaseDataSetId"
                width={120}
                dataMap={baseDataSetDataMapRef.current || undefined}
              />
            </FlexGrid>
          </div>
        </div>

        {/* Right panel - Current extract view details */}
        <div className={`w-1 flex-auto flex flex-col ${theme.mainContentSection}`}>
          {currentExtractView ? (
            <>
              {/* Extract view properties */}
              <div className={`px-3 py-2 border-b ${t('border_default')}`}>
                <div className="grid grid-cols-2 gap-3">
                  <div className="flex items-center py-1">
                    <label className={`w-32 text-xs ${theme.label} mr-2`}>Name:</label>
                    <input
                      type="text"
                      value={currentExtractView.Name || ''}
                      onChange={(e) => {
                        setCurrentExtractView({ ...currentExtractView, Name: e.target.value });
                        setIsModified(true);
                      }}
                      autoComplete="off"
                      className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                    />
                  </div>
                  <div className="flex items-center py-1">
                    <label className={`w-32 text-xs ${theme.label} mr-2`}>Base Dataset:</label>
                    <span className="text-xs">{baseDataSets.find(ds => ds.Id === currentExtractView.BaseDataSetId)?.Name || '-'}</span>
                  </div>
                  <div className="flex items-center col-span-2 py-1">
                    <label className={`w-32 text-xs ${theme.label} mr-2`}>Description:</label>
                    <input
                      type="text"
                      value={currentExtractView.Description || ''}
                      onChange={(e) => {
                        setCurrentExtractView({ ...currentExtractView, Description: e.target.value });
                        setIsModified(true);
                      }}
                      autoComplete="off"
                      className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                    />
                  </div>
                </div>
              </div>

              {/* Fields toolbar */}
              <div className={`px-3 py-2 border-b flex items-center justify-between ${t('border_default')}`}>
                <span className={`text-sm font-semibold ${theme.title}`}>Fields</span>
                <div className="flex items-center gap-2">
                  <button
                    onClick={handleAddField}
                    className={`w-8 h-6 rounded-[4px] text-xs ${theme.button_default}`}
                    title="Add Field"
                  >
                    <i className="fa-solid fa-plus"></i>
                  </button>
                  <button
                    onClick={handleRemoveField}
                    className={`w-8 h-6 rounded-[4px] text-xs ${theme.button_secondary}`}
                    title="Remove Field"
                  >
                    <i className="fa-solid fa-minus"></i>
                  </button>
                  <button
                    onClick={handleSaveExtractView}
                    disabled={!isModified}
                    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50 flex items-center gap-1`}
                  >
                    <i className="fa-solid fa-save"></i>
                    Save
                  </button>
                  <button
                    onClick={handleDeleteExtractView}
                    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary} flex items-center gap-1`}
                  >
                    <i className="fa-solid fa-trash"></i>
                    Delete
                  </button>
                </div>
              </div>

              {/* Fields grid */}
              <div className="h-1 flex-auto overflow-hidden p-2">
                <FlexGrid
                  itemsSource={currentFieldsCV || undefined}
                  autoGenerateColumns={false}
                  selectionMode="Row"
                  headersVisibility="Column"
                  initialized={(grid:wjGrid.FlexGrid) => { fieldsGridRef.current = grid; }}
                  cellEditEnded={handleFieldCellEditEnded}
                  className="w-full h-full"
                >
                  <FlexGridColumn
                    header="Field Name"
                    binding="DbfiledName"
                    width={180}
                    dataMap={columnsDataMapRef.current || undefined}
                  />
                  <FlexGridColumn header="Description" binding="Description" width={200} />
                  <FlexGridColumn
                    header="Is Group"
                    binding="IsGroup"
                    width={80}
                    dataType={1}
                  />
                  <FlexGridColumn
                    header="Aggregation"
                    binding="AggFunction"
                    width={120}
                    dataMap={aggFunctionDataMapRef.current || undefined}
                  />
                  <FlexGridColumn header="" binding="" width="*" isReadOnly={true} />
                </FlexGrid>
              </div>
            </>
          ) : (
            <div className={`flex items-center justify-center h-full ${theme.label}`}>
              <div className="text-center">
                <i className="fa-solid fa-table-columns text-4xl mb-2 opacity-50"></i>
                <div className="text-sm">Select an extract view to edit</div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* New Extract View Dialog */}
      {showNewDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className={`rounded-lg shadow-lg w-[450px] border ${t('border_default')} ${theme.mainContentSection}`}>
            <div className={`px-4 py-3 border-b flex items-center justify-between ${t('border_default')} ${theme.mainHeader}`}>
              <span className={`font-semibold ${theme.title}`}>Create New Extract View</span>
              <button
                onClick={() => setShowNewDialog(false)}
                className={theme.label}
              >
                <i className="fa-solid fa-times"></i>
              </button>
            </div>
            <div className="p-4 space-y-3">
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Name:</label>
                <input
                  type="text"
                  value={newExtractView.Name || ''}
                  onChange={(e) => setNewExtractView({ ...newExtractView, Name: e.target.value })}
                  autoComplete="off"
                  className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  placeholder="Enter extract view name"
                />
              </div>
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Base Dataset:</label>
                <select
                  value={newExtractView.BaseDataSetId || ''}
                  onChange={(e) => setNewExtractView({ ...newExtractView, BaseDataSetId: parseInt(e.target.value) })}
                  className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                >
                  <option value="">Select Base Dataset</option>
                  {baseDataSets.map((ds) => (
                    <option key={ds.Id} value={ds.Id}>{ds.Name}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Description:</label>
                <input
                  type="text"
                  value={newExtractView.Description || ''}
                  onChange={(e) => setNewExtractView({ ...newExtractView, Description: e.target.value })}
                  autoComplete="off"
                  className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  placeholder="Optional description"
                />
              </div>
            </div>
            <div className={`px-4 py-3 border-t flex justify-end gap-2 ${t('border_default')}`}>
              <button
                onClick={() => setShowNewDialog(false)}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Cancel
              </button>
              <button
                onClick={handleCreateExtractView}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Create
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ExtractViewManagement;
