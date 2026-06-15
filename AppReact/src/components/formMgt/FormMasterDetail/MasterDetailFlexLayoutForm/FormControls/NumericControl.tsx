import React from 'react';
import { useTheme } from '../../../../../redux/hooks/useTheme';
import { getOneToOneFieldValue, buildFormDataWithOneToOneValue } from './formDataBindingHelper';

interface NumericControlProps {
  layoutItemExDto: any;
  fieldDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
}

const NumericControl: React.FC<NumericControlProps> = ({
  layoutItemExDto,
  fieldDto,
  controllerModel,
  dataModel,
  onDataModelChange
}) => {
  const { theme, t } = useTheme();

  const fieldName = fieldDto.DataBaseFieldName;
  const fieldValue = getOneToOneFieldValue(dataModel.currentFormData, fieldDto, fieldName, '', layoutItemExDto) as string;

  // Angular source uses `aAppTransactionFieldExDto.Nbdecimal` to drive display format: `format="n{Nbdecimal}"`.
  const decimalDigitsRaw =
    fieldDto?.Nbdecimal ??
    fieldDto?.NumberOfDecimal ??
    fieldDto?.Scale ??
    fieldDto?.DecimalDigits ??
    fieldDto?.Decimals ??
    fieldDto?.Digit ??
    null;
  const decimalDigits =
    typeof decimalDigitsRaw === 'number'
      ? decimalDigitsRaw
      : typeof decimalDigitsRaw === 'string'
        ? Number(decimalDigitsRaw)
        : NaN;
  const resolvedDecimalDigits =
    Number.isFinite(decimalDigits) && decimalDigits >= 0 ? Math.min(10, Math.floor(decimalDigits)) : fieldDto?.IsDecimal ? 2 : 0;

  /** Display string for a committed value (empty / invalid → zero) — avoids sending '' to SQL numeric columns. */
  const formatNumberString = React.useCallback(
    (value: string) => {
      if (value == null) return (0).toFixed(resolvedDecimalDigits);
      const trimmed = String(value).trim();
      if (!trimmed || trimmed === '-' || trimmed === '.' || trimmed === '-.') {
        return (0).toFixed(resolvedDecimalDigits);
      }
      const n = Number(trimmed);
      if (!Number.isFinite(n)) return (0).toFixed(resolvedDecimalDigits);
      return n.toFixed(resolvedDecimalDigits);
    },
    [resolvedDecimalDigits]
  );

  const parseToModelNumber = React.useCallback(
    (value: string): number => {
      const formatted = formatNumberString(value);
      const n = parseFloat(formatted);
      return Number.isFinite(n) ? n : 0;
    },
    [formatNumberString]
  );

  const [isFocused, setIsFocused] = React.useState(false);
  const [displayValue, setDisplayValue] = React.useState<string>(() => formatNumberString(fieldValue));

  React.useEffect(() => {
    if (isFocused) return;
    setDisplayValue(formatNumberString(fieldValue));
  }, [fieldValue, formatNumberString, isFocused]);
  
  // Check if field is read-only
  const isReadOnly = fieldDto.IsFormLayoutReadOnly === true || 
                    dataModel.currentFormData?.IsLockTransaction === true;
  
  // Get required mark (UI only)
  const isRequired = fieldDto.IsAllowEmpty === false;
  const requiredMark = isRequired ? <span className="text-red-500">*</span> : null;
  
  // Get tooltip
  const tooltip = fieldDto.ToolTip || fieldDto.LabelDisplayBinding || '';
  
  // Get label
  const label = fieldDto.DisplayName || fieldDto.LabelDisplayBinding || fieldName;

  // UI required validation message (set by FormMainMenus on Save)
  const rootUnitId = dataModel.currentFormData?.RootUnitId ?? null;
  const errorKey =
    rootUnitId != null && fieldDto?.TransactionUnitId != null && String(fieldDto.TransactionUnitId) !== String(rootUnitId)
      ? `${String(fieldDto.TransactionUnitId)}.${String(fieldName)}`
      : String(fieldName);
  const errorText = dataModel?.uiValidationErrors?.[errorKey] as string | undefined;
  
  // While focused, allow empty display; on blur we commit 0 via formatNumberString (avoids empty in SQL).
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    setDisplayValue(newValue);
    if (newValue.trim() === '') {
      return;
    }
    onDataModelChange({
      ...dataModel,
      uiValidationErrors:
        errorText && dataModel?.uiValidationErrors
          ? (() => {
              const copy = { ...(dataModel.uiValidationErrors ?? {}) };
              delete copy[errorKey];
              return copy;
            })()
          : dataModel?.uiValidationErrors,
      currentFormData: buildFormDataWithOneToOneValue(dataModel.currentFormData, fieldDto, fieldName, newValue, layoutItemExDto)
    });
  };

  // Get validation class (TODO: implement validation)
  const validationClass = ''; // TODO: Add validation CSS class

  // Parse style string to object
  const styleObject: React.CSSProperties = {};
  if (layoutItemExDto.StyleLayoutInfo) {
    const styles = layoutItemExDto.StyleLayoutInfo.split(';').filter((s: string) => s.trim());
    styles.forEach((style: string) => {
      const [key, value] = style.split(':').map((s: string) => s.trim());
      if (key && value) {
        const camelKey = key.replace(/-([a-z])/g, (g: string) => g[1].toUpperCase());
        (styleObject as any)[camelKey] = value;
      }
    });
  }

  const isHideLabel = controllerModel?.isFilePropertyEdit === true;

  return (
    <div 
      className={`w-full flex items-start gap-2 ${validationClass}`}
      style={styleObject}
      title={tooltip}
    >
      {!isHideLabel && (
        <div className="flex-shrink-0 min-w-[120px]">
          <label className={`text-xs font-semibold ${theme.title}`}>
            {label} {requiredMark}
          </label>
        </div>
      )}
      <div className="w-1 flex-auto">
        <input
          type="text"
          inputMode={fieldDto.IsDecimal ? 'decimal' : 'numeric'}
          value={displayValue}
          onChange={handleChange}
          onFocus={() => {
            setIsFocused(true);
            setDisplayValue(fieldValue ?? '');
          }}
          onBlur={() => {
            setIsFocused(false);
            const formatted = formatNumberString(displayValue);
            const modelNum = parseToModelNumber(displayValue);
            setDisplayValue(formatted);
            onDataModelChange({
              ...dataModel,
              uiValidationErrors:
                errorText && dataModel?.uiValidationErrors
                  ? (() => {
                      const copy = { ...(dataModel.uiValidationErrors ?? {}) };
                      delete copy[errorKey];
                      return copy;
                    })()
                  : dataModel?.uiValidationErrors,
              currentFormData: buildFormDataWithOneToOneValue(dataModel.currentFormData, fieldDto, fieldName, modelNum, layoutItemExDto)
            });
          }}
          disabled={isReadOnly}
          className={`w-full h-[30px] px-2 py-1 text-sm border rounded ${theme.inputBox} ${
            isReadOnly ? `${t('bg_input_readonly')} cursor-not-allowed` : ''
          } ${errorText ? 'border-red-500' : ''}`}
        />
        {errorText && <div className="text-xs text-red-600 mt-0.5">{errorText}</div>}
      </div>
    </div>
  );
};

export default NumericControl;

