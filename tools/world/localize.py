"""Materialize the offline-authored world interface/geonym glossary into the existing seventeen-language CSV.

No translation service is called. Proper-name roots keep canonical Latin spelling;
descriptors and controls are localized. Native-language acceptance remains pending.
"""
import csv
import json
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[2]
TARGETS=dict(spanish='es',eurofrench='fr',brazilianportuguese='pt',german='de',italian='it',polish='pl',arabic='ar',turkish='tr',japanese='ja',korean='ko',mandarin='zh-CN',hebrew='iw',traditionalchinese='zh-TW',indonesian='id',vietnamese='vi',thai='th')


def sources():
    """Collect explicit world UI source calls and every named district/destination from the authoritative catalog."""
    values=set()
    for path in (ROOT/'scripts/world').glob('*.cs'):
        text=path.read_text(encoding='utf-8')
        values.update(json.loads('"'+value+'"') for value in re.findall(r'WorldText.Get\("([^"\n]+)"\)',text))
    catalog=(ROOT/'scripts/world/WorldCatalog.cs').read_text(encoding='utf-8')
    values.update(re.findall(r'new(?: WorldRegion)?\(\d+,"([^"]+)"',catalog))
    names=re.search(r'string\[\] names=\{(.*?)\};',catalog,re.S).group(1)
    values.update(re.findall(r'"([^"]+)"',names))
    # Conditional/array sources are explicit here so missing branches cannot silently disappear.
    values.update(['Enter mission','Mission locked','Surface','Downtown underground','Foundry underworks','Saving campaign…','Campaign saved','Preview · no active campaign','+','−','Free transit','Set waypoint','Clear waypoint','Return to main menu','Close map','Pan: WASD / left stick\nZoom: + − / shoulders\nRight-drag to pan · click for waypoint','Return to Neo-Kyoto'])
    return sorted(values)


def main():
    """Import reviewed, offline-authored UI and geonym descriptors without contacting a service.

    Name roots remain canonical proper names. Descriptors use the same authored
    glossary in every district, station and map marker. Native review is pending.
    """
    labels=sources()
    place=json.loads((ROOT/'tools/world/authored.json').read_text(encoding='utf-8'))
    interface=json.loads((ROOT/'tools/world/interface.json').read_text(encoding='utf-8'))
    terms=place['terms'].split('|')
    keys=interface['keys'].split('|')
    path=ROOT/'assets/translations/Translations - Sheet1.csv'
    with path.open(encoding='utf-8-sig',newline='') as handle:
        reader=csv.DictReader(handle); columns=reader.fieldnames; rows=list(reader)
    by_key={row['']:row for row in rows}
    result={'source':place['provenance'],'languages':{}}
    old=['Viejo Kyoto','Vieux Kyoto','Kyoto Antiga','Altes Kyoto','Vecchia Kyoto','Stare Kyoto','كيوتو القديمة','Eski Kyoto','旧京都','옛 교토','旧京都','קיוטו העתיקה','舊京都','Kyoto Lama','Kyoto cổ','เกียวโตเก่า']
    for index,language in enumerate(TARGETS):
        glossary=dict(zip(terms,place[language].split('|'),strict=True))
        ui=dict(zip(keys,interface[language].split('|'),strict=True))
        translated={}
        for source in labels:
            if source in ui: value=ui[source]
            elif source in ('+','−'): value=source
            elif source=='Old Kyoto': value=old[index]
            elif source.startswith('Neo-Kyoto'): value=source
            elif source.startswith('Move:'):
                value=f"{ui['Move']}: WASD / LS · {ui['Run']}: Shift / L3 · {ui['Map']}: M / Back · {ui['Interact']}: E / A · {ui['Squad']}: Tab / Y"
            elif source.startswith('Pan:'):
                value=f"{ui['Pan']}: WASD / LS\n{ui['Zoom']}: + − / LB RB\n{ui['Right-drag to pan · click for waypoint']}"
            else:
                term=next((t for t in terms if source.endswith(' '+t)),None)
                if term: value=glossary[term]+' · '+source[:-len(term)-1]
                elif source=='Beacon of Dawn': value=glossary['Beacon']+' · Dawn'
                elif source=='Spire of Axiom': value=glossary['Spire']+' · Axiom'
                else: raise ValueError('Unauthored label: '+source)
            translated[source]=value
        result['languages'][language]=translated
    for source in labels:
        candidate={'':source,'english':source,**{language:result['languages'][language][source] for language in TARGETS}}
        if source in by_key:
            if by_key[source]['english']!=source: raise ValueError('Source-key collision: '+source)
            # Exact source-key rows are owned by this authoring pipeline, so corrected text refreshes deterministically.
            by_key[source].update(candidate)
        else: rows.append(candidate)
    with path.open('w',encoding='utf-8',newline='') as handle:
        writer=csv.DictWriter(handle,fieldnames=columns); writer.writeheader(); writer.writerows(rows)
    (ROOT/'tools/world/translations.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(len(labels), 'labels across seventeen languages')


if __name__=='__main__':
    main()
