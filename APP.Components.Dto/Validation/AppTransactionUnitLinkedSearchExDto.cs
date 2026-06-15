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
 
    public partial class AppTransactionUnitLinkedSearchExDto 
    {
        protected override void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            base.CustomerValidateExDto(aValidationResult);
            ValidateAppTransactionFieldList(aValidationResult);
        }

        public void ValidateAppTransactionFieldList(ValidationResult aValidationResult)
        {
            foreach (var aSearchFieldMappingDto in AppTransactionUnitSearchFieldMappingList)
            {
                ValidationResult filedValidationResult = aSearchFieldMappingDto.ValidateDto();

                if (filedValidationResult.HasErrors)
                {
                    aValidationResult.Merge(filedValidationResult);
                }
            }

            foreach (var aSearchViewFieldMappingDto in AppTransactionUnitSearchViewFieldMappingList)
            {
                ValidationResult filedValidationResult = aSearchViewFieldMappingDto.ValidateDto();

                if (filedValidationResult.HasErrors)
                {
                    aValidationResult.Merge(filedValidationResult);
                }
            }
        }       
        
    }
}

