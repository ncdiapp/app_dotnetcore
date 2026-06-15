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
    public partial class AppTransactionNavigationDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? NavigationType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppSearchViewDto> FolderViewDtoList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppSearchViewDto DefaultFolderViewDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppSearchDto DefaultSearchDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppListMenuExDto DefaultMenuDto
        {
            get;
            set;
        }
    }
}

