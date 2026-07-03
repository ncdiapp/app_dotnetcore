import React, { useCallback, useEffect, useState, useRef, useMemo } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { DataMap } from '@mescius/wijmo.grid';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { appFolderNavigationService } from '../../webapi/appfoldernavigationsvc';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import type { Theme } from '../../redux/features/ui/theme/types';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch, useSelector } from 'react-redux';
import appHelper from '../../helper/appHelper';
import { isAdminUserFromContext } from '../../helper/adminPermissionHelper';
import FileNavigationSharingEditor from './FileNavigationSharingEditor';
import FolderSecurityEditor from './FolderSecurityEditor';
import FormMasterDetail from '../formMgt/FormMasterDetail';
import FileUploader from '../common/FileUploader';
import { searchSvc } from '../../webapi/searchSvc';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';
import { fileLatestUrl as buildLatestFileUrl, downloadFileById } from '../../webapi/fileEndpoints';
import { FolderFileGrid, type FileViewItem } from './FolderFileGrid';

// Fallback when enum not yet loaded (must match backend EmAppFileFolderCategory: MyRecycleBin = 9)
const FALLBACK_FILE_FOLDER_CATEGORIES: Record<string, number> = {
  MyRecentlyFiles: 1,
  Favorites: 2,
  Company: 3,
  Private: 4,
  Public: 5,
  ShareToOthers: 6,
  SharedToMe: 7,
  MyRecycleBin: 9,
};

const CATEGORY_DISPLAY_KEYS: (keyof typeof FALLBACK_FILE_FOLDER_CATEGORIES)[] = [
  'MyRecentlyFiles', 'Favorites', 'Company', 'Private', 'Public', 'ShareToOthers', 'SharedToMe', 'MyRecycleBin',
];
const CATEGORY_DISPLAY_META: Record<string, { display: string; icon: string }> = {
  MyRecentlyFiles: { display: 'My Recently Files', icon: 'fa-pen-to-square' },
  Favorites: { display: 'My Favorites', icon: 'fa-star' },
  Company: { display: 'My Company', icon: 'fa-folder' },
  Private: { display: 'My Private', icon: 'fa-briefcase' },
  Public: { display: 'Public', icon: 'fa-globe' },
  ShareToOthers: { display: 'Share To Others', icon: 'fa-share-nodes' },
  SharedToMe: { display: 'Share To Me Folder', icon: 'fa-user' },
  MyRecycleBin: { display: 'My Recycle Bin', icon: 'fa-trash' },
};

// Order for left vertical menu (Angular: no Public, add Dropbox at end)
const FILE_MGT_CATEGORY_ORDER_KEYS: (keyof typeof FALLBACK_FILE_FOLDER_CATEGORIES)[] = [
  'MyRecentlyFiles', 'Favorites', 'ShareToOthers', 'SharedToMe', 'MyRecycleBin', 'Company', 'Private',
];

// Search view column control type: Image = 5 (same as GridViewLayout / Angular EmAppControlType.Image)

const FOLDER_CONTEXT_MENU_ESTIMATED_WIDTH = 200;
const FOLDER_CONTEXT_MENU_ESTIMATED_HEIGHT = 360;
const FILE_CONTEXT_MENU_ESTIMATED_WIDTH = 200;
const FILE_CONTEXT_MENU_ESTIMATED_HEIGHT = 400;

// Preview type by file extension (matches Angular docHelper.getFilePreviewType)
const IMAGE_EXT = ['jpg', 'jpeg', 'png', 'gif', 'bmp'];
const DOC_EXT_FOR_GOOGLE_GVIEW = [
  'xls', 'xlsx', 'doc', 'docx', 'pdf', 'txt', 'ppt', 'pptx',
  'ai', 'psd', 'tiff', 'tif', 'svg', 'dwg', 'eps', 'ttf', 'xps', 'zip', 'rar',
];
const VIDEO_EXT = ['mp4', 'ogg'];
const AUDIO_EXT = ['mp3', 'wav'];

/** Infer TransRootIdColumnId from view fields when API does not return it (e.g. AppSearchViewExDto vs ReferenceViewDto) */
function inferTransRootIdColumnId(fields: { Id?: number; SearchViewFieldID?: number; Name?: string; DisplayText?: string; SysTableFiledPath?: string }[]): number | null {
  const idNames = ['id', 'transrootid', 'fileid', 'transrootidcolumnid'];
  for (const f of fields) {
    const name = (f.Name ?? f.DisplayText ?? f.SysTableFiledPath ?? '').toLowerCase();
    if (idNames.some((n) => name === n || name.endsWith('.' + n))) {
      const id = f.Id ?? f.SearchViewFieldID;
      if (id != null) return Number(id);
    }
  }
  return null;
}

/** Infer FileCodeColumnId from view fields when API does not return it */
function inferFileCodeColumnId(fields: { Id?: number; SearchViewFieldID?: number; Name?: string; DisplayText?: string; SysTableFiledPath?: string }[]): number | null {
  const codeNames = ['filecode', 'filename', 'display', 'name'];
  for (const f of fields) {
    const name = (f.Name ?? f.DisplayText ?? f.SysTableFiledPath ?? '').toLowerCase();
    if (codeNames.some((n) => name === n || name.endsWith('.' + n))) {
      const id = f.Id ?? f.SearchViewFieldID;
      if (id != null) return Number(id);
    }
  }
  return null;
}

/** Infer the Extension column id (FileCode has no extension, so preview type comes from here) */
function inferExtensionColumnId(fields: { Id?: number; SearchViewFieldID?: number; Name?: string; DisplayText?: string; SysTableFiledPath?: string }[]): number | null {
  for (const f of fields) {
    const name = (f.SysTableFiledPath ?? f.Name ?? f.DisplayText ?? '').toLowerCase();
    if (name === 'extension' || name.endsWith('.extension')) {
      const id = f.Id ?? f.SearchViewFieldID;
      if (id != null) return Number(id);
    }
  }
  return null;
}

function getFilePreviewType(fileName: string): 'imagePreView' | 'googleGview' | 'videoPreView' | 'audioPreView' | null {
  if (!fileName) return null;
  const ext = fileName.split('.').pop()?.toLowerCase() ?? '';
  if (IMAGE_EXT.includes(ext)) return 'imagePreView';
  if (DOC_EXT_FOR_GOOGLE_GVIEW.includes(ext)) return 'googleGview';
  if (VIDEO_EXT.includes(ext)) return 'videoPreView';
  if (AUDIO_EXT.includes(ext)) return 'audioPreView';
  return null;
}

/** Preview panel: images via img, documents via Google Docs viewer iframe (Angular pattern) */
const FilePreviewPanel = React.memo(function FilePreviewPanel({
  selectedFile,
  getFileIdFromItem,
  getFileDisplayFromItem,
  getFileExtensionFromItem,
  theme,
}: {
  selectedFile: FileViewItem | null;
  getFileIdFromItem: (item: FileViewItem | null) => number | null;
  getFileDisplayFromItem: (item: FileViewItem | null) => string;
  getFileExtensionFromItem: (item: FileViewItem | null) => string;
  theme: Theme;
}) {
  const fileId = selectedFile ? getFileIdFromItem(selectedFile) : null;
  const fileName = selectedFile ? getFileDisplayFromItem(selectedFile) : '';
  // FileCode has no extension; the view exposes it in a separate Extension column.
  // Combine so preview type can be resolved (e.g. "151380_TECHNI" + ".png").
  const extension = selectedFile ? getFileExtensionFromItem(selectedFile) : '';
  const effectiveName = /\.[a-z0-9]+$/i.test(fileName)
    ? fileName
    : fileName + (extension ? (extension.startsWith('.') ? extension : `.${extension}`) : '');
  const previewType = effectiveName ? getFilePreviewType(effectiveName) : null;
  const getFileUrl = (id: number) => buildLatestFileUrl(id);

  if (!selectedFile) {
    return <p className={`text-xs ${theme.label}`}>Select a file to preview.</p>;
  }
  if (!fileId) {
    return <p className={`text-xs ${theme.label}`}>Preview Not Available</p>;
  }
  if (previewType === 'imagePreView') {
    const imgUrl = getFileUrl(fileId);
    return (
      <div className="relative flex items-center justify-center w-full min-h-[200px]">
        <img src={imgUrl} alt="" className="max-h-full max-w-full object-contain" />
        <button
          type="button"
          onClick={() => downloadFileById(fileId, effectiveName)}
          className={`absolute top-2 right-2 px-2 py-1 text-xs rounded ${theme.button_default}`}
        >
          <i className="fa-solid fa-download mr-1" />
          Download
        </button>
      </div>
    );
  }
  if (previewType === 'googleGview') {
    const fileUrl = encodeURIComponent(getFileUrl(fileId));
    const iframeUrl = `https://docs.google.com/gview?embedded=true&url=${fileUrl}`;
    return (
      <div className="relative w-full h-full min-h-[300px]">
        <iframe src={iframeUrl} title="Document preview" className="w-full h-full min-h-[300px] border-0" />
        <button
          type="button"
          onClick={() => downloadFileById(fileId, effectiveName)}
          className={`absolute top-2 right-2 px-2 py-1 text-xs rounded ${theme.button_default}`}
        >
          <i className="fa-solid fa-download mr-1" />
          Download
        </button>
      </div>
    );
  }
  if (previewType === 'videoPreView' || previewType === 'audioPreView') {
    const mediaUrl = getFileUrl(fileId);
    return (
      <div className="w-full">
        {previewType === 'videoPreView' ? (
          <video src={mediaUrl} controls className="w-full max-h-[400px]" />
        ) : (
          <audio src={mediaUrl} controls className="w-full" />
        )}
        <button type="button" onClick={() => downloadFileById(fileId, effectiveName)} className={`mt-2 inline-block px-2 py-1 text-xs rounded ${theme.button_default}`}>
          <i className="fa-solid fa-download mr-1" />
          Download
        </button>
      </div>
    );
  }
  return <p className={`text-xs ${theme.label}`}>Preview Not Available</p>;
});

