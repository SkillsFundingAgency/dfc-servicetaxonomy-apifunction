
# GetOccupationsWithMatchingSkillsDetailed Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```


1. Currently in use

MATCH (la {esco__language:'en'})<-[:dct__description]-(o:esco__Occupation ) <-[r:esco__isEssentialSkillFor|esco__isOptionalSkillFor]- (s:esco__Skill ) -[:skos__broader]->(d) where d.skos__notation starts with 'S' and o.uri = $occupation
WITH o, la.esco__nodeLiteral as description, collect({uri:d.uri, rel:type(r)}  ) as allSkillDets
WITH o, description, allSkillDets, [ x in allSkillDets WHERE x.rel ='esco__isEssentialSkillFor' ] as allEssentialSkills
OPTIONAL MATCH(o:esco__Occupation ) <-[r:esco__isEssentialSkillFor|esco__isOptionalSkillFor]- (sx:esco__Skill ) -[:skos__broader]->(d) where d.skos__notation starts with 'S' and d.uri in $skillList
WITH o, description, allEssentialSkills,
        collect(distinct d.uri) as matchingSkills,
        collect(distinct{uri:d.uri,
                         skill:d.skos__prefLabel,
                         alternativeLabels:coalesce(d.skos__altLabel,[]),
                         type:'',
                         skillReusability:'',
                         relationshipType:case({uri:d.uri,rel:'esco__isEssentialSkillFor'} in allEssentialSkills) when true then 'essential' else 'optional' end,
                         lastModified:datetime() }) as matchingSkillDetails
OPTIONAL MATCH(o:esco__Occupation ) <-[r:esco__isEssentialSkillFor|esco__isOptionalSkillFor]- (sm:esco__Skill ) -[:skos__broader]->(dm) where dm.skos__notation starts with 'S' and not (dm.uri in matchingSkills) 
WITH o, description, allEssentialSkills, matchingSkills,matchingSkillDetails,
        collect(distinct dm.uri) as MissingSkillsUris,
        collect(distinct{uri:dm.uri,
                         skill:dm.skos__prefLabel,
                         alternativeLabels:coalesce( dm.skos__altLabel,[]),
                         type:'',
                         skillReusability:'', 
                         relationshipType:case({uri:dm.uri,rel:'esco__isEssentialSkillFor'} in allEssentialSkills) when true then 'essential' else 'optional' end,
                         lastModified:datetime()}) as missingSkills
return 
{ uri:o.uri, occupation:o.skos__prefLabel, description:description,
  jobProfileTitle:o.skos__prefLabel, jobProfileUri:'', alternativeLabels:coalesce(o.skos__altLabel,[]), lastModified:o.dct__modified, matchingSkills:case size(matchingSkills) when 0 then [] else matchingSkillDetails end, missingSkills:missingSkills } as results

2 possible replacement.
MATCH (o:esco__Occupation ) <-[r:esco__isEssentialSkillFor|esco__isOptionalSkillFor]- (s:esco__Skill ) -[:skos__broaderTransitive]->(d) -[:skos__broader]-(c) where not exists (c.skos__notation) and d.skos__notation starts with 'S' and o.uri = 'http://data.europa.eu/esco/occupation/9581c837-fee7-4068-9618-1116fd93d723'
WITH o, collect({uri:d.uri, rel:type(r)}  ) as allSkillDets, collect(d.uri) as allSkillUris
WITH o, allSkillDets, allSkillUris, [ x in allSkillDets WHERE x.rel ='esco__isEssentialSkillFor' ] as allEssentialSkills
OPTIONAL MATCH(d:skos__Concept) where d.uri in allSkillUris and d.uri in ['http://data.europa.eu/esco/skill/S4.4.3','http://data.europa.eu/esco/skill/S2.8.1','http://data.europa.eu/esco/skill/S4.2.1','http://data.europa.eu/esco/skill/S1.13.3']
WITH o, allEssentialSkills, allSkillUris,
        collect(distinct d.uri) as matchingSkills,
        collect(distinct{uri:d.uri,
                         skill:d.skos__prefLabel,
                         alternativeLabels:coalesce(d.skos__altLabel,[]),
                         type:'',
                         skillReusability:'',
                         relationshipType:case({uri:d.uri,rel:'esco__isEssentialSkillFor'} in allEssentialSkills) when true then 'essential' else 'optional' end,
                         lastModified:'' }) as matchingSkillDetails
OPTIONAL MATCH(dm:skos__Concept) where dm.uri in allSkillUris and not dm.uri in ['http://data.europa.eu/esco/skill/S4.4.3','http://data.europa.eu/esco/skill/S2.8.1','http://data.europa.eu/esco/skill/S4.2.1','http://data.europa.eu/esco/skill/S1.13.3']
WITH o, allEssentialSkills, matchingSkills,matchingSkillDetails,
        collect(distinct dm.uri) as MissingSkillsUris,
        collect(distinct{uri:dm.uri,
                         skill:dm.skos__prefLabel,
                         alternativeLabels:coalesce( dm.skos__altLabel,[]),
                         type:'',
                         skillReusability:'', 
                         relationshipType:case({uri:dm.uri,rel:'esco__isEssentialSkillFor'} in allEssentialSkills) when true then 'essential' else 'optional' end,
                         lastModified:''}) as missingSkills
return 
{ uri:o.uri, occupation:o.skos__prefLabel, 
  jobProfileTitle:o.skos__prefLabel, jobProfileUri:'', alternativeLabels:coalesce(o.skos__altLabel,[]), lastModified:o.dct__modified, matchingSkills:case size(matchingSkills) when 0 then [] else matchingSkillDetails end, missingSkills:missingSkills } as results


```

##Questions
