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
    public partial class AppEsiteUserDefinedJsFunctionDto
    {
        [DataMember]
        public int? EsiteId
        {
            get;
            set;
        }

        [DataMember]
        public string FunctionName
        {
            get; set;
        }

        [DataMember]
        public Dictionary<string, string> DictParameterNameAndType
        {
            get; set;
        }

        [DataMember]
        public string HtmlContent
        {
            get; set;
        }

        [DataMember]
        public bool IsNewFunction
        {
            get;set;
        }

        [DataMember]
        public string InitalExpression
        {
            get; set;
        }
    }


}