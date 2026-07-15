import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { CommandBasicOptions } from './CommandEditorPart/CommandBasicOptions';
import { CommandStatusLog } from './CommandEditorPart/CommandStatusLog';
import { CommandExecutionResultDisplay } from './CommandEditorPart/CommandExecutionResultDisplay';
import { PrintFromMessageTemplateSection } from './CommandEditorPart/PrintFromMessageTemplateSection';
import { OpenLinkedSearchSection } from './CommandEditorPart/OpenLinkedSearchSection';
import { BatchCommandSettingSection } from './CommandEditorPart/BatchCommandSettingSection';
import { SqlStatementSection } from './CommandEditorPart/SqlStatementSection';
import { NoteSection } from './CommandEditorPart/NoteSection';
import { IntegrationConfigSection } from './CommandEditorPart/IntegrationConfigSection';
import { PluginWebApiCallSection } from './CommandEditorPart/PluginWebApiCallSection';
import { CallApiOperationSection } from './CommandEditorPart/CallApiOperationSection';
import { OperationFileAndImportSection } from './CommandEditorPart/OperationFileAndImportSection';
import { ExecuteDataTransferSection } from './CommandEditorPart/ExecuteDataTransferSection';
import { QuickCreateUserSection } from './CommandEditorPart/QuickCreateUserSection';
import { CommandFormulaSection } from './CommandEditorPart/CommandFormulaSection';
import { GenerateMatrixSection } from './CommandEditorPart/GenerateMatrixSection';
import { NotificationExpressionSection } from './CommandEditorPart/NotificationExpressionSection';
import { MessageLinkCommandsSection } from './CommandEditorPart/MessageLinkCommandsSection';
import { CompositionCommandSection } from './CommandEditorPart/CompositionCommandSection';
import { ExecDataModelAllFormulaSection } from './CommandEditorPart/ExecDataModelAllFormulaSection';
import { LinkTargetSettingSection } from './CommandEditorPart/LinkTargetSettingSection';
import { CollapsibleSection } from './CommandEditorPart/CollapsibleSection';
import { AiActionConfigSection } from './CommandEditorPart/AiActionConfigSection';
import { buildChildGridFieldGroups } from './CommandEditorPart/sqlQueryBuilderTokens';
import type { CommandEditorHostContext } from './commandEditorContext';

type OperationTypeOption = { Id: any; Display: string };

const SECTION_STATE_STORAGE_KEY = 'appai.transactionCommandEditor.sections.collapsed.v1';

