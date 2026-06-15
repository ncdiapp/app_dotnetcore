import React, { useCallback, useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { InputDate } from '@mescius/wijmo.react.input';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';

type Props = { companyId: string | number | null; isEmbedded?: boolean };

const CompanyIntegrationTokenManagement: React.FC<Props> = ({ companyId, isEmbedded }) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const [tokensCV, setTokensCV] = useState<CollectionView | null>(null);
  const [showTokenEditor, setShowTokenEditor] = useState(false);
  const [tokenObj, setTokenObj] = useState<{ Id?: number; UserName?: string; TokenExpirationDate?: Date } | null>(null);
  const [tokenErrors, setTokenErrors] = useState<string[]>([]);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const list = await adminSvc.retrieveAllIntegrationTokenDto();
      const arr = Array.isArray(list) ? list : [];
      setTokensCV(new CollectionView(arr));
    } catch (e) {
      setTokensCV(new CollectionView<any>([]));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const openCreateToken = useCallback(() => {
    const nextYear = new Date();
    nextYear.setFullYear(nextYear.getFullYear() + 1);
    setTokenObj({ UserName: '', TokenExpirationDate: nextYear });
    setTokenErrors([]);
    setShowTokenEditor(true);
  }, []);

  const openEditToken = useCallback((item: any) => {
    if (!item) return;
    setTokenObj({
      Id: item.Id ?? item.UserId,
      UserName: item.UserName ?? '',
      TokenExpirationDate: item.TokenExpirationDate ? new Date(item.TokenExpirationDate) : undefined,
    });
    setTokenErrors([]);
    setShowTokenEditor(true);
  }, []);

  const closeTokenEditor = useCallback(() => {
    setShowTokenEditor(false);
    setTokenObj(null);
    setTokenErrors([]);
  }, []);

  const saveToken = useCallback(async () => {
    if (!tokenObj?.UserName?.trim()) {
      setTokenErrors(['Application Name is required.']);
      return;
    }
    if (!tokenObj.TokenExpirationDate) {
      setTokenErrors(['Expiration Date is required.']);
      return;
    }
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.saveOneIntegrationTokenExDto(tokenObj);
      const errs = data?.ValidationResult ? (Array.isArray(data.ValidationResult) ? data.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean) : []) : [];
      setTokenErrors(errs);
      if (data?.IsSuccessful) {
        closeTokenEditor();
        await loadData();
      }
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [tokenObj, dispatch, loadData, closeTokenEditor, errorMessage]);

  const deleteToken = useCallback(async (item: any) => {
    const id = item?.Id ?? item?.UserId;
    if (id == null) return;
    if (!window.confirm('Do you want to delete this token?')) return;
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.deleteAppSecurityUser(String(id));
      if (data?.IsSuccessful) await loadData();
      else if (data?.ValidationResult) {
        const errs = Array.isArray(data.ValidationResult) ? data.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean) : [];
        errorMessage.showError(errs.join(' ') || 'Delete failed.');
      }
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, loadData, errorMessage]);

  const formatDate = (val: any) => {
    if (val == null) return '';
    const d = new Date(val);
    return isNaN(d.getTime()) ? '' : d.toLocaleString();
  };

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Integration Tokens</div>
        <div className="flex items-center space-x-2">
          <button type="button" className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600" onClick={openCreateToken} title="Create Token">
            <i className="fa fa-plus" aria-hidden="true" />
          </button>
          <button type="button" className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500" onClick={loadData} title="Refresh">
            <i className="fa fa-refresh" aria-hidden="true" />
          </button>
        </div>
      </div>
      <div className={`w-full flex-1 min-h-0 overflow-hidden ${theme.mainContentSection}`}>
        {tokensCV && (
          <FlexGrid
            itemsSource={tokensCV}
            selectionMode="Row"
            headersVisibility="Column"
            isReadOnly={true}
            style={{ height: '100%' }}
          >
            <FlexGridColumn header="" width={70}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => {
                  const item = cell.item;
                  if (!item) return null;
                  return (
                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        className={theme.menu_default}
                        title="Edit"
                        onClick={() => openEditToken(item)}
                      >
                        <i className="fa fa-pencil" aria-hidden="true" />
                      </button>
                      <button
                        type="button"
                        className={theme.menu_default}
                        title="Delete"
                        onClick={() => deleteToken(item)}
                      >
                        <i className="fa fa-trash-o" aria-hidden="true" />
                      </button>
                    </div>
                  );
                }}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="UserName" header="Application Name" width={200} />
            <FlexGridColumn binding="LoginName" header="Application Key" width={200} />
            <FlexGridColumn binding="Password" header="Secret" width={150} />
            <FlexGridColumn binding="AccessToken" header="Access Token" width={200} />
            <FlexGridColumn header="Expiration Date" width={160}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => (cell.item ? formatDate(cell.item.TokenExpirationDate) : '')}
              />
            </FlexGridColumn>
            <FlexGridColumn header="Created Date" width={140}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => (cell.item ? formatDate(cell.item.CreateDate) : '')}
              />
            </FlexGridColumn>
            <FlexGridColumn header="Updated Date" width={140}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => (cell.item ? formatDate(cell.item.UpDateDate) : '')}
              />
            </FlexGridColumn>
            <FlexGridColumn width="*" />
          </FlexGrid>
        )}
      </div>

      {/* Token Editor modal */}
      {showTokenEditor && tokenObj && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
          <div
            className={`${theme.mainContentSection} rounded shadow-xl w-[400px] max-w-[90vw] flex flex-col`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 mb-1 border-b ${theme.mainContentSection}`}>
              <span className={`text-md font-semibold ${theme.title}`}>Integration Token</span>
              <button type="button" className="text-lg leading-none w-6 h-6" onClick={closeTokenEditor}>&times;</button>
            </div>
            <div className="h-full w-full overflow-auto px-5 py-5">
              <div className="flex items-center py-1 gap-2">
                <label className={`w-32 shrink-0 text-xs ${theme.label}`}>Application Name</label>
                <input
                  type="text"
                  className={`flex-auto min-w-0 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={tokenObj.UserName ?? ''}
                  onChange={(e) => setTokenObj((p) => (p ? { ...p, UserName: e.target.value } : null))}
                />
              </div>
              <div className="flex items-center py-1 gap-2">
                <label className={`w-32 shrink-0 text-xs ${theme.label}`}>Expiration Date</label>
                <InputDate
                  value={tokenObj.TokenExpirationDate}
                  valueChanged={(s: any) => setTokenObj((p) => (p ? { ...p, TokenExpirationDate: s.value } : null))}
                  format="g"
                  className="flex-auto min-w-0"
                />
              </div>
              {tokenErrors.length > 0 && (
                <div className="text-red-600 text-xs mt-2">
                  {tokenErrors.map((msg, i) => (
                    <div key={i}>{msg}</div>
                  ))}
                </div>
              )}
            </div>
            <div className="px-5 pb-5 flex items-center space-x-2">
              <button type="button" className="w-8 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500" onClick={saveToken} title="Save">
                <i className="fa fa-save" aria-hidden="true" />
              </button>
              <button type="button" className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500" onClick={closeTokenEditor} title="Cancel">
                <i className="fa fa-times" aria-hidden="true" />
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default CompanyIntegrationTokenManagement;
