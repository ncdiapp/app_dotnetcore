import React, { useCallback, useEffect, useState, useRef, type Dispatch, type SetStateAction } from 'react';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../../hooks/useClampedContextMenuPosition';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, PropertyGroupDescription, SortDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import UserLoginInfoEditor from './UserLoginInfoEditor';
import UserEditor from './UserEditor';

const CONTEXT_MENU_ESTIMATED_WIDTH = 170;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 120;

type Props = {
  companyId: string | number | null;
  isEmbedded?: boolean;
  /** When provided, parent (e.g. CompanyUsersTab) owns context menu and user modals */
  usersCV?: CollectionView | null;
  onRefresh?: () => void;
  onOpenContextMenu?: (user: any, position: { x: number; y: number }) => void;
  onCreateUser?: () => void;
};

const CompanyOrgnizationSetup: React.FC<Props> = ({
  companyId,
  isEmbedded: _isEmbedded,
  usersCV: usersCVProp,
  onRefresh,
  onOpenContextMenu,
  onCreateUser,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [usersCVInternal, setUsersCVInternal] = useState<CollectionView | null>(null);
  const [_selectedUser, setSelectedUser] = useState<any>(null);

  const isControlled = usersCVProp !== undefined && onOpenContextMenu !== undefined;
  const usersCV = isControlled ? usersCVProp ?? null : usersCVInternal;

  const [contextMenu, setContextMenu] = useState<{ x: number; y: number; user: any } | null>(null);
  const [showUserLoginEditor, setShowUserLoginEditor] = useState(false);
  const [showUserEditor, setShowUserEditor] = useState(false);
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const contextMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!contextMenu) return;
    const handleClickOutside = (e: MouseEvent) => {
      if (contextMenuRef.current && !contextMenuRef.current.contains(e.target as Node)) setContextMenu(null);
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [contextMenu]);

  const handleEditUserLogin = useCallback((userId: string) => {
    setEditingUserId(userId);
    setShowUserLoginEditor(true);
    setContextMenu(null);
  }, []);

  const handleEditUser = useCallback((userId: string) => {
    setEditingUserId(userId);
    setShowUserEditor(true);
    setContextMenu(null);
  }, []);

  const loadUsers = useCallback(async () => {
    if (!companyId) return;
    dispatch(setIsBusy());
    try {
      const raw = await adminSvc.getCurrentTenantUserDtoList(false);
      const list = Array.isArray(raw)
        ? raw
        : raw == null
          ? []
          : Array.isArray((raw as any)?.Object)
            ? (raw as any).Object
            : Array.isArray((raw as any)?.Data)
              ? (raw as any).Data
              : Array.isArray((raw as any)?.Items)
                ? (raw as any).Items
                : [];
      const arr = Array.isArray(list) ? list : [];
      arr.forEach((u: any) => { u.isSelected = false; });
      const cv = new CollectionView(arr);
      cv.sortDescriptions.push(new SortDescription('DomainId', false));
      cv.sortDescriptions.push(new SortDescription('LoginName', true));
      cv.groupDescriptions.push(new PropertyGroupDescription('DomainDispaly'));
      setUsersCVInternal(cv);
    } catch (e) {
      setUsersCVInternal(new CollectionView<any>([]));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [companyId, dispatch]);

  const _handleDeleteUser = useCallback(async (user: any) => {
    if (!user?.Id) return;
    if (user.IsSystemDefine) {
      alert('System defined user does not allow delete.');
      return;
    }
    if (!window.confirm(`Confirm to delete ${user.UserName}?`)) return;
    dispatch(setIsBusy());
    try {
      const response = await adminSvc.deleteAppSecurityUser(String(user.Id));
      if (response?.IsSuccessful) {
        if (isControlled && onRefresh) onRefresh();
        else loadUsers();
      } else {
        const errs = response?.ValidationResult?.map((e: any) => e?.ErrorMessage || e?.Message).join(', ');
        if (errs) alert(errs);
      }
    } finally {
      dispatch(setIsNotBusy());
    }
    setContextMenu(null);
  }, [dispatch, isControlled, onRefresh, loadUsers]);

  useEffect(() => {
    if (!isControlled) loadUsers();
  }, [isControlled, loadUsers]);

  const handleCreateUser = useCallback(() => {
    if (onCreateUser) {
      onCreateUser();
    } else {
      setEditingUserId(null);
      setShowUserLoginEditor(true);
    }
  }, [onCreateUser]);

  const refineContextMenu = useCallback<Dispatch<SetStateAction<{ x: number; y: number; user: any }>>>(
    (action) => {
      setContextMenu((prev) => {
        if (!prev) return prev;
        return typeof action === 'function' ? action(prev) : action;
      });
    },
    []
  );

  useRefineContextMenuField(!isControlled && !!contextMenu, contextMenuRef, refineContextMenu);

  if (!companyId) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className={`text-xs ${theme.label}`}>Select a company to manage organization.</p>
      </div>
    );
  }

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      <div className="flex h-1 flex-auto gap-2 p-2">
        {/* Users Panel */}
        <div className="flex-1 flex flex-col min-w-0">
          <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
            <div className={`text-md font-semibold ${theme.title}`}>Users</div>
            <div className="flex items-center space-x-1">
              <button
                type="button"
                className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600"
                onClick={handleCreateUser}
                title="Invite New User"
              >
                <i className="fa-solid fa-plus" aria-hidden="true" />
              </button>
              <button
                type="button"
                className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                onClick={() => (isControlled ? onRefresh?.() : loadUsers())}
                title="Refresh"
              >
                <i className="fa-solid fa-refresh" aria-hidden="true" />
              </button>
            </div>
          </div>
          <div className={`flex-1 min-h-0 border ${theme.inputBox}`}>
            {usersCV && (
              <FlexGrid
                itemsSource={usersCV}
                selectionMode="Row"
                headersVisibility="Column"
                isReadOnly={true}
                showGroups={true}
                selectionChanged={(s) => setSelectedUser(s.collectionView?.currentItem)}
                style={{ height: '100%' }}
              >
                <FlexGridColumn header="" width={100} allowSorting={false}>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const item = cell.item;
                      if (!item) return null;
                      return (
                        <button
                          type="button"
                          className={`${theme.menu_default}`}
                          title="More Options"
                          style={{ width: '30px' }}
                          onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            const { x, y } = clampContextMenuPosition(
                              e.clientX,
                              e.clientY,
                              CONTEXT_MENU_ESTIMATED_WIDTH,
                              CONTEXT_MENU_ESTIMATED_HEIGHT
                            );
                            if (onOpenContextMenu) {
                              onOpenContextMenu(item, { x, y });
                            } else {
                              setContextMenu({ x, y, user: item });
                            }
                          }}
                        >
                          <i className="fa fa-pencil" aria-hidden style={{ fontSize: '12px' }} />
                          <i className="fa fa-navicon" aria-hidden style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }} />
                        </button>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="LoginName" header="Login Name" width={150} />
                <FlexGridColumn binding="UserName" header="User Name" width={150} />
                <FlexGridColumn binding="LanguageId" header="Language" width={120} />
                <FlexGridColumn binding="CultureInfoCode" header="Culture Info" width={120} />
                <FlexGridColumn binding="UserGroupString" header="Roles" width="*" />
                <FlexGridColumn binding="IsActive" header="Is Active" width={80} />
              </FlexGrid>
            )}
          </div>
        </div>
      </div>

      {/* Context menu - same as UserManagement (only when not controlled) */}
      {!isControlled && contextMenu && contextMenu.user && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => handleEditUserLogin(String(contextMenu.user.Id))}
          >
            <i className="fa-solid fa-pen-to-square mr-2" aria-hidden /> Edit Login Info
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => handleEditUser(String(contextMenu.user.Id))}
          >
            <i className="fa-solid fa-pen-to-square mr-2" aria-hidden /> Edit User Detail
          </button>
          {/* Delete hidden per product requirement */}
        </div>
      )}

      {/* Modals - only when not controlled by parent */}
      {!isControlled && showUserLoginEditor && (
        <UserLoginInfoEditor
          userId={editingUserId}
          domainId={2} // Employee
          companyId={companyId}
          onClose={() => setShowUserLoginEditor(false)}
          onSaved={loadUsers}
        />
      )}

      {!isControlled && showUserEditor && (
        <UserEditor
          userId={editingUserId}
          domainId={2} // Employee
          companyId={companyId}
          onClose={() => setShowUserEditor(false)}
          onSaved={loadUsers}
        />
      )}
    </div>
  );
};

export default CompanyOrgnizationSetup;
