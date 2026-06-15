import React, { useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { RootState } from '../../redux/store';
import { dashboardService } from '../../webapi/dashboardsvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { BusyLoader } from '../common/BusyLoader';
import { useTheme } from '../../redux/hooks/useTheme';
import appHelper from '../../helper/appHelper';
import { DashboardWidgetRenderer } from './widgets/DashboardWidgetRenderer';

interface DashboardParams extends Record<string, string | undefined> {
  param?: string;
}

interface DashboardProps {
  dashboardId?: string | null;
}

const Dashboard: React.FC<DashboardProps> = ({ dashboardId: propDashboardId }) => {
  const { param } = useParams<DashboardParams>();
  const { addTabAndNavigate } = useTabNavigation();
  const { userContext } = useSelector((state: RootState) => state.userSession);
  const { t, theme } = useTheme();
  
  const [dashboardData, setDashboardData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAccessDenied, setIsAccessDenied] = useState(false);

  // Prevent repeated loads for the same desktopId (even if parent re-renders a lot)
  const lastLoadedDesktopIdRef = useRef<string | null>(null);
  const inFlightDesktopIdRef = useRef<string | null>(null);

  // Parse desktopId from param or prop
  const getDesktopId = (): string | null => {
    // If dashboardId is provided as prop, use it
    if (propDashboardId !== undefined) {
      return propDashboardId == null ? null : String(propDashboardId);
    }
    
    // Otherwise, parse from URL param
    if (!param) return null;
    try {
      const decoded = decodeURIComponent(param);
      const paramObj = JSON.parse(decoded);
      return paramObj.id ? String(paramObj.id) : null;
    } catch {
      // If parsing fails, treat param as direct ID
      return param;
    }
  };

  useEffect(() => {
    const loadDashboard = async () => {
      const desktopId = getDesktopId();

      // If we're already loading this desktopId, don't start another request.
      if (desktopId && inFlightDesktopIdRef.current === desktopId) {
        return;
      }

      // If we've already loaded this desktopId and have data, don't reload.
      if (
        desktopId &&
        lastLoadedDesktopIdRef.current === desktopId &&
        dashboardData &&
        !error &&
        !isAccessDenied
      ) {
        return;
      }

      setLoading(true);
      setError(null);
      
      if (!desktopId) {
        // No desktop ID, show empty dashboard
        setDashboardData(null);
        setLoading(false);
        return;
      }

      inFlightDesktopIdRef.current = desktopId;

      try {
        const data = await dashboardService.getOneAppDesktop(desktopId);
        
        if (data.IsAccessDenied) {
          setIsAccessDenied(true);
          setLoading(false);
          inFlightDesktopIdRef.current = null;
          return;
        }

        // Parse OtherSettings if it's a string
        if (data.OtherSettings && typeof data.OtherSettings === 'string') {
          try {
            data.OtherSettingsDto = JSON.parse(data.OtherSettings);
          } catch (e) {
            console.error('Failed to parse OtherSettings:', e);
            data.OtherSettingsDto = {};
          }
        } else if (data.OtherSettings) {
          data.OtherSettingsDto = data.OtherSettings;
        }

        setDashboardData(data);
        lastLoadedDesktopIdRef.current = desktopId;
        appHelper.debugLog('Dashboard data loaded:', data);
        appHelper.debugLog('LayoutType:', data.LayoutType);
        appHelper.debugLog('AppDesktopItemList:', data.AppDesktopItemList);
        appHelper.debugLog('OtherSettingsDto:', data.OtherSettingsDto);
        
        // For Flex Layout, check FlexLayoutItems
        if (data.LayoutType === 2 && data.OtherSettingsDto?.FlexLayoutItems) {
          appHelper.debugLog('FlexLayoutItems:', data.OtherSettingsDto.FlexLayoutItems);
          data.OtherSettingsDto.FlexLayoutItems.forEach((flexItem: any, flexIndex: number) => {
            appHelper.debugLog(`FlexLayoutItem ${flexIndex}:`, flexItem);
            if (flexItem.ChildDesktopItems) {
              flexItem.ChildDesktopItems.forEach((childItem: any, childIndex: number) => {
                appHelper.debugLog(`  Child ${childIndex} (full object):`, childItem);
                appHelper.debugLog(`  Child ${childIndex} properties:`, {
                  Id: childItem.Id,
                  WidgetItemType: childItem.WidgetItemType,
                  DirectiveName: childItem.DirectiveName,
                  DisplayName: childItem.DisplayName,
                  Name: childItem.Name,
                  Description: childItem.Description,
                  RouteCode: childItem.RouteCode,
                  Link: childItem.Link,
                  SearchId: childItem.SearchId,
                  TransactionId: childItem.TransactionId,
                  FormId: childItem.FormId,
                  InternalStateName: childItem.InternalStateName,
                  InternalLinkStr: childItem.InternalLinkStr,
                  ExternalUrl: childItem.ExternalUrl,
                  InternalPageRoute: childItem.InternalPageRoute,
                  TargetId: childItem.TargetId,
                  LinkType: childItem.LinkType,
                  ChildDesktopItems: childItem.ChildDesktopItems
                });
              });
            }
          });
        }
        
        if (data.AppDesktopItemList && data.AppDesktopItemList.length > 0) {
          data.AppDesktopItemList.forEach((item: any, index: number) => {
            appHelper.debugLog(`Widget ${index}:`, {
              Id: item.Id,
              WidgetItemType: item.WidgetItemType,
              DirectiveName: item.DirectiveName,
              DisplayName: item.DisplayName,
              X: item.X,
              Y: item.Y,
              Width: item.Width,
              Height: item.Height
            });
          });
        }
        setLoading(false);
        inFlightDesktopIdRef.current = null;
      } catch (err: any) {
        console.error('Failed to load dashboard:', err);
        setError(err.message || 'Failed to load dashboard');
        setLoading(false);
        inFlightDesktopIdRef.current = null;
      }
    };

    loadDashboard();
  }, [param, propDashboardId]);

  // Handle internal shortcut navigation
  const handleInternalShortcut = (stateName: string, menuLinkStr: string, menuName?: string) => {
    const tabHeading = menuName || stateName;
    
    // Build route path - keep original casing for routes like MasterDataManagement
    const routeLower = (stateName || '').toLowerCase();
    let routePath: string;
    if (routeLower === 'masterdatamanagement') {
      routePath = '/MasterDataManagement';
    } else {
      routePath = `/${stateName}`;
    }
    
    if (menuLinkStr) {
      // Build parameter object based on route type
      let paramObj: any = {};
      
      if (routeLower === 'masterdatamanagement') {
        // MasterDataManagement uses searchId parameter
        paramObj.searchId = menuLinkStr || null;
      } else if (routeLower === 'user-editor') {
        // user-editor uses userId parameter
        paramObj.userId = menuLinkStr || null;
      } else {
        // Common pattern: "id_param1_param2"
        let paramId: string | null = null;
        let param1: string | null = null;
        let param2: string | null = null;

        if (menuLinkStr.indexOf("_") > 0) {
          const paramArray = menuLinkStr.split("_");
          paramId = paramArray[0] || null;
          param1 = paramArray[1] || null;
          param2 = paramArray[2] || null;
        } else {
          paramId = menuLinkStr;
        }

        paramObj.id = paramId;
        if (param1) paramObj.param1 = param1;
        if (param2) paramObj.param2 = param2;
      }

      addTabAndNavigate(routePath, tabHeading, paramObj, true);
    } else {
      addTabAndNavigate(routePath, tabHeading, {}, true);
    }
  };

  // Handle search shortcut navigation
  const handleSearchShortcut = (searchId: string, isSavedSearchParameter?: boolean, displayTitle?: string) => {
    const isSavedSearch = isSavedSearchParameter?.toString().toLowerCase() === 'true';
    const tabHeading = displayTitle || 'Search';
    
    addTabAndNavigate('/masterdatamanagement', tabHeading, { 
      searchId,
      isSavedSearch: isSavedSearch 
    }, true);
  };

  // Handle workflow execution (reserved for future use)
  const _handleExecuteWorkflow = async (workflowTransactionId: string) => {
    if (!workflowTransactionId) return;
    
    // TODO: Implement workflow execution
    appHelper.debugLog('Execute workflow:', workflowTransactionId);
  };

  if (loading) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <BusyLoader />
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '20px', color: 'red' }}>
        Error: {error}
      </div>
    );
  }

  if (isAccessDenied) {
    return (
      <div style={{ padding: '20px', color: 'red', textAlign: 'center' }}>
        Access Denied
      </div>
    );
  }

  if (!dashboardData) {
    return (
      <div className={`${theme.mainContentSection} rounded-lg p-6`}>
        <h2 className={`text-xl font-bold mb-4 ${t('text_title')}`}>Dashboard</h2>
        <p className={t('text_default')}>No dashboard selected. Please select a dashboard from the menu.</p>
      </div>
    );
  }

  // Render dashboard based on layout type
  // LayoutType: 1 = Canvas Layout, 2 = Flex/Responsive Layout
  const layoutType = dashboardData.LayoutType;

  // Edit permission: Angular logic from AppSecurityUserBL / desktop view
  // isAllowEditDashboard = (OtherSettingsDto != null && OtherSettingsDto.IsUserDesktop) || (AppCreatedById == CurrentUserId)
  const currentUserId = userContext?.UserId ?? userContext?.Id ?? null;
  const isUserDesktop = Boolean(
    dashboardData?.OtherSettingsDto != null && dashboardData?.OtherSettingsDto?.IsUserDesktop
  );
  const isCreator =
    currentUserId != null &&
    dashboardData?.AppCreatedById != null &&
    String(dashboardData.AppCreatedById) === String(currentUserId);
  const canEdit = isUserDesktop || isCreator;

  // Get desktop items for Canvas Layout
  const desktopItems = dashboardData.AppDesktopItemList || [];
  
  // Render Flex Layout container recursively
  const renderFlexLayoutContainer = (containerItem: any, depth: number = 0): React.ReactNode => {
    const containerType = containerItem.WidgetItemType;
    const childItems = containerItem.ChildDesktopItems || [];
    const styleInfo = containerItem.StyleLayoutInfo || '';
    
    appHelper.debugLog(`renderFlexLayoutContainer depth ${depth}:`, {
      containerType,
      childItemsCount: childItems.length
    });
    
    // Parse style info for additional CSS
    const styleObj: React.CSSProperties = { overflow: 'visible' };
    if (styleInfo) {
      const styles = styleInfo.split(';').filter((s: string) => s.trim());
      styles.forEach((style: string) => {
        const [key, value] = style.split(':').map((s: string) => s.trim());
        if (key && value) {
          const camelKey = key.replace(/-([a-z])/g, (g: string) => g[1].toUpperCase());
          (styleObj as any)[camelKey] = value;
        }
      });
    }
    
    switch (containerType) {
      case 100: // ResponsiveContainer - Angular: Ctn-ResponsiveContainer
        return (
          <div 
            key={containerItem.Id || `responsive-${depth}`}
            className="Ctn-ResponsiveContainer"
            style={styleObj}
          >
            {childItems.length > 0 ? (
              childItems.map((child: any, index: number) => {
                appHelper.debugLog(`  ResponsiveContainer child ${index}:`, {
                  WidgetItemType: child.WidgetItemType,
                  DisplayName: child.DisplayName || child.Name,
                  hasChildren: !!(child.ChildDesktopItems && child.ChildDesktopItems.length > 0)
                });
                
                if (child.WidgetItemType === 100 || child.WidgetItemType === 101 || child.WidgetItemType === 102) {
                  // Recursive: render nested container
                  return <React.Fragment key={child.Id || `child-${index}`}>{renderFlexLayoutContainer(child, depth + 1)}</React.Fragment>;
                } else {
                  // Render actual widget with styling
                  const childColSpan = child.ColSpanValue || 1;
                  return (
                    <div 
                      key={child.Id || `widget-${index}`} 
                      className={`dashboard-widget-item CSpan_${childColSpan} ${theme.mainContentSection} rounded-lg shadow-sm border ${t('border_default')}`}
                    >
                      {renderWidget(child)}
                    </div>
                  );
                }
              })
            ) : (
              <div className={`p-4 ${theme.mainContentSection}`}>
                <p className={`text-xs ${t('text_default')}`}>No child items in ResponsiveContainer</p>
              </div>
            )}
          </div>
        );
      
      case 101: // RowContainer - horizontal layout - Angular: Ctn-HorizontalFlexDiv TotalColumnsX
        // Calculate total columns: sum of all child ColSpan values (not child count!)
        // Angular version: TotalColumns = sum of child ColSpan values
        let totalColumns = 0;
        childItems.forEach((child: any) => {
          const childColSpan = child.ColSpanValue ?? child.ColumnSpan ?? 1;
          totalColumns += childColSpan;
        });
        // If no children, default to 1
        if (totalColumns === 0) totalColumns = 1;
        const totalColumnsClass = `TotalColumns${totalColumns}`;
        const rowHeight = containerItem.RowHeight || containerItem.Height || 'auto';
        const rowStyle = {
          ...styleObj,
          height: typeof rowHeight === 'number' ? `${rowHeight}px` : rowHeight, 
          position: 'relative' as const,
          overflow: 'visible' as const,
          flexWrap: 'nowrap' as const
        };
        
        return (
          <div 
            key={containerItem.Id || `row-${depth}`}
            className={`Ctn-HorizontalFlexDiv ${totalColumnsClass}`}
            style={rowStyle}
          >
            {childItems.length > 0 ? (
              childItems.map((child: any, index: number) => {
                // Get column span from child or DesktopWidget
                const childColSpan = child.ColSpanValue || child.ColumnSpan || 
                  (child.DesktopWidget && (child.DesktopWidget.ColSpanValue || child.DesktopWidget.ColumnSpan)) || 1;
                
                appHelper.debugLog(`    RowContainer child ${index}:`, {
                  WidgetItemType: child.WidgetItemType,
                  DisplayName: child.DisplayName || child.Name,
                  hasChildren: !!(child.ChildDesktopItems && child.ChildDesktopItems.length > 0),
                  hasDesktopWidget: !!child.DesktopWidget,
                  ColSpanValue: child.ColSpanValue,
                  ColumnSpan: child.ColumnSpan,
                  DesktopWidget_ColSpanValue: child.DesktopWidget?.ColSpanValue,
                  DesktopWidget_ColumnSpan: child.DesktopWidget?.ColumnSpan,
                  finalColSpan: childColSpan,
                  allKeys: Object.keys(child),
                  DesktopWidget_keys: child.DesktopWidget ? Object.keys(child.DesktopWidget) : []
                });
                
                if (child.WidgetItemType === 100 || child.WidgetItemType === 101 || child.WidgetItemType === 102) {
                  // Recursive: render nested container
                  return <React.Fragment key={child.Id || `child-${index}`}>{renderFlexLayoutContainer(child, depth + 1)}</React.Fragment>;
                } else {
                  // Render actual widget with col span
                  const childColSpan = child.ColSpanValue || child.ColumnSpan || 1;
                  return (
                    <div 
                      key={child.Id || `widget-${index}`} 
                      className={`dashboard-widget-item CSpan_${childColSpan} ${theme.mainContentSection} rounded-lg shadow-sm border ${t('border_default')}`}
                    >
                      {renderWidget(child)}
                    </div>
                  );
                }
              })
            ) : (
              <div className={`p-4 ${theme.mainContentSection}`}>
                <p className={`text-xs ${t('text_default')}`}>Empty RowContainer</p>
              </div>
            )}
          </div>
        );
      
      case 102: // ColumnContainer (CellContainer) - Angular: CellContainerControl ColSpanX
        // According to Angular version: Check ChildDesktopItems first, then DesktopWidget
        // If ChildDesktopItems exists and has items, recursively render ResponsiveContainer
        // Otherwise, if DesktopWidget exists, render the actual widget
        // Otherwise, render empty div
        
        const colSpan = containerItem.ColSpanValue ?? containerItem.ColumnSpan ?? 1;
        const minWidth = containerItem.MinWidth ? `${containerItem.MinWidth}px` : '300px';
        const cellRowHeight = containerItem.RowHeight || containerItem.Height || 'auto';
        
        // Apply ColSpan class (ColSpan1, ColSpan2, etc.) - Angular only uses ColSpanX, not CSpan_X
        const colSpanClass = colSpan ? `ColSpan${colSpan}` : 'ColSpan1';
        
        // Check if ColumnContainer itself has widget data (not in DesktopWidget property)
        // According to Angular, it checks DesktopWidget, but React data might have widget data directly
        const hasWidgetDataInContainer = containerItem.WidgetItemType && 
          containerItem.WidgetItemType !== 100 && 
          containerItem.WidgetItemType !== 101 && 
          containerItem.WidgetItemType !== 102;
        
        // Log full item structure for debugging
        appHelper.debugLog(`    ColumnContainer (depth ${depth}):`, {
          hasChildren: childItems.length > 0,
          hasDesktopWidget: !!containerItem.DesktopWidget,
          DesktopWidget_WidgetItemType: containerItem.DesktopWidget?.WidgetItemType,
          DesktopWidget_DirectiveName: containerItem.DesktopWidget?.DirectiveName,
          DesktopWidget_DomElementTag: containerItem.DesktopWidget?.DomElementTag,
          hasWidgetDataInContainer,
          WidgetItemType: containerItem.WidgetItemType,
          ColumnSpan: containerItem.ColumnSpan,
          ColSpanValue: containerItem.ColSpanValue,
          finalColSpan: colSpan,
          colSpanClass: colSpanClass,
          MinWidth: minWidth,
          RowHeight: cellRowHeight
        });
        
        // If has ChildDesktopItems, recursively render ResponsiveContainer (nested container)
        if (childItems.length > 0) {
          return (
            <div 
              key={containerItem.Id || `col-${depth}`}
              className={`CellContainerControl ${colSpanClass}`}
              style={{
                height: '100%',
                position: 'relative' as const,
                overflow: 'visible' as const
              }}
            >
              {childItems.map((child: any, index: number) => {
                appHelper.debugLog(`    ColumnContainer child ${index}:`, {
                  WidgetItemType: child.WidgetItemType,
                  DisplayName: child.DisplayName || child.Name,
                  hasChildren: !!(child.ChildDesktopItems && child.ChildDesktopItems.length > 0),
                  ColSpanValue: child.ColSpanValue,
                  allKeys: Object.keys(child)
                });
                
                if (child.WidgetItemType === 100 || child.WidgetItemType === 101 || child.WidgetItemType === 102) {
                  // Recursive: render nested container
                  return <React.Fragment key={child.Id || `child-${index}`}>{renderFlexLayoutContainer(child, depth + 1)}</React.Fragment>;
                } else {
                  // Render actual widget with styling
                  const childColSpan = child.ColSpanValue || 1;
                  return (
                    <div 
                      key={child.Id || `widget-${index}`} 
                      className={`dashboard-widget-item CSpan_${childColSpan} ${theme.mainContentSection} rounded-lg shadow-sm border ${t('border_default')}`}
                    >
                      {renderWidget(child)}
                    </div>
                  );
                }
              })}
            </div>
          );
        }
        
        // If has DesktopWidget, render the actual widget
        if (containerItem.DesktopWidget) {
          appHelper.debugLog(`    -> Rendering DesktopWidget in ColumnContainer`);
          return (
            <div 
              key={containerItem.Id || `col-widget-${depth}`}
              className={`CellContainerControl ${colSpanClass}`}
              style={{
                height: '100%',
                position: 'relative' as const,
                overflow: 'visible' as const
              }}
            >
              <div 
                id={`CellDropContainer_${containerItem.Id || `cell-${depth}`}`}
                style={{ height: '100%', width: '100%', position: 'relative' as const }}
              >
                <div 
                  className="LayoutCellItemContainer w-full h-full">
                  
                    {renderWidget(containerItem.DesktopWidget)}
                 
                </div>
              </div>
            </div>
          );
        }
        
        // If ColumnContainer itself has widget data (WidgetItemType is not a container type)
        if (hasWidgetDataInContainer) {
          console.log(`    -> Rendering ColumnContainer as widget (has widget data in container itself)`);
          return (
            <div 
              key={containerItem.Id || `col-widget-${depth}`}
              className={`CellContainerControl ${colSpanClass}`}
              style={{
                height: '100%',
                position: 'relative' as const,
                overflow: 'visible' as const
              }}
            >
              <div 
                id={`CellDropContainer_${containerItem.Id || `cell-${depth}`}`}
                style={{ height: '100%', width: '100%', position: 'relative' as const }}
              >
                <div 
                   className="LayoutCellItemContainer w-full h-full">
                 
                    {renderWidget(containerItem)}
                
                </div>
              </div>
            </div>
          );
        }
        
        // If ColumnContainer has no children and no widget data, it's just a layout placeholder
        // Return null to skip rendering it (don't show "Empty ColumnContainer" message)
        if (childItems.length === 0 && !hasWidgetDataInContainer && !containerItem.DesktopWidget) {
          appHelper.debugLog(`    -> Skipping empty ColumnContainer (no children, no widget data)`);
          return null;
        }
        
        // Otherwise, render empty div (placeholder)
        appHelper.debugLog(`    -> Rendering empty ColumnContainer placeholder`);
        return (
          <div 
            key={containerItem.Id || `col-empty-${depth}`}
            className={`CellContainerControl ${colSpanClass}`}
            style={{
              height: '100%',
              position: 'relative' as const,
              overflow: 'visible' as const
            }}
          >
            <div 
              id={`CellDropContainer_${containerItem.Id || `cell-${depth}`}`}
              style={{ height: '100%', width: '100%', position: 'relative' as const }}
            >
              <div 
                 className="LayoutCellItemContainer w-full h-full">
               
              </div>
            </div>
          </div>
        );
      
      default:
        // Not a container, render as widget
        return renderWidget(containerItem);
    }
  };

  const handleOpenDashboardEditor = () => {
    if (!dashboardData?.Id) return;
    addTabAndNavigate('/dashboard-edit', dashboardData.Name || 'Dashboard Editor', { id: dashboardData.Id }, true);
  };

  return (
    <div className="w-full h-full relative">
      {/* Edit shortcut (gear) - top-right, only when user has edit permission (Angular dashboard runtime behavior) */}
      {canEdit && (
        <div className="absolute top-[-18px] right-2 z-10 flex items-center gap-1 p-2">
          <button
            type="button"
            onClick={handleOpenDashboardEditor}
            className={`rounded-full w-6 h-6 ${theme.button_default} hover:opacity-90`}
            title="Edit Dashboard"
          >
            <i className="fa fa-cog" aria-hidden="true"></i>
          </button>
        </div>
      )}
      <div className="w-full h-full overflow-auto pr-2.5">
        {layoutType === 1 ? (
          // Canvas Layout - Absolute positioning
          <div 
            className="dashboard-canvas-layout relative" 
            style={{ 
              minHeight: '600px',
              width: '100%',
              height: '100%'
            }}
          >
            {desktopItems.map((item: any, index: number) => (
              <div
                key={item.Id || index}
                className={`${theme.mainContentSection} rounded-lg shadow-sm border ${t('border_default')}`}
                style={{
                  position: 'absolute',
                  left: `${item.X || 0}px`,
                  top: `${item.Y || 0}px`,
                  width: `${item.Width || 300}px`,
                  height: `${item.Height || 200}px`,
                  minWidth: '200px',
                  minHeight: '150px',
                }}
              >
                {renderWidget(item)}
              </div>
            ))}
          </div>
        ) : (
          // Flex/Responsive Layout - Render containers recursively
          <div className="dashboard-flex-layout Row24">
            {dashboardData.OtherSettingsDto?.FlexLayoutItems && dashboardData.OtherSettingsDto.FlexLayoutItems.length > 0 ? (
              dashboardData.OtherSettingsDto.FlexLayoutItems.map((flexItem: any, index: number) => {
                appHelper.debugLog(`Rendering FlexLayoutItem ${index}:`, {
                  WidgetItemType: flexItem.WidgetItemType,
                  ChildDesktopItems: flexItem.ChildDesktopItems?.length || 0,
                  ColSpanValue: flexItem.ColSpanValue
                });
                
                // FlexLayoutItem itself might be a container (WidgetItemType 100, 101, or 102)
                if (flexItem.WidgetItemType === 100 || flexItem.WidgetItemType === 101 || flexItem.WidgetItemType === 102) {
                  return (
                    <React.Fragment key={flexItem.Id || `flex-${index}`}>
                      {renderFlexLayoutContainer(flexItem)}
                    </React.Fragment>
                  );
                } else {
                  // If not a container, render as widget
                  return (
                    <div key={flexItem.Id || `widget-${index}`} className="dashboard-widget-item">
                      {renderWidget(flexItem)}
                    </div>
                  );
                }
              })
            ) : (
              <div className={`p-4 ${theme.mainContentSection} rounded-lg`}>
                <p className={t('text_default')}>No FlexLayoutItems found</p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );

  function renderWidget(item: any) {
    return (
      <DashboardWidgetRenderer
        item={item}
        theme={theme as any}
        t={t as any}
        onInternalShortcut={handleInternalShortcut}
        onSearchShortcut={handleSearchShortcut}
      />
    );
  }
};

export default Dashboard;
