import React, { useState, useCallback, useRef } from 'react';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import {
    dbGenieService,
    SchemaExtractionResultDto,
    DbGenieCreateTransactionResultDto,
} from '../../webapi/dbgeniesvc';
import { DbaGenieSessionState } from './DbaGenie';

interface DbaGenieDashboardProps {
    sessionState: DbaGenieSessionState;
    onSessionStateChange: (updates: Partial<DbaGenieSessionState>) => void;
    onSchemaExtracted: (result: SchemaExtractionResultDto) => void;
    onNavigateToChat: () => void;
}

const DbaGenieDashboard: React.FC<DbaGenieDashboardProps> = ({
    sessionState,
    onSessionStateChange,
    onSchemaExtracted,
    onNavigateToChat,
}) => {
    const dispatch = useDispatch();
    const { theme } = useTheme();
    const errorMessage = useErrorMessage();

    const [requirementText, setRequirementText] = useState('');
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [transactionName, setTransactionName] = useState('');
    const [createTransactionResult, setCreateTransactionResult] = useState<DbGenieCreateTransactionResultDto | null>(null);

    // Handle file selection
    const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
        const files = event.target.files;
        if (files && files.length > 0) {
            const file = files[0];
            const ext = file.name.toLowerCase().split('.').pop();
            if (!['pdf', 'docx', 'doc', 'txt'].includes(ext || '')) {
                errorMessage.showError('Unsupported file type. Please use PDF, DOCX, DOC, or TXT files.');
                return;
            }
            setSelectedFile(file);
        }
    };

    // Handle schema extraction
    const handleExtractSchema = useCallback(async () => {
        if (!requirementText.trim() && !selectedFile) {
            errorMessage.showError('Please enter requirements text or upload a document');
            return;
        }

        dispatch(setIsBusy());
        try {
            let result;
            if (selectedFile) {
                result = await dbGenieService.extractSchemaFromFile(selectedFile);
            } else {
                result = await dbGenieService.extractSchemaFromText(requirementText);
            }

            if (result.IsSuccessful && result.Object) {
                if (result.Object.IsSuccess) {
                    onSchemaExtracted(result.Object);
                    errorMessage.showInfo(`Extracted ${result.Object.Tables?.length || 0} tables from requirements`);
                } else {
                    errorMessage.showError(result.Object.Error || 'Failed to extract schema');
                }
            } else {
                const errMsg = result.ValidationResult?.Items?.[0]?.Message || 'Failed to extract schema';
                errorMessage.showError(errMsg);
            }
        } catch (err) {
            errorMessage.showError('Error extracting schema: ' + (err as Error).message);
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [requirementText, selectedFile, dispatch, errorMessage, onSchemaExtracted]);

    // Handle create hierarchy transaction
    const handleCreateTransaction = useCallback(async () => {
        if (!requirementText.trim()) {
            errorMessage.showError('Please enter requirements text');
            return;
        }

        setCreateTransactionResult(null);
        dispatch(setIsBusy());
        try {
            const result = await dbGenieService.createHierarchyTransactionFromRequirements({
                RequirementsText: requirementText,
                TransactionName: transactionName.trim() || undefined,
            });

            if (result.Object) {
                setCreateTransactionResult(result.Object);
                if (result.Object.IsSuccess) {
                    errorMessage.showInfo('App Transaction created successfully');
                } else {
                    errorMessage.showError(result.Object.Error || 'Failed to create transaction');
                }
            } else {
                errorMessage.showError(result.ValidationResult?.Items?.[0]?.Message || 'Failed to create transaction');
            }
        } catch (err) {
            errorMessage.showError('Error: ' + (err as Error).message);
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [requirementText, transactionName, dispatch, errorMessage]);

    // Clear file selection
    const handleClearFile = () => {
        setSelectedFile(null);
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    return (
        <div className="w-full h-full flex flex-col p-4 overflow-auto">
            {/* Header */}
            <div className="mb-6">
                <h1 className={`text-xl font-semibold ${theme.title}`}>
                    <i className="fa-solid fa-robot mr-2 text-blue-500"></i>
                    DBA-Genie AI Agent
                </h1>
                <p className={`text-sm mt-1 ${theme.label}`}>
                    Transform natural language requirements and documents into database schemas and SQL queries
                </p>
            </div>

            {/* Schema Extraction Section */}
            <div className={`rounded-md p-4 mb-4 bg-white dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700`}>
                <h2 className={`text-md font-medium mb-4 ${theme.title}`}>
                    <i className="fa-solid fa-wand-magic-sparkles mr-2"></i>
                    Extract Schema from Requirements
                </h2>

                {/* File Upload */}
                <div className="mb-4">
                    <label className={`block text-xs mb-1 ${theme.label}`}>Upload Document (PDF, DOCX, TXT)</label>
                    <div className="flex items-center gap-2">
                        <input
                            ref={fileInputRef}
                            type="file"
                            accept=".pdf,.docx,.doc,.txt"
                            onChange={handleFileSelect}
                            className="hidden"
                        />
                        <button
                            onClick={() => fileInputRef.current?.click()}
                            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                        >
                            <i className="fa-solid fa-upload mr-2"></i>
                            Choose File
                        </button>
                        {selectedFile && (
                            <div className="flex items-center gap-2">
                                <span className={`text-sm ${theme.label}`}>
                                    <i className="fa-solid fa-file mr-1"></i>
                                    {selectedFile.name}
                                </span>
                                <button
                                    onClick={handleClearFile}
                                    className="text-red-500 hover:text-red-700"
                                >
                                    <i className="fa-solid fa-times"></i>
                                </button>
                            </div>
                        )}
                    </div>
                </div>

                {/* Or divider */}
                <div className="flex items-center gap-4 mb-4">
                    <div className={`flex-auto h-px bg-gray-200 dark:bg-gray-700`}></div>
                    <span className={`text-xs ${theme.label}`}>OR</span>
                    <div className={`flex-auto h-px bg-gray-200 dark:bg-gray-700`}></div>
                </div>

                {/* Text Input */}
                <div className="mb-4">
                    <label className={`block text-xs mb-1 ${theme.label}`}>Enter Requirements Text</label>
                    <textarea
                        value={requirementText}
                        onChange={(e) => setRequirementText(e.target.value)}
                        placeholder="Describe your database requirements in natural language. For example:
- I need a customer management system with customers, orders, and products
- Each customer can have multiple orders
- Each order contains multiple products with quantities and prices"
                        rows={6}
                        className={`w-full p-2 text-sm border rounded resize-none ${theme.inputBox}`}
                    />
                </div>

                {/* Extract Button */}
                <div className="flex justify-end">
                    <button
                        onClick={handleExtractSchema}
                        disabled={!requirementText.trim() && !selectedFile}
                        className={`px-4 py-2 text-sm rounded-[4px] bg-blue-500 hover:bg-blue-600 text-white disabled:opacity-50`}
                    >
                        <i className="fa-solid fa-wand-magic-sparkles mr-2"></i>
                        Extract Schema
                    </button>
                </div>
            </div>

            {/* Create App Transaction Section */}
            <div className={`rounded-md p-4 mb-4 bg-white dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700`}>
                <h2 className={`text-md font-medium mb-4 ${theme.title}`}>
                    <i className="fa-solid fa-sitemap mr-2"></i>
                    Create App Transaction from Requirements
                </h2>
                <p className={`text-xs mb-4 ${theme.label}`}>
                    Uses the requirements text above — the LLM will design tables, create them in the selected data source, and build an AppTransaction hierarchy automatically.
                </p>

                <div className="mb-3">
                    <label className={`block text-xs mb-1 ${theme.label}`}>Transaction Name (Optional)</label>
                    <input
                        type="text"
                        value={transactionName}
                        onChange={(e) => setTransactionName(e.target.value)}
                        placeholder="e.g. Order Management"
                        className={`flex-auto w-full h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                    />
                </div>

                <div className="flex justify-end mb-4">
                    <button
                        onClick={handleCreateTransaction}
                        disabled={!requirementText.trim()}
                        className={`px-4 py-2 text-sm rounded-[4px] bg-green-600 hover:bg-green-700 text-white disabled:opacity-50`}
                    >
                        <i className="fa-solid fa-wand-sparkles mr-2"></i>
                        Create App Transaction
                    </button>
                </div>

                {/* Result */}
                {createTransactionResult && (
                    <div className={`rounded p-3 text-xs border ${createTransactionResult.IsSuccess
                        ? 'border-green-300 bg-green-50 dark:bg-green-900/20'
                        : 'border-red-300 bg-red-50 dark:bg-red-900/20'}`}>
                        {createTransactionResult.IsSuccess ? (
                            <div>
                                <div className="font-semibold text-green-700 dark:text-green-400 mb-2">
                                    <i className="fa-solid fa-circle-check mr-1"></i>
                                    Transaction created successfully
                                </div>
                                {createTransactionResult.SchemaExtraction?.Tables && (
                                    <div className={theme.label}>
                                        <span className="font-medium">Tables created: </span>
                                        {createTransactionResult.SchemaExtraction.Tables.map(t => t.Name).join(', ')}
                                    </div>
                                )}
                            </div>
                        ) : (
                            <div className="text-red-700 dark:text-red-400">
                                <i className="fa-solid fa-circle-xmark mr-1"></i>
                                {createTransactionResult.Error}
                            </div>
                        )}
                    </div>
                )}
            </div>

            {/* Quick Actions */}
            <div className={`rounded-md p-4 bg-white dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700`}>
                <h2 className={`text-md font-medium mb-4 ${theme.title}`}>
                    <i className="fa-solid fa-bolt mr-2"></i>
                    Quick Actions
                </h2>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <button
                        onClick={onNavigateToChat}
                        className={`p-4 rounded-md border text-left hover:shadow-md transition-shadow bg-white dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 border-gray-200 dark:border-gray-700`}
                    >
                        <div className="flex items-center gap-3">
                            <i className="fa-solid fa-comments text-2xl text-blue-500"></i>
                            <div>
                                <div className={`text-sm font-medium ${theme.title}`}>Chat with DBA-Genie</div>
                                <div className={`text-xs ${theme.label}`}>Ask questions in natural language</div>
                            </div>
                        </div>
                    </button>

                    <button
                        onClick={() => {
                            if (sessionState.extractedSchema) {
                                onSchemaExtracted(sessionState.extractedSchema);
                            }
                        }}
                        disabled={!sessionState.extractedSchema}
                        className={`p-4 rounded-md border text-left hover:shadow-md transition-shadow disabled:opacity-50 bg-white dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 border-gray-200 dark:border-gray-700`}
                    >
                        <div className="flex items-center gap-3">
                            <i className="fa-solid fa-diagram-project text-2xl text-green-500"></i>
                            <div>
                                <div className={`text-sm font-medium ${theme.title}`}>View Extracted Schema</div>
                                <div className={`text-xs ${theme.label}`}>
                                    {sessionState.extractedSchema
                                        ? `${sessionState.extractedSchema.Tables?.length || 0} tables extracted`
                                        : 'No schema extracted yet'}
                                </div>
                            </div>
                        </div>
                    </button>
                </div>
            </div>
        </div>
    );
};

export default DbaGenieDashboard;
