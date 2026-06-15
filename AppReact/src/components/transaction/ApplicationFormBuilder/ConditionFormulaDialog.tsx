/**
 * Simple Condition Formula dialog for Conditional Lock or Hide.
 * Angular opens TransactionFieldFormulaEditor (Condition Formula Builder); this provides a text input for condition formula.
 */

import React, { useEffect, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

export interface ConditionFormulaDialogProps {
  isOpen: boolean;
  initialFormulaText: string;
  onClose: () => void;
  onConfirm: (formulaText: string) => void;
}

const ConditionFormulaDialog: React.FC<ConditionFormulaDialogProps> = ({
  isOpen,
  initialFormulaText,
  onClose,
  onConfirm,
}) => {
  const { theme } = useTheme();
  const [formulaText, setFormulaText] = useState(initialFormulaText ?? '');

  useEffect(() => {
    if (isOpen) {
      setFormulaText(initialFormulaText ?? '');
    }
  }, [isOpen, initialFormulaText]);

  const handleConfirm = () => {
    onConfirm(formulaText.trim());
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[10000] flex items-center justify-center bg-black/50" onClick={(e) => e.stopPropagation()}>
      <div
        className={`max-w-lg w-full rounded-t-md rounded-b-md border shadow-lg ${theme.mainContentSection}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-1.5 border-b ${theme.modalHeader}`}>
          <h3 className={`text-sm font-semibold ${theme.title}`}>Condition Formula Builder</h3>
        </div>
        <div className="p-3">
          <label className={`block text-xs mb-0.5 ${theme.label}`}>Condition Formula</label>
          <textarea
            className={`w-full min-h-[100px] px-2 py-1 text-xs border rounded ${theme.inputBox} resize-none focus:outline-none`}
            value={formulaText}
            onChange={(e) => setFormulaText(e.target.value)}
            placeholder="e.g. [FieldName_123] = true"
          />
        </div>
        <div className={`px-3 py-1.5 border-t flex justify-end space-x-2 ${theme.mainContentSection}`}>
          <button type="button" onClick={onClose} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Cancel">
            <i className="fa fa-times" aria-hidden="true" /> Cancel
          </button>
          <button type="button" onClick={handleConfirm} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="OK">
            <i className="fa fa-check" aria-hidden="true" /> OK
          </button>
        </div>
      </div>
    </div>
  );
};

export default ConditionFormulaDialog;
