
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

    public partial class AppSefolderEntity
    {


        public int CountContent
        { get; set; }

        public bool IsRootFolder
        { get; set; }

        public List<string> UserAvailableActions
        {
            get;
            set;
        }


       

    }

 

}


