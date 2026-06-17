import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { adminSvc } from '../../../webapi/adminsvc';

export type EnumDictionary = Record<string, Record<string, number>>;

interface UserSessionState {
  isAuthenticated: boolean;
  userContext: any | null;
  userMenu: any[] | null;
  enumDictionary: EnumDictionary | null;
  loading: boolean;
  error: string | null;
}

const initialState: UserSessionState = {
  isAuthenticated: false,
  userContext: null,
  userMenu: null,
  enumDictionary: null,
  loading: false,
  error: null
};

// Login function
export const login = createAsyncThunk<
  { userContext: any; enumDictionary: EnumDictionary | null },
  { userName: string; password: string; callBack: () => void; preloadedResponse?: any },
  { rejectValue: string }
>(
  'userSession/login',
  async ({ userName, password, callBack, preloadedResponse }, { rejectWithValue }) => {
    try {
      const response = preloadedResponse ?? await adminSvc.mgtLogin(userName, password);
      if (response.IsLoginFailed) {
        return rejectWithValue(response.LoginFailedErroMessage);
      }

      let enumDictionary: EnumDictionary | null = response?.DictEnumApp ?? null;
      let resolvedContext: any = response;

      if (response?.SessionId) {
        try {
          const contextFromServer = await adminSvc.getUserContextBySessionId(response.SessionId);
          if (contextFromServer) {
            resolvedContext = contextFromServer;
            enumDictionary = (contextFromServer.DictEnumApp as EnumDictionary) ?? enumDictionary;
          }
        } catch (contextError) {
          console.warn('Failed to retrieve full user context. Falling back to login payload.', contextError);
        }
      }

      if (response.token) {
        localStorage.setItem('token', response.token);
      }

      // Save SessionId to localStorage for session restoration on page refresh
      if (resolvedContext?.SessionId) {
        localStorage.setItem('sessionId', resolvedContext.SessionId);
      }

      callBack();

      return {
        userContext: resolvedContext,
        enumDictionary,
      };
    } catch (error) {
      return rejectWithValue('Login failed. Please check your credentials.');
    }
  }
);

// Restore session from localStorage on page refresh
let _restoreSessionInFlight: Promise<{ userContext: any; enumDictionary: EnumDictionary | null } | null> | null = null;
export const restoreSession = createAsyncThunk<
  { userContext: any; enumDictionary: EnumDictionary | null } | null,
  void,
  { rejectValue: string }
>(
  'userSession/restoreSession',
  async (_, { rejectWithValue, getState }) => {
    // If we already have a userContext, don't re-restore (prevents request storms).
    try {
      const state: any = getState();
      if (state?.userSession?.userContext && !state.userSession.userContext?.IsLoginFailed) {
        return {
          userContext: state.userSession.userContext,
          enumDictionary: (state.userSession.enumDictionary as EnumDictionary) ?? null,
        };
      }
    } catch {
      // ignore
    }

    // De-dupe concurrent restoreSession dispatches.
    if (_restoreSessionInFlight) {
      return _restoreSessionInFlight;
    }

    _restoreSessionInFlight = (async () => {
    try {
      const sessionId = localStorage.getItem('sessionId');
      if (!sessionId) {
        return null; // No saved session
      }

      // Verify session is still valid and get user context
      const contextFromServer = await adminSvc.getUserContextBySessionId(sessionId);
      if (!contextFromServer || contextFromServer.IsLoginFailed) {
        // Session is invalid, clear it
        localStorage.removeItem('sessionId');
        return null;
      }

      const enumDictionary = (contextFromServer.DictEnumApp as EnumDictionary) ?? null;

      return {
        userContext: contextFromServer,
        enumDictionary,
      };
    } catch (error: any) {
      // Session is invalid or expired, clear it
      localStorage.removeItem('sessionId');
      return null;
    } finally {
      _restoreSessionInFlight = null;
    }
    })();

    return _restoreSessionInFlight;
  }
  ,
  {
    // Don't dispatch restoreSession again while one is already pending.
    condition: (_, { getState }) => {
      try {
        const state: any = getState();
        if (state?.userSession?.loading) return false;
      } catch {
        // ignore
      }
      return true;
    },
  }
);

const userSessionSlice = createSlice({
  name: 'userSession',
  initialState,
  reducers: {
    logout: (state) => {
      state.isAuthenticated = false;
      state.userContext = null;
      state.userMenu = null;
      state.enumDictionary = null;
      state.error = null;
      state.loading = false;
      localStorage.removeItem('token');
      localStorage.removeItem('sessionId');
    },
    clearError: (state) => {
      state.error = null;
    },
    setUserContext: (state, action: PayloadAction<any>) => {
      state.userContext = action.payload;
      state.isAuthenticated = true;
    },
    setUserMenu: (state, action: PayloadAction<any[]>) => {
      state.userMenu = action.payload;
    },
    setEnumDictionary: (state, action: PayloadAction<EnumDictionary | null>) => {
      state.enumDictionary = action.payload;
    }
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.loading = false;
        state.isAuthenticated = true;
        state.userContext = action.payload.userContext;
        state.enumDictionary = action.payload.enumDictionary ?? null;
        state.error = null;
      })
      .addCase(login.rejected, (state, action) => {
        state.loading = false;
        state.isAuthenticated = false;
        state.userContext = null;
        state.error = action.payload as string;
      })
      .addCase(restoreSession.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(restoreSession.fulfilled, (state, action) => {
        state.loading = false;
        if (action.payload) {
          state.isAuthenticated = true;
          state.userContext = action.payload.userContext;
          state.enumDictionary = action.payload.enumDictionary ?? null;
          state.error = null;
        } else {
          // No valid session found
          state.isAuthenticated = false;
          state.userContext = null;
          state.enumDictionary = null;
        }
      })
      .addCase(restoreSession.rejected, (state) => {
        state.loading = false;
        state.isAuthenticated = false;
        state.userContext = null;
        state.enumDictionary = null;
      });
  },
});

export const { logout, clearError, setUserContext, setUserMenu, setEnumDictionary } = userSessionSlice.actions;
export default userSessionSlice.reducer; 