/** Transient credentials. Reloading the document deliberately signs out the user. */
export const tokens = {
  access: null,
  refresh: null,
  generation: 0,
  /** A newly authenticated identity invalidates outstanding work from the old pair. */
  install(access, refresh) {
    this.generation += 1;
    this.access = access;
    this.refresh = refresh;
  },
  /** Removes credentials from memory without accessing browser storage. */
  clear() {
    this.generation += 1;
    this.access = null;
    this.refresh = null;
  },
};
