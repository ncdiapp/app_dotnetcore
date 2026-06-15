import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export const EmAppTransactionCommandTypeQuickCreateUser = 70;

function TransFieldSelect(props: {
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
            {o.ShortDisplay ?? o.Display ?? String(o.Id)}
          </option>
        ))}
      </select>
    </div>
  );
}

export function QuickCreateUserSection(props: { action: any; rootLevelTransFieldLookUpList: any[]; onMarkChange: () => void }) {
  const { action, rootLevelTransFieldLookUpList, onMarkChange } = props;

  if (!action) return null;
  if (Number(action.ActionType) !== EmAppTransactionCommandTypeQuickCreateUser) return null;

  const attr = (action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] });

  // Angular uses separate CVs (UserTypeTransFieldCV/UserNameTransFieldCV/...) but all derive from same transFieldLookUpList.
  const options = rootLevelTransFieldLookUpList || [];

  return (
    <>
      <TransFieldSelect
        label="User Type Field"
        value={attr.UserTypeTransFieldId}
        options={options}
        onChange={(v) => {
          attr.UserTypeTransFieldId = v;
          onMarkChange();
        }}
      />
      <TransFieldSelect
        label="User Name Field"
        value={attr.UserNameTransFieldId}
        options={options}
        onChange={(v) => {
          attr.UserNameTransFieldId = v;
          onMarkChange();
        }}
      />
      <TransFieldSelect
        label="User Password Field"
        value={attr.UserPasswordTransFieldId}
        options={options}
        onChange={(v) => {
          attr.UserPasswordTransFieldId = v;
          onMarkChange();
        }}
      />
      <TransFieldSelect
        label="User Email Field"
        value={attr.UserEmailTransFieldId}
        options={options}
        onChange={(v) => {
          attr.UserEmailTransFieldId = v;
          onMarkChange();
        }}
      />
    </>
  );
}

