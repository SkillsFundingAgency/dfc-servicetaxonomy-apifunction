
# GetOccupationsWithMatchingSkillsDetailed Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
MATCH (soc:ncs__SOCCode)-[:ncs__hasSocCode]-(j:ncs__JobProfile)--(o:esco__Occupation )<-[r:esco__isEssentialSkillFor|:esco__isOptionalSkillFor]- (s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
MATCH(jc:ncs__JobCategory)-[:ncs__hasJobProfile]-(j)
WITH COLLECT({Uri:'https://nationalcareers.service.gov.uk/' + jc.ncs__WebsiteURI, Name:jc.skos__prefLabel}) as JobCategories, soc, o, s, st, srl, rrl
OPTIONAL MATCH (soc:ncs__SOCCode)-[:ncs__hasSocCode]-(j:ncs__JobProfile)--(o:esco__Occupation )<-[r:esco__isOptionalSkillFor]- (s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
 WITH soc, o, collect(distinct
{
    uri:s.uri,
    skill:s.skos__prefLabel,
    alternativeLabels:s.skos__altLabel,
    type:case head(st.skos__prefLabel)
            when 'skill' then 'competency'
            when 'knowledge' then 'knowledge'
        end,
    relationshipType:'optional',
    skillReusability:srl.skos__prefLabel,
    lastModified:head(s.dct__modified)
}) as MatchingOptionalSkills, JobCategories
OPTIONAL MATCH (soc:ncs__SOCCode)-[:ncs__hasSocCode]-(j:ncs__JobProfile)--(o:esco__Occupation )<-[r:esco__isEssentialSkillFor]- (s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
 WITH soc, o, collect(distinct
{
    uri:s.uri,
    skill:s.skos__prefLabel,
    alternativeLabels:s.skos__altLabel,
    type:case head(st.skos__prefLabel)
            when 'skill' then 'competency'
            when 'knowledge' then 'knowledge'
        end,
    relationshipType:'essential',
    skillReusability:srl.skos__prefLabel,
    lastModified:head(s.dct__modified)
}) as MatchingEssentialSkills, MatchingOptionalSkills, JobCategories
MATCH (j:ncs__JobProfile)--(o)<-[:esco__isEssentialSkillFor]-(sa)-[orrl:esco__skillReuseLevel]-(osrl:skos__Concept),(s)-[ocrst:esco__skillType]-(ost:skos__Concept) WHERE size(MatchingOptionalSkills) + size(MatchingEssentialSkills) >= 1
WITH soc, o, collect(distinct
{
    uri:sa.uri,
    skill:sa.skos__prefLabel,
    alternativeLabels:sa.skos__altLabel,
    type:case head(ocrst.skos__prefLabel)
            when 'skill' then 'competency'
            when 'knowledge' then 'knowledge'
        end,
    relationshipType:'essential',
    skillReusability:orrl.skos__prefLabel,
    lastModified:head(sa.dct__modified)
}) as AllEssentialSkills,MatchingEssentialSkills,MatchingOptionalSkills, JobCategories, j.skos__prefLabel as JobProfile, j.uri as JobProfileUri, j.ncs__Description as JobProfileDescription
OPTIONAL MATCH (j:ncs__JobProfile)--(o)<-[:esco__isOptionalSkillFor]-(sa)-[orrl:esco__skillReuseLevel]-(osrl:skos__Concept),(s)-[ocrst:esco__skillType]-(ost:skos__Concept) WITH soc, o, collect(distinct
{
    uri:sa.uri,
    skill:sa.skos__prefLabel,
    alternativeLabels:sa.skos__altLabel,
    type:case head(ocrst.skos__prefLabel)
            when 'skill' then 'competency'
            when 'knowledge' then 'knowledge'
        end,
    relationshipType:'optional',
    skillReusability:orrl.skos__prefLabel,
    lastModified:head(sa.dct__modified)
}) as AllOptionalSkills, AllEssentialSkills, MatchingEssentialSkills, MatchingOptionalSkills, JobCategories, JobProfile, JobProfileUri, JobProfileDescription
RETURN
{
    matchingOccupations: collect({uri:o.uri, 
    occupation:o.skos__prefLabel,
    jobProfileTitle:JobProfile,
    jobProfileUri:JobProfileUri,
    jobProfileDescription:JobProfileDescription,
    matchingEssentialSkills:MatchingEssentialSkills,
    matchingOptionalSkills:MatchingOptionalSkills,
    socCode:soc.skos__prefLabel,
    occupationEssentialSkills:AllEssentialSkills,
    occupationOptionalSkills:AllOptionalSkills,
    jobCategories:JobCategories,
    lastModified:o.dct__modified})
} as results
````
Previous Query
```
MATCH (soc:ncs__SOCCode)-[:ncs__hasSocCode]-(j:ncs__JobProfile)--(o:esco__Occupation )<-[r:esco__isEssentialSkillFor|:esco__isOptionalSkillFor]- (s:esco__Skill ), (jc:ncs__JobCategory)-[:ncs__hasJobProfile]-(j) 
WHERE s.uri in ['http://data.europa.eu/esco/skill/9436db78-4331-495b-a97d-223fd246de2f']  WITH soc, o, s, r, collect({name:jc.skos__prefLabel,uri:'mytesthost.com' + jc.WebsiteURI}) as JobCategories
OPTIONAL MATCH (o)<-[re:esco__isEssentialSkillFor]-(sx)-[rst:esco__skillType]-(st:skos__Concept),(sm:esco__Skill )-[rrl:esco__skillReuseLevel]-(srl:skos__Concept) 
WHERE sx.uri in ['http://data.europa.eu/esco/skill/9436db78-4331-495b-a97d-223fd246de2f']  
WITH soc, o, s, st, sm, rrl, srl, r, sx, collect(
{
	uri:sx.uri, 
	skill:sx.skos__prefLabel, 
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
WHERE sx.uri in ['http://data.europa.eu/esco/skill/9436db78-4331-495b-a97d-223fd246de2f'] 
WITH soc, o, s, st, sm, rrl, srl, r, collect(
{
	uri:sx.uri, 
	skill:sx.skos__prefLabel, 
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
WHERE size(MatchingOptionalSkills) + size(MatchingEssentialSkills) >= 1 
WITH o, collect(
{
	uri:sa.uri, 
	skill:sa.skos__prefLabel, 
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
	skill:sa.skos__prefLabel, 
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