/** Properties panel: embeds FormMasterDetail (file transaction form) like Angular's openFilePropertyPage */
function FilePropertiesFormPanel({
  selectedFile,
  transactionId,
  getFileIdFromItem,
  theme,
}: {
  selectedFile: FileViewItem | null;
  transactionId: string | number | null;
  getFileIdFromItem: (item: FileViewItem | null) => number | null;
  theme: Theme;
}) {
  const fileId = selectedFile ? getFileIdFromItem(selectedFile) : null;
  const transId = transactionId != null ? Number(transactionId) : null;

  if (!selectedFile) {
    return <p className={`text-xs ${theme.label} p-3`}>Select a file to view properties.</p>;
  }
  if (!fileId || !transId) {
    return <p className={`text-xs ${theme.label} p-3`}>Unable to load file properties.</p>;
  }

  return (
    <div className="w-full h-full min-h-0 flex flex-col overflow-hidden -m-3">
      <FormMasterDetail
        key={`file-property-${fileId}`}
        embedded={{
          embeddedTransactionId: transId,
          embeddedRootPrimaryKeyValue: fileId,
          embeddedParam2: {
            isFilePropertyEdit: true,
            isEmbeddedByOtherPage: true,
            isHideHeaderAndFooter: true,
          },
        }}
      />
    </div>
  );
}

type Props = {
  transactionId: string | number | null;
  defaultCategoryId?: number | null;
  initialFolderId?: string | number | null;
  isFileMgt?: boolean;
  isUseAsSelector?: boolean;
  onFileSelected?: (fileId: string | number) => void;
};

type FolderDto = {
  Id?: number;
  Name: string;
  ParentId?: number | null;
  FolderPath?: string;
  HasChildren?: boolean;
  DefaultViewId?: number | null;
  Level?: number;
  IsFolderReadonly?: boolean;
  Children?: FolderDto[];
  children?: FolderDto[]; // fallback if backend returns camelCase
  FolderType?: number;
  TransactionId?: number;
  CountContent?: number;
};

