/**
 * Entity Data Preview – migrated from AngularJS entityLookUpItemsPreviewCtrl / EntityLookUpItemsPreview.cshtml.
 * Displays lookup items (Id, Display) for an AppEntityInfo. Reusable as full page (route) or inside a popup.
 * Actions: Refresh, Edit Data (when allowed), Configuration (when allowed).
 */
import React, { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import { adminSvc } from '../../../webapi/adminsvc';

const EmAppEntityType = { SystemDefineTable: 1, Enum: 2, DataSet: 3, SimpleValueList: 4 };

export interface EntityDataPreviewProps {
  /** When provided, use this entity Id instead of route param (e.g. when embedded in popup). */
  entityId?: number | string | null;
  /** Optional title override (e.g. for popup). */
  title?: string;
  /** When true, render compact for use inside a modal (optional close button area). */
  asPopup?: boolean;
  /** Called when popup should close (e.g. after Configuration/Edit in popup mode). */
  onClose?: () => void;
  /**
   * Optional override to open "Edit Data" in a DIV popup instead of a tab.
   * When provided, Edit Data will call this callback with the target route.
   */
  onOpenEditDataPopup?: (args: { route: 'simple-value-list-entity-edit' | 'entity-info-edit'; entityId: number; title?: string }) => void;
}

interface EntityDto {
  Id?: number;
  EntityCode?: string;
  EntityType?: number;
  IsSystemDefine?: boolean;
  TableName?: string;
  EntityDataList?: { Id: number; Display?: string }[];
}

const EntityDataPreview: React.FC<EntityDataPreviewProps> = ({
  entityId: entityIdProp,
  title = 'Entity Data Preview',
  asPopup = false,
  onClose,
  onOpenEditDataPopup
}) => {
  const { param } = useParams<{ param?: string }>();
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showWarning } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();

  const entityId = entityIdProp != null
    ? String(entityIdProp)
    : (param ? (() => {
        try {
          const decoded = decodeURIComponent(param);
          const parsed = JSON.parse(decoded);
          return parsed?.id != null ? String(parsed.id) : decoded;
        } catch {
          return param;
        }
      })() : null);
  const idParsed = entityId != null && entityId !== '' ? entityId : null;

  const [entity, setEntity] = useState<EntityDto | null>(null);
  const [entityDataCV, setEntityDataCV] = useState<CollectionView<{ Id: number; Display?: string }> | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const loadData = useCallback(async () => {
    if (!idParsed) {
      setEntity(null);
      setEntityDataCV(null);
      return;
    }
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const dto = await adminSvc.retrieveOneAppEntityInfoExDto(idParsed, true);
      const list = dto?.EntityDataList ?? [];
      setEntity(dto ?? null);
      setEntityDataCV(new CollectionView(list));
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load entity data');
      setEntity(null);
      setEntityDataCV(null);
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [idParsed, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const isAllowEditEntity = (): boolean => {
    if (!entity) return false;
    if (entity.IsSystemDefine || entity.EntityType === EmAppEntityType.Enum) return false;
    return true;
  };

  const isCurrentEntitySimpleList = (): boolean => {
    return entity?.EntityType === EmAppEntityType.SimpleValueList;
  };

  const isAllowEditEntityData = (): boolean => {
    if (!entity) return false;
    if (entity.IsSystemDefine) return false;
    if (entity.EntityType === EmAppEntityType.SimpleValueList) return true;
    if (entity.EntityType === EmAppEntityType.SystemDefineTable && entity.TableName) return true;
    return false;
  };

  const handleRefresh = useCallback(() => {
    loadData();
  }, [loadData]);

  const handleEditEntityData = useCallback(() => {
    if (!entity) return;
    if (entity.IsSystemDefine) {
      showWarning('System defined entity does not allow edit.');
      return;
    }
    if (entity.EntityType === EmAppEntityType.SimpleValueList) {
      if (onOpenEditDataPopup && entity.Id != null) {
        onOpenEditDataPopup({ route: 'simple-value-list-entity-edit', entityId: entity.Id, title: entity.EntityCode || 'Edit Data' });
      } else {
        addTabAndNavigate('simple-value-list-entity-edit', entity.EntityCode || 'Edit Data', { id: entity.Id }, true);
        onClose?.();
      }
    } else if (entity.EntityType === EmAppEntityType.SystemDefineTable && entity.TableName) {
      if (onOpenEditDataPopup && entity.Id != null) {
        onOpenEditDataPopup({ route: 'entity-info-edit', entityId: entity.Id, title: entity.EntityCode || 'Edit Data' });
      } else {
        addTabAndNavigate('entity-info-edit', entity.EntityCode || 'Edit Data', { id: entity.Id }, true);
        onClose?.();
      }
    }
  }, [entity, addTabAndNavigate, showWarning, onClose, onOpenEditDataPopup]);

  const handleConfiguration = useCallback(() => {
    if (!entity) return;
    if (entity.IsSystemDefine) {
      showWarning('System defined entity does not allow edit.');
      return;
    }
    if (entity.EntityType === EmAppEntityType.SimpleValueList) {
      addTabAndNavigate('simple-value-list-entity-edit', entity.EntityCode || 'Configuration', { id: entity.Id }, true);
      onClose?.();
    } else {
      addTabAndNavigate('entity-info-edit', entity.EntityCode || 'Configuration', { id: entity.Id }, true);
      onClose?.();
    }
  }, [entity, addTabAndNavigate, showWarning, onClose]);

  if (idParsed == null) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className={`text-sm ${theme.label}`}>No entity selected. Provide entity Id to preview.</p>
      </div>
    );
  }

  return (
    <div className={`w-full h-full flex flex-col ${theme.mainContentSection}`}>
      <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
        <div className={`text-sm font-medium ${theme.title}`}>
          {title}: {entity?.EntityCode ?? '…'}
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isLoading}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
          </button>
          {isAllowEditEntityData() && (
            <button
              type="button"
              onClick={handleEditEntityData}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              title="Edit Data"
            >
              <i className="fa-solid fa-pencil mr-1" aria-hidden /> Edit Data
            </button>
          )}
          {isAllowEditEntity() && !isCurrentEntitySimpleList() && (
            <button
              type="button"
              onClick={handleConfiguration}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              title="Configuration"
            >
              <i className="fa-solid fa-gear mr-1" aria-hidden /> Configuration
            </button>
          )}
          {asPopup && onClose && (
            <button
              type="button"
              onClick={onClose}
              className={`p-1 rounded ${theme.button_default}`}
              aria-label="Close"
            >
              <i className="fa-solid fa-times" aria-hidden />
            </button>
          )}
        </div>
      </div>
      <div className="w-full h-1 flex-auto min-h-0 overflow-hidden p-2">
        {isLoading ? (
          <div className={`p-4 text-sm ${theme.label}`}>Loading…</div>
        ) : entityDataCV ? (
          <FlexGrid
            className="w-full h-full"
            itemsSource={entityDataCV}
            selectionMode="Row"
            isReadOnly
            autoGenerateColumns={false}
          >
            <FlexGridFilter />
            <FlexGridColumn binding="Id" header="Id" width={100} format="d" />
            <FlexGridColumn binding="Display" header="Display" width="*" />
          </FlexGrid>
        ) : (
          <div className={`p-4 text-sm ${theme.label}`}>No data.</div>
        )}
      </div>
    </div>
  );
};

export default EntityDataPreview;
