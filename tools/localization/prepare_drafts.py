"""Optional, throttled authoring of review drafts; never changes the game catalog.

Uses the public Google Translate web endpoint, with no account credentials or
billed API project. Availability is not guaranteed. The game never invokes this
command or requires network translation. Cache files include source/provenance;
only a later explicit review/import step can promote them to authored sources.
"""
import argparse
import concurrent.futures
import hashlib
import json
import re
import time
import threading
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from catalog import ROOT, read_catalog, report, placeholders

TARGETS = dict(spanish='es', eurofrench='fr', brazilianportuguese='pt', german='de',
               italian='it', polish='pl', arabic='ar', turkish='tr', japanese='ja',
               korean='ko', mandarin='zh-CN', hebrew='iw', traditionalchinese='zh-TW',
               indonesian='id', vietnamese='vi', thai='th')
CACHE = ROOT / '.validation/localization-review'
NAMES = ('AXIOM-7', 'Axiom Force', 'Cyber Shogun', 'Kaito Kurogane', 'Surya Prime',
         'Arjun Varma', 'Oceanus', 'Kiana Vaea', 'Mirage Blade', 'Zayd Al-Mansoor',
         'Apex Titan', 'King Tariq Nzinga', 'Aura Phoenicia', 'Maya Khoury',
         'Kaito', 'Surya', 'Titan', 'Mirage', 'Aura', 'Vanguard-X', 'VTOL')
KEYS = r'(?:SHIFT|CTRL|ALT|ESC|SPACE|ENTER|START|TAB|Q|E|F|RT|RB|LT|LB|A|B|X|Y)'
TOKEN = (r'\{[^{}]+\}|\[(?:/?(?:center|color|b|i|u|font|url)(?:=[^\]]+)?|'
         + KEYS + r'(?:\s*/\s*' + KEYS + r')*|SEC:\s*OMEGA)\]|\r?\n|%\d*\$?[sdif]|(?<!\w)[-+]?\d+(?:[.,]\d+)*(?:%|m\b|M\b)?')
CONTROL_ONLY = {'[SHIFT] {0} ({1:0.0}s)', '[SEC: OMEGA]', '{0} / Tab', 'Y / LB', '[Q / RT]'}
LAST_REQUEST = 0.0
RATE_LOCK = threading.Lock()
STOP_REQUESTS = threading.Event()


def request_translation(text, language):
    """Serialize requests at one per second; access/rate refusals stop authoring rather than rerouting it."""
    global LAST_REQUEST
    with RATE_LOCK:
        if STOP_REQUESTS.is_set():
            raise RuntimeError('Authoring stopped after service refused a request')
        time.sleep(max(0.0, 1.0 - (time.monotonic() - LAST_REQUEST)))
        LAST_REQUEST = time.monotonic()
    url = 'https://translate.googleapis.com/translate_a/single?' + urllib.parse.urlencode(
        dict(client='gtx', sl='en', tl=TARGETS[language], dt='t', q=text))
    request = urllib.request.Request(url, headers={'User-Agent': 'AxiomForce-Localization-Authoring/1.0'})
    try:
        with urllib.request.urlopen(request, timeout=40) as response:
            result = json.load(response)
    except urllib.error.HTTPError as error:
        if error.code in (403, 429):
            STOP_REQUESTS.set()
        raise
    return ''.join(part[0] for part in result[0] if part[0])


def protect(source, names):
    """Keep numbers, input bindings, line breaks, markup and known display identities outside translation."""
    values = []
    pattern = re.compile('|'.join(re.escape(name) for name in sorted(names, key=len, reverse=True)) + '|' + TOKEN)

    def substitute(match):
        """Retain the exact literal or its existing locale-specific proper-name spelling."""
        values.append(names.get(match.group(), match.group()))
        return f'ZXP{len(values)-1:04d}XZ'

    return pattern.sub(substitute, source), values


def restore(draft, values):
    """Require exactly one of every protected marker; restore whitespace only after trimming the response."""
    draft = draft.strip()
    for index, value in enumerate(values):
        marker = f'ZXP{index:04d}XZ'
        expression = re.compile(r'\s*'.join(map(re.escape, marker)), re.I)
        if len(expression.findall(draft)) != 1:
            raise ValueError(f'Protected token {index} changed')
        draft = expression.sub(lambda _: value, draft)
    if re.search(r'ZX[QP]\s*\d', draft, re.I):
        raise ValueError('Unresolved draft marker')
    return draft


def cache_path(language):
    """Return the ignored checkpoint path for one locale."""
    return CACHE / (language + '.json')


