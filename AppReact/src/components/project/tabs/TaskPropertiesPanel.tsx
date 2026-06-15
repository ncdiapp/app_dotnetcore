import React, { useState, useEffect } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

interface TaskPropertiesPanelProps {
  task: any | null;
  projectId: number;
  onTaskUpdate: (task: any) => void;
}

const TaskPropertiesPanel: React.FC<TaskPropertiesPanelProps> = ({ task, projectId, onTaskUpdate }) => {
  const { theme } = useTheme();
  const [activeTab, setActiveTab] = useState(0);
  const [formData, setFormData] = useState<any>(null);

  // Update form data when task changes
  useEffect(() => {
    if (task) {
      setFormData({ ...task });
    } else {
      setFormData(null);
    }
  }, [task]);

  const handleFieldChange = (field: string, value: any) => {
    if (!formData) return;

    const updated = {
      ...formData,
      [field]: value
    };

    setFormData(updated);
    onTaskUpdate(updated);
  };

  if (!task) {
    return (
      <div className="w-full h-full flex items-center justify-center p-4">
        <div className={`text-sm text-center ${theme.label}`}>
          <i className="fa fa-arrow-left text-2xl mb-2"></i>
          <p>Select a task to view properties</p>
        </div>
      </div>
    );
  }

  if (!formData) {
    return (
      <div className="w-full h-full flex items-center justify-center">
        <div className={`text-sm ${theme.label}`}>Loading...</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      {/* Tab Bar */}
      <div className={`flex border-b ${theme.mainContentSection}`}>
        <button
          className={`px-3 py-2 text-xs font-medium border-b-2 ${
            activeTab === 0
              ? `border-blue-500 ${theme.title}`
              : `border-transparent ${theme.label}`
          }`}
          onClick={() => setActiveTab(0)}
        >
          Task Info
        </button>
        {task.Id && task.Id > 0 && (
          <>
            <button
              className={`px-3 py-2 text-xs font-medium border-b-2 ${
                activeTab === 1
                  ? `border-blue-500 ${theme.title}`
                  : `border-transparent ${theme.label}`
              }`}
              onClick={() => setActiveTab(1)}
            >
              Checklist
            </button>
            <button
              className={`px-3 py-2 text-xs font-medium border-b-2 ${
                activeTab === 2
                  ? `border-blue-500 ${theme.title}`
                  : `border-transparent ${theme.label}`
              }`}
              onClick={() => setActiveTab(2)}
            >
              Time & Expense
            </button>
          </>
        )}
      </div>

      {/* Tab Content */}
      <div className="flex-auto overflow-auto p-3">
        {activeTab === 0 && (
          <div className="space-y-4">
            {/* Properties Section */}
            <div>
              <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Properties</div>

              <div className="space-y-2">
                {/* Task Name */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Name</label>
                  <input
                    type="text"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={formData.Name || ''}
                    onChange={(e) => handleFieldChange('Name', e.target.value)}
                  />
                </div>

                {/* Description */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Description</label>
                  <textarea
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    rows={3}
                    value={formData.Description || ''}
                    onChange={(e) => handleFieldChange('Description', e.target.value)}
                  />
                </div>

                {/* Task Type */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Type</label>
                  <select
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={formData.EmTaskType || 1}
                    onChange={(e) => handleFieldChange('EmTaskType', parseInt(e.target.value))}
                  >
                    <option value={1}>Normal Task</option>
                    <option value={2}>Milestone</option>
                  </select>
                </div>

                {/* Priority */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Priority</label>
                  <select
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={formData.EmPriority || 2}
                    onChange={(e) => handleFieldChange('EmPriority', parseInt(e.target.value))}
                  >
                    <option value={1}>High</option>
                    <option value={2}>Medium</option>
                    <option value={3}>Low</option>
                  </select>
                </div>

                {/* Weight */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Weight</label>
                  <input
                    type="number"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={formData.Weight || 0}
                    onChange={(e) => handleFieldChange('Weight', parseFloat(e.target.value) || 0)}
                    step="0.1"
                  />
                </div>
              </div>
            </div>

            {/* Dates Section */}
            <div>
              <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Schedule</div>

              <div className="space-y-2">
                {/* Planned Start */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Planned Start</label>
                  <input
                    type="date"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={formData.DatePlannedStart ? new Date(formData.DatePlannedStart).toISOString().split('T')[0] : ''}
                    onChange={(e) => handleFieldChange('DatePlannedStart', e.target.value ? new Date(e.target.value) : null)}
                  />
                </div>

                {/* Planned End */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Planned End</label>
                  <input
                    type="date"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={formData.DatePlannedEnd ? new Date(formData.DatePlannedEnd).toISOString().split('T')[0] : ''}
                    onChange={(e) => handleFieldChange('DatePlannedEnd', e.target.value ? new Date(e.target.value) : null)}
                  />
                </div>

                {/* Duration */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Duration (Days)</label>
                  <input
                    type="number"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={formData.AmountOfTime || 0}
                    onChange={(e) => handleFieldChange('AmountOfTime', parseFloat(e.target.value) || 0)}
                    step="0.5"
                  />
                </div>

                {/* Lead Days */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Lead Days</label>
                  <input
                    type="number"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded`}
                    value={formData.TimingDays || 0}
                    onChange={(e) => handleFieldChange('TimingDays', parseFloat(e.target.value) || 0)}
                    step="0.5"
                  />
                </div>
              </div>
            </div>

            {/* Assignment Section */}
            <div>
              <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Assignment</div>

              <div className="space-y-2">
                {/* Assigned To (read-only for now) */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Assigned To</label>
                  <div className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100 flex items-center justify-between`}>
                    <span>{formData.ResourceDisplay || 'Not assigned'}</span>
                    <button
                      className={`px-2 py-0.5 text-xs ${theme.button_default} rounded`}
                      onClick={() => alert('Resource assignment coming soon')}
                    >
                      <i className="fa fa-users"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>

            {/* Progress Section */}
            <div>
              <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Progress & Status</div>

              <div className="space-y-2">
                {/* Complete % (read-only) */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Complete %</label>
                  <input
                    type="number"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                    value={formData.CompletedPercent || 0}
                    readOnly
                  />
                </div>

                {/* Stage (read-only) */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Stage</label>
                  <input
                    type="text"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                    value={formData.TaskStageDisplay || 'Not Started'}
                    readOnly
                  />
                </div>

                {/* Status (read-only) */}
                <div>
                  <label className={`block text-xs mb-1 ${theme.label}`}>Status</label>
                  <input
                    type="text"
                    className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                    value={formData.TaskStatusDisplay || 'Pending'}
                    readOnly
                  />
                </div>
              </div>
            </div>

            {/* Cost Section (read-only) */}
            {(formData.PlannedWorkHours || formData.ActualWorkHours) && (
              <div>
                <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Cost</div>

                <div className="space-y-2">
                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <label className={`block text-xs mb-1 ${theme.label}`}>Planned Hours</label>
                      <input
                        type="number"
                        className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                        value={formData.PlannedWorkHours || 0}
                        readOnly
                      />
                    </div>
                    <div>
                      <label className={`block text-xs mb-1 ${theme.label}`}>Actual Hours</label>
                      <input
                        type="number"
                        className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                        value={formData.ActualWorkHours || 0}
                        readOnly
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <label className={`block text-xs mb-1 ${theme.label}`}>Planned Cost</label>
                      <input
                        type="number"
                        className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                        value={formData.PlannedResourceCost || 0}
                        readOnly
                      />
                    </div>
                    <div>
                      <label className={`block text-xs mb-1 ${theme.label}`}>Actual Cost</label>
                      <input
                        type="number"
                        className={`${theme.inputBox} border w-full px-2 py-1 text-xs rounded bg-gray-100`}
                        value={formData.ActualResourceCost || 0}
                        readOnly
                      />
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}

        {activeTab === 1 && (
          <div className="text-center py-8">
            <div className={`text-sm ${theme.label}`}>
              <i className="fa fa-list-ul text-2xl mb-2"></i>
              <p>Checklist feature coming in Phase 2</p>
            </div>
          </div>
        )}

        {activeTab === 2 && (
          <div className="text-center py-8">
            <div className={`text-sm ${theme.label}`}>
              <i className="fa fa-clock-o text-2xl mb-2"></i>
              <p>Time & Expense feature coming in Phase 2</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default TaskPropertiesPanel;
