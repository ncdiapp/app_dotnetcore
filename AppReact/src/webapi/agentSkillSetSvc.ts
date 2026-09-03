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
}

export const agentSkillSetSvc = new AgentSkillSetService();
