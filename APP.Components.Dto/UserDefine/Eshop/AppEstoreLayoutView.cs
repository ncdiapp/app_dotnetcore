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
    public partial class AppEsiteCatalogueLayoutView
    {

        [DataMember]
        public object StoreId
        {
            get; set;

        }

        [DataMember]
        public int EmEstoreLayoutView
        {
            get;set;

        }

        [DataMember]
        public List<AppCatalogueTreeDto> CatalogueTreeList
        {
            get; set;

        }


        [DataMember]
        public AppEshopCatalogViewDto CatalogCardView
        {
            get; set;

        }


        [DataMember]
        public bool IsHaveTreeNavigation
        {
            get; set;

        }

        [DataMember]
        public bool IsHaveProductDetail
        {
            get; set;

        }
    }
}

