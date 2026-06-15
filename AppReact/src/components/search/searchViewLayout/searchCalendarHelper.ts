declare const DayPilot: any;

/** Matches APP.Components.Dto.EmInternalCodeRegistration */
export const EmInternalCodeRegistration = {
  CalendarNavigationStartDate: 1,
  CalendarNavigationEndDate: 2,
} as const;

/** Matches EmAppLinkTargetUsageType (AppEnums) */
export const EmAppLinkTargetUsageType = {
  SearchViewLinkToForm: 1,
  SearchViewLinkToFormGroup: 2,
  SearchViewLinkToSearch: 3,
} as const;

/** Matches EmAppLinkTargetActionType (server / AppEnums) */
export const EmAppLinkTargetActionType = {
  Create: 2,
  PasteEvent: 16,
} as const;

/** Matches EmAppLinkTargetSourceColumnType */
export const EmAppLinkTargetSourceColumnType = {
  SearchViewField: 1,
  SearchViewSpecialProperty: 2,
} as const;

/** Matches EmAppViewType.CalendarView */
export const EmAppViewTypeCalendarView = 6;

/** Matches EmAppCanlendarMode */
export const EmAppCanlendarMode = {
  MonthView: 1,
  WeekView: 2,
  DayView: 3,
} as const;

export type CalendarCellParam = {
  EventDateId: string | null;
  EventUserId: any;
  EventStartDate: any;
  EventEndDate: any;
};

export function convertSearchRowsToDayPilotEvents(rows: any[] | undefined | null): any[] {
  if (!Array.isArray(rows) || typeof DayPilot === "undefined") {
    return [];
  }
  const events: any[] = [];
  for (const resultDto of rows) {
    const aEvent: any = {};
    aEvent.DataItem = resultDto;
    aEvent.RowIdentity = DayPilot.guid();
    aEvent.id = aEvent.RowIdentity;
    aEvent.text = resultDto.EventName;
    aEvent.start = resultDto.EventStartDateString ? new DayPilot.Date(resultDto.EventStartDateString) : "";
    aEvent.end = resultDto.EventEndDateString ? new DayPilot.Date(resultDto.EventEndDateString) : "";
    aEvent.EventTypeDisplay = resultDto.EventTypeDisplay;
    aEvent.body = resultDto.EventBody;
    aEvent.UserId = resultDto.EventUserId;
    aEvent.userDisplay = resultDto.EventUserDisplay;
    if (aEvent.start && aEvent.end && aEvent.start.getTimePart() === 0 && aEvent.end.getTimePart() === 86399000) {
      aEvent.allday = true;
    }
    if (resultDto.EventColorId) {
      aEvent.backColor = resultDto.EventColorId;
    }
    if (aEvent.start && aEvent.end) {
      events.push(aEvent);
    }
  }
  return events;
}

export function findCalendarNavigationCriteriaDcuIds(searchDto: any | null | undefined): {
  startDcuId: string | null;
  endDcuId: string | null;
} {
  let startDcuId: string | null = null;
  let endDcuId: string | null = null;
  const list = searchDto?.Criterias;
  if (!Array.isArray(list)) {
    return { startDcuId, endDcuId };
  }
  for (const c of list) {
    if (c?.EmInternalCodeRegistration === EmInternalCodeRegistration.CalendarNavigationStartDate && c?.SearcDCUID) {
      startDcuId = String(c.SearcDCUID);
    }
    if (c?.EmInternalCodeRegistration === EmInternalCodeRegistration.CalendarNavigationEndDate && c?.SearcDCUID) {
      endDcuId = String(c.SearcDCUID);
    }
  }
  return { startDcuId, endDcuId };
}

export function hasCalendarNavigationCriteria(searchDto: any | null | undefined): boolean {
  const { startDcuId, endDcuId } = findCalendarNavigationCriteriaDcuIds(searchDto);
  return Boolean(startDcuId && endDcuId);
}

/** Build dictDcuValue patch from navigator range (Angular calendarHelper.changeCalendarTimeRange). */
export function buildNavigatorCriteriaPatch(
  searchDto: any | null | undefined,
  naviStart: any,
  naviEnd: any,
): Record<string, Date> {
  const patch: Record<string, Date> = {};
  if (!searchDto?.Criterias || !naviStart) {
    return patch;
  }
  const endAdj = naviEnd && typeof naviEnd.addDays === "function" ? naviEnd.addDays(-1) : naviEnd;
  for (const aCriteriaDto of searchDto.Criterias) {
    if (aCriteriaDto?.EmInternalCodeRegistration === EmInternalCodeRegistration.CalendarNavigationStartDate && aCriteriaDto?.SearcDCUID) {
      patch[String(aCriteriaDto.SearcDCUID)] = new Date(naviStart.value);
    } else if (
      aCriteriaDto?.EmInternalCodeRegistration === EmInternalCodeRegistration.CalendarNavigationEndDate &&
      aCriteriaDto?.SearcDCUID &&
      endAdj
    ) {
      patch[String(aCriteriaDto.SearcDCUID)] = new Date(endAdj.value);
    }
  }
  return patch;
}

