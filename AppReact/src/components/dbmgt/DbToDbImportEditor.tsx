/**
 * DB-to-DB table import setting editor – full Angular parity (metaDataTableImportSettingCtrl).
 * Multi-table levels 1/2/3, drag-drop mapping, existing/new table grids, matrix keys, FK mapping.
 */
import React, { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import DatabaseTableSelectorDialog from '../transaction/ApplicationFormBuilder/DatabaseTableSelectorDialog';
import MetaDataTableDesign from '../transaction/metaDataTableDesign';
import TableDataPreview from '../transaction/TableDataPreview';
import { useDbToDbImportEditor } from './dbToDbImport/useDbToDbImportEditor';
import DbToDbImportSourcePanel from './dbToDbImport/DbToDbImportSourcePanel';
import DbToDbImportTablesPanel from './dbToDbImport/DbToDbImportTablesPanel';
import MatrixKeySelectorPopup from './dbToDbImport/MatrixKeySelectorPopup';
import FkMappingPopup from './dbToDbImport/FkMappingPopup';
import EntitySelectorPopup from './dbToDbImport/EntitySelectorPopup';

export interface DbToDbImportEditorProps {
  /** When embedded in a popup, ignore URL `param` so parent route JSON is not mistaken for dataset id. */
  ignoreRouteParam?: boolean;
  /** Import setting (AppDataSet) id when `ignoreRouteParam` is true. */
  dataSetId?: number | null;
  onClose?: () => void;
}

const DbToDbImportEditor: React.FC<DbToDbImportEditorProps> = (props) => {
  const { ignoreRouteParam = false, dataSetId: dataSetIdProp = null } = props;
  const { theme } = useTheme();
  const { param } = useParams<{ param?: string }>();

  const routeDataSetId: number | null = useMemo(() => {
    if (ignoreRouteParam || !param) return null;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      const idValue = obj.id ?? obj.Id ?? null;
      return idValue != null ? Number(idValue) : null;
    } catch {
      const n = Number(param);
      return Number.isFinite(n) ? n : null;
    }
  }, [param, ignoreRouteParam]);

  const dataSetId = dataSetIdProp ?? routeDataSetId;

  const ed = useDbToDbImportEditor(dataSetId);

  if (ed.loadError) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className={`px-3 py-2 text-sm font-semibold ${theme.title}`}>DB to DB Import Editor</div>
        <div className="px-3 py-2 text-sm">{ed.loadError}</div>
      </div>
    );
  }

  if (!ed.dto) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <div className="text-sm opacity-80">Loading…</div>
      </div>
    );
  }

  const { dto, tip, editorApi } = ed;

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex flex-wrap items-center justify-between gap-2 px-3 py-2 mb-1 shrink-0 ${theme.mainContentSection}`}>
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <span className={`text-xs ${theme.label}`}>Import Name</span>
            <input
              className={`h-7 px-2 text-xs border rounded-[4px] w-48 ${theme.inputBox}`}
              value={dto.Name ?? ''}
              onChange={(e) => ed.updateDtoField((d) => { d.Name = e.target.value; })}
            />
          </div>
          <div className="flex items-center gap-2">
            <span className={`text-xs ${theme.label}`}>Description</span>
            <input
              className={`h-7 px-2 text-xs border rounded-[4px] w-48 ${theme.inputBox}`}
              value={dto.Description ?? ''}
              onChange={(e) => ed.updateDtoField((d) => { d.Description = e.target.value; })}
            />
          </div>
          <label className="flex items-center gap-2 text-xs cursor-pointer">
            <input
              type="checkbox"
              checked={!!tip?.IsNeedToCreateImportApi}
              disabled={ed.committed}
              onChange={(e) =>
                ed.updateDtoField((_d, t) => {
                  if (t) t.IsNeedToCreateImportApi = e.target.checked;
                })
              }
            />
            <span className={theme.label}>Create Import API</span>
          </label>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={ed.loadData}>
            <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={ed.saveDraftSetting}>
            <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save Setting
          </button>
          {!ed.needToUpdateTransactionId && (
            <>
              <button
                type="button"
                disabled={ed.isFinalized}
                className={`px-3 py-1.5 text-sm rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
                onClick={() => ed.runImport(false)}
              >
                Simulate Import Data
              </button>
              <button
                type="button"
                disabled={!ed.isDataImported}
                className={`px-3 py-1.5 text-sm rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
                onClick={() => ed.runImport(true)}
              >
                Release Import
              </button>
            </>
          )}
        </div>
      </div>

      <div className={`flex flex-wrap items-center gap-4 px-3 py-2 mb-1 shrink-0 ${theme.mainContentSection}`}>
        {ed.fromDatabaseName && (
          <div className="flex items-center gap-2">
            <span className={`text-xs w-28 ${theme.label}`}>From Database</span>
            <input className={`h-7 px-2 text-xs border rounded-[4px] w-48 ${theme.inputBox}`} value={ed.fromDatabaseName} readOnly />
          </div>
        )}
        {ed.fromTableName && (
          <div className="flex items-center gap-2">
            <span className={`text-xs w-24 ${theme.label}`}>From Table</span>
            <input className={`h-7 px-2 text-xs border rounded-[4px] w-48 ${theme.inputBox}`} value={ed.fromTableName} readOnly />
          </div>
        )}
        {ed.fromDataSetName && (
          <div className="flex items-center gap-2">
            <span className={`text-xs w-24 ${theme.label}`}>From DataSet</span>
            <div className="flex">
              <input className={`h-7 px-2 text-xs border rounded-l-[4px] w-44 ${theme.inputBox}`} value={ed.fromDataSetName} readOnly />
              <button
                type="button"
                className={`h-7 w-7 border border-l-0 rounded-r-[4px] ${theme.button_default}`}
                onClick={ed.editFromDataSet}
                title="Edit dataset"
              >
                <i className="fa-solid fa-pencil text-xs" aria-hidden />
              </button>
            </div>
          </div>
        )}
        {ed.toDatabaseName && (
          <div className="flex items-center gap-2">
            <span className={`text-xs ${theme.label}`}>To Database</span>
            <input className={`h-7 px-2 text-xs border rounded-[4px] w-48 ${theme.inputBox}`} value={ed.toDatabaseName} readOnly />
          </div>
        )}
      </div>

      <div className="w-full h-1 flex-auto overflow-hidden flex gap-1">
        <DbToDbImportSourcePanel
          sourceColumnsCV={ed.sourceColumnsCV}
          dataTypeDataMap={ed.dataTypeDataMap}
          isMultiTable={editorApi.isMultiTable}
          committed={ed.committed}
          onRefreshSource={ed.refreshSourceColumns}
          onPreview={() => editorApi.previewTableData()}
          onDropToSource={editorApi.dropColumnsToSource}
        />
        <DbToDbImportTablesPanel editor={editorApi} />
      </div>

      <DatabaseTableSelectorDialog
        isOpen={ed.tableSelectorOpen}
        dataSourceRegisterId={dto.DataSourceFrom ?? null}
        applicationId={dto.SaasApplicationId != null ? String(dto.SaasApplicationId) : null}
        onClose={() => ed.setTableSelectorOpen(false)}
        onSelect={ed.onDatabaseTableSelected}
      />

      {ed.tableDesignOpen && ed.tableDesignCtx && (
        <MetaDataTableDesign
          isOpen={ed.tableDesignOpen}
          tableName={ed.tableDesignCtx.tableName}
          dataSourceRegisterId={dto.DataSourceFrom ?? null}
          applicationId={dto.SaasApplicationId ?? null}
          onClose={() => {
            ed.setTableDesignOpen(false);
          }}
          onSave={ed.onTableDesignSaved}
          defaultFullscreen
        />
      )}

      {ed.previewOpen && ed.previewTable && (
        <TableDataPreview
          isOpen={ed.previewOpen}
          tableName={ed.previewTable.tableName}
          schemaOwner={ed.previewTable.schemaOwner ?? ''}
          dataSourceRegisterId={ed.previewTable.dataSourceRegisterId ?? null}
          onClose={() => {
            ed.setPreviewOpen(false);
          }}
        />
      )}

      <MatrixKeySelectorPopup
        isOpen={ed.matrixPopupOpen}
        options={ed.matrixOptions}
        onToggle={ed.toggleMatrixOption}
        onApply={ed.applyMatrixKeys}
        onClose={() => ed.setMatrixPopupOpen(false)}
      />

      {ed.fkMappingCtx && (
        <FkMappingPopup
          isOpen={ed.fkMappingOpen}
          fkTableName={ed.fkMappingCtx.FkTableName ?? ''}
          orgValueColumnName={ed.fkMappingCtx.OrgValueColumnName}
          newValueColumnName={ed.fkMappingCtx.NewValueColumnName}
          orgValueColumnCV={ed.fkMappingCtx.orgValueColumnCV}
          newValueColumnCV={ed.fkMappingCtx.newValueColumnCV}
          onChange={ed.patchFkMapping}
          onPickFkTable={ed.openDatabaseTableSelectorForFk}
          onApply={ed.applyFkMapping}
          onClose={() => ed.setFkMappingOpen(false)}
        />
      )}

      <EntitySelectorPopup
        isOpen={ed.entityPopupOpen}
        entities={ed.allEntities}
        onSelect={ed.onEntitySelected}
        onClose={() => ed.setEntityPopupOpen(false)}
      />
    </div>
  );
};

export default DbToDbImportEditor;
