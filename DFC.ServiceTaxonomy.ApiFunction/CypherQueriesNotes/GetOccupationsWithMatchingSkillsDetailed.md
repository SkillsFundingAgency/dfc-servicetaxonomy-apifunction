﻿
# GetOccupationsWithMatchingSkillsDetailed Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
MATCH (soc:ncs__SOCCode)-[:ncs__hasSocCode]-(j:ncs__JobProfile)--(o:esco__Occupation )<-[r:esco__isEssentialSkillFor|:esco__isOptionalSkillFor]- (s:esco__Skill ) 
WHERE s.uri in ['http://data.europa.eu/esco/skill/9436db78-4331-495b-a97d-223fd246de2f'] 
OPTIONAL MATCH (o)<-[re:esco__isEssentialSkillFor]-(sx)-[rst:esco__skillType]-(st:skos__Concept),(sm:esco__Skill )-[rrl:esco__skillReuseLevel]-(srl:skos__Concept) 
WHERE sx.uri in ['http://data.europa.eu/esco/skill/9436db78-4331-495b-a97d-223fd246de2f']  WITH soc, o, s, st, sm, rrl, srl, r, sx, collect(
{
	uri:sx.uri, 
	skill:sx.skos__prefLabel, 
	alternativeLabels:coalesce(sx.skos__altLabel,[]), 
	type:case st.skos__prefLabel when 'skill' then 'competency' when 'knowledge' then 'knowledge' end, 
	skillReusability:srl.skos__prefLabel, 
	relationshipType:case type(r) when 'esco__isEssentialSkillFor' then 'essential' when 'esco__isOptionalSkillFor' then 'optional' end, 
	lastModified:sx.dct__modified
}) as MatchingEssentialSkills 
OPTIONAL MATCH (o)<-[:esco__isOptionalSkillFor]-(sx)-[rst:esco__skillType]-(st:skos__Concept),(sm:esco__Skill )-[rrl:esco__skillReuseLevel]-(srl:skos__Concept) 
WHERE sx.uri in ['http://data.europa.eu/esco/skill/9436db78-4331-495b-a97d-223fd246de2f']  
WITH soc, o, s, st, sm, rrl, srl, r, collect(
{
	uri:sx.uri, 
	skill:sx.skos__prefLabel, 
	alternativeLabels:coalesce(sx.skos__altLabel,[]), 
	type:case st.skos__prefLabel when 'skill' then 'competency' when 'knowledge' then 'knowledge' end, 
	skillReusability:case srl.skos__prefLabel when 'cross-sector skills and competences' then 'cross-sectoral' when 'sector specific skills and competences' then 'sector-specific' when 'occupation specific skills and competences' then 'occupation-specific' when 'transversal skills and competences' then 'transversal' end, 
	relationshipType:'optional', lastModified:sx.dct__modified
}) as MatchingOptionalSkills, MatchingEssentialSkills 
MATCH (j:ncs__JobProfile) -- (o)<-[:esco__isEssentialSkillFor]-(sa) WHERE size(MatchingOptionalSkills) + size(MatchingEssentialSkills) >= 2 
WITH o, collect(
{
	uri:sa.uri, 
	skill:sa.skos__prefLabel, 
	alternativeLabels:coalesce(sa.skos__altLabel,[]), 
	type:case st.skos__prefLabel when 'skill' then 'competency' when 'knowledge' then 'knowledge' end, 
	skillReusability:case srl.skos__prefLabel when 'cross-sector skills and competences' then 'cross-sectoral' when 'sector specific skills and competences' then 'sector-specific' when 'occupation specific skills and competences' then 'occupation-specific' when 'transversal skills and competences' then 'transversal' end, 
	relationshipType:'essential', 
	lastModified:sa.dct__modified
}) as AllEssentialSkills, s, st, sm, rrl, srl, soc, r, MatchingOptionalSkills,MatchingEssentialSkills, j.skos__prefLabel as JobProfile, j.uri as JobProfileUri, j.ncs__Description as JobProfileDescription 
OPTIONAL MATCH (j:ncs__JobProfile) -- (o)<-[:esco__isOptionalSkillFor]-(sa) 
WITH o, collect(
{
	uri:sa.uri, 
	skill:sa.skos__prefLabel, 
	alternativeLabels:coalesce(sa.skos__altLabel,[]), 
	type:case st.skos__prefLabel when 'skill' then 'competency' when 'knowledge' then 'knowledge' end, skillReusability:case srl.skos__prefLabel when 'cross-sector skills and competences' then 'cross-sectoral' when 'sector specific skills and competences' then 'sector-specific' when 'occupation specific skills and competences' then 'occupation-specific' when 'transversal skills and competences' then 'transversal' end, 
	relationshipType:'optional', 
	lastModified:sa.dct__modified 
}) as AllOptionalSkills, AllEssentialSkills, soc, MatchingOptionalSkills,MatchingEssentialSkills, JobProfile, JobProfileUri, JobProfileDescription 
RETURN 
{ 
	matchingOccupations: collect(
		{
			uri:o.uri, 
			occupation:o.skos__prefLabel, 
			jobProfileTitle:JobProfile, 
			jobProfileUri:JobProfileUri, 
			jobProfileDescription:JobProfileDescription, 
			matchingEssentialSkills:MatchingEssentialSkills, 
			matchingOptionalSkills:MatchingOptionalSkills, 
			socCode:soc.skos__prefLabel, 
			occupationEssentialSkills:AllEssentialSkills, 
			occupationOptionalSkills:AllOptionalSkills, 
			lastModified:o.dct__modified}
		)
} as results
```

##Questions