using System.Collections.Generic;
using System.Runtime.Serialization;

namespace APP.Components.EntityDto
{
    public partial class DatabaseTableInfoDto
    {
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, UpdateByFkTableMappingDto> DictColumnNameAndUpdateMappingDto
        {
            get;
            set;
        }
    }

    public class UpdateByFkTableMappingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string ColumnName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string FkTableName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string FkTableSchema { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string OrgValueColumnName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string NewValueColumnName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string MappingDisplay { get; set; }
    }
}
