
# GetOccupationsWithMatchingSkillsDetailed Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
MATCH (soc:ncs__SOCCode)-[:ncs__hasSocCode]-(j:ncs__JobProfile)--(o:esco__Occupation )<-[r:esco__isEssentialSkillFor|:esco__isOptionalSkillFor]- (s:esco__Skill ), (jc:ncs__JobCategory)-[:ncs__hasJobProfile]-(j) 
WHERE s.uri in $skillList WITH soc, o, s, r, collect({name:jc.skos__prefLabel,uri:$websiteHost + jc.WebsiteURI}) as JobCategories
OPTIONAL MATCH (o)<-[re:esco__isEssentialSkillFor]-(sx)-[rst:esco__skillType]-(st:skos__Concept),(sm:esco__Skill )-[rrl:esco__skillReuseLevel]-(srl:skos__Concept) 
WHERE sx.uri in $skillList 
WITH soc, o, s, st, sm, rrl, srl, r, sx, collect(
{
	uri:sx.uri, 
	skill:head(sx.skos__prefLabel), 
	alternativeLabels:coalesce(sx.skos__altLabel,[]), 
	type:case head(st.skos__prefLabel) 
			when 'skill' then 'competency' 
			when 'knowledge' then 'knowledge' 
		end, 
	skillReusability:srl.skos__prefLabel, 
	relationshipType:case type(r) 
						when 'esco__isEssentialSkillFor' then 'essential' 
						when 'esco__isOptionalSkillFor' then 'optional' 
					end, 
			lastModified:head(sx.dct__modified)
}) as MatchingEssentialSkills, JobCategories
OPTIONAL MATCH (o)<-[:esco__isOptionalSkillFor]-(sx)-[rst:esco__skillType]-(st:skos__Concept),(sm:esco__Skill )-[rrl:esco__skillReuseLevel]-(srl:skos__Concept) 
WHERE sx.uri in $skillList
WITH soc, o, s, st, sm, rrl, srl, r, collect(
{
	uri:sx.uri, 
	skill:head(sx.skos__prefLabel), 
	alternativeLabels:coalesce(sx.skos__altLabel,[]), 
	type:case head(st.skos__prefLabel) 
			when 'skill' then 'competency' 
			when 'knowledge' then 'knowledge' 
		end, 
	skillReusability:case head(srl.skos__prefLabel) 
						when 'cross-sector skills and competences' then 'cross-sectoral' 
						when 'sector specific skills and competences' then 'sector-specific' 
						when 'occupation specific skills and competences' then 'occupation-specific' 
						when 'transversal skills and competences' then 'transversal' 
					end, relationshipType:'optional', 
	lastModified:head(sx.dct__modified)}) as MatchingOptionalSkills, MatchingEssentialSkills, JobCategories
MATCH (j:ncs__JobProfile) -- (o)<-[:esco__isEssentialSkillFor]-(sa) 
WHERE size(MatchingOptionalSkills) + size(MatchingEssentialSkills) >= $minimumMatchingSkills 
WITH o, collect(
{
	uri:sa.uri, 
	skill:head(sa.skos__prefLabel), 
	alternativeLabels:coalesce(sa.skos__altLabel,[]), 
	type:case head(st.skos__prefLabel) 
			when 'skill' then 'competency' 
			when 'knowledge' then 'knowledge' 
		end, 
	skillReusability:case head(srl.skos__prefLabel) 
						when 'cross-sector skills and competences' then 'cross-sectoral' 
						when 'sector specific skills and competences' then 'sector-specific' 
						when 'occupation specific skills and competences' then 'occupation-specific' 
						when 'transversal skills and competences' then 'transversal' 
					end, relationshipType:'essential', 
	lastModified:head(sa.dct__modified)
}) as AllEssentialSkills, s, st, sm, rrl, srl, soc, r, MatchingOptionalSkills, MatchingEssentialSkills, j.skos__prefLabel as JobProfile, j.uri as JobProfileUri, j.ncs__Description as JobProfileDescription, JobCategories 
OPTIONAL MATCH (j:ncs__JobProfile) -- (o)<-[:esco__isOptionalSkillFor]-(sa) 
WITH o, collect(
{
	uri:sa.uri, 
	skill:head(sa.skos__prefLabel), 
	alternativeLabels:coalesce(sa.skos__altLabel,[]), 
	type:case head(st.skos__prefLabel) 
			when 'skill' then 'competency' 
			when 'knowledge' then 'knowledge' 
		end, 
	skillReusability:case head(srl.skos__prefLabel) 
						when 'cross-sector skills and competences' then 'cross-sectoral' 
						when 'sector specific skills and competences' then 'sector-specific' 
						when 'occupation specific skills and competences' then 'occupation-specific' 
						when 'transversal skills and competences' then 'transversal' 
					end, relationshipType:'optional', 
	lastModified:head(sa.dct__modified) }) as AllOptionalSkills, AllEssentialSkills, soc, MatchingOptionalSkills,MatchingEssentialSkills, JobProfile, JobProfileUri, JobProfileDescription, JobCategories
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
		lastModified:o.dct__modified,
		jobCategories:JobCategories
	}) 
} as results
```

##Questions
