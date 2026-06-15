import React, { useState } from 'react';
import { DataMap } from '@mescius/wijmo.grid';
import { useTheme } from '../../../redux/hooks/useTheme';
import ImportTableBlock from './ImportTableBlock';
import { readDb2DbDragData } from './dragPayload';

type EditorApi = {
  levelOneTable: any | null;
  levelTwoTables: any[];
  levelThreeTables: any[];
  isMultiTable: boolean;
  committed: boolean;
  needToUpdateTransactionId: boolean;
  currentEditTableUiId: string | null;
  revision: number;
  sourceColumnDataMap: DataMap | null;
  getParentColumnDataMap: (parentUiId: string | null | undefined) => DataMap | null;
  dataTypeDataMap: DataMap | null;
  systemTokenDataMap: DataMap | null;
  getEntityCodeById: (id: string | number | null | undefined) => string;
  getForeignMatrixKeyDisplay: (t: any) => string;
  changeCurrentEditTable: (t: any) => void;
  setImportToMultipleTable: () => void;
  setImportToSingleTable: () => void;
  openDatabaseTableSelector: (level: number, parent: any | null) => void;
  createNewTable: (level: number, parent: any | null) => void;
  addNewMatrixTable: () => void;
  dropSourceColumnsOnTable: (tableUiId: string, sourceNames: string[]) => void;
  moveSelectedColumnsBetweenTables: (fromUiId: string, toUiId: string) => void;
  mapSourceToExistingColumn: (tableDto: any, columnDto: any, sourceName: string) => void;
  removeSelectedColumnsFromTable: (tableDto: any) => void;
  autoMapColumns: (tableDto: any) => void;
  editDatabaseTable: (tableDto: any) => void;
  previewTableData: (tableDto: any) => void;
  toggleTableFullScreen: (tableDto: any) => void;
  removeOneTable: (tableDto: any) => void;
  openMatrixKeyPopup: (tableDto: any) => void;
  openFkMappingPopup: (tableDto: any, columnDto: any) => void;
  openEntitySelector: (tableDto: any, columnDto: any) => void;
  clearEntity: (columnDto: any) => void;
  onTableFieldChange: () => void;
  onSelectAllTableColumns: (tableDto: any, checked: boolean) => void;
  onColumnCheckbox: (tableDto: any, columnDto: any) => void;
  onColumnCellEdit: (tableDto: any, columnDto: any, binding: string) => void;
  dropColumnsToSource: (payload: { fromTableUiId: string }) => void;
};

type Props = { editor: EditorApi };

