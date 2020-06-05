﻿# GetAllSkillsForOccupation

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