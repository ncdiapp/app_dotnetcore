import React, { useCallback, useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import BusinessPartnerEditor from './BusinessPartnerEditor';

type Props = {
  companyId: string | number | null;
  /** Numeric EmAppUserType: 3=Customer, 4=Supplier, 5=ClientAgent, 9=SupplierAgent */
  partnerType: number;
  isEmbedded?: boolean;
};

type PartnerDto = {
  Id: number;
  Code: string;
  ShortName: string;
  FullName?: string;
  IsModified?: boolean;
};

const PARTNER_TYPE_LABELS: Record<number, string> = {
  3: 'Customers',
  4: 'Suppliers',
  5: 'Client Agents',
  9: 'Supplier Agents',
};

const BusinessPartnerManagement: React.FC<Props> = ({
  companyId,
  partnerType,
  isEmbedded: _isEmbedded,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [_partners, setPartners] = useState<PartnerDto[]>([]);
  const [partnersCV, setPartnersCV] = useState<CollectionView | null>(null);
  const [currentPartner, setCurrentPartner] = useState<PartnerDto | null>(null);
  const [isShowLeftPanel, setIsShowLeftPanel] = useState(true);
  const [showNewPartnerPopup, setShowNewPartnerPopup] = useState(false);
  const [newPartner, setNewPartner] = useState({ Code: '', ShortName: '', FullName: '' });
  const [errors, setErrors] = useState<string[]>([]);

  /** @param selectFirst when true (e.g. after tab switch), always select first partner; when false/undefined (e.g. Refresh), only select first if none selected */
  const loadPartners = useCallback(async (selectFirst?: boolean) => {
    if (!companyId) return;
    dispatch(setIsBusy());
    try {
      const list = await adminSvc.retrieveCompanyPartnerDtoList(String(companyId), partnerType);
      const arr = Array.isArray(list) ? list : [];
      setPartners(arr);
      const cv = new CollectionView(arr);
      setPartnersCV(cv);
      setCurrentPartner((prev) => {
        const shouldSelectFirst = selectFirst === true || (arr.length > 0 && !prev);
        if (arr.length > 0 && shouldSelectFirst) {
          cv.moveCurrentToFirst();
          return arr[0];
        }
        return prev;
      });
    } catch (e) {
      setPartners([]);
      setPartnersCV(new CollectionView<any>([]));
      if (selectFirst) setCurrentPartner(null);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [companyId, partnerType, dispatch]);

  // When User Type tab changes (e.g. Customer -> Supplier), clear selection then load and select first partner of new type
  useEffect(() => {
    setCurrentPartner(null);
  }, [partnerType, companyId]);

  useEffect(() => {
    loadPartners(true); // selectFirst so first partner is chosen after list loads (avoids race with stale currentPartner)
  }, [companyId, partnerType, loadPartners]);

  const handlePartnerSelect = useCallback((s: any) => {
    const flex = s?.control ?? s;
    const rowIndex = flex?.selection?.row ?? -1;
    if (rowIndex < 0 || !flex?.rows?.length) return;
    const row = flex.rows[rowIndex];
    const item = row?.dataItem as PartnerDto | null;
    if (item) setCurrentPartner(item);
  }, []);

  const synchronizePartner = useCallback((partner: PartnerDto) => {
    setCurrentPartner((prev) => (prev && prev.Id === partner.Id ? { ...prev, ...partner, IsModified: false } : prev));
    setPartners((prev) => {
      const next = prev.map((p) => (p.Id === partner.Id ? { ...p, ...partner, IsModified: false } : p));
      if (partnersCV) {
        partnersCV.sourceCollection = [...next];
        partnersCV.refresh();
      }
      return next;
    });
  }, [partnersCV]);

  const openNewPartnerPopup = useCallback(() => {
    setNewPartner({ Code: '', ShortName: '', FullName: '' });
    setErrors([]);
    setShowNewPartnerPopup(true);
  }, []);

  const saveNewPartner = useCallback(async () => {
    if (!newPartner.Code?.trim()) {
      setErrors(['Partner Code is required.']);
      return;
    }
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.saveOneAppBusinessPartnerExDto({
        ...newPartner,
        AppCompanyId: companyId,
        PartnerType: partnerType,
      });
      const errs = data?.ValidationResult
        ? (Array.isArray(data.ValidationResult)
          ? data.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean)
          : [])
        : [];
      setErrors(errs);

      if (data?.IsSuccessful && data?.Object) {
        setShowNewPartnerPopup(false);
        const newP = data.Object;
        setPartners((prev) => [...prev, newP]);
        if (partnersCV) {
          partnersCV.sourceCollection = [...partnersCV.sourceCollection as any[], newP];
          partnersCV.refresh();
        }
        setCurrentPartner(newP);
      }
    } catch (e) {
      setErrors([e instanceof Error ? e.message : String(e)]);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [newPartner, companyId, partnerType, partnersCV, dispatch]);

  if (!companyId) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className={`text-xs ${theme.label}`}>Select a company to manage partners.</p>
      </div>
    );
  }

  const typeLabel = PARTNER_TYPE_LABELS[partnerType] || 'Partners';

  return (
    <div className={`w-full h-full flex overflow-hidden ${theme.mainContentSection}`}>
      {/* Left Panel: Partner List */}
      {isShowLeftPanel && partnersCV && (
        <div className="flex flex-col border-r shrink-0" style={{ width: 280 }}>
          <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
            <span className={`text-md font-semibold ${theme.title}`}>{typeLabel}</span>
            <div className="flex items-center gap-1">
              <button
                type="button"
                className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600"
                onClick={openNewPartnerPopup}
                title="New Partner"
              >
                <i className="fa-solid fa-plus" aria-hidden="true" />
              </button>
              <button
                type="button"
                className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                onClick={() => loadPartners()}
                title="Refresh"
              >
                <i className="fa-solid fa-refresh" aria-hidden="true" />
              </button>
              <button
                type="button"
                className="w-8 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500"
                onClick={() => setIsShowLeftPanel(false)}
                title="Hide list"
              >
                <i className="fa-solid fa-chevron-left" aria-hidden="true" />
              </button>
            </div>
          </div>
          <div className="flex-1 min-h-0">
            <FlexGrid
              itemsSource={partnersCV}
              selectionMode="Row"
              headersVisibility="Column"
              isReadOnly={true}
              selectionChanged={handlePartnerSelect}
              style={{ height: '100%' }}
            >
              <FlexGridColumn binding="Code" header="Code" width={100} />
              <FlexGridColumn binding="ShortName" header="Short Name" width="*" />
            </FlexGrid>
          </div>
        </div>
      )}

      {/* Right Panel: Partner Editor */}
      <div className="flex-1 flex flex-col min-w-0">
        {!isShowLeftPanel && (
          <div className={`flex items-center px-3 py-2 border-b ${theme.mainContentSection}`}>
            <button
              type="button"
              className="w-8 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 mr-2"
              onClick={() => setIsShowLeftPanel(true)}
              title="Show partner list"
            >
              <i className="fa-solid fa-chevron-right" aria-hidden="true" />
            </button>
            <span className={`text-md font-semibold ${theme.title}`}>
              {currentPartner?.ShortName || currentPartner?.Code || 'Select a partner'}
            </span>
          </div>
        )}
        <div className="flex-1 min-h-0">
          <BusinessPartnerEditor
            key={currentPartner?.Id ?? 'none'}
            partnerId={currentPartner?.Id ?? null}
            partnerType={partnerType}
            companyId={companyId}
            isEmbedded
            onSynchronize={synchronizePartner}
          />
        </div>
      </div>

      {/* New Partner Popup */}
      {showNewPartnerPopup && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
          <div
            className={`${theme.mainContentSection} rounded shadow-xl w-[360px] max-w-[90vw] flex flex-col`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 mb-1 border-b ${theme.mainContentSection}`}>
              <span className={`text-md font-semibold ${theme.title}`}>New Partner</span>
              <button type="button" className="text-lg leading-none w-6 h-6" onClick={() => setShowNewPartnerPopup(false)}>&times;</button>
            </div>
            <div className="h-full w-full overflow-auto px-5 py-5">
              <div className="flex items-center py-1">
                <label className={`w-24 text-xs ${theme.label} mr-2`}>Code</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={newPartner.Code}
                  onChange={(e) => setNewPartner((p) => ({ ...p, Code: e.target.value }))}
                />
              </div>
              <div className="flex items-center py-1">
                <label className={`w-24 text-xs ${theme.label} mr-2`}>Short Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={newPartner.ShortName}
                  onChange={(e) => setNewPartner((p) => ({ ...p, ShortName: e.target.value }))}
                />
              </div>
              <div className="flex items-center py-1">
                <label className={`w-24 text-xs ${theme.label} mr-2`}>Full Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={newPartner.FullName}
                  onChange={(e) => setNewPartner((p) => ({ ...p, FullName: e.target.value }))}
                />
              </div>
              {errors.length > 0 && (
                <div className="text-red-600 text-xs mt-2">
                  {errors.map((msg, i) => (
                    <div key={i}>{msg}</div>
                  ))}
                </div>
              )}
            </div>
            <div className="px-5 pb-5 flex justify-end space-x-2">
              <button
                type="button"
                className="w-8 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500"
                onClick={saveNewPartner}
                title="Save"
              >
                <i className="fa-solid fa-save" />
              </button>
              <button
                type="button"
                className="w-8 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500"
                onClick={() => setShowNewPartnerPopup(false)}
                title="Cancel"
              >
                <i className="fa-solid fa-times" />
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default BusinessPartnerManagement;
