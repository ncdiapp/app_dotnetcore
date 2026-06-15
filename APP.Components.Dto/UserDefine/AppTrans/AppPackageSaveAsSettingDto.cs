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
    public partial class AppPackageSaveAsSettingDto
    {
        public int? WorkflowTransactionId
        {
            get;
            set;
        }
      
        public List<int> OrgTransactionIdList
        {
            get;
            set;
        }

        public List<int> OrgSearchIdList
        {
            get;
            set;
        }

        public List<int> OrgDatasetIdList
        {
            get;
            set;
        }

        public List<int> OrgEntityIdList
        {
            get;
            set;
        }  
    }
}

