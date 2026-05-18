#!/usr/bin/env python3
"""
translate_all.py
Translates english.json into all 48 language files using the
unofficial Google Translate endpoint (no API key required).

Usage:
    python translate_all.py                          # all languages
    python translate_all.py spanish german japanese  # specific languages
    python translate_all.py --force                  # re-translate existing files
"""

import sys
import json
import time
import copy
import urllib.request
import urllib.parse
from pathlib import Path

# ---------------------------------------------------------------------------
# Language map: filename stem → Google Translate language code
# ---------------------------------------------------------------------------
LANG_MAP = {
    "arabic":              "ar",
    "armenian":            "hy",
    "azerbaijani":         "az",
    "bengali":             "bn",
    "brazilianportuguese": "pt",       # Google defaults pt to Brazilian
    "canadianfrench":      "fr",       # no distinct fr-CA in gtx endpoint
    "czech":               "cs",
    "danish":              "da",
    "dutch":               "nl",
    "estonian":            "et",
    "eurofrench":          "fr",
    "europortuguese":      "pt-PT",
    "filipino":            "tl",
    "finnish":             "fi",
    "georgian":            "ka",
    "german":              "de",
    "greek":               "el",
    "gujarati":            "gu",
    "hebrew":              "iw",
    "hindi":               "hi",
    "hungarian":           "hu",
    "icelandic":           "is",
    "indonesian":          "id",
    "italian":             "it",
    "japanese":            "ja",
    "kannada":             "kn",
    "korean":              "ko",
    "latvian":             "lv",
    "lithuanian":          "lt",
    "malay":               "ms",
    "malayalam":           "ml",
    "mandarin":            "zh-CN",
    "marathi":             "mr",
    "norwegian":           "no",
    "polish":              "pl",
    "punjabi":             "pa",
    "romanian":            "ro",
    "sinhala":             "si",
    "slovak":              "sk",
    "slovenian":           "sl",
    "spanish":             "es",
    "swedish":             "sv",
    "tamil":               "ta",
    "telugu":              "te",
    "thai":                "th",
    "turkish":             "tr",
    "ukrainian":           "uk",
    "vietnamese":          "vi",
}

# Separator unlikely to appear in dialogue or be altered by translation
SEP = "\n<<<>>>\n"

DIR = Path(__file__).parent


# ---------------------------------------------------------------------------
# HTTP helper
# ---------------------------------------------------------------------------
def https_get(url: str) -> str:
    req = urllib.request.Request(
        url,
        headers={
            "User-Agent": "Mozilla/5.0",
            "Accept-Encoding": "identity",
        },
    )
    with urllib.request.urlopen(req, timeout=15) as resp:
        return resp.read().decode("utf-8")


# ---------------------------------------------------------------------------
# Translation helpers
# ---------------------------------------------------------------------------
def translate_batch(texts: list[str], tl: str) -> list[str]:
    """Translate a batch of strings, using SEP to join/split them."""
    if not texts:
        return []

    joined = SEP.join(texts)
    encoded = urllib.parse.quote(joined)
    url = (
        f"https://translate.googleapis.com/translate_a/single"
        f"?client=gtx&sl=en&tl={tl}&dt=t&q={encoded}"
    )
    try:
        raw = https_get(url)
        parsed = json.loads(raw)
        translated = "".join(
            s[0] for s in parsed[0] if s and s[0]
        )
        parts = translated.split("<<<>>>")
        clean = [p.strip("\n") for p in parts]
        if len(clean) != len(texts):
            print(
                f"\n    WARN: split mismatch (got {len(clean)}, "
                f"expected {len(texts)}) — retrying individually",
                flush=True,
            )
            return translate_individually(texts, tl)
        return clean
    except Exception as e:
        print(f"\n    WARN batch error: {e} — retrying individually", flush=True)
        return translate_individually(texts, tl)


