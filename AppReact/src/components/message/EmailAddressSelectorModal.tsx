import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';

export type ContactGroupOption = { Id: number; Display: string };

export type EmailUserRow = {
  userId: string;
  display: string;
  userName: string;
  email: string;
  contactGroupIds: number[];
};

type EmailAddressSelectorModalProps = {
  open: boolean;
  onClose: () => void;
  /** Raw rows from `retrieveCurrentUserAvailableEmailToUsers` (must include Email for list) */
  rawUsers: any[] | null;
  initialFollowerUserIds: string[];
  onApply: (followupUserIdList: string[]) => Promise<void>;
  isBusy: boolean;
};

function buildRows(rawUsers: any[] | null): { rows: EmailUserRow[]; contactGroups: ContactGroupOption[] } {
  const dict: Record<number, ContactGroupOption> = {};
  const groups: ContactGroupOption[] = [{ Id: -1, Display: 'All' }];
  const rows: EmailUserRow[] = [];

  (rawUsers || []).forEach((userDto: any) => {
    const email = userDto?.Email ?? userDto?.email ?? '';
    if (!email) return;
    const uid = String(userDto?.Id ?? userDto?.id ?? '');
    if (!uid) return;

    const contactGroupIds: number[] = [];
    const ucg = userDto?.UserContactGroups ?? userDto?.userContactGroups;
    if (Array.isArray(ucg)) {
      ucg.forEach((g: any) => {
        const gid = g?.Id ?? g?.id;
        if (gid == null) return;
        const n = Number(gid);
        contactGroupIds.push(n);
        const disp = g?.Display ?? g?.display ?? g?.GroupName ?? `Group ${n}`;
        if (!dict[n]) {
          dict[n] = { Id: n, Display: String(disp) };
          groups.push(dict[n]);
        }
      });
    }

    const userName = userDto?.UserName ?? userDto?.userName ?? userDto?.Display ?? uid;
    rows.push({
      userId: uid,
      display: `${userName} [${email}]`,
      userName: String(userName),
      email: String(email),
      contactGroupIds
    });
  });

  return { rows, contactGroups: groups };
}

/**
 * Angular parity: "Email Address Selector" for conversation followers (contact group, filter, checkboxes, add).
 */
