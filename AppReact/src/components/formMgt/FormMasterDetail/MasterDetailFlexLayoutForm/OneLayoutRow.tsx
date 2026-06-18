import React from 'react';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import OneLayoutItem from './OneLayoutItem';
import {
  isLayoutRowItem,
  resolveLayoutRowType,
  shouldRenderRuntimeLayoutItem,
} from './flexLayoutItemHelper';

interface OneLayoutRowProps {
  layoutRowExDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
  /** Pass self to break circular import with OneLayoutItem (sections render rows) */
  RowComponent?: React.ComponentType<any>;
}

const OneLayoutRow: React.FC<OneLayoutRowProps> = ({
  layoutRowExDto,
  controllerModel,
  dataModel,
  onDataModelChange,
  transactionExDto,
  RowComponent
}) => {
  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');
  const layoutRowType = resolveLayoutRowType(layoutItemTypeEnum);
  const Row = RowComponent ?? OneLayoutRow;
  
  // Get child layout items, ordered by FlowOrGridLayoutSortOrder
  // Support both PascalCase (AppFormLayoutItem_List) and camelCase (appFormLayoutItem_List) from API
  const rawList = layoutRowExDto?.AppFormLayoutItem_List ?? layoutRowExDto?.appFormLayoutItem_List;
  const childItems = rawList
    ? [...(Array.isArray(rawList) ? rawList : [])]
        .filter((item: any) => shouldRenderRuntimeLayoutItem(item))
        .sort((a: any, b: any) =>
          (a.FlowOrGridLayoutSortOrder ?? a.flowOrGridLayoutSortOrder ?? 0) -
          (b.FlowOrGridLayoutSortOrder ?? b.flowOrGridLayoutSortOrder ?? 0)
        )
    : [];
  
  if (childItems.length === 0) {
    return null;
  }

  let itemRuntimeOrder = 0;

  return (
    <div className="FormLayoutRow Row24" style={{ position: 'relative' }}>
      {childItems.map((layoutItemExDto: any) => {
        itemRuntimeOrder++;
        const itemWithOrder = {
          ...layoutItemExDto,
          GrandChildEditMode: layoutRowExDto.GrandChildEditMode,
          ItemRuntimeOrder: itemRuntimeOrder
        };

        if (isLayoutRowItem(layoutItemExDto, layoutRowType)) {
          return (
            <Row
              key={layoutItemExDto.Id || layoutItemExDto.CurrentHostId || `row-${itemRuntimeOrder}`}
              layoutRowExDto={itemWithOrder}
              controllerModel={controllerModel}
              dataModel={dataModel}
              onDataModelChange={onDataModelChange}
              transactionExDto={transactionExDto}
              RowComponent={Row}
            />
          );
        }

        return (
          <OneLayoutItem
            key={layoutItemExDto.Id || layoutItemExDto.CurrentHostId || `item-${itemRuntimeOrder}`}
            layoutItemExDto={itemWithOrder}
            controllerModel={controllerModel}
            dataModel={dataModel}
            onDataModelChange={onDataModelChange}
            transactionExDto={transactionExDto}
            RowComponent={Row}
          />
        );
      })}
    </div>
  );
};


export default OneLayoutRow;

