/** Chat UI modules unlocked by subscribed tool libraries (not AppAgentSkillSet.AgentUi). */
export type AgentChatUiModule = 'files';

const LIBRARY_CHAT_MODULES: Record<string, AgentChatUiModule> = {
    'agent-files': 'files',
};

export function chatModulesFromLibraries(libraryKeys: string[]): Set<AgentChatUiModule> {
    const mods = new Set<AgentChatUiModule>();
    for (let i = 0; i < libraryKeys.length; i++) {
        const mod = LIBRARY_CHAT_MODULES[libraryKeys[i]];
        if (mod) mods.add(mod);
    }
    return mods;
}
