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

    public partial class AppSearchDto : NotifyDataErrorDto 
    {

        partial void OnInitialized()
        {
            MandatoryProperties.Remove(AppSearchDto.TypeProperty);
            MandatoryProperties.Remove(AppSearchDto.IsAutoExecuteProperty);
            
        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppSearchDto.DataSetIdProperty, aValidationResult);
          
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppSearchDto.DataSetIdProperty)
            {
                if (!DataSetId.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_Search_DataSetIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }
        }


        
        
    }
}

