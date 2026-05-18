#!/usr/bin/env node
/**
 * translate_all.js
 * Translates english.json into all 48 language stub files using the
 * unofficial Google Translate API (no API key required).
 *
 * Usage:  node translate_all.js
 *         node translate_all.js spanish german japanese   (specific langs)
 */
"use strict";

const fs   = require("fs");
const path = require("path");
const https = require("https");

const DIR = path.dirname(require.main.filename);

// -----------------------------------------------------------------
// Language map: filename stem → Google Translate language code
// -----------------------------------------------------------------
const LANG_MAP = {
  arabic:              "ar",
  armenian:            "hy",
  azerbaijani:         "az",
  bengali:             "bn",
  brazilianportuguese: "pt",       // Google defaults pt to Brazilian
  canadianfrench:      "fr",       // no distinct fr-CA in gtx endpoint
  czech:               "cs",
  danish:              "da",
  dutch:               "nl",
  estonian:            "et",
  eurofrench:          "fr",
  europortuguese:      "pt-PT",
  filipino:            "tl",
  finnish:             "fi",
  georgian:            "ka",
  german:              "de",
  greek:               "el",
  gujarati:            "gu",
  hebrew:              "iw",
  hindi:               "hi",
  hungarian:           "hu",
  icelandic:           "is",
  indonesian:          "id",
  italian:             "it",
  japanese:            "ja",
  kannada:             "kn",
  korean:              "ko",
  latvian:             "lv",
  lithuanian:          "lt",
  malay:               "ms",
  malayalam:           "ml",
  mandarin:            "zh-CN",
  marathi:             "mr",
  norwegian:           "no",
  polish:              "pl",
  punjabi:             "pa",
  romanian:            "ro",
  sinhala:             "si",
  slovak:              "sk",
  slovenian:           "sl",
  spanish:             "es",
  swedish:             "sv",
  tamil:               "ta",
  telugu:              "te",
  thai:                "th",
  turkish:             "tr",
  ukrainian:           "uk",
  vietnamese:          "vi",
};

// Separator unlikely to appear in dialogue or be altered by translation
const SEP = "\n<<<>>>\n";

// -----------------------------------------------------------------
// HTTP helper
// -----------------------------------------------------------------
function httpsGet(url) {
  return new Promise((resolve, reject) => {
    const opts = {
      headers: {
        "User-Agent": "Mozilla/5.0",
        "Accept-Encoding": "identity",
      },
    };
    https.get(url, opts, (res) => {
      let raw = "";
      res.on("data", (c) => (raw += c));
      res.on("end", () => resolve(raw));
    }).on("error", reject);
  });
}

// -----------------------------------------------------------------
// Translate a batch of strings to target language
// Returns the translated array (same length as input).
// Falls back to originals on any error.
// -----------------------------------------------------------------
async function translateBatch(texts, tl) {
  if (!texts.length) return [];

  const joined   = texts.join(SEP);
  const encoded  = encodeURIComponent(joined);
  const url      = `https://translate.googleapis.com/translate_a/single` +
                   `?client=gtx&sl=en&tl=${tl}&dt=t&q=${encoded}`;
  try {
    const raw    = await httpsGet(url);
    const parsed = JSON.parse(raw);
    // Flatten the result segments
    const translated = parsed[0].map(s => (s && s[0]) ? s[0] : "").join("");
    // Split by separator – allow for minor whitespace variations
    const parts  = translated.split(/<<<>>>/);
    const clean  = parts.map(p => p.replace(/^\n+|\n+$/g, ""));
    if (clean.length !== texts.length) {
      console.warn(`    WARN: split mismatch (got ${clean.length}, expected ${texts.length}) – retrying individually`);
      return await translateIndividually(texts, tl);
    }
    return clean;
  } catch (err) {
    console.warn(`    WARN batch error: ${err.message} – retrying individually`);
    return await translateIndividually(texts, tl);
  }
}

async function translateIndividually(texts, tl) {
  const out = [];
  for (const text of texts) {
    if (!text) { out.push(text); continue; }
    try {
      const encoded = encodeURIComponent(text);
      const url     = `https://translate.googleapis.com/translate_a/single` +
                      `?client=gtx&sl=en&tl=${tl}&dt=t&q=${encoded}`;
      const raw     = await httpsGet(url);
      const parsed  = JSON.parse(raw);
      out.push(parsed[0].map(s => (s && s[0]) ? s[0] : "").join(""));
    } catch (e) {
      console.warn(`      ERR individual: ${e.message}`);
      out.push(text); // keep original on failure
    }
    await sleep(250);
  }
  return out;
}

