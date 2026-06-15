using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the entity 'AppSearchViewField'.
    /// </summary>
   
    public partial class AppSearchViewFieldDto
    {


        //<wj-flex-grid-column is-read-only="false" binding="MappingSearchFieldId" header="Mapping Search Field" is-required="false" data-map="dataModel.mappingSearchFieldDataMap" width="180"
        //                       ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
        //  <wj-flex-grid-column is-read-only="false" binding="TreeLevel" data-type="Number" header="Tree Level" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
        //  <wj-flex-grid-column is-read-only="false" binding="IsTreeNodeId" data-type="Boolean" header="Is Tree Node Id" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
        //  <wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDisplay" data-type="Boolean" header="Is Tree Node Display" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
        //  <wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDesc" data-type="Boolean" header="Is Tree Node Desc" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
        //  <wj-flex-grid-column is-read-only="false" binding="IsTreeNodeImageUrl" data-type="Boolean" header="Is Tree Node Image Url" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>




        // Card View  EShopCardView
        [IgnoreDataMember]
        public int? OptionLevel
        {
            get
            {
                return this.TreeLevel; ;
            }
        }

        // Card View
        [IgnoreDataMember]
        public bool? IsOptionId
        {
            get
            {
                return this.IsTreeNodeId; ;
            }
        }

        // Card View
        [IgnoreDataMember]
        public bool? IsOptionDisplay
        {
            get
            {
                return this.IsTreeNodeDisplay; ;
            }
        }

        // Card View
        [IgnoreDataMember]
        public bool? IsOptionDesc
        {
            get
            {
                return this.IsTreeNodeDesc; ;
            }
        }

        // Card View
        [IgnoreDataMember]
        public bool? IsOptionImageUrl
        {
            get
            {
                return this.IsTreeNodeImageUrl; ;
            }
        }

        [IgnoreDataMember]
        public bool? IsSkuFIsEshopRootGroupield
        {
            get
            {
                return this.IsGroupBy;
            }
        }

        [IgnoreDataMember]
        public bool? IsImageColumn
        {
            get
            {
                return this.IsMapToChartX;
            }
        }

        [IgnoreDataMember]
        public bool? IsDisplayColumn
        {
            get
            {
                return this.IsMapToChartY;
            }
        }

        [IgnoreDataMember]
        public bool? IsPricecolumn
        {
            get
            {
                return this.IsFilterByCurrentUser;
            }
        }
        //end of EShopCardView

        //@*Is FOr Product DEtail View*@
        [IgnoreDataMember]
        public bool? IsSku
        {
            get
            {
                return this.IsMapToChartX;
            }
        }

        [IgnoreDataMember]
        public bool? IsDetailKey
        {
            get
            {
                return this.IsGroupBy;
            }
        }

        [IgnoreDataMember]
        public bool? IsAvailableQty
        {
            get
            {
                return this.IsUserDefined1;
            }
        }
        [IgnoreDataMember]
        public bool? IsSelectedQty
        {
            get
            {
                return this.IsUserDefined2;
            }
        }
    }
}


                //@*  is for Tree View*@
                //<wj-flex-grid-column is-read-only="false" binding="MappingSearchFieldId" header="Mapping Search Field" is-required="false" data-map="dataModel.mappingSearchFieldDataMap" width="180"
                //                     ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="TreeLevel" data-type="Number" header="Tree Level" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeId" data-type="Boolean" header="Is Tree Node Id" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDisplay" data-type="Boolean" header="Is Tree Node Display" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDesc" data-type="Boolean" header="Is Tree Node Desc" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeImageUrl" data-type="Boolean" header="Is Tree Node Image Url" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.FlatDataSetTreeView"></wj-flex-grid-column>


                //@* eshop option maxtrix *@

                //@*is For Card View*@
                //<wj-flex-grid-column is-read-only="false" binding="TreeLevel" data-type="Number" header="Option #" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeId" data-type="Boolean" header="Is Option Id" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDisplay" data-type="Boolean" header="Is Option Display" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDesc" data-type="Boolean" header="Is Option Desc" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeImageUrl" data-type="Boolean" header="Is Option Image Url" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsGroupBy" header="Is Eshop Root Group " date-type="Boolean" width="100" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"> </wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsMapToChartX" data-type="Boolean" header="Is Image Column " width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsMapToChartY" data-type="Boolean" header="Is Display Column " width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsFilterByCurrentUser" data-type="Boolean" header="Is Price column " width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopCardView"></wj-flex-grid-column>



                //@* Is FOr Product DEtail View*@

                //<wj-flex-grid-column is-read-only="false" binding="ProductDetaiMapTransFiledId" header="Mapping Trans. Field" is-required="false" data-map="dataModel.transactionFieldDataMap" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>

                //<wj-flex-grid-column is-read-only="false" binding="IsMapToChartX" data-type="Boolean" header="Is Sku" width="180" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>

                //<wj-flex-grid-column is-read-only="false" binding="TreeLevel" data-type="Number" header="Option #" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeId" data-type="Boolean" header="Is Option Id" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDisplay" data-type="Boolean" header="Is Option Display" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeDesc" data-type="Boolean" header="Is Sku Description" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsTreeNodeImageUrl" data-type="Boolean" header="Is Sku Image Url" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>

                //<wj-flex-grid-column is-read-only="false" binding="IsGroupBy" header="Is Detail Key" date-type="Boolean" width="100" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"> </wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsMapToChartY" data-type="Boolean" header="Is Display Column " width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsFilterByCurrentUser" data-type="Boolean" header="Is Price column " width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>

                //<wj-flex-grid-column is-read-only="false" binding="IsUserDefined1" data-type="Boolean" header="Is Available Qty" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>
                //<wj-flex-grid-column is-read-only="false" binding="IsUserDefined2" data-type="Boolean" header="Is Selected Qty" width="150" ng-if="dataModel.currentSearchView.ViewType == dataModel.EmAppViewType.EShopProductDetailView"></wj-flex-grid-column>
