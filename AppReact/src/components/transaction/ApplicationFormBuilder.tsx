import React, { useState, useEffect, useMemo } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import TransactionGraphicEditor from './ApplicationFormBuilder/TransactionGraphicEditor';
import TransactionFormulaEditor from './ApplicationFormBuilder/TransactionFormulaEditor';
import TransactionDataLoadManagement from './ApplicationFormBuilder/TransactionDataLoadManagement';
import TransactionConditionLockEditor from './ApplicationFormBuilder/TransactionConditionLockEditor';
import TransactionDataTransferSettingMgt from './ApplicationFormBuilder/TransactionDataTransferSettingMgt';
import TransactionReportEditor from './ApplicationFormBuilder/TransactionReportEditor';
import TransactionCommandActionEditor from './ApplicationFormBuilder/TransactionCommandMgt';
import TransactionNavigationEditor from './TransactionNavigationEditor';
import FormDesign from '../formMgt/FormDesign';

// TransactionOrganizedType: 1 = Master Detail (Form Model), 3 = List Edit
const EmTransactionOrganizedType = { MasterDetail: 1, List: 3 } as const;

interface ApplicationFormBuilderProps {
  isOpen: boolean;
  onClose: () => void;
  onRefresh?: () => void;
  applicationId: string | null;
  defaultSectionCode?: string;
  isCreateNewItem?: boolean;
  formId?: number | null;
  transactionId?: number | null;
  workflowId?: number | null;
  transactionType?: number | null;
  formLayoutType?: number | null;
  modelName?: string | null;
  isCreateDtoDataModel?: boolean;
  dataSourceRegisterId?: number | null;
  isCreateApiDataModel?: boolean;
  isCreateDataModelView?: boolean;
  erDiagramId?: number | null;
  /** Open Data Model Design with this unit's editor (Angular: needToEditUnitId from TransactionGraphicEditor route). */
  initialNeedToEditUnitId?: number | null;
  /** Open Form Design with this field's layout item selected (runtime master-detail gear). */
  initialFormDesignFieldFocus?: {
    transactionFieldId: number;
    layoutItemId?: string | number;
    isGrid?: boolean;
  } | null;
  /** Operation Task editor: select this command on load (Angular initialSelectedCommandId). */
  initialSelectedCommandId?: number | null;
}

// Section codes matching AngularJS EmSectionCode
const FormBuilderSection = {
  Form: 'Form',
  PrintForm: 'PrintForm',
  Transaction: 'Transaction',
  Workflow: 'Workflow',
  TransactionGraphicEditor: 'TransactionGraphicEditor',
  TransactionAdvancedEditor: 'TransactionAdvancedEditor',
  TransactionDDLCreator: 'TransactionDDLCreator',
  TransactionFormulaSetting: 'TransactionFormulaSetting',
  TransactionConditionLockEditor: 'TransactionConditionLockEditor',
  TransactionCommandActionSetting: 'TransactionCommandActionSetting',
  TransactionCrossRelationSetting: 'TransactionCrossRelationSetting',
  TransactionSaveAsMapping: 'TransactionSaveAsMapping',
  TransactionDataTransfer: 'TransactionDataTransfer',
  TransactionReportSetting: 'TransactionReportSetting',
  TransactionDataTransferSetting: 'TransactionDataTransferSetting',
  TransactionSearchCreator: 'TransactionSearchCreator',
  TransactionNavigationEditor: 'TransactionNavigationEditor',
  TransactionDataLoadSetting: 'TransactionDataLoadSetting',
  TransactionApiSetting: 'TransactionApiSetting',
};

const TransactionApiSettingSection = {
  Consume: 'TransactionApiSetting_Consume',
  Provide: 'TransactionApiSetting_Provide',
} as const;

