using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppViewLinkedSeaechOrUrlDto
    {
        [DataMember(EmitDefaultValue = false)]
        public AppViewLinkedSearchOrUrlOtherSettingsDto OtherSettingsDto
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? SourceViewColumnId4
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TargetSearchFieldId4
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? SourceViewColumnId5
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TargetSearchFieldId5
        {
            get; set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string TargetSearchFieldName1
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TargetSearchFieldName2
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TargetSearchFieldName3
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TargetSearchFieldName4
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TargetSearchFieldName5
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UiId
        {
            get; set;
        }
    }


    public partial class AppViewLinkedSearchOrUrlOtherSettingsDto   
    {
        [DataMember(EmitDefaultValue = false)]
        public int? TemplateItemType { get; set; }

    }
}