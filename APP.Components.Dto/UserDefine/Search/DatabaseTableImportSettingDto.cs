using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Components.Dto;
using DatabaseSchemaMrg.DataSchema;

namespace APP.Components.EntityDto
{
    public class DatabaseTableImportSettingDto
    {
        //[DataMember(EmitDefaultValue = false)]
        //public string ImportName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Status { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? NeedToUpdateTransactionId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsFlatSingleTableImport { get; set; }

        //[DataMember(EmitDefaultValue = false)]
        //public bool IsSettingCommitted
        //{
        //    get
        //    {
        //        return Status.HasValue && Status.Value == (int)EmAppImportStatus.Released;
        //    }            
        //}

        [DataMember(EmitDefaultValue = false)]
        public bool IsDataImported { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsDraft
        {
            get
            {
                return Status.HasValue && Status.Value == (int)EmAppImportStatus.Draft;
            }
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsFinalized
        {
            get
            {
                return Status.HasValue && Status.Value == (int)EmAppImportStatus.Released;
            }
        }



        [IgnoreDataMember]
        public bool IsUpdateImportedTableDataFromTempTable { get; set; }









        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedToCreateImportApi { get; set; }





        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseColumnExDto> OrgSourceColumns { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseColumnExDto> SourceColumns { get; set; }







        [DataMember(EmitDefaultValue = false)]
        public bool IsEntityImport { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsEntityPureManyToManyRelationshipImport { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ParentEntityId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ChildEntityId { get; set; }




        [DataMember(EmitDefaultValue = false)]
        public int? DataSourceRegisterId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? SaasApplicationId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string ImportFileName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string TempTableName { get; set; }


        [IgnoreDataMember]
        public List<string> NeedToDropTempTableNames { get; set; }

        [IgnoreDataMember]
        public string CurrentImportFileName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string OrgTempTableName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string RelationShipTableName { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public int? DefaultTransactionId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? DefaultSearchId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? DefaultUpdateApiId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsSpilitToMultipleTables { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseTableInfoDto> Tables { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictTableNameAndEntityLookUpIdColumnName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<string>> DictTableNameAndEntityLookUpDisplayColumnNameList
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<string, string> DictManyToManyCascadingChildTableNameAndParentTableName
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, DatabaseColumnCascadingDto>> DictTableNameColumnNameAndCascadingDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, string>> DictTableNameColumnNameAndSourceColumnNameMapping
        {
            get;
            set;
        }


        [IgnoreDataMember]
        public Dictionary<string, DatabaseTableInfoDto> DictTableNameAndDto
        {
            get
            {
                if (Tables != null)
                {
                    return Tables.ToDictionary(o => o.Name, o => o);
                }
                else
                {
                    return new Dictionary<string, DatabaseTableInfoDto>();
                }
            }
        }


        [DataMember(EmitDefaultValue = false)]
        public string TableNameDisplay
        {
            get
            {
                if (Tables != null)
                {
                    return string.Join(", ", Tables.Select(o => o.Name).ToList());
                }
                else
                {
                    return "";
                }
            }

        }


        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<string, List<DatabaseTable>> DictLevelAndTableList
        //{
        //    get
        //    {
        //        Dictionary<string, List<DatabaseTable>> toReturn = new Dictionary<string, List<DatabaseTable>>();

        //        if (DcitTableNameAndDto != null)
        //        {
        //            toReturn.Add("1", DcitTableNameAndDto.Values.Where(o => o.Description == "1").ToList());
        //            toReturn.Add("2", DcitTableNameAndDto.Values.Where(o => o.Description == "2").ToList());
        //            toReturn.Add("3", DcitTableNameAndDto.Values.Where(o => o.Description == "3").ToList());
        //        }

        //        return toReturn;
        //    }
        //}


        [IgnoreDataMember]
        public List<DatabaseTableInfoDto> LevelOneTables
        {
            get
            {
                if (Tables != null)
                {
                    return Tables.Where(o => o.Tag.ToString() == "1").ToList();
                }
                else
                {
                    return new List<DatabaseTableInfoDto>();
                }
            }
        }


        [IgnoreDataMember]
        public List<DatabaseTableInfoDto> LevelTwoTables
        {
            get
            {
                if (Tables != null)
                {
                    return Tables.Where(o => o.Tag.ToString() == "2").ToList();
                }
                else
                {
                    return new List<DatabaseTableInfoDto>();
                }
            }

        }

        [IgnoreDataMember]
        public List<DatabaseTableInfoDto> LevelThreeTables
        {
            get
            {
                if (Tables != null)
                {
                    return Tables.Where(o => o.Tag.ToString() == "3").ToList();
                }
                else
                {
                    return new List<DatabaseTableInfoDto>();
                }
            }

        }

        private Dictionary<string, string> _DictTableNameAndPkColumnName;

        [IgnoreDataMember]
        public Dictionary<string, string> DictTableNameAndPkColumnName
        {
            get
            {
                if (_DictTableNameAndPkColumnName == null)
                {
                    Dictionary<string, string> dictTableNameAndPkColumnName = new Dictionary<string, string>();

                    foreach (var tableDto in Tables)
                    {
                        dictTableNameAndPkColumnName.Add(tableDto.Name, "");

                        var pkColumn = tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);
                        if (pkColumn != null)
                        {
                            dictTableNameAndPkColumnName[tableDto.Name] = pkColumn.Name;
                        }

                    }

                    _DictTableNameAndPkColumnName = dictTableNameAndPkColumnName;
                }

                return _DictTableNameAndPkColumnName;
            }
        }



        private Dictionary<string, List<string>> _DictTableNameAndLogicKeyColumnNameList;

        [IgnoreDataMember]
        public Dictionary<string, List<string>> DictTableNameAndLogicKeyColumnNameList
        {
            get
            {
                if (_DictTableNameAndLogicKeyColumnNameList == null)
                {
                    Dictionary<string, List<string>> dictTableNameAndLogicKeyColumnNameList = new Dictionary<string, List<string>>();

                    foreach (var tableDto in Tables)
                    {
                        List<string> uniqueKeyColumnNames = tableDto.Columns.Where(o => o.IsLogicKey).Select(o => o.Name).ToList();
                        dictTableNameAndLogicKeyColumnNameList.Add(tableDto.Name, uniqueKeyColumnNames);
                    }

                    _DictTableNameAndLogicKeyColumnNameList = dictTableNameAndLogicKeyColumnNameList;
                }

                return _DictTableNameAndLogicKeyColumnNameList;
            }
        }

        public void ResetDictTableNameAndLogicKeyColumnNameList()
        {
            _DictTableNameAndLogicKeyColumnNameList = null;
        }


       

        //[IgnoreDataMember]
        //public Dictionary<string, List<string>> DictTableNameAndLogicKeyColumnNameList
        //{
        //    get;
        //    set;
        //}

        //public void ResetDictTableNameAndLogicKeyColumnNameList()
        //{
        //    _DictTableNameAndLogicKeyColumnNameList = null;
        //}


        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<string, List<DatabaseColumn>> DictTableNameAndForeignLogicKeyColumns
        //{
        //    get;
        //    set;
        //}




        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<string, List<string>> DictTableNameAndOrgColumnNameList
        //{
        //    get
        //    {
        //        Dictionary<string, List<string>> dictTableNameAndOrgColumnNameList = new Dictionary<string, List<string>>();

        //        foreach (var tableDto in Tables)
        //        {
        //            List<string> orgColumnNameList = tableDto.Columns.Where(o => !(o.IsPrimaryKey && o.IsAutoNumber) && !o.IsForeignKey).Select(o => o.Name).ToList();
        //            dictTableNameAndOrgColumnNameList.Add(tableDto.Name, orgColumnNameList);
        //        }

        //        return dictTableNameAndOrgColumnNameList;
        //    }

        //}


        [IgnoreDataMember]
        public bool IsHaveConditionFilter
        {
            get
            {
                if (Tables != null)
                {
                    return Tables.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.TransformCondition)) != null;
                }
                else
                {
                    return false;
                }
            }
        }


        public List<string> SimulateImportedTableNames
        {
            get;
            set;
        }


        [IgnoreDataMember]
        public bool IsHaveSimulateImportedData
        {
            get
            {
                return SimulateImportedTableNames != null && SimulateImportedTableNames.Count > 0;
            }
        }


        [DataMember(EmitDefaultValue = false)]
        public int? SourceDataSourceFrom { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? SourceDataSourceType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string SourceSchemaOwner { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string SourceTableName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? SourceDataSetId { get; set; }
    }


    public partial class DatabaseColumnExDto : DatabaseColumn
    {
        public DatabaseColumnExDto()
        { 
        
        }

        public DatabaseColumnExDto(DatabaseColumn columnDto)
        {
            Name = columnDto.Name;
            Tag = columnDto.Tag;
            NetName = columnDto.NetName;
            DbDataType = columnDto.DbDataType;
            DataType = columnDto.DataType;
            Nullable = columnDto.Nullable;
            TableName = columnDto.TableName;
            IsAutoNumber = columnDto.IsAutoNumber;
            Length = columnDto.Length;
            Precision = columnDto.Precision;
            Scale = columnDto.Scale;
            DefaultValue = columnDto.DefaultValue;
            SchemaOwner = columnDto.SchemaOwner;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EntityId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string EntityColumnName
        {
            get;
            set;
        }
    }
}
