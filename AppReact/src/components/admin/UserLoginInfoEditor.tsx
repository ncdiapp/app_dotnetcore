import React, { useState, useEffect, useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { useParams } from 'react-router-dom';
import { adminSvc } from '../../webapi/adminsvc';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { updateCurrentTabLabel, getDataModelFromCache, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { useTabDataAutoCache } from '../../redux/hooks/useTabNavigation';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useTheme } from '../../redux/hooks/useTheme';
import { ComboBox } from '@mescius/wijmo.react.input';
import * as wjInput from '@mescius/wijmo.input';
import { useEnumEntry } from '../../hooks/useEnumDictionary';


// Function to prepare default data for a new user
const prepareNewUserDefaultData = () => ({
  UserName: '',
  LoginName: '',
  Password: '',
  ConfirmPassword: '',
  Email: '',
  DomainId: null,
  LanguageId: null,
  CultureInfoCode: null,
  TimeZoneInfoToken: null,
  IsActive: true,
  MappingExternalEmployeeAccountId: null
});

const UserLoginInfoEditor: React.FC = () => {
  //const currentTheme = useSelector((state: RootState) => state.theme.currentTheme);
  const { theme } = useTheme();

  const dispatch = useDispatch();
  const { showValidationMessages, showError } = useErrorMessage();
  const employeeUserType = useEnumEntry('EmAppUserType', 'Employee');



  // Parse JSON string from param and extract individual parameters
  let paramObj: any = {};

  const { param } = useParams<{ param: string }>();

  if (param) {
    try {
      // Decode the URL-encoded JSON string
      const decodedParam = decodeURIComponent(param);
      paramObj = JSON.parse(decodedParam);
    } catch (error) {
      console.error('Error parsing param JSON:', error);
      showError('Invalid URL parameters. ' + error);
    }
  }

  // Extract individual parameters from the parsed object
  const userId = paramObj.userId || null;


  // Data model - contains all data-related state and UI controls
  const [dataModel, setDataModel] = useState<{
    userData: any;
    domains: any[];
    languages: any[];
    timeZones: any[];
    cultureInfos: any[];
    externalAccounts: any[];
    uiControl: {
      userName: string;
      showExternalMapping: boolean;
    };
  }>({
    userData: prepareNewUserDefaultData(),
    domains: [],
    languages: [],
    timeZones: [],
    cultureInfos: [],
    externalAccounts: [],
    uiControl: {
      userName: '',
      showExternalMapping: false
    }
  });

  const initComboboxBlankSelections = useCallback(() => {
    setDataModel((prev) => {
      return {
        ...prev,
        userData: {
          ...prev.userData,
          DomainId: '',
          LanguageId: '',
          CultureInfoCode: '',
          TimeZoneInfoToken: '',
          MappingExternalEmployeeAccountId: '',
        },
      };
    });
  }, []);

  // Data Loading Functions
  const loadLookupData = async () => {
    const [massEntityData, timeZoneData, cultureData] = await Promise.all([
      adminSvc.getMassEntitiesLookupItem('AppSecurityRegDomain|AppLanguage'),
      adminSvc.retrieveTimeZones(),
      adminSvc.getCultroInfos()
    ]);

    setDataModel(prev => ({
      ...prev,
      domains: massEntityData['AppSecurityRegDomain'] || [],
      languages: massEntityData['AppLanguage'] || [],
      timeZones: timeZoneData || [],
      cultureInfos: cultureData || []
    }));
  };

  const loadUserData = async (userId: string) => {
    const userDataFromServer = await adminSvc.RetrieveMasterDBUserLoginInfo(userId);
    if (!userDataFromServer) return;

    const updatedUserData = {
      ...userDataFromServer,
      ConfirmPassword: userDataFromServer.Password,
      DomainId: userDataFromServer.DomainId || null,
      LanguageId: userDataFromServer.LanguageId || null,
      CultureInfoCode: userDataFromServer.CultureInfoCode || null,
      TimeZoneInfoToken: userDataFromServer.TimeZoneInfoToken || null,
      MappingExternalEmployeeAccountId: userDataFromServer.MappingExternalEmployeeAccountId || null
    };

    setDataModel(prev => ({
      ...prev,
      userData: updatedUserData
    }));

    // Update tab label

    const userName = userDataFromServer.UserName || userDataFromServer.LoginName || `User ${userId}`;
    dispatch(updateCurrentTabLabel(`Edit ${userName}`));
    setDataModel(prev => ({
      ...prev,
      uiControl: { ...prev.uiControl, userName }
    }));

    // Handle external mapping for Employee domain
    if (employeeUserType !== undefined && (userDataFromServer.DomainId === employeeUserType || userDataFromServer.DomainId === String(employeeUserType))) {
      const externalAccountList = await adminSvc.RetrieveEmployeeUserExternalMappingAccountLookupItemList();
      setDataModel(prev => ({
        ...prev,
        externalAccounts: externalAccountList || [],
        uiControl: { ...prev.uiControl, showExternalMapping: true }
      }));
    }
  };

  // Main load data function - consolidates all service calls
  const loadDataFromServer = async () => {
    dispatch(setIsBusy());

    try {
      await loadLookupData();
      initComboboxBlankSelections();
      if (userId) {
        await loadUserData(userId);
      } else {
        // Reset form for new user
        setTimeout(() => {


          setDataModel(prev => ({
            ...prev,
            userData: prepareNewUserDefaultData(),
            externalAccounts: [],
            uiControl: {
              userName: '',
              showExternalMapping: false
            }
          }));
        }, 0);
      }
    } catch (error) {
      console.error('Error loading data:', error);
      showError('Failed to load data');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Save user
  const saveUser = async () => {
    dispatch(setIsBusy());

    try {
      const result = dataModel.userData.Id
        ? await adminSvc.UpdateSaasUserLoginInfo(dataModel.userData)
        : await adminSvc.InviteSaasComapnyNewUser(dataModel.userData);

      if (result.IsSuccessful && result.Object && !dataModel.userData.Id) {
        await loadDataFromServer();
      }

      showValidationMessages(result.ValidationResult);

    } catch (error) {
      showError('An error occurred while saving the user. ' + error);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Handle input changes
  const handleInputChange = (field: string, value: any) => {
    setDataModel(prev => ({
      ...prev,
      userData: { ...prev.userData, [field]: value }
    }));

    // Handle domain changes for external mapping
    if (field === 'DomainId') {
      if (employeeUserType !== undefined && (value === employeeUserType || value === String(employeeUserType))) {
        adminSvc.RetrieveEmployeeUserExternalMappingAccountLookupItemList()
          .then(externalAccountList => {
            setDataModel(prev => ({
              ...prev,
              externalAccounts: externalAccountList || [],
              uiControl: { ...prev.uiControl, showExternalMapping: true }
            }));
          })
          .catch(error => {
            console.error('Error loading external accounts:', error);
            showError('Failed to load external accounts');
          });
      } else {
        setDataModel(prev => ({
          ...prev,
          externalAccounts: [],
          userData: { ...prev.userData, MappingExternalEmployeeAccountId: '' },
          uiControl: { ...prev.uiControl, showExternalMapping: false }
        }));
      }
    }
  };

  const loadData = async () => {
    let cachedDataModel = null;
    let dataModelKey = null;

    if (paramObj.isNavigatedFromTab) {
      dataModelKey = getCurrentActiveTab()?.tabKey || null;
    }

    if (dataModelKey) {
      cachedDataModel = getDataModelFromCache(dataModelKey);
      if (cachedDataModel) {
        // Reconstruct non-serializable objects from cached data
        // const reconstructedDataModel = {
        //   ...cachedDataModel,
        //   // Add any non-serializable object reconstruction here if needed
        // };
        setDataModel(cachedDataModel);
      }
    }

    if (!cachedDataModel) {
      await loadDataFromServer();
    }
  };

  // Initialize component
  useEffect(() => {
    loadData();
  }, [userId]);

  // Register tab data saver - saves data before switching tabs
  useTabDataAutoCache(dataModel);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      <div className={`flex items-center justify-between px-3 mb-1 py-2 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {userId ? `Edit User: ${dataModel.uiControl.userName}` : 'New User Login Info'}
        </div>

        <div className="flex items-center space-x-2">
          <button
            onClick={loadDataFromServer}
            className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
            title="Refresh"
          >
            <i className="fa fa-refresh"></i>
          </button>

          <button
            onClick={saveUser}
            className="w-8 h-6 text-center bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500"
            title="Save"
          >
            <svg className="w-[14px] h-[14px] fill-white mx-auto" viewBox="0 0 448 512">
              <path d="M433.941 129.941l-83.882-83.882A48 48 0 0 0 316.118 32H48C21.49 32 0 53.49 0 80v352c0 26.51 21.49 48 48 48h352c26.51 0 48-21.49 48-48V163.882a48 48 0 0 0-14.059-33.941zM272 80v80H144V80h128zm122 352H54a6 6 0 0 1-6-6V86a6 6 0 0 1 6-6h42v104c0 13.255 10.745 24 24 24h176c13.255 0 24-10.745 24-24V83.882l78.243 78.243a6 6 0 0 1 1.757 4.243V426a6 6 0 0 1-6 6zM224 232c-48.523 0-88 39.477-88 88s39.477 88 88 88 88-39.477 88-88-39.477-88-88-88zm0 128c-22.056 0-40-17.944-40-40s17.944-40 40-40 40 17.944 40 40-17.944 40-40 40z" />
            </svg>
          </button>
        </div>
      </div>

      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {/* Main Form Area */}
        <div className="h-full w-full overflow-auto px-5 py-5">
          <div className="flex flex-wrap gap-10 w-full">
            {/* Left Column */}
            <div className="w-1/2 max-w-[500px] flex-auto">
              {/* User Name */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  User Name
                </label>
                <input
                  type="text"
                  autoComplete="off"
                  value={dataModel.userData.UserName || ''}
                  onChange={e => handleInputChange('UserName', e.target.value)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                />
              </div>

              {/* Login Name */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  Login Name
                </label>
                <input
                  type="text"
                  autoComplete="off"
                  value={dataModel.userData.LoginName || ''}
                  onChange={e => handleInputChange('LoginName', e.target.value)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                />
              </div>

              {/* Password */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  Password
                </label>
                <input
                  type="password"
                  autoComplete="off"
                  value={dataModel.userData.Password || ''}
                  onChange={e => handleInputChange('Password', e.target.value)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                />
              </div>

              {/* Confirm Password */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  Confirm Password
                </label>
                <input
                  name="EditConfirmPassword"
                  autoComplete="off"
                  type="password"
                  value={dataModel.userData.ConfirmPassword || ''}
                  onChange={e => handleInputChange('ConfirmPassword', e.target.value)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                />
              </div>

              {/* Email */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  Email
                </label>
                <input
                  type="email"
                  autoComplete="off"
                  value={dataModel.userData.Email || ''}
                  onChange={e => handleInputChange('Email', e.target.value)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                />
              </div>

              {/* External Mapping */}
              {dataModel.uiControl.showExternalMapping && (
                <div className="flex items-center py-1">
                  <label className={`w-32 text-xs ${theme.label} mr-2`}>
                    Mapping To Account
                  </label>
                  <ComboBox
                    itemsSource={dataModel.externalAccounts || []}
                    displayMemberPath="Display"
                    selectedValuePath="Id"
                    selectedValue={dataModel.userData.MappingExternalEmployeeAccountId}
                    isEditable={false}
                    isRequired={false}
                    className={`flex-auto w-32 h-7 text-xs ${theme.inputBox}`}
                    selectedIndexChanged={(sender: wjInput.ComboBox) => {
                      const value = sender.selectedValue;
                      handleInputChange('MappingExternalEmployeeAccountId', value);
                    }}
                  />
                </div>
              )}
            </div>

            {/* Right Column */}
            <div className="w-1/2 max-w-[500px] flex-auto">
              {/* User Type (Domain) */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  User Type
                </label>
                <ComboBox
                  itemsSource={dataModel.domains || []}
                  displayMemberPath="Display"
                  selectedValuePath="Id"
                  selectedValue={dataModel.userData.DomainId}
                  isEditable={false}
                  isRequired={false}
                  className={`flex-auto w-32 h-7 text-xs ${theme.inputBox}`}
                  isDisabled
                  selectedIndexChanged={(sender: wjInput.ComboBox) => {
                    const value = sender.selectedValue;
                    handleInputChange('DomainId', value);
                  }}
                />
              </div>

              {/* Language */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  Language
                </label>
                <ComboBox
                  itemsSource={dataModel.languages || []}
                  displayMemberPath="Display"
                  selectedValuePath="Id"
                  selectedValue={dataModel.userData.LanguageId}
                  isEditable={false}
                  isRequired={false}
                  className={`flex-auto w-32 h-7 text-xs ${theme.inputBox}`}
                  selectedIndexChanged={(sender: wjInput.ComboBox) => {
                    const value = sender.selectedValue;
                    handleInputChange('LanguageId', value);
                  }}
                />
              </div>

              {/* Culture Info */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  Culture Info
                </label>
                <ComboBox
                  itemsSource={dataModel.cultureInfos || []}
                  displayMemberPath="Display"
                  selectedValuePath="Id"
                  selectedValue={dataModel.userData.CultureInfoCode}
                  isEditable={false}
                  isRequired={false}
                  className={`flex-auto w-32 h-7 text-xs ${theme.inputBox}`}
                  selectedIndexChanged={(sender: wjInput.ComboBox) => {
                    const value = sender.selectedValue;
                    handleInputChange('CultureInfoCode', value);
                  }}
                />
              </div>

              {/* Time Zone */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  Time Zone
                </label>
                <ComboBox
                  itemsSource={dataModel.timeZones || []}
                  displayMemberPath="Display"
                  selectedValuePath="Id"
                  selectedValue={dataModel.userData.TimeZoneInfoToken}
                  isEditable={false}
                  isRequired={false}
                  className={`flex-auto w-32 h-7 text-xs ${theme.inputBox}`}
                  selectedIndexChanged={(sender: wjInput.ComboBox) => {
                    const value = sender.selectedValue;
                    handleInputChange('TimeZoneInfoToken', value);
                  }}
                />
              </div>

              {/* Is Active */}
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>
                  Active
                </label>
                <div className="flex items-center px-1">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={dataModel.userData.IsActive || false}
                    onChange={e => handleInputChange('IsActive', e.target.checked)}
                    className={`h-3 w-3 text-blue-600 focus:ring-blue-500 ${theme.inputBox} rounded`}
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default UserLoginInfoEditor; 