//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using APP.LBL.EntityClasses;
////using APP.Persistence.Common;
//using APP.Components.Dto;
//using APP.Components.EntityDto;
//using APP.LBL.DatabaseSpecific;

//using APP.Framework;
//namespace App.BL
//{
//    public static class AppCatalogueForEshopTreeBL
//    {
//        public static AppCatalogueTreeDto[] RetrieveCatalogueTreeList()
//        {
//            List<AppCatalogueTreeDto> rootFolders = new List<AppCatalogueTreeDto>();

//            int? topNewProductViewId = AppTenantSettingBL.GetIntValue(EmTenantSettings.EshopNewProductTreeView);

//            var allTreeViewList = AppSearchViewConfigBL.RetrievAllTreeViewEntityList();

//            if (topNewProductViewId.HasValue)
//            {
//                allTreeViewList = allTreeViewList.Where(o => (int)o.Id != topNewProductViewId.Value).ToList();
//            }


//            foreach (var treeViewEntityDto in allTreeViewList)
//            {

//                ConvertOneTreeSearchViewToFolderTree(rootFolders, treeViewEntityDto);
//            }

//            return rootFolders.ToArray();
//        }

//        public static AppCatalogueTreeDto RetrieveNewArriveCatalogueTree()
//        {
//            List<AppCatalogueTreeDto> rootFolders = new List<AppCatalogueTreeDto>();

//            int? viewId = AppTenantSettingBL.GetIntValue(EmTenantSettings.EshopNewProductTreeView);
//            if (viewId.HasValue)
//            {
//                var treeViewEntityDto = AppSearchViewConfigBL.RetrievAllTreeViewEntityList().FirstOrDefault(o => (int)o.Id == viewId.Value);

//                ConvertOneTreeSearchViewToFolderTree(rootFolders, treeViewEntityDto);

//            }
//            if (rootFolders.Count > 0)
//            {
//                return rootFolders[0];
//            }
//            else
//            {
//                return null;
//            }


//        }



//        public static AppEshopCatalogViewDto GetOneTreeNodeCatalog(BaseAppCatalogueTreeDto appCatalogueTreeDto, bool IsWithOptionFilter)
//        {
//            AppEshopCatalogViewDto toReturn = new AppEshopCatalogViewDto();

//            SearchDto aSearchDto = SetupEshopCardListSearchDto(appCatalogueTreeDto);

//            if (aSearchDto != null)
//            {
//                AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(int.Parse(aSearchDto.ReferenceViewDefinitionDto.Id.ToString()));
//                AppSearchViewExDto aAppSearchViewExDto = AppSearchViewConfigBL.ConvertOneEntityToExDto(viewEntity);

//                DataTable dataTableResult = AppStaticDataSetSearchBL.RetriveMasterDataSetDataTable(aSearchDto, viewEntity);

//                IEnumerable<DataRow> dataTableRows = dataTableResult.AsEnumerable();

//                if (IsWithOptionFilter)
//                {
//                    Dictionary<int, List<LookupItemDto>> dictOptionCheckedLevel = appCatalogueTreeDto.DictOptionCheckedLevel;

//                    if (dictOptionCheckedLevel != null)
//                    {
//                        Dictionary<int, List<AppSearchViewFieldExDto>> dictOptionlevelFieldExdto = GetDictTreeViewLevelNodeList(aAppSearchViewExDto);

//                        foreach (int checkedlevelkey in dictOptionCheckedLevel.Keys)
//                        {
//                            string columnIdColumnName;
//                            string columnDislay;
//                            string columnImage;
//                            GetIdDisplayColumnName(dictOptionlevelFieldExdto, checkedlevelkey, out columnIdColumnName, out columnDislay, out columnImage);

//                            List<LookupItemDto> checkedLookItemList = dictOptionCheckedLevel[checkedlevelkey];

//                            if (!checkedLookItemList.IsEmpty())
//                            {
//                                //object[] includeIds = checkedLookItemList.Select(o => o.Id).ToArray();
//                                // dataTableRows = dataTableRows
//                                //    .Where(row => includeIds.Contains(row[columnIdColumnName]));

//                                string[] includeIds = checkedLookItemList.Select(o => o.Id.ToString()).ToArray();
//                                dataTableRows = dataTableRows
//                                   .Where(row => includeIds.Contains(row[columnIdColumnName].ToString()));
//                            }
//                        }
//                    }
//                }

//                // optionList
//                PorcessOptionList(toReturn, dataTableRows, aAppSearchViewExDto);
//                // process cardList
//                PorcesCardList(toReturn, dataTableRows, aAppSearchViewExDto);

//                //.Aggregate((current, next) => current + ", " + next);
//            }
//            else
//            {
//                return null;
//            }

//            return toReturn;
//        }

//        // <wj-flex-grid-column is-read-only="false" binding="TreeLevel" data-type="Number" header="Option #" width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeId" data-type="Boolean" header="Is Option Id" width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDisplay" data-type="Boolean" header="Is Option Display" width="150" visible="{{ currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDesc" data-type="Boolean" header="Is Sku Description" width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeImageUrl" data-type="Boolean" header="Is Sku Image Url" width="150" visible="{{ currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>

//        //                    @*  eshop display  *@
//        //<wj-flex-grid-column is-read-only="false" binding="IsGroupBy" header="Is Detail Key" date-type="Boolean" width="100"  visible="{{ currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"> </wj-flex-grid-column>  
//        //<wj-flex-grid-column is-read-only="false" binding="IsMapToChartX" data-type="Boolean" header="Is Sku " width="150" visible="{{ currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsMapToChartY" data-type="Boolean" header="Is Display Column " width="150" visible="{{ currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsFilterByCurrentUser" data-type="Boolean" header="Is Price column " width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>