export function getNavigatorRangeDisplayText(navigatorConfig: any | null | undefined): string {
  if (!navigatorConfig) return "";
  try {
    if (navigatorConfig.selectMode === "month") {
      return navigatorConfig.selectionDay.toString("yyyy-MM");
    }
    if (navigatorConfig.selectMode === "week") {
      const rangeStartDate = navigatorConfig.selectionStart;
      const rangeEndDate = navigatorConfig.selectionEnd;
      return `${rangeStartDate.toString("MM-dd")} to ${rangeEndDate.toString("MM-dd")}`;
    }
    if (navigatorConfig.selectMode === "day") {
      return navigatorConfig.selectionDay.toString("dddd, MM-dd");
    }
  } catch {
    return "";
  }
  return "";
}

export function parseCriteriaDate(raw: any): Date | null {
  if (raw == null || raw === "") return null;
  if (raw instanceof Date && !Number.isNaN(raw.getTime())) return raw;
  const d = new Date(raw);
  return Number.isNaN(d.getTime()) ? null : d;
}

/** After search completes: sync DayPilot start from calendar navigation criteria unless last trigger was navigator. */
export function getCalendarStartDateFromSearchDto(
  searchDto: any | null | undefined,
  dictDcuValue: Record<string, any> | null | undefined,
): any | null {
  if (typeof DayPilot === "undefined") return null;
  const minPilotDate = new DayPilot.Date("1000-01-01");
  const criterias = searchDto?.Criterias;
  if (!Array.isArray(criterias) || !dictDcuValue) return null;
  for (const aCriteriaDto of criterias) {
    if (aCriteriaDto?.EmInternalCodeRegistration === EmInternalCodeRegistration.CalendarNavigationStartDate && aCriteriaDto?.SearcDCUID) {
      const raw = dictDcuValue[String(aCriteriaDto.SearcDCUID)];
      const dt = parseCriteriaDate(raw);
      if (!dt) return null;
      const dayPilotStartDate = new DayPilot.Date(dt);
      if (dayPilotStartDate && dayPilotStartDate > minPilotDate) {
        return dayPilotStartDate;
      }
    }
  }
  return null;
}

function findViewColumnById(columns: any[] | undefined, columnId: any): any | null {
  if (!Array.isArray(columns) || columnId == null) return null;
  const idStr = String(columnId);
  return columns.find((c: any) => c?.Id != null && String(c.Id) === idStr) ?? null;
}

/** Calendar blank-cell / create link mapping — mirrors searchViewHelper.executeSearchViewLinkTarget calendar branch. */
export function buildCalendarLinkTargetValueMapping(
  viewDto: any,
  linkTarget: any,
  selecedDataRow: Record<string, any>,
): Record<string, any> {
  const linkTargetValueMapping: Record<string, any> = {};
  const columns = viewDto?.Columns;

  const mapOne = (sourceViewColumnId: any, targetColumn: string | null | undefined) => {
    if (!sourceViewColumnId || !targetColumn) return;
    const viewColumn = findViewColumnById(columns, sourceViewColumnId);
    if (viewColumn?.DisplayName) {
      const eventPropertyName = viewColumn.DisplayName;
      linkTargetValueMapping[targetColumn] = selecedDataRow[eventPropertyName];
    }
  };

  mapOne(linkTarget?.SourceViewColumnId1, linkTarget?.TargetColumn1);
  mapOne(linkTarget?.SourceViewColumnId2, linkTarget?.TargetColumn2);
  mapOne(linkTarget?.SourceViewColumnId3, linkTarget?.TargetColumn3);
  return linkTargetValueMapping;
}

export type BlankCellMenuLists = {
  calendarBlankCellLinkTargetList: any[];
  calendarBlankCellLinkedSearchList: any[];
};

