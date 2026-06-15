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
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using APP.Framework;
namespace App.BL
{
    public static class AppFileCollaborationBL
    {
        public static string WriteFileComments(AppFileDto appFileDtoWithcomments)
        {
            AppFileEntity aAppFileEntity = AppFileBL.GetInitialFileEntityWithoutContent(appFileDtoWithcomments.Id);

            string commetns = aAppFileEntity.Comments + string.Format("{0}{1} written by{2} at {3}", System.Environment.NewLine, appFileDtoWithcomments.Comments, AppSecurityUserBL.CurrentUserEntity.LoginName, System.DateTime.Now);
            aAppFileEntity.Comments = commetns;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.SaveEntity(aAppFileEntity);

            }
            return commetns;



        }

        public static AppFileDto RenameFileName(AppFileDto appFileDto)
        {

            int? fileId = appFileDto.Id as int?;
            string fileNewName = appFileDto.FileCode;

            AppFileEntity aAppFileEntity = new AppFileEntity();

            aAppFileEntity.FileId = fileId.Value;
            aAppFileEntity.FileCode = fileNewName;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.UpdateEntitiesDirectly(aAppFileEntity, new RelationPredicateBucket(AppFileFields.FileId == fileId));

            }
            return appFileDto;



        }

        public static AppFileDto DeleteFile(AppFileDto appFileDto)
        {

            int? fileId = appFileDto.Id as int?;
            string fileNewName = appFileDto.FileCode;

            AppFileEntity aAppFileEntity = new AppFileEntity();

            aAppFileEntity.FileId = fileId.Value;
            aAppFileEntity.FileCode = fileNewName;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.DeleteEntitiesDirectly(typeof(AppFileEntity), new RelationPredicateBucket(AppFileFields.FileId == fileId));

            }
            return appFileDto;



        }



        public static string ReadFileComments(object fileId)
        {
            AppFileEntity aAppFileEntity = AppFileBL.GetInitialFileEntityWithoutContent(fileId);

            return aAppFileEntity.Comments;

        }

        public static bool AddFilesToMyFavourite(List<int> fileIds)
        {
            EntityCollection<AppCurrentUserFavouriteFolderOrFileEntity> list = new EntityCollection<AppCurrentUserFavouriteFolderOrFileEntity>();
            foreach (int filedId in fileIds)
            {
                AppCurrentUserFavouriteFolderOrFileEntity favouriteFolderOrFileEntity = new AppCurrentUserFavouriteFolderOrFileEntity();
                favouriteFolderOrFileEntity.FiledId = filedId;
                favouriteFolderOrFileEntity.CurrentUserId = AppSecurityUserBL.CurrentUserId;
                favouriteFolderOrFileEntity.AppCreatedById = AppSecurityUserBL.CurrentUserId;

                favouriteFolderOrFileEntity.AppCreatedDate = System.DateTime.UtcNow;

                favouriteFolderOrFileEntity.AppModifiedById = AppSecurityUserBL.CurrentUserId;
                favouriteFolderOrFileEntity.AppModifiedDate = System.DateTime.UtcNow;

                list.Add(favouriteFolderOrFileEntity);

            }



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppCurrentUserFavouriteFolderOrFileFields.CurrentUserId == AppSecurityUserBL.CurrentUserId);
                    filter.PredicateExpression.AddWithAnd(AppCurrentUserFavouriteFolderOrFileFields.FiledId == fileIds);

                    adapter.DeleteEntitiesDirectly(typeof(AppCurrentUserFavouriteFolderOrFileEntity), filter);

                    foreach (AppCurrentUserFavouriteFolderOrFileEntity favouriteFolderOrFileEntity in list)
                    {
                        adapter.SaveEntity(favouriteFolderOrFileEntity);
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }



        public static bool RemoveFilesFromMyFavourite(List<int> fileIds)
        {


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppCurrentUserFavouriteFolderOrFileFields.CurrentUserId == AppSecurityUserBL.CurrentUserId);

                    filter.PredicateExpression.AddWithAnd(AppCurrentUserFavouriteFolderOrFileFields.FiledId == fileIds);

                    adapter.DeleteEntitiesDirectly(typeof(AppCurrentUserFavouriteFolderOrFileEntity), filter);



                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }


        //public static bool AddFilesToShareOther(KeyValuePair<int, KeyValuePair<List<int>, List<int>>> fileIdUserIdsRoleIds)
        //{
        //    int fileId = fileIdUserIdsRoleIds.Key;

        //    KeyValuePair<List<int>, List<int>> shareUseridRoleIds = fileIdUserIdsRoleIds.Value;

        //    EntityCollection<AppFileOrFolderShareToOtherEntity> list = new EntityCollection<AppFileOrFolderShareToOtherEntity>();
        //    foreach (int userId in shareUseridRoleIds.Key)
        //    {
        //        AppFileOrFolderShareToOtherEntity shareToOtherFileEntity = new AppFileOrFolderShareToOtherEntity();
        //        shareToOtherFileEntity.ShareToOtherUserId = userId;
        //        shareToOtherFileEntity.FileId = fileId;


        //        shareToOtherFileEntity.AppCreatedById = AppSecurityUserBL.CurrentUserId;

        //        shareToOtherFileEntity.AppCreatedDate = System.DateTime.UtcNow;

        //        shareToOtherFileEntity.AppModifiedById = AppSecurityUserBL.CurrentUserId;
        //        shareToOtherFileEntity.AppModifiedDate = System.DateTime.UtcNow;

        //        list.Add(shareToOtherFileEntity);

        //    }


        //    foreach (int roleId in shareUseridRoleIds.Value)
        //    {
        //        AppFileOrFolderShareToOtherEntity shareToOtherFileEntity = new AppFileOrFolderShareToOtherEntity();
        //        shareToOtherFileEntity.ShareToOtherRoleId = roleId;
        //        shareToOtherFileEntity.FileId = fileId;


        //        shareToOtherFileEntity.AppCreatedById = AppSecurityUserBL.CurrentUserId;

        //        shareToOtherFileEntity.AppCreatedDate = System.DateTime.UtcNow;

        //        shareToOtherFileEntity.AppModifiedById = AppSecurityUserBL.CurrentUserId;
        //        shareToOtherFileEntity.AppModifiedDate = System.DateTime.UtcNow;

        //        list.Add(shareToOtherFileEntity);

        //    }

        //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        try
        //        {

        //            RelationPredicateBucket filter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);
        //            //	filter.PredicateExpression.AddWithAnd(AppFileOrFolderShareToOtherFields.ShareToOtherUserId == shareUserids);
        //            filter.PredicateExpression.AddWithAnd(AppFileOrFolderShareToOtherFields.FileId == fileId);

        //            adapter.DeleteEntitiesDirectly(typeof(AppFileOrFolderShareToOtherEntity), filter);

        //            foreach (AppFileOrFolderShareToOtherEntity favouriteFolderOrFileEntity in list)
        //            {
        //                adapter.SaveEntity(favouriteFolderOrFileEntity);
        //            }

        //            return true;
        //        }
        //        catch
        //        {
        //            return false;
        //        }
        //    }
        //}

        //List<AppFileOrFolderShareToOtherDto> fileshareOtherList




        public static bool SendFileNotificationFromFileSharingMessageTemplate(AppMessageDto fileMessageTemplate)
        {
            if (fileMessageTemplate != null && fileMessageTemplate.FileshareOtherList != null)
            {
                List<AppFileOrFolderShareToOtherDto> fileshareOtherList = fileMessageTemplate.FileshareOtherList;
                string subject = fileMessageTemplate.Subject;
                string message = fileMessageTemplate.Message;
                bool isNeedAttachFile = fileMessageTemplate.IsAttachFile;

                try
                {
                    foreach (var group in fileshareOtherList.Where(o => o.FileId.HasValue
                    //&& o.IsNeedNotifyUser
                    )
                        .GroupBy(o => o.FileId.Value))
                    {
                        int fileId = group.Key;

                        List<int> userIdList = group.Where(o => o.ShareToOtherUserId.HasValue).Select(o => o.ShareToOtherUserId.Value).ToList();
                        List<int> roleIdList = group.Where(o => o.ShareToOtherRoleId.HasValue).Select(o => o.ShareToOtherRoleId.Value).ToList();

                        SendSingleFileNotification(fileId, userIdList, roleIdList, fileMessageTemplate, isNeedAttachFile);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }

            }

            return false;
        }

        private static void SendSingleFileNotification(int fileId, List<int> userIdList, List<int> roleIdList, AppMessageDto fileMessageTemplate, bool isNeedAttachFile)
        {
            if (userIdList == null)
            {
                userIdList = new List<int>();
            }

            if (roleIdList != null)
            {
                userIdList.AddRange(AppSecurityGroupBL.GetUserIdsByGroupIds(roleIdList));
            }

            userIdList = userIdList.Distinct().ToList();

            if (userIdList.Count > 0)
            {
                AppFileDto fileDto = AppFileBL.RetrieveMultipleFileSimpleDtoByIds(new List<int>() { fileId }).FirstOrDefault();

                if (fileDto != null)
                {
                    AppMessageDto messageDto = new AppMessageDto();

                    messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Transaction;
                    messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;

                    messageDto.Subject = fileMessageTemplate.Subject;
                    messageDto.Message = fileMessageTemplate.Message;
                    messageDto.TransactionId = fileMessageTemplate.TransactionId;
                    messageDto.TransactionRootValueId = fileId.ToString();

                    if (isNeedAttachFile)
                    {
                        messageDto.AttachmentFileToken = fileDto.Id.ToString();
                    }



                    messageDto.ToList = string.Empty;

                    var dictUserDto = AppSecurityUserBL.DictAllUserDto;

                    foreach (int userId in userIdList.Distinct())
                    {
                        if (dictUserDto.ContainsKey(userId))
                        {
                            var userDto = dictUserDto[userId];

                            if (!string.IsNullOrEmpty(userDto.Email.Trim()))
                            {
                                messageDto.ToList += userDto.Email.Trim() + ";";
                            }
                        }
                    }

                    AppMessageBL.SaveOneAppMessageDto(messageDto);
                }

            }
        }



        public static bool AddFilesToShareOther(AppMessageDto fileMessageTemplate)
        {
            if (fileMessageTemplate != null && fileMessageTemplate.FileshareOtherList != null)
            {
                List<AppFileOrFolderShareToOtherDto> fileshareOtherList = fileMessageTemplate.FileshareOtherList;

                EntityCollection<AppFileOrFolderShareToOtherEntity> list = new EntityCollection<AppFileOrFolderShareToOtherEntity>();

                List<int> fileIds = fileshareOtherList.Where(o => o.FileId.HasValue).Select(o => o.FileId.Value).ToList();

                foreach (var dto in fileshareOtherList)
                {
                    AppFileOrFolderShareToOtherEntity aAppFileOrFolderShareToOtherEntity = new AppFileOrFolderShareToOtherEntity();
                    AppFileOrFolderShareToOtherConverter.CopyDtoToEntity(aAppFileOrFolderShareToOtherEntity, dto);

                    list.Add(aAppFileOrFolderShareToOtherEntity);

                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        RelationPredicateBucket filter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);

                        filter.PredicateExpression.AddWithAnd(AppFileOrFolderShareToOtherFields.FileId == fileIds);

                        adapter.DeleteEntitiesDirectly(typeof(AppFileOrFolderShareToOtherEntity), filter);

                        foreach (AppFileOrFolderShareToOtherEntity favouriteFolderOrFileEntity in list)
                        {
                            adapter.SaveEntity(favouriteFolderOrFileEntity);
                        }

                        if (fileMessageTemplate.IsNeedToSendMessageAfterFileSharing)
                        {
                            return SendFileNotificationFromFileSharingMessageTemplate(fileMessageTemplate);
                        }
                        else
                        {
                            return true;
                        }

                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;

        }

        //public static bool SendFileSharingNotification(List<AppFileOrFolderShareToOtherDto> fileshareOtherList, string subject, string message)
        //{
        //    if (fileshareOtherList == null)
        //    {
        //        return false;
        //    }

        //    Dictionary<int, List<int>> dictFileIdAndUserIdList = new Dictionary<int, List<int>>();

        //    try
        //    {
        //        foreach (var group in fileshareOtherList.Where(o => o.FileId.HasValue && o.IsNeedNotifyUser).GroupBy(o => o.FileId.Value))
        //        {
        //            int fileId = group.Key;

        //            List<int> userIdList = group.Where(o => o.ShareToOtherUserId.HasValue).Select(o => o.ShareToOtherUserId.Value).ToList();
        //            List<int> roleIdList = group.Where(o => o.ShareToOtherRoleId.HasValue).Select(o => o.ShareToOtherRoleId.Value).ToList();

        //            SendSingleFileNotificationByType(fileId, userIdList, roleIdList, EmAppFileNotificationType.FileSharingChanged, subject, message);
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }

        //}


        public static KeyValuePair<List<int>, List<int>> GetCurrentUserFilesToShareOtherUserOrRole(int fileId)
        {

            EntityCollection<AppFileOrFolderShareToOtherEntity> list = new EntityCollection<AppFileOrFolderShareToOtherEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);
                filter.PredicateExpression.AddWithAnd(AppFileOrFolderShareToOtherFields.FileId == fileId);
                adapter.FetchEntityCollection(list, filter);

            }

            List<int> userids = list.Where(o => o.ShareToOtherUserId.HasValue).Select(o => o.ShareToOtherUserId.Value).ToList();
            List<int> roleids = list.Where(o => o.ShareToOtherRoleId.HasValue).Select(o => o.ShareToOtherRoleId.Value).ToList();

            return new KeyValuePair<List<int>, List<int>>(userids, roleids);
        }

        public static List<AppFileOrFolderShareToOtherDto> GetCurrentUserFilesToShareOtherDtoList(int fileId)
        {
            List<AppFileOrFolderShareToOtherDto> toReturnDtoList = new List<AppFileOrFolderShareToOtherDto>();

            EntityCollection<AppFileOrFolderShareToOtherEntity> list = new EntityCollection<AppFileOrFolderShareToOtherEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);
                filter.PredicateExpression.AddWithAnd(AppFileOrFolderShareToOtherFields.FileId == fileId);
                adapter.FetchEntityCollection(list, filter);

            }

            foreach (var appFileOrFolderShareToOtherEntity in list)
            {
                toReturnDtoList.Add(AppFileOrFolderShareToOtherConverter.ConvertEntityToDto(appFileOrFolderShareToOtherEntity));
            }

            return toReturnDtoList;
        }

        public static bool RemoveFilesToShareOther(List<int> fileIds)
        {




            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);

                    filter.PredicateExpression.AddWithAnd(AppFileOrFolderShareToOtherFields.FileId == fileIds);

                    adapter.DeleteEntitiesDirectly(typeof(AppFileOrFolderShareToOtherEntity), filter);



                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }




        public static bool AddFolderIdsToMyFavourite(List<int> folderIds)
        {
            EntityCollection<AppCurrentUserFavouriteFolderOrFileEntity> list = new EntityCollection<AppCurrentUserFavouriteFolderOrFileEntity>();
            foreach (int folderId in folderIds)
            {
                AppCurrentUserFavouriteFolderOrFileEntity favouriteFolderOrFileEntity = new AppCurrentUserFavouriteFolderOrFileEntity();
                favouriteFolderOrFileEntity.FolderId = folderId;
                favouriteFolderOrFileEntity.CurrentUserId = AppSecurityUserBL.CurrentUserId;
                favouriteFolderOrFileEntity.AppCreatedById = AppSecurityUserBL.CurrentUserId;
                favouriteFolderOrFileEntity.AppCreatedDate = System.DateTime.UtcNow;

                favouriteFolderOrFileEntity.AppModifiedById = AppSecurityUserBL.CurrentUserId;
                favouriteFolderOrFileEntity.AppModifiedDate = System.DateTime.UtcNow;
                list.Add(favouriteFolderOrFileEntity);

            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppCurrentUserFavouriteFolderOrFileFields.CurrentUserId == AppSecurityUserBL.CurrentUserId);
                    filter.PredicateExpression.AddWithAnd(AppCurrentUserFavouriteFolderOrFileFields.FolderId == folderIds);

                    adapter.DeleteEntitiesDirectly(typeof(AppCurrentUserFavouriteFolderOrFileEntity), filter);

                    foreach (AppCurrentUserFavouriteFolderOrFileEntity favouriteFolderOrFileEntity in list)
                    {
                        adapter.SaveEntity(favouriteFolderOrFileEntity);
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }

        }

        internal static int[] GetMyFavouriteFileIds()
        {


            EntityCollection<AppCurrentUserFavouriteFolderOrFileEntity> list = new EntityCollection<AppCurrentUserFavouriteFolderOrFileEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppCurrentUserFavouriteFolderOrFileFields.CurrentUserId == AppSecurityUserBL.CurrentUserId);

                IncludeFieldsList includeFied = new IncludeFieldsList();
                includeFied.Add(AppCurrentUserFavouriteFolderOrFileFields.FiledId);


                adapter.FetchEntityCollection(list, includeFied, filter);



            }

            return list.Where(o => o.FiledId.HasValue).Select(o => o.FiledId.Value).ToArray();
        }

        internal static int[] GetSharedToMeFileIds()
        {


            EntityCollection<AppFileOrFolderShareToOtherEntity> list = new EntityCollection<AppFileOrFolderShareToOtherEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.ShareToOtherUserId == AppSecurityUserBL.CurrentUserId);

                filter.PredicateExpression.AddWithOr(AppFileOrFolderShareToOtherFields.ShareToOtherRoleId == AppSecurityUserBL.CurrentGroupIds);


                IncludeFieldsList includeFied = new IncludeFieldsList();
                includeFied.Add(AppFileOrFolderShareToOtherFields.FileId);

                adapter.FetchEntityCollection(list, includeFied, filter);

            }

            return list.Where(o => o.FileId.HasValue).Select(o => o.FileId.Value).ToArray();
        }


        internal static int[] GetShareToOthersFileIds()
        {


            EntityCollection<AppFileOrFolderShareToOtherEntity> list = new EntityCollection<AppFileOrFolderShareToOtherEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);

                IncludeFieldsList includeFied = new IncludeFieldsList();
                includeFied.Add(AppFileOrFolderShareToOtherFields.FileId);


                adapter.FetchEntityCollection(list, includeFied, filter);



            }

            return list.Where(o => o.FileId.HasValue).Select(o => o.FileId.Value).ToArray();
        }
        internal static int[] GetMyRecentlyFilesFileIds()
        {
            EntityCollection<AppFileEntity> list = new EntityCollection<AppFileEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppFileFields.AppModifiedById == AppSecurityUserBL.CurrentUserId);

                filter.PredicateExpression.AddWithAnd(AppFileFields.AppModifiedDate >= System.DateTime.Now.AddDays(-30));

                IncludeFieldsList includeFied = new IncludeFieldsList();
                includeFied.Add(AppFileFields.FileId);
                includeFied.Add(AppFileFields.InitialFileId);
                adapter.FetchEntityCollection(list, includeFied, filter);


            }
            //NO Child
            var rootFileId = list.Where(o => !o.InitialFileId.HasValue).Select(o => o.FileId).ToArray();

            // child parent ID
            var oarentFileIds = list.Where(o => o.InitialFileId.HasValue).Select(o => o.FileId).ToArray();

            EntityCollection<AppFileEntity> parentList = new EntityCollection<AppFileEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppFileFields.AppModifiedById == AppSecurityUserBL.CurrentUserId);

                filter.PredicateExpression.AddWithAnd(AppFileFields.FileId == oarentFileIds);

                IncludeFieldsList includeFied = new IncludeFieldsList();
                includeFied.Add(AppFileFields.FileId);

                adapter.FetchEntityCollection(parentList, includeFied, filter);


            }

            parentList.Select(o => o.FileId).Union(rootFileId);


            return (parentList.Select(o => o.FileId).Union(rootFileId)).ToArray();
        }

        public static AppRolesAndUsersDto GetCurrentUserAvailaleShareFileToRolesAndUsers()
        {
            AppRolesAndUsersDto toReturn = new AppRolesAndUsersDto();

            toReturn.UserDtoList = AppSecurityUserBL.RetrieveCurrentUserAvailableEmailToUsers();
            toReturn.RoleDtoList = new List<AppSecurityGroupDto>();

            var allRoles = AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType(1).ToList();

            if (AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.Supplier
                || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.Customer
                || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.ClientAgent
                || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.SupplierAgent)
            {
                foreach (var roleDto in allRoles)
                {
                    if ((int)roleDto.Id == (int)EmAppUserType.Employee)
                    {
                        toReturn.RoleDtoList.Add(roleDto);
                    }
                    if ((int)roleDto.Id > 10)
                    {
                        if (roleDto.RoleUserTypeId.HasValue)
                        {
                            if (roleDto.RoleUserTypeId.Value == (int)EmAppUserType.Employee
                                || roleDto.RoleUserTypeId.Value == (int)EmAppUserType.SysAdmin
                                || roleDto.RoleUserTypeId.Value == (int)EmAppUserType.SaasCompanyAdmin)
                            {
                                toReturn.RoleDtoList.Add(roleDto);
                            }
                            else
                            {
                                if (AppSecurityUserBL.CurrentGroupIds.Contains((int)roleDto.Id))
                                {
                                    toReturn.RoleDtoList.Add(roleDto);
                                }
                            }
                        }
                        else
                        {
                            toReturn.RoleDtoList.Add(roleDto);
                        }
                    }
                }
            }
            else if (AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.Employee)
            {
                toReturn.RoleDtoList = allRoles;
            }
            else if (AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.SaasCompanyAdmin
                || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.SysAdmin)
            {
                toReturn.RoleDtoList = allRoles;
            }


            return toReturn;
        }
    }
}