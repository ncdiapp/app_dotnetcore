/**
 * Example usage of Alert and Confirm components
 * 
 * These components can be used on any page/component throughout the app.
 * 
 * Usage:
 * 
 * 1. Import the hook:
 *    import { useAlertConfirm } from '../../components/common/AlertConfirmProvider';
 * 
 * 2. In your component:
 *    const { showAlert, showConfirm } = useAlertConfirm();
 * 
 * 3. Show an alert (simple message with OK button):
 *    await showAlert('This is an alert message');
 *    // or with options:
 *    await showAlert('Custom title alert', { title: 'Custom Title', okLabel: 'Got it' });
 * 
 * 4. Show a confirmation dialog:
 *    const confirmed = await showConfirm('Do you want to proceed?');
 *    if (confirmed) {
 *      // User clicked Yes
 *      console.log('User confirmed');
 *    } else {
 *      // User clicked No or closed the dialog
 *      console.log('User cancelled');
 *    }
 * 
 *    // With custom options:
 *    const confirmed = await showConfirm(
 *      'Are you sure you want to delete this item?',
 *      {
 *        title: 'Delete Confirmation',
 *        confirmLabel: 'Delete',
 *        cancelLabel: 'Cancel',
 *        confirmButtonStyle: 'bg-red-500 hover:bg-red-600 text-white'
 *      }
 *    );
 * 
 * Both showAlert and showConfirm return Promises, so you can use async/await
 * or .then() to handle the results.
 */

export {};

