using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
#if NETFRAMEWORK
using Microsoft.Web.Administration;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.EntityClasses;

namespace App.BL
{
#if NETFRAMEWORK
    public static class AppIISServerBL
    {


        //CreateWebsite(string websitename, string physicalPath, int portNumber, string applicationPool, string hostname, string ipAddress)
        public static OperationCallResult<bool> CreateWebsite(string websitename, string physicalPath, int portNumber)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            bool isWebsiteExists = IsWebsiteExists(websitename);

            if (!isWebsiteExists)
            {
                try
                {
                    ServerManager serverMgr = new ServerManager();
                    Site mySite = serverMgr.Sites.Add(websitename, physicalPath, portNumber);
                    mySite.ServerAutoStart = true;
                    serverMgr.CommitChanges();

                    validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateWebsite_OK", ValidationItemType.Message, "Create Website Successfully"));

                    toReturn.Object = true;
                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateWebsite_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
            else
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateWebsite_Error", ValidationItemType.Error, "Website already exists."));
            }


            return toReturn;
        }


        public static OperationCallResult<bool> CreateWebApplication(string websitename, string applicationPath, string physicalPath, string applicationPoolName)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            bool isAppPoolExists = IsApplicationPoolExists(applicationPoolName);

            if (!isAppPoolExists)
            {
                CreateApplicationPool(applicationPoolName);
            }


            Site site = null;
            ServerManager serverMgr = new ServerManager();
            if (!string.IsNullOrWhiteSpace(websitename))
            {
                bool isWebsiteExists = IsWebsiteExists(websitename);

                if (isWebsiteExists)
                {
                    site = serverMgr.Sites[websitename];
                }
            }
            else
            {
                site = serverMgr.Sites.FirstOrDefault();
            }

            if (site != null)
            {
                if (!applicationPath.StartsWith("/"))
                {
                    applicationPath = "/" + applicationPath;
                }

                bool isApplicationExists = IsApplicationExists(site, applicationPath);
                if (!isApplicationExists)
                {

                    try
                    {
                        Application newApplication = site.Applications.Add(applicationPath, physicalPath);
                        newApplication.ApplicationPoolName = applicationPoolName;
                        serverMgr.CommitChanges();

                        validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateWebApplication_OK", ValidationItemType.Message, "Create Web Application Successfully"));

                        toReturn.Object = true;
                    }
                    catch (Exception ex)
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateWebApplication_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
                else
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateWebApplication_Error", ValidationItemType.Error, "Web application already exists."));
                }
            }
            else
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateWebApplication_Error", ValidationItemType.Error, "Website does not exist."));
            }


            return toReturn;
        }


        public static OperationCallResult<bool> CreateApplicationPool(string applicationPoolName)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            bool isApplicationPoolExists = IsApplicationPoolExists(applicationPoolName);

            if (!isApplicationPoolExists)
            {
                try
                {
                    ServerManager serverMgr = new ServerManager();
                    serverMgr.ApplicationPools.Add(applicationPoolName);
                    ApplicationPool apppool = serverMgr.ApplicationPools[applicationPoolName];
                    //apppool.ManagedPipelineMode = ManagedPipelineMode.ISAPI;
                    serverMgr.CommitChanges();
                    // apppool.Recycle();

                    validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateApplicationPool_OK", ValidationItemType.Message, "Create Application pool Successfully"));

                    toReturn.Object = true;
                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateApplicationPool_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
            else
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_CreateApplicationPool_Error", ValidationItemType.Error, "Application pool already exists."));
            }


            return toReturn;
        }


        private static bool IsWebsiteExists(string websitename)
        {
            ServerManager serverMgr = new ServerManager();
            bool isSiteExist = serverMgr.Sites.FirstOrDefault(o => o.Name == websitename) != null;
            return isSiteExist;
        }

        private static bool IsApplicationPoolExists(string applicationPoolName)
        {
            ServerManager serverMgr = new ServerManager();
            bool isApplicationPoolExists = serverMgr.ApplicationPools.FirstOrDefault(o => o.Name == applicationPoolName) != null;
            return isApplicationPoolExists;
        }

        private static bool IsApplicationExists(Site site, string applicationPath)
        {
            bool isApplicationExists = site.Applications.FirstOrDefault(o => o.Path == applicationPath) != null;
            return isApplicationExists;
        }
    }
#else
    public static class AppIISServerBL
    {
        // TODO-PHASE4: Replace with .NET 10 equivalent
        public static object CreateWebsite(string websitename, string physicalPath, int portNumber)
            => throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
        public static object CreateWebApplication(string websitename, string applicationPath, string physicalPath, string applicationPoolName)
            => throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
        public static object CreateApplicationPool(string applicationPoolName)
            => throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
    }
#endif
}