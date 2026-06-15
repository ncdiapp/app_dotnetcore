using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the  Extend Relation Entity 'AppEstore'.
    /// </summary>

    //[DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEstoreExDto
    {

        


 //       [TreeNavigationViewID]
 //       [int] NULL,
	//[CatalogCardViewID]
 //       [int] NULL,
	//[CatalogCardDetailID]
 //       [int] NU

        [IgnoreDataMember]
        public AppSearchViewExDto NavigationTreeSearchViewExDto
        {
            get; set;
        }

        [IgnoreDataMember]
        public AppSearchViewExDto CatalogCardViewExDto
        {
            get; set;
        }


        [IgnoreDataMember]
        public AppSearchViewExDto CatalogCardDetailExDto
        {
            get; set;
        }

       
        // aAppEstoreExDto

        //aAppEstoreExDto.AppEstoreProductFieldList
        //var cardGroupRootNode = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value).FirstOrDefault();
        //var ImageNode = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartX.HasValue && o.IsMapToChartX.Value).FirstOrDefault();
        //var DisplayColumnList = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartY.HasValue && o.IsMapToChartY.Value).OrderBy(o => o.Sort).ToList();
        //var PricesNode = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsFilterByCurrentUser.HasValue && o.IsFilterByCurrentUser.Value).FirstOrDefault();


    }
}

