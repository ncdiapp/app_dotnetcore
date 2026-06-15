import { useMemo } from 'react';
import { useSelector } from 'react-redux';
import { RootState } from '../redux/store';
import { EnumDictionary } from '../redux/features/admin/userSessionSlice';

export const useEnumDictionary = (): EnumDictionary | null =>
  useSelector((state: RootState) => state.userSession.enumDictionary);

export const useEnumValues = (enumName: string): Record<string, number> | null => {
  const dictionary = useEnumDictionary();

  return useMemo(() => {
    if (!dictionary || !enumName) {
      return null;
    }
    return dictionary[enumName] ?? null;
  }, [dictionary, enumName]);
};

export const useEnumEntry = (enumName: string, key: string): number | undefined => {
  const values = useEnumValues(enumName);

  return useMemo(() => {
    if (!values || !key) {
      return undefined;
    }
    return values[key];
  }, [values, key]);
};

