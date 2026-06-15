using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Framework
{
    /// <summary>
    /// DTO class for the entity 'AppLanguage'.
    /// </summary>
    
   
    internal   class AppLanguageDto 
    {


   
        public System.String ResourceKey
        {
            get;
            set;
        }

        public System.Int32 LanguageId
        {
            get;
            set;
            
        }


        public System.String Value
        {
            get;
            set;

        }


       
        
    }
}

