/**
 * Tests for Transaction Unit Editor flows: Unit Options popup, Link Target editor, Delete Flow, Linked Search.
 */
import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import UnitAdvancedOptionsPopup from './UnitAdvancedOptionsPopup';
import TransactionUnitLinkTargetEditor from './TransactionUnitLinkTargetEditor';
import TransactionUnitLinkedSearchEditor from './TransactionUnitLinkedSearchEditor';

// Mock theme and feedback hooks so components render without Redux
jest.mock('../../../redux/hooks/useTheme', () => ({
  useTheme: () => ({
    theme: {
      mainContentSection: 'bg-white',
      title: 'text-gray-900',
      label: 'text-gray-600',
      button_default: 'border rounded',
      inputBox: 'border rounded px-2',
    },
  }),
}));

jest.mock('../../../redux/hooks/useErrorMessage', () => ({
  useErrorMessage: () => ({
    showError: jest.fn(),
    showInfo: jest.fn(),
    showValidationMessages: jest.fn(),
  }),
}));

jest.mock('react-redux', () => ({
  useDispatch: () => jest.fn(),
}));

jest.mock('../../../webapi/apptransactionsvc', () => ({
  appTransactionService: {
    retrieveOneAppTransactionUnitExDto: jest.fn().mockResolvedValue({ AppTransactionFieldList: [] }),
    retrieveOneTransactionUnitLinkTargetList: jest.fn().mockResolvedValue([]),
    retrieveAllAppTransactions: jest.fn().mockResolvedValue([]),
    retrieveOneAppTransactionUnitLinkedSearchExDto: jest.fn().mockResolvedValue(null),
    saveAppTransactionUnitLinkedSearchExDto: jest.fn().mockResolvedValue({ IsSuccessful: true }),
  },
}));

jest.mock('../../../webapi/adminsvc', () => ({
  adminSvc: {
    getMassEntitiesLookupItem: jest.fn().mockResolvedValue({ AppSearch: [], AppSearchSaved: [], AppSearchView: [] }),
  },
}));

describe('Transaction Unit Editor flows', () => {
  describe('UnitAdvancedOptionsPopup', () => {
    const defaultProps = {
      isOpen: true,
      unitData: {
        IsDisableAddButton: false,
        IsDisableDeleteButton: false,
        IsPrimaryKeyIdentityInsert: false,
        IsExclusiveForOwner: false,
      },
      hasParent: false,
      onClose: jest.fn(),
      onPropertyChange: jest.fn(),
    };

    it('renders when open and shows Unit Options title', () => {
      render(<UnitAdvancedOptionsPopup {...defaultProps} />);
      expect(screen.getByText('Unit Options')).toBeInTheDocument();
    });

    it('shows Is Identity Insert and Is Owner Exclusive checkboxes', () => {
      render(<UnitAdvancedOptionsPopup {...defaultProps} />);
      expect(screen.getByText('Is Identity Insert')).toBeInTheDocument();
      expect(screen.getByText('Is Owner Exclusive')).toBeInTheDocument();
    });

    it('shows Add/Delete button options when hasParent is true', () => {
      render(<UnitAdvancedOptionsPopup {...defaultProps} hasParent={true} />);
      expect(screen.getByText('Is Disable Add Button')).toBeInTheDocument();
      expect(screen.getByText('Is Disable Delete Button')).toBeInTheDocument();
    });

    it('does not render when isOpen is false', () => {
      const { container } = render(<UnitAdvancedOptionsPopup {...defaultProps} isOpen={false} />);
      expect(container.firstChild).toBeNull();
    });

    it('renders Ok button', () => {
      render(<UnitAdvancedOptionsPopup {...defaultProps} />);
      expect(screen.getByRole('button', { name: /ok/i })).toBeInTheDocument();
    });
  });

  describe('TransactionUnitLinkTargetEditor', () => {
    it('returns null when isOpen is false', () => {
      const { container } = render(
        <TransactionUnitLinkTargetEditor
          isOpen={false}
          transactionUnitId={1}
          linkTargetUsageType={101}
          title="Unit Navigate To Data Model"
          onClose={jest.fn()}
        />
      );
      expect(container.firstChild).toBeNull();
    });

    it('renders when open with Unit title and Refresh/Save/Close', () => {
      render(
        <TransactionUnitLinkTargetEditor
          isOpen={true}
          transactionUnitId={1}
          linkTargetUsageType={101}
          title="Unit Navigate To Data Model"
          onClose={jest.fn()}
        />
      );
      expect(screen.getByText(/Navigate To Data Model Menus/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /refresh/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /save/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /close/i })).toBeInTheDocument();
    });
  });

  describe('TransactionUnitLinkedSearchEditor', () => {
    it('returns null when isOpen is false', () => {
      const { container } = render(
        <TransactionUnitLinkedSearchEditor
          isOpen={false}
          transactionUnitId={1}
          onClose={jest.fn()}
          onSaved={jest.fn()}
        />
      );
      expect(container.firstChild).toBeNull();
    });

    it('renders when open with header and Save/Cancel', async () => {
      render(
        <TransactionUnitLinkedSearchEditor
          isOpen={true}
          transactionUnitId={1}
          onClose={jest.fn()}
          onSaved={jest.fn()}
        />
      );
      expect(screen.getByText(/New Linked Search|Edit Linked Search/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /save/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
      await waitFor(() => expect(screen.queryByText('Loading...')).not.toBeInTheDocument(), { timeout: 2000 });
    });
  });
});
