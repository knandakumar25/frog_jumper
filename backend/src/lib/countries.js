/**
 * Country data (the REST Countries dataset, 250 entries, about 788 KB), loaded once per process.
 *
 * Only the common English name (`name.common`) is stored on accounts: the website's account page
 * offers `<option value={c.name.common}>`, and onboarding accepts that vocabulary and nothing
 * else, so the column never mixes names with codes.
 */
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const FILE = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', '..', 'data', 'countries.json');

let full = null;
let basic = null;
let names = null;

/** @returns {object[]} the full dataset (the website's account page needs `idd` dial codes) */
export function loadCountries() {
  full ??= JSON.parse(readFileSync(FILE, 'utf8'));
  return full;
}

/** @returns {Set<string>} accepted `name.common` values */
export function countryCommonNames() {
  names ??= new Set(loadCountries().filter((c) => c?.name?.common).map((c) => c.name.common));
  return names;
}

/** @returns {{cca3: string, name: {common: string}, flag: string}[]} the small list for onboarding */
export function basicCountries() {
  basic ??= loadCountries().filter((c) => c?.name?.common).map((c) => ({
    cca3: c.cca3 ?? '',
    name: { common: c.name.common },
    flag: c.flag ?? '',
  }));
  return basic;
}
