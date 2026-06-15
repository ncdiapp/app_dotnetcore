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
    /// Convert Properties between  AppIntergrationSettingParameterEntity and  AppIntergrationSettingParameterDto
    /// </summary>
    public static partial class AppIntergrationSettingParameterConverter 
    {
         /// <summary>
        ///  Convert AppIntergrationSettingParameterEntity To  AppIntergrationSettingParameterDto
        /// </summary>
        public static AppIntergrationSettingParameterDto ConvertEntityToDto(AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity)
        {        
    		AppIntergrationSettingParameterDto aAppIntergrationSettingParameterDto = new AppIntergrationSettingParameterDto();
    		CopyEntityPropertyToDto( aAppIntergrationSettingParameterEntity, aAppIntergrationSettingParameterDto);          
			return aAppIntergrationSettingParameterDto;
        }
		 /// <summary>
        ///  Convert AppIntergrationSettingParameterEntity To  AppIntergrationSettingParameterExDto
        /// </summary>
        public static AppIntergrationSettingParameterExDto ConvertEntityToExDto(AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity)
        {        
    		AppIntergrationSettingParameterExDto aAppIntergrationSettingParameterExDto = new AppIntergrationSettingParameterExDto();
			CopyEntityPropertyToDto( aAppIntergrationSettingParameterEntity, aAppIntergrationSettingParameterExDto);
			
			
			
            return aAppIntergrationSettingParameterExDto;
        }
		
		 /// <summary>
        ///  Convert AppIntergrationSettingParameterEntity To  AppIntergrationSettingParameterDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity,AppIntergrationSettingParameterDto aAppIntergrationSettingParameterDto)
        {        
    		
           // aAppIntergrationSettingParameterDto.StopChangeTracking();
 			aAppIntergrationSettingParameterDto.Id = aAppIntergrationSettingParameterEntity.SettingParameterId;
 			aAppIntergrationSettingParameterDto.IntergrationSettingId = aAppIntergrationSettingParameterEntity.IntergrationSettingId;
 			aAppIntergrationSettingParameterDto.MappingInternalCode = aAppIntergrationSettingParameterEntity.MappingInternalCode;
 			aAppIntergrationSettingParameterDto.InternalFiledName = aAppIntergrationSettingParameterEntity.InternalFiledName;
 			aAppIntergrationSettingParameterDto.ExternalFieldName = aAppIntergrationSettingParameterEntity.ExternalFieldName;
 			aAppIntergrationSettingParameterDto.TranscationId = aAppIntergrationSettingParameterEntity.TranscationId;
 			aAppIntergrationSettingParameterDto.TranscationFieId = aAppIntergrationSettingParameterEntity.TranscationFieId;
 			aAppIntergrationSettingParameterDto.DefaultValue = aAppIntergrationSettingParameterEntity.DefaultValue;
 			aAppIntergrationSettingParameterDto.ValidationRule = aAppIntergrationSettingParameterEntity.ValidationRule;
 			aAppIntergrationSettingParameterDto.ActionCode = aAppIntergrationSettingParameterEntity.ActionCode;
 			aAppIntergrationSettingParameterDto.ActionDescription = aAppIntergrationSettingParameterEntity.ActionDescription;
 			aAppIntergrationSettingParameterDto.JsonQuery = aAppIntergrationSettingParameterEntity.JsonQuery;
 			aAppIntergrationSettingParameterDto.WhereClauseFormat = aAppIntergrationSettingParameterEntity.WhereClauseFormat;
 			aAppIntergrationSettingParameterDto.IsSimpleQuery = aAppIntergrationSettingParameterEntity.IsSimpleQuery;
 			aAppIntergrationSettingParameterDto.JsonSampleData = aAppIntergrationSettingParameterEntity.JsonSampleData;
 			aAppIntergrationSettingParameterDto.JsonSchema = aAppIntergrationSettingParameterEntity.JsonSchema;
 			aAppIntergrationSettingParameterDto.SchemaDataSetMapping = aAppIntergrationSettingParameterEntity.SchemaDataSetMapping;
 			aAppIntergrationSettingParameterDto.HttpMethd = aAppIntergrationSettingParameterEntity.HttpMethd;
 			aAppIntergrationSettingParameterDto.DataSourceId = aAppIntergrationSettingParameterEntity.DataSourceId;
 			aAppIntergrationSettingParameterDto.SchemaFromDataSetMapping = aAppIntergrationSettingParameterEntity.SchemaFromDataSetMapping;
 			aAppIntergrationSettingParameterDto.PostProcessScript = aAppIntergrationSettingParameterEntity.PostProcessScript;
 			aAppIntergrationSettingParameterDto.ApiconfigParameters = aAppIntergrationSettingParameterEntity.ApiconfigParameters;
 			aAppIntergrationSettingParameterDto.TablePrefix = aAppIntergrationSettingParameterEntity.TablePrefix;
 			aAppIntergrationSettingParameterDto.AppCreatedById = aAppIntergrationSettingParameterEntity.AppCreatedById;
 			aAppIntergrationSettingParameterDto.AppCreatedDate = aAppIntergrationSettingParameterEntity.AppCreatedDate;
 			aAppIntergrationSettingParameterDto.AppModifiedDate = aAppIntergrationSettingParameterEntity.AppModifiedDate;
 			aAppIntergrationSettingParameterDto.AppModifiedById = aAppIntergrationSettingParameterEntity.AppModifiedById;
 			aAppIntergrationSettingParameterDto.AppCreatedByCompanyId = aAppIntergrationSettingParameterEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppIntergrationSettingParameterDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppIntergrationSettingParameterEntity.AppCreatedDate);
                aAppIntergrationSettingParameterDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppIntergrationSettingParameterEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppIntergrationSettingParameterEntity, aAppIntergrationSettingParameterDto);
		}
		
		 /// <summary>
        ///  Copy AppIntergrationSettingParameterDto Properties to   AppIntergrationSettingParameterEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity,AppIntergrationSettingParameterDto aAppIntergrationSettingParameterDto)
        {        
 
      			aAppIntergrationSettingParameterEntity.IntergrationSettingId = aAppIntergrationSettingParameterDto.IntergrationSettingId;
      			aAppIntergrationSettingParameterEntity.MappingInternalCode = aAppIntergrationSettingParameterDto.MappingInternalCode;
      			aAppIntergrationSettingParameterEntity.InternalFiledName = aAppIntergrationSettingParameterDto.InternalFiledName;
      			aAppIntergrationSettingParameterEntity.ExternalFieldName = aAppIntergrationSettingParameterDto.ExternalFieldName;
      			aAppIntergrationSettingParameterEntity.TranscationId = aAppIntergrationSettingParameterDto.TranscationId;
      			aAppIntergrationSettingParameterEntity.TranscationFieId = aAppIntergrationSettingParameterDto.TranscationFieId;
      			aAppIntergrationSettingParameterEntity.DefaultValue = aAppIntergrationSettingParameterDto.DefaultValue;
      			aAppIntergrationSettingParameterEntity.ValidationRule = aAppIntergrationSettingParameterDto.ValidationRule;
      			aAppIntergrationSettingParameterEntity.ActionCode = aAppIntergrationSettingParameterDto.ActionCode;
      			aAppIntergrationSettingParameterEntity.ActionDescription = aAppIntergrationSettingParameterDto.ActionDescription;
      			aAppIntergrationSettingParameterEntity.JsonQuery = aAppIntergrationSettingParameterDto.JsonQuery;
      			aAppIntergrationSettingParameterEntity.WhereClauseFormat = aAppIntergrationSettingParameterDto.WhereClauseFormat;
      			aAppIntergrationSettingParameterEntity.IsSimpleQuery = aAppIntergrationSettingParameterDto.IsSimpleQuery;
      			aAppIntergrationSettingParameterEntity.JsonSampleData = aAppIntergrationSettingParameterDto.JsonSampleData;
      			aAppIntergrationSettingParameterEntity.JsonSchema = aAppIntergrationSettingParameterDto.JsonSchema;
      			aAppIntergrationSettingParameterEntity.SchemaDataSetMapping = aAppIntergrationSettingParameterDto.SchemaDataSetMapping;
      			aAppIntergrationSettingParameterEntity.HttpMethd = aAppIntergrationSettingParameterDto.HttpMethd;
      			aAppIntergrationSettingParameterEntity.DataSourceId = aAppIntergrationSettingParameterDto.DataSourceId;
      			aAppIntergrationSettingParameterEntity.SchemaFromDataSetMapping = aAppIntergrationSettingParameterDto.SchemaFromDataSetMapping;
      			aAppIntergrationSettingParameterEntity.PostProcessScript = aAppIntergrationSettingParameterDto.PostProcessScript;
      			aAppIntergrationSettingParameterEntity.ApiconfigParameters = aAppIntergrationSettingParameterDto.ApiconfigParameters;
      			aAppIntergrationSettingParameterEntity.TablePrefix = aAppIntergrationSettingParameterDto.TablePrefix;
 
  
   
    
      			aAppIntergrationSettingParameterEntity.AppCreatedByCompanyId = aAppIntergrationSettingParameterDto.AppCreatedByCompanyId;
			
			if(aAppIntergrationSettingParameterDto.Id == null)
			{
				aAppIntergrationSettingParameterEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppIntergrationSettingParameterEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppIntergrationSettingParameterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppIntergrationSettingParameterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppIntergrationSettingParameterEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppIntergrationSettingParameterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppIntergrationSettingParameterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppIntergrationSettingParameterEntity, aAppIntergrationSettingParameterDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity,AppIntergrationSettingParameterDto aAppIntergrationSettingParameterDto);
		
		static partial void OnCopyDtoToEntityDone(AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity,AppIntergrationSettingParameterDto aAppIntergrationSettingParameterDto);
		
   
       
    }
}

 