using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;


namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppListDataValidationDto
    {
        // <RowIndex,  AppChildDatValidationResultDto>   
        [DataMember]
        public Dictionary<int, AppChildDatValidationResultDto> ListData
        {
            get;
            set;
        }
    }

}
