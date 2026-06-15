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
    public class AppMasterDetailValidationDto
    {
        //<FieldName, isInvalid>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, bool> DictOneToOneFields
        {
            get;
            set;
        }

        //<FieldName, ErrorMessage>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictOneToOneFieldNameAndErrorMessage
        {
            get;
            set;
        }

        //<UnitId, <SiblingFieldName, isInvalid>>        
        [DataMember(EmitDefaultValue = false)]
		public Dictionary<string, Dictionary<string, bool> > DictSiblingOneToOneFields
		{
            get;
            set;
        }

        //<UnitId, <SiblingFieldName, ErrorMessage>>        
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, string>> DictSiblingOneToOneFieldNameAndErrorMessage
        {
            get;
            set;
        }


        //<UnitId, <UnitId, <RowIndex,  AppChildDatValidationResultDto>>   
        [DataMember]
        public Dictionary<string, Dictionary<int, AppChildDatValidationResultDto>> DictOneToManyFields
        {
            get;
            set;
        }

        //<UnitId, <UnitId, ErrorMessageList>>        
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<string>> DictChildGridUnitIdAndErrorMessageList
        {
            get;
            set;
        }

    }



    
    public class AppChildDatValidationResultDto
    {
        //<FieldName, isInvalid>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, bool> DictOneToOneFields
        {
            get;
            set;
        }
        

        // <GrandChildUnitId, <RowIndex, <FiledName, IsInvalid>>>
        [DataMember]
        public Dictionary<String, Dictionary<int, Dictionary<string, bool>>> DictOneToManyFields
        {
            get;
            set;
        }

       

    }

}
