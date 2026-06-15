using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;
using DatabaseSchemaMrg.DataSchema;

namespace APP.Components.EntityDto
{
    public partial class DatabaseTableInfoDto : DatabaseTable
    {
        [DataMember(EmitDefaultValue = false)]
        public int? ApplicationId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsManyToManyCascadingChild
        {
            get;
            set;
        }

        public DatabaseTable DatabaseTable
        {
            get;
            set;
        }

        public bool IsEntityRelationTable
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsMatrixTable
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> ForeignMatrixKeyTableNameList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TransformCondition
        {
            get;
            set;
        }


        public bool IsNewTable
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsImportToExistingTable
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, TableColumnImportMappingDto> DictExistingTableColumnNameAndImportMappingDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, UpdateByFkTableMappingDto> DictColumnNameAndUpdateMappingDto
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public Dictionary<string, DatabaseColumn> DictNewColumnNameAndDto
        {
            get;
            set;
        }
    }


    public class TableColumnImportMappingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string ColumnName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string MapToSourceColumnName
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public DatabaseColumn MapToSourceColumnDto
        {
            get;
            set;
        }
        
    }


    public class UpdateByFkTableMappingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string ColumnName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string FkTableName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string FkTableSchema
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string OrgValueColumnName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string NewValueColumnName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string MappingDisplay
        {
            get;
            set;
        }
        
    }
}
