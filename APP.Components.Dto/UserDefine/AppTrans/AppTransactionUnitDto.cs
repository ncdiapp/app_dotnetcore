using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppTransactionUnitDto
    {
        public string JsonPath
        {
            get
            {
                if (DataBaseTableName != null)
                {
                    return DataBaseTableName.Replace("___", ".");
                }

                return "";
            }
        }



        public int? MaxWidthToEnableMobileRowEditorPopup
        {
            get
            {

                return MaxRowCount;


            }
        }
    }
}