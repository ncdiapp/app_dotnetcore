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
    /// Convert Properties between  AppProjectPortfolioBoardEntity and  AppProjectPortfolioBoardDto
    /// </summary>
    public static partial class AppProjectPortfolioBoardConverter 
    {
         /// <summary>
        ///  Convert AppProjectPortfolioBoardEntity To  AppProjectPortfolioBoardDto
        /// </summary>
        public static AppProjectPortfolioBoardDto ConvertEntityToDto(AppProjectPortfolioBoardEntity aAppProjectPortfolioBoardEntity)
        {        
    		AppProjectPortfolioBoardDto aAppProjectPortfolioBoardDto = new AppProjectPortfolioBoardDto();
    		CopyEntityPropertyToDto( aAppProjectPortfolioBoardEntity, aAppProjectPortfolioBoardDto);          
			return aAppProjectPortfolioBoardDto;
        }
		 /// <summary>
        ///  Convert AppProjectPortfolioBoardEntity To  AppProjectPortfolioBoardExDto
        /// </summary>
        public static AppProjectPortfolioBoardExDto ConvertEntityToExDto(AppProjectPortfolioBoardEntity aAppProjectPortfolioBoardEntity)
        {        
    		AppProjectPortfolioBoardExDto aAppProjectPortfolioBoardExDto = new AppProjectPortfolioBoardExDto();
			CopyEntityPropertyToDto( aAppProjectPortfolioBoardEntity, aAppProjectPortfolioBoardExDto);
			
			
			
            return aAppProjectPortfolioBoardExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectPortfolioBoardEntity To  AppProjectPortfolioBoardDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectPortfolioBoardEntity aAppProjectPortfolioBoardEntity,AppProjectPortfolioBoardDto aAppProjectPortfolioBoardDto)
        {        
    		
           // aAppProjectPortfolioBoardDto.StopChangeTracking();
 			aAppProjectPortfolioBoardDto.Id = aAppProjectPortfolioBoardEntity.BoardItmeId;
 			aAppProjectPortfolioBoardDto.PortfolioId = aAppProjectPortfolioBoardEntity.PortfolioId;
 			aAppProjectPortfolioBoardDto.SummaryProjectId = aAppProjectPortfolioBoardEntity.SummaryProjectId;
 			aAppProjectPortfolioBoardDto.AppCreatedById = aAppProjectPortfolioBoardEntity.AppCreatedById;
 			aAppProjectPortfolioBoardDto.AppCreatedDate = aAppProjectPortfolioBoardEntity.AppCreatedDate;
 			aAppProjectPortfolioBoardDto.AppModifiedDate = aAppProjectPortfolioBoardEntity.AppModifiedDate;
 			aAppProjectPortfolioBoardDto.AppModifiedById = aAppProjectPortfolioBoardEntity.AppModifiedById;
 			aAppProjectPortfolioBoardDto.AppCreatedByCompanyId = aAppProjectPortfolioBoardEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectPortfolioBoardDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPortfolioBoardEntity.AppCreatedDate);
                aAppProjectPortfolioBoardDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPortfolioBoardEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectPortfolioBoardEntity, aAppProjectPortfolioBoardDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectPortfolioBoardDto Properties to   AppProjectPortfolioBoardEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectPortfolioBoardEntity aAppProjectPortfolioBoardEntity,AppProjectPortfolioBoardDto aAppProjectPortfolioBoardDto)
        {        
 
      			aAppProjectPortfolioBoardEntity.PortfolioId = aAppProjectPortfolioBoardDto.PortfolioId;
      			aAppProjectPortfolioBoardEntity.SummaryProjectId = aAppProjectPortfolioBoardDto.SummaryProjectId;
 
  
   
    
      			aAppProjectPortfolioBoardEntity.AppCreatedByCompanyId = aAppProjectPortfolioBoardDto.AppCreatedByCompanyId;
			
			if(aAppProjectPortfolioBoardDto.Id == null)
			{
				aAppProjectPortfolioBoardEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPortfolioBoardEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectPortfolioBoardEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPortfolioBoardEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPortfolioBoardEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectPortfolioBoardEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPortfolioBoardEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectPortfolioBoardEntity, aAppProjectPortfolioBoardDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectPortfolioBoardEntity aAppProjectPortfolioBoardEntity,AppProjectPortfolioBoardDto aAppProjectPortfolioBoardDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectPortfolioBoardEntity aAppProjectPortfolioBoardEntity,AppProjectPortfolioBoardDto aAppProjectPortfolioBoardDto);
		
   
       
    }
}

 