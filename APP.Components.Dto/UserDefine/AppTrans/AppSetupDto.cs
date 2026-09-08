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
    public partial class AppSetupDto 
    {
        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> EntityDataSource
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsReadOnly
        {
            get;
            set;
        }

        /// <summary>UI primary group (AppTenantSetting.Category).</summary>
        [DataMember(EmitDefaultValue = false)]
        public string Category
        {
            get;
            set;
        }

        /// <summary>UI secondary group under Category (AppTenantSetting.SubCategory).</summary>
        [DataMember(EmitDefaultValue = false)]
        public string SubCategory
        {
            get;
            set;
        }
    }
}

