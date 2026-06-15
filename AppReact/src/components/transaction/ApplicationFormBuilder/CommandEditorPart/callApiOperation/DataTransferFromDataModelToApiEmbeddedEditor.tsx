/**
 * Embedded FromDataModelToApi mapping editor (Angular: TransactionFormDataTransferSettingEditor, isEmbedded).
 * Tabs: Before Publish API Call Mapping / After Publish API Call Mapping.
 */

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { useTheme } from '../../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../../redux/hooks/useErrorMessage';
import { useTabNavigation } from '../../../../../redux/hooks/useTabNavigation';
import { appTransactionService } from '../../../../../webapi/apptransactionsvc';
import { integrationService } from '../../../../../webapi/integrationsvc';
import { PopupStackLayer, usePopupZIndex } from '../../../../formMgt/popupStack';
import {
  ApiParamMappingRow,
  buildFromDataModelToApiMappingState,
  buildIntegrationApiOperationDict,
  buildSourceTransactionFieldLookups,
  prepareFromDataModelToApiSavePayload,
  TransactionFieldLookup,
} from './dataTransferFromDataModelToApiHelpers';

const YES_LOOKUP = [{ Id: 1, Display: 'Yes' }];

function payloadRowEditable(rowData: any): boolean {
  return rowData && !rowData.IsArray && !rowData.IsObject;
}

