using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using APP.Framework;
using APP.Framework.Collections;
using System.Runtime.Serialization;
using APP.Components.Dto;
using APP.Framework.Validation;

namespace APP.Components.EntityDto
{

    public partial class AppSearchExDto : AppSearchDto
    {


        private Dictionary<int, List<AppSearchFieldExDto>> _DictInnerEntityChildFieldExdto;

        //Key: master SearchFiledId
        public Dictionary<int, List<AppSearchFieldExDto>> DictInnerEntityChildFieldExdto
        {
            get
            {
                if (_DictInnerEntityChildFieldExdto == null)
                {
                    _DictInnerEntityChildFieldExdto = new Dictionary<int, List<AppSearchFieldExDto>>();

                    var masterEntityFiledIds = this.AppSearchFieldList.Where(o => o.MasterEntityFieldlId.HasValue).Select(o => o.MasterEntityFieldlId.Value).Distinct();
                    foreach (int masterFieldId in masterEntityFiledIds)
                    {
                        _DictInnerEntityChildFieldExdto.Add(masterFieldId, this.AppSearchFieldList.Where(o => o.MasterEntityFieldlId == masterFieldId).ToList());
                    }

                }

                return _DictInnerEntityChildFieldExdto;

            }
        }

        [DataMember(EmitDefaultValue = false)]
        public AppSearchViewExDto DefaultSearchViewExDto
        {
            get; set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppSearchExDto EshopCardSearchExDto
        {
            get; set;
        }


    }
}

