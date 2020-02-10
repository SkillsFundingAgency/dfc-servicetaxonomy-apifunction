
# GetSkillById Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
WITH toLower('http://data.europa.eu/esco/skill/15d76317-c71a-4fa2-aadc-2ecc34e627b7') as loweruri
MATCH (skillreuselevel)<-[:esco__skillReuseLevel]-(s:esco__Skill)-[:esco__skillType]->(skilltype) 
WHERE toLower(s.uri) = loweruri
WITH  
{ 
	uri:s.uri, 
    skill:head(s.skos__prefLabel), 
    skillType:
      case head(skilltype.skos__prefLabel) 
          when 'skill' then 'competency' 
          when 'knowledge' then 'knowledge' 
      end, 
    alternativeLabels:coalesce(s.skos__altLabel,[]),
    skillReusability: 
    	case head(skillreuselevel.skos__prefLabel)
        	when 'cross-sector skills and competences' then 'cross-sectoral'
            when 'sector specific skills and competences' then 'sector-specific'
            when 'occupation specific skills and competences' then 'occupation-specific'
            when 'transversal skills and competences' then 'transversal'
		end,
	lastModified:head(s.dct__modified)
} as skill
return skill
LIMIT 1
```

##Questions
