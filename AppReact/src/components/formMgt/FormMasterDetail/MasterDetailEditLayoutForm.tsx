import React, { useState, useEffect, useMemo, useRef, useCallback, useContext } from 'react';
import { createPortal } from 'react-dom';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumEntry, useEnumValues } from '../../../hooks/useEnumDictionary';
import MasterDetailFlexLayoutForm from './MasterDetailFlexLayoutForm';
import MasterDetailGridLayoutForm from './MasterDetailGridLayoutForm';
import MasterDetailFlowLayoutForm from './MasterDetailFlowLayoutForm';
import MasterDetailCanvasLayoutForm from './MasterDetailCanvasLayoutForm';
import TransactionHeaderItem from './TransactionHeaderItem';
import TransactionCrossHeaderItem from './TransactionCrossHeaderItem';
import FormMainMenus from './FormMainMenus';
import FormSidePanel from '../FormAgent/FormSidePanel';
import { FormAgentResultSlotContext, FormGroupAgentHostsContext, FormGroupDockSlotContext } from '../FormAgent/formGroupDock';
import {
  FormAgentHostContext,
  buildFormAgentContext,
  describeFormAction,
  type FormAgentHostSlots,
} from '../FormAgent/formAgentHost';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import {
  FormMasterDetailRuntimeConfigProvider,
  emptyFormMasterDetailRuntimeConfigApi,
  type FormMasterDetailRuntimeConfigApi,
} from './formMasterDetailRuntimeConfig';

interface MasterDetailEditLayoutFormProps {
  controllerModel: any;
  dataModel: any;
  formStructureData: any;
  transactionExDto?: any;
  onDataModelChange: (dataModel: any) => void;
  onRefresh?: () => void;
  onSave?: (savedFormData: any) => void;
  onApplyWorkflowCommandFormData?: (serverFormData: any) => void;
  onOpenWorkflowExecutionMonitor?: () => void;
  onReloadCurrentForm?: () => void;
  onToggleFormConfigButtons?: () => void;
  onFormActionApiReady?: (api: { save: () => Promise<void>; refresh: () => void; reload: () => void }) => void;
  additionalFormActionApis?: Array<{ save: () => Promise<void>; refresh: () => void; reload: () => void }>;
  additionalFormCanSave?: boolean;
  additionalFormIsDirty?: boolean;
  onTemplateHeaderContainerReady?: (element: HTMLElement | null) => void;
}

