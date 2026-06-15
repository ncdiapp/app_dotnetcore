import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { GridViewLayout } from "./GridViewLayout";
import { useTheme } from "../../../redux/hooks/useTheme";
import { useTabNavigation } from "../../../redux/hooks/useTabNavigation";
import { buildLinkTargetTabTitle, getDictViewColumnValue } from "../../../utils/linkTargetTabTitle";
import {
  filterRowContextMenuFormLinkTargets,
  filterRowContextMenuLinkedSearches,
} from "../../../utils/transactionFormGroupHelper";
import {
  buildCalendarBlankCellMenuLists,
  buildCalendarLinkTargetValueMapping,
  buildLinkedSearchCriteriaDict,
  buildNavigatorCriteriaPatch,
  convertSearchRowsToDayPilotEvents,
  EmAppLinkTargetActionType,
  EmAppLinkTargetUsageType as EmSearchLinkUsageType,
  EmAppViewTypeCalendarView,
  EmAppCanlendarMode,
  getCalendarStartDateFromSearchDto,
  getNavigatorRangeDisplayText,
  hasCalendarNavigationCriteria,
  weekHeaderDateFormatForViewport,
  type CalendarCellParam,
} from "./searchCalendarHelper";

declare const DayPilot: any;

/** Event row context menu: same action types as Angular grid row menu (AppEnums). */
const EmAppLinkTargetActionTypeMenu = {
  Edit: 1,
  Create: 2,
  Preview: 5,
  EditOnPopup: 12,
  CreateFromExistingItem: 13,
};

const EmAppLinkTargetUsageType = {
  SearchViewLinkSystemDefinedPage: 5,
};

interface CalendarViewLayoutProps {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  /** Persist merged criteria + run search (calendar navigator / range). */
  onPatchDictDcuValueAndSearch?: (patch: Record<string, any>) => Promise<void>;
}

