using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    public partial class AppIntergrationSettingDto
    {


        [DataMember(EmitDefaultValue = false)]
        public AppIntergrationOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }

    }


    public partial class AppIntergrationOtherSettingsDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string DatabaseTablePrefix
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string ApiDefaultAuthorization
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictEnvironmentVariable
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictCookieSetting
        {
            get;
            set;
        }
        
    }
}
