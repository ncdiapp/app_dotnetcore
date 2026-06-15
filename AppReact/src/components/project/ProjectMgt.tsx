import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { projectWorkflowService } from '../../webapi/projectWorkFlowSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import ProjectTaskList from './tabs/ProjectTaskList';
import ProjectGanttChart from './tabs/ProjectGanttChart';
import ProjectSettings from './tabs/ProjectSettings';

interface DataModel {
  // Project data
  allProjectList: any[];
  allProjectCV: CollectionView<any> | null;
  currentProject: any | null;

  // Lookups for form dropdowns
  domainCV: CollectionView<any> | null;
  currencyCV: CollectionView<any> | null;

  // UI state
  selectedTab: number; // 1=Tasks, 2=Gantt, 3=Settings
  showNewProjectDialog: boolean;
  newProject: any | null;

  // Context menu
  uiControl: {
    contextMenu: { visible: boolean; x: number; y: number; item: any };
  };
}

const ProjectMgt: React.FC = () => {
  const { theme } = useTheme();
  const { showError, showInfo } = useErrorMessage();
  const dispatch = useDispatch();
  const flex = useRef<any>(null);

  const [dataModel, setDataModel] = useState<DataModel>({
    allProjectList: [],
    allProjectCV: null,
    currentProject: null,
    domainCV: null,
    currencyCV: null,
    selectedTab: 1,
    showNewProjectDialog: false,
    newProject: null,
    uiControl: {
      contextMenu: { visible: false, x: 0, y: 0, item: null }
    }
  });

  // Initialize CollectionViews after mount
  useEffect(() => {
    setDataModel(prev => ({
      ...prev,
      allProjectCV: new CollectionView<any>([]),
      domainCV: new CollectionView<any>([]),
      currencyCV: new CollectionView<any>([])
    }));
  }, []);

  // Note: useTabDataAutoCache is not used here because CollectionView objects
  // cannot be serialized/deserialized properly. Manual caching would need to
  // exclude CollectionView objects and recreate them on restoration.

  // Load projects from server
  const loadProjectsFromServer = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      // Load projects with hierarchy for grouping
      const projects = await projectWorkflowService.RetrieveAppProjectOrWorkFlows(1, null, true);

      // Create CollectionView with grouping
      const cv = new CollectionView(projects || []);
      cv.groupDescriptions.push(new PropertyGroupDescription('ProjectStageDisplay'));

      setDataModel(prev => ({
        ...prev,
        allProjectList: projects || [],
        allProjectCV: cv
      }));
    } catch (error: any) {
      console.error('Error loading projects:', error);
      showError(error.message || 'Failed to load projects');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  // Load lookup data for new project form
  const loadLookupData = useCallback(async () => {
    try {
      const entityCode_AppSecurityRegDomain = 'AppSecurityRegDomain';
      const entityCode_AppCurrency = 'AppCurrency';

      const entityCodeList: string[] = [];
      entityCodeList.push(entityCode_AppSecurityRegDomain);
      entityCodeList.push(entityCode_AppCurrency);

      const entityCodesAggregation = entityCodeList.join('|');

      const dictEntityCodeAndListItems = await adminSvc.getMassEntitiesLookupItem(entityCodesAggregation);

      setDataModel(prev => ({
        ...prev,
        domainCV: new CollectionView(dictEntityCodeAndListItems[entityCode_AppSecurityRegDomain] || []),
        currencyCV: new CollectionView(dictEntityCodeAndListItems[entityCode_AppCurrency] || [])
      }));
    } catch (error: any) {
      console.error('Error loading lookup data:', error);
      // Non-critical, don't show error to user
    }
  }, []);

  // Initial data load
  useEffect(() => {
    loadProjectsFromServer();
    loadLookupData();
  }, [loadProjectsFromServer, loadLookupData]);

  // Handle project selection from grid
  const handleProjectSelection = useCallback(() => {
    if (flex.current && flex.current.control) {
      const grid = flex.current.control;
      const selectedItem = grid.selectedItems?.[0];

      if (selectedItem) {
        setDataModel(prev => ({
          ...prev,
          currentProject: selectedItem,
          selectedTab: 1 // Switch to Tasks tab
        }));
      }
    }
  }, []);

  // Handle tab change
  const handleTabChange = useCallback((tabIndex: number) => {
    setDataModel(prev => ({
      ...prev,
      selectedTab: tabIndex
    }));
  }, []);

  // Render tab content
  const renderTabContent = () => {
    switch (dataModel.selectedTab) {
      case 1:
        return <ProjectTaskList currentProject={dataModel.currentProject} />;
      case 2:
        return <ProjectGanttChart currentProject={dataModel.currentProject} />;
      case 3:
        return <ProjectSettings currentProject={dataModel.currentProject} />;
      default:
        return null;
    }
  };

  // Open new project dialog
  const openNewProjectDialog = useCallback(() => {
    const today = new Date();
    const startOfDay = new Date(today.getFullYear(), today.getMonth(), today.getDate(), 0, 0, 0);
    const endOfDay = new Date(today.getFullYear(), today.getMonth(), today.getDate(), 23, 59, 59);

    setDataModel(prev => ({
      ...prev,
      showNewProjectDialog: true,
      newProject: {
        Name: 'New Project',
        Description: '',
        ProjectDirectionId: 1, // Forward
        ProjectWorkflowType: 1, // Project
        DisplayLayoutType: 1, // TaskList
        EmPrivacy: 1, // CrossDomain
        DatePlannedStart: startOfDay,
        DatePlannedEnd: endOfDay,
        DefaultGanttDisplayUnit: 1,
        IsChildProjectAllowParentTtrickleDown: true,
        IsChildProjectAllowChildBubbleUpParent: true,
        IsBillable: false,
        BudgetAmount: 0,
        CurrencyId: null
      }
    }));
  }, []);

  // Close new project dialog
  const closeNewProjectDialog = useCallback(() => {
    setDataModel(prev => ({
      ...prev,
      showNewProjectDialog: false,
      newProject: null
    }));
  }, []);

  // Save new project
  const saveNewProject = useCallback(async () => {
    if (!dataModel.newProject?.Name) {
      showError('Project name is required');
      return;
    }

    dispatch(setIsBusy());
    try {
      const result = await projectWorkflowService.SaveProjectSettingExDto(dataModel.newProject);
      showInfo('Project created successfully');
      closeNewProjectDialog();

      // Reload projects and select the new one
      await loadProjectsFromServer();

      // Find and select the new project
      if (result?.Id) {
        setDataModel(prev => {
          const newProject = prev.allProjectList.find(p => p.Id === result.Id);
          return {
            ...prev,
            currentProject: newProject || null,
            selectedTab: 3 // Switch to Project Settings tab
          };
        });
      }
    } catch (error: any) {
      console.error('Error saving project:', error);
      showError(error.message || 'Failed to save project');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataModel.newProject, dispatch, showError, showInfo, closeNewProjectDialog, loadProjectsFromServer]);

  // Delete selected project
  const deleteSelectedProject = useCallback(async () => {
    if (!dataModel.currentProject) {
      showError('Please select a project to delete');
      return;
    }

    if (!window.confirm(`Are you sure you want to delete project "${dataModel.currentProject.Name}"?`)) {
      return;
    }

    dispatch(setIsBusy());
    try {
      await projectWorkflowService.DeleteProjectWorkFlow(dataModel.currentProject.Id);
      showInfo('Project deleted successfully');

      // Clear selection and reload
      setDataModel(prev => ({
        ...prev,
        currentProject: null
      }));
      await loadProjectsFromServer();
    } catch (error: any) {
      console.error('Error deleting project:', error);
      showError(error.message || 'Failed to delete project');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataModel.currentProject, dispatch, showError, showInfo, loadProjectsFromServer]);

  // Update new project form field
  const updateNewProjectField = useCallback((field: string, value: any) => {
    setDataModel(prev => ({
      ...prev,
      newProject: prev.newProject ? {
        ...prev.newProject,
        [field]: value
      } : null
    }));
  }, []);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header / Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Project Management</div>
        <div className="flex items-center space-x-2">
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={loadProjectsFromServer}
          >
            <i className="fa fa-refresh mr-1"></i>
            Refresh
          </button>
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={openNewProjectDialog}
          >
            <i className="fa fa-plus mr-1"></i>
            Add
          </button>
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={deleteSelectedProject}
            disabled={!dataModel.currentProject}
          >
            <i className="fa fa-trash mr-1"></i>
            Delete
          </button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className={`flex-auto overflow-hidden flex ${theme.mainContentSection}`}>
        {/* Left Panel - Project Tree */}
        <div className="w-[350px] flex flex-col border-r overflow-hidden">
          <div className="flex-auto overflow-hidden">
            {dataModel.allProjectCV && (
              <FlexGrid
                ref={flex}
                itemsSource={dataModel.allProjectCV}
                isReadOnly={true}
                selectionMode="Row"
                selectionChanged={handleProjectSelection}
                style={{ width: '100%', height: '100%' }}
              >
                <FlexGridFilter filterColumns={['Name', 'ProjectStageDisplay']} />
                <FlexGridColumn header="Project Name" binding="Name" width="*" />
                <FlexGridColumn header="Stage" binding="ProjectStageDisplay" width={120} />
              </FlexGrid>
            )}
          </div>
        </div>

        {/* Right Panel - Tabs */}
        <div className="flex-auto flex flex-col overflow-hidden">
          {/* Tab Bar */}
          <div className={`flex border-b ${theme.mainContentSection}`}>
            <button
              className={`px-4 py-2 text-xs font-medium border-b-2 ${
                dataModel.selectedTab === 1
                  ? `border-blue-500 ${theme.title}`
                  : `border-transparent ${theme.label}`
              }`}
              onClick={() => handleTabChange(1)}
            >
              <i className="fa fa-tasks mr-1"></i>
              Tasks
            </button>
            <button
              className={`px-4 py-2 text-xs font-medium border-b-2 ${
                dataModel.selectedTab === 2
                  ? `border-blue-500 ${theme.title}`
                  : `border-transparent ${theme.label}`
              }`}
              onClick={() => handleTabChange(2)}
            >
              <i className="fa fa-bar-chart mr-1"></i>
              Gantt Chart
            </button>
            <button
              className={`px-4 py-2 text-xs font-medium border-b-2 ${
                dataModel.selectedTab === 3
                  ? `border-blue-500 ${theme.title}`
                  : `border-transparent ${theme.label}`
              }`}
              onClick={() => handleTabChange(3)}
            >
              <i className="fa fa-cog mr-1"></i>
              Project Settings
            </button>
          </div>

          {/* Tab Content */}
          <div className="flex-auto overflow-auto">
            {renderTabContent()}
          </div>
        </div>
      </div>

      {/* New Project Dialog */}
      {dataModel.showNewProjectDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className={`${theme.mainContentSection} rounded-lg shadow-xl w-[600px] max-h-[80vh] overflow-auto`}>
            {/* Dialog Header */}
            <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.title}`}>
              <div className="text-md font-semibold">New Project</div>
              <button onClick={closeNewProjectDialog} className="text-xl">
                <i className="fa fa-times"></i>
              </button>
            </div>

            {/* Dialog Body */}
            <div className="px-4 py-4 space-y-4">
              {/* Name */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>
                  Project Name <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={dataModel.newProject?.Name || ''}
                  onChange={(e) => updateNewProjectField('Name', e.target.value)}
                />
              </div>

              {/* Description */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Description</label>
                <textarea
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  rows={3}
                  value={dataModel.newProject?.Description || ''}
                  onChange={(e) => updateNewProjectField('Description', e.target.value)}
                />
              </div>

              {/* Display Layout Type */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Display Layout</label>
                <select
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={dataModel.newProject?.DisplayLayoutType || 1}
                  onChange={(e) => updateNewProjectField('DisplayLayoutType', parseInt(e.target.value))}
                >
                  <option value={1}>Task List</option>
                  <option value={2}>Board</option>
                  <option value={3}>Gantt</option>
                </select>
              </div>

              {/* Privacy */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Privacy</label>
                <select
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={dataModel.newProject?.EmPrivacy || 1}
                  onChange={(e) => updateNewProjectField('EmPrivacy', parseInt(e.target.value))}
                >
                  <option value={1}>Cross Domain</option>
                  <option value={2}>Private</option>
                </select>
              </div>

              {/* Date Range */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Start Date</label>
                  <input
                    type="date"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={dataModel.newProject?.DatePlannedStart ? new Date(dataModel.newProject.DatePlannedStart).toISOString().split('T')[0] : ''}
                    onChange={(e) => updateNewProjectField('DatePlannedStart', new Date(e.target.value))}
                  />
                </div>
                <div>
                  <label className={`block text-xs font-medium mb-1 ${theme.label}`}>End Date</label>
                  <input
                    type="date"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={dataModel.newProject?.DatePlannedEnd ? new Date(dataModel.newProject.DatePlannedEnd).toISOString().split('T')[0] : ''}
                    onChange={(e) => updateNewProjectField('DatePlannedEnd', new Date(e.target.value))}
                  />
                </div>
              </div>

              {/* Is Billable */}
              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="isBillable"
                  className="mr-2"
                  checked={dataModel.newProject?.IsBillable || false}
                  onChange={(e) => updateNewProjectField('IsBillable', e.target.checked)}
                />
                <label htmlFor="isBillable" className={`text-xs ${theme.label}`}>
                  Billable Project
                </label>
              </div>

              {/* Budget Amount (conditional) */}
              {dataModel.newProject?.IsBillable && (
                <>
                  <div>
                    <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Budget Amount</label>
                    <input
                      type="number"
                      className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                      value={dataModel.newProject?.BudgetAmount || 0}
                      onChange={(e) => updateNewProjectField('BudgetAmount', parseFloat(e.target.value))}
                    />
                  </div>

                  {/* Currency */}
                  <div>
                    <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Currency</label>
                    <select
                      className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                      value={dataModel.newProject?.CurrencyId || ''}
                      onChange={(e) => updateNewProjectField('CurrencyId', e.target.value)}
                    >
                      <option value="">Select Currency</option>
                      {dataModel.currencyCV?.sourceCollection.map((currency: any) => (
                        <option key={currency.Id} value={currency.Id}>
                          {currency.Display}
                        </option>
                      ))}
                    </select>
                  </div>
                </>
              )}
            </div>

            {/* Dialog Footer */}
            <div className="flex justify-end space-x-2 px-4 py-3 border-t">
              <button
                className={`px-4 py-1 rounded text-xs ${theme.button_default}`}
                onClick={closeNewProjectDialog}
              >
                Cancel
              </button>
              <button
                className={`px-4 py-1 rounded text-xs ${theme.button_default}`}
                onClick={saveNewProject}
              >
                <i className="fa fa-save mr-1"></i>
                Save
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ProjectMgt;
