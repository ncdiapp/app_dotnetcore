using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using APP.Framework;
using APP.Framework.Collections;
using System.Runtime.Serialization;
using APP.Components.Dto;
namespace APP.Components.EntityDto
{
    [DataContract]
    public sealed class CriteriaOperatorDto
    {
        public CriteriaOperatorDto()
        { }

        [DataMember]
        public EmAppCriteriaOperatorType? OperatorType
        {
            get;
            set;
        }

        [DataMember]
        public string Display
        {
            get;
            set;
        }

        [DataMember]
        public bool IsMultipleValuesAllowed
        {
            get;
            set;
        }

        [DataMember]
        public bool IsEditorRequired
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Enum.Equals(OperatorType, ((CriteriaOperatorDto)obj).OperatorType);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
