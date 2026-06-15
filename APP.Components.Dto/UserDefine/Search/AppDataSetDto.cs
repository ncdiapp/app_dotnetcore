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

    public partial class AppDataSetDto
    {
        //[DataMember(EmitDefaultValue = false)]
        //public int? SaasApplicationId
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public AppDataSetOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }

    }


    public partial class AppDataSetOtherSettingsDto
    {
        [DataMember(EmitDefaultValue = false)]
        public DatabaseViewDto DatabaseDiagramInfo
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public DatabaseTableImportSettingDto TableImportSettingDto
        {
            get;
            set;
        }

    }
}

