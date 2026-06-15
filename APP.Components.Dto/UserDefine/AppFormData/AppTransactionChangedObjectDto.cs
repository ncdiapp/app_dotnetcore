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
    public class AppTransactionChangedObjectDto
    {
        public Dictionary<int, object> AllFreshRootValue { get; set; }

        public List<int> RootLevelChangeFiedIds { get; set; }

        public Dictionary<string, List<AppChildDataDto>> DictChildUnitIdAndNewRows { get; set; }

        public Dictionary<string, List<AppChildDataDto>> DictChildUnitIdAndChangedRows { get; set; }

        public Dictionary<string, List<AppChildDataDto>> DictChildUnitIdAndDeletedRows { get; set; }


    }
}
