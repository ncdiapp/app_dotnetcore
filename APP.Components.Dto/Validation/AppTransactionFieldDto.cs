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

    public partial class AppTransactionFieldDto
    {
        //partial void OnInitialized()
        //{

        //    //MandatoryProperties.Remove(AppTransactionFieldDto.NoSecurityProperty);          

        //}


        //partial void CustomerValidateDto(ValidationResult aValidationResult)
        //{
        //    CustomerValidateExDto(aValidationResult);
        //}

        //protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        //{
        //    CustomerValidateProperty(AppTransactionFieldDto.n, aValidationResult);
        //}

        //partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        //{
        //    if (PropertyName == AppTransactionFieldDto.DataSetIdProperty)
        //    {
        //        if (!DataSetId.HasValue)
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(null, "App_SearchView_DataSetIsEmpty", ValidationItemType.Error, "", PropertyName));
        //        }
        //    }
        //}



        public ValidationResult ValidateDto(string transactionUnitName)
        {
            ValidationResult aValidationResult = new ValidationResult();

            //CustomerValidateProperty(AppTransactionFieldDto.DataBaseFieldNameProperty, aValidationResult, transactionUnitName);
            CustomerValidateProperty(AppTransactionFieldDto.DisplayNameProperty, aValidationResult, transactionUnitName);
            CustomerValidateProperty(AppTransactionFieldDto.ControlTypeProperty, aValidationResult, transactionUnitName);
            CustomerValidateProperty(AppTransactionFieldDto.EntityIdProperty, aValidationResult, transactionUnitName);
            CustomerValidateProperty(AppTransactionFieldDto.IsLinkToParentPrimaryKeyProperty, aValidationResult, transactionUnitName);

            return aValidationResult;
        }

        public void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult, string transactionUnitName)
        {
            if (PropertyName == AppTransactionFieldDto.DataBaseFieldNameProperty)
            {
                if (string.IsNullOrEmpty(DataBaseFieldName) || string.IsNullOrEmpty(DataBaseFieldName.Trim()))
                {
                    if (!(IsTempVariable.HasValue && IsTempVariable.Value || IsStoreToExtendTable.HasValue && IsStoreToExtendTable.Value))
                    {
                        ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_DataBaseFieldNameIsEmpty", ValidationItemType.Error, "Transaction Field DataBaseFieldName Is Empty On Unit [{0}].", PropertyName);
                        item.MessageParameters.Add(transactionUnitName);
                    }
                }
            }

            if (PropertyName == AppTransactionFieldDto.DisplayNameProperty)
            {
                if (string.IsNullOrEmpty(DisplayName) || string.IsNullOrEmpty(DisplayName.Trim()))
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_DisplayNameIsEmpty", ValidationItemType.Error, "Transaction Field Display Name Is Empty On Unit [{0}] Field [{1}].", PropertyName);
                    item.MessageParameters.Add(transactionUnitName);
                    item.MessageParameters.Add(DataBaseFieldName);
                }
            }

            if (PropertyName == AppTransactionFieldDto.ControlTypeProperty)
            {
                if (ControlType == 0)
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_ControlTypeIsEmpty", ValidationItemType.Error, "Transaction Field Control Type Is Empty On Unit [{0}] Field [{1}].", PropertyName);
                    item.MessageParameters.Add(transactionUnitName);
                    item.MessageParameters.Add(DataBaseFieldName);
                }
            }


            if (PropertyName == AppTransactionFieldDto.EntityIdProperty)
            {
                //if (ControlType == (int)EmAppControlType.DDL && !EntityId.HasValue && string.IsNullOrEmpty())
                //{
                //    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_EntityIsEmpty", ValidationItemType.Error, "Transaction Field DDL Entity Is Empty On Unit [{0}] Field [{1}].", PropertyName);
                //    item.MessageParameters.Add(transactionUnitName);
                //    item.MessageParameters.Add(DataBaseFieldName);
                //}
            }

            if (PropertyName == AppTransactionFieldDto.IsLinkToParentPrimaryKeyProperty)
            {
                if (IsLinkToParentPrimaryKey)
                {
                    if (!ParentPKFieldGuid.HasValue && !LinkToParentPrimaryKeyFieldId.HasValue)
                    {
                        ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_ParentPrimaryKeyIsEmpty", ValidationItemType.Error, "Transaction Field Parent PrimaryKey Is Empty On Unit [{0}] Field [{1}].", PropertyName);
                        item.MessageParameters.Add(transactionUnitName);
                        item.MessageParameters.Add(DataBaseFieldName);
                    }
                }
            }
        }

    }
}

