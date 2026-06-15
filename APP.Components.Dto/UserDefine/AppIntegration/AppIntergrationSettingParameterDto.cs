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
    public partial class AppIntergrationSettingParameterDto
    {
       
        [DataMember(EmitDefaultValue = false)]
        public APIConfigParameterDTO APIConfigParameters { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string ConnectionString { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int ActionId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string APIType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? SaasApplicationId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string ApplicatoinDisplay { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public bool IsArrayJsonData { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string TransactionCrudType {
            get 
            {
                return InternalFiledName;
            }           
        }

        [DataMember(EmitDefaultValue = false)]
        public APIPostResponseDto PostResponseDto { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public AppDataSetExDto ImportDataSetDto { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictProviderEnvironmentVariable
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictUsedEnvironmentVariable
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<KeyValuePair<string, string>> ResponseHeaderKeyAndValueList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppIntergrationParameterOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppIntergrationSchemaDataSetMappingDto SchemaDataSetMappingDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> SimpleQueryParameterNameList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ProviderName
        {
            get;
            set;
        }

       

        [DataMember(EmitDefaultValue = false)]
        public string ParentOperationName
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppFileDto PayloadFile
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<string> GeneratedTableNameList
        {
            get
            {
                if (SchemaDataSetMappingDto != null && SchemaDataSetMappingDto.GeneratedTableNameList != null)
                {
                    return SchemaDataSetMappingDto.GeneratedTableNameList.Distinct().ToList();
                }

                return null;
            }
            
        }


        [DataMember(EmitDefaultValue = false)]
        public int? DataSourceType
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<ApiDataStructureNodeDto> ApiDataStructure
        {
            get;
            set;
        }
    }


    public partial class AppIntergrationParameterOtherSettingsDto
    { 
        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedToGenerateStagingTables
        {
            get;
            set;
        }  

        [DataMember(EmitDefaultValue = false)]
        public int? ParentOperationId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public EmAppApiPayloadDataType? PayloadDataType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? PayloadImportSettingId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ResponseImportSettingId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsMultipartFormDataContent
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsForceRecreateStagingTables { get; set; }
    }

    public partial class AppIntergrationSchemaDataSetMappingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string RootNodeName
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<SchemaDataSetMappingDefinitionDto> NodeSettingDtoList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<ApiDataStructureNodeDto> HierachyNodeNameList
        {
            get;
            set;
        }

       

        

        [DataMember(EmitDefaultValue = false)]
        public List<string> NodeTypeNameList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> GeneratedTableNameList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DatabaseViewDto ImportTableDiagram
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string OriginalSchemaDataSetMappingJsonString
        {
            get;
            set;
        }      
        
    }

    public partial class SchemaDataSetMappingDefinitionDto
    {

        [DataMember(EmitDefaultValue = false)]
        public string NodeName
        {
            get;
            set;
        }

        public string Type
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string MappingToTableName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsSingleField
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ProcessMode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RollUpToParentNodeName
        {
            get;
            set;
        }

        [IgnoreDataMember]                
        public bool IsCreateTable
        {
            get
            {
                return ProcessMode.HasValue && ProcessMode.Value == (int)EmAppSchemaDataSetMappingNodeProcessMode.CreateTable;
            }
        }

        [IgnoreDataMember]
        public bool IsMapToExistingTable
        {
            get
            {
                return ProcessMode.HasValue && ProcessMode.Value == (int)EmAppSchemaDataSetMappingNodeProcessMode.MapToExistingTable;
            }
        }

        [IgnoreDataMember]
        public bool IsRemoved
        {
            get
            {
                return !IsCreateTable && !IsMapToExistingTable;
            }
        }



        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedToRollUpAllChild
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public bool IsNeedToserialize
        {
            get
            {
                return ProcessMode.HasValue && ProcessMode.Value == (int)EmAppSchemaDataSetMappingNodeProcessMode.SerializeToParent;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public List<SchemaDataSetMappingDefinitionPropertyDto> Properties
        {
            get;
            set;
        }

    }

    public partial class SchemaDataSetMappingDefinitionPropertyDto
    {

        [DataMember(EmitDefaultValue = false)]
        public string PropertyName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Type
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string OverwirtType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RefToDefinitionName
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string SqlDataType
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public bool IsLogicalKey
        {
            get;
            set;
        }
       

        [DataMember(EmitDefaultValue = false)]
        public bool IsCreateColumn
        {
            get;
            set;
        }

        public bool IsRemoved
        {
            get
            {
                return !IsCreateColumn;
            }
        }

        public bool IsRefToSerializedObj
        {
            get;
            set;
        }

    }


}
