/**
 * Application Helper - Reusable utility methods
 * Usage: import appHelper from './helper/appHelper';
 *        const id = appHelper.randomId();
 */

// Global DEBUG log control
// Set to true to enable all DEBUG logs, false to disable
// You can also control this via browser console: window.__DEBUG_ENABLED__ = true/false
let DEBUG_ENABLED = true;

// Check if DEBUG is enabled via window object (for runtime control)
if (typeof window !== 'undefined' && (window as any).__DEBUG_ENABLED__ !== undefined) {
  DEBUG_ENABLED = (window as any).__DEBUG_ENABLED__;
}

/**
 * Debug logging utility
 * Only logs when DEBUG_ENABLED is true
 * Usage: appHelper.debugLog('message', { data });
 * 
 * To enable/disable DEBUG logs:
 * 1. Set window.__DEBUG_ENABLED__ = true/false in browser console
 * 2. Or modify DEBUG_ENABLED constant in this file
 * 
 * @param {...any} args - Arguments to log (same as console.log)
 */
const debugLog = (...args: any[]): void => {
  if (DEBUG_ENABLED || (typeof window !== 'undefined' && (window as any).__DEBUG_ENABLED__)) {
    console.log('[DEBUG]', ...args);
  }
};

/**
 * Enable or disable DEBUG logging
 * @param {boolean} enabled - Whether to enable DEBUG logs
 */
const setDebugEnabled = (enabled: boolean): void => {
  DEBUG_ENABLED = enabled;
  if (typeof window !== 'undefined') {
    (window as any).__DEBUG_ENABLED__ = enabled;
  }
};

/**
 * Check if DEBUG logging is enabled
 * @returns {boolean} Whether DEBUG logging is enabled
 */
const isDebugEnabled = (): boolean => {
  return DEBUG_ENABLED || (typeof window !== 'undefined' && (window as any).__DEBUG_ENABLED__) || false;
};

/**
 * Generates a random ID with 4 uppercase letters followed by 2 numbers
 * Format: ABCD32
 * @returns {string} 6-character random ID (4 uppercase letters + 2 numbers)
 */
const randomId = (): string => {
  const uppercaseLetters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
  const numbers = '0123456789';
  let result = '';
  
  // Generate 4 uppercase letters
  for (let i = 0; i < 4; i++) {
    result += uppercaseLetters.charAt(Math.floor(Math.random() * uppercaseLetters.length));
  }
  
  // Generate 2 numbers
  for (let i = 0; i < 2; i++) {
    result += numbers.charAt(Math.floor(Math.random() * numbers.length));
  }
  
  return result;
};

/**
 * Generates a GUID/UUID (version 4)
 * Format: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx
 * @returns {string} GUID string
 */
const guid = (): string => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : ((r & 0x3) | 0x8);
    return v.toString(16);
  });
};

/**
 * Finds the maximum value from an array based on a property or mapper function
 * Similar to AngularJS appHelper.findMaxValueFromArray
 * @param {T[]} array - The array to search
 * @param {string | ((item: T) => number)} propertyOrMapper - Property name or mapper function
 * @param {number} defaultValue - Default value if array is empty
 * @returns {number} Maximum value found
 */
const findMaxValueFromArray = <T>(
  array: T[],
  propertyOrMapper: string | ((item: T) => number),
  defaultValue: number = 0
): number => {
  if (!array || array.length === 0) {
    return defaultValue;
  }

  const values = array.map((item: T) => {
    if (typeof propertyOrMapper === 'string') {
      return (item as any)[propertyOrMapper] || 0;
    } else {
      return propertyOrMapper(item) || 0;
    }
  });

  return Math.max(...values);
};

/**
 * Finds the minimum value from an array based on a property or mapper function
 * @param {T[]} array - The array to search
 * @param {string | ((item: T) => number)} propertyOrMapper - Property name or mapper function
 * @param {number} defaultValue - Default value if array is empty
 * @returns {number} Minimum value found
 */
const findMinValueFromArray = <T>(
  array: T[],
  propertyOrMapper: string | ((item: T) => number),
  defaultValue: number = 0
): number => {
  if (!array || array.length === 0) {
    return defaultValue;
  }

  const values = array.map((item: T) => {
    if (typeof propertyOrMapper === 'string') {
      return (item as any)[propertyOrMapper] || 0;
    } else {
      return propertyOrMapper(item) || 0;
    }
  });

  return Math.min(...values);
};

/**
 * Generates a unique ID using timestamp and random number
 * Format: temp_{timestamp}_{random}
 * @returns {string} Unique temporary ID
 */
const tempId = (): string => {
  return `temp_${Date.now()}_${Math.random()}`;
};

/**
 * Deep clones an object
 * @param {T} obj - Object to clone
 * @returns {T} Cloned object
 */
