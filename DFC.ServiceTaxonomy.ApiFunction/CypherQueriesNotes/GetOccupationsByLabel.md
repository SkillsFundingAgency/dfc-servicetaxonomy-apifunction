
# GetOccupationsByLabel Query Notes

## Current Query

```
with 'chemical' as lowerlabel

call db.index.fulltext.queryNodes("OccupationLabels", "skos__prefLabel: "+lowerlabel+"^2 ncs__altLabel: "+ lowerlabel) yield node, score
match (node:esco__Occupation)
with collect({Occupation:node, Score:score}) as prefocc, lowerlabel
call db.index.fulltext.queryNodes("OccupationLabels", "skos__prefLabel: "+lowerlabel+"^2 ncs__altLabel: "+ lowerlabel) yield node, score
optional match (node)<-[:ncs_OccupationAltLabel]-(altocc)
with prefocc + collect({Occupation:altocc, Score:score}) as combocc, lowerlabel
unwind combocc as o
with distinct o, lowerlabel

with { occupations:collect(
{
  uri:o.Occupation.uri,
  occupation:o.Occupation.skos__prefLabel,
  alternativeLabels:o.Occupation.skos__altLabel,
  lastModified:o.Occupation.dct__modified,
  matches:
  {
    occupation:[preflab in o.Occupation.skos__prefLabel where toLower(preflab) contains lowerlabel],
    alternativeLabels:[altlab in o.Occupation.skos__altLabel where toLower(altlab) contains lowerlabel]
  },
  score:o.Score
}
)} as occupations 
return occupations
```

## Previous Query 2

Here's the current query, formatted and with params replaced by literals...

```
with toLower('toxic') as lowerlabel
match (o:esco__Occupation)
where toLower(o.skos__prefLabel) contains lowerlabel or 
case toLower('true')
  when 'true' then
    any(alt in o.skos__altLabel where toLower(alt) contains lowerlabel)
  else
    false
  end
with { occupations:collect(
{
  uri:o.uri,
  occupation:o.skos__prefLabel,
  alternativeLabels:o.skos__altLabel,
  lastModified:o.dct__modified,
  matches:
  {
    occupation:[preflab in o.skos__prefLabel where toLower(preflab) contains lowerlabel],
    alternativeLabels:[altlab in o.skos__altLabel where toLower(altlab) contains lowerlabel]
  }
}
)} as occupations 
return occupations
```

## Previous Query 1

Here's a previous version of the query that returns just the matches and ids (in case we need to revert)...

```
match (o:esco__Occupation) where toLower(o.skos__prefLabel) contains 'toxic' or any(alt in o.skos__altLabel where toLower(alt) contains 'toxic') or any(hidden in o.skos__hiddenLabel where toLower(hidden) contains 'toxic')
with o.uri as uri, o.skos__prefLabel + coalesce(o.skos__altLabel, []) + coalesce(o.skos__hiddenLabel, []) as labels
unwind([label in labels where toLower(label) contains 'toxic']) as matchedLabels
with {Occupations:collect( {Label: matchedLabels, Uri: uri})} as matches
return matches
```

## Alternative for Previous Query 1

Here's a simpler looking, but slower version of the query...

```
match (o:esco__Occupation) with o.skos__prefLabel + coalesce(o.skos__altLabel, []) + coalesce(o.skos__hiddenLabel, []) as labels, o.uri as uri
unwind ([label in labels where toLower(label) contains 'toxic']) as matchedLabels
with {Occupations:collect( {Label: matchedLabels, Uri: uri})} as matches
return matches
```

## Notes

The only 2 occupations with hidden labels, looks like they should have been alt labels instead, as the occupations are missing alt labels, and the hidden labels have no reason to be hidden.

The two occupations are:
[general practitioner](https://ec.europa.eu/esco/portal/occupation?uri=http%3A%2F%2Fdata.europa.eu%2Fesco%2Foccupation%2F9b889f07-c39c-464d-b9d9-b2daa650f9ac&conceptLanguage=en&full=true#&uri=http://data.europa.eu/esco/occupation/9b889f07-c39c-464d-b9d9-b2daa650f9ac)
[specialist doctor](https://ec.europa.eu/esco/portal/occupation?uri=http%3A%2F%2Fdata.europa.eu%2Fesco%2Foccupation%2F9b889f07-c39c-464d-b9d9-b2daa650f9ac&conceptLanguage=en&full=true#&uri=http://data.europa.eu/esco/occupation/9b889f07-c39c-464d-b9d9-b2daa650f9ac)

Presumably, the next release of the ESCO dataset wll fix the issue by changing them to alt labels.

We handle the issue in this cypher query, by returning the hidden labels as alt labels. (We'll also have to update all the queries that return occupations to handle the issue.)

Alternatively we could fix up the issue post import of the ESCO data, which would localise the fix to a single location, and we'd only have to remove that one post-import cypher statement once a new ESCO dataset is released, rather than updating all relevant cypher queries. However we're sticking to the principle of not mutating the ESCO dataset.