const MasterDetailEditLayoutForm: React.FC<MasterDetailEditLayoutFormProps> = ({
  controllerModel,
  dataModel,
  formStructureData,
  transactionExDto,
  onDataModelChange,
  onRefresh,
  onSave,
  onApplyWorkflowCommandFormData,
  onOpenWorkflowExecutionMonitor,
  onReloadCurrentForm,
  onToggleFormConfigButtons,
  onFormActionApiReady,
  additionalFormActionApis,
  additionalFormCanSave,
  additionalFormIsDirty,
  onTemplateHeaderContainerReady,
}) => {
  const { theme, t } = useTheme();
  const formGroupDockEl = useContext(FormGroupDockSlotContext);
  const formGroupAgentHostsRef = useContext(FormGroupAgentHostsContext);
  const [resultSlotEl, setResultSlotEl] = useState<HTMLDivElement | null>(null);
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());
  const [isMobileMenuVisible, setIsMobileMenuVisible] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const mainContainerRef = useRef<HTMLDivElement>(null);
  const collapseSeededRef = useRef(false);

  const [runtimeFieldConfigApi, setRuntimeFieldConfigApi] = useState<FormMasterDetailRuntimeConfigApi | null>(null);
  const handleRuntimeConfigApiReady = useCallback((api: FormMasterDetailRuntimeConfigApi) => {
    setRuntimeFieldConfigApi(api);
  }, []);
  
  // All hooks must be called before any conditional returns
  const layoutTypeEnum = useEnumValues('EmAppFormLayoutType');
  const flexLayoutType = layoutTypeEnum?.Flex ?? 4;
  const emMessageScopeTypeTransaction = useEnumEntry('EmAppMessgaeScopeType', 'Transaction') ?? 2;
  const emDockBottom = useEnumEntry('EmAppDockPosition', 'Bottom');
  const emDockRight = useEnumEntry('EmAppDockPosition', 'Right');
  const templateHeaderContainerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!onTemplateHeaderContainerReady) return;
    onTemplateHeaderContainerReady(templateHeaderContainerRef.current);
    return () => onTemplateHeaderContainerReady(null);
  }, [onTemplateHeaderContainerReady, formStructureData, transactionExDto]);

  // Template header "Collapsed" visibility (EmAppTemplateHeaderVisibility.Collapsed = 3):
  // start collapsed once, then let the user toggle freely.
  useEffect(() => {
    if (collapseSeededRef.current) return;
    if (!controllerModel.isTemplateHeader) return;
    if (Number(controllerModel.param2Obj?.templateHeaderVisibility) !== 3) return;
    collapseSeededRef.current = true;
    const sectionId = `MasterDetailEditLayoutForm${controllerModel.uiId}`;
    setCollapsedSections((prev) => {
      if (prev.has(sectionId)) return prev;
      const next = new Set(prev);
      next.add(sectionId);
      return next;
    });
  }, [controllerModel.isTemplateHeader, controllerModel.param2Obj, controllerModel.uiId]);

  // Detect mobile on mount and window resize
  useEffect(() => {
    const checkMobile = () => {
      setIsMobile(window.innerWidth < 1024); // DesignWidthSM
    };
    
    // Check on mount
    checkMobile();
    
    // Listen for resize events
    window.addEventListener('resize', checkMobile);
    
    return () => {
      window.removeEventListener('resize', checkMobile);
    };
  }, []);

  // Get form layout type (null => Flex, matches backend RetrieveTransactionAppFormExDto)
  const formLayoutType =
    transactionExDto?.ForeignAppFormExDto?.LayoutType ??
    formStructureData?.ForeignAppFormExDto?.LayoutType ??
    formStructureData?.LayoutType ??
    flexLayoutType;

  // Get transaction data from props (transactionExDtoRef) or formStructureData
  // Note: Do not use dataModel.transactionExDto as dataModel gets reset during refresh
  const transactionDto = transactionExDto || formStructureData;
  const transactionHeader = transactionDto?.TransactionHeader || [];
  const transactionCrossHeader = transactionDto?.TransactionCrossHeader || [];
  const runtimeTransactionId = String(transactionDto?.Id ?? transactionDto?.TransactionId ?? controllerModel?.transactionId ?? '');
  const runtimeTransactionRootValueId = String(
    transactionDto?.RootPrimaryKeyValue ??
      transactionDto?.TransactionRootValueId ??
      controllerModel?.rootPrimaryKeyValue ??
      ''
  );
  const conversationDockPos: number | null =
    typeof transactionDto?.ConversationBoxDockPosition === 'number'
      ? transactionDto.ConversationBoxDockPosition
      : transactionDto?.ConversationBoxDockPosition
        ? parseInt(String(transactionDto.ConversationBoxDockPosition), 10)
        : null;

  // Spec (Angular): conversation can dock Right or Bottom; default dock is Right when not set.
  const isConversationEnabled = Boolean(transactionDto?.IsNeedToSetComunication);
  const isAiEnabled = Boolean(transactionDto?.OtherOptions?.IsEnableTransactionAiAgent);
  const isSidePanelEnabled = isConversationEnabled || isAiEnabled;
  const aiDockPos: number | null =
    typeof transactionDto?.OtherOptions?.AiAgentDockPosition === 'number'
      ? transactionDto.OtherOptions.AiAgentDockPosition
      : transactionDto?.OtherOptions?.AiAgentDockPosition
        ? parseInt(String(transactionDto.OtherOptions.AiAgentDockPosition), 10)
        : null;
  const sideDockPos = isConversationEnabled ? conversationDockPos : aiDockPos;
  const dockBottomValue = emDockBottom ?? 3; // backend enum: Bottom=3
  const dockRightValue = emDockRight ?? 2; // backend enum: Right=2

  const isNeedToShowBottomConversation = isSidePanelEnabled && sideDockPos === dockBottomValue;
  const isNeedToShowRightConversation =
    isSidePanelEnabled && !isNeedToShowBottomConversation && (sideDockPos === null || sideDockPos === dockRightValue);

  // Right-side conversation (Angular parity: collapsible + resizable + open-by-default)
  const openConversationByDefault = Boolean(
    (isConversationEnabled && (transactionDto?.OtherOptions?.IsOpenCommunicationByDefault ?? transactionDto?.IsOpenCommunicationByDefault))
    || (isAiEnabled && transactionDto?.OtherOptions?.IsOpenAiAgentByDefault)
  );
  const hostTransaction = transactionExDto || transactionDto || dataModel?.currentFormStructure;
  const hostSlotsRef = useRef<FormAgentHostSlots>({});
  hostSlotsRef.current.getContext = () => {
    const catalog = buildFormAgentContext({
      mode: 'master',
      transaction: hostTransaction,
      formData: dataModel?.currentFormData,
    }) as object;
    if (!controllerModel.isTemplateHeader) return catalog;
    return { formRole: 'header', ...catalog };
  };
  hostSlotsRef.current.applyTool = async (toolName, args) => {
    const result = await appTransactionService.applyFormAgentTool({
      ToolName: toolName,
      Mode: 'master',
      FormData: dataModel?.currentFormData,
      Args: args || {},
    });
    const obj = result?.Object ?? result;
    const toolResult = obj?.ToolResult ?? { ok: false, error: 'Empty apply result.' };
    if (obj?.FormData && toolName !== 'get_form_state') {
      onDataModelChange?.({ ...dataModel, currentFormData: { ...obj.FormData, IsDirty: true } });
    }
    return toolResult;
  };
  hostSlotsRef.current.setFields = (args) => hostSlotsRef.current.applyTool!('set_fields', args);
  hostSlotsRef.current.updateGrid = (args) => hostSlotsRef.current.applyTool!('update_grid', args);
  hostSlotsRef.current.describeAction = (args) => describeFormAction(transactionDto, args);
  const formGroupHostKey = `${controllerModel.isTemplateHeader ? 'header' : 'main'}:${runtimeTransactionId}:${controllerModel.uiId}`;
  useEffect(() => {
    if (!formGroupAgentHostsRef) return;
    formGroupAgentHostsRef.current.set(formGroupHostKey, {
      key: formGroupHostKey,
      role: controllerModel.isTemplateHeader ? 'header' : 'main',
      name: String(transactionDto?.TransactionName ?? controllerModel?.TemplateHeaderName ?? ''),
      slots: hostSlotsRef.current,
    });
    return () => {
      formGroupAgentHostsRef.current.delete(formGroupHostKey);
    };
  }, [formGroupAgentHostsRef, formGroupHostKey, controllerModel.isTemplateHeader, transactionDto?.TransactionName, controllerModel?.TemplateHeaderName]);
  const chatScope = runtimeTransactionRootValueId || `new:${controllerModel?.uiId || 'draft'}`;
  const [isBottomConversationExpanded, setIsBottomConversationExpanded] = useState(true);
  const [bottomConversationHeight, setBottomConversationHeight] = useState(280);
  const [isResizingBottomConversation, setIsResizingBottomConversation] = useState(false);
  const bottomConversationHeightRef = useRef(280);
  bottomConversationHeightRef.current = bottomConversationHeight;
  const renderSidePanel = (placement: 'right' | 'bottom' = 'right') => (
    <FormSidePanel
      showMessages={isConversationEnabled}
      showAgent={isAiEnabled}
      transactionId={runtimeTransactionId}
      messageRootId={runtimeTransactionRootValueId}
      messageScopeType={emMessageScopeTypeTransaction}
      chatScope={chatScope}
      initialTab={isAiEnabled && !isConversationEnabled ? 'ai' : 'messages'}
      dockPlacement={placement}
      onHidePanel={placement === 'bottom'
        ? () => setIsBottomConversationExpanded(false)
        : () => setIsRightConversationExpanded(false)}
    />
  );
  const rightConvStorageKeyBase = useMemo(
    () => `mdConvRight:${runtimeTransactionId || 'unknown'}`,
    [runtimeTransactionId]
  );
  const [isRightConversationExpanded, setIsRightConversationExpanded] = useState<boolean>(openConversationByDefault);
  const [rightConversationWidth, setRightConversationWidth] = useState<number>(420);
  const [isResizingRightConversation, setIsResizingRightConversation] = useState(false);
  const rightConversationWidthRef = useRef(rightConversationWidth);
  rightConversationWidthRef.current = rightConversationWidth;

  useEffect(() => {
    if (!isNeedToShowRightConversation) return;
    try {
      const savedExpanded = localStorage.getItem(`${rightConvStorageKeyBase}:expanded`);
      const savedWidth = localStorage.getItem(`${rightConvStorageKeyBase}:width`);
      setIsRightConversationExpanded(savedExpanded !== null ? savedExpanded === '1' : openConversationByDefault);
      const parsed = savedWidth ? parseInt(savedWidth, 10) : NaN;
      if (!Number.isNaN(parsed) && parsed >= 300) {
        setRightConversationWidth(Math.min(Math.max(parsed, 300), 900));
      } else {
        setRightConversationWidth(420);
      }
    } catch {
      setIsRightConversationExpanded(openConversationByDefault);
      setRightConversationWidth(420);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rightConvStorageKeyBase, isNeedToShowRightConversation]);

  useEffect(() => {
    if (!isNeedToShowRightConversation) return;
    try {
      localStorage.setItem(`${rightConvStorageKeyBase}:expanded`, isRightConversationExpanded ? '1' : '0');
      localStorage.setItem(`${rightConvStorageKeyBase}:width`, String(rightConversationWidth));
    } catch {
      // ignore storage errors
    }
  }, [isNeedToShowRightConversation, isRightConversationExpanded, rightConversationWidth, rightConvStorageKeyBase]);

  const bottomConvStorageKey = useMemo(
    () => `mdConvBottom:${runtimeTransactionId || 'unknown'}`,
    [runtimeTransactionId]
  );

  useEffect(() => {
    if (!isNeedToShowBottomConversation) return;
    try {
      const savedH = localStorage.getItem(`${bottomConvStorageKey}:height`);
      const parsed = savedH ? parseInt(savedH, 10) : NaN;
      if (!Number.isNaN(parsed) && parsed >= 160) {
        setBottomConversationHeight(Math.min(Math.max(parsed, 160), 700));
      }
    } catch {
      /* ignore */
    }
  }, [bottomConvStorageKey, isNeedToShowBottomConversation]);

  useEffect(() => {
    if (!isNeedToShowBottomConversation) return;
    try {
      localStorage.setItem(`${bottomConvStorageKey}:height`, String(bottomConversationHeight));
    } catch {
      /* ignore */
    }
  }, [bottomConversationHeight, bottomConvStorageKey, isNeedToShowBottomConversation]);

  const startResizeBottomConversation = useCallback((e: React.PointerEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    const handle = e.currentTarget;
    const startY = e.clientY;
    const startH = bottomConversationHeightRef.current;
    const host = mainContainerRef.current;
    const hostH = host?.getBoundingClientRect().height ?? window.innerHeight;
    const maxH = Math.max(160, Math.min(700, hostH - 120));
    setIsResizingBottomConversation(true);
    try { handle.setPointerCapture(e.pointerId); } catch { /* ignore */ }

    const onMove = (ev: PointerEvent) => {
      const next = startH + (startY - ev.clientY);
      setBottomConversationHeight(Math.min(Math.max(next, 160), maxH));
    };
    const onUp = () => {
      setIsResizingBottomConversation(false);
      try { handle.releasePointerCapture(e.pointerId); } catch { /* ignore */ }
      handle.removeEventListener('pointermove', onMove);
      handle.removeEventListener('pointerup', onUp);
      handle.removeEventListener('pointercancel', onUp);
    };
    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onUp);
  }, []);

  const startResizeRightConversation = useCallback((e: React.PointerEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    const handle = e.currentTarget;
    const startX = e.clientX;
    const startW = rightConversationWidthRef.current;
    const row = handle.closest('.flex-row') as HTMLElement | null;
    const rowWidth = row?.getBoundingClientRect().width ?? window.innerWidth;
    const maxWidth = Math.max(300, Math.min(900, rowWidth - 180));
    setIsResizingRightConversation(true);
    try { handle.setPointerCapture(e.pointerId); } catch { /* ignore */ }

    const onMove = (ev: PointerEvent) => {
      const next = startW + (startX - ev.clientX);
      setRightConversationWidth(Math.min(Math.max(next, 300), maxWidth));
    };
    const onUp = () => {
      setIsResizingRightConversation(false);
      try { handle.releasePointerCapture(e.pointerId); } catch { /* ignore */ }
      handle.removeEventListener('pointermove', onMove);
      handle.removeEventListener('pointerup', onUp);
      handle.removeEventListener('pointercancel', onUp);
    };
    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onUp);
  }, []);
  
  // If enum is not loaded yet, return loading state
  if (!layoutTypeEnum) {
    return (
      <div className="p-4 text-gray-500">
        Loading enum values...
      </div>
    );
  }

  // Toggle section collapse
  const toggleSection = (sectionId: string) => {
    setCollapsedSections(prev => {
      const newSet = new Set(prev);
      if (newSet.has(sectionId)) {
        newSet.delete(sectionId);
      } else {
        newSet.add(sectionId);
      }
      return newSet;
    });
  };

  const isSectionCollapsed = (sectionId: string) => {
    return collapsedSections.has(sectionId);
  };

  // Render based on layout type
  const renderLayout = () => {
    if (!formStructureData && !dataModel.currentFormStructure) {
      return (
        <div className="p-4 text-gray-500">
          Loading form structure...
        </div>
      );
    }

    const structure = formStructureData || dataModel.currentFormStructure;

    // Use Flex as fallback for Grid (MasterDetailGridLayoutForm not yet implemented)
    const effectiveLayoutType = formLayoutType === layoutTypeEnum.Grid ? layoutTypeEnum.Flex : formLayoutType;

    switch (effectiveLayoutType) {
      case layoutTypeEnum.Flex:
        return (
          <MasterDetailFlexLayoutForm
            controllerModel={controllerModel}
            dataModel={dataModel}
            formStructureData={structure}
            transactionExDto={transactionExDto}
            onDataModelChange={onDataModelChange}
          />
        );

      case layoutTypeEnum.Grid:
        return (
          <MasterDetailGridLayoutForm
            controllerModel={controllerModel}
            dataModel={dataModel}
            formStructureData={structure}
            onDataModelChange={onDataModelChange}
          />
        );

      case layoutTypeEnum.Flow:
        return (
          <MasterDetailFlowLayoutForm
            controllerModel={controllerModel}
            dataModel={dataModel}
            formStructureData={structure}
            onDataModelChange={onDataModelChange}
          />
        );

      case layoutTypeEnum.Canvas:
        return (
          <MasterDetailCanvasLayoutForm
            controllerModel={controllerModel}
            dataModel={dataModel}
            formStructureData={structure}
            onDataModelChange={onDataModelChange}
          />
        );

      default:
        // Default to Flex layout when form not designed yet or layout type unknown (project standard)
        return (
          <MasterDetailFlexLayoutForm
            controllerModel={controllerModel}
            dataModel={dataModel}
            formStructureData={structure}
            transactionExDto={transactionExDto}
            onDataModelChange={onDataModelChange}
          />
        );
    }
  };

  // Template header section (if applicable)
  const renderTemplateHeader = () => {
    if (!controllerModel.isTemplateHeader) return null;

    const sectionId = `MasterDetailEditLayoutForm${controllerModel.uiId}`;
    const collapsed = isSectionCollapsed(sectionId);

    return (
      <div className="w-full p-1">
        <div
          className={`${theme.mainContentSection} cursor-pointer flex items-center justify-between p-2 rounded`}
          onClick={() => toggleSection(sectionId)}
        >
          <span className="font-semibold">{controllerModel.TemplateHeaderName}</span>
          <div className="flex items-center">
            <i className={`fa fa-chevron-circle-${collapsed ? 'down' : 'up'}`}></i>
          </div>
        </div>
      </div>
    );
  };


  // Template header collapse state: when this form is a group template header ("Style Header"),
  // clicking the header band collapses/expands its body.
  const templateHeaderSectionId = `MasterDetailEditLayoutForm${controllerModel.uiId}`;
  const isTemplateHeaderCollapsed = controllerModel.isTemplateHeader && isSectionCollapsed(templateHeaderSectionId);

  // Desktop layout
  if (!isMobile) {
    // Show menu: not in preview; for file property show minimal menu (Refresh, Save); for other embedded
    // (isHideHeaderAndFooter / template header) hide. Template headers always use the main form toolbar.
    const hideEmbeddedMenu =
      Boolean(controllerModel.param2Obj?.isHideHeaderAndFooter) ||
      Boolean(controllerModel.isTemplateHeader);
    const menuVisible =
      !controllerModel.isPreview &&
      (controllerModel.isFilePropertyEdit || !hideEmbeddedMenu);
    return (
      <FormMasterDetailRuntimeConfigProvider
        value={runtimeFieldConfigApi ?? emptyFormMasterDetailRuntimeConfigApi}
      >
      <FormAgentHostContext.Provider value={hostSlotsRef}>
      <FormAgentResultSlotContext.Provider value={resultSlotEl}>
      <div className="w-full h-full flex flex-row min-h-0">
      <div ref={setResultSlotEl} className="relative w-1 flex-auto h-full min-w-0 flex flex-col">
        {/* Menu Bar - Desktop only - Hide in preview, file property edit, or isHideHeaderAndFooter (Angular: directivOutercontrol.isHideMasterDetailFormTopMenu) */}
        {menuVisible && (
          <div className={`h-10 flex-shrink-0 border-b ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
            <div className="h-full">
              <FormMainMenus
                controllerModel={controllerModel}
                dataModel={dataModel}
                formStructureData={formStructureData}
                transactionExDto={transactionExDto}
                onRefresh={onRefresh}
                onReloadCurrentForm={onReloadCurrentForm}
                onSave={onSave}
                onApplyWorkflowCommandFormData={onApplyWorkflowCommandFormData}
                onOpenWorkflowExecutionMonitor={onOpenWorkflowExecutionMonitor}
                onFormActionApiReady={onFormActionApiReady}
                additionalFormActionApis={additionalFormActionApis}
                additionalFormCanSave={additionalFormCanSave}
                additionalFormIsDirty={additionalFormIsDirty}
                onDataModelChange={onDataModelChange}
                onToggleFormConfigButtons={onToggleFormConfigButtons}
                onRuntimeConfigApiReady={handleRuntimeConfigApiReady}
              />
            </div>
          </div>
        )}

        {/* In embedded mode we often hide the menu; still mount it (hidden) so parent toolbars can call Save/Refresh. */}
        {!menuVisible && onFormActionApiReady && (
          <div className="hidden">
            <FormMainMenus
              controllerModel={controllerModel}
              dataModel={dataModel}
              formStructureData={formStructureData}
              transactionExDto={transactionExDto}
              onRefresh={onRefresh}
              onReloadCurrentForm={onReloadCurrentForm}
              onSave={onSave}
              onApplyWorkflowCommandFormData={onApplyWorkflowCommandFormData}
              onOpenWorkflowExecutionMonitor={onOpenWorkflowExecutionMonitor}
              onFormActionApiReady={onFormActionApiReady}
              additionalFormActionApis={additionalFormActionApis}
              additionalFormCanSave={additionalFormCanSave}
              additionalFormIsDirty={additionalFormIsDirty}
              onDataModelChange={onDataModelChange}
              onToggleFormConfigButtons={onToggleFormConfigButtons}
              onRuntimeConfigApiReady={handleRuntimeConfigApiReady}
            />
          </div>
        )}

        {/* Template Header */}
        {renderTemplateHeader()}

        {/* Main Container - full height when menu hidden; hidden when template header collapsed */}
        {!isTemplateHeaderCollapsed && (
        <div
          ref={mainContainerRef}
          id={`MasterDetailEditLayoutForm${controllerModel.uiId}`}
          className={`w-full h-1 flex-auto p-1 min-h-0 flex flex-col ${theme.mainContentSection}`}
          style={{ height: menuVisible ? 'calc(100% - 40px)' : '100%' }}
        >
          <div className="w-full h-1 flex-auto min-h-0 flex flex-col">
            {/* Main scroll area */}
            <div className="w-full min-w-0 h-1 flex-auto overflow-auto">
              {/* Template Header Container (for dynamic content) */}
              <div
                id={`TemplateHeaderContainer${controllerModel.uiId}`}
                ref={templateHeaderContainerRef}
                className="w-full"
              />

              {/* Transaction Headers - stable key so typing in form does not remount (no Date.now()) */}
              {transactionHeader.length > 0 && transactionHeader.map((headerDto: any, index: number) => {
                const transUiId = `transHeader_${controllerModel.uiId}_${index}_${headerDto.Id ?? index}`;
                return (
                  <TransactionHeaderItem
                    key={transUiId}
                    transactionDto={headerDto}
                    uiId={transUiId}
                    controllerModel={controllerModel}
                    dataModel={dataModel}
                    onDataModelChange={onDataModelChange}
                  />
                );
              })}

              {/* Transaction Cross Headers - stable key so typing in form does not remount (no Date.now()) */}
              {transactionCrossHeader.length > 0 && transactionCrossHeader.map((crossHeaderDto: any, index: number) => {
                const transUiId = `transCrossHeader_${controllerModel.uiId}_${index}_${crossHeaderDto.Id ?? index}`;
                return (
                  <TransactionCrossHeaderItem
                    key={transUiId}
                    transactionDto={crossHeaderDto}
                    uiId={transUiId}
                    controllerModel={controllerModel}
                    dataModel={dataModel}
                    onDataModelChange={onDataModelChange}
                  />
                );
              })}

              {/* Main Transaction Container */}
              <div id="mainTransactionContainer" className="w-full">
                {renderLayout()}
              </div>

            </div>
            {isNeedToShowBottomConversation && !controllerModel.isTemplateHeader && isBottomConversationExpanded && (
              <div
                id="BottomConversationContainer"
                className={`relative shrink-0 w-full flex flex-col overflow-hidden border-t ${t('border_mainContentSection')}`}
                style={{ height: `${bottomConversationHeight}px`, minHeight: 160 }}
              >
                <div
                  role="separator"
                  aria-orientation="horizontal"
                  aria-label="Drag to resize bottom panel"
                  title="Drag to resize"
                  onPointerDown={startResizeBottomConversation}
                  className="group absolute left-0 right-0 top-0 z-20 flex h-1.5 cursor-row-resize items-center justify-center select-none touch-none"
                >
                  <div
                    className={`pointer-events-none absolute inset-x-0 top-1/2 h-3 -translate-y-1/2 rounded-sm transition-opacity ${
                      isResizingBottomConversation
                        ? `opacity-100 ${t('bg_menu_default')}`
                        : `opacity-0 group-hover:opacity-100 ${t('bg_menu_default')}`
                    }`}
                    aria-hidden
                  />
                  <div
                    className={`pointer-events-none w-[min(200px,45vw)] h-2 rounded-full border ${t('border_mainContentSection')} ${theme.menu_default} shadow-sm transition-[opacity,box-shadow] group-hover:border-2 group-hover:shadow-md ${
                      isResizingBottomConversation
                        ? 'border-2 opacity-100 shadow-md'
                        : 'opacity-80 group-hover:opacity-100'
                    }`}
                    aria-hidden
                  />
                </div>
                {renderSidePanel('bottom')}
              </div>
            )}
            {isNeedToShowBottomConversation && !controllerModel.isTemplateHeader && !isBottomConversationExpanded && (
              <button
                type="button"
                className={`shrink-0 w-full h-8 flex items-center justify-between px-2 text-xs border-t ${t('border_mainContentSection')} ${theme.mainHeader}`}
                onClick={() => setIsBottomConversationExpanded(true)}
                title="Expand panel"
                aria-label="Expand panel"
              >
                <span>{isConversationEnabled && isAiEnabled ? 'Conversation / Assistant' : isAiEnabled ? 'Assistant' : 'Conversation'}</span>
                <i className="fa-solid fa-chevron-up" aria-hidden />
              </button>
            )}
          </div>
        </div>
        )}
      </div>

            {/* Right dock. Inside a form group it is portaled up beside the record navigation bar. */}
            {(() => {
              const dock = isNeedToShowRightConversation && !controllerModel.isTemplateHeader && !isTemplateHeaderCollapsed ? (
              <>
                {isRightConversationExpanded ? (
                  <div
                    id="RightBox"
                    className={`shrink-0 h-full overflow-hidden border-l ${t('border_mainContentSection')} relative`}
                    style={{ width: `${rightConversationWidth}px`, minWidth: 300 }}
                  >
                    {/* Splitter: wide hit area + visible rail (common IDE / split-pane affordance) */}
                    <div
                      role="separator"
                      aria-orientation="vertical"
                      aria-label="Drag to resize conversation panel"
                      title="Drag to resize"
                      onPointerDown={startResizeRightConversation}
                      className="group absolute left-0 top-0 bottom-0 z-20 flex w-1.5 cursor-col-resize items-center justify-center select-none touch-none"
                    >
                      {/* Track: subtle strip on hover / while dragging */}
                      <div
                        className={`pointer-events-none absolute inset-y-0 left-1/2 w-3 -translate-x-1/2 rounded-sm transition-opacity ${
                          isResizingRightConversation
                            ? `opacity-100 ${t('bg_menu_default')}`
                            : `opacity-0 group-hover:opacity-100 ${t('bg_menu_default')}`
                        }`}
                        aria-hidden
                      />
                      {/* Always-visible grip rail */}
                      <div
                        className={`pointer-events-none h-[min(200px,45vh)] w-2 rounded-full border ${t('border_mainContentSection')} ${theme.menu_default} shadow-sm transition-[opacity,box-shadow] group-hover:border-2 group-hover:shadow-md ${
                          isResizingRightConversation
                            ? 'border-2 opacity-100 shadow-md'
                            : 'opacity-80 group-hover:opacity-100'
                        }`}
                        aria-hidden
                      />
                    </div>
                    <div className="flex h-full min-h-0 w-full flex-col overflow-hidden">
                      <div id="RightConversationContainer" className="flex h-full min-h-0 w-full flex-col">
                        {renderSidePanel()}
                      </div>
                    </div>
                  </div>
                ) : (
                <div className={`shrink-0 h-full w-[27px] border-l ${t('border_mainContentSection')} px-[1px] py-[1px]`}>
                  <button
                    type="button"
                    className={`w-[25px] px-1 py-2 text-xs rounded-[4px] ${theme.button_default}`}
                    style={{ writingMode: 'vertical-rl', height: '9rem' }}
                    onClick={() => setIsRightConversationExpanded(true)}
                    title="Show panel"
                    aria-label="Show panel"
                  >
                    Show panel <i className="fa-solid fa-angle-double-left" aria-hidden="true" />
                  </button>
                </div>
                )}
              </>
            ) : null;
              if (!dock) return null;
              return formGroupDockEl ? createPortal(dock, formGroupDockEl) : dock;
            })()}
      </div>
      </FormAgentResultSlotContext.Provider>
      </FormAgentHostContext.Provider>
      </FormMasterDetailRuntimeConfigProvider>
    );
  }

  // Mobile layout
  return (
    <FormMasterDetailRuntimeConfigProvider
      value={runtimeFieldConfigApi ?? emptyFormMasterDetailRuntimeConfigApi}
    >
    <FormAgentHostContext.Provider value={hostSlotsRef}>
    <div
      id={`MasterDetailEditLayoutForm${controllerModel.uiId}`}
      className="w-full h-full overflow-x-hidden"
      style={{ position: 'relative' }}
    >
      {/* Template Header */}
      {renderTemplateHeader()}

      {/* Main Content - hidden when template header collapsed */}
      {!isTemplateHeaderCollapsed && (
      <div className="w-full h-full p-1 pb-52 overflow-auto overflow-x-hidden">
        {/* Template Header Container */}
        <div
          id={`TemplateHeaderContainer${controllerModel.uiId}`}
          ref={templateHeaderContainerRef}
          className="w-full"
        />

        {/* Main Transaction Container - Mobile */}
        <div id="mainTransactionContainer" className="w-full">
          {renderLayout()}
        </div>

        {isNeedToShowBottomConversation && (
          <div
            id="BottomConversationContainer"
            className={`w-full h-[240px] min-h-[160px] mt-1 flex flex-col overflow-hidden border-t ${t('border_mainContentSection')}`}
          >
            {renderSidePanel('bottom')}
          </div>
        )}
      </div>
      )}

      {/* Bottom Menu Button - Mobile - hidden when embedded (file property, template header, isHideHeaderAndFooter) */}
      {!controllerModel.isFilePropertyEdit &&
        !controllerModel.param2Obj?.isHideHeaderAndFooter &&
        !controllerModel.isTemplateHeader && (
        <>
      <div
        className="fixed bottom-4 right-4 w-12 h-12 bg-blue-500 text-white rounded-full flex items-center justify-center shadow-lg cursor-pointer hover:bg-blue-600 z-50 transition-all"
        onClick={() => setIsMobileMenuVisible(!isMobileMenuVisible)}
      >
        <i className={`fa ${isMobileMenuVisible ? 'fa-times-circle' : 'fa-ellipsis-h'}`}></i>
      </div>

      {/* Bottom Right Menu Bar - Mobile */}
      {isMobileMenuVisible && (
        <div
          id="formBottomRightMenuBar"
          className="fixed bottom-0 right-0 w-64 bg-white dark:bg-gray-800 shadow-2xl rounded-tl-lg p-4 z-40 max-h-96 overflow-auto"
          onClick={(e) => {
            // Don't close when clicking inside menu
            e.stopPropagation();
          }}
        >
          <FormMainMenus
            controllerModel={controllerModel}
            dataModel={dataModel}
            formStructureData={formStructureData}
            transactionExDto={transactionExDto}
            layoutVariant="mobilePopup"
            onRefresh={onRefresh}
            onReloadCurrentForm={onReloadCurrentForm}
            onSave={onSave}
            onApplyWorkflowCommandFormData={onApplyWorkflowCommandFormData}
            onOpenWorkflowExecutionMonitor={onOpenWorkflowExecutionMonitor}
            onFormActionApiReady={onFormActionApiReady}
            additionalFormActionApis={additionalFormActionApis}
            additionalFormCanSave={additionalFormCanSave}
            additionalFormIsDirty={additionalFormIsDirty}
            onDataModelChange={onDataModelChange}
            onToggleFormConfigButtons={onToggleFormConfigButtons}
            onRuntimeConfigApiReady={handleRuntimeConfigApiReady}
          />
        </div>
      )}

      {/* Overlay to close menu when clicking outside */}
      {isMobileMenuVisible && (
        <div
          className="fixed inset-0 bg-black bg-opacity-20 z-30"
          onClick={() => setIsMobileMenuVisible(false)}
        />
      )}
        </>
      )}
    </div>
    </FormAgentHostContext.Provider>
    </FormMasterDetailRuntimeConfigProvider>
  );
};

export default MasterDetailEditLayoutForm;

