using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    public partial class AppFilePathDto
    {
        //[DataMember(EmitDefaultValue = false)]
        //public int? FileId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string FilePath { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string FtpUserName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string FtpPassword { get; set; }

    }
}
