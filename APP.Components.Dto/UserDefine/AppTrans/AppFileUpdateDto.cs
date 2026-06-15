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
    public class AppFileUpdateDto
    {
        public List<int> FileIdList { get; set; }

        public int? TargetFolderId { get; set; }
    

    }        

    
}





