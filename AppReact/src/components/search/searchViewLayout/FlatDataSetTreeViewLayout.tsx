import React, { useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';

interface FlatDataSetTreeViewLayoutProps {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  onSelectionChanged?: (selectedItems: any[]) => void;
}

const NAME_BINDING = 'BaseAppCatalogueTreeDto.Name';

export const FlatDataSetTreeViewLayout: React.FC<FlatDataSetTreeViewLayoutProps> = ({
  viewDto: _viewDto,
  viewDataList,
  dataModel,
  onExecuteSearch: _onExecuteSearch,
  onSelectionChanged,
}) => {
  const { theme } = useTheme();
  const flexRef = useRef<any>(null);
  const clickHandlerRef = useRef<null | ((e: MouseEvent) => void)>(null);
  const [treeCv] = useState<CollectionView<any>>(() => new CollectionView<any>([]));

  useEffect(() => {
    const src = Array.isArray(viewDataList) ? viewDataList : [];
    treeCv.sourceCollection = src;
    treeCv.refresh();
  }, [viewDataList, treeCv]);

  const isLinkedSearchMode = Boolean(dataModel?.uiControl?.isLinkedSearch);
  const onFlatHide = dataModel?.uiControl?.flatDataSetTreeOnHide as (() => void) | undefined;

  const emitSelectionFromGrid = (flex: any) => {
    if (!flex || !onSelectionChanged) return;
    const row = flex.selection?.row ?? -1;
    if (row < 0) return;
    const item = flex.rows?.[row]?.dataItem;
    if (item != null) {
      onSelectionChanged([item]);
    }
  };

  const attachClickListener = (flex: any) => {
    if (!flex || !onSelectionChanged) return;
    // Attach to main host for maximum reliability (Wijmo may replace cells host).
    const host: HTMLElement | undefined = flex?.hostElement ?? flex?.cells?.hostElement;
    if (!host || !flex?.hitTest) return;

    // Remove previous handler if any (re-init / rerender safety)
    if (clickHandlerRef.current) {
      host.removeEventListener('click', clickHandlerRef.current, true);
      clickHandlerRef.current = null;
    }

    const onClick = (e: MouseEvent) => {
      try {
        const ht = flex.hitTest(e);
        const rowIndex = ht?.row ?? -1;
        // Accept both Cell and RowHeader clicks (users often click the indent area).
        const cellType = ht?.cellType;
        const isDataArea = cellType === 1 /* Cell */ || cellType === 3 /* RowHeader */;
        if (rowIndex < 0 || !isDataArea) return;
        const item = flex.rows?.[rowIndex]?.dataItem;
        if (item == null) return;
        // Keep grid selection in sync so keyboard navigation still works.
        if (flex?.select) flex.select(rowIndex, 0);
        onSelectionChanged([item]);
      } catch {
        // ignore click handler errors
      }
    };

    clickHandlerRef.current = onClick;
    // Capture phase avoids cases where Wijmo stops propagation.
    host.addEventListener('click', onClick, true);
  };

  // Cleanup listener on unmount.
  useEffect(() => {
    return () => {
      const flex = flexRef.current?.control ?? flexRef.current;
      const host: HTMLElement | undefined = flex?.hostElement ?? flex?.cells?.hostElement;
      if (host && clickHandlerRef.current) {
        host.removeEventListener('click', clickHandlerRef.current, true);
      }
      clickHandlerRef.current = null;
    };
  }, []);

  return (
    <div className="w-full h-full flex flex-col relative">
      <div className={`h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          ref={flexRef}
          className="w-full h-full CategoryTree"
          itemsSource={treeCv}
          childItemsPath="Children"
          treeIndent={30}
          headersVisibility="Column"
          selectionMode="Cell"
          isReadOnly
          allowDelete={false}
          initialized={(s: any) => {
            const flex = s?.control ?? s;
            // Defer to next tick to ensure panels/host are ready.
            window.setTimeout(() => attachClickListener(flex), 0);
          }}
          selectionChanged={(s: any) => {
            // Keep old behavior for linked-search popups / keyboard selection.
            if (!isLinkedSearchMode) return;
            const flex = s?.control ?? s;
            emitSelectionFromGrid(flex);
          }}
          style={{ width: '100%', height: '100%', border: 'none', fontSize: 12 }}
        >
          <FlexGridFilter filterColumns={[NAME_BINDING]} />
          <FlexGridColumn binding={NAME_BINDING} header="Name" width={300} isReadOnly />
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>
      {typeof onFlatHide === 'function' && (
        <div className="absolute top-0 right-0 py-0.5 px-2.5 z-10">
          <button
            type="button"
            className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
            onClick={() => onFlatHide()}
            title="Hide"
          >
            <i className="fa-solid fa-arrow-left" aria-hidden="true" /> Hide
          </button>
        </div>
      )}
    </div>
  );
};
