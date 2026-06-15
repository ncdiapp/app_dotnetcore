using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;
using System.ComponentModel;

namespace APP.Components.EntityDto
{
    public partial class AppEsiteStyleSheetUpdateDto
    {
        [DataMember]
        public int? EsiteId
        {
            get;
            set;
        }


        //  "CompanySite" or "AppMain"
        [DataMember]        
        public string ScssFileComanyOrAppType
        {
            get;
            set;
        }

        
        [DataMember]
        public Dictionary<string, Dictionary<string, AppEsiteStyleObjDto>> DictMeidaSizeCodeAndDomIdAndStyleObj
        {
            get;
            set;
        }

        [DataMember]
        public Dictionary<string, string> DictNeedToUpdateStyleRuleSelectorAndStyleText
        {
            get;
            set;
        }

        [DataMember]
        public bool IsModified
        {
            get;
            set;
        }
    }

    public partial class AppEsiteStyleObjDto
    {
        [DataMember]
        public string CssText
        {
            get;
            set;
        }


        // Update or Append to SCSS file
        [DataMember]
        public bool IsNew
        {
            get;
            set;
        }
    }
}