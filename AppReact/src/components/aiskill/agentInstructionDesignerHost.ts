export const AGENT_INSTRUCTION_DESIGNER_SKILL_KEY = 'agent-instruction-designer';

export function isAgentInstructionDesignerKey(code: string | null | undefined): boolean {
  return (code || '').trim().toLowerCase() === AGENT_INSTRUCTION_DESIGNER_SKILL_KEY;
}

/** Slim HostContext for Instruction Designer — full prompt/tools via host tools. */
export function buildInstructionDesignerHostContext(args: {
  targetAgentCode: string;
  targetDisplayName: string;
  isNew: boolean;
  dirty: boolean;
  systemPrompt: string;
  subscribedLibraryKeys: string[];
  excludedTools: Array<{ libraryKey: string; toolName: string }>;
  executionMode?: string;
  maxToolResultChars?: number;
}): unknown {
  const prompt = args.systemPrompt || '';
  const previewMax = 480;
  return {
    surface: 'agent-management',
    mode: 'instruction-designer',
    targetAgentCode: args.targetAgentCode || null,
    targetDisplayName: args.targetDisplayName || null,
    isNew: !!args.isNew,
    dirty: !!args.dirty,
    systemPromptChars: prompt.length,
    systemPromptPreview: prompt.length <= previewMax ? prompt : prompt.slice(0, previewMax) + '…',
    subscribedLibraryKeys: args.subscribedLibraryKeys || [],
    excludedTools: args.excludedTools || [],
    privateBuiltInToolNames: [],
    executionMode: args.executionMode || 'Interactive',
    limits: { maxToolResultChars: args.maxToolResultChars ?? 4000 },
  };
}
