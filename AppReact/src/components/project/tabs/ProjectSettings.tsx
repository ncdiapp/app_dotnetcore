import React, { useState, useEffect } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { projectWorkflowService } from '../../../webapi/projectWorkFlowSvc';
import { adminSvc } from '../../../webapi/adminsvc';

interface ProjectSettingsProps {
  currentProject: any | null;
  onSave?: () => Promise<void>;
  onRefresh?: () => Promise<void>;
}

const ProjectSettings: React.FC<ProjectSettingsProps> = ({ currentProject, onSave, onRefresh }) => {
  const { theme } = useTheme();
  const { showError, showInfo } = useErrorMessage();
  const dispatch = useDispatch();

  const [formData, setFormData] = useState<any>(null);
  const [lookupData, setLookupData] = useState<any>({
    currencies: [],
    directions: [
      { Id: 1, Display: 'Forward' },
      { Id: 2, Display: 'Backward' }
    ],
    displayLayouts: [
      { Id: 1, Display: 'Task List' },
      { Id: 2, Display: 'Board Summary' },
      { Id: 3, Display: 'Gantt' }
    ],
    privacyOptions: [
      { Id: 1, Display: 'Cross Domain' },
      { Id: 2, Display: 'Organization' }
    ]
  });

  // Load project settings when currentProject changes
  useEffect(() => {
    if (currentProject?.Id) {
      loadProjectSettings();
    }
  }, [currentProject?.Id]);

  // Load lookup data on mount
  useEffect(() => {
    loadLookupData();
  }, []);

  const loadLookupData = async () => {
    try {
      const data = await adminSvc.getMassEntitiesLookupItem('AppCurrency');
      // getMassEntitiesLookupItem can return either an array or a dictionary
      const currencies = Array.isArray(data) ? data : (data?.AppCurrency || data || []);

      setLookupData((prev: any) => ({
        ...prev,
        currencies: currencies
      }));
    } catch (error: any) {
      console.error('Error loading lookup data:', error);
    }
  };

  const loadProjectSettings = async () => {
    if (!currentProject?.Id) return;

    dispatch(setIsBusy());
    try {
      const settings = await projectWorkflowService.RetrieveProjectSettingExDto(currentProject.Id);
      setFormData(settings);
    } catch (error: any) {
      console.error('Error loading project settings:', error);
      showError(error.message || 'Failed to load project settings');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleFieldChange = (field: string, value: any) => {
    setFormData((prev: any) => ({
      ...prev,
      [field]: value,
      IsModified: true
    }));
  };

  const handleSave = async () => {
    if (!formData) {
      showError('No data to save');
      return;
    }

    if (!formData.Name) {
      showError('Project name is required');
      return;
    }

    dispatch(setIsBusy());
    try {
      await projectWorkflowService.SaveProjectSettingExDto(formData);
      showInfo('Project settings saved successfully');

      // Reload settings
      await loadProjectSettings();

      // Call parent refresh if provided
      if (onRefresh) {
        await onRefresh();
      }
    } catch (error: any) {
      console.error('Error saving project settings:', error);
      showError(error.message || 'Failed to save project settings');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  if (!currentProject) {
    return (
      <div className="w-full h-full flex items-center justify-center">
        <div className={`text-sm ${theme.label}`}>
          Please select a project to view settings
        </div>
      </div>
    );
  }

  if (!formData) {
    return (
      <div className="w-full h-full flex items-center justify-center">
        <div className={`text-sm ${theme.label}`}>Loading project settings...</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      {/* Header with Save button */}
      <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Project Settings: {currentProject.Name}
        </div>
        <div className="flex items-center space-x-2">
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={loadProjectSettings}
          >
            <i className="fa fa-refresh mr-1"></i>
            Refresh
          </button>
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={handleSave}
            disabled={!formData?.IsModified}
          >
            <i className="fa fa-save mr-1"></i>
            Save
          </button>
        </div>
      </div>

      {/* Form Content */}
      <div className="flex-auto overflow-auto p-4">
        <div className="max-w-4xl space-y-6">

          {/* General Information Section */}
          <div className={`p-4 rounded border ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold mb-4 ${theme.title}`}>General Information</div>

            <div className="grid grid-cols-2 gap-4">
              {/* Project Name */}
              <div className="col-span-2">
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>
                  Project Name <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={formData.Name || ''}
                  onChange={(e) => handleFieldChange('Name', e.target.value)}
                />
              </div>

              {/* Description */}
              <div className="col-span-2">
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Description</label>
                <textarea
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  rows={3}
                  value={formData.Description || ''}
                  onChange={(e) => handleFieldChange('Description', e.target.value)}
                />
              </div>

              {/* Display Layout */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Display Layout</label>
                <select
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={formData.DisplayLayoutType || 1}
                  onChange={(e) => handleFieldChange('DisplayLayoutType', parseInt(e.target.value))}
                >
                  {lookupData.displayLayouts.map((layout: any) => (
                    <option key={layout.Id} value={layout.Id}>
                      {layout.Display}
                    </option>
                  ))}
                </select>
              </div>

              {/* Direction */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>
                  Direction <span className="text-red-500">*</span>
                </label>
                <select
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={formData.ProjectDirectionId || 1}
                  onChange={(e) => handleFieldChange('ProjectDirectionId', parseInt(e.target.value))}
                >
                  {lookupData.directions.map((dir: any) => (
                    <option key={dir.Id} value={dir.Id}>
                      {dir.Display}
                    </option>
                  ))}
                </select>
              </div>

              {/* Privacy */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Privacy</label>
                <select
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={formData.EmPrivacy || 1}
                  onChange={(e) => handleFieldChange('EmPrivacy', parseInt(e.target.value))}
                >
                  {lookupData.privacyOptions.map((privacy: any) => (
                    <option key={privacy.Id} value={privacy.Id}>
                      {privacy.Display}
                    </option>
                  ))}
                </select>
              </div>

              {/* Stage (read-only) */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Stage</label>
                <input
                  type="text"
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                  value={formData.ProjectStageDisplay || 'Planning'}
                  readOnly
                />
              </div>
            </div>
          </div>

          {/* Dates Section */}
          <div className={`p-4 rounded border ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold mb-4 ${theme.title}`}>Schedule</div>

            <div className="grid grid-cols-2 gap-4">
              {/* Planned Start */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Planned Start Date</label>
                <input
                  type="date"
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={formData.DatePlannedStart ? new Date(formData.DatePlannedStart).toISOString().split('T')[0] : ''}
                  onChange={(e) => handleFieldChange('DatePlannedStart', e.target.value ? new Date(e.target.value) : null)}
                  disabled={formData.ProjectDirectionId === 2} // Disabled for backward direction
                />
              </div>

              {/* Planned End */}
              <div>
                <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Planned End Date</label>
                <input
                  type="date"
                  className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                  value={formData.DatePlannedEnd ? new Date(formData.DatePlannedEnd).toISOString().split('T')[0] : ''}
                  onChange={(e) => handleFieldChange('DatePlannedEnd', e.target.value ? new Date(e.target.value) : null)}
                  disabled={formData.ProjectDirectionId === 1} // Disabled for forward direction
                />
              </div>

              {/* Actual Start (read-only) */}
              {formData.DateActualStart && (
                <div>
                  <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Actual Start Date</label>
                  <input
                    type="date"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                    value={new Date(formData.DateActualStart).toISOString().split('T')[0]}
                    readOnly
                  />
                </div>
              )}

              {/* Actual End (read-only) */}
              {formData.DateActualEnd && (
                <div>
                  <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Actual End Date</label>
                  <input
                    type="date"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                    value={new Date(formData.DateActualEnd).toISOString().split('T')[0]}
                    readOnly
                  />
                </div>
              )}
            </div>
          </div>

          {/* Budget/Financial Section */}
          <div className={`p-4 rounded border ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold mb-4 ${theme.title}`}>Budget & Financials</div>

            <div className="space-y-4">
              {/* Is Billable */}
              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="isNeedBudget"
                  className="mr-2"
                  checked={formData.IsNeedBudget || false}
                  onChange={(e) => handleFieldChange('IsNeedBudget', e.target.checked)}
                />
                <label htmlFor="isNeedBudget" className={`text-xs ${theme.label}`}>
                  Billable Project (Enable Budget Tracking)
                </label>
              </div>

              {/* Budget Fields (conditional) */}
              {formData.IsNeedBudget && (
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Budget Amount</label>
                    <input
                      type="number"
                      className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                      value={formData.ProjectModelBugestCost || 0}
                      onChange={(e) => handleFieldChange('ProjectModelBugestCost', parseFloat(e.target.value) || 0)}
                    />
                  </div>

                  <div>
                    <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Currency</label>
                    <select
                      className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                      value={formData.CurrencyId || ''}
                      onChange={(e) => handleFieldChange('CurrencyId', parseInt(e.target.value))}
                    >
                      <option value="">Select Currency</option>
                      {lookupData.currencies.map((currency: any) => (
                        <option key={currency.Id} value={currency.Id}>
                          {currency.Display}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Planned Cost (read-only) */}
                  <div>
                    <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Planned Cost</label>
                    <input
                      type="number"
                      className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                      value={formData.PlannedResourceCost || 0}
                      readOnly
                    />
                  </div>

                  {/* Actual Cost (read-only) */}
                  <div>
                    <label className={`block text-xs font-medium mb-1 ${theme.label}`}>Actual Cost</label>
                    <input
                      type="number"
                      className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                      value={formData.ActualResourceCost || 0}
                      readOnly
                    />
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Child/Parent Options (if applicable) */}
          {(formData.ParentProjectId || formData.HasChildren) && (
            <div className={`p-4 rounded border ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold mb-4 ${theme.title}`}>Parent/Child Options</div>

              <div className="space-y-2">
                {formData.HasChildren && (
                  <div className="flex items-center">
                    <input
                      type="checkbox"
                      id="allowTrickleDown"
                      className="mr-2"
                      checked={formData.IsChildProjectAllowParentTtrickleDown || false}
                      onChange={(e) => handleFieldChange('IsChildProjectAllowParentTtrickleDown', e.target.checked)}
                    />
                    <label htmlFor="allowTrickleDown" className={`text-xs ${theme.label}`}>
                      Allow Trickle Down to Child Projects
                    </label>
                  </div>
                )}

                {formData.ParentProjectId && (
                  <div className="flex items-center">
                    <input
                      type="checkbox"
                      id="allowBubbleUp"
                      className="mr-2"
                      checked={formData.IsChildProjectAllowChildBubbleUpParent || false}
                      onChange={(e) => handleFieldChange('IsChildProjectAllowChildBubbleUpParent', e.target.checked)}
                    />
                    <label htmlFor="allowBubbleUp" className={`text-xs ${theme.label}`}>
                      Allow Bubble Up to Parent Project
                    </label>
                  </div>
                )}
              </div>
            </div>
          )}

        </div>
      </div>
    </div>
  );
};

export default ProjectSettings;
