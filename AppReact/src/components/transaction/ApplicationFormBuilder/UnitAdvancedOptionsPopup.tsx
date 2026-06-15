/**
 * Unit Advanced Options popup (More Unit Options)
 * Ported from Angular: UnitAdvancedOptionPopup in TransactionGraphicEditor.cshtml
 * Options: IsDisableAddButton, IsDisableDeleteButton, IsPrimaryKeyIdentityInsert, IsExclusiveForOwner
 */
import React from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

export interface UnitAdvancedOptionsPopupProps {
  isOpen: boolean;
  unitDisplayName?: string | null;
  unitData: any | null;
  transactionOrganizedType?: number;
  hasParent: boolean;
  onClose: () => void;
  onPropertyChange: (property: string, value: boolean) => void;
}

const UnitAdvancedOptionsPopup: React.FC<UnitAdvancedOptionsPopupProps> = ({
  isOpen,
  unitDisplayName,
  unitData,
  transactionOrganizedType,
  hasParent,
  onClose,
  onPropertyChange,
}) => {
  const { theme } = useTheme();

  if (!isOpen) return null;

  const showAddDeleteOptions = hasParent || transactionOrganizedType === 2; // List

  return (
    <div
      className="fixed inset-0 z-[10001] flex items-center justify-center bg-black bg-opacity-50"
    >
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: 280, padding: 0 }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Unit Options</div>
          <button
            type="button"
            onClick={onClose}
            className="text-xl leading-none px-2 py-0 hover:opacity-80"
            title="Close"
          >
            &times;
          </button>
        </div>
        <div className="p-3 space-y-3">
          {showAddDeleteOptions && (
            <>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={Boolean(unitData?.IsDisableAddButton)}
                  onChange={(e) => onPropertyChange('IsDisableAddButton', e.target.checked)}
                  className="rounded"
                />
                <span className={`text-xs ${theme.label}`}>Is Disable Add Button</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={Boolean(unitData?.IsDisableDeleteButton)}
                  onChange={(e) => onPropertyChange('IsDisableDeleteButton', e.target.checked)}
                  className="rounded"
                />
                <span className={`text-xs ${theme.label}`}>Is Disable Delete Button</span>
              </label>
            </>
          )}
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={Boolean(unitData?.IsPrimaryKeyIdentityInsert)}
              onChange={(e) => onPropertyChange('IsPrimaryKeyIdentityInsert', e.target.checked)}
              className="rounded"
            />
            <span className={`text-xs ${theme.label}`}>Is Identity Insert</span>
          </label>
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={Boolean(unitData?.IsExclusiveForOwner)}
              onChange={(e) => onPropertyChange('IsExclusiveForOwner', e.target.checked)}
              className="rounded"
            />
            <span className={`text-xs ${theme.label}`}>Is Owner Exclusive</span>
          </label>
        </div>
        <div className="px-4 py-3 border-t flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className={`px-4 py-1.5 text-sm rounded ${theme.button_default}`}
          >
            Ok
          </button>
        </div>
      </div>
    </div>
  );
};

export default UnitAdvancedOptionsPopup;
