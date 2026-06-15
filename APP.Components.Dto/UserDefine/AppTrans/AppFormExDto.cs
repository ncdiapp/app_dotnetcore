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
    public partial class AppFormExDto
    {

        private List<AppFormLayoutItemExDto> _OneToOneFieldFormLayoutItems = null;
        public List<AppFormLayoutItemExDto> OneToOneFieldFormLayoutItems
        {
            get
            {
                if (_OneToOneFieldFormLayoutItems == null)
                {
                    _OneToOneFieldFormLayoutItems = AppFormLayoutItemList.Where(o => o.GridTransactionUnitId == null).ToList();


                }
                return _OneToOneFieldFormLayoutItems;
            }

        }

        private List<AppFormLayoutItemExDto> _OneToManyGridFieldFormLayoutItems = null;
        public List<AppFormLayoutItemExDto> OneToManyGridFieldFormLayoutItems
        {
            get
            {
                if (_OneToManyGridFieldFormLayoutItems == null)
                {
                    _OneToManyGridFieldFormLayoutItems = AppFormLayoutItemList.Where(o => o.GridTransactionUnitId.HasValue).ToList();


                }
                return _OneToManyGridFieldFormLayoutItems;
            }

        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictFormPropertyNameAndErrorMessage { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, string>> DictLayoutItemHostIdAndPropertyErrorMessage { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public FormPublishSettingDto FormPublishSettingDto { get; set; }


        


    }
}