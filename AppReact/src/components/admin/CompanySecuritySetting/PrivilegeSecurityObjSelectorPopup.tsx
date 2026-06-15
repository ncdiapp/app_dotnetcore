import React, { useRef } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';

type Props = {
  open: boolean;
  title: string;
  availableItemsCV: CollectionView;
  gridRef?: React.RefObject<any>;
  onClose: () => void;
  onAdd: () => void;
};

/**
 * Popup to select security objects from "All {DisplayName}" grid (multi-select). OK adds selected to privileges.
 * Matches Angular SecurityObjSelectorPopup.
 */
const PrivilegeSecurityObjSelectorPopup: React.FC<Props> = ({
  open,
  title,
  availableItemsCV,
  gridRef: externalGridRef,
  onClose,
  onAdd,
}) => {
  const { theme } = useTheme();
  const internalRef = useRef<any>(null);
  const gridRef = externalGridRef ?? internalRef;

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={onClose}>
      <div
        className={`${theme.mainContentSection} rounded shadow-xl flex flex-col overflow-hidden`}
        style={{ width: 400, height: 520 }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <span className={`text-md font-semibold ${theme.title}`}>All {title}</span>
          <button type="button" className="text-lg leading-none w-8 h-8" onClick={onClose} aria-label="Close">
            &times;
          </button>
        </div>
        <div className="w-full h-1 flex-auto overflow-hidden p-2">
          <FlexGrid
            ref={gridRef}
            itemsSource={availableItemsCV}
            selectionMode="ListBox"
            headersVisibility="Column"
            isReadOnly={true}
            className="h-full"
          >
            <FlexGridColumn header="Name" binding="Display" width="*" />
          </FlexGrid>
        </div>
        <div className="flex justify-end gap-2 px-3 py-2 border-t">
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={onAdd}
          >
            Ok
          </button>
        </div>
      </div>
    </div>
  );
};

export default PrivilegeSecurityObjSelectorPopup;
