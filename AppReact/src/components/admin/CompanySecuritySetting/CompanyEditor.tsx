import React, { useCallback, useEffect, useRef, useState } from 'react';
import { adminSvc } from '../../../webapi/adminsvc';
import { toAbsoluteResourceUrl } from '../../../webapi/endpoints';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import FileUploader from '../../common/FileUploader';
import type { CompanyDto } from './types';

const extractErrorMessages = (validationResult: any): string[] => {
  if (!validationResult) return [];
  if (Array.isArray(validationResult)) {
    return validationResult.map((item: any) => item?.ErrorMessage || item?.Message || '').filter(Boolean);
  }
  if (Array.isArray(validationResult?.Errors)) {
    return validationResult.Errors.map((err: any) => err?.ErrorMessage || err?.Message || '').filter(Boolean);
  }
  if (typeof validationResult === 'string') return [validationResult];
  return [];
};

type Props = {
  companyId: string | number | null;
  isEmbedded?: boolean;
  onSynchronize?: (company: CompanyDto) => void;
  onMarkModified?: (modified: boolean) => void;
};

const CompanyEditor: React.FC<Props> = ({ companyId, isEmbedded, onSynchronize, onMarkModified }) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const [currentCompany, setCurrentCompany] = useState<CompanyDto | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [saveSuccess, setSaveSuccess] = useState<string | null>(null);
  const [uploadPopup, setUploadPopup] = useState<'logo' | 'background' | null>(null);
  const onSynchronizeRef = useRef(onSynchronize);
  onSynchronizeRef.current = onSynchronize;

  const loadData = useCallback(async () => {
    if (!companyId) {
      setCurrentCompany(null);
      return;
    }
    setCurrentCompany(null);
    dispatch(setIsBusy());
    const timeoutMs = 20000;
    const timeoutPromise = new Promise<never>((_, reject) =>
      setTimeout(() => reject(new Error('Request timeout')), timeoutMs)
    );
    try {
      const data = await Promise.race([
        adminSvc.retrieveOneAppCompanyExDto(String(companyId)),
        timeoutPromise,
      ]);
      const company = data ?? {};
      setCurrentCompany(company);
      onSynchronizeRef.current?.(company);
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [companyId, dispatch, errorMessage]);

  useEffect(() => {
    loadData();
    return () => {
      dispatch(setIsNotBusy());
    };
  }, [loadData, dispatch]);

  const markChange = useCallback(() => {
    setCurrentCompany((prev) => (prev ? { ...prev, IsModified: true } : null));
    setSaveSuccess(null);
    onMarkModified?.(true);
  }, [onMarkModified]);

  const handleSave = useCallback(async () => {
    if (!currentCompany) return;
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.saveOneAppCompanyExDto(currentCompany);
      const messages = extractErrorMessages(data?.ValidationResult);
      setErrorMessages(messages);
      if (data?.IsSuccessful && data?.Object) {
        setCurrentCompany(data.Object);
        onSynchronize?.(data.Object);
        onMarkModified?.(false);
        setSaveSuccess('Saved successfully.');
        setTimeout(() => setSaveSuccess(null), 3000);
      }
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentCompany, dispatch, errorMessage, onSynchronize, onMarkModified]);

  if (!companyId) {
    return (
      <div className={`w-full h-full overflow-auto px-5 py-5 ${theme.mainContentSection}`}>
        <p className={`text-xs ${theme.label}`}>Select a company to edit.</p>
      </div>
    );
  }

  if (!currentCompany) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <i className="fa-solid fa-rotate fa-spin text-gray-400 text-xl" />
      </div>
    );
  }

  const other = currentCompany.OtherSettingsDto ?? {};

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      {/* Company Info own header and buttons - buttons aligned left next to title */}
      <div className={`flex items-center gap-3 px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Company: {currentCompany.Code ?? ''}</div>
        <button type="button" className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500" onClick={loadData} title="Refresh">
          <i className="fa fa-refresh" aria-hidden="true" />
        </button>
        <button
          type="button"
          className="w-8 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500 disabled:opacity-50 disabled:cursor-not-allowed"
          onClick={handleSave}
          disabled={!currentCompany.IsModified}
          title="Save"
        >
          <i className="fa fa-save" aria-hidden="true" />
        </button>
        {saveSuccess && <span className="text-green-600 text-xs">{saveSuccess}</span>}
        {errorMessages.length > 0 && <span className="text-red-600 text-xs">{errorMessages[0]}</span>}
      </div>

      <div className={`w-full flex-1 min-h-0 overflow-auto ${theme.mainContentSection}`}>
        <div className="h-full w-full overflow-auto px-5 py-5 max-w-[900px]">
          {/* Panel: General Info - Row1 two columns, Row2 full width */}
          <div className={`border border-gray-200 rounded bg-white dark:bg-gray-900/50 p-4 mb-4 ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold mb-3 ${theme.title}`}>General Info</div>
            <div className="grid grid-cols-2 gap-x-6 gap-y-3">
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Company Code</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={currentCompany.Code ?? ''}
                  onChange={(e) => {
                    setCurrentCompany((p) => (p ? { ...p, Code: e.target.value } : null));
                    markChange();
                  }}
                />
              </div>
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Short Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={currentCompany.ShortName ?? ''}
                  onChange={(e) => {
                    setCurrentCompany((p) => (p ? { ...p, ShortName: e.target.value } : null));
                    markChange();
                  }}
                />
              </div>
              <div className="flex items-center py-1 col-span-2">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Full Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={currentCompany.FullName ?? ''}
                  onChange={(e) => {
                    setCurrentCompany((p) => (p ? { ...p, FullName: e.target.value } : null));
                    markChange();
                  }}
                />
              </div>
            </div>
          </div>

          {/* Panel: Business Partner Registration - 4 rows x 2 cols, aligned (flex wrap) */}
          <div className={`border border-gray-200 rounded bg-white dark:bg-gray-900/50 p-4 mb-4 ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold mb-3 ${theme.title}`}>Business Partner Registration</div>
            <div className="flex flex-col gap-1">
              {/* Row 1 */}
              <div className="flex flex-wrap items-center min-h-8 gap-x-6 gap-y-1">
                <div className="flex items-center gap-2 shrink-0 w-64">
                  <input
                    type="checkbox"
                    id="bp-client"
                    className={`h-3 w-3 text-blue-600 focus:ring-blue-500 ${theme.inputBox} rounded`}
                    checked={!!other.IsEnableClientSelfRegistration}
                    onChange={(e) => {
                      setCurrentCompany((p) => {
                        if (!p) return p;
                        const o = { ...(p.OtherSettingsDto ?? {}), IsEnableClientSelfRegistration: e.target.checked };
                        return { ...p, OtherSettingsDto: o };
                      });
                      markChange();
                    }}
                  />
                  <label htmlFor="bp-client" className={`text-xs ${theme.label} cursor-pointer`}>Enable Client Registration</label>
                </div>
                <div className="flex items-center flex-1 min-w-0">
                  <label className={`min-w-[11rem] w-44 text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`}>Client Label Name</label>
                  <input
                    type="text"
                    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none min-w-0`}
                    value={other.ClientLabelName ?? ''}
                    onChange={(e) => {
                      setCurrentCompany((p) => {
                        if (!p) return p;
                        const o = { ...(p.OtherSettingsDto ?? {}), ClientLabelName: e.target.value };
                        return { ...p, OtherSettingsDto: o };
                      });
                      markChange();
                    }}
                  />
                </div>
              </div>
              {/* Row 2 */}
              <div className="flex flex-wrap items-center min-h-8 gap-x-6 gap-y-1">
                <div className="flex items-center gap-2 shrink-0 w-64">
                  <input
                    type="checkbox"
                    id="bp-supplier"
                    className={`h-3 w-3 text-blue-600 focus:ring-blue-500 ${theme.inputBox} rounded`}
                    checked={!!other.IsEnableSupplierSelfRegistration}
                    onChange={(e) => {
                      setCurrentCompany((p) => {
                        if (!p) return p;
                        const o = { ...(p.OtherSettingsDto ?? {}), IsEnableSupplierSelfRegistration: e.target.checked };
                        return { ...p, OtherSettingsDto: o };
                      });
                      markChange();
                    }}
                  />
                  <label htmlFor="bp-supplier" className={`text-xs ${theme.label} cursor-pointer`}>Enable Supplier Registration</label>
                </div>
                <div className="flex items-center flex-1 min-w-0">
                  <label className={`min-w-[11rem] w-44 text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`}>Supplier Label Name</label>
                  <input
                    type="text"
                    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none min-w-0`}
                    value={other.SupplierLabelName ?? ''}
                    onChange={(e) => {
                      setCurrentCompany((p) => {
                        if (!p) return p;
                        const o = { ...(p.OtherSettingsDto ?? {}), SupplierLabelName: e.target.value };
                        return { ...p, OtherSettingsDto: o };
                      });
                      markChange();
                    }}
                  />
                </div>
              </div>
              {/* Row 3 */}
              <div className="flex flex-wrap items-center min-h-8 gap-x-6 gap-y-1">
                <div className="flex items-center gap-2 shrink-0 w-64">
                  <input
                    type="checkbox"
                    id="bp-client-agent"
                    className={`h-3 w-3 text-blue-600 focus:ring-blue-500 ${theme.inputBox} rounded`}
                    checked={!!other.IsEnableClientAgentSelfRegistration}
                    onChange={(e) => {
                      setCurrentCompany((p) => {
                        if (!p) return p;
                        const o = { ...(p.OtherSettingsDto ?? {}), IsEnableClientAgentSelfRegistration: e.target.checked };
                        return { ...p, OtherSettingsDto: o };
                      });
                      markChange();
                    }}
                  />
                  <label htmlFor="bp-client-agent" className={`text-xs ${theme.label} cursor-pointer`}>Enable Client Agent Registration</label>
                </div>
                <div className="flex items-center flex-1 min-w-0">
                  <label className={`min-w-[11rem] w-44 text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`}>Client Agent Label Name</label>
                  <input
                    type="text"
                    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none min-w-0`}
                    value={other.ClientAgentLabelName ?? ''}
                    onChange={(e) => {
                      setCurrentCompany((p) => {
                        if (!p) return p;
                        const o = { ...(p.OtherSettingsDto ?? {}), ClientAgentLabelName: e.target.value };
                        return { ...p, OtherSettingsDto: o };
                      });
                      markChange();
                    }}
                  />
                </div>
              </div>
              {/* Row 4 */}
              <div className="flex flex-wrap items-center min-h-8 gap-x-6 gap-y-1">
                <div className="flex items-center gap-2 shrink-0 w-64">
                  <input
                    type="checkbox"
                    id="bp-supplier-agent"
                    className={`h-3 w-3 text-blue-600 focus:ring-blue-500 ${theme.inputBox} rounded`}
                    checked={!!other.IsEnableSupplierAgentSelfRegistration}
                    onChange={(e) => {
                      setCurrentCompany((p) => {
                        if (!p) return p;
                        const o = { ...(p.OtherSettingsDto ?? {}), IsEnableSupplierAgentSelfRegistration: e.target.checked };
                        return { ...p, OtherSettingsDto: o };
                      });
                      markChange();
                    }}
                  />
                  <label htmlFor="bp-supplier-agent" className={`text-xs ${theme.label} cursor-pointer`}>Enable Supplier Agent Registration</label>
                </div>
                <div className="flex items-center flex-1 min-w-0">
                  <label className={`min-w-[11rem] w-44 text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`}>Supplier Agent Label Name</label>
                  <input
                    type="text"
                    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none min-w-0`}
                    value={other.SupplierAgentLabelName ?? ''}
                    onChange={(e) => {
                      setCurrentCompany((p) => {
                        if (!p) return p;
                        const o = { ...(p.OtherSettingsDto ?? {}), SupplierAgentLabelName: e.target.value };
                        return { ...p, OtherSettingsDto: o };
                      });
                      markChange();
                    }}
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Company Logo + Login Page Background: two columns, same layout + taller image area */}
          <div className="flex flex-wrap gap-4 mb-4">
            {/* Left: Company Logo */}
            <div className={`flex-1 min-w-[280px] border border-gray-200 rounded bg-white dark:bg-gray-900/50 p-4 ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold mb-3 ${theme.title}`}>Company Logo</div>
              <div className="flex flex-col items-start gap-3">
                <div className="h-44 w-56 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg flex items-center justify-center bg-gray-50 dark:bg-gray-800/80 overflow-hidden shrink-0">
                  {currentCompany.LogoImageUrl ? (
                    <img src={toAbsoluteResourceUrl(currentCompany.LogoImageUrl)} alt="Logo" className="max-h-full max-w-full object-contain" />
                  ) : (
                    <span className={`text-xs ${theme.label} text-center px-2`}>No logo</span>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    className="h-7 px-3 text-xs rounded border bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-600"
                    title="Upload logo"
                    onClick={() => setUploadPopup('logo')}
                  >
                    <i className="fa fa-upload mr-1" aria-hidden="true" /> Upload
                  </button>
                  <button
                    type="button"
                    className="h-7 px-3 text-xs rounded border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 disabled:opacity-50 disabled:cursor-not-allowed"
                    title="Remove logo"
                    disabled={!currentCompany.LogoImageUrl}
                    onClick={async () => {
                      if (!currentCompany.LogoImageUrl || !window.confirm('Delete the company logo?')) return;
                      dispatch(setIsBusy());
                      try {
                        const data = await adminSvc.deleteCompanyLogoImage();
                        if (data?.IsSuccessful) await loadData();
                      } finally {
                        dispatch(setIsNotBusy());
                      }
                    }}
                  >
                    <i className="fa fa-trash mr-1" aria-hidden="true" /> Remove
                  </button>
                </div>
              </div>
            </div>

            {/* Right: Login Page Background - same layout and size as Company Logo, no inline delete */}
            <div className={`flex-1 min-w-[280px] border border-gray-200 rounded bg-white dark:bg-gray-900/50 p-4 ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold mb-3 ${theme.title}`}>Login Page Background</div>
              <div className="flex flex-col items-start gap-3">
                <div className="h-44 w-56 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg flex items-center justify-center bg-gray-50 dark:bg-gray-800/80 overflow-hidden shrink-0">
                  {(currentCompany.BackgroundImageUrlList ?? []).length > 0 ? (
                    <img
                      src={toAbsoluteResourceUrl((currentCompany.BackgroundImageUrlList ?? [])[0])}
                      alt=""
                      className="max-h-full max-w-full object-contain"
                    />
                  ) : (
                    <span className={`text-xs ${theme.label} text-center px-2`}>No background image</span>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    className="h-7 px-3 text-xs rounded border bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-600"
                    title="Upload background image"
                    onClick={() => setUploadPopup('background')}
                  >
                    <i className="fa fa-upload mr-1" aria-hidden="true" /> Upload
                  </button>
                  <button
                    type="button"
                    className="h-7 px-3 text-xs rounded border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 disabled:opacity-50 disabled:cursor-not-allowed"
                    title="Remove all background images"
                    disabled={(currentCompany.BackgroundImageUrlList ?? []).length === 0}
                    onClick={async () => {
                      const list = currentCompany.BackgroundImageUrlList ?? [];
                      if (list.length === 0 || !window.confirm('Remove all background images?')) return;
                      dispatch(setIsBusy());
                      try {
                        for (const imgUrl of list) {
                          const fileName = imgUrl.lastIndexOf('/') >= 0 ? imgUrl.slice(imgUrl.lastIndexOf('/') + 1) : imgUrl;
                          await adminSvc.deleteOneCompanyBackgroundImage(fileName);
                        }
                        await loadData();
                      } finally {
                        dispatch(setIsNotBusy());
                      }
                    }}
                  >
                    <i className="fa fa-trash mr-1" aria-hidden="true" /> Remove
                  </button>
                </div>
              </div>
            </div>
          </div>

        </div>
      </div>

      {uploadPopup === 'logo' && (
        <FileUploader
          isOpen={true}
          onClose={() => setUploadPopup(null)}
          mode="folder"
          folderCallingFrom="UploadCompanyLogoImage"
          title="Upload Company Logo"
          accept="image/jpeg,image/png,image/gif,image/bmp"
          queueLimit={1}
          onUploaded={() => loadData()}
        />
      )}
      {uploadPopup === 'background' && (
        <FileUploader
          isOpen={true}
          onClose={() => setUploadPopup(null)}
          mode="folder"
          folderCallingFrom="UploadCompanyBackgrundImage"
          title="Upload Login Page Background"
          accept="image/jpeg,image/png,image/gif,image/bmp"
          queueLimit={1}
          onUploaded={() => loadData()}
        />
      )}
    </div>
  );
};

export default CompanyEditor;
