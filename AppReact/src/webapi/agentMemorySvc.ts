import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export interface AgentMemoryFact {
    FactId:        number;
    SkillKey:      string | null;
    Category:      string;
    FactKey:       string;
    FactValue:     string;
    PreviousValue: string | null;
    /** confirmed | pending (proposed by the extractor, not injected until confirmed) */
    Status:        string;
    /** user | agent | extracted */
    Source:        string;
    UpdatedAt:     string;
}

export interface AgentMemoryEpisode {
    EpisodeId: number;
    SkillKey:  string;
    Summary:   string;
    Topics:    string | null;
    CreatedAt: string;
}

const BASE = `${endpoints.BASE_URL}/webapi/AgentMemory`;

class AgentMemoryService {
    async ListFacts(skillKey: string): Promise<AgentMemoryFact[]> {
        const res = await fetch(`${BASE}/ListFacts?skillKey=${encodeURIComponent(skillKey || '')}`, { headers: getHeaders() });
        if (!res.ok) throw new Error('Could not load saved facts.');
        const data = await res.json();
        return data?.Object ?? [];
    }

    async ListEpisodes(skillKey: string): Promise<AgentMemoryEpisode[]> {
        const res = await fetch(`${BASE}/ListEpisodes?skillKey=${encodeURIComponent(skillKey || '')}`, { headers: getHeaders() });
        if (!res.ok) throw new Error('Could not load remembered conversations.');
        const data = await res.json();
        return data?.Object ?? [];
    }

    async ConfirmFact(factId: number): Promise<boolean> {
        return this.post(`${BASE}/ConfirmFact?factId=${factId}`);
    }

    async UpdateFact(factId: number, value: string): Promise<boolean> {
        return this.post(`${BASE}/UpdateFact`, { FactId: factId, Value: value });
    }

    async DeleteFact(factId: number): Promise<boolean> {
        return this.post(`${BASE}/DeleteFact?factId=${factId}`);
    }

    async DeleteEpisode(episodeId: number): Promise<boolean> {
        return this.post(`${BASE}/DeleteEpisode?episodeId=${episodeId}`);
    }

    async ForgetEverything(): Promise<boolean> {
        return this.post(`${BASE}/ForgetEverything`);
    }

    async SetChatMemoryOptOut(skillKey: string, sessionKey: string, optOut: boolean): Promise<boolean> {
        return this.post(
            `${BASE}/SetChatMemoryOptOut?skillKey=${encodeURIComponent(skillKey || '')}&sessionKey=${encodeURIComponent(sessionKey || '')}&optOut=${optOut}`
        );
    }

    private async post(url: string, body?: unknown): Promise<boolean> {
        const res = await fetch(url, {
            method: 'POST',
            headers: getHeaders(),
            body: body === undefined ? undefined : JSON.stringify(body),
        });
        if (!res.ok) throw new Error('The memory request failed.');
        const data = await res.json();
        return !!data?.Object;
    }
}

export const agentMemorySvc = new AgentMemoryService();
