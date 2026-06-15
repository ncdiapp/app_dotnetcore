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
   
    public partial class AppSearchSavedDto : NotifyDataErrorDto 
    {

        //partial void OnInitialized()
        //{

        //}


        //partial void CustomerValidateDto(ValidationResult aValidationResult)
        //{
        //    CustomerValidateExDto(aValidationResult);
        //}

        //protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        //{
        //    CustomerValidateProperty(AppEntityInfoDto.EntityTypeProperty, aValidationResult);
        //}

        //partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        //{
        //    if (PropertyName == AppEntityInfoDto.EntityTypeProperty)
        //    {
        //        if (!EntityType.HasValue)
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(null, "App_EntityInfoTypeNotEmpty", ValidationItemType.Error, "", PropertyName));
        //        }
        //    }
        //}

       
        
    }
}

