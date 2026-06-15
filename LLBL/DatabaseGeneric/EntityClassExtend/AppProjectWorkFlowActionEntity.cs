
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using APP.LBL;
using APP.LBL.HelperClasses;
using APP.LBL.FactoryClasses;
using APP.LBL.RelationClasses;

using SD.LLBLGen.Pro.ORMSupportClasses;

namespace APP.LBL.EntityClasses
{
	


	public partial class AppProjectWorkFlowActionEntity 
    {
        public Guid? NextWorkFlowGuid
        {
            get;
            set;
        }
	}
}
