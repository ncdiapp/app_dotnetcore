declare const DayPilot: any;

/** EmInternalCodeRegistration.SchedulerBaseDate (APP.Components.Dto) */
export const EmInternalCodeRegistrationSchedulerBaseDate = 5;

/** EmAppProjectStage */
const EmAppProjectStage = {
  Planning: 1,
  Processing: 2,
  Completed: 3,
} as const;

/** EmAppProjectTaskStatus */
const EmAppProjectTaskStatus = {
  Completed: 1,
  OnSchedule: 2,
  Late: 3,
  AtRisk: 4,
  NotAvailable: 5,
} as const;

export type GanttScaleLevel = {
  scale: string;
  timeHeaders: any[];
  cellDuration?: number;
};

/** Mirrors ganttHelper.initialGanttChartDefaultParameter dictScaleLevel */
export const GANTT_DICT_SCALE_LEVEL: Record<number, GanttScaleLevel> = {
  1: { scale: "Hour", timeHeaders: [{ groupBy: "Day", format: "d MMM yyyy" }, { groupBy: "Hour", format: "hh:mm" }] },
  2: { scale: "CellDuration", timeHeaders: [{ groupBy: "Month" }, { groupBy: "Day", format: "d" }], cellDuration: 360 },
  3: { scale: "Day", timeHeaders: [{ groupBy: "Month" }, { groupBy: "Day", format: "d" }] },
  4: { scale: "Week", timeHeaders: [{ groupBy: "Year" }, { groupBy: "Month", format: "MMM yyyy" }] },
  5: { scale: "CellDuration", timeHeaders: [{ groupBy: "Year" }, { groupBy: "Month", format: "MMM yyyy" }], cellDuration: 20160 },
  6: { scale: "Month", timeHeaders: [{ groupBy: "Year" }, { groupBy: "Month", format: "MM" }] },
  7: { scale: "Year", timeHeaders: [{ groupBy: "Year" }] },
};

export function applyGanttScaleToControl(dp: any, scaleIndex: number): void {
  if (!dp) return;
  const idx = scaleIndex >= 1 && scaleIndex <= 7 ? scaleIndex : 3;
  const scaleLevelObj = GANTT_DICT_SCALE_LEVEL[idx];
  dp.scale = scaleLevelObj.scale;
  dp.timeHeaders = scaleLevelObj.timeHeaders;
  if (scaleLevelObj.cellDuration != null) {
    dp.cellDuration = scaleLevelObj.cellDuration;
  }
}

function ganttEventCssClass(stageId: any, statusId: any): string {
  let eventClass = "";
  if (stageId) {
    if (stageId === EmAppProjectStage.Planning || stageId === EmAppProjectStage.Processing) {
      eventClass = "ProjectStatus_2_OnSchedule";
    } else if (stageId === EmAppProjectStage.Completed) {
      eventClass = "ProjectStatus_1_Completed";
    }
  }
  if (statusId) {
    if (statusId === EmAppProjectTaskStatus.Completed) {
      eventClass = "ProjectStatus_1_Completed";
    } else if (statusId === EmAppProjectTaskStatus.OnSchedule) {
      eventClass = "ProjectStatus_2_OnSchedule";
    } else if (statusId === EmAppProjectTaskStatus.Late) {
      eventClass = "ProjectStatus_3_Late";
    } else if (statusId === EmAppProjectTaskStatus.AtRisk) {
      eventClass = "ProjectStatus_4_AtRisk";
    } else if (statusId === EmAppProjectTaskStatus.NotAvailable) {
      eventClass = "ProjectStatus_5_NotAvailable";
    }
  }
  return eventClass;
}

