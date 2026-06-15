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
    
    public partial class AppTransactionUnitSearchFieldMappingDto 
    {
        partial void OnInitialized()
        {
            MandatoryProperties.Remove(AppTransactionUnitSearchFieldMappingDto.TransactionFieldIdProperty);
            MandatoryProperties.Remove(AppTransactionUnitSearchFieldMappingDto.SearchFieldIdProperty);

        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppTransactionUnitSearchFieldMappingDto.TransactionFieldIdProperty, aValidationResult);
            CustomerValidateProperty(AppTransactionUnitSearchFieldMappingDto.SearchFieldIdProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppTransactionUnitSearchFieldMappingDto.TransactionFieldIdProperty)
            {
                
                if (TransactionFieldId == 0)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_TransactionUnitSearchFieldMapping_TransactionFieldIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }

            if (PropertyName == AppTransactionUnitSearchFieldMappingDto.SearchFieldIdProperty)
            {
                if (SearchFieldId == 0)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_TransactionUnitSearchViewFieldMapping_SearchFieldIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }



        }
     
    }
}

