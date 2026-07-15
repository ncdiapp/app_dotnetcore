import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useVoiceInput } from '../../hooks/useVoiceInput';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useAlert } from '../../hooks/useAlert';
import Confirm from '../common/Confirm';
import {
    dbGenieService,
    DbGenieChatMessageDto,
    ExecuteSQLResultDto,
    DataSourceOptionDto,
} from '../../webapi/dbgeniesvc';
import appHelper from '../../helper/appHelper';
import { DbaGenieSessionState } from './DbaGenie';

// DataSourceType: 1=SQL Server, 2=Oracle, 3=MySQL
const DIALECT_MAP: Record<number, string> = { 1: 'SQL Server', 2: 'Oracle', 3: 'MySQL' };
const DIALECT_ICON: Record<number, string> = { 1: 'text-blue-500', 2: 'text-red-500', 3: 'text-orange-500' };

function getDialectName(type?: number): string {
    return type ? (DIALECT_MAP[type] ?? 'SQL Server') : 'SQL Server';
}

interface DbaGenieChatProps {
    sessionState: DbaGenieSessionState;
    onSessionStateChange: (updates: Partial<DbaGenieSessionState>) => void;
    onNavigateToSchemaView: () => void;
    onNewChat?: () => void;
}

const DbaGenieChat: React.FC<DbaGenieChatProps> = ({
    sessionState,
    onSessionStateChange,
    onNavigateToSchemaView,
    onNewChat,
}) => {
    const dispatch = useDispatch();
    const { theme } = useTheme();
    const errorMessage = useErrorMessage();
    const { confirmState, showConfirm, closeConfirm } = useAlert();

    const [inputMessage, setInputMessage] = useState('');
    const [isThinking, setIsThinking] = useState(false);
    const [executionResult, setExecutionResult] = useState<ExecuteSQLResultDto | null>(null);
    const [editingMessageId, setEditingMessageId] = useState<string | null>(null);
    const [editingContent, setEditingContent] = useState('');
    const [dataSources, setDataSources] = useState<DataSourceOptionDto[]>([]);
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const inputRef       = useRef<HTMLTextAreaElement>(null);

    const voice = useVoiceInput((transcript) => {
        setInputMessage(prev => prev ? `${prev} ${transcript}` : transcript);
        inputRef.current?.focus();
    });

    // Load data sources on mount
    useEffect(() => {
        dbGenieService.getDataSourceList().then(list => setDataSources(list)).catch(() => {});
    }, []);

    // Scroll to bottom when messages change
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [sessionState.conversationHistory]);

    const handleDataSourceChange = useCallback((e: React.ChangeEvent<HTMLSelectElement>) => {
        const id = e.target.value ? Number(e.target.value) : undefined;
        const ds = dataSources.find(d => (d.DataSourceRegisterId ?? d.Id) === id);
        onSessionStateChange({
            dataSourceRegisterId: id,
            dataSourceType: ds?.DataSourceType ?? undefined,
            dataSourceName: ds?.DataSourceName,
        });
    }, [dataSources, onSessionStateChange]);

    // Core LLM call — reused by both handleSendMessage and auto-continue after SQL execution
    const callLLMAsync = useCallback(async (
        message: string,
        historyIncludingMessage: DbGenieChatMessageDto[]
    ) => {
        setIsThinking(true);
        try {
            const result = await dbGenieService.sendMessage(
                message,
                sessionState.sessionId,
                sessionState.dataSourceRegisterId,
                historyIncludingMessage,
                getDialectName(sessionState.dataSourceType)
            );

            if (result.IsSuccessful && result.Object?.IsSuccess) {
                const assistantMessage = result.Object.Message;
                onSessionStateChange({
                    conversationHistory: [...historyIncludingMessage, assistantMessage],
                    sessionId: result.Object.SessionId,
                });
            } else {
                const errMsg = result.Object?.Error || result.ValidationResult?.Items?.[0]?.Message || 'Failed to get response';
                errorMessage.showError(errMsg);
            }
        } catch (err) {
            errorMessage.showError('Error: ' + (err as Error).message);
        } finally {
            setIsThinking(false);
        }
    }, [sessionState, onSessionStateChange, errorMessage]);

    // Send message
    const handleSendMessage = useCallback(async () => {
        if (!inputMessage.trim()) return;

        const userMessage: DbGenieChatMessageDto = {
            Role: 'user',
            Content: inputMessage,
            Timestamp: new Date().toISOString(),
            MessageId: appHelper.guid(),
            HasSQL: false,
        };

        const newHistory = [...sessionState.conversationHistory, userMessage];
        onSessionStateChange({ conversationHistory: newHistory });
        setInputMessage('');

        await callLLMAsync(inputMessage, newHistory);
    }, [inputMessage, sessionState, onSessionStateChange, callLLMAsync]);

    // Handle key press
    const handleKeyPress = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSendMessage();
        }
    };

    // Build a user message summarising SQL execution result (fed back to LLM)
    const buildExecutionFeedbackMessage = useCallback((
        sql: string,
        execResult: ExecuteSQLResultDto | null,
        errorText?: string
    ): DbGenieChatMessageDto => {
        let content: string;
        if (errorText) {
            content = `SQL execution error: ${errorText}`;
        } else if (execResult?.IsSuccess) {
            const rows = execResult.Results ?? [];
            const rowCount = execResult.RowsAffected ?? rows.length;
            if (rows.length > 0 && execResult.ColumnNames?.length) {
                // Serialize up to 20 rows as a compact text table for the LLM
                const cols = execResult.ColumnNames;
                const header = cols.join(' | ');
                const divider = cols.map(() => '---').join(' | ');
                const dataRows = rows.slice(0, 20).map(r =>
                    cols.map(c => String(r[c] ?? '')).join(' | ')
                );
                content = `SQL executed successfully. ${rowCount} row(s) returned.\n\n${header}\n${divider}\n${dataRows.join('\n')}`;
                if (rows.length > 20) content += `\n... (${rows.length - 20} more rows not shown)`;
            } else {
                content = `SQL executed successfully. ${rowCount} row(s) affected.`;
            }
        } else {
            content = `SQL execution failed: ${execResult?.Error ?? 'Unknown error'}`;
        }
        return {
            Role: 'user',
            Content: content,
            Timestamp: new Date().toISOString(),
            MessageId: appHelper.guid(),
            HasSQL: false,
        };
    }, []);

    // Execute SQL
    const handleExecuteSQL = useCallback(async (sql: string, isConfirmed: boolean = false) => {
        dispatch(setIsBusy());
        try {
            const result = await dbGenieService.executeSQL(
                sql,
                sessionState.dataSourceRegisterId,
                true,
                isConfirmed
            );

            if (result.IsSuccessful && result.Object) {
                if (result.Object.RequiresConfirmation) {
                    const confirmed = await showConfirm(
                        result.Object.ConfirmationMessage || 'This operation may modify data. Are you sure you want to proceed?',
                        {
                            title: 'Confirm Destructive Operation',
                            confirmLabel: 'Execute',
                            cancelLabel: 'Cancel',
                            confirmButtonStyle: 'danger',
                        }
                    );
                    if (confirmed) {
                        await handleExecuteSQL(sql, true);
                    }
                    return;
                }

                if (result.Object.IsSuccess) {
                    setExecutionResult(result.Object);
                } else {
                    errorMessage.showError(result.Object.Error || 'Execution failed');
                }

                // Feed result to LLM — auto-continue so LLM processes result without user re-typing
                const feedback = buildExecutionFeedbackMessage(sql, result.Object);
                const historyWithFeedback = [...sessionState.conversationHistory, feedback];
                onSessionStateChange({ conversationHistory: historyWithFeedback });
                await callLLMAsync(feedback.Content, historyWithFeedback);
            } else {
                const errMsg = result.ValidationResult?.Items?.[0]?.Message || 'Failed to execute SQL';
                errorMessage.showError(errMsg);
                // Feed error to LLM so it can suggest a fix
                const feedback = buildExecutionFeedbackMessage(sql, null, errMsg);
                const historyWithFeedback = [...sessionState.conversationHistory, feedback];
                onSessionStateChange({ conversationHistory: historyWithFeedback });
                await callLLMAsync(feedback.Content, historyWithFeedback);
            }
        } catch (err) {
            const errMsg = 'Error executing SQL: ' + (err as Error).message;
            errorMessage.showError(errMsg);
            const feedback = buildExecutionFeedbackMessage(sql, null, errMsg);
            const historyWithFeedback = [...sessionState.conversationHistory, feedback];
            onSessionStateChange({ conversationHistory: historyWithFeedback });
            await callLLMAsync(feedback.Content, historyWithFeedback);
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [sessionState, onSessionStateChange, dispatch, errorMessage, showConfirm, buildExecutionFeedbackMessage, callLLMAsync]);

    // Copy SQL to clipboard
    const handleCopySQL = useCallback((sql: string) => {
        navigator.clipboard.writeText(sql);
        errorMessage.showInfo('SQL copied to clipboard');
    }, [errorMessage]);

    // Clear conversation
    const handleClearConversation = useCallback(() => {
        onSessionStateChange({
            conversationHistory: [],
            sessionId: appHelper.guid(),
        });
        setExecutionResult(null);
        setEditingMessageId(null);
    }, [onSessionStateChange]);

    // Copy message text to clipboard (no popup — silent copy)
    const [copiedMessageId, setCopiedMessageId] = useState<string | null>(null);
    const handleCopyMessage = useCallback((content: string, messageId?: string) => {
        navigator.clipboard.writeText(content);
        if (messageId) {
            setCopiedMessageId(messageId);
            setTimeout(() => setCopiedMessageId(null), 1500);
        }
    }, []);

    // Retry: truncate history back to (not including) this message, re-send it
    const handleRetryMessage = useCallback(async (message: DbGenieChatMessageDto, index: number) => {
        if (isThinking) return;
        const historyBefore = sessionState.conversationHistory.slice(0, index);
        const newHistory = [...historyBefore, message];
        onSessionStateChange({ conversationHistory: newHistory });
        await callLLMAsync(message.Content, newHistory);
    }, [isThinking, sessionState.conversationHistory, onSessionStateChange, callLLMAsync]);

    // Start editing a message
    const handleStartEdit = useCallback((message: DbGenieChatMessageDto) => {
        setEditingMessageId(message.MessageId ?? null);
        setEditingContent(message.Content);
    }, []);

    // Submit edited message: truncate to before this message, send the new content
    const handleSubmitEdit = useCallback(async (message: DbGenieChatMessageDto, index: number) => {
        const trimmed = editingContent.trim();
        if (!trimmed) return;
        setEditingMessageId(null);
        const updatedMessage: DbGenieChatMessageDto = {
            ...message,
            Content: trimmed,
            Timestamp: new Date().toISOString(),
        };
        const historyBefore = sessionState.conversationHistory.slice(0, index);
        const newHistory = [...historyBefore, updatedMessage];
        onSessionStateChange({ conversationHistory: newHistory });
        await callLLMAsync(trimmed, newHistory);
    }, [editingContent, sessionState.conversationHistory, onSessionStateChange, callLLMAsync]);

    // Render a message
    const renderMessage = (message: DbGenieChatMessageDto, index: number) => {
        const isUser = message.Role === 'user';
        const isExecResult = isUser && (
            message.Content.startsWith('SQL execution error:') ||
            message.Content.startsWith('SQL executed successfully') ||
            message.Content.startsWith('SQL execution failed:')
        );
        const isError = isExecResult && !message.Content.startsWith('SQL executed successfully');

        // Execution result — compact system-style bubble
        if (isExecResult) {
            return (
                <div key={message.MessageId || index} className="flex justify-center mb-3">
                    <div className={`max-w-[90%] w-full rounded-md px-3 py-2 text-xs border ${
                        isError
                            ? 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800 text-red-700 dark:text-red-300'
                            : 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800 text-green-700 dark:text-green-300'
                    }`}>
                        <div className="flex items-center gap-1.5 mb-1 font-medium">
                            <i className={`fa-solid ${isError ? 'fa-circle-xmark' : 'fa-circle-check'} text-[11px]`}></i>
                            <span>SQL Execution Result</span>
                            <span className="opacity-50 ml-auto font-normal">
                                {new Date(message.Timestamp).toLocaleTimeString()}
                            </span>
                        </div>
                        <pre className="whitespace-pre-wrap font-mono text-[11px] overflow-x-auto">{message.Content}</pre>
                    </div>
                </div>
            );
        }

        const isEditing = isUser && editingMessageId === message.MessageId;

        return (
            <div
                key={message.MessageId || index}
                className={`group flex flex-col ${isUser ? 'items-end' : 'items-start'} mb-4`}
            >
                <div
                    className={`max-w-[80%] rounded-lg p-3 ${
                        isUser
                            ? 'bg-blue-500 text-white'
                            : `bg-white dark:bg-gray-900/50 border-gray-200 dark:border-gray-700 border`
                    }`}
                >
                    {/* Message header */}
                    <div className={`flex items-center gap-2 mb-2 text-xs ${isUser ? 'text-blue-100' : theme.label}`}>
                        <i className={`fa-solid ${isUser ? 'fa-user' : 'fa-robot'}`}></i>
                        <span>{isUser ? 'You' : 'DBA-Genie'}</span>
                        <span className="opacity-70">
                            {new Date(message.Timestamp).toLocaleTimeString()}
                        </span>
                    </div>

                    {/* Message content — or edit textarea */}
                    {isEditing ? (
                        <div className="flex flex-col gap-2">
                            <textarea
                                value={editingContent}
                                onChange={e => setEditingContent(e.target.value)}
                                onKeyDown={e => {
                                    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSubmitEdit(message, index); }
                                    if (e.key === 'Escape') setEditingMessageId(null);
                                }}
                                rows={3}
                                autoFocus
                                className="w-full p-1.5 text-sm rounded border border-blue-300 text-gray-900 dark:text-gray-100 bg-white dark:bg-gray-800 resize-none focus:outline-none"
                            />
                            <div className="flex gap-1.5 justify-end">
                                <button
                                    onClick={() => setEditingMessageId(null)}
                                    className="px-2 py-0.5 text-xs rounded bg-blue-400 hover:bg-blue-300 text-white"
                                >Cancel</button>
                                <button
                                    onClick={() => handleSubmitEdit(message, index)}
                                    className="px-2 py-0.5 text-xs rounded bg-white hover:bg-blue-50 text-blue-600 font-medium"
                                >Send</button>
                            </div>
                        </div>
                    ) : (
                        <div className={`text-sm whitespace-pre-wrap ${isUser ? '' : 'text-gray-900 dark:text-gray-100'}`}>
                            {renderMessageContent(message.Content)}
                        </div>
                    )}

                    {/* SQL actions (assistant only) */}
                    {message.HasSQL && message.GeneratedSQL && (
                        <div className={`mt-3 pt-3 border-t ${isUser ? 'border-blue-400' : 'border-gray-200 dark:border-gray-700'}`}>
                            <div className="flex items-center gap-2 mb-2">
                                <span className={`text-xs font-medium ${isUser ? 'text-blue-100' : theme.label}`}>
                                    Generated SQL:
                                </span>
                            </div>
                            <pre className={`text-xs p-2 rounded overflow-x-auto ${isUser ? 'bg-blue-600' : 'bg-gray-100 dark:bg-gray-800'}`}>
                                {message.GeneratedSQL}
                            </pre>
                            <div className="flex gap-2 mt-2">
                                <button
                                    onClick={() => handleExecuteSQL(message.GeneratedSQL!)}
                                    className={`px-2 py-1 text-xs rounded bg-blue-500 hover:bg-blue-600 text-white`}
                                >
                                    <i className="fa-solid fa-play mr-1"></i>
                                    Execute
                                </button>
                                <button
                                    onClick={() => handleCopySQL(message.GeneratedSQL!)}
                                    className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                                >
                                    <i className="fa-solid fa-copy mr-1"></i>
                                    Copy
                                </button>
                            </div>
                        </div>
                    )}
                </div>

                {/* User message action buttons — shown on hover, below the bubble */}
                {isUser && !isEditing && (
                    <div className="flex items-center gap-0.5 mt-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <button
                            onClick={() => handleRetryMessage(message, index)}
                            disabled={isThinking}
                            className={`w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 ${theme.label}`}
                            title="Retry"
                        >
                            <i className="fa-solid fa-rotate-right text-[11px]"></i>
                        </button>
                        <button
                            onClick={() => handleStartEdit(message)}
                            disabled={isThinking}
                            className={`w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 ${theme.label}`}
                            title="Edit"
                        >
                            <i className="fa-solid fa-pencil text-[11px]"></i>
                        </button>
                        <button
                            onClick={() => handleCopyMessage(message.Content, message.MessageId)}
                            className={`w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${theme.label}`}
                            title="Copy"
                        >
                            <i className={`fa-solid text-[11px] ${copiedMessageId === message.MessageId ? 'fa-check text-green-500' : 'fa-copy'}`}></i>
                        </button>
                    </div>
                )}
            </div>
        );
    };

    // Render message content with code block handling
    const renderMessageContent = (content: string) => {
        const parts = content.split(/(```[\s\S]*?```)/g);
        return parts.map((part, index) => {
            if (part.startsWith('```') && part.endsWith('```')) {
                const codeContent = part.slice(3, -3);
                const lines = codeContent.split('\n');
                const language = lines[0]?.trim() || '';
                const code = lines.slice(1).join('\n') || codeContent;

                return (
                    <pre key={index} className={`my-2 p-2 rounded text-xs overflow-x-auto bg-gray-100 dark:bg-gray-800`}>
                        <code>{code}</code>
                    </pre>
                );
            }
            return <span key={index}>{part}</span>;
        });
    };

    return (
        <div className="w-full h-full flex flex-col">
            {/* Header */}
            <div className={`flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700`}>
                <div className={`text-md font-semibold ${theme.title}`}>
                    <i className="fa-solid fa-comments mr-2 text-blue-500"></i>
                    Chat with DBA-Genie
                </div>
                <div className="flex items-center gap-2">
                    {/* Data source selector */}
                    <div className="flex items-center gap-1.5">
                        <i className={`fa-solid fa-database text-xs ${sessionState.dataSourceRegisterId ? (DIALECT_ICON[sessionState.dataSourceType ?? 0] ?? 'text-gray-400') : 'text-gray-400'}`}></i>
                        <select
                            value={sessionState.dataSourceRegisterId ?? ''}
                            onChange={handleDataSourceChange}
                            className={`h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            style={{ maxWidth: 160 }}
                        >
                            <option value="">— Data Source —</option>
                            {dataSources.map(ds => {
                                const id = ds.DataSourceRegisterId ?? ds.Id;
                                return (
                                    <option key={id} value={id ?? ''}>
                                        {ds.DataSourceName}
                                    </option>
                                );
                            })}
                        </select>
                        {sessionState.dataSourceRegisterId && (
                            <span className={`text-xs px-1.5 py-0.5 rounded font-medium bg-gray-100 dark:bg-gray-800 ${DIALECT_ICON[sessionState.dataSourceType ?? 0] ?? 'text-gray-500'}`}>
                                {getDialectName(sessionState.dataSourceType)}
                            </span>
                        )}
                    </div>
                    {onNewChat && (
                        <button
                            onClick={onNewChat}
                            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                            title="New chat"
                        >
                            <i className="fa-solid fa-plus mr-1"></i>
                            New Chat
                        </button>
                    )}
                    <button
                        onClick={handleClearConversation}
                        className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                        title="Clear conversation"
                    >
                        <i className="fa-solid fa-trash mr-1"></i>
                        Clear
                    </button>
                </div>
            </div>

            {/* Messages area */}
            <div className="flex-auto overflow-y-auto p-4">
                {sessionState.conversationHistory.length === 0 ? (
                    <div className="flex flex-col items-center justify-center h-full text-center">
                        <i className="fa-solid fa-robot text-6xl text-blue-500 mb-4"></i>
                        <h3 className={`text-lg font-medium mb-2 ${theme.title}`}>Welcome to DBA-Genie</h3>
                        <p className={`text-sm max-w-md ${theme.label}`}>
                            Ask me anything about databases! I can help you:
                        </p>
                        <ul className={`text-sm mt-2 ${theme.label} text-left`}>
                            <li><i className="fa-solid fa-check text-green-500 mr-2"></i>Generate SQL queries from natural language</li>
                            <li><i className="fa-solid fa-check text-green-500 mr-2"></i>Design database schemas</li>
                            <li><i className="fa-solid fa-check text-green-500 mr-2"></i>Create DDL scripts (CREATE TABLE, etc.)</li>
                            <li><i className="fa-solid fa-check text-green-500 mr-2"></i>Answer database-related questions</li>
                        </ul>
                    </div>
                ) : (
                    <>
                        {sessionState.conversationHistory.map((msg, idx) => renderMessage(msg, idx))}
                        {isThinking && (
                            <div className="flex justify-start mb-4">
                                <div className={`rounded-lg p-3 bg-white dark:bg-gray-900/50 border-gray-200 dark:border-gray-700 border`}>
                                    <div className="flex items-center gap-2">
                                        <i className="fa-solid fa-robot"></i>
                                        <span className={`text-sm ${theme.label}`}>Thinking</span>
                                        <span className="flex gap-1">
                                            <span className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-bounce" style={{ animationDelay: '0ms' }}></span>
                                            <span className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-bounce" style={{ animationDelay: '150ms' }}></span>
                                            <span className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-bounce" style={{ animationDelay: '300ms' }}></span>
                                        </span>
                                    </div>
                                </div>
                            </div>
                        )}
                        <div ref={messagesEndRef} />
                    </>
                )}
            </div>

            {/* Execution Results */}
            {executionResult && executionResult.Results && executionResult.Results.length > 0 && (
                <div className={`border-t px-4 py-3 border-gray-200 dark:border-gray-700`}>
                    <div className="flex items-center justify-between mb-2">
                        <span className={`text-sm font-medium ${theme.title}`}>
                            <i className="fa-solid fa-table mr-2"></i>
                            Query Results ({executionResult.RowsAffected} rows)
                        </span>
                        <button
                            onClick={() => setExecutionResult(null)}
                            className={`text-xs ${theme.label} hover:text-red-500`}
                        >
                            <i className="fa-solid fa-times"></i>
                        </button>
                    </div>
                    <div className="max-h-40 overflow-auto">
                        <table className="w-full text-xs">
                            <thead className="bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300">
                                <tr>
                                    {executionResult.ColumnNames.map((col, idx) => (
                                        <th key={idx} className="px-2 py-1 text-left border">{col}</th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {executionResult.Results.slice(0, 10).map((row, rowIdx) => (
                                    <tr key={rowIdx} className="hover:bg-gray-50 dark:hover:bg-gray-800">
                                        {executionResult.ColumnNames.map((col, colIdx) => (
                                            <td key={colIdx} className="px-2 py-1 border truncate max-w-[200px]">
                                                {String(row[col] ?? '')}
                                            </td>
                                        ))}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                        {executionResult.Results.length > 10 && (
                            <div className={`text-xs text-center py-1 ${theme.label}`}>
                                Showing 10 of {executionResult.Results.length} rows
                            </div>
                        )}
                    </div>
                </div>
            )}

            {/* Input area */}
            <div className={`border-t px-4 py-3 border-gray-200 dark:border-gray-700`}>
                <div className="flex gap-2">
                    <textarea
                        ref={inputRef}
                        value={inputMessage}
                        onChange={(e) => setInputMessage(e.target.value)}
                        onKeyPress={handleKeyPress}
                        placeholder="Ask me anything about databases... (Press Enter to send)"
                        rows={2}
                        className={`flex-auto p-2 text-sm border rounded resize-none ${theme.inputBox}`}
                        disabled={isThinking}
                    />
                    {voice.supported && (
                        <button
                            onClick={voice.toggle}
                            disabled={isThinking}
                            className={`w-9 h-9 flex items-center justify-center rounded-[4px] shrink-0 self-end transition-colors disabled:opacity-40 disabled:cursor-not-allowed ${
                                voice.isListening
                                    ? 'bg-red-500 hover:bg-red-600 text-white animate-pulse'
                                    : 'bg-gray-100 hover:bg-gray-200 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-600 dark:text-gray-300'
                            }`}
                            title={voice.isListening ? 'Stop listening' : 'Voice input'}
                        >
                            <i className={`fa-solid ${voice.isListening ? 'fa-stop' : 'fa-microphone'} text-sm`} />
                        </button>
                    )}
                    <button
                        onClick={handleSendMessage}
                        disabled={!inputMessage.trim() || isThinking}
                        className={`px-4 py-2 text-sm rounded-[4px] bg-blue-500 hover:bg-blue-600 text-white disabled:opacity-50`}
                    >
                        {isThinking ? (
                            <i className="fa-solid fa-spinner fa-spin"></i>
                        ) : (
                            <i className="fa-solid fa-paper-plane"></i>
                        )}
                    </button>
                </div>
            </div>

            {/* Confirm dialog */}
            <Confirm
                isOpen={confirmState.isOpen}
                title={confirmState.title || 'Confirm'}
                message={confirmState.message}
                confirmLabel={confirmState.confirmLabel}
                cancelLabel={confirmState.cancelLabel}
                onConfirm={() => confirmState.onConfirm?.()}
                onCancel={closeConfirm}
            />
        </div>
    );
};

export default DbaGenieChat;
