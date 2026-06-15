import React from 'react';
import { useTheme } from '../../../../../redux/hooks/useTheme';

interface LabelControlProps {
  layoutItemExDto?: any;
  fieldDto?: any;
  controllerModel?: any;
}

const LabelControl: React.FC<LabelControlProps> = ({
  layoutItemExDto,
  fieldDto,
  controllerModel
}) => {
  const { theme } = useTheme();
  const isHideLabel = controllerModel?.isFilePropertyEdit === true;
  if (isHideLabel) return null;

  // Get label text
  const label = fieldDto?.DisplayName || 
                fieldDto?.LabelDisplayBinding || 
                layoutItemExDto?.DomAttribute?.DisplayName || 
                'Label';

  return (
    <div className="w-full">
      <label className={`text-xs font-semibold ${theme.title}`}>
        {label}
      </label>
    </div>
  );
};

export default LabelControl;

