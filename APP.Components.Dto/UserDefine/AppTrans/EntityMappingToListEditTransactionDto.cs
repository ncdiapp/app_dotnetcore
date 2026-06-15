using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public class EntityMappingToListEditTransactionDto
    {      
    
        public int? EntityId
        {
            get;
            set;
        }
       
        public string EntityCode
        {
            get;
            set;
        }

        public int? EntityType
        {
            get;
            set;
        }

        public int? MappingToListEditTransactionId
        {
            get;
            set;
        }

        public string MappingToListEditTransactionName
        {
            get;
            set;
        }

        public int? MappingToItemDetailTransactionId
        {
            get;
            set;
        }

        public string MappingToItemDetailTransactionName
        {
            get;
            set;
        }
    }

}

