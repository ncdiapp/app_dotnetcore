import React, { useCallback, useEffect, useRef, useState } from 'react';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../../hooks/useClampedContextMenuPosition';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { websiteMgtService } from '../../../webapi/websitemgtsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import UserLoginInfoEditor from './UserLoginInfoEditor';
import UserEditor from './UserEditor';

type Props = {
  partnerId: string | number | null;
  partnerType?: string | number;
  companyId?: string | number | null;
  isEmbedded?: boolean;
  onSynchronize?: (partner: any) => void;
};

type PartnerData = {
  Id?: number;
  Code: string;
  ShortName: string;
  FullName: string;
  AppCompanyId?: string | number;
  PartnerType?: string | number;
  IsModified?: boolean;
};

const CONTEXT_MENU_ESTIMATED_WIDTH = 170;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 160;

const PARTNER_TYPE_LABELS: Record<number, string> = {
  3: 'Client',
  4: 'Supplier',
  5: 'Client Agent',
  9: 'Supplier Agent',
};

const BusinessPartnerEditor: React.FC<Props> = ({
  partnerId,
  partnerType,
  companyId,
  isEmbedded: _isEmbedded,
  onSynchronize,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [partner, setPartner] = useState<PartnerData | null>(null);
  const [usersCV, setUsersCV] = useState<CollectionView | null>(null);
  const [websiteCV, setWebsiteCV] = useState<CollectionView | null>(null);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);

  // Modal states: login (create + Edit Login Info) vs user detail (Edit User Detail)
  const [showUserLoginEditor, setShowUserLoginEditor] = useState(false);
  const [showUserEditor, setShowUserEditor] = useState(false);
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [showInvitePopup, setShowInvitePopup] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [showActivationPopup, setShowActivationPopup] = useState(false);
  const [activationUser, setActivationUser] = useState<any>(null);
  const [selectedWebsiteId, setSelectedWebsiteId] = useState<number | null>(null);

  // Context menu for User list (standard pattern: Actions column + floating menu)
  const [userContextMenu, setUserContextMenu] = useState<{ visible: boolean; x: number; y: number; item: any }>({
    visible: false,
    x: 0,
    y: 0,
    item: null,
  });
  const userMenuRef = useRef<HTMLDivElement | null>(null);

  const loadPartner = useCallback(async () => {
    if (!partnerId) {
      setPartner(null);
      setUsersCV(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const [data, websites] = await Promise.all([
        adminSvc.retrieveOneAppBusinessPartnerExDto(String(partnerId)),
        websiteMgtService.retrieveApplicationWebsiteDtoList(),
      ]);

      if (data) {
        setPartner({
          Id: data.Id,
          Code: data.Code || '',
          ShortName: data.ShortName || '',
          FullName: data.FullName || '',
          AppCompanyId: data.AppCompanyId,
          PartnerType: data.PartnerType,
        });

        // Get users from partner (API returns BusinessPartnerUserList; fallback to AppUsers/Users)
        const users = data.BusinessPartnerUserList ?? data.AppUsers ?? data.Users ?? [];
        setUsersCV(new CollectionView(Array.isArray(users) ? users : []));
      }

      // Filter published websites
      const publishedSites = (websites || []).filter((s: any) => s.SitePublishedBaseUrl);
      setWebsiteCV(new CollectionView(publishedSites));
    } catch (e) {
      setErrors([e instanceof Error ? e.message : 'Failed to load partner']);
    } finally {
      setLoading(false);
    }
  }, [partnerId]);

  useEffect(() => {
    loadPartner();
  }, [loadPartner]);

  const handleFieldChange = useCallback((field: keyof PartnerData, value: any) => {
    setPartner((prev) => prev ? { ...prev, [field]: value, IsModified: true } : null);
  }, []);

  const handleSavePartner = useCallback(async () => {
    if (!partner) return;
    dispatch(setIsBusy());
    try {
      const response = await adminSvc.saveOneAppBusinessPartnerExDto(partner);
      const errs = response?.ValidationResult
        ? (Array.isArray(response.ValidationResult)
          ? response.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean)
          : [])
        : [];
      setErrors(errs);

      if (response?.IsSuccessful && response?.Object) {
        setPartner((prev) => prev ? { ...prev, IsModified: false } : null);
        onSynchronize?.(response.Object);
        loadPartner();
      }
    } catch (e) {
      setErrors([e instanceof Error ? e.message : String(e)]);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [partner, dispatch, loadPartner, onSynchronize]);

  const handleCreateUser = useCallback(() => {
    setEditingUserId(null);
    setShowUserLoginEditor(true);
  }, []);

  const handleEditUserLogin = useCallback((userId: string) => {
    setEditingUserId(userId);
    setShowUserLoginEditor(true);
  }, []);

  const handleEditUser = useCallback((userId: string) => {
    setEditingUserId(userId);
    setShowUserEditor(true);
  }, []);

  const _handleDeleteUser = useCallback(async (user: any) => {
    if (!user?.Id) return;
    if (user.IsSystemDefine) {
      alert('System defined user cannot be deleted.');
      return;
    }
    if (!window.confirm(`Confirm to delete ${user.UserName}?`)) return;

    dispatch(setIsBusy());
    try {
      const response = await adminSvc.deleteAppSecurityUser(String(user.Id));
      if (response?.IsSuccessful) {
        loadPartner();
      } else {
        const errs = response?.ValidationResult?.map((e: any) => e?.ErrorMessage || e?.Message).join(', ');
        if (errs) alert(errs);
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, loadPartner]);

  const handleInviteUser = useCallback(async () => {
    if (!inviteEmail?.trim() || !partnerId) return;
    dispatch(setIsBusy());
    try {
      const response = await adminSvc.inviteBusinessPaternerUser({
        Email: inviteEmail.trim(),
        PartnerId: partnerId,
        PartnerType: partnerType,
      });

      if (response?.IsSuccessful) {
        setShowInvitePopup(false);
        setInviteEmail('');
        loadPartner();
      } else {
        const errs = response?.ValidationResult?.map((e: any) => e?.ErrorMessage || e?.Message).join(', ');
        if (errs) alert(errs);
      }
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to invite user');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [inviteEmail, partnerId, partnerType, dispatch, loadPartner]);

  const handleSendActivation = useCallback(async () => {
    if (!activationUser?.Id) return;
    dispatch(setIsBusy());
    try {
      const response = await adminSvc.sendUserAccountUnlockEmail({
        UserId: activationUser.Id,
        RedirectWebsiteId: selectedWebsiteId,
      });

      if (response?.IsSuccessful) {
        setShowActivationPopup(false);
        setActivationUser(null);
        alert('Activation email sent successfully.');
      } else {
        const errs = response?.ValidationResult?.map((e: any) => e?.ErrorMessage || e?.Message).join(', ');
        if (errs) alert(errs);
      }
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to send activation email');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [activationUser, selectedWebsiteId, dispatch]);

  const openActivationPopup = useCallback((user: any) => {
    setActivationUser(user);
    setSelectedWebsiteId(null);
    setShowActivationPopup(true);
  }, []);

  // Close user context menu on outside click
  useEffect(() => {
    if (!userContextMenu.visible) return;
    const onDocClick = (e: MouseEvent) => {
      const el = userMenuRef.current;
      if (el && !el.contains(e.target as Node)) setUserContextMenu((prev) => ({ ...prev, visible: false }));
    };
    document.addEventListener('click', onDocClick, true);
    return () => document.removeEventListener('click', onDocClick, true);
  }, [userContextMenu.visible]);

  useRefineContextMenuField(userContextMenu.visible, userMenuRef, setUserContextMenu);

  if (!partnerId) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className={`text-xs ${theme.label}`}>Select a partner to view details.</p>
      </div>
    );
  }

  if (loading) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className={`text-xs ${theme.label}`}>Loading...</p>
      </div>
    );
  }

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      {/* Partner Info */}
      <div className={`px-3 py-2 border-b ${theme.mainContentSection}`}>
        <div className="flex items-center gap-4 mb-2">
          <div className="flex items-center">
            <label className={`w-20 text-xs ${theme.label} mr-2`}>Code</label>
            <input
              type="text"
              className={`w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
              value={partner?.Code || ''}
              onChange={(e) => handleFieldChange('Code', e.target.value)}
            />
          </div>
          <div className="flex items-center">
            <label className={`w-20 text-xs ${theme.label} mr-2`}>Short Name</label>
            <input
              type="text"
              className={`w-40 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
              value={partner?.ShortName || ''}
              onChange={(e) => handleFieldChange('ShortName', e.target.value)}
            />
          </div>
          <div className="flex-1 flex items-center">
            <label className={`w-20 text-xs ${theme.label} mr-2`}>Full Name</label>
            <input
              type="text"
              className={`flex-1 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
              value={partner?.FullName || ''}
              onChange={(e) => handleFieldChange('FullName', e.target.value)}
            />
          </div>
          {partner?.IsModified && (
            <button
              type="button"
              className="w-8 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500"
              onClick={handleSavePartner}
              title="Save Partner"
            >
              <i className="fa-solid fa-save" />
            </button>
          )}
        </div>
        {errors.length > 0 && (
          <div className="text-red-600 text-xs">
            {errors.map((msg, i) => <span key={i}>{msg} </span>)}
          </div>
        )}
      </div>

      {/* Users Header — buttons with icon + label text (Refresh, Create X User, Invite New X User) */}
      <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Users</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default}`}
            onClick={loadPartner}
            title="Refresh"
          >
            <i className="fa-solid fa-refresh" aria-hidden /> Refresh
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default}`}
            onClick={handleCreateUser}
            title="Create User"
          >
            <i className="fa-solid fa-plus" aria-hidden /> Create {PARTNER_TYPE_LABELS[Number(partnerType)] || 'Partner'} User
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default}`}
            onClick={() => setShowInvitePopup(true)}
            title="Invite by Email"
          >
            <i className="fa-solid fa-envelope" aria-hidden /> Invite New {PARTNER_TYPE_LABELS[Number(partnerType)] || 'Partner'} User
          </button>
        </div>
      </div>

      {/* Users Grid */}
      <div className={`flex-1 min-h-0 px-2 pb-2`}>
        <div className={`h-full border ${theme.inputBox}`}>
          {usersCV && (
            <FlexGrid
              itemsSource={usersCV}
              selectionMode="Row"
              headersVisibility="Column"
              isReadOnly={true}
              style={{ height: '100%' }}
            >
              <FlexGridColumn width={60} header="Actions" isReadOnly>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => (
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
                          setUserContextMenu({ visible: true, x, y, item: cell.item });
                        }}
                      >
                        <i className="fa-solid fa-pencil text-xs" aria-hidden />
                        <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                      </button>
                    </div>
                  )}
                />
              </FlexGridColumn>
              <FlexGridColumn binding="UserName" header="User Name" width={150} />
              <FlexGridColumn binding="LoginName" header="Login Name" width={120} />
              <FlexGridColumn binding="Email" header="Email" width={200} />
              <FlexGridColumn binding="IsActive" header="Active" width={60} />
            </FlexGrid>
          )}
        </div>
      </div>

      {/* User list context menu — align with Angular: Edit Login Info, Edit User Detail, Send Activation Request */}
      {userContextMenu.visible && userContextMenu.item && (
        <div
          ref={userMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{ left: userContextMenu.x, top: userContextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={() => {
              handleEditUserLogin(String(userContextMenu.item.Id));
              setUserContextMenu((prev) => ({ ...prev, visible: false, item: null }));
            }}
          >
            <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden /> Edit Login Info
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={() => {
              handleEditUser(String(userContextMenu.item.Id));
              setUserContextMenu((prev) => ({ ...prev, visible: false, item: null }));
            }}
          >
            <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden /> Edit User Detail
          </button>
          {/* Show only when user has Email and is not active (align with Angular isShowSendUserActivationRequestButton) */}
          {userContextMenu.item?.Email && !userContextMenu.item?.IsActive && (
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={() => {
                openActivationPopup(userContextMenu.item);
                setUserContextMenu((prev) => ({ ...prev, visible: false, item: null }));
              }}
            >
              <i className="fa-solid fa-envelope mr-2 flex-shrink-0" aria-hidden /> Send Activation Request
            </button>
          )}
        </div>
      )}

      {/* Login editor (Create User + Edit Login Info) */}
      {showUserLoginEditor && (
        <UserLoginInfoEditor
          userId={editingUserId}
          domainId={Number(partnerType) || 3}
          companyId={companyId}
          partnerId={partnerId}
          onClose={() => setShowUserLoginEditor(false)}
          onSaved={loadPartner}
        />
      )}

      {/* User detail editor (Edit User Detail) */}
      {showUserEditor && (
        <UserEditor
          userId={editingUserId}
          domainId={Number(partnerType) || undefined}
          companyId={companyId ?? undefined}
          onClose={() => setShowUserEditor(false)}
          onSaved={loadPartner}
        />
      )}

      {/* Invite Popup */}
      {showInvitePopup && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
          <div
            className={`${theme.mainContentSection} rounded shadow-xl w-[400px] flex flex-col`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <span className={`text-md font-semibold ${theme.title}`}>Invite User by Email</span>
              <button type="button" className="text-lg leading-none w-6 h-6" onClick={() => setShowInvitePopup(false)}>&times;</button>
            </div>
            <div className="px-5 py-4">
              <div className="flex items-center">
                <label className={`w-20 text-xs ${theme.label} mr-2`}>Email</label>
                <input
                  type="email"
                  className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  placeholder="user@example.com"
                />
              </div>
            </div>
            <div className="px-5 pb-4 flex items-center space-x-2">
              <button
                type="button"
                className="px-3 py-1.5 text-sm rounded-[4px] bg-orange-400 text-white hover:bg-orange-500"
                onClick={handleInviteUser}
              >
                Send Invitation
              </button>
              <button
                type="button"
                className="px-3 py-1.5 text-sm rounded-[4px] bg-gray-400 text-white hover:bg-gray-500"
                onClick={() => setShowInvitePopup(false)}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Activation Popup */}
      {showActivationPopup && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
          <div
            className={`${theme.mainContentSection} rounded shadow-xl w-[400px] flex flex-col`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <span className={`text-md font-semibold ${theme.title}`}>Send Activation Email</span>
              <button type="button" className="text-lg leading-none w-6 h-6" onClick={() => setShowActivationPopup(false)}>&times;</button>
            </div>
            <div className="px-5 py-4 space-y-3">
              <div className={`text-xs ${theme.label}`}>
                Send activation email to: <strong>{activationUser?.Email || activationUser?.UserName}</strong>
              </div>
              <div className="flex items-center">
                <label className={`w-28 text-xs ${theme.label} mr-2`}>Redirect Site</label>
                {websiteCV && (
                  <ComboBox
                    itemsSource={websiteCV}
                    displayMemberPath="Name"
                    selectedValuePath="Id"
                    selectedValue={selectedWebsiteId}
                    selectedIndexChanged={(s) => setSelectedWebsiteId(s.selectedValue)}
                    isRequired={false}
                    placeholder="Select site..."
                    style={{ flex: 1, height: 28 }}
                  />
                )}
              </div>
            </div>
            <div className="px-5 pb-4 flex items-center space-x-2">
              <button
                type="button"
                className="px-3 py-1.5 text-sm rounded-[4px] bg-orange-400 text-white hover:bg-orange-500"
                onClick={handleSendActivation}
              >
                Send Email
              </button>
              <button
                type="button"
                className="px-3 py-1.5 text-sm rounded-[4px] bg-gray-400 text-white hover:bg-gray-500"
                onClick={() => setShowActivationPopup(false)}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default BusinessPartnerEditor;
