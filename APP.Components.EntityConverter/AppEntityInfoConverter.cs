using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.EntityClasses;
using APP.Components.EntityDto;



namespace APP.Components.EntityConverter
{
    /// <summary>
    /// Convert Properties between  AppEntityInfoEntity and  AppEntityInfoDto
    /// </summary>
    public static partial class AppEntityInfoConverter 
    {
         /// <summary>
        ///  Convert AppEntityInfoEntity To  AppEntityInfoDto
        /// </summary>
        public static AppEntityInfoDto ConvertEntityToDto(AppEntityInfoEntity aAppEntityInfoEntity)
        {        
    		AppEntityInfoDto aAppEntityInfoDto = new AppEntityInfoDto();
    		CopyEntityPropertyToDto( aAppEntityInfoEntity, aAppEntityInfoDto);          
			return aAppEntityInfoDto;
        }
		 /// <summary>
        ///  Convert AppEntityInfoEntity To  AppEntityInfoExDto
        /// </summary>
        public static AppEntityInfoExDto ConvertEntityToExDto(AppEntityInfoEntity aAppEntityInfoEntity)
        {        
    		AppEntityInfoExDto aAppEntityInfoExDto = new AppEntityInfoExDto();
			CopyEntityPropertyToDto( aAppEntityInfoEntity, aAppEntityInfoExDto);
			
			
			
            return aAppEntityInfoExDto;
        }
		
