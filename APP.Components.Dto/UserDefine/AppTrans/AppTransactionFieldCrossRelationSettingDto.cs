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
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppTransactionFieldCrossRelationSettingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? SubscribeToUnitId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? SubscribeToTransFieldId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? AggregationType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ParentUnitSubscribeChildAggFunctionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ChildUnitSubscribeParentFieldId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CurrentUnitId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsModified
        {
            get;
            set;
        }


    }
}
