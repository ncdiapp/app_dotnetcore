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

    public partial class AppTransactionUnitSearchViewFieldMappingDto
    {

        partial void OnInitialized()
        {
            MandatoryProperties.Remove(AppTransactionUnitSearchViewFieldMappingDto.TransactionFieldIdProperty);
            MandatoryProperties.Remove(AppTransactionUnitSearchViewFieldMappingDto.SearchViewFieldIdProperty);

        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppTransactionUnitSearchViewFieldMappingDto.TransactionFieldIdProperty, aValidationResult);
            CustomerValidateProperty(AppTransactionUnitSearchViewFieldMappingDto.SearchViewFieldIdProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppTransactionUnitSearchViewFieldMappingDto.TransactionFieldIdProperty)
            {
                if (!TransactionFieldId.HasValue || TransactionFieldId.Value == 0)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_TransactionUnitSearchViewFieldMapping_TransactionFieldIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }

            if (PropertyName == AppTransactionUnitSearchViewFieldMappingDto.SearchViewFieldIdProperty)
            {
                //if ((!SearchViewFieldId.HasValue || SearchViewFieldId.Value == 0) && string.IsNullOrWhiteSpace(ExternalAppFieldMappingCode))
                //{
                //    aValidationResult.Items.Add(new ValidationItem(null, "App_TransactionUnitSearchViewFieldMapping_SearchViewFieldIsEmpty", ValidationItemType.Error, "", PropertyName));
                //}
            }
          


        }
        
    }
}

