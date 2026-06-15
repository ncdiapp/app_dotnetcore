import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export enum MessageType {
  Error = 1,
  Warning = 2,
  Message = 3
}

export interface ErrorMessage {
  id: string;
  message: string;
  timestamp: number;
  type?: MessageType;
}

interface ErrorMessageState {
  messages: ErrorMessage[];
  isPopupOpen: boolean;
}

const initialState: ErrorMessageState = {
  messages: [],
  isPopupOpen: false,
};

const errorMessageSlice = createSlice({
  name: 'errorMessage',
  initialState,
  reducers: {
    addErrorMessage: (state, action: PayloadAction<{ message: string; type: MessageType }>) => {
      const newMessage: ErrorMessage = {
        message: action.payload.message,
        type: action.payload.type,
        id: Date.now().toString() + Math.random().toString(36).substr(2, 9),
        timestamp: Date.now(),
      };
      state.messages.push(newMessage);
    },
    clearAllMessages: (state) => {
      state.messages = [];
      state.isPopupOpen = false;
    },
    removeMessage: (state, action: PayloadAction<string>) => {
      state.messages = state.messages.filter(msg => msg.id !== action.payload);
    },
    togglePopup: (state) => {
      state.isPopupOpen = !state.isPopupOpen;
    },
    setPopupOpen: (state, action: PayloadAction<boolean>) => {
      state.isPopupOpen = action.payload;
    },
  },
});

export const {
  addErrorMessage,
  clearAllMessages,
  removeMessage,
  togglePopup,
  setPopupOpen,
} = errorMessageSlice.actions;

export default errorMessageSlice.reducer; 