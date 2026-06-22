import React, { useEffect, useState, useRef } from 'react';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { dashboardService } from '../../webapi/dashboardsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { BusyLoader } from '../common/BusyLoader';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useTheme } from '../../redux/hooks/useTheme';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';

const CONTEXT_MENU_ESTIMATED_WIDTH = 170;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 140;

interface DashboardDto {
  Id: string;
  DesktopName: string;
  StateCode: string;
  IsGlobalDefault?: boolean;
  CreatedBy?: string;
  CreatedDate?: string;
  ModifiedBy?: string;
  ModifiedDate?: string;
}

const DashboardManagement: React.FC = () => {
  const { addTabAndNavigate } = useTabNavigation();
  const { theme } = useTheme();
  const flexGridRef = useRef<any>(null);
  
  const [dashboardList, setDashboardList] = useState<DashboardDto[]>([]);
  const [usersDataMap, setUsersDataMap] = useState<Map<string, string>>(new Map());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedDashboard, setSelectedDashboard] = useState<DashboardDto | null>(null);
  const [rowContextMenu, setRowContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    dashboard: DashboardDto | null;
    canEdit: boolean;
    canDelete: boolean;
  }>({
    visible: false,
    x: 0,
    y: 0,
    dashboard: null,
    canEdit: false,
    canDelete: false
  });
  const rowContextMenuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  // Close row context menu on outside click
  useEffect(() => {
    const handleClick = () => {
      if (rowContextMenu.visible) {
        closeRowContextMenu();
      }
    };
    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [rowContextMenu.visible]);

  useRefineContextMenuField(rowContextMenu.visible, rowContextMenuRef, setRowContextMenu);

  const loadData = async () => {
    setLoading(true);
    setError(null);
    
    try {
      // Load dashboard list
      const dashboardData = await dashboardService.retrieveAllAppDesktopDto(true);
      setDashboardList(dashboardData || []);

      // Load users lookup
      const usersData = await adminSvc.getMassEntitiesLookupItem('AppSecurityUser');
      if (usersData && usersData['AppSecurityUser']) {
        const userMap = new Map<string, string>();
        usersData['AppSecurityUser'].forEach((user: any) => {
          userMap.set(user.Id, user.Display);
        });
        setUsersDataMap(userMap);
      }

      setLoading(false);
    } catch (err: any) {
      console.error('Failed to load dashboard data:', err);
      setError(err.message || 'Failed to load dashboard data');
      setLoading(false);
    }
  };

  const handleEditDashboard = (dashboard?: DashboardDto | null) => {
    const target = dashboard ?? rowContextMenu.dashboard ?? selectedDashboard;
    if (!target || !target.Id) {
      alert('Please select a dashboard to edit');
      return;
    }

    const paramObj = { id: target.Id };
    addTabAndNavigate(
      '/dashboard-edit',
      target.DesktopName || 'Dashboard Editor',
      paramObj,
      true
    );
  };

  const handleCreateDashboard = () => {
    // Always create Flex/Responsive dashboards (LayoutType = 2)
    const paramObj = { 
      layoutType: '2'
    };
    addTabAndNavigate(
      '/dashboard-edit',
      'New Dashboard',
      paramObj,
      true
    );
  };

  const handleDeleteDashboard = async (dashboard?: DashboardDto | null) => {
    const target = dashboard ?? rowContextMenu.dashboard ?? selectedDashboard;
    if (!target || !target.Id) {
      alert('Please select a dashboard to delete');
      return;
    }

    if (!window.confirm('Confirm To Delete')) {
      return;
    }

    try {
      const result = await dashboardService.deleteOneAppDesktop(target.Id);
      
      if (result && result.ValidationResult) {
        if (result.ValidationResult.IsValid) {
          // Refresh the list
          loadData();
          setSelectedDashboard(null);
        } else {
          alert('Error: ' + (result.ValidationResult.LocalizedResult || 'Failed to delete dashboard'));
        }
      }
    } catch (err: any) {
      console.error('Failed to delete dashboard:', err);
      alert('Error: ' + (err.message || 'Failed to delete dashboard'));
    }
  };

  const closeRowContextMenu = () =>
    setRowContextMenu({ visible: false, x: 0, y: 0, dashboard: null, canEdit: false, canDelete: false });

  const getBool = (obj: any, key: string): boolean | null => {
    if (!obj || !key) return null;
    const v = obj[key];
    if (typeof v === 'boolean') return v;
    if (typeof v === 'number') return v !== 0;
    if (typeof v === 'string') {
      const s = v.trim().toLowerCase();
      if (s === 'true') return true;
      if (s === 'false') return false;
      if (s === '1') return true;
      if (s === '0') return false;
    }
    return null;
  };

  const resolveRowPermissions = (dashboard: any): { canEdit: boolean; canDelete: boolean } => {
    // Angular behavior:
    // - Edit is always visible
    // - Delete is hidden for global default dashboards
    const isGlobalDefault = getBool(dashboard, 'IsGlobalDefault') ?? false;
    return { canEdit: true, canDelete: !isGlobalDefault };
  };

  const openRowContextMenu = (event: React.MouseEvent, dashboard: DashboardDto) => {
    event.preventDefault();
    event.stopPropagation();
    setSelectedDashboard(dashboard);

    const perms = resolveRowPermissions(dashboard as any);
    const { x, y } = clampContextMenuPosition(
      event.clientX,
      event.clientY,
      CONTEXT_MENU_ESTIMATED_WIDTH,
      CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setRowContextMenu({
      visible: true,
      x,
      y,
      dashboard,
      ...perms
    });
  };

  const menuCellTemplate = (ctx: any) => {
    return (
      <button
        className={`${theme.menu_default}`}
        title="More Options"
        style={{ width: '30px' }}
        onClick={(e) => openRowContextMenu(e, ctx.item)}
      >
        <i className="fa fa-pencil" aria-hidden="true" style={{ fontSize: '12px' }}></i>
        <i className="fa fa-navicon" aria-hidden="true" style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }}></i>
      </button>
    );
  };


  if (loading) {
    return (
      <div className="p-5 text-center">
        <BusyLoader />
      </div>
    );
  }

  if (error) {
    return (
      <div className={`p-5 ${theme.mainContentSection}`}>
        <div className="text-sm text-red-500">Error: {error}</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header / Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Dashboard Management</div>

        <div className="flex items-center gap-2">
          <button
            onClick={loadData}
            className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center justify-center"
            title="Refresh"
          >
            <i className="fa fa-refresh" aria-hidden="true"></i>
          </button>

          <button
            onClick={() => {
              closeRowContextMenu();
              handleCreateDashboard();
            }}
            className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600 flex items-center justify-center"
            title="Create"
          >
            <i className="fa fa-plus" aria-hidden="true"></i>
          </button>
        </div>
      </div>

      {/* Grid */}
      <div className={`w-full h-[200px] ${theme.mainContentSection} flex-auto overflow-hidden`}>
        <FlexGrid
          ref={flexGridRef}
          itemsSource={new CollectionView(dashboardList)}
          selectionMode="Row"
          isReadOnly={true}
          allowDelete={false}
          style={{
            width: '100%',
            height: '100%',
            borderLeft: 'none',
            borderRight: 'none',
            borderTop: 'none',
            borderBottom: 'none'
          }}
          selectionChanged={(s) => {
            if (s && s.selection) {
              const sel = s.selection;
              if (sel.row >= 0 && s.rows[sel.row].dataItem) {
                setSelectedDashboard(s.rows[sel.row].dataItem);
              } else {
                setSelectedDashboard(null);
              }
            }
            if (rowContextMenu.visible) {
              closeRowContextMenu();
            }
          }}
        >
          <FlexGridColumn header="" binding="" width={60} allowSorting={false}>
            <FlexGridCellTemplate cellType="Cell" template={menuCellTemplate} />
          </FlexGridColumn>
          <FlexGridColumn binding="DesktopName" header="Dashboard Name" width={200} />
          <FlexGridColumn binding="StateCode" header="State" width={100} />
          <FlexGridColumn
            binding="CreatedBy"
            header="Created By"
            width={150}
            dataMap={new DataMap(
              Array.from(usersDataMap.entries()).map(([id, display]) => ({ id, display })),
              'id',
              'display'
            )}
          />
          <FlexGridColumn binding="CreatedDate" header="Created Date" width={150} format="g" />
          <FlexGridColumn
            binding="ModifiedBy"
            header="Modified By"
            width={150}
            dataMap={new DataMap(
              Array.from(usersDataMap.entries()).map(([id, display]) => ({ id, display })),
              'id',
              'display'
            )}
          />
          <FlexGridColumn binding="ModifiedDate" header="Modified Date" width={150} format="g" />
          {/* Spacer column to fill remaining width (must be last) */}
          <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
        </FlexGrid>
      </div>

      {/* Row Context Menu */}
      {rowContextMenu.visible && (
        <div
          ref={rowContextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
          style={{
            left: rowContextMenu.x,
            top: rowContextMenu.y,
          }}
          onClick={(e) => e.stopPropagation()}
        >
          {/* Edit: always visible (Angular behavior) */}
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => {
              handleEditDashboard(rowContextMenu.dashboard);
              closeRowContextMenu();
            }}
          >
            <i className="fa fa-edit mr-2"></i>
            Edit
          </button>

          {/* Divider + Delete: only when not global default */}
          {rowContextMenu.canDelete && (
            <>
              <div className="my-1 border-t border-slate-200 dark:border-slate-700" />
              <button
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
                onClick={async () => {
                  await handleDeleteDashboard(rowContextMenu.dashboard);
                  closeRowContextMenu();
                }}
              >
                <i className="fa fa-trash-o mr-2"></i>
                Delete
              </button>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default DashboardManagement;
