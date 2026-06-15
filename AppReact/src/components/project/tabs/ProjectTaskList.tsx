import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { projectWorkflowService } from '../../../webapi/projectWorkFlowSvc';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import TaskPropertiesPanel from './TaskPropertiesPanel';

interface ProjectTaskListProps {
  currentProject: any | null;
  onSave?: () => Promise<void>;
  onRefresh?: () => Promise<void>;
}

const ProjectTaskList: React.FC<ProjectTaskListProps> = ({ currentProject, onRefresh }) => {
  const { theme } = useTheme();
  const { showError, showInfo } = useErrorMessage();
  const dispatch = useDispatch();
  const flex = useRef<any>(null);

  const [taskData, setTaskData] = useState<{
    tasks: any[];
    tasksCV: CollectionView<any> | null;
    selectedTask: any | null;
    projectData: any | null;
    showPropertiesPanel: boolean;
  }>({
    tasks: [],
    tasksCV: null,
    selectedTask: null,
    projectData: null,
    showPropertiesPanel: true
  });

  const [contextMenu, setContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    task: any | null;
  }>({
    visible: false,
    x: 0,
    y: 0,
    task: null
  });

  // Load tasks when currentProject changes
  useEffect(() => {
    if (currentProject?.Id) {
      loadTasksFromServer();
    }
  }, [currentProject?.Id]);

  // Close context menu on outside click
  useEffect(() => {
    const handleClick = () => {
      if (contextMenu.visible) {
        setContextMenu({ visible: false, x: 0, y: 0, task: null });
      }
    };
    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [contextMenu.visible]);

  const loadTasksFromServer = useCallback(async () => {
    if (!currentProject?.Id) return;

    dispatch(setIsBusy());
    try {
      const projectData = await projectWorkflowService.RetrieveOneAppProjectOrWorkFlowExDto(currentProject.Id);

      // Use RootTreeList for hierarchical structure
      const tasks = projectData.RootTreeList || [];

      const cv = new CollectionView<any>(tasks);

      setTaskData({
        tasks: tasks,
        tasksCV: cv,
        selectedTask: null,
        projectData: projectData,
        showPropertiesPanel: true
      });
    } catch (error: any) {
      console.error('Error loading tasks:', error);
      showError(error.message || 'Failed to load tasks');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentProject?.Id, dispatch, showError]);

  const findTaskById = useCallback((tasks: any[], rowIdentity: string): any | null => {
    for (const task of tasks) {
      if (task.RowIdentity === rowIdentity) {
        return task;
      }
      if (task.Children && task.Children.length > 0) {
        const found = findTaskById(task.Children, rowIdentity);
        if (found) return found;
      }
    }
    return null;
  }, []);

  const handleAddRootTask = useCallback(() => {
    const newTask = {
      Id: 0,
      RowIdentity: crypto.randomUUID(),
      Name: 'New Task',
      Description: '',
      DatePlannedStart: new Date(),
      DatePlannedEnd: new Date(),
      AmountOfTime: 1,
      UnitOfTime: 1, // Days
      TimingDays: 0,
      CompletedPercent: 0,
      EmTaskType: 1, // Normal task
      EmPriority: 2, // Medium
      ProjectId: currentProject?.Id,
      Sort: taskData.tasks.length + 1,
      Children: []
    };

    const updatedTasks = [...taskData.tasks, newTask];
    const cv = new CollectionView<any>(updatedTasks);

    setTaskData({
      ...taskData,
      tasks: updatedTasks,
      tasksCV: cv,
      selectedTask: newTask,
      projectData: { ...taskData.projectData, IsModified: true }
    });

    showInfo('New task added. Click Save to persist changes.');
  }, [taskData, currentProject, showInfo]);

  const handleAddChildTask = useCallback((parentTask: any) => {
    if (!parentTask) return;

    const newTask = {
      Id: 0,
      RowIdentity: crypto.randomUUID(),
      Name: 'New Subtask',
      Description: '',
      DatePlannedStart: new Date(),
      DatePlannedEnd: new Date(),
      AmountOfTime: 1,
      UnitOfTime: 1,
      TimingDays: 0,
      CompletedPercent: 0,
      EmTaskType: 1,
      EmPriority: 2,
      ProjectId: currentProject?.Id,
      MainTaskId: parentTask.Id,
      Sort: (parentTask.Children?.length || 0) + 1,
      Children: []
    };

    // Clone tasks and add child
    const updatedTasks = JSON.parse(JSON.stringify(taskData.tasks));
    const parent = findTaskById(updatedTasks, parentTask.RowIdentity);

    if (parent) {
      if (!parent.Children) {
        parent.Children = [];
      }
      parent.Children.push(newTask);
    }

    const cv = new CollectionView<any>(updatedTasks);

    setTaskData({
      ...taskData,
      tasks: updatedTasks,
      tasksCV: cv,
      selectedTask: newTask,
      projectData: { ...taskData.projectData, IsModified: true }
    });

    showInfo('Subtask added. Click Save to persist changes.');
  }, [taskData, currentProject, findTaskById, showInfo]);

  const collectAllTaskIds = useCallback((task: any, ids: number[]): void => {
    if (task.Id && task.Id > 0) {
      ids.push(task.Id);
    }
    if (task.Children) {
      task.Children.forEach((child: any) => collectAllTaskIds(child, ids));
    }
  }, []);

  const removeTaskRecursive = useCallback((tasks: any[], rowIdentity: string): { tasks: any[], deletedIds: number[] } => {
    const deletedIds: number[] = [];
    const filteredTasks = tasks.filter(task => {
      if (task.RowIdentity === rowIdentity) {
        collectAllTaskIds(task, deletedIds);
        return false;
      }
      if (task.Children && task.Children.length > 0) {
        const result = removeTaskRecursive(task.Children, rowIdentity);
        task.Children = result.tasks;
        deletedIds.push(...result.deletedIds);
      }
      return true;
    });
    return { tasks: filteredTasks, deletedIds };
  }, [collectAllTaskIds]);

  const handleRemoveTask = useCallback((task: any) => {
    if (!task) return;

    if (!window.confirm(`Are you sure you want to delete task "${task.Name}" and all its subtasks?`)) {
      return;
    }

    const result = removeTaskRecursive([...taskData.tasks], task.RowIdentity);
    const cv = new CollectionView<any>(result.tasks);

    // Track deleted task IDs
    const projectData = { ...taskData.projectData };
    if (!projectData.DeletedItemsIds) {
      projectData.DeletedItemsIds = [];
    }
    projectData.DeletedItemsIds.push(...result.deletedIds);
    projectData.IsModified = true;

    setTaskData({
      ...taskData,
      tasks: result.tasks,
      tasksCV: cv,
      selectedTask: null,
      projectData: projectData
    });

    showInfo('Task marked for deletion. Click Save to persist changes.');
  }, [taskData, removeTaskRecursive, showInfo]);

  const handleSave = useCallback(async () => {
    if (!taskData.projectData) {
      showError('No project data to save');
      return;
    }

    dispatch(setIsBusy());
    try {
      // Update RootTreeList with current tasks
      taskData.projectData.RootTreeList = taskData.tasks;

      // Validate before save (check for circular predecessors)
      // For Phase 1, we'll skip this validation since we don't have predecessor UI yet

      await projectWorkflowService.SaveProjectOrWorkFlowExDto(taskData.projectData);
      showInfo('Tasks saved successfully');

      // Reload tasks
      await loadTasksFromServer();

      if (onRefresh) {
        await onRefresh();
      }
    } catch (error: any) {
      console.error('Error saving tasks:', error);
      showError(error.message || 'Failed to save tasks');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [taskData, dispatch, showError, showInfo, loadTasksFromServer, onRefresh]);

  const handleTaskSelection = useCallback(() => {
    if (flex.current && flex.current.control) {
      const grid = flex.current.control;
      const selectedItem = grid.selectedItems?.[0];

      if (selectedItem) {
        setTaskData(prev => ({
          ...prev,
          selectedTask: selectedItem
        }));
      }
    }
  }, []);

  const handleCellEditEnded = useCallback(() => {
    // Mark data as modified when cells are edited
    if (taskData.projectData) {
      setTaskData(prev => ({
        ...prev,
        projectData: { ...prev.projectData, IsModified: true }
      }));
    }
  }, [taskData.projectData]);

  const handleContextMenu = useCallback((e: React.MouseEvent, task: any) => {
    e.preventDefault();
    e.stopPropagation();

    setContextMenu({
      visible: true,
      x: e.clientX,
      y: e.clientY,
      task: task
    });
  }, []);

  const handleContextMenuAction = useCallback((action: string) => {
    const task = contextMenu.task;
    setContextMenu({ visible: false, x: 0, y: 0, task: null });

    switch (action) {
      case 'addChild':
        handleAddChildTask(task);
        break;
      case 'remove':
        handleRemoveTask(task);
        break;
    }
  }, [contextMenu.task, handleAddChildTask, handleRemoveTask]);

  const handleTaskUpdate = useCallback((updatedTask: any) => {
    // Update the task in the tree
    const updateTaskInTree = (tasks: any[]): any[] => {
      return tasks.map(task => {
        if (task.RowIdentity === updatedTask.RowIdentity) {
          return { ...task, ...updatedTask };
        }
        if (task.Children && task.Children.length > 0) {
          return { ...task, Children: updateTaskInTree(task.Children) };
        }
        return task;
      });
    };

    const updatedTasks = updateTaskInTree(taskData.tasks);
    const cv = new CollectionView<any>(updatedTasks);

    setTaskData({
      ...taskData,
      tasks: updatedTasks,
      tasksCV: cv,
      selectedTask: updatedTask,
      projectData: { ...taskData.projectData, IsModified: true }
    });
  }, [taskData]);

  // Custom cell template for context menu trigger
  const nameCellTemplate = (ctx: any) => {
    const task = ctx.item;
    return (
      <div
        onContextMenu={(e) => handleContextMenu(e, task)}
        style={{ cursor: 'context-menu' }}
      >
        {task.Name}
      </div>
    );
  };

  if (!currentProject) {
    return (
      <div className="w-full h-full flex items-center justify-center">
        <div className={`text-sm ${theme.label}`}>
          Please select a project to view tasks
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      {/* Header with action buttons */}
      <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Task List: {currentProject.Name}
        </div>
        <div className="flex items-center space-x-2">
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={loadTasksFromServer}
          >
            <i className="fa fa-refresh mr-1"></i>
            Refresh
          </button>
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={handleAddRootTask}
          >
            <i className="fa fa-plus mr-1"></i>
            Add Main Task
          </button>
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={() => setTaskData(prev => ({ ...prev, showPropertiesPanel: !prev.showPropertiesPanel }))}
          >
            <i className={`fa fa-${taskData.showPropertiesPanel ? 'angle-right' : 'angle-left'} mr-1`}></i>
            {taskData.showPropertiesPanel ? 'Hide' : 'Show'} Properties
          </button>
          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={handleSave}
            disabled={!taskData.projectData?.IsModified}
          >
            <i className="fa fa-save mr-1"></i>
            Save
          </button>
        </div>
      </div>

      {/* Main Content - Grid + Properties Panel */}
      <div className="flex-auto overflow-hidden flex">
        {/* Task Grid (left side or full width) */}
        <div className={taskData.showPropertiesPanel ? 'w-[70%] border-r' : 'w-full'}>
          {taskData.tasksCV && (
            <FlexGrid
              ref={flex}
              itemsSource={taskData.tasksCV}
              isReadOnly={false}
              selectionMode="Row"
              selectionChanged={handleTaskSelection}
              cellEditEnded={handleCellEditEnded}
              childItemsPath="Children"
              allowAddNew={false}
              allowDelete={false}
              style={{ width: '100%', height: '100%' }}
            >
              <FlexGridFilter filterColumns={['Name', 'TaskStageDisplay', 'TaskStatusDisplay']} />

              <FlexGridColumn header="Task Name" binding="Name" width={250}>
                <FlexGridCellTemplate cellType="Cell" template={nameCellTemplate} />
              </FlexGridColumn>

              <FlexGridColumn header="Sort" binding="Sort" width={60} />

              <FlexGridColumn
                header="Work Days"
                binding="AmountOfTime"
                width={100}
                format="n1"
              />

              <FlexGridColumn
                header="Lead Days"
                binding="TimingDays"
                width={100}
                format="n1"
              />

              <FlexGridColumn
                header="Planned Start"
                binding="DatePlannedStart"
                width={120}
                format="d"
              />

              <FlexGridColumn
                header="Planned Due"
                binding="DatePlannedEnd"
                width={120}
                format="d"
              />

              <FlexGridColumn
                header="Assigned To"
                binding="ResourceDisplay"
                width={150}
                isReadOnly={true}
              />

              <FlexGridColumn
                header="Complete %"
                binding="CompletedPercent"
                width={100}
                format="n0"
                isReadOnly={true}
              />

              <FlexGridColumn
                header="Stage"
                binding="TaskStageDisplay"
                width={120}
                isReadOnly={true}
              />

              <FlexGridColumn
                header="Status"
                binding="TaskStatusDisplay"
                width={120}
                isReadOnly={true}
              />
            </FlexGrid>
          )}
        </div>

        {/* Task Properties Panel (right side) */}
        {taskData.showPropertiesPanel && (
          <div className="w-[30%] overflow-hidden">
            <TaskPropertiesPanel
              task={taskData.selectedTask}
              projectId={currentProject?.Id}
              onTaskUpdate={handleTaskUpdate}
            />
          </div>
        )}
      </div>

      {/* Context Menu */}
      {contextMenu.visible && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded shadow-lg py-1 min-w-[150px]`}
          style={{
            left: contextMenu.x,
            top: contextMenu.y,
          }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => handleContextMenuAction('addChild')}
          >
            <i className="fa fa-plus mr-2"></i>
            Add Child Task
          </button>
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => handleContextMenuAction('remove')}
          >
            <i className="fa fa-trash mr-2"></i>
            Remove Task
          </button>
        </div>
      )}
    </div>
  );
};

export default ProjectTaskList;
