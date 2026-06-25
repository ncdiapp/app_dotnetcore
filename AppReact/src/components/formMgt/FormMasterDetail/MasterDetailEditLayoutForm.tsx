import React, { useState, useEffect, useMemo, useRef, useCallback } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumEntry, useEnumValues } from '../../../hooks/useEnumDictionary';
import MasterDetailFlexLayoutForm from './MasterDetailFlexLayoutForm';
import MasterDetailGridLayoutForm from './MasterDetailGridLayoutForm';
import MasterDetailFlowLayoutForm from './MasterDetailFlowLayoutForm';
import MasterDetailCanvasLayoutForm from './MasterDetailCanvasLayoutForm';
import TransactionHeaderItem from './TransactionHeaderItem';
import TransactionCrossHeaderItem from './TransactionCrossHeaderItem';
import FormMainMenus from './FormMainMenus';
import ConversationEditorEmbedded from '../../message/ConversationEditorEmbedded';
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
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());
  const [isMobileMenuVisible, setIsMobileMenuVisible] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const mainContainerRef = useRef<HTMLDivElement>(null);

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
  const dockBottomValue = emDockBottom ?? 3; // backend enum: Bottom=3
  const dockRightValue = emDockRight ?? 2; // backend enum: Right=2

  const isNeedToShowBottomConversation = isConversationEnabled && conversationDockPos === dockBottomValue;
  const isNeedToShowRightConversation =
    isConversationEnabled && !isNeedToShowBottomConversation && (conversationDockPos === null || conversationDockPos === dockRightValue);

  // Right-side conversation (Angular parity: collapsible + resizable + open-by-default)
  const openConversationByDefault = Boolean(
    transactionDto?.OtherOptions?.IsOpenCommunicationByDefault ?? transactionDto?.IsOpenCommunicationByDefault
  );
  const rightConvStorageKeyBase = useMemo(
    () => `mdConvRight:${runtimeTransactionId || 'unknown'}`,
    [runtimeTransactionId]
  );
  const [isRightConversationExpanded, setIsRightConversationExpanded] = useState<boolean>(openConversationByDefault);
  const [rightConversationWidth, setRightConversationWidth] = useState<number>(420);
  const [isResizingRightConversation, setIsResizingRightConversation] = useState(false);

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

  const startResizeRightConversation = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsResizingRightConversation(true);
  }, []);

  useEffect(() => {
    if (!isResizingRightConversation) return;

    const handleMove = (ev: MouseEvent) => {
      const host = mainContainerRef.current;
      if (!host) return;
      const rect = host.getBoundingClientRect();
      const pointerX = ev.clientX;
      const nextWidthRaw = rect.right - pointerX;
      const maxWidth = Math.max(300, Math.min(900, rect.width - 180));
      const nextWidth = Math.min(Math.max(nextWidthRaw, 300), maxWidth);
      setRightConversationWidth(nextWidth);
    };

    const handleUp = () => setIsResizingRightConversation(false);

    window.addEventListener('mousemove', handleMove);
    window.addEventListener('mouseup', handleUp);
    return () => {
      window.removeEventListener('mousemove', handleMove);
      window.removeEventListener('mouseup', handleUp);
    };
  }, [isResizingRightConversation]);
  
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


  const bottomBoxMaxWidth = transactionDto?.ForeignAppFormExDto?.DefaultWidth 
    ? `${parseInt(transactionDto.ForeignAppFormExDto.DefaultWidth.trim()) || 1000}px`
    : '1000px';

  // Template header collapse state: when this form is a group template header ("Style Header"),
  // clicking the header band collapses/expands its body.
  const templateHeaderSectionId = `MasterDetailEditLayoutForm${controllerModel.uiId}`;
  const isTemplateHeaderCollapsed = controllerModel.isTemplateHeader && isSectionCollapsed(templateHeaderSectionId);

  // Desktop layout
  if (!isMobile) {
    // Show menu: not in preview; for file property show minimal menu (Refresh, Save); for other embedded (isHideHeaderAndFooter) hide
    const menuVisible = !controllerModel.isPreview && (controllerModel.isFilePropertyEdit || !controllerModel.param2Obj?.isHideHeaderAndFooter);
    return (
      <FormMasterDetailRuntimeConfigProvider
        value={runtimeFieldConfigApi ?? emptyFormMasterDetailRuntimeConfigApi}
      >
      <div className="w-full h-full flex flex-col">
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
          className={`w-full h-1 flex-auto p-1 ${theme.mainContentSection}`}
          style={{ height: menuVisible ? 'calc(100% - 40px)' : '100%' }}
        >
          <div className="w-full h-full flex">
            {/* Main scroll area */}
            <div className="w-1 flex-auto min-w-0 h-full overflow-auto">
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

              {/* Bottom Conversation Container */}
              {isNeedToShowBottomConversation && !controllerModel.isTemplateHeader && (
                <div className="w-full h-auto p-1">
                  <div
                    id="BottomConversationContainer"
                    className="w-full"
                    style={{
                      maxWidth: bottomBoxMaxWidth,
                      minWidth: '300px',
                      height: 'auto'
                    }}
                  >
                    <ConversationEditorEmbedded
                      scopeType={emMessageScopeTypeTransaction}
                      transactionId={runtimeTransactionId}
                      transactionRootValueId={runtimeTransactionRootValueId}
                      defaultMessageSubject="New Form Message"
                    />
                  </div>
                </div>
              )}
            </div>

            {/* Right Conversation (default, collapsible + resizable like Angular RightBox) */}
            {isNeedToShowRightConversation && !controllerModel.isTemplateHeader ? (
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
                      onMouseDown={startResizeRightConversation}
                      className="group absolute left-0 top-0 bottom-0 z-10 flex w-4 -ml-2 cursor-col-resize items-center justify-center select-none touch-none"
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
                        <ConversationEditorEmbedded
                          scopeType={emMessageScopeTypeTransaction}
                          transactionId={runtimeTransactionId}
                          transactionRootValueId={runtimeTransactionRootValueId}
                          defaultMessageSubject="New Form Message"
                        />
                      </div>
                    </div>
                  </div>
                ) : null}

                {/* Collapse/Expand strip (far right) */}
                <div className={`shrink-0 h-full w-[27px] border-l ${t('border_mainContentSection')} px-[1px] py-[1px]`}>
                  {!isRightConversationExpanded ? (
                    <button
                      type="button"
                      className={`w-[25px] px-1 py-2 text-xs rounded-[4px] ${theme.button_default}`}
                      style={{ writingMode: 'vertical-rl' as any }}
                      onClick={() => setIsRightConversationExpanded(true)}
                      title="Conversation"
                    >
                      Conversation <i className="fa-solid fa-angle-double-left" aria-hidden="true" />
                    </button>
                  ) : (
                    <button
                      type="button"
                      className={`w-[25px] px-1 py-2 text-xs rounded-[4px] ${theme.button_default}`}
                      style={{ writingMode: 'vertical-rl' as any }}
                      onClick={() => setIsRightConversationExpanded(false)}
                      title="Hide Conversation"
                    >
                      Hide Conversation <i className="fa-solid fa-angle-double-right" aria-hidden="true" />
                    </button>
                  )}
                </div>
              </>
            ) : null}
          </div>
        </div>
        )}
      </div>
      </FormMasterDetailRuntimeConfigProvider>
    );
  }

  // Mobile layout
  return (
    <FormMasterDetailRuntimeConfigProvider
      value={runtimeFieldConfigApi ?? emptyFormMasterDetailRuntimeConfigApi}
    >
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

        {/* Bottom Conversation Container - Mobile */}
        {isNeedToShowBottomConversation && (
          <div className="w-full h-auto p-1">
            <div
              id="BottomConversationContainer"
              className="w-full"
              style={{
                maxWidth: bottomBoxMaxWidth,
                minWidth: '300px',
                height: 'auto'
              }}
            >
              <ConversationEditorEmbedded
                scopeType={emMessageScopeTypeTransaction}
                transactionId={String(transactionDto?.Id ?? transactionDto?.TransactionId ?? controllerModel?.transactionId ?? '')}
                transactionRootValueId={String(
                  transactionDto?.RootPrimaryKeyValue ??
                    transactionDto?.TransactionRootValueId ??
                    controllerModel?.rootPrimaryKeyValue ??
                    ''
                )}
                defaultMessageSubject="New Form Message"
              />
            </div>
          </div>
        )}
      </div>
      )}

      {/* Bottom Menu Button - Mobile - hidden when embedded (file property, isHideHeaderAndFooter) */}
      {!controllerModel.isFilePropertyEdit && !controllerModel.param2Obj?.isHideHeaderAndFooter && (
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
    </FormMasterDetailRuntimeConfigProvider>
  );
};

export default MasterDetailEditLayoutForm;

