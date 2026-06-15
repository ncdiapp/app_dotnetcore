import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { EmbeddedLinkedPopupFrame } from '../../../formMgt/EmbeddedLinkedPopupFrame';
import { CommandEditor } from '../CommandEditor';
import { cloneCommand, copyCommandInPlace } from '../commandEditCache';

type OperationTypeOption = { Id: any; Display: string };

export function ChildCommandEditorPopup(props: {
  commandId: number;
  hierarchy: any;
  applicationId: string | null;
  transactionId: number | null;
  operationTypeOptions: OperationTypeOption[];
  rootLevelTransFieldLookUpList: any[];
  rootLevelConditionTransFieldLookUpList: any[];
  rootLevelSwitchConditionTransFieldLookUpList: any[];
  transactionFieldLookUpList: any[];
  rootLevelAllFieldLookUpList: any[];
  onClose: () => void;
  onApplied: (updatedCommand: any) => void;
}) {
  const { theme } = useTheme();
  const {
    commandId,
    hierarchy,
    applicationId,
    transactionId,
    operationTypeOptions,
    rootLevelTransFieldLookUpList,
    rootLevelConditionTransFieldLookUpList,
    rootLevelSwitchConditionTransFieldLookUpList,
    transactionFieldLookUpList,
    rootLevelAllFieldLookUpList,
    onClose,
    onApplied,
  } = props;

  const originalRef = useRef<any>(null);
  const [draft, setDraft] = useState<any>(null);
  const [, setEditTick] = useState(0);

  useEffect(() => {
    const original = (hierarchy?.CommandActionList || []).find((c: any) => Number(c?.Id) === Number(commandId));
    originalRef.current = original ?? null;
    setDraft(original ? cloneCommand(original) : null);
  }, [commandId, hierarchy]);

  const handleApply = () => {
    const original = originalRef.current;
    if (original && draft) {
      copyCommandInPlace(original, draft);
      onApplied(original);
    }
    onClose();
  };

  if (!draft) return null;

  return (
    <EmbeddedLinkedPopupFrame
      zIndex={10070}
      title="Edit Command"
      frameInstanceKey={commandId}
      defaultWidth="1200px"
      defaultHeight="90vh"
      fullscreenPosition="afterTrailing"
      toolbarTrailing={
        <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleApply}>
          Apply
        </button>
      }
      toolbarTrailingEnd={
        <button
          type="button"
          className={`w-8 h-8 flex items-center justify-center rounded-[4px] ${theme.button_default}`}
          onClick={onClose}
          aria-label="Close"
          title="Close"
        >
          <i className="fa-solid fa-xmark" aria-hidden />
        </button>
      }
    >
      <div className="h-full w-full min-h-0 overflow-hidden">
        <CommandEditor
          embeddedPopup
          transactionId={transactionId}
          applicationId={applicationId}
          hierarchy={hierarchy}
          currentEditAction={draft}
          operationTypeOptions={operationTypeOptions}
          rootLevelTransFieldLookUpList={rootLevelTransFieldLookUpList}
          rootLevelConditionTransFieldLookUpList={rootLevelConditionTransFieldLookUpList}
          rootLevelSwitchConditionTransFieldLookUpList={rootLevelSwitchConditionTransFieldLookUpList}
          transactionFieldLookUpList={transactionFieldLookUpList}
          rootLevelAllFieldLookUpList={rootLevelAllFieldLookUpList}
          onMarkChange={() => setEditTick((t) => t + 1)}
          onRefreshGridCell={() => void 0}
          onFlowOrderChanged={() => void 0}
        />
      </div>
    </EmbeddedLinkedPopupFrame>
  );
}
