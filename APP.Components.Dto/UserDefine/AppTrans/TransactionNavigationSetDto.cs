using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class TransactionNavigationSetDto
    {

        [DataMember(EmitDefaultValue = false)]
        public int? TransactionId { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public int? NavigationType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppTransactionNavigationExDto> TransactionNavigationExDtoSet { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<object> DeletedItemIds { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public int? MgtRootFolderId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableFolderSecurity { get; set; }
        

    }
}
