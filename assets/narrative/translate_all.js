#!/usr/bin/env node
/**
 * translate_all.js
 * Translates english.json into all 48 language files using the
 * unofficial Google Translate endpoint (no API key required).
 * 
 * Usage:
 *     node translate_all.js                          # all languages
 *     node translate_all.js spanish german japanese  # specific languages
 *     node translate_all.js --force                  # re-translate existing files
 */

const https = require('https');
const fs = require('fs');
const path = require('path');

// ---------------------------------------------------------------------------
// Language map: filename stem → Google Translate language code
// ---------------------------------------------------------------------------
const LANG_MAP = {
    "arabic":              "ar",
    "armenian":            "hy",
    "azerbaijani":         "az",
    "bengali":             "bn",
    "brazilianportuguese": "pt",       // Google defaults pt to Brazilian
    "canadianfrench":      "fr",       // no distinct fr-CA in gtx endpoint
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
};

// Separator unlikely to appear in dialogue or be altered by translation
const SEP = "\n<<<>>>\n";

const DIR = __dirname;

// ---------------------------------------------------------------------------
// HTTP helper
// ---------------------------------------------------------------------------
function httpsGet(url) {
    return new Promise((resolve, reject) => {
        const req = https.get(url, {
            headers: {
                "User-Agent": "Mozilla/5.0",
                "Accept-Encoding": "identity",
            }
        }, (res) => {
            let data = '';
            res.on('data', chunk => { data += chunk; });
            res.on('end', () => resolve(data));
        });
        req.on('error', reject);
        req.setTimeout(15000, () => {
            req.destroy();
            reject(new Error("Timeout"));
        });
    });
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// ---------------------------------------------------------------------------
// Translation helpers
// ---------------------------------------------------------------------------
async function translateBatch(texts, tl) {
    if (!texts || texts.length === 0) return [];

    const joined = texts.join(SEP);
    const encoded = encodeURIComponent(joined);
    const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=${tl}&dt=t&q=${encoded}`;

    try {
        const raw = await httpsGet(url);
        const parsed = JSON.parse(raw);
        
        let translated = "";
        if (parsed && parsed[0]) {
            for (const s of parsed[0]) {
                if (s && s[0]) {
                    translated += s[0];
                }
            }
        }
        
        const parts = translated.split("<<<>>>");
        const clean = parts.map(p => p.replace(/^\n|\n$/g, ''));
        
        if (clean.length !== texts.length) {
            console.warn(`\n    WARN: split mismatch (got ${clean.length}, expected ${texts.length}) — retrying individually`);
            return await translateIndividually(texts, tl);
        }
        return clean;
    } catch (e) {
        console.warn(`\n    WARN batch error: ${e.message} — retrying individually`);
        return await translateIndividually(texts, tl);
    }
}

async function translateIndividually(texts, tl) {
    const out = [];
    for (const text of texts) {
        if (!text) {
            out.push(text);
            continue;
        }
        try {
            const encoded = encodeURIComponent(text);
            const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=${tl}&dt=t&q=${encoded}`;
            const raw = await httpsGet(url);
            const parsed = JSON.parse(raw);
            
            let translated = "";
            if (parsed && parsed[0]) {
                for (const s of parsed[0]) {
                    if (s && s[0]) translated += s[0];
                }
            }
            out.push(translated);
        } catch (e) {
            console.error(`\n      ERR individual: ${e.message}`);
            out.push(text); // keep original on failure
        }
        await sleep(250);
    }
    return out;
}

// ---------------------------------------------------------------------------
// Chunking
// ---------------------------------------------------------------------------
function chunkArray(arr, maxItems = 12, maxChars = 3500) {
    const chunks = [];
    let cur = [];
    let curLen = 0;

    for (const item of arr) {
        if ((cur.length >= maxItems || curLen + item.length > maxChars) && cur.length > 0) {
            chunks.push(cur);
            cur = [];
            curLen = 0;
        }
        cur.push(item);
        curLen += item.length;
    }
    if (cur.length > 0) {
        chunks.push(cur);
    }
    return chunks;
}

