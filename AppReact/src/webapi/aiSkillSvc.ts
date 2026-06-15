import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

// --- Types ---

export interface AppAISkillDto {
    SkillId: number;
    Name: string;
    Description?: string;
    SkillContent?: string;
    IsActive: boolean;
    CreatedDate: string;
    UpdatedDate?: string;
    References: AppAISkillRefDto[];
}

export interface AppAISkillRefDto {
    RefId: number;
    SkillId: number;
    FileName: string;
    FileContent?: string;
    SortOrder: number;
    CreatedDate: string;
}

export interface OperationCallResult<T> {
    Object: T;
    ValidationResult: {
        Items: Array<{ Type: string; PropertyName: string; Message: string }>;
        IsValid: boolean;
    };
    IsSuccessful: boolean;
    HasResult: boolean;
}

// --- Service ---

class AISkillService {
    private baseUrl = `${endpoints.BASE_URL}/webapi/AISkill`;

    async GetDefaultDataSourceId(): Promise<OperationCallResult<number | null>> {
        const res = await fetch(`${this.baseUrl}/GetDefaultDataSourceId`, {
            headers: getHeaders()
        });
        if (!res.ok) throw new Error('Failed to get default data source');
        return res.json();
    }

    async GetAll(dataSourceId: number): Promise<OperationCallResult<AppAISkillDto[]>> {
        const res = await fetch(`${this.baseUrl}/GetAll?dataSourceId=${dataSourceId}`, {
            headers: getHeaders()
        });
        if (!res.ok) throw new Error('Failed to load skills');
        return res.json();
    }

    async GetById(dataSourceId: number, skillId: number): Promise<OperationCallResult<AppAISkillDto>> {
        const res = await fetch(`${this.baseUrl}/GetById?dataSourceId=${dataSourceId}&skillId=${skillId}`, {
            headers: getHeaders()
        });
        if (!res.ok) throw new Error('Failed to load skill');
        return res.json();
    }

    async Create(dataSourceId: number, dto: AppAISkillDto): Promise<OperationCallResult<number>> {
        const res = await fetch(`${this.baseUrl}/Create?dataSourceId=${dataSourceId}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(dto)
        });
        if (!res.ok) throw new Error('Failed to create skill');
        return res.json();
    }

    async Update(dataSourceId: number, dto: AppAISkillDto): Promise<OperationCallResult<boolean>> {
        const res = await fetch(`${this.baseUrl}/Update?dataSourceId=${dataSourceId}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(dto)
        });
        if (!res.ok) throw new Error('Failed to update skill');
        return res.json();
    }

    async Delete(dataSourceId: number, skillId: number): Promise<OperationCallResult<boolean>> {
        const res = await fetch(`${this.baseUrl}/Delete?dataSourceId=${dataSourceId}&skillId=${skillId}`, {
            method: 'POST',
            headers: getHeaders()
        });
        if (!res.ok) throw new Error('Failed to delete skill');
        return res.json();
    }

    async GetComposed(dataSourceId: number, skillId: number): Promise<OperationCallResult<string>> {
        const res = await fetch(`${this.baseUrl}/GetComposed?dataSourceId=${dataSourceId}&skillId=${skillId}`, {
            headers: getHeaders()
        });
        if (!res.ok) throw new Error('Failed to get composed prompt');
        return res.json();
    }

    async CreateRef(dataSourceId: number, dto: AppAISkillRefDto): Promise<OperationCallResult<number>> {
        const res = await fetch(`${this.baseUrl}/CreateRef?dataSourceId=${dataSourceId}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(dto)
        });
        if (!res.ok) throw new Error('Failed to create reference');
        return res.json();
    }

    async UpdateRef(dataSourceId: number, dto: AppAISkillRefDto): Promise<OperationCallResult<boolean>> {
        const res = await fetch(`${this.baseUrl}/UpdateRef?dataSourceId=${dataSourceId}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(dto)
        });
        if (!res.ok) throw new Error('Failed to update reference');
        return res.json();
    }

    async DeleteRef(dataSourceId: number, refId: number): Promise<OperationCallResult<boolean>> {
        const res = await fetch(`${this.baseUrl}/DeleteRef?dataSourceId=${dataSourceId}&refId=${refId}`, {
            method: 'POST',
            headers: getHeaders()
        });
        if (!res.ok) throw new Error('Failed to delete reference');
        return res.json();
    }
}

export const aiSkillSvc = new AISkillService();
