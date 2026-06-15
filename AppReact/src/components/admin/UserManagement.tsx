import React, { useRef, useState, useEffect } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { DataMap } from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { adminSvc } from '../../webapi/adminsvc';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { getDataModelFromCache, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { useTabDataAutoCache } from '../../redux/hooks/useTabNavigation';
import { useTheme } from '../../redux/hooks/useTheme';

// Function to prepare default data for user management (reserved)
const _prepareDefaultData = () => ({
  userData: [],
  userDataCV: new CollectionView<any>([]),
  languages: [],
  languageDataMap: new DataMap([], 'Id', 'Display')
});

// Function to load user data from server
const loadUserDataFromServer = async () => {
  try {
    // Load user data and lookup data in parallel
    const [userData, massEntityData] = await Promise.all([
      adminSvc.getOrganizationUserDtoList(4026, false), // companyId: '1', includeBusinessPartnerUsers: false
      adminSvc.getMassEntitiesLookupItem('AppLanguage')
    ]);

    console.log('Loaded user data:', userData); // Debug log
    console.log('Loaded lookup data:', massEntityData); // Debug log

    // Prepare language data map
    const languages = massEntityData['AppLanguage'] || [];
    const languageDataMap = new DataMap(languages, 'Id', 'Display');

    return {
      userData,
      languages,
      languageDataMap
    };
  } catch (error) {
    console.error('Error loading user data:', error);
    return {
      userData: [],
      languages: [],
      languageDataMap: new DataMap([], 'Id', 'Display')
    };
  }
};

// --- Main component ---
const UserManagement: React.FC = () => {
  // Data model - contains all data-related state and UI controls
  const [dataModel, setDataModel] = useState<{
    userData: any[];
    userDataCV: CollectionView<any>;
    languages: any[];
    languageDataMap: DataMap;
    uiControl: {
      contextMenu: {
        visible: boolean;
        x: number;
        y: number;
        item: any;
      };
    };
  }>({
    userData: [],
    userDataCV: new CollectionView<any>([]),
    languages: [],
    languageDataMap: new DataMap([], 'Id', 'Display'),
    uiControl: {
      contextMenu: {
        visible: false,
        x: 0,
        y: 0,
        item: null
      }
    }
  });

  const flex = useRef<any>(null);
  //const currentTheme = useSelector((state: RootState) => state.theme.currentTheme);
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();

  // Main load data from server function
  const loadDataFromServer = async () => {
    dispatch(setIsBusy());
    try {
      const { userData, languages, languageDataMap } = await loadUserDataFromServer();
      setDataModel(prev => ({
        ...prev,
        userData: userData,
        userDataCV: new CollectionView<any>(userData),
        languages: languages,
        languageDataMap: languageDataMap
      }));
    } catch (error) {
      console.error('Error loading user data:', error);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Initial data load function - checks cache first, then loads from server
  const loadData = async () => {
    let cachedDataModel = null;
    const dataModelKey = getCurrentActiveTab()?.tabKey || null;

    if (dataModelKey) {
      cachedDataModel = getDataModelFromCache(dataModelKey);
      if (cachedDataModel) {
       
        setDataModel(cachedDataModel);
      }
    }

    if (!cachedDataModel) {
      await loadDataFromServer();
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  // Register tab data saver - saves data before switching tabs
  useTabDataAutoCache(dataModel);

  const menuCellTemplate = (ctx: any) => {
    return (
      <>
        <button
          className={`${theme.menu_default}`}
          title="More Options"
          style={{ width: '30px' }}
          onClick={(e) => {
            openUserContextMenu(e, ctx.item);
          }}
        >
          <i className="fa fa-pencil" aria-hidden="true"
            style={{ fontSize: '12px' }}></i>
          <i className="fa fa-navicon" aria-hidden="true"
            style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }}></i>
        </button>
      </>
    );
  };

  const openUserContextMenu = (event: React.MouseEvent, item: any) => {
    event.preventDefault();
    event.stopPropagation();
    console.log('Open context menu for user:', item);

    setDataModel(prev => ({
      ...prev,
      uiControl: {
        ...prev.uiControl,
        contextMenu: {
          visible: true,
          x: event.clientX,
          y: event.clientY,
          item: item
        }
      }
    }));
  };

  const closeContextMenu = () => {
    setDataModel(prev => ({
      ...prev,
      uiControl: {
        ...prev.uiControl,
        contextMenu: {
          visible: false,
          x: 0,
          y: 0,
          item: null
        }
      }
    }));
  };

  const handleEditUser = () => {
    console.log('Edit user:', dataModel.uiControl.contextMenu.item);

    // Navigate to user editor page with user ID
    if (dataModel.uiControl.contextMenu.item && dataModel.uiControl.contextMenu.item.Id) {
      const userId = dataModel.uiControl.contextMenu.item.Id;
      const userName = dataModel.uiControl.contextMenu.item.UserName || dataModel.uiControl.contextMenu.item.LoginName || `User ${userId}`;
      const userEmail = dataModel.uiControl.contextMenu.item.Email || '';
      
      // Use the new JSON parameter approach
      addTabAndNavigate(
        '/user-editor/',
        `Edit ${userName}`,               
        { userId: userId, userName: userName, userEmail: userEmail });
    } else {
      console.error('No user ID found for editing');
    }

    closeContextMenu();
  };

  const handleDeleteUser = () => {
    console.log('Delete user:', dataModel.uiControl.contextMenu.item);
    // Implement delete user logic here
    closeContextMenu();
  };

  // Close context menu when clicking outside
  useEffect(() => {
    const handleClickOutside = () => {
      if (dataModel.uiControl.contextMenu.visible) {
        closeContextMenu();
      }
    };

    document.addEventListener('click', handleClickOutside);
    return () => {
      document.removeEventListener('click', handleClickOutside);
    };
  }, [dataModel.uiControl.contextMenu.visible]);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>User Management</div>
        <div className="flex items-center space-x-2">
          <button
            onClick={loadDataFromServer}
            className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500" title='Refresh'
          >
            <i className="fa fa-refresh"></i>
          </button>
          <button
            onClick={() => addTabAndNavigate('/user-editor/', 'New User', { userId: null })}
            className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600" title='Create New User'
          >
            <i className="fa fa-plus"></i>
          </button>
        </div>
      </div>

      <div className={`w-full h-[200px] ${theme.mainContentSection} flex-auto overflow-hidden`}>
        <FlexGrid
          ref={flex}
          itemsSource={dataModel.userDataCV}
          isReadOnly={true}
          selectionMode="MultiRange"
          validateEdits={false}
          pinningColumn={(grid, args) => {
            const column = grid.columns[args.col];
            if (!column?.binding) {
              args.cancel = true;
            }
          }}
          style={{ width: '100%', height: '100%', borderLeft: 'none', borderRight: 'none', borderTop: 'none', borderBottom: 'none' }}
        >
          <FlexGridFilter
            filterColumns={[
              'DomainId',
              'LoginName',
              'UserName',
              'Email',
              'LanguageId',
              'CultureInfoCode',
              'UserGroupString',
              'IsActive',
            ]}
          />

          <FlexGridColumn header="" binding="" width={100} allowSorting={false}>
            <FlexGridCellTemplate cellType="Cell" template={menuCellTemplate} />
          </FlexGridColumn>

          {/* User Type Column (hidden) */}
          <FlexGridColumn header="User Type" binding="DomainId" width={150} isReadOnly={true} visible={false} />

          {/* Login Name Column */}
          <FlexGridColumn header="Login Name" binding="LoginName" width={150} isReadOnly={true} />

          {/* User Name Column */}
          <FlexGridColumn header="User Name" binding="UserName" width={150} isReadOnly={true} />

          {/* Email Column (hidden) */}
          <FlexGridColumn header="Email" binding="Email" width={200} isReadOnly={true} visible={false} />

          {/* Language Column */}
          <FlexGridColumn header="Language" binding="LanguageId" width={150} isReadOnly={true} dataMap={dataModel.languageDataMap} />

          {/* Culture Info Column */}
          <FlexGridColumn header="Culture Info" binding="CultureInfoCode" width={150} isReadOnly={true} />

          {/* Roles Column */}
          <FlexGridColumn header="Roles" binding="UserGroupString" width="*" isReadOnly={true} />

          {/* Is Active Column */}
          <FlexGridColumn header="Is Active" binding="IsActive" width={100} isReadOnly={true} />
        </FlexGrid>
      </div>

      {/* Context Menu */}
      {dataModel.uiControl.contextMenu.visible && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
          style={{
            left: dataModel.uiControl.contextMenu.x,
            top: dataModel.uiControl.contextMenu.y,
          }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={handleEditUser}
          >
            <i className="fa fa-edit mr-2"></i>
            Edit Login Info
          </button>
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={handleEditUser}
          >
            <i className="fa fa-edit mr-2"></i>
            Edit User Details
          </button>
          <button
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={handleDeleteUser}
          >
            <i className="fa fa-trash mr-2"></i>
            Delete User
          </button>
        </div>
      )}
    </div>
  );
};

export default UserManagement;