/** searchViewHelper.convertOneSearchResultRowToDaypilotTask — GanttView branch */
export function convertSearchRowToGanttTask(resultDto: any): any {
  if (typeof DayPilot === "undefined") return null;
  const aTask: any = {};
  aTask.DataItem = resultDto;
  aTask.RowIdentity = DayPilot.guid();
  aTask.id = aTask.RowIdentity;
  aTask.text = resultDto.EventName;
  aTask.start = resultDto.EventStartDateString ? new DayPilot.Date(resultDto.EventStartDateString) : "";
  aTask.end = resultDto.EventEndDateString ? new DayPilot.Date(resultDto.EventEndDateString) : "";
  aTask.EventTypeDisplay = resultDto.EventTypeDisplay;
  aTask.complete = resultDto.EventCompletePercentage || 50;
  aTask.completeDisplay = `${aTask.complete}%`;
  aTask.stageId = resultDto.EventCompletStage;
  aTask.statusId = resultDto.EventStatus;
  aTask.stage = resultDto.EventCompletStageString || "";
  aTask.status = resultDto.EventStatusString || "";
  if (resultDto.EventColorId) {
    aTask.backColor = resultDto.EventColorId;
  }
  const eventClass = ganttEventCssClass(aTask.stageId, aTask.statusId);
  aTask.actualStart = resultDto.EventActualStartDateString ? new DayPilot.Date(resultDto.EventActualStartDateString) : "";
  aTask.actualEnd = resultDto.EventActualEndDateString ? new DayPilot.Date(resultDto.EventActualEndDateString) : "";
  aTask.datePlannedStartStr = "";
  aTask.datePlannedEndStr = "";
  aTask.dateActualStartStr = "";
  aTask.dateActualEndStr = "";
  if (aTask.start) aTask.datePlannedStartStr = aTask.start.toString("yyyy-MM-dd");
  if (aTask.end) aTask.datePlannedEndStr = aTask.end.toString("yyyy-MM-dd");
  if (aTask.actualStart) aTask.dateActualStartStr = aTask.actualStart.toString("yyyy-MM-dd");
  if (aTask.actualEnd) aTask.dateActualEndStr = aTask.actualEnd.toString("yyyy-MM-dd");
  if (aTask.actualStart) {
    aTask.versions = [
      {
        start: aTask.actualStart,
        end: aTask.actualEnd || aTask.actualStart,
        text: "",
      },
    ];
  }
  aTask.box = {};
  aTask.box.html = `<div style="font-size:8px"> ${aTask.completeDisplay}</div>`;
  aTask.box.cssClass = eventClass;
  return aTask;
}

export function buildGanttTaskList(rows: any[] | undefined | null): { tasks: any[]; minDate: any | null; maxDate: any | null } {
  if (typeof DayPilot === "undefined" || !Array.isArray(rows)) {
    return { tasks: [], minDate: null, maxDate: null };
  }
  let minDate: any = null;
  let maxDate: any = null;
  const tasks: any[] = [];
  for (const resultDto of rows) {
    const aTask = convertSearchRowToGanttTask(resultDto);
    if (!aTask) continue;
    if (aTask.start && aTask.end) {
      tasks.push(aTask);
    }
    const bump = (d: any) => {
      if (!d) return;
      if (!minDate || d < minDate) minDate = d;
      if (!maxDate || d > maxDate) maxDate = d;
    };
    bump(aTask.start);
    bump(aTask.end);
    bump(aTask.actualStart);
    bump(aTask.actualEnd);
  }
  return { tasks, minDate, maxDate };
}

/** searchViewHelper.convertOneSearchResultRowToDaypilotTask — SchedulerView branch */
export function convertSearchRowToSchedulerEvent(resultDto: any): any {
  if (typeof DayPilot === "undefined") return null;
  const aTask: any = {};
  aTask.DataItem = resultDto;
  aTask.RowIdentity = DayPilot.guid();
  aTask.id = aTask.RowIdentity;
  aTask.text = resultDto.EventName;
  aTask.start = resultDto.EventStartDateString ? new DayPilot.Date(resultDto.EventStartDateString) : "";
  aTask.end = resultDto.EventEndDateString ? new DayPilot.Date(resultDto.EventEndDateString) : "";
  aTask.EventTypeDisplay = resultDto.EventTypeDisplay;
  aTask.complete = resultDto.EventCompletePercentage || 50;
  aTask.completeDisplay = `${aTask.complete}%`;
  aTask.stageId = resultDto.EventCompletStage;
  aTask.statusId = resultDto.EventStatus;
  aTask.stage = resultDto.EventCompletStageString || "";
  aTask.status = resultDto.EventStatusString || "";
  if (resultDto.EventColorId) {
    aTask.backColor = resultDto.EventColorId;
  }
  const eventClass = ganttEventCssClass(aTask.stageId, aTask.statusId);
  aTask.body = resultDto.EventBody;
  aTask.UserId = resultDto.EventUserId;
  aTask.userDisplay = resultDto.EventUserDisplay;
  aTask.Description1 = resultDto.Description1;
  aTask.Description2 = resultDto.Description2;
  aTask.resource = resultDto.EventGroupById;
  aTask.DateId = resultDto.EventDateId;
  if (aTask.start && aTask.end && aTask.start.getTimePart() === 0 && aTask.end.getTimePart() === 86399000) {
    aTask.allday = true;
  }
  aTask.cssClass = eventClass;
  aTask.row = { collapsed: true };
  return aTask;
}

