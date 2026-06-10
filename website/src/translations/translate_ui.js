import fs from 'fs';
import path from 'path';
import https from 'https';
import { fileURLToPath } from 'url';
import i18n from './i18n.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const enTranslation = i18n.options.resources.en.translation;

const CODE_TO_GT_CODE = {
  ar: 'ar',
  es: 'es',
  fr: 'fr',
  pt: 'pt',
  hy: 'hy',
  az: 'az',
  bn: 'bn',
  cs: 'cs',
  da: 'da',
  nl: 'nl',
  et: 'et',
  fil: 'tl',
  fi: 'fi',
  ka: 'ka',
  de: 'de',
  el: 'el',
  gu: 'gu',
  he: 'iw',
  hi: 'hi',
  hu: 'hu',
  is: 'is',
  id: 'id',
  it: 'it',
  ja: 'ja',
  kn: 'kn',
  ko: 'ko',
  lv: 'lv',
  lt: 'lt',
  ms: 'ms',
  ml: 'ml',
  zh: 'zh-CN',
  mr: 'mr',
  no: 'no',
  pl: 'pl',
  pa: 'pa',
  ro: 'ro',
  si: 'si',
  sk: 'sk',
  sl: 'sl',
  sv: 'sv',
  ta: 'ta',
  te: 'te',
  th: 'th',
  tr: 'tr',
  uk: 'uk',
  vi: 'vi'
};

const SEP = "\n<<<>>>\n";

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
            return await translateIndividually(texts, tl);
        }
        return clean;
    } catch (e) {
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
            out.push(text);
        }
        await sleep(200);
    }
    return out;
}

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

function getPathsAndStrings(obj, prefix = '') {
  const result = [];
  for (const key of Object.keys(obj)) {
    const val = obj[key];
    const path = prefix ? `${prefix}.${key}` : key;
    if (typeof val === 'string') {
      result.push({ path, text: val });
    } else if (typeof val === 'object' && val !== null) {
      result.push(...getPathsAndStrings(val, path));
    }
  }
  return result;
}

function setPathValue(obj, path, value) {
  const keys = path.split('.');
  let current = obj;
  for (let i = 0; i < keys.length - 1; i++) {
    const key = keys[i];
    if (!current[key]) current[key] = {};
    current = current[key];
  }
  current[keys[keys.length - 1]] = value;
}

async function main() {
  const items = getPathsAndStrings(enTranslation);
  const strings = items.map(item => item.text);
  const paths = items.map(item => item.path);

  console.log(`Extracted ${strings.length} UI strings from English bundle.`);

  // Load existing translations to preserve human-vetted strings
  const finalResources = {
    en: enTranslation,
    es: i18n.options.resources.es.translation,
    fr: i18n.options.resources.fr.translation,
    ar: i18n.options.resources.ar.translation,
    he: i18n.options.resources.he.translation
  };

  const targetLangs = Object.keys(CODE_TO_GT_CODE);

  for (const lang of targetLangs) {
    // Skip already covered base translated languages to avoid API calls and preserve vetted translations
    if (['en', 'es', 'fr', 'ar', 'he'].includes(lang)) {
      continue;
    }

    const code = CODE_TO_GT_CODE[lang];
    console.log(`Translating website UI into ${lang} (${code})...`);

    const chunks = chunkArray(strings, 20, 3000);
    const translatedStrings = [];

    for (let ci = 0; ci < chunks.length; ci++) {
      const ch = chunks[ci];
      const result = await translateBatch(ch, code);
      translatedStrings.push(...result);
      if (ci < chunks.length - 1) {
        await sleep(250);
      }
    }

    const langBundle = {};
    for (let i = 0; i < paths.length; i++) {
      setPathValue(langBundle, paths[i], translatedStrings[i]);
    }

    finalResources[lang] = langBundle;
    console.log(`Finished ${lang}.`);
    await sleep(250);
  }

  const outPath = path.join(__dirname, 'ui_translations.json');
  fs.writeFileSync(outPath, JSON.stringify(finalResources, null, 2), 'utf8');
  console.log(`Saved translations to ${outPath}`);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
