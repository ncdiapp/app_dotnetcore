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
    public partial class AppTransactionExDto
    {

        //not for client , no datacontract need

        [IgnoreDataMember]
        public Dictionary<string, AppTransactionUnitExDto> DictAllTransactionUnitIdExDto
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public Dictionary<Guid, Guid> DictCurrentPKOrFKLinkToParentKeyGuidMap
        {
            get;
            set;
        }


        //	o => o.IsMasterSiblingUnit.HasValue && o.IsMasterSiblingUnit.Value

        //not for client , no datacontract need



        //[IgnoreDataMember]
        //public	AppTransactionUnitExDto RootTransactionUnitExDto
        //{
        //	get;set;

        //}

        [IgnoreDataMember]
        public List<AppTransactionUnitExDto> SibLineTransactionUnitIdExDtoList
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public AppTransactionUnitExDto RootMasterUnit
        {
            get
            {
                return this.AppTransactionUnitList.FirstOrDefault();
            }
        }


        //	AppTransactionUnitExDto rootMasterUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);


        [IgnoreDataMember]
        public Dictionary<string, string> DictAllTableNameUnitId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string PrimaryKeyField { get; set; }


        [IgnoreDataMember]
        public Dictionary<string, List<AppTransactionUnitDeleteFlowExDto>> DictUnitDeleteList
        {
            get;
            set;
        }

        private Dictionary<int, AppTransactionFieldExDto> _DictAllTransactionField;

        // key: FieldID
        [IgnoreDataMember]
        public Dictionary<int, AppTransactionFieldExDto> DictAllTransactionField
        {
            get
            {
                if (_DictAllTransactionField == null || _DictAllTransactionField.Count == 0)
                {

                    List<AppTransactionFieldExDto> allfiedList = new List<AppTransactionFieldExDto>();

                    if (this.DictAllTransactionUnitIdExDto != null)
                    {
                        this.DictAllTransactionUnitIdExDto.Values.ForAll(o => allfiedList.AddRange(o.AppTransactionFieldList));
                    }

                    _DictAllTransactionField = allfiedList.ToDictionary(o => (int)o.Id, o => o);




                }

                return _DictAllTransactionField;

            }

        }


        private Dictionary<object, AppTransactionFieldExDto> _DictRootLevelUnitTransactionField;

        [IgnoreDataMember]
        public Dictionary<object, AppTransactionFieldExDto> DictRootLevelUnitTransactionField
        {
            get
            {
                if (_DictRootLevelUnitTransactionField == null || _DictRootLevelUnitTransactionField.Count == 0)
                {

                    List<AppTransactionFieldExDto> allRootLevelFiedList = new List<AppTransactionFieldExDto>();

                    if (this.DictAllTransactionUnitIdExDto != null)
                    {
                        this.DictAllTransactionUnitIdExDto.Values.Where(o => !o.ParentTransactionUnitId.HasValue).ForAll(o => allRootLevelFiedList.AddRange(o.AppTransactionFieldList));
                    }

                    _DictRootLevelUnitTransactionField = allRootLevelFiedList.ToDictionary(o => o.Id, o => o);




                }

                return _DictRootLevelUnitTransactionField;

            }

        }


        [IgnoreDataMember]
        public List<AppTransactionUnitExDto> ChildMatrixUnit
        {
            get
            {


                if (DictAllTransactionUnitIdExDto != null)
                {

                    return DictAllTransactionUnitIdExDto.Values
                   .Where(unit => unit.IsMatrixUnit.HasValue && unit.IsMatrixUnit.Value)
                   //  .OrderBy (o=>o.TransactionFlow )
                   .ToList();
                }
                else
                {
                    return new List<AppTransactionUnitExDto>();
                }


            }

        }

        [IgnoreDataMember]
        public List<AppTransactionUnitExDto> ChildMatrixPivotUnit
        {
            get
            {


                if (DictAllTransactionUnitIdExDto != null)
                {

                    return DictAllTransactionUnitIdExDto.Values
                   .Where(unit => unit.EmGridViewDisplayType.HasValue && unit.EmGridViewDisplayType.Value == (int)EmAppTransactionGridDisplayType.PivotEditGrid)
                   //  .OrderBy (o=>o.TransactionFlow )
                   .ToList();
                }
                else
                {
                    return new List<AppTransactionUnitExDto>();
                }


            }

        }

        //Key: Matrix UnitID Value, Value Tuple : matrixKeyFieldExdto, matrixForeignKeyFieldExdto, matrixForeignKeyUnit
        [IgnoreDataMember]
        public Dictionary<AppTransactionUnitExDto, List<Tuple<AppTransactionFieldExDto, AppTransactionFieldExDto, AppTransactionUnitExDto>>> DictMatrixUnitSetting
        {
            get
            {


                Dictionary<AppTransactionUnitExDto, List<Tuple<AppTransactionFieldExDto, AppTransactionFieldExDto, AppTransactionUnitExDto>>> toRetrun = new Dictionary<AppTransactionUnitExDto, List<Tuple<AppTransactionFieldExDto, AppTransactionFieldExDto, AppTransactionUnitExDto>>>();

                foreach (AppTransactionUnitExDto matrixUnit in ChildMatrixUnit)
                {
                    var matrixKeyFieldIdList = matrixUnit.AppTransactionFieldList.Where(o => o.MatrixForeignKeyFieldId.HasValue).ToList();

                    List<Tuple<AppTransactionFieldExDto, AppTransactionFieldExDto, AppTransactionUnitExDto>> listMatrixForeignKeyUnit = new List<Tuple<AppTransactionFieldExDto, AppTransactionFieldExDto, AppTransactionUnitExDto>>();
                    foreach (var matrixKeyFieldExdto in matrixKeyFieldIdList)
                    {

                        var matrixForeignKeyFieldExdto = DictAllTransactionField[matrixKeyFieldExdto.MatrixForeignKeyFieldId.Value];

                        int unitId = matrixForeignKeyFieldExdto.TransactionUnitId;

                        var matrixForeignKeyUnit = DictAllTransactionUnitIdExDto[unitId.ToString()];



                        Tuple<AppTransactionFieldExDto, AppTransactionFieldExDto, AppTransactionUnitExDto> atuple =
                        new Tuple<AppTransactionFieldExDto, AppTransactionFieldExDto, AppTransactionUnitExDto>(matrixKeyFieldExdto, matrixForeignKeyFieldExdto, matrixForeignKeyUnit);

                        listMatrixForeignKeyUnit.Add(atuple);


                    }


                    toRetrun[matrixUnit] = listMatrixForeignKeyUnit;

                }




                return toRetrun;
            }

        }

        //HasChildMatrixUnit 
        //DataContractSerializer dcs = new DataContractSerializer(typeof(T));
        //    using (MemoryStream ms = new MemoryStream())
        [IgnoreDataMember]
        public bool HasChildMatrixUnit
        {
            get
            {

                return (!ChildMatrixUnit.IsEmpty() && ChildMatrixUnit.Count > 0);
            }
        }

        [IgnoreDataMember]
        public bool HasChildMatrixPivotUnit
        {
            get
            {

                return (!ChildMatrixPivotUnit.IsEmpty() && ChildMatrixPivotUnit.Count > 0);
            }
        }

        [IgnoreDataMember]
        public Dictionary<string, AppTransactionUnitDeleteFlowExDto> DictUnitValidationDeleteList
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public string RootPrimaryKeyValue { get; set; }


        [IgnoreDataMember]
        public AppTransactionStructureDto AppMasterDetailStructureDtoInfo
        {
            get;
            set;
        }


        [IgnoreDataMember]
        public List<AppTransactionExDto> TransactionHeader
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public List<AppTransactionExDto> TransactionCrossHeader
        {
            get;
            set;
        }

        private bool _IsAllowAccess = false;

        [IgnoreDataMember]
        public bool IsAllowAccess
        {
            get
            {
                if (this.IsForPublicAcesss.HasValue && this.IsForPublicAcesss.Value)
                {
                    return true;
                }
                else
                {
                    return _IsAllowAccess;
                }

            }

            set
            {
                _IsAllowAccess = value;
            }
        }

        [IgnoreDataMember]
        public List<int> RestrictedTransactionUserActionList
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public List<int> RestrictedTransactionCommandIdList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? DefaultMasterDetailTransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? DefaultMasterDetailTransactionFormId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? DefaultTransactionSearchId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TransactionFileStorageRootFolderName
        {
            get;
            set;
        }


        [IgnoreDataMember]
        public List<int> SepcialEditPermissionTransFieldIdList
        {
            get;
            set;
        }


        [IgnoreDataMember]
        public AppFormExDto PrintAppFormExDto
        {
            get;
            set;
        }


        [IgnoreDataMember]
        public AppMasterDetailDto PrintAppMasterDetailDto
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public Dictionary<int, AppTransactionFieldExDto> PrintDictTransactionFieldIdAndExDto
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public bool IsLoadingPrintForm { get; set; }

        [IgnoreDataMember]
        public int PrintRowCount
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public int PrintColumnCount
        {
            get;
            set;
        }


        //[IgnoreDataMember]
        //public List<int> PrintFileIdList
        //{
        //    get;
        //    set;
        //}             



        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, AppTransactionUnitFormulaSetDto> DictUnitldIdAndFormulaSetDto { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, AppTransactionFieldCrossRelationSettingDto> DictTransFieldIdAndCrossRelationSettingDto { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<AppProjectWorkFlowActionExDto> CommandActionList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<AppProjectWorkFlowActionDto> WorkflowCommandNodeTree
        {
            get;
            set;
        }




        // Default (not set) is transaction master
        // if == 1, SearchView Command
        [DataMember(EmitDefaultValue = false)]
        public int? HostCommandMasterType
        {
            get;
            set;
        }




        //[DataMember(EmitDefaultValue = false)]
        //public AppIntergrationSettingParameterExDto ApiConfigCreate
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public AppIntergrationSettingParameterExDto BaseApiConfigDto
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public AppIntergrationSettingParameterExDto ApiConfigUpdate
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public AppIntergrationSettingParameterExDto ApiConfigDelete
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public List<ApiDataStructureNodeDto> ApiDataStructure
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<ApiDataStructureNodeDto> ApiPostResponseDataStructure
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string LayoutBackGroundColor
        {
            get;
            set;
        }




        
    }


}