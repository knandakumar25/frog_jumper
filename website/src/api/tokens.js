/** Transient credentials. Reloading the document deliberately signs out the user. */
export const tokens = {
  access: null,
  refresh: null,
  /** Removes credentials from memory without accessing browser storage. */
  clear() {
    this.access = null;
    this.refresh = null;
  },
};
