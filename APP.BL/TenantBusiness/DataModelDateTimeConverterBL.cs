using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.LBL.DatabaseSpecific;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

using APP.Framework;
namespace App.BL
{
    public static class DataModelDateTimeConverterBL
    {
        public const string NoneDisplayCharactor = "\u202E"; //Unicode U+202E

        // ********* Date Time Convert Rules
        //
        //	  ControlType DateTime:	Store UTC datetime in DB; 
        //							Save: Browser Auto Converted To UTC; 
        //							Client Retrieve: Call ConvertToClient

        //    ControlType Date:		Store Client date in DB; 
        //							Save: Browser Auto Converted To UTC => Truancate Time,
        //                            Client Retrieve: Keep same as DB(Client date)

        //    ControlType Time:		Store Client time in DB; 
        //							Save: Browser Auto Converted To UTC => Convert to Client Datetime ==> Truncate Date,
        //                            Client Retrieve: Keep same as DB(Client Time)

        //    Server端计算必须以CLIENT TIME为主（模拟成CLIENT Browser TIME）

        // ConvertMasterDetailPostedUtcToClientForCalculation(AppMasterDetailDto appformDataDto)
        // ConvertMasterDetailPostedUtcForSaving(AppMasterDetailDto appformDataDto)
        // ConvertMasterDetailFromUtcToClient(AppMasterDetailDto appformDataDto)
        // ConvertMasterDetailFromClientToUtc(AppMasterDetailDto appformDataDto)

        // ConvertListEditPostedUtcToClientForCalculation(AppListDataDto aformData)
        // ConvertListEditPostedUtcForSaving(AppListDataDto aformData)
        // ConvertListEditFromUtcToClient(AppListDataDto appListDataDto)
        // ConvertListEditFromClientToUtc(AppListDataDto appListDataDto)


        //For MasterDetail Form
        public static AppMasterDetailDto ConvertMasterDetailPostedUtcToClientForCalculation(AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto = null)
        {
            if(appTransactionExDto == null)
            {
                appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

            }

            // root unit
            var rootMasterUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);

            Dictionary<string, object> dictRootDbFieNameValue = appformDataDto.DictOneToOneFields;

            ConvertOneUnitDateTime_FromPostedUtcToClientForCalculation(rootMasterUnit, dictRootDbFieNameValue);

            // Sibling unit

            Dictionary<string, Dictionary<string, object>> dictSiblingValue = appformDataDto.DictSiblingOneToOneFields;

            if (!dictSiblingValue.IsEmpty())
            {
                foreach (string siblingUnitId in dictSiblingValue.Keys)
                {
                    AppTransactionUnitExDto siblingTransactionUnitExDto = appTransactionExDto.DictAllTransactionUnitIdExDto[siblingUnitId];

                    Dictionary<string, object> dictsiblingDbFieNameValue = dictSiblingValue[siblingUnitId];

                    ConvertOneUnitDateTime_FromPostedUtcToClientForCalculation(siblingTransactionUnitExDto, dictsiblingDbFieNameValue);

                }
            }

            // One to Many, only to conert the Date , arkwark !!,need to fix the time@root unit UI Date tije, Wijimo Grid seems  o kfor time format 

            if (!appformDataDto.DictOneToManyFields.IsEmpty())
            {
                foreach (string unitId in appformDataDto.DictOneToManyFields.Keys)
                {
                    var childUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId];

                    List<AppChildDataDto> childRowList = appformDataDto.DictOneToManyFields[unitId];

                    foreach (var childRow in childRowList)
                    {
                        ConvertOneUnitDateTime_FromPostedUtcToClientForCalculation(childUnitDto, childRow.DictOneToOneFields);

                        if (!childRow.DictOneToManyFields.IsEmpty())
                        {
                            foreach (string grandunitId in childRow.DictOneToManyFields.Keys)
                            {
                                var granchildUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[grandunitId];

                                List<Dictionary<string, object>> grandChildRowList = childRow.DictOneToManyFields[grandunitId]
                                    .Select(o => o.DictOneToOneFields).ToList();

                                foreach (Dictionary<string, object> grandOnetoOne in grandChildRowList)
                                {
                                    ConvertOneUnitDateTime_FromPostedUtcToClientForCalculation(granchildUnitDto, grandOnetoOne);
                                }

                            }
                        }

                    }
                }
            }