//        //        <wj-flex-grid-column is-read-only="false" binding="IsUserDefined1" data-type="Boolean" header="Is Available Qty" width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsUserDefined2" data-type="Boolean" header="Is Selected Qty" width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopProductDetailView ? 'true':'false'}}"></wj-flex-grid-column>


//        public static AppEshopCatalogViewDto GetTop10CatalogView()
//        {
//            AppEshopCatalogViewDto toReturn = new AppEshopCatalogViewDto();

//            int? viewId = AppTenantSettingBL.GetIntValue(EmTenantSettings.EshopTopProductsSearchView);

//            //   SearchDto aSearchDto = SetupEshopCardListSearchDto(appCatalogueTreeDto);


//            AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(viewId);
//            AppSearchViewExDto aAppSearchViewExDto = AppSearchViewConfigBL.ConvertOneEntityToExDto(viewEntity);
//            DataTable dataTableResult = AppStaticDataSetSearchBL.GetMasterDatasetwDataTableFromViewEntity(viewEntity);
//            IEnumerable<DataRow> dataTableRows = dataTableResult.AsEnumerable();



//            // optionList
//            PorcessOptionList(toReturn, dataTableRows, aAppSearchViewExDto);
//            // process cardList
//            PorcesCardList(toReturn, dataTableRows, aAppSearchViewExDto);

//            //.Aggregate((current, next) => current + ", " + next);



//            return toReturn;
//        }

//        public static AppEshopCatalogCardDetailDto GetOneCardDetail(AppEshopCatalogCardDto appEshopCatalogCardDto)
//        {
//            AppEshopCatalogCardDetailDto aAppEshopCatalogCardDetailDto = new AppEshopCatalogCardDetailDto();

//            SearchDto aSearchDto = SetupEshopCardDetailSearchDto(appEshopCatalogCardDto);

//            AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(int.Parse(aSearchDto.ReferenceViewDefinitionDto.Id.ToString()));
//            AppSearchViewExDto aAppSearchViewExDto = AppSearchViewConfigBL.ConvertOneEntityToExDto(viewEntity);

//            DataTable dataTableResult = AppStaticDataSetSearchBL.RetriveMasterDataSetDataTable(aSearchDto, viewEntity);

//            IEnumerable<DataRow> dataTableRows = dataTableResult.AsEnumerable();


//            var dictOptionLevel = new Dictionary<int, List<LookupItemDto>>();
//            var dictOptionDisplay = new Dictionary<int, string>();
//            GetDictOptionLevelIdAndDisplayAndLabel(dataTableRows, aAppSearchViewExDto, dictOptionLevel, dictOptionDisplay);
//            aAppEshopCatalogCardDetailDto.DictOptionLevelLookup = dictOptionLevel;
//            aAppEshopCatalogCardDetailDto.DictOptionLable = dictOptionDisplay;
//            //aAppEshopCatalogCardDetailDto.DictOptionAndDto = new Dictionary<int, AppEshopCatalogLevelOptionDto>();

//            // Image and Display
//            // AppSearchViewFieldExDto ImageNode = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartX.HasValue && o.IsMapToChartX.Value).FirstOrDefault();
//            List<AppSearchViewFieldExDto> DisplayColumnList = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartY.HasValue && o.IsMapToChartY.Value).OrderBy(o => o.Sort).ToList();



//            aAppEshopCatalogCardDetailDto.ProductDisplay = new Dictionary<string, List<string>>();

//            aAppEshopCatalogCardDetailDto.ImageUrl = new List<string>();


//            List<DataRow> rowList = dataTableRows.ToList();

//            foreach (AppSearchViewFieldExDto DisplayNode in DisplayColumnList)
//            {
//                // string dislplay = rowList.Select(o => ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(o[DisplayNode.SysTableFiledPath])).Distinct().FirstOrDefault();
//                //  aAppEshopCatalogCardDetailDto.Display.Add(  dislplay);
//                List<string> displayList = new List<string>();
//                foreach (DataRow row in rowList)
//                {
//                    displayList.Add(ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row[DisplayNode.SysTableFiledPath]));
//                }

//                aAppEshopCatalogCardDetailDto.ProductDisplay.Add(DisplayNode.DisplayText, displayList.Distinct().ToList());
//            }



//            // matrix key and logical 

//            Dictionary<int, List<AppSearchViewFieldExDto>> dictOptionLevelSearchField = GetDictTreeViewLevelNodeList(aAppSearchViewExDto);
//            Dictionary<int, AppSearchViewFieldExDto> dictOptionLevelTreeNodeId = new Dictionary<int, AppSearchViewFieldExDto>();
//            Dictionary<int, AppSearchViewFieldExDto> dictOptionLevelTreeNodeDisplay = new Dictionary<int, AppSearchViewFieldExDto>();

//            foreach (int levelKey in dictOptionLevelSearchField.Keys)
//            {

//                AppSearchViewFieldExDto earchViewFieldExDto = dictOptionLevelSearchField[levelKey].FirstOrDefault(o => o.IsTreeNodeId.HasValue && o.IsTreeNodeId.Value);
//                dictOptionLevelTreeNodeId.Add(levelKey, earchViewFieldExDto);

