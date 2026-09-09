import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export interface AppAgentSkillSetDto {
    SkillKey:           string;
    DisplayName:        string;
    Description:        string;
    SystemPrompt:       string;
    CapabilityFlags:    number;
    IsActive:           boolean;
    SortOrder:          number;
    Version:            number;
    MaxHistoryTokens:   number;
    SummarizeThreshold: number;
    MaxToolResultChars: number;
    RecentWindowSize:   number;
    MaxIterations:      number;
    ExecutionMode:      string;
    /** EmAppAgentUi: 0 Unspecified, 1 GenericChat, 2 ConfigurationAndIntegration, 3 DbManagement, 4 ImageAndFileProcess */
    AgentUi:            number;
}

export interface AppAgentToolRegisterDto {
    Id:          number;
    SkillKey:    string;
    ToolName:    string;
    Description: string;
    ToolType:    string;
    ToolConfig:  string;
    IsActive:    boolean;
    SortOrder:   number;
}

export interface AppAgentMcpServerDto {
    McpServerId: number;
    SkillKey:    string;
    ServerName:  string;
    ServerType:  string;
    ServerUrl:   string;
    Command:     string;
    IsActive:    boolean;
}

// Tool Library DTOs
export interface AppAgentToolDomainDto {
    DomainKey:   string;
    DomainName:  string;
    Description: string;
    SortOrder:   number;
    IsActive:    boolean;
}

export interface AppAgentToolLibraryDto {
    LibraryKey:   string;
    DomainKey:    string;
    LibraryName:  string;
    Description:  string;
    ToolCategory: string;
    IsActive:     boolean;
    ToolCount:    number;
}

export interface AppAgentLibrarySubscriptionDto {
    SkillKey:   string;
    LibraryKey: string;
}

export interface LibraryToolPreviewDto {
    ToolName:        string;
    ToolDescription: string;
    ToolConfig?:     string;
}

export interface AppAgentPromptHistoryDto {
    HistoryId:    number;
    SkillKey:     string;
    SystemPrompt: string;
    SavedAt:      string;
    SavedBy?:     string;
}

export interface GenerateAgentResult {
    SystemPrompt:                string;
    RecommendedLibraryKeys:      string[];
    RecommendedBuiltInToolNames: string[];
}

interface OperationResult<T> {
    Object:           T;
    ValidationResult: { Items: Array<{ Message: string }>; IsValid: boolean };
    IsSuccessful:     boolean;
}

const BASE = `${endpoints.BASE_URL}/webapi/AgentSkillSet`;

class AgentSkillSetService {
    async GetAllSkillSets(): Promise<OperationResult<AppAgentSkillSetDto[]>> {
        const res = await fetch(`${BASE}/GetAllSkillSets`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetAllSkillSets failed (${res.status})`);
        return res.json();
    }

    async UpsertSkillSet(dto: AppAgentSkillSetDto): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/UpsertSkillSet`, {
            method: 'POST', headers: getHeaders(), body: JSON.stringify(dto),
        });
        if (!res.ok) throw new Error(`UpsertSkillSet failed (${res.status})`);
        return res.json();
    }