const EmailAddressSelectorModal: React.FC<EmailAddressSelectorModalProps> = ({
  open,
  onClose,
  rawUsers,
  initialFollowerUserIds,
  onApply,
  isBusy
}) => {
  const { theme, t } = useTheme();

  const { rows, contactGroups } = useMemo(() => buildRows(rawUsers), [rawUsers]);

  const [filterByContactGroupId, setFilterByContactGroupId] = useState(-1);
  const [filterText, setFilterText] = useState('');
  const [checkedIds, setCheckedIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (!open) return;
    setFilterByContactGroupId(-1);
    setFilterText('');
    setCheckedIds(new Set(initialFollowerUserIds));
  }, [open, initialFollowerUserIds]);

  const visibleRows = useMemo(() => {
    return rows.filter((r) => {
      if (filterByContactGroupId !== -1 && !r.contactGroupIds.includes(filterByContactGroupId)) {
        return false;
      }
      if (filterText.trim()) {
        const q = filterText.trim().toLowerCase();
        if (
          !r.userName.toLowerCase().includes(q) &&
          !r.email.toLowerCase().includes(q) &&
          !r.display.toLowerCase().includes(q)
        ) {
          return false;
        }
      }
      return true;
    });
  }, [rows, filterByContactGroupId, filterText]);

  const allVisibleChecked = useMemo(() => {
    if (visibleRows.length === 0) return false;
    return visibleRows.every((r) => checkedIds.has(r.userId));
  }, [visibleRows, checkedIds]);

  const someVisibleChecked = useMemo(() => {
    return visibleRows.some((r) => checkedIds.has(r.userId));
  }, [visibleRows, checkedIds]);

  const selectAllRef = useRef<HTMLInputElement>(null);
  useEffect(() => {
    const el = selectAllRef.current;
    if (!el) return;
    el.indeterminate = someVisibleChecked && !allVisibleChecked;
  }, [someVisibleChecked, allVisibleChecked]);

  const toggleUser = useCallback((userId: string) => {
    setCheckedIds((prev) => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  }, []);

  const handleSelectAllVisible = useCallback(
    (checked: boolean) => {
      setCheckedIds((prev) => {
        const next = new Set(prev);
        visibleRows.forEach((r) => {
          if (checked) next.add(r.userId);
          else next.delete(r.userId);
        });
        return next;
      });
    },
    [visibleRows]
  );

  const handleApply = useCallback(async () => {
    await onApply(Array.from(checkedIds));
    onClose();
  }, [checkedIds, onApply, onClose]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-[8000] flex items-start justify-center pt-16 px-4 bg-black/40"
      onMouseDown={(e) => e.target === e.currentTarget && !isBusy && onClose()}
      role="dialog"
      aria-modal="true"
      aria-labelledby="email-address-selector-title"
    >
      <div
        className={`w-full max-w-lg max-h-[min(560px,80vh)] overflow-hidden flex flex-col rounded border shadow-lg ${theme.mainContentSection} ${t('border_mainContentSection')}`}
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div className={`shrink-0 px-3 py-2 border-b flex justify-between items-center ${t('border_mainContentSection')}`}>
          <div id="email-address-selector-title" className={`text-sm font-semibold ${theme.title}`}>
            Email Address Selector
          </div>
          <button
            type="button"
            className={`px-2 py-0.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={onClose}
            disabled={isBusy}
            aria-label="Close"
          >
            ×
          </button>
        </div>

        <div className="shrink-0 px-3 pt-3 pb-2 space-y-2">
          <div className="flex items-center gap-2">
            <label className={`w-28 shrink-0 text-xs ${theme.label}`}>Contact Group</label>
            <select
              className={`flex-1 min-w-0 h-7 px-2 text-xs border rounded ${theme.inputBox}`}
              value={filterByContactGroupId}
              onChange={(e) => setFilterByContactGroupId(Number(e.target.value))}
              disabled={isBusy}
            >
              {contactGroups.map((g) => (
                <option key={g.Id} value={g.Id}>
                  {g.Display}
                </option>
              ))}
            </select>
          </div>

          <div className="flex items-center gap-2">
            <label className="flex items-center gap-1 text-xs shrink-0">
              <input
                ref={selectAllRef}
                type="checkbox"
                checked={allVisibleChecked}
                onChange={(e) => handleSelectAllVisible(e.target.checked)}
                disabled={isBusy || visibleRows.length === 0}
              />
              Select All
            </label>
            <div className="min-w-0 flex-1 relative">
              <input
                type="text"
                className={`w-full h-7 pl-7 pr-2 text-xs border rounded ${theme.inputBox}`}
                placeholder="Enter a username or email to filter."
                value={filterText}
                onChange={(e) => setFilterText(e.target.value)}
                disabled={isBusy}
              />
              <i className="fa-solid fa-filter absolute left-2 top-2 text-gray-400 pointer-events-none" aria-hidden />
            </div>
          </div>
        </div>

        <div className={`min-h-0 flex-1 overflow-y-auto mx-3 mb-2 border rounded ${theme.inputBox}`}>
          {visibleRows.length === 0 ? (
            <div className={`p-3 text-xs ${theme.label}`}>No users match the filter.</div>
          ) : (
            visibleRows.map((r) => (
              <div
                key={r.userId}
                className={`flex items-center px-2 py-1.5 border-b last:border-b-0 ${t('border_mainContentSection')}`}
              >
                <input
                  type="checkbox"
                  className="mr-2 shrink-0"
                  checked={checkedIds.has(r.userId)}
                  onChange={() => toggleUser(r.userId)}
                  disabled={isBusy}
                />
                <span
                  className={`text-xs ${theme.label} cursor-pointer select-none`}
                  onClick={() => !isBusy && toggleUser(r.userId)}
                >
                  {r.display}
                </span>
              </div>
            ))
          )}
        </div>

        <div className={`shrink-0 px-3 py-2 border-t flex justify-end gap-2 ${t('border_mainContentSection')}`}>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={onClose}
            disabled={isBusy}
          >
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => void handleApply()}
            disabled={isBusy}
          >
            + Add Selected Emails
          </button>
        </div>
      </div>
    </div>
  );
};

export default EmailAddressSelectorModal;
