using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppConditionalActionDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string BooleanConditionFormulaDisplay
        {
            get;
            set;
        }        

    }

}