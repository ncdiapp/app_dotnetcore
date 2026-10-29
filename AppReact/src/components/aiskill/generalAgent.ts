export const GENERAL_AGENT_KEY = '__general_agent__';
export const LEGACY_GENERAL_AGENT_KEY = '__universal_agent__';
export const GENERAL_AGENT_ROUTE = '/general-agent';
export const STAY_IN_GENERAL_MARKER = '[stay_in_general]';

export const isGeneralAgentKey = (skillKey?: string): boolean => {
    const key = (skillKey || '').trim().toLowerCase();
    return key === GENERAL_AGENT_KEY || key === LEGACY_GENERAL_AGENT_KEY;
};