const deepClone = <T>(obj: T): T => {
  if (obj === null || typeof obj !== 'object') {
    return obj;
  }

  if (obj instanceof Date) {
    return new Date(obj.getTime()) as any;
  }

  if (obj instanceof Array) {
    return obj.map(item => deepClone(item)) as any;
  }

  if (typeof obj === 'object') {
    const clonedObj = {} as T;
    for (const key in obj) {
      if (obj.hasOwnProperty(key)) {
        clonedObj[key] = deepClone(obj[key]);
      }
    }
    return clonedObj;
  }

  return obj;
};

/**
 * Checks if a value is empty (null, undefined, empty string, empty array, empty object)
 * @param {any} value - Value to check
 * @returns {boolean} True if value is empty
 */
const isEmpty = (value: any): boolean => {
  if (value === null || value === undefined) {
    return true;
  }

  if (typeof value === 'string') {
    return value.trim().length === 0;
  }

  if (Array.isArray(value)) {
    return value.length === 0;
  }

  if (typeof value === 'object') {
    return Object.keys(value).length === 0;
  }

  return false;
};

/**
 * Debounce function - delays execution until after wait time has passed
 * @param {Function} func - Function to debounce
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} Debounced function
 */
const debounce = <T extends (...args: any[]) => any>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let timeout: NodeJS.Timeout | null = null;

  return function executedFunction(...args: Parameters<T>) {
    const later = () => {
      timeout = null;
      func(...args);
    };

    if (timeout) {
      clearTimeout(timeout);
    }
    timeout = setTimeout(later, wait);
  };
};

/**
 * Throttle function - limits execution to at most once per wait time
 * @param {Function} func - Function to throttle
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} Throttled function
 */
const throttle = <T extends (...args: any[]) => any>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let timeout: NodeJS.Timeout | null = null;
  let previous = 0;

  return function executedFunction(...args: Parameters<T>) {
    const now = Date.now();
    const remaining = wait - (now - previous);

    if (remaining <= 0 || remaining > wait) {
      if (timeout) {
        clearTimeout(timeout);
        timeout = null;
      }
      previous = now;
      func(...args);
    } else if (!timeout) {
      timeout = setTimeout(() => {
        previous = Date.now();
        timeout = null;
        func(...args);
      }, remaining);
    }
  };
};

type ClampMenuPositionToViewportParams = {
  x: number;
  y: number;
  menuWidth: number;
  menuHeight: number;
  margin?: number;
  viewportWidth?: number;
  viewportHeight?: number;
};

/**
 * Clamp a context menu position so it stays within viewport.
 * Useful for fixed-position context menus triggered by mouse clicks.
 */
/**
 * Global monotonic z-index for stacked modals (linked form popup, unit search, etc.).
 * Each call returns a value strictly greater than the previous, starting at 10000.
 * Stored on `window.__APP_NEXT_POPUP_Z__` so nested popups and Fast Refresh keep ordering sane.
 */
const POPUP_Z_INDEX_START = 10000;
let modulePopupZCursor = POPUP_Z_INDEX_START - 1;

const getNextPopupZIndex = (): number => {
  if (typeof window !== 'undefined') {
    const w = window as any;
    const stored = w.__APP_NEXT_POPUP_Z__;
    const cur =
      typeof stored === 'number' && Number.isFinite(stored) ? stored : POPUP_Z_INDEX_START - 1;
    const next = cur < POPUP_Z_INDEX_START ? POPUP_Z_INDEX_START : cur + 1;
    w.__APP_NEXT_POPUP_Z__ = next;
    modulePopupZCursor = next;
    return next;
  }
  modulePopupZCursor =
    modulePopupZCursor < POPUP_Z_INDEX_START ? POPUP_Z_INDEX_START : modulePopupZCursor + 1;
  return modulePopupZCursor;
};

const clampMenuPositionToViewport = ({
  x,
  y,
  menuWidth,
  menuHeight,
  margin = 8,
  viewportWidth,
  viewportHeight
}: ClampMenuPositionToViewportParams): { x: number; y: number } => {
  const vw =
    typeof viewportWidth === 'number'
      ? viewportWidth
      : typeof window !== 'undefined'
      ? window.innerWidth
      : 0;
  const vh =
    typeof viewportHeight === 'number'
      ? viewportHeight
      : typeof window !== 'undefined'
      ? window.innerHeight
      : 0;

  if (!vw || !vh) return { x, y };

  return {
    x: Math.max(margin, Math.min(x, vw - menuWidth - margin)),
    y: Math.max(margin, Math.min(y, vh - menuHeight - margin))
  };
};

const appHelper = {
  randomId,
  guid,
  tempId,
  findMaxValueFromArray,
  findMinValueFromArray,
  deepClone,
  isEmpty,
  debounce,
  throttle,
  clampMenuPositionToViewport,
  POPUP_Z_INDEX_START,
  getNextPopupZIndex,
  debugLog,
  setDebugEnabled,
  isDebugEnabled,
};

export default appHelper;
