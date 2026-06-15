import React, { useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

interface FormSettingToolboxProps {
  formData: any;
  onFormDataChange: (formData: any) => void;
}

const FormSettingToolbox: React.FC<FormSettingToolboxProps> = ({
  formData,
  onFormDataChange
}) => {
  const { theme } = useTheme();
  const [isCollapsed, setIsCollapsed] = useState<boolean>(true);

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onFormDataChange({
      ...formData,
      Name: e.target.value
    });
  };

  const handleDescriptionChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onFormDataChange({
      ...formData,
      Description: e.target.value
    });
  };

  const handleWidthChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const width = parseInt(e.target.value) || 800;
    onFormDataChange({
      ...formData,
      DefaultWidth: width
    });
  };

  return (
    <div className="w-full mb-4">
      <div 
        className={`flex items-center justify-between px-2 py-2 cursor-pointer ${theme.mainContentSection} border rounded-t`}
        onClick={() => setIsCollapsed(!isCollapsed)}
      >
        <span className={`text-sm font-semibold ${theme.title}`}>Form Setting</span>
        <i 
          className={`fa ${isCollapsed ? 'fa-chevron-circle-down' : 'fa-chevron-circle-up'} text-gray-500`}
        ></i>
      </div>
      
      {!isCollapsed && (
        <div className={`p-3 border-l border-r border-b rounded-b ${theme.mainContentSection}`}>
          <div className="mb-4">
            <div className={`text-xs font-semibold ${theme.title} mb-2`}>Properties</div>
            
            {/* Form Name */}
            <div className="mb-3">
              <label className={`block text-xs mb-1 ${theme.label}`}>Form Name</label>
              <input
                type="text"
                className={`w-full px-2 py-1 text-sm border rounded ${theme.inputBox}`}
                placeholder="Name"
                value={formData?.Name || ''}
                onChange={handleNameChange}
              />
            </div>

            {/* Description */}
            <div className="mb-3">
              <label className={`block text-xs mb-1 ${theme.label}`}>Description</label>
              <input
                type="text"
                className={`w-full px-2 py-1 text-sm border rounded ${theme.inputBox}`}
                placeholder="Description"
                value={formData?.Description || ''}
                onChange={handleDescriptionChange}
              />
            </div>

            {/* Max Width */}
            <div className="mb-3">
              <label className={`block text-xs mb-1 ${theme.label}`}>Max Width</label>
              <div className="text-center text-xs text-gray-500 mb-1">
                {formData?.DefaultWidth || 800} px
              </div>
              <input
                type="range"
                min="300"
                max="1900"
                step="50"
                value={formData?.DefaultWidth || 800}
                onChange={handleWidthChange}
                className="w-full"
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default FormSettingToolbox;
