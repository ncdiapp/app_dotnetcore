import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { setUserMenu } from '../../redux/features/admin/userSessionSlice';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { searchSvc } from '../../webapi/searchSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import appHelper from '../../helper/appHelper';
import AppSearch, { type AppSearchHandle } from './AppSearch';
import { SearchFilter } from './SearchFilter';
import DataSetEditor from '../dbmgt/DataSetEditor';
import type { RootState } from '../../redux/store';
import {
  applyCategoryTreeSetting,
  applyEshopCardSearchViewSetting,
  applyEshopCardViewFilterSetting,
  type FilterOptionRow,
  type LevelSetting,
} from './eshopCategorySearchEditorApply';
import {
  CardViewSettingModal,
  CategoryTreeSettingModal,
  ItemDetailSettingModal,
  ItemFilterSettingModal,
} from './EshopCategorySearchEditorModals';

type EshopCategorySearchRouteParam = {
  id?: string | number;
  param1?: string | number;
};

function decodeRouteParam(paramRaw: string | undefined): EshopCategorySearchRouteParam {
  if (!paramRaw) return {};
  try {
    return JSON.parse(decodeURIComponent(paramRaw));
  } catch {
    return { id: paramRaw };
  }
}

const EmAppSearchUsageType = {
  EshopCategorySearch: 7,
} as const;

