using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public class AppApplicationDataManipulationDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? ApplicationId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<AppFormExDto> PublishedFormList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionDto> MasterDetailTransactionList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionDto> ListEditTransactionList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionNavigationExDto> SearchNavigationList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionNavigationExDto> FolderNavigationList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<AppSearchDto> ApplicationSearchDtoList { get; set; }
        

        [DataMember(EmitDefaultValue = false)]
        public List<AppSearchDto> AllSearchDtoList { get; set; }


    }
}
