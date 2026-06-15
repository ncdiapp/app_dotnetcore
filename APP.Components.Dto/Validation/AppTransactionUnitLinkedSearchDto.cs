using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto{
    
    public partial class AppTransactionUnitLinkedSearchDto
    {
        partial void OnInitialized()
        {            
        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppTransactionUnitLinkedSearchDto.NameProperty, aValidationResult);
            CustomerValidateProperty(AppTransactionUnitLinkedSearchDto.ActionProperty, aValidationResult);
            CustomerValidateProperty(AppTransactionUnitLinkedSearchDto.SearchIdProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppTransactionUnitLinkedSearchDto.NameProperty)
            {
                if (string.IsNullOrEmpty(Name))
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_AppTransactionUnitLinkedSearch_NameIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }

            if (PropertyName == AppTransactionUnitLinkedSearchDto.ActionProperty)
            {
                if (!Action.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_AppTransactionUnitLinkedSearch_ActionIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }

            if (PropertyName == AppTransactionUnitLinkedSearchDto.SearchIdProperty)
            {
                if (!SearchId.HasValue && !SearchSaveId.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_AppTransactionUnitLinkedSearch_PleaseSelectSearchOrSavedSearch", ValidationItemType.Error, "", PropertyName));
                }
            }
        }
        
       
        
    }
}

