using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{


    public class AppApplicationAssetsItemSetDto
    {
        public int? ApplicationId { get; set; }
        
        public int? EmAppApplicationAssetsType { get; set; }

        public ObservableSet<AppApplicationAssetsItemExDto> AppApplicationAssetsItemExDtoSet { get; set; }


    }
}