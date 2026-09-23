using System;
using System.Threading;
using System.Threading.Tasks;
using App.BL.Document;
using SessionBL = App.BL.AIAgent.GenericAgent.AppGenericAgentSessionBL;
using APP.Components.Dto;
using APP.Components.Dto.Document;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Auth;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public sealed class PdfTechPackController : SecureBaseController
{
    private readonly IPdfTechPackExtractor _extractor;

    public PdfTechPackController(IPdfTechPackExtractor extractor) => _extractor = extractor;

    [HttpPost]
    [RequestSizeLimit(100L * 1024 * 1024)]
    public async Task<OperationCallResult<PdfTechPackExtractionResultDto>> Extract(
        IFormFile file,
        [FromQuery] string sessionKey,
        [FromQuery] string skillKey,
        CancellationToken cancellationToken)
    {
        var result = new OperationCallResult<PdfTechPackExtractionResultDto>();
        if (ServerContext.Instance.CurrnetClientIdentity is not AppClientIdentity identity || identity.UserId == null)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(PdfTechPackController), "PdfTechPack_Unauthorized", ValidationItemType.Error,
                "Authenticated agent identity is required."));
            return result;
        }

        var companyId = identity.CurrentWorkingCompanyId == null
            ? 0
            : Convert.ToInt32(identity.CurrentWorkingCompanyId);
        if (companyId <= 0 || string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(PdfTechPackController), "PdfTechPack_Context", ValidationItemType.Error,
                "Company and session context are required."));
            return result;
        }
        var userId = Convert.ToInt32(identity.UserId);
        var ownsSession = SessionBL.IsFixedKey(sessionKey, skillKey, userId)
            || SessionBL.LoadBySessionKey(sessionKey, skillKey, userId) != null;
        if (!ownsSession)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(PdfTechPackController), "PdfTechPack_Session", ValidationItemType.Error,
                "The agent session does not belong to the current user."));
            return result;
        }
        if (file == null || file.Length == 0 || !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(PdfTechPackController), "PdfTechPack_File", ValidationItemType.Error,
                "A non-empty PDF file is required."));
            return result;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await using var memory = new System.IO.MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            result.Object = await _extractor.ExtractAsync(new PdfTechPackExtractionRequest
            {
                CompanyId = companyId,
                SessionKey = sessionKey,
                FileName = file.FileName,
                PdfBytes = memory.ToArray(),
                Configuration = PdfTechPackExtractor.ResolveTenantConfiguration()
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(PdfTechPackController), "PdfTechPack_Extract", ValidationItemType.Error, ex.Message));
        }
        return result;
    }
}
