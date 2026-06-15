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
    /// Convert Properties between  AppFormGridLayoutItemBindFieldEntity and  AppFormGridLayoutItemBindFieldDto
    /// </summary>
    public static partial class AppFormGridLayoutItemBindFieldConverter 
    {
         /// <summary>
        ///  Convert AppFormGridLayoutItemBindFieldEntity To  AppFormGridLayoutItemBindFieldDto
        /// </summary>
        public static AppFormGridLayoutItemBindFieldDto ConvertEntityToDto(AppFormGridLayoutItemBindFieldEntity aAppFormGridLayoutItemBindFieldEntity)
        {        
    		AppFormGridLayoutItemBindFieldDto aAppFormGridLayoutItemBindFieldDto = new AppFormGridLayoutItemBindFieldDto();
    		CopyEntityPropertyToDto( aAppFormGridLayoutItemBindFieldEntity, aAppFormGridLayoutItemBindFieldDto);          
			return aAppFormGridLayoutItemBindFieldDto;
        }
		 /// <summary>
        ///  Convert AppFormGridLayoutItemBindFieldEntity To  AppFormGridLayoutItemBindFieldExDto
        /// </summary>
        public static AppFormGridLayoutItemBindFieldExDto ConvertEntityToExDto(AppFormGridLayoutItemBindFieldEntity aAppFormGridLayoutItemBindFieldEntity)
        {        
    		AppFormGridLayoutItemBindFieldExDto aAppFormGridLayoutItemBindFieldExDto = new AppFormGridLayoutItemBindFieldExDto();
			CopyEntityPropertyToDto( aAppFormGridLayoutItemBindFieldEntity, aAppFormGridLayoutItemBindFieldExDto);
			
			
			
            return aAppFormGridLayoutItemBindFieldExDto;
        }
		
		 /// <summary>
        ///  Convert AppFormGridLayoutItemBindFieldEntity To  AppFormGridLayoutItemBindFieldDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppFormGridLayoutItemBindFieldEntity aAppFormGridLayoutItemBindFieldEntity,AppFormGridLayoutItemBindFieldDto aAppFormGridLayoutItemBindFieldDto)
        {        
    		
           // aAppFormGridLayoutItemBindFieldDto.StopChangeTracking();
 			aAppFormGridLayoutItemBindFieldDto.Id = aAppFormGridLayoutItemBindFieldEntity.LayoutBindFieldId;
 			aAppFormGridLayoutItemBindFieldDto.FormLayoutId = aAppFormGridLayoutItemBindFieldEntity.FormLayoutId;
 			aAppFormGridLayoutItemBindFieldDto.TransactionField = aAppFormGridLayoutItemBindFieldEntity.TransactionField;
 			aAppFormGridLayoutItemBindFieldDto.AliasName = aAppFormGridLayoutItemBindFieldEntity.AliasName;
 			aAppFormGridLayoutItemBindFieldDto.Width = aAppFormGridLayoutItemBindFieldEntity.Width;
 			aAppFormGridLayoutItemBindFieldDto.Height = aAppFormGridLayoutItemBindFieldEntity.Height;
 			aAppFormGridLayoutItemBindFieldDto.Visible = aAppFormGridLayoutItemBindFieldEntity.Visible;
 			aAppFormGridLayoutItemBindFieldDto.ChildTransactionUnitId = aAppFormGridLayoutItemBindFieldEntity.ChildTransactionUnitId;
 			aAppFormGridLayoutItemBindFieldDto.GrandChildTransactionUnitId = aAppFormGridLayoutItemBindFieldEntity.GrandChildTransactionUnitId;
 			aAppFormGridLayoutItemBindFieldDto.AppCreatedById = aAppFormGridLayoutItemBindFieldEntity.AppCreatedById;
 			aAppFormGridLayoutItemBindFieldDto.AppCreatedDate = aAppFormGridLayoutItemBindFieldEntity.AppCreatedDate;
 			aAppFormGridLayoutItemBindFieldDto.AppModifiedDate = aAppFormGridLayoutItemBindFieldEntity.AppModifiedDate;
 			aAppFormGridLayoutItemBindFieldDto.AppModifiedById = aAppFormGridLayoutItemBindFieldEntity.AppModifiedById;
 			aAppFormGridLayoutItemBindFieldDto.AppCreatedByCompanyId = aAppFormGridLayoutItemBindFieldEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppFormGridLayoutItemBindFieldDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormGridLayoutItemBindFieldEntity.AppCreatedDate);
                aAppFormGridLayoutItemBindFieldDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormGridLayoutItemBindFieldEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppFormGridLayoutItemBindFieldEntity, aAppFormGridLayoutItemBindFieldDto);
		}
		
		 /// <summary>
        ///  Copy AppFormGridLayoutItemBindFieldDto Properties to   AppFormGridLayoutItemBindFieldEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppFormGridLayoutItemBindFieldEntity aAppFormGridLayoutItemBindFieldEntity,AppFormGridLayoutItemBindFieldDto aAppFormGridLayoutItemBindFieldDto)
        {        
 
      			aAppFormGridLayoutItemBindFieldEntity.FormLayoutId = aAppFormGridLayoutItemBindFieldDto.FormLayoutId;
      			aAppFormGridLayoutItemBindFieldEntity.TransactionField = aAppFormGridLayoutItemBindFieldDto.TransactionField;
      			aAppFormGridLayoutItemBindFieldEntity.AliasName = aAppFormGridLayoutItemBindFieldDto.AliasName;
      			aAppFormGridLayoutItemBindFieldEntity.Width = aAppFormGridLayoutItemBindFieldDto.Width;
      			aAppFormGridLayoutItemBindFieldEntity.Height = aAppFormGridLayoutItemBindFieldDto.Height;
      			aAppFormGridLayoutItemBindFieldEntity.Visible = aAppFormGridLayoutItemBindFieldDto.Visible;
      			aAppFormGridLayoutItemBindFieldEntity.ChildTransactionUnitId = aAppFormGridLayoutItemBindFieldDto.ChildTransactionUnitId;
      			aAppFormGridLayoutItemBindFieldEntity.GrandChildTransactionUnitId = aAppFormGridLayoutItemBindFieldDto.GrandChildTransactionUnitId;
 
  
   
    
      			aAppFormGridLayoutItemBindFieldEntity.AppCreatedByCompanyId = aAppFormGridLayoutItemBindFieldDto.AppCreatedByCompanyId;
			
			if(aAppFormGridLayoutItemBindFieldDto.Id == null)
			{
				aAppFormGridLayoutItemBindFieldEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormGridLayoutItemBindFieldEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppFormGridLayoutItemBindFieldEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormGridLayoutItemBindFieldEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormGridLayoutItemBindFieldEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppFormGridLayoutItemBindFieldEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormGridLayoutItemBindFieldEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppFormGridLayoutItemBindFieldEntity, aAppFormGridLayoutItemBindFieldDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppFormGridLayoutItemBindFieldEntity aAppFormGridLayoutItemBindFieldEntity,AppFormGridLayoutItemBindFieldDto aAppFormGridLayoutItemBindFieldDto);
		
		static partial void OnCopyDtoToEntityDone(AppFormGridLayoutItemBindFieldEntity aAppFormGridLayoutItemBindFieldEntity,AppFormGridLayoutItemBindFieldDto aAppFormGridLayoutItemBindFieldDto);
		
   
       
    }
}

 