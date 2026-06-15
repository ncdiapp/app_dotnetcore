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
    public partial class AppEsiteThemParameterDto
    {
        [DataMember]
        public string ParameterName
        {
            get;
            set;
        }
        
       
        [DataMember]        
        public string ParameterValue
        {
            get;
            set;
        }


        [DataMember]
        public string ParameterType
        {
            get;
            set;
        }

        [DataMember]
        public string ParameterCategory
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
        public int Sort
        {
            get;
            set;
        }
        
    }

    
}