const ApplicationFormBuilder: React.FC<ApplicationFormBuilderProps> = ({
  isOpen,
  onClose,
  onRefresh,
  applicationId,
  defaultSectionCode = 'Transaction',
  isCreateNewItem = true,
  formId = null,
  transactionId = null,
  workflowId = null,
  transactionType = null,
  formLayoutType = null,
  modelName = null,
  isCreateDtoDataModel = false,
  dataSourceRegisterId = null,
  isCreateApiDataModel = false,
  isCreateDataModelView = false,
  erDiagramId = null,
  initialNeedToEditUnitId = null,
  initialFormDesignFieldFocus = null,
  initialSelectedCommandId = null,
}) => {
  const { theme } = useTheme();
  // IMPORTANT:
  // `onRefresh` is intended to refresh the *host page* after the popup closes.
  // Child pages within the popup may have their own Save/Refresh buttons; those must NOT
  // trigger host refresh (which can cause the popup to unmount and appear "auto-closed").
  const childOnRefresh: undefined = undefined;

  // AngularJS mapping: defaultSectionCode='Transaction' => active sub-section 'TransactionGraphicEditor'
  const mapDefaultSectionCodeToSection = (code: string | null | undefined): string => {
    if (!code) return FormBuilderSection.TransactionGraphicEditor;
    if (code === FormBuilderSection.Transaction) return FormBuilderSection.TransactionGraphicEditor;
    return code;
  };

  const [currentSection, setCurrentSection] = useState<string>(() => mapDefaultSectionCodeToSection(defaultSectionCode));
  /** Mirrors Angular controllerModel.formId after first save / auto-save assigns a real id (props may stay null on route open). */
  const [resolvedFormId, setResolvedFormId] = useState<number | null>(formId ?? null);
  /** Mirrors Angular controllerModel.transactionId after first save assigns a real id (route props stay null for new transactions). */
  const [resolvedTransactionId, setResolvedTransactionId] = useState<number | null>(transactionId ?? null);
  /** When switching to Form via Design Form dropdown: true = Auto Design Form, false = Design On Blank Form. Cleared when FormDesign consumes it or when leaving Form section. */
  const [formDesignInitialAutoLayout, setFormDesignInitialAutoLayout] = useState<boolean | null>(null);

  const effectiveTransactionId = resolvedTransactionId ?? transactionId ?? null;

  useEffect(() => {
    setResolvedFormId(formId ?? null);
  }, [formId]);

  useEffect(() => {
    setResolvedTransactionId(transactionId ?? null);
  }, [transactionId]);

  // Section definitions with icons (matching AngularJS structure)
  const allSections = [
    { code: FormBuilderSection.Form, name: 'Editing Form Design', icon: 'fa-file-pen' },
    { code: FormBuilderSection.PrintForm, name: 'Printing Form Design', icon: 'fa-file-pdf' },
    { code: FormBuilderSection.TransactionGraphicEditor, name: 'Data Model Design', icon: 'fa-diagram-project' },
    { code: FormBuilderSection.TransactionCommandActionSetting, name: 'Operation Task', icon: 'fa-play-circle' },
    { code: FormBuilderSection.TransactionFormulaSetting, name: 'Calculation Validation Flow', icon: 'fa-calculator' },
    { code: FormBuilderSection.TransactionDataLoadSetting, name: 'Data Load Settings', icon: 'fa-download' },
    { code: FormBuilderSection.TransactionConditionLockEditor, name: 'Conditional Lock or Hide', icon: 'fa-eye' },
    { code: FormBuilderSection.TransactionDataTransferSetting, name: 'Internal Data Transfer Data Model', icon: 'fa-arrows-rotate' },
    { code: FormBuilderSection.TransactionReportSetting, name: 'Report', icon: 'fa-chart-column' },
    { code: FormBuilderSection.TransactionNavigationEditor, name: 'Navigation', icon: 'fa-folder-tree' },
    { code: FormBuilderSection.Workflow, name: 'Workflow', icon: 'fa-diagram-project' },
  ];

  // When TransactionOrganizedType === 3 (List Edit), left menu shows only 3 items (matching AngularJS)
  const listEditSectionCodes = [
    FormBuilderSection.TransactionGraphicEditor,      // Data Model Design
    FormBuilderSection.TransactionCommandActionSetting, // Operation Task
    FormBuilderSection.TransactionDataTransferSetting,  // Internal Data Transfer Data Model
  ];
  const sections = useMemo(() => {
    if (transactionType === EmTransactionOrganizedType.List) {
      return allSections.filter((s) => listEditSectionCodes.includes(s.code));
    }
    return allSections;
  }, [transactionType]);

  useEffect(() => {
    if (isOpen) {
      setCurrentSection(mapDefaultSectionCodeToSection(defaultSectionCode));
    }
  }, [isOpen, defaultSectionCode]);

  // When List Edit (type 3), ensure current section is one of the visible 3; otherwise switch to Data Model Design
  useEffect(() => {
    const visibleCodes = sections.map((s) => s.code);
    const isTransactionApiSetting = currentSection === TransactionApiSettingSection.Consume || currentSection === TransactionApiSettingSection.Provide;
    if (isTransactionApiSetting) return;
    if (visibleCodes.length > 0 && !visibleCodes.includes(currentSection)) {
      setCurrentSection(visibleCodes[0]);
    }
  }, [sections, currentSection]);

  // Clear Form Design initial flag when leaving Form section
  useEffect(() => {
    if (currentSection !== FormBuilderSection.Form) {
      setFormDesignInitialAutoLayout(null);
    }
  }, [currentSection]);

  const handleClose = () => {
    onClose();
    if (onRefresh) {
      onRefresh();
    }
  };

  const getSectionClass = (sectionCode: string): string => {
    return currentSection === sectionCode 
      ? `${theme.tab_active}` 
      : `${theme.tab} opacity-60 hover:opacity-90`;
  };

  const renderSectionContent = () => {
    // TransactionGraphicEditor has its own layout with toolbar
    if (currentSection === FormBuilderSection.TransactionGraphicEditor) {
      return (
        <TransactionGraphicEditor
          transactionId={effectiveTransactionId}
          applicationId={applicationId}
          transactionType={transactionType}
          isCreateNewItem={isCreateNewItem && !effectiveTransactionId}
          dataSourceRegisterId={dataSourceRegisterId}
          isCreateDtoDataModel={isCreateDtoDataModel}
          isCreateApiDataModel={isCreateApiDataModel}
          isCreateDataModelView={isCreateDataModelView}
          erDiagramId={erDiagramId}
          onRefresh={childOnRefresh}
          initialNeedToEditUnitId={initialNeedToEditUnitId}
          onTransactionIdResolved={(id) => setResolvedTransactionId(id)}
          onFormIdResolved={(id) => setResolvedFormId(id)}
          onOpenFormDesign={(isAuto) => {
            setFormDesignInitialAutoLayout(isAuto ?? null);
            setCurrentSection(FormBuilderSection.Form);
          }}
        />
      );
    }

    // Form Design section
    if (currentSection === FormBuilderSection.Form) {
      return (
        <FormDesign
          formId={resolvedFormId}
          transactionId={effectiveTransactionId}
          applicationId={applicationId}
          onRefresh={childOnRefresh}
          onSwitchToDataModelDesign={() => setCurrentSection(FormBuilderSection.TransactionGraphicEditor)}
          initialIsAutoDesignFormLayout={formDesignInitialAutoLayout}
          onConsumedInitialAutoLayout={() => setFormDesignInitialAutoLayout(null)}
          onFormIdResolved={(id) => setResolvedFormId(id)}
          initialFieldDesignFocus={initialFormDesignFieldFocus}
        />
      );
    }

    // Operation Task (Transaction Command)
    if (currentSection === FormBuilderSection.TransactionCommandActionSetting) {
      return (
        <div className="h-full flex flex-col overflow-hidden">
          <TransactionCommandActionEditor
            transactionId={effectiveTransactionId}
            applicationId={applicationId}
            transactionName={modelName}
            transactionType={transactionType ?? null}
            initialSelectedCommandId={initialSelectedCommandId ?? null}
            onRefresh={childOnRefresh}
          />
        </div>
      );
    }

    // Calculation Validation Flow (Transaction Formula Editor)
    if (currentSection === FormBuilderSection.TransactionFormulaSetting) {
      return (
        <div className="h-full flex flex-col overflow-hidden">
          <TransactionFormulaEditor
            transactionId={effectiveTransactionId}
            applicationId={applicationId}
            onRefresh={childOnRefresh}
          />
        </div>
      );
    }

    // Data Load Settings (Transaction Data Load Management)
    if (currentSection === FormBuilderSection.TransactionDataLoadSetting) {
      return (
        <div className="h-full flex flex-col overflow-hidden">
          <TransactionDataLoadManagement
            transactionId={effectiveTransactionId}
            transactionName={modelName}
            onRefresh={childOnRefresh}
          />
        </div>
      );
    }

    // Conditional Lock or Hide (Data Model Condition Lock)
    if (currentSection === FormBuilderSection.TransactionConditionLockEditor) {
      return (
        <div className="h-full flex flex-col overflow-hidden">
          <TransactionConditionLockEditor
            transactionId={effectiveTransactionId}
            transactionName={modelName}
            onRefresh={childOnRefresh}
          />
        </div>
      );
    }

    // Internal Data Transfer Data Model
    if (currentSection === FormBuilderSection.TransactionDataTransferSetting) {
      return (
        <div className="h-full flex flex-col overflow-hidden">
          <TransactionDataTransferSettingMgt
            transactionId={effectiveTransactionId}
            transactionName={modelName}
            onRefresh={childOnRefresh}
          />
        </div>
      );
    }

    // API Settings (Consume/Provide) - match Angular "API Settings" dropdown in TransactionGraphicEditor
    if (currentSection === TransactionApiSettingSection.Consume || currentSection === TransactionApiSettingSection.Provide) {
      const isConsume = currentSection === TransactionApiSettingSection.Consume;
      return (
        <div className="h-full flex flex-col overflow-hidden">
          <TransactionDataTransferSettingMgt
            transactionId={effectiveTransactionId}
            transactionName={modelName}
            filterTransferTypeIds={isConsume ? [3] : [4]}
            titleOverride={isConsume ? 'Consume APIs (Call 3rd Part API)' : 'Provide APIs (Provide API To 3rd Part)'}
            onRefresh={childOnRefresh}
          />
        </div>
      );
    }

    // Report (Data Model Report Editor)
    if (currentSection === FormBuilderSection.TransactionReportSetting) {
      return (
        <div className="h-full flex flex-col overflow-hidden">
          <TransactionReportEditor
            transactionId={effectiveTransactionId}
            transactionName={modelName}
            onRefresh={childOnRefresh}
          />
        </div>
      );
    }

    if (currentSection === FormBuilderSection.TransactionNavigationEditor) {
      return (
        <div className="h-full flex flex-col overflow-hidden">
          <TransactionNavigationEditor embedTransactionId={effectiveTransactionId} onRefresh={childOnRefresh} />
        </div>
      );
    }

    const sectionName = sections.find(s => s.code === currentSection)?.name || 'Section';
    
    // All other sections follow the same layout standard: toolbar at top, content below
    return (
      <div className="h-full flex flex-col gap-1">
        {/* Toolbar - matching DataModelDesign layout */}
        <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>
            {sectionName}
          </div>
          <div className="flex items-center space-x-2">
            {/* Section-specific buttons can be added here */}
          </div>
        </div>

        {/* Content Area */}
        <div className={`h-1 flex-auto overflow-auto ${theme.mainContentSection}`}>
          <div className="p-4">
            <div className={`p-4 rounded border ${theme.mainContentSection}`}>
              <p className={`text-sm ${theme.label}`}>
                {currentSection === FormBuilderSection.PrintForm && 'Print form design section - to be implemented.'}
                {![FormBuilderSection.Form, FormBuilderSection.PrintForm, FormBuilderSection.TransactionGraphicEditor, FormBuilderSection.TransactionCommandActionSetting, FormBuilderSection.TransactionFormulaSetting, FormBuilderSection.TransactionDataLoadSetting, FormBuilderSection.TransactionConditionLockEditor, FormBuilderSection.TransactionDataTransferSetting, FormBuilderSection.TransactionReportSetting, FormBuilderSection.TransactionNavigationEditor].includes(currentSection) && 
                  'This section is under development. Full implementation to be added based on AngularJS ApplicationFormBuilder.'}
              </p>
            </div>
          </div>
        </div>
      </div>
    );
  };

  if (!isOpen) return null;

  return (
    <div className={`fixed inset-0 ${theme.default} p-1 flex items-center justify-center z-[9999]`}>
      <div
        className={`w-full h-full flex flex-col gap-1 overflow-hidden rounded-lg border ${theme.default}`}       
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header with Close Button */}
        <div className={`w-full h-8 flex items-center justify-between px-4 ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>
            Application Builder: {modelName || 'New Transaction'}
          </div>
          <button
            onClick={handleClose}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} transition-all duration-200 hover:shadow-sm active:scale-95 flex items-center gap-1`}
            title="Back"
          >
            <i className="fa-solid fa-arrow-left text-xs"></i>
            <span>Back</span>
          </button>
        </div>

        {/* Main Content Area - matching MyApplicationEditor layout */}
        <div className="h-1 flex-auto overflow-hidden flex gap-1">
          {/* Left Sidebar Navigation (90px wide) */}
          <div className={`w-[90px] flex-none ${theme.mainContentSection} overflow-y-auto rounded-l-md`}>
            <div className="py-5">
              {sections.map((section) => (
                <div
                  key={section.code}
                  onClick={() => setCurrentSection(section.code)}
                  className={`
                    cursor-pointer flex flex-col items-center py-3 px-2 mb-1 transition-colors
                    ${getSectionClass(section.code)}
                  `}
                  title={section.name}
                >
                  <i className={`fa-solid ${section.icon} text-2xl mb-1 ${currentSection === section.code ? theme.title : theme.label}`}></i>
                  <div className="text-[10px] text-center leading-tight" style={{ wordBreak: 'break-word' }}>
                    {section.name}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Content Area */}
          <div className={`w-1 flex-auto h-full overflow-hidden rounded-r-md`}>
            {renderSectionContent()}
          </div>
        </div>
      </div>
    </div>
  );
};

export default ApplicationFormBuilder;

