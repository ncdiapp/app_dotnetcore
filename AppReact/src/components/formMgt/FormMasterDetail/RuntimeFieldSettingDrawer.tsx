import React, { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import appHelper from '../../../helper/appHelper';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { adminSvc } from '../../../webapi/adminsvc';
import TransactionFieldEditorPanel from './TransactionFieldEditorPanel';

export type FieldSettingAnchorRect = {
  top: number;
  left: number;
  right: number;
  bottom: number;
  width: number;
  height: number;
};

export type RuntimeFieldSettingDrawerProps = {
  isOpen: boolean;
  onClose: () => void;
  transactionId: number;
  transactionFieldId: number;
  layoutItemId?: string | number | null;
  isGridField?: boolean;
  /** Viewport rect of the gear button — popup is placed beside it (does not cover the field). */
  anchorRect: FieldSettingAnchorRect;
  onSaved?: () => void;
};

const POPUP_GAP = 8;
const VIEW_MARGIN = 8;

function positionPopupNearAnchor(
  anchor: FieldSettingAnchorRect,
  popupWidth: number,
  popupHeight: number
): { top: number; left: number } {
  const vw = typeof window !== 'undefined' ? window.innerWidth : 1024;
  const vh = typeof window !== 'undefined' ? window.innerHeight : 768;

  let left = anchor.right + POPUP_GAP;
  if (left + popupWidth > vw - VIEW_MARGIN) {
    const leftOfAnchor = anchor.left - popupWidth - POPUP_GAP;
    if (leftOfAnchor >= VIEW_MARGIN) {
      left = leftOfAnchor;
    } else {
      left = Math.max(VIEW_MARGIN, Math.min(left, vw - popupWidth - VIEW_MARGIN));
    }
  }

  let top = anchor.top;
  if (top + popupHeight > vh - VIEW_MARGIN) {
    top = Math.max(VIEW_MARGIN, vh - popupHeight - VIEW_MARGIN);
  }
  if (top < VIEW_MARGIN) {
    top = VIEW_MARGIN;
  }
  return { top, left };
}

function buildTransactionEditorState(appTransactionData: any) {
  const dictTransactionFieldIdAndDto: Record<number, any> = {};
  const dictChildGridTransactionUnitIdAndDto: Record<number, any> = {};
  const dictUnitIdAndDto: Record<number, any> = {};
  const dictCommandActionIdAndDto: Record<number, any> = {};
  const rootLevelUnitFieldList: any[] = [];
  const childLevelUnitList: any[] = [];
  const siblingUnitList: any[] = [];
  const dictRootLevelUnitLinkedSearchIdAndDto: Record<number, any> = {};
  const dictTransfieldIdAndFormulaDto: Record<number, any> = {};

  if (appTransactionData.CommandActionList) {
    appTransactionData.CommandActionList.forEach((cmd: any) => {
      dictCommandActionIdAndDto[cmd.Id] = cmd;
    });
  }

  if (appTransactionData.AppTransactionUnitList?.length) {
    appTransactionData.AppTransactionUnitList.forEach((unit: any) => {
      dictUnitIdAndDto[unit.Id] = unit;
      if (unit.AppTransactionFieldList) {
        unit.AppTransactionFieldList.forEach((field: any) => {
          dictTransactionFieldIdAndDto[field.Id] = field;
          rootLevelUnitFieldList.push(field);
        });
      }
      if (unit.IsMasterSiblingUnit) siblingUnitList.push(unit);
      if (unit.AppTransactionUnitLinkedSearchList) {
        unit.AppTransactionUnitLinkedSearchList.forEach((linkedSearch: any) => {
          dictRootLevelUnitLinkedSearchIdAndDto[linkedSearch.Id] = linkedSearch;
        });
      }
      if (unit.Children) {
        unit.Children.forEach((childUnit: any) => {
          dictUnitIdAndDto[childUnit.Id] = childUnit;
          dictChildGridTransactionUnitIdAndDto[childUnit.Id] = childUnit;
          childLevelUnitList.push(childUnit);
          if (childUnit.AppTransactionFieldList) {
            childUnit.AppTransactionFieldList.forEach((field: any) => {
              dictTransactionFieldIdAndDto[field.Id] = field;
            });
          }
          if (childUnit.Children) {
            childUnit.Children.forEach((grandchildUnit: any) => {
              dictUnitIdAndDto[grandchildUnit.Id] = grandchildUnit;
              if (grandchildUnit.AppTransactionFieldList) {
                grandchildUnit.AppTransactionFieldList.forEach((field: any) => {
                  dictTransactionFieldIdAndDto[field.Id] = field;
                });
              }
            });
          }
        });
      }
    });
  }

  const dictUnitldIdAndFormulaSetDto = appTransactionData?.DictUnitldIdAndFormulaSetDto;
  if (dictUnitldIdAndFormulaSetDto) {
    Object.values(dictUnitldIdAndFormulaSetDto as Record<string, any>).forEach((formularSetDto: any) => {
      const list = formularSetDto?.ListAppTransactionUnitFormula || [];
      list.forEach((formulaDto: any) => {
        const assignTo = formulaDto?.AssignToTransFieldId;
        if (!assignTo) return;
        const expr = formulaDto?.FormulaExpression || '';
        let displayText = expr;
        const idxEq = expr.indexOf('=');
        if (idxEq >= 0 && expr.length > idxEq) displayText = expr.substring(idxEq + 1);
        formulaDto.displayText = displayText;
        dictTransfieldIdAndFormulaDto[assignTo] = formulaDto;
      });
    });
  }

  return {
    AppTransactionData: appTransactionData,
    dictTransactionFieldIdAndDto,
    dictChildGridTransactionUnitIdAndDto,
    dictUnitIdAndDto,
    dictCommandActionIdAndDto,
    rootLevelUnitFieldList,
    childLevelUnitList,
    siblingUnitList,
    dictRootLevelUnitLinkedSearchIdAndDto,
    dictTransfieldIdAndFormulaDto,
  };
}

function getPropertiesFromStyleString(styleString: string | null | undefined): { width: number; height: number } {
  if (!styleString) return { width: 330, height: 30 };
  const arrayStyleInfo = styleString.split(';');
  if (arrayStyleInfo.length >= 2) {
    const wPart = arrayStyleInfo[0].split(':')[1];
    const hPart = arrayStyleInfo[1].split(':')[1];
    const widthValue = parseInt(wPart, 10);
    const heightValue = parseInt(hPart, 10);
    return {
      width: Number.isFinite(widthValue) ? widthValue : 330,
      height: Number.isFinite(heightValue) ? heightValue : 30,
    };
  }
  return { width: 330, height: 30 };
}

function replaceFieldInHierarchy(appTransactionData: any, field: any) {
  if (!appTransactionData?.AppTransactionUnitList || !field?.Id) return;
  const patchList = (list: any[]) => {
    if (!list) return;
    for (let i = 0; i < list.length; i++) {
      if (list[i]?.Id === field.Id) {
        list[i] = field;
      }
    }
  };
  const walk = (units: any[]) => {
    for (const u of units || []) {
      patchList(u.AppTransactionFieldList);
      if (u.Children?.length) walk(u.Children);
    }
  };
  walk(appTransactionData.AppTransactionUnitList);
}

function getStyleString(widthValue: number, heightValue: number): string {
  let w = widthValue;
  let h = heightValue;
  w = w - (w % 10 >= 5 ? (w % 10) - 10 : w % 10);
  h = h - (h % 10 >= 5 ? (h % 10) - 10 : h % 10);
  return `width:${w}px;height:${h}px;`;
}

const RuntimeFieldSettingDrawer: React.FC<RuntimeFieldSettingDrawerProps> = ({
  isOpen,
  onClose,
  transactionId,
  transactionFieldId,
  layoutItemId,
  isGridField,
  anchorRect,
  onSaved,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showInfo, showValidationMessages } = useErrorMessage();
  const emControl = useEnumValues('EmAppControlType');

  const popupRef = useRef<HTMLDivElement | null>(null);
  const [popupPos, setPopupPos] = useState(() => {
    const estW = Math.min(448, typeof window !== 'undefined' ? window.innerWidth - 16 : 448);
    return positionPopupNearAnchor(anchorRect, estW, 280);
  });
  const [zIndex, setZIndex] = useState(6000);

  const [loading, setLoading] = useState(false);
  const [currentTransField, setCurrentTransField] = useState<any>(null);
  const [orgTransField, setOrgTransField] = useState<any>(null);
  const [transactionData, setTransactionData] = useState<any>(null);
  const [dictEntityIdAndDto, setDictEntityIdAndDto] = useState<Record<number, any>>({});
  const [gridFormFieldStyleObj, setGridFormFieldStyleObj] = useState({ width: 330, height: 30 });

  const load = useCallback(async () => {
    if (!transactionId || !transactionFieldId) return;
    setLoading(true);
    try {
      dispatch(setIsBusy());
      const hierarchy = await appTransactionService.getOneHierarchyTransaction(
        String(transactionId),
        false,
        '',
        '',
        '',
        false,
        ''
      );
      const entities = await adminSvc.retrieveAllAppEntityInfoDto(false);
      const dictEnt: Record<number, any> = {};
      if (Array.isArray(entities)) {
        entities.forEach((e: any) => {
          if (e?.Id != null) dictEnt[e.Id] = e;
        });
      }
      setDictEntityIdAndDto(dictEnt);

      const rawField = await appTransactionService.retrieveOneAppTransactionFieldExDto(
        transactionFieldId,
        layoutItemId ?? ''
      );
      const orgCopy = JSON.parse(JSON.stringify(rawField));
      setOrgTransField(orgCopy);

      const field = { ...rawField };
      field.FieldChangeSetting = {
        OrgControlType: orgCopy.ControlType,
        OrgEntityId: orgCopy.EntityId || null,
      };

      const td = buildTransactionEditorState(hierarchy);
      td.dictTransactionFieldIdAndDto[field.Id] = field;
      replaceFieldInHierarchy(hierarchy, field);

      if (field.StyleLayoutInfo && !field.DomAttribute) {
        setGridFormFieldStyleObj(getPropertiesFromStyleString(field.StyleLayoutInfo));
      } else {
        setGridFormFieldStyleObj({ width: 330, height: 30 });
      }

      setTransactionData(td);
      setCurrentTransField(field);
    } catch (e: any) {
      showError(e?.message || 'Failed to load field settings.');
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [transactionId, transactionFieldId, layoutItemId, dispatch, showError]);

  useEffect(() => {
    if (!isOpen) {
      setCurrentTransField(null);
      setOrgTransField(null);
      setTransactionData(null);
      return;
    }
    load();
  }, [isOpen, load]);

  const patchField = useCallback(
    (next: any) => {
      setCurrentTransField(next);
      if (transactionData?.dictTransactionFieldIdAndDto && next?.Id) {
        transactionData.dictTransactionFieldIdAndDto[next.Id] = next;
      }
      if (transactionData?.AppTransactionData && next?.Id) {
        replaceFieldInHierarchy(transactionData.AppTransactionData, next);
      }
      setTransactionData(transactionData ? { ...transactionData } : null);
    },
    [transactionData]
  );

  const isGridFormLayout = (field: any) => !!(field && !field.DomAttribute && field.StyleLayoutInfo);

  const handleSave = async () => {
    if (!currentTransField || !transactionData) return;
    try {
      dispatch(setIsBusy());
      const field = { ...currentTransField };
      field.TransactionId = transactionId;

      if (isGridFormLayout(field)) {
        if (!isGridField) {
          const widthValue = gridFormFieldStyleObj.width || 330;
          const heightValue = gridFormFieldStyleObj.height || 30;
          field.StyleLayoutInfo = getStyleString(widthValue, heightValue);
        }
      } else if (field.DomAttribute && emControl) {
        const ct = field.ControlType;
        const Em = emControl as Record<string, number>;
        const tallControls = [
          Em.Memo,
          Em.Image,
          Em.ExternalImageUrl,
          Em.Video,
          Em.SearchAndView,
          Em.GoogleMap,
        ].filter((x) => typeof x === 'number');
        if (tallControls.includes(ct) && !field.DomAttribute.HeightValue) {
          field.DomAttribute = { ...field.DomAttribute, HeightValue: 200 };
        }
      }

      const saveResult = await appTransactionService.saveAppTransactionFieldExDto(field);
      if (saveResult?.ValidationResult) {
        showValidationMessages(saveResult.ValidationResult, true);
      }
      if (!saveResult?.IsSuccessful) {
        throw new Error(saveResult?.Message || 'Save field failed.');
      }

      const unitId = field.TransactionUnitId;
      const formulaSet =
        unitId != null ? transactionData?.AppTransactionData?.DictUnitldIdAndFormulaSetDto?.[unitId] : null;

      if (formulaSet?.IsModified) {
        const fr = await appTransactionService.saveAppTransactionUnitFormulaSetDto(formulaSet);
        if (fr?.ValidationResult) {
          showValidationMessages(fr.ValidationResult, true);
        }
        if (!fr?.IsSuccessful) {
          throw new Error('Failed to save formula set.');
        }
      }

      showInfo('Field settings saved.');
      onSaved?.();
      onClose();
    } catch (e: any) {
      showError(e?.message || 'Save failed.');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const isAllowRemap = (): boolean => {
    const fs = currentTransField?.FieldChangeSetting;
    if (!fs) return false;
    if (fs.IsChagneFromDDLToOtherType) return true;
    if (fs.IsChangeFromOtherTypeToDDL && fs.NewEntityId) return true;
    return false;
  };

  const updatePopupPosition = useCallback(() => {
    if (!anchorRect || !popupRef.current) return;
    const r = popupRef.current.getBoundingClientRect();
    setPopupPos(positionPopupNearAnchor(anchorRect, r.width, r.height));
  }, [anchorRect]);

  useLayoutEffect(() => {
    if (!isOpen) return;
    setZIndex(appHelper.getNextPopupZIndex());
  }, [isOpen]);

  useLayoutEffect(() => {
    updatePopupPosition();
  }, [updatePopupPosition, loading, currentTransField, transactionData, anchorRect]);

  useEffect(() => {
    if (!isOpen || !anchorRect) return;
    const el = popupRef.current;
    if (!el || typeof ResizeObserver === 'undefined') return;
    const ro = new ResizeObserver(() => updatePopupPosition());
    ro.observe(el);
    return () => ro.disconnect();
  }, [isOpen, anchorRect, updatePopupPosition]);

  useEffect(() => {
    if (!isOpen) return;
    const onScrollOrResize = () => updatePopupPosition();
    window.addEventListener('scroll', onScrollOrResize, true);
    window.addEventListener('resize', onScrollOrResize);
    return () => {
      window.removeEventListener('scroll', onScrollOrResize, true);
      window.removeEventListener('resize', onScrollOrResize);
    };
  }, [isOpen, updatePopupPosition]);

  if (!isOpen || typeof document === 'undefined') return null;

  const titleName = currentTransField?.DisplayName || `Field ${transactionFieldId}`;

  return createPortal(
    <div
      ref={popupRef}
      className={`fixed flex max-h-[min(90vh,calc(100vh-16px))] w-[min(28rem,calc(100vw-16px))] flex-col overflow-hidden rounded-md border shadow-xl ${theme.mainContentSection}`}
      style={{ top: popupPos.top, left: popupPos.left, zIndex }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="runtime-field-setting-title"
      onMouseDown={(e) => e.stopPropagation()}
    >
      <div className={`flex shrink-0 items-center justify-between border-b px-3 py-2 ${theme.mainContentSection}`}>
        <div id="runtime-field-setting-title" className={`text-sm font-semibold truncate pr-2 ${theme.title}`}>
          Field Setting: {titleName}
        </div>
        <button
          type="button"
          className={`rounded-[4px] p-1.5 shrink-0 ${theme.button_default}`}
          onClick={onClose}
          title="Close"
          aria-label="Close"
        >
          <i className="fa-solid fa-xmark" aria-hidden />
        </button>
      </div>
      <div className="min-h-0 w-full flex-1 overflow-y-auto overscroll-contain px-2 py-2">
        {loading && <div className={`text-sm p-4 ${theme.label}`}>Loading…</div>}
        {!loading && currentTransField && transactionData && (
          <TransactionFieldEditorPanel
            isGridColumn={!!isGridField}
            currentTransField={currentTransField}
            onChangeField={(next) => {
              patchField(next);
            }}
            transactionData={transactionData}
            dictEntityIdAndDto={dictEntityIdAndDto}
            orgTransField={orgTransField}
            gridFormFieldStyleObj={gridFormFieldStyleObj}
            setGridFormFieldStyleObj={setGridFormFieldStyleObj}
          />
        )}
      </div>
      <div className={`shrink-0 border-t px-3 py-2 ${theme.mainContentSection}`}>
        {isAllowRemap() && (
          <button
            type="button"
            className={`mb-2 w-full px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() =>
              showInfo('Complete column mapping in Application Form Builder (Design Layout) if advanced remap is required.')
            }
          >
            Re-map Entity Column
          </button>
        )}
        <div className="flex items-center justify-between gap-3">
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={handleSave}
            disabled={loading || !currentTransField}
          >
            Save
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
        </div>
      </div>
    </div>,
    document.body
  );
};

export default RuntimeFieldSettingDrawer;