//                AppSearchViewFieldExDto aAppSearchViewFieldExDto = dictOptionLevelSearchField[levelKey].FirstOrDefault(o => o.IsTreeNodeDisplay.HasValue && o.IsTreeNodeDisplay.Value);
//                dictOptionLevelTreeNodeDisplay.Add(levelKey, aAppSearchViewFieldExDto);

//            }


//            AppSearchViewFieldExDto detailKeyField = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value).FirstOrDefault();
//            AppSearchViewFieldExDto skuSearchFieldExDto = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartX.HasValue && o.IsMapToChartX.Value).FirstOrDefault();
//            AppSearchViewFieldExDto priceSearchFieldExDto = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsFilterByCurrentUser.HasValue && o.IsFilterByCurrentUser.Value).FirstOrDefault();
//            AppSearchViewFieldExDto availableQtyFieldExDto = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsUserDefined1.HasValue && o.IsUserDefined1.Value).FirstOrDefault();

//            AppSearchViewFieldExDto selectQtyFieldExDto = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsUserDefined2.HasValue && o.IsUserDefined2.Value).FirstOrDefault();


//            List<AppSearchViewFieldExDto> skuDisplayFieldList = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsTreeNodeDesc.HasValue && o.IsTreeNodeDesc.Value).ToList();

//            List<AppSearchViewFieldExDto> skuImageFiedList = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsTreeNodeImageUrl.HasValue && o.IsTreeNodeImageUrl.Value).ToList();




//            Dictionary<string, string> DictMatrixConbineKeySku = new Dictionary<string, string>();
//            Dictionary<string, string> DictSkuMatrixDisplay = new Dictionary<string, string>();

//            Dictionary<string, decimal> DictSkuPrice = new Dictionary<string, decimal>();
//            Dictionary<string, string> DictSkuDetailId = new Dictionary<string, string>();

//            Dictionary<string, Dictionary<string, string>> DictSkuDescription = new Dictionary<string, Dictionary<string, string>>();
//            Dictionary<string, Dictionary<string, string>> DictSkuImageUrl = new Dictionary<string, Dictionary<string, string>>();
//            Dictionary<string, AppEshopBagItemDto> DictSkuAppEshopBagItem = new Dictionary<string, AppEshopBagItemDto>();
//            Dictionary<string, DataRow> DictSkuDataRow = new Dictionary<string, DataRow>();



//            foreach (DataRow dataRow in rowList)
//            {
//                string combineKey = string.Empty;
//                foreach (int levelKey in dictOptionLevelTreeNodeId.Keys)
//                {
//                    string systempath = dictOptionLevelTreeNodeId[levelKey].SysTableFiledPath;
//                    combineKey = combineKey + dataRow[systempath].ToString() + "_";


//                }

//                if (combineKey != string.Empty)
//                {
//                    combineKey = combineKey.Substring(0, combineKey.Length - 1);
//                }



//                string skuValue = dataRow[skuSearchFieldExDto.SysTableFiledPath].ToString();
//                string detailID = dataRow[detailKeyField.SysTableFiledPath].ToString();

//                // fro bagitem display 
//                string combineDisplay = string.Empty;

//                foreach (int levelKey in dictOptionLevelTreeNodeDisplay.Keys)
//                {
//                    string systempath = dictOptionLevelTreeNodeDisplay[levelKey].SysTableFiledPath;
//                    combineDisplay = combineDisplay + dataRow[systempath].ToString() + ",";


//                }

//                if (combineDisplay != string.Empty)
//                {
//                    combineDisplay = combineDisplay.Substring(0, combineDisplay.Length - 1);
//                    combineDisplay = "(" + combineDisplay + ")";

//                }

//                DictSkuMatrixDisplay.Add(skuValue, combineDisplay);




//                decimal price = ControlTypeValueConverter.ConvertValueToDecimalWithDefautZero(dataRow[priceSearchFieldExDto.SysTableFiledPath]);

//                DictMatrixConbineKeySku.Add(combineKey, skuValue);
//                DictSkuPrice.Add(skuValue, price);
//                DictSkuDetailId.Add(skuValue, detailID);

//                Dictionary<string, string> dictDisplay = new Dictionary<string, string>();
//                foreach (AppSearchViewFieldExDto displayFiled in skuDisplayFieldList)
//                {
//                    object displayValue = dataRow[displayFiled.SysTableFiledPath];
//                    dictDisplay.Add(displayFiled.DisplayText, ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(displayValue));

//                }

//                DictSkuDescription.Add(skuValue, dictDisplay);


//                Dictionary<string, string> dictImage = new Dictionary<string, string>();
//                foreach (AppSearchViewFieldExDto disimageFiled in skuImageFiedList)
//                {
//                    object iamgeValue = dataRow[disimageFiled.SysTableFiledPath];
//                    dictImage.Add(disimageFiled.DisplayText, ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(iamgeValue));

//                }




//                DictSkuImageUrl.Add(skuValue, dictImage);
//                DictSkuDataRow.Add(skuValue, dataRow);

//            }


