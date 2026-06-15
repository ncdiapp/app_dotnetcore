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
    public partial class AppEstoreProductFieldExDto
    {



        [IgnoreDataMember]
        public string SysTableFiledPath
        {
            get; set;
        }

    }
}

