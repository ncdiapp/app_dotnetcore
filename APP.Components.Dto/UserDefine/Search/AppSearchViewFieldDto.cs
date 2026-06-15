using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityDto
{
   
    public partial class AppSearchViewFieldDto
    {
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> Expression
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UiId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string FormulaDisplayName
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppSearchViewFieldOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }
    }



    public partial class AppSearchViewFieldOtherSettingsDto
    {
        public AppFilterDto FilterDto { get; set; }
    }
    

    public partial class AppFilterDto
    {
        [DataMember(EmitDefaultValue = false)]
        public AppFilterConditionDto Condition1 { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public AppFilterConditionDto Condition2 { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsAnd { get; set; }
    }

    public partial class AppFilterConditionDto
    {
        [DataMember(EmitDefaultValue = false)]
        public EmAppWijmoOperator Operator { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public object Value { get; set; }
    }
}

