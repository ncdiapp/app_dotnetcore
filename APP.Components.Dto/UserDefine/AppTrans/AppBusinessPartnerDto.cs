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
 
    public partial class AppBusinessPartnerDto
    {

        [DataMember(EmitDefaultValue = false), EditableMemberAttribute]
        public int? AssociatedEsiteId
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false), EditableMemberAttribute]
        public string DefaultUserFirstName
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false), EditableMemberAttribute]
        public string DefaultUserLastName
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false), EditableMemberAttribute]
        public string DefaultUserEmail
        {
            get; set;
        }


        [DataMember(EmitDefaultValue = false), EditableMemberAttribute]
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FullName))
                {
                    return FullName;
                }
                else if (!string.IsNullOrWhiteSpace(ShortName))
                {
                    return ShortName;
                }
                else if (!string.IsNullOrWhiteSpace(Code))
                {
                    return Code;
                }
                else
                {
                    return "";
                }
            }

        }
    }
}

