import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { projectWorkflowService } from '../../../webapi/projectWorkFlowSvc';

// Declare DayPilot global type
declare const DayPilot: any;

interface ProjectGanttChartProps {
  currentProject: any | null;
  onSave?: () => Promise<void>;
  onRefresh?: () => Promise<void>;
}

interface DayPilotTask {
  id: string;
  text: string;
  start: any; // DayPilot.Date
  end: any; // DayPilot.Date
  complete: number;
  completeDisplay: string;
  calDurationDays: string;
  timingDays: number;
  timingDaysDisplay: string;
  datePlannedStart: string;
  datePlannedEnd: string;
  dateActualStart: string;
  dateActualEnd: string;
  status: string;
  statusId: number;
  box?: {
    html: string;
    barBackColor: string;
    barColor: string;
  };
  DataItem: any;
  children?: DayPilotTask[];
  type?: string;
}

const ProjectGanttChart: React.FC<ProjectGanttChartProps> = ({ currentProject, onRefresh }) => {
  const { theme } = useTheme();
  const { showError, showInfo } = useErrorMessage();
  const dispatch = useDispatch();
  const ganttRef = useRef<any>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const uiId = useRef(Math.random().toString(36).substring(7));

  const [ganttData, setGanttData] = useState<{
    tasks: DayPilotTask[];
    links: any[];
    projectData: any | null;
    currentScaleIndex: number;
    showCriticalPath: boolean;
    showActualDates: boolean;
    leftSideGridWidth: number;
  }>({
    tasks: [],
    links: [],
    projectData: null,
    currentScaleIndex: 3,
    showCriticalPath: false,
    showActualDates: false,
    leftSideGridWidth: 500
  });

  const scaleOptions = [
    { id: 1, name: 'Hour', scale: 'Hour', timeHeaders: [{ groupBy: 'Day', format: 'd MMM yyyy' }, { groupBy: 'Hour', format: 'hh:mm' }] },
    { id: 2, name: '6 Hours', scale: 'CellDuration', cellDuration: 360, timeHeaders: [{ groupBy: 'Month' }, { groupBy: 'Day', format: 'd' }] },
    { id: 3, name: 'Day', scale: 'Day', timeHeaders: [{ groupBy: 'Month' }, { groupBy: 'Day', format: 'd' }] },
    { id: 4, name: 'Week', scale: 'Week', timeHeaders: [{ groupBy: 'Year' }, { groupBy: 'Month', format: 'MMM yyyy' }] },
    { id: 5, name: '2 Weeks', scale: 'CellDuration', cellDuration: 20160, timeHeaders: [{ groupBy: 'Year' }, { groupBy: 'Month', format: 'MMM yyyy' }] },
    { id: 6, name: 'Month', scale: 'Month', timeHeaders: [{ groupBy: 'Year' }, { groupBy: 'Month', format: 'MM' }] }
  ];

  // Load project data when currentProject changes
  useEffect(() => {
    if (currentProject?.Id) {
      loadProjectData();
    }
  }, [currentProject?.Id]);

  // Initialize DayPilot Gantt after data is loaded
  useEffect(() => {
    if (ganttData.tasks.length > 0 && typeof DayPilot !== 'undefined' && containerRef.current) {
      initializeDayPilot();
    }
  }, [ganttData.tasks, ganttData.currentScaleIndex, ganttData.showCriticalPath, ganttData.showActualDates]);

  const loadProjectData = useCallback(async () => {
    if (!currentProject?.Id) return;

    dispatch(setIsBusy());
    try {
      const projectData = await projectWorkflowService.RetrieveOneAppProjectOrWorkFlowExDto(currentProject.Id);

      // Convert tasks to DayPilot format
      const dayPilotTasks = convertToDeepPilotTasks(projectData.RootTreeList || []);

      // Build links from predecessor data
      const dayPilotLinks: any[] = [];
      if (projectData.DictGuidKeyPredecessorList) {
        Object.keys(projectData.DictGuidKeyPredecessorList).forEach(toGuid => {
          const fromList = projectData.DictGuidKeyPredecessorList[toGuid];
          fromList?.forEach((fromGuid: string) => {
            dayPilotLinks.push({
              id: Math.random().toString(36).substring(7),
              from: fromGuid,
              to: toGuid,
              type: 'FinishToStart'
            });
          });
        });
      }

      setGanttData(prev => ({
        ...prev,
        tasks: dayPilotTasks,
        links: dayPilotLinks,
        projectData: projectData
      }));
    } catch (error: any) {
      console.error('Error loading project data:', error);
      showError(error.message || 'Failed to load project data');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentProject?.Id, dispatch, showError]);

  const convertToDeepPilotTasks = (tasks: any[]): DayPilotTask[] => {
    return tasks.map(task => convertOneTaskToDayPilot(task));
  };

  const convertOneTaskToDayPilot = (task: any): DayPilotTask => {
    const EmAppProjectTaskType = { Milestone: 2 };
    const EmAppProjectTaskStatus = { Late: 1, OnSchedule: 2, Completed: 3, AtRisk: 4 };

    const dayPilotTask: DayPilotTask = {
      id: task.RowIdentity || Math.random().toString(36).substring(7),
      text: task.Name || 'Unnamed Task',
      start: new DayPilot.Date(task.DatePlannedStart || new Date(), true),
      end: new DayPilot.Date(task.DatePlannedEnd || new Date(), true),
      complete: task.CompletedPercent || 0,
      completeDisplay: (task.CompletedPercent || 0) + '%',
      calDurationDays: ((task.CalDurationDays || 0).toFixed(2)) + ' Days',
      timingDays: task.TimingDays || 0,
      timingDaysDisplay: (task.TimingDays || 0) + ' Days',
      datePlannedStart: task.DatePlannedStart ? new Date(task.DatePlannedStart).toLocaleDateString() : '',
      datePlannedEnd: task.DatePlannedEnd ? new Date(task.DatePlannedEnd).toLocaleDateString() : '',
      dateActualStart: task.DateActualStart ? new Date(task.DateActualStart).toLocaleDateString() : '',
      dateActualEnd: task.DateActualEnd ? new Date(task.DateActualEnd).toLocaleDateString() : '',
      status: task.TaskStatusDisplay || '',
      statusId: task.ProjectActivityStatusId || 0,
      DataItem: task
    };

    // Add resource name to text if available
    if (task.ResourceDisplay) {
      dayPilotTask.text = task.Name + ' (' + task.ResourceDisplay + ')';
    }

    // Set milestone type
    if (task.EmTaskType === EmAppProjectTaskType.Milestone) {
      dayPilotTask.type = 'Milestone';
    }

    // Configure bar styling based on status
    const completePercent = task.CompletedPercent || 0;
    dayPilotTask.box = {
      html: '<div style="font-size:8px"> ' + completePercent + '%</div>',
      barBackColor: '#eeeeee',
      barColor: 'limeGreen'
    };

    if (dayPilotTask.statusId === EmAppProjectTaskStatus.Late) {
      dayPilotTask.box.barColor = 'red';
      dayPilotTask.status = 'Late';
    } else if (dayPilotTask.statusId === EmAppProjectTaskStatus.OnSchedule) {
      dayPilotTask.box.barColor = 'limeGreen';
      dayPilotTask.status = 'On Schedule';
    } else if (dayPilotTask.statusId === EmAppProjectTaskStatus.Completed) {
      dayPilotTask.box.barColor = 'dodgerblue';
      dayPilotTask.status = 'Completed';
    } else if (dayPilotTask.statusId === EmAppProjectTaskStatus.AtRisk) {
      dayPilotTask.box.barColor = 'yellow';
      dayPilotTask.status = 'At Risk';
    }

    // Critical path highlighting
    if (ganttData.showCriticalPath && task.IsCriticalPathTask) {
      dayPilotTask.box.html = '<div style="font-size:8px;background-color:orangeRed;width:100%;height:100%;color:white;"> ' + completePercent + '%</div>';
    }

    // Add children recursively
    if (task.Children && task.Children.length > 0) {
      dayPilotTask.children = task.Children.map((child: any) => convertOneTaskToDayPilot(child));
    }

    return dayPilotTask;
  };

  const initializeDayPilot = () => {
    if (!containerRef.current || typeof DayPilot === 'undefined') return;

    // Create or update gantt control
    if (!ganttRef.current) {
      ganttRef.current = new DayPilot.Gantt('projectGanttControl_' + uiId.current);
    }

    const dp = ganttRef.current;

    // Get current scale configuration
    const currentScale = scaleOptions.find(s => s.id === ganttData.currentScaleIndex) || scaleOptions[2];

    // Calculate date range
    const projectData = ganttData.projectData;
    let startDate = new DayPilot.Date(projectData?.DatePlannedStart || new Date());
    let endDate = new DayPilot.Date(projectData?.DatePlannedEnd || new Date());
    const days = Math.max(parseInt(((endDate.getTime() - startDate.getTime()) / (1000 * 3600 * 24)) as any) + 31, 90);

    // Configure DayPilot Gantt
    dp.startDate = startDate.addDays(-2);
    dp.days = days;
    dp.heightSpec = 'Parent100Pct';
    dp.theme = 'dpgantt';
    dp.scale = currentScale.scale;
    if (currentScale.cellDuration) {
      dp.cellDuration = currentScale.cellDuration;
    }
    dp.timeHeaders = currentScale.timeHeaders;

    dp.tasks.list = ganttData.tasks;
    dp.links.list = ganttData.links;

    // Configure columns
    dp.columns = [
      { title: 'Name', width: 200, property: 'text' },
      { title: 'Plan Start', width: 78, property: 'datePlannedStart' },
      { title: 'Plan End', width: 78, property: 'datePlannedEnd' },
      { title: 'Work Days', width: 75, property: 'calDurationDays' },
      { title: 'Lead Days', width: 70, property: 'timingDaysDisplay' },
      { title: 'Actual Start', width: 80, property: 'dateActualStart' },
      { title: 'Actual End', width: 80, property: 'dateActualEnd' },
      { title: '%Done', width: 60, property: 'completeDisplay' },
      { title: 'Status', width: 70, property: 'status' }
    ];

    // Configure row header
    dp.rowHeaderWidth = ganttData.leftSideGridWidth;
    dp.rowHeaderScrolling = true;
    dp.rowHeaderSplitterWidth = 1;

    // Add current date separator
    dp.separators = [{
      color: 'lightblue',
      location: new Date(),
      layer: 'BelowEvents',
      width: 3
    }];

    // Enable task versions (actual dates) if requested
    if (ganttData.showActualDates) {
      dp.taskVersionsEnabled = true;
      dp.taskVersionHeight = 10;
    } else {
      dp.taskVersionsEnabled = false;
    }

    // Configure task appearance
    dp.linkPointSize = 6;
    dp.taskHeight = 18;

    // Corner label with project name
    dp.cornerHtml = currentProject?.Name || 'Project';

    // Event handlers
    dp.onTaskResized = handleTaskResized;
    dp.onTaskMoved = handleTaskMoved;
    dp.onRowEdited = (args: any) => {
      console.log('Task renamed to:', args.newText);
    };

    // Context menus
    dp.contextMenuTask = new DayPilot.Menu({
      items: [
        { text: 'Properties', onclick: () => showInfo('Task properties dialog coming in Phase 2') }
      ]
    });

    dp.contextMenuLink = new DayPilot.Menu({
      items: [
        {
          text: 'Delete link',
          onclick: function(this: any) {
            dp.links.remove(this.source);
            markProjectModified();
          }
        }
      ]
    });

    dp.taskClickHandling = 'ContextMenu';
    dp.rowClickHandling = 'ContextMenu';

    // Initialize the control
    dp.init();
    dp.update();
  };

  const handleTaskResized = (args: any) => {
    const task = args.task;
    if (task && task.data && task.data.DataItem) {
      task.data.DataItem.DatePlannedStart = task.start().toDateLocal();
      task.data.DataItem.DatePlannedEnd = task.end().toDateLocal();
      markProjectModified();
      showInfo('Task resized. Click Save to persist changes.');
    }
  };

  const handleTaskMoved = (args: any) => {
    const task = args.task;
    if (task && task.data && task.data.DataItem) {
      task.data.DataItem.DatePlannedStart = task.start().toDateLocal();
      task.data.DataItem.DatePlannedEnd = task.end().toDateLocal();
      markProjectModified();
      showInfo('Task moved. Click Save to persist changes.');
    }
  };

  const markProjectModified = () => {
    if (ganttData.projectData) {
      setGanttData(prev => ({
        ...prev,
        projectData: { ...prev.projectData, IsModified: true }
      }));
    }
  };

  const buildTaskDataFromDayPilot = () => {
    if (!ganttRef.current) return;

    const dp = ganttRef.current;
    const updatedRootTreeList: any[] = [];

    // Recursively convert DayPilot tasks back to project tasks
    const convertDayPilotToTask = (dayPilotTask: any): any => {
      const task = dayPilotTask.data.DataItem;
      task.RowIdentity = dayPilotTask.id();
      task.DatePlannedStart = dayPilotTask.start().toDateLocal();
      task.DatePlannedEnd = dayPilotTask.end().toDateLocal();

      task.Children = [];
      if (dayPilotTask.children && dayPilotTask.children().length > 0) {
        dayPilotTask.children().forEach((child: any) => {
          task.Children.push(convertDayPilotToTask(child));
        });
      }

      return task;
    };

    dp.tasks.list.forEach((dayPilotTask: any) => {
      updatedRootTreeList.push(convertDayPilotToTask(dayPilotTask));
    });

    // Update project data
    ganttData.projectData.RootTreeList = updatedRootTreeList;

    // Rebuild predecessor links
    ganttData.projectData.DictGuidKeyPredecessorList = {};
    dp.links.list.forEach((link: any) => {
      if (link.to && link.from) {
        if (!ganttData.projectData.DictGuidKeyPredecessorList[link.to]) {
          ganttData.projectData.DictGuidKeyPredecessorList[link.to] = [];
        }
        ganttData.projectData.DictGuidKeyPredecessorList[link.to].push(link.from);
      }
    });
  };

  const handleCalculateCriticalPath = useCallback(async () => {
    if (!ganttData.projectData) return;

    buildTaskDataFromDayPilot();

    dispatch(setIsBusy());
    try {
      const result = await projectWorkflowService.CalculateProjectCriticalPath(ganttData.projectData);
      showInfo('Critical path calculated successfully');

      setGanttData(prev => ({ ...prev, showCriticalPath: true }));
      await loadProjectData();
    } catch (error: any) {
      console.error('Error calculating critical path:', error);
      showError(error.message || 'Failed to calculate critical path');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [ganttData.projectData, dispatch, showError, showInfo, loadProjectData]);

  const handleSave = useCallback(async () => {
    if (!ganttData.projectData) {
      showError('No project data to save');
      return;
    }

    buildTaskDataFromDayPilot();

    dispatch(setIsBusy());
    try {
      await projectWorkflowService.SaveProjectOrWorkFlowExDto(ganttData.projectData);
      showInfo('Project saved successfully');

      await loadProjectData();

      if (onRefresh) {
        await onRefresh();
      }
    } catch (error: any) {
      console.error('Error saving project:', error);
      showError(error.message || 'Failed to save project');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [ganttData.projectData, dispatch, showError, showInfo, loadProjectData, onRefresh]);

  const handleCalculateDates = useCallback(async () => {
    if (!ganttData.projectData) return;

    buildTaskDataFromDayPilot();

    dispatch(setIsBusy());
    try {
      const result = await projectWorkflowService.CalculateProjectOrWorkFlow(ganttData.projectData);
      showInfo('Task dates calculated successfully');

      await loadProjectData();
    } catch (error: any) {
      console.error('Error calculating dates:', error);
      showError(error.message || 'Failed to calculate task dates');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [ganttData.projectData, dispatch, showError, showInfo, loadProjectData]);

  const handleZoomChange = (scaleIndex: number) => {
    setGanttData(prev => ({ ...prev, currentScaleIndex: scaleIndex }));
  };

  if (!currentProject) {
    return (
      <div className="w-full h-full flex items-center justify-center">
        <div className={`text-sm ${theme.label}`}>
          Please select a project to view Gantt chart
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      {/* Header with toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Gantt Chart: {currentProject.Name}
        </div>
        <div className="flex items-center space-x-2">
          {/* Zoom controls */}
          <select
            className={`${theme.inputBox} border px-2 py-1 text-xs rounded`}
            value={ganttData.currentScaleIndex}
            onChange={(e) => handleZoomChange(parseInt(e.target.value))}
          >
            {scaleOptions.map(scale => (
              <option key={scale.id} value={scale.id}>{scale.name}</option>
            ))}
          </select>

          {/* Toggle buttons */}
          <button
            className={`px-3 py-1 rounded text-xs ${ganttData.showCriticalPath ? 'bg-orange-500 text-white' : theme.button_default}`}
            onClick={() => setGanttData(prev => ({ ...prev, showCriticalPath: !prev.showCriticalPath }))}
          >
            <i className="fa fa-flash mr-1"></i>
            Critical Path
          </button>

          <button
            className={`px-3 py-1 rounded text-xs ${ganttData.showActualDates ? 'bg-blue-500 text-white' : theme.button_default}`}
            onClick={() => setGanttData(prev => ({ ...prev, showActualDates: !prev.showActualDates }))}
          >
            <i className="fa fa-calendar-check mr-1"></i>
            Actual Dates
          </button>

          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={handleCalculateCriticalPath}
          >
            <i className="fa fa-calculator mr-1"></i>
            Calculate Path
          </button>

          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={handleCalculateDates}
          >
            <i className="fa fa-calendar mr-1"></i>
            Calculate Dates
          </button>

          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={loadProjectData}
          >
            <i className="fa fa-refresh mr-1"></i>
            Refresh
          </button>

          <button
            className={`px-3 py-1 rounded text-xs ${theme.button_default}`}
            onClick={handleSave}
            disabled={!ganttData.projectData?.IsModified}
          >
            <i className="fa fa-save mr-1"></i>
            Save
          </button>
        </div>
      </div>

      {/* Gantt chart container */}
      <div className="flex-auto overflow-hidden" style={{ position: 'relative' }}>
        {ganttData.tasks.length > 0 ? (
          <div
            id={'projectGanttControl_' + uiId.current}
            ref={containerRef}
            style={{ width: '100%', height: '100%' }}
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center">
            <div className={`text-sm ${theme.label}`}>
              No tasks available. Add tasks in the Task List tab.
            </div>
          </div>
        )}
      </div>

      {/* Info footer */}
      <div className={`px-3 py-2 border-t ${theme.mainContentSection}`}>
        <div className={`text-xs ${theme.label}`}>
          <i className="fa fa-info-circle mr-1"></i>
          <strong>DayPilot Gantt Chart:</strong> Drag tasks to move, resize handles to change duration. Right-click for context menu.
        </div>
      </div>
    </div>
  );
};

export default ProjectGanttChart;
