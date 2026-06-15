/**
 * File Management page – migrated from AngularJS File Management / FolderNavigation.
 * Renders breadcrumb and delegates to the shared FolderNavigation component for system file storage
 * (My Company, My Private, Favorites, etc.). Used by the /file-management route and by sidebar/list
 * menu when opening File Management or transaction folder navigation.
 *
 * FolderNavigation is shared here and can be reused elsewhere (e.g. embedded file picker) by passing
 * transactionId, defaultCategoryId, initialFolderId, and isFileMgt / isUseAsSelector as needed.
 * Transaction ID comes from userContext.DictAppSetup.SystemDefinedFileTransactionId or from route param.
 */
import React, { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { useSelector } from 'react-redux';
import type { RootState } from '../../redux/store';
import { useTheme } from '../../redux/hooks/useTheme';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { FolderNavigation } from '../folderNavigation';

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

const CATEGORY_BREADCRUMB_KEYS: (keyof typeof FALLBACK_FILE_FOLDER_CATEGORIES)[] = [
  'MyRecentlyFiles', 'Favorites', 'Company', 'Private', 'Public', 'ShareToOthers', 'SharedToMe', 'MyRecycleBin',
];
const CATEGORY_BREADCRUMB_META: Record<string, string> = {
  MyRecentlyFiles: 'My Recently Files',
  Favorites: 'My Favorites',
  Company: 'My Company',
  Private: 'My Private',
  Public: 'Public',
  ShareToOthers: 'Share To Others',
  SharedToMe: 'Share To Me Folder',
  MyRecycleBin: 'My Recycle Bin',
};

const FileManagement: React.FC = () => {
  const { param } = useParams<{ param?: string }>();
  const { theme } = useTheme();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const fileFolderCategoryEnum = useEnumValues('EmAppFileFolderCategory');
  const FILE_FOLDER_CATEGORIES = useMemo(
    () => fileFolderCategoryEnum ?? FALLBACK_FILE_FOLDER_CATEGORIES,
    [fileFolderCategoryEnum]
  );
  const CATEGORY_BREADCRUMB = useMemo(() => {
    const d: Record<number, string> = {};
    CATEGORY_BREADCRUMB_KEYS.forEach((k) => {
      const v = FILE_FOLDER_CATEGORIES[k];
      if (v != null) d[v] = CATEGORY_BREADCRUMB_META[k];
    });
    return d;
  }, [FILE_FOLDER_CATEGORIES]);

  const { transactionId, defaultCategoryId, initialFolderId } = useMemo(() => {
    let id: string | number | null = null;
    let categoryId: number = FILE_FOLDER_CATEGORIES.Company;
    let folderId: string | number | null = null;

    if (param) {
      try {
        const decoded = decodeURIComponent(param);
        const obj = JSON.parse(decoded);
        if (obj.transactionId != null) id = obj.transactionId;
        if (obj.defaultCategoryId != null) categoryId = Number(obj.defaultCategoryId);
        else if (obj.id != null && Number(obj.id) >= 1 && Number(obj.id) <= 9) categoryId = Number(obj.id);
        if (obj.initialFolderId != null) folderId = obj.initialFolderId;
      } catch {
        if (param) id = param;
      }
    }

    if (id == null && userContext?.DictAppSetup?.SystemDefinedFileTransactionId != null) {
      id = userContext.DictAppSetup.SystemDefinedFileTransactionId;
    }

    return {
      transactionId: id,
      defaultCategoryId: categoryId,
      initialFolderId: folderId,
    };
  }, [param, userContext, FILE_FOLDER_CATEGORIES]);

  const breadcrumbCategoryName = CATEGORY_BREADCRUMB[defaultCategoryId] ?? 'My Company';

  if (transactionId == null) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
        <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>
            File Management / {breadcrumbCategoryName}
          </div>
        </div>
        <div className={`w-full h-1 flex-auto overflow-hidden px-4 py-4 ${theme.mainContentSection}`}>
          <p className={`text-sm ${theme.label}`}>
            File management is not configured. SystemDefinedFileTransactionId is missing.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
     
      <div className={`w-full h-full overflow-hidden`}>
        <FolderNavigation
          transactionId={transactionId}
          defaultCategoryId={defaultCategoryId}
          initialFolderId={initialFolderId}
          isFileMgt={true}
          isUseAsSelector={false}
        />
      </div>
    </div>
  );
};

export default FileManagement;
