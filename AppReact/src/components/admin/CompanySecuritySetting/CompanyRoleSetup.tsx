import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../../hooks/useClampedContextMenuPosition';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, PropertyGroupDescription, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import RoleEditorModal from './RoleEditorModal';

type Props = { companyId: string | number | null; isEmbedded?: boolean };

const GROUP_USAGE_SECURITY_ROLE = 1;

const CONTEXT_MENU_ESTIMATED_WIDTH = 170;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 120;

/** Role User Type options for grid and editor — align with Angular domainTypeList + All Types */
const ROLE_USER_TYPE_LIST = [
  { Id: null as number | null, Display: 'All Types' },
  { Id: 2, Display: 'Employee' },
  { Id: 3, Display: 'Customer' },
  { Id: 4, Display: 'Supplier' },
  { Id: 5, Display: 'Client Agent' },
  { Id: 9, Display: 'Supplier Agent' },
];

const CompanyRoleSetup: React.FC<Props> = ({ companyId, isEmbedded: _isEmbedded }) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [rolesCV] = useState(() => {
    const cv = new CollectionView<any>([]);
    cv.sortDescriptions.push(new SortDescription('RoleUserTypeId', true));
    cv.groupDescriptions.push(new PropertyGroupDescription('RoleUserTypeId'));
    return cv;
  });
  const [, setRefreshTrigger] = useState(0);
  const [roleEditorGroupId, setRoleEditorGroupId] = useState<string | null | 'new'>(''); // 'new' = create, null = closed, string = edit
  const [roleContextMenu, setRoleContextMenu] = useState<{ visible: boolean; x: number; y: number; item: any }>({ visible: false, x: 0, y: 0, item: null });
  const flexRef = useRef<any>(null);
  const roleContextMenuRef = useRef<HTMLDivElement>(null);

  const domainTypeDataMap = useMemo(() => new DataMap(ROLE_USER_TYPE_LIST, 'Id', 'Display'), []);

  const loadData = useCallback(async () => {
    if (!companyId) return;
    dispatch(setIsBusy());
    try {
      const list = await adminSvc.retrieveAppSecurityGroupDtoByGroupUsage('1', false, '');
      const arr = Array.isArray(list) ? list : [];
      const filtered = arr.filter((r: any) => !r?.IsBuiltIn || r?.IsAllowedToAddAvailableUser);
      rolesCV.sourceCollection = filtered;
      rolesCV.refresh();
      setRefreshTrigger((t) => t + 1);
    } catch (e) {
      rolesCV.sourceCollection = [];
      rolesCV.refresh();
      setRefreshTrigger((t) => t + 1);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [companyId, dispatch, rolesCV]);

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

  const handleDeleteRole = useCallback(async (item: any) => {
    if (!item?.Id) return;
    if (item?.IsBuiltIn) {
      alert('Cannot delete system built-in role.');
      return;
    }
    if (!window.confirm('Confirm to delete this role?')) return;
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

  useRefineContextMenuField(roleContextMenu.visible, roleContextMenuRef, setRoleContextMenu);

  const roleMenuCellTemplate = useCallback((cell: any) => {
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
            const { x, y } = clampContextMenuPosition(
              rect.right,
              rect.top,
              CONTEXT_MENU_ESTIMATED_WIDTH,
              CONTEXT_MENU_ESTIMATED_HEIGHT
            );
            setRoleContextMenu({ visible: true, x, y, item: cell.item });
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
        <div className={`text-md font-semibold ${theme.title}`}>Roles</div>
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
            title="Create Role"
          >
            <i className="fa-solid fa-plus" aria-hidden /> Create Role
          </button>
        </div>
      </div>
      <div className={`w-full h-1 flex-auto min-h-0 overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          ref={flexRef}
          itemsSource={rolesCV}
          selectionMode="Row"
          headersVisibility="Column"
          isReadOnly={true}
          showGroups={true}
          style={{ height: '100%' }}
        >
          <FlexGridColumn width={60} header="Actions" isReadOnly>
            <FlexGridCellTemplate cellType="Cell" template={roleMenuCellTemplate} />
          </FlexGridColumn>
          <FlexGridColumn binding="GroupName" header="Role Name" width={200} />
          <FlexGridColumn binding="OrganizationPath" header="Path" width={200} />
          <FlexGridColumn binding="RoleUserTypeId" header="Role User Type" width={150} dataMap={domainTypeDataMap} />
          <FlexGridColumn binding="GroupUserString" header="Users" width="*" />
        </FlexGrid>
      </div>

      {roleContextMenu.visible && roleContextMenu.item && (
        <div
          ref={roleContextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{ left: roleContextMenu.x, top: roleContextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {roleContextMenu.item.Id && (
            <>
              <button
                type="button"
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                onClick={() => handleEditFromMenu(roleContextMenu.item)}
              >
                <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden /> Edit Role
              </button>
              {!roleContextMenu.item.IsBuiltIn && (
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                  onClick={() => handleDeleteRole(roleContextMenu.item)}
                >
                  <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden /> Delete Role
                </button>
              )}
            </>
          )}
        </div>
      )}

      {roleEditorGroupId !== '' && (
        <RoleEditorModal
          groupUsage={GROUP_USAGE_SECURITY_ROLE}
          companyId={companyId}
          groupId={roleEditorGroupId === 'new' ? null : roleEditorGroupId}
          onClose={handleCloseEditor}
          onSaved={loadData}
        />
      )}
    </div>
  );
};

export default CompanyRoleSetup;
