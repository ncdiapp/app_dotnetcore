import { adminSvc } from '../webapi/adminsvc';
import { store } from '../redux/store';
import { setUserMenu } from '../redux/features/admin/userSessionSlice';

/** Reload left-sidebar / Application Configuration menus from the server. */
export async function refreshUserTreeMenu(): Promise<void> {
  const userMenu = await adminSvc.retrieveUserTreeMenu();
  if (Array.isArray(userMenu)) {
    store.dispatch(setUserMenu(userMenu));
  }
}
