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
    public partial class AppEsiteThemParameterUpdateDto
    {
        [DataMember]
        public int? EsiteId
        {
            get;
            set;
        }

        [DataMember]
        public string FilePath
        {
            get;
            set;
        }

        [DataMember]
        public string PreviewFileHtmlContent
        {
            get;
            set;
        }

        [DataMember]
        public List<AppEsiteThemParameterDto> ParameterDtoList
        {
            get;
            set;
        }       

       
    }

   
}