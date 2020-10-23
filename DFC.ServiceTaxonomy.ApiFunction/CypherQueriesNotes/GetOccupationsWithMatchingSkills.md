# GetOccupationsWithMatchingSkills Query Notes

## Params

$skillList : a list of skill categories, e.g. ['http://data.europa.eu/esco/skill/S2.4.1']
$minimumMatchingSkills : the minimum number of skill groups that need to match between $skillList and the skill groups associated with the matched occupations

## Query

Here's the query, formatted...

```
MATCH (la {esco__language:'en'})<-[:dct__description]-(o:esco__Occupation)-[r:esco__isEssentialSkillFor]-(s2:esco__Skill)-[:skos__broader]->(bg2:skos__Concept)
  where bg2.uri in ['http://data.europa.eu/esco/skill/S7.2.1','http://data.europa.eu/esco/skill/S7.1.2','http://data.europa.eu/esco/skill/S7.1.1']
with o, la, o.uri as occupationsMatchingEssential, collect(distinct bg2.uri) as essentialUris
MATCH (o)-[r3:esco__isEssentialSkillFor]-(s3:esco__Skill)-[:skos__broader]->(bg3:skos__Concept) WHERE bg3.skos__notation starts with 'S'
with o, la, occupationsMatchingEssential,essentialUris, collect(distinct bg3.uri) as allEssentialSkills
MATCH (la {esco__language:'en'})<-[:dct__description]-(ooptional:esco__Occupation)-[r:esco__isOptionalSkillFor]-(s2:esco__Skill)-[:skos__broader]->(bg2:skos__Concept)
  where bg2.uri in ['http://data.europa.eu/esco/skill/S7.2.1','http://data.europa.eu/esco/skill/S7.1.2','http://data.europa.eu/esco/skill/S7.1.1']
  and ooptional.uri in occupationsMatchingEssential
  and not bg2.uri in essentialUris
with o, la, occupationsMatchingEssential, essentialUris, allEssentialSkills, collect(distinct bg2.uri) as optionalWithEssentialUris
MATCH (o)-[r3:esco__isOptionalSkillFor]-(s3:esco__Skill)-[:skos__broader]->(bg3:skos__Concept) WHERE bg3.skos__notation starts with 'S'
  and not bg3.uri in allEssentialSkills
with o, la, occupationsMatchingEssential, essentialUris, allEssentialSkills, optionalWithEssentialUris, collect(distinct bg3.uri) as allOptionalSkills
with o, la, occupationsMatchingEssential, essentialUris, size(essentialUris) as essentialsMatches, optionalWithEssentialUris, size(optionalWithEssentialUris) as optionalMatches, allEssentialSkills as occupationEssentialSkills, size(allEssentialSkills) as occupationEssentialMatches, allOptionalSkills, size(allOptionalSkills) as occupationOptionalSkills
with occupationsMatchingEssential, essentialUris, essentialsMatches, optionalWithEssentialUris, optionalMatches, occupationEssentialSkills, occupationEssentialMatches, allOptionalSkills, occupationOptionalSkills, { description:la.esco__nodeLiteral, rank:(essentialsMatches*10000/occupationEssentialMatches)+((optionalMatches*10000/occupationOptionalSkills)/10000), occ:o} as ranking
with collect(ranking) as collectedRanking
with {filteredMatches:[val in collectedRanking where val.rank > 800]} as filteredRanking
UNWIND filteredRanking.filteredMatches as mat
RETURN { matchingOccupations: collect({uri:mat.occ.uri, occupation:mat.occ.skos__prefLabel, jobProfileTitle:mat.occ.skos__prefLabel, jobProfileUri:mat.occ.uri, jobProfileDescription:mat.description, matchingEssentialSkills:0, matchingOptionalSkills:0, socCode:9999, totalOccupationEssentialSkills:mat.rank, totalOccupationOptionalSkills:0, lastModified:mat.occ.dct__modified})} as results
```
