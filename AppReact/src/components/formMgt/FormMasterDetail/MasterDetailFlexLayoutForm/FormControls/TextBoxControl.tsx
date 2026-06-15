import React from 'react';
import { useTheme } from '../../../../../redux/hooks/useTheme';
import { getOneToOneFieldValue, buildFormDataWithOneToOneValue } from './formDataBindingHelper';

interface TextBoxControlProps {
  layoutItemExDto: any;
  fieldDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
}

const TextBoxControl: React.FC<TextBoxControlProps> = ({
  layoutItemExDto,
  fieldDto,
  controllerModel,
  dataModel,
  onDataModelChange
}) => {
  const { theme, t } = useTheme();

  // Get field value (DictOneToOneFields or DictSiblingOneToOneFields per Angular)
  const fieldName = fieldDto.DataBaseFieldName;
  const fieldValue = getOneToOneFieldValue(dataModel.currentFormData, fieldDto, fieldName, '', layoutItemExDto) as string;
  
  // Check if field is read-only
  const isReadOnly = fieldDto.IsFormLayoutReadOnly === true || 
                    dataModel.currentFormData?.IsLockTransaction === true;
  
  // Get required mark (UI only)
  const isRequired = fieldDto.IsAllowEmpty === false;
  const requiredMark = isRequired ? <span className="text-red-500">*</span> : null;
  
  // Get max length
  const maxLength = fieldDto.MaxCharLegnth || undefined;
  
  // Get tooltip
  const tooltip = fieldDto.ToolTip || fieldDto.LabelDisplayBinding || '';
  
  // Get label
  const label = fieldDto.DisplayName || fieldDto.LabelDisplayBinding || fieldName;
  
  // UI required validation message (set by FormMainMenus on Save)
  const rootUnitId = dataModel.currentFormData?.RootUnitId ?? null;
  const errorKey =
    rootUnitId != null && fieldDto?.TransactionUnitId != null && String(fieldDto.TransactionUnitId) !== String(rootUnitId)
      ? `${String(fieldDto.TransactionUnitId)}.${String(fieldName)}`
      : String(fieldName);
  const errorText = dataModel?.uiValidationErrors?.[errorKey] as string | undefined;

  // Handle value change
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    
    onDataModelChange({
      ...dataModel,
      uiValidationErrors:
        errorText && dataModel?.uiValidationErrors
          ? (() => {
              const copy = { ...(dataModel.uiValidationErrors ?? {}) };
              delete copy[errorKey];
              return copy;
            })()
          : dataModel?.uiValidationErrors,
      currentFormData: buildFormDataWithOneToOneValue(dataModel.currentFormData, fieldDto, fieldName, newValue, layoutItemExDto)
    });
  };

  // Get validation class (TODO: implement validation)
  const validationClass = ''; // TODO: Add validation CSS class

  // Parse style string to object
  const styleObject: React.CSSProperties = {};
  if (layoutItemExDto.StyleLayoutInfo) {
    const styles = layoutItemExDto.StyleLayoutInfo.split(';').filter((s: string) => s.trim());
    styles.forEach((style: string) => {
      const [key, value] = style.split(':').map((s: string) => s.trim());
      if (key && value) {
        const camelKey = key.replace(/-([a-z])/g, (g: string) => g[1].toUpperCase());
        (styleObject as any)[camelKey] = value;
      }
    });
  }

  const isHideLabel = controllerModel?.isFilePropertyEdit === true;

  return (
    <div 
      className={`w-full flex items-start gap-2 ${validationClass}`}
      style={styleObject}
      title={tooltip}
    >
      {!isHideLabel && (
        <div className="flex-shrink-0 min-w-[120px]">
          <label className={`text-xs font-semibold ${theme.title}`}>
            {label} {requiredMark}
          </label>
        </div>
      )}
      <div className="w-1 flex-auto">
        <input
          type="text"
          value={fieldValue}
          onChange={handleChange}
          disabled={isReadOnly}
          maxLength={maxLength}
          className={`w-full h-[30px] px-2 py-1 text-sm border rounded ${theme.inputBox} ${
            isReadOnly ? `${t('bg_input_readonly')} cursor-not-allowed` : ''
          } ${errorText ? 'border-red-500' : ''}`}
        />
        {errorText && <div className="text-xs text-red-600 mt-0.5">{errorText}</div>}
      </div>
    </div>
  );
};

export default TextBoxControl;

