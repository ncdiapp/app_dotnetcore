import React, { useCallback } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { clampContextMenuPosition } from '../../hooks/useClampedContextMenuPosition';
import { fileThumbnailUrl } from '../../webapi/fileEndpoints';

const EM_APP_CONTROL_TYPE_IMAGE = 5;
const FILE_CONTEXT_MENU_ESTIMATED_WIDTH = 200;
const FILE_CONTEXT_MENU_ESTIMATED_HEIGHT = 400;

export type FileViewItem = {
  Id?: number;
  Display?: string;
  FileName?: string;
  FileSize?: number;
  CreatedDate?: string;
  ModifiedDate?: string;
  CreatedByUserId?: number;
  ModifiedByUserId?: number;
  DictViewColumnIDKeyValue?: Record<number | string, unknown>;
  DictSketchOrFileDisplayCode?: Record<number | string, string>;
  [key: string]: any;
};

type ViewField = {
  Id?: number;
  SearchViewFieldID?: number;
  DisplayText?: string;
  Name?: string;
  ControlType?: number;
};

/** Stable thumbnail cell — avoids img reload when only grid selection changes elsewhere. */
const FileThumbnailCell = React.memo(({ fileId }: { fileId: number }) => (
  <div className="flex items-center justify-center w-[60px] h-[60px]">
    <img
      src={fileThumbnailUrl(fileId)}
      alt=""
      className="max-h-[60px] max-w-[60px] object-contain"
    />
  </div>
));
FileThumbnailCell.displayName = 'FileThumbnailCell';

export type FolderFileGridProps = {
  loading: boolean;
  filesCV: CollectionView<FileViewItem> | null;
  viewFields: ViewField[];
  userDataMap: any;
  menuButtonClass: string;
  fileGridRef: React.RefObject<any>;
  onSelectionChanged: (item: FileViewItem | null) => void;
  onOpenFileContextMenu: (item: FileViewItem, x: number, y: number) => void;
};

const FolderFileGridInner: React.FC<FolderFileGridProps> = ({
  loading,
  filesCV,
  viewFields,
  userDataMap,
  menuButtonClass,
  fileGridRef,
  onSelectionChanged,
  onOpenFileContextMenu,
}) => {
  const handleSelectionChanged = useCallback(
    (s: any) => {
      const flex = s?.control ?? s;
      const row = flex?.selection?.row;
      const item =
        row != null && flex?.rows?.[row] != null ? (flex.rows[row] as any).dataItem as FileViewItem : null;
      onSelectionChanged(item ?? null);
    },
    [onSelectionChanged]
  );

  if (loading) {
    return <p className="text-xs p-4 opacity-70">Loading...</p>;
  }
  if (!filesCV) {
    return <p className="text-xs p-4 opacity-70">No files found.</p>;
  }

  return (
    <FlexGrid
      ref={fileGridRef}
      itemsSource={filesCV}
      selectionMode="ListBox"
      headersVisibility="Column"
      isReadOnly={true}
      style={{ height: '100%' }}
      selectionChanged={handleSelectionChanged}
    >
      <FlexGridColumn header="Actions" width={60} isReadOnly>
        <FlexGridCellTemplate
          cellType="Cell"
          template={(cell: any) => {
            const item = cell.item as FileViewItem;
            return (
              <div className="flex items-center justify-center w-full">
                <button
                  type="button"
                  className={`${menuButtonClass} w-8 h-6 flex items-center justify-center`}
                  title="More Options"
                  onMouseDown={(e) => {
                    e.stopPropagation();
                  }}
                  onClick={(e) => {
                    e.stopPropagation();
                    const rect = e.currentTarget.getBoundingClientRect();
                    const { x, y } = clampContextMenuPosition(
                      rect.right,
                      rect.top,
                      FILE_CONTEXT_MENU_ESTIMATED_WIDTH,
                      FILE_CONTEXT_MENU_ESTIMATED_HEIGHT
                    );
                    onOpenFileContextMenu(item, x, y);
                  }}
                >
                  <i className="fa-solid fa-pencil text-xs" aria-hidden />
                  <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                </button>
              </div>
            );
          }}
        />
      </FlexGridColumn>
      {viewFields.length > 0
        ? viewFields.map((field, idx) => {
            const fieldId = field.Id ?? field.SearchViewFieldID;
            const header = field.DisplayText ?? field.Name ?? `Column ${fieldId}`;
            const isImageColumn = field.ControlType === EM_APP_CONTROL_TYPE_IMAGE;
            const width = isImageColumn ? 70 : (idx === 0 ? 200 : 120);
            return (
              <FlexGridColumn key={fieldId ?? header} header={header} width={width}>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const item = cell.item as FileViewItem;
                    const dict = item?.DictViewColumnIDKeyValue ?? (item as any)?.dictViewColumnIDKeyValue;
                    const sketchDict = item?.DictSketchOrFileDisplayCode ?? (item as any)?.dictSketchOrFileDisplayCode;
                    const raw = dict && fieldId != null ? (dict[fieldId] ?? dict[String(fieldId)]) : null;
                    if (isImageColumn) {
                      const fileId = raw != null ? Number(raw) : null;
                      if (fileId == null) return <div className="w-[60px] h-[60px]" />;
                      return <FileThumbnailCell fileId={fileId} />;
                    }
                    const display = sketchDict && fieldId != null ? (sketchDict[fieldId] ?? sketchDict[String(fieldId)]) : null;
                    const value = display ?? raw;
                    return <span className="truncate block">{value != null ? String(value) : ''}</span>;
                  }}
                />
              </FlexGridColumn>
            );
          })
        : (
          <>
            <FlexGridColumn binding="Display" header="File Name" width="*" />
            <FlexGridColumn binding="FileName" header="Name" width={120} />
            <FlexGridColumn binding="FileSize" header="Size" width={80} />
            <FlexGridColumn binding="CreatedDate" header="Created" width={120} />
            <FlexGridColumn binding="ModifiedDate" header="Modified" width={120} />
            <FlexGridColumn binding="CreatedByUserId" header="Created By" width={120} dataMap={userDataMap} />
          </>
        )}
      <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
    </FlexGrid>
  );
};

/** Isolated file grid — does not re-render when preview panel selection state changes in the parent. */
export const FolderFileGrid = React.memo(FolderFileGridInner);