		 /// <summary>
        ///  Convert AppEntityInfoEntity To  AppEntityInfoDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppEntityInfoEntity aAppEntityInfoEntity,AppEntityInfoDto aAppEntityInfoDto)
        {        
    		
           // aAppEntityInfoDto.StopChangeTracking();
 			aAppEntityInfoDto.Id = aAppEntityInfoEntity.EntityInfoId;
 			aAppEntityInfoDto.EntityCode = aAppEntityInfoEntity.EntityCode;
 			aAppEntityInfoDto.Description = aAppEntityInfoEntity.Description;
 			aAppEntityInfoDto.EntityType = aAppEntityInfoEntity.EntityType;
 			aAppEntityInfoDto.TableName = aAppEntityInfoEntity.TableName;
 			aAppEntityInfoDto.SchemaOwner = aAppEntityInfoEntity.SchemaOwner;
 			aAppEntityInfoDto.IdentityField = aAppEntityInfoEntity.IdentityField;
 			aAppEntityInfoDto.DisplayFiled1 = aAppEntityInfoEntity.DisplayFiled1;
 			aAppEntityInfoDto.DisplayFiled3 = aAppEntityInfoEntity.DisplayFiled3;
 			aAppEntityInfoDto.DisplayFiled2 = aAppEntityInfoEntity.DisplayFiled2;
 			aAppEntityInfoDto.PartnerFilterFiled = aAppEntityInfoEntity.PartnerFilterFiled;
 			aAppEntityInfoDto.QueryText = aAppEntityInfoEntity.QueryText;
 			aAppEntityInfoDto.DataSourceFrom = aAppEntityInfoEntity.DataSourceFrom;
 			aAppEntityInfoDto.IsSystemDefine = aAppEntityInfoEntity.IsSystemDefine;
 			aAppEntityInfoDto.IsSharedbyMutipleCompany = aAppEntityInfoEntity.IsSharedbyMutipleCompany;
 			aAppEntityInfoDto.AppCreatedById = aAppEntityInfoEntity.AppCreatedById;
 			aAppEntityInfoDto.AppCreatedDate = aAppEntityInfoEntity.AppCreatedDate;
 			aAppEntityInfoDto.AppModifiedDate = aAppEntityInfoEntity.AppModifiedDate;
 			aAppEntityInfoDto.AppModifiedById = aAppEntityInfoEntity.AppModifiedById;
 			aAppEntityInfoDto.AppCreatedByCompanyId = aAppEntityInfoEntity.AppCreatedByCompanyId;
 			aAppEntityInfoDto.ColorCodeField = aAppEntityInfoEntity.ColorCodeField;
 			aAppEntityInfoDto.SaasApplicationId = aAppEntityInfoEntity.SaasApplicationId;
 			aAppEntityInfoDto.ExternalKeyField = aAppEntityInfoEntity.ExternalKeyField;
 			aAppEntityInfoDto.OtherSettings = aAppEntityInfoEntity.OtherSettings;
 			aAppEntityInfoDto.IdentityCoumnDataType = aAppEntityInfoEntity.IdentityCoumnDataType;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppEntityInfoDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEntityInfoEntity.AppCreatedDate);
                aAppEntityInfoDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEntityInfoEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppEntityInfoEntity, aAppEntityInfoDto);
		}
		
		 /// <summary>
        ///  Copy AppEntityInfoDto Properties to   AppEntityInfoEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppEntityInfoEntity aAppEntityInfoEntity,AppEntityInfoDto aAppEntityInfoDto)
        {        
 
      			aAppEntityInfoEntity.EntityCode = aAppEntityInfoDto.EntityCode;
      			aAppEntityInfoEntity.Description = aAppEntityInfoDto.Description;
      			aAppEntityInfoEntity.EntityType = aAppEntityInfoDto.EntityType;
      			aAppEntityInfoEntity.TableName = aAppEntityInfoDto.TableName;
      			aAppEntityInfoEntity.SchemaOwner = aAppEntityInfoDto.SchemaOwner;
      			aAppEntityInfoEntity.IdentityField = aAppEntityInfoDto.IdentityField;
      			aAppEntityInfoEntity.DisplayFiled1 = aAppEntityInfoDto.DisplayFiled1;
      			aAppEntityInfoEntity.DisplayFiled3 = aAppEntityInfoDto.DisplayFiled3;
      			aAppEntityInfoEntity.DisplayFiled2 = aAppEntityInfoDto.DisplayFiled2;
      			aAppEntityInfoEntity.PartnerFilterFiled = aAppEntityInfoDto.PartnerFilterFiled;
      			aAppEntityInfoEntity.QueryText = aAppEntityInfoDto.QueryText;
      			aAppEntityInfoEntity.DataSourceFrom = aAppEntityInfoDto.DataSourceFrom;
      			aAppEntityInfoEntity.IsSystemDefine = aAppEntityInfoDto.IsSystemDefine;
      			aAppEntityInfoEntity.IsSharedbyMutipleCompany = aAppEntityInfoDto.IsSharedbyMutipleCompany;
 
  
   
    
      			aAppEntityInfoEntity.AppCreatedByCompanyId = aAppEntityInfoDto.AppCreatedByCompanyId;
      			aAppEntityInfoEntity.ColorCodeField = aAppEntityInfoDto.ColorCodeField;
      			aAppEntityInfoEntity.SaasApplicationId = aAppEntityInfoDto.SaasApplicationId;
      			aAppEntityInfoEntity.ExternalKeyField = aAppEntityInfoDto.ExternalKeyField;
      			aAppEntityInfoEntity.OtherSettings = aAppEntityInfoDto.OtherSettings;
      			aAppEntityInfoEntity.IdentityCoumnDataType = aAppEntityInfoDto.IdentityCoumnDataType;
			
			if(aAppEntityInfoDto.Id == null)
			{
				aAppEntityInfoEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppEntityInfoEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppEntityInfoEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEntityInfoEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppEntityInfoEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppEntityInfoEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEntityInfoEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppEntityInfoEntity, aAppEntityInfoDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppEntityInfoEntity aAppEntityInfoEntity,AppEntityInfoDto aAppEntityInfoDto);
		
		static partial void OnCopyDtoToEntityDone(AppEntityInfoEntity aAppEntityInfoEntity,AppEntityInfoDto aAppEntityInfoDto);
		
   
       
    }
}

 