//            aAppEshopCatalogCardDetailDto.DictMatrixKeySku = DictMatrixConbineKeySku;
//            aAppEshopCatalogCardDetailDto.DictSkuPrice = DictSkuPrice;
//            aAppEshopCatalogCardDetailDto.DictSkuDetailId = DictSkuDetailId;
//            aAppEshopCatalogCardDetailDto.DictSkuDescription = DictSkuDescription;
//            aAppEshopCatalogCardDetailDto.DictSkuImageUrl = DictSkuImageUrl;
//            //DictSkuAppEshopBagItem
//            foreach (string sku in DictSkuDetailId.Keys)
//            {
//                AppEshopBagItemDto aAppEshopBagItemDto = new AppEshopBagItemDto();
//                aAppEshopBagItemDto.SkuNo = sku;
//                aAppEshopBagItemDto.DetailId = DictSkuDetailId[sku];
//                aAppEshopBagItemDto.ImageUrl = DictSkuImageUrl[sku].Values.FirstOrDefault();
//                aAppEshopBagItemDto.Price = DictSkuPrice[sku];


//                List<string> allDisplay = new List<string>();

//                foreach (List<string> listdisplay in aAppEshopCatalogCardDetailDto.ProductDisplay.Values)
//                {
//                    allDisplay.AddRange(listdisplay);
//                }



//                allDisplay = allDisplay.Select(o => o.Trim()).Distinct().ToList();

//                aAppEshopBagItemDto.ProductDisplay = allDisplay.Aggregate((o, next) => o + "," + next);

//                aAppEshopBagItemDto.DictMaxtrixDisplay = DictSkuMatrixDisplay[sku];

//                aAppEshopBagItemDto.DictSkuDisplay = DictSkuDescription[sku];

//                //  aAppEshopBagItemDto.Price = DictSkuPrice[sku];
//                //  aAppEshopBagItemDto.Quantity = 0;
//                //  aAppEshopBagItemDto.Weight = 0;

//                aAppEshopBagItemDto.SkuFieldName = skuSearchFieldExDto.SysTableFiledPath;
//                aAppEshopBagItemDto.PriceFieldName = priceSearchFieldExDto.SysTableFiledPath;
//                //aAppEshopBagItemDto.SelectedQtyFieldName = selectQtyFieldExDto.SysTableFiledPath;

//                aAppEshopBagItemDto.ProductDetaiViewMapUnitId = aAppSearchViewExDto.ProductDetaiViewMapUnitId;
//                aAppEshopBagItemDto.TransactionId = aAppSearchViewExDto.TransactionId;

//                aAppEshopBagItemDto.SearchViewId = ControlTypeValueConverter.ConvertValueToInt(aAppSearchViewExDto.Id);

//                //     aAppEshopBagItemDto.QtyFieldName = 

//                var dataRow = DictSkuDataRow[sku];
//                aAppEshopBagItemDto.DictOneToOneFields = new Dictionary<string, object>();

//                foreach (var column in aAppSearchViewExDto.AppSearchViewFieldList)
//                {

//                    aAppEshopBagItemDto.DictOneToOneFields.Add(column.SysTableFiledPath, dataRow[column.SysTableFiledPath]);
//                }

//                DictSkuAppEshopBagItem.Add(sku, aAppEshopBagItemDto);

//            }


//            aAppEshopCatalogCardDetailDto.DictSkuAppEshopBagItem = DictSkuAppEshopBagItem;



//            return aAppEshopCatalogCardDetailDto;

//            //throw new NotImplementedException();
//        }

//        //<wj-flex-grid-column is-read-only="false" binding="IsGroupBy" header="Is Eshop Root Group " date-type="Boolean" width="100"  visible="{{currentSearchView.ViewType == EmAppViewType.EShopView? 'true':'false'}}"> </wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsMapToChartX" data-type="Boolean" header="Is Image Column " width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopView? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsMapToChartY" data-type="Boolean" header="Is Display Column " width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopView? 'true':'false'}}"></wj-flex-grid-column>
//        //<wj-flex-grid-column is-read-only="false" binding="IsFilterByCurrentUser" data-type="Boolean" header="Is Price column " width="150" visible="{{currentSearchView.ViewType == EmAppViewType.EShopView? 'true':'false'}}"></wj-flex-grid-column>


//        private static void ConvertOneTreeSearchViewToFolderTree(List<AppCatalogueTreeDto> rootFolders, AppSearchViewExDto treeViewEntityDto)
//        {
//            AppCatalogueTreeDto appCatalogueTreeDto = new AppCatalogueTreeDto();

//            appCatalogueTreeDto.Id = appCatalogueTreeDto.TreeViewEntityId = treeViewEntityDto.Id;

//            appCatalogueTreeDto.Name = treeViewEntityDto.Name;
//            appCatalogueTreeDto.UiId = Guid.NewGuid().ToString();
//            rootFolders.Add(appCatalogueTreeDto);

//            AppDataSetExDto aAppDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(treeViewEntityDto.DataSetId);

//            //DataTable dataTable = new DBInteractionBase().RetriveDataTable(aAppDataSetExDto.QueryText);

//            DataTable dataTable = new DataTable();
//            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
//            {
//                dataTable = adpater.ExecuteDataTableRetrievalQuery(aAppDataSetExDto.QueryText, new List<System.Data.SqlClient.SqlParameter>());

//            }



//            var dictLevelTree = GetDictTreeViewLevelNodeList(treeViewEntityDto);

//            List<string> levelNodeIdColumnName = new List<string>();
//            foreach (int levelKey in dictLevelTree.Keys)
//            {
//                string columnIdColumnName;
//                string columnDislay;
//                string columnImage;
//                GetIdDisplayColumnName(dictLevelTree, levelKey, out columnIdColumnName, out columnDislay, out columnImage);

//                levelNodeIdColumnName.Add(columnIdColumnName);

