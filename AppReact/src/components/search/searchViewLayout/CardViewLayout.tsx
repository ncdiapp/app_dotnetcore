import React, { useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import { useTheme } from "../../../redux/hooks/useTheme";
import { fileRegularUrl } from "../../../webapi/fileEndpoints";
import { searchSvc } from "../../../webapi/searchSvc";
import { useTabNavigation } from "../../../redux/hooks/useTabNavigation";
import { preserveTabInitialPath, getCurrentActiveTab } from "../../../redux/features/ui/navigation/tabnavSlice";
import { isTransactionFormGroupPath } from "../../../helper/navigationHelper";
import { buildLinkTargetTabTitle, getDictViewColumnValue } from "../../../utils/linkTargetTabTitle";
import {
  buildFormGroupOpenPayload,
  cacheFormGroupSession,
  EmAppLinkTargetActionType,
  filterRowContextMenuFormLinkTargets,
  filterRowContextMenuLinkedSearches,
  shouldOpenAsFormGroup,
} from "../../../utils/transactionFormGroupHelper";

interface CardViewLayoutProps {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onSelectionChanged?: (selectedItems: any[]) => void;
}

const EmAppControlType = {
  DDL: 1,
  TextBox: 2,
  Memo: 4,
  Image: 5,
  Date: 7,
  File: 9,
  CheckBox: 13,
  Numeric: 20,
  Video: 25,
  DateTimeDetail: 27,
  Audio: 28,
  Time: 34,
  Rating: 46,
  YoutubeVideo: 49,
  ExternalImageUrl: 50,
};

/** Stable card image — avoids img reload when another card's checkbox selection changes. */
const SearchCardImage = React.memo(
  ({ src, alt }: { src: string; alt: string }) => (
    <img
      src={src}
      alt={alt}
      className="absolute left-0 right-0 top-0 bottom-0 m-auto max-h-[200px] max-w-[280px] object-contain"
      onError={(e) => {
        (e.currentTarget as HTMLImageElement).style.display = "none";
      }}
    />
  )
);
SearchCardImage.displayName = 'SearchCardImage';

const EmAppWebPageUiControlDisplayType = {
  TextDisplay: 1,
  Checkbox: 4,
  ImageBox: 10,
  RatingControl: 17,
  YoutubeVideo: 18,
  SplittedTextBlock: 106,
  CardViewFieldBinding: 999,
};
const getCellValue = (row: any, columnId: any) => row?.DictViewColumnIDKeyValue?.[columnId];
const getNumber = (v: any) => {
  const n = Number(v);
  return Number.isFinite(n) ? n : undefined;
};
const getColumnControlType = (column: any) => {
  return getNumber(column?.ControlType ?? column?.controlType);
};

const getColumnEntityId = (column: any) =>
  getNumber(column?.EntityId ?? column?.entityId);
const getColumnId = (column: any) => column?.Id ?? column?.id;
const getColumnName = (column: any) => column?.Name ?? column?.Display ?? column?.name ?? column?.display ?? getColumnId(column);

const formatDate = (value: any) => {
  if (!value) return "";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return String(value);
  return d.toLocaleDateString();
};

const formatDateTime = (value: any) => {
  if (!value) return "";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return String(value);
  return d.toLocaleString();
};

const formatTime = (value: any) => {
  if (!value) return "";
  const d = new Date(value);
  if (!Number.isNaN(d.getTime())) return d.toLocaleTimeString();
  return String(value);
};

const toEmbedYoutubeUrl = (value: any) => {
  const text = String(value ?? "").trim();
  if (!text) return "";
  const watchMatch = text.match(/[?&]v=([^&]+)/);
  if (watchMatch?.[1]) return `https://www.youtube.com/embed/${watchMatch[1]}`;
  const shortMatch = text.match(/youtu\.be\/([^?&]+)/);
  if (shortMatch?.[1]) return `https://www.youtube.com/embed/${shortMatch[1]}`;
  if (text.includes("/embed/")) return text;
  return text;
};

export const CardViewLayout: React.FC<CardViewLayoutProps> = ({ viewDto, viewDataList, dataModel, onSelectionChanged }) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();
  const [entityDisplayMap, setEntityDisplayMap] = useState<Record<string, Map<any, string>>>({});
  const [rowMenu, setRowMenu] = useState<{ visible: boolean; x: number; y: number; rowItem: any | null }>({
    visible: false,
    x: 0,
    y: 0,
    rowItem: null,
  });

  useEffect(() => {
    const viewId = viewDto?.Id;
    if (!viewId) {
      setEntityDisplayMap({});
      return;
    }
    let isMounted = true;
    searchSvc.retrieveViewDictEntityLookupItemDto(String(viewId))
      .then((data: any) => {
        if (!isMounted || !data || typeof data !== "object") return;
        const next: Record<string, Map<any, string>> = {};
        Object.entries(data).forEach(([key, value]) => {
          const list = Array.isArray(value) ? value : [];
          const map = new Map<any, string>();
          list.forEach((item: any) => {
            map.set(item?.Id, item?.Display ?? item?.Name ?? String(item?.Id ?? ""));
          });
          next[String(key)] = map;
        });
        setEntityDisplayMap(next);
      })
      .catch(() => setEntityDisplayMap({}));
    return () => {
      isMounted = false;
    };
  }, [viewDto?.Id]);

  useEffect(() => {
    const close = () => setRowMenu((m) => ({ ...m, visible: false }));
    document.addEventListener("click", close);
    return () => document.removeEventListener("click", close);
  }, []);

  const visibleColumns = useMemo(() => {
    const columns = Array.isArray(viewDto?.Columns) ? viewDto.Columns : [];
    return columns
      .filter((c: any) => c?.IsVisible !== false && c?.IsUserDefined3 !== true)
      .sort((a: any, b: any) => (a?.Sort ?? 0) - (b?.Sort ?? 0));
  }, [viewDto]);

  const rows = Array.isArray(viewDataList) ? viewDataList : [];
  const safeCardWidth = 350;
  const viewUiId = dataModel?.uiControl?.uiId ?? "cardview";
  const [selectedKeySet, setSelectedKeySet] = useState<Set<string>>(new Set());

  const linkTargets = useMemo(() => {
    const list = filterRowContextMenuFormLinkTargets(viewDto?.AppFormLinkTargetList);
    const menuLinkTargets = list.filter(
      (lt: any) =>
        lt?.ActionType === EmAppLinkTargetActionType.Edit ||
        lt?.ActionType === EmAppLinkTargetActionType.EditOnPopup ||
        lt?.ActionType === EmAppLinkTargetActionType.Preview ||
        lt?.ActionType === EmAppLinkTargetActionType.CreateFromExistingItem
    );
    const availableLinkToSearchList = filterRowContextMenuLinkedSearches(
      viewDto?.AppViewLinkedSeaechOrUrlDtoList?.filter((ls: any) => ls?.LinkTargetSearchId) || [],
    );
    const menuItemCount = menuLinkTargets.length + availableLinkToSearchList.length;
    const editLinkTarget =
      list
        .filter(
          (lt: any) =>
            lt?.ActionType === EmAppLinkTargetActionType.Edit ||
            lt?.ActionType === EmAppLinkTargetActionType.EditOnPopup ||
            lt?.ActionType === EmAppLinkTargetActionType.Preview
        )
        .sort((a: any, b: any) => (a?.Sort || 0) - (b?.Sort || 0))[0] || null;
    const deleteLinkTarget =
      list
        .filter((lt: any) => lt?.ActionType === EmAppLinkTargetActionType.Delete)
        .sort((a: any, b: any) => (a?.Sort || 0) - (b?.Sort || 0))[0] || null;
    return { editLinkTarget, deleteLinkTarget, menuItemCount, menuLinkTargets, availableLinkToSearchList };
  }, [viewDto?.AppFormLinkTargetList, viewDto?.AppViewLinkedSeaechOrUrlDtoList]);

  const isLinkTargetAllowed = (linkTarget: any, row: any): boolean => {
    if (!linkTarget?.SourceConditionViewColumnId) return true;
    const conditionValue = row?.DictViewColumnIDKeyValue?.[linkTarget.SourceConditionViewColumnId];
    return Boolean(conditionValue) && conditionValue !== "False" && conditionValue !== "0";
  };

  const executeLinkTarget = (linkTarget: any, row: any) => {
    if (!linkTarget || !row) return;
    if (!isLinkTargetAllowed(linkTarget, row)) return;

    if (linkTarget.LinkTargetUsageType === 5 && linkTarget.LinkTargetUrlOrRouteCode) {
      let routeCode = linkTarget.LinkTargetUrlOrRouteCode;
      if (routeCode.indexOf("main.") >= 0) routeCode = routeCode.replace("main.", "");
      const paramObj: any = {};
      if (linkTarget.SourceViewColumnId1) paramObj.id = row?.DictViewColumnIDKeyValue?.[linkTarget.SourceViewColumnId1] ?? null;
      if (linkTarget.SourceViewColumnId2 && linkTarget.TargetColumn2) paramObj.param1 = row?.DictViewColumnIDKeyValue?.[linkTarget.SourceViewColumnId2] ?? null;
      if (linkTarget.SourceViewColumnId3 && linkTarget.TargetColumn3) paramObj.param2 = row?.DictViewColumnIDKeyValue?.[linkTarget.SourceViewColumnId3] ?? null;
      const pkForTitle = getDictViewColumnValue(row?.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId1);
      const tabTitle = buildLinkTargetTabTitle(
        linkTarget.NavigationActionName || "Open",
        Boolean(linkTarget.RowDisplayViewColumnId),
        linkTarget.RowDisplayViewColumnId
          ? getDictViewColumnValue(row?.DictViewColumnIDKeyValue, linkTarget.RowDisplayViewColumnId)
          : undefined,
        pkForTitle ?? undefined
      );
      addTabAndNavigate(routeCode, tabTitle, paramObj);
      return;
    }

    if (shouldOpenAsFormGroup(linkTarget, viewDto)) {
      const dictDefaults = dataModel?.dictViewColumnIdAndValue_For_LinkTargetParameterDefaultValue || {};
      const { tabTitle, sessionKey, param2, sessionData } = buildFormGroupOpenPayload(
        linkTarget,
        row,
        viewDto,
        viewDataList,
        dictDefaults,
      );
      cacheFormGroupSession(dispatch, sessionKey, sessionData);
      const activeTab = getCurrentActiveTab();
      if (activeTab?.tabKey && activeTab.path && !isTransactionFormGroupPath(activeTab.path)) {
        dispatch(preserveTabInitialPath({ tabKey: activeTab.tabKey, path: activeTab.path }));
      }
      addTabAndNavigate('TransactionFormGroup', tabTitle, {
        id: sessionKey,
        preserveLinkTabTitle: true,
        param2,
      });
      return;
    }

    if (linkTarget.LinkTargetTransactionId) {
      const paramObj: any = { id: linkTarget.LinkTargetTransactionId, preserveLinkTabTitle: true };
      if (linkTarget.SourceViewColumnId1) {
        paramObj.param1 = getDictViewColumnValue(row?.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId1) ?? null;
      }
      if (linkTarget.SourceViewColumnId2 && linkTarget.TargetColumn2) {
        paramObj.param2 = getDictViewColumnValue(row?.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId2) ?? null;
      }
      const pkForTitle = getDictViewColumnValue(row?.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId1);
      const tabTitle = buildLinkTargetTabTitle(
        linkTarget.NavigationActionName || "Open",
        Boolean(linkTarget.RowDisplayViewColumnId),
        linkTarget.RowDisplayViewColumnId
          ? getDictViewColumnValue(row?.DictViewColumnIDKeyValue, linkTarget.RowDisplayViewColumnId)
          : undefined,
        pkForTitle ?? undefined
      );
      addTabAndNavigate("FormMasterDetail", tabTitle, paramObj);
    }
  };

  const executeLinkedSearchMenu = (linkedSearchMenu: any, row: any) => {
    const title = linkedSearchMenu?.NavigationActionName || linkedSearchMenu?.Name || "Navigate To Search";
    const paramObj: any = {
      searchId: linkedSearchMenu?.LinkTargetSearchId,
      isSavedSearch: false,
    };
    if (linkedSearchMenu?.LinkTargetSearchViewId) {
      paramObj.initialViewId = linkedSearchMenu.LinkTargetSearchViewId;
    }
    if (row?.Id != null) {
      paramObj.linkedSourceRowId = row.Id;
    }
    addTabAndNavigate("masterdatamanagement", title, paramObj);
  };

  const getRowKey = (row: any, index: number) =>
    String(row?.Id ?? row?.id ?? row?.RID ?? row?.RId ?? row?.Guid ?? row?.guid ?? index);

  useEffect(() => {
    setSelectedKeySet(new Set());
  }, [viewDataList]);

  const toggleRowSelected = (row: any, rowIndex: number, checked: boolean) => {
    const key = getRowKey(row, rowIndex);
    setSelectedKeySet((prev) => {
      const next = new Set(prev);
      if (checked) next.add(key);
      else next.delete(key);
      if (onSelectionChanged) {
        const selected = rows.filter((r: any, i: number) => next.has(getRowKey(r, i)));
        onSelectionChanged(selected);
      }
      return next;
    });
  };

  const renderFieldValue = (row: any, column: any) => {
    const value = getCellValue(row, getColumnId(column));
    
    const controlType = getColumnControlType(column);
  

    if (controlType === EmAppControlType.DDL && getColumnEntityId(column) != null) {
      const map = entityDisplayMap[String(getColumnEntityId(column))];
      const text = map?.get(value) ?? (value == null ? "" : String(value));
      return <div className={`w-full py-1 text-xs ${theme.label}`}>{text}</div>;
    }

    if (controlType === EmAppControlType.CheckBox) {
     
      return <div className={`w-full py-1 text-xs whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}>{value ? "Yes" : "No"}</div>;
    }

    if (controlType === EmAppControlType.Numeric || controlType === EmAppControlType.Rating) {
      
      return <div className={`w-full py-1 text-xs whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}>{value == null ? "" : String(value)}</div>;
    }

    if (controlType === EmAppControlType.Date) return <div className={`w-full py-1 text-xs whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}>{formatDate(value)}</div>;
    if (controlType === EmAppControlType.DateTimeDetail) return <div className={`w-full py-1 text-xs whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}>{formatDateTime(value)}</div>;
    if (controlType === EmAppControlType.Time) return <div className={`w-full py-1 text-xs whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}>{formatTime(value)}</div>;

    if (controlType === EmAppControlType.Image || controlType === EmAppControlType.ExternalImageUrl) {
     
        const imageUrl = controlType === EmAppControlType.ExternalImageUrl
          ? String(value ?? "")
          : (value ? fileRegularUrl(value) : "");
        return (
          <div className="w-full h-[202px] relative">
            {imageUrl ? (
              <SearchCardImage src={imageUrl} alt={String(getColumnName(column) || "image")} />
            ) : <div className="w-full h-full" />}
          </div>
        );
      
      return <div className={`w-full py-1 text-xs whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}>{value == null ? "" : String(value)}</div>;
    }

    if (controlType === EmAppControlType.YoutubeVideo) {
      
        const embedUrl = toEmbedYoutubeUrl(value);
        return (
          <div className="w-full h-[210px] relative py-[5px]">
            {embedUrl ? (
              <iframe
                className="w-full h-full max-h-full m-auto border border-solid border-[#e8e8e8]"
                src={embedUrl}
                allow="fullscreen"
                title={column?.Name || "Youtube"}
              />
            ) : null}
          </div>
        );
      
      return <div className={`w-full py-1 text-xs whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}>{value == null ? "" : String(value)}</div>;
    }

    if (controlType === EmAppControlType.File || controlType === EmAppControlType.Video || controlType === EmAppControlType.Audio) {
      const sketch = row?.DictSketchOrFileDisplayCode?.[getColumnId(column)] ?? value;
      return (
        <a
          href="#"
          onClick={(e) => e.preventDefault()}
          className={`w-full py-1 text-xs block whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}
        >
          {sketch == null ? "" : String(sketch)}
        </a>
      );
    }

    

    return <div className={`w-full py-1 text-xs whitespace-nowrap overflow-hidden text-ellipsis ${theme.label}`}>{value == null ? "" : String(value)}</div>;
  };

  const renderField = (row: any, column: any) => {
    const showLabel = (column?.IsNeedToShowCardFieldLabel ?? column?.isNeedToShowCardFieldLabel) !== false
      && (column?.IsUserDefined4 ?? column?.isUserDefined4) !== true;
    
  
    const controlType = getColumnControlType(column);
    const isImageControl = controlType === EmAppControlType.Image || controlType === EmAppControlType.ExternalImageUrl;

    if (isImageControl) {
      return (
        <div key={column?.Id} className="w-full">
          {showLabel ? (
            <div className="gridFormControlUnit">
              <label className={`w-full text-xs ${theme.label}`}>{getColumnName(column)}</label>
            </div>
          ) : null}
          <div className="w-full">{renderFieldValue(row, column)}</div>
        </div>
      );
    }

    return (
      <div key={column?.Id} className="w-full gridFormControlUnit flex" title="">
        {showLabel ? (
          <div className="w-[125px]">
            <label className={`text-xs ${theme.label}`}>{getColumnName(column)}</label>
          </div>
        ) : null}
        <div className={`${showLabel ? "w-full" : "w-full"} whitespace-nowrap overflow-hidden text-ellipsis`}>
          {renderFieldValue(row, column)}
        </div>
      </div>
    );
  };

  return (
    <div className={`w-full h-full overflow-hidden ${theme.mainContentSection}`}>
      <div className="w-full h-full flex-auto overflow-auto overflow-x-hidden flex flex-wrap content-start">
        {rows.length === 0 ? (
          <div className={`text-xs ${theme.label}`}></div>
        ) : (
          <div className="w-full flex flex-wrap content-start pl-2">
            {rows.map((row: any, rowIndex: number) => (
              <div
                key={getRowKey(row, rowIndex)}
                className="flex-none max-w-full p-[5px]"
                style={{ width: `${safeCardWidth}px`, height: "auto" }}
              >
                <div
                  className={`w-full h-auto border border-solid border-[#e8e8e8] overflow-hidden p-[10px] min-h-[100px] ${theme.mainContentSection}`}
                >
                  <div className="w-full h-6 relative">
                    <input
                      type="checkbox"
                      checked={selectedKeySet.has(getRowKey(row, rowIndex))}
                      onChange={(e) => toggleRowSelected(row, rowIndex, e.target.checked)}
                      aria-label="Select card row"
                      className="absolute top-[2px] left-0"
                    />
                    <div className="absolute top-0 right-0 flex items-center">
                      {linkTargets.editLinkTarget && isLinkTargetAllowed(linkTargets.editLinkTarget, row) && (
                        <button
                          type="button"
                          className="btn-GridRowHeaderButton"
                          title="Open"
                          onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            executeLinkTarget(linkTargets.editLinkTarget, row);
                          }}
                          style={{
                            width: "28px",
                            height: "28px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            padding: 0,
                            border: "none",
                            background: "transparent",
                            cursor: "pointer",
                          }}
                        >
                          <i className="fa fa-pencil" aria-hidden="true" style={{ fontSize: "12px" }} />
                        </button>
                      )}
                      {linkTargets.deleteLinkTarget && linkTargets.menuItemCount === 1 && isLinkTargetAllowed(linkTargets.deleteLinkTarget, row) ? (
                        <button
                          type="button"
                          className="btn-GridRowHeaderButton"
                          title="Delete"
                          onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            executeLinkTarget(linkTargets.deleteLinkTarget, row);
                          }}
                          style={{
                            width: "28px",
                            height: "28px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            padding: 0,
                            border: "none",
                            background: "transparent",
                            cursor: "pointer",
                          }}
                        >
                          <i className="fa fa-trash-o" aria-hidden="true" style={{ fontSize: "12px" }} />
                        </button>
                      ) : null}
                      {(linkTargets.menuItemCount > 1 || (linkTargets.editLinkTarget && linkTargets.deleteLinkTarget && linkTargets.menuItemCount === 1)) && (
                        <button
                          type="button"
                          className="btn-GridRowHeaderButton"
                          title="More Options"
                          style={{
                            width: "30px",
                            height: "28px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            padding: 0,
                            border: "none",
                            background: "transparent",
                            cursor: "pointer",
                          }}
                          onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            setRowMenu({
                              visible: true,
                              x: e.clientX,
                              y: e.clientY,
                              rowItem: row,
                            });
                          }}
                        >
                          <i className="fa fa-navicon" aria-hidden="true" style={{ fontSize: "12px" }} />
                        </button>
                      )}
                    </div>
                  </div>
                  {visibleColumns.map((column: any) => renderField(row, column))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
      {rowMenu.visible && rowMenu.rowItem && (
        <div
          className={`fixed z-[1200] min-w-[220px] border rounded shadow-lg ${theme.mainContentSection}`}
          style={{ left: rowMenu.x, top: rowMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {linkTargets.menuLinkTargets
            .filter((lt: any) => isLinkTargetAllowed(lt, rowMenu.rowItem))
            .map((lt: any, idx: number) => (
              <button
                key={`lt-${lt?.Id ?? idx}`}
                type="button"
                className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
                onClick={() => {
                  executeLinkTarget(lt, rowMenu.rowItem);
                  setRowMenu((m) => ({ ...m, visible: false }));
                }}
              >
                {lt?.NavigationActionName || "Menu"}
              </button>
            ))}
          {linkTargets.availableLinkToSearchList
            .filter((ls: any) => Boolean(ls?.LinkTargetSearchId))
            .map((ls: any, idx: number) => (
              <button
                key={`ls-${ls?.Id ?? idx}`}
                type="button"
                className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
                onClick={() => {
                  executeLinkedSearchMenu(ls, rowMenu.rowItem);
                  setRowMenu((m) => ({ ...m, visible: false }));
                }}
              >
                {ls?.NavigationActionName || "Navigate To Search"}
              </button>
            ))}
        </div>
      )}
    </div>
  );
};

