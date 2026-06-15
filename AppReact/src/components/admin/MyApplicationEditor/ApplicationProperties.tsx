import React, { useEffect, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { adminSvc } from '../../../webapi/adminsvc';
import { InputNumber } from '@mescius/wijmo.react.input';
import '@mescius/wijmo.styles/wijmo.css';

interface ApplicationPropertiesProps {
  menuId: string | null;
  applicationData: any;
}

const ApplicationProperties: React.FC<ApplicationPropertiesProps> = ({ menuId, applicationData }) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages } = useErrorMessage();
  const dispatch = useDispatch();
  
  const [formData, setFormData] = useState({
    Name: '',
    Description: '',
    Sort: 1
  });
  
  // Ref for application name input
  const applicationNameInputRef = React.useRef<HTMLInputElement>(null);

  // Load application data when component mounts or applicationData changes
  useEffect(() => {
    if (applicationData) {
      setFormData({
        Name: applicationData.Name || '',
        Description: applicationData.Description || '',
        Sort: applicationData.Sort || 1
      });
      
      // If application name is "New Application", auto-focus on the application name input
      if (applicationData.Name === 'New Application' && applicationNameInputRef.current) {
        // Use setTimeout to ensure the input is rendered before focusing
        setTimeout(() => {
          applicationNameInputRef.current?.focus();
          // Select all text if needed
          applicationNameInputRef.current?.select();
        }, 100);
      }
    }
  }, [applicationData]);

  const handleRefresh = async () => {
    if (!menuId) return;
    
    try {
      dispatch(setIsBusy());
      const menuData = await adminSvc.retrieveOneAppListMenuExDto(menuId);
      if (menuData) {
        setFormData({
          Name: menuData.Name || '',
          Description: menuData.Description || '',
          Sort: menuData.Sort || 1
        });
      }
    } catch (error: any) {
      showError(error.message || 'Failed to refresh application data');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleSave = async () => {
    if (!menuId || !applicationData) return;
    
    try {
      dispatch(setIsBusy());
      
      // Prepare data to save - merge applicationData with formData and set IsModified
      const dataToSave = {
        ...applicationData,
        ...formData,
        IsModified: true
      };
      
      // Call the save API
      const result = await adminSvc.saveOneSaasApplicationSetting(dataToSave);
      
      // If save was successful, reload data from server
      if (result?.IsSuccessful) {
        const menuData = await adminSvc.retrieveOneAppListMenuExDto(menuId);
        if (menuData) {
          setFormData({
            Name: menuData.Name || '',
            Description: menuData.Description || '',
            Sort: menuData.Sort || 1
          });
        }
      }
      
      // Always display validation messages from server (errors, warnings, info messages)
      // Server returns ValidationResult regardless of success/failure
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
    } catch (error: any) {
      showError(error.message || 'Failed to save application settings');
    } finally {
      dispatch(setIsNotBusy());
    }
  };


  return (
    <div className="h-full flex flex-col gap-1">
      {/* Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Application Properties
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={handleRefresh}
            className="px-3 py-1 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center gap-1"
            title="Refresh"
          >
            <i className="fa-solid fa-rotate"></i>
            <span>Refresh</span>
          </button>
          <button
            onClick={handleSave}
            className="px-3 py-1 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600 flex items-center gap-1"
            title="Save"
          >
            <i className="fa-solid fa-save"></i>
            <span>Save</span>
          </button>
        </div>
      </div>

      {/* Content Area */}
      <div className={`flex-auto h-1 overflow-y-auto ${theme.mainContentSection} p-4`}>
       

        {/* Form Fields */}
        <div className="w-full max-w-[850px]">
          {/* Application Name */}
          <div className="mb-4">
            <label className={`block mb-2 text-xs font-semibold ${theme.title}`}>
              Application Name
            </label>
            <input
              ref={applicationNameInputRef}
              type="text"
              value={formData.Name}
              onChange={(e) => setFormData(prev => ({ ...prev, Name: e.target.value }))}
              placeholder="Application Name"
              className={`w-full px-2 py-1 text-xs border rounded ${theme.inputBox}`}
            />
          </div>

          {/* Sort Order */}
          <div className="mb-4">
            <label className={`block mb-2 text-xs font-semibold ${theme.title}`}>
              Sort Order
            </label>
            <div style={{ width: '120px' }}>
              <InputNumber
                value={formData.Sort}
                valueChanged={(sender) => {
                  setFormData(prev => ({ ...prev, Sort: sender.value || 1 }));
                }}
                min={1}
                step={1}
                format="n0"
                style={{ height: '24px', fontSize: '11px' }}
              />
            </div>
          </div>

          {/* Description */}
          <div className="mb-4">
            <label className={`block mb-2 text-xs font-semibold ${theme.title}`}>
              Description
            </label>
            <textarea
              value={formData.Description}
              onChange={(e) => setFormData(prev => ({ ...prev, Description: e.target.value }))}
              placeholder="Description"
              rows={12}
              className={`w-full px-2 py-1 text-xs border rounded resize-none ${theme.inputBox}`}
              style={{ height: '300px' }}
            />
          </div>
        </div>
      </div>
    </div>
  );
};

export default ApplicationProperties;
