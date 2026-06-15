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
    public class FormPublishSettingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public bool IsCreateFolderNavigation { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsCreateSearchNavigation { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsCreateApplicationMenu { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableComunication { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsGenerateDropdownFieldItems { get; set; }
    }
}