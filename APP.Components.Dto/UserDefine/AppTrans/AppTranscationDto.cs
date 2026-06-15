using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppTransactionDto
    {      

        //[DataMember(EmitDefaultValue = false)]
        //public int? SaasApplicationId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? FormLayoutType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? PrintFormLayoutType { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public bool? IsAllowSaveAs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TransactionOptionDto OtherOptions { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string CreatedFromDisplay { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? DefaultNavigationSearchId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? DefaultNavigationFolderViewId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? DefaultNavigationMenuId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public TransactionApiSettingDto ConsumApiDataModelSaveSettingDto { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsShowDeleteButton { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TransactionApiSettingDto ConsumApiDataModelDeleteSettingDto { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<string> ApiInputParameterList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? RootWorkflowTransactionId { get; set; }

    }
}