using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
  

    public partial class AppSecurityUserSimpleDto
    {

        [DataMember(EmitDefaultValue = false)]
        public object Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
          public string UserName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int DomainId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string  Email
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> UserContactEmails
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> UserContactGroups
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string Phone
        //{
        //    get;
        //    set;
        //}
    }
}