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
    /// Convert Properties between  AppDatabaseDiagramItemEntity and  AppDatabaseDiagramItemDto
    /// </summary>
    public static partial class AppDatabaseDiagramItemConverter 
    {
         /// <summary>
        ///  Convert AppDatabaseDiagramItemEntity To  AppDatabaseDiagramItemDto
        /// </summary>
        public static AppDatabaseDiagramItemDto ConvertEntityToDto(AppDatabaseDiagramItemEntity aAppDatabaseDiagramItemEntity)
        {        
    		AppDatabaseDiagramItemDto aAppDatabaseDiagramItemDto = new AppDatabaseDiagramItemDto();
    		CopyEntityPropertyToDto( aAppDatabaseDiagramItemEntity, aAppDatabaseDiagramItemDto);          
			return aAppDatabaseDiagramItemDto;
        }
		 /// <summary>
        ///  Convert AppDatabaseDiagramItemEntity To  AppDatabaseDiagramItemExDto
        /// </summary>
        public static AppDatabaseDiagramItemExDto ConvertEntityToExDto(AppDatabaseDiagramItemEntity aAppDatabaseDiagramItemEntity)
        {        
    		AppDatabaseDiagramItemExDto aAppDatabaseDiagramItemExDto = new AppDatabaseDiagramItemExDto();
			CopyEntityPropertyToDto( aAppDatabaseDiagramItemEntity, aAppDatabaseDiagramItemExDto);
			
			
			
            return aAppDatabaseDiagramItemExDto;
        }
		
		 /// <summary>
        ///  Convert AppDatabaseDiagramItemEntity To  AppDatabaseDiagramItemDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppDatabaseDiagramItemEntity aAppDatabaseDiagramItemEntity,AppDatabaseDiagramItemDto aAppDatabaseDiagramItemDto)
        {        
    		
           // aAppDatabaseDiagramItemDto.StopChangeTracking();
 			aAppDatabaseDiagramItemDto.Id = aAppDatabaseDiagramItemEntity.DiagramItemId;
 			aAppDatabaseDiagramItemDto.DiagramId = aAppDatabaseDiagramItemEntity.DiagramId;
 			aAppDatabaseDiagramItemDto.Name = aAppDatabaseDiagramItemEntity.Name;
 			aAppDatabaseDiagramItemDto.Description = aAppDatabaseDiagramItemEntity.Description;
 			aAppDatabaseDiagramItemDto.ItemType = aAppDatabaseDiagramItemEntity.ItemType;
 			aAppDatabaseDiagramItemDto.PositionX = aAppDatabaseDiagramItemEntity.PositionX;
 			aAppDatabaseDiagramItemDto.PositionY = aAppDatabaseDiagramItemEntity.PositionY;
 			aAppDatabaseDiagramItemDto.Height = aAppDatabaseDiagramItemEntity.Height;
 			aAppDatabaseDiagramItemDto.Width = aAppDatabaseDiagramItemEntity.Width;
 			aAppDatabaseDiagramItemDto.AppCreatedById = aAppDatabaseDiagramItemEntity.AppCreatedById;
 			aAppDatabaseDiagramItemDto.AppCreatedDate = aAppDatabaseDiagramItemEntity.AppCreatedDate;
 			aAppDatabaseDiagramItemDto.AppModifiedDate = aAppDatabaseDiagramItemEntity.AppModifiedDate;
 			aAppDatabaseDiagramItemDto.AppModifiedById = aAppDatabaseDiagramItemEntity.AppModifiedById;
 			aAppDatabaseDiagramItemDto.AppCreatedByCompanyId = aAppDatabaseDiagramItemEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppDatabaseDiagramItemDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDatabaseDiagramItemEntity.AppCreatedDate);
                aAppDatabaseDiagramItemDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDatabaseDiagramItemEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppDatabaseDiagramItemEntity, aAppDatabaseDiagramItemDto);
		}
		
		 /// <summary>
        ///  Copy AppDatabaseDiagramItemDto Properties to   AppDatabaseDiagramItemEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppDatabaseDiagramItemEntity aAppDatabaseDiagramItemEntity,AppDatabaseDiagramItemDto aAppDatabaseDiagramItemDto)
        {        
 
      			aAppDatabaseDiagramItemEntity.DiagramId = aAppDatabaseDiagramItemDto.DiagramId;
      			aAppDatabaseDiagramItemEntity.Name = aAppDatabaseDiagramItemDto.Name;
      			aAppDatabaseDiagramItemEntity.Description = aAppDatabaseDiagramItemDto.Description;
      			aAppDatabaseDiagramItemEntity.ItemType = aAppDatabaseDiagramItemDto.ItemType;
      			aAppDatabaseDiagramItemEntity.PositionX = aAppDatabaseDiagramItemDto.PositionX;
      			aAppDatabaseDiagramItemEntity.PositionY = aAppDatabaseDiagramItemDto.PositionY;
      			aAppDatabaseDiagramItemEntity.Height = aAppDatabaseDiagramItemDto.Height;
      			aAppDatabaseDiagramItemEntity.Width = aAppDatabaseDiagramItemDto.Width;
 
  
   
    
      			aAppDatabaseDiagramItemEntity.AppCreatedByCompanyId = aAppDatabaseDiagramItemDto.AppCreatedByCompanyId;
			
			if(aAppDatabaseDiagramItemDto.Id == null)
			{
				aAppDatabaseDiagramItemEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppDatabaseDiagramItemEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppDatabaseDiagramItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDatabaseDiagramItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppDatabaseDiagramItemEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppDatabaseDiagramItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDatabaseDiagramItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppDatabaseDiagramItemEntity, aAppDatabaseDiagramItemDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppDatabaseDiagramItemEntity aAppDatabaseDiagramItemEntity,AppDatabaseDiagramItemDto aAppDatabaseDiagramItemDto);
		
		static partial void OnCopyDtoToEntityDone(AppDatabaseDiagramItemEntity aAppDatabaseDiagramItemEntity,AppDatabaseDiagramItemDto aAppDatabaseDiagramItemDto);
		
   
       
    }
}

 