const FolderNavigation: React.FC<Props> = ({
  transactionId,
  defaultCategoryId,
  initialFolderId,
  isFileMgt = true,
  isUseAsSelector = false,
  onFileSelected,
}) => {
  const dispatch = useDispatch();
  const userContext = useSelector((state: any) => state?.userSession?.userContext);
  const isSysAdmin = useMemo(() => isAdminUserFromContext(userContext), [userContext]);
  const { theme } = useTheme();
  const { showInfo } = useErrorMessage();
  const fileFolderCategoryEnum = useEnumValues('EmAppFileFolderCategory');
  const FILE_FOLDER_CATEGORIES = useMemo(
    () => fileFolderCategoryEnum ?? FALLBACK_FILE_FOLDER_CATEGORIES,
    [fileFolderCategoryEnum]
  );
  const CATEGORY_DISPLAY = useMemo(() => {
    const d: Record<number, { display: string; icon: string }> = {};
    CATEGORY_DISPLAY_KEYS.forEach((k) => {
      const v = FILE_FOLDER_CATEGORIES[k];
      if (v != null) d[v] = CATEGORY_DISPLAY_META[k];
    });
    return d;
  }, [FILE_FOLDER_CATEGORIES]);
  const FILE_MGT_CATEGORY_ORDER = useMemo(
    () => FILE_MGT_CATEGORY_ORDER_KEYS.map((k) => FILE_FOLDER_CATEGORIES[k]).filter((n): n is number => n != null),
    [FILE_FOLDER_CATEGORIES]
  );

  // UI State
  const [isShowFolderTree, setIsShowFolderTree] = useState(true);
  const [folderTreeWidth, setFolderTreeWidth] = useState(300);
  const [isResizing, setIsResizing] = useState(false);
  const resizeStartX = useRef(0);
  const resizeStartWidth = useRef(0);

  const [previewPanelWidth, setPreviewPanelWidth] = useState(280);
  const [isPreviewResizing, setIsPreviewResizing] = useState(false);
  const previewResizeStartX = useRef(0);
  const previewResizeStartWidth = useRef(0);

  // Data State
  const [folders, setFolders] = useState<FolderDto[]>([]);
  const [foldersCV, setFoldersCV] = useState<CollectionView<FolderDto> | null>(null);
  const [currentFolderId, setCurrentFolderId] = useState<number | null>(null);
  const [currentFolderData, setCurrentFolderData] = useState<FolderDto | null>(null);
  // Angular default: MyRecentlyFiles
  const [currentCategory, setCurrentCategory] = useState<number>(
    defaultCategoryId ?? FALLBACK_FILE_FOLDER_CATEGORIES.MyRecentlyFiles
  );

  const [files, setFiles] = useState<FileViewItem[]>([]);
  const [filesCV, setFilesCV] = useState<CollectionView<FileViewItem> | null>(null);
  const [selectedFiles, setSelectedFiles] = useState<FileViewItem[]>([]);

  const [userDataMap, setUserDataMap] = useState<any>(null);
  const [searchText, setSearchText] = useState('');
  const [isNeedSearchSubFolders, setIsNeedSearchSubFolders] = useState(true);
  const [rangeOption, setRangeOption] = useState<string>('AllFolders');
  const [previewTab, setPreviewTab] = useState<'Preview' | 'Properties'>('Preview');
  const [selectedFileForPreview, setSelectedFileForPreview] = useState<FileViewItem | null>(null);

  const [viewsDropdownOpen, setViewsDropdownOpen] = useState(false);
  const viewsDropdownRef = useRef<HTMLDivElement>(null);

  /** File row context menu (Data Model Design style): visible, position, item */
  const [fileContextMenu, setFileContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    item: FileViewItem | null;
  }>({ visible: false, x: 0, y: 0, item: null });
  const fileContextMenuRef = useRef<HTMLDivElement>(null);

  /** Default view ID for current category (from ViewList); required for GetTransctionFolderViewList. */
  const [categoryDefaultViewId, setCategoryDefaultViewId] = useState<number | null>(null);
  /** View list for current category (from getCurrentUserFileFolderCategoryViewList.ViewList). */
  const [categoryViewList, setCategoryViewList] = useState<{ Id?: number; Name?: string; DisplayText?: string; IsDefaultView?: boolean }[]>([]);
  /** User-selected view id (overrides category default when set). */
  const [selectedViewId, setSelectedViewId] = useState<number | null>(null);
  /** View column definitions for current view (from RetrieveOneAppSearchViewExDto.AppSearchViewFieldList). */
  const [viewFields, setViewFields] = useState<{ Id?: number; SearchViewFieldID?: number; DisplayText?: string; Name?: string; ControlType?: number }[]>([]);
  /** TransRootIdColumnId from view DTO - required to resolve file ID from DictViewColumnIDKeyValue (Angular: CurrentViewDto.TransRootIdColumnId) */
  const [transRootIdColumnId, setTransRootIdColumnId] = useState<number | null>(null);
  /** FileCodeColumnId from view DTO - for file name resolution (Angular: CurrentViewDto.FileCodeColumnId) */
  const [fileCodeColumnId, setFileCodeColumnId] = useState<number | null>(null);
  /** Extension column id - FileCode has no extension; preview type is resolved from this column. */
  const [extensionColumnId, setExtensionColumnId] = useState<number | null>(null);

  const [loading, setLoading] = useState(true);

  const isLogicCategory = useMemo(() => {
    if (!isFileMgt) return false;
    return (
      currentCategory === FILE_FOLDER_CATEGORIES.MyRecentlyFiles ||
      currentCategory === FILE_FOLDER_CATEGORIES.Favorites ||
      currentCategory === FILE_FOLDER_CATEGORIES.ShareToOthers ||
      currentCategory === FILE_FOLDER_CATEGORIES.SharedToMe ||
      currentCategory === FILE_FOLDER_CATEGORIES.MyRecycleBin
    );
  }, [isFileMgt, currentCategory]);

  // Modal State
  const [showSharingEditor, setShowSharingEditor] = useState(false);
  const [sharingFileId, setSharingFileId] = useState<string | number | null>(null);
  const [sharingFileName, setSharingFileName] = useState('');
  const [isNotificationMode, setIsNotificationMode] = useState(false);
  const [cutFileIdList, setCutFileIdList] = useState<number[]>([]);
  /** Cut folder ID for Paste (Angular: currentCutFolderId) */
  const [cutFolderId, setCutFolderId] = useState<number | null>(null);

  /** Folder tree context menu (Angular: folderMenuPopup, user_clicks_opencontextmenu) */
  const [folderContextMenu, setFolderContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    folder: FolderDto | null;
  }>({ visible: false, x: 0, y: 0, folder: null });
  const folderContextMenuRef = useRef<HTMLDivElement>(null);

  /** Folder edit popup (Create/Rename/Set Default View) */
  const [folderEditPopup, setFolderEditPopup] = useState<{
    visible: boolean;
    mode: 'create' | 'rename' | 'defaultView';
    folder: Partial<FolderDto> | null;
    parentId: number | null;
  }>({ visible: false, mode: 'rename', folder: null, parentId: null });

  /** File upload popup - target folder from context menu or current folder */
  const [fileUploaderOpen, setFileUploaderOpen] = useState(false);
  const [fileUploadTargetFolderId, setFileUploadTargetFolderId] = useState<number | null>(null);

  /** Folder Security popup (admin only) */
  const [folderSecurityPopup, setFolderSecurityPopup] = useState<{ visible: boolean; folder: FolderDto | null }>({
    visible: false,
    folder: null,
  });

  /** Edit Drag & Drop Post Process popup (admin only) */
  const [dragDropPostProcessPopup, setDragDropPostProcessPopup] = useState<{
    visible: boolean;
    folder: Partial<FolderDto> & { DragDropPostProcessSetting?: { DataSetId?: number | null } } | null;
    importSettingList: { Id: number; Display?: string; Name?: string }[];
  }>({ visible: false, folder: null, importSettingList: [] });

  // Grid refs
  const folderGridRef = useRef<any>(null);
  const fileGridRef = useRef<any>(null);

  // Close Views dropdown on outside click
  useEffect(() => {
    if (!viewsDropdownOpen) return;
    const handler = (e: MouseEvent) => {
      if (viewsDropdownRef.current && !viewsDropdownRef.current.contains(e.target as Node)) {
        setViewsDropdownOpen(false);
      }
    };
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, [viewsDropdownOpen]);

  // Keep category in sync with route params/menu navigation
  useEffect(() => {
    if (defaultCategoryId != null && defaultCategoryId !== currentCategory) {
      setCurrentCategory(defaultCategoryId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [defaultCategoryId]);

  // Initialize folders CV
  useEffect(() => {
    setFoldersCV(new CollectionView<FolderDto>(folders));
  }, [folders]);

  // Initialize files CV
  useEffect(() => {
    setFilesCV(new CollectionView<FileViewItem>(files));
  }, [files]);

  // Load user data map
  useEffect(() => {
    const loadUserDataMap = async () => {
      try {
        const massData = await adminSvc.getMassEntitiesLookupItem('AppSecurityUser');
        const users = massData['AppSecurityUser'] || [];
        setUserDataMap(new DataMap(users, 'Id', 'Display'));
      } catch (e) {
        appHelper.debugLog('FolderNavigation loadUserDataMap error:', e);
      }
    };
    loadUserDataMap();
  }, []);

  // Load folder hierarchy
  const loadFolderHierarchy = useCallback(async () => {
    if (!transactionId) return;

    setLoading(true);
    dispatch(setIsBusy());

    try {
      // For file management, load folder categories first
      if (isFileMgt && currentCategory) {
        const categoryData = await appFolderNavigationService.getCurrentUserFileFolderCategoryViewList(
          String(currentCategory),
          String(transactionId)
        );

        // Normalize API response: backend may return ViewList/viewList, SearchResultList/searchResultList (PascalCase or camelCase)
        let viewListRaw = categoryData?.ViewList ?? categoryData?.viewList;
        let viewList = Array.isArray(viewListRaw) ? viewListRaw : [];
        const searchResultRaw = categoryData?.SearchResultList ?? categoryData?.searchResultList;
        const searchResultList = Array.isArray(searchResultRaw) ? searchResultRaw : [];

        // Recycle Bin (and any logic category) sometimes returns empty ViewList from backend; use My Company's ViewList as fallback
        if (isLogicCategory && viewList.length === 0) {
          try {
            const companyData = await appFolderNavigationService.getCurrentUserFileFolderCategoryViewList(
              String(FILE_FOLDER_CATEGORIES.Company),
              String(transactionId)
            );
            viewListRaw = companyData?.ViewList ?? companyData?.viewList;
            viewList = Array.isArray(viewListRaw) ? viewListRaw : [];
          } catch (_e) {
            appHelper.debugLog('FolderNavigation: fallback Company ViewList for Recycle Bin failed', _e);
          }
        }

        // Angular: logic categories (Recent/Favorites/Share/Recycle) show SearchResultList without folder tree
        if (isLogicCategory) {
          setFolders([]);
          setCurrentFolderId(null);
          setCurrentFolderData(null);
          setSelectedFileForPreview(null);
          // Same as folder categories: set ViewList and default view so FILE VIEW (columns + dropdown) loads
          setCategoryViewList(viewList);
          const defaultView = viewList.find((v: any) => v.IsDefaultView || v.isDefaultView) ?? viewList[0];
          setCategoryDefaultViewId(defaultView?.Id != null ? Number(defaultView.Id) : (defaultView?.id != null ? Number(defaultView.id) : null));
          setSelectedViewId(null);
          setFiles(searchResultList);
          return;
        }

        const rootList = categoryData?.HairarchyFolderRootList ?? categoryData?.FolderHierarchyList ?? categoryData?.hairarchyFolderRootList ?? [];
        setFiles([]);
        setSelectedFileForPreview(null);

        // Use ViewList to set default view for GetTransctionFolderViewList (Angular: defaultView = IsDefaultView or views[0])
        setCategoryViewList(viewList);
        const defaultViewFolder = viewList.find((v: any) => v.IsDefaultView || v.isDefaultView) ?? viewList[0];
        setCategoryDefaultViewId(defaultViewFolder?.Id != null ? Number(defaultViewFolder.Id) : (defaultViewFolder?.id != null ? Number(defaultViewFolder.id) : null));
        setSelectedViewId(null);

        if (rootList.length > 0) {
          setFolders(rootList);

          const initialIdNum = initialFolderId != null && initialFolderId !== '' ? Number(initialFolderId) : null;
          const targetId = initialIdNum != null && !Number.isNaN(initialIdNum) ? initialIdNum : rootList[0].Id;
          setCurrentFolderId(targetId);
        } else {
          setFolders([]);
          setCurrentFolderId(null);
          setCurrentFolderData(null);
          setCategoryDefaultViewId(null);
          setCategoryViewList([]);
          setViewFields([]);
        }
      } else {
        // For form data, load transaction folder hierarchy (API returns array directly)
        const hierarchyData = await appFolderNavigationService.retrieveFolderHierarchyDto(
          String(transactionId),
          String(initialFolderId || '')
        );
        const list = Array.isArray(hierarchyData) ? hierarchyData : hierarchyData?.FolderHierarchyList ?? hierarchyData?.HairarchyFolderRootList ?? [];
        if (list.length > 0) {
          setFolders(list);
          const initialIdNum = initialFolderId != null && initialFolderId !== '' ? Number(initialFolderId) : null;
          const targetId = initialIdNum != null && !Number.isNaN(initialIdNum) ? initialIdNum : list[0].Id;
          setCurrentFolderId(targetId);
        }
      }
    } catch (e) {
      appHelper.debugLog('FolderNavigation loadFolderHierarchy error:', e);
    } finally {
      setLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [transactionId, isFileMgt, currentCategory, initialFolderId, dispatch]);

  useEffect(() => {
    loadFolderHierarchy();
  }, [loadFolderHierarchy]);

  // Load view definition (columns) when view id is set so we can bind DictViewColumnIDKeyValue
  const effectiveViewId = selectedViewId ?? categoryDefaultViewId;
  useEffect(() => {
    if (!isFileMgt || effectiveViewId == null) {
      setViewFields([]);
      setTransRootIdColumnId(null);
      setFileCodeColumnId(null);
      setExtensionColumnId(null);
      return;
    }
    let cancelled = false;
    searchSvc
      .retrieveOneAppSearchViewExDto(String(effectiveViewId))
      .then((dto) => {
        if (cancelled) return;
        const list = dto?.AppSearchViewFieldList ?? dto?.appSearchViewFieldList ?? [];
        const fields = Array.isArray(list) ? list : [];
        setViewFields(fields);
        const transId = dto?.TransRootIdColumnId ?? dto?.transRootIdColumnId;
        const fileCodeId = dto?.FileCodeColumnId ?? dto?.fileCodeColumnId;
        setTransRootIdColumnId(transId != null ? Number(transId) : inferTransRootIdColumnId(fields));
        setFileCodeColumnId(fileCodeId != null ? Number(fileCodeId) : inferFileCodeColumnId(fields));
        setExtensionColumnId(inferExtensionColumnId(fields));
      })
      .catch((e) => {
        if (!cancelled) {
          appHelper.debugLog('FolderNavigation load view definition error:', e);
          setViewFields([]);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [isFileMgt, effectiveViewId]);

  // Load folder content when folder changes
  const loadFolderContent = useCallback(async () => {
    if (!transactionId) return;
    if (isFileMgt && isLogicCategory) return;
    if (!currentFolderId) {
      setFiles([]);
      return;
    }

    dispatch(setIsBusy());

    try {
      // Find current folder data
      const folderData = folders.find((f) => f.Id === currentFolderId);
      setCurrentFolderData(folderData || null);

      if (isFileMgt) {
        // Load files for file management (searchViewId required: selected view or folder DefaultViewId or category default view)
        const viewId =
          selectedViewId ??
          (folderData?.DefaultViewId != null ? Number(folderData.DefaultViewId) : null) ??
          categoryDefaultViewId;
        const searchViewId = viewId != null ? String(viewId) : '';
        if (!searchViewId) {
          appHelper.debugLog('FolderNavigation: no searchViewId for GetTransctionFolderViewList, skipping');
          setFiles([]);
          dispatch(setIsNotBusy());
          return;
        }
        const contentData = await appFolderNavigationService.getTransactionFolderViewList(
          String(currentFolderId),
          searchViewId,
          String(transactionId)
        );

        // API returns array of StaticSearchResultRowJsonDto directly (not { ResultDataList })
        const list = Array.isArray(contentData)
          ? contentData
          : (contentData?.ResultDataList ?? []);
        setFiles(list);
      } else {
        // Load form data for form navigation
        const viewId = folderData?.DefaultViewId ?? categoryDefaultViewId;
        const searchViewId = viewId != null ? String(viewId) : '';
        const contentData = await appFolderNavigationService.getTransactionFolderViewList(
          String(currentFolderId),
          searchViewId || '',
          String(transactionId)
        );
        // API returns array directly when not wrapped
        const list = Array.isArray(contentData) ? contentData : (contentData?.ResultDataList ?? []);
        setFiles(list);
      }
    } catch (e) {
      appHelper.debugLog('FolderNavigation loadFolderContent error:', e);
      setFiles([]);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [transactionId, currentFolderId, folders, isFileMgt, isLogicCategory, categoryDefaultViewId, selectedViewId, dispatch]);

  const loadLogicCategoryContent = useCallback(async () => {
    if (!transactionId || !isFileMgt || !isLogicCategory) return;
    dispatch(setIsBusy());
    try {
      const categoryData = await appFolderNavigationService.getCurrentUserFileFolderCategoryViewList(
        String(currentCategory),
        String(transactionId)
      );
      const list = categoryData?.SearchResultList ?? categoryData?.ResultDataList ?? [];
      setFiles(Array.isArray(list) ? list : []);
    } catch (e) {
      appHelper.debugLog('FolderNavigation loadLogicCategoryContent error:', e);
      setFiles([]);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [transactionId, isFileMgt, isLogicCategory, currentCategory, dispatch]);

  const refreshContent = useCallback(() => {
    if (isLogicCategory) loadLogicCategoryContent();
    else loadFolderContent();
  }, [isLogicCategory, loadLogicCategoryContent, loadFolderContent]);
  useEffect(() => {
    loadFolderContent();
  }, [loadFolderContent]);

  // Handle folder selection
  const handleFolderSelect = useCallback((folderId: number) => {
    setCurrentFolderId(folderId);
  }, []);

  // Handle category change
  const handleCategoryChange = useCallback((categoryId: number) => {
    setCurrentCategory(categoryId);
    setCurrentFolderId(null);
    setCurrentFolderData(null);
    setCategoryDefaultViewId(null);
    setCategoryViewList([]);
    setSelectedViewId(null);
    setViewFields([]);
    setFolders([]);
    setFiles([]);
    setSelectedFileForPreview(null);
  }, []);

  // Resolve file id from row (Angular: DictViewColumnIDKeyValue[TransRootIdColumnId]; fallback: Id or first numeric)
  const getFileIdFromItem = useCallback((item: FileViewItem | null): number | null => {
    if (!item) return null;
    if (item.Id != null) return Number(item.Id);
    const dict = item.DictViewColumnIDKeyValue ?? (item as any).dictViewColumnIDKeyValue;
    if (dict && typeof dict === 'object') {
      if (transRootIdColumnId != null) {
        const val = dict[transRootIdColumnId] ?? dict[String(transRootIdColumnId)];
        if (val != null && typeof val === 'number' && Number.isInteger(val)) return val;
      }
      const num = Object.values(dict).find((v) => typeof v === 'number' && Number.isInteger(v));
      return num != null ? (num as number) : null;
    }
    return null;
  }, [transRootIdColumnId]);

  const getFileDisplayFromItem = useCallback((item: FileViewItem | null): string => {
    if (!item) return '';
    if (item.Display) return String(item.Display);
    if (item.FileName) return String(item.FileName);
    const dict = item.DictViewColumnIDKeyValue ?? (item as any).dictViewColumnIDKeyValue;
    const sketch = item.DictSketchOrFileDisplayCode ?? (item as any).dictSketchOrFileDisplayCode;
    if (fileCodeColumnId != null && (dict || sketch)) {
      const key = fileCodeColumnId;
      const val = sketch?.[key] ?? sketch?.[String(key)] ?? dict?.[key] ?? dict?.[String(key)];
      if (val != null && val !== '') return String(val);
    }
    if (sketch && typeof sketch === 'object') {
      const first = Object.values(sketch).find((v) => v != null && v !== '');
      return first != null ? String(first) : '';
    }
    const id = getFileIdFromItem(item);
    return id != null ? `File ${id}` : 'File';
  }, [getFileIdFromItem, fileCodeColumnId]);

  const getFileExtensionFromItem = useCallback((item: FileViewItem | null): string => {
    if (!item) return '';
    if ((item as any).Extension) return String((item as any).Extension);
    const dict = item.DictViewColumnIDKeyValue ?? (item as any).dictViewColumnIDKeyValue;
    if (extensionColumnId != null && dict) {
      const val = dict[extensionColumnId] ?? dict[String(extensionColumnId)];
      if (val != null && val !== '') return String(val);
    }
    return '';
  }, [extensionColumnId]);

  const handleFileGridSelectionChanged = useCallback(
    (item: FileViewItem | null) => {
      setSelectedFileForPreview(item);
      if (isUseAsSelector && onFileSelected && item) {
        const id = getFileIdFromItem(item);
        if (id != null) onFileSelected(id);
      }
    },
    [isUseAsSelector, onFileSelected, getFileIdFromItem]
  );

  const handleOpenFileContextMenu = useCallback((item: FileViewItem, x: number, y: number) => {
    setFileContextMenu({ visible: true, x, y, item });
  }, []);

  const isFolderReadonly = currentFolderData?.IsFolderReadonly === true;
  // Header toolbar visibility per category (matches Angular folderNavigationFileHelper is*ButtonVisible)
  const headerVisible = useMemo(() => ({
    upload: !!currentFolderId && !isFolderReadonly && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    cut: !!currentFolderId && !isFolderReadonly && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    paste: !!currentFolderId && !isFolderReadonly && cutFileIdList.length > 0 && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    addFavorites: currentCategory !== FILE_FOLDER_CATEGORIES.Favorites && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    removeFavorites: currentCategory === FILE_FOLDER_CATEGORIES.Favorites && !isUseAsSelector,
    removeSharing: currentCategory === FILE_FOLDER_CATEGORIES.ShareToOthers && !isUseAsSelector,
    delete: !!currentFolderId && !isFolderReadonly && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    restore: currentCategory === FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    deleteFromRecycleBin: currentCategory === FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    views: !isUseAsSelector,
  }), [currentCategory, currentFolderId, isFolderReadonly, cutFileIdList.length, isUseAsSelector]);
  const fileRowVisible = useMemo(() => ({
    download: currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    delete: !!currentFolderId && !isFolderReadonly && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    cut: !!currentFolderId && !isFolderReadonly && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    addFavorites: currentCategory !== FILE_FOLDER_CATEGORIES.Favorites && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    removeFavorites: currentCategory === FILE_FOLDER_CATEGORIES.Favorites && !isUseAsSelector,
    share: !isFolderReadonly && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && currentCategory !== FILE_FOLDER_CATEGORIES.Favorites && currentCategory !== FILE_FOLDER_CATEGORIES.SharedToMe && !isUseAsSelector,
    notification: !isFolderReadonly && currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin && currentCategory !== FILE_FOLDER_CATEGORIES.Favorites && currentCategory !== FILE_FOLDER_CATEGORIES.SharedToMe && !isUseAsSelector,
    removeSharing: currentCategory === FILE_FOLDER_CATEGORIES.ShareToOthers && !isUseAsSelector,
    restoreRecycleBin: currentCategory === FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
    deleteFromRecycleBin: currentCategory === FILE_FOLDER_CATEGORIES.MyRecycleBin && !isUseAsSelector,
  }), [currentCategory, currentFolderId, isFolderReadonly, isUseAsSelector]);

  // Handle file selection
  const handleFileSelect = useCallback(
    (file: FileViewItem) => {
      if (isUseAsSelector && onFileSelected) {
        const id = getFileIdFromItem(file);
        if (id != null) onFileSelected(id);
      }
    },
    [isUseAsSelector, onFileSelected, getFileIdFromItem]
  );

  // Handle file double-click (open/download)
  const handleFileDoubleClick = useCallback(
    (file: FileViewItem) => {
      if (isUseAsSelector && onFileSelected) {
        const id = getFileIdFromItem(file);
        if (id != null) onFileSelected(id);
        return;
      }
      const id = getFileIdFromItem(file);
      if (id != null) {
        const fileUrl = `/api/file/download/${id}`;
        window.open(fileUrl, '_blank');
      }
    },
    [isUseAsSelector, onFileSelected, getFileIdFromItem]
  );

  // Search files
  const handleSearch = useCallback(async () => {
    if (!searchText.trim() || !transactionId) return;
    if (!isLogicCategory && !currentFolderId) return;

    dispatch(setIsBusy());

    try {
      if (isFileMgt && isLogicCategory) {
        const results = await appFolderNavigationService.getFileLogicCategoryFullTextSearchResult(
          String(currentCategory),
          '', // searchViewId
          String(transactionId),
          searchText
        );
        if (results && results.ResultDataList) {
          setFiles(results.ResultDataList);
        }
        return;
      }

      const searchOption = isFileMgt ? (rangeOption === 'AllFolders' ? 1 : 0) : (isNeedSearchSubFolders ? 1 : 0);
      const results = await appFolderNavigationService.getFileFolderFullTextSearchResult(
        '', // searchViewId
        String(currentFolderId),
        String(searchOption),
        String(transactionId),
        searchText
      );

      if (results && results.ResultDataList) {
        setFiles(results.ResultDataList);
      }
    } catch (e) {
      appHelper.debugLog('FolderNavigation search error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    searchText,
    transactionId,
    currentFolderId,
    isNeedSearchSubFolders,
    isFileMgt,
    isLogicCategory,
    currentCategory,
    rangeOption,
    dispatch,
  ]);

  // Open sharing editor
  const openSharingEditor = useCallback((file: FileViewItem, asNotification = false) => {
    const id = getFileIdFromItem(file);
    setSharingFileId(id != null ? id : null);
    setSharingFileName(getFileDisplayFromItem(file));
    setIsNotificationMode(asNotification);
    setShowSharingEditor(true);
  }, [getFileIdFromItem, getFileDisplayFromItem]);

  // Add to favorites
  const addToFavorites = useCallback(async (fileIds: number[]) => {
    if (fileIds.length === 0) return;

    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.addFilesToMyFavorite(fileIds);
      showInfo('Added to favorites', true);
    } catch (e) {
      appHelper.debugLog('FolderNavigation addToFavorites error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showInfo]);

  // Move to recycle bin
  const moveToRecycleBin = useCallback(async (fileIds: number[]) => {
    if (fileIds.length === 0) return;

    if (!window.confirm('Are you sure you want to move the selected files to recycle bin?')) {
      return;
    }

    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.moveFileToRecycleBin({ Key: Number(transactionId), Value: fileIds });
      loadFolderContent();
    } catch (e) {
      appHelper.debugLog('FolderNavigation moveToRecycleBin error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, loadFolderContent]);

  const removeFromFavorites = useCallback(async (fileIds: number[]) => {
    if (fileIds.length === 0 || currentCategory !== FILE_FOLDER_CATEGORIES.Favorites) return;
    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.removeFilesFromMyFavorite(fileIds);
      refreshContent();
    } catch (e) {
      appHelper.debugLog('FolderNavigation removeFromFavorites error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentCategory, dispatch, refreshContent]);

  const restoreFromRecycleBin = useCallback(async (fileIds: number[]) => {
    if (fileIds.length === 0 || currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin) return;
    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.restoreFileFromRecycleBin({ Key: Number(transactionId), Value: fileIds });
      refreshContent();
    } catch (e) {
      appHelper.debugLog('FolderNavigation restoreFromRecycleBin error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentCategory, transactionId, dispatch, refreshContent]);

  const deleteFromRecycleBin = useCallback(async (fileIds: number[]) => {
    if (fileIds.length === 0 || currentCategory !== FILE_FOLDER_CATEGORIES.MyRecycleBin) return;
    if (!window.confirm('Permanently delete these files from Recycle Bin?')) return;
    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.deleteFileFromRecycleBin({ Key: Number(transactionId), Value: fileIds });
      refreshContent();
    } catch (e) {
      appHelper.debugLog('FolderNavigation deleteFromRecycleBin error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentCategory, transactionId, dispatch, refreshContent]);

  const removeSharing = useCallback(async (fileIds: number[]) => {
    if (fileIds.length === 0 || currentCategory !== FILE_FOLDER_CATEGORIES.ShareToOthers) return;
    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.removeFilesToShareOther(fileIds);
      refreshContent();
    } catch (e) {
      appHelper.debugLog('FolderNavigation removeSharing error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentCategory, dispatch, refreshContent]);

  const cutFile = useCallback((fileId: number) => {
    setCutFileIdList([fileId]);
  }, []);

  // Folder context menu (Angular: folderMenuPopup, bindMethod.*)
  const openFolderContextMenu = useCallback((e: React.MouseEvent, folder: FolderDto) => {
    e.preventDefault();
    e.stopPropagation();
    if (isUseAsSelector || folder.IsFolderReadonly) return;
    const { x, y } = clampContextMenuPosition(
      e.clientX,
      e.clientY,
      FOLDER_CONTEXT_MENU_ESTIMATED_WIDTH,
      FOLDER_CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setFolderContextMenu({ visible: true, x, y, folder });
  }, [isUseAsSelector]);

  const closeFolderContextMenu = useCallback(() => {
    setFolderContextMenu({ visible: false, x: 0, y: 0, folder: null });
  }, []);

  const openFolderEditPopup = useCallback((mode: 'create' | 'rename' | 'defaultView', folder: Partial<FolderDto> | null, parentId: number | null) => {
    setFolderEditPopup({ visible: true, mode, folder: folder ?? { Name: 'New Folder', DefaultViewId: null }, parentId });
    closeFolderContextMenu();
  }, [closeFolderContextMenu]);

  const closeFolderEditPopup = useCallback(() => {
    setFolderEditPopup({ visible: false, mode: 'rename', folder: null, parentId: null });
  }, []);

  const createFolder = useCallback(() => {
    const parent = folderContextMenu.folder;
    const parentId = parent?.Id ?? null;
    openFolderEditPopup('create', {
      Name: 'New Folder',
      ParentId: parentId ?? undefined,
      TransactionId: transactionId ? Number(transactionId) : undefined,
      FolderType: parent?.FolderType,
    }, parentId);
  }, [folderContextMenu.folder, openFolderEditPopup, transactionId]);

  const editFolder = useCallback(async (isDefaultViewOnly?: boolean) => {
    const folder = folderContextMenu.folder;
    if (!folder?.Id) return;
    dispatch(setIsBusy());
    try {
      const data = await appFolderNavigationService.retrieveOneAppSeFolderExDto(String(folder.Id));
      if (data) {
        openFolderEditPopup(isDefaultViewOnly ? 'defaultView' : 'rename', data, folder.ParentId ?? null);
      }
    } catch (e) {
      appHelper.debugLog('FolderNavigation editFolder error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folderContextMenu.folder, openFolderEditPopup, dispatch]);

  const saveFolder = useCallback(async () => {
    const { folder, mode, parentId } = folderEditPopup;
    if (!folder) return;
    dispatch(setIsBusy());
    try {
      const payload: any = {
        ...folder,
        ParentId: mode === 'create' ? (parentId ?? undefined) : folder.ParentId,
        TransactionId: transactionId ? Number(transactionId) : folder.TransactionId,
      };
      await appFolderNavigationService.saveAppSeFolder(payload);
      closeFolderEditPopup();
      loadFolderHierarchy();
      if (folder.Id && currentFolderId === folder.Id) {
        setCurrentFolderData(folder as FolderDto);
      }
    } catch (e) {
      appHelper.debugLog('FolderNavigation saveFolder error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folderEditPopup, closeFolderEditPopup, loadFolderHierarchy, transactionId, currentFolderId, dispatch]);

  const deleteFolder = useCallback(async () => {
    const folderId = folderContextMenu.folder?.Id;
    if (!folderId) return;
    if (!window.confirm('Are you sure you want to delete this folder?')) return;
    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.deleteAppSeFolder(String(folderId));
      closeFolderContextMenu();
      const parentId = folderContextMenu.folder?.ParentId ?? null;
      if (currentFolderId === folderId && parentId != null) {
        handleFolderSelect(parentId);
      }
      loadFolderHierarchy();
    } catch (e) {
      appHelper.debugLog('FolderNavigation deleteFolder error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folderContextMenu.folder, currentFolderId, closeFolderContextMenu, loadFolderHierarchy, dispatch]);

  const cutFolder = useCallback(() => {
    const folderId = folderContextMenu.folder?.Id;
    if (folderId) setCutFolderId(folderId);
    closeFolderContextMenu();
  }, [folderContextMenu.folder, closeFolderContextMenu]);

  const pasteFolder = useCallback(async () => {
    const targetFolderId = folderContextMenu.folder?.Id;
    if (!targetFolderId || !cutFolderId) return;
    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.pasteAppSeFolder(String(cutFolderId), String(targetFolderId));
      setCutFolderId(null);
      closeFolderContextMenu();
      loadFolderHierarchy();
    } catch (e) {
      appHelper.debugLog('FolderNavigation pasteFolder error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folderContextMenu.folder, cutFolderId, closeFolderContextMenu, loadFolderHierarchy, dispatch]);

  const pasteFilesToFolder = useCallback(async (targetFolderIdOverride?: number) => {
    const targetFolderId = targetFolderIdOverride ?? folderContextMenu.folder?.Id;
    if (!targetFolderId || cutFileIdList.length === 0) {
      if (cutFileIdList.length === 0) showInfo('No files cut to paste.', true);
      if (!targetFolderIdOverride) closeFolderContextMenu();
      return;
    }
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.updateAppFileFolder({
        TargetFolderId: targetFolderId,
        FileIdList: cutFileIdList,
      });
      if (result?.IsSuccessful !== false) {
        setCutFileIdList([]);
        showInfo('Files moved.', true);
        if (currentFolderId === targetFolderId) {
          loadFolderContent();
        }
      } else {
        showInfo(result?.ValidationResult?.Items?.[0]?.LocalizedMessage ?? 'Failed to move files.', true);
      }
    } catch (e) {
      appHelper.debugLog('FolderNavigation pasteFilesToFolder error:', e);
      showInfo(e instanceof Error ? e.message : 'Failed to move files.', true);
    } finally {
      dispatch(setIsNotBusy());
    }
    if (!targetFolderIdOverride) closeFolderContextMenu();
  }, [folderContextMenu.folder, cutFileIdList, currentFolderId, closeFolderContextMenu, loadFolderContent, dispatch, showInfo]);

  const uploadFilesToFolder = useCallback(() => {
    const targetFolderId = folderContextMenu.folder?.Id;
    if (!targetFolderId) {
      showInfo('Please select a folder to upload to.', true);
      closeFolderContextMenu();
      return;
    }
    setFileUploadTargetFolderId(targetFolderId);
    setFileUploaderOpen(true);
    closeFolderContextMenu();
  }, [folderContextMenu.folder, closeFolderContextMenu, showInfo]);

  const openFolderSecurityPopup = useCallback(async () => {
    const folder = folderContextMenu.folder;
    if (!folder?.Id) return;
    dispatch(setIsBusy());
    try {
      const data = await appFolderNavigationService.retrieveOneAppSeFolderExDto(String(folder.Id));
      if (data) {
        setFolderSecurityPopup({ visible: true, folder: data });
      }
    } catch (e) {
      appHelper.debugLog('FolderNavigation openFolderSecurityPopup error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
    closeFolderContextMenu();
  }, [folderContextMenu.folder, closeFolderContextMenu, dispatch]);

  const openDragDropPostProcessPopup = useCallback(async () => {
    const folder = folderContextMenu.folder;
    if (!folder?.Id) return;
    dispatch(setIsBusy());
    try {
      const [folderData, tableList, entityList] = await Promise.all([
        appFolderNavigationService.retrieveOneAppSeFolderExDto(String(folder.Id)),
        appTransactionService.retrieveAllExcelTableImportSettingDto(false),
        appTransactionService.retrieveAllEntityImportSettingDto(),
      ]);
      if (folderData) {
        const dragDrop = folderData.DragDropPostProcessSetting || {};
        const importList: { Id: number; Display: string }[] = [];
        (tableList || []).forEach((t: any) => {
          importList.push({ Id: t.Id, Display: `${t.Id} Table - ${t.Name ?? ''}` });
        });
        (entityList || []).forEach((e: any) => {
          importList.push({ Id: e.Id, Display: `${e.Id} Entity - ${e.Name ?? ''}` });
        });
        setDragDropPostProcessPopup({
          visible: true,
          folder: { ...folderData, DragDropPostProcessSetting: { ...dragDrop, DataSetId: dragDrop.DataSetId ?? null } },
          importSettingList: importList,
        });
      }
    } catch (e) {
      appHelper.debugLog('FolderNavigation openDragDropPostProcessPopup error:', e);
    } finally {
      dispatch(setIsNotBusy());
    }
    closeFolderContextMenu();
  }, [folderContextMenu.folder, closeFolderContextMenu, dispatch]);

  const closeFolderSecurityPopup = useCallback(() => {
    setFolderSecurityPopup({ visible: false, folder: null });
  }, []);

  const closeDragDropPostProcessPopup = useCallback(() => {
    setDragDropPostProcessPopup({ visible: false, folder: null, importSettingList: [] });
  }, []);

  const downloadFile = useCallback((fileId: number) => {
    // Fetch bytes and save (window.open of a relative API path hits the SPA fallback in dev).
    downloadFileById(fileId).catch((e) => {
      appHelper.debugLog('FolderNavigation download error:', e);
      showInfo('Download failed.', true);
    });
  }, [showInfo]);

  // Resize handlers
  const handleResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizing(true);
    resizeStartX.current = e.clientX;
    resizeStartWidth.current = folderTreeWidth;
  }, [folderTreeWidth]);

  useEffect(() => {
    if (!isResizing) return;

    const handleMouseMove = (e: MouseEvent) => {
      const delta = e.clientX - resizeStartX.current;
      const newWidth = Math.max(200, Math.min(600, resizeStartWidth.current + delta));
      setFolderTreeWidth(newWidth);
    };

    const handleMouseUp = () => {
      setIsResizing(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isResizing]);

  // Preview panel resize (drag border between file list and preview)
  const handlePreviewResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsPreviewResizing(true);
    previewResizeStartX.current = e.clientX;
    previewResizeStartWidth.current = previewPanelWidth;
  }, [previewPanelWidth]);

  useEffect(() => {
    if (!isPreviewResizing) return;
    const handleMouseMove = (e: MouseEvent) => {
      const delta = e.clientX - previewResizeStartX.current;
      // Invert: dragging handle left widens preview (boundary moves left), dragging right narrows it
      const newWidth = Math.max(200, Math.min(600, previewResizeStartWidth.current - delta));
      setPreviewPanelWidth(newWidth);
    };
    const handleMouseUp = () => setIsPreviewResizing(false);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isPreviewResizing]);

  // Close folder context menu on outside click
  useEffect(() => {
    if (!folderContextMenu.visible) return;
    const handleClick = (e: MouseEvent) => {
      if (folderContextMenuRef.current?.contains(e.target as Node)) return;
      setFolderContextMenu({ visible: false, x: 0, y: 0, folder: null });
    };
    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [folderContextMenu.visible]);

  // Close file context menu on outside click
  useEffect(() => {
    if (!fileContextMenu.visible) return;
    const handleClick = (e: MouseEvent) => {
      if (fileContextMenuRef.current?.contains(e.target as Node)) return;
      setFileContextMenu({ visible: false, x: 0, y: 0, item: null });
    };
    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [fileContextMenu.visible]);

  useRefineContextMenuField(folderContextMenu.visible, folderContextMenuRef, setFolderContextMenu);
  useRefineContextMenuField(fileContextMenu.visible, fileContextMenuRef, setFileContextMenu);

  // Get selected file IDs from grid (Id or first numeric value from DictViewColumnIDKeyValue)
  const getSelectedFileIds = useCallback((): number[] => {
    const fileGrid = fileGridRef.current?.control;
    if (!fileGrid || !fileGrid.selectedRows) return [];

    return fileGrid.selectedRows
      .map((row: any) => {
        const item = row.dataItem;
        if (!item) return null;
        if (item.Id != null) return item.Id;
        const dict = item.DictViewColumnIDKeyValue ?? item.dictViewColumnIDKeyValue;
        if (dict && typeof dict === 'object') {
          const vals = Object.values(dict);
          const num = vals.find((v) => typeof v === 'number' && Number.isInteger(v));
          return num != null ? (num as number) : null;
        }
        return null;
      })
      .filter(Boolean);
  }, []);

  if (!transactionId) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className={`text-xs ${theme.label}`}>Transaction ID is required.</p>
      </div>
    );
  }

  const getCategoryMenuClass = (categoryId: number) =>
    currentCategory === categoryId ? theme.tab_active : theme.tab;

  const isFolderTreeVisible = isShowFolderTree && (!isFileMgt || !isLogicCategory);

  return (
    <div className={`w-full h-full flex overflow-hiddenss`}>
      {/* Left vertical category menu (My Application Editor style), only on File Management */}
      {isFileMgt && (
        <div
          className={`w-[90px] mr-1 flex-none flex flex-col overflow-y-auto rounded-l-md ${theme.mainContentSection}`}
        >
          <div className="py-5">
            {FILE_MGT_CATEGORY_ORDER.map((categoryId) => {
              if (categoryId === FILE_FOLDER_CATEGORIES.MyRecycleBin && isUseAsSelector) return null;
              const info = CATEGORY_DISPLAY[categoryId];
              if (!info) return null;
              return (
                <div
                  key={categoryId}
                  role="button"
                  tabIndex={0}
                  onClick={() => handleCategoryChange(categoryId)}
                  onKeyDown={(e) => e.key === 'Enter' && handleCategoryChange(categoryId)}
                  className={`
                    cursor-pointer flex flex-col items-center py-3 px-2 mb-1 transition-colors
                    ${getCategoryMenuClass(categoryId)}
                  `}
                  title={info.display}
                >
                  <i className={`fa-solid ${info.icon} text-2xl mb-1`} />
                  <div className="text-[10px] text-center leading-tight" style={{ wordBreak: 'break-word' }}>
                    {info.display}
                  </div>
                </div>
              );
            })}
            {/* Dropbox at bottom (My Application Editor style) */}
            <div
              role="button"
              tabIndex={0}
              onClick={() => {}}
              onKeyDown={(e) => e.key === 'Enter'}
              className={`cursor-pointer flex flex-col items-center py-3 px-2 mb-1 transition-colors mt-2 ${theme.tab}`}
              title="Dropbox"
            >
              <i className="fa-solid fa-cloud-arrow-up text-2xl mb-1" />
              <div className="text-[10px] text-center leading-tight" style={{ wordBreak: 'break-word' }}>
                Dropbox
              </div>
            </div>
          </div>
        </div>
      )}

      <div className="w-1 flex-auto h-full flex overflow-hidden min-w-0">
        {/* Folder Tree Panel */}
        {isFolderTreeVisible && (
          <div
            className="flex flex-col mr-1 relative"
            style={{ width: folderTreeWidth, minWidth: 200, maxWidth: 600 }}
          >
            {/* Folder Tree Header (title = category name when file mgt, else folder name) */}
            <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
              <div className={`text-xs font-semibold ${theme.title} truncate`} title={isFileMgt ? CATEGORY_DISPLAY[currentCategory]?.display : currentFolderData?.Name}>
                <i className="fa-solid fa-folder-open mr-1" />
                {isFileMgt ? (CATEGORY_DISPLAY[currentCategory]?.display ?? 'Folders') : (currentFolderData?.Name || 'Folders')}
              </div>
              <div className="flex items-center space-x-1">
                <button
                  type="button"
                  className={`w-6 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                  onClick={loadFolderHierarchy}
                  title="Refresh"
                >
                  <i className="fa-solid fa-rotate" />
                </button>
                <button
                  type="button"
                  className={`w-6 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                  onClick={() => setIsShowFolderTree(false)}
                  title="Hide"
                >
                  <i className="fa-solid fa-chevron-left" />
                </button>
              </div>
            </div>

            {/* Folder Tree Grid */}
            <div className={`h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
              {foldersCV && (
                <FlexGrid
                  ref={folderGridRef}
                  itemsSource={foldersCV}
                  selectionMode="Row"
                  headersVisibility="None"
                  isReadOnly={true}
                  childItemsPath="Children"
                  style={{ height: '100%' }}
                  selectionChanged={(s: any) => {
                    const flex = s?.control ?? s;
                    const rowIndex = flex?.selection?.row;
                    const item =
                      rowIndex != null && flex?.rows?.[rowIndex] != null
                        ? ((flex.rows[rowIndex] as any).dataItem as FolderDto)
                        : null;
                    if (item?.Id) handleFolderSelect(item.Id);
                  }}
                >
                  <FlexGridColumn binding="Name" header="Folder" width="*">
                    <FlexGridCellTemplate
                      cellType="Cell"
                      template={(cell: any) => {
                        const item = cell.item as FolderDto;
                        const isSelected = item.Id === currentFolderId;
                        const showContextBtn = !isUseAsSelector && !item.IsFolderReadonly;
                        return (
                          <div
                            className={`flex items-center justify-between py-1 group ${isSelected ? 'font-semibold' : ''}`}
                            style={{ paddingLeft: (item.Level || 0) * 16 }}
                            onContextMenu={showContextBtn ? (e) => openFolderContextMenu(e, item) : undefined}
                          >
                            <div className="flex items-center min-w-0 flex-auto">
                              <i className={`fa-solid fa-folder mr-2 text-yellow-500 flex-shrink-0`} />
                              <span className="truncate">{item.Name}</span>
                              {item.CountContent != null && (
                                <span className="text-gray-400 ml-1 text-xs">({item.CountContent})</span>
                              )}
                            </div>
                            {showContextBtn && (
                              <button
                                type="button"
                                className={`flex-shrink-0 w-6 h-6 opacity-0 group-hover:opacity-100 ${theme.button_default} rounded px-1`}
                                onClick={(e) => openFolderContextMenu(e, item)}
                                title="More Options"
                              >
                                <i className="fa-solid fa-ellipsis-vertical text-xs" aria-hidden />
                              </button>
                            )}
                          </div>
                        );
                      }}
                    />
                  </FlexGridColumn>
                </FlexGrid>
              )}
            </div>

            {/* Resize Handle */}
            <div
              className="absolute top-0 right-0 w-1 h-full cursor-ew-resize hover:bg-blue-400"
              onMouseDown={handleResizeStart}
            />
          </div>
        )}

        {/* Show Folder Tree Button (when hidden) */}
        {!isFolderTreeVisible && !isLogicCategory && (
          <div className="h-full flex flex-col items-center mr-1">
            <button
              type="button"
              className={`px-1 py-4 h-full text-xs ${theme.button_default}`}
              onClick={() => setIsShowFolderTree(true)}
              title="Show Folder Tree"
              style={{ writingMode: 'vertical-lr' }}
            >
              Folder Tree
            </button>
          </div>
        )}

        {/* Content Panel */}
        <div className="w-1 flex-auto flex flex-col overflow-hidden min-w-0">
          {/* Angular-style: horizontal toolbar row (File Management only) */}
          {isFileMgt && (
            <div className={`flex items-center gap-2 px-3 py-2 border-b ${theme.mainContentSection}`}>
              {headerVisible.upload && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  title="Upload"
                  onClick={() => {
                    if (currentFolderId) {
                      setFileUploadTargetFolderId(currentFolderId);
                      setFileUploaderOpen(true);
                    }
                  }}
                >
                  <i className="fa-solid fa-arrow-up mr-1" />
                  Upload
                </button>
              )}
              {headerVisible.cut && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  title="Cut"
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) setCutFileIdList(ids);
                  }}
                >
                  <i className="fa-solid fa-scissors mr-1" />
                  Cut
                </button>
              )}
              {headerVisible.paste && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  title="Paste"
                  onClick={() => currentFolderId && pasteFilesToFolder(currentFolderId)}
                >
                  <i className="fa-solid fa-clipboard mr-1" />
                  Paste
                </button>
              )}
              {headerVisible.addFavorites && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) addToFavorites(ids);
                  }}
                  title="Add Favorites"
                >
                  <i className="fa-solid fa-star mr-1" />
                  Add Favorites
                </button>
              )}
              {headerVisible.removeFavorites && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) removeFromFavorites(ids);
                  }}
                  title="Remove from Favorites"
                >
                  <i className="fa-solid fa-times mr-1" />
                  Remove from Favorites
                </button>
              )}
              {headerVisible.removeSharing && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) removeSharing(ids);
                  }}
                  title="Remove Sharing"
                >
                  <i className="fa-solid fa-share-nodes-slash mr-1" />
                  Remove Sharing
                </button>
              )}
              {headerVisible.delete && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) moveToRecycleBin(ids);
                  }}
                  title="Delete"
                >
                  <i className="fa-solid fa-trash mr-1" />
                  Delete
                </button>
              )}
              {headerVisible.restore && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) restoreFromRecycleBin(ids);
                  }}
                  title="Restore"
                >
                  <i className="fa-solid fa-rotate-left mr-1" />
                  Restore
                </button>
              )}
              {headerVisible.deleteFromRecycleBin && (
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) deleteFromRecycleBin(ids);
                  }}
                  title="Delete permanently"
                >
                  <i className="fa-solid fa-trash mr-1" />
                  Delete
                </button>
              )}
              {headerVisible.views && (
              <div className="relative" ref={viewsDropdownRef}>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    setViewsDropdownOpen((v) => !v);
                  }}
                  className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}
                  title="Views"
                >
                  {(() => {
                    const viewId = selectedViewId ?? categoryDefaultViewId;
                    const current = categoryViewList.find((v) => (v.Id ?? (v as any).id) === viewId);
                    const label = current?.Name ?? current?.DisplayText ?? (current as any)?.Display ?? 'Default';
                    return <>{label} <i className="fa-solid fa-caret-down text-xs" aria-hidden /></>;
                  })()}
                </button>
                {viewsDropdownOpen && (
                  <div
                    className={`absolute left-0 top-full mt-1 min-w-[160px] max-h-[280px] overflow-y-auto rounded-[4px] shadow-lg z-50 border py-1 ${theme.mainContentSection}`}
                    onClick={(e) => e.stopPropagation()}
                  >
                    {categoryViewList.length === 0 ? (
                      <button
                        type="button"
                        className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}
                        onClick={() => setViewsDropdownOpen(false)}
                      >
                        Default
                      </button>
                    ) : (
                      categoryViewList.map((view) => {
                        const id = view.Id ?? (view as any).id;
                        const name = view.Name ?? view.DisplayText ?? (view as any).Display ?? `View ${id}`;
                        const isActive = (selectedViewId ?? categoryDefaultViewId) === id;
                        return (
                          <button
                            key={id ?? name}
                            type="button"
                            className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu} ${isActive ? theme.tab_active : ''}`}
                            onClick={() => {
                              setSelectedViewId(id != null ? Number(id) : null);
                              setViewsDropdownOpen(false);
                            }}
                          >
                            {name}
                          </button>
                        );
                      })
                    )}
                  </div>
                )}
              </div>
              )}
            </div>
          )}

          {/* Content Header: File View label + Search + Range (and legacy actions when not isFileMgt) */}
          <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
            <div className="flex items-center gap-3 flex-wrap">
              {isFileMgt && (
                <span className={`text-xs font-medium ${theme.title}`}>
                  File View: ({files.length})
                </span>
              )}
              <div className="flex items-center">
                <input
                  type="text"
                  className={`w-48 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  placeholder="Search..."
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                />
                <button
                  type="button"
                  className={`w-7 h-7 ${theme.button_default} rounded-r-[4px] text-xs`}
                  onClick={handleSearch}
                  title="Search"
                >
                  <i className="fa-solid fa-search" />
                </button>
              </div>
              {isFileMgt ? (
                <>
                  <div className="flex items-center">
                    <span className={`text-xs ${theme.label} mr-1`}>Range</span>
                    <select
                      className={`h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none rounded-[4px] min-w-[100px]`}
                      value={rangeOption}
                      onChange={(e) => setRangeOption(e.target.value)}
                    >
                      <option value="AllFolders">AllFolders</option>
                      <option value="CurrentFolder">CurrentFolder</option>
                    </select>
                  </div>
                  <label className="flex items-center text-xs">
                    <input
                      type="checkbox"
                      checked={isNeedSearchSubFolders}
                      onChange={(e) => setIsNeedSearchSubFolders(e.target.checked)}
                      className="mr-1"
                    />
                    Include Subfolders
                  </label>
                </>
              ) : (
                <label className="flex items-center text-xs">
                  <input
                    type="checkbox"
                    checked={isNeedSearchSubFolders}
                    onChange={(e) => setIsNeedSearchSubFolders(e.target.checked)}
                    className="mr-1"
                  />
                  Include Subfolders
                </label>
              )}
            </div>

            {/* Actions when not isFileMgt (toolbar is above when isFileMgt) */}
            {!isFileMgt && (
              <div className="flex items-center space-x-1">
                <button
                  type="button"
                  className={`w-7 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) addToFavorites(ids);
                  }}
                  title="Add to Favorites"
                >
                  <i className="fa-solid fa-heart" />
                </button>
                <button
                  type="button"
                  className={`w-7 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                  onClick={() => {
                    const fileGrid = fileGridRef.current?.control;
                    const file = fileGrid?.collectionView?.currentItem;
                    if (file) openSharingEditor(file);
                  }}
                  title="Share"
                >
                  <i className="fa-solid fa-share" />
                </button>
                <button
                  type="button"
                  className={`w-7 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                  onClick={() => {
                    const ids = getSelectedFileIds();
                    if (ids.length > 0) moveToRecycleBin(ids);
                  }}
                  title="Delete"
                >
                  <i className="fa-solid fa-trash" />
                </button>
                <button
                  type="button"
                  className={`w-7 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                  onClick={loadFolderContent}
                  title="Refresh"
                >
                  <i className="fa-solid fa-rotate" />
                </button>
              </div>
            )}
          </div>

          {/* File grid + optional right panel (Angular layout) */}
          <div className="h-1 flex-auto flex overflow-hidden min-w-0">
          {/* File Grid - mr-1 separator (no border), relative for resize handle */}
          <div className={`h-full flex-auto overflow-hidden min-w-0 mr-1 relative ${theme.mainContentSection}`} style={{ minWidth: 200 }}>
            <FolderFileGrid
              loading={loading}
              filesCV={filesCV}
              viewFields={viewFields}
              userDataMap={userDataMap}
              menuButtonClass={theme.menu_default}
              fileGridRef={fileGridRef}
              onSelectionChanged={handleFileGridSelectionChanged}
              onOpenFileContextMenu={handleOpenFileContextMenu}
            />
            {/* Resize handle on right edge (drag to resize file list vs preview) */}
            <div
              className="absolute top-0 right-0 bottom-0 w-1 cursor-ew-resize hover:bg-blue-400 z-10"
              onMouseDown={handlePreviewResizeStart}
              title="Drag to resize"
              role="separator"
              aria-orientation="vertical"
            />
          </div>

          {/* Right panel: Preview / Properties - resizable width, no left border */}
          {isFileMgt && (
            <div
              className={`flex-none flex flex-col overflow-hidden rounded-r-md border-l-0 ${theme.mainContentSection}`}
              style={{ width: previewPanelWidth, minWidth: 200, maxWidth: 600 }}
            >
              <div className="flex border-b">
                <button
                  type="button"
                  className={`flex-1 px-3 py-2 text-xs font-medium ${previewTab === 'Preview' ? theme.tab_active : theme.tab}`}
                  onClick={() => setPreviewTab('Preview')}
                >
                  Preview
                </button>
                <button
                  type="button"
                  className={`flex-1 px-3 py-2 text-xs font-medium ${previewTab === 'Properties' ? theme.tab_active : theme.tab}`}
                  onClick={() => setPreviewTab('Properties')}
                >
                  Properties
                </button>
              </div>
              <div className="flex-1 overflow-auto p-3">
                {previewTab === 'Preview' && (
                  <FilePreviewPanel
                    selectedFile={selectedFileForPreview}
                    getFileIdFromItem={getFileIdFromItem}
                    getFileDisplayFromItem={getFileDisplayFromItem}
                    getFileExtensionFromItem={getFileExtensionFromItem}
                    theme={theme}
                  />
                )}
                {previewTab === 'Properties' && (
                  <FilePropertiesFormPanel
                    selectedFile={selectedFileForPreview}
                    transactionId={transactionId}
                    getFileIdFromItem={getFileIdFromItem}
                    theme={theme}
                  />
                )}
              </div>
            </div>
          )}
          </div>
        </div>
      </div>

      {/* Folder tree context menu (Angular: folderMenuPopup) */}
      {folderContextMenu.visible && folderContextMenu.folder && (
        <div
          ref={folderContextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[180px]`}
          style={{
            left: folderContextMenu.x,
            top: folderContextMenu.y,
          }}
          onClick={(e) => e.stopPropagation()}
          role="menu"
        >
          <div className="py-1">
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={createFolder}
            >
              <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-plus" aria-hidden /></span>
              <span className="ml-2">Create Folder</span>
            </button>
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={() => editFolder(false)}
            >
              <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-pen" aria-hidden /></span>
              <span className="ml-2">Rename Folder</span>
            </button>
            {isSysAdmin && (
              <>
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                  onClick={() => { void openFolderSecurityPopup(); }}
                >
                  <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-lock" aria-hidden /></span>
                  <span className="ml-2">Folder Security</span>
                </button>
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                  onClick={() => { void openDragDropPostProcessPopup(); }}
                >
                  <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-cloud-arrow-up" aria-hidden /></span>
                  <span className="ml-2">Edit Drag & Drop Post Process</span>
                </button>
              </>
            )}
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={deleteFolder}
            >
              <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-times" aria-hidden /></span>
              <span className="ml-2">Delete Folder</span>
            </button>
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={cutFolder}
            >
              <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-scissors" aria-hidden /></span>
              <span className="ml-2">Cut Folder</span>
            </button>
            {cutFolderId != null && (
              <button
                type="button"
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                onClick={pasteFolder}
              >
                <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-clipboard" aria-hidden /></span>
                <span className="ml-2">Paste Folder</span>
              </button>
            )}
          </div>
          {isFileMgt && (
            <>
              <div className={`border-t ${theme.mainContentSection} my-1`} />
              <div className="py-1">
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                  onClick={() => { void pasteFilesToFolder(); }}
                >
                  <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-clipboard" aria-hidden /></span>
                  <span className="ml-2">Paste Files</span>
                </button>
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                  onClick={() => uploadFilesToFolder()}
                >
                  <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-arrow-up" aria-hidden /></span>
                  <span className="ml-2">Upload Files</span>
                </button>
              </div>
            </>
          )}
          <div className={`border-t ${theme.mainContentSection} my-1`} />
          <div className="py-1">
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={() => editFolder(true)}
            >
              <span className="w-5 flex-shrink-0 inline-flex items-center justify-center"><i className="fa-solid fa-tag" aria-hidden /></span>
              <span className="ml-2">Set Default View</span>
            </button>
          </div>
        </div>
      )}

      {/* Folder edit popup (Create/Rename/Set Default View) */}
      {folderEditPopup.visible && folderEditPopup.folder && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/30">
          <div
            className={`${theme.mainContentSection} rounded-lg shadow-xl w-[350px] max-w-[95vw] p-4 border`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-3">
              <span className={`text-sm font-semibold ${theme.title}`}>
                {folderEditPopup.mode === 'create' ? 'Create Folder' : folderEditPopup.mode === 'defaultView' ? 'Set Default View' : 'Rename Folder'}
              </span>
              <button type="button" className={`${theme.button_default} w-6 h-6 rounded`} onClick={closeFolderEditPopup}>&times;</button>
            </div>
            {folderEditPopup.mode !== 'defaultView' && (
              <div className="mb-3">
                <label className={`block text-xs ${theme.label} mb-1`}>Folder Name</label>
                <input
                  type="text"
                  value={folderEditPopup.folder.Name ?? ''}
                  onChange={(e) => setFolderEditPopup((p) => ({ ...p, folder: { ...p.folder!, Name: e.target.value } }))}
                  className={`w-full h-8 px-2 text-sm border rounded ${theme.inputBox}`}
                />
              </div>
            )}
            {folderEditPopup.mode === 'defaultView' && (
              <div className="mb-3">
                <label className={`block text-xs ${theme.label} mb-1`}>Default View</label>
                <select
                  value={folderEditPopup.folder.DefaultViewId ?? ''}
                  onChange={(e) => setFolderEditPopup((p) => ({ ...p, folder: { ...p.folder!, DefaultViewId: e.target.value ? Number(e.target.value) : null } }))}
                  className={`w-full h-8 px-2 text-sm border rounded ${theme.inputBox}`}
                >
                  <option value="">Default</option>
                  {categoryViewList.map((v) => {
                    const id = v.Id ?? (v as any).id;
                    const name = v.Name ?? v.DisplayText ?? `View ${id}`;
                    return <option key={id} value={id}>{name}</option>;
                  })}
                </select>
              </div>
            )}
            <div className="flex justify-end gap-2 mt-3">
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeFolderEditPopup}>Cancel</button>
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={saveFolder}>
                <i className="fa-solid fa-floppy-disk mr-1" /> Save
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Folder Security popup (admin only) */}
      <FolderSecurityEditor
        open={folderSecurityPopup.visible}
        onClose={closeFolderSecurityPopup}
        folder={folderSecurityPopup.folder}
        transactionId={transactionId != null ? String(transactionId) : ''}
        onSaved={() => loadFolderHierarchy()}
      />

      {/* Edit Drag & Drop Post Process popup (admin only) */}
      {dragDropPostProcessPopup.visible && dragDropPostProcessPopup.folder && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/30" onClick={closeDragDropPostProcessPopup}>
          <div
            className={`${theme.mainContentSection} rounded-lg shadow-xl w-[350px] max-w-[95vw] p-4 border`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-3">
              <span className={`text-sm font-semibold ${theme.title}`}>Drag & Drop Post Process Setting</span>
              <button type="button" className={`${theme.button_default} w-6 h-6 rounded`} onClick={closeDragDropPostProcessPopup}>&times;</button>
            </div>
            <div className="mb-3">
              <label className={`block text-xs ${theme.label} mb-1`}>Folder Name</label>
              <input
                type="text"
                value={dragDropPostProcessPopup.folder.Name ?? ''}
                readOnly
                className={`w-full h-8 px-2 text-sm border rounded ${theme.inputBox} bg-opacity-50`}
              />
            </div>
            <div className="mb-3">
              <label className={`block text-xs ${theme.label} mb-1`}>Import Setting</label>
              <select
                value={dragDropPostProcessPopup.folder.DragDropPostProcessSetting?.DataSetId ?? ''}
                onChange={(e) => {
                  const val = e.target.value ? Number(e.target.value) : null;
                  setDragDropPostProcessPopup((p) => ({
                    ...p,
                    folder: p.folder
                      ? {
                          ...p.folder,
                          DragDropPostProcessSetting: { ...(p.folder.DragDropPostProcessSetting ?? {}), DataSetId: val },
                        }
                      : null,
                  }));
                }}
                className={`w-full h-8 px-2 text-sm border rounded ${theme.inputBox}`}
              >
                <option value="">— Select —</option>
                {dragDropPostProcessPopup.importSettingList.map((s) => (
                  <option key={s.Id} value={s.Id}>{s.Display ?? `${s.Id} - ${s.Name ?? ''}`}</option>
                ))}
              </select>
            </div>
            <div className="flex justify-end gap-2 mt-3">
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeDragDropPostProcessPopup}>Cancel</button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={async () => {
                  const f = dragDropPostProcessPopup.folder;
                  if (!f?.Id) return;
                  dispatch(setIsBusy());
                  try {
                    const payload = {
                      ...f,
                      DragDropPostProcessSetting: { DataSetId: f.DragDropPostProcessSetting?.DataSetId ?? null },
                    };
                    await appFolderNavigationService.saveAppSeFolder(payload);
                    closeDragDropPostProcessPopup();
                    loadFolderHierarchy();
                  } catch (e) {
                    appHelper.debugLog('FolderNavigation saveDragDropPostProcess error:', e);
                  } finally {
                    dispatch(setIsNotBusy());
                  }
                }}
              >
                <i className="fa-solid fa-floppy-disk mr-1" /> Save
              </button>
            </div>
          </div>
        </div>
      )}

      {/* File upload popup (from folder context menu: Upload Files) */}
      <FileUploader
        isOpen={fileUploaderOpen}
        onClose={() => {
          const wasTargetingCurrent = fileUploadTargetFolderId === currentFolderId;
          setFileUploaderOpen(false);
          setFileUploadTargetFolderId(null);
          if (wasTargetingCurrent) loadFolderContent();
        }}
        mode="appFile"
        targetFolderId={fileUploadTargetFolderId ?? currentFolderId ?? undefined}
        onUploaded={() => showInfo('File uploaded.', true)}
        title="Upload Files"
        queueLimit={10}
      />

      {/* File row context menu (Data Model Design style) - items by category (Angular fileRow_is*Visible) */}
      {fileContextMenu.visible && fileContextMenu.item && (
        <div
          ref={fileContextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{
            left: fileContextMenu.x,
            top: fileContextMenu.y,
          }}
          onClick={(e) => e.stopPropagation()}
          role="menu"
        >
          {(() => {
            const item = fileContextMenu.item;
            const fileId = getFileIdFromItem(item);
            const closeMenu = () => setFileContextMenu({ visible: false, x: 0, y: 0, item: null });
            const v = fileRowVisible;
            return (
              <div className="py-1">
                {v.download && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { downloadFile(fileId); closeMenu(); }}
                  >
                    <i className="fa-solid fa-download mr-2 flex-shrink-0" aria-hidden />
                    Download
                  </button>
                )}
                {v.delete && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { moveToRecycleBin([fileId!]); closeMenu(); }}
                  >
                    <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden />
                    Move to Recycle Bin
                  </button>
                )}
                {v.cut && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { cutFile(fileId); closeMenu(); }}
                  >
                    <i className="fa-solid fa-scissors mr-2 flex-shrink-0" aria-hidden />
                    Cut
                  </button>
                )}
                {v.addFavorites && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { addToFavorites([fileId]); closeMenu(); }}
                  >
                    <i className="fa-solid fa-star mr-2 flex-shrink-0" aria-hidden />
                    Add to Favorites
                  </button>
                )}
                {v.share && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { openSharingEditor(item, false); closeMenu(); }}
                  >
                    <i className="fa-solid fa-share mr-2 flex-shrink-0" aria-hidden />
                    Share
                  </button>
                )}
                {v.notification && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { openSharingEditor(item, true); closeMenu(); }}
                  >
                    <i className="fa-solid fa-envelope mr-2 flex-shrink-0" aria-hidden />
                    Email / Notification
                  </button>
                )}
                {v.restoreRecycleBin && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { restoreFromRecycleBin([fileId]); closeMenu(); }}
                  >
                    <i className="fa-solid fa-share-from-square mr-2 flex-shrink-0" aria-hidden />
                    Restore from Recycle Bin
                  </button>
                )}
                {v.removeSharing && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { removeSharing([fileId]); closeMenu(); }}
                  >
                    <i className="fa-solid fa-xmark mr-2 flex-shrink-0" aria-hidden />
                    Remove Sharing
                  </button>
                )}
                {v.deleteFromRecycleBin && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { deleteFromRecycleBin([fileId]); closeMenu(); }}
                  >
                    <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden />
                    Delete from Recycle Bin
                  </button>
                )}
                {v.removeFavorites && fileId != null && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => { removeFromFavorites([fileId]); closeMenu(); }}
                  >
                    <i className="fa-solid fa-xmark mr-2 flex-shrink-0" aria-hidden />
                    Remove from Favorites
                  </button>
                )}
              </div>
            );
          })()}
        </div>
      )}

      {/* Sharing Editor Modal */}
      {showSharingEditor && sharingFileId && (
        <FileNavigationSharingEditor
          securityObjId={sharingFileId}
          sharingObjType="File"
          sharingObjName={sharingFileName}
          isNotification={isNotificationMode}
          transactionId={transactionId}
          onClose={() => { setShowSharingEditor(false); setIsNotificationMode(false); }}
          onSaved={loadFolderContent}
        />
      )}
    </div>
  );
};

export default FolderNavigation;