//                // root level from 1
//                if (levelNodeIdColumnName.Count == 1)
//                {
//                    FirstlevelTree(appCatalogueTreeDto, dataTable, columnIdColumnName, columnDislay, columnImage);
//                }
//                else if (levelNodeIdColumnName.Count == 2)
//                {
//                    SecondLevelTree(appCatalogueTreeDto, dataTable, levelNodeIdColumnName, columnIdColumnName, columnDislay, columnImage);
//                }
//                else if (levelNodeIdColumnName.Count == 3)
//                {
//                    ThirdLevelTree(appCatalogueTreeDto, dataTable, levelNodeIdColumnName, columnIdColumnName, columnDislay, columnImage);
//                }
//                else if (levelNodeIdColumnName.Count == 4)
//                {
//                    FourthLevelTree(appCatalogueTreeDto, dataTable, levelNodeIdColumnName, columnIdColumnName, columnDislay, columnImage);
//                }
//                else if (levelNodeIdColumnName.Count == 5)
//                {
//                    FivelevelTree(appCatalogueTreeDto, dataTable, levelNodeIdColumnName, columnIdColumnName, columnDislay, columnImage);
//                }
//            }
//        }


//        private static Dictionary<int, List<AppSearchViewFieldExDto>> GetDictTreeViewLevelNodeList(AppSearchViewExDto aAppSearchViewExDto)
//        {
//            var dictLevelTree = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.TreeLevel.HasValue)
//                 .OrderBy(o => o.TreeLevel)
//                 .GroupBy(o => o.TreeLevel)
//                 .ToDictionary(o => o.Key.Value, g => g.ToList());
//            return dictLevelTree;
//        }

//        private static void PorcesCardList(AppEshopCatalogViewDto toReturn, IEnumerable<DataRow> dataTableRows, AppSearchViewExDto aAppSearchViewExDto)
//        {
//            toReturn.CardList = new List<AppEshopCatalogCardDto>();

//            var cardGroupRootNode = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value).FirstOrDefault();
//            var ImageNode = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartX.HasValue && o.IsMapToChartX.Value).FirstOrDefault();
//            var DisplayColumnList = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartY.HasValue && o.IsMapToChartY.Value).OrderBy(o => o.Sort).ToList();
//            var PricesNode = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsFilterByCurrentUser.HasValue && o.IsFilterByCurrentUser.Value).FirstOrDefault();

//            if (cardGroupRootNode != null)
//            {
//                var groupList = dataTableRows.GroupBy(o => o[cardGroupRootNode.SysTableFiledPath]);

//                foreach (var group in groupList)
//                {
//                    AppEshopCatalogCardDto card1 = new AppEshopCatalogCardDto();

//                    card1.CardDetailSearchId = aAppSearchViewExDto.CatalogueSearchId;
//                    var rootKeyMappingSearchFiled = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.MappingSearchFieldId.HasValue).FirstOrDefault();
//                    if (rootKeyMappingSearchFiled != null)
//                    {
//                        card1.GroupRootKeyMappingSearchFiled = rootKeyMappingSearchFiled.MappingSearchFieldId;
//                    }
//                    card1.Display = new List<string>();

//                    card1.GroupKey = group.Key;

//                    List<DataRow> rowList = group.ToList();
//                    if (rowList.Count > 0)
//                    {
//                        if (ImageNode != null)
//                        {
//                            card1.ImageUrl = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rowList[0][ImageNode.SysTableFiledPath]);
//                        }

//                        card1.Price = ControlTypeValueConverter.ConvertValueToDecimal(rowList[0][PricesNode.SysTableFiledPath]);
//                        // card1.SkuNo = "S0000001";

//                        foreach (var DisplayNode in DisplayColumnList)
//                        {
//                            string dislplay = rowList.Select(o => ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(o[DisplayNode.SysTableFiledPath])).Distinct().FirstOrDefault();
//                            card1.Display.Add(dislplay);
//                        }

//                        toReturn.CardList.Add(card1);

//                        //Aggregate((current, next) => current + ", " + next);
//                    }
//                }
//            }
//        }


//        private static void PorcessOptionList(AppEshopCatalogViewDto toReturn, IEnumerable<DataRow> dataTableRows, AppSearchViewExDto aAppSearchViewExDto)
//        {
//            //   toReturn.DictOptionLevel = new Dictionary<int, List<LookupItemDto>>();
//            //   toReturn.DictOptionDisplay = new Dictionary<int, string>();


//            var dictOptionLevel = new Dictionary<int, List<LookupItemDto>>();
//            var dictOptionDisplay = new Dictionary<int, string>();


//            GetDictOptionLevelIdAndDisplayAndLabel(dataTableRows, aAppSearchViewExDto, dictOptionLevel, dictOptionDisplay);


//            toReturn.DictOptionLevel = dictOptionLevel;
//            toReturn.DictOptionDisplay = dictOptionDisplay;


//        }

//        private static void GetDictOptionLevelIdAndDisplayAndLabel(IEnumerable<DataRow> dataTableRows, AppSearchViewExDto aAppSearchViewExDto, Dictionary<int, List<LookupItemDto>> dictOptionLevel, Dictionary<int, string> dictOptionDisplay)
//        {
//            Dictionary<int, List<AppSearchViewFieldExDto>> dictOptionLevelSearchField = GetDictTreeViewLevelNodeList(aAppSearchViewExDto);

//            foreach (int noOfLevel in dictOptionLevelSearchField.Keys)
//            {
//                string columnIdColumnName;
//                string columnDislay;
//                string columnImage;
//                GetIdDisplayColumnName(dictOptionLevelSearchField, noOfLevel, out columnIdColumnName, out columnDislay, out columnImage);

