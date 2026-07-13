import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams, useLocation } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabDataAutoCache } from '../../redux/hooks/useTabNavigation';
import { updateCurrentTabLabel, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { tabRoutePathsMatch } from '../../helper/navigationHelper';
import FormMasterDetail, { type TemplateHeaderEmbeddedForm } from '../formMgt/FormMasterDetail';
import AppSearch from '../search/AppSearch';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import {
  buildEmbeddedFormParam2,
  buildLinkedSearchCriteriaDict,
  buildLinkTargetsFromBusinessTemplateGroup,
  buildTemplateItemLists,
  EmAppLinkTargetActionType,
  EmAppTemplateHeaderVisibility,
  findLinkTargetInList,
  isCreateLikeLinkTarget,
  parseRouteParam2,
  preferFormGroupMainItemLinkTarget,
  cacheFormGroupSession,
  repairFormGroupSession,
  resolveFormGroupSession,
  viewHasTemplateTypedFormGroupItems,
  type TransactionFormGroupSessionData,
} from '../../utils/transactionFormGroupHelper';
import { adaptLinkTargetForFolderNavigationCreate } from '../../utils/folderNavigationHelper';
import { appTransactionService } from '../../webapi/apptransactionsvc';

type NavigationObj = {
  currentIndex: number;
  itemCount: number;
  indexDisplay: string;
  isAllowNavFirst: boolean;
  isAllowNavPrev: boolean;
  isAllowNavNext: boolean;
  isAllowNavLast: boolean;
};

type EmbeddedFormConfig = {
  key: string;
  transactionId: number;
  rootPrimaryKeyValue: string | number | null;
  param2: Record<string, unknown>;
  isHeader: boolean;
  label: string;
};

type EmbeddedSearchConfig = {
  key: string;
  paramObj: Record<string, unknown>;
  isHeader: boolean;
  label: string;
};

const TransactionFormGroup: React.FC = () => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError } = useErrorMessage();
  const { param } = useParams<{ param: string }>();
  const location = useLocation();

  const [paramObj, setParamObj] = useState<any>(() => {
    if (!param) return {};
    try {
      return JSON.parse(decodeURIComponent(param));
    } catch {
      return {};
    }
  });

  useEffect(() => {
    if (!param) {
      setParamObj({});
      return;
    }
    try {
      setParamObj(JSON.parse(decodeURIComponent(param)));
    } catch (error) {
      showError('Invalid URL parameters. ' + error);
      setParamObj({});
    }
  }, [param, showError]);

  const param2Obj = useMemo(() => parseRouteParam2(paramObj), [paramObj]);

  const sessionKey = param2Obj.formGroupSessionKey || paramObj.id || '';
  const [sessionData, setSessionData] = useState<TransactionFormGroupSessionData | null>(null);
  const [sessionResolved, setSessionResolved] = useState(false);
  const [businessTemplateReady, setBusinessTemplateReady] = useState(false);
  const initializedRef = useRef(false);
  const businessTemplateExpandAttemptedRef = useRef<number | null>(null);

  useEffect(() => {
    initializedRef.current = false;
    businessTemplateExpandAttemptedRef.current = null;
    setBusinessTemplateReady(false);
    if (!sessionKey) {
      setSessionData(null);
      setSessionResolved(true);
      setBusinessTemplateReady(true);
      return;
    }

    let data = resolveFormGroupSession(sessionKey);
    if (!data && param2Obj.viewDto && param2Obj.selecedDataRow && param2Obj.linkTargetDto) {
      data = {
        viewDto: param2Obj.viewDto,
        selecedDataRow: param2Obj.selecedDataRow,
        linkTargetDto: param2Obj.linkTargetDto,
        searchResultRowList: param2Obj.searchResultRowList || [],
      };
    }
    if (data) {
      data = repairFormGroupSession(data, param2Obj);
      cacheFormGroupSession(dispatch, sessionKey, data);
      setSessionData(data);
    } else {
      setSessionData(null);
    }
    setSessionResolved(true);
  }, [dispatch, param2Obj, sessionKey]);

  // When Open carries LinkTargetTransactionGroupId (Business Template), expand group items into MainItems.
  useEffect(() => {
    let cancelled = false;

    const expandBusinessTemplate = async () => {
      if (!sessionResolved) return;
      if (!sessionData?.linkTargetDto) {
        setBusinessTemplateReady(true);
        return;
      }

      const groupId = Number(sessionData.linkTargetDto.LinkTargetTransactionGroupId);
      if (!groupId || Number.isNaN(groupId)) {
        setBusinessTemplateReady(true);
        return;
      }

      // Already expanded for this group (or Data Model Template already has typed items).
      if (viewHasTemplateTypedFormGroupItems(sessionData.viewDto)) {
        setBusinessTemplateReady(true);
        return;
      }

      // Strict Mode / remount: allow retry until a successful expand for this sessionKey+groupId.
      if (businessTemplateExpandAttemptedRef.current === groupId) {
        setBusinessTemplateReady(true);
        return;
      }

      try {
        const groupDto = await appTransactionService.retrieveOneAppBusinessTemplateGroupExDto(groupId);
        if (cancelled) return;
        const synthetic = buildLinkTargetsFromBusinessTemplateGroup(groupDto, sessionData.linkTargetDto);
        if (synthetic.length === 0) {
          console.warn(
            'Business template group returned no expandable transactions',
            groupId,
            groupDto,
          );
          // Mark attempted only after response so Strict Mode remount can retry once if first aborted.
          businessTemplateExpandAttemptedRef.current = groupId;
          setBusinessTemplateReady(true);
          return;
        }

        const preferred = preferFormGroupMainItemLinkTarget(synthetic, sessionData.linkTargetDto);

        const next: TransactionFormGroupSessionData = {
          ...sessionData,
          viewDto: {
            ...sessionData.viewDto,
            AppFormLinkTargetList: synthetic,
            FormGroupLinkTargetList: synthetic,
          },
          linkTargetDto: preferred,
        };
        businessTemplateExpandAttemptedRef.current = groupId;
        cacheFormGroupSession(dispatch, sessionKey, next);
        setSessionData(next);
      } catch (err) {
        console.error('Failed to expand business template group', err);
        showError('Failed to load transaction form group: ' + (err as Error).message);
        businessTemplateExpandAttemptedRef.current = groupId;
      } finally {
        if (!cancelled) setBusinessTemplateReady(true);
      }
    };

    expandBusinessTemplate();
    return () => {
      cancelled = true;
    };
  }, [dispatch, sessionData, sessionKey, sessionResolved, showError]);

  const { linkTargetList, templateHeaderList } = useMemo(() => {
    if (!sessionData || !businessTemplateReady) return { linkTargetList: [], templateHeaderList: [] };
    return buildTemplateItemLists(sessionData.viewDto, sessionData.linkTargetDto);
  }, [businessTemplateReady, sessionData]);

  const [selectedLinkTarget, setSelectedLinkTarget] = useState<any>(null);
  const [selecedDataRow, setSelecedDataRow] = useState<any>(null);
  const [navigationObj, setNavigationObj] = useState<NavigationObj | null>(null);
  const [isHideLeftMenu, setIsHideLeftMenu] = useState(false);
  const [headerForms, setHeaderForms] = useState<EmbeddedFormConfig[]>([]);
  const [headerVisibility, setHeaderVisibility] = useState<number>(EmAppTemplateHeaderVisibility.Show);
  const [headerSearches, setHeaderSearches] = useState<EmbeddedSearchConfig[]>([]);
  const [mainForm, setMainForm] = useState<EmbeddedFormConfig | null>(null);
  const [mainSearch, setMainSearch] = useState<EmbeddedSearchConfig | null>(null);
  const [createMainForms, setCreateMainForms] = useState<EmbeddedFormConfig[]>([]);
  const [createMainSearches, setCreateMainSearches] = useState<EmbeddedSearchConfig[]>([]);

  const isFormGroupCreateSession = useMemo(
    () => isCreateLikeLinkTarget(sessionData?.linkTargetDto),
    [sessionData?.linkTargetDto],
  );

  const navLinkTargetList = useMemo(
    () => (isFormGroupCreateSession ? [] : linkTargetList),
    [isFormGroupCreateSession, linkTargetList],
  );

  const [dataModel, setDataModel] = useState<any>({
    linkTargetList,
    templateHeaderList,
    selectedLinkTarget,
    selecedDataRow,
    navigationObj,
    isHideLeftMenu,
  });

  // Session payload (viewDto + row) is stored under formGroupSessionKey by Search — do not overwrite via tab cache.
  useTabDataAutoCache(null);

  useEffect(() => {
    setDataModel((prev: any) => ({
      ...prev,
      linkTargetList,
      templateHeaderList,
      selectedLinkTarget,
      selecedDataRow,
      navigationObj,
      isHideLeftMenu,
    }));
  }, [linkTargetList, templateHeaderList, selectedLinkTarget, selecedDataRow, navigationObj, isHideLeftMenu]);

  useEffect(() => {
    if (!param2Obj.tabTitle) return;
    const activeTab = getCurrentActiveTab();
    if (activeTab && tabRoutePathsMatch(activeTab.path, location.pathname)) {
      dispatch(updateCurrentTabLabel(param2Obj.tabTitle));
    }
  }, [dispatch, param2Obj.tabTitle, location.pathname]);

  const buildNavigationObj = useCallback((row: any, rowList: any[]): NavigationObj | null => {
    if (!Array.isArray(rowList) || rowList.length <= 1) return null;
    const currentIndex = rowList.indexOf(row);
    if (currentIndex < 0) return null;
    return {
      currentIndex,
      itemCount: rowList.length,
      indexDisplay: `${currentIndex + 1}/${rowList.length}`,
      isAllowNavFirst: currentIndex > 0,
      isAllowNavPrev: currentIndex > 0,
      isAllowNavNext: currentIndex < rowList.length - 1,
      isAllowNavLast: currentIndex < rowList.length - 1,
    };
  }, []);

  const buildFormEmbedded = useCallback(
    (linkTarget: any, row: any, isHeader: boolean, headerVisibility?: number | null): EmbeddedFormConfig | null => {
      const built = buildEmbeddedFormParam2(linkTarget, row, {
        isHeader,
        openFrom: param2Obj.openFrom ?? null,
        headerVisibility,
      });
      if (!built) return null;

      return {
        key: `${isHeader ? 'hdr' : 'main'}_form_${linkTarget.Id}_${built.rootPrimaryKeyValue}`,
        transactionId: built.transactionId!,
        rootPrimaryKeyValue: built.rootPrimaryKeyValue,
        param2: built.param2,
        isHeader,
        label: linkTarget.display || linkTarget.NavigationActionName || '',
      };
    },
    [param2Obj.openFrom],
  );

  const buildSearchEmbedded = useCallback(
    (linkTarget: any, row: any, isHeader: boolean): EmbeddedSearchConfig | null => {
      if (!linkTarget.LinkTargetSearchId) return null;
      const dictCreteriaIdValue = buildLinkedSearchCriteriaDict(linkTarget, row);
      return {
        key: `${isHeader ? 'hdr' : 'main'}_search_${linkTarget.Id}_${row?.Id ?? ''}`,
        paramObj: {
          searchId: linkTarget.LinkTargetSearchId,
          isSavedSearch: false,
          initialViewId: linkTarget.LinkTargetSearchViewId ?? null,
          isShowCriterias: false,
          dictCreteriaIdValue,
          linkedSourceRowId: row?.Id ?? null,
        },
        isHeader,
        label: linkTarget.display || linkTarget.DisplayText || linkTarget.NavigationActionName || '',
      };
    },
    [],
  );

  const adaptTemplateItemForCreate = useCallback(
    (linkTarget: any) => {
      if (!linkTarget) return linkTarget;
      if (param2Obj.openFrom === 'folderNavigation' && sessionData?.viewDto) {
        return adaptLinkTargetForFolderNavigationCreate(linkTarget, sessionData.viewDto);
      }
      if (isCreateLikeLinkTarget(linkTarget)) return { ...linkTarget };
      return {
        ...linkTarget,
        ActionType: EmAppLinkTargetActionType.Create,
        NavigationActionName: linkTarget.NavigationActionName || 'Create',
      };
    },
    [param2Obj.openFrom, sessionData?.viewDto],
  );

  const loadFormGroupCreateLayout = useCallback(
    (row: any) => {
      if (!row) return;

      setSelecedDataRow(row);
      setSelectedLinkTarget(sessionData?.linkTargetDto ?? null);
      setNavigationObj(null);
      setMainForm(null);
      setMainSearch(null);
      setHeaderForms([]);
      setHeaderSearches([]);

      const nextMainForms: EmbeddedFormConfig[] = [];
      const nextMainSearches: EmbeddedSearchConfig[] = [];

      if (templateHeaderList.length > 0) {
        templateHeaderList.forEach((hdr: any) => {
          const createHdr = adaptTemplateItemForCreate(hdr);
          if (createHdr.LinkTargetTransactionId) {
            const cfg = buildFormEmbedded(createHdr, row, false);
            if (cfg) nextMainForms.push(cfg);
          } else if (createHdr.LinkTargetSearchId) {
            const cfg = buildSearchEmbedded(createHdr, row, false);
            if (cfg) nextMainSearches.push(cfg);
          }
        });
      }

      if (nextMainForms.length === 0 && nextMainSearches.length === 0) {
        const fallbackTarget = adaptTemplateItemForCreate(
          sessionData?.linkTargetDto ?? linkTargetList[0],
        );
        if (fallbackTarget?.LinkTargetTransactionId) {
          const cfg = buildFormEmbedded(fallbackTarget, row, false);
          if (cfg) nextMainForms.push(cfg);
        } else if (fallbackTarget?.LinkTargetSearchId) {
          const cfg = buildSearchEmbedded(fallbackTarget, row, false);
          if (cfg) nextMainSearches.push(cfg);
        }
      }

      setCreateMainForms(nextMainForms);
      setCreateMainSearches(nextMainSearches);
    },
    [
      adaptTemplateItemForCreate,
      buildFormEmbedded,
      buildSearchEmbedded,
      linkTargetList,
      sessionData?.linkTargetDto,
      templateHeaderList,
    ],
  );

  const loadTemplateItem = useCallback(
    (linkTarget: any, row: any) => {
      if (!linkTarget || !row) return;

      setCreateMainForms([]);
      setCreateMainSearches([]);

      const effectiveLinkTarget = linkTarget;

      if (effectiveLinkTarget.SourceConditionViewColumnId) {
        const cond = row.DictViewColumnIDKeyValue?.[effectiveLinkTarget.SourceConditionViewColumnId];
        if (!cond || cond === 'False' || cond === '0') {
          alert(`${effectiveLinkTarget.NavigationActionName || effectiveLinkTarget.display || 'Action'} is not available for current row.`);
          return;
        }
      }

      setSelecedDataRow(row);
      setSelectedLinkTarget(effectiveLinkTarget);
      setNavigationObj(
        buildNavigationObj(row, sessionData?.searchResultRowList || []),
      );

      const nextHeaderForms: EmbeddedFormConfig[] = [];
      const nextHeaderSearches: EmbeddedSearchConfig[] = [];

      // Header Visibility is configured on the Main Item and applies to all its template headers.
      const visibility = Number(
        effectiveLinkTarget.OtherSettingsDto?.HeaderVisibility ?? EmAppTemplateHeaderVisibility.Show,
      );
      setHeaderVisibility(visibility);

      // Hide: do not render/load/save the header transactions at all (no band, no form).
      const buildHeaders =
        visibility !== EmAppTemplateHeaderVisibility.Hide &&
        !effectiveLinkTarget.isTemplateHeader &&
        templateHeaderList.length > 0;

      if (buildHeaders) {
        templateHeaderList.forEach((hdr: any) => {
          if (hdr.LinkTargetTransactionId) {
            const cfg = buildFormEmbedded(hdr, row, true, visibility);
            if (cfg) nextHeaderForms.push(cfg);
          } else if (hdr.LinkTargetSearchId) {
            const cfg = buildSearchEmbedded(hdr, row, true);
            if (cfg) nextHeaderSearches.push(cfg);
          }
        });
      }

      setHeaderForms(nextHeaderForms);
      setHeaderSearches(nextHeaderSearches);
      setMainForm(null);
      setMainSearch(null);

      if (effectiveLinkTarget.LinkTargetTransactionId) {
        const cfg = buildFormEmbedded(effectiveLinkTarget, row, false);
        setMainForm(cfg);
      } else if (effectiveLinkTarget.LinkTargetSearchId) {
        const cfg = buildSearchEmbedded(effectiveLinkTarget, row, false);
        setMainSearch(cfg);
      }
    },
    [
      buildFormEmbedded,
      buildNavigationObj,
      buildSearchEmbedded,
      sessionData?.searchResultRowList,
      templateHeaderList,
    ],
  );

  useEffect(() => {
    if (!sessionData || !businessTemplateReady || initializedRef.current) return;
    if (!linkTargetList.length && !isCreateLikeLinkTarget(sessionData.linkTargetDto)) return;
    initializedRef.current = true;
    if (isCreateLikeLinkTarget(sessionData.linkTargetDto)) {
      loadFormGroupCreateLayout(sessionData.selecedDataRow);
      return;
    }
    const initial =
      findLinkTargetInList(sessionData.linkTargetDto, linkTargetList) ||
      sessionData.linkTargetDto ||
      linkTargetList[0];
    loadTemplateItem(initial, sessionData.selecedDataRow);
  }, [businessTemplateReady, sessionData, linkTargetList, loadFormGroupCreateLayout, loadTemplateItem]);

  const loadFromByNavigation = useCallback(
    (direction: 'First' | 'Prev' | 'Next' | 'Last') => {
      if (!selectedLinkTarget || !navigationObj || !sessionData?.searchResultRowList?.length) return;
      const list = sessionData.searchResultRowList;
      let row: any = null;
      if (direction === 'First' && navigationObj.isAllowNavFirst) row = list[0];
      else if (direction === 'Last' && navigationObj.isAllowNavLast) row = list[list.length - 1];
      else if (direction === 'Prev' && navigationObj.isAllowNavPrev) row = list[navigationObj.currentIndex - 1];
      else if (direction === 'Next' && navigationObj.isAllowNavNext) row = list[navigationObj.currentIndex + 1];
      if (row) loadTemplateItem(selectedLinkTarget, row);
    },
    [loadTemplateItem, navigationObj, selectedLinkTarget, sessionData?.searchResultRowList],
  );

  const templateHeaderForms = useMemo((): TemplateHeaderEmbeddedForm[] =>
    headerForms.map((cfg) => ({
      key: cfg.key,
      transactionId: cfg.transactionId,
      rootPrimaryKeyValue: cfg.rootPrimaryKeyValue,
      param2: cfg.param2,
    })),
  [headerForms]);

  if (!sessionResolved || !businessTemplateReady) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.default}`}>
        <div className={`text-sm ${theme.label}`}>Loading form group...</div>
      </div>
    );
  }

  if (!sessionData) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.default}`}>
        <div className="text-red-500 p-5 text-sm text-center max-w-md">
          Transaction form group session data is unavailable. Re-open this record from the search grid.
        </div>
      </div>
    );
  }

  const showLeftMenu = !isHideLeftMenu && navLinkTargetList.length >= 2;
  const viewDisplay = sessionData.viewDto?.Display || 'Form Group';

  const renderEmbeddedSearch = (cfg: EmbeddedSearchConfig) => (
    <div key={cfg.key} className={`w-full ${cfg.isHeader ? 'mb-2' : 'h-full'}`}>
      {cfg.isHeader && (
        <div className={`px-3 py-1 text-xs font-semibold border-b ${theme.modalHeader}`}>
          {cfg.label}
        </div>
      )}
      <div className={cfg.isHeader ? 'h-64' : 'h-full min-h-[320px]'}>
        <AppSearch embeddedParamObj={cfg.paramObj} />
      </div>
    </div>
  );

  const renderEmbeddedForm = (cfg: EmbeddedFormConfig) => (
    <div
      key={cfg.key}
      className={`w-full min-h-0 ${cfg.isHeader ? 'shrink-0 mb-2' : 'h-1 flex-auto min-h-0 overflow-hidden flex flex-col'}`}
    >
      <FormMasterDetail
        embedded={{
          embeddedTransactionId: cfg.transactionId,
          embeddedRootPrimaryKeyValue: cfg.rootPrimaryKeyValue,
          embeddedParam2: cfg.param2,
        }}
      />
    </div>
  );

  return (
    <div className={`w-full h-full min-h-0 flex relative ${theme.default}`}>
      {showLeftMenu && (
        <div className="h-full min-h-0 w-[300px] shrink-0 flex flex-col border-r border-gray-200">
          <div className={`flex items-center px-3 py-1.5 text-sm border-b ${theme.mainHeader}`}>
            <span className="w-1 flex-auto truncate font-medium">{viewDisplay}</span>
            <button
              type="button"
              className={`shrink-0 px-2 py-0.5 text-xs rounded-[4px] ${theme.button_default}`}
              title="Hide Left Menu"
              onClick={() => setIsHideLeftMenu(true)}
            >
              <i className="fa-solid fa-thumbtack" aria-hidden />
            </button>
          </div>
          <div className="h-1 w-full flex-auto overflow-y-auto overflow-x-hidden p-2">
            {navLinkTargetList.map((lt: any) => {
              const isSelected = selectedLinkTarget?.Id === lt.Id &&
                (selectedLinkTarget?.LinkTargetTransactionId === lt.LinkTargetTransactionId ||
                  selectedLinkTarget?.LinkTargetSearchId === lt.LinkTargetSearchId);
              return (
                <button
                  key={`${lt.Id}_${lt.LinkTargetTransactionId || lt.LinkTargetSearchId}`}
                  type="button"
                  className={`w-full text-left px-4 py-3 text-sm rounded-[4px] mb-1 flex items-center gap-2 ${
                    isSelected ? `${theme.tab_active} font-semibold` : theme.menu_default
                  }`}
                  onClick={() => loadTemplateItem(lt, selecedDataRow)}
                >
                  <i className={`fa-solid fa-${lt.IconName || 'file-lines'} shrink-0`} aria-hidden />
                  <span className="truncate">{lt.display}</span>
                </button>
              );
            })}
          </div>
        </div>
      )}

      {isHideLeftMenu && navLinkTargetList.length >= 2 && (
        <button
          type="button"
          className={`absolute top-2 left-2 z-10 px-3 py-2 text-sm rounded-[4px] ${theme.button_secondary}`}
          title="Show Left Menu"
          onClick={() => setIsHideLeftMenu(false)}
        >
          <i className="fa-solid fa-angles-right" aria-hidden />
        </button>
      )}

      <div className="h-full w-1 flex-auto min-h-0 flex flex-col overflow-hidden">
        {navigationObj && navigationObj.itemCount > 1 && (
          <div className={`flex items-center justify-center gap-1 px-2 py-1 text-xs border-b ${theme.mainHeader}`}>
            <button
              type="button"
              disabled={!navigationObj.isAllowNavFirst}
              className={`px-2 py-1 rounded-[4px] ${theme.button_default} disabled:opacity-40`}
              onClick={() => loadFromByNavigation('First')}
            >
              <i className="fa-solid fa-angles-left" aria-hidden /> First
            </button>
            <button
              type="button"
              disabled={!navigationObj.isAllowNavPrev}
              className={`px-2 py-1 rounded-[4px] ${theme.button_default} disabled:opacity-40`}
              onClick={() => loadFromByNavigation('Prev')}
            >
              <i className="fa-solid fa-angle-left" aria-hidden /> Prev
            </button>
            <span className="px-2 min-w-[70px] text-center">{navigationObj.indexDisplay}</span>
            <button
              type="button"
              disabled={!navigationObj.isAllowNavNext}
              className={`px-2 py-1 rounded-[4px] ${theme.button_default} disabled:opacity-40`}
              onClick={() => loadFromByNavigation('Next')}
            >
              Next <i className="fa-solid fa-angle-right" aria-hidden />
            </button>
            <button
              type="button"
              disabled={!navigationObj.isAllowNavLast}
              className={`px-2 py-1 rounded-[4px] ${theme.button_default} disabled:opacity-40`}
              onClick={() => loadFromByNavigation('Last')}
            >
              Last <i className="fa-solid fa-angles-right" aria-hidden />
            </button>
          </div>
        )}

        <div className="h-1 w-full flex-auto min-h-0 flex flex-col overflow-hidden">
          {isFormGroupCreateSession ? (
            <>
              {createMainForms.map((cfg) => renderEmbeddedForm(cfg))}
              {createMainSearches.map(renderEmbeddedSearch)}
              {createMainForms.length === 0 && createMainSearches.length === 0 && (
                <div className={`flex items-center justify-center h-full text-sm ${theme.label}`}>
                  No create form could be loaded for this template.
                </div>
              )}
            </>
          ) : (
            <>
              {mainSearch && headerForms.length > 0 && headerVisibility !== EmAppTemplateHeaderVisibility.Hide && (
                <div className="w-full shrink-0 overflow-y-auto max-h-[40%]">
                  {headerForms.map((cfg) => renderEmbeddedForm(cfg))}
                </div>
              )}
              {headerSearches.length > 0 && (
                <div className={`w-full shrink-0 overflow-y-auto ${mainForm ? 'max-h-[30%]' : 'max-h-[40%]'}`}>
                  {headerSearches.map(renderEmbeddedSearch)}
                </div>
              )}

              <div className="w-full h-1 flex-auto min-h-0 overflow-hidden flex flex-col">
                {mainForm && (
                  <div className="w-full h-1 flex-auto min-h-0 overflow-hidden flex flex-col">
                    <FormMasterDetail
                      embedded={{
                        embeddedTransactionId: mainForm.transactionId,
                        embeddedRootPrimaryKeyValue: mainForm.rootPrimaryKeyValue,
                        embeddedParam2: mainForm.param2,
                      }}
                      templateHeaderForms={templateHeaderForms}
                    />
                  </div>
                )}
                {mainSearch && renderEmbeddedSearch(mainSearch)}
                {!mainForm && !mainSearch && (
                  <div className={`flex items-center justify-center h-full text-sm ${theme.label}`}>
                    No template item to display.
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default TransactionFormGroup;
