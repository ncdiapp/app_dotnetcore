import React, { useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import ApplicationFormBuilder from '../../ApplicationFormBuilder';

export const EmAppTransactionCommandTypeDataModelAllFormulaCalculation = 44;

export function ExecDataModelAllFormulaSection(props: {
  action: any;
  applicationId: string | null;
  transactionId: number | null;
  transactionType?: number | null;
  transactionName?: string | null;
}) {
  const { theme } = useTheme();
  const { action, applicationId, transactionId, transactionType = null, transactionName = null } = props;

  const [open, setOpen] = useState(false);

  if (!action) return null;
  if (Number(action.ActionType) !== EmAppTransactionCommandTypeDataModelAllFormulaCalculation) return null;

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Edit Data Model Formulas</label>
        <div className={`w-72 h-7 flex items-center px-2 border ${theme.inputBox}`}>
          <button
            type="button"
            className={`text-xs underline ${theme.label}`}
            onClick={() => setOpen(true)}
            title="Click To Edit Formulas"
          >
            <i className="fa-solid fa-pencil mr-1" aria-hidden /> Click To Edit Formulas
          </button>
        </div>
      </div>

      {open ? (
        <ApplicationFormBuilder
          isOpen={true}
          onClose={() => setOpen(false)}
          applicationId={applicationId}
          defaultSectionCode="TransactionFormulaSetting"
          isCreateNewItem={false}
          transactionId={transactionId ?? null}
          transactionType={transactionType}
          modelName={transactionName}
        />
      ) : null}
    </>
  );
}

