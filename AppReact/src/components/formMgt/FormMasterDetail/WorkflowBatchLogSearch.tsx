/**
 * Workflow batch log search — Angular _WorkflowBatchLogSearch.cshtml + loadWorkflowBatchLogSearchToContainer.
 * Embeds configured Search (WorkflowLogTrackDetailSearchId) filtered by workflowLogBatchNumber.
 */

import React, { useMemo } from 'react';
import { useSelector } from 'react-redux';
import type { RootState } from '../../../redux/store';
import { useTheme } from '../../../redux/hooks/useTheme';
import AppSearch from '../../search/AppSearch';

export type WorkflowBatchLogSearchProps = {
  controllerModel: any;
};

function resolveWorkflowLogTrackSearchId(userContext: any): number | null {
  const raw =
    userContext?.DictAppSetup?.WorkflowLogTrackDetailSearchId ??
    userContext?.DictAppSetup?.workflowLogTrackDetailSearchId ??
    null;
  if (raw == null || raw === '') return null;
  const n = Number(raw);
  return Number.isNaN(n) || n <= 0 ? null : n;
}

/** Angular partial visibility uses WorkflowBatchLogSearchId; content load uses WorkflowLogTrackDetailSearchId. */
function resolveWorkflowBatchLogContainerEnabled(userContext: any): boolean {
  const trackId = resolveWorkflowLogTrackSearchId(userContext);
  if (trackId != null) return true;
  const batchRaw =
    userContext?.DictAppSetup?.WorkflowBatchLogSearchId ??
    userContext?.DictAppSetup?.workflowBatchLogSearchId ??
    null;
  if (batchRaw == null || batchRaw === '') return false;
  const n = Number(batchRaw);
  return !Number.isNaN(n) && n > 0;
}

const WorkflowBatchLogSearch: React.FC<WorkflowBatchLogSearchProps> = ({ controllerModel }) => {
  const { theme } = useTheme();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const searchId = resolveWorkflowLogTrackSearchId(userContext);
  const showContainer = resolveWorkflowBatchLogContainerEnabled(userContext);

  const transactionId = controllerModel?.transactionId ?? null;
  const rootPrimaryKeyValue = controllerModel?.rootPrimaryKeyValue ?? null;

  const embeddedParamObj = useMemo(() => {
    if (searchId == null || transactionId == null || rootPrimaryKeyValue == null) return null;
    const rid = String(rootPrimaryKeyValue).trim();
    if (!rid) return null;
    const batchNumber = `${transactionId}_${rid}`;
    return {
      searchId,
      isShowCriterias: true,
      isWorkflowLogSearch: true,
      workflowTransactionId: Number(transactionId),
      workflowTransactionRId: rid,
      workflowLogBatchNumber: batchNumber,
      isHideHeaderAndFooter: true,
      isEmbeddedByOtherPage: true,
    };
  }, [searchId, transactionId, rootPrimaryKeyValue]);

  if (!showContainer) return null;

  if (!embeddedParamObj) {
    return (
      <div
        className={`w-full px-2.5 pb-1.5 pt-2 text-xs ${theme.label}`}
        style={{ minHeight: 120 }}
      >
        Save the workflow run record first to view execution log details.
      </div>
    );
  }

  return (
    <div
      className="w-full min-w-0 px-2.5 pb-1.5"
      style={{ height: 'calc(100vh - 300px)', minHeight: 360 }}
    >
      <div className={`w-full h-full min-h-0 overflow-hidden rounded border ${theme.mainContentSection}`}>
        <AppSearch
          key={`wf-batch-log-${embeddedParamObj.searchId}-${embeddedParamObj.workflowLogBatchNumber}`}
          embeddedParamObj={embeddedParamObj}
        />
      </div>
    </div>
  );
};

export default WorkflowBatchLogSearch;
