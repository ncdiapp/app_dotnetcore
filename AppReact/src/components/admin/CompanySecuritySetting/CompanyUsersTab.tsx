import React, { useCallback, useEffect, useRef, useState, type Dispatch, type SetStateAction } from 'react';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../../hooks/useClampedContextMenuPosition';
import { CollectionView, PropertyGroupDescription, SortDescription } from '@mescius/wijmo';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../../webapi/adminsvc';
import CompanyOrgnizationSetup from './CompanyOrgnizationSetup';
import BusinessPartnerManagement from './BusinessPartnerManagement';
import UserLoginInfoEditor from './UserLoginInfoEditor';
import UserEditor from './UserEditor';

const USER_SUB_TABS = ['Employee', 'Customer', 'Supplier', 'Client Agent', 'Supplier Agent'] as const;

const CONTEXT_MENU_ESTIMATED_WIDTH = 170;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 120;

// Backend expects int PartnerType (EmAppUserType: Customer=3, Supplier=4, ClientAgent=5, SupplierAgent=9)
const PARTNER_TYPE_MAP: Record<number, number> = {
  1: 3,  // Customer
  2: 4,  // Supplier
  3: 5,  // ClientAgent
  4: 9,  // SupplierAgent
};

type Props = {
  companyId: string | number | null;
  isModified: boolean;
  currentUserSubTab: number;
  onUserSubTabChange: (index: number) => void;
};

const CompanyUsersTab: React.FC<Props> = ({
  companyId,
  isModified,
  currentUserSubTab,
  onUserSubTabChange,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const [usersCV, setUsersCV] = useState<CollectionView | null>(null);
  const [contextMenu, setContextMenu] = useState<{ x: number; y: number; user: any } | null>(null);
  const [showUserLoginEditor, setShowUserLoginEditor] = useState(false);
  const [showUserEditor, setShowUserEditor] = useState(false);
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const contextMenuRef = useRef<HTMLDivElement>(null);

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
      setUsersCV(cv);
    } catch (e) {
      setUsersCV(new CollectionView<any>([]));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [companyId, dispatch]);

  useEffect(() => {
    if (companyId && currentUserSubTab === 0) loadUsers();
  }, [companyId, currentUserSubTab, loadUsers]);

  useEffect(() => {
    if (!contextMenu) return;
    const handleClickOutside = (e: MouseEvent) => {
      if (contextMenuRef.current && !contextMenuRef.current.contains(e.target as Node)) setContextMenu(null);
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [contextMenu]);

  const handleOpenContextMenu = useCallback((user: any, position: { x: number; y: number }) => {
    const { x, y } = clampContextMenuPosition(
      position.x,
      position.y,
      CONTEXT_MENU_ESTIMATED_WIDTH,
      CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setContextMenu({ x, y, user });
  }, []);

  const refineContextMenu = useCallback<Dispatch<SetStateAction<{ x: number; y: number; user: any }>>>(
    (action) => {
      setContextMenu((prev) => {
        if (!prev) return prev;
        return typeof action === 'function' ? action(prev) : action;
      });
    },
    []
  );

  useRefineContextMenuField(!!contextMenu, contextMenuRef, refineContextMenu);

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
      if (response?.IsSuccessful) loadUsers();
      else {
        const errs = response?.ValidationResult?.map((e: any) => e?.ErrorMessage || e?.Message).join(', ');
        if (errs) alert(errs);
      }
    } finally {
      dispatch(setIsNotBusy());
    }
    setContextMenu(null);
  }, [dispatch, loadUsers]);

  const handleCreateUser = useCallback(() => {
    setEditingUserId(null);
    setShowUserLoginEditor(true);
  }, []);

  if (!companyId) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className={`text-xs ${theme.label}`}>Select a company.</p>
      </div>
    );
  }

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      {/* Sub-tabs */}
      <div className={`flex flex-wrap gap-0 border-b ${theme.mainContentSection}`}>
        {USER_SUB_TABS.map((label, idx) => (
          <button
            key={idx}
            type="button"
            className={`px-3 py-2 text-xs border-b-2 whitespace-nowrap ${currentUserSubTab === idx ? theme.tab_active : theme.tab}`}
            onClick={() => onUserSubTabChange(idx)}
            disabled={isModified}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {currentUserSubTab === 0 ? (
          <>
            <CompanyOrgnizationSetup
              companyId={companyId}
              isEmbedded
              usersCV={usersCV}
              onRefresh={loadUsers}
              onOpenContextMenu={handleOpenContextMenu}
              onCreateUser={handleCreateUser}
            />
            {/* Context menu - same as UserManagement */}
            {contextMenu?.user && (
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
            {showUserLoginEditor && (
              <UserLoginInfoEditor
                userId={editingUserId}
                domainId={2}
                companyId={companyId}
                onClose={() => setShowUserLoginEditor(false)}
                onSaved={loadUsers}
              />
            )}
            {showUserEditor && (
              <UserEditor
                userId={editingUserId}
                domainId={2}
                companyId={companyId}
                onClose={() => setShowUserEditor(false)}
                onSaved={loadUsers}
              />
            )}
          </>
        ) : (
          <BusinessPartnerManagement
            companyId={companyId}
            partnerType={PARTNER_TYPE_MAP[currentUserSubTab] ?? 3}
            isEmbedded
          />
        )}
      </div>
    </div>
  );
};

export default CompanyUsersTab;
