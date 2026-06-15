using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using DatabaseSchemaMrg;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.Serialization;
using Twilio.TwiML.Voice;

using APP.Framework;
namespace App.BL
{
    public static class AppThemeBL
    {
        public static List<AppUserThemeDto> RetrieveAvailableUserDefineThemeDtoList()
        {
            List<AppUserThemeDto> toReturn = new List<AppUserThemeDto>();
                      
            int currentUserId = AppSecurityUserBL.CurrentUserId;

            string query = $@"
                SELECT [ThemeID], [ThemeName], [Description], [ThemeDetails], [IsForAllUsers], [AppCreatedByID]
                FROM [appUserDefineTheme]
                WHERE ([IsForAllUsers] is not null and [IsForAllUsers] = 1) OR ([AppCreatedByID] is not null and [AppCreatedByID] = {currentUserId})";

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    var dataTable = adpater.ExecuteDataTableRetrievalQuery(query, new List<SqlParameter>());

                    foreach (DataRow aRow in dataTable.Rows)
                    {
                        AppUserThemeDto dto = new AppUserThemeDto
                        {
                            Id = ControlTypeValueConverter.ConvertValueToInt(aRow["ThemeID"]),
                            ThemeName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aRow["ThemeName"]),
                            Description = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aRow["Description"]),
                            IsForAllUsers = ControlTypeValueConverter.ConvertValueToBoolean(aRow["IsForAllUsers"]),
                            AppCreatedById = ControlTypeValueConverter.ConvertValueToInt(aRow["AppCreatedByID"]),
                            DictThemeDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aRow["ThemeDetails"]))
                        };

                        toReturn.Add(dto);
                    }

                }
                catch (Exception ex)
                {
                                        
                }
            }


            
            return toReturn;
        }

        public static AppUserThemeDto RetrieveOneAppUserDefineThemeDto(int? themeId)
        {
            if (!themeId.HasValue)
                return null;

            int? defaultDataSourceRegId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            DatabaseFixture fixture = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(defaultDataSourceRegId, null);

            string query = @"SELECT [ThemeID], [ThemeName], [Description], [ThemeDetails], [IsForAllUsers], [AppCreatedByID]
                         FROM [AppUserDefineTheme]
                         WHERE ThemeID = @ThemeID";     

            List<SqlParameter> parameters = new List<SqlParameter>();

            SqlParameter themeIdParam = new SqlParameter("@ThemeID", SqlDbType.Int);
            themeIdParam.Value = themeId.Value;
            parameters.Add(themeIdParam);

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    var dt = adpater.ExecuteDataTableRetrievalQuery(query, parameters);

                    if (dt.Rows.Count > 0)
                    {

                        DataRow aRow = dt.Rows[0];

                        AppUserThemeDto dto = new AppUserThemeDto
                        {
                            Id = ControlTypeValueConverter.ConvertValueToInt(aRow["ThemeID"]),
                            ThemeName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aRow["ThemeName"]),
                            Description = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aRow["Description"]),
                            IsForAllUsers = ControlTypeValueConverter.ConvertValueToBoolean(aRow["IsForAllUsers"]),
                            AppCreatedById = ControlTypeValueConverter.ConvertValueToInt(aRow["AppCreatedByID"]),
                            DictThemeDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aRow["ThemeDetails"]))
                        };

                        return dto;
                    }

                }
                catch (Exception ex)
                {

                }
            }

            return null;
        }

        public static OperationCallResult<AppUserThemeDto> SaveOneAppUserDefineThemeDto(AppUserThemeDto themeDto)
        {
            OperationCallResult<AppUserThemeDto> result = new OperationCallResult<AppUserThemeDto> { ValidationResult = new ValidationResult() };

            if (themeDto != null)
            {

                try
                {
                    string themeDetailsJson = themeDto.DictThemeDetails != null
                        ? JsonConvert.SerializeObject(themeDto.DictThemeDetails)
                        : string.Empty;

                    int? themeId = ControlTypeValueConverter.ConvertValueToInt(themeDto.Id);
                    bool isUpdate = themeId.HasValue;

                    string query = string.Empty;

                    if (isUpdate)
                    {
                        // Update existing theme
                        query = @"
                            UPDATE [appUserDefineTheme]
                            SET 
                                ThemeName = @ThemeName,
                                Description = @Description,
                                ThemeDetails = @ThemeDetails,
                                IsForAllUsers = @IsForAllUsers,
                                AppCreatedByID = @AppCreatedByID
                            WHERE ThemeID = @ThemeID";
                        
                    }
                    else
                    {
                        // Insert new theme
                        query = @"
                           
                            INSERT INTO dbo.[appUserDefineTheme] 
                                (ThemeName, Description, ThemeDetails, IsForAllUsers, AppCreatedByID)
                            VALUES 
                                (@ThemeName, @Description, @ThemeDetails, @IsForAllUsers, @AppCreatedByID);
                            SELECT CAST(SCOPE_IDENTITY() AS int);";
                    }


                    List<SqlParameter> parameters = new List<SqlParameter>();

                    if (isUpdate)
                    {
                        SqlParameter themeIdParam = new SqlParameter("@ThemeID", SqlDbType.Int)
                        { Value = themeId.Value };
                        parameters.Add(themeIdParam);
                    }

                    SqlParameter themeNameParam = new SqlParameter("@ThemeName", SqlDbType.NVarChar, 200)
                    { Value = themeDto.ThemeName ?? string.Empty };

                    SqlParameter descriptionParam = new SqlParameter("@Description", SqlDbType.NVarChar, 500)
                    { Value = themeDto.Description ?? string.Empty };

                    SqlParameter themeDetailsParam = new SqlParameter("@ThemeDetails", SqlDbType.NVarChar)
                    { Value = themeDetailsJson };

                    SqlParameter isForAllUsersParam = new SqlParameter("@IsForAllUsers", SqlDbType.Bit)
                    { Value = themeDto.IsForAllUsers.HasValue ? (object)themeDto.IsForAllUsers.Value : DBNull.Value };

                    SqlParameter createdByParam = new SqlParameter("@AppCreatedByID", SqlDbType.Int)
                    { Value = AppSecurityUserBL.CurrentUserId };

                    // Add parameters to command
                    parameters.Add(themeNameParam);
                    parameters.Add(descriptionParam);
                    parameters.Add(themeDetailsParam);
                    parameters.Add(isForAllUsersParam);
                    parameters.Add(createdByParam);


                    using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            object queryResult = adpater.ExecuteScalarQuery(query, parameters);

                            if (isUpdate)
                            {
                                themeId = ControlTypeValueConverter.ConvertValueToInt(queryResult);
                            }                            

                            if (themeId.HasValue)
                            {                               
                                result.Object = RetrieveOneAppUserDefineThemeDto(themeId.Value);
                                result.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "app_SaveOneUserTheme_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                            }
                            else
                            {
                                result.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "app_SaveOneAppUserTheme_Error", ValidationItemType.Error, "Unable to determine ThemeID after save."));
                            }
                        }
                        catch (Exception ex)
                        {
                            adpater.Rollback();
                            result.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "app_SaveOneAppUserTheme_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }                    
                }
                catch (Exception ex)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "app_SaveOneAppUserTheme_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return result;
        }

        public static OperationCallResult<bool> DeleteOneAppUserDefineTheme(int? themeId)
        {
            OperationCallResult<bool> result = new OperationCallResult<bool> { ValidationResult = new ValidationResult() };

            if (!themeId.HasValue)
            {
                result.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "app_DeleteOneAppUserTheme_ThemeIdRequired", ValidationItemType.Error, "ThemeId is required"));
                return result;
            }

            string query = @"
                DELETE FROM [appUserDefineTheme]
                WHERE ThemeID = @ThemeID";

            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@ThemeID", SqlDbType.Int) { Value = themeId.Value }
            };

            using (var adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    int rowsAffected = adapter.ExecuteExecuteNonQuery(query, parameters);

                    if (rowsAffected > 0)
                    {
                        result.Object = true;
                        result.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "app_DeleteOneAppUserTheme_Delete_OK", ValidationItemType.Message, "Delete Successfully"));
                    }
                    else
                    {
                        result.Object = false;
                        result.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "app_DeleteOneAppUserTheme_NotFound", ValidationItemType.Error, "Theme not found or already deleted"));
                    }
                }
                catch (Exception ex)
                {
                    adapter.Rollback();
                    result.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "app_DeleteOneAppUserTheme_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return result;
        }

        public class AppUserThemeDto
        {
            [DataMember(EmitDefaultValue = false)]
            public object Id { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public string ThemeName { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public string Description { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public Dictionary<string, string> DictThemeDetails { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public bool? IsForAllUsers { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public int? AppCreatedById { get; set; }

        }


    }
}