//                //  List<LookupItemDto> levelNodeLookup = dataTableRows.Select(row => new LookupItemDto { Id = row[columnIdColumnName], Display = row[columnDislay] as string }).Distinct().ToList();

//                var levelNodeLookup = dataTableRows.Select(row => new
//                {
//                    Id = row[columnIdColumnName],
//                    Display = row[columnDislay],
//                })
//                 .Distinct().Select(o => new LookupItemDto { Id = o.Id, Display = o.Display as string }).ToList()
//                 ;

//                dictOptionLevel.Add(noOfLevel, levelNodeLookup);

//                // Display label
//                var optioncolumnList = dictOptionLevelSearchField[noOfLevel];
//                var ColumnDisplayDto = optioncolumnList.FirstOrDefault(o => o.IsTreeNodeDisplay.HasValue && o.IsTreeNodeDisplay.Value);
//                if (ColumnDisplayDto != null)
//                {
//                    dictOptionDisplay.Add(noOfLevel, ColumnDisplayDto.DisplayText);
//                }
//                else
//                {
//                    dictOptionDisplay.Add(noOfLevel, "");
//                }
//            }
//        }



//        private static SearchDto SetupEshopCardDetailSearchDto(AppEshopCatalogCardDto appEshopCatalogCardDto)
//        {
//            object searchId = appEshopCatalogCardDto.CardDetailSearchId;
//            object groupKey = appEshopCatalogCardDto.GroupKey;

//            SearchDto aSearchDto = null;

//            if (searchId != null)
//            {
//                aSearchDto = AppSearchBL.RetrieveOneSearchDto(int.Parse(searchId.ToString()), false, false);

//                ReferenceViewDefinitionDto referenceViewDefinitionDto = new ReferenceViewDefinitionDto();
//                referenceViewDefinitionDto.IsMassUpdate = false;
//                referenceViewDefinitionDto.Id = aSearchDto.DefaultView.Id;

//                aSearchDto.ReferenceViewDefinitionDto = referenceViewDefinitionDto;

//                // default take frist one
//                SearchCriteriaDto aCriteriaDto = aSearchDto.Criterias.FirstOrDefault();

//                if (appEshopCatalogCardDto.GroupRootKeyMappingSearchFiled != null)
//                {
//                    aCriteriaDto = aSearchDto.Criterias.FirstOrDefault(o => o.SearcDCUID.ToString() == appEshopCatalogCardDto.GroupRootKeyMappingSearchFiled.ToString());
//                }

//                if (aCriteriaDto != null)
//                {
//                    aCriteriaDto.Values.Add(groupKey);

//                    if (aCriteriaDto.CriteriaOperator == null)
//                    {
//                        aCriteriaDto.CriteriaOperator = new CriteriaOperatorDto()
//                        {
//                        };
//                    }

//                    aCriteriaDto.CriteriaOperator.OperatorType = EmAppCriteriaOperatorType.Equals;
//                }
//            }
//            return aSearchDto;
//        }

//        private static SearchDto SetupEshopCardListSearchDto(BaseAppCatalogueTreeDto appCatalogueTreeDto)
//        {
//            object treeviewID = appCatalogueTreeDto.TreeViewEntityId;
//            string branchPath = appCatalogueTreeDto.BranchPath;
//            int treelevel = appCatalogueTreeDto.TreeLevel;

//            AppSearchViewExDto aAppSearchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(treeviewID);
//            int? searchId = aAppSearchViewExDto.CatalogueSearchId;

//            SearchDto aSearchDto = null;

//            if (searchId.HasValue && (!branchPath.IsEmpty()))
//            {
//                aSearchDto = AppSearchBL.RetrieveOneSearchDto(searchId.Value, false, false);

//                ReferenceViewDefinitionDto referenceViewDefinitionDto = new ReferenceViewDefinitionDto();
//                referenceViewDefinitionDto.IsMassUpdate = false;
//                referenceViewDefinitionDto.Id = aSearchDto.DefaultView.Id;

//                aSearchDto.ReferenceViewDefinitionDto = referenceViewDefinitionDto;

//                Dictionary<int, List<AppSearchViewFieldExDto>> dictLevelTree = GetDictTreeViewLevelNodeList(aAppSearchViewExDto);

//                string[] levelValue = branchPath.Split("|".ToArray());
//                // need to reset all SearchCriteriaDto Dto value
//                foreach (SearchCriteriaDto aCriteriaDto in aSearchDto.Criterias)
//                {
//                    aCriteriaDto.Value = null;
//                    aCriteriaDto.Values.Clear();
//                }

//                for (int i = 1; i <= treelevel; i++)
//                {
//                    SetupTreeNodeValueToSearchDto(i, aSearchDto, dictLevelTree, levelValue[i - 1]);
//                }
//            }
//            return aSearchDto;
//        }

//        private static void SetupTreeNodeValueToSearchDto(int treelevel, SearchDto aSearchDto, Dictionary<int, List<AppSearchViewFieldExDto>> dictLevelTree, string levelValue)
//        {
//            List<AppSearchViewFieldExDto> levelViewFiedList = dictLevelTree[treelevel];

