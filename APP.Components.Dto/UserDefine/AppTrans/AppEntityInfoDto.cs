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
    public partial class AppEntityInfoDto
    {
        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> EntityDataList
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public bool? IsNeedToCheckSeccurity
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppEntityInfoOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }

    }


    public class AppEntityInfoOtherSettingsDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string IdentityColumnDataType
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<string> LogicKeyColumnNameList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? ListEditTransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ItemSimpleFormTransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ItemDetailFormTransactionId
        {
            get;
            set;
        }
    }
}