export const CalendarViewLayout: React.FC<CalendarViewLayoutProps> = ({
  viewDto,
  viewDataList,
  dataModel,
  onExecuteSearch,
  onPatchDictDcuValueAndSearch,
}) => {
  const { theme, t } = useTheme();
  const { addTabAndNavigate } = useTabNavigation();

  const uiId = dataModel?.uiControl?.uiId ?? "cal_default";
  const naviElId = `navi_${uiId}`;
  const calElId = `cal_main_${uiId}`;
  const monthElId = `cal_month_${uiId}`;

  const locale =
    (typeof navigator !== "undefined" && navigator.language) ? navigator.language : "en-us";

  const [rangeLabel, setRangeLabel] = useState("");
  const [activeMode, setActiveMode] = useState<"day" | "week" | "month">("week");
  const [detailOpen, setDetailOpen] = useState(false);
  const [currentEvent, setCurrentEvent] = useState<any>(null);

  const [eventMenu, setEventMenu] = useState<{ visible: boolean; x: number; y: number; dataItem: any | null }>({
    visible: false,
    x: 0,
    y: 0,
    dataItem: null,
  });

  const [cellMenu, setCellMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    param: CalendarCellParam | null;
  }>({ visible: false, x: 0, y: 0, param: null });

  const navigatorRef = useRef<any>(null);
  const calendarRef = useRef<any>(null);
  const monthRef = useRef<any>(null);
  const isOnInitRef = useRef(true);
  const skipSyncRangeFromSearchRef = useRef(false);
  const searchDtoRef = useRef<any>(null);
  const patchSearchRef = useRef(onPatchDictDcuValueAndSearch);
  const executeSearchRef = useRef(onExecuteSearch);
  const onEventClickRef = useRef<(args: any) => void>(() => {});
  const onTimeRangeSelectedRef = useRef<(args: any) => void>(() => {});
  const onTimeRangeDoubleClickRef = useRef<(args: any, ev: any) => void>(() => {});

  useEffect(() => {
    patchSearchRef.current = onPatchDictDcuValueAndSearch;
  }, [onPatchDictDcuValueAndSearch]);

  useEffect(() => {
    executeSearchRef.current = onExecuteSearch;
  }, [onExecuteSearch]);

  useEffect(() => {
    searchDtoRef.current = dataModel?.searchDto ?? null;
  }, [dataModel?.searchDto]);

  useEffect(() => {
    const t = setTimeout(() => {
      isOnInitRef.current = false;
    }, 1000);
    return () => clearTimeout(t);
  }, []);

  useEffect(() => {
    const close = () => {
      setEventMenu((m) => ({ ...m, visible: false }));
      setCellMenu((m) => ({ ...m, visible: false }));
    };
    document.addEventListener("click", close);
    return () => document.removeEventListener("click", close);
  }, []);

  const showNavigator = Boolean(viewDto?.IsEnableCalendarNavigator);
  let showMonthBtn = Boolean(viewDto?.IsEnableCalendarMonthView);
  let showWeekBtn = Boolean(viewDto?.IsEnableCalendarWeekView);
  let showDayBtn = Boolean(viewDto?.IsEnableCalendarDayView);
  if (!showMonthBtn && !showWeekBtn && !showDayBtn) {
    showWeekBtn = true;
  }

  const defaultMode = useMemo((): "day" | "week" | "month" => {
    const m = viewDto?.CanlendarDefaultViewMode;
    if (m === EmAppCanlendarMode.DayView) return "day";
    if (m === EmAppCanlendarMode.MonthView) return "month";
    return "week";
  }, [viewDto?.CanlendarDefaultViewMode]);

  useEffect(() => {
    setActiveMode(defaultMode);
  }, [defaultMode, viewDto?.Id]);

  const events = useMemo(() => convertSearchRowsToDayPilotEvents(viewDataList), [viewDataList]);

  const blankMenus = useMemo(
    () => buildCalendarBlankCellMenuLists(viewDto, dataModel?.currentCopyOrCutObj),
    [viewDto, dataModel?.currentCopyOrCutObj],
  );

  const eventMenuLinkTargets = useMemo(() => {
    const list = filterRowContextMenuFormLinkTargets(viewDto?.AppFormLinkTargetList);
    return list.filter(
      (lt: any) =>
        lt?.ActionType === EmAppLinkTargetActionTypeMenu.Edit ||
        lt?.ActionType === EmAppLinkTargetActionTypeMenu.EditOnPopup ||
        lt?.ActionType === EmAppLinkTargetActionTypeMenu.Preview ||
        lt?.ActionType === EmAppLinkTargetActionTypeMenu.CreateFromExistingItem,
    );
  }, [viewDto?.AppFormLinkTargetList]);

  const linkedSearchList = useMemo(() => {
    const raw = viewDto?.AppViewLinkedSeaechOrUrlDtoList?.filter((ls: any) => ls?.LinkTargetSearchId) || [];
    return filterRowContextMenuLinkedSearches(raw);
  }, [viewDto?.AppViewLinkedSeaechOrUrlDtoList]);

  const hasFormLinkTargets = Array.isArray(viewDto?.AppFormLinkTargetList) && viewDto.AppFormLinkTargetList.length > 0;

  const isLinkTargetAllowed = (linkTarget: any, row: any): boolean => {
    if (!linkTarget?.SourceConditionViewColumnId) return true;
    const v = row?.DictViewColumnIDKeyValue?.[linkTarget.SourceConditionViewColumnId];
    return Boolean(v && v !== "False" && v !== "0");
  };

  const executeLinkTarget = useCallback(
    (linkTarget: any, row: any) => {
      if (!linkTarget || !row) return;
      if (!isLinkTargetAllowed(linkTarget, row)) {
        window.alert(`${linkTarget.NavigationActionName || "Action"} is not available for current row.`);
        return;
      }

      if (linkTarget.LinkTargetUsageType === EmAppLinkTargetUsageType.SearchViewLinkSystemDefinedPage && linkTarget.LinkTargetUrlOrRouteCode) {
        let paramId: string | null = null;
        let param1: string | null = null;
        let _param2: string | null = null;
        if (linkTarget.SourceViewColumnId1) {
          paramId = getDictViewColumnValue(row.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId1) ?? null;
        }
        if (linkTarget.SourceViewColumnId2 && linkTarget.TargetColumn2) {
          param1 = getDictViewColumnValue(row.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId2) ?? null;
        }
        if (linkTarget.SourceViewColumnId3 && linkTarget.TargetColumn3) {
          _param2 = getDictViewColumnValue(row.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId3) ?? null;
        }
        const tabTitle = buildLinkTargetTabTitle(
          linkTarget.NavigationActionName,
          Boolean(linkTarget.RowDisplayViewColumnId),
          linkTarget.RowDisplayViewColumnId
            ? getDictViewColumnValue(row.DictViewColumnIDKeyValue, linkTarget.RowDisplayViewColumnId)
            : undefined,
          paramId ?? undefined
        );
        let routeCode = String(linkTarget.LinkTargetUrlOrRouteCode);
        if (routeCode.includes("main.")) routeCode = routeCode.replace("main.", "");
        const paramObj: any = {};
        if (paramId) paramObj.id = paramId;
        if (param1) paramObj.param1 = param1;
        if (_param2) paramObj.param2 = _param2;
        addTabAndNavigate(routeCode, tabTitle, paramObj);
        return;
      }

      if (linkTarget.LinkTargetTransactionId) {
        let paramId: string | null = null;
        let param1: string | null = null;
        if (linkTarget.SourceViewColumnId1) {
          paramId = getDictViewColumnValue(row.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId1) ?? null;
        }
        if (linkTarget.SourceViewColumnId2 && linkTarget.TargetColumn2) {
          param1 = getDictViewColumnValue(row.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId2) ?? null;
        }
        const tabTitle = buildLinkTargetTabTitle(
          linkTarget.NavigationActionName,
          Boolean(linkTarget.RowDisplayViewColumnId),
          linkTarget.RowDisplayViewColumnId
            ? getDictViewColumnValue(row.DictViewColumnIDKeyValue, linkTarget.RowDisplayViewColumnId)
            : undefined,
          paramId ?? undefined
        );
        const paramObj: any = { preserveLinkTabTitle: true };
        if (linkTarget.LinkTargetTransactionId) paramObj.id = linkTarget.LinkTargetTransactionId;
        if (paramId) paramObj.param1 = paramId;
        if (param1) paramObj.param2 = param1;
        addTabAndNavigate("FormMasterDetail", tabTitle, paramObj);
        return;
      }
    },
    [addTabAndNavigate],
  );

  const openCreateFormFromCell = useCallback(
    (linkTarget: any, cell: CalendarCellParam) => {
      if (!linkTarget?.LinkTargetTransactionId) return;
      const row = {
        EventDateId: cell.EventDateId,
        EventUserId: cell.EventUserId,
        EventStartDate: cell.EventStartDate,
        EventEndDate: cell.EventEndDate,
      };
      const mapping = buildCalendarLinkTargetValueMapping(viewDto, linkTarget, row);
      const param2Obj: any = {
        openFrom: null,
        opennedFormAutoExecuteCommandId: linkTarget.OpennedFormAutoExecuteCommandId ?? null,
        linkTargetValueMapping: mapping,
      };
      addTabAndNavigate("FormMasterDetail", linkTarget.NavigationActionName || "Create", {
        id: linkTarget.LinkTargetTransactionId,
        param2: JSON.stringify(param2Obj),
      });
    },
    [addTabAndNavigate, viewDto],
  );

  const openLinkedSearchFromCell = useCallback(
    (linked: any, cell: CalendarCellParam) => {
      if (!linked?.LinkTargetSearchId) return;
      const pseudoRow = {
        DictViewColumnIDKeyValue: {},
        EventDateId: cell.EventDateId,
        EventUserId: cell.EventUserId,
        EventStartDate: cell.EventStartDate,
        EventEndDate: cell.EventEndDate,
      };
      const dictCreteriaIdValue = buildLinkedSearchCriteriaDict(linked, pseudoRow);
      const title = linked.DisplayText || linked.NavigationActionName || "Search";
      const paramObj: any = {
        searchId: linked.LinkTargetSearchId,
        isSavedSearch: false,
        dictCreteriaIdValue,
      };
      if (linked.LinkTargetSearchViewId) {
        paramObj.initialViewId = linked.LinkTargetSearchViewId;
      }
      addTabAndNavigate("masterdatamanagement", title, paramObj);
    },
    [addTabAndNavigate],
  );

  /** Navigator / config: update visible range + optional search (Angular calendarHelper.changeCalendarTimeRange). */
  const applyNavigatorRange = useCallback(
    async (naviStart: any, naviEnd: any) => {
      const sd = searchDtoRef.current;
      const patch = buildNavigatorCriteriaPatch(sd, naviStart, naviEnd);
      if (calendarRef.current && naviStart) {
        calendarRef.current.startDate = naviStart;
        calendarRef.current.update?.();
      }
      if (monthRef.current && naviStart) {
        monthRef.current.startDate = naviStart;
        monthRef.current.update?.();
      }
      const run = patchSearchRef.current;
      if (run) {
        skipSyncRangeFromSearchRef.current = true;
        await run(patch);
      } else if (executeSearchRef.current) {
        await executeSearchRef.current();
      }
    },
    [],
  );

  const onTimeRangeSelectedHandler = useCallback(
    async (args: any) => {
      if (!args?.start || !args?.end) return;
      const rangeTicks = args.end.ticks - args.start.ticks;
      if (rangeTicks < 1800000 || isOnInitRef.current) return;
      const positionObj = {
        top: "40%",
        left: "calc(40% - 40px)",
      };
      const elemSelectedCell = args.control?.elements?.selection;
      let pos = positionObj;
      if (elemSelectedCell && elemSelectedCell.length > 0 && typeof window !== "undefined" && (window as any).$) {
        try {
          const $ = (window as any).$;
          const el = $(elemSelectedCell);
          if (el?.offset) {
            pos = { top: `${el.offset().top + 10}px`, left: `${el.offset().left + 20}px` };
          }
        } catch {
          /* ignore */
        }
      }
      const dateId = args.start ? args.start.toString("yyyyMMdd") : null;
      const cell: CalendarCellParam = {
        EventDateId: dateId,
        EventUserId: dataModel?.userContext?.UserId ?? dataModel?.UserId,
        EventStartDate: args.start?.value,
        EventEndDate: args.end?.value,
      };
      if (
        blankMenus.calendarBlankCellLinkTargetList.length > 0 ||
        blankMenus.calendarBlankCellLinkedSearchList.length > 0
      ) {
        const x =
          typeof pos.left === "string" && pos.left.includes("px")
            ? parseInt(pos.left, 10)
            : Math.round((typeof window !== "undefined" ? window.innerWidth : 800) * 0.35);
        const y =
          typeof pos.top === "string" && pos.top.includes("px")
            ? parseInt(pos.top, 10)
            : Math.round((typeof window !== "undefined" ? window.innerHeight : 600) * 0.35);
        setCellMenu({ visible: true, x: Number.isFinite(x) ? x : 120, y: Number.isFinite(y) ? y : 120, param: cell });
      }
    },
    [blankMenus, dataModel?.userContext?.UserId, dataModel?.UserId],
  );

  const onTimeRangeDoubleClickHandler = useCallback(
    (args: any, clickEvent: any) => {
      if (!args?.start) return;
      const dateId = args.start.toString("yyyyMMdd");
      const cell: CalendarCellParam = {
        EventDateId: dateId,
        EventUserId: dataModel?.userContext?.UserId ?? dataModel?.UserId,
        EventStartDate: args.start.value,
        EventEndDate: args.end.value,
      };
      if (
        blankMenus.calendarBlankCellLinkTargetList.length > 0 ||
        blankMenus.calendarBlankCellLinkedSearchList.length > 0
      ) {
        if (clickEvent?.clientX != null) {
          setCellMenu({ visible: true, x: clickEvent.clientX, y: clickEvent.clientY, param: cell });
        }
      }
    },
    [blankMenus, dataModel?.userContext?.UserId, dataModel?.UserId],
  );

  const onEventClickHandler = useCallback(
    (args: any) => {
      const ev = args?.e?.data;
      if (!ev) return;
      setCurrentEvent(ev);
      if (hasFormLinkTargets) {
        const ce = args?.originalEvent;
        if (ce?.clientX != null) {
          setEventMenu({ visible: true, x: ce.clientX, y: ce.clientY, dataItem: ev.DataItem });
        }
      } else {
        setDetailOpen(true);
      }
    },
    [hasFormLinkTargets],
  );

  useEffect(() => {
    onEventClickRef.current = onEventClickHandler;
    onTimeRangeSelectedRef.current = onTimeRangeSelectedHandler;
    onTimeRangeDoubleClickRef.current = onTimeRangeDoubleClickHandler;
  }, [onEventClickHandler, onTimeRangeSelectedHandler, onTimeRangeDoubleClickHandler]);

  const applyNavigatorRangeRef = useRef(applyNavigatorRange);
  useEffect(() => {
    applyNavigatorRangeRef.current = applyNavigatorRange;
  }, [applyNavigatorRange]);

  const refreshRangeLabel = useCallback(() => {
    const nav = navigatorRef.current;
    if (nav) {
      setRangeLabel(getNavigatorRangeDisplayText(nav));
    }
  }, []);

  const refreshRangeLabelRef = useRef(refreshRangeLabel);
  useEffect(() => {
    refreshRangeLabelRef.current = refreshRangeLabel;
  }, [refreshRangeLabel]);

  /** Init DayPilot once per layout id (Angular initializeCalendarView / initializeNavigator). */
  useEffect(() => {
    if (typeof DayPilot === "undefined") return;

    const navEl = document.getElementById(naviElId);
    if (!navEl) return;

    if (!navigatorRef.current) {
      const navigatorControl = new DayPilot.Navigator(naviElId);
      navigatorControl.theme = "dpnav";
      navigatorControl.selectMode = "week";
      navigatorControl.showMonths = 3;
      navigatorControl.skipMonths = 3;
      navigatorControl.onTimeRangeSelected = (args: any) => {
        applyNavigatorRangeRef.current(args.start, args.end);
        requestAnimationFrame(() => refreshRangeLabelRef.current());
      };
      navigatorControl.selectionEnd = null;
      navigatorControl.selectionStart = null;
      navigatorControl.init();
      navigatorRef.current = navigatorControl;
    }

    const calHost = document.getElementById(calElId);
    if (calHost && !calendarRef.current) {
      const cal = new DayPilot.Calendar(calElId);
      cal.theme = "dpcalendar";
      cal.viewType = "Week";
      cal.showAllDayEvents = true;
      cal.locale = locale;
      cal.heightSpec = "Parent100Pct";
      cal.columnWidthSpec = "Auto";
      cal.headerLevels = 1;
      cal.headerHeight = 20;
      cal.headerDateFormat = weekHeaderDateFormatForViewport();
      cal.cellDuration = 30;
      cal.cellHeight = 20;
      cal.crosshairType = "Header";
      cal.businessBeginsHour = 8;
      cal.businessEndsHour = 18;
      cal.showCurrentTime = false;
      cal.eventArrangement = "Cascade";
      cal.allowEventOverlap = true;
      cal.eventDeleteHandling = "Disabled";
      cal.eventMoveHandling = "Disabled";
      cal.eventResizeHandling = "Disabled";
      cal.eventClickHandling = "Enabled";
      cal.onEventClick = (args: any) => onEventClickRef.current(args);
      cal.onTimeRangeSelected = (args: any) => {
        const rangeTicks = args.end.ticks - args.start.ticks;
        if (rangeTicks >= 1800000 && !isOnInitRef.current) {
          onTimeRangeSelectedRef.current(args);
        }
      };
      cal.timeRangeDoubleClickHandling = "Enabled";
      cal.onTimeRangeDoubleClick = (args: any) => {
        const ev = (args as any).originalEvent || (typeof window !== "undefined" ? window.event : null);
        onTimeRangeDoubleClickRef.current(args, ev);
      };
      cal.init();
      calendarRef.current = cal;
    }

    const monthHost = document.getElementById(monthElId);
    if (monthHost && !monthRef.current) {
      const month = new DayPilot.Month(monthElId);
      month.theme = "dpmonth";
      month.heightSpec = "Parent100Pct";
      month.locale = locale;
      month.eventClickHandling = "Enabled";
      month.eventMoveHandling = "Disabled";
      month.eventResizeHandling = "Disabled";
      month.onEventClick = (args: any) => onEventClickRef.current(args);
      month.onTimeRangeSelected = (args: any) => onTimeRangeSelectedRef.current(args);
      month.init();
      monthRef.current = month;
    }

    return () => {
      try {
        navigatorRef.current?.dispose?.();
      } catch {
        /* ignore */
      }
      try {
        calendarRef.current?.dispose?.();
      } catch {
        /* ignore */
      }
      try {
        monthRef.current?.dispose?.();
      } catch {
        /* ignore */
      }
      navigatorRef.current = null;
      calendarRef.current = null;
      monthRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- init once per element ids; handlers use refs
  }, [naviElId, calElId, monthElId, locale]);

  /** Default view mode once controls exist (Angular showDay/showWeek/showMonth). */
  useEffect(() => {
    const nav = navigatorRef.current;
    if (!nav || typeof DayPilot === "undefined") return;
    const apply = () => {
      if (activeMode === "month") {
        nav.selectMode = "month";
      } else if (activeMode === "week") {
        nav.selectMode = "week";
      } else {
        nav.selectMode = "day";
      }
      nav.update?.();
      if (nav.selectionDay) {
        nav.select(nav.selectionDay, { dontFocus: false, dontNotify: true });
      }
      const cal = calendarRef.current;
      if (cal && activeMode !== "month") {
        cal.viewType = activeMode === "day" ? "Day" : "Week";
        cal.headerDateFormat = activeMode === "day" ? "dddd, yyyy-MM-dd" : weekHeaderDateFormatForViewport();
        cal.update?.();
      }
      refreshRangeLabel();
    };
    const t = window.setTimeout(apply, 200);
    return () => window.clearTimeout(t);
  }, [activeMode, refreshRangeLabel]);

  /** Push events into DayPilot lists. */
  useEffect(() => {
    if (typeof DayPilot === "undefined") return;
    const cal = calendarRef.current;
    const month = monthRef.current;
    if (cal?.events) {
      cal.events.list = events;
      cal.update?.();
    }
    if (month?.events) {
      month.events.list = events;
      month.update?.();
    }
  }, [events]);

  /** After search: sync start date from criteria unless navigator triggered this refresh (Angular setCalendarDisplayRangeFromSearchDto). */
  useEffect(() => {
    if (typeof DayPilot === "undefined") return;
    if (skipSyncRangeFromSearchRef.current) {
      skipSyncRangeFromSearchRef.current = false;
      return;
    }
    const start = getCalendarStartDateFromSearchDto(dataModel?.searchDto, dataModel?.dictDcuValue);
    const nav = navigatorRef.current;
    const cal = calendarRef.current;
    const month = monthRef.current;
    if (start && nav?.select) {
      nav.select(start, { dontFocus: false, dontNotify: true });
    }
    if (start && cal) {
      cal.startDate = start;
      cal.update?.();
    }
    if (start && month) {
      month.startDate = start;
      month.update?.();
    }
    refreshRangeLabel();
  }, [viewDataList, dataModel?.dictDcuValue, dataModel?.searchDto, refreshRangeLabel]);

  const modeButtonClass = (mode: "day" | "week" | "month") =>
    `px-2 py-0.5 text-xs rounded ${theme.button_default} ${activeMode === mode ? "ring-1 ring-offset-1 ring-blue-500" : ""}`;

  /** Angular calendarHelper.stepChangeCalendarRange */
  const stepCalendarRange = useCallback((isBackward: boolean) => {
    if (typeof DayPilot === "undefined") return;
    const nav = navigatorRef.current;
    if (!nav) return;
    const stepLength = isBackward ? -1 : 1;
    const rangeStartDate = nav.selectionStart;
    let newStart: any = null;
    let newEnd: any = null;
    if (nav.selectMode === "month") {
      newStart = rangeStartDate.addMonths(stepLength);
      newEnd = newStart.addMonths(1).addDays(-1);
    } else if (nav.selectMode === "week") {
      newStart = rangeStartDate.addDays(stepLength * 7);
      newEnd = newStart.addDays(6);
    } else {
      newStart = rangeStartDate.addDays(stepLength);
      newEnd = newStart;
    }
    if (!newStart || !newEnd) return;
    const newStartDateUtc = new DayPilot.Date(new Date(newStart));
    const newEndDateUtc = new DayPilot.Date(new Date(newEnd));
    if (hasCalendarNavigationCriteria(searchDtoRef.current)) {
      nav.select(newStart, { dontFocus: false, dontNotify: false });
      return;
    }
    let isInSearchDateRange = true;
    const sd = searchDtoRef.current;
    const searchStartDate = sd?.searchStartDate;
    const searchEndDate = sd?.searchEndDate;
    if (searchStartDate) {
      const searchStartDateDp = new DayPilot.Date(searchStartDate);
      if (newEndDateUtc < searchStartDateDp && isBackward) {
        isInSearchDateRange = false;
      }
    }
    if (searchEndDate) {
      const searchEndDateDp = new DayPilot.Date(searchEndDate);
      if (newStartDateUtc > searchEndDateDp && !isBackward) {
        isInSearchDateRange = false;
      }
    }
    if (isInSearchDateRange) {
      nav.select(newStart, { dontFocus: false, dontNotify: false });
    }
  }, []);

  const goPrev = useCallback(() => stepCalendarRange(true), [stepCalendarRange]);
  const goNext = useCallback(() => stepCalendarRange(false), [stepCalendarRange]);

  const increaseMonths = useCallback(() => {
    const nav = navigatorRef.current;
    if (nav?.showMonths < 4) {
      nav.showMonths++;
      nav.update?.();
    }
  }, []);

  const decreaseMonths = useCallback(() => {
    const nav = navigatorRef.current;
    if (nav?.showMonths > 1) {
      nav.showMonths--;
      nav.update?.();
    }
  }, []);

  if (viewDto?.IsClusterChildView) {
    return (
      <div className="w-full h-full flex flex-col">
        <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>
          Cluster child calendar view is not migrated in React; showing grid.
        </div>
        <div className="w-full h-1 flex-auto overflow-hidden">
          <GridViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
          />
        </div>
      </div>
    );
  }

  if (typeof DayPilot === "undefined" || viewDto?.ViewType !== EmAppViewTypeCalendarView) {
    return (
      <div className="w-full h-full flex flex-col">
        {typeof DayPilot === "undefined" && (
          <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>DayPilot not loaded; showing grid.</div>
        )}
        <div className="w-full h-1 flex-auto overflow-hidden">
          <GridViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
          />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-row overflow-hidden">
      {showNavigator ? (
        <div className={`w-[160px] shrink-0 h-full overflow-x-hidden overflow-y-auto p-1 border-r ${t("border_mainContentSection")}`}>
          <div id={naviElId} className="w-full" />
          <div className="w-full h-8 flex items-center justify-center gap-1 mt-1">
            <button type="button" className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`} onClick={decreaseMonths} title="Fewer months">
              −
            </button>
            <button type="button" className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`} onClick={increaseMonths} title="More months">
              +
            </button>
          </div>
        </div>
      ) : (
        <div className="absolute w-px h-px overflow-hidden opacity-0 pointer-events-none" aria-hidden>
          <div id={naviElId} />
        </div>
      )}

      <div className="h-full w-1 flex-auto flex flex-col min-w-0 min-h-0">
        <div className={`flex flex-wrap items-center gap-1 px-2 py-1 border-b shrink-0 ${theme.mainContentSection}`}>
          {showMonthBtn && (
            <button type="button" className={modeButtonClass("month")} onClick={() => setActiveMode("month")}>
              Month {activeMode === "month" ? <i className="fa-solid fa-check ml-1" aria-hidden /> : null}
            </button>
          )}
          {showWeekBtn && (
            <button type="button" className={modeButtonClass("week")} onClick={() => setActiveMode("week")}>
              Week {activeMode === "week" ? <i className="fa-solid fa-check ml-1" aria-hidden /> : null}
            </button>
          )}
          {showDayBtn && (
            <button type="button" className={modeButtonClass("day")} onClick={() => setActiveMode("day")}>
              Day {activeMode === "day" ? <i className="fa-solid fa-check ml-1" aria-hidden /> : null}
            </button>
          )}
          <div className="flex items-center gap-1 pl-2">
            <button type="button" className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`} onClick={goPrev} title="Previous">
              <i className="fa-solid fa-caret-left" aria-hidden />
            </button>
            <button type="button" className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`} onClick={goNext} title="Next">
              <i className="fa-solid fa-caret-right" aria-hidden />
            </button>
            <span className={`text-xs font-semibold pl-1 ${theme.label}`}>{rangeLabel}</span>
          </div>
        </div>

        <div className={`w-full h-1 flex-auto relative min-h-[200px] ${theme.mainContentSection}`}>
          <div
            className="absolute inset-0 w-full h-full"
            style={{ display: activeMode === "month" ? "none" : "block" }}
          >
            <div id={calElId} className="w-full h-full min-h-[200px]" />
          </div>
          <div
            className="absolute inset-0 w-full h-full"
            style={{ display: activeMode === "month" ? "block" : "none" }}
          >
            <div id={monthElId} className="w-full h-full min-h-[200px]" />
          </div>
        </div>
      </div>

      {detailOpen && currentEvent && (
        <div
          className="fixed inset-0 z-[1300] flex items-center justify-center bg-black/40"
          onClick={() => setDetailOpen(false)}
        >
          <div
            className={`w-[min(100%,360px)] max-h-[90vh] overflow-auto rounded border shadow-lg p-3 ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex justify-between items-center mb-2 ${theme.title}`}>
              <span className="text-sm font-semibold truncate pr-2">{currentEvent.text}</span>
              <button type="button" className={theme.button_default} onClick={() => setDetailOpen(false)} aria-label="Close">
                ×
              </button>
            </div>
            <div className={`text-xs space-y-1 ${theme.label}`}>
              <div>
                <span className="font-semibold">Event Type: </span>
                {currentEvent.EventTypeDisplay}
              </div>
              <div>
                <span className="font-semibold">User: </span>
                {currentEvent.userDisplay}
              </div>
              <div>
                <span className="font-semibold">Start: </span>
                {currentEvent.start?.toString?.("yyyy-MM-dd HH:mm:ss")}
              </div>
              <div>
                <span className="font-semibold">End: </span>
                {currentEvent.end?.toString?.("yyyy-MM-dd HH:mm:ss")}
              </div>
              <div>
                <span className="font-semibold">Description</span>
                <div className="mt-1 p-2 rounded border border-opacity-50 whitespace-pre-wrap">{currentEvent.body}</div>
              </div>
            </div>
            <div className="mt-3 flex justify-end">
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setDetailOpen(false)}>
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {eventMenu.visible && eventMenu.dataItem && (
        <div
          className={`fixed z-[1200] min-w-[220px] border rounded shadow-lg ${theme.mainContentSection}`}
          style={{ left: eventMenu.x, top: eventMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {eventMenuLinkTargets
            .filter((lt: any) => isLinkTargetAllowed(lt, eventMenu.dataItem))
            .map((lt: any, idx: number) => (
              <button
                key={`ce-${lt?.Id ?? idx}`}
                type="button"
                className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
                onClick={() => {
                  executeLinkTarget(lt, eventMenu.dataItem);
                  setEventMenu((m) => ({ ...m, visible: false }));
                }}
              >
                {lt?.NavigationActionName || "Action"}
              </button>
            ))}
          {linkedSearchList.map((ls: any, idx: number) => (
            <button
              key={`cls-${ls?.Id ?? idx}`}
              type="button"
              className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
              onClick={() => {
                const title = ls?.NavigationActionName || ls?.DisplayText || "Search";
                const paramObj: any = {
                  searchId: ls.LinkTargetSearchId,
                  isSavedSearch: false,
                };
                if (ls.LinkTargetSearchViewId) paramObj.initialViewId = ls.LinkTargetSearchViewId;
                if (eventMenu.dataItem?.Id != null) paramObj.linkedSourceRowId = eventMenu.dataItem.Id;
                addTabAndNavigate("masterdatamanagement", title, paramObj);
                setEventMenu((m) => ({ ...m, visible: false }));
              }}
            >
              {ls?.NavigationActionName || ls?.DisplayText || "Navigate To Search"}
            </button>
          ))}
        </div>
      )}

      {cellMenu.visible && cellMenu.param && (
        <div
          className={`fixed z-[1200] min-w-[220px] border rounded shadow-lg ${theme.mainContentSection}`}
          style={{ left: cellMenu.x, top: cellMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {blankMenus.calendarBlankCellLinkTargetList
            .filter((lt: any) => lt.isLinkTargetMenuVisible == null || lt.isLinkTargetMenuVisible())
            .map((lt: any, idx: number) => (
              <button
                key={`cc-${lt?.Id ?? idx}`}
                type="button"
                className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
                onClick={() => {
                  const p = cellMenu.param!;
                  if (lt.LinkTargetUsageType === EmSearchLinkUsageType.SearchViewLinkToForm && lt.ActionType === EmAppLinkTargetActionType.Create) {
                    openCreateFormFromCell(lt, p);
                  } else if (
                    lt.LinkTargetUsageType === EmSearchLinkUsageType.SearchViewLinkToForm &&
                    lt.ActionType === EmAppLinkTargetActionType.PasteEvent
                  ) {
                    /* Angular requires currentCopyOrCutObj — not wired in React yet */
                  } else if (lt.LinkTargetUsageType === EmSearchLinkUsageType.SearchViewLinkToSearch) {
                    openLinkedSearchFromCell(lt, p);
                  }
                  setCellMenu((m) => ({ ...m, visible: false }));
                }}
              >
                <i className="fa-solid fa-plus mr-1" aria-hidden />
                {lt.NavigationActionName}
              </button>
            ))}
          {blankMenus.calendarBlankCellLinkedSearchList.map((ls: any, idx: number) => (
            <button
              key={`ccl-${ls?.Id ?? idx}`}
              type="button"
              className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
              onClick={() => {
                if (cellMenu.param) openLinkedSearchFromCell(ls, cellMenu.param);
                setCellMenu((m) => ({ ...m, visible: false }));
              }}
            >
              <i className="fa-solid fa-plus mr-1" aria-hidden />
              {ls.DisplayText || ls.NavigationActionName}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};
