import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { GridViewLayout } from "./GridViewLayout";
import { useTheme } from "../../../redux/hooks/useTheme";
import { useEnumValues } from "../../../hooks/useEnumDictionary";
import {
  applyGanttScaleToControl,
  buildGanttTaskList,
} from "./searchGanttSchedulerHelper";
import { useSearchViewRowLinkExecution } from "./useSearchViewRowLinkExecution";

declare const DayPilot: any;

const EmAppViewTypeGanttView = 15;

interface GanttViewLayoutProps {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
}

export const GanttViewLayout: React.FC<GanttViewLayoutProps> = ({
  viewDto,
  viewDataList,
  dataModel,
  onExecuteSearch,
}) => {
  const { theme } = useTheme();
  const emAppViewType = useEnumValues("EmAppViewType");
  const ganttViewType = emAppViewType?.GanttView ?? EmAppViewTypeGanttView;

  const uiId = dataModel?.uiControl?.uiId ?? "gantt_default";
  const ganttElId = `ganttViewControl_${uiId}`;

  const ganttRef = useRef<any>(null);
  const leftSideWidthRef = useRef(200);
  const currentScaleIndexRef = useRef(3);
  const onTaskClickedRef = useRef<(args: any) => void>(() => {});

  const [currentScaleIndex, setCurrentScaleIndex] = useState(3);
  const [detailOpen, setDetailOpen] = useState(false);
  const [currentEvent, setCurrentEvent] = useState<any>(null);
  const [eventMenu, setEventMenu] = useState<{ visible: boolean; x: number; y: number; dataItem: any | null }>({
    visible: false,
    x: 0,
    y: 0,
    dataItem: null,
  });

  const rowLinks = useSearchViewRowLinkExecution(viewDto, viewDataList, dataModel);

  const { tasks, minDate, maxDate } = useMemo(() => buildGanttTaskList(viewDataList), [viewDataList]);

  useEffect(() => {
    const close = () => setEventMenu((m) => ({ ...m, visible: false }));
    document.addEventListener("click", close);
    return () => document.removeEventListener("click", close);
  }, []);

  const onTaskClickedHandler = useCallback(
    (args: any) => {
      const task = args?.task;
      const data = task?.data;
      if (!data) return;
      setCurrentEvent(data);
      if (rowLinks.hasFormLinkTargets) {
        const ce = args?.originalEvent;
        if (ce?.clientX != null) {
          setEventMenu({ visible: true, x: ce.clientX, y: ce.clientY, dataItem: data.DataItem });
        }
      } else {
        setDetailOpen(true);
      }
    },
    [rowLinks.hasFormLinkTargets],
  );

  useEffect(() => {
    onTaskClickedRef.current = onTaskClickedHandler;
  }, [onTaskClickedHandler]);

  useEffect(() => {
    currentScaleIndexRef.current = currentScaleIndex;
  }, [currentScaleIndex]);

  useEffect(() => {
    if (typeof DayPilot === "undefined") return;
    const host = document.getElementById(ganttElId);
    if (!host || ganttRef.current) return;

    const dp = new DayPilot.Gantt(ganttElId);
    dp.rowHeaderScrolling = true;
    dp.rowHeaderWidth = leftSideWidthRef.current;
    dp.cellDuration = 1440;
    dp.heightSpec = "Parent100Pct";
    dp.theme = "dpgantt";
    dp.taskHeight = 18;
    dp.columns = [
      { title: "Name", width: 200, property: "text" },
      { title: "Plan Start", width: 78, property: "datePlannedStartStr" },
      { title: "Plan End", width: 78, property: "datePlannedEndStr" },
      { title: "Actual Start", width: 80, property: "dateActualStartStr" },
      { title: "Actual End", width: 80, property: "dateActualEndStr" },
      { title: "%Done", width: 60, property: "completeDisplay" },
      { title: "Status", width: 70, property: "status" },
      { title: "Stage", width: 70, property: "stage" },
    ];
    dp.separators = [
      {
        color: "lightblue",
        location: new Date(),
        layer: "BelowEvents",
        width: 3,
      },
    ];
    dp.taskVersionsEnabled = true;
    dp.taskVersionHeight = 8;
    dp.taskClickHandling = "Enabled";
    dp.onTaskClicked = (args: any) => onTaskClickedRef.current(args);
    dp.tasks.list = [];
    dp.links.list = [];
    applyGanttScaleToControl(dp, currentScaleIndexRef.current);
    dp.init();
    ganttRef.current = dp;

    return () => {
      try {
        ganttRef.current?.dispose?.();
      } catch {
        /* ignore */
      }
      ganttRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- mount once per gantt element id
  }, [ganttElId]);

  useEffect(() => {
    const dp = ganttRef.current;
    if (!dp || typeof DayPilot === "undefined") return;

    applyGanttScaleToControl(dp, currentScaleIndex);
    dp.update?.();
  }, [currentScaleIndex]);

  useEffect(() => {
    const dp = ganttRef.current;
    if (!dp || typeof DayPilot === "undefined") return;

    dp.tasks.list = tasks;
    dp.links.list = [];

    if (minDate && maxDate) {
      let start = minDate.addDays(-1);
      const timeDiff = Math.abs(maxDate.ticks - minDate.ticks);
      const diffDays = Math.ceil(timeDiff / (1000 * 3600 * 24)) || 31;
      let days = diffDays + 3;
      if (dataModel?.isAutoFit) {
        start = new DayPilot.Date(start);
        days = null as any;
      }
      dp.startDate = start;
      dp.days = days;
    }

    dp.update?.();
  }, [tasks, minDate, maxDate, dataModel?.isAutoFit]);

  const zoomIn = () => {
    setCurrentScaleIndex((i) => (i > 1 ? i - 1 : i));
  };

  const zoomOut = () => {
    setCurrentScaleIndex((i) => (i < 7 ? i + 1 : i));
  };

  const changeScale = (scaleIndex: number) => {
    setCurrentScaleIndex(scaleIndex);
  };

  const scaleBtnClass = (scaleIndex: number) =>
    `px-2 py-0.5 text-xs rounded-[4px] ${theme.button_default} ${
      currentScaleIndex === scaleIndex ? "ring-1 ring-offset-1 ring-blue-500" : ""
    }`;

  if (viewDto?.IsClusterChildView) {
    return (
      <div className="w-full h-full flex flex-col">
        <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>
          Cluster child Gantt view is not migrated in React; showing grid.
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

  if (typeof DayPilot === "undefined" || viewDto?.ViewType !== ganttViewType) {
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
    <div className="h-full w-full flex flex-col min-h-0 overflow-hidden">
      <div className={`flex flex-wrap items-center gap-2 px-2 py-1 border-b shrink-0 ${theme.mainContentSection}`}>
        <div className="flex items-center gap-1 flex-wrap">
          <span className={`text-xs ${theme.label}`}>Scale By:</span>
          <button type="button" className={scaleBtnClass(3)} onClick={() => changeScale(3)} title="Scale By Week">
            Week
          </button>
          <button type="button" className={scaleBtnClass(4)} onClick={() => changeScale(4)} title="Scale By Month">
            Month
          </button>
          <button type="button" className={scaleBtnClass(6)} onClick={() => changeScale(6)} title="Scale By Year">
            Year
          </button>
        </div>
        <div className="flex items-center gap-1 flex-wrap">
          <span className={`text-xs ${theme.label}`}>Zoom:</span>
          <button type="button" className={`px-2 py-0.5 text-xs rounded-[4px] ${theme.button_default}`} onClick={zoomOut} title="Zoom -">
            <i className="fa-solid fa-magnifying-glass-minus" aria-hidden />
          </button>
          <button type="button" className={`px-2 py-0.5 text-xs rounded-[4px] ${theme.button_default}`} onClick={zoomIn} title="Zoom +">
            <i className="fa-solid fa-magnifying-glass-plus" aria-hidden />
          </button>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto min-h-[200px] min-w-0 ${theme.mainContentSection}`}>
        <div id={ganttElId} className="w-full h-full min-h-[200px]" />
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
                <span className="font-semibold">% Done: </span>
                {currentEvent.completeDisplay}
              </div>
              <div>
                <span className="font-semibold">Status: </span>
                {currentEvent.status}
              </div>
              <div>
                <span className="font-semibold">Stage: </span>
                {currentEvent.stage}
              </div>
              <div>
                <span className="font-semibold">Plan Start: </span>
                {currentEvent.start?.toString?.("yyyy-MM-dd HH:mm:ss")}
              </div>
              <div>
                <span className="font-semibold">Plan End: </span>
                {currentEvent.end?.toString?.("yyyy-MM-dd HH:mm:ss")}
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
                key={`ge-${lt?.Id ?? idx}`}
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
              key={`gls-${ls?.Id ?? idx}`}
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
