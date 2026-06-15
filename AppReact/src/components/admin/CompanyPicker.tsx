import React, { useState } from 'react';
import { adminSvc } from '../../webapi/adminsvc';

interface CompanyOption {
  Id: any;
  Display: string;
  UserDefinedValue1: string;
}

interface CompanyPickerProps {
  companies: CompanyOption[];
  sessionId: string;
  onSelected: () => void;
}

const CompanyPicker: React.FC<CompanyPickerProps> = ({ companies, sessionId, onSelected }) => {
  const [selectedId, setSelectedId] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleConfirm = async () => {
    if (selectedId == null) return;
    setLoading(true);
    setError(null);
    try {
      await adminSvc.selectCompany(sessionId, Number(selectedId));
      onSelected();
    } catch {
      setError('Failed to select company. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 flex items-center justify-center z-50 bg-black/40 backdrop-blur-sm">
      <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-sm mx-4">
        <h2 className="text-lg font-semibold text-gray-900 mb-1">Select Company</h2>
        <p className="text-sm text-gray-500 mb-4">Your account belongs to multiple companies. Choose one to continue.</p>
        <div className="space-y-2 mb-4">
          {companies.map((c) => (
            <button
              key={String(c.Id)}
              type="button"
              onClick={() => setSelectedId(c.Id)}
              className={`w-full text-left px-4 py-2.5 rounded-[4px] border text-sm transition-colors ${
                selectedId === c.Id
                  ? 'border-blue-500 bg-blue-50 text-blue-800'
                  : 'border-gray-200 hover:border-gray-400 text-gray-800'
              }`}
            >
              <span className="font-medium">{c.Display}</span>
              {c.UserDefinedValue1 && (
                <span className="ml-2 text-xs text-gray-400">{c.UserDefinedValue1}</span>
              )}
            </button>
          ))}
        </div>
        {error && <div className="text-red-500 text-sm mb-3">{error}</div>}
        <button
          type="button"
          onClick={handleConfirm}
          disabled={selectedId == null || loading}
          className="w-full py-2 px-4 border border-transparent rounded-[4px] shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? 'Confirming...' : 'Continue'}
        </button>
      </div>
    </div>
  );
};

export default CompanyPicker;