/** Mirrors calendarHelper.blank cell list filtering. */
export function buildCalendarBlankCellMenuLists(viewDto: any, currentCopyOrCutObj: any | null | undefined): BlankCellMenuLists {
  const calendarBlankCellLinkTargetList: any[] = [];
  const calendarBlankCellLinkedSearchList: any[] = [];
  const formTargets = Array.isArray(viewDto?.AppFormLinkTargetList) ? viewDto.AppFormLinkTargetList : [];

  for (const linkTargetDto of formTargets) {
    if (linkTargetDto?.LinkTargetUsageType === EmAppLinkTargetUsageType.SearchViewLinkToForm) {
      if (linkTargetDto.ActionType === EmAppLinkTargetActionType.Create) {
        calendarBlankCellLinkTargetList.push(linkTargetDto);
      } else if (linkTargetDto.ActionType === EmAppLinkTargetActionType.PasteEvent) {
        const dto = { ...linkTargetDto };
        dto.isLinkTargetMenuVisible = () => currentCopyOrCutObj != null;
        calendarBlankCellLinkTargetList.push(dto);
      }
    } else if (linkTargetDto?.LinkTargetUsageType === EmAppLinkTargetUsageType.SearchViewLinkToSearch) {
      calendarBlankCellLinkTargetList.push(linkTargetDto);
    }
  }

  const linked = Array.isArray(viewDto?.AppViewLinkedSeaechOrUrlDtoList) ? viewDto.AppViewLinkedSeaechOrUrlDtoList : [];
  for (const linkTargetDto of linked) {
    calendarBlankCellLinkedSearchList.push(linkTargetDto);
  }

  return { calendarBlankCellLinkTargetList, calendarBlankCellLinkedSearchList };
}

/** Linked search criteria from a row or calendar cell param (Angular openSearchViewLinkTargetSearchOrRoutePage). */
export function buildLinkedSearchCriteriaDict(linkTargetDto: any, aSearchResult: any): Record<string, any> {
  const dictCreteriaIdValue: Record<string, any> = {};
  if (!linkTargetDto || !aSearchResult) return dictCreteriaIdValue;

  const spec = EmAppLinkTargetSourceColumnType.SearchViewSpecialProperty;
  if (linkTargetDto.SourceColumnType === spec) {
    if (linkTargetDto.SourceViewColumnId1 && linkTargetDto.TargetSearchFieldId1) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId1] = aSearchResult[linkTargetDto.SourceViewColumnId1];
    }
    if (linkTargetDto.SourceViewColumnId2 && linkTargetDto.TargetSearchFieldId2) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId2] = aSearchResult[linkTargetDto.SourceViewColumnId2];
    }
    if (linkTargetDto.SourceViewColumnId3 && linkTargetDto.TargetSearchFieldId3) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId3] = aSearchResult[linkTargetDto.SourceViewColumnId3];
    }
    if (linkTargetDto.SourceViewColumnId4 && linkTargetDto.TargetSearchFieldId4) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId4] = aSearchResult[linkTargetDto.SourceViewColumnId4];
    }
    if (linkTargetDto.SourceViewColumnId5 && linkTargetDto.TargetSearchFieldId5) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId5] = aSearchResult[linkTargetDto.SourceViewColumnId5];
    }
  } else {
    const dict = aSearchResult.DictViewColumnIDKeyValue || {};
    if (linkTargetDto.SourceViewColumnId1 && linkTargetDto.TargetSearchFieldId1) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId1] = dict[linkTargetDto.SourceViewColumnId1];
    }
    if (linkTargetDto.SourceViewColumnId2 && linkTargetDto.TargetSearchFieldId2) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId2] = dict[linkTargetDto.SourceViewColumnId2];
    }
    if (linkTargetDto.SourceViewColumnId3 && linkTargetDto.TargetSearchFieldId3) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId3] = dict[linkTargetDto.SourceViewColumnId3];
    }
    if (linkTargetDto.SourceViewColumnId4 && linkTargetDto.TargetSearchFieldId4) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId4] = dict[linkTargetDto.SourceViewColumnId4];
    }
    if (linkTargetDto.SourceViewColumnId5 && linkTargetDto.TargetSearchFieldId5) {
      dictCreteriaIdValue[linkTargetDto.TargetSearchFieldId5] = dict[linkTargetDto.SourceViewColumnId5];
    }
  }
  return dictCreteriaIdValue;
}

export function weekHeaderDateFormatForViewport(): string {
  if (typeof window === "undefined") return "dddd, yyyy-MM-dd";
  const vw = Math.max(document.documentElement.clientWidth, window.innerWidth || 0);
  if (vw > 1400) return "dddd, yyyy-MM-dd";
  if (vw > 350) return "ddd MM-dd";
  return "ddd";
}