const Level3ParentMenu: React.FC<{
  parents: any[];
  onExisting: (l2: any) => void;
  onCreate: (l2: any) => void;
}> = ({ parents, onExisting, onCreate }) => {
  const { theme } = useTheme();
  const [open, setOpen] = useState(false);
  const [sub, setSub] = useState<'existing' | 'create' | null>(null);
  return (
    <span className="relative inline-block pl-2">
      <button
        type="button"
        className={`px-2 py-0.5 text-xs rounded-[4px] ${theme.button_default}`}
        onClick={() => setOpen((v) => !v)}
      >
        <i className="fa-solid fa-plus" aria-hidden /> Add Table
      </button>
      {open && (
        <ul className={`absolute left-0 top-full mt-1 z-30 min-w-[180px] border rounded shadow text-left ${theme.mainContentSection}`}>
          <li>
            <button type="button" className="w-full px-3 py-1.5 text-xs" onMouseEnter={() => setSub('existing')}>
              Add Existing Table →
            </button>
            {sub === 'existing' && (
              <ul className={`absolute left-full top-0 ml-1 min-w-[200px] border rounded shadow ${theme.mainContentSection}`}>
                {parents.map((l2) => (
                  <li key={l2.UiId}>
                    <button
                      type="button"
                      className="w-full text-left px-3 py-1.5 text-xs"
                      onClick={() => { setOpen(false); setSub(null); onExisting(l2); }}
                    >
                      Parent: {l2.Name}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </li>
          <li>
            <button type="button" className="w-full px-3 py-1.5 text-xs" onMouseEnter={() => setSub('create')}>
              Create New Table →
            </button>
            {sub === 'create' && (
              <ul className={`absolute left-full top-0 ml-1 min-w-[200px] border rounded shadow ${theme.mainContentSection}`}>
                {parents.map((l2) => (
                  <li key={l2.UiId}>
                    <button
                      type="button"
                      className="w-full text-left px-3 py-1.5 text-xs"
                      onClick={() => { setOpen(false); setSub(null); onCreate(l2); }}
                    >
                      Parent: {l2.Name}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </li>
        </ul>
      )}
    </span>
  );
};

const AddTableMenu: React.FC<{
  disabled?: boolean;
  onExisting: () => void;
  onCreate: () => void;
  onMatrix?: () => void;
}> = ({ disabled, onExisting, onCreate, onMatrix }) => {
  const { theme } = useTheme();
  const [open, setOpen] = useState(false);
  return (
    <span className="relative inline-block pl-2">
      <button
        type="button"
        disabled={disabled}
        className={`px-2 py-0.5 text-xs rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
        onClick={() => setOpen((v) => !v)}
      >
        <i className="fa-solid fa-plus" aria-hidden /> Add Table <i className="fa-solid fa-caret-down text-[10px]" aria-hidden />
      </button>
      {open && (
        <ul className={`absolute left-0 top-full mt-1 z-20 min-w-[160px] border rounded shadow ${theme.mainContentSection}`}>
          <li>
            <button type="button" className="w-full text-left px-3 py-1.5 text-xs hover:opacity-90" onClick={() => { setOpen(false); onExisting(); }}>
              Add Existing Table
            </button>
          </li>
          <li>
            <button type="button" className="w-full text-left px-3 py-1.5 text-xs hover:opacity-90" onClick={() => { setOpen(false); onCreate(); }}>
              Create New Table
            </button>
          </li>
          {onMatrix && (
            <li>
              <button type="button" className="w-full text-left px-3 py-1.5 text-xs hover:opacity-90" onClick={() => { setOpen(false); onMatrix(); }}>
                Add New Matrix Table
              </button>
            </li>
          )}
        </ul>
      )}
    </span>
  );
};

const DbToDbImportTablesPanel: React.FC<Props> = ({ editor: ed }) => {
  const { theme } = useTheme();

  const handleDropNew = (tableUiId: string, payload: ReturnType<typeof readDb2DbDragData>) => {
    if (!payload) return;
    if (payload.type === 'source') {
      ed.dropSourceColumnsOnTable(tableUiId, payload.names);
    } else if (payload.type === 'tableColumns') {
      ed.moveSelectedColumnsBetweenTables(payload.fromTableUiId, tableUiId);
    }
  };

  const blockProps = (tableDto: any, level: number) => ({
    tableDto,
    level,
    isSelected: ed.currentEditTableUiId === tableDto.UiId,
    isMultiTable: ed.isMultiTable,
    committed: ed.committed,
    needToUpdateTransactionId: !!ed.needToUpdateTransactionId,
    usageDbToDb: true,
    sourceColumnDataMap: ed.sourceColumnDataMap,
    parentColumnDataMap: ed.getParentColumnDataMap(tableDto.parentTableUiId),
    dataTypeDataMap: ed.dataTypeDataMap,
    systemTokenDataMap: ed.systemTokenDataMap,
    getEntityCodeById: ed.getEntityCodeById,
    matrixKeyDisplay: ed.getForeignMatrixKeyDisplay(tableDto),
    revision: ed.revision,
    onSelectTable: () => ed.changeCurrentEditTable(tableDto),
    onTableNameBlur: ed.onTableFieldChange,
    onTransformChange: (v: string) => {
      tableDto.TransformCondition = v;
      ed.onTableFieldChange();
    },
    onAutoMap: () => ed.autoMapColumns(tableDto),
    onEditTable: () => ed.editDatabaseTable(tableDto),
    onPreview: () => ed.previewTableData(tableDto),
    onToggleFullscreen: () => ed.toggleTableFullScreen(tableDto),
    onRemove: () => ed.removeOneTable(tableDto),
    onOpenMatrixKeys: tableDto.IsMatrixTable ? () => ed.openMatrixKeyPopup(tableDto) : undefined,
    onDropNewTable: handleDropNew,
    onDropExistingTable: (_uiId: string, col: any, src: string) => ed.mapSourceToExistingColumn(tableDto, col, src),
    onOpenFkMapping: (col: any) => ed.openFkMappingPopup(tableDto, col),
    onOpenEntity: (col: any) => ed.openEntitySelector(tableDto, col),
    onClearEntity: (col: any) => ed.clearEntity(col),
    onColumnCheckbox: (col: any) => ed.onColumnCheckbox(tableDto, col),
    onSelectAllColumns: (checked: boolean) => ed.onSelectAllTableColumns(tableDto, checked),
    onCellEdit: (col: any, binding: string) => ed.onColumnCellEdit(tableDto, col, binding),
  });

  const levelWidth = ed.isMultiTable ? 'w-[500px]' : 'w-[1180px]';

  return (
    <div className={`w-1 flex-auto h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      <div className="flex items-center px-3 py-2 shrink-0 gap-3">
        <div className={`text-sm font-semibold ${theme.title} shrink-0`}>
          {ed.isMultiTable ? 'Import To Tables' : 'Import To Table'}
        </div>
        {!ed.needToUpdateTransactionId && !ed.isMultiTable && (
          <AddTableMenu
            disabled={!!ed.levelOneTable}
            onExisting={() => ed.openDatabaseTableSelector(1, null)}
            onCreate={() => ed.createNewTable(1, null)}
          />
        )}
        <div className="flex items-center gap-1 text-xs text-gray-500">
          <span>Import To Multi-Tables</span>
          <button
            type="button"
            className="text-2xl leading-none px-1"
            disabled={ed.committed}
            onClick={() => (ed.isMultiTable ? ed.setImportToSingleTable() : ed.setImportToMultipleTable())}
            title={ed.isMultiTable ? 'Switch to single table' : 'Switch to multi-table'}
          >
            <i className={`fa-solid ${ed.isMultiTable ? 'fa-toggle-on' : 'fa-toggle-off'}`} aria-hidden />
          </button>
        </div>
      </div>

      <div className="w-full h-1 flex-auto overflow-auto px-2 pb-2">
        <div className="min-w-[1500px] h-full relative">
          {ed.isMultiTable && (
            <div className="flex h-[25px] bg-white border-b shrink-0 sticky top-0 z-10">
              <div className={`${levelWidth} text-center text-xs font-semibold py-1`}>
                Level 1 Table
                <AddTableMenu
                  disabled={!!ed.levelOneTable}
                  onExisting={() => ed.openDatabaseTableSelector(1, null)}
                  onCreate={() => ed.createNewTable(1, null)}
                />
              </div>
              <div className={`${levelWidth} text-center text-xs font-semibold py-1 border-l border-dashed border-indigo-200`}>
                Level 2 Tables ({ed.levelTwoTables.length})
                {ed.levelOneTable && (
                  <AddTableMenu
                    onExisting={() => ed.openDatabaseTableSelector(2, ed.levelOneTable)}
                    onCreate={() => ed.createNewTable(2, ed.levelOneTable)}
                    onMatrix={ed.levelTwoTables.length > 0 ? () => ed.addNewMatrixTable() : undefined}
                  />
                )}
              </div>
              <div className={`${levelWidth} text-center text-xs font-semibold py-1 border-l border-dashed border-indigo-200`}>
                Level 3 Tables ({ed.levelThreeTables.length})
                {ed.levelTwoTables.length > 0 && (
                  <Level3ParentMenu
                    parents={ed.levelTwoTables}
                    onExisting={(l2) => ed.openDatabaseTableSelector(3, l2)}
                    onCreate={(l2) => ed.createNewTable(3, l2)}
                  />
                )}
              </div>
            </div>
          )}

          <div className="flex h-full border border-dashed border-indigo-200 min-h-[400px]">
            <div className={`${levelWidth} flex-none h-full border-r border-dashed border-indigo-200 overflow-auto`}>
              {ed.levelOneTable ? (
                <ImportTableBlock {...blockProps(ed.levelOneTable, 1)} />
              ) : (
                !ed.isMultiTable && (
                  <div className={`p-4 text-xs ${theme.label}`}>Add a level-1 import table to begin mapping.</div>
                )
              )}
            </div>

            {ed.isMultiTable && (
              <>
                <div className={`${levelWidth} flex-none h-full border-r border-dashed border-indigo-200 overflow-auto`}>
                  {ed.levelTwoTables.map((t) => (
                    <div key={t.UiId} className="h-[500px] border-b border-dashed border-indigo-200">
                      <ImportTableBlock {...blockProps(t, 2)} />
                    </div>
                  ))}
                </div>
                <div className={`${levelWidth} flex-none h-full overflow-auto`}>
                  {ed.levelThreeTables.map((t) => (
                    <div key={t.UiId} className="h-[500px] border-b border-dashed border-indigo-200">
                      <ImportTableBlock {...blockProps(t, 3)} />
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default DbToDbImportTablesPanel;
