import React, { useCallback, useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import SecurityGroupEditor from './SecurityGroupEditor';

type Props = {
  groupUsage?: number; // 1 = SecurityGroup, 2 = ProjectTeam
  companyId?: string | number | null;
  isEmbedded?: boolean;
};

const USAGE_TITLES: Record<number, string> = {
  1: 'Security Group Management',
  2: 'Project Team Management',
};

const SecurityGroupManagement: React.FC<Props> = ({
  groupUsage = 1,
  companyId,
  isEmbedded,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [groupsCV, setGroupsCV] = useState<CollectionView | null>(null);
  const [showEditor, setShowEditor] = useState(false);
  const [editingGroupId, setEditingGroupId] = useState<string | null>(null);

  const loadGroups = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const list = await adminSvc.retrieveAppSecurityGroupDtoByGroupUsage(String(groupUsage), false, '');
      setGroupsCV(new CollectionView(Array.isArray(list) ? list : []));
    } catch (e) {
      setGroupsCV(new CollectionView<any>([]));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [groupUsage, dispatch]);

  useEffect(() => {
    loadGroups();
  }, [loadGroups]);

  const handleCreate = useCallback(() => {
    setEditingGroupId(null);
    setShowEditor(true);
  }, []);

  const handleEdit = useCallback((groupId: string) => {
    setEditingGroupId(groupId);
    setShowEditor(true);
  }, []);

  const handleDelete = useCallback(async (group: any) => {
    if (!group?.Id) return;
    if (group.IsBuiltIn) {
      alert('Built-in group cannot be deleted.');
      return;
    }
    if (!window.confirm(`Confirm to delete ${group.GroupName}?`)) return;

    dispatch(setIsBusy());
    try {
      const response = await adminSvc.deleteAppSecurityGroup(String(group.Id));
      if (response?.IsSuccessful) {
        loadGroups();
      } else {
        const errs = response?.ValidationResult?.map((e: any) => e?.ErrorMessage || e?.Message).join(', ');
        if (errs) alert(errs);
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, loadGroups]);

  const title = USAGE_TITLES[groupUsage] || 'Group Management';

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      {/* Header */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>{title}</div>
        <div className="flex items-center space-x-1">
          <button
            type="button"
            className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600"
            onClick={handleCreate}
            title="Create Group"
          >
            <i className="fa-solid fa-plus" aria-hidden="true" />
          </button>
          <button
            type="button"
            className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
            onClick={loadGroups}
            title="Refresh"
          >
            <i className="fa-solid fa-refresh" aria-hidden="true" />
          </button>
        </div>
      </div>

      {/* Grid */}
      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {groupsCV && (
          <FlexGrid
            itemsSource={groupsCV}
            selectionMode="Row"
            headersVisibility="Column"
            isReadOnly={true}
            style={{ height: '100%' }}
          >
            <FlexGridColumn header="" width={70}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => {
                  const item = cell.item;
                  if (!item) return null;
                  const canEdit = !item.IsBuiltIn;
                  const canDelete = !item.IsBuiltIn;
                  return (
                    <div className="flex items-center gap-1">
                      {canEdit && (
                        <button
                          type="button"
                          className={`${theme.menu_default}`}
                          title="Edit Group"
                          onClick={() => handleEdit(String(item.Id))}
                        >
                          <i className="fa-solid fa-pencil" />
                        </button>
                      )}
                      {canDelete && (
                        <button
                          type="button"
                          className="text-red-500 hover:text-red-700"
                          title="Delete"
                          onClick={() => handleDelete(item)}
                        >
                          <i className="fa-solid fa-trash" />
                        </button>
                      )}
                    </div>
                  );
                }}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="GroupName" header="Group Name" width={200} />
            <FlexGridColumn binding="Description" header="Description" width="*" />
            <FlexGridColumn binding="OrganizationPath" header="Organization" width={200} />
          </FlexGrid>
        )}
      </div>

      {/* Editor Modal */}
      {showEditor && (
        <SecurityGroupEditor
          groupId={editingGroupId}
          groupUsage={groupUsage}
          companyId={companyId}
          onClose={() => setShowEditor(false)}
          onSaved={loadGroups}
        />
      )}
    </div>
  );
};

export default SecurityGroupManagement;
