
# GetOccupationsByLabel Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
match (o:esco__Occupation)--(:ncs__JobProfile)
where head(o.skos__prefLabel) contains 'toxic' or 
case toLower('true')
  when 'true' then
    any(alt in o.skos__altLabel where alt contains 'toxic')
    or any(hidden in o.skos__hiddenLabel where hidden contains 'toxic')
  else
    false
  end
with { occupations:collect(
{
  uri:o.uri,
  occupation:head(o.skos__prefLabel),
  alternativeLabels:coalesce(o.skos__altLabel, o.skos__hiddenLabel),
  lastModified:head(o.dct__modified),
  matches:
  {
    occupation:[preflab in o.skos__prefLabel where preflab contains 'toxic'],
    alternativeLabels:coalesce([altlab in o.skos__altLabel where altlab contains 'toxic'], [hidlab in o.skos__hiddenLabel where hidlab contains 'toxic'])
  }
}
)} as occupations 
return occupations
```

## Previous Query 1

Here's a previous version of the query that returns just the matches and ids (in case we need to revert)...

```
match (o:esco__Occupation) where toLower(head(o.skos__prefLabel)) contains 'toxic' or any(alt in o.skos__altLabel where toLower(alt) contains 'toxic') or any(hidden in o.skos__hiddenLabel where toLower(hidden) contains 'toxic')
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