            if (appformDataDto.OrgAppListDataDto != null)
            {
                DataModelDateTimeConverterBL.ConvertListEditPostedUtcToClientForCalculation(appformDataDto.OrgAppListDataDto);
            }

            return appformDataDto;
        }

        public static AppMasterDetailDto ConvertMasterDetailPostedUtcForSaving(AppMasterDetailDto appformDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

            // root unit
            var rootMasterUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);
            Dictionary<string, object> dictRootDbFieNameValue = appformDataDto.DictOneToOneFields;
            ConvertOneUnitDateTime_FromPostedUtcForSaving(rootMasterUnit, dictRootDbFieNameValue);

            // Sibling unit

            Dictionary<string, Dictionary<string, object>> dictSiblingValue = appformDataDto.DictSiblingOneToOneFields;

            if (!dictSiblingValue.IsEmpty())
            {
                foreach (string siblingUnitId in dictSiblingValue.Keys)
                {
                    AppTransactionUnitExDto siblingTransactionUnitExDto = appTransactionExDto.DictAllTransactionUnitIdExDto[siblingUnitId];

                    Dictionary<string, object> dictsiblingDbFieNameValue = dictSiblingValue[siblingUnitId];

                    ConvertOneUnitDateTime_FromPostedUtcForSaving(siblingTransactionUnitExDto, dictsiblingDbFieNameValue);

                }
            }

            // One to Many, only to conert the Date , arkwark !!,need to fix the time@root unit UI Date tije, Wijimo Grid seems  o kfor time format 

            if (!appformDataDto.DictOneToManyFields.IsEmpty())
            {
                foreach (string unitId in appformDataDto.DictOneToManyFields.Keys)
                {
                    var childUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId];

                    List<AppChildDataDto> childRowList = appformDataDto.DictOneToManyFields[unitId];

                    foreach (var childRow in childRowList)
                    {
                        ConvertOneUnitDateTime_FromPostedUtcForSaving(childUnitDto, childRow.DictOneToOneFields);

                        if (!childRow.DictOneToManyFields.IsEmpty())
                        {
                            foreach (string grandunitId in childRow.DictOneToManyFields.Keys)
                            {
                                var granchildUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[grandunitId];

                                List<Dictionary<string, object>> grandChildRowList = childRow.DictOneToManyFields[grandunitId]
                                    .Select(o => o.DictOneToOneFields).ToList();

                                foreach (Dictionary<string, object> grandOnetoOne in grandChildRowList)
                                {
                                    ConvertOneUnitDateTime_FromPostedUtcForSaving(granchildUnitDto, grandOnetoOne);
                                }

                            }
                        }

                    }
                }
            }

            if (appformDataDto.OrgAppListDataDto != null)
            {
                DataModelDateTimeConverterBL.ConvertListEditPostedUtcForSaving(appformDataDto.OrgAppListDataDto);
            }

            return appformDataDto;
        }

        public static AppMasterDetailDto ConvertMasterDetailFromUtcToClient(AppMasterDetailDto appformDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

            // root unit
            var rootMasterUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);
            Dictionary<string, object> dictRootDbFieNameValue = appformDataDto.DictOneToOneFields;
            ConvertOneUnitDateTime_FromUtcToClient(rootMasterUnit, dictRootDbFieNameValue);

            // Sibling unit

            Dictionary<string, Dictionary<string, object>> dictSiblingValue = appformDataDto.DictSiblingOneToOneFields;

            if (!dictSiblingValue.IsEmpty())
            {
                foreach (string siblingUnitId in dictSiblingValue.Keys)
                {
                    AppTransactionUnitExDto siblingTransactionUnitExDto = appTransactionExDto.DictAllTransactionUnitIdExDto[siblingUnitId];

                    Dictionary<string, object> dictsiblingDbFieNameValue = dictSiblingValue[siblingUnitId];

                    ConvertOneUnitDateTime_FromUtcToClient(siblingTransactionUnitExDto, dictsiblingDbFieNameValue);

                }
            }

            // One to Many, only to conert the Date , arkwark !!,need to fix the time@root unit UI Date tije, Wijimo Grid seems  o kfor time format 

            if (!appformDataDto.DictOneToManyFields.IsEmpty())
            {
                foreach (string unitId in appformDataDto.DictOneToManyFields.Keys)
                {
                    var childUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId];

                    List<AppChildDataDto> childRowList = appformDataDto.DictOneToManyFields[unitId];

                    foreach (var childRow in childRowList)
                    {
                        ConvertOneUnitDateTime_FromUtcToClient(childUnitDto, childRow.DictOneToOneFields);

                        if (!childRow.DictOneToManyFields.IsEmpty())
                        {
                            foreach (string grandunitId in childRow.DictOneToManyFields.Keys)
                            {
                                var granchildUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[grandunitId];

                                List<Dictionary<string, object>> grandChildRowList = childRow.DictOneToManyFields[grandunitId]
                                    .Select(o => o.DictOneToOneFields).ToList();
                                foreach (Dictionary<string, object> grandOnetoOne in grandChildRowList)
                                {
                                    ConvertOneUnitDateTime_FromUtcToClient(granchildUnitDto, grandOnetoOne);
                                }

                            }
                        }

                    }
                }
            }

            if (appformDataDto.OrgAppListDataDto != null)
            {
                DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(appformDataDto.OrgAppListDataDto);
            }

            return appformDataDto;
        }

        public static AppMasterDetailDto ConvertMasterDetailFromClientToUtc(AppMasterDetailDto appformDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

            // root unit
            var rootMasterUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);
            Dictionary<string, object> dictRootDbFieNameValue = appformDataDto.DictOneToOneFields;
            ConvertOneUnitDateTime_FromClientToUtc(rootMasterUnit, dictRootDbFieNameValue);

            // Sibling unit

            Dictionary<string, Dictionary<string, object>> dictSiblingValue = appformDataDto.DictSiblingOneToOneFields;

            if (!dictSiblingValue.IsEmpty())
            {
                foreach (string siblingUnitId in dictSiblingValue.Keys)
                {
                    AppTransactionUnitExDto siblingTransactionUnitExDto = appTransactionExDto.DictAllTransactionUnitIdExDto[siblingUnitId];

                    Dictionary<string, object> dictsiblingDbFieNameValue = dictSiblingValue[siblingUnitId];

                    ConvertOneUnitDateTime_FromClientToUtc(siblingTransactionUnitExDto, dictsiblingDbFieNameValue);

                }
            }

            // One to Many, only to conert the Date , arkwark !!,need to fix the time@root unit UI Date tije, Wijimo Grid seems  o kfor time format 

            if (!appformDataDto.DictOneToManyFields.IsEmpty())
            {
                foreach (string unitId in appformDataDto.DictOneToManyFields.Keys)
                {
                    var childUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId];

                    List<AppChildDataDto> childRowList = appformDataDto.DictOneToManyFields[unitId];

                    foreach (var childRow in childRowList)
                    {
                        ConvertOneUnitDateTime_FromClientToUtc(childUnitDto, childRow.DictOneToOneFields);

                        if (!childRow.DictOneToManyFields.IsEmpty())
                        {
                            foreach (string grandunitId in childRow.DictOneToManyFields.Keys)
                            {
                                var granchildUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[grandunitId];

                                List<Dictionary<string, object>> grandChildRowList = childRow.DictOneToManyFields[grandunitId]
                                    .Select(o => o.DictOneToOneFields).ToList();
                                foreach (Dictionary<string, object> grandOnetoOne in grandChildRowList)
                                {
                                    ConvertOneUnitDateTime_FromClientToUtc(granchildUnitDto, grandOnetoOne);
                                }

                            }
                        }

                    }
                }
            }

            if (appformDataDto.OrgAppListDataDto != null)
            {
                DataModelDateTimeConverterBL.ConvertListEditFromClientToUtc(appformDataDto.OrgAppListDataDto);
            }

            return appformDataDto;
        }



        //For ListEdit Form
        public static AppListDataDto ConvertListEditPostedUtcToClientForCalculation(AppListDataDto appListDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appListDataDto.TransactionId);

            // root unit
            var childUnitDto = appTransactionExDto.AppTransactionUnitList.ElementAt(0);

            if (!appListDataDto.ListData.IsEmpty())
            {

                foreach (AppChildDataDto childRow in appListDataDto.ListData)
                {
                    ConvertOneUnitDateTime_FromPostedUtcToClientForCalculation(childUnitDto, childRow.DictOneToOneFields);

                    if (!childRow.DictOneToManyFields.IsEmpty())
                    {
                        foreach (string grandunitId in childRow.DictOneToManyFields.Keys)
                        {
                            var granchildUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[grandunitId];

                            List<Dictionary<string, object>> grandChildRowList = childRow.DictOneToManyFields[grandunitId]
                                    .Select(o => o.DictOneToOneFields).ToList();

                            foreach (Dictionary<string, object> grandOnetoOne in grandChildRowList)
                            {
                                ConvertOneUnitDateTime_FromPostedUtcToClientForCalculation(granchildUnitDto, grandOnetoOne);
                            }

                        }
                    }

                }

            }

            return appListDataDto;
        }

        public static AppListDataDto ConvertListEditPostedUtcForSaving(AppListDataDto appListDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appListDataDto.TransactionId);

            // root unit
            var childUnitDto = appTransactionExDto.AppTransactionUnitList.ElementAt(0);

            if (!appListDataDto.ListData.IsEmpty())
            {

                foreach (AppChildDataDto childRow in appListDataDto.ListData)
                {
                    ConvertOneUnitDateTime_FromPostedUtcForSaving(childUnitDto, childRow.DictOneToOneFields);

                    if (!childRow.DictOneToManyFields.IsEmpty())
                    {
                        foreach (string grandunitId in childRow.DictOneToManyFields.Keys)
                        {
                            var granchildUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[grandunitId];

                            List<Dictionary<string, object>> grandChildRowList = childRow.DictOneToManyFields[grandunitId]
                                    .Select(o => o.DictOneToOneFields).ToList();

                            foreach (Dictionary<string, object> grandOnetoOne in grandChildRowList)
                            {
                                ConvertOneUnitDateTime_FromPostedUtcForSaving(granchildUnitDto, grandOnetoOne);
                            }
                        }
                    }

                }
            }

            return appListDataDto;
        }

        public static AppListDataDto ConvertListEditFromUtcToClient(AppListDataDto appListDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appListDataDto.TransactionId);

            // root unit
            var childUnitDto = appTransactionExDto.AppTransactionUnitList.ElementAt(0);

            if (!appListDataDto.ListData.IsEmpty())
            {
                foreach (AppChildDataDto childRow in appListDataDto.ListData)
                {
                    ConvertOneUnitDateTime_FromUtcToClient(childUnitDto, childRow.DictOneToOneFields);

                    if (!childRow.DictOneToManyFields.IsEmpty())
                    {
                        foreach (string grandunitId in childRow.DictOneToManyFields.Keys)
                        {
                            var granchildUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[grandunitId];

                            List<Dictionary<string, object>> grandChildRowList = childRow.DictOneToManyFields[grandunitId]
                                    .Select(o => o.DictOneToOneFields).ToList();

                            foreach (Dictionary<string, object> grandOnetoOne in grandChildRowList)
                            {
                                ConvertOneUnitDateTime_FromUtcToClient(granchildUnitDto, grandOnetoOne);
                            }
                        }
                    }
                }
            }

            return appListDataDto;
        }

        public static AppListDataDto ConvertListEditFromClientToUtc(AppListDataDto appListDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appListDataDto.TransactionId);

            // root unit
            var childUnitDto = appTransactionExDto.AppTransactionUnitList.ElementAt(0);

            if (!appListDataDto.ListData.IsEmpty())
            {
                foreach (AppChildDataDto childRow in appListDataDto.ListData)
                {
                    ConvertOneUnitDateTime_FromClientToUtc(childUnitDto, childRow.DictOneToOneFields);

                    if (!childRow.DictOneToManyFields.IsEmpty())
                    {
                        foreach (string grandunitId in childRow.DictOneToManyFields.Keys)
                        {
                            var granchildUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[grandunitId];

                            List<Dictionary<string, object>> grandChildRowList = childRow.DictOneToManyFields[grandunitId]
                                    .Select(o => o.DictOneToOneFields).ToList();
                            foreach (Dictionary<string, object> grandOnetoOne in grandChildRowList)
                            {
                                ConvertOneUnitDateTime_FromClientToUtc(granchildUnitDto, grandOnetoOne);
                            }
                        }
                    }
                }
            }

            return appListDataDto;
        }




        private static void ConvertOneUnitDateTime_FromPostedUtcToClientForCalculation(AppTransactionUnitExDto aUnitExDto, Dictionary<string, object> dictDbFieNameValue, bool isNeedToConertTime = true)
        {
            HashSet<string> datetimeTypefieldNameList = new HashSet<string>();
            HashSet<string> dateTypefiledNameList = new HashSet<string>();
            HashSet<string> timeTypefiledNameList = new HashSet<string>();

            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.DateTime)
                .ForAll(o => datetimeTypefieldNameList.Add(o.DataBaseFieldName));

            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType == (int)EmAppDataType.Date)
                .ForAll(o => dateTypefiledNameList.Add(o.DataBaseFieldName));


            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType == (int)EmAppDataType.Time)
                .ForAll(o => timeTypefiledNameList.Add(o.DataBaseFieldName));

            foreach (string filedName in datetimeTypefieldNameList)
            {
                if (dictDbFieNameValue.ContainsKey(filedName))
                {
                    DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(dictDbFieNameValue[filedName]);
                    if (dateTime.HasValue)
                    {
                        dateTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dateTime);
                        dictDbFieNameValue[filedName] = dateTime.Value;
                    }
                }
            }


            foreach (string filedName in dateTypefiledNameList)
            {
                if (dictDbFieNameValue.ContainsKey(filedName))
                {
                    DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(dictDbFieNameValue[filedName]);
                    if (dateTime.HasValue)
                    {
                        dictDbFieNameValue[filedName] = dateTime.Value.Date;
                    }
                }
            }

            if (isNeedToConertTime)
            {
                foreach (string filedName in timeTypefiledNameList)
                {
                    if (dictDbFieNameValue.ContainsKey(filedName))
                    {
                        DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(dictDbFieNameValue[filedName]);
                        if (dateTime.HasValue)
                        {
                            dateTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dateTime);

                            dictDbFieNameValue[filedName] = dateTime.Value; // dateTime.Value.TimeOfDay;
                        }
                    }
                }
            }
        }


        private static void ConvertOneUnitDateTime_FromPostedUtcForSaving(AppTransactionUnitExDto aUnitExDto, Dictionary<string, object> dictDbFieNameValue, bool isNeedToConertTime = true)
        {
            HashSet<string> dateTypefiledNameList = new HashSet<string>();
            HashSet<string> timeTypefiledNameList = new HashSet<string>();


            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType == (int)EmAppDataType.Date)
                .ForAll(o => dateTypefiledNameList.Add(o.DataBaseFieldName));


            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType == (int)EmAppDataType.Time)
                .ForAll(o => timeTypefiledNameList.Add(o.DataBaseFieldName));

            foreach (string filedName in dateTypefiledNameList)
            {
                if (dictDbFieNameValue.ContainsKey(filedName))
                {
                    DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(dictDbFieNameValue[filedName]);
                    if (dateTime.HasValue)
                    {
                        dictDbFieNameValue[filedName] = dateTime.Value.Date;
                    }
                }
            }

            if (isNeedToConertTime)
            {
                foreach (string filedName in timeTypefiledNameList)
                {
                    if (dictDbFieNameValue.ContainsKey(filedName))
                    {
                        DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(dictDbFieNameValue[filedName]);
                        if (dateTime.HasValue)
                        {
                            dateTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dateTime);

                            dictDbFieNameValue[filedName] = dateTime.Value; // dateTime.Value.TimeOfDay;
                        }
                    }
                }
            }
        }


        public static void ConvertOneUnitDateTime_FromUtcToClient(AppTransactionUnitExDto aUnitExDto, Dictionary<string, object> dictDbFieNameValue)
        {
            HashSet<string> datetimeTypefieldNameList = new HashSet<string>();
            HashSet<string> timeTypefiledNameList = new HashSet<string>();

            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.DateTime)
                .ForAll(o => datetimeTypefieldNameList.Add(o.DataBaseFieldName));

            aUnitExDto.AppTransactionFieldList
            .Where(o => o.DataType.HasValue && o.DataType == (int)EmAppDataType.Time)
            .ForAll(o => timeTypefiledNameList.Add(o.DataBaseFieldName));

            foreach (string fieldName in datetimeTypefieldNameList)
            {
                if (dictDbFieNameValue.ContainsKey(fieldName))
                {
                    DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(dictDbFieNameValue[fieldName]);
                    if (dateTime.HasValue)
                    {
                        dateTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dateTime);
                        dictDbFieNameValue[fieldName] = dateTime.Value;
                    }

                }
            }

            foreach (string filedName in timeTypefiledNameList)
            {
                if (dictDbFieNameValue.ContainsKey(filedName))
                {
                    TimeSpan? timespan = ControlTypeValueConverter.ConvertValueToTimeSpan(dictDbFieNameValue[filedName]);
                    if (timespan.HasValue)
                    {
                        dictDbFieNameValue[filedName] = new DateTime(1900, 1, 1).Date + timespan.Value;
                    }
                }
            }

            AddDateTimeStringNoneDisplayCharactorToken(aUnitExDto, dictDbFieNameValue);
        }


        private static void ConvertOneUnitDateTime_FromClientToUtc(AppTransactionUnitExDto aUnitExDto, Dictionary<string, object> dictDbFieNameValue)
        {
            HashSet<string> datetimeTypefieldNameList = new HashSet<string>();

            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.DateTime)
                .ForAll(o => datetimeTypefieldNameList.Add(o.DataBaseFieldName));


            foreach (string fieldName in datetimeTypefieldNameList)
            {
                if (dictDbFieNameValue.ContainsKey(fieldName))
                {
                    DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(dictDbFieNameValue[fieldName]);
                    if (dateTime.HasValue)
                    {
                        dateTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(dateTime);
                        dictDbFieNameValue[fieldName] = dateTime.Value;
                    }

                }

            }

            RemoveDateTimeStringNoneDisplayCharactorToken(aUnitExDto, dictDbFieNameValue);
        }

        private static void AddDateTimeStringNoneDisplayCharactorToken(AppTransactionUnitExDto aUnitExDto, Dictionary<string, object> dictDbFieNameValue)
        {
            HashSet<string> stringTypeFieldNameList = new HashSet<string>();

            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.String)
                .ForAll(o => stringTypeFieldNameList.Add(o.DataBaseFieldName));

            foreach (string fieldName in stringTypeFieldNameList)
            {
                if (dictDbFieNameValue.ContainsKey(fieldName))
                {
                    if (dictDbFieNameValue[fieldName] != null)
                    {
                        string stringValue = dictDbFieNameValue[fieldName].ToString();

                        if (stringValue.Length >= 10 && stringValue.Length <= 30 
                            && ControlTypeValueConverter.ConvertValueToDate(stringValue).HasValue)
                        {
                            dictDbFieNameValue[fieldName] = stringValue + NoneDisplayCharactor;
                        }
                    }
                }

            }
        }

        private static void RemoveDateTimeStringNoneDisplayCharactorToken(AppTransactionUnitExDto aUnitExDto, Dictionary<string, object> dictDbFieNameValue)
        {
            HashSet<string> stringTypeFieldNameList = new HashSet<string>();

            aUnitExDto.AppTransactionFieldList
                .Where(o => o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.String)
                .ForAll(o => stringTypeFieldNameList.Add(o.DataBaseFieldName));

            foreach (string fieldName in stringTypeFieldNameList)
            {
                if (dictDbFieNameValue.ContainsKey(fieldName))
                {
                    if (dictDbFieNameValue[fieldName] != null)
                    {
                        string stringValue = dictDbFieNameValue[fieldName].ToString();

                        if (stringValue.IndexOf(NoneDisplayCharactor) >= 0)
                        {
                            dictDbFieNameValue[fieldName] = stringValue.Replace(NoneDisplayCharactor, "");
                        }
                    }
                }

            }
        }
    }
}