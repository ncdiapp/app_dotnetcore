using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using App.BL.AppMgr.AiSkill;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

/// <summary>
/// WebAPI Controller for AI Skill Management
/// </summary>
[Route("webapi/[controller]/[action]")]
public class AISkillController : SecureBaseController
{
    #region Default DataSource

    [HttpGet]
    public OperationCallResult<int?> GetDefaultDataSourceId()
    {
        var result = new OperationCallResult<int?>();
        try
        {
            result.Object = AppAISkillBL.GetDefaultDataSourceId();
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_GetDefault_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    #endregion

    #region Skill

    [HttpGet]
    public OperationCallResult<List<AppAISkillDto>> GetAll(int dataSourceId)
    {
        var result = new OperationCallResult<List<AppAISkillDto>>();
        try
        {
            result.Object = AppAISkillBL.GetAllSkills(dataSourceId);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_GetAll_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<AppAISkillDto> GetById(int dataSourceId, int skillId)
    {
        var result = new OperationCallResult<AppAISkillDto>();
        try
        {
            result.Object = AppAISkillBL.GetSkillById(dataSourceId, skillId);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_GetById_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<int> Create(int dataSourceId, [FromBody] AppAISkillDto dto)
    {
        var result = new OperationCallResult<int>();
        try
        {
            if (string.IsNullOrWhiteSpace(dto?.Name))
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(AISkillController), "AISkill_NameRequired", ValidationItemType.Error, "Name is required."));
                return result;
            }
            result.Object = AppAISkillBL.CreateSkill(dataSourceId, dto);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_Create_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> Update(int dataSourceId, [FromBody] AppAISkillDto dto)
    {
        var result = new OperationCallResult<bool>();
        try
        {
            if (string.IsNullOrWhiteSpace(dto?.Name))
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(AISkillController), "AISkill_NameRequired", ValidationItemType.Error, "Name is required."));
                return result;
            }
            AppAISkillBL.UpdateSkill(dataSourceId, dto);
            result.Object = true;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_Update_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> Delete(int dataSourceId, int skillId)
    {
        var result = new OperationCallResult<bool>();
        try
        {
            AppAISkillBL.DeleteSkill(dataSourceId, skillId);
            result.Object = true;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_Delete_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<string> GetComposed(int dataSourceId, int skillId)
    {
        var result = new OperationCallResult<string>();
        try
        {
            result.Object = AppAISkillBL.GetComposedSkillPrompt(dataSourceId, skillId);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_GetComposed_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    #endregion

    #region References

    [HttpGet]
    public OperationCallResult<List<AppAISkillRefDto>> GetRefs(int dataSourceId, int skillId)
    {
        var result = new OperationCallResult<List<AppAISkillRefDto>>();
        try
        {
            result.Object = AppAISkillBL.GetRefsBySkillId(dataSourceId, skillId);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_GetRefs_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<int> CreateRef(int dataSourceId, [FromBody] AppAISkillRefDto dto)
    {
        var result = new OperationCallResult<int>();
        try
        {
            result.Object = AppAISkillBL.CreateRef(dataSourceId, dto);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_CreateRef_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> UpdateRef(int dataSourceId, [FromBody] AppAISkillRefDto dto)
    {
        var result = new OperationCallResult<bool>();
        try
        {
            AppAISkillBL.UpdateRef(dataSourceId, dto);
            result.Object = true;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_UpdateRef_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> DeleteRef(int dataSourceId, int refId)
    {
        var result = new OperationCallResult<bool>();
        try
        {
            AppAISkillBL.DeleteRef(dataSourceId, refId);
            result.Object = true;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AISkillController), "AISkill_DeleteRef_Error", ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    #endregion
}
