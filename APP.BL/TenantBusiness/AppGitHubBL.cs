using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using FirebaseAdmin;
using APP.BL;
#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using System.IO.Compression;
using System.Net.Http.Headers;

namespace App.BL
{
    public static class AppGitHubBL
    {
        // private static readonly string repoOwner = "koke4545";

        private static readonly HttpClient _httpClient = new HttpClient();




        public static async Task<OperationCallResult<AppGitHubConfigDto>> PushAllFilesToGitFromOneNextJsApp(AppGitHubConfigDto gitDto)
        {
            OperationCallResult<AppGitHubConfigDto> aOperationCallResult = new OperationCallResult<AppGitHubConfigDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (!gitDto.ESiteId.HasValue)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Push_Error", ValidationItemType.Error, "ESiteId is missing."));
                return aOperationCallResult;
            }

            await ValidateNextJsAppGitConnection(gitDto, aValidationResult, true);

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }

            var esiteExDto = AppNextJsAppConfigBL.RetrieveOneNextJsAppExDto(gitDto.ESiteId.Value);

            gitDto.LocalRootFolderPath = esiteExDto.RootFolderPath;
            var pushResult = await PushAllFilesToGitFromOneFolder(gitDto);


            if (pushResult.IsSuccessful)
            {
                esiteExDto.EsiteAttribute.GitHubRepositoryInfo = gitDto;

                var saveResult = AppNextJsAppConfigBL.UpdateNextJsApp(esiteExDto);

                if (saveResult.ValidationResult.HasErrors)
                {
                    pushResult.ValidationResult.Merge(saveResult.ValidationResult);
                }
            }


