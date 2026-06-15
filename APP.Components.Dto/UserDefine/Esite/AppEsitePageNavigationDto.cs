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
    public partial class AppEsitePageNavigationDto
    {



        [DataMember]
        public int? SiteId
        {
            get;
            set;
        }

        [DataMember]
        public int? FromPageId
        {
            get;
            set;
        }

        [DataMember]
        public int? ToPageId
        {
            get;
            set;
        }


        public List<string> ParamList
        {
            get; set;
        }

        [DataMember]
        public System.String ClosePopupCallBackFunc
        {
            get; set;
        }

        [DataMember]
        public bool isPopup
        {
            get; set;
        }





    }


}