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
    public class AppTransactionStructureDto
    {

        [DataMember]
        public int? FormID
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

        [DataMember(EmitDefaultValue = false)]
        public int? SaasApplicationId
        {
            get;
            set;
        }

        //Key: unitID : value PK Field for delete ID 
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, List<String>> DictTransactionUnitPKFied
        {
            get;
            set;
        }

        //Key: unitID : Unit internal code for UI design
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, string> DictTransactionUnitIdInternalCode
        {
            get;
            set;
        }


        //Key: child UnitID : value Parent UnitId
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, string> DictUnitIdParentUnitId
        {
            get;
            set;
        }

        //Key: FieldId : value Cascading Pareant t UnitId
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, string> DictCascadedIdParentField
        {
            get;
            set;
        }

        //Key: child UnitID : value Parent UnitId
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, string> DictFieldIdUnitId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> ChildDeleteValidationUnitIds
        {
            get;
            set;
        }





        [DataMember]
        public bool IsLockingTransaction
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, Dictionary<string, int>> DictTransactionUnitIdDBFiledNameControlType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, Dictionary<string, string>> DictTransactionUnitIdDBFiledNameBarCodeType
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, int> DictAvailableSourceUnitIdAndSubscribeUnitId
        {
            get;
            set;
        }

        //Key:SubscribeUnitId , value: SubscribeUnitMappingKeyFieldDBName ,key: SourceUnitMappingKeyFieldDBName
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, Dictionary<string, string>> DictAvailableSubscribeUnitIdAndKeyFieldMapping
        {
            get;
            set;
        }


        ////Key UnitId, value Filed DbFeildName
        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<int, List<string>> DictTransactionUnitIdDBFieldNameMantory
        //{
        //    get;
        //    set;
        //}


        //// <UnitId,  <TransFiledDbFeildName, AppTransactionFieldValidationDto>> 
        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<int, Dictionary<string, AppTransactionFieldValidationDto>> DictUnitIdFieldDbNameAndValidationDto
        //{
        //    get;
        //    set;
        //}



        //Key:UnitId, Key:filedname, value:FiledID
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, Dictionary<string, int>> DictTransactionUnitIdFiledNameFiledID
        {
            get;
            set;
        }


        //Key:UnitId, Key:FieldID, Value: FieldDisplayName
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, Dictionary<int, string>> DictTransactionUnitIdFieldIdFieldDisplayName
        {
            get;
            set;
        }


        //Key:UnitId,
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<string>> DictTransactionUnitIdGroupByFields
        {
            get;
            set;
        }

        //Key:UnitId,
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<AppTransactionUnitLinkedSearchExDto>> DictTransactionUnitIdLinkedSearchList
        {
            get;
            set;
        }

        //Key:field.Id,

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<LookupItemDto>> DictStandAloneEntityDataSource
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictStandAloneFiledIDMappingEntityID
        {
            get;
            set;
        }


        //Key:UnitId,  value: list filedname,
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<string>> DictUnitIdStoreProcOrQueryDataSetMasterFiled
        {
            get;
            set;
        }





        // value : transaction FieldID
        [DataMember]
        public List<string> IsUsedRetrieveDataMappingDataSourceFiedIds
        {
            get;
            set;
        }

        [DataMember]
        public Dictionary<string, List<string>> DictRetrieveDataChildDDLFieldIdAndRetrieveButtonFieldIdList
        {
            get;
            set;
        }

        [DataMember]
        public Dictionary<string, Dictionary<string, List<string>>> DictRetrieveDataOneToManyCascading_TransUnitId_ParentFieldNameAndChildFieldNames
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppEntityInfoDto> EntityInfoDtoList
        {
            get;
            set;
        }


        //Key:UnitId , key: DatabaseFileName, value: DatabaseFileName
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, Dictionary<string, string>> DictUnitIdFiledIdMappingCrosssId
        {
            get;
            set;
        }



        //Key:UnitId , key: DatabaseFileName, value: DatabaseFileName
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, AppPivotDto> DictUnitIdPivotGrid
        {
            get;
            set;
        }





        //Key:UnitId,  value: list filedname,
        [IgnoreDataMember]
        public Dictionary<string, List<string>> DictUnitIdUnitImageBinaryFieldNames
        {
            get
            {
                Dictionary<string, List<string>> toReturn = new Dictionary<string, List<string>>();

                foreach (int unitIdKey in this.DictTransactionUnitIdDBFiledNameControlType.Keys)
                {
                    List<string> binaryFieldNames = this.DictTransactionUnitIdDBFiledNameControlType[unitIdKey]
                           .Where(o => o.Value == (int)EmAppControlType.ImageBinary).Select(o => o.Key).ToList();
                    toReturn.Add(unitIdKey.ToString(), binaryFieldNames);
                }

                return toReturn;




            }

        }

        [IgnoreDataMember]
        public AppTransactionExDto HierarchyTransactionExdto
        {
            get; set;

        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictFolderIdFolderDisplay
        {
            get;
            set;
        }



        [DataMember]
        public bool? EnableFolderSecurity
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? FolderTransactionId
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public int? FolderUsageType
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public int? FileStorageRootFolderId
        {
            get;
            set;
        }

        //Key:UnitId,  value: list filedname,
        [IgnoreDataMember]
        public Dictionary<string, Dictionary<string, int>> DictUnitIdAuditorFieldNames
        {
            get; set;

        }

        //Key:UnitId,  value: list filedname,
        [IgnoreDataMember]
        public Dictionary<string, Dictionary<string, int>> DictUnitIdLogicalDisplayNames
        {
            get; set;

        }

        //Key:UnitId,  value: list filedname,
        [IgnoreDataMember]
        public Dictionary<string, Dictionary<string, int>> DictUnitIdChangeNotificationDisplayNames
        {
            get; set;

        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, TreeViewSettingDto> DictUnitIdAndTreeViewSetting
        {
            get;
            set;
        }


        //// Changed Trans Field Id trigger CommandAction, can be single or composition command
        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<int, int> DictChangedFieldIdAndCommandId
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, CommandUiDto> DictFieldIdAndCommandDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, CommandUiDto> DictCommandIdAndCommandDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PartnerIDFieldName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UserIDFieldName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, string>> DictGoogleAddressFieldIdAndSubFieldMapping
        {
            get; set;

        }

        [DataMember(EmitDefaultValue = false)]
        public int? ErDiagramId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ImportSettingId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? TransactionDataUpdateImportSettingId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsDraft { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? BusinessScopeId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsWorkflowAutomation
        {
            get
            {
                return BusinessScopeId.HasValue && BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation;
            }
        }       
    }


    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppPivotDto
    {

        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionFieldDto> PivotRowFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionFieldDto> PivotColumnFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionFieldDto> PivotValueFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionFieldDto> AllFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsPivotEdit
        {
            get;
            set;
        }





    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class TreeViewSettingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string TreeViewKeyField
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TreeViewParentKeyField
        {
            get;
            set;
        }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class CommandUiDto
    {
        [DataMember(EmitDefaultValue = false)]
        public object Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ActionType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? MessageTemplateId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public dynamic ActionAttribute
        {
            get;
            set;
        }
    }


    //[DataContract(Namespace = ContractNamespaces.Dto)]
    //public class AppTransactionFieldValidationDto
    //{
    //    [DataMember(EmitDefaultValue = false)]
    //    public object TransactionFieldId
    //    {
    //        get;
    //        set;
    //    }

    //    [DataMember(EmitDefaultValue = false)]
    //    public string DataBaseFieldName
    //    {
    //        get;
    //        set;
    //    }

    //    [DataMember(EmitDefaultValue = false)]
    //    public string DisplayName
    //    {
    //        get;
    //        set;
    //    }

    //    [DataMember(EmitDefaultValue = false)]
    //    public string UnitDisplayName
    //    {
    //        get;
    //        set;
    //    }

    //    [DataMember(EmitDefaultValue = false)]
    //    public int? DataType
    //    {
    //        get;
    //        set;
    //    }


    //    [DataMember(EmitDefaultValue = false)]
    //    public int? MaxCharLegnth
    //    {
    //        get;
    //        set;
    //    }

    //    [DataMember(EmitDefaultValue = false)]
    //    public bool? IsAllowEmpty
    //    {
    //        get;
    //        set;
    //    }     

    //}



}
