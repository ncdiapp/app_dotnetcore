import React from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, DataType } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setDb2DbDragData } from './dragPayload';

type Props = {
  sourceColumnsCV: CollectionView<any>;
  dataTypeDataMap: DataMap | null;
  isMultiTable: boolean;
  committed: boolean;
  onRefreshSource: () => void;
  onPreview: () => void;
  onDropToSource: (payload: { fromTableUiId: string }) => void;
};

const DbToDbImportSourcePanel: React.FC<Props> = ({
  sourceColumnsCV,
  dataTypeDataMap,
  isMultiTable,
  committed,
  onRefreshSource,
  onPreview,
  onDropToSource,
}) => {
  const { theme } = useTheme();

  const onDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const onDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (committed) return;
    try {
      const raw = e.dataTransfer.getData('application/json');
      if (!raw) return;
      const o = JSON.parse(raw);
      if (o?.type === 'tableColumns') onDropToSource(o);
    } catch {
      /* ignore */
    }
  };

  const startSourceDrag = (e: React.DragEvent, name: string) => {
    if (committed) {
      e.preventDefault();
      return;
    }
    setDb2DbDragData(e, { type: 'source', names: [name] });
    document.body.style.cursor = 'grabbing';
  };

  const onDragEnd = () => {
    document.body.style.cursor = '';
  };

  return (
    <div
      className={`w-[400px] flex-none h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}
      onDragOver={onDragOver}
      onDrop={onDrop}
    >
      <div className="flex items-center justify-between px-3 py-2 shrink-0">
        <div className={`text-sm font-semibold ${theme.title}`}>Source Columns</div>
        <div className="flex items-center gap-2">
          <button type="button" className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`} onClick={onRefreshSource}>
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh Source
          </button>
          <button type="button" className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`} onClick={onPreview}>
            <i className="fa-solid fa-database" aria-hidden /> Preview Data
          </button>
        </div>
      </div>
      {isMultiTable && (
        <div className={`px-3 pb-1 text-xs text-gray-400`}>Drag &amp; drop columns to a target table on the right</div>
      )}
      <div className="w-full h-1 flex-auto overflow-hidden px-3 pb-3">
        <FlexGrid
          itemsSource={sourceColumnsCV}
          autoGenerateColumns={false}
          selectionMode="ListBox"
          isReadOnly
          className="w-full h-full !border-0"
        >
          <FlexGridColumn binding="Name" header="Column Name" width={220} isReadOnly>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: any) => {
                const name = String(ctx.item?.Name ?? '');
                return (
                  <div
                    className="flex items-center gap-1 px-1 cursor-grab"
                    draggable={!committed}
                    onDragStart={(e) => startSourceDrag(e, name)}
                    onDragEnd={onDragEnd}
                    title="Drag to import table"
                  >
                    <i className="fa-solid fa-arrow-right text-[10px] text-blue-600" aria-hidden />
                    <span className="truncate">{name}</span>
                  </div>
                );
              }}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="Tag" header="Data Type" width={90} dataMap={dataTypeDataMap} isReadOnly />
          <FlexGridColumn binding="TableName" header="Table" width={120} isReadOnly />
          <FlexGridColumn binding="" header="" width="*" />
        </FlexGrid>
      </div>
    </div>
  );
};

export default DbToDbImportSourcePanel;
