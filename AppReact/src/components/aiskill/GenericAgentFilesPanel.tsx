import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { genericAgentSvc, GenericAgentFile } from '../../webapi/genericAgentSvc';

interface Props {
    skillKey: string;
    /** AppGenericAgentSession.SessionKey (fixed SkillKey:UserId or GUID). */
    sessionKey: string | null;
}

const formatSize = (n: number) => {
    if (n < 1024) return `${n} B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
    return `${(n / (1024 * 1024)).toFixed(1)} MB`;
};

const basename = (p: string) => {
    const parts = (p || '').replace(/\\/g, '/').split('/').filter(Boolean);
    return parts[parts.length - 1] || p;
};

const parentPath = (p: string) => {
    const parts = (p || '').replace(/\\/g, '/').split('/').filter(Boolean);
    parts.pop();
    return parts.join('/');
};

const GenericAgentFilesPanel: React.FC<Props> = ({ skillKey, sessionKey }) => {
    const { theme } = useTheme();
    const [cwd, setCwd] = useState('');
    const [files, setFiles] = useState<GenericAgentFile[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [editingPath, setEditingPath] = useState<string | null>(null);
    const [editContent, setEditContent] = useState('');
    const [editSaving, setEditSaving] = useState(false);
    const fileInputRef = useRef<HTMLInputElement | null>(null);

    const btn = `px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`;
    const iconBtn = `w-7 h-6 ${theme.button_default} rounded-[4px] text-xs`;

    const refresh = useCallback(async (path = cwd) => {
        if (!skillKey || !sessionKey) {
            setFiles([]);
            return;
        }
        setLoading(true);
        setError(null);
        try {
            const list = await genericAgentSvc.ListAgentFiles(skillKey, sessionKey, path || '');
            setFiles(list);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
            setFiles([]);
        } finally {
            setLoading(false);
        }
    }, [skillKey, sessionKey, cwd]);

    useEffect(() => {
        setCwd('');
        setEditingPath(null);
    }, [skillKey, sessionKey]);

    useEffect(() => {
        void refresh(cwd);
    }, [refresh, cwd]);

    const openDir = (rel: string) => {
        setEditingPath(null);
        setCwd(rel.replace(/\\/g, '/'));
    };

    const goUp = () => {
        if (!cwd) return;
        setEditingPath(null);
        setCwd(parentPath(cwd));
    };

    const handleUpload = async (list: FileList | null) => {
        if (!list?.length || !sessionKey) return;
        setError(null);
        try {
            for (const file of Array.from(list)) {
                const dest = cwd ? `${cwd}/${file.name}` : file.name;
                await genericAgentSvc.UploadAgentFile(skillKey, sessionKey, dest, file);
            }
            await refresh(cwd);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        } finally {
            if (fileInputRef.current) fileInputRef.current.value = '';
        }
    };

    const handleMkdir = async () => {
        if (!sessionKey) return;
        const name = window.prompt('New folder name');
        if (!name?.trim()) return;
        const rel = cwd ? `${cwd}/${name.trim()}` : name.trim();
        try {
            await genericAgentSvc.MkdirAgentFile(skillKey, sessionKey, rel);
            await refresh(cwd);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        }
    };

    const handleDelete = async (f: GenericAgentFile) => {
        if (!sessionKey) return;
        const label = basename(f.RelativePath);
        if (!window.confirm(`Delete ${f.IsDirectory ? 'folder' : 'file'} "${label}"?`)) return;
        try {
            await genericAgentSvc.DeleteAgentFile(skillKey, sessionKey, f.RelativePath);
            if (editingPath === f.RelativePath) setEditingPath(null);
            await refresh(cwd);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        }
    };

    const handleRename = async (f: GenericAgentFile) => {
        if (!sessionKey) return;
        const current = basename(f.RelativePath);
        const next = window.prompt('Rename to', current);
        if (!next?.trim() || next.trim() === current) return;
        const newPath = cwd ? `${cwd}/${next.trim()}` : next.trim();
        try {
            await genericAgentSvc.RenameAgentFile(skillKey, sessionKey, f.RelativePath, newPath);
            if (editingPath === f.RelativePath) setEditingPath(null);
            await refresh(cwd);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        }
    };

    const openEdit = async (f: GenericAgentFile) => {
        if (!sessionKey || f.IsDirectory) return;
        setError(null);
        const content = await genericAgentSvc.ReadAgentFile(skillKey, sessionKey, f.RelativePath);
        if (!content) {
            setError('Could not read file.');
            return;
        }
        setEditingPath(f.RelativePath);
        setEditContent(content.Content ?? '');
        if (content.Truncated) setError('File truncated for display (large file). Saving will overwrite with visible content only.');
    };

    const saveEdit = async () => {
        if (!sessionKey || !editingPath) return;
        setEditSaving(true);
        setError(null);
        try {
            await genericAgentSvc.WriteAgentFile(skillKey, sessionKey, editingPath, editContent);
            setEditingPath(null);
            await refresh(cwd);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        } finally {
            setEditSaving(false);
        }
    };

    if (!sessionKey) {
        return (
            <div className={`p-3 text-xs ${theme.label} opacity-70`}>
                Resolving file session…
            </div>
        );
    }

    if (editingPath) {
        return (
            <div className="w-full h-full flex flex-col overflow-hidden min-h-0">
                <div className={`flex items-center gap-1 px-2 py-1.5 border-b border-gray-200 shrink-0`}>
                    <button type="button" className={btn} onClick={() => setEditingPath(null)} title="Back">
                        <i className="fa-solid fa-arrow-left" />
                    </button>
                    <span className={`text-xs truncate flex-auto w-1 ${theme.label}`} title={editingPath}>
                        {basename(editingPath)}
                    </span>
                    <button type="button" className={btn} onClick={saveEdit} disabled={editSaving} title="Save">
                        {editSaving ? <i className="fa-solid fa-spinner fa-spin" /> : <i className="fa-solid fa-floppy-disk" />}
                    </button>
                </div>
                {error && (
                    <div className={`px-2 py-1 text-xs ${theme.label} shrink-0`}>{error}</div>
                )}
                <textarea
                    className={`w-full h-1 flex-auto p-2 text-xs border-0 resize-none font-mono focus:outline-none ${theme.inputBox}`}
                    value={editContent}
                    onChange={e => setEditContent(e.target.value)}
                    spellCheck={false}
                />
            </div>
        );
    }

    return (
        <div className="w-full h-full flex flex-col overflow-hidden min-h-0">
            <div className={`flex items-center gap-1 px-2 py-1.5 border-b border-gray-200 shrink-0`}>
                <button type="button" className={iconBtn} onClick={goUp} disabled={!cwd} title="Up">
                    <i className="fa-solid fa-arrow-up" />
                </button>
                <button type="button" className={iconBtn} onClick={() => void refresh(cwd)} title="Refresh">
                    <i className={`fa-solid fa-rotate ${loading ? 'fa-spin' : ''}`} />
                </button>
                <button type="button" className={iconBtn} onClick={handleMkdir} title="New folder">
                    <i className="fa-solid fa-folder-plus" />
                </button>
                <button type="button" className={iconBtn} onClick={() => fileInputRef.current?.click()} title="Upload">
                    <i className="fa-solid fa-upload" />
                </button>
                <input
                    ref={fileInputRef}
                    type="file"
                    multiple
                    className="hidden"
                    onChange={e => void handleUpload(e.target.files)}
                />
            </div>

            <div className={`px-2 py-1 text-xs truncate shrink-0 ${theme.label}`} title={cwd || '/'}>
                /{cwd || ''}
            </div>

            {error && (
                <div className={`px-2 py-1 text-xs shrink-0 ${theme.label}`}>{error}</div>
            )}

            <div className="w-full h-1 flex-auto overflow-y-auto px-1 pb-2">
                {files.length === 0 && !loading && (
                    <div className={`p-3 text-xs opacity-60 ${theme.label}`}>
                        Empty folder. Upload official <span className="font-mono">source/</span> files here for Phase B.
                    </div>
                )}
                {files.map(f => (
                    <div
                        key={f.RelativePath}
                        className={`flex items-center gap-1 px-1.5 py-1 rounded text-xs group ${theme.label}`}
                    >
                        <button
                            type="button"
                            className="flex items-center gap-1.5 min-w-0 w-1 flex-auto text-left hover:underline"
                            onClick={() => (f.IsDirectory ? openDir(f.RelativePath) : void openEdit(f))}
                            title={f.RelativePath}
                        >
                            <i className={`fa-solid ${f.IsDirectory ? 'fa-folder' : 'fa-file'} shrink-0 opacity-70`} />
                            <span className="truncate">{basename(f.RelativePath)}</span>
                        </button>
                        {!f.IsDirectory && (
                            <span className="opacity-40 shrink-0">{formatSize(f.SizeBytes)}</span>
                        )}
                        <button type="button" className={`${iconBtn} opacity-0 group-hover:opacity-100`} title="Rename"
                            onClick={() => void handleRename(f)}>
                            <i className="fa-solid fa-pencil" />
                        </button>
                        {!f.IsDirectory && (
                            <button type="button" className={`${iconBtn} opacity-0 group-hover:opacity-100`} title="Download"
                                onClick={() => void genericAgentSvc.DownloadAgentFile(skillKey, sessionKey, f.RelativePath)}>
                                <i className="fa-solid fa-download" />
                            </button>
                        )}
                        <button type="button" className={`${iconBtn} opacity-0 group-hover:opacity-100`} title="Delete"
                            onClick={() => void handleDelete(f)}>
                            <i className="fa-solid fa-trash" />
                        </button>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default GenericAgentFilesPanel;