def checkpoint(language, drafts):
    """Atomically retain complete batches so an interrupted run never repeats accepted network requests."""
    CACHE.mkdir(parents=True, exist_ok=True)
    path = cache_path(language)
    temporary = path.with_suffix('.tmp')
    temporary.write_text(json.dumps(drafts, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    temporary.replace(path)


def fetch_locale(language, sources, rows, limit):
    """Prepare small numbered batches; malformed batch results fall back to individually validated strings."""
    path = cache_path(language)
    drafts = json.loads(path.read_text(encoding='utf-8')) if path.exists() else {}
    known = {row['english']: row[language] for row in rows if row.get(language, '').strip()}
    names = {name: known.get(name, name) for name in NAMES}
    glossary = json.loads(Path(__file__).with_name('glossary.json').read_text(encoding='utf-8'))
    names.update(glossary[language])
    names.update({name.upper(): value for name, value in list(names.items())})
    pending = [source for source in sources if source not in drafts]
    if limit:
        pending = pending[:limit]
    batches, batch, size = [], [], 0
    for source in pending:
        if source in CONTROL_ONLY:
            drafts[source] = dict(text=source, source_sha256=hashlib.sha256(source.encode()).hexdigest(),
                                  provider='literal control/format contract', review='no translatable prose')
            continue
        text, values = protect(source, names)
        if batch and size + len(text) > 1600:
            batches.append(batch)
            batch, size = [], 0
        batch.append((source, text, values))
        size += len(text) + 16
    if batch:
        batches.append(batch)
    for number, batch in enumerate(batches, 1):
        query = '\n'.join(f'ZXQ{index:04d}ZX {text}' for index, (_, text, _) in enumerate(batch))
        answer = request_translation(query, language)
        parts = re.split(r'ZXQ\s*(\d{4})\s*ZX\s*', answer, flags=re.I)
        entries = {int(parts[index]): parts[index+1].strip() for index in range(1, len(parts)-1, 2)}
        for index, (source, text, values) in enumerate(batch):
            try:
                translated = restore(entries.get(index, ''), values)
                if not meaningful(source, translated, names) or placeholders(translated) != placeholders(source):
                    raise ValueError('Format contract changed')
            except ValueError:
                try:
                    translated = restore(request_translation(text, language), values)
                    if not meaningful(source, translated, names) or placeholders(translated) != placeholders(source):
                        raise ValueError('Standalone format/meaning contract changed')
                except ValueError as error:
                    # Leave this cell empty for explicit authoring. One rejected
                    # draft must not prevent unrelated locales from progressing.
                    print(f'REVIEW_REQUIRED {language}: {source!r}: {error}', flush=True)
                    continue
            drafts[source] = dict(text=translated, source_sha256=hashlib.sha256(source.encode()).hexdigest(),
                                  provider='Google Translate public web endpoint', review='unreviewed authoring draft')
        checkpoint(language, drafts)
        print(f'{language}: {len(drafts)} source drafts; batch {number}/{len(batches)}', flush=True)
    return len(drafts)


def meaningful(source, translated, names):
    """Reject dropped leading labels/clauses and symbol-only results when the source still contains prose."""
    if source and source[0].isalnum() and translated.lstrip().startswith((':', '.', ';', '。', '،')):
        return False
    protected, _ = protect(source, names)
    prose = re.sub(r'ZXP\d{4}XZ', '', protected)
    return bool(translated.strip()) and (not any(char.isalpha() for char in prose)
        or any(char.isalpha() for char in re.sub(TOKEN, '', translated)))


def main():
    """Require an explicit network-authoring flag; default invocation displays usage and changes nothing."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fetch-drafts', action='store_true')
    parser.add_argument('--language', choices=list(TARGETS), action='append')
    parser.add_argument('--limit', type=int, default=0)
    args = parser.parse_args()
    if not args.fetch_drafts:
        parser.print_help()
        return
    data = report()
    _, rows = read_catalog()
    # Overlap network latency for two independent locale files while the shared
    # lock still limits request starts to one per second across the whole process.
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        futures = []
        for language in args.language or TARGETS:
            missing = set(data['languages'][language]['missing'])
            sources = sorted({entry['english'] for entry in data['entries'] if entry['key'] in missing})
            futures.append(pool.submit(fetch_locale, language, sources, rows, args.limit))
        for future in concurrent.futures.as_completed(futures):
            print('Locale drafts ready:', future.result(), flush=True)


if __name__ == '__main__':
    main()
