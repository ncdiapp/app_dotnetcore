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
    /// <summary>
    /// DTO class for the entity 'AppSefolder'.
    /// </summary>


    public partial class AppCatalogueTreeDto : BaseAppCatalogueTreeDto
    {

        [DataMember(EmitDefaultValue = false)]
        public AppCatalogueTreeDto[] Children
        {
            get;
            set;
        }




    }
}

