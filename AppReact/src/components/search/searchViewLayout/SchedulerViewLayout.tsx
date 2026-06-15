import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { GridViewLayout } from "./GridViewLayout";
import { useTheme } from "../../../redux/hooks/useTheme";
import { useEnumValues } from "../../../hooks/useEnumDictionary";
import {
  applySchedulerScrollableWeekDaySnap,
  buildSchedulerBaseDateCriteriaPatch,
  buildSchedulerResources,
  buildSchedulerWeekButtons,
  computeSchedulerResourceRangesFromEvents,
  convertSearchRowToSchedulerEvent,
  getSchedulerViewportLabel,
  updateWeekButtonResourceCounts,
  type SchedulerWeekBtn,
} from "./searchGanttSchedulerHelper";
import { useSearchViewRowLinkExecution } from "./useSearchViewRowLinkExecution";

declare const DayPilot: any;

const EmAppViewTypeSchedulerView = 16;

interface SchedulerViewLayoutProps {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  onPatchDictDcuValueAndSearch?: (patch: Record<string, any>) => Promise<void>;
}

function ensureWeekButtons(weekStartDateList: any[] | undefined | null, startDateTime: any): SchedulerWeekBtn[] {
  const fromServer = buildSchedulerWeekButtons(weekStartDateList);
  if (fromServer.length) return fromServer;
  if (typeof DayPilot === "undefined") return [];
  const base = startDateTime ? new DayPilot.Date(startDateTime, true) : new DayPilot.Date();
  const d0 = base.getDatePart();
  const out: SchedulerWeekBtn[] = [];
  for (let i = 0; i < 4; i++) {
    const dateValue = d0.addDays(i * 7);
    out.push({
      dateValue,
      display: dateValue.toString("MM/dd"),
      resourceCount: 0,
    });
  }
  return out;
}

