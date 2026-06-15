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
    public partial class AppEsiteTemplateRegisterDto
    {
        [DataMember]
        public string TemplateCode
        {
            get;
            set;
        }

        [DataMember]        
        public string Description
        {            
            get;
            set;
        }

        [DataMember]
        public string ThumbnailImageUrl
        {
            get;
            set;
        }

        [DataMember]
        public string PreviewUrl
        {
            get;
            set;
        }

        [DataMember]
        public Dictionary<string, string> LandingPageLayout
        {
            get;
            set;
        }        
    }    
}