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
    public class AppListDataDto
    {

        [DataMember(EmitDefaultValue = false)]
        public List<AppChildDataDto> ListData
        {
            get;
            set;
        }

        [DataMember]
        public int FormID
        {
            get;
            set;
        }

        [DataMember]
        public int? FolderId
        {
            get;
            set;
        }


        [DataMember]
        public int TransactionId
        {
            get;
            set;
        }

        //[DataMember]
        //public Dictionary<string, List<LookupItemDto>> DictStandAloneEntityDataSource
        //{
        //    get;
        //    set;
        //}
        //[DataMember]
        //public Dictionary<string, string> DictStandAloneFiledIDMappingEntityID
        //{
        //    get;
        //    set;
        //}


        //Key: Tanscation unit ID toString()
        //aChildTransactionUnitExDto.Id.ToString()
        [DataMember(EmitDefaultValue = false)]
        public AppChildDataDto EditCloneAppChildDataDto
        {
            get;
            set;
        }

        ////Key: unitID : value PK Field for delete ID 
        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<String, String> DictTransactionUnitPKFied
        //{
        //    get;
        //    set;
        //}

        [DataMember]
        public bool IsDirty
        {
            get;
            set;
        }


     
        //[DataMember(EmitDefaultValue = false)]
        //public List<Dictionary<string, object>> DeleteChildrenPK
        //{
        //    get;
        //    set;
        //}



        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictDocumentIdFileCode
        {
            get;
            set;
        }

		//[DataMember(EmitDefaultValue = false)]
		//public List<string> ChildDeleteValidationUnitIds
		//{
		//    get;
		//    set;
		//}


		[DataMember(EmitDefaultValue = false)]
		public List<object> MassUpdateRootIdList
		{
			get;
			set;
		}


		


		[DataMember(EmitDefaultValue = false)]
        public AppListDataValidationDto ValidationResultDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsMassUpdate
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int MassUpdateViewId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? DataTransferSettingId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TransactionCommandId
        {
            get;
            set;
        }

    }
}
