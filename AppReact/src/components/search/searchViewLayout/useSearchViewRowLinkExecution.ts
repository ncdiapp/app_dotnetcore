import { useCallback, useMemo } from "react";
import { useDispatch } from "react-redux";
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

const EmAppLinkTargetUsageType = {
  SearchViewLinkSystemDefinedPage: 5,
};

/** Row context actions for DayPilot event/task clicks (parity with CalendarViewLayout). */
export function useSearchViewRowLinkExecution(viewDto: any, viewDataList: any[] = [], dataModel?: any) {
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();

  const eventMenuLinkTargets = useMemo(() => {
    const list = filterRowContextMenuFormLinkTargets(viewDto?.AppFormLinkTargetList);
    return list.filter(
      (lt: any) =>
        lt?.ActionType === EmAppLinkTargetActionType.Edit ||
        lt?.ActionType === EmAppLinkTargetActionType.EditOnPopup ||
        lt?.ActionType === EmAppLinkTargetActionType.Preview ||
        lt?.ActionType === EmAppLinkTargetActionType.CreateFromExistingItem,
    );
  }, [viewDto?.AppFormLinkTargetList]);

  const linkedSearchList = useMemo(() => {
    const raw = viewDto?.AppViewLinkedSeaechOrUrlDtoList?.filter((ls: any) => ls?.LinkTargetSearchId) || [];
    return filterRowContextMenuLinkedSearches(raw);
  }, [viewDto?.AppViewLinkedSeaechOrUrlDtoList]);

  const hasFormLinkTargets = Array.isArray(viewDto?.AppFormLinkTargetList) && viewDto.AppFormLinkTargetList.length > 0;

  const isLinkTargetAllowed = useCallback((linkTarget: any, row: any): boolean => {
    if (!linkTarget?.SourceConditionViewColumnId) return true;
    const v = row?.DictViewColumnIDKeyValue?.[linkTarget.SourceConditionViewColumnId];
    return Boolean(v && v !== "False" && v !== "0");
  }, []);

  const executeLinkTarget = useCallback(
    (linkTarget: any, row: any) => {
      if (!linkTarget || !row) return;
      if (!isLinkTargetAllowed(linkTarget, row)) {
        window.alert(`${linkTarget.NavigationActionName || "Action"} is not available for current row.`);
        return;
      }

      if (
        linkTarget.LinkTargetUsageType === EmAppLinkTargetUsageType.SearchViewLinkSystemDefinedPage &&
        linkTarget.LinkTargetUrlOrRouteCode
      ) {
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

      const useFormGroup =
        dataModel?.forceTransactionFormGroup || shouldOpenAsFormGroup(linkTarget, viewDto);
      if (useFormGroup) {
        const dictDefaults = dataModel?.dictViewColumnIdAndValue_For_LinkTargetParameterDefaultValue || {};
        const effectiveLinkTarget =
          linkTarget ??
          (dataModel?.forceTransactionFormGroup
            ? viewDto?.AppFormLinkTargetList?.find(
                (lt: any) => lt?.ActionType === EmAppLinkTargetActionType.Edit,
              )
            : null);
        if (!effectiveLinkTarget) {
          window.alert('No form group link target is configured for this view.');
          return;
        }
        const { tabTitle, sessionKey, param2, sessionData } = buildFormGroupOpenPayload(
          effectiveLinkTarget,
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
        const param2Payload: Record<string, unknown> = {};
        if (linkTarget.LinkTargetTransactionGroupId) {
          param2Payload.transGroupId = linkTarget.LinkTargetTransactionGroupId;
          param2Payload.LinkTargetTransactionGroupId = linkTarget.LinkTargetTransactionGroupId;
        }
        if (param1) param2Payload.legacyParam1 = param1;
        if (Object.keys(param2Payload).length > 0) {
          paramObj.param2 = param2Payload;
        }
        addTabAndNavigate("FormMasterDetail", tabTitle, paramObj);
      }
    },
    [addTabAndNavigate, dispatch, dataModel, isLinkTargetAllowed, viewDataList, viewDto],
  );

  return {
    eventMenuLinkTargets,
    linkedSearchList,
    hasFormLinkTargets,
    isLinkTargetAllowed,
    executeLinkTarget,
    addTabAndNavigate,
  };
}