// ---------------------------------------------------------------------------
// JSON path extraction / application
// ---------------------------------------------------------------------------
function extractStrings(data) {
    const strings = [];
    const paths = [];

    const acts = data.acts || {};
    for (const arcKey of Object.keys(acts)) {
        const arc = acts[arcKey];
        for (const field of ["title", "subtitle", "description"]) {
            if (arc[field]) {
                strings.push(arc[field]);
                paths.push(["acts", arcKey, field]);
            }
        }
    }

    const levels = data.levels || {};
    for (const lvlKey of Object.keys(levels)) {
        const lvl = levels[lvlKey];
        for (const field of ["title", "setting", "objective"]) {
            if (lvl[field]) {
                strings.push(lvl[field]);
                paths.push(["levels", lvlKey, field]);
            }
        }
        const soloMonologues = lvl.solo_monologues || [];
        for (let i = 0; i < soloMonologues.length; i++) {
            const mono = soloMonologues[i];
            if (mono.text) {
                strings.push(mono.text);
                paths.push(["levels", lvlKey, "solo_monologues", i, "text"]);
            }
        }
        const dialogue = lvl.dialogue || [];
        for (let i = 0; i < dialogue.length; i++) {
            const entry = dialogue[i];
            if (entry.text) {
                strings.push(entry.text);
                paths.push(["levels", lvlKey, "dialogue", i, "text"]);
            }
        }
    }

    return { strings, paths };
}

function applyStrings(template, strings, paths) {
    const result = JSON.parse(JSON.stringify(template)); // deep copy
    for (let i = 0; i < strings.length; i++) {
        const translated = strings[i];
        const path = paths[i];
        
        let obj = result;
        for (let j = 0; j < path.length - 1; j++) {
            obj = obj[path[j]];
        }
        obj[path[path.length - 1]] = translated;
    }
    return result;
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------
async function main() {
    const args = process.argv.slice(2);
    const force = args.includes('--force');
    const explicitLangs = args.filter(a => !a.startsWith('--'));
    const targetLangs = explicitLangs.length > 0 ? explicitLangs : Object.keys(LANG_MAP);

    const englishPath = path.join(DIR, 'english.json');
    let english;
    try {
        english = JSON.parse(fs.readFileSync(englishPath, 'utf8'));
    } catch (err) {
        console.error(`Error reading ${englishPath}:`, err.message);
        process.exit(1);
    }

    const { strings, paths } = extractStrings(english);
    console.log(`Source: ${strings.length} translatable strings from english.json\n`);

    for (const lang of targetLangs) {
        const code = LANG_MAP[lang];
        if (!code) {
            console.log(`SKIP ${lang} — not in LANG_MAP`);
            continue;
        }

        const outPath = path.join(DIR, `${lang}.json`);

        if (!force && fs.existsSync(outPath)) {
            try {
                const existing = JSON.parse(fs.readFileSync(outPath, 'utf8'));
                if (existing.levels && existing.levels["1"] && existing.levels["1"].title) {
                    console.log(`SKIP ${lang} (already translated)`);
                    continue;
                }
            } catch (err) {
                // Ignore parse errors, just re-translate
            }
        }

        console.log(`Translating → ${lang} (${code}) ...`);
        const t0 = Date.now();

        const chunks = chunkArray(strings);
        const translated = [];
        
        for (let ci = 0; ci < chunks.length; ci++) {
            const ch = chunks[ci];
            const pct = Math.round(((ci) / chunks.length) * 100);
            process.stdout.write(`  chunk ${ci + 1}/${chunks.length} (${pct}%) ... `);
            
            const result = await translateBatch(ch, code);
            translated.push(...result);
            console.log("ok");
            
            if (ci < chunks.length - 1) {
                await sleep(350);
            }
        }

        const output = applyStrings(english, translated, paths);
        if (!output.meta) output.meta = {};
        output.meta.language = lang;
        output.meta.description = `Axiom Force: Reclaim Earth — ${lang} narrative data`;

        fs.writeFileSync(outPath, JSON.stringify(output, null, 2) + "\n", 'utf8');
        
        const duration = ((Date.now() - t0) / 1000).toFixed(1);
        console.log(`  done in ${duration}s → ${lang}.json\n`);
        
        await sleep(500);
    }

    console.log("All translations complete.");
}

if (require.main === module) {
    main().catch(err => {
        console.error(err);
        process.exit(1);
    });
}
