
using System;
using System.ComponentModel;
using System.Collections.Generic;

using System.Xml.Serialization;
using APP.LBL;
using SD.LLBLGen.Pro.ORMSupportClasses;

using APP.LBL.HelperClasses;
using System.Linq;
namespace APP.LBL.EntityClasses
{
    public partial class AppProjectTaskPredecessorEntity
    {
        public Guid? PredecessorGuid
        {
            get;
            set;
        }
    }
}
