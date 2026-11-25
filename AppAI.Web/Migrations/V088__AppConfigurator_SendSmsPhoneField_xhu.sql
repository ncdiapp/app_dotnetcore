-- V088: Send Message / SMS (60–63) agent options for upsert_command.
-- - Destination field ids (phone/email/partner/userId) — previously ignored by ApplyCommandOptions
--   so UI Phone Number / Email Address Field stayed (None).
-- - notificationSubject, isUseRichTextMessageTemplate, isAttachAllFormFilesToMessage, toEmailAddress.
-- - Message Link childActionList = Partner (62) only (UI hides for Email 61).

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Create/update Data Model Operation. actionType=EmAppTransactionCommandType (NOT Link Target 1/2). Common: 50=Refresh, 49=Save, 42=SQL, 43=Data Transfer, 59=Call API, 57=Cmd Formula, 53=Navigate Search, 74=Print From Message Template, 200=Composition, 60/61/62=Send Message, 63=Send SMS, 205=Validate Required. Type 74→messageTemplateId. Type 42→notificationMessage+sqlToken. Type 57→executionFormulaUI+formulaToken. Type 53→linkedSearchId. Send Message 60–62: notificationSubject + notificationMessage OR isUseRichTextMessageTemplate:true + messageTemplateId; destination=userIdFieldId (60) / emailAddressFieldId (61) / partnerIdFieldId|toEmailAddress (62); optional isAttachAllFormFilesToMessage. Message body: real line breaks (JSON \\n → newline) or HTML <br /> — never literal backslash-n text (shows as \n in email). Message Link childActionList = Partner 62 only (not 61). Type 63 SMS: smsMessageToPhoneNumberFiledId (or phoneNumberFieldId) + notificationMessage body. Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_command';
GO