const EshopCategorySearchEditor: React.FC = () => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showInfo, showWarning, showValidationMessages } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();

  const { param } = useParams<{ param: string }>();
  const routeParam = useMemo(() => decodeRouteParam(param), [param]);

  const searchId = routeParam.id != null ? String(routeParam.id) : null;
  const applicationId = routeParam.param1 != null ? String(routeParam.param1) : null;

  const isBusy = useSelector((s: RootState) => s.busyLoader.isBusy);

  const [currentSearch, setCurrentSearch] = useState<any>({
    Id: null,
    Name: 'Eshop Category Search',
    Description: '',
    SearchViewId: null,
    DataSetId: null,
    Type: EmAppSearchUsageType.EshopCategorySearch,
    SaasApplicationId: applicationId,
    IsBuiltIn: false,
    IsDefault: false,
    IsAutoExecute: true,
    IsHideAllToolsBar: false,
    IsForPublicAcesss: false,
    IsModified: false,
    EshopCardSearchExDto: null,
    AppSearchFieldList: [],
    DeletedItemsIds: [],
  });

  const [advancedOpen, setAdvancedOpen] = useState(false);
  const [categoryTreeHidden, setCategoryTreeHidden] = useState(false);
  const [openCategoryModal, setOpenCategoryModal] = useState(false);
  const [openCardModal, setOpenCardModal] = useState(false);
  const [openFilterModal, setOpenFilterModal] = useState(false);
  const [openItemDetailModal, setOpenItemDetailModal] = useState(false);
  const isEditing = openCategoryModal || openCardModal || openFilterModal || openItemDetailModal || advancedOpen;

  const [dataSetList, setDataSetList] = useState<any[]>([]);
  const [categoryColumnIds, setCategoryColumnIds] = useState<string[]>([]);
  const [cardColumnIds, setCardColumnIds] = useState<string[]>([]);
  const [eshopCardSearchDtoForFilter, setEshopCardSearchDtoForFilter] = useState<any>(null);
  const [runtimeOptionCriterias, setRuntimeOptionCriterias] = useState<any[] | null>(null);
  const [cardCrit, setCardCrit] = useState<Record<string, any>>({});
  const lastCardCritExecuteSigRef = useRef<string>('__UNSET__');
  const lastCardAutoExecuteTsRef = useRef<number>(0);
  const [filterClearSignal, setFilterClearSignal] = useState(0);
  const [transactionOptions, setTransactionOptions] = useState<any[]>([]);
  const [categorySearchReloadKey, setCategorySearchReloadKey] = useState(0);
  const [dataSetEditorOpen, setDataSetEditorOpen] = useState(false);
  const [dataSetEditorId, setDataSetEditorId] = useState<number | null>(null);
  const [dataSetEditorInitialName, setDataSetEditorInitialName] = useState<string>('New Dataset');
  const [dataSetEditorTarget, setDataSetEditorTarget] = useState<'category' | 'card'>('category');
  const dataSetEditorLastSavedIdRef = useRef<number | null>(null);

  const filterTextGetterRef = useRef<null | (() => Record<string, any>)>(null);
  const categorySearchRef = useRef<AppSearchHandle>(null);
  const cardSearchRef = useRef<AppSearchHandle>(null);

  const eshopCard = currentSearch?.EshopCardSearchExDto;

  const categoryView = currentSearch?.DefaultSearchViewExDto;
  const cardDefaultView = eshopCard?.DefaultSearchViewExDto;
  const categoryFieldCount = categoryView?.AppSearchViewFieldList?.length ?? 0;

  const dataSetName = useMemo(() => {
    const id = currentSearch?.DataSetId;
    if (id == null) return '';
    const hit = dataSetList.find((d) => String(d.Id) === String(id));
    return hit?.Name || String(id);
  }, [currentSearch?.DataSetId, dataSetList]);
  const cardDataSetName = useMemo(() => {
    const id = eshopCard?.DataSetId;
    if (id == null) return '';
    const hit = dataSetList.find((d) => String(d.Id) === String(id));
    return hit?.Name || String(id);
  }, [eshopCard?.DataSetId, dataSetList]);

  const searchFieldPaths = useMemo(() => {
    const list = eshopCard?.AppSearchFieldList ?? [];
    return list
      .map((f: any) => f?.SysTableFiledPath)
      .filter((s: any) => s != null && String(s).trim() !== '');
  }, [eshopCard]);

  const viewFieldOptions = useMemo(() => {
    const list = cardDefaultView?.AppSearchViewFieldList ?? [];
    return list.filter((f: any) => f?.Id != null);
  }, [cardDefaultView]);

  const loadData = useCallback(
    async (markBusy = true, idOverride?: string | null) => {
      const effectiveIdRaw = idOverride ?? searchId ?? (currentSearch?.Id != null ? String(currentSearch.Id) : null);
      const effectiveId = effectiveIdRaw != null && String(effectiveIdRaw).trim() !== '' ? String(effectiveIdRaw) : null;
      if (!effectiveId) return;
      if (markBusy) dispatch(setIsBusy());
      try {
        const serachData = await searchSvc.retrieveOneAppSearchExDto(effectiveId);
        if (!serachData) return;
        setCurrentSearch(serachData);
        setCardCrit({});
        setFilterClearSignal((n) => n + 1);
      } catch (e) {
        showError(e instanceof Error ? e.message : 'Failed to load eshop category search');
      } finally {
        if (markBusy) dispatch(setIsNotBusy());
      }
    },
    [currentSearch?.Id, dispatch, searchId, showError],
  );

  useEffect(() => {
    setCurrentSearch((prev: any) => ({
      ...prev,
      SaasApplicationId: applicationId ?? prev.SaasApplicationId,
      Type: EmAppSearchUsageType.EshopCategorySearch,
    }));
  }, [applicationId]);

  useEffect(() => {
    void loadData(true);
  }, [loadData]);

  useEffect(() => {
    // Angular: new Eshop Category Search starts with a default name.
    if (searchId) return;
    setCurrentSearch((prev: any) => ({
      ...prev,
      Name: prev?.Name && String(prev.Name).trim() ? prev.Name : 'Eshop Category Search',
    }));
  }, [searchId]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const raw = await searchSvc.retrieveAllAppDataSetEntityDto();
        if (!cancelled) setDataSetList(Array.isArray(raw) ? raw : []);
      } catch {
        if (!cancelled) setDataSetList([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const raw = await appTransactionService.retrieveAllAppTransactions(false, '', true);
        if (!cancelled) setTransactionOptions(Array.isArray(raw) ? raw : []);
      } catch {
        if (!cancelled) setTransactionOptions([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    const dsId = currentSearch?.DataSetId;
    if (dsId == null || dsId === '') {
      setCategoryColumnIds([]);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const cols = await searchSvc.retrieveQueryColumnList(String(dsId));
        if (cancelled) return;
        const ids = (Array.isArray(cols) ? cols : [])
          .map((c: any) => c?.Id ?? c?.id)
          .filter((x: any) => x != null && String(x).trim() !== '')
          .map((x: any) => String(x));
        setCategoryColumnIds(ids);
      } catch {
        if (!cancelled) setCategoryColumnIds([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [currentSearch?.DataSetId]);

  useEffect(() => {
    const dsId = eshopCard?.DataSetId;
    if (dsId == null || dsId === '') {
      setCardColumnIds([]);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const cols = await searchSvc.retrieveQueryColumnList(String(dsId));
        if (cancelled) return;
        const ids = (Array.isArray(cols) ? cols : [])
          .map((c: any) => c?.Id ?? c?.id)
          .filter((x: any) => x != null && String(x).trim() !== '')
          .map((x: any) => String(x));
        setCardColumnIds(ids);
      } catch {
        if (!cancelled) setCardColumnIds([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [eshopCard?.DataSetId]);

  useEffect(() => {
    const cardId = eshopCard?.Id;
    if (!cardId) {
      setEshopCardSearchDtoForFilter(null);
      setRuntimeOptionCriterias(null);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const dto = await searchSvc.retrieveOneSearch(String(cardId), false);
        if (cancelled) return;
        const treeFields = (cardDefaultView?.AppSearchViewFieldList ?? [])
          .filter((f: any) => Boolean(f?.TreeLevel) && (Boolean(f?.IsTreeNodeId) || Boolean(f?.IsTreeNodeDisplay)))
          .sort((a: any, b: any) => Number(a?.TreeLevel || 0) - Number(b?.TreeLevel || 0));
        if (!treeFields.length || !Array.isArray(dto?.Criterias)) {
          setEshopCardSearchDtoForFilter(dto);
          return;
        }
        const levelConfig = new Map<number, { idPath?: string; displayPath?: string; displayLabel?: string }>();
        treeFields.forEach((f: any) => {
          const level = Number(f?.TreeLevel || 0);
          if (!level) return;
          const path = String(f?.SysTableFiledPath ?? '').trim();
          if (!path) return;
          const c = levelConfig.get(level) ?? {};
          if (f?.IsTreeNodeId) c.idPath = path;
          if (f?.IsTreeNodeDisplay) {
            c.displayPath = path;
            c.displayLabel = String(f?.DisplayText ?? path);
          }
          levelConfig.set(level, c);
        });
        const preferred = Array.from(levelConfig.entries())
          .sort((a, b) => a[0] - b[0])
          .map(([level, c]) => ({
            level,
            idPath: String(c.idPath || '').trim(),
            path: String(c.displayPath || c.idPath || '').trim(),
            label: String(c.displayLabel || c.displayPath || c.idPath || ''),
          }))
          .filter((x) => x.path !== '');
        const pickCriteriaPath = (c: any): string => {
          const p =
            c?.SysTableFiledPath ??
            c?.SearchField?.SysTableFiledPath ??
            c?.SearchFieldDto?.SysTableFiledPath ??
            c?.DictSearchField?.SysTableFiledPath ??
            c?.SearchFieldName ??
            c?.Name ??
            c?.FieldName ??
            '';
          return String(p).trim();
        };
        const normalize = (v: any) => String(v ?? '').trim().toLowerCase();
        const criteriaByPath = new Map<string, any>();
        (dto.Criterias ?? []).forEach((c: any) => {
          const p = pickCriteriaPath(c);
          if (!p || criteriaByPath.has(p)) return;
          criteriaByPath.set(p, c);
        });
        const filtered = preferred
          .map((p, idx) => {
            const targetDisplayPath = normalize(p.path);
            const targetIdPath = normalize(p.idPath);
            const targetLabel = normalize(p.label);
            const candidates = (dto.Criterias ?? []).filter((x: any) => {
              const path = normalize(pickCriteriaPath(x));
              const display = normalize(x?.Display ?? x?.DisplayText ?? x?.Label ?? '');
              const fieldDisplay = normalize(
                x?.SearchField?.DisplayText ??
                  x?.SearchFieldDto?.DisplayText ??
                  x?.SearchField?.SysTableFiledPath ??
                  x?.SearchFieldDto?.SysTableFiledPath ??
                  '',
              );
              return (
                path === targetDisplayPath ||
                (targetIdPath !== '' && path === targetIdPath) ||
                display === targetDisplayPath ||
                fieldDisplay === targetDisplayPath ||
                (targetLabel !== '' && (display === targetLabel || fieldDisplay === targetLabel))
              );
            });
            const matched = candidates.sort((a: any, b: any) => {
              const aLen = Array.isArray(a?.ItemsSource) ? a.ItemsSource.length : 0;
              const bLen = Array.isArray(b?.ItemsSource) ? b.ItemsSource.length : 0;
              return bLen - aLen;
            })[0];
            return {
              ...(matched ?? {}),
              SearcDCUID: matched?.SearcDCUID ?? `eshop_opt_${p.level}_${p.path}`,
              Display: String(p.label || matched?.Display || matched?.DisplayText || p.path),
              RowIndex: 1,
              ColumnIndex: p.level > 0 ? p.level : idx + 1,
            };
          })
          .sort((a: any, b: any) => Number(a?.ColumnIndex || 0) - Number(b?.ColumnIndex || 0));
        setEshopCardSearchDtoForFilter((prev: any) => {
          const prevCriterias = Array.isArray(prev?.Criterias) ? prev.Criterias : [];
          const merged = (filtered ?? []).map((c: any) => {
            const prevMatch = prevCriterias.find(
              (p: any) =>
                Number(p?.ColumnIndex || 0) === Number(c?.ColumnIndex || 0) ||
                String(p?.Display ?? '').trim().toLowerCase() === String(c?.Display ?? '').trim().toLowerCase(),
            );
            const nextItems = Array.isArray(c?.ItemsSource) ? c.ItemsSource : [];
            const prevItems = Array.isArray(prevMatch?.ItemsSource) ? prevMatch.ItemsSource : [];
            return {
              ...c,
              // Keep richer option data if this refresh returns empty items (load order race).
              ItemsSource: nextItems.length > 0 ? nextItems : prevItems,
              Values: Array.isArray(c?.Values)
                ? c.Values
                : Array.isArray(prevMatch?.Values)
                  ? prevMatch.Values
                  : [],
            };
          });
          return {
            ...(prev ?? {}),
            ...dto,
            Criterias: merged,
            CriteriasRowCount: 1,
          };
        });
      } catch {
        if (!cancelled) setEshopCardSearchDtoForFilter(null);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [eshopCard?.Id, cardDefaultView]);

  useEffect(() => {
    if (!(eshopCard?.Id && eshopCard?.SearchViewId)) return;
    if (!eshopCardSearchDtoForFilter?.Criterias?.length) return;
    if (openCategoryModal || openCardModal || openFilterModal || openItemDetailModal) return;
    const normalizeForSig = (v: any) => {
      if (Array.isArray(v)) return v.map((x) => String(x)).slice().sort();
      if (v == null) return v;
      if (typeof v === 'object') return v;
      return v;
    };
    const sigObj: Record<string, any> = {};
    for (const k of Object.keys(cardCrit ?? {}).sort()) {
      sigObj[k] = normalizeForSig(cardCrit?.[k]);
    }
    const sig = JSON.stringify(sigObj);
    if (lastCardCritExecuteSigRef.current === sig) return;
    lastCardCritExecuteSigRef.current = sig;
    const now = Date.now();
    // Extra throttle: avoid rapid repeated executeSearch bursts.
    if (now - lastCardAutoExecuteTsRef.current < 600) return;
    lastCardAutoExecuteTsRef.current = now;
    const t = window.setTimeout(() => {
      void cardSearchRef.current?.executeSearch();
    }, 400);
    return () => window.clearTimeout(t);
  }, [
    cardCrit,
    eshopCard?.Id,
    eshopCard?.SearchViewId,
    eshopCardSearchDtoForFilter?.Criterias?.length,
    openCategoryModal,
    openCardModal,
    openFilterModal,
    openItemDetailModal,
  ]);

  const savePayloadResult = useCallback(
    async (payload: any): Promise<{ ok: boolean; obj: any | null }> => {
      if (!String(payload?.Name ?? '').trim()) {
        showError('Please enter a name.');
        return { ok: false, obj: null };
      }
      dispatch(setIsBusy());
      try {
        // Avoid backend optimistic concurrency errors by saving on top of the latest server DTO.
        let effectivePayload = payload;
        const idNum = payload?.Id != null ? Number(payload.Id) : 0;
        if (Number.isFinite(idNum) && idNum > 0) {
          try {
            const latest = await searchSvc.retrieveOneAppSearchExDto(String(idNum));
            if (latest) {
              effectivePayload = {
                ...latest,
                // Overwrite only what user can change here.
                Name: payload?.Name ?? latest?.Name,
                Description: payload?.Description ?? latest?.Description,
                DataSetId: payload?.DataSetId ?? latest?.DataSetId,
                IsModified: true,
                // Keep view edits from the editor (may be a draft or modified view).
                DefaultSearchViewExDto: payload?.DefaultSearchViewExDto ?? latest?.DefaultSearchViewExDto,
                EshopCardSearchExDto: payload?.EshopCardSearchExDto ?? latest?.EshopCardSearchExDto,
                AppSearchFieldList: payload?.AppSearchFieldList ?? latest?.AppSearchFieldList,
                AppSearchParameterList: payload?.AppSearchParameterList ?? latest?.AppSearchParameterList,
                DeletedItemsIds: payload?.DeletedItemsIds ?? latest?.DeletedItemsIds,
              };
            }
          } catch {
            // If reload fails, fall back to payload and let server decide.
          }
        }

        const data = await searchSvc.saveEshopCategorySearchExDto(effectivePayload);
        if (data?.IsSuccessful && data?.Object) {
          // Prefer server message(s) if provided.
          const msg =
            data?.ValidationResult?.Items?.find((i: any) => i?.LocalizedMessage)?.LocalizedMessage ??
            data?.ValidationResult?.Items?.find((i: any) => i?.ErrorMessage)?.ErrorMessage ??
            'Saved';
          showInfo(msg, true);
          await loadData(true);
          // Force embedded category AppSearch remount so tree is reloaded with latest mapping changes.
          setCategorySearchReloadKey((k) => k + 1);
          window.setTimeout(() => {
            void categorySearchRef.current?.executeSearch();
          }, 250);
          return { ok: true, obj: data.Object };
        }
        if (data?.ValidationResult) {
          showValidationMessages(data.ValidationResult, true);
        } else {
          showError('Save failed');
        }
        return { ok: false, obj: null };
      } catch (e) {
        showError(e instanceof Error ? e.message : 'Save failed');
        return { ok: false, obj: null };
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, loadData, showError, showInfo, showValidationMessages],
  );

  const savePayload = useCallback(
    async (payload: any): Promise<boolean> => {
      const r = await savePayloadResult(payload);
      return r.ok;
    },
    [savePayloadResult],
  );

  const runSave = useCallback(async (): Promise<boolean> => {
    return savePayload(currentSearch);
  }, [currentSearch, savePayload]);

  const embeddedCategoryParam = useMemo(() => {
    if (!currentSearch?.Id || !currentSearch?.SearchViewId) return null;
    return {
      searchId: currentSearch.Id,
      isSavedSearch: false,
      initialViewId: currentSearch.SearchViewId,
      isShowCriterias: false,
      isLinkedSearch: false,
      flatDataSetTreeOnHide: () => setCategoryTreeHidden(true),
    };
  }, [currentSearch?.Id, currentSearch?.SearchViewId]);

  const embeddedCardParam = useMemo(() => {
    const cardId = eshopCard?.Id;
    const cardViewId = eshopCard?.SearchViewId;
    if (!cardId || !cardViewId) return null;
    return {
      searchId: cardId,
      isSavedSearch: false,
      initialViewId: cardViewId,
      isShowCriterias: false,
      isLinkedSearch: false,
      criteriaDictOverride: cardCrit,
      getTextCriteriaOverrides: () => filterTextGetterRef.current?.() ?? {},
      onSearchResult: (searchResult: any) => {
        const extractedDto =
          searchResult?.EshopCategoryViewDto ??
          searchResult?.EshopCatelogViewDto ??
          searchResult?.EshopCatalogViewDto ??
          searchResult?.SearchResultDto?.EshopCategoryViewDto ??
          searchResult?.SearchResultDto?.EshopCatelogViewDto ??
          searchResult?.SearchResultDto?.EshopCatalogViewDto ??
          {};
        appHelper.debugLog('EshopCategory embedded onSearchResult called', {
          rootKeys: Object.keys(searchResult ?? {}),
          hasEshopCategoryViewDto: Boolean(searchResult?.EshopCategoryViewDto),
          hasEshopCatelogViewDto: Boolean(searchResult?.EshopCatelogViewDto),
          hasSearchResultDto: Boolean(searchResult?.SearchResultDto),
          extractedDtoKeys: Object.keys(extractedDto ?? {}),
        });
        const dto = extractedDto;
        const tryParseJsonObject = (v: any): any => {
          if (typeof v !== 'string') return v;
          const s = v.trim();
          if (!s) return {};
          if (!(s.startsWith('{') || s.startsWith('['))) return v;
          try {
            return JSON.parse(s);
          } catch {
            return v;
          }
        };
        const displayByLevel =
          tryParseJsonObject(
            dto?.DictoptionDisplay ?? dto?.DictOptionDisplay ?? dto?.DictoptoinDisplay ?? dto?.DictOptoinDisplay ?? {},
          ) ?? {};
        const dictItems =
          tryParseJsonObject(
            dto?.DictoptionItems ??
              dto?.DictOptionItems ??
              dto?.DictoptionItemsArray ??
              dto?.DictOptionItemsArray ??
              dto?.DictoptionItems ??
              dto?.DictOptionItems ??
              dto?.DictoptoinItems ??
              dto?.DictOptoinItems ??
              {},
          ) ?? {};
        const dictLevel =
          tryParseJsonObject(
            dto?.DictoptionLevel ??
              dto?.DictOptionLevel ??
              dto?.DictoptoinLevel ??
              dto?.DictOptoinLevel ??
              {},
          ) ?? {};
        // Some APIs return empty DictoptionItems but real data in DictoptionLevel.
        const itemsByLabel = (Object.keys(dictLevel ?? {}).length > 0 ? dictLevel : dictItems) ?? {};
        appHelper.debugLog('EshopCategory filter raw source keys', {
          displayByLevelKeys: Object.keys(displayByLevel ?? {}),
          itemsByLabelKeys: Object.keys(itemsByLabel ?? {}),
          dictoptionLevelKeys: Object.keys(
            tryParseJsonObject(
              dto?.DictoptionLevel ?? dto?.DictOptionLevel ?? dto?.DictoptoinLevel ?? dto?.DictOptoinLevel ?? {},
            ) ?? {},
          ),
        });
        const getItemsByKeys = (source: any, keys: string[]): any[] => {
          if (!source) return [];
          for (const key of keys) {
            const direct = source?.[key];
            if (Array.isArray(direct)) return direct;
            const nested =
              direct?.Items ??
              direct?.items ??
              direct?.Values ??
              direct?.values ??
              direct?.List ??
              direct?.list ??
              direct?.Data ??
              direct?.data;
            if (Array.isArray(nested)) return nested;
          }
          return [];
        };
        const pairs: { label: string; items: any[]; order: number }[] = [];
        const displayEntries = Object.entries(displayByLevel ?? {});
        if (displayEntries.length) {
          for (const [k, v] of displayEntries) {
            const vv: any = v as any;
            const label =
              String(vv?.Display ?? vv?.display ?? vv?.Label ?? vv?.label ?? vv?.Name ?? vv?.name ?? vv ?? '').trim();
            if (!label) continue;
            const keyNum = Number(k);
            const keys = [label, k, Number.isFinite(keyNum) ? String(keyNum) : '', Number.isFinite(keyNum) ? String(keyNum + 1) : '']
              .map((x) => String(x || '').trim())
              .filter(Boolean);
            let arr = getItemsByKeys(itemsByLabel, keys);
            if (!arr.length && Array.isArray(itemsByLabel) && Number.isFinite(keyNum)) {
              const byIndex = itemsByLabel[keyNum - 1] ?? itemsByLabel[keyNum];
              arr = Array.isArray(byIndex)
                ? byIndex
                : Array.isArray(byIndex?.Items ?? byIndex?.items)
                  ? (byIndex?.Items ?? byIndex?.items)
                  : [];
            }
            
            if (arr.length) pairs.push({ label, items: arr, order: Number(k) || 0 });
          }
        }
        if (!pairs.length && itemsByLabel && typeof itemsByLabel === 'object') {
          for (const [k, v] of Object.entries(itemsByLabel)) {
            const list = Array.isArray(v)
              ? v
              : Array.isArray((v as any)?.Items ?? (v as any)?.items)
                ? ((v as any)?.Items ?? (v as any)?.items)
                : [];
            if (!list.length) continue;
            const mappedLabel = String((displayByLevel as any)?.[k] ?? k);
            pairs.push({ label: mappedLabel, items: list as any[], order: Number(k) || 0 });
          }
        }
        if (!pairs.length) return;
        appHelper.debugLog('EshopCategory filter onSearchResult raw', {
          displayByLevelKeys: Object.keys(displayByLevel ?? {}),
          itemsByLabelKeys: Object.keys(itemsByLabel ?? {}),
          sampleEshopCategoryViewDto: dto,
        });
        pairs.sort((a, b) => a.order - b.order);
        appHelper.debugLog(
          'EshopCategory filter parsed pairs',
          pairs.map((p) => ({ label: p.label, order: p.order, itemCount: (p.items ?? []).length })),
        );
        const normalizeOptionItems = (items: any[]): any[] => {
          const seen = new Set<string>();
          const list: any[] = [];
          for (const raw of items ?? []) {
            const displayRaw =
              raw?.Display ??
              raw?.display ??
              raw?.Name ??
              raw?.name ??
              raw?.Text ??
              raw?.text ??
              raw?.Label ??
              raw?.label ??
              raw?.Code ??
              raw?.code ??
              raw?.Value ??
              raw?.value ??
              (typeof raw === 'string' || typeof raw === 'number' ? raw : '');
            const idRaw =
              raw?.Id ??
              raw?.id ??
              raw?.Value ??
              raw?.value ??
              raw?.Code ??
              raw?.code ??
              raw?.Key ??
              raw?.key ??
              // Some APIs only return label text, no explicit Id.
              displayRaw;
            const id = String(idRaw ?? '').trim();
            const display = String(displayRaw ?? '').trim();
            if (!id) continue;
            if (seen.has(id)) continue;
            seen.add(id);
            list.push({ Id: id, Display: display || id });
          }
          return list;
        };
        const pairByLabelLower = new Map<string, any[]>();
        for (const p of pairs) {
          pairByLabelLower.set(String(p.label ?? '').trim().toLowerCase(), Array.isArray(p.items) ? p.items : []);
        }

        setEshopCardSearchDtoForFilter((prev: any) => {
          const prevCriterias = Array.isArray(prev?.Criterias) ? prev.Criterias : [];
          if (!prevCriterias.length) return prev;
          const nextCriterias = prevCriterias.map((c: any) => {
            const labelLower = String(c?.Display ?? '').trim().toLowerCase();
            const rawItems = pairByLabelLower.get(labelLower);
            if (!rawItems) return c;
            const normalizedItems = normalizeOptionItems(Array.isArray(rawItems) ? rawItems : []);
            const prevValues = Array.isArray(c?.Values) ? c.Values : [];
            return {
              ...c,
              CriteriaType: 1,
              IsAllowMultipleSelect: true,
              IsOptionFilterChecklist: true,
              Values: prevValues,
              ItemsSource: normalizedItems,
            };
          });

         

          return {
            ...(prev ?? {}),
            Criterias: nextCriterias,
            CriteriasRowCount: 1,
          };
        });

        // We only update ItemsSource based on current DTO criterias to avoid breaking backend expectations.
        setRuntimeOptionCriterias(null);
      },
    };
  }, [eshopCard?.Id, eshopCard?.SearchViewId, cardCrit]);

  const normalizedFilterDto = useMemo(() => {
    const dto = eshopCardSearchDtoForFilter;
    if (!dto || !Array.isArray(dto.Criterias)) return dto;
    return {
      ...dto,
      Criterias: dto.Criterias.map((c: any) => {
        const hasItems = Array.isArray(c?.ItemsSource) && c.ItemsSource.length > 0;
        if (!hasItems) return c;
        return {
          ...c,
          // Force option criteria to render as entity multi-select like Angular left filter.
          CriteriaType: 1,
          IsAllowMultipleSelect: true,
          Values: Array.isArray(c?.Values) ? c.Values : [],
        };
      }),
      CriteriasRowCount: 1,
    };
  }, [eshopCardSearchDtoForFilter]);

  useEffect(() => {
    if (!normalizedFilterDto || !Array.isArray(normalizedFilterDto.Criterias)) return;
    const brief = normalizedFilterDto.Criterias.map((c: any) => ({
      display: String(c?.Display ?? ''),
      itemCount: Array.isArray(c?.ItemsSource) ? c.ItemsSource.length : 0,
    }));
    appHelper.debugLog('EshopCategory filter render criterias', brief);
  }, [normalizedFilterDto]);

  const onRefresh = useCallback(async () => {
    await loadData(true, null);
  }, [loadData]);

  const openCardViewSetting = useCallback(() => {
    if (categoryFieldCount <= 0) return;
    if (!currentSearch?.EshopCardSearchExDto) {
      const draftCardSearch = {
        Name: `${currentSearch?.Name || 'Search'} - Card View`,
        Type: 1,
        // Angular parity: card view dataset is independent from category tree dataset.
        DataSetId: null,
        SaasApplicationId: currentSearch?.SaasApplicationId ?? null,
        IsAutoExecute: true,
        SearchViewId: null,
        DefaultSearchViewExDto: null,
        AppSearchFieldList: [],
        DeletedItemsIds: [],
      };
      setCurrentSearch((prev: any) => ({
        ...prev,
        EshopCardSearchExDto: draftCardSearch,
        IsModified: true,
      }));
    }
    setOpenCardModal(true);
  }, [categoryFieldCount, currentSearch?.EshopCardSearchExDto, currentSearch?.Name, currentSearch?.SaasApplicationId]);

  const onAddToMainMenu = useCallback(async () => {
    try {
      if (!currentSearch?.Id || !currentSearch?.SearchViewId) {
        showWarning('Save the search and set a default view first.');
        return;
      }

      dispatch(setIsBusy());

      const searchDto: any = currentSearch;
      const saasAppId = searchDto.SaasApplicationId;
      const menuData = await adminSvc.retrieveListMenuHairarchyDto(false, saasAppId != null ? String(saasAppId) : '');

      if (!Array.isArray(menuData) || !menuData.length) {
        showError('No main menu root found for this application.');
        return;
      }

      const rootMenu = menuData[0];
      let existSearchMenu: any = null;
      let maxSort = 0;

      if (Array.isArray(rootMenu.AppListMenu_List)) {
        rootMenu.AppListMenu_List.forEach((aMenu: any) => {
          if (typeof aMenu.Sort === 'number' && aMenu.Sort > maxSort) {
            maxSort = aMenu.Sort;
          }
          if (aMenu.RouteCode === 'MasterDataManagement' && String(aMenu.Link) === String(searchDto.Id)) {
            existSearchMenu = aMenu;
          }
        });
      }

      if (existSearchMenu) {
        showInfo(`Current search is already in main menu.\nMenu path: ${rootMenu.Name} / ${existSearchMenu.Name}`);
        return;
      }

      const menuDto: any = {
        IsNew: true,
        EmAppMenuItemCategory: 1,
        EmDeviceMenuShowMode: 3,
        LinkType: 1,
        RouteCode: 'MasterDataManagement',
        IconName: '',
        Sort: maxSort + 1,
        Name: searchDto.Name,
        Description: searchDto.Description,
        Link: searchDto.Id,
        ParentId: rootMenu.Id,
      };

      const result = await adminSvc.saveOneAppListMenuTreeNode(menuDto);
      if (result?.IsSuccessful && result?.Object) {
        showInfo(`Current search has been added to main menu.\nMenu path: ${rootMenu.Name} / ${menuDto.Name}`);
        try {
          const userMenu = await adminSvc.retrieveUserTreeMenu();
          dispatch(setUserMenu(userMenu));
        } catch (menuError: any) {
          // eslint-disable-next-line no-console
          console.error('Error refreshing user menu after adding search to main menu:', menuError);
        }
      } else {
        showError('Failed to add to main menu');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to add to main menu');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentSearch, dispatch, showError, showInfo, showWarning]);

  const onTestRun = useCallback(() => {
    if (!currentSearch?.Id || !currentSearch?.SearchViewId) {
      showWarning('Save the search and set a default view first');
      return;
    }
    addTabAndNavigate('masterdatamanagement', 'Search', { searchId: currentSearch.Id, isSavedSearch: false }, true);
  }, [addTabAndNavigate, currentSearch?.Id, currentSearch?.SearchViewId, showWarning]);

  const AdvancedOptionsPopup = () => {
    if (!advancedOpen) return null;
    return (
      <div
        className="fixed inset-0 z-[1200] bg-black/30 flex items-center justify-center"
        onClick={() => setAdvancedOpen(false)}
      >
        <div
          className={`w-[400px] rounded border shadow-lg bg-white ${theme.mainContentSection}`}
          onClick={(e) => e.stopPropagation()}
        >
          <div className={`px-3 py-2 border-b flex items-center justify-between ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold ${theme.title}`}>Advanced Options</div>
            <button type="button" className={theme.button_default} onClick={() => setAdvancedOpen(false)}>
              Close
            </button>
          </div>
          <div className="p-3 space-y-2">
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={Boolean(currentSearch.IsBuiltIn)}
                onChange={(e) => {
                  setCurrentSearch((prev: any) => ({ ...prev, IsBuiltIn: e.target.checked, IsModified: true }));
                }}
              />
              Is Builtin
            </label>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={Boolean(currentSearch.IsDefault)}
                onChange={(e) => {
                  setCurrentSearch((prev: any) => ({ ...prev, IsDefault: e.target.checked, IsModified: true }));
                }}
              />
              Is Default
            </label>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={Boolean(currentSearch.IsAutoExecute)}
                onChange={(e) => {
                  setCurrentSearch((prev: any) => ({ ...prev, IsAutoExecute: e.target.checked, IsModified: true }));
                }}
              />
              Is Auto Execute
            </label>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={Boolean(currentSearch.IsHideAllToolsBar)}
                onChange={(e) => {
                  setCurrentSearch((prev: any) => ({ ...prev, IsHideAllToolsBar: e.target.checked, IsModified: true }));
                }}
              />
              Is Hide All Toolsbar Buttons
            </label>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={Boolean(currentSearch.IsForPublicAcesss)}
                onChange={(e) => {
                  setCurrentSearch((prev: any) => ({ ...prev, IsForPublicAcesss: e.target.checked, IsModified: true }));
                }}
              />
              Is For Public Acesss
            </label>
          </div>
          <div className="px-3 py-2 border-t flex justify-end gap-2">
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => setAdvancedOpen(false)}
            >
              Ok
            </button>
          </div>
        </div>
      </div>
    );
  };

  // Angular parity hints:
  // 1) Show card-view hint only when tree fields are configured (>=2) but card view is not created yet.
  const showCardViewSettingHint = !isEditing && categoryFieldCount >= 2 && !cardDefaultView;

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Eshop Category Search Editor</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className={`px-2 h-6 rounded-[4px] text-xs ${theme.button_default}`}
            onClick={onRefresh}
            disabled={isBusy || !(searchId || currentSearch?.Id)}
            title="Refresh"
          >
            <i className="fa fa-refresh" aria-hidden="true" /> Refresh
          </button>
          <button
            type="button"
            className={`px-2 h-6 rounded-[4px] text-xs ${theme.button_default}`}
            onClick={() => void runSave()}
            disabled={!currentSearch?.IsModified || isBusy}
            title="Save"
          >
            <i className="fa fa-save" aria-hidden="true" /> Save
          </button>
          <button
            type="button"
            className={`px-2 h-6 rounded-[4px] text-xs ${theme.button_default}`}
            onClick={() => setAdvancedOpen(true)}
            disabled={!currentSearch?.Id || isBusy}
            title="Advanced Options"
          >
            <i className="fa fa-cogs" aria-hidden="true" /> Advanced Options
          </button>
          <button
            type="button"
            className={`px-2 h-6 rounded-[4px] text-xs ${theme.button_default}`}
            onClick={onAddToMainMenu}
            disabled={!currentSearch?.Id || !currentSearch?.SearchViewId || isBusy}
            title="Add To Main Menu"
          >
            <i className="fa fa-plus" aria-hidden="true" /> Add To Main Menu
          </button>
          <button
            type="button"
            className={`px-2 h-6 rounded-[4px] text-xs ${theme.button_default}`}
            onClick={onTestRun}
            disabled={!currentSearch?.Id || !currentSearch?.SearchViewId || isBusy}
            title="Test Run"
          >
            <i className="fa-solid fa-window-maximize" aria-hidden /> Test Run
          </button>
        </div>
      </div>

      <AdvancedOptionsPopup />

      <CategoryTreeSettingModal
        open={openCategoryModal}
        theme={theme}
        dataSetId={currentSearch?.DataSetId}
        dataSetName={dataSetName}
        dataSetOptions={dataSetList}
        onDataSetIdChange={(id: number | string | null) => {
          setCurrentSearch((prev: any) => ({
            ...prev,
            DataSetId: id,
            IsModified: true,
          }));
        }}
        onCreateDataSet={() => {
          setDataSetEditorTarget('category');
          setDataSetEditorInitialName('New Dataset');
          setDataSetEditorId(null);
          setDataSetEditorOpen(true);
        }}
        onEditDataSet={(id: number | string) => {
          const n = Number(id);
          if (!Number.isFinite(n)) return;
          setDataSetEditorTarget('category');
          setDataSetEditorInitialName('Dataset');
          setDataSetEditorId(n);
          setDataSetEditorOpen(true);
        }}
        columnIds={categoryColumnIds}
        categoryView={categoryView}
        onClose={() => setOpenCategoryModal(false)}
        onApply={async (levels: number, rows: LevelSetting[]) => {
        
          const isNewSearch = !(Number(currentSearch?.Id) > 0);

          let base = currentSearch;
          if (isNewSearch) {
            const firstPayload = { ...currentSearch, IsModified: true };
            const r1 = await savePayloadResult(firstPayload);
            if (!r1.ok || !r1.obj) return false;
            base = r1.obj;
          }

          const view = base?.DefaultSearchViewExDto ?? categoryView;
          if (!view) {
            showError('Search view is missing. Please save the search first.');
            return false;
          }

          const err = applyCategoryTreeSetting(view, levels, rows);
          if (err) {
            showError(err);
            return false;
          }

          const payload = { ...base, DefaultSearchViewExDto: view, IsModified: true };
          setCurrentSearch(payload);
          return savePayload(payload);
        }}
      />

      {dataSetEditorOpen && (
        <div className="fixed inset-0 z-[1400] bg-black/40">
          <div className="absolute inset-3 rounded border shadow-lg overflow-hidden bg-white">
            <DataSetEditor
              ignoreRouteParam
              dataSetId={dataSetEditorId}
              initialName={dataSetEditorInitialName}
              onSave={(saved: any) => {
                // If user clicks Save then closes later, we still need to return/select dataset id.
                const id = saved?.Id != null ? Number(saved.Id) : null;
                if (id != null && Number.isFinite(id)) {
                  dataSetEditorLastSavedIdRef.current = id;
                }
              }}
              onConfirmAndClose={async (saved: any) => {
                // Update dataset list + select the saved dataset (Angular parity).
                try {
                  const raw = await searchSvc.retrieveAllAppDataSetEntityDto();
                  setDataSetList(Array.isArray(raw) ? raw : []);
                } catch {
                  // ignore
                }
                if (saved?.Id != null) {
                  setCurrentSearch((prev: any) => {
                    if (dataSetEditorTarget === 'card') {
                      return {
                        ...prev,
                        EshopCardSearchExDto: {
                          ...(prev?.EshopCardSearchExDto ?? {}),
                          DataSetId: saved.Id,
                          IsModified: true,
                        },
                        IsModified: true,
                      };
                    }
                    return {
                      ...prev,
                      DataSetId: saved.Id,
                      IsModified: true,
                    };
                  });
                }
                dataSetEditorLastSavedIdRef.current = null;
                setDataSetEditorOpen(false);
              }}
              onClose={async () => {
                // If the dataset was saved (even without Save&Close), return/select it on close.
                const lastId = dataSetEditorLastSavedIdRef.current;
                if (lastId != null && Number.isFinite(lastId)) {
                  try {
                    const raw = await searchSvc.retrieveAllAppDataSetEntityDto();
                    setDataSetList(Array.isArray(raw) ? raw : []);
                  } catch {
                    // ignore
                  }
                  setCurrentSearch((prev: any) => {
                    if (dataSetEditorTarget === 'card') {
                      return {
                        ...prev,
                        EshopCardSearchExDto: {
                          ...(prev?.EshopCardSearchExDto ?? {}),
                          DataSetId: lastId,
                          IsModified: true,
                        },
                        IsModified: true,
                      };
                    }
                    return {
                      ...prev,
                      DataSetId: lastId,
                      IsModified: true,
                    };
                  });
                }
                dataSetEditorLastSavedIdRef.current = null;
                setDataSetEditorOpen(false);
              }}
            />
          </div>
        </div>
      )}

      <CardViewSettingModal
        open={openCardModal}
        theme={theme}
        categoryView={categoryView}
        eshopCardSearchExDto={eshopCard}
        dataSetId={eshopCard?.DataSetId}
        dataSetName={cardDataSetName}
        dataSetOptions={dataSetList}
        onDataSetIdChange={(id: number | string | null) => {
          setCurrentSearch((prev: any) => ({
            ...prev,
            EshopCardSearchExDto: {
              ...(prev?.EshopCardSearchExDto ?? {}),
              DataSetId: id,
              IsModified: true,
            },
            IsModified: true,
          }));
        }}
        onCreateDataSet={() => {
          setDataSetEditorTarget('card');
          setDataSetEditorInitialName('New Dataset');
          setDataSetEditorId(null);
          setDataSetEditorOpen(true);
        }}
        onEditDataSet={(id: number | string) => {
          const n = Number(id);
          if (!Number.isFinite(n)) return;
          setDataSetEditorTarget('card');
          setDataSetEditorInitialName('Dataset');
          setDataSetEditorId(n);
          setDataSetEditorOpen(true);
        }}
        cardColumnIds={cardColumnIds}
        searchFieldPaths={searchFieldPaths}
        onClose={() => setOpenCardModal(false)}
        onApply={async ({
          levels,
          rootGroupKeyField,
          cardViewFieldList,
          deletedViewFieldIds,
        }: {
          levels: any[];
          rootGroupKeyField: string | null;
          cardViewFieldList: any[];
          deletedViewFieldIds: (number | string)[];
        }) => {
          if (!currentSearch?.EshopCardSearchExDto) return false;
          let base = currentSearch;
          const isNewCardSearch = !(Number(eshopCard?.Id) > 0);
          if (isNewCardSearch) {
            const r1 = await savePayloadResult({ ...currentSearch, IsModified: true });
            if (!r1.ok || !r1.obj) return false;
            base = r1.obj;
          }

          const baseCategoryView = base?.DefaultSearchViewExDto;
          const baseCardSearch = base?.EshopCardSearchExDto;
          if (!baseCategoryView || !baseCardSearch) return false;

          const err = applyEshopCardSearchViewSetting(baseCategoryView, baseCardSearch, levels, rootGroupKeyField);
          if (err) {
            showError(err);
            return false;
          }
          baseCardSearch.DefaultSearchViewExDto = baseCardSearch.DefaultSearchViewExDto ?? {};
          baseCardSearch.DefaultSearchViewExDto.AppSearchViewFieldList = Array.isArray(cardViewFieldList)
            ? cardViewFieldList
            : [];
          baseCardSearch.DefaultSearchViewExDto.DeletedItemsIds = [
            ...(baseCardSearch.DefaultSearchViewExDto.DeletedItemsIds ?? []),
            ...(deletedViewFieldIds ?? []),
          ];
          baseCardSearch.DefaultSearchViewExDto.IsModified = true;
          base.IsModified = true;
          return savePayload(base);
        }}
      />

      <ItemFilterSettingModal
        open={openFilterModal}
        theme={theme}
        cardDefaultView={cardDefaultView}
        columnIds={cardColumnIds}
        onClose={() => setOpenFilterModal(false)}
        onApply={async (options: FilterOptionRow[]) => {
          if (!cardDefaultView) return false;
          applyEshopCardViewFilterSetting(cardDefaultView, options);
          setCurrentSearch((prev: any) => ({ ...prev, IsModified: true }));
          return runSave();
        }}
      />

      <ItemDetailSettingModal
        open={openItemDetailModal}
        theme={theme}
        cardDefaultView={cardDefaultView}
        viewFieldOptions={viewFieldOptions}
        transactionOptions={transactionOptions}
        onClose={() => setOpenItemDetailModal(false)}
        onApply={async (patch: { needSaveAsFromBaseId: number | null; logicKeyFieldId: number | null }) => {
          if (!cardDefaultView) return false;
          cardDefaultView.OtherSettingsDto = cardDefaultView.OtherSettingsDto ?? {};
          cardDefaultView.OtherSettingsDto.LogicKeyFieldId = patch.logicKeyFieldId;
          if (patch.needSaveAsFromBaseId != null) {
            cardDefaultView.NeedToSaveAsFromEshopProductBaseDataModelId = patch.needSaveAsFromBaseId;
          }
          setCurrentSearch((prev: any) => ({ ...prev, IsModified: true }));
          return runSave();
        }}
      />

      <div className={`flex-auto overflow-hidden flex flex-col p-3 ${theme.mainContentSection}`}>
        <div className="flex items-start gap-4">
          <div className="flex items-center gap-2 w-[400px]">
            <label className={`w-[78px] text-xs ${theme.label}`}>Name</label>
            <input
              className={`flex-auto h-7 px-2 text-xs border rounded ${theme.inputBox}`}
              value={currentSearch?.Name ?? ''}
              onChange={(e) => {
                const v = e.target.value;
                setCurrentSearch((prev: any) => ({ ...prev, Name: v, IsModified: true }));
              }}
            />
          </div>
          <div className="flex items-center gap-2 w-[400px]">
            <label className={`w-[78px] text-xs ${theme.label}`}>Description</label>
            <input
              className={`flex-auto h-7 px-2 text-xs border rounded ${theme.inputBox}`}
              value={currentSearch?.Description ?? ''}
              onChange={(e) => {
                const v = e.target.value;
                setCurrentSearch((prev: any) => ({ ...prev, Description: v, IsModified: true }));
              }}
            />
          </div>
        </div>

        <div className="flex-auto min-h-0 flex gap-3 mt-3">
          {!categoryTreeHidden && (
            <div className="w-[400px] min-w-[400px] border rounded overflow-hidden flex flex-col">
              <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>
                Category Tree
                <div className="float-right relative">
                  <button
                    type="button"
                    className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                    onClick={() => setOpenCategoryModal(true)}
                    disabled={isBusy}
                  >
                    <i className="fa fa-gear" aria-hidden="true" /> Setting
                  </button>
                  {!isEditing && !currentSearch?.Id && (
                    <div className="absolute right-0 top-[28px] z-20">
                      <div className="relative max-w-[230px] rounded border border-yellow-300 bg-yellow-50 text-yellow-950 shadow-md px-2 py-1 text-[11px] leading-tight">
                        <div className="absolute right-[18px] top-[-5px] w-[10px] h-[10px] rotate-45 bg-yellow-50 border-l border-t border-yellow-300" />
                        Click <span className="font-semibold">Setting</span> to start configuration.
                      </div>
                    </div>
                  )}
                </div>
              </div>
              <div className="flex-auto min-h-0 overflow-hidden">
                {embeddedCategoryParam ? (
                  <AppSearch
                    key={`category-app-search-${categorySearchReloadKey}`}
                    ref={categorySearchRef}
                    embeddedParamObj={embeddedCategoryParam}
                  />
                ) : (
                  <div className="p-3 text-xs text-gray-600">
                    Configure the dataset and category levels first (click <span className="font-semibold">Setting</span>).
                  </div>
                )}
              </div>
            </div>
          )}

          {categoryTreeHidden && (
            <button
              type="button"
              className={`self-start px-2 py-1 text-xs rounded ${theme.button_default}`}
              onClick={() => setCategoryTreeHidden(false)}
            >
              Show category tree
            </button>
          )}

          <div className="flex-auto min-w-0 border rounded overflow-hidden flex flex-col relative">
            <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>
              Item Card View
              <div className="float-right flex gap-1 relative">
                <button
                  type="button"
                  className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                  onClick={openCardViewSetting}
                  disabled={categoryFieldCount <= 0}
                  title={categoryFieldCount <= 0 ? 'Configure category tree fields first' : ''}
                >
                  <i className="fa fa-gear" aria-hidden="true" /> Card View Setting
                </button>
                {showCardViewSettingHint && (
                  <div className="absolute right-0 top-[28px] z-20">
                    <div className="relative max-w-[260px] rounded border border-yellow-300 bg-yellow-50 text-yellow-950 shadow-md px-2 py-1 text-[11px] leading-tight">
                      <div className="absolute right-[18px] top-[-5px] w-[10px] h-[10px] rotate-45 bg-yellow-50 border-l border-t border-yellow-300" />
                      Click the <span className="font-semibold">Card View Setting</span> button to configure item card view.
                    </div>
                  </div>
                )}
                {Boolean(eshopCard?.Id) && (
                  null
                )}
              </div>
            </div>

            <div className="flex-auto min-h-0 overflow-hidden flex">
              <div className="w-[400px] min-w-[400px] border-r flex flex-col">
                <div className={`px-3 py-1 text-xs border-b flex justify-between items-center ${theme.mainContentSection}`}>
                  <span>Item Filter</span>
                  <button
                    type="button"
                    className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                    onClick={() => {
                      setOpenFilterModal(true);
                    }}
                    disabled={!eshopCard?.Id}
                  >
                    <i className="fa fa-gear" aria-hidden="true" /> Setting
                  </button>
                </div>
                <div className="flex-auto min-h-0 overflow-auto p-2">
                  {normalizedFilterDto ? (
                    <>
                      <SearchFilter
                        searchDto={normalizedFilterDto}
                        dictDcuValue={cardCrit}
                        onCriteriaValueChanged={(searchFieldId, value) => {
                          setCardCrit((prev) => ({ ...prev, [searchFieldId]: value }));
                        }}
                        onRegisterTextCriteriaOverrides={(getter) => {
                          filterTextGetterRef.current = getter;
                        }}
                        clearSignal={filterClearSignal}
                      />
                    </>
                  ) : (
                    <div className={`text-xs ${theme.label}`}>Load card search criteria…</div>
                  )}
                </div>
              </div>
              <div className="flex-auto min-w-0 overflow-hidden">
                <div className="w-full h-full">
                  {embeddedCardParam ? (
                    <AppSearch ref={cardSearchRef} embeddedParamObj={embeddedCardParam} />
                  ) : (
                    <div className="p-3 text-xs text-gray-600">Eshop card search view is missing.</div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default EshopCategorySearchEditor;
