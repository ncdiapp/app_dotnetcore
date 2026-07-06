import React, { useMemo } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import type { PlmImportWizardState } from '../types';

type FolderImportStepProps = {
  state: PlmImportWizardState;
};

const FolderImportStep: React.FC<FolderImportStepProps> = ({ state }) => {
  const { theme } = useTheme();

  const sessionId = state.session?.SessionId ?? null;

  const plan = useMemo(() => ([
    {
      title: '1) Discover (PLM → folder graph)',
      points: [
        'Read PLM folder list + parent/child relationships (and any “root” folder entry).',
        'Normalize to a single tree: ensure no cycles, fix missing parents, and de-duplicate by PLM folder ID.',
        'Capture metadata needed by APP: Name, Path, ParentId, Sort, and any PLM reference IDs.',
      ],
    },
    {
      title: '2) Map (PLM folder → APP folder)',
      points: [
        'Choose an APP “Root Folder” for the import (e.g. /PLM).',
        'Create a stable mapping table: PlmFolderId → AppFolderId.',
        'Rules: same PLM ID always maps to the same APP folder; rename collisions are resolved deterministically (e.g. “Name (2)”).',
      ],
    },
    {
      title: '3) Preview (dry-run)',
      points: [
        'Show counts: total PLM folders, new folders, existing matched folders, conflicts.',
        'Show a tree preview with create/update actions.',
        'Block execute if root mapping is missing or conflicts are unresolved.',
      ],
    },
    {
      title: '4) Execute (idempotent import)',
      points: [
        'Create missing folders from top → down so parents exist first.',
        'If mapping exists, update folder name/sort if allowed; otherwise keep APP as source of truth.',
        'Record import job + progress (same pattern as Image Import job polling).',
      ],
    },
    {
      title: '5) Link assets (optional, next phase)',
      points: [
        'After Image Import creates AppFile, place files under their mapped folders.',
        'If PLM provides folder-to-sketch/file relations, import those relations and attach AppFile to AppFolder.',
      ],
    },
  ]), []);

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div>
          <h2 className={`text-sm font-semibold ${theme.title}`}>Step 4 — Folder Import</h2>
          <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
            Plan to import PLM folder structures into APP folders.
          </p>
        </div>
        <div className={`text-xs ${theme.menu_secondary}`}>
          Session: {sessionId ? `#${sessionId}` : 'Not saved yet'}
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-auto px-5 py-5 ${theme.mainContentSection}`}>
        <div className={`rounded border p-4 mb-4 ${theme.inputBox}`}>
          <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Import Plan (PLM Folder → APP Folder)</div>
          <div className={`text-xs leading-6 ${theme.menu_secondary}`}>
            This step is UI-first (plan + placeholders). Next we’ll wire backend APIs to preview/execute.
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {plan.map((section) => (
            <div key={section.title} className={`rounded border p-4 ${theme.inputBox}`}>
              <div className={`text-xs font-semibold mb-2 ${theme.label}`}>{section.title}</div>
              <ul className={`text-xs leading-6 list-disc pl-5 ${theme.menu_secondary}`}>
                {section.points.map((p) => <li key={p}>{p}</li>)}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default FolderImportStep;

