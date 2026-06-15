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

    public partial class AppSearchViewExDto : AppSearchViewDto 
    {
        protected override void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            base.CustomerValidateExDto(aValidationResult);
            ValidateSearchField(aValidationResult);
        }

        public void ValidateSearchField(ValidationResult aValidationResult)
        {

            foreach (var aSearchViewField in AppSearchViewFieldList)
            {
                ValidationResult aSearchViewFieldValidationResult = aSearchViewField.ValidateDto();
                if (aSearchViewFieldValidationResult.HasItems)
                {
                    aValidationResult.Merge(aSearchViewFieldValidationResult);
                }

                //if (!aSearchField.ControlType.HasValue)
                //{
                //    aValidationResult.AddItem(null, "App_SearchField_ControlTypeIsEmpty", ValidationItemType.Error, "Search Field Control Type Is Empty.");
                //}
            }
        }
     
        
    }
}

