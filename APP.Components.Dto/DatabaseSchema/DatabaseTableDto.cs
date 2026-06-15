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
    public class DatabaseTableDto
    {
       

       // public ReadOnlyCollection<DatabaseConstraint> CheckConstraints { get; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<DatabaseColumnDto> Columns { set; get; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public bool HasAutoNumberColumn { get; set; }

        [DataMember]
        public bool HasCompositeKey { get; set; }

        [DataMember]
        public string NetName { get; set; }

        [DataMember]
        public DatabaseColumnDto PrimaryKeyColumn { get; set; }


       [DataMember]
        public bool IsDbView { get; set; }

        [DataMember]
        public string ObjType { get; set; }

        [DataMember]
		public string SchemaOwner { get; set; }


        private bool _IsEnableAutoKey = true;
        [DataMember]
        public bool IsEnableAutoKey
        {
            get
            {
                return _IsEnableAutoKey;

            }
            set
            {
                _IsEnableAutoKey = value;
            }
        }
        


        [DataMember]
        public string SchemaName { get; set; }

        [DataMember]
        public int? RowCounts { get; set; }

        //[DataMember]
        //public decimal? TotalSpaceKB { get; set; }

        //[DataMember]
        //public decimal? TotalSpaceMB { get; set; }

        //[DataMember]
        //public decimal? UsedSpaceKB { get; set; }

        //[DataMember]
        //public decimal? UsedSpaceMB { get; set; }

        //[DataMember]
        //public decimal? UnusedSpaceKB { get; set; }

        //[DataMember]
        //public decimal? UnusedSpaceMB { get; set; }

        [DataMember]
        public int? EmDataSourceType { get; set; }

    }
}