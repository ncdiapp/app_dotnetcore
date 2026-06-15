import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import RoleEditorModal from './RoleEditorModal';

type Props = { companyId: string | number | null; isEmbedded?: boolean };

/** GroupUsage for Contact Group — align with Angular companyContactGroupSetupCtrl groupUsage_ContactGroup = 3 */
const GROUP_USAGE_CONTACT_GROUP = 3;

const CompanyContactGroupSetup: React.FC<Props> = ({ companyId, isEmbedded: _isEmbedded }) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [groupsCV] = useState(() => new CollectionView<any>([]));
  const [, setRefreshTrigger] = useState(0);
  const [roleEditorGroupId, setRoleEditorGroupId] = useState<string | null | 'new'>('');
  const [roleContextMenu, setRoleContextMenu] = useState<{ visible: boolean; x: number; y: number; item: any }>({ visible: false, x: 0, y: 0, item: null });
  const flexRef = useRef<any>(null);

  const loadData = useCallback(async () => {
    if (!companyId) return;
    dispatch(setIsBusy());
    try {
      const list = await adminSvc.retrieveAppSecurityGroupDtoByGroupUsage(String(GROUP_USAGE_CONTACT_GROUP), false, '');
      const arr = Array.isArray(list) ? list : [];
      const udRoleList = arr.filter((r: any) => !r?.IsBuiltIn);
      groupsCV.sourceCollection = udRoleList;
      groupsCV.refresh();
      setRefreshTrigger((t) => t + 1);
    } catch (e) {
      groupsCV.sourceCollection = [];
      groupsCV.refresh();
      setRefreshTrigger((t) => t + 1);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [companyId, dispatch, groupsCV]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const openCreate = useCallback(() => setRoleEditorGroupId('new'), []);
  const openEdit = useCallback((item: any) => {
    if (item?.Id) setRoleEditorGroupId(String(item.Id));
  }, []);
  const closeEditor = useCallback(() => setRoleEditorGroupId(''), []);
  const handleCloseEditor = useCallback(() => {
    closeEditor();
    loadData();
  }, [closeEditor, loadData]);

  const handleDeleteGroup = useCallback(async (item: any) => {
    if (!item?.Id) return;
    if (item?.IsBuiltIn) {
      alert('Cannot delete system built-in group.');
      return;
    }
    if (!window.confirm('Confirm To Delete')) return;
    setRoleContextMenu({ visible: false, x: 0, y: 0, item: null });
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.deleteAppSecurityGroup(String(item.Id));
      if (data?.IsSuccessful) await loadData();
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, loadData]);

  const handleEditFromMenu = useCallback((item: any) => {
    setRoleContextMenu({ visible: false, x: 0, y: 0, item: null });
    openEdit(item);
  }, [openEdit]);

  useEffect(() => {
    if (!roleContextMenu.visible) return;
    const onDocClick = () => setRoleContextMenu((prev) => (prev.visible ? { ...prev, visible: false } : prev));
    document.addEventListener('click', onDocClick);
    return () => document.removeEventListener('click', onDocClick);
  }, [roleContextMenu.visible]);

  const groupMenuCellTemplate = useCallback((cell: any) => {
    if (!cell.item) return null;
    return (
      <div className="flex items-center justify-center w-full">
        <button
          type="button"
          className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
          title="More Options"
          onClick={(e) => {
            e.stopPropagation();
            const rect = e.currentTarget.getBoundingClientRect();
            setRoleContextMenu({ visible: true, x: rect.right, y: rect.top, item: cell.item });
          }}
        >
          <i className="fa-solid fa-pencil text-xs" aria-hidden />
          <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
        </button>
      </div>
    );
  }, [theme.menu_default]);

  if (!companyId) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className="text-gray-600 dark:text-gray-400">Select a company.</p>
      </div>
    );
  }

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Contact Groups</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default}`}
            onClick={loadData}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default}`}
            onClick={openCreate}
            title="Create Group"
          >
            <i className="fa-solid fa-plus" aria-hidden /> Create Group
          </button>
        </div>
      </div>
      <div className={`w-full h-1 flex-auto min-h-0 overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          ref={flexRef}
          itemsSource={groupsCV}
          selectionMode="Row"
          headersVisibility="Column"
          isReadOnly={true}
          style={{ height: '100%' }}
        >
          <FlexGridColumn width={60} header="Actions" isReadOnly>
            <FlexGridCellTemplate cellType="Cell" template={groupMenuCellTemplate} />
          </FlexGridColumn>
          <FlexGridColumn binding="GroupName" header="Contact Group Name" width={200} />
          <FlexGridColumn binding="GroupUserString" header="Users" width="*" />
        </FlexGrid>
      </div>

      {roleContextMenu.visible && roleContextMenu.item && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{ left: roleContextMenu.x, top: roleContextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {roleContextMenu.item.Id && !roleContextMenu.item.IsBuiltIn && (
            <>
              <button
                type="button"
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                onClick={() => handleEditFromMenu(roleContextMenu.item)}
              >
                <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden /> Edit Group
              </button>
              <button
                type="button"
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                onClick={() => handleDeleteGroup(roleContextMenu.item)}
              >
                <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden /> Delete Group
              </button>
            </>
          )}
        </div>
      )}

      {roleEditorGroupId !== '' && (
        <RoleEditorModal
          groupUsage={GROUP_USAGE_CONTACT_GROUP}
          companyId={companyId}
          groupId={roleEditorGroupId === 'new' ? null : roleEditorGroupId}
          onClose={handleCloseEditor}
          onSaved={loadData}
        />
      )}
    </div>
  );
};

export default CompanyContactGroupSetup;
