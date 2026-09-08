/** EmAppAgentUi — aligns with APP.Components.Dto.EmAppAgentUi */
export const EmAppAgentUi = {
  Unspecified: 0,
  GenericChat: 1,
  ConfigurationAndIntegration: 2,
  DbManagement: 3,
  ImageAndFileProcess: 4,
} as const;

export type EmAppAgentUiValue = (typeof EmAppAgentUi)[keyof typeof EmAppAgentUi];

export const AGENT_UI_OPTIONS: { value: number; label: string }[] = [
  { value: EmAppAgentUi.GenericChat, label: 'Generic Chat' },
  { value: EmAppAgentUi.ConfigurationAndIntegration, label: 'Configuration and Integration' },
  { value: EmAppAgentUi.DbManagement, label: 'DB Management' },
  { value: EmAppAgentUi.ImageAndFileProcess, label: 'Image and File Process' },
];

/** 0 / null / undefined → GenericChat */
export function resolveAgentUi(agentUi: number | null | undefined): number {
  if (agentUi == null || agentUi === EmAppAgentUi.Unspecified) return EmAppAgentUi.GenericChat;
  return agentUi;
}
