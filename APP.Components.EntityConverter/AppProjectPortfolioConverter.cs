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
    /// Convert Properties between  AppProjectPortfolioEntity and  AppProjectPortfolioDto
    /// </summary>
    public static partial class AppProjectPortfolioConverter 
    {
         /// <summary>
        ///  Convert AppProjectPortfolioEntity To  AppProjectPortfolioDto
        /// </summary>
        public static AppProjectPortfolioDto ConvertEntityToDto(AppProjectPortfolioEntity aAppProjectPortfolioEntity)
        {        
    		AppProjectPortfolioDto aAppProjectPortfolioDto = new AppProjectPortfolioDto();
    		CopyEntityPropertyToDto( aAppProjectPortfolioEntity, aAppProjectPortfolioDto);          
			return aAppProjectPortfolioDto;
        }
		 /// <summary>
        ///  Convert AppProjectPortfolioEntity To  AppProjectPortfolioExDto
        /// </summary>
        public static AppProjectPortfolioExDto ConvertEntityToExDto(AppProjectPortfolioEntity aAppProjectPortfolioEntity)
        {        
    		AppProjectPortfolioExDto aAppProjectPortfolioExDto = new AppProjectPortfolioExDto();
			CopyEntityPropertyToDto( aAppProjectPortfolioEntity, aAppProjectPortfolioExDto);
			
			
			
            return aAppProjectPortfolioExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectPortfolioEntity To  AppProjectPortfolioDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectPortfolioEntity aAppProjectPortfolioEntity,AppProjectPortfolioDto aAppProjectPortfolioDto)
        {        
    		
           // aAppProjectPortfolioDto.StopChangeTracking();
 			aAppProjectPortfolioDto.Id = aAppProjectPortfolioEntity.PortfolioId;
 			aAppProjectPortfolioDto.PortfilioName = aAppProjectPortfolioEntity.PortfilioName;
 			aAppProjectPortfolioDto.Description = aAppProjectPortfolioEntity.Description;
 			aAppProjectPortfolioDto.AppCreatedById = aAppProjectPortfolioEntity.AppCreatedById;
 			aAppProjectPortfolioDto.AppCreatedDate = aAppProjectPortfolioEntity.AppCreatedDate;
 			aAppProjectPortfolioDto.AppModifiedDate = aAppProjectPortfolioEntity.AppModifiedDate;
 			aAppProjectPortfolioDto.AppModifiedById = aAppProjectPortfolioEntity.AppModifiedById;
 			aAppProjectPortfolioDto.AppCreatedByCompanyId = aAppProjectPortfolioEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectPortfolioDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPortfolioEntity.AppCreatedDate);
                aAppProjectPortfolioDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPortfolioEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectPortfolioEntity, aAppProjectPortfolioDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectPortfolioDto Properties to   AppProjectPortfolioEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectPortfolioEntity aAppProjectPortfolioEntity,AppProjectPortfolioDto aAppProjectPortfolioDto)
        {        
 
      			aAppProjectPortfolioEntity.PortfilioName = aAppProjectPortfolioDto.PortfilioName;
      			aAppProjectPortfolioEntity.Description = aAppProjectPortfolioDto.Description;
 
  
   
    
      			aAppProjectPortfolioEntity.AppCreatedByCompanyId = aAppProjectPortfolioDto.AppCreatedByCompanyId;
			
			if(aAppProjectPortfolioDto.Id == null)
			{
				aAppProjectPortfolioEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPortfolioEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectPortfolioEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPortfolioEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPortfolioEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectPortfolioEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPortfolioEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectPortfolioEntity, aAppProjectPortfolioDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectPortfolioEntity aAppProjectPortfolioEntity,AppProjectPortfolioDto aAppProjectPortfolioDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectPortfolioEntity aAppProjectPortfolioEntity,AppProjectPortfolioDto aAppProjectPortfolioDto);
		
   
       
    }
}

 