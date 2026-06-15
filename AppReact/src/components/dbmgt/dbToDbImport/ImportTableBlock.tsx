import React, { useCallback, useMemo, useRef } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, DataType } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useTheme } from '../../../redux/hooks/useTheme';
import { readDb2DbDragData, setDb2DbDragData } from './dragPayload';
import { filterImportTableColumns } from './dbToDbImportEditorModel';

type Props = {
  tableDto: any;
  level: number;
  isSelected: boolean;
  isMultiTable: boolean;
  committed: boolean;
  needToUpdateTransactionId: boolean;
  usageDbToDb: boolean;
  sourceColumnDataMap: DataMap | null;
  parentColumnDataMap: DataMap | null;
  dataTypeDataMap: DataMap | null;
  systemTokenDataMap: DataMap | null;
  getEntityCodeById: (id: string | number | null | undefined) => string;
  matrixKeyDisplay?: string;
  onSelectTable: () => void;
  onTableNameBlur: () => void;
  onTransformChange: (v: string) => void;
  onAutoMap: () => void;
  onEditTable: () => void;
  onPreview: () => void;
  onToggleFullscreen: () => void;
  onRemove: () => void;
  onOpenMatrixKeys?: () => void;
  onDropNewTable: (tableUiId: string, payload: ReturnType<typeof readDb2DbDragData>) => void;
  onDropExistingTable: (tableUiId: string, targetColumn: any, sourceName: string) => void;
  onOpenFkMapping: (columnDto: any) => void;
  onOpenEntity: (columnDto: any) => void;
  onClearEntity: (columnDto: any) => void;
  onColumnCheckbox: (columnDto: any) => void;
  onSelectAllColumns: (checked: boolean) => void;
  onCellEdit: (columnDto: any, binding: string, value: unknown) => void;
  revision: number;
};

