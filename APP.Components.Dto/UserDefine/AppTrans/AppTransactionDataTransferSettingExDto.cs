using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{

    public partial class AppTransactionDataTransferSettingExDto
    {
        public List<AppTransactionSaveAsMappingExDto> DataTransferMappingList
        {
            get;
            set;
        }

        public List<AppTransactionSaveAsMappingExDto> ApiResponseMappingList
        {
            get;
            set;
        }


        public int? TargetApiOperationId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppIntergrationSettingParameterExDto TargetApiOperationDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<string> TargetApiInputParameterList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<ApiDataStructureNodeDto> TargetApiPayloadDataStructure
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<ApiDataStructureNodeDto> TargetApiResponseDataStructure
        {
            get;
            set;
        }
    }
}

