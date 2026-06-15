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
    public partial class AppFormLinkTargetSetDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? FormId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? SearchViewId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? TransactionUnitId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<object> DeletedItemIds { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppFormLinkTargetDto> AppFormLinkTargetDtoSet { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppViewLinkedSeaechOrUrlExDto> AppViewLinkedSeaechOrUrlDtoSet { get; set; }
    }
}