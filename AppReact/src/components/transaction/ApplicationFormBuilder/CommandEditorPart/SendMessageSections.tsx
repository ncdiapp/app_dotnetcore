import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export const EmAppTransactionCommandTypeSendMessageToTransFieldUserId = 60;
export const EmAppTransactionCommandTypeSendMessageToTransFieldEmailAddress = 61;
export const EmAppTransactionCommandTypeSendMessageToTransFieldPartnerId = 62;
export const EmAppTransactionCommandTypeSendSmsToTransFieldPhoneNumber = 63;

function FieldSelect(props: {
  label: string;
  value: any;
  options: any[];
  onChange: (v: number | null) => void;
}) {
  const { theme } = useTheme();
  const { label, value, options, onChange } = props;
  return (
    <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
      <label className={`text-xs ${theme.label}`}>{label}</label>
      <select
        className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
        value={value == null ? '' : String(value)}
        onChange={(e) => onChange(e.target.value ? Number(e.target.value) : null)}
      >
        <option value="">(None)</option>
        {options.map((o: any) => (
          <option key={o.Id} value={String(o.Id)}>
            {o.ShortDisplay ?? o.Display2 ?? o.Display ?? o.Name ?? String(o.Id)}
          </option>
        ))}
      </select>
    </div>
  );
}

/** Destination fields for send-message/SMS command types 60–63 (inside Notification Message section). */
export function SendMessageDestinationFields(props: {
  action: any;
  transactionFieldLookUpList: any[];
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { action, transactionFieldLookUpList, onMarkChange } = props;

  if (!action) return null;
  const actionType = Number(action.ActionType);

  const isPartner = actionType === EmAppTransactionCommandTypeSendMessageToTransFieldPartnerId;
  const isUserId = actionType === EmAppTransactionCommandTypeSendMessageToTransFieldUserId;
  const isEmail = actionType === EmAppTransactionCommandTypeSendMessageToTransFieldEmailAddress;
  const isSms = actionType === EmAppTransactionCommandTypeSendSmsToTransFieldPhoneNumber;

  if (!isPartner && !isUserId && !isEmail && !isSms) return null;

  const attr = (action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] });
  const options = transactionFieldLookUpList || [];

  return (
    <>
      {isPartner ? (
        <FieldSelect
          label="Message To Partner Field"
          value={attr.NotificationDestinationPartnerIdTransactionFiledId}
          options={options}
          onChange={(v) => {
            attr.NotificationDestinationPartnerIdTransactionFiledId = v;
            onMarkChange();
          }}
        />
      ) : null}

      {isUserId ? (
        <FieldSelect
          label="Message To UserId Field"
          value={attr.NotificationDestinationPartnerIdTransactionFiledId}
          options={options}
          onChange={(v) => {
            // Angular binds UserId to the same property as PartnerId for this action type.
            attr.NotificationDestinationPartnerIdTransactionFiledId = v;
            onMarkChange();
          }}
        />
      ) : null}

      {isEmail ? (
        <FieldSelect
          label="Email Address Field"
          value={attr.NotificationDestinationEmailAddressTransactionFiledId}
          options={options}
          onChange={(v) => {
            attr.NotificationDestinationEmailAddressTransactionFiledId = v;
            onMarkChange();
          }}
        />
      ) : null}

      {isPartner ? (
        <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>Aditional To Email Address</label>
          <input
            type="text"
            className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            value={attr.ToEmailAddress ?? ''}
            onChange={(e) => {
              attr.ToEmailAddress = e.target.value;
              onMarkChange();
            }}
          />
        </div>
      ) : null}

      {isSms ? (
        <FieldSelect
          label="Phone Number Field"
          value={attr.SmsMessageToPhoneNumberFiledId}
          options={options}
          onChange={(v) => {
            attr.SmsMessageToPhoneNumberFiledId = v;
            onMarkChange();
          }}
        />
      ) : null}
    </>
  );
}

