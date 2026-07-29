import React, { useState, useCallback, useRef } from 'react';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { dbGenieService } from '../../webapi/dbgeniesvc';
import { DbaGenieSessionState } from './DbaGenie';
import DbaGenieSchemaView from './DbaGenieSchemaView';

interface DbaGenieSchemaProps {
    sessionState: DbaGenieSessionState;
    onSessionStateChange: (updates: Partial<DbaGenieSessionState>) => void;
}

const DbaGenieSchema: React.FC<DbaGenieSchemaProps> = ({
    sessionState,
    onSessionStateChange,
}) => {
    const dispatch = useDispatch();
    const { theme } = useTheme();
    const errorMessage = useErrorMessage();

    const [activeTab, setActiveTab] = useState<'extract' | 'view'>('extract');
    const [requirementText, setRequirementText] = useState('');
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);

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

    const handleClearFile = () => {
        setSelectedFile(null);
        if (fileInputRef.current) fileInputRef.current.value = '';
    };

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
                    onSessionStateChange({ extractedSchema: result.Object });
                    setActiveTab('view');
                    errorMessage.showInfo(`Extracted ${result.Object.Tables?.length || 0} tables from requirements`);
                } else {
                    errorMessage.showError(result.Object.Error || 'Failed to extract schema');
                }
            } else {
                errorMessage.showError(result.ValidationResult?.Items?.[0]?.Message || 'Failed to extract schema');
            }
        } catch (err) {
            errorMessage.showError('Error extracting schema: ' + (err as Error).message);
        } finally {
            dispatch(setIsNotBusy());
        }
    }, [requirementText, selectedFile, dispatch, errorMessage, onSessionStateChange]);

    const tableCount = sessionState.extractedSchema?.Tables?.length;

    return (
        <div className="w-full h-full flex flex-col overflow-hidden">
            {/* Tab row */}
            <div className="flex items-center gap-1 px-4 pt-3 pb-2 flex-none">
                <button
                    onClick={() => setActiveTab('extract')}
                    className={`px-3 py-1.5 text-sm rounded-[4px] ${activeTab === 'extract' ? theme.tab_active : theme.tab}`}
                >
                    <i className="fa-solid fa-wand-magic-sparkles mr-2"></i>
                    Extract Schema
                </button>
                <button
                    onClick={() => setActiveTab('view')}
                    className={`px-3 py-1.5 text-sm rounded-[4px] ${activeTab === 'view' ? theme.tab_active : theme.tab}`}
                >
                    <i className="fa-solid fa-diagram-project mr-2"></i>
                    View Schema
                    {tableCount ? (
                        <span className="ml-1.5 text-xs opacity-70">({tableCount})</span>
                    ) : null}
                </button>
            </div>

            {/* Tab content */}
            <div className="h-1 flex-auto overflow-hidden">
                {activeTab === 'extract' ? (
                    <div className="h-full overflow-auto p-4">
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
                                        <button onClick={handleClearFile} className="text-red-500 hover:text-red-700">
                                            <i className="fa-solid fa-times"></i>
                                        </button>
                                    </div>
                                )}
                            </div>
                        </div>

                        {/* OR divider */}
                        <div className="flex items-center gap-4 mb-4">
                            <div className={`w-1 flex-auto h-px bg-gray-200 dark:bg-gray-700`}></div>
                            <span className={`text-xs ${theme.label}`}>OR</span>
                            <div className={`w-1 flex-auto h-px bg-gray-200 dark:bg-gray-700`}></div>
                        </div>

                        {/* Requirements textarea */}
                        <div className="mb-4">
                            <label className={`block text-xs mb-1 ${theme.label}`}>Enter Requirements Text</label>
                            <textarea
                                value={requirementText}
                                onChange={(e) => setRequirementText(e.target.value)}
                                placeholder={`Describe your database requirements in natural language. For example:\n- I need a customer management system with customers, orders, and products\n- Each customer can have multiple orders\n- Each order contains multiple products with quantities and prices`}
                                rows={8}
                                className={`w-full p-2 text-sm border rounded resize-none ${theme.inputBox}`}
                            />
                        </div>

                        <div className="flex justify-end">
                            <button
                                onClick={handleExtractSchema}
                                disabled={!requirementText.trim() && !selectedFile}
                                className="px-4 py-2 text-sm rounded-[4px] bg-blue-500 hover:bg-blue-600 text-white disabled:opacity-50"
                            >
                                <i className="fa-solid fa-wand-magic-sparkles mr-2"></i>
                                Extract Schema
                            </button>
                        </div>
                    </div>
                ) : (
                    sessionState.extractedSchema ? (
                        <DbaGenieSchemaView
                            sessionState={sessionState}
                            onSessionStateChange={onSessionStateChange}
                        />
                    ) : (
                        <div className="flex items-center justify-center h-full">
                            <div className={`text-center ${theme.label}`}>
                                <i className="fa-solid fa-diagram-project text-4xl mb-3 opacity-30 block"></i>
                                <div className="text-sm">Extract a schema first to view it here</div>
                                <button
                                    onClick={() => setActiveTab('extract')}
                                    className={`mt-3 px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                >
                                    Go to Extract
                                </button>
                            </div>
                        </div>
                    )
                )}
            </div>
        </div>
    );
};

export default DbaGenieSchema;
