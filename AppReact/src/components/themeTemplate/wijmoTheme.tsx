import React, { useRef, useState, useEffect } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
// import { FlexGridSearch } from '@mescius/wijmo.react.grid.search';
// import { GroupPanel as FlexGridGroupPanel } from '@mescius/wijmo.react.grid.grouppanel';
import { DataMap } from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import { CellMaker, SparklineMarkers } from '@mescius/wijmo.grid.cellmaker';
import * as wjCore from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';

// --- Data and helpers (all in this file) ---
const products = ['Widget', 'Gadget', 'Doohickey'];
const colors = ['Black', 'White', 'Red', 'Green', 'Blue'];
const countries = [
  { id: 0, name: 'US', flag: 'us' },
  { id: 1, name: 'Germany', flag: 'de' },
  { id: 2, name: 'UK', flag: 'gb' },
  { id: 3, name: 'Japan', flag: 'jp' },
  { id: 4, name: 'Italy', flag: 'it' },
  { id: 5, name: 'Greece', flag: 'gr' }
];

function getRandomIndex(arr: any[]) {
  return Math.floor(Math.random() * arr.length);
}
function getRandomArray(len: number, maxValue: number) {
  return Array.from({ length: len }, () => Math.floor(Math.random() * maxValue));
}
function getHistoryData() {
  return getRandomArray(25, 100);
}
function getRating() {
  return Math.ceil(Math.random() * 5);
}
function getData(count: number) {
  const data = [];
  const dt = new Date();
  const year = dt.getFullYear();
  for (let i = 0; i < count; i++) {
    const date = new Date(year, i % 12, 25, i % 24, i % 60, i % 60);
    const countryIndex = getRandomIndex(countries);
    const productIndex = getRandomIndex(products);
    const colorIndex = getRandomIndex(colors);
    data.push({
      id: i,
      date: date,
      countryId: countries[countryIndex].id,
      productId: productIndex,
      colorId: colorIndex,
      price: wjCore.toFixed(Math.random() * 10000 + 5000, 2, true),
      change: wjCore.toFixed(Math.random() * 1000 - 500, 2, true),
      history: getHistoryData(),
      discount: wjCore.toFixed(Math.random() / 4, 2, true),
      rating: getRating(),
      active: i % 4 === 0,
      size: Math.floor(100 + Math.random() * 900),
      weight: Math.floor(100 + Math.random() * 900),
      quantity: Math.floor(Math.random() * 10),
      description: "Sample description"
    });
  }
  // Add some invalid data for demo
  if (data.length > 4) {
    data[1].price = -2000;
    data[2].date = new Date('1970-01-01T00:00:00');
    data[4].price = -1000;
  }
  return data;
}
function buildDataMap(items: string[]) {
  return new DataMap(items.map((v, i) => ({ key: i, value: v })), 'key', 'value');
}
const countryMap = new DataMap(countries, 'id', 'name');
const productMap = buildDataMap(products);
const colorMap = buildDataMap(colors);

// --- Cell templates ---
const countryCellTemplate = (ctx: any) => {
  const country = countryMap.getDataItem(ctx.item.countryId) || { name: '', flag: '' };
  return (
    <>
      <span className={`flag-icon flag-icon-${country.flag}`} />
      {' '}{country.name}{' '}
    </>
  );
};
const colorCellTemplate = (ctx: any) => {
  const color = (colorMap.getDataItem(ctx.item.colorId) || { value: '' }).value;
  return (
    <>
      <span className="color-tile" style={{ background: color }} />
      {' '}{color}{' '}
    </>
  );
};
const changeCellTemplate = (ctx: any) => {
  const change = ctx.item.change;
  let cssClass = '';
  let displayValue = '';
  if (wjCore.isNumber(change)) {
    if (change > 0) cssClass = 'change-up';
    else if (change < 0) cssClass = 'change-down';
    displayValue = wjCore.Globalize.formatNumber(change, 'c');
  } else if (!wjCore.isUndefined(change) && change !== null) {
    displayValue = wjCore.changeType(change, wjCore.DataType.String);
  }
  return <span className={cssClass}>{displayValue}</span>;
};
const historyCellTemplate = CellMaker.makeSparkline({
  markers: SparklineMarkers.High | SparklineMarkers.Low,
  maxPoints: 25,
  label: 'price history',
});
const ratingCellTemplate = CellMaker.makeRating({
  range: [1, 5],
  label: 'rating'
});

// --- Main component ---
const WijmoTheme: React.FC = () => {
  const [itemsCount, setItemsCount] = useState(50);
  const [data, setData] = useState(() => getData(itemsCount));
  const flex = useRef<any>(null);
  //const theSearch = useRef<any>(null);

  useEffect(() => {
    setData(getData(itemsCount));
  }, [itemsCount]);

//   useEffect(() => {
//     if (theSearch.current && flex.current) {
//       theSearch.current.control.grid = flex.current.control;
//     }
//   }, [data]);
//const currentTheme = useSelector((state: RootState) => state.theme.currentTheme);
const { theme } = useTheme();
  return (
    <div className="w-full h-full flex flex-col">
     
      <div className={`text-lg mb-4 font-semibold ${theme.title}`}>Wijmo Theme Preview</div>
       
     
      <div className="mb-4 flex gap-4 items-center">
        {/* <FlexGridSearch ref={theSearch} placeholder="Search" cssMatch="" /> */}
        <div>
          <span>Items: </span>
          <select value={itemsCount} onChange={e => setItemsCount(Number(e.target.value))}>
            <option value={5}>5</option>
            <option value={50}>50</option>
            <option value={500}>500</option>
          </select>
        </div>
      </div>
    
      <div className="w-full h-[200px] flex-auto bg-white shadow-lg overflow-hidden">
        <FlexGrid
          ref={flex}
          itemsSource={new CollectionView(data)}
          autoGenerateColumns={false}
          allowAddNew
          allowDelete
          allowPinning="SingleColumn"
          newRowAtTop
          showMarquee
          selectionMode="MultiRange"
          validateEdits={false}
          cssClass="w-full"
          style={{ height: '100%' }}
        >
          <FlexGridFilter />
          <FlexGridColumn header="ID" binding="id" width={70} isReadOnly={true} />
          <FlexGridColumn header="Date" binding="date" format="MMM d yyyy" isRequired={false} width={130} />
          <FlexGridColumn header="Country" binding="countryId" dataMap={countryMap} width={145}>
            <FlexGridCellTemplate cellType="Cell" template={countryCellTemplate} />
          </FlexGridColumn>
          <FlexGridColumn header="Price" binding="price" format="c" isRequired={false} width={100} />
          <FlexGridColumn header="History" binding="history" align="center" width={180} allowSorting={false} cellTemplate={historyCellTemplate} />
          <FlexGridColumn header="Change" binding="change" align="right" width={115}>
            <FlexGridCellTemplate cellType="Cell" template={changeCellTemplate} />
          </FlexGridColumn>
          <FlexGridColumn header="Rating" binding="rating" align="center" width={180} cssClass="cell-rating" cellTemplate={ratingCellTemplate} />
          <FlexGridColumn header="Color" binding="colorId" dataMap={colorMap} width={145}>
            <FlexGridCellTemplate cellType="Cell" template={colorCellTemplate} />
          </FlexGridColumn>
          <FlexGridColumn header="Product" binding="productId" dataMap={productMap} width={145} />
          <FlexGridColumn header="Discount" binding="discount" format="p0" width={130} />
          <FlexGridColumn header="Active" binding="active" width={100} />
        </FlexGrid>
      </div>
    </div>
  );
};

export default WijmoTheme;