    async DeleteSkillSet(skillKey: string): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/DeleteSkillSet?skillKey=${encodeURIComponent(skillKey)}`, {
            method: 'DELETE', headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`DeleteSkillSet failed (${res.status})`);
        return res.json();
    }

    async GetToolsBySkillKey(skillKey: string): Promise<OperationResult<AppAgentToolRegisterDto[]>> {
        const res = await fetch(`${BASE}/GetToolsBySkillKey?skillKey=${encodeURIComponent(skillKey)}`, {
            headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`GetToolsBySkillKey failed (${res.status})`);
        return res.json();
    }

    async UpsertTool(dto: AppAgentToolRegisterDto): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/UpsertTool`, {
            method: 'POST', headers: getHeaders(), body: JSON.stringify(dto),
        });
        if (!res.ok) throw new Error(`UpsertTool failed (${res.status})`);
        return res.json();
    }

    async DeleteTool(id: number): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/DeleteTool?id=${id}`, {
            method: 'DELETE', headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`DeleteTool failed (${res.status})`);
        return res.json();
    }

    async GetAllMcpServers(): Promise<OperationResult<AppAgentMcpServerDto[]>> {
        const res = await fetch(`${BASE}/GetAllMcpServers`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetAllMcpServers failed (${res.status})`);
        return res.json();
    }

    async UpsertMcpServer(dto: AppAgentMcpServerDto): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/UpsertMcpServer`, {
            method: 'POST', headers: getHeaders(), body: JSON.stringify(dto),
        });
        if (!res.ok) throw new Error(`UpsertMcpServer failed (${res.status})`);
        return res.json();
    }

    async DeleteMcpServer(mcpServerId: number): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/DeleteMcpServer?mcpServerId=${mcpServerId}`, {
            method: 'DELETE', headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`DeleteMcpServer failed (${res.status})`);
        return res.json();
    }

    // ── Tool Library — Domains ────────────────────────────────────────────

    async GetAllDomains(): Promise<OperationResult<AppAgentToolDomainDto[]>> {
        const res = await fetch(`${BASE}/GetAllDomains`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetAllDomains failed (${res.status})`);
        return res.json();
    }

    async UpsertDomain(dto: AppAgentToolDomainDto): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/UpsertDomain`, {
            method: 'POST', headers: getHeaders(), body: JSON.stringify(dto),
        });
        if (!res.ok) throw new Error(`UpsertDomain failed (${res.status})`);
        return res.json();
    }

    async DeleteDomain(domainKey: string): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/DeleteDomain?domainKey=${encodeURIComponent(domainKey)}`, {
            method: 'DELETE', headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`DeleteDomain failed (${res.status})`);
        return res.json();
    }

    // ── Tool Library — Libraries ──────────────────────────────────────────

    async GetAllLibraries(): Promise<OperationResult<AppAgentToolLibraryDto[]>> {
        const res = await fetch(`${BASE}/GetAllLibraries`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetAllLibraries failed (${res.status})`);
        return res.json();
    }

    async GetLibrariesByDomain(domainKey: string): Promise<OperationResult<AppAgentToolLibraryDto[]>> {
        const res = await fetch(`${BASE}/GetLibrariesByDomain?domainKey=${encodeURIComponent(domainKey)}`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetLibrariesByDomain failed (${res.status})`);
        return res.json();
    }

    async SearchLibraries(query: string): Promise<OperationResult<AppAgentToolLibraryDto[]>> {
        const res = await fetch(`${BASE}/SearchLibraries?query=${encodeURIComponent(query || '')}`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`SearchLibraries failed (${res.status})`);
        return res.json();
    }

    async GetLibraryToolPreview(libraryKey: string): Promise<OperationResult<LibraryToolPreviewDto[]>> {
        const res = await fetch(`${BASE}/GetLibraryToolPreview?libraryKey=${encodeURIComponent(libraryKey)}`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetLibraryToolPreview failed (${res.status})`);
        return res.json();
    }

    async UpsertLibrary(dto: AppAgentToolLibraryDto): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/UpsertLibrary`, {
            method: 'POST', headers: getHeaders(), body: JSON.stringify(dto),
        });
        if (!res.ok) throw new Error(`UpsertLibrary failed (${res.status})`);
        return res.json();
    }

    async DeleteLibrary(libraryKey: string): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/DeleteLibrary?libraryKey=${encodeURIComponent(libraryKey)}`, {
            method: 'DELETE', headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`DeleteLibrary failed (${res.status})`);
        return res.json();
    }

    // ── Tool Library — Subscriptions ──────────────────────────────────────

    async GetSubscriptions(skillKey: string): Promise<OperationResult<AppAgentLibrarySubscriptionDto[]>> {
        const res = await fetch(`${BASE}/GetSubscriptions?skillKey=${encodeURIComponent(skillKey)}`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetSubscriptions failed (${res.status})`);
        return res.json();
    }

    async SetSubscriptions(skillKey: string, libraryKeys: string[]): Promise<OperationResult<boolean>> {
        const res = await fetch(`${BASE}/SetSubscriptions`, {
            method: 'POST', headers: getHeaders(), body: JSON.stringify({ SkillKey: skillKey, LibraryKeys: libraryKeys }),
        });
        if (!res.ok) throw new Error(`SetSubscriptions failed (${res.status})`);
        return res.json();
    }

    // ── BuiltIn tool browser ──────────────────────────────────────────────

    async GetAvailableBuiltInTools(): Promise<OperationResult<LibraryToolPreviewDto[]>> {
        const res = await fetch(`${BASE}/GetAvailableBuiltInTools`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetAvailableBuiltInTools failed (${res.status})`);
        return res.json();
    }

    // ── Agent Templates ───────────────────────────────────────────────────

    async GetTemplates(): Promise<OperationResult<AppAgentSkillSetDto[]>> {
        const res = await fetch(`${BASE}/GetTemplates`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetTemplates failed (${res.status})`);
        return res.json();
    }

    // ── Prompt Version History ────────────────────────────────────────────

    async GetPromptHistory(skillKey: string): Promise<OperationResult<AppAgentPromptHistoryDto[]>> {
        const res = await fetch(`${BASE}/GetPromptHistory?skillKey=${encodeURIComponent(skillKey)}`, { headers: getHeaders() });
        if (!res.ok) throw new Error(`GetPromptHistory failed (${res.status})`);
        return res.json();
    }

    // ── AI-Assisted Agent Design Generation ──────────────────────────────

    async GenerateAgentDesign(description: string): Promise<OperationResult<GenerateAgentResult>> {
        const res = await fetch(`${BASE}/GenerateAgentDesign`, {
            method: 'POST', headers: getHeaders(), body: JSON.stringify({ Description: description }),
        });
        if (!res.ok) throw new Error(`GenerateAgentDesign failed (${res.status})`);
        return res.json();
    }
}

export const agentSkillSetSvc = new AgentSkillSetService();
