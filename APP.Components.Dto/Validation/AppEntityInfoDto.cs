using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppEntityInfoDto
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
            CustomerValidateProperty(AppEntityInfoDto.EntityTypeProperty, aValidationResult);
            CustomerValidateProperty(AppEntityInfoDto.TableNameProperty, aValidationResult);
            CustomerValidateProperty(AppEntityInfoDto.IdentityFieldProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {

            if (PropertyName == AppEntityInfoDto.EntityTypeProperty)
            {
                if (!EntityType.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_EntityInfo_TypeIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }

            if (PropertyName == AppEntityInfoDto.TableNameProperty)
            {
                if (string.IsNullOrEmpty(TableName) && (EntityType.HasValue && EntityType.Value == (int)EmAppEntityType.SystemDefineTable))
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_EntityInfo_TableNameIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }


            if (PropertyName == AppEntityInfoDto.IdentityFieldProperty)
            {
                if (EntityType.HasValue && EntityType.Value == (int)EmAppEntityType.SystemDefineTable && string.IsNullOrEmpty(IdentityField)
                        && (string.IsNullOrEmpty(QueryText) || string.IsNullOrEmpty(QueryText.Trim()))
                    )
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_EntityInfo_IdentityFieldIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }
        }

        //private void SetUpDefaultValue()
        //{
        //    this.IsAllowCopyValueInRefSaveAsAndTabCopy = true;
        //    this.IsAllowCopyValueInTabCopy = true;
        //    this.IsAllowProductTabCopy = false;
        //    // this.IsChangeTrackingEnabled  = false;
        //    this.IsForcedCreateFirstVersion = false;
        //    this.IsMasterSynToChild = false;
        //    this.IsMerchPlanCopyAble = false;
        //    this.IsReferenceStaticFiledControl = false;
        //    this.IsUsedForAutoGeneration = false;
        //    this.IsUseDrillDownControl = false;



        //}
    }
}