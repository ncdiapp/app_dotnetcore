import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import UserLoginInfoEditor from './UserLoginInfoEditor';

interface Props {
  companyId: number;
}

const TenantAdminUsersTab: React.FC<Props> = ({ companyId }) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const [cv, setCv] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [error, setError] = useState('');
  const [contextMenu, setContextMenu] = useState<{ x: number; y: number; user: any } | null>(null);
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [showLoginEditor, setShowLoginEditor] = useState(false);
  const contextMenuRef = useRef<HTMLDivElement>(null);

  const load = useCallback(async () => {
    if (!companyId) return;
    dispatch(setIsBusy());
    setError('');
    try {
      const data = await adminSvc.getSimpleUserListByCompanyId(companyId);
      setCv(new CollectionView<any>(Array.isArray(data) ? data : []));
    } catch (e: any) {
      setError(e.message ?? 'Failed to load users');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [companyId, dispatch]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (!contextMenu) return;
    const handleClick = (e: MouseEvent) => {
      if (contextMenuRef.current && !contextMenuRef.current.contains(e.target as Node))
        setContextMenu(null);
    };
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [contextMenu]);

  const openContextMenu = useCallback((user: any, x: number, y: number) => {
    setContextMenu({ x, y, user });
  }, []);

  const handleEditLogin = useCallback((userId: string) => {
    setEditingUserId(userId);
    setShowLoginEditor(true);
    setContextMenu(null);
  }, []);

  const handleAdd = useCallback(() => {
    setEditingUserId(null);
    setShowLoginEditor(true);
  }, []);

  const handleDelete = useCallback(async (user: any) => {
    if (!user?.Id) return;
    if (!window.confirm(`Delete user "${user.UserName}"?`)) return;
    dispatch(setIsBusy());
    try {
      const res = await adminSvc.deleteAppSecurityUser(String(user.Id));
      if (res?.IsSuccessful) {
        load();
      } else {
        const msg = res?.ValidationResult?.map((e: any) => e?.ErrorMessage || e?.Message).join(', ');
        if (msg) alert(msg);
      }
    } finally {
      dispatch(setIsNotBusy());
    }
    setContextMenu(null);
  }, [dispatch, load]);

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      {/* Toolbar */}
      <div className={`flex items-center gap-2 px-3 py-2 border-b shrink-0 ${theme.mainContentSection}`}>
        <span className={`text-sm font-semibold ${theme.title}`}>Users</span>
        <div className="ml-auto flex gap-1">
          <button
            type="button"
            onClick={handleAdd}
            className="px-3 py-1.5 text-sm rounded-[4px] bg-green-500 text-white hover:bg-green-600"
            title="Add User"
          >
            <i className="fa-solid fa-plus mr-1" />Add
          </button>
          <button
            type="button"
            onClick={load}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" />
          </button>
        </div>
      </div>

      {error && <div className="px-3 py-1 text-xs text-red-500 shrink-0">{error}</div>}

      {/* Grid */}
      <div className="w-full h-1 flex-auto overflow-hidden">
        <FlexGrid
          itemsSource={cv}
          className="h-full w-full"
          isReadOnly
          selectionMode="Row"
        >
          <FlexGridColumn header="Login Name" binding="LoginName" width={160} />
          <FlexGridColumn header="User Name"  binding="UserName"  width={160} />
          <FlexGridColumn header="Email"      binding="Email"     width="*"   />
          <FlexGridColumn header="Active"     binding="IsActive"  width={80}  />
          <FlexGridColumn header="Actions" binding="" width={80} isReadOnly allowSorting={false}>
            <FlexGridCellTemplate cellType="Cell" template={(cell) => (
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  title="Edit"
                  className={`w-6 h-6 text-xs rounded-[4px] ${theme.button_default}`}
                  onClick={(e) => { e.stopPropagation(); openContextMenu(cell.item, e.clientX, e.clientY); }}
                >
                  <i className="fa-solid fa-ellipsis-vertical" />
                </button>
              </div>
            )} />
          </FlexGridColumn>
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>

      {/* Context menu */}
      {contextMenu?.user && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[160px]`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => handleEditLogin(String(contextMenu.user.Id))}
          >
            <i className="fa-solid fa-pen-to-square mr-2" />Edit Login Info
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs text-red-500 hover:bg-red-50 flex items-center`}
            onClick={() => handleDelete(contextMenu.user)}
          >
            <i className="fa-solid fa-trash mr-2" />Delete
          </button>
        </div>
      )}

      {showLoginEditor && (
        <UserLoginInfoEditor
          userId={editingUserId}
          companyId={companyId}
          onClose={() => setShowLoginEditor(false)}
          onSaved={load}
        />
      )}

    </div>
  );
};

export default TenantAdminUsersTab;
