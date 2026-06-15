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
    
    public partial class AppTransactionDto : NotifyDataErrorDto 
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
            CustomerValidateProperty(AppTransactionDto.TransactionNameProperty, aValidationResult);
            CustomerValidateProperty(AppTransactionDto.TransactionOrganizedTypeProperty, aValidationResult);          
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppTransactionDto.TransactionNameProperty)
            {
                if (string.IsNullOrEmpty(TransactionName) || string.IsNullOrEmpty(TransactionName.Trim()))
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_Transaction_TransactionNameIsEmpty", ValidationItemType.Error, "Transaction Name Is Empty", PropertyName));                   

                }
            }

            if (PropertyName == AppTransactionDto.TransactionOrganizedTypeProperty)
            {
                if (!TransactionOrganizedType.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_Transaction_OrganizedTypeIsEmpty", ValidationItemType.Error, "Organized Type Is Empty", PropertyName));
                }
            }

            
        }
    }
}

