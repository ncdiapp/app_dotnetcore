using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;

namespace APP.Components.EntityDto
{  
    public class DatabaseViewUpdateDto
    {
        [DataMember(EmitDefaultValue = false)]
        public DatabaseViewDto OrgViewDto { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public AppDataSetExDto DataSetDto { get; set; }
                        

        [DataMember(EmitDefaultValue = false)]
        public int? SearchId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? SearchViewId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? TransactionId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string TransactionRootPrimaryKey { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<EmAppLinkTargetActionType> NeedToCreateLinkTargetActions { get; set; }


		[DataMember(EmitDefaultValue = false)]
		public int? DataSourceRegisterId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? RootFolderId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableFolderSecurity { get; set; }

    }
}





