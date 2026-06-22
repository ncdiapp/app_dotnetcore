import React from 'react';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import { useTheme } from '../../../../redux/hooks/useTheme';
import TextBoxControl from './FormControls/TextBoxControl';
import DDLControl from './FormControls/DDLControl';
import MemoControl from './FormControls/MemoControl';
import ImageControl from './FormControls/ImageControl';
import FileControl from './FormControls/FileControl';
import DateControl from './FormControls/DateControl';
import CheckBoxControl from './FormControls/CheckBoxControl';
import NumericControl from './FormControls/NumericControl';
import LabelControl from './FormControls/LabelControl';
import { isRuntimeTransactionFieldVisible } from './flexLayoutItemHelper';

interface FormItemLayoutProps {
  layoutItemExDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
}

const FormItemLayout: React.FC<FormItemLayoutProps> = ({
  layoutItemExDto,
  controllerModel,
  dataModel,
  onDataModelChange,
  transactionExDto
}) => {
  const { theme } = useTheme();
  const controlTypeEnum = useEnumValues('EmAppControlType');
  const systemTokenFieldEnum = useEnumValues('EmBLFiledMappingSystemTokenField');

  const fieldDto = layoutItemExDto.ForeignAppTransactionFieldExDto;
  
  if (!fieldDto) {
    // Render built-in label if no field
    return <LabelControl layoutItemExDto={layoutItemExDto} />;
  }

  // Check visibility
  if (!isRuntimeTransactionFieldVisible(fieldDto)) {
    return null;
  }

  // Get control type
  const controlType = fieldDto.ControlType;
  
  // Get field binding path
  const fieldName = fieldDto.DataBaseFieldName;
  const _bindField = `dataModel.currentFormData.DictOneToOneFields['${fieldName}']`;
  
  const isHideLabel = controllerModel?.isFilePropertyEdit === true;

  // Check if it's a calendar repeat setting
  if (fieldDto.MappingEmSystemTokenField === systemTokenFieldEnum?.CalendarRepeatSetting) {
    // TODO: Implement CalendarRepeatSetting control
    return (
      <div className="w-full p-2 border">
        {!isHideLabel && <label className={`text-xs font-semibold ${theme.title}`}>{fieldDto.DisplayName || fieldDto.LabelDisplayBinding}</label>}
        <div className="text-sm text-gray-500">Calendar Repeat Setting (TODO)</div>
      </div>
    );
  }

  // Render based on control type
  const commonProps = {
    layoutItemExDto,
    fieldDto,
    controllerModel,
    dataModel,
    onDataModelChange,
    transactionExDto
  };

  switch (controlType) {
    case controlTypeEnum?.DDL:
      return <DDLControl {...commonProps} />;
    
    case controlTypeEnum?.SearchAbleDDL:
      // TODO: Implement SearchableDDL
      return <DDLControl {...commonProps} />;
    
    case controlTypeEnum?.AutoComplete:
      // TODO: Implement AutoComplete
      return <TextBoxControl {...commonProps} />;
    
    case controlTypeEnum?.TextBox:
      return <TextBoxControl {...commonProps} />;
    
    case controlTypeEnum?.Memo:
      return <MemoControl {...commonProps} />;
    
    case controlTypeEnum?.Image:
    case controlTypeEnum?.ExternalImageUrl:
    case controlTypeEnum?.ImageBinary:
      return <ImageControl {...commonProps} />;
    
    case controlTypeEnum?.Date:
    case controlTypeEnum?.DateTimeDetail:
    case controlTypeEnum?.Time:
      return <DateControl {...commonProps} />;
    
    case controlTypeEnum?.File:
      return <FileControl {...commonProps} />;
    
    case controlTypeEnum?.Label:
    case controlTypeEnum?.TextDisplay:
      return <LabelControl {...commonProps} />;
    
    case controlTypeEnum?.CheckBox:
      return <CheckBoxControl {...commonProps} />;
    
    case controlTypeEnum?.Numeric:
      return <NumericControl {...commonProps} />;
    
    case controlTypeEnum?.RichText:
      // TODO: Implement RichText
      return <MemoControl {...commonProps} />;
    
    default:
      // Default to TextBox
      return <TextBoxControl {...commonProps} />;
  }
};

export default FormItemLayout;

