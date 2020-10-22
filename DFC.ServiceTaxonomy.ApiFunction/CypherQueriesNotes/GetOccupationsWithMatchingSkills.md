# GetOccupationsWithMatchingSkills Query Notes

## Params

$skillList : a list of skill categories, e.g. ['http://data.europa.eu/esco/skill/S2.4.1']
$minimumMatchingSkills : the minimum number of skill groups that need to match between $skillList and the skill groups associated with the matched occupations

## Query

Here's the query, formatted...

```
MATCH (la {esco__language:'en'})<-[:dct__description]-(o:esco__Occupation)-[:esco__isOptionalSkillFor|esco__isEssentialSkillFor]-(s2:esco__Skill)-[:skos__broaderTransitive]->(bg2:skos__Concept)
  where bg2.uri in $skillList
  
WITH { NumberOfMatches : COUNT(distinct bg2), o:o, Description: la.esco__nodeLiteral } as occupationMatches ORDER BY occupationMatches.NumberOfMatches DESC

WITH { Occupations: [val in collect(occupationMatches) where val.NumberOfMatches >= (case when val.NumberOfMatches < $minimumMatchingSkills THEN val.NumberOfMatches else $minimumMatchingSkills end)] } as filteredMatches

UNWIND filteredMatches as mat1

UNWIND mat1.Occupations as mat2

WITH { values: apoc.agg.slice(mat2, 0, 50) } as restrictedResults

UNWIND restrictedResults.values as mat3

RETURN { matchingOccupations: collect({uri:mat3.o.uri, occupation:mat3.o.skos__prefLabel, jobProfileTitle:mat3.o.skos__prefLabel, jobProfileUri:mat3.o.uri, jobProfileDescription:mat3.Description, matchingEssentialSkills:0, matchingOptionalSkills:0, socCode:9999, totalOccupationEssentialSkills:mat3.NumberOfMatches, totalOccupationOptionalSkills:0, lastModified:mat3.o.dct__modified})} as results
```
