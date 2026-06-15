import React, { useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import MasterDetailFlexLayoutForm from './MasterDetailFlexLayoutForm';
import MasterDetailGridLayoutForm from './MasterDetailGridLayoutForm';

interface TransactionCrossHeaderItemProps {
  transactionDto: any;
  uiId: string;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
}

const TransactionCrossHeaderItem: React.FC<TransactionCrossHeaderItemProps> = ({
  transactionDto,
  uiId,
  controllerModel,
  dataModel,
  onDataModelChange
}) => {
  const { theme: _theme } = useTheme();
  const [isCollapsed, setIsCollapsed] = useState(false);
  const layoutTypeEnum = useEnumValues('EmAppFormLayoutType');

  if (!transactionDto || !layoutTypeEnum) return null;

  const headerFormExDto = transactionDto.ForeignAppFormExDto;
  const layoutType = headerFormExDto?.LayoutType;
  const isFlexLayout = layoutType === layoutTypeEnum.Flex;

  const toggleCollapse = () => {
    setIsCollapsed(!isCollapsed);
  };

  const formStructureData = {
    ...transactionDto,
    ForeignAppFormExDto: headerFormExDto
  };

  return (
    <div className="w-full mb-2">
      {/* Collapse/Expand Button and Header */}
      <div
        className="cursor-pointer flex items-center gap-2 p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded"
        onClick={toggleCollapse}
      >
        Cross Header Data Model: {transactionDto.TransactionName}
        <button
          className="w-5 h-5 rounded-full border border-gray-400 dark:border-gray-600 flex items-center justify-center text-xs font-bold hover:bg-gray-200 dark:hover:bg-gray-700"
          onClick={(e) => {
            e.stopPropagation();
            toggleCollapse();
          }}
        >
          {isCollapsed ? '+' : '-'}
        </button>
        <span className="font-semibold">Cross Header Data Model: {transactionDto.TransactionName}</span>
      </div>

      {/* Cross Header Transaction Content */}
      {!isCollapsed && (
        <div className="ml-7 mt-2">
          {isFlexLayout ? (
            <MasterDetailFlexLayoutForm
              controllerModel={controllerModel}
              dataModel={dataModel}
              formStructureData={formStructureData}
              onDataModelChange={onDataModelChange}
            />
          ) : (
            <MasterDetailGridLayoutForm
              controllerModel={controllerModel}
              dataModel={dataModel}
              formStructureData={formStructureData}
              onDataModelChange={onDataModelChange}
            />
          )}
        </div>
      )}
    </div>
  );
};

export default TransactionCrossHeaderItem;