export function CommandEditor(props: {
  transactionId: number | null;
  applicationId: string | null;
  hierarchy: any;
  currentEditAction: any | null;
  operationTypeOptions: OperationTypeOption[];
  rootLevelTransFieldLookUpList: any[];
  rootLevelConditionTransFieldLookUpList: any[];
  rootLevelSwitchConditionTransFieldLookUpList: any[];
  transactionFieldLookUpList: any[];
  rootLevelAllFieldLookUpList: any[];
  onMarkChange: () => void;
  onRefreshGridCell: () => void;
  onFlowOrderChanged: () => void;
  onSelectChildCommand?: (commandId: number) => void;
  onCommandAdded?: (commandDto: any) => void;
  /** When true (child-command popup), editor fills popup width instead of master-detail panel width. */
  embeddedPopup?: boolean;
  /** applicationFormBuilder = Transaction Command Mgt; workflowAutomation = Workflow Automation Editor right panel. */
  hostContext?: CommandEditorHostContext;
  /** Workflow parameter transaction — enables Global Fields token keypad (Angular rootWorkflowTransactionData). */
  rootWorkflowTransactionData?: any | null;
  globalTransFieldLookUpList?: any[];
  /** Workflow external command: parent bumps after in-place action edits to refresh controlled fields */
  renderRevision?: number;
}) {
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const validationResultPrefEnum = useEnumValues('EmAppValidationResultPreference');
  const commandLoggingPrefEnum = useEnumValues('EmAppCommandLoggingPreference');

  const {
    transactionId,
    applicationId,
    hierarchy,
    currentEditAction,
    operationTypeOptions,
    rootLevelTransFieldLookUpList,
    rootLevelConditionTransFieldLookUpList,
    rootLevelSwitchConditionTransFieldLookUpList,
    transactionFieldLookUpList,
    rootLevelAllFieldLookUpList,
    onMarkChange,
    onRefreshGridCell,
    onFlowOrderChanged,
    onSelectChildCommand,
    onCommandAdded,
    embeddedPopup = false,
    hostContext = 'applicationFormBuilder',
    rootWorkflowTransactionData = null,
    globalTransFieldLookUpList = [],
    renderRevision: _renderRevision,
  } = props;

  const [collapsedBySectionId, setCollapsedBySectionId] = useState<Record<string, boolean>>(() => {
    try {
      const raw = localStorage.getItem(SECTION_STATE_STORAGE_KEY);
      if (!raw) return {};
      const obj = JSON.parse(raw);
      return obj && typeof obj === 'object' ? (obj as Record<string, boolean>) : {};
    } catch {
      return {};
    }
  });

  useEffect(() => {
    try {
      localStorage.setItem(SECTION_STATE_STORAGE_KEY, JSON.stringify(collapsedBySectionId));
    } catch {
      // ignore
    }
  }, [collapsedBySectionId]);

  const toggleSection = useCallback((sectionId: string) => {
    setCollapsedBySectionId((prev) => ({ ...prev, [sectionId]: !prev?.[sectionId] }));
  }, []);

  const childGridFieldGroups = useMemo(
    () => buildChildGridFieldGroups(hierarchy, transactionFieldLookUpList),
    [hierarchy, transactionFieldLookUpList],
  );

  const currentActionType = Number(currentEditAction?.ActionType);
  const showNotificationExpression = [60, 61, 62, 63].includes(currentActionType);
  const showMessageLinkCommands =
    currentActionType === 61 || currentActionType === 62;

  // (ActionType 74: Print From Message Template) moved into CommandEditorPart/PrintFromMessageTemplateSection

  // (ActionType 53: OpenLinkedSearch) moved into CommandEditorPart/OpenLinkedSearchSection

  // (SQL Statement / Query Builder) moved into CommandEditorPart/SqlStatementSection
  // (Batch Command Setting) moved into CommandEditorPart/BatchCommandSettingSection

  const shellWidthClass =
    embeddedPopup || hostContext === 'workflowAutomation'
      ? 'w-full max-w-full min-w-0 min-h-0 h-full'
      : 'w-1 min-w-[620px] flex-auto min-w-0 min-h-0 h-full';

  return (
    <div className={`${shellWidthClass} flex flex-col border rounded overflow-hidden ${theme.mainContentSection}`}>
      <div className={`text-xs font-semibold px-3 py-2 shrink-0 border-b ${theme.mainContentSection} ${theme.title}`}>Current Edit Operation Task</div>
      <div className="min-h-0 flex-auto overflow-auto px-5 py-5">
        {!currentEditAction && <p className={`text-sm ${theme.label}`}>Select a command from the list.</p>}

        {currentEditAction && (
          <div className="flex flex-col gap-3">
            <CollapsibleSection
              sectionId="basic"
              title="Command Basic Options"
              collapsed={!!collapsedBySectionId['basic']}
              onToggle={toggleSection}
            >
              <CommandBasicOptions
                action={currentEditAction}
                operationTypeOptions={operationTypeOptions}
                rootLevelConditionTransFieldLookUpList={rootLevelConditionTransFieldLookUpList}
                hostContext={hostContext}
                onMarkChange={onMarkChange}
                onRefreshGridCell={onRefreshGridCell}
                onFlowOrderChanged={onFlowOrderChanged}
              />
            </CollapsibleSection>

            <CollapsibleSection
              sectionId="statusLog"
              title="Command Status Log"
              collapsed={!!collapsedBySectionId['statusLog']}
              onToggle={toggleSection}
            >
              <CommandStatusLog action={currentEditAction} commandLoggingPrefEnum={commandLoggingPrefEnum} onMarkChange={onMarkChange} />
            </CollapsibleSection>

            <CollapsibleSection
              sectionId="executionResult"
              title="Command Execution Result Display"
              collapsed={!!collapsedBySectionId['executionResult']}
              onToggle={toggleSection}
            >
              <CommandExecutionResultDisplay
                action={currentEditAction}
                validationResultPrefEnum={validationResultPrefEnum}
                onMarkChange={onMarkChange}
              />
            </CollapsibleSection>

            <CollapsibleSection
              sectionId="batch"
              title="Batch Command Setting"
              collapsed={!!collapsedBySectionId['batch']}
              onToggle={toggleSection}
            >
              <BatchCommandSettingSection
                hierarchy={hierarchy}
                action={currentEditAction}
                rootLevelAllFieldLookUpList={rootLevelAllFieldLookUpList}
                onMarkChange={onMarkChange}
              />
            </CollapsibleSection>

            {Number(currentEditAction.ActionType) === 42 ? (
              <CollapsibleSection
                sectionId="sql"
                title="SQL Statement"
                collapsed={!!collapsedBySectionId['sql']}
                onToggle={toggleSection}
              >
                <SqlStatementSection
                  hierarchy={hierarchy}
                  action={currentEditAction}
                  rootLevelTransFieldLookUpList={rootLevelTransFieldLookUpList}
                  childGridFieldGroups={childGridFieldGroups}
                  globalTransFieldLookUpList={globalTransFieldLookUpList}
                  rootFieldSectionTitle={
                    rootWorkflowTransactionData ? 'Current Form Root Level Fields' : 'Form Root Level Fields'
                  }
                  onMarkChange={onMarkChange}
                />
              </CollapsibleSection>
            ) : null}

            {[83, 84, 85, 86].includes(Number(currentEditAction.ActionType)) ? (
              <CollapsibleSection
                sectionId="integrationConfig"
                title="Integration Config (JSON)"
                collapsed={!!collapsedBySectionId['integrationConfig']}
                onToggle={toggleSection}
              >
                <IntegrationConfigSection action={currentEditAction} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 59 ? (
              <CollapsibleSection
                sectionId="callApiOperation"
                title="Call API Operation"
                collapsed={!!collapsedBySectionId['callApiOperation']}
                onToggle={toggleSection}
              >
                <CallApiOperationSection
                  transactionId={transactionId}
                  hierarchy={hierarchy}
                  action={currentEditAction}
                  onMarkChange={onMarkChange}
                />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 40 ? (
              <CollapsibleSection
                sectionId="pluginWebApiCall"
                title="Call Plugin Web Api"
                collapsed={!!collapsedBySectionId['pluginWebApiCall']}
                onToggle={toggleSection}
              >
                <PluginWebApiCallSection action={currentEditAction} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {[66, 67, 68, 71, 72, 73, 76, 77, 81, 82].includes(Number(currentEditAction.ActionType)) ? (
              <CollapsibleSection
                sectionId="fileAndImport"
                title="File / Import Settings"
                collapsed={!!collapsedBySectionId['fileAndImport']}
                onToggle={toggleSection}
              >
                <OperationFileAndImportSection action={currentEditAction} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 43 ? (
              <CollapsibleSection
                sectionId="executeDataTransfer"
                title="Execute Data Transfer"
                collapsed={!!collapsedBySectionId['executeDataTransfer']}
                onToggle={toggleSection}
              >
                <ExecuteDataTransferSection action={currentEditAction} transactionId={transactionId} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 70 ? (
              <CollapsibleSection
                sectionId="quickCreateUser"
                title="Create User"
                collapsed={!!collapsedBySectionId['quickCreateUser']}
                onToggle={toggleSection}
              >
                <QuickCreateUserSection action={currentEditAction} rootLevelTransFieldLookUpList={rootLevelTransFieldLookUpList} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 57 ? (
              <CollapsibleSection
                sectionId="commandFormula"
                title="Execute Commnad Formula"
                collapsed={!!collapsedBySectionId['commandFormula']}
                onToggle={toggleSection}
              >
                <CommandFormulaSection action={currentEditAction} rootLevelAllFieldLookUpList={rootLevelAllFieldLookUpList} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 46 ? (
              <CollapsibleSection
                sectionId="generateMatrix"
                title="Generate Matrix"
                collapsed={!!collapsedBySectionId['generateMatrix']}
                onToggle={toggleSection}
              >
                <GenerateMatrixSection action={currentEditAction} hierarchy={hierarchy} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 44 ? (
              <CollapsibleSection
                sectionId="execAllFormulas"
                title="Exec DataModel All Formula"
                collapsed={!!collapsedBySectionId['execAllFormulas']}
                onToggle={toggleSection}
              >
                <ExecDataModelAllFormulaSection
                  action={currentEditAction}
                  applicationId={applicationId}
                  transactionId={transactionId}
                  transactionType={hierarchy?.TransactionOrganizedType ?? null}
                  transactionName={hierarchy?.TransactionName ?? null}
                />
              </CollapsibleSection>
            ) : null}

            {showNotificationExpression ? (
              <CollapsibleSection
                sectionId="notificationExpression"
                title="Notification Message"
                collapsed={!!collapsedBySectionId['notificationExpression']}
                onToggle={toggleSection}
              >
                <NotificationExpressionSection
                  action={currentEditAction}
                  hierarchy={hierarchy}
                  transactionId={transactionId}
                  transactionFieldLookUpList={transactionFieldLookUpList}
                  rootLevelTransFieldLookUpList={rootLevelTransFieldLookUpList}
                  childGridFieldGroups={childGridFieldGroups}
                  globalTransFieldLookUpList={globalTransFieldLookUpList}
                  rootFieldSectionTitle={rootWorkflowTransactionData ? 'Current Form Fields' : 'Form Fields'}
                  onMarkChange={onMarkChange}
                />
              </CollapsibleSection>
            ) : null}

            {showMessageLinkCommands ? (
              <CollapsibleSection
                sectionId="messageLinkCommands"
                title="Message Link Commands"
                collapsed={!!collapsedBySectionId['messageLinkCommands']}
                onToggle={toggleSection}
              >
                <MessageLinkCommandsSection
                  action={currentEditAction}
                  hierarchy={hierarchy}
                  transactionId={transactionId}
                  applicationId={applicationId}
                  transactionName={hierarchy?.TransactionName ?? null}
                  rootLevelTransFieldLookUpList={rootLevelTransFieldLookUpList}
                  rootLevelConditionTransFieldLookUpList={rootLevelConditionTransFieldLookUpList}
                  rootLevelSwitchConditionTransFieldLookUpList={rootLevelSwitchConditionTransFieldLookUpList}
                  transactionFieldLookUpList={transactionFieldLookUpList}
                  rootLevelAllFieldLookUpList={rootLevelAllFieldLookUpList}
                  operationTypeOptions={operationTypeOptions}
                  onMarkChange={onMarkChange}
                  onCommandAdded={onCommandAdded}
                />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 200 ? (
              <CollapsibleSection
                sectionId="composition"
                title="Composition Command"
                collapsed={!!collapsedBySectionId['composition']}
                onToggle={toggleSection}
              >
                <CompositionCommandSection
                  action={currentEditAction}
                  hierarchy={hierarchy}
                  applicationId={applicationId}
                  transactionId={transactionId}
                  transactionName={hierarchy?.TransactionName ?? null}
                  rootLevelSwitchConditionTransFieldLookUpList={rootLevelSwitchConditionTransFieldLookUpList}
                  rootLevelTransFieldLookUpList={rootLevelTransFieldLookUpList}
                  rootLevelConditionTransFieldLookUpList={rootLevelConditionTransFieldLookUpList}
                  transactionFieldLookUpList={transactionFieldLookUpList}
                  rootLevelAllFieldLookUpList={rootLevelAllFieldLookUpList}
                  operationTypeOptions={operationTypeOptions}
                  onMarkChange={onMarkChange}
                  onCommandAdded={onCommandAdded}
                />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 53 ? (
              <CollapsibleSection
                sectionId="openLinkedSearch"
                title="Navigate To Search"
                collapsed={!!collapsedBySectionId['openLinkedSearch']}
                onToggle={toggleSection}
              >
                <OpenLinkedSearchSection hierarchy={hierarchy} action={currentEditAction} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 56 ? (
              <CollapsibleSection
                sectionId="aiAction"
                title="AI Action Configuration"
                collapsed={!!collapsedBySectionId['aiAction']}
                onToggle={toggleSection}
              >
                <AiActionConfigSection
                  action={currentEditAction}
                  hierarchy={hierarchy}
                  onMarkChange={onMarkChange}
                />
              </CollapsibleSection>
            ) : null}

            {[51, 55].includes(Number(currentEditAction.ActionType)) ? (
              <CollapsibleSection
                sectionId="linkTarget"
                title="Link Target Setting"
                collapsed={!!collapsedBySectionId['linkTarget']}
                onToggle={toggleSection}
              >
                <LinkTargetSettingSection action={currentEditAction} onMarkChange={onMarkChange} />
              </CollapsibleSection>
            ) : null}

            {Number(currentEditAction.ActionType) === 74 ? (
              <CollapsibleSection
                sectionId="messageTemplate"
                title="Message Template"
                collapsed={!!collapsedBySectionId['messageTemplate']}
                onToggle={toggleSection}
              >
                <PrintFromMessageTemplateSection
                  transactionId={transactionId}
                  hierarchy={hierarchy}
                  action={currentEditAction}
                  onMarkChange={onMarkChange}
                />
              </CollapsibleSection>
            ) : null}

            <CollapsibleSection
              sectionId="notes"
              title="Notes"
              collapsed={!!collapsedBySectionId['notes']}
              onToggle={toggleSection}
            >
              <NoteSection action={currentEditAction} onMarkChange={onMarkChange} />
            </CollapsibleSection>
          </div>
        )}
      </div>

    </div>
  );
}

