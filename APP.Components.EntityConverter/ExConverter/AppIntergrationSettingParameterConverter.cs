using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.EntityClasses;
using APP.Components.EntityDto;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityConverter
{

    public static partial class AppIntergrationSettingParameterConverter
    {

        static partial void OnCopyEntityToDtoDone(AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity, AppIntergrationSettingParameterDto aAppIntergrationSettingParameterDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppIntergrationSettingParameterEntity.ApiconfigParameters))
            {
                try
                {
                    aAppIntergrationSettingParameterDto.APIConfigParameters = JsonConvert.DeserializeObject<APIConfigParameterDTO>(aAppIntergrationSettingParameterEntity.ApiconfigParameters);

                    aAppIntergrationSettingParameterDto.APIConfigParameters.Method = EnumHttpMethod.Get;

                    if (!string.IsNullOrWhiteSpace(aAppIntergrationSettingParameterDto.HttpMethd))
                    {
                        if (aAppIntergrationSettingParameterDto.HttpMethd == "Get")
                        {
                            aAppIntergrationSettingParameterDto.APIConfigParameters.Method = EnumHttpMethod.Get;
                        }
                        else if (aAppIntergrationSettingParameterDto.HttpMethd == "Post")
                        {
                            aAppIntergrationSettingParameterDto.APIConfigParameters.Method = EnumHttpMethod.Post;
                        }
                        else if (aAppIntergrationSettingParameterDto.HttpMethd == "Put")
                        {
                            aAppIntergrationSettingParameterDto.APIConfigParameters.Method = EnumHttpMethod.Put;
                        }
                        else if (aAppIntergrationSettingParameterDto.HttpMethd == "Delete")
                        {
                            aAppIntergrationSettingParameterDto.APIConfigParameters.Method = EnumHttpMethod.Delete;
                        }
                    }
                 
                }
                catch
                {
                    aAppIntergrationSettingParameterDto.APIConfigParameters = null;
                }

            }
            else
            {
                if (aAppIntergrationSettingParameterEntity.IsSimpleQuery.HasValue && aAppIntergrationSettingParameterEntity.IsSimpleQuery.Value)
                {
                    aAppIntergrationSettingParameterDto.APIConfigParameters = new APIConfigParameterDTO();
                }
                else
                {
                    aAppIntergrationSettingParameterDto.APIConfigParameters = null;
                }                
            }

            if (!string.IsNullOrWhiteSpace(aAppIntergrationSettingParameterEntity.PostProcessScript))
            {
                try
                {
                    aAppIntergrationSettingParameterDto.PostResponseDto = JsonConvert.DeserializeObject<APIPostResponseDto>(aAppIntergrationSettingParameterEntity.PostProcessScript);
                }
                catch
                {
                    aAppIntergrationSettingParameterDto.PostResponseDto = new APIPostResponseDto();
                }

            }
            else
            {
                aAppIntergrationSettingParameterDto.PostResponseDto = new APIPostResponseDto();
            }




            if (!string.IsNullOrWhiteSpace(aAppIntergrationSettingParameterEntity.DefaultValue))
            {
                try
                {
                    aAppIntergrationSettingParameterDto.OtherSettingsDto = JsonConvert.DeserializeObject<AppIntergrationParameterOtherSettingsDto>(aAppIntergrationSettingParameterEntity.DefaultValue);                                        
                }
                catch
                {
                    aAppIntergrationSettingParameterDto.OtherSettingsDto = new AppIntergrationParameterOtherSettingsDto();
                }

            }
            else
            {
                aAppIntergrationSettingParameterDto.OtherSettingsDto = new AppIntergrationParameterOtherSettingsDto();
            }

            if (!string.IsNullOrWhiteSpace(aAppIntergrationSettingParameterEntity.SchemaDataSetMapping))
            {
                try
                {
                    aAppIntergrationSettingParameterDto.SchemaDataSetMappingDto = JsonConvert.DeserializeObject<AppIntergrationSchemaDataSetMappingDto>(aAppIntergrationSettingParameterEntity.SchemaDataSetMapping);
                }
                catch
                {
                    aAppIntergrationSettingParameterDto.SchemaDataSetMappingDto = null;
                }

            }
            else
            {
                aAppIntergrationSettingParameterDto.SchemaDataSetMappingDto = null;
            }

            if (aAppIntergrationSettingParameterEntity.IsSimpleQuery.HasValue && aAppIntergrationSettingParameterEntity.IsSimpleQuery.Value)
            {
                aAppIntergrationSettingParameterDto.SimpleQueryParameterNameList = new List<string>();

                if (aAppIntergrationSettingParameterDto.APIConfigParameters != null && aAppIntergrationSettingParameterDto.APIConfigParameters.QueryParams != null)
                {
                    foreach (string key in aAppIntergrationSettingParameterDto.APIConfigParameters.QueryParams.Keys)
                    {
                        aAppIntergrationSettingParameterDto.SimpleQueryParameterNameList.Add(key);
                    }
                }
                
            }

        }

        static partial void OnCopyDtoToEntityDone(AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity, AppIntergrationSettingParameterDto aAppIntergrationSettingParameterDto)
        {
            //try
            //{
            //    aAppIntergrationSettingParameterEntity.ApiconfigParameters = JsonConvert.SerializeObject(aAppIntergrationSettingParameterDto.APIConfigParameters);
            //}
            //catch
            //{
            //    aAppIntergrationSettingParameterEntity.ApiconfigParameters = string.Empty;
            //}

            try
            {
                aAppIntergrationSettingParameterEntity.PostProcessScript = JsonConvert.SerializeObject(aAppIntergrationSettingParameterDto.PostResponseDto);
            }
            catch
            {
                aAppIntergrationSettingParameterEntity.PostProcessScript = string.Empty;
            }

            try
            {
                aAppIntergrationSettingParameterEntity.DefaultValue = JsonConvert.SerializeObject(aAppIntergrationSettingParameterDto.OtherSettingsDto);
            }
            catch
            {
                aAppIntergrationSettingParameterEntity.DefaultValue = string.Empty;
            }

            try
            {
                aAppIntergrationSettingParameterEntity.SchemaDataSetMapping = JsonConvert.SerializeObject(aAppIntergrationSettingParameterDto.SchemaDataSetMappingDto);
            }
            catch
            {
                aAppIntergrationSettingParameterEntity.SchemaDataSetMapping = string.Empty;
            }

            if (aAppIntergrationSettingParameterDto.IsSimpleQuery.HasValue && aAppIntergrationSettingParameterDto.IsSimpleQuery.Value)
            {
                aAppIntergrationSettingParameterDto.APIConfigParameters = new APIConfigParameterDTO();
                aAppIntergrationSettingParameterDto.APIConfigParameters.QueryParams = new Dictionary<string, string>();

                if (aAppIntergrationSettingParameterDto.SimpleQueryParameterNameList != null)
                {                    
                    aAppIntergrationSettingParameterDto.APIConfigParameters.QueryParams = aAppIntergrationSettingParameterDto.SimpleQueryParameterNameList.Distinct().ToDictionary(o => o, o=>"");
                }

                try
                {
                    aAppIntergrationSettingParameterEntity.ApiconfigParameters = JsonConvert.SerializeObject(aAppIntergrationSettingParameterDto.APIConfigParameters);
                }
                catch
                {
                    aAppIntergrationSettingParameterEntity.SchemaDataSetMapping = string.Empty;
                }


            }
        }
    }
}