export function DataTransferFromDataModelToApiEmbeddedEditor(props: {
  settingId: number;
  srcTransactionId: number;
  onMarkChange: () => void;
  onRegisterSave: (saveFn: ((callback?: () => void) => Promise<void>) | null) => void;
}) {
  const { settingId, srcTransactionId, onMarkChange, onRegisterSave } = props;
  const { theme } = useTheme();
  const { showError, showValidationMessages } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();

  const [loading, setLoading] = useState(true);
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const [settingData, setSettingData] = useState<any>(null);
  const [srcTransactionData, setSrcTransactionData] = useState<any>(null);
  const [allSrcFields, setAllSrcFields] = useState<TransactionFieldLookup[]>([]);
  const [unitList, setUnitList] = useState<any[]>([]);
  const [filterByUnitId, setFilterByUnitId] = useState<number | null>(null);
  const [targetApiParamMappingList, setTargetApiParamMappingList] = useState<ApiParamMappingRow[]>([]);
  const [payloadRoots, setPayloadRoots] = useState<any[] | null>(null);
  const [responseRoots, setResponseRoots] = useState<any[] | null>(null);
  const [hasSrcApiInputParams, setHasSrcApiInputParams] = useState(false);
  const [integrationSettingList, setIntegrationSettingList] = useState<any[]>([]);
  const [dictApiOperationIdAndDto, setDictApiOperationIdAndDto] = useState<Record<number, any>>({});

  const [showApiOpDropdown, setShowApiOpDropdown] = useState(false);
  const [apiOpDropdownPos, setApiOpDropdownPos] = useState<{ top: number; left: number } | null>(null);
  const [hoveredIntegrationId, setHoveredIntegrationId] = useState<number | null>(null);
  const apiOpTriggerRef = useRef<HTMLButtonElement>(null);
  const apiOpDropdownZIndex = usePopupZIndex();

  const targetApiParamMappingCV = useMemo(
    () => new CollectionView<ApiParamMappingRow>([]),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [settingId],
  );
  const payloadCV = useMemo(() => new CollectionView<any>([]), [settingId]);
  const responseCV = useMemo(() => new CollectionView<any>([]), [settingId]);

  const filteredSrcFields = useMemo(() => {
    if (!filterByUnitId) return allSrcFields;
    return allSrcFields.filter((f) => f.UnitId === filterByUnitId);
  }, [allSrcFields, filterByUnitId]);

  const sourceFieldDataMap = useMemo(() => {
    const sorted = [...filteredSrcFields].sort((a, b) => a.Display2.localeCompare(b.Display2));
    return new DataMap(sorted, 'Id', 'Display2');
  }, [filteredSrcFields]);

  const srcApiParamDataMap = useMemo(() => {
    const list = (srcTransactionData?.ApiInputParameterList || []).map((p: string) => ({ Id: p, Display: p }));
    return list.length ? new DataMap(list, 'Id', 'Display') : null;
  }, [srcTransactionData]);

  const yesOrEmptyDataMap = useMemo(() => new DataMap(YES_LOOKUP, 'Id', 'Display'), []);

  const applyMappingState = useCallback((setting: any, srcData: any) => {
    const built = buildFromDataModelToApiMappingState(setting, srcData);
    setTargetApiParamMappingList(built.targetApiParamMappingList);
    targetApiParamMappingCV.sourceCollection = built.targetApiParamMappingList;
    targetApiParamMappingCV.refresh();
    setPayloadRoots(built.payloadRoots);
    setResponseRoots(built.responseRoots);
    setHasSrcApiInputParams(built.hasSrcApiInputParams);
    if (built.payloadRoots) {
      payloadCV.sourceCollection = built.payloadRoots;
      payloadCV.refresh();
    }
    if (built.responseRoots) {
      responseCV.sourceCollection = built.responseRoots;
      responseCV.refresh();
    }
  }, [payloadCV, responseCV, targetApiParamMappingCV]);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const [setting, srcData, integrations] = await Promise.all([
        appTransactionService.retrieveOneAppTransactionDataTransferSettingExDto(settingId),
        appTransactionService.getOneAppTransactionData(String(srcTransactionId)),
        integrationService.retrieveAllAppIntegrationSettingDto(false),
      ]);
      const integrationList = Array.isArray(integrations) ? integrations : [];
      setIntegrationSettingList(integrationList);
      setDictApiOperationIdAndDto(buildIntegrationApiOperationDict(integrationList));
      setSettingData(setting);
      setSrcTransactionData(srcData);
      const { allSrcTransactionFieldList, unitList: units } = buildSourceTransactionFieldLookups(srcData);
      setAllSrcFields(allSrcTransactionFieldList);
      setUnitList(units);
      setFilterByUnitId(null);
      if (setting && srcData) {
        applyMappingState(setting, srcData);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load API call mapping');
    } finally {
      setLoading(false);
    }
  }, [applyMappingState, settingId, showError, srcTransactionId]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  const handleCellEditEnded = useCallback(() => {
    if (settingData) {
      settingData.IsModified = true;
    }
    onMarkChange();
  }, [onMarkChange, settingData]);

  const saveEmbedded = useCallback(
    async (callback?: () => void) => {
      if (!settingData) {
        callback?.();
        return;
      }
      try {
        const payload = prepareFromDataModelToApiSavePayload(
          settingData,
          srcTransactionId,
          targetApiParamMappingList,
          payloadRoots,
          responseRoots,
        );
        const result = await appTransactionService.saveOneAppTransactionDataTransferSettingExDto(payload);
        if (result?.ValidationResult) {
          showValidationMessages(result.ValidationResult);
        }
        if (result?.IsSuccessful) {
          await loadAll();
          callback?.();
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to save API call mapping');
      }
    },
    [
      loadAll,
      payloadRoots,
      responseRoots,
      settingData,
      showError,
      showValidationMessages,
      srcTransactionId,
      targetApiParamMappingList,
    ],
  );

  useEffect(() => {
    onRegisterSave(saveEmbedded);
    return () => onRegisterSave(null);
  }, [onRegisterSave, saveEmbedded]);

  const handleSelectApiOperation = useCallback(
    async (apiOperationId: number) => {
      if (!settingData || !apiOperationId) return;
      settingData.InternalCode = apiOperationId;
      settingData.IsModified = true;
      onMarkChange();
      setShowApiOpDropdown(false);
      setApiOpDropdownPos(null);
      await saveEmbedded();
    },
    [onMarkChange, saveEmbedded, settingData],
  );

  const handleEditApiOperation = useCallback(() => {
    const opId = settingData?.InternalCode;
    if (!opId) return;
    const dto = dictApiOperationIdAndDto[opId];
    const label = dto?.actionDisplay || `API Operation ${opId}`;
    addTabAndNavigate('third-party-api-editor', label, { Id: opId }, true);
  }, [addTabAndNavigate, dictApiOperationIdAndDto, settingData?.InternalCode]);

  const payloadItemFormatter = useCallback((panel: any, r: number, _c: number, _cell: HTMLElement) => {
    if (panel?.cellType !== 1) return;
    const rowData = panel.rows?.[r]?.dataItem;
    if (rowData && panel.rows?.[r]) {
      panel.rows[r].isReadOnly = !payloadRowEditable(rowData);
    }
  }, []);

  const responseItemFormatter = useCallback((panel: any, r: number, _c: number, _cell: HTMLElement) => {
    if (panel?.cellType !== 1) return;
    if (panel.rows?.[r]) {
      panel.rows[r].isReadOnly = false;
    }
  }, []);

  const apiOpDisplay =
    settingData?.InternalCode != null ? dictApiOperationIdAndDto[settingData.InternalCode]?.actionDisplay : '';

  if (loading) {
    return <p className={`text-xs py-2 ${theme.label}`}>Loading API call mapping…</p>;
  }

  return (
    <div className="w-full flex flex-col gap-2 min-h-[320px]">
      {/* Angular: _PropertyHeader_CallFromCommand.cshtml */}
      <div className="grid grid-cols-[8.5rem_1fr] items-center gap-2">
        <label className={`text-xs ${theme.label}`}>API Operation</label>
        <div className={`h-7 flex border rounded-[4px] overflow-hidden ${theme.inputBox}`}>
          <div className="w-1 flex-auto min-w-0 h-full px-2 text-xs flex items-center truncate">
            {apiOpDisplay ? (
              <button type="button" className="underline truncate text-left" title={apiOpDisplay} onClick={handleEditApiOperation}>
                <i className="fa-solid fa-pencil mr-1" aria-hidden />
                {apiOpDisplay}
              </button>
            ) : (
              <span className="opacity-60">—</span>
            )}
          </div>
          <button
            ref={apiOpTriggerRef}
            type="button"
            title="Select API Operation"
            className={`h-full w-7 shrink-0 border-l flex items-center justify-center ${theme.inputBox}`}
            onClick={() => {
              if (!showApiOpDropdown) {
                const rect = apiOpTriggerRef.current?.getBoundingClientRect();
                if (rect) setApiOpDropdownPos({ top: rect.bottom + 4, left: rect.left });
              } else {
                setApiOpDropdownPos(null);
              }
              setShowApiOpDropdown((v) => !v);
            }}
          >
            <i className="fa-solid fa-chevron-down text-xs" />
          </button>
        </div>
      </div>

      {showApiOpDropdown &&
        apiOpDropdownPos &&
        createPortal(
          <PopupStackLayer zIndex={apiOpDropdownZIndex}>
            <div
              className={`fixed flex flex-row items-start ${theme.mainContentSection}`}
              style={{ zIndex: apiOpDropdownZIndex, top: apiOpDropdownPos.top, left: apiOpDropdownPos.left }}
              onMouseLeave={() => setHoveredIntegrationId(null)}
            >
            <div className={`min-w-[200px] max-h-96 overflow-y-auto border rounded-l shadow-lg py-1 ${theme.mainContentSection}`}>
              <div className="px-3 py-1 text-xs font-semibold border-b">Select Provider:</div>
              {integrationSettingList.map((s: any) => (
                <div
                  key={s.Id}
                  className={`px-3 py-2 text-xs flex items-center justify-between cursor-default ${hoveredIntegrationId === s.Id ? (theme.tab_active ?? 'bg-gray-100') : ''}`}
                  onMouseEnter={() => setHoveredIntegrationId(s.Id)}
                >
                  <span className="truncate">{s.Name || 'Unnamed'}</span>
                  <i className="fa-solid fa-chevron-right text-xs ml-1 shrink-0" />
                </div>
              ))}
            </div>
            {hoveredIntegrationId != null && (() => {
              const setting = integrationSettingList.find((s: any) => s.Id === hoveredIntegrationId);
              const ops =
                setting?.AppIntergrationSettingParameterList || setting?.AppIntegrationSettingParameterList || [];
              return (
                <div className={`min-w-[240px] max-w-[320px] max-h-96 overflow-y-auto border border-l-0 rounded-r shadow-lg py-1 ${theme.mainContentSection}`}>
                  <div className="px-3 py-1 text-xs font-semibold">Select Operation:</div>
                  {ops.map((p: any) => (
                    <button
                      key={p.Id}
                      type="button"
                      className={`w-full px-3 py-2 text-left text-xs ${theme.contextMenu}`}
                      onClick={() => handleSelectApiOperation(p.Id)}
                    >
                      {p.ActionCode}
                    </button>
                  ))}
                </div>
              );
            })()}
            </div>
          </PopupStackLayer>,
          document.body,
        )}

      {/* Tabs */}
      <div className="flex border-b gap-0">
        {['Before Publish API Call Mapping', 'After Publish API Call Mapping'].map((label, idx) => (
          <button
            key={label}
            type="button"
            className={`px-4 py-2 text-xs border-b-2 -mb-px ${
              activeTabIndex === idx ? `border-current font-semibold ${theme.title}` : `border-transparent ${theme.label}`
            }`}
            onClick={() => setActiveTabIndex(idx)}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Direction labels + unit filter (Angular _FromDataModelToApiMapping.cshtml) */}
      <div className="flex flex-wrap items-center gap-4 text-xs">
        {activeTabIndex === 0 ? (
          <span className={theme.label}>
            Target ← Source
          </span>
        ) : (
          <span className={theme.label}>
            Source → Target
          </span>
        )}
        <div className="flex items-center gap-2 ml-auto">
          <label className={`${theme.label} shrink-0`}>
            <i className="fa-solid fa-filter mr-1" aria-hidden />
            Unit Filter:
          </label>
          <select
            className={`h-7 px-2 text-xs border rounded-[4px] w-48 ${theme.inputBox}`}
            value={filterByUnitId == null ? '' : String(filterByUnitId)}
            onChange={(e) => setFilterByUnitId(e.target.value ? Number(e.target.value) : null)}
          >
            <option value="">All units</option>
            {unitList.map((u: any) => (
              <option key={u.Id} value={String(u.Id)}>
                {u.UnitDisplayName}
              </option>
            ))}
          </select>
        </div>
      </div>

      {activeTabIndex === 0 ? (
        <div className="w-full flex flex-col gap-3 min-h-0">
          {targetApiParamMappingList.length > 0 ? (
            <div className="w-full h-[200px] shrink-0">
              <FlexGrid
                itemsSource={targetApiParamMappingCV}
                headersVisibility="Column"
                selectionMode="Row"
                className="w-full h-full"
                cellEditEnded={handleCellEditEnded}
              >
                <FlexGridColumn binding="targetApiParamName" header="Target API Input Parameter" width={400} isReadOnly />
                <FlexGridColumn
                  binding="SourceFiledId"
                  header="Mapping To Current Data Model Field"
                  width={400}
                  dataMap={sourceFieldDataMap}
                />
                <FlexGridColumn header="" binding="" width="*" isReadOnly />
              </FlexGrid>
            </div>
          ) : null}
          <div className={`w-full h-1 flex-auto min-h-[220px] ${targetApiParamMappingList.length > 0 ? '' : 'min-h-[280px]'}`}>
            <FlexGrid
              itemsSource={payloadCV}
              childItemsPath="Children"
              headersVisibility="Column"
              treeIndent={20}
              className="w-full h-full"
              itemFormatter={payloadItemFormatter}
              cellEditEnded={handleCellEditEnded}
            >
              <FlexGridColumn binding="Display" header="API Payload Json Structure" width={400} isReadOnly />
              <FlexGridColumn binding="AbsolutePath" header="Node Path" width={300} visible={false} isReadOnly />
              <FlexGridColumn
                binding="SourceFiledId"
                header="Mapping To Current Data Model Field"
                width={400}
                dataMap={sourceFieldDataMap}
              />
              {hasSrcApiInputParams && srcApiParamDataMap ? (
                <FlexGridColumn
                  binding="mapToInputParam"
                  header="Mapping To Data Model Input Parameter"
                  width={250}
                  dataMap={srcApiParamDataMap}
                />
              ) : null}
              <FlexGridColumn header="" binding="" width="*" isReadOnly />
            </FlexGrid>
          </div>
        </div>
      ) : (
        <div className="w-full h-1 flex-auto min-h-[280px]">
          <FlexGrid
            itemsSource={responseCV}
            childItemsPath="Children"
            headersVisibility="Column"
            treeIndent={20}
            className="w-full h-full"
            itemFormatter={responseItemFormatter}
            cellEditEnded={handleCellEditEnded}
          >
            <FlexGridColumn binding="Display" header="Response Json Structure" width={400} isReadOnly />
            <FlexGridColumn binding="AbsolutePath" header="Node Path" width={300} visible={false} isReadOnly />
            <FlexGridColumn
              binding="TargetFiledId"
              header="Mapping To Current Data Model Field"
              width={300}
              dataMap={sourceFieldDataMap}
            />
            <FlexGridColumn
              binding="SourceFiledId"
              header="Is Array Logic Key"
              width={200}
              dataMap={yesOrEmptyDataMap}
            />
            {hasSrcApiInputParams && srcApiParamDataMap ? (
              <FlexGridColumn
                binding="mapToInputParam"
                header="Mapping To Data Model Input Parameter"
                width={250}
                dataMap={srcApiParamDataMap}
              />
            ) : null}
            <FlexGridColumn header="" binding="" width="*" isReadOnly />
          </FlexGrid>
        </div>
      )}
    </div>
  );
}
