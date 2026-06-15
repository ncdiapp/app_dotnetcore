using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the  Extend Relation Entity 'AppEstoreCatelogTreeLevel'.
    /// </summary>
  
    public partial class AppEstoreCatelogTreeLevelExDto 
    {

        [IgnoreDataMember]
        public AppSearchViewFieldExDto TreeNodeIDViewFieldIdDto
        {
            get;set;
        }

        [IgnoreDataMember]
        public AppSearchViewFieldExDto TreeNodeDisplay1ViewFieldIdDto
        {
            get; set;
        }


        [IgnoreDataMember]
        public AppSearchViewFieldExDto TreeNodeDisplay2ViewFieldIdDto
        {
            get; set;
        }

        [IgnoreDataMember]
        public AppSearchViewFieldExDto TreeNodeImageIdviewFieldIdDto
        {
            get; set;
        }

        [IgnoreDataMember]
        public AppSearchFieldExDto PassToProductSearchFieldIdDto
        {
            get; set;
        }

        
    }
}