//            AppSearchViewFieldExDto aIdViewFieldExDto = levelViewFiedList.FirstOrDefault(o => o.IsTreeNodeId.HasValue && o.IsTreeNodeId.Value);
//            if (aIdViewFieldExDto != null)
//            {
//                if (aIdViewFieldExDto.MappingSearchFieldId.HasValue)
//                {
//                    foreach (SearchCriteriaDto aCriteriaDto in aSearchDto.Criterias)
//                    {
//                        //  aDto.SearcDCUID = searchField.SearchFielDid;
//                        if (aCriteriaDto.SearcDCUID == aIdViewFieldExDto.MappingSearchFieldId)
//                        {
//                            aCriteriaDto.Values.Add(levelValue);

//                            if (aCriteriaDto.CriteriaOperator == null)
//                            {
//                                aCriteriaDto.CriteriaOperator = new CriteriaOperatorDto()
//                                {
//                                };
//                            }

//                            aCriteriaDto.CriteriaOperator.OperatorType = EmAppCriteriaOperatorType.Equals;
//                        }
//                    }
//                }
//            }
//        }

//        private static void FirstlevelTree(AppCatalogueTreeDto oneTreeRootFolder, DataTable dataTable, string columnIdColumnName, string columnDislay, string columnImage)
//        {
//            EnumerableRowCollection<DataRow> rowsList = dataTable.AsEnumerable();
//            List<AppCatalogueTreeDto> firstlevelDtoList = ConvertDataRowToFolderDto(columnIdColumnName, columnDislay, columnImage, rowsList);
//            oneTreeRootFolder.Children = firstlevelDtoList.ToArray();


//            foreach (var node in firstlevelDtoList)
//            {
//                node.TreeViewEntityId = oneTreeRootFolder.Id;
//                node.BranchPath = string.Format("{0}", node.Id);
//                node.TreeLevel = 1;
//                node.ParentId = oneTreeRootFolder.UiId;
//                node.UiId = Guid.NewGuid().ToString();
//            }
//        }

//        private static void SecondLevelTree(AppCatalogueTreeDto oneTreeRootFolder, DataTable dataTable, List<string> levelNodeIdColumnName, string columnIdColumnName, string columnDislay, string columnImage)
//        {
//            foreach (AppCatalogueTreeDto firstlevelDto in oneTreeRootFolder.Children)
//            {
//                // Select("Size >= 230 AND Sex = 'm'"
//                var secondLevelRowList = dataTable.Select(levelNodeIdColumnName[0] + "=" + firstlevelDto.Id);
//                List<AppCatalogueTreeDto> secondlevelDtoList = ConvertDataRowToFolderDto(columnIdColumnName, columnDislay, columnImage, secondLevelRowList);
//                firstlevelDto.Children = secondlevelDtoList.ToArray();

//                foreach (var node in secondlevelDtoList)
//                {
//                    node.TreeViewEntityId = oneTreeRootFolder.Id;
//                    node.BranchPath = string.Format("{0}|{1}", firstlevelDto.Id, node.Id);
//                    node.TreeLevel = 2;
//                    node.ParentId = firstlevelDto.UiId;
//                    node.UiId = Guid.NewGuid().ToString();
//                }
//            }
//        }

//        private static void ThirdLevelTree(AppCatalogueTreeDto oneTreeRootFolder, DataTable dataTable, List<string> levelNodeIdColumnName, string columnIdColumnName, string columnDislay, string columnImage)
//        {
//            foreach (AppCatalogueTreeDto firstlevelDto in oneTreeRootFolder.Children)
//            {
//                // Select("Size >= 230 AND Sex = 'm'"

//                foreach (AppCatalogueTreeDto secondlevelDto in firstlevelDto.Children)
//                {
//                    var thirdLevelRowList = dataTable.Select(levelNodeIdColumnName[0] + "=" + firstlevelDto.Id + " and " + levelNodeIdColumnName[1] + "=" + secondlevelDto.Id);
//                    List<AppCatalogueTreeDto> thirdlevelDtoList = ConvertDataRowToFolderDto(columnIdColumnName, columnDislay, columnImage, thirdLevelRowList);
//                    secondlevelDto.Children = thirdlevelDtoList.ToArray();

//                    foreach (var node in thirdlevelDtoList)
//                    {
//                        node.TreeViewEntityId = oneTreeRootFolder.Id;
//                        node.BranchPath = string.Format("{0}|{1}|{2}", firstlevelDto.Id, secondlevelDto.Id, node.Id);
//                        node.TreeLevel = 3;
//                        node.ParentId = secondlevelDto.UiId;
//                        node.UiId = Guid.NewGuid().ToString();
//                    }
//                }
//            }
//        }

//        private static void FourthLevelTree(AppCatalogueTreeDto oneTreeRootFolder, DataTable dataTable, List<string> levelNodeIdColumnName, string columnIdColumnName, string columnDislay, string columnImage)
//        {
//            foreach (AppCatalogueTreeDto firstlevelDto in oneTreeRootFolder.Children)
//            {
//                // Select("Size >= 230 AND Sex = 'm'"

//                foreach (AppCatalogueTreeDto secondlevelDto in firstlevelDto.Children)
//                {
//                    foreach (AppCatalogueTreeDto thirdlevelDto in secondlevelDto.Children)
//                    {
//                        var foruthLevelRowList = dataTable.Select(levelNodeIdColumnName[0] + "=" + firstlevelDto.Id + " and " + levelNodeIdColumnName[1] + "=" + secondlevelDto.Id + " and " + levelNodeIdColumnName[2] + "=" + thirdlevelDto.Id);
//                        List<AppCatalogueTreeDto> foruthlevelDtoList = ConvertDataRowToFolderDto(columnIdColumnName, columnDislay, columnImage, foruthLevelRowList);
//                        thirdlevelDto.Children = foruthlevelDtoList.ToArray();

