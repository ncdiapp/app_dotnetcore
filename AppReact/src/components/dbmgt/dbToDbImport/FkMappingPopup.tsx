import React from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useTheme } from '../../../redux/hooks/useTheme';

type Props = {
  isOpen: boolean;
  fkTableName: string;
  orgValueColumnName: string | null;
  newValueColumnName: string | null;
  orgValueColumnCV: CollectionView<any>;
  newValueColumnCV: CollectionView<any>;
  onChange: (patch: Partial<{ FkTableName: string; OrgValueColumnName: string | null; NewValueColumnName: string | null }>) => void;
  onPickFkTable: () => void;
  onApply: () => void;
  onClose: () => void;
};

const FkMappingPopup: React.FC<Props> = ({
  isOpen,
  fkTableName,
  orgValueColumnName,
  newValueColumnName,
  orgValueColumnCV,
  newValueColumnCV,
  onChange,
  onPickFkTable,
  onApply,
  onClose,
}) => {
  const { theme } = useTheme();
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[1200] flex items-center justify-center bg-black/40">
      <div className={`w-[420px] flex flex-col rounded-md shadow-lg ${theme.mainContentSection}`}>
        <div className="flex items-center justify-between px-3 py-2 border-b">
          <div className={`text-sm font-semibold ${theme.title}`}>Update Mapping From FK Table</div>
          <button type="button" className={`px-2 py-1 text-xs ${theme.button_default}`} onClick={onClose}>
            <i className="fa-solid fa-xmark" aria-hidden />
          </button>
        </div>
        <div className="px-3 py-2 space-y-2 text-xs">
          <div className="flex items-center gap-2">
            <span className={`w-24 ${theme.label}`}>FK Table</span>
            <input className={`h-7 px-2 flex-auto border rounded-[4px] ${theme.inputBox}`} value={fkTableName} readOnly />
            <button type="button" className={`px-2 py-1 rounded-[4px] ${theme.button_default}`} onClick={onPickFkTable} title="Select table">
              <i className="fa-solid fa-plus" aria-hidden />
            </button>
          </div>
          <div>
            <div className={`mb-1 ${theme.label}`}>Original value column</div>
            <div className="h-[140px] w-full overflow-hidden border">
              <FlexGrid
                itemsSource={orgValueColumnCV}
                autoGenerateColumns={false}
                selectionMode="Row"
                headersVisibility="Column"
                className="w-full h-full"
                initialized={(s: any) => {
                  s.selectionChanged.addHandler(() => {
                    const row = s.selection?.row;
                    const item = row != null && row >= 0 ? s.rows[row]?.dataItem : null;
                    if (item?.Name) onChange({ OrgValueColumnName: item.Name });
                  });
                }}
              >
                <FlexGridColumn binding="Name" header="Column" width="*" />
              </FlexGrid>
            </div>
            {orgValueColumnName && <div className="mt-1 opacity-80">Selected: {orgValueColumnName}</div>}
          </div>
          <div>
            <div className={`mb-1 ${theme.label}`}>New value column</div>
            <div className="h-[140px] w-full overflow-hidden border">
              <FlexGrid
                itemsSource={newValueColumnCV}
                autoGenerateColumns={false}
                selectionMode="Row"
                headersVisibility="Column"
                className="w-full h-full"
                initialized={(s: any) => {
                  s.selectionChanged.addHandler(() => {
                    const row = s.selection?.row;
                    const item = row != null && row >= 0 ? s.rows[row]?.dataItem : null;
                    if (item?.Name) onChange({ NewValueColumnName: item.Name });
                  });
                }}
              >
                <FlexGridColumn binding="Name" header="Column" width="*" />
              </FlexGrid>
            </div>
            {newValueColumnName && <div className="mt-1 opacity-80">Selected: {newValueColumnName}</div>}
          </div>
        </div>
        <div className="flex justify-end gap-2 px-3 py-2 border-t">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onApply}>
            Apply
          </button>
        </div>
      </div>
    </div>
  );
};

export default FkMappingPopup;
