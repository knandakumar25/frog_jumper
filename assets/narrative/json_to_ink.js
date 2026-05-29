#!/usr/bin/env node
/**
 * json_to_ink.js
 * Converts narrative JSON files to Ink (.ink) source files.
 *
 * Usage:
 *     node json_to_ink.js              # convert all *.json in this directory
 *     node json_to_ink.js english      # convert specific locale(s)
 */

const fs = require('fs');
const path = require('path');

const DIR = __dirname;

function escapeInkText(text) {
    if (text == null) return '';
    return String(text)
        .replace(/\\/g, '\\\\')
        .replace(/\{/g, '\\{')
        .replace(/\}/g, '\\}')
        .replace(/\[/g, '\\[')
        .replace(/\]/g, '\\]');
}

function escapeInkComment(text) {
    if (text == null) return '';
    return String(text).replace(/\r?\n/g, ' ');
}

function writeMetaComments(lines, meta, locale) {
    lines.push('// Narrative source — Axiom Force: Reclaim Earth');
    if (locale) lines.push(`// locale: ${locale}`);
    if (meta) {
        if (meta.language) lines.push(`// meta.language: ${meta.language}`);
        if (meta.version) lines.push(`// meta.version: ${meta.version}`);
        if (meta.description) lines.push(`// meta.description: ${escapeInkComment(meta.description)}`);
    }
    lines.push('');
}

function writeTaggedLine(lines, tags, text) {
    const tagParts = Object.entries(tags)
        .filter(([, value]) => value != null && value !== '')
        .map(([key, value]) => `${key}: ${value}`);
    if (tagParts.length > 0) {
        lines.push(`# ${tagParts.join(' # ')}`);
    }
    const escaped = escapeInkText(text);
    if (escaped.length > 0) {
        lines.push(escaped);
    }
    lines.push('');
}

function convertJsonToInk(data, locale) {
    const lines = [];
    writeMetaComments(lines, data.meta || {}, locale);
    lines.push('// Generated from JSON narrative data. Compile with inklecate to produce .ink.json.');
    lines.push('');

    const acts = data.acts || {};
    const actKeys = Object.keys(acts).sort();
    if (actKeys.length > 0) {
        lines.push('=== acts ===');
        for (const key of actKeys) {
            lines.push(`-> act_${key}`);
        }
        lines.push('-> END');
        lines.push('');
    }

    for (const key of actKeys) {
        const act = acts[key];
        lines.push(`=== act_${key} ===`);
        writeTaggedLine(lines, {
            kind: 'act',
            key,
            title: act.title || '',
            subtitle: act.subtitle || '',
        }, act.description || '');
        lines.push('-> END');
        lines.push('');
    }

    const levels = data.levels || {};
    const levelKeys = Object.keys(levels).sort((a, b) => Number(a) - Number(b));
    if (levelKeys.length > 0) {
        lines.push('=== levels ===');
        for (const key of levelKeys) {
            lines.push(`-> level_${key}`);
        }
        lines.push('-> END');
        lines.push('');
    }

    for (const key of levelKeys) {
        const level = levels[key];
        lines.push(`=== level_${key} ===`);
        writeTaggedLine(lines, {
            kind: 'level',
            level: key,
            title: level.title || '',
            arc: level.arc == null ? '' : String(level.arc),
            dialogue_status: level.dialogue_status || 'pending',
        }, '');

        if (level.setting) {
            writeTaggedLine(lines, { kind: 'setting' }, level.setting);
        }
        if (level.objective) {
            writeTaggedLine(lines, { kind: 'objective' }, level.objective);
        }

        const monologues = level.solo_monologues || [];
        for (const mono of monologues) {
            writeTaggedLine(lines, {
                kind: 'solo_monologue',
                character: mono.character || '',
                type: mono.type || '',
            }, mono.text || '');
        }

        const dialogue = level.dialogue || [];
        for (const entry of dialogue) {
            writeTaggedLine(lines, {
                kind: 'dialogue',
                speaker: entry.speaker || '',
                type: entry.type || '',
            }, entry.text || '');
        }

        lines.push('-> END');
        lines.push('');
    }

    lines.push('=== END ===');
    lines.push('-> DONE');
    lines.push('');

    return lines.join('\n');
}

function convertFile(jsonPath) {
    const locale = path.basename(jsonPath, '.json');
    const data = JSON.parse(fs.readFileSync(jsonPath, 'utf8'));
    const ink = convertJsonToInk(data, locale);
    const outPath = path.join(DIR, `${locale}.ink`);
    fs.writeFileSync(outPath, ink, 'utf8');
    return { locale, outPath, levels: Object.keys(data.levels || {}).length };
}

function main() {
    const args = process.argv.slice(2);
    let jsonFiles;

    if (args.length > 0) {
        jsonFiles = args.map((name) => {
            const stem = name.replace(/\.json$/i, '');
            return path.join(DIR, `${stem}.json`);
        });
    } else {
        jsonFiles = fs.readdirSync(DIR)
            .filter((name) => name.endsWith('.json'))
            .map((name) => path.join(DIR, name))
            .sort();
    }

    let converted = 0;
    for (const jsonPath of jsonFiles) {
        if (!fs.existsSync(jsonPath)) {
            console.warn(`SKIP missing file: ${jsonPath}`);
            continue;
        }
        const result = convertFile(jsonPath);
        console.log(`Converted ${result.locale}.json -> ${result.locale}.ink (${result.levels} levels)`);
        converted += 1;
    }

    console.log(`\nDone. Converted ${converted} file(s).`);
}

if (require.main === module) {
    main();
}

module.exports = { convertJsonToInk, escapeInkText };