            return pushResult;
        }





        public static async Task<OperationCallResult<AppGitHubConfigDto>> PushOneNextJsAppFileToGit(AppGitHubConfigDto gitDto)
        {
            OperationCallResult<AppGitHubConfigDto> aOperationCallResult = new OperationCallResult<AppGitHubConfigDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (!gitDto.ESiteId.HasValue)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Push_Error", ValidationItemType.Error, "ESiteId is missing."));
                return aOperationCallResult;
            }

            string localFilePath = gitDto.LocalFilePath;

            if (!File.Exists(localFilePath))
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_FileUpload_Error", ValidationItemType.Error, $"Local file not found: {localFilePath}"));
                return aOperationCallResult;
            }

            await ValidateNextJsAppGitConnection(gitDto, aValidationResult, true);

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }

            var esiteExDto = AppNextJsAppConfigBL.RetrieveOneNextJsAppExDto(gitDto.ESiteId.Value);
            gitDto.LocalRootFolderPath = esiteExDto.RootFolderPath;

            try
            {


                if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) || string.IsNullOrWhiteSpace(gitDto.RepoName) || string.IsNullOrWhiteSpace(gitDto.GithubToken) || string.IsNullOrWhiteSpace(gitDto.LocalRootFolderPath))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Push failed. Missing required GitHub parameters."));
                    return aOperationCallResult;
                }

                string latestCommitSha = await GetLatestCommitSha(gitDto, aValidationResult);

                if (string.IsNullOrWhiteSpace(latestCommitSha))
                {
                    bool isInitialCommitSuccess = await CreateInitialCommit(gitDto.RepoUsername, gitDto.RepoName, gitDto.GithubToken, aValidationResult);

                    if (isInitialCommitSuccess)
                    {
                        latestCommitSha = await GetLatestCommitSha(gitDto, aValidationResult);

                        if (string.IsNullOrWhiteSpace(latestCommitSha))
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Push failed. Missing The Latest Commit Sha."));
                            return aOperationCallResult;
                        }
                    }
                    else
                    {
                        return aOperationCallResult;
                    }
                }


                string base64Content = Convert.ToBase64String(File.ReadAllBytes(localFilePath));
                // relativePath = Path.GetFileName(filePath);
                string relativePath = localFilePath.Replace(gitDto.LocalRootFolderPath, "").Replace(@"\", "/");

                string filePayload = $@"
                    {{
                        path: ""{relativePath}"",
                        contents: ""{base64Content}""
                    }}";

                var response = await UploadFilesToGitHub(gitDto, filePayload, latestCommitSha, aValidationResult);

            }
            catch (Exception ex)
            {
                string errorMessage = "Push All Files To GitHub failed. \n" + ex.Message;
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, errorMessage));
            }


            return aOperationCallResult;
        }


        //public static async Task<OperationCallResult<AppGitHubConfigDto>> PushOneFolderToGit(AppGitHubConfigDto gitDto)
        //{
        //    OperationCallResult<AppGitHubConfigDto> aOperationCallResult = new OperationCallResult<AppGitHubConfigDto>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    if (!gitDto.ESiteId.HasValue)
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Push_Error", ValidationItemType.Error, "ESiteId is missing."));
        //        return aOperationCallResult;
        //    }

        //    await PushFilesToGitFromNextJsApp_ValidateGitConnection(gitDto, aOperationCallResult, aValidationResult);

        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) || string.IsNullOrWhiteSpace(gitDto.RepoName) || string.IsNullOrWhiteSpace(gitDto.GithubToken) || string.IsNullOrWhiteSpace(gitDto.LocalRootFolderPath))
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Push failed. Missing required GitHub parameters."));
        //            return aOperationCallResult;
        //        }

        //        string latestCommitSha = await GetLatestCommitSha(gitDto, aValidationResult);

        //        if (string.IsNullOrWhiteSpace(latestCommitSha))
        //        {
        //            bool isInitialCommitSuccess = await CreateInitialCommit(gitDto.RepoUsername, gitDto.RepoName, gitDto.GithubToken, aValidationResult);

        //            if (isInitialCommitSuccess)
        //            {
        //                latestCommitSha = await GetLatestCommitSha(gitDto, aValidationResult);

        //                if (string.IsNullOrWhiteSpace(latestCommitSha))
        //                {
        //                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Push failed. Missing The Latest Commit Sha."));
        //                    return aOperationCallResult;
        //                }
        //            }
        //            else
        //            {
        //                return aOperationCallResult;
        //            }
        //        }

        //        var filesToUpload = new StringBuilder();
        //        foreach (var filePath in Directory.GetFiles(gitDto.LocalRootFolderPath, "*.*", SearchOption.AllDirectories))
        //        {
        //            if (!filePath.Contains(gitDto.LocalRootFolderPath + "node_modules")
        //                && !filePath.Contains(gitDto.LocalRootFolderPath + ".next")
        //                && !filePath.Contains(gitDto.LocalRootFolderPath + ".vs")
        //                && !filePath.Contains(gitDto.LocalRootFolderPath + ".git")
        //                )
        //            {

        //                string relativePath = filePath.Replace(gitDto.LocalRootFolderPath, "").Replace(@"\", "/");
        //                string base64Content = Convert.ToBase64String(File.ReadAllBytes(filePath));

        //                filesToUpload.Append($@"
        //            {{
        //                path: ""{relativePath}"",
        //                contents: ""{base64Content}""
        //            }},");

        //            }
        //        }

        //        var response = await UploadFilesToGitHub(gitDto, filesToUpload.ToString().TrimEnd(','), latestCommitSha, aValidationResult);
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = "Push All Files To GitHub failed. \n" + ex.Message;
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, errorMessage));
        //    }


        //    return aOperationCallResult;
        //}


        public static async Task<OperationCallResult<AppGitHubConfigDto>> PullAllFilesFromGitToNextJsApp(AppGitHubConfigDto gitDto)
        {
            OperationCallResult<AppGitHubConfigDto> aOperationCallResult = new OperationCallResult<AppGitHubConfigDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (!gitDto.ESiteId.HasValue)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Push_Error", ValidationItemType.Error, "ESiteId is missing."));
                return aOperationCallResult;
            }

            OperationCallResult<string> connectionResult = await TestRepositoryConnection(gitDto);

            if (connectionResult.IsSuccessfulWithResult && connectionResult.Object == "Success")
            {
                var esiteExDto = AppNextJsAppConfigBL.RetrieveOneNextJsAppExDto(gitDto.ESiteId.Value);

                gitDto.LocalRootFolderPath = esiteExDto.RootFolderPath;


                string downloadZipFilePath = await DownloadRepositoryAsync(gitDto, aValidationResult);

                if (string.IsNullOrWhiteSpace(downloadZipFilePath))
                {
                    return aOperationCallResult;
                }

                ExtractRepositoryZipFileToNextJsAppFolder(gitDto.ESiteId.Value, downloadZipFilePath, aValidationResult);

                if (!aValidationResult.HasErrors)
                {
                    esiteExDto.EsiteAttribute.GitHubRepositoryInfo = gitDto;

                    var saveResult = AppNextJsAppConfigBL.UpdateNextJsApp(esiteExDto);

                    if (saveResult.ValidationResult.HasErrors)
                    {
                        aValidationResult.Merge(saveResult.ValidationResult);
                    }
                }

                return aOperationCallResult;
            }
            else
            {
                aValidationResult.Merge(connectionResult.ValidationResult);
                return aOperationCallResult;
            }
        }


        public static async Task<OperationCallResult<AppGitHubConfigDto>> PullOneNextJsAppFileFromGit(AppGitHubConfigDto gitDto)
        {
            var aOperationCallResult = new OperationCallResult<AppGitHubConfigDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (!gitDto.ESiteId.HasValue)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Pull_Error", ValidationItemType.Error, "ESiteId is missing."));
                return aOperationCallResult;
            }

            string localFilePath = gitDto.LocalFilePath;

            if (!File.Exists(localFilePath))
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_FileUpload_Error", ValidationItemType.Error, $"Local file not found: {localFilePath}"));
                return aOperationCallResult;
            }

           

            await ValidateNextJsAppGitConnection(gitDto, aValidationResult, false);

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }

            var esiteExDto = AppNextJsAppConfigBL.RetrieveOneNextJsAppExDto(gitDto.ESiteId.Value);
            gitDto.LocalRootFolderPath = esiteExDto.RootFolderPath;

            try
            {
                if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) || string.IsNullOrWhiteSpace(gitDto.RepoName) || string.IsNullOrWhiteSpace(gitDto.GithubToken) || string.IsNullOrWhiteSpace(gitDto.LocalRootFolderPath))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Pull_Error", ValidationItemType.Error, "Pull failed. Missing required GitHub parameters."));
                    return aOperationCallResult;
                }

                string relativePath = localFilePath.Replace(gitDto.LocalRootFolderPath, "").Replace(@"\", "/");

                string fileContent = await DownloadOneFileFromGitHub(gitDto, relativePath, aValidationResult);

                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Pull_Error", ValidationItemType.Error, "Failed to retrieve file content from GitHub."));
                    return aOperationCallResult;
                }

                //string localFilePath = Path.Combine(gitDto.LocalRootFolderPath, relativePath.Replace("/", "\\"));

                Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
                File.WriteAllText(localFilePath, fileContent);
            }
            catch (Exception ex)
            {
                string errorMessage = "Pull file from GitHub failed. \n" + ex.Message;
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Pull_Error", ValidationItemType.Error, errorMessage));
            }

            return aOperationCallResult;
        }


        public static async Task<OperationCallResult<string>> TestRepositoryConnection(AppGitHubConfigDto gitDto)
        {
            OperationCallResult<string> aOperationCallResult = new OperationCallResult<string>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string resultCode = await CheckIfRepositoryExists(gitDto);
            aOperationCallResult.Object = resultCode;

            if (resultCode == "Success")
            {

                if (string.IsNullOrWhiteSpace(gitDto.RepoUrl))
                {
                    gitDto.RepoUrl = $"https://github.com/{gitDto.RepoUsername}/{gitDto.RepoName}";
                }

                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Success", ValidationItemType.Message,
                   $"Git repository successfully connected. View the repository details online (Git login required) using the link below. \n"
                   + $"<a style=\"color:royalblue;text-decoration:underline;\" href=\"{gitDto.RepoUrl}\" target=\"_blank\"> {gitDto.RepoUrl} </a>"));

            }
            else
            {
                if (resultCode == "UsernameTokenMismatch")
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                            $"Git user {gitDto.RepoUsername} does not match this access token.\n"));
                }
                else if (resultCode == "UserNotFound")
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                        $"Git user {gitDto.RepoUsername} does not exist.\n"));
                }
                else if (resultCode == "RepositoryNotFound")
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                       $"Git account successfully connected. The Repository does not exist. "));
                }
                else if (resultCode == "InvalidToken")
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                       $"Invalid Git Access Token.\n"));
                }
                else if (resultCode == "AccessDenied")
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                       $"Git Access Denied With This Access Token.\n"));
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                       $"Connect to repository failed.\n"));
                }
            }

            return aOperationCallResult;
        }


        public static async Task<OperationCallResult<AppGitHubConfigDto>> CreateNextJsAppGitHubRepository(AppGitHubConfigDto gitDto)
        {
            if (gitDto.ESiteId.HasValue)
            {
                var esiteExDto = AppNextJsAppConfigBL.RetrieveOneNextJsAppExDto(gitDto.ESiteId.Value);

                OperationCallResult<AppGitHubConfigDto> createRepoResult = await CreateGitHubRepository(gitDto);

                if (createRepoResult.IsSuccessfulWithResult)
                {
                    gitDto = createRepoResult.Object;

                    esiteExDto.EsiteAttribute.GitHubRepositoryInfo = gitDto;

                    var saveResult = AppNextJsAppConfigBL.UpdateNextJsApp(esiteExDto);

                    if (saveResult.ValidationResult.HasErrors)
                    {
                        createRepoResult.ValidationResult.Merge(saveResult.ValidationResult);
                    }
                }

                return createRepoResult;
            }

            return null;
        }

        //public static async Task<OperationCallResult<AppGitHubConfigDto>> PushAllFilesToGitFromOneFolder(AppGitHubConfigDto gitDto)
        //{
        //    OperationCallResult<AppGitHubConfigDto> aOperationCallResult = new OperationCallResult<AppGitHubConfigDto>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) || string.IsNullOrWhiteSpace(gitDto.RepoName) || string.IsNullOrWhiteSpace(gitDto.GithubToken) || string.IsNullOrWhiteSpace(gitDto.LocalRootFolderPath))
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Push failed. Missing required GitHub parameters."));
        //            return aOperationCallResult;
        //        }

        //        string latestCommitSha = await GetLatestCommitSha(gitDto, aValidationResult);

        //        if (string.IsNullOrWhiteSpace(latestCommitSha))
        //        {
        //            bool isInitialCommitSuccess = await CreateInitialCommit(gitDto.RepoUsername, gitDto.RepoName, gitDto.GithubToken, aValidationResult);

        //            if (isInitialCommitSuccess)
        //            {
        //                latestCommitSha = await GetLatestCommitSha(gitDto, aValidationResult);

        //                if (string.IsNullOrWhiteSpace(latestCommitSha))
        //                {
        //                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Push failed. Missing The Latest Commit Sha."));
        //                    return aOperationCallResult;
        //                }
        //            }
        //            else
        //            {
        //                return aOperationCallResult;
        //            }
        //        }

        //        var filesToUpload = new StringBuilder();
        //        foreach (var filePath in Directory.GetFiles(gitDto.LocalRootFolderPath, "*.*", SearchOption.AllDirectories))
        //        {
        //            if (!filePath.Contains(gitDto.LocalRootFolderPath + "node_modules")
        //                && !filePath.Contains(gitDto.LocalRootFolderPath + ".next")
        //                && !filePath.Contains(gitDto.LocalRootFolderPath + ".vs")
        //                && !filePath.Contains(gitDto.LocalRootFolderPath + ".git")
        //                )
        //            {

        //                string relativePath = filePath.Replace(gitDto.LocalRootFolderPath, "").Replace(@"\", "/");
        //                string base64Content = Convert.ToBase64String(File.ReadAllBytes(filePath));

        //                filesToUpload.Append($@"
        //            {{
        //                path: ""{relativePath}"",
        //                contents: ""{base64Content}""
        //            }},");

        //            }
        //        }

        //        var response = await UploadFilesToGitHub(gitDto, filesToUpload.ToString().TrimEnd(','), latestCommitSha, aValidationResult);
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = "Push All Files To GitHub failed. \n" + ex.Message;
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, errorMessage));
        //    }



        //    return aOperationCallResult;
        //}

        public static async Task<OperationCallResult<AppGitHubConfigDto>> PushAllFilesToGitFromOneFolder(AppGitHubConfigDto gitDto)
        {
            var aOperationCallResult = new OperationCallResult<AppGitHubConfigDto> { ValidationResult = new ValidationResult() };
            var aValidationResult = aOperationCallResult.ValidationResult;

            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) ||
                    string.IsNullOrWhiteSpace(gitDto.RepoName) ||
                    string.IsNullOrWhiteSpace(gitDto.GithubToken) ||
                    string.IsNullOrWhiteSpace(gitDto.LocalRootFolderPath))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Missing required GitHub parameters."));
                    return aOperationCallResult;
                }

                string latestCommitSha = await GetLatestCommitSha(gitDto, aValidationResult);

                if (string.IsNullOrWhiteSpace(latestCommitSha))
                {
                    bool isInitialCommitSuccess = await CreateInitialCommit(gitDto.RepoUsername, gitDto.RepoName, gitDto.GithubToken, aValidationResult);

                    if (isInitialCommitSuccess)
                    {
                        latestCommitSha = await GetLatestCommitSha(gitDto, aValidationResult);

                        if (string.IsNullOrWhiteSpace(latestCommitSha))
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Push failed. Missing The Latest Commit Sha."));
                            return aOperationCallResult;
                        }
                    }
                    else
                    {
                        return aOperationCallResult;
                    }
                }

                var allFilePaths = Directory.GetFiles(gitDto.LocalRootFolderPath, "*.*", SearchOption.AllDirectories)
                     .Where(path => 
                        !path.Contains("node_modules") 
                        && !path.Contains(".next") 
                        && !path.Contains(".vs") 
                        && !path.Contains(".git")
                        && !path.Contains("FigmaTemplate"))
                     .ToList();

                const int maxFilesPerBatch = 100;
                const int maxBatchSizeBytes = 10 * 1024 * 1024; // 10MB

                var batches = PrepareBatches(aValidationResult, allFilePaths, gitDto.LocalRootFolderPath, maxFilesPerBatch, maxBatchSizeBytes);

                if (aValidationResult.HasErrors)
                {                    
                    return aOperationCallResult;
                }

                foreach (var batch in batches)
                {
                    var filesToUpload = new StringBuilder();
                    foreach (var (path, content) in batch)
                    {
                        filesToUpload.Append($@"
        {{
            path: ""{path}"",
            contents: ""{content}""
        }},");
                    }                    

                    var response = await UploadFilesToGitHub(gitDto, filesToUpload.ToString().TrimEnd(','), latestCommitSha, aValidationResult);

                    if (aValidationResult.HasErrors)
                        return aOperationCallResult;

                    if (response != null && response.ContainsKey("data"))
                    {
                        var commitUrl = response["data"]?["createCommitOnBranch"]?["commit"]?["url"]?.ToString();
                        if (!string.IsNullOrEmpty(commitUrl))
                        {
                            // Extract SHA from the URL (last part after the last slash)
                            var parts = commitUrl.Split('/');
                            latestCommitSha = parts[parts.Length - 1];

                        }
                        else
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_UploadFiles_Error", ValidationItemType.Error, "Failed to retrieve new commit SHA."));
                            return aOperationCallResult;
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_UploadFiles_Error", ValidationItemType.Error, "Batch upload failed."));
                        return aOperationCallResult;
                    }
                }

                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_UploadFiles_Success", ValidationItemType.Message, "All files successfully pushed to GitHub."));
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Push All Files To GitHub failed. " + ex.Message));
            }

            return aOperationCallResult;
        }



        private static List<List<(string Path, string Base64Content)>> PrepareBatches(ValidationResult aValidationResult, List<string> allFilePaths, string localRootFolderPath, int maxFilesPerBatch, int maxBatchSizeBytes)
        {
            var batches = new List<List<(string Path, string Base64Content)>>();
            var currentBatch = new List<(string Path, string Base64Content)>();
            int currentBatchSize = 0;

            foreach (var file in allFilePaths)
            {
                string relativePath = file.Replace(localRootFolderPath, "").Replace(@"\", "/");
                string base64Content = Convert.ToBase64String(File.ReadAllBytes(file));
                int fileSize = Encoding.UTF8.GetByteCount(base64Content);

                if (fileSize > maxBatchSizeBytes)
                {
                    string message = "Push All Files To GitHub failed. File " + relativePath + " exceed the git file size limit.";
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_PrepareBatches_Error", ValidationItemType.Error, message));
                    break;
                }

                // If adding this file exceeds either the file count or batch size, start a new batch
                if (currentBatch.Count >= maxFilesPerBatch || (currentBatchSize + fileSize) > maxBatchSizeBytes)
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<(string Path, string Base64Content)>();
                    currentBatchSize = 0;
                }

                currentBatch.Add((relativePath, base64Content));
                currentBatchSize += fileSize;
            }

            // Add the last batch if it has files
            if (currentBatch.Any())
            {
                batches.Add(currentBatch);
            }

            return batches;
        }

        public static async Task<OperationCallResult<AppGitHubConfigDto>> CreateGitHubRepository(AppGitHubConfigDto gitDto)
        {
            OperationCallResult<AppGitHubConfigDto> aOperationCallResult = new OperationCallResult<AppGitHubConfigDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            try
            {
                if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) || string.IsNullOrWhiteSpace(gitDto.RepoName) || string.IsNullOrWhiteSpace(gitDto.GithubToken))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Repository creation failed. Missing required GitHub parameters."));
                    return aOperationCallResult;
                }

                var getOwnerIdResult = await GetGitHubOwnerId(gitDto);

                if (getOwnerIdResult.IsSuccessfulWithResult)
                {
                    string query = $@"
                        mutation {{
                            createRepository(input: {{ 
                                name: ""{gitDto.RepoName}"", 
                                visibility: {(gitDto.IsPublic ? "PUBLIC" : "PRIVATE")}, 
                                description: ""{gitDto.Description}"",
                                ownerId: ""{gitDto.RepoOwnerId}""
                            }}) {{
                                repository {{ id name url }}
                            }}
                        }}";

                    var requestBody = new { query };

                    var response = await SendGraphQLRequest(requestBody, gitDto.GithubToken);

                    if (response == null)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Repository creation failed. GitHub API response is null."));
                    }

                    else if (response.ContainsKey("errors"))
                    {
                        string errorMessage = "Repository creation failed. \n" + BuildGitApiResponseErrorText(response);
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, errorMessage));
                    }
                    else
                    {
                        string repoUrl = response?["data"]?["createRepository"]?["repository"]?["url"]?.ToString();
                        string repoName = response?["data"]?["createRepository"]?["repository"]?["name"]?.ToString();

                        if (string.IsNullOrWhiteSpace(repoUrl))
                        {
                            string message = "Repository creation failed. No repository URL returned. \n" + BuildGidApiResonseMessageText(response);
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, message));
                        }
                        else if (string.IsNullOrWhiteSpace(repoName))
                        {
                            string message = "Repository creation failed. No repository name returned. \n" + BuildGidApiResonseMessageText(response);
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, message));
                        }
                        else
                        {
                            gitDto.RepoUrl = repoUrl;
                            gitDto.RepoName = repoName;

                            aOperationCallResult.Object = gitDto;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Success", ValidationItemType.Message, "Repository " + repoName + " has been created. \n" + "url: " + repoUrl));
                        }
                    }
                }
                else
                {
                    aValidationResult.Merge(getOwnerIdResult.ValidationResult);

                    return aOperationCallResult;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Repository creation failed. \n" + ex.Message;
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, errorMessage));
            }

            return aOperationCallResult;
        }


        private static async Task<OperationCallResult<string>> GetGitHubOwnerId(AppGitHubConfigDto gitDto)
        {
            OperationCallResult<string> aOperationCallResult = new OperationCallResult<string>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            try
            {
                if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) || string.IsNullOrWhiteSpace(gitDto.RepoName) || string.IsNullOrWhiteSpace(gitDto.GithubToken))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Get GitHub Owner Id failed. Missing required GitHub parameters."));
                    return aOperationCallResult;
                }


                string query = $@"
                    query {{
                        user(login: ""{gitDto.RepoUsername}"") {{
                            id
                        }}
                    }}";

                var requestBody = new { query };

                var response = await SendGraphQLRequest(requestBody, gitDto.GithubToken);

                if (response == null)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Error", ValidationItemType.Error, "Get GitHub Owner Id failed. GitHub API response is null."));
                }

                else if (response.ContainsKey("errors"))
                {
                    string errorMessage = "Get GitHub Owner Id failed. \n" + BuildGitApiResponseErrorText(response);
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Error", ValidationItemType.Error, errorMessage));
                }
                else
                {
                    string ownerId = response?["data"]?["user"]?["id"]?.ToString();

                    if (string.IsNullOrWhiteSpace(ownerId))
                    {
                        string message = "Get GitHub Owner Id failed. No ownerId returned. \n" + BuildGidApiResonseMessageText(response);
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Error", ValidationItemType.Error, message));

                    }
                    else
                    {
                        aOperationCallResult.Object = gitDto.RepoOwnerId = ownerId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Success", ValidationItemType.Message, "Get GitHub Owner Id success."));
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Get GitHub Owner Id failed. \n" + ex.Message;
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Error", ValidationItemType.Error, errorMessage));
            }

            return aOperationCallResult;
        }


        private static async Task<bool> CreateInitialCommit(string repoOwner, string repoName, string githubToken, ValidationResult aValidationResult)
        {
            try
            {
                // Step 1: Define the API URL
                string url = $"https://api.github.com/repos/{repoOwner}/{repoName}/contents/README.md";

                // Step 2: Create a sample content (Base64 encoded)
                string fileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes("# Initial Commit\n\nThis is the first commit."));

                // Step 3: Construct the request payload
                var requestBody = new
                {
                    message = "Initial commit",
                    content = fileContent,
                    branch = "main"
                };

                // Step 4: Serialize request body to JSON
                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Step 5: Add authentication & headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubGraphQLSync");

                // Step 6: Make the API request
                HttpResponseMessage response = await _httpClient.PutAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                // Step 7: Check if the commit was successful
                if (response.IsSuccessStatusCode)
                {
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateInitialCommit_Success", ValidationItemType.Message, "Initial Commit Completed."));
                    return true;
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateInitialCommit_Error", ValidationItemType.Message, $"Failed to create initial commit. \n {responseString}"));
                    return false;
                }
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateInitialCommit_Error", ValidationItemType.Message, $"Failed to create initial commit. \n Exception: {ex.Message}"));
                return false;
            }
        }


        private static async Task<string> GetLatestCommitSha(AppGitHubConfigDto gitDto, ValidationResult aValidationResult)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) || string.IsNullOrWhiteSpace(gitDto.RepoName) || string.IsNullOrWhiteSpace(gitDto.GithubToken))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Get GitHub Latest Commit Sha Failed. Missing required GitHub parameters."));
                    return "";
                }

                if (string.IsNullOrWhiteSpace(gitDto.Branch))
                {
                    gitDto.Branch = "main";
                }

                string query = $@"
                query {{
                    repository(owner: ""{gitDto.RepoUsername}"", name: ""{gitDto.RepoName}"") {{
                        ref(qualifiedName: ""refs/heads/{gitDto.Branch}"") {{
                            target {{
                                ... on Commit {{
                                    oid
                                }}
                            }}
                        }}
                        defaultBranchRef {{
                            name
                        }}
                    }}
                }}";

                var requestBody = new { query };

                var response = await SendGraphQLRequest(requestBody, gitDto.GithubToken);

                //return response?["data"]?["repository"]?["ref"]?["target"]?["oid"]?.ToString();

                if (response == null)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Error", ValidationItemType.Error, "Get GitHub Latest Commit Sha oid Failed. GitHub API response is null."));
                }

                else if (response.ContainsKey("errors"))
                {
                    string errorMessage = "Get GitHub Latest Commit Sha oid Failed. \n" + BuildGitApiResponseErrorText(response);
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Error", ValidationItemType.Error, errorMessage));
                }
                else
                {
                    var repository = response?["data"]?["repository"];

                    if (repository != null && repository["ref"] != null && repository["ref"]["target"] != null && repository["ref"]["target"]["oid"] != null)
                    {
                        return repository["ref"]["target"]["oid"].ToString();
                    }
                    else if (repository["defaultBranchRef"] != null && repository["defaultBranchRef"]["name"] != null)
                    {
                        string defaultBranch = repository["defaultBranchRef"]["name"].ToString();
                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubLatestCommitSha_Warning", ValidationItemType.Message, $"Branch '{gitDto.Branch}' not found. Default branch is '{defaultBranch}'."));
                        return defaultBranch;
                    }
                    else
                    {
                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubLatestCommitSha_Error", ValidationItemType.Message, "Repository is empty."));
                    }

                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Get GitHub Latest Commit Sha oid Failed. \n" + ex.Message;
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Error", ValidationItemType.Error, errorMessage));
            }

            return "";
        }


        private static async Task<dynamic> UploadFilesToGitHub(AppGitHubConfigDto gitDto, string filesJson, string latestCommitSha, ValidationResult aValidationResult)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gitDto.RepoUsername) || string.IsNullOrWhiteSpace(gitDto.RepoName) || string.IsNullOrWhiteSpace(gitDto.GithubToken))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_CreateGitHubRepository_Error", ValidationItemType.Error, "Get GitHub Latest Commit Sha Failed. Missing required GitHub parameters."));
                    return "";
                }

                if (string.IsNullOrWhiteSpace(gitDto.Branch))
                {
                    gitDto.Branch = "main";
                }

                string query = $@"
                mutation {{
                    createCommitOnBranch(input: {{
                        branch: {{
                            repositoryNameWithOwner: ""{gitDto.RepoUsername}/{gitDto.RepoName}"",
                            branchName: ""{gitDto.Branch}""
                        }},
                        message: {{ headline: ""Batch update files"" }},
                        expectedHeadOid: ""{latestCommitSha}"",
                        fileChanges: {{
                            additions: [{filesJson}]
                        }}
                    }}) {{
                        commit {{
                            url
                            message
                        }}
                    }}
                }}";



                var requestBody = new { query };

                var response = await SendGraphQLRequest(requestBody, gitDto.GithubToken);

                if (response == null)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_UploadFiles_Error", ValidationItemType.Error,
                        "Upload Files To GitHub Failed. No response from GitHub API. " + "\n\n\nFile Path:\n" + filesJson));
                    //return null;
                }
                else
                {

                    if (response.ContainsKey("errors"))
                    {
                        string errorMessage = "Upload Files To GitHub Failed. \n" + BuildGitApiResponseErrorText(response);
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_UploadFiles_Error", ValidationItemType.Error, errorMessage));
                        //return null;
                    }

                    // ✅ Check for commit URL
                    var commitUrl = response?["data"]?["createCommitOnBranch"]?["commit"]?["url"]?.ToString();
                    if (string.IsNullOrWhiteSpace(commitUrl))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_UploadFiles_Error", ValidationItemType.Error,
                            "Upload Files To GitHub Failed. No commit URL returned."));
                        //return null;
                    }
                    else
                    {
                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_UploadFiles_Success", ValidationItemType.Message,
                        //    $"Files successfully pushed to GitHub. \nYou may view commit details online by " + $"<a style=\"color:royalblue;text-decoration:underline;\" href=\"{commitUrl}\" target=\"_blank\"> Click This Link </a> (Git login requriied)."));
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                string errorMessage = "Upload Files To GitHub Failed. \n" + ex.Message;
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GetGitHubOwnerId_Error", ValidationItemType.Error, errorMessage));
            }

            return null;
        }


        private static async Task<dynamic> SendGraphQLRequest(object requestBody, string githubToken)
        {

            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(20)
            };

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
            client.DefaultRequestHeaders.Add("User-Agent", "GitHubGraphQLSync");

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.github.com/graphql", content);

            string responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(responseString);
        }


        //private static async Task<string> CheckIfRepositoryExists(AppGitHubConfigDto gitDto)
        //{
        //    string resultCode = "UnknownError";

        //    try
        //    {
        //        string repoApiUrl = $"https://api.github.com/repos/{gitDto.RepoUsername}/{gitDto.RepoName}";
        //        string userApiUrl = $"https://api.github.com/users/{gitDto.RepoUsername}"; // 🔹 Check if username exists

        //        using (HttpClient client = new HttpClient())
        //        {
        //            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {gitDto.GithubToken}");
        //            client.DefaultRequestHeaders.Add("User-Agent", "GitHubGraphQLSync");

        //            // 🔹 Step 1: Check if user exists
        //            HttpResponseMessage userResponse = await client.GetAsync(userApiUrl);
        //            if (userResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        //            {
        //                resultCode = "UserNotFound";

        //                return resultCode;
        //            }
        //            else
        //            {
        //                // 🔹 Step 2: Check if repository exists
        //                HttpResponseMessage repoResponse = await client.GetAsync(repoApiUrl);
        //                string responseBody = await repoResponse.Content.ReadAsStringAsync();

        //                if (repoResponse.IsSuccessStatusCode)
        //                {
        //                    resultCode = "Success";
        //                }
        //                else if (repoResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        //                {
        //                    resultCode = "RepositoryNotFound";
        //                }
        //                else if (repoResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //                {
        //                    resultCode = "InvalidToken";
        //                }
        //                else
        //                {
        //                    resultCode = "UnknownError";
        //                }

        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        resultCode = "Exception";
        //    }

        //    return resultCode;
        //}


        private static async Task<string> CheckIfRepositoryExists(AppGitHubConfigDto gitDto)
        {
            string resultCode = "UnknownError";

            try
            {
                string repoApiUrl = $"https://api.github.com/repos/{gitDto.RepoUsername}/{gitDto.RepoName}";
                string authUserApiUrl = "https://api.github.com/user";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {gitDto.GithubToken}");
                    client.DefaultRequestHeaders.Add("User-Agent", "GitHubGraphQLSync");

                    // Step 1: Retrieve authenticated user's username
                    HttpResponseMessage authUserResponse = await client.GetAsync(authUserApiUrl);
                    if (authUserResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        resultCode = "InvalidToken";
                        return resultCode;
                    }

                    authUserResponse.EnsureSuccessStatusCode();
                    string authUserResponseBody = await authUserResponse.Content.ReadAsStringAsync();
                    dynamic authUser = JsonConvert.DeserializeObject(authUserResponseBody);
                    string tokenUsername = authUser.login;

                    // Step 2: Compare token's username with input username
                    if (!string.Equals(tokenUsername, gitDto.RepoUsername, StringComparison.OrdinalIgnoreCase))
                    {
                        resultCode = "UsernameTokenMismatch";
                        return resultCode;
                    }

                    // Step 3: Check if repository exists
                    HttpResponseMessage repoResponse = await client.GetAsync(repoApiUrl);
                    if (repoResponse.IsSuccessStatusCode)
                    {
                        resultCode = "Success";
                    }
                    else if (repoResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        resultCode = "RepositoryNotFound";
                    }
                    else if (repoResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        resultCode = "AccessDenied";
                    }
                    else
                    {
                        resultCode = "UnknownError";
                    }
                }
            }
            catch (Exception ex)
            {
                resultCode = "Exception";
            }

            return resultCode;
        }


        private static async Task<bool> CheckIfGitUserExists(AppGitHubConfigDto gitDto, ValidationResult aValidationResult)
        {
            try
            {

                string userApiUrl = $"https://api.github.com/users/{gitDto.RepoUsername}";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {gitDto.GithubToken}");
                    client.DefaultRequestHeaders.Add("User-Agent", "GitHubGraphQLSync");

                    // 🔹 Step 1: Check if user exists
                    HttpResponseMessage userResponse = await client.GetAsync(userApiUrl);
                    if (userResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity),
                            "GitHub_User_Not_Found", ValidationItemType.Error,
                            $"The GitHub username '{gitDto.RepoUsername}' does not exist."));


                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity),
                    "GitHub_Connection_Error", ValidationItemType.Error,
                    $"Error checking repository existence: {ex.Message}"));

            }

            return false;
        }



        private static string BuildGitApiResponseErrorText(dynamic response)
        {
            string responseError = "";

            var errors = response["errors"];
            foreach (var error in errors)
            {
                string errorText = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(error.message);
                if (!string.IsNullOrWhiteSpace(errorText))
                {
                    responseError += errorText;
                }
                else
                {
                    responseError += error;
                }
            }

            return responseError;
        }


        private static string BuildGidApiResonseMessageText(dynamic response)
        {
            string responseMsg = "";

            string msgText = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(response.message);

            if (!string.IsNullOrWhiteSpace(msgText))
            {
                responseMsg += msgText;
            }
            else
            {
                responseMsg += response;
            }

            return responseMsg;
        }




        //private static async Task<string> GetGitHubUserId(string githubToken)
        //{
        //    string query = "{ \"query\": \"query { viewer { id login } }\" }";
        //    var response = await SendGraphQLRequest(query, githubToken);
        //    return response?["data"]?["viewer"]?["id"]?.ToString();
        //}


        private static async Task<string> DownloadRepositoryAsync(AppGitHubConfigDto gitDto, ValidationResult aValidationResult)
        {
            try
            {
                var esiteId = gitDto.ESiteId;

                string companyTempPath = AppEsiteFileBL.GetCompanyTempPath();

                string url = $"https://github.com/{gitDto.RepoUsername}/{gitDto.RepoName}/archive/refs/heads/{gitDto.Branch}.zip";

                if (!string.IsNullOrEmpty(gitDto.GithubToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gitDto.GithubToken);
                }

                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CSharpApp", "1.0"));


                byte[] zipBytes = await _httpClient.GetByteArrayAsync(url);


                string zipPath = Path.Combine(companyTempPath, $"Site_{esiteId}_GetFromGit.zip");

                File.WriteAllBytes(zipPath, zipBytes);

                return zipPath;
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_DownloadRepositoryAsync_Error", ValidationItemType.Error, $"Failed to download git repository git file. \n Exception: {ex.Message}"));
            }

            return "";
        }

        //private static bool ExtractRepositoryZipFileToNextJsAppFolder(int siteId, string zipFilePath, ValidationResult aValidationResult)
        //{
        //    try
        //    {
        //        if (!File.Exists(zipFilePath))
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_ExtractRepositoryZipFileToNextJsAppFolder_Error", ValidationItemType.Error, 
        //                $"Cannot find the downloaded zip file."));

        //            return false;
        //        }

        //        string extractPath = AppEsiteFileBL.GetWebSiteBasePath(siteId);

        //        if (!string.IsNullOrWhiteSpace(extractPath) && !string.IsNullOrWhiteSpace(zipFilePath))
        //        {
        //            using (ZipArchive archive = new ZipArchive(new FileStream(zipFilePath, FileMode.Open)))
        //            {
        //                archive.ExtractToDirectory(extractPath, true);

        //                return true;
        //            }
        //        }               
        //    }

        //    catch (Exception ex)
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_ExtractRepositoryZipFileToNextJsAppFolder_Error", ValidationItemType.Error,
        //                $"Extract zip file failed. Exception: " + ex.Message));

        //        return false;
        //    }

        //    return false;
        //}

        private static bool ExtractRepositoryZipFileToNextJsAppFolder(int siteId, string zipFilePath, ValidationResult aValidationResult)
        {
            try
            {
                if (!File.Exists(zipFilePath))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_ExtractRepositoryZipFileToNextJsAppFolder_Error", ValidationItemType.Error,
                        $"Cannot find the downloaded zip file."));
                    return false;
                }

                string extractPath = AppEsiteFileBL.GetWebSiteBasePath(siteId);

                if (!string.IsNullOrWhiteSpace(extractPath) && !string.IsNullOrWhiteSpace(zipFilePath))
                {
                    using (ZipArchive archive = new ZipArchive(new FileStream(zipFilePath, FileMode.Open)))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            // Skip directory entries
                            if (string.IsNullOrEmpty(entry.Name))
                                continue;

                            // Determine the relative path excluding the root folder
                            string entryPath = entry.FullName;
                            int directorySeparatorIndex = entryPath.IndexOf('/');
                            if (directorySeparatorIndex >= 0)
                            {
                                entryPath = entryPath.Substring(directorySeparatorIndex + 1);
                            }

                            // Determine the full path for the extracted file
                            string destinationPath = Path.Combine(extractPath, entryPath);

                            // Ensure the destination directory exists
                            string destinationDir = Path.GetDirectoryName(destinationPath);
                            if (!Directory.Exists(destinationDir))
                            {
                                Directory.CreateDirectory(destinationDir);
                            }

                            // Extract the file
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_ExtractRepositoryZipFileToNextJsAppFolder_Error", ValidationItemType.Error,
                    $"Extract zip file failed. Exception: {ex.Message}"));
                return false;
            }

            return false;
        }

        private static async Task<string> DownloadOneFileFromGitHub(AppGitHubConfigDto gitDto, string relativeFilePath, ValidationResult validationResult)
        {
            try
            {
                string apiUrl = $"https://api.github.com/repos/{gitDto.RepoUsername}/{gitDto.RepoName}/contents/{relativeFilePath}";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {gitDto.GithubToken}");
                    client.DefaultRequestHeaders.Add("User-Agent", "GitHubFileDownloader");

                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        dynamic jsonData = JsonConvert.DeserializeObject(jsonResponse);
                        string base64Content = jsonData.content;
                        byte[] fileBytes = Convert.FromBase64String(base64Content);
                        return Encoding.UTF8.GetString(fileBytes);
                    }
                    else
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Pull_Error", ValidationItemType.Error, $"Download file from GitHub failed. Status code {response.StatusCode}"));
                    }
                }
            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GitHub_Pull_Error", ValidationItemType.Error, "Download file from GitHub failed. \n" + ex.Message));
            }
            return null;
        }

        private static async Task<ValidationResult> ValidateNextJsAppGitConnection(AppGitHubConfigDto gitDto, ValidationResult aValidationResult, bool isAutoCreateNewRepository)
        {
            string resultCode = await CheckIfRepositoryExists(gitDto);

            if (resultCode != "Success")
            {
                if (resultCode == "RepositoryNotFound")
                {
                    if (isAutoCreateNewRepository)
                    {
                        OperationCallResult<AppGitHubConfigDto> createRepoResult = await CreateNextJsAppGitHubRepository(gitDto);

                        if (!createRepoResult.IsSuccessfulWithResult)
                        {
                            aValidationResult.Merge(createRepoResult.ValidationResult);
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                                $"Repository does not exist.\n"));
                    }
                }
                else
                {
                    if (resultCode == "UsernameTokenMismatch")
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                                $"Git user {gitDto.RepoUsername} does not match this access token.\n"));
                    }
                    else if (resultCode == "UserNotFound")
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                            $"Git user {gitDto.RepoUsername} does not exist.\n"));
                    }
                    else if (resultCode == "InvalidToken")
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                           $"Wrong Git Access Token.\n"));
                    }
                    else if (resultCode == "AccessDenied")
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                           $"Git Access Denied With This Access Token.\n"));
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_TestRepositoryConnection_Error", ValidationItemType.Message,
                           $"Connect to repository failed.\n"));
                    }
                }
            }

            return aValidationResult;
        }


        //public static async Task DownloadAndExtractRepositoryAsync(string owner, string repository, string branch, string token, string localPath)
        //{
        //    try
        //    {
        //        // Construct the download URL
        //        string url = $"https://github.com/{owner}/{repository}/archive/refs/heads/{branch}.zip";

        //        // Set up the HttpClient with authentication
        //        if (!string.IsNullOrEmpty(token))
        //        {
        //            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        }
        //        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CSharpApp", "1.0"));

        //        // Download the ZIP archive
        //        Console.WriteLine($"Downloading {url}...");
        //        byte[] zipBytes = await client.GetByteArrayAsync(url);

        //        // Define the path for the ZIP file
        //        string zipPath = Path.Combine(localPath, $"{repository}-{branch}.zip");

        //        // Save the ZIP archive to the local path
        //        await File.WriteAllBytesAsync(zipPath, zipBytes);
        //        Console.WriteLine($"Downloaded to {zipPath}");

        //        // Ensure the destination directory exists
        //        string extractPath = Path.Combine(localPath, $"{repository}-{branch}");
        //        Directory.CreateDirectory(extractPath);

        //        // Extract the ZIP archive to the destination directory
        //        ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);
        //        Console.WriteLine($"Extracted to {extractPath}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"An error occurred: {ex.Message}");
        //    }
        //}
    }

}