export const SchedulerViewLayout: React.FC<SchedulerViewLayoutProps> = ({
  viewDto,
  viewDataList,
  dataModel,
  onExecuteSearch,
  onPatchDictDcuValueAndSearch,
}) => {
  const { theme } = useTheme();
  const emAppViewType = useEnumValues("EmAppViewType");
  const schedulerViewType = emAppViewType?.SchedulerView ?? EmAppViewTypeSchedulerView;

  const uiId = dataModel?.uiControl?.uiId ?? "sched_default";
  const viewId = viewDto?.Id != null ? String(viewDto.Id) : "view";
  const schedulerElId = `schedulerViewControl_${viewId}_${uiId}`;

  const searchResultDto = dataModel?.searchResultDto;

  const schedulerRef = useRef<any>(null);
  const viewportRef = useRef<{ start?: any; end?: any } | null>(null);
  const weekBtnsRef = useRef<SchedulerWeekBtn[]>([]);
  const currentWeekDateValueRef = useRef<any>(null);
  const currentWeekIndexRef = useRef(1);
  const onEventClickRef = useRef<(args: any) => void>(() => {});

  // DayPilot can fire async callbacks after React has unmounted the component.
  // Guard + cancel timers to avoid errors like reading DOM `scrollLeft` after unmount.
  const isMountedRef = useRef(true);
  const pendingTimeoutIdsRef = useRef<number[]>([]);
  const trackTimeout = (id: number) => {
    pendingTimeoutIdsRef.current.push(id);
    return id;
  };

  const patchSearchRef = useRef(onPatchDictDcuValueAndSearch);
  const executeSearchRef = useRef(onExecuteSearch);
  const searchDtoRef = useRef(dataModel?.searchDto);

  useEffect(() => {
    patchSearchRef.current = onPatchDictDcuValueAndSearch;
  }, [onPatchDictDcuValueAndSearch]);

  useEffect(() => {
    executeSearchRef.current = onExecuteSearch;
  }, [onExecuteSearch]);

  useEffect(() => {
    searchDtoRef.current = dataModel?.searchDto;
  }, [dataModel?.searchDto]);

  const [viewportLabel, setViewportLabel] = useState("");
  const [currentWeekIndex, setCurrentWeekIndex] = useState(1);
  const [currentWeekDateValue, setCurrentWeekDateValue] = useState<any>(null);
  const [weekBtns, setWeekBtns] = useState<SchedulerWeekBtn[]>([]);

  const [detailOpen, setDetailOpen] = useState(false);
  const [currentEvent, setCurrentEvent] = useState<any>(null);
  const [eventMenu, setEventMenu] = useState<{ visible: boolean; x: number; y: number; dataItem: any | null }>({
    visible: false,
    x: 0,
    y: 0,
    dataItem: null,
  });

  const rowLinks = useSearchViewRowLinkExecution(viewDto, viewDataList, dataModel);

  const { events, resources } = useMemo(() => {
    if (typeof DayPilot === "undefined" || !Array.isArray(viewDataList)) {
      return { events: [] as any[], resources: [] as any[] };
    }
    const raw: any[] = [];
    for (const row of viewDataList) {
      const ev = convertSearchRowToSchedulerEvent(row);
      if (ev && ev.start && ev.end) {
        raw.push(ev);
      }
    }
    const dict = computeSchedulerResourceRangesFromEvents(raw);
    const grouping = searchResultDto?.SchedulerViewGroupByResources;
    const res = buildSchedulerResources(grouping, dict);
    const eventsCopy = raw.map((e) => {
      const c = { ...e, row: { collapsed: true } };
      return c;
    });
    applySchedulerScrollableWeekDaySnap(eventsCopy);
    return { events: eventsCopy, resources: res };
  }, [viewDataList, searchResultDto?.SchedulerViewGroupByResources]);

  useEffect(() => {
    const btns = ensureWeekButtons(searchResultDto?.WeekStartDateList, searchResultDto?.StartDateTime);
    updateWeekButtonResourceCounts(
      btns,
      computeSchedulerResourceRangesFromEvents(
        (viewDataList || [])
          .map((row) => convertSearchRowToSchedulerEvent(row))
          .filter((e) => e && e.start && e.end) as any[],
      ),
    );
    weekBtnsRef.current = btns;
    setWeekBtns(btns);
    const defaultIdx = 1;
    if (btns[defaultIdx]) {
      currentWeekIndexRef.current = defaultIdx;
      currentWeekDateValueRef.current = btns[defaultIdx].dateValue;
      setCurrentWeekIndex(defaultIdx);
      setCurrentWeekDateValue(btns[defaultIdx].dateValue);
    } else {
      currentWeekDateValueRef.current = null;
      setCurrentWeekDateValue(null);
    }
  }, [searchResultDto?.WeekStartDateList, searchResultDto?.StartDateTime, viewDataList]);

  useEffect(() => {
    currentWeekIndexRef.current = currentWeekIndex;
  }, [currentWeekIndex]);

  useEffect(() => {
    currentWeekDateValueRef.current = currentWeekDateValue;
  }, [currentWeekDateValue]);

  useEffect(() => {
    const close = () => setEventMenu((m) => ({ ...m, visible: false }));
    document.addEventListener("click", close);
    return () => document.removeEventListener("click", close);
  }, []);

  const onEventClickHandler = useCallback(
    (args: any) => {
      const ev = args?.e?.data;
      if (!ev) return;
      setCurrentEvent(ev);
      if (rowLinks.hasFormLinkTargets) {
        const ce = args?.originalEvent;
        if (ce?.clientX != null) {
          setEventMenu({ visible: true, x: ce.clientX, y: ce.clientY, dataItem: ev.DataItem });
        }
      } else {
        setDetailOpen(true);
      }
    },
    [rowLinks.hasFormLinkTargets],
  );

  useEffect(() => {
    onEventClickRef.current = onEventClickHandler;
  }, [onEventClickHandler]);

  useEffect(() => {
    if (typeof DayPilot === "undefined") return;
    const host = document.getElementById(schedulerElId);
    if (!host || schedulerRef.current) return;

    const dp = new DayPilot.Scheduler(schedulerElId);
    dp.eventMoveHandling = "Disabled";
    dp.eventResizeHandling = "Disabled";
    dp.timeRangeSelectedHandling = "Disabled";
    dp.theme = "dpscheduler";
    dp.rowMinHeight = 25;
    dp.eventHeight = 18;
    dp.eventMarginLeft = 2;
    dp.eventMarginRight = 2;
    dp.eventMarginBottom = 2;
    dp.separators = [
      {
        color: "lightblue",
        location: new Date(),
        layer: "BelowEvents",
        width: 3,
      },
    ];

    dp.eventHeight = 32;
    dp.cellGroupBy = "Week";
    dp.scale = "Day";
    dp.cellWidthSpec = "Auto";
    dp.autoScroll = true;
    dp.dynamicLoading = true;
    dp.durationBarVisible = false;
    dp.eventMarginLeft = 0;
    dp.eventMarginRight = -1;
    dp.timeHeaders = [
      { groupBy: "Day", format: "ddd" },
      { groupBy: "Day", format: "d" },
    ];

    dp.onBeforeEventRender = (args: any) => {
      args.data.cssClass = "ScrollableWeekViewEvent";
      const eventBgColor = args.data.backColor || "orange";
      const eventStatus = args.data.statusId || 0;
      let eventContentHtml = "";
      if (eventStatus === 0) {
        eventContentHtml = '<div style="font-size:10px;padding:2px 0px;">◆</div>';
      } else if (eventStatus === 1) {
        eventContentHtml =
          '<i class="fa-solid fa-bell" aria-hidden="true"></i><span class="SchedulerEventText" style="padding:0px 2px;">' +
          (args.e.body || "") +
          " / " +
          (args.e.text || "") +
          "</span>";
      } else if (eventStatus === 2) {
        eventContentHtml =
          '<i class="fa-solid fa-bell" aria-hidden="true" style="color:gray;"></i><span class="SchedulerEventText" style="padding:0px 2px;color:gray;">' +
          (args.e.body || "") +
          " / " +
          (args.e.text || "") +
          "</span>";
      }
      args.data.html =
        '<div style="position:absolute;top:0;left:0;width:100%;height:100%;background:' +
        eventBgColor +
        ';color:white;font-size:14px;text-align:center;padding:6px 0px 6px 0px;">' +
        eventContentHtml +
        "</div>";
    };

    dp.onScroll = (args: any) => {
      const viewport = args.viewport;
      viewportRef.current = viewport;
      setViewportLabel(getSchedulerViewportLabel(viewport));

      const setupBtns = weekBtnsRef.current;
      if (setupBtns.length >= 4 && viewport?.start) {
        const vs = viewport.start;
        let idx = currentWeekIndexRef.current;
        let weekDate = currentWeekDateValueRef.current;
        if (vs >= setupBtns[0].dateValue && vs < setupBtns[1].dateValue.addDays(-2)) {
          idx = 0;
          weekDate = setupBtns[0].dateValue;
        } else if (vs >= setupBtns[1].dateValue.addDays(-2) && vs < setupBtns[2].dateValue.addDays(-2)) {
          idx = 1;
          weekDate = setupBtns[1].dateValue;
        } else if (vs >= setupBtns[2].dateValue.addDays(-2) && vs < setupBtns[3].dateValue.addDays(-2)) {
          idx = 2;
          weekDate = setupBtns[2].dateValue;
        } else if (vs >= setupBtns[3].dateValue.addDays(-2)) {
          idx = 3;
          weekDate = setupBtns[3].dateValue;
        }
        const weekStr = weekDate?.toString?.() ?? "";
        const prevStr = currentWeekDateValueRef.current?.toString?.() ?? "";
        if (weekDate && (idx !== currentWeekIndexRef.current || weekStr !== prevStr)) {
          currentWeekIndexRef.current = idx;
          currentWeekDateValueRef.current = weekDate;
          setCurrentWeekIndex(idx);
          setCurrentWeekDateValue(weekDate);
        }
      }

      trackTimeout(
        window.setTimeout(() => {
          if (!isMountedRef.current) return;
        try {
          dp.isDisableOnScrollEvent = true;
          dp.rows?.filter?.("filterByViewPortRange");
            trackTimeout(
              window.setTimeout(() => {
                if (!isMountedRef.current) return;
                dp.isDisableOnScrollEvent = false;
              }, 500),
            );
        } catch {
          /* ignore */
        }
        }, 200),
      );
    };

    dp.onRowFilter = (args: any) => {
      if (args.filter === "filterByViewPortRange") {
        const vp = viewportRef.current;
        if (!vp?.start || !vp?.end) {
          args.visible = true;
          return;
        }
        try {
          const countEventInViewPortRange = args.row.events.forRange(vp.start, vp.end).length;
          args.visible = countEventInViewPortRange > 0;
        } catch {
          args.visible = true;
        }
      }
    };

    dp.onEventClick = (args: any) => onEventClickRef.current(args);

    dp.init();
    schedulerRef.current = dp;

    return () => {
      try {
        isMountedRef.current = false;
        // Cancel any pending callbacks that might call DayPilot APIs after unmount.
        for (const id of pendingTimeoutIdsRef.current) {
          window.clearTimeout(id);
        }
        pendingTimeoutIdsRef.current = [];

        // Stop DayPilot callbacks early so internal code doesn't call into disposed state.
        dp.onScroll = undefined;
        dp.onRowFilter = undefined;
        dp.onEventClick = undefined;
        schedulerRef.current?.dispose?.();
      } catch {
        /* ignore */
      }
      schedulerRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [schedulerElId]);

  useEffect(() => {
    const dp = schedulerRef.current;
    if (!dp || typeof DayPilot === "undefined") return;

    const startRaw = searchResultDto?.StartDateTime;
    if (startRaw) {
      dp.startDate = new DayPilot.Date(startRaw, true);
    } else if (events.length && events[0].start) {
      dp.startDate = events[0].start.getDatePart().addDays(-7);
    } else {
      dp.startDate = new DayPilot.Date();
    }
    dp.days = 28;
    dp.heightSpec = "Parent100Pct";
    dp.events.list = events;
    dp.resources = resources;
    dp.update?.();

    const scrollTarget = currentWeekDateValueRef.current;
    trackTimeout(
      window.setTimeout(() => {
        if (!isMountedRef.current) return;
        if (scrollTarget && dp.scrollTo) {
          try {
            dp.scrollTo(scrollTarget);
          } catch {
            /* ignore */
          }
        }
      }, 0),
    );
  }, [events, resources, searchResultDto?.StartDateTime]);

  const goToWeekOf = (btnIndex: number) => {
    const dp = schedulerRef.current;
    const btn = weekBtns[btnIndex];
    if (!dp || !btn) return;
    currentWeekIndexRef.current = btnIndex;
    currentWeekDateValueRef.current = btn.dateValue;
    setCurrentWeekIndex(btnIndex);
    setCurrentWeekDateValue(btn.dateValue);
    try {
      dp.scrollTo(btn.dateValue);
    } catch {
      /* ignore */
    }
  };

  const weekSelectionBtnGroup_goToPrevWeek = async () => {
    const idx = currentWeekIndexRef.current;
    if (idx > 0) {
      goToWeekOf(idx - 1);
      return;
    }
    const b0 = weekBtnsRef.current[0];
    if (!b0 || typeof DayPilot === "undefined") return;
    const changeToBaseDateValue = new Date(b0.dateValue.addDays(-7).toString("yyyy-MM-dd") + " 00:00:00");
    const patch = buildSchedulerBaseDateCriteriaPatch(searchDtoRef.current, changeToBaseDateValue);
    const run = patchSearchRef.current;
    if (run && Object.keys(patch).length) {
      await run(patch);
    } else if (executeSearchRef.current) {
      await executeSearchRef.current();
    }
  };

  const weekSelectionBtnGroup_goToNextWeek = async () => {
    const idx = currentWeekIndexRef.current;
    if (idx < 3) {
      goToWeekOf(idx + 1);
      return;
    }
    const b3 = weekBtnsRef.current[3];
    if (!b3 || typeof DayPilot === "undefined") return;
    const changeToBaseDateValue = new Date(b3.dateValue.addDays(7).toString("yyyy-MM-dd") + " 00:00:00");
    const patch = buildSchedulerBaseDateCriteriaPatch(searchDtoRef.current, changeToBaseDateValue);
    const run = patchSearchRef.current;
    if (run && Object.keys(patch).length) {
      await run(patch);
    } else if (executeSearchRef.current) {
      await executeSearchRef.current();
    }
  };

  const weekBtnClass = (btnIndex: number) => {
    const selected =
      weekBtns[btnIndex] &&
      currentWeekDateValue &&
      weekBtns[btnIndex].dateValue.toString() === currentWeekDateValue.toString();
    return `SchedulerWeekBtn px-3 py-1 text-xs font-semibold rounded-full border relative pr-5 ${theme.button_default} ${
      selected ? "opacity-100 ring-1 ring-gray-400 bg-gray-400/30" : ""
    }`;
  };

  if (viewDto?.IsClusterChildView) {
    return (
      <div className="w-full h-full flex flex-col">
        <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>
          Cluster child Scheduler view is not migrated in React; showing grid.
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

  if (typeof DayPilot === "undefined" || viewDto?.ViewType !== schedulerViewType) {
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
    <div className="h-full w-full flex flex-col min-h-0 overflow-hidden SchedulerSearchView">
      <style>{`
        .SchedulerWeekBtn .TaskCountIcon {
          width: 18px;
          height: 18px;
          border-radius: 50%;
          padding-top: 0px;
          position: absolute;
          top: 2px;
          font-weight: 600;
          right: 2px;
          color: #a98181;
          background-color: #fff0f0;
          font-size: 10px;
          line-height: 18px;
          text-align: center;
        }
        .dpscheduler_rowheader_inner div {
          padding: 0px 5px;
        }
      `}</style>

      <div className={`flex flex-wrap items-center justify-center gap-1 px-2 py-2 border-b shrink-0 ${theme.mainContentSection}`}>
        <button
          type="button"
          className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
          onClick={() => void weekSelectionBtnGroup_goToPrevWeek()}
          aria-label="Previous week window"
        >
          <i className="fa-solid fa-caret-left" aria-hidden />
        </button>
        {[0, 1, 2, 3].map((i) => (
          <button
            key={`wk-${i}`}
            type="button"
            className={weekBtnClass(i)}
            onClick={() => goToWeekOf(i)}
            disabled={!weekBtns[i]}
          >
            {weekBtns[i]?.display ?? "—"}
            <span className="TaskCountIcon">{weekBtns[i]?.resourceCount ?? 0}</span>
          </button>
        ))}
        <button
          type="button"
          className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
          onClick={() => void weekSelectionBtnGroup_goToNextWeek()}
          aria-label="Next week window"
        >
          <i className="fa-solid fa-caret-right" aria-hidden />
        </button>
        <span className={`text-xs font-semibold pl-2 ${theme.label}`}>{viewportLabel}</span>
      </div>

      <div className={`w-full h-1 flex-auto min-h-[200px] min-w-0 ${theme.mainContentSection}`}>
        <div id={schedulerElId} className="w-full h-full min-h-[200px]" />
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
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => setDetailOpen(false)}
              >
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
          {rowLinks.eventMenuLinkTargets
            .filter((lt: any) => rowLinks.isLinkTargetAllowed(lt, eventMenu.dataItem))
            .map((lt: any, idx: number) => (
              <button
                key={`se-${lt?.Id ?? idx}`}
                type="button"
                className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
                onClick={() => {
                  rowLinks.executeLinkTarget(lt, eventMenu.dataItem);
                  setEventMenu((m) => ({ ...m, visible: false }));
                }}
              >
                {lt?.NavigationActionName || "Action"}
              </button>
            ))}
          {rowLinks.linkedSearchList.map((ls: any, idx: number) => (
            <button
              key={`sls-${ls?.Id ?? idx}`}
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
                rowLinks.addTabAndNavigate("masterdatamanagement", title, paramObj);
                setEventMenu((m) => ({ ...m, visible: false }));
              }}
            >
              {ls?.NavigationActionName || ls?.DisplayText || "Navigate To Search"}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};
