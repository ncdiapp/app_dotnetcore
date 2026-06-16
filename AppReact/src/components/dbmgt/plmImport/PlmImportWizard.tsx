import React, { useCallback, useMemo } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { getStepByCode, getStepIndex, PLM_IMPORT_STEPS } from './plmImportStepRegistry';
import type { PlmImportEntityStepUiState, PlmImportStepCode, PlmImportWizardState } from './types';
import ConnectionStep from './steps/ConnectionStep';
import EntityStep from './steps/EntityStep';
import TemplateStep from './steps/TemplateStep';
import OtherDataStep from './steps/OtherDataStep';

export type PlmImportWizardProps = {
  state: PlmImportWizardState;
  entityStepUi: PlmImportEntityStepUiState;
  isSysAdmin: boolean;
  onStateChange: (patch: Partial<PlmImportWizardState>) => void;
  onEntityStepUiChange: (patch: Partial<PlmImportEntityStepUiState>) => void;
  onReloadSession: () => void;
};

const PlmImportWizard: React.FC<PlmImportWizardProps> = ({
  state,
  entityStepUi,
  isSysAdmin,
  onStateChange,
  onEntityStepUiChange,
  onReloadSession,
}) => {
  const { theme } = useTheme();

  const currentIndex = getStepIndex(state.currentStepCode);

  const isPlmConnected = state.connectionTested;

  const canAccessStep = useCallback((idx: number) => idx === 0 || isPlmConnected, [isPlmConnected]);

  const canGoNext = useMemo(() => {
    if (state.currentStepCode === 'Connect') {
      return isPlmConnected && Boolean(state.saasApplicationId);
    }
    return true;
  }, [state.currentStepCode, state.saasApplicationId, isPlmConnected]);

  const goToStep = useCallback((code: PlmImportStepCode) => {
    const idx = getStepIndex(code);
    if (!canAccessStep(idx)) return;
    onStateChange({ currentStepCode: code });
  }, [canAccessStep, onStateChange]);

  const handlePrev = useCallback(() => {
    if (currentIndex > 0) {
      goToStep(PLM_IMPORT_STEPS[currentIndex - 1].code);
    }
  }, [currentIndex, goToStep]);

  const handleNext = useCallback(() => {
    if (currentIndex < PLM_IMPORT_STEPS.length - 1) {
      goToStep(PLM_IMPORT_STEPS[currentIndex + 1].code);
    }
  }, [currentIndex, goToStep]);

  const renderStep = () => {
    switch (state.currentStepCode) {
      case 'Connect':
        return (
          <ConnectionStep
            state={state}
            isSysAdmin={isSysAdmin}
            onStateChange={onStateChange}
            onSessionSaved={onReloadSession}
          />
        );
      case 'Entity':
        return (
          <EntityStep
            state={state}
            entityStepUi={entityStepUi}
            onStateChange={onStateChange}
            onEntityStepUiChange={onEntityStepUiChange}
            onSessionSaved={onReloadSession}
          />
        );
      case 'Template':
        return <TemplateStep />;
      case 'OtherData':
        return <OtherDataStep />;
      default:
        return null;
    }
  };

  const activeStep = getStepByCode(state.currentStepCode);

  return (
    <div className="flex flex-col h-full w-full overflow-hidden">
      {/* Step header */}
      <div className={`flex-none px-4 py-3 border-b ${theme.mainContentSection}`}>
        <div className="flex flex-wrap gap-2">
          {PLM_IMPORT_STEPS.map((step, idx) => {
            const isActive = step.code === state.currentStepCode;
            const isDone = idx < currentIndex;
            const isLocked = !canAccessStep(idx);
            return (
              <button
                key={step.code}
                type="button"
                onClick={() => goToStep(step.code)}
                disabled={isLocked}
                className={`px-2 py-1 text-xs rounded-[4px] ${
                  isActive ? theme.tab_active : isDone ? theme.button_secondary : theme.tab
                } ${isLocked ? 'opacity-40 cursor-not-allowed' : ''}`}
                title={isLocked ? 'Connect to PLM successfully before opening this step.' : step.description}
              >
                <i className={`${step.icon} mr-1`} />
                {idx + 1}. {step.label}
              </button>
            );
          })}
        </div>
        {state.session?.SessionId && (
          <div className={`text-[10px] mt-2 ${theme.menu_secondary}`}>
            Session #{state.session.SessionId}
            {state.session.SessionStatus ? ` · ${state.session.SessionStatus}` : ''}
          </div>
        )}
      </div>

      {/* Step body */}
      <div className="h-1 flex-auto overflow-hidden">
        {renderStep()}
      </div>

      {/* Footer nav */}
      <div className={`flex-none flex items-center justify-between px-4 py-2 border-t ${theme.mainContentSection}`}>
        <div className={`text-xs ${theme.label}`}>
          {activeStep?.label}
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}
            onClick={handlePrev}
            disabled={currentIndex <= 0}
          >
            Previous
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={handleNext}
            disabled={currentIndex >= PLM_IMPORT_STEPS.length - 1 || !canGoNext}
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
};

export default PlmImportWizard;
