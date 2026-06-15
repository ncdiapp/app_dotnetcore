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
    /// Convert Properties between  AppSysLabelLanguageEntity and  AppSysLabelLanguageDto
    /// </summary>
    public static partial class AppSysLabelLanguageConverter 
    {
         /// <summary>
        ///  Convert AppSysLabelLanguageEntity To  AppSysLabelLanguageDto
        /// </summary>
        public static AppSysLabelLanguageDto ConvertEntityToDto(AppSysLabelLanguageEntity aAppSysLabelLanguageEntity)
        {        
    		AppSysLabelLanguageDto aAppSysLabelLanguageDto = new AppSysLabelLanguageDto();
    		CopyEntityPropertyToDto( aAppSysLabelLanguageEntity, aAppSysLabelLanguageDto);          
			return aAppSysLabelLanguageDto;
        }
		 /// <summary>
        ///  Convert AppSysLabelLanguageEntity To  AppSysLabelLanguageExDto
        /// </summary>
        public static AppSysLabelLanguageExDto ConvertEntityToExDto(AppSysLabelLanguageEntity aAppSysLabelLanguageEntity)
        {        
    		AppSysLabelLanguageExDto aAppSysLabelLanguageExDto = new AppSysLabelLanguageExDto();
			CopyEntityPropertyToDto( aAppSysLabelLanguageEntity, aAppSysLabelLanguageExDto);
			
			
			
            return aAppSysLabelLanguageExDto;
        }
		
		 /// <summary>
        ///  Convert AppSysLabelLanguageEntity To  AppSysLabelLanguageDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSysLabelLanguageEntity aAppSysLabelLanguageEntity,AppSysLabelLanguageDto aAppSysLabelLanguageDto)
        {        
    		
           // aAppSysLabelLanguageDto.StopChangeTracking();
 			aAppSysLabelLanguageDto.Id = aAppSysLabelLanguageEntity.SysLableLanguageId;
 			aAppSysLabelLanguageDto.LanguageId = aAppSysLabelLanguageEntity.LanguageId;
 			aAppSysLabelLanguageDto.MenuId = aAppSysLabelLanguageEntity.MenuId;
 			aAppSysLabelLanguageDto.TransactionFieldId = aAppSysLabelLanguageEntity.TransactionFieldId;
 			aAppSysLabelLanguageDto.FormId = aAppSysLabelLanguageEntity.FormId;
 			aAppSysLabelLanguageDto.LanguageText = aAppSysLabelLanguageEntity.LanguageText;
 			aAppSysLabelLanguageDto.TransactionUnitLinkedSearchId = aAppSysLabelLanguageEntity.TransactionUnitLinkedSearchId;
 			aAppSysLabelLanguageDto.LinkTargetId = aAppSysLabelLanguageEntity.LinkTargetId;
 			aAppSysLabelLanguageDto.SearchViewFieldId = aAppSysLabelLanguageEntity.SearchViewFieldId;
 			aAppSysLabelLanguageDto.SearchFieldId = aAppSysLabelLanguageEntity.SearchFieldId;
 			aAppSysLabelLanguageDto.TransactionUnitId = aAppSysLabelLanguageEntity.TransactionUnitId;
 			aAppSysLabelLanguageDto.SearchViewId = aAppSysLabelLanguageEntity.SearchViewId;
 			aAppSysLabelLanguageDto.SearchId = aAppSysLabelLanguageEntity.SearchId;
 			aAppSysLabelLanguageDto.ApplicationId = aAppSysLabelLanguageEntity.ApplicationId;
 			aAppSysLabelLanguageDto.AppCreatedById = aAppSysLabelLanguageEntity.AppCreatedById;
 			aAppSysLabelLanguageDto.AppCreatedDate = aAppSysLabelLanguageEntity.AppCreatedDate;
 			aAppSysLabelLanguageDto.AppModifiedDate = aAppSysLabelLanguageEntity.AppModifiedDate;
 			aAppSysLabelLanguageDto.AppModifiedById = aAppSysLabelLanguageEntity.AppModifiedById;
 			aAppSysLabelLanguageDto.AppCreatedByCompanyId = aAppSysLabelLanguageEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSysLabelLanguageDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSysLabelLanguageEntity.AppCreatedDate);
                aAppSysLabelLanguageDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSysLabelLanguageEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSysLabelLanguageEntity, aAppSysLabelLanguageDto);
		}
		
		 /// <summary>
        ///  Copy AppSysLabelLanguageDto Properties to   AppSysLabelLanguageEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSysLabelLanguageEntity aAppSysLabelLanguageEntity,AppSysLabelLanguageDto aAppSysLabelLanguageDto)
        {        
 
      			aAppSysLabelLanguageEntity.LanguageId = aAppSysLabelLanguageDto.LanguageId;
      			aAppSysLabelLanguageEntity.MenuId = aAppSysLabelLanguageDto.MenuId;
      			aAppSysLabelLanguageEntity.TransactionFieldId = aAppSysLabelLanguageDto.TransactionFieldId;
      			aAppSysLabelLanguageEntity.FormId = aAppSysLabelLanguageDto.FormId;
      			aAppSysLabelLanguageEntity.LanguageText = aAppSysLabelLanguageDto.LanguageText;
      			aAppSysLabelLanguageEntity.TransactionUnitLinkedSearchId = aAppSysLabelLanguageDto.TransactionUnitLinkedSearchId;
      			aAppSysLabelLanguageEntity.LinkTargetId = aAppSysLabelLanguageDto.LinkTargetId;
      			aAppSysLabelLanguageEntity.SearchViewFieldId = aAppSysLabelLanguageDto.SearchViewFieldId;
      			aAppSysLabelLanguageEntity.SearchFieldId = aAppSysLabelLanguageDto.SearchFieldId;
      			aAppSysLabelLanguageEntity.TransactionUnitId = aAppSysLabelLanguageDto.TransactionUnitId;
      			aAppSysLabelLanguageEntity.SearchViewId = aAppSysLabelLanguageDto.SearchViewId;
      			aAppSysLabelLanguageEntity.SearchId = aAppSysLabelLanguageDto.SearchId;
      			aAppSysLabelLanguageEntity.ApplicationId = aAppSysLabelLanguageDto.ApplicationId;
 
  
   
    
      			aAppSysLabelLanguageEntity.AppCreatedByCompanyId = aAppSysLabelLanguageDto.AppCreatedByCompanyId;
			
			if(aAppSysLabelLanguageDto.Id == null)
			{
				aAppSysLabelLanguageEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSysLabelLanguageEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSysLabelLanguageEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSysLabelLanguageEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSysLabelLanguageEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSysLabelLanguageEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSysLabelLanguageEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSysLabelLanguageEntity, aAppSysLabelLanguageDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSysLabelLanguageEntity aAppSysLabelLanguageEntity,AppSysLabelLanguageDto aAppSysLabelLanguageDto);
		
		static partial void OnCopyDtoToEntityDone(AppSysLabelLanguageEntity aAppSysLabelLanguageEntity,AppSysLabelLanguageDto aAppSysLabelLanguageDto);
		
   
       
    }
}

 