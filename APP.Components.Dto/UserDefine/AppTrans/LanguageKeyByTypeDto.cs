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
    public class LanguageKeyByTypeDto 
    {
        [DataMember(EmitDefaultValue = false)]
        public bool IsModified
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? LanguageId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int KeyType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string KeyTypeDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string DefaultLanguageText
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string LanguageText 
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? LanguageKeyId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string ResourceKey
        {
            get;
            set;
        }

        
        [DataMember(EmitDefaultValue = false)]
        public int? SysLableLanguageKeyId
        {
            get;
            set;
        }               

        [DataMember(EmitDefaultValue = false)]
        public int? TargetTypeSystemFieldId
        {
            get;
            set;
        }

        public int? MenuId
        {
            get;
            set;
        }

        public int? TransactionUnitId
        {
            get;
            set;
        }

        public int? TransactionFieldId
        {
            get;
            set;
        }

        public int? LinkTargetId
        {
            get;
            set;
        }

        public int? TransactionUnitLinkedSearchId
        {
            get;
            set;
        }

        public int? SearchId
        {
            get;
            set;
        }

        public int? SearchFieldId
        {
            get;
            set;
        }

        public int? SearchViewId
        {
            get;
            set;
        }

        public int? SearchViewFieldId
        {
            get;
            set;
        }        
    }
}

