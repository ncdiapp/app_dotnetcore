
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppEsiteCatalogueExDto
    {
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


    }
}
