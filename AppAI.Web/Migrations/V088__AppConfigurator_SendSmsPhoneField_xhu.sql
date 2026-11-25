-- V088: Send Message / SMS (60–63) agent options + Data Transfer targetFieldId guardrails.
-- - Destination field ids (phone/email/partner/userId) — previously ignored by ApplyCommandOptions
--   so UI Phone Number / Email Address Field stayed (None).
-- - notificationSubject, isUseRichTextMessageTemplate, isAttachAllFormFilesToMessage, toEmailAddress.
-- - Message Link childActionList = Partner (62) only (UI hides for Email 61).
-- - upsert_data_transfer / get_data_transfer_detail / find_fields: destinationFields catalogs;
--   reject invented targetFieldId (Agent previously used wrong-TX ids → raw numbers in UI DDL).

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Create/update Data Model Operation. actionType=EmAppTransactionCommandType (NOT Link Target 1/2). Common: 50=Refresh, 49=Save, 42=SQL, 43=Data Transfer, 59=Call API, 57=Cmd Formula, 53=Navigate Search, 74=Print From Message Template, 200=Composition, 60/61/62=Send Message, 63=Send SMS, 205=Validate Required. Type 74→messageTemplateId. Type 42→notificationMessage+sqlToken. Type 57→executionFormulaUI+formulaToken. Type 53→linkedSearchId. Type 43→optionsJson.dataTransferSettingId from upsert_data_transfer (UI Data Transfer Setting column — NOT ParamId1); optional isNeedToOpenNewForm. Send Message 60–62: notificationSubject + notificationMessage OR isUseRichTextMessageTemplate:true + messageTemplateId; destination=userIdFieldId (60) / emailAddressFieldId (61) / partnerIdFieldId|toEmailAddress (62); optional isAttachAllFormFilesToMessage. Message body: real line breaks (JSON \\n → newline) or HTML <br /> — never literal backslash-n text. Message Link childActionList = Partner 62 only (not 61). Type 63 SMS: smsMessageToPhoneNumberFiledId + notificationMessage. Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_command';
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Create/update Internal Data Transfer. Types: 1=ToMessageTemplate, 2=DM→DM (needs destinationTransactionId), 3=Api→DM, 4=DM→Api. mappingsJson FULL REPLACE [{sourceFieldId,targetFieldId,…}]; [] clears. targetFieldId MUST exist on destinationTransactionId — use destinationFields / find_fields(transactionId=destination). Never invent ids. Creating a setting alone does NOT set Operation→Data Transfer Setting (stays None). Pass bindToCommandId=<Execute Data Transfer command id> (+ optional isNeedToOpenNewForm) to bind in the same save, OR upsert_command(optionsJson.dataTransferSettingId). Do not claim success until get_command_detail shows dataTransferSettingId. Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_data_transfer';
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'One Data Transfer: header + mappings + mappingHints + sourceFields/destinationFields catalogs (fieldId,name,dbName,unitName). For type 2 map only using destinationFields[].fieldId. Never invent targetFieldId.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_transfer_detail';
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Search fields by display/DB name. Default=HostContext forms. Pass transactionId to search another Data Model (e.g. Data Transfer destinationTransactionId). Returns fieldId + tokens. Never invent field ids.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'find_fields';
GO