/** schedulerHelper.setLongEventToSingleDayEventOfStartDate when isScrollableWeekView */
export function applySchedulerScrollableWeekDaySnap(events: any[]): void {
  if (!Array.isArray(events)) return;
  for (const dpEvent of events) {
    if (dpEvent.start && dpEvent.end) {
      dpEvent.text = dpEvent.text;
      dpEvent.start = dpEvent.start.getDatePart();
      dpEvent.end = dpEvent.start.getDatePart().addDays(1).addSeconds(-1);
    }
  }
}

export type SchedulerWeekBtn = { dateValue: any; display: string; resourceCount: number };

export function buildSchedulerWeekButtons(weekStartDateList: any[] | undefined | null): SchedulerWeekBtn[] {
  if (typeof DayPilot === "undefined" || !Array.isArray(weekStartDateList) || weekStartDateList.length < 4) {
    return [];
  }
  const out: SchedulerWeekBtn[] = [];
  for (let i = 0; i <= 3; i++) {
    const dateValue = new DayPilot.Date(weekStartDateList[i], true);
    out.push({
      dateValue,
      display: dateValue.toString("MM/dd"),
      resourceCount: 0,
    });
  }
  return out;
}

export function computeSchedulerResourceRangesFromEvents(events: any[]): Record<string, { rangeStart: any; rangeEnd: any }> {
  const dict: Record<string, { rangeStart: any; rangeEnd: any }> = {};
  for (const aTask of events) {
    if (!aTask?.resource) continue;
    const rid = aTask.resource;
    if (!dict[rid]) {
      dict[rid] = { rangeStart: aTask.start, rangeEnd: aTask.end };
    } else {
      const resourceObj = dict[rid];
      if (aTask.start && aTask.start < resourceObj.rangeStart) {
        resourceObj.rangeStart = aTask.start;
      }
      if (aTask.end && aTask.end > resourceObj.rangeEnd) {
        resourceObj.rangeEnd = aTask.end;
      }
    }
  }
  return dict;
}

export function buildSchedulerResources(
  groupingLookupItems: any[] | undefined | null,
  dictResourceIdAndObjInCurrentDateRange: Record<string, { rangeStart: any; rangeEnd: any }>,
): any[] {
  const resourceData: any[] = [];
  if (!Array.isArray(groupingLookupItems)) return resourceData;
  for (const aLookupItem of groupingLookupItems) {
    if (aLookupItem?.Id != null && dictResourceIdAndObjInCurrentDateRange[String(aLookupItem.Id)]) {
      resourceData.push({
        id: aLookupItem.Id,
        name: aLookupItem.Display,
      });
    }
  }
  return resourceData;
}

export function updateWeekButtonResourceCounts(
  weekBtns: SchedulerWeekBtn[],
  dictResourceIdAndObjInCurrentDateRange: Record<string, { rangeStart: any; rangeEnd: any }>,
): void {
  if (!weekBtns.length) return;
  for (let i = 0; i <= 3; i++) {
    let weekBtnResourceCount = 0;
    const weekBtnObj = weekBtns[i];
    if (!weekBtnObj) continue;
    const weekStart = weekBtnObj.dateValue;
    const nextWeekStart = weekStart.addDays(7);
    for (const resourceObj of Object.values(dictResourceIdAndObjInCurrentDateRange)) {
      if (resourceObj.rangeStart && resourceObj.rangeEnd) {
        if (
          (weekStart <= resourceObj.rangeStart && nextWeekStart > resourceObj.rangeStart) ||
          (weekStart < resourceObj.rangeEnd && nextWeekStart > resourceObj.rangeEnd) ||
          (weekStart >= resourceObj.rangeStart && nextWeekStart <= resourceObj.rangeEnd)
        ) {
          weekBtnResourceCount++;
        }
      }
    }
    weekBtnObj.resourceCount = weekBtnResourceCount;
  }
}

/** Patch dict for scheduler base-date criteria (schedulerHelper.executeSearchByBaseDate). */
export function buildSchedulerBaseDateCriteriaPatch(searchDto: any | null | undefined, baseDate: Date): Record<string, Date> {
  const patch: Record<string, Date> = {};
  const list = searchDto?.Criterias;
  if (!Array.isArray(list)) return patch;
  for (const aCriteria of list) {
    if (aCriteria?.EmInternalCodeRegistration === EmInternalCodeRegistrationSchedulerBaseDate && aCriteria?.SearcDCUID) {
      patch[String(aCriteria.SearcDCUID)] = baseDate;
    }
  }
  return patch;
}

export function getSchedulerViewportLabel(viewport: { start?: any; end?: any } | null | undefined): string {
  if (!viewport?.start || !viewport?.end) return "";
  try {
    const a = viewport.start.toString("MM-dd");
    const b = viewport.end.toString("MM-dd");
    return `${a} to ${b}`;
  } catch {
    return "";
  }
}
