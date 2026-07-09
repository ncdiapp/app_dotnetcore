import React, { useCallback, useMemo } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { getStepIndex, PLM_IMPORT_STEPS } from './plmImportStepRegistry';
import type { PlmImportEntityStepUiState, PlmImportStepCode, PlmImportDwBlueprintStepUiState, PlmImportWizardState } from './types';
import { normalizePlmImportStepCode } from './types';
import ConnectionStep from './steps/ConnectionStep';
import EntityStep from './steps/EntityStep';
import DwBlueprintStep from './steps/DwBlueprintStep';
import FolderImportStep from './steps/FolderImportStep';
import ColorImportStep from './steps/ColorImportStep';
import PomImportStep from './steps/PomImportStep';
import ImageImportStep from './steps/ImageImportStep';
import PlmImportSessionGear from './PlmImportSessionGear';

export type PlmImportWizardProps = {
  state: PlmImportWizardState;
  entityStepUi: PlmImportEntityStepUiState;
  dwBlueprintStepUi: PlmImportDwBlueprintStepUiState;
  isSysAdmin: boolean;
  onStateChange: (patch: Partial<PlmImportWizardState>) => void;
  onEntityStepUiChange: (patch: Partial<PlmImportEntityStepUiState>) => void;
  onDwBlueprintStepUiChange: (patch: Partial<PlmImportDwBlueprintStepUiState>) => void;
  onReloadSession: () => void;
  onDiscardSession: () => Promise<void>;
};

const PlmImportWizard: React.FC<PlmImportWizardProps> = ({
  state,
  entityStepUi,
  dwBlueprintStepUi,
  isSysAdmin,
  onStateChange,
  onEntityStepUiChange,
  onDwBlueprintStepUiChange,
  onReloadSession,
  onDiscardSession,
}) => {
  const { theme } = useTheme();

  const activeStepCode = normalizePlmImportStepCode(state.currentStepCode);
  const currentIndex = getStepIndex(activeStepCode);

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

  const getStepTabClass = (isActive: boolean, isLocked: boolean) => {
    if (isLocked) return `${theme.tab} opacity-40 cursor-not-allowed`;
    return isActive ? theme.tab_active : theme.tab;
  };

  const navButtonClass = (disabled: boolean) => {
    const base = `inline-flex items-center justify-center gap-2 min-w-[132px] px-5 py-2.5 text-sm font-semibold rounded-[4px] ${theme.button_secondary}`;
    return disabled ? `${base} opacity-40 cursor-not-allowed` : base;
  };

  const renderStep = () => {
    switch (activeStepCode) {
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
      case 'DwBlueprint':
        return (
          <DwBlueprintStep
            state={state}
            dwBlueprintStepUi={dwBlueprintStepUi}
            onStateChange={onStateChange}
            onDwBlueprintStepUiChange={onDwBlueprintStepUiChange}
            onSessionSaved={onReloadSession}
          />
        );
      case 'FolderImport':
        return <FolderImportStep state={state} />;
      case 'ColorImport':
        return <ColorImportStep state={state} />;
      case 'PomImport':
        return <PomImportStep state={state} />;
      case 'ImageImport':
        return <ImageImportStep state={state} />;
      default:
        return null;
    }
  };

  return (
    <div className="flex flex-col h-full w-full overflow-hidden">
      {/* Step header */}
      <div className={`flex-none px-4 py-3 border-b ${theme.mainContentSection}`}>
        <div className="flex items-start justify-between gap-3">
          <div className="flex flex-wrap gap-0 min-w-0 flex-auto">
            {PLM_IMPORT_STEPS.map((step, idx) => {
              const isActive = step.code === activeStepCode;
              const isLocked = !canAccessStep(idx);
              return (
                <button
                  key={step.code}
                  type="button"
                  onClick={() => goToStep(step.code)}
                  disabled={isLocked}
                  className={`px-3 py-2 text-xs border-b-2 whitespace-nowrap ${getStepTabClass(isActive, isLocked)}`}
                  title={isLocked ? 'Connect to PLM successfully before opening this step.' : step.description}
                >
                  <i className={`${step.icon} mr-1`} />
                  {idx + 1}. {step.label}
                </button>
              );
            })}
          </div>
          <PlmImportSessionGear state={state} onDiscardSession={onDiscardSession} />
        </div>
      </div>

      {/* Step body */}
      <div className="h-1 flex-auto overflow-hidden">
        {renderStep()}
      </div>

      {/* Footer nav */}
      <div className={`flex-none flex items-center justify-center px-4 py-3 border-t ${theme.mainContentSection}`}>
        <div className="flex items-center gap-4">
          <button
            type="button"
            className={navButtonClass(currentIndex <= 0)}
            onClick={handlePrev}
            disabled={currentIndex <= 0}
          >
            <i className="fa-solid fa-chevron-left" />
            Previous
          </button>
          <button
            type="button"
            className={navButtonClass(currentIndex >= PLM_IMPORT_STEPS.length - 1 || !canGoNext)}
            onClick={handleNext}
            disabled={currentIndex >= PLM_IMPORT_STEPS.length - 1 || !canGoNext}
          >
            Next
            <i className="fa-solid fa-chevron-right" />
          </button>
        </div>
      </div>
    </div>
  );
};

export default PlmImportWizard;
