-- V079: Fix Form Configurator controlType docs — Memo=4 (EmAppControlType has no 3).
-- Also refresh update_field_setting tool description so agents stop inventing controlType=3.

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(
    SystemPrompt,
    N'- controlType: 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL(dropdown), 34=Time.',
    N'- controlType EmAppControlType (NO value 3): 1=DDL, 2=TextBox, 4=Memo, 5=Image, 7=Date, 9=File, 10=Label,
  13=CheckBox, 20=Numeric, 26=RichText, 27=DateTimeDetail, 34=Time, 37=ImageBinary, 38=AutoComplete,
  48=SearchAbleDDL, 50=ExternalImageUrl, 53=MultiSelectDDL. Multiline text = Memo=4 (never invent 3).')
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt LIKE N'%- controlType: 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL(dropdown), 34=Time.%';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription =
    N'Update durable field settings. changesJson keys: displayName, controlType, entityId, defaultValue, isVisible, isReadonly, isAllowEmpty. controlType EmAppControlType (no 3): 1=DDL, 2=TextBox, 4=Memo, 5=Image, 7=Date, 9=File, 10=Label, 13=CheckBox, 20=Numeric, 26=RichText, 34=Time, 37=ImageBinary, 38=AutoComplete, 48=SearchAbleDDL, 50=ExternalImageUrl, 53=MultiSelectDDL. Memo=4 not 3. Prefer fieldId; ask_user then request_form_reload.'
WHERE LibraryKey = N'platform-form-config'
  AND ToolName = N'update_field_setting';

-- App Builder iterative tool description (DB copy; runtime also validates in TransactionModifierPlugin).
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription =
    N'Modifies field displayName, controlType, entityId, or defaultValue. controlType EmAppControlType (no 3): 1=DDL, 2=TextBox, 4=Memo, 5=Image, 7=Date, 9=File, 10=Label, 13=CheckBox, 20=Numeric, 26=RichText, 34=Time, 37=ImageBinary, 38=AutoComplete, 48=SearchAbleDDL, 53=MultiSelectDDL. Memo=4 not 3.'
WHERE ToolName = N'update_transaction_field'
  AND ToolConfig LIKE N'%UpdateTransactionField%';
GO
