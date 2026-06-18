import React, { useCallback, useEffect, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import type {
  PlmTemplateBlockAnalysisDto,
  PlmTemplateBlockStorageOverrideDto,
  PlmTemplateImportSettingDto,
  PlmTemplateSimilarTabGroupDto,
} from '../../../../webapi/plmMigrationSvc';

export type TemplateTabWarningDialogProps = {
  open: boolean;
  tabLabel: string;
  blocks: PlmTemplateBlockAnalysisDto[];
  similarTabGroups: PlmTemplateSimilarTabGroupDto[];
  importSetting: PlmTemplateImportSettingDto | null;
  onClose: () => void;
  onApply: (setting: PlmTemplateImportSettingDto) => void;
};

const TemplateTabWarningDialog: React.FC<TemplateTabWarningDialogProps> = ({
  open,
  tabLabel,
  blocks,
  similarTabGroups,
  importSetting,
  onClose,
  onApply,
}) => {
  const { theme } = useTheme();
  const [blockOverrides, setBlockOverrides] = useState<PlmTemplateBlockStorageOverrideDto[]>([]);
  const [sharedGroups, setSharedGroups] = useState<PlmTemplateSimilarTabGroupDto[]>([]);
  const [contextMenu, setContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    block: PlmTemplateBlockAnalysisDto | null;
  }>({ visible: false, x: 0, y: 0, block: null });
  const [sharedTablePrompt, setSharedTablePrompt] = useState('');

  useEffect(() => {
    if (!open) return;
    setBlockOverrides(importSetting?.BlockStorageOverrides ? [...importSetting.BlockStorageOverrides] : []);
    setSharedGroups(importSetting?.TabSharedTableGroups
      ? importSetting.TabSharedTableGroups.map((g) => ({
          GroupId: g.GroupId,
          PlmTemplateId: 0,
          SuggestedSharedTableName: g.SharedTableName,
          JaccardScore: 0,
          TabIds: g.TabIds ? [...g.TabIds] : [],
          TabLabels: [],
        }))
      : []);
    setSharedTablePrompt('');
  }, [importSetting, open]);

  useEffect(() => {
    if (!contextMenu.visible) return undefined;
    const close = () => setContextMenu({ visible: false, x: 0, y: 0, block: null });
    window.addEventListener('click', close);
    return () => window.removeEventListener('click', close);
  }, [contextMenu.visible]);

  const applyBlockOverride = useCallback((blockId: number, target: 'Root' | 'SharedSibling', sharedName?: string) => {
    setBlockOverrides((prev) => {
      const next = prev.filter((o) => o.BlockId !== blockId);
      next.push({
        BlockId: blockId,
        StorageTarget: target,
        SharedTableName: sharedName || null,
      });
      return next;
    });
    setContextMenu({ visible: false, x: 0, y: 0, block: null });
  }, []);

  const acceptSimilarGroup = useCallback((group: PlmTemplateSimilarTabGroupDto, tableName: string) => {
    if (!tableName.trim() || !group.TabIds?.length) return;
    setSharedGroups((prev) => {
      const filtered = prev.filter((g) => g.GroupId !== group.GroupId);
      filtered.push({
        ...group,
        SuggestedSharedTableName: tableName.trim(),
      });
      return filtered;
    });
  }, []);

  const handleApply = useCallback(() => {
    if (!importSetting) return;
    onApply({
      ...importSetting,
      BlockStorageOverrides: blockOverrides,
      TabSharedTableGroups: sharedGroups.map((g) => ({
        GroupId: g.GroupId,
        SharedTableName: g.SuggestedSharedTableName,
        TabIds: g.TabIds,
      })),
    });
    onClose();
  }, [blockOverrides, importSetting, onApply, onClose, sharedGroups]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div
        className={`w-full max-w-4xl flex flex-col rounded border shadow-lg ${theme.mainContentSection}`}
        style={{ height: 'min(85vh, 720px)', minHeight: '480px' }}
      >
        <div className={`flex-shrink-0 flex items-center justify-between px-4 py-3 border-b ${theme.inputBox}`}>
          <h3 className={`text-sm font-semibold ${theme.label}`}>Tab mapping warnings — {tabLabel}</h3>
          <button type="button" className={`text-xs px-2 py-1 rounded ${theme.button_default}`} onClick={onClose}>
            <i className="fa-solid fa-xmark" />
          </button>
        </div>

        <div className="flex-1 min-h-0 overflow-y-auto p-4 flex flex-col gap-5">
          <section>
            <h4 className={`text-xs font-semibold mb-2 ${theme.label}`}>Blocks referenced by multiple tabs</h4>
            {blocks.length === 0 ? (
              <p className={`text-xs ${theme.menu_secondary}`}>No multi-referenced blocks for this tab.</p>
            ) : (
              <ul className={`text-xs border rounded divide-y ${theme.inputBox}`}>
                {blocks.map((block) => {
                  const override = blockOverrides.find((o) => o.BlockId === block.BlockId);
                  return (
                    <li
                      key={block.BlockId}
                      className={`px-3 py-2 flex items-start justify-between gap-2 cursor-context-menu ${theme.contextMenu}`}
                      onContextMenu={(e) => {
                        e.preventDefault();
                        setSharedTablePrompt(`${block.BlockName || `Block_${block.BlockId}`}_Shared`);
                        setContextMenu({ visible: true, x: e.clientX, y: e.clientY, block });
                      }}
                    >
                      <div>
                        <div className={theme.label}>{block.BlockName} (ID {block.BlockId})</div>
                        <div className={theme.menu_secondary}>
                          Used by {block.ReferencedTabCount} tab(s): {(block.ReferencedTabLabels || []).join('; ')}
                        </div>
                        {override && (
                          <div className={`mt-1 ${theme.label}`}>
                            → {override.StorageTarget === 'Root' ? 'ReferenceBasicInfo' : override.SharedTableName}
                          </div>
                        )}
                      </div>
                      <span className={theme.menu_secondary}>Right-click</span>
                    </li>
                  );
                })}
              </ul>
            )}
          </section>

          <section>
            <h4 className={`text-xs font-semibold mb-2 ${theme.label}`}>Similar tabs (Jaccard ≥ 0.80)</h4>
            {similarTabGroups.length === 0 ? (
              <p className={`text-xs ${theme.menu_secondary}`}>No similar-tab merge suggestions for this tab.</p>
            ) : (
              <ul className="flex flex-col gap-2">
                {similarTabGroups.map((group) => (
                  <li key={group.GroupId} className={`text-xs border rounded p-3 ${theme.inputBox}`}>
                    <div className={theme.label}>
                      Score {(group.JaccardScore * 100).toFixed(0)}% — {(group.TabLabels || []).join(' + ')}
                    </div>
                    <div className="flex items-center gap-2 mt-2">
                      <label className={`w-24 ${theme.label}`}>Shared table</label>
                      <input
                        type="text"
                        className={`h-7 flex-auto w-32 px-2 text-xs border ${theme.inputBox}`}
                        defaultValue={group.SuggestedSharedTableName || ''}
                        id={`shared-${group.GroupId}`}
                      />
                      <button
                        type="button"
                        className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
                        onClick={() => {
                          const el = document.getElementById(`shared-${group.GroupId}`) as HTMLInputElement | null;
                          acceptSimilarGroup(group, el?.value || group.SuggestedSharedTableName || '');
                        }}
                      >
                        Accept merge
                      </button>
                    </div>
                    {sharedGroups.some((g) => g.GroupId === group.GroupId) && (
                      <div className={`mt-1 ${theme.label}`}>Merge accepted for this group.</div>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>

        <div className={`flex-shrink-0 flex justify-end gap-2 px-4 py-3 border-t ${theme.inputBox}`}>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`} onClick={handleApply}>
            Apply to mapping
          </button>
        </div>
      </div>

      {contextMenu.visible && contextMenu.block && (
        <div
          className={`fixed z-[60] min-w-[200px] border rounded shadow py-1 text-xs ${theme.mainContentSection}`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-3 py-2 ${theme.contextMenu}`}
            onClick={() => applyBlockOverride(contextMenu.block!.BlockId, 'Root')}
          >
            Move block to ReferenceBasicInfo
          </button>
          <div className={`px-3 py-2 border-t ${theme.inputBox}`}>
            <input
              type="text"
              className={`w-full h-7 px-2 text-xs border mb-1 ${theme.inputBox}`}
              value={sharedTablePrompt}
              onChange={(e) => setSharedTablePrompt(e.target.value)}
              placeholder="Shared sibling table name"
            />
            <button
              type="button"
              className={`w-full text-left px-1 py-1 ${theme.contextMenu}`}
              onClick={() => applyBlockOverride(contextMenu.block!.BlockId, 'SharedSibling', sharedTablePrompt)}
            >
              Move to shared sibling table
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default TemplateTabWarningDialog;
