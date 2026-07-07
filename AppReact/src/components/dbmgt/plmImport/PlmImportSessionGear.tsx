import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import type { PlmImportWizardState } from './types';

export type PlmImportSessionGearProps = {
  state: PlmImportWizardState;
  onDiscardSession: () => Promise<void>;
};

const PlmImportSessionGear: React.FC<PlmImportSessionGearProps> = ({
  state,
  onDiscardSession,
}) => {
  const { theme } = useTheme();
  const [sessionPanelOpen, setSessionPanelOpen] = useState(false);
  const [isDiscarding, setIsDiscarding] = useState(false);
  const sessionPanelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!sessionPanelOpen) return undefined;
    const onDocClick = (e: MouseEvent) => {
      if (sessionPanelRef.current && !sessionPanelRef.current.contains(e.target as Node)) {
        setSessionPanelOpen(false);
      }
    };
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, [sessionPanelOpen]);

  const handleDiscardSession = useCallback(async () => {
    if (isDiscarding) return;
    if (!window.confirm(
      'Discard this import session and start fresh?\n\nSaved progress (connection, table/entity import state) will no longer be resumed.',
    )) return;
    setIsDiscarding(true);
    try {
      await onDiscardSession();
      setSessionPanelOpen(false);
    } finally {
      setIsDiscarding(false);
    }
  }, [isDiscarding, onDiscardSession]);

  return (
    <div className="relative flex-none shrink-0" ref={sessionPanelRef}>
      <button
        type="button"
        className={`inline-flex items-center justify-center w-8 h-8 rounded-[4px] ${theme.button_default}`}
        title="Import session"
        onClick={() => setSessionPanelOpen((open) => !open)}
        aria-expanded={sessionPanelOpen}
        aria-haspopup="dialog"
      >
        <i className="fa-solid fa-gear" />
      </button>
      {sessionPanelOpen && (
        <div
          className={`absolute right-0 top-full mt-1 z-30 w-72 rounded border shadow-lg p-3 flex flex-col gap-2 ${theme.inputBox}`}
          role="dialog"
          aria-label="Import session"
        >
          <div className={`text-xs font-semibold ${theme.label}`}>Import session</div>
          {state.session?.SessionId ? (
            <>
              <div className={`text-xs ${theme.menu_secondary}`}>
                <div>
                  Session #
                  {state.session.SessionId}
                  {state.session.SessionStatus ? ` · ${state.session.SessionStatus}` : ''}
                </div>
                {state.session.SaasApplicationName && (
                  <div className="mt-1 truncate" title={state.session.SaasApplicationName}>
                    Application:
                    {' '}
                    {state.session.SaasApplicationName}
                  </div>
                )}
                <div className="mt-1">
                  Table prefix:
                  {' '}
                  <span className="font-mono">{state.tablePrefix}</span>
                </div>
                {state.session.CurrentStepCode && (
                  <div className="mt-1">
                    Current step:
                    {' '}
                    {state.session.CurrentStepCode}
                  </div>
                )}
                {state.session.UpdatedAt && (
                  <div className="mt-1">
                    Updated:
                    {' '}
                    {new Date(state.session.UpdatedAt).toLocaleString()}
                  </div>
                )}
              </div>
              <button
                type="button"
                className={`w-full inline-flex items-center justify-center gap-1 px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
                onClick={handleDiscardSession}
                disabled={isDiscarding}
                title="Mark this in-progress session as completed and start a new import"
              >
                <i className={`fa-solid ${isDiscarding ? 'fa-spinner fa-spin' : 'fa-trash'}`} />
                Discard Session
              </button>
            </>
          ) : (
            <div className={`text-xs ${theme.menu_secondary}`}>No active import session.</div>
          )}
        </div>
      )}
    </div>
  );
};

export default PlmImportSessionGear;