//                        foreach (var node in foruthlevelDtoList)
//                        {
//                            node.TreeViewEntityId = oneTreeRootFolder.Id;
//                            node.BranchPath = string.Format("{0}|{1}|{2}|{3}", firstlevelDto.Id, secondlevelDto.Id, thirdlevelDto.Id, node.Id);
//                            node.TreeLevel = 4;
//                            node.ParentId = thirdlevelDto.UiId;
//                            node.UiId = Guid.NewGuid().ToString();
//                        }
//                    }
//                }
//            }
//        }

//        private static void FivelevelTree(AppCatalogueTreeDto oneTreeRootFolder, DataTable dataTable, List<string> levelNodeIdColumnName, string columnIdColumnName, string columnDislay, string columnImage)
//        {
//            foreach (AppCatalogueTreeDto firstlevelDto in oneTreeRootFolder.Children)
//            {
//                // Select("Size >= 230 AND Sex = 'm'"

//                foreach (AppCatalogueTreeDto secondlevelDto in firstlevelDto.Children)
//                {
//                    foreach (AppCatalogueTreeDto thirdlevelDto in secondlevelDto.Children)
//                    {
//                        foreach (AppCatalogueTreeDto forthlevelDto in thirdlevelDto.Children)
//                        {
//                            var fiveLevelRowList = dataTable.Select(
//                                levelNodeIdColumnName[0] + "=" + firstlevelDto.Id
//                                + " and " + levelNodeIdColumnName[1] + "=" + secondlevelDto.Id
//                                + " and " + levelNodeIdColumnName[2] + "=" + thirdlevelDto.Id
//                                + " and " + levelNodeIdColumnName[3] + "=" + forthlevelDto.Id
//                                );
//                            List<AppCatalogueTreeDto> fivelevelDtoList = ConvertDataRowToFolderDto(columnIdColumnName, columnDislay, columnImage, fiveLevelRowList);
//                            forthlevelDto.Children = fivelevelDtoList.ToArray();

//                            foreach (var node in fivelevelDtoList)
//                            {
//                                node.BranchPath = string.Format("{0}|{1}|{2}|{3}|{4}", firstlevelDto.Id, secondlevelDto.Id, thirdlevelDto.Id, forthlevelDto.Id, node.Id);
//                                node.TreeViewEntityId = oneTreeRootFolder.Id;
//                                node.TreeLevel = 5;
//                                node.ParentId = forthlevelDto.UiId;
//                                node.UiId = Guid.NewGuid().ToString();
//                            }
//                        }
//                    }
//                }
//            }
//        }

//        private static List<AppCatalogueTreeDto> ConvertDataRowToFolderDto(string columnIdName, string columnDisplay, string columnImage, IEnumerable<DataRow> rowsList)
//        {
//            var list = rowsList.Select(row => new
//            {
//                Id = row[columnIdName],
//                Display = !string.IsNullOrWhiteSpace(columnDisplay) ? row[columnDisplay] : null,
//                ImageId = !string.IsNullOrWhiteSpace(columnImage) ? row[columnImage] : null,

//            })
//            .Distinct();

//            List<AppCatalogueTreeDto> firstlevelList = new List<AppCatalogueTreeDto>();
//            foreach (var idDispaly in list)
//            {
//                AppCatalogueTreeDto firstlevelTreeNode = new AppCatalogueTreeDto();

//                firstlevelTreeNode.Id = idDispaly.Id;
//                firstlevelTreeNode.Name = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(idDispaly.Display);
//                firstlevelTreeNode.ImageId = ControlTypeValueConverter.ConvertValueToInt(idDispaly.ImageId);
//                firstlevelTreeNode.TreeLevel = 1;
//                firstlevelTreeNode.BranchPath = firstlevelTreeNode.Id.ToString();
//                firstlevelList.Add(firstlevelTreeNode);
//            }
//            return firstlevelList;
//        }

//        private static void GetIdDisplayColumnName(Dictionary<int, List<AppSearchViewFieldExDto>> dictLevelTree, int levelKey, out string columnIdName, out string columnDisplay, out string columnImage)
//        {
//            var columnList = dictLevelTree[levelKey];

//            var ColumnDisplayDto = columnList.FirstOrDefault(o => o.IsTreeNodeDisplay.HasValue && o.IsTreeNodeDisplay.Value);
//            var ColumnIdDto = columnList.FirstOrDefault(o => o.IsTreeNodeId.HasValue && o.IsTreeNodeId.Value);
//            var ColumnImageIdDto = columnList.FirstOrDefault(o => o.IsTreeNodeImageUrl.HasValue && o.IsTreeNodeImageUrl.Value);

//            if (ColumnIdDto != null)
//            {
//                columnIdName = ColumnIdDto.SysTableFiledPath;
//            }
//            else
//            {
//                columnIdName = string.Empty;
//            }

//            if (ColumnDisplayDto != null)
//            {
//                columnDisplay = ColumnDisplayDto.SysTableFiledPath;
//            }
//            else
//            {
//                columnDisplay = string.Empty;
//            }

//            if (ColumnImageIdDto != null)
//            {
//                columnImage = ColumnImageIdDto.SysTableFiledPath;
//            }
//            else
//            {
//                columnImage = string.Empty;
//            }
//        }
//    }
//}