using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppTransactionFieldDto
    {

        [DataMember(EmitDefaultValue = false)]
        public string DataBaseTableName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UnitDisplayName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TransactionName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TransactionOrganizedType
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Guid? ParentPKFieldGuid
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppTransactionFieldExDto MappingToAvailableSourceUnitTransactionFieldExDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> Expression
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? LayoutItemId
        {
            get;
            set;
        }


        public string JsonPath
        {
            get
            {
                if (DataBaseFieldName != null)
                {
                    return DataBaseFieldName.Replace("___", ".");
                }

                return "";
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public int FieldStoreMode
        {
            get
            {
                if (IsTempVariable.HasValue && IsTempVariable.Value)
                {
                    return (int)EmAppDataFieldStoreMode.TemporaryField;
                }
                else if (IsStoreToExtendTable.HasValue && IsStoreToExtendTable.Value)
                {
                    return (int)EmAppDataFieldStoreMode.ExtendTable;
                }

                return (int)EmAppDataFieldStoreMode.DatabaseTable;
            }
            set 
            { 
            
            }
        }

      
    }
}

