import React, { useEffect, useMemo, useState } from 'react';
import { CollectionView, PropertyGroupDescription, SortDescription } from '@mescius/wijmo';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { OpenApiSelectableApi } from '../../webapi/openApiDocumentSvc';

type Props = {
  apis: OpenApiSelectableApi[];
  onApply: (actionCodes: string[]) => void;
  onClose: () => void;
};

const OpenApiAddApisDialog: React.FC<Props> = ({ apis, onApply, onClose }) => {
  const { theme } = useTheme();
  const [tab, setTab] = useState<'app' | 'thirdParty'>('app');

  const appView = useMemo(() => {
    const rows = apis.filter((a) => a.ProviderKind === 'app').map((a) => ({ ...a, isSelected: false }));
    const cv = new CollectionView(rows);
    cv.sortDescriptions.push(new SortDescription('ActionCode', true));
    return cv;
  }, [apis]);

  const thirdView = useMemo(() => {
    const rows = apis.filter((a) => a.ProviderKind !== 'app').map((a) => ({ ...a, isSelected: false }));
    const cv = new CollectionView(rows);
    cv.sortDescriptions.push(new SortDescription('ActionCode', true));
    cv.groupDescriptions.push(new PropertyGroupDescription('ProviderName'));
    return cv;
  }, [apis]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);

  const apply = () => {
    const source = tab === 'app' ? appView : thirdView;
    const codes = source.items
      .filter((row: any) => row.isSelected && row.ActionCode)
      .map((row: any) => row.ActionCode as string);
    onApply(codes);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onMouseDown={onClose}>
      <div className={`w-[960px] h-[640px] flex flex-col rounded-[4px] border ${theme.mainContentSection}`} onMouseDown={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between px-3 py-2 border-b">
          <div className={`text-sm font-semibold ${theme.title}`}>Add APIs</div>
          <button type="button" className={`px-2 py-1 text-xs ${theme.button_default}`} onClick={onClose}>Close</button>
        </div>
        <div className="flex items-center gap-2 px-3 py-2">
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${tab === 'app' ? theme.button_default : theme.button_default}`} onClick={() => setTab('app')}>App API Provider</button>
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={() => setTab('thirdParty')}>3rd Party Provider</button>
        </div>
        <div className="h-1 flex-auto px-3 pb-2 overflow-hidden">
          {tab === 'app' ? (
            <FlexGrid itemsSource={appView} autoGenerateColumns={false} className="w-full h-full">
              <FlexGridColumn binding="isSelected" header="" width={36} dataType="Boolean" />
              <FlexGridColumn binding="Id" header="Id" width={70} isReadOnly />
              <FlexGridColumn binding="ActionCode" header="API Code" width={220} isReadOnly />
              <FlexGridColumn binding="Description" header="Description" width={180} isReadOnly />
              <FlexGridColumn binding="ApiType" header="API Type" width={160} isReadOnly />
              <FlexGridColumn binding="HttpMethod" header="Http Method" width={110} isReadOnly />
              <FlexGridColumn binding="DataSourceName" header="Data Source" width={160} isReadOnly />
              <FlexGridColumn binding="DataModelName" header="Data Model" width={160} isReadOnly />
              <FlexGridColumn binding="Application" header="Application" width={140} isReadOnly />
              <FlexGridColumn header="" binding="" width="*" isReadOnly />
            </FlexGrid>
          ) : (
            <FlexGrid itemsSource={thirdView} autoGenerateColumns={false} showGroups className="w-full h-full">
              <FlexGridColumn binding="isSelected" header="" width={36} dataType="Boolean" />
              <FlexGridColumn binding="ProviderName" header="Provider" width={160} isReadOnly />
              <FlexGridColumn binding="Id" header="ID" width={70} isReadOnly />
              <FlexGridColumn binding="HttpMethod" header="Method" width={90} isReadOnly />
              <FlexGridColumn binding="ActionCode" header="Operation Code" width={220} isReadOnly />
              <FlexGridColumn binding="Description" header="Description" width={220} isReadOnly />
              <FlexGridColumn binding="DataSourceName" header="Data Source" width={160} isReadOnly />
              <FlexGridColumn header="" binding="" width="*" isReadOnly />
            </FlexGrid>
          )}
        </div>
        <div className="flex justify-end gap-2 px-3 py-2 border-t">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>Cancel</button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={apply}>Apply</button>
        </div>
      </div>
    </div>
  );
};

export default OpenApiAddApisDialog;
