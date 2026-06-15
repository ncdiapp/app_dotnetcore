using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;
using Newtonsoft.Json;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json.Linq;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DatabaseViewDto
    {

        [DataMember(EmitDefaultValue = false)]
        public string ViewName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsNewView { get; set; }

        //[DataMember(EmitDefaultValue = false)]
        //public int? DataSetId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string QueryString { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string ErrorMessage { get; set; }

        //Table
        //[DataMember(EmitDefaultValue = false)]
        //public List<DatabaseViewTableDto> Tables { get; set; }


        private Dictionary<string, DatabaseViewTableDto> _dictTables;


        [DataMember(EmitDefaultValue = false)]
        //Key: Table Uniq Name or AliasName
        public Dictionary<string, DatabaseViewTableDto> DictTables 
        {
            get
            {
                return _dictTables;
            }
            set
            {
                if (value != null)
                {
                    var caseInsensitiveDictionary = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase);

                    foreach (var kvp in value)
                    {
                        caseInsensitiveDictionary[kvp.Key] = kvp.Value;
                    }

                    _dictTables = caseInsensitiveDictionary;
                }
                else
                {
                    _dictTables = null;
                }
            }
        }





        //// Key: schemOnwer_TableName,
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, bool>> DictAllColumns { get; set; }   // Dictionary<tableName, Dictionary<columnName, isSelected>>


        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<string, Dictionary<string, DatabaseViewColumnDto>> DictSelectedColumns { get; set; }    // Dictionary<tableName, Dictionary<columnName, DatabaseViewColumnDto>>


        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseViewColumnDto> SelectedColumnsList { get; set; }



        // Join
        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseViewJoinDto> Joins { get; set; }


        // Where Condition
        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseViewColumnDto> WhereConditionFilterColumns { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, DatabaseViewTextAnnotationDto> DictTextAnnotationGuidAndDto { get; set; }
                       

        // 
        [DataMember(EmitDefaultValue = false)]
        public int? DataSourceRegisterId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ApplicationId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsErDiagram { get; set; }

    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class JoinTreeNode
    {
        [DataMember]
        public Guid GUID { get; set; }

        [IgnoreDataMember]
        public DatabaseViewJoinDto ParentJoin { get; set; }

        [IgnoreDataMember]
        public DatabaseViewJoinDto RootJoin { get; set; }

        [IgnoreDataMember]
        public object FirstChild { get; set; }

        [IgnoreDataMember]
        public object SecondChild { get; set; }

    }


    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DatabaseViewTableDto : JoinTreeNode
    {
        
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(NullableDatabaseFkMappingDtoListConverter))]
        public List<DatabaseFkMappingDto> FKRefTables { get; set; }

        
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(NullableDatabaseFkMappingDtoListConverter))]
        public List<DatabaseFkMappingDto> FKRefedTables { get; set; }

        [DataMember]
        public int SortOrder { get; set; }


        [DataMember]
        public string SchemaOwner { get; set; }


        [DataMember]
        public string TableName { get; set; }


        [DataMember]
        public string TableAlias { get; set; }


        [DataMember]
        public string UniqTableOrAliasName { get; set; }

        [DataMember]
        public int? Level { get; set; }


        [DataMember]
        public string TableFullNamePath { get; set; }


        [DataMember]
        public List<string> PkNames { get; set; }


        [DataMember]
        public DatabaseViewTableType TableType { get; set; }


        [DataMember]
        public int PositionX { get; set; }

        [DataMember]
        public int PositionY { get; set; }

        [DataMember]
        public int Width { get; set; }

        [DataMember]
        public int Height { get; set; }


      


    }

   
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DatabaseViewColumnDto
    {

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public string ColumnName { get; set; }

        [DataMember]
        public string ColumnAlias { get; set; }             

        [DataMember]
        public string ColumnDisplayName
        {
            get; set;
        }

        [DataMember]
        public string SearchViewDisplayName { get; set; }

        //[DataMember]
        //public string TableName { get; set; }

        //[DataMember]
        //public string TableAliasName { get; set; }

        [DataMember]
        public string UniqTableOrAliasName { get; set; }

        [DataMember]
        public string SortType { get; set; }

        [DataMember]
        public int? SortLevel { get; set; }

        [DataMember]
        public int? EntityId { get; set; }

        [DataMember]
        public bool IsUsedAsSearchField { get; set; }

        [DataMember]
        public bool IsUsedAsViewField { get; set; }

        [DataMember]
        public bool IsFolderIdKeyField { get; set; }

        [DataMember]
        public string WhereConditionExpression { get; set; }


       
        public int? ControlType { get; set; }
    }




    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DatabaseViewJoinDto : JoinTreeNode
    {
        [DataMember(EmitDefaultValue = false)]
        public int SortOrder { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public string JoinMethod { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseViewJoinConditionDto> JoinConditionList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string JoinConditionDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ContraintName
        {
            get;
            set;
        }

        


    }

    public class DatabaseViewJoinConditionDto
    {
        [DataMember(EmitDefaultValue = false)]
        public Guid GUID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string LeftSideSchemaOwner { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string LeftSideTable { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string LeftSideColumn { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string RightSideSchemaOwner { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string RightSideTable { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string RightSideColumn { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string JoinOperationType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }
       
    }


    public enum DatabaseViewTableType
    {
        Table = 1,
        View = 2,
        Function = 3,
        Synonym = 4,
    }


    public class JoinMethod
    {
        public const string CrossJoin = "CROSS JOIN";
        public const string InnerJoin = "INNER JOIN";
        public const string LeftOuterJoin = "LEFT OUTER JOIN";
        public const string RightOuterJoin = "RIGHT OUTER JOIN";
        public const string FullOuterJoin = "FULL OUTER JOIN";

        public const string ForeignKey = "Foreign Key";
    }

    public class JoinConditionOperationType
    {
        public const string EqualTo = "=";
        public const string NotEqualTo = "<>";
        public const string GreaterThan = ">";
        public const string LessThan = "<";
        public const string GreaterOrEqualTo = ">=";
        public const string LessOrEqualTo = "<=";
        public const string Function = "Fx";
    }



    public class DatabaseViewColumnSortType
    {
        public const string Ascending = "";
        public const string Descending = "DESC";
    }

    public class DatabaseViewTextAnnotationDto
    {
        [DataMember(EmitDefaultValue = false)]
        public Guid GUID { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string Text { get; set; }

        [DataMember]
        public int PositionX { get; set; }

        [DataMember]
        public int PositionY { get; set; }

        [DataMember]
        public int Width { get; set; }

        [DataMember]
        public int Height { get; set; }
    }


    public class ExceptionHandlingConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            try
            {
                JToken t = JToken.FromObject(value);
                t.WriteTo(writer);
            }
            catch (Exception)
            {
                // Handle the exception here
                // For example, you can log it and continue without serializing the property
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }





    public class NullableDatabaseFkMappingDtoListConverter : JsonConverter<List<DatabaseFkMappingDto>>
    {
        public override List<DatabaseFkMappingDto> ReadJson(JsonReader reader, Type objectType, List<DatabaseFkMappingDto> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                JToken token = JToken.Load(reader);

                if (token.Type == JTokenType.Array)
                {
                    // If it's a valid array, deserialize it normally
                    return token.ToObject<List<DatabaseFkMappingDto>>(serializer);
                }
                else if (token.Type == JTokenType.Null)
                {
                    // If it's null, return null
                    return null;
                }
                else
                {
                    // If it's some other format (e.g., old format List<string>), return null
                    return null;
                }
            }
            catch (Exception)
            {
                // If an exception occurs during deserialization, return null
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, List<DatabaseFkMappingDto> value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            JArray array = JArray.FromObject(value, serializer);
            array.WriteTo(writer);
        }
    }
}





