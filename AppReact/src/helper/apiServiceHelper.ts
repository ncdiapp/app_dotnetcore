import { store, RootState } from '../redux/store';

export const getHeaders = () => {
        const userContext = (store.getState() as RootState).userSession.userContext;

    const headers = new Headers();
    headers.append('Content-Type', 'application/json');

    if (userContext?.SessionId) {
        headers.append('CurrentUserSessionId', userContext.SessionId);
    }

    return headers;
}


export const getHeadersWithAuth = (authValue?: string) => {
    if (authValue) {
        return {
            'Authorization': authValue,
        };
    }
}