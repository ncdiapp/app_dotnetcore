
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
	

	
	public partial class AppProjectOrWorkFlowEntity
	{

	
		public EntityCollection<AppProjectWorkFlowTaskEntity> ChildProjectExternalSumaryTask { get; set; }

        private bool _IsConvertDBUtcToClient =true;
        public bool IsConvertDBUtcToClient
        {
            get 
            {
                return _IsConvertDBUtcToClient;
            }
            set
            {
                _IsConvertDBUtcToClient = value;
            }
        }

	}
}
