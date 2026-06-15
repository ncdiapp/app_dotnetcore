using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.EntityDto;

namespace APP.Components.Dto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public sealed class UserContextShopbag 
    {
       
        public UserContextShopbag()
        {
          
        }

        [DataMember]
        public UserContext UserContext
        {
            get;
            set;
        }


          [DataMember]
        public List<AppEntitySearchInfoDto> AddressList 
        { get; set; }
    }
}