/**
 * Full-page wrapper for ApplicationFormBuilder opened in a new tab.
 * Route: application-form-builder/:param (param = JSON with id, transactionId, defaultSectionCode, etc.)
 * onClose closes the current tab and navigates to the newly active tab.
 */

import React, { useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../redux/store';
import { closeTab } from '../../redux/features/ui/navigation/tabnavSlice';
import ApplicationFormBuilder from './ApplicationFormBuilder';

type RouteParams = { param?: string };

const ApplicationFormBuilderPage: React.FC = () => {
  const { param } = useParams<RouteParams>();
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const tabs = useSelector((state: RootState) => state.tabnav.tabs);
  const activeTabKey = useSelector((state: RootState) => state.tabnav.activeTabKey);
  const previousActiveTabKey = useSelector((state: RootState) => state.tabnav.previousActiveTabKey);

  const parsed = useMemo(() => {
    if (!param) return null;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      return obj as {
        id?: string | null;
        transactionId?: number | null;
        defaultSectionCode?: string;
        isCreateNewItem?: boolean;
        transactionType?: number | null;
        dataSourceRegisterId?: number | null;
        isCreateDtoDataModel?: boolean;
        isCreateApiDataModel?: boolean;
        isCreateDataModelView?: boolean;
        modelName?: string | null;
        erDiagramId?: number | null;
      };
    } catch {
      return null;
    }
  }, [param]);

  const handleClose = () => {
    const key = activeTabKey;
    if (key == null) {
      navigate('/home');
      return;
    }
    const remainingTabs = tabs.filter((t) => t.tabKey !== key);
    const prevActive = previousActiveTabKey
      ? remainingTabs.find((t) => t.tabKey === previousActiveTabKey)
      : undefined;
    const newPath = (prevActive?.path ?? remainingTabs[remainingTabs.length - 1]?.path) ?? '/home';
    dispatch(closeTab(key));
    navigate(newPath);
  };

  if (!parsed) {
    return (
      <div className="p-4">
        Invalid or missing parameters. Close this tab to return.
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      <ApplicationFormBuilder
        isOpen={true}
        onClose={handleClose}
        applicationId={parsed.id ?? null}
        defaultSectionCode={parsed.defaultSectionCode ?? 'TransactionGraphicEditor'}
        isCreateNewItem={parsed.isCreateNewItem ?? false}
        transactionId={parsed.transactionId ?? null}
        transactionType={parsed.transactionType ?? null}
        dataSourceRegisterId={parsed.dataSourceRegisterId ?? null}
        isCreateDtoDataModel={parsed.isCreateDtoDataModel ?? false}
        isCreateApiDataModel={parsed.isCreateApiDataModel ?? false}
        isCreateDataModelView={parsed.isCreateDataModelView ?? false}
        modelName={parsed.modelName ?? null}
        erDiagramId={parsed.erDiagramId ?? null}
      />
    </div>
  );
};

export default ApplicationFormBuilderPage;
