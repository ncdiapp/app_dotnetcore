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
    public partial class AppTransactionUnitFormulaDto
    {

        [DataMember(EmitDefaultValue = false)]
        public int? AssignToTransFieldId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string OrgFormulaExpression { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public int? WarningHighlightTransFieldId {
        //    get {
        //        return 14754;
        //    }
        //    set {

        //    }
        //}


        //[DataMember(EmitDefaultValue = false)]
        //public int? WarningHighlightStyleId
        //{
        //    get
        //    {
        //        return 1;
        //    }
        //    set
        //    {

        //    }
        //}

        [DataMember(EmitDefaultValue = false)]
        public AppFormulaLeadSettingDto LeadFunctionSettingDto { get; set; }

    }

    public partial class AppFormulaLeadSettingDto
    {
      

        [DataMember(EmitDefaultValue = false)]
        public int? LeadFunctionType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? LeadRows { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? LeadFieldId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? AssignToFieldId { get; set; }


    }
}