// -----------------------------------------------------------------
// Chunk an array into groups of up to `size` items,
// keeping total character length under `maxChars`.
// -----------------------------------------------------------------
function chunk(arr, maxItems = 12, maxChars = 3500) {
  const chunks = [];
  let cur = [], curLen = 0;
  for (const item of arr) {
    if ((cur.length >= maxItems || curLen + item.length > maxChars) && cur.length) {
      chunks.push(cur);
      cur = [];
      curLen = 0;
    }
    cur.push(item);
    curLen += item.length;
  }
  if (cur.length) chunks.push(cur);
  return chunks;
}

// -----------------------------------------------------------------
// Deep extract all translatable strings + their JSON paths
// -----------------------------------------------------------------
function extractStrings(data) {
  const strings = [], paths = [];

  for (const [arcKey, arc] of Object.entries(data.acts || {})) {
    for (const f of ["title", "subtitle", "description"]) {
      if (arc[f]) { strings.push(arc[f]); paths.push(["acts", arcKey, f]); }
    }
  }

  for (const [lvlKey, lvl] of Object.entries(data.levels || {})) {
    for (const f of ["title", "setting", "objective"]) {
      if (lvl[f]) { strings.push(lvl[f]); paths.push(["levels", lvlKey, f]); }
    }
    for (let i = 0; i < (lvl.solo_monologues || []).length; i++) {
      const text = lvl.solo_monologues[i].text;
      if (text) { strings.push(text); paths.push(["levels", lvlKey, "solo_monologues", i, "text"]); }
    }
    for (let i = 0; i < (lvl.dialogue || []).length; i++) {
      const text = lvl.dialogue[i].text;
      if (text) { strings.push(text); paths.push(["levels", lvlKey, "dialogue", i, "text"]); }
    }
  }

  return { strings, paths };
}

// -----------------------------------------------------------------
// Apply translated strings back at their paths (deep clone first)
// -----------------------------------------------------------------
function applyStrings(template, strings, paths) {
  const result = JSON.parse(JSON.stringify(template)); // deep clone
  for (let i = 0; i < paths.length; i++) {
    let obj = result;
    for (let k = 0; k < paths[i].length - 1; k++) obj = obj[paths[i][k]];
    obj[paths[i][paths[i].length - 1]] = strings[i];
  }
  return result;
}

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

// -----------------------------------------------------------------
// Main
// -----------------------------------------------------------------
async function main() {
  const englishPath = path.join(DIR, "english.json");
  const english     = JSON.parse(fs.readFileSync(englishPath, "utf8"));
  const { strings, paths } = extractStrings(english);
  console.log(`Source: ${strings.length} translatable strings from english.json\n`);

  const targetLangs = process.argv.slice(2).length
    ? process.argv.slice(2)
    : Object.keys(LANG_MAP);

  for (const lang of targetLangs) {
    const code = LANG_MAP[lang];
    if (!code) { console.warn(`SKIP ${lang} — not in LANG_MAP`); continue; }

    const outPath = path.join(DIR, `${lang}.json`);

    // Skip if already translated (level 1 title is non-empty)
    if (fs.existsSync(outPath)) {
      const existing = JSON.parse(fs.readFileSync(outPath, "utf8"));
      if (existing?.levels?.["1"]?.title) {
        console.log(`SKIP ${lang} (already translated)`);
        continue;
      }
    }

    console.log(`Translating → ${lang} (${code}) ...`);
    const t0 = Date.now();

    // Translate in chunks
    const chunks    = chunk(strings);
    const translated = [];
    for (let ci = 0; ci < chunks.length; ci++) {
      const pct = Math.round((ci / chunks.length) * 100);
      process.stdout.write(`  chunk ${ci + 1}/${chunks.length} (${pct}%) ...`);
      const result = await translateBatch(chunks[ci], code);
      translated.push(...result);
      process.stdout.write(` ok\n`);
      if (ci < chunks.length - 1) await sleep(350);
    }

    const output = applyStrings(english, translated, paths);
    output.meta.language    = lang;
    output.meta.description = `Axiom Force: Reclaim Earth — ${lang} narrative data`;

    fs.writeFileSync(outPath, JSON.stringify(output, null, 2) + "\n", "utf8");
    console.log(`  ✓ done in ${((Date.now() - t0) / 1000).toFixed(1)}s → ${lang}.json\n`);
    await sleep(500);
  }

  console.log("All translations complete.");
}

main().catch(err => { console.error(err); process.exit(1); });
