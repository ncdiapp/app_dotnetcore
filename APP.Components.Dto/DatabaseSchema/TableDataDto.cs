using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.EntityDto;
using APP.Components.Dto;

namespace APP.Components.Dto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class TableDataDto
	{
       

       // public ReadOnlyCollection<DatabaseConstraint> CheckConstraints { get; }

        [DataMember]
        public string DataBaseTableName { get; set; }

        [DataMember]
        public List<string> Columns { set; get; }


	
		public Dictionary<string,string> DictColumnDataType { set; get; }


		[DataMember]
		public List<Dictionary<string,object>> DataRowList { set; get; }

		//SchemaOwner

		[DataMember]    
		public  string SchemaOwner { set; get; }

        [DataMember]
        public string ErrorMessage { set; get; }

        public bool IsHaveErrors { set; get; }
    }
}