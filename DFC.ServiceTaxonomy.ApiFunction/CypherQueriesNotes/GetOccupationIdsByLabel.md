
# GetOccupationIdsByLabel Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
match (o:esco__Occupation) where toLower(head(o.skos__prefLabel)) contains 'toxic' or any(alt in o.skos__altLabel where toLower(alt) contains 'toxic') or any(hidden in o.skos__hiddenLabel where toLower(hidden) contains 'toxic')
with o.uri as uri, o.skos__prefLabel + coalesce(o.skos__altLabel, []) + coalesce(o.skos__hiddenLabel, []) as labels
unwind([label in labels where toLower(label) contains 'toxic']) as matchedLabels
with {Occupations:collect( {Label: matchedLabels, Uri: uri})} as matches
return matches
```

## Alternative queries

Here's a simpler looking, but slower version of the query...

```
match (o:esco__Occupation) with o.skos__prefLabel + coalesce(o.skos__altLabel, []) + coalesce(o.skos__hiddenLabel, []) as labels, o.uri as uri
unwind ([label in labels where toLower(label) contains 'toxic']) as matchedLabels
with {Occupations:collect( {Label: matchedLabels, Uri: uri})} as matches
return matches
```