def translate_individually(texts: list[str], tl: str) -> list[str]:
    out = []
    for text in texts:
        if not text:
            out.append(text)
            continue
        try:
            encoded = urllib.parse.quote(text)
            url = (
                f"https://translate.googleapis.com/translate_a/single"
                f"?client=gtx&sl=en&tl={tl}&dt=t&q={encoded}"
            )
            raw = https_get(url)
            parsed = json.loads(raw)
            out.append("".join(s[0] for s in parsed[0] if s and s[0]))
        except Exception as e:
            print(f"\n      ERR individual: {e}", flush=True)
            out.append(text)  # keep original on failure
        time.sleep(0.25)
    return out


# ---------------------------------------------------------------------------
# Chunking
# ---------------------------------------------------------------------------
def chunk(arr: list[str], max_items: int = 12, max_chars: int = 3500) -> list[list[str]]:
    chunks, cur, cur_len = [], [], 0
    for item in arr:
        if (len(cur) >= max_items or cur_len + len(item) > max_chars) and cur:
            chunks.append(cur)
            cur, cur_len = [], 0
        cur.append(item)
        cur_len += len(item)
    if cur:
        chunks.append(cur)
    return chunks


# ---------------------------------------------------------------------------
# JSON path extraction / application
# ---------------------------------------------------------------------------
def extract_strings(data: dict) -> tuple[list[str], list[list]]:
    strings, paths = [], []

    for arc_key, arc in (data.get("acts") or {}).items():
        for field in ("title", "subtitle", "description"):
            if arc.get(field):
                strings.append(arc[field])
                paths.append(["acts", arc_key, field])

    for lvl_key, lvl in (data.get("levels") or {}).items():
        for field in ("title", "setting", "objective"):
            if lvl.get(field):
                strings.append(lvl[field])
                paths.append(["levels", lvl_key, field])
        for i, mono in enumerate(lvl.get("solo_monologues") or []):
            if mono.get("text"):
                strings.append(mono["text"])
                paths.append(["levels", lvl_key, "solo_monologues", i, "text"])
        for i, entry in enumerate(lvl.get("dialogue") or []):
            if entry.get("text"):
                strings.append(entry["text"])
                paths.append(["levels", lvl_key, "dialogue", i, "text"])

    return strings, paths


def apply_strings(template: dict, strings: list[str], paths: list[list]) -> dict:
    result = copy.deepcopy(template)
    for translated, path in zip(strings, paths):
        obj = result
        for key in path[:-1]:
            obj = obj[key]
        obj[path[-1]] = translated
    return result


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def main():
    force = "--force" in sys.argv
    explicit_langs = [a for a in sys.argv[1:] if not a.startswith("--")]
    target_langs = explicit_langs if explicit_langs else list(LANG_MAP.keys())

    english_path = DIR / "english.json"
    english = json.loads(english_path.read_text(encoding="utf-8"))
    strings, paths = extract_strings(english)
    print(f"Source: {len(strings)} translatable strings from english.json\n")

    for lang in target_langs:
        code = LANG_MAP.get(lang)
        if not code:
            print(f"SKIP {lang} — not in LANG_MAP")
            continue

        out_path = DIR / f"{lang}.json"

        if not force and out_path.exists():
            existing = json.loads(out_path.read_text(encoding="utf-8"))
            if existing.get("levels", {}).get("1", {}).get("title"):
                print(f"SKIP {lang} (already translated)")
                continue

        print(f"Translating → {lang} ({code}) ...")
        t0 = time.time()

        chunks = chunk(strings)
        translated = []
        for ci, ch in enumerate(chunks):
            pct = round(ci / len(chunks) * 100)
            print(f"  chunk {ci + 1}/{len(chunks)} ({pct}%) ...", end=" ", flush=True)
            result = translate_batch(ch, code)
            translated.extend(result)
            print("ok")
            if ci < len(chunks) - 1:
                time.sleep(0.35)

        output = apply_strings(english, translated, paths)
        output["meta"]["language"] = lang
        output["meta"]["description"] = (
            f"Axiom Force: Reclaim Earth — {lang} narrative data"
        )

        out_path.write_text(
            json.dumps(output, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )
        print(f"  done in {time.time() - t0:.1f}s → {lang}.json\n")
        time.sleep(0.5)

    print("All translations complete.")


if __name__ == "__main__":
    main()
