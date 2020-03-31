
# GetOccupationsByLabel Query Notes

## Current Query

```
with 'boat' as lowerlabel
   
call db.index.fulltext.queryNodes("OccupationLabels", "skos__prefLabel:"+ "*" + lowerlabel + "*") yield node, score
match (node)-[r:ncs__hasPrefLabel|:ncs__hasAltLabel]-(po:esco__Occupation)-[:ncs__hasAltLabel]-(AltLabels)
with collect(distinct po) as occupations, avg(score) as averageScore, AltLabels, po, node, lowerlabel
with collect(distinct AltLabels.skos__prefLabel) as allAltLabels, occupations, lowerlabel, averageScore
unwind occupations as o
optional match (altLabel)-[:ncs__hasAltLabel]-(o)
with collect(distinct altLabel.skos__prefLabel) as matchingAltLabels, allAltLabels, occupations, lowerlabel, o, averageScore
optional match(prefLabel)-[:ncs__hasPrefLabel]-(o)
with collect(distinct prefLabel.skos__prefLabel) as matchingPrefLabels, matchingAltLabels, allAltLabels, occupations, lowerlabel, o, averageScore
with {Value: [prefLab in matchingPrefLabels where toLower(prefLab) contains lowerlabel]} as matchingPrefLabelCount, o, allAltLabels, matchingAltLabels, matchingPrefLabels, occupations, lowerlabel, averageScore
with {Occupations: case 'true' when 'true' then o when 'false' then [val in occupations where size(matchingPrefLabelCount.Value) > 0] end } as filteredResults, matchingPrefLabels, matchingAltLabels, matchingPrefLabelCount, allAltLabels, occupations, lowerlabel, {Value: case 'true' when 'true' then averageScore + (size(matchingPrefLabelCount.Value) * 10) + size(matchingAltLabels) when 'false' then averageScore + size(matchingAltLabels) end } as boostedScore
unwind filteredResults.Occupations as o
with {occupations:collect(
{
  uri:o.uri,
  occupation:o.skos__prefLabel,
  alternativeLabels:allAltLabels,
    lastModified:o.dct__modified,
  matches:
  {
    occupation:[prefLab in matchingPrefLabels where toLower(prefLab) contains lowerlabel],
    alternativeLabels:[altlab in matchingAltLabels where toLower(altlab) contains lowerlabel]
  },
  Score:boostedScore.Value
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
