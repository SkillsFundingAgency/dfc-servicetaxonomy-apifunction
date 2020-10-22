# GetAllSkillsForOccupation

##N ew Match Skills Query
```
MATCH (o:esco__Occupation)-[:esco__isOptionalSkillFor|esco__isEssentialSkillFor]-(s:esco__Skill)-[:skos__broaderTransitive]->(d:skos__Concept) where o.uri = 'http://data.europa.eu/esco/occupation/3a55ef85-5abf-48e2-884b-5efaf881bfb1' 
MATCH (d)<-[:skos__broader]-(c) where not exists (c.skos__notation) AND d.skos__notation starts with 'S'
WITH distinct d as ddistinct, o
WITH collect({uri:ddistinct.uri, skill:ddistinct.skos__prefLabel, alternativeLabels:[], type:'competency', skillReusability:'cross-sectoral', lastModified:ddistinct.dct__modified }) as skills,o 
RETURN { uri:o.uri, occupation:o.skos__prefLabel, alternativeLabels:coalesce(o.skos__altLabel,[]), lastModified:o.dct__modified, skills:skills }
```

## Query

```
MATCH (o: esco__Occupation)<-[r: esco__isEssentialSkillFor| esco__isOptionalSkillFor]-(s: esco__Skill)-[rst: esco__skillType]-(st: skos__Concept),
      (s: esco__Skill)-[rrl: esco__skillReuseLevel]-(srl: skos__Concept)
WHERE o.uri = $uri WITH o,
      collect(
      {
          uri: s.uri,
          skill: s.skos__prefLabel,
          alternativeLabels: coalesce(s.skos__altLabel,[]),
          type: case st.skos__prefLabel when 'skill' then 'competency' when 'knowledge' then 'knowledge' end,
          skillReusability: case srl.skos__prefLabel when 'cross-sector skills and competences' then 'cross-sectoral' when 'sector specific skills and competences' then 'sector-specific' when 'occupation specific skills and competences' then 'occupation-specific' when 'transversal skills and competences' then 'transversal' end,
          relationshipType: case type(r) when 'esco__isEssentialSkillFor' then 'essential' when 'esco__isOptionalSkillFor' then 'optional' end,
          lastModified: s.dct__modified
      }
      ) as skills 
RETURN
{
      uri: o.uri,
      occupation: o.skos__prefLabel,
      alternativeLabels: coalesce(o.skos__altLabel,[]),
      lastModified: o.dct__modified,
      skills: skills
}
```