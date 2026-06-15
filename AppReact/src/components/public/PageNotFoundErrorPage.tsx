import React from 'react';
import { useTheme } from '../../redux/hooks/useTheme';

const PageNotFoundErrorPage: React.FC = () => {
  const { theme } = useTheme();
  return (
    <div className="w-full">
      <div className="flex items-center justify-between mb-6">
        <div className={`text-xl font-semibold ${theme.title}`}>Page Not Found</div>
        
      </div>


      <div className="space-y-5">

        <div className={`rounded-lg p-5 ${theme.mainContentSection}`}>
          <h2 className={`text-md font-semibold ${theme.title} mb-4`}>Page Not Found</h2>
          <h3 className={`text-[15px] font-semibold ${theme.title} mb-3`}>
            Page Not Found
          </h3>
          <p className={` text-sm`}>Page Not Found</p>
        </div>


      </div>
    </div>
  );
};

export default PageNotFoundErrorPage; 