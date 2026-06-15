using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;
using System.Reflection;

namespace APP.Components.EntityDto
{
    public class ViewTableAddRemoveDto : DatabaseViewDto
    {
        public ViewTableAddRemoveDto()
        {

        }

        public ViewTableAddRemoveDto(DatabaseViewDto databaseViewDto)
        {          
            var type = typeof(DatabaseViewDto);
            PropertyInfo[] properties = type.GetProperties();

            foreach (var property in properties)
            {
                property.SetValue(this, property.GetValue(databaseViewDto, null), null);
            }

        }


        [DataMember(EmitDefaultValue = false)]
        // KeyValuePair<SchemaOwner, TableName>
        public List<KeyValuePair<string, string>> NeedToAddOwnerTablePairList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<string> NeedToRemoveUniqTableOrAliasNames { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedToDropTableFromDb { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public string AddFkRefTableFromTableName { get; set; }
    }
}





