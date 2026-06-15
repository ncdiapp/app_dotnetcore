using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppTransactionUnitExDto
    {

        protected override void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            base.CustomerValidateExDto(aValidationResult);
            ValidateAppTransactionFieldList(aValidationResult);
        }

        public void ValidateAppTransactionFieldList(ValidationResult aValidationResult)
        {
            HashSet<string> duplicatedFiledNameHashSetCheck = new HashSet<string>();
            int pkCount = 0;
            int fkCount = 0;


            foreach (var aFieldDto in AppTransactionFieldList)
            {
                ValidationResult filedValidationResult = aFieldDto.ValidateDto(UnitDisplayName);

                if (filedValidationResult.HasErrors)
                {
                    aValidationResult.Merge(filedValidationResult);
                }

                string fieldUniqName = aFieldDto.DataBaseFieldName;

                if (aFieldDto.IsNew && string.IsNullOrWhiteSpace(aFieldDto.DataBaseFieldName))
                {
                    fieldUniqName = aFieldDto.DisplayName;
                }

                if (!duplicatedFiledNameHashSetCheck.Add(fieldUniqName))
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_DuplicatedTransactionFieldFound", ValidationItemType.Error, "Duplicated Transaction Field [{0}] on Unit [{1}].", "");
                    item.MessageParameters.Add(aFieldDto.DisplayName);
                    item.MessageParameters.Add(UnitDisplayName);
                }

                if (aFieldDto.IsPrimaryKey)
                {
                    pkCount++;
                }

                if (aFieldDto.IsLinkToParentPrimaryKey)
                {
                    fkCount++;
                }

            }

            if (IsSynchToDatabaseTable.HasValue && IsSynchToDatabaseTable.Value)
            {
                if (pkCount == 0)
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_NeedToSetAPrimaryKeyFieldOnUnit", ValidationItemType.Error, "Need To Set A Primary Key Field On Transaction Unit [{0}].", "");
                    item.MessageParameters.Add(UnitDisplayName);                
                }

                if (pkCount > 1)
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_MoreThanOnePrimaryKeyFieldsFoundOnUnit", ValidationItemType.Error, "More Than One Primary Key Fields Found On Transaction Unit [{0}].", "");
                    item.MessageParameters.Add(UnitDisplayName);
                }

                if (Level >= 2) 
                {
                    if (fkCount == 0)
                    {
                        ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_NeedToSetAFieldToLinkToParentPrimaryKeyOnUnit", ValidationItemType.Error, "Need To Set A Field To Link To Parent Primary Key On Transaction Unit [{0}].", "");
                        item.MessageParameters.Add(UnitDisplayName);
                    }

                    if (fkCount > 1)
                    {
                        ValidationItem item = aValidationResult.AddItem(null, "App_TransactionField_MoreThanOneFieldsAreSetToLinkToParentPrimaryKeyOnUnit", ValidationItemType.Error, "More Than One Fields Are Set To Link To Parent Primary Key On Transaction Unit [{0}].", "");
                        item.MessageParameters.Add(UnitDisplayName);
                    }
                }
            }

            
 
        }       

    }
}