const ImportTableBlock: React.FC<Props> = ({
  tableDto,
  level,
  isSelected,
  isMultiTable,
  committed,
  needToUpdateTransactionId,
  usageDbToDb,
  sourceColumnDataMap,
  parentColumnDataMap,
  dataTypeDataMap,
  systemTokenDataMap,
  getEntityCodeById,
  matrixKeyDisplay,
  onSelectTable,
  onTableNameBlur,
  onTransformChange,
  onAutoMap,
  onEditTable,
  onPreview,
  onToggleFullscreen,
  onRemove,
  onOpenMatrixKeys,
  onDropNewTable,
  onDropExistingTable,
  onOpenFkMapping,
  onOpenEntity,
  onClearEntity,
  onColumnCheckbox,
  onSelectAllColumns,
  onCellEdit,
  revision,
}) => {
  const { theme } = useTheme();
  const gridRef = useRef<any>(null);
  const hoverColumnRef = useRef<any>(null);

  const columnCV = useMemo(() => {
    const cols = tableDto.IsImportToExistingTable
      ? tableDto.Columns || []
      : filterImportTableColumns(tableDto.Columns);
    return new CollectionView(cols);
  }, [tableDto, revision]);

  const containerStyle = useMemo(() => {
    if (!isMultiTable) return {};
    if (tableDto.isEditOnFullScreen) {
      return { position: 'absolute' as const, top: 0, left: 0, width: '100%', height: '100%', zIndex: 50 };
    }
    return {};
  }, [isMultiTable, tableDto.isEditOnFullScreen]);

  const borderStyle = isMultiTable && isSelected ? { border: 'solid 2px black' } : { border: 'solid 1px rgb(214, 212, 244)' };

  const onDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'copy';
  };

  const onDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const payload = readDb2DbDragData(e);
    if (!payload) return;
    if (tableDto.IsImportToExistingTable) {
      if (payload.type === 'source' && payload.names[0] && hoverColumnRef.current) {
        onDropExistingTable(tableDto.UiId, hoverColumnRef.current, payload.names[0]);
      }
    } else {
      onDropNewTable(tableDto.UiId, payload);
    }
  };

  const startColumnDrag = (e: React.DragEvent, columnDto: any) => {
    if (committed || !columnDto.isSelected || columnDto.IsPrimaryKey || columnDto.IsForeignKey) {
      e.preventDefault();
      return;
    }
    setDb2DbDragData(e, {
      type: 'tableColumns',
      fromTableUiId: tableDto.UiId,
      columnNames: [columnDto.Name],
    });
  };

  const renderNewTableColumns = () => (
    <FlexGrid
      ref={gridRef}
      itemsSource={columnCV}
      autoGenerateColumns={false}
      selectionMode="ListBox"
      headersVisibility="Column"
      allowSorting={false}
      className="w-full h-full !border-0"
      cellEditEnded={(s: any, e: any) => {
        const col = s.columns[e.col];
        const item = s.rows[e.row]?.dataItem;
        if (item && col?.binding) onCellEdit(item, col.binding, item[col.binding]);
      }}
    >
      {!needToUpdateTransactionId && (
        <FlexGridColumn header="" width={28} isReadOnly>
          <FlexGridCellTemplate
            cellType="ColumnHeader"
            template={() => (
              <input
                type="checkbox"
                checked={!!tableDto.isSelectAllColumn}
                onChange={(ev) => onSelectAllColumns(ev.target.checked)}
              />
            )}
          />
          <FlexGridCellTemplate
            cellType="Cell"
            template={(ctx: any) => (
              <input
                type="checkbox"
                checked={!!ctx.item?.isSelected}
                onChange={() => onColumnCheckbox(ctx.item)}
              />
            )}
          />
        </FlexGridColumn>
      )}
      <FlexGridColumn binding="Name" header="Column Name" width={150} isReadOnly>
        <FlexGridCellTemplate
          cellType="Cell"
          template={(ctx: any) => {
            const item = ctx.item;
            const selected = !!item?.isSelected;
            return (
              <div
                className={`flex items-center gap-1 px-1 py-0.5 rounded ${selected ? 'bg-indigo-400 text-white cursor-grab' : ''}`}
                draggable={selected && !committed}
                onDragStart={(e) => startColumnDrag(e, item)}
              >
                {selected && <i className="fa-solid fa-arrow-right text-[10px]" aria-hidden />}
                <span className="truncate">{item?.Name}</span>
              </div>
            );
          }}
        />
      </FlexGridColumn>
      <FlexGridColumn binding="IsLogicKey" header="Logic Key" width={70} dataType={DataType.Boolean} />
      <FlexGridColumn binding="Tag" header="Data Type" width={80} dataMap={dataTypeDataMap} isReadOnly />
      <FlexGridColumn binding="Length" header="Length" width={90} dataType={DataType.Number} />
      <FlexGridColumn binding="Precision" header="Precision" width={90} dataType={DataType.Number} />
      <FlexGridColumn binding="Scale" header="Scale" width={90} dataType={DataType.Number} />
      <FlexGridColumn header="Mapping Entity" width={140} isReadOnly>
        <FlexGridCellTemplate
          cellType="Cell"
          template={(ctx: any) => (
            <div className="flex items-center gap-1 text-xs">
              {!committed && (ctx.item?.isNew || !committed) && (
                <button type="button" className="px-1" onClick={() => onOpenEntity(ctx.item)} title="Select Entity">
                  <i className="fa-solid fa-plus" aria-hidden />
                </button>
              )}
              {ctx.item?.EntityId && !committed && (
                <button type="button" className="text-red-700 px-1" onClick={() => onClearEntity(ctx.item)} title="Clear">
                  <i className="fa-solid fa-xmark" aria-hidden />
                </button>
              )}
              <span className="truncate" title={getEntityCodeById(ctx.item?.EntityId)}>
                {getEntityCodeById(ctx.item?.EntityId)}
              </span>
            </div>
          )}
        />
      </FlexGridColumn>
      <FlexGridColumn binding="EntityColumnName" header="Entity Column" width={120} isReadOnly />
      <FlexGridColumn binding="" header="" width="*" />
    </FlexGrid>
  );

  const renderExistingTableColumns = () => (
    <FlexGrid
      ref={gridRef}
      itemsSource={columnCV}
      autoGenerateColumns={false}
      selectionMode="ListBox"
      headersVisibility="Column"
      allowSorting={false}
      className="w-full h-full !border-0"
      formatItem={(s: any, e: any) => {
        if (e.panel === s.cells && e.row >= 0) {
          const item = s.rows[e.row]?.dataItem;
          if (item) hoverColumnRef.current = item;
        }
      }}
      cellEditEnded={(s: any, ev: any) => {
        const col = s.columns[ev.col];
        const item = s.rows[ev.row]?.dataItem;
        if (item && col?.binding) {
          onCellEdit(item, col.binding, item[col.binding]);
          if (col.binding === 'MapToSourceColumnName' && tableDto.DictExistingTableColumnNameAndImportMappingDto?.[item.Name]) {
            tableDto.DictExistingTableColumnNameAndImportMappingDto[item.Name].MapToSourceColumnName = item.MapToSourceColumnName;
          }
        }
      }}
    >
      <FlexGridColumn binding="Name" header="Column Name" width={180} isReadOnly />
      <FlexGridColumn binding="IsLogicKey" header="Logic Key" width={70} dataType={DataType.Boolean} />
      <FlexGridColumn
        binding="MapToSourceColumnName"
        header="Map To Source Column"
        width={180}
        dataMap={sourceColumnDataMap}
      />
      {level > 1 && (
        <FlexGridColumn
          binding="LinkToParentTablePkColumnName"
          header="Link To Parent"
          width={150}
          dataMap={parentColumnDataMap}
        />
      )}
      <FlexGridColumn header="Update Mapping FK" width={220} isReadOnly>
        <FlexGridCellTemplate
          cellType="Cell"
          template={(ctx: any) => {
            if (!ctx.item?.MapToSourceColumnName) return null;
            const mapping = tableDto.DictColumnNameAndUpdateMappingDto?.[ctx.item.Name];
            return (
              <div className="flex items-center gap-1 text-xs w-full">
                <button type="button" className="px-1" onClick={() => onOpenFkMapping(ctx.item)}>
                  <i className="fa-solid fa-plus" aria-hidden />
                </button>
                <span className="truncate" title={mapping?.MappingDisplay}>
                  {mapping?.MappingDisplay}
                </span>
              </div>
            );
          }}
        />
      </FlexGridColumn>
      <FlexGridColumn binding="Nullable" header="Nullable" width={80} dataType={DataType.Boolean} isReadOnly />
      <FlexGridColumn binding="SystemToken" header="System Token" width={120} dataMap={systemTokenDataMap} />
      <FlexGridColumn binding="DefaultValue" header="DB Default" width={120} isReadOnly />
      <FlexGridColumn binding="OverrideDefaultValue" header="Override Default" width={120} />
      <FlexGridColumn binding="Tag" header="Data Type" width={80} dataMap={dataTypeDataMap} isReadOnly />
      <FlexGridColumn binding="" header="" width="*" />
    </FlexGrid>
  );

  return (
    <div
      className="w-full h-full p-2"
      style={containerStyle}
      onClick={onSelectTable}
    >
      <div className="w-full h-full flex flex-col relative" style={borderStyle}>
        <div className="flex items-center gap-1 px-1 py-0.5 shrink-0">
          <span className="text-xs w-[90px] shrink-0">Table Name</span>
          <input
            className={`h-6 px-2 text-xs flex-auto border rounded-[4px] ${theme.inputBox}`}
            value={tableDto.Name ?? ''}
            disabled={committed || usageDbToDb}
            onChange={(e) => {
              tableDto.Name = e.target.value;
            }}
            onBlur={onTableNameBlur}
            onClick={(e) => e.stopPropagation()}
          />
          {!needToUpdateTransactionId && (
            <div className="flex items-center gap-0.5 shrink-0" onClick={(e) => e.stopPropagation()}>
              {tableDto.IsImportToExistingTable && (
                <button type="button" className={`w-6 h-6 text-xs ${theme.button_default}`} title="Auto map" onClick={onAutoMap}>
                  <i className="fa-solid fa-copy" aria-hidden />
                </button>
              )}
              <button type="button" className={`w-6 h-6 text-xs ${theme.button_default}`} title="Edit table" onClick={onEditTable}>
                <i className="fa-solid fa-database" aria-hidden />
              </button>
              <button type="button" className={`w-6 h-6 text-xs ${theme.button_default}`} title="Preview" onClick={onPreview}>
                <i className="fa-solid fa-eye" aria-hidden />
              </button>
              <button type="button" className={`w-6 h-6 text-xs ${theme.button_default}`} title="Fullscreen" onClick={onToggleFullscreen}>
                <i className="fa-solid fa-maximize" aria-hidden />
              </button>
              <button
                type="button"
                className={`w-6 h-6 text-xs ${theme.button_default}`}
                title="Remove"
                disabled={committed}
                onClick={onRemove}
              >
                <i className="fa-solid fa-trash" aria-hidden />
              </button>
            </div>
          )}
        </div>
        <div className="flex items-center gap-1 px-1 py-0.5 shrink-0">
          <span className="text-xs w-[90px] shrink-0">Transform</span>
          <input
            className={`h-6 px-2 text-xs flex-auto border rounded-[4px] ${theme.inputBox}`}
            value={tableDto.TransformCondition ?? ''}
            disabled={committed}
            onChange={(e) => onTransformChange(e.target.value)}
            onClick={(e) => e.stopPropagation()}
          />
        </div>
        {level >= 2 && (
          <div className="px-2 py-1 text-xs shrink-0 max-h-[50px] overflow-auto">
            {tableDto.foreignLogicKeyDisplay && (
              <div>
                <span className="font-medium">Foreign Logic Keys: </span>
                <span className="italic text-gray-500">{tableDto.foreignLogicKeyDisplay}</span>
              </div>
            )}
            {tableDto.IsMatrixTable && (
              <div className="flex items-start gap-1 mt-1">
                <span className="font-medium shrink-0">Foreign Matrix Keys</span>
                {onOpenMatrixKeys && (
                  <button type="button" className={`px-1 ${theme.button_default}`} onClick={(e) => { e.stopPropagation(); onOpenMatrixKeys(); }}>
                    <i className="fa-solid fa-plus" aria-hidden />
                  </button>
                )}
                <span className="italic text-gray-500 truncate" title={matrixKeyDisplay}>
                  {matrixKeyDisplay}
                </span>
              </div>
            )}
          </div>
        )}
        <div
          id={tableDto.UiId}
          className="w-full h-1 flex-auto overflow-hidden bg-white"
          onDragOver={onDragOver}
          onDrop={onDrop}
        >
          {tableDto.IsImportToExistingTable ? renderExistingTableColumns() : renderNewTableColumns()}
        </div>
      </div>
    </div>
  );
};

export default ImportTableBlock;
