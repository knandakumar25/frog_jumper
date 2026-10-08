"""Country data, loaded once per process.

countries.json is the REST Countries dataset: 250 entries, each carrying the UN
M49 numeric code as `ccn3` plus `translations` in 24 languages. At 788 KB it is
far too large to re-open and re-parse per request, which is what CountryListView
used to do on every GET.

Only the "common name" vocabulary means anything to this project: Account.jsx
renders `<option value={c.name.common}>`, so `User.country` already holds
"Japan" rather than "JP" or "JPN". Onboarding accepts the same vocabulary and
nothing else -- accepting codes too would leave the column a mix of two formats
and force every consumer to carry a translation table.
"""

import functools
import json
import os

_COUNTRIES_PATH = os.path.join(os.path.dirname(__file__), 'countries.json')


@functools.lru_cache(maxsize=1)
def load_countries():
    """Return the full parsed country list, cached for the process lifetime."""
    with open(_COUNTRIES_PATH, 'r', encoding='utf-8') as handle:
        return json.load(handle)


@functools.lru_cache(maxsize=1)
def country_common_names():
    """Return the set of accepted `name.common` values."""
    return frozenset(
        entry['name']['common']
        for entry in load_countries()
        if entry.get('name', {}).get('common')
    )


@functools.lru_cache(maxsize=1)
def basic_countries():
    """The lean projection the onboarding page needs (~15 KB, not 788 KB)."""
    return [
        {
            'cca3': entry.get('cca3', ''),
            'name': {'common': entry['name']['common']},
        }
        for entry in load_countries()
        if entry.get('name', {}).get('common')
    ]
