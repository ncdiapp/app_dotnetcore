/** Chat UI modules unlocked by subscribed tool libraries (not AppAgentSkillSet.AgentUi). */
export type AgentChatUiModule = 'files';

const LIBRARY_CHAT_MODULES: Record<string, AgentChatUiModule> = {
    'agent-files': 'files',
    'platform-document-export': 'files',
};

export function chatModulesFromLibraries(libraryKeys: string[]): Set<AgentChatUiModule> {
    const mods = new Set<AgentChatUiModule>();
    for (let i = 0; i < libraryKeys.length; i++) {
        const mod = LIBRARY_CHAT_MODULES[libraryKeys[i]];
        if (mod) mods.add(mod);
    }
    return mods;
}

/** Files opens when at least one tool in a files library is enabled (subscribed and not excluded). */
export function chatModulesFromEnabledTools(
    libraryKeys: string[],
    toolsByLibrary: { libraryKey: string; toolNames: string[] }[],
    excludedKeys: Set<string>,
): Set<AgentChatUiModule> {
    const mods = new Set<AgentChatUiModule>();
    const names = new Map<string, string[]>();
    for (let i = 0; i < toolsByLibrary.length; i++) {
        names.set(toolsByLibrary[i].libraryKey, toolsByLibrary[i].toolNames);
    }
    for (let i = 0; i < libraryKeys.length; i++) {
        const key = libraryKeys[i];
        const mod = LIBRARY_CHAT_MODULES[key];
        if (!mod) continue;
        const toolNames = names.get(key) ?? [];
        for (let t = 0; t < toolNames.length; t++) {
            if (!excludedKeys.has(`${key}\u0001${toolNames[t]}`)) {
                mods.add(mod);
                break;
            }
        }
    }
    return mods;
}
