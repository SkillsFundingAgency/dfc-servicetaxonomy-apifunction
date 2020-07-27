
# GetOccupationsWithMatchingSkillsDetailed Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
MATCH (soc:SOCCode)-[hasSocCode]-(j:JobProfile)--(o:esco__Occupation )<-[r:esco__isEssentialSkillFor|esco__isOptionalSkillFor]-(s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) 
WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
OPTIONAL MATCH(jc:JobCategory)-[:hasJobProfile]-(j)
WITH COLLECT({Uri:'https://nationalcareers.service.gov.uk/' + jc.WebsiteURI, Name:jc.skos__prefLabel}) as JobCategories, soc, o, s, st, srl, rrl, j.skos__prefLabel as JobProfile, j.uri as JobProfileUri, j.Description as JobProfileDescription
OPTIONAL MATCH (soc:SOCCode)-[:hasSocCode]-(j:JobProfile)--(o:esco__Occupation)<-[r:esco__isOptionalSkillFor]-(s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) 
WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
WITH soc, o, collect(distinct
{
    uri:s.uri,
    skill:s.skos__prefLabel,
    alternativeLabels:s.skos__altLabel,
    type:case st.skos__prefLabel
            when 'skill' then 'competency'
            when 'knowledge' then 'knowledge'
        end,
    relationshipType:'optional',
    skillReusability:srl.skos__prefLabel,
    lastModified:s.dct__modified
}) as MatchingOptionalSkills, JobCategories, JobProfile, JobProfileUri, JobProfileDescription
OPTIONAL MATCH (soc:SOCCode)-[:hasSocCode]-(j:JobProfile)--(o:esco__Occupation)<-[r:esco__isEssentialSkillFor]-(s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) 
WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
WITH soc, o, collect(distinct
{
    uri:s.uri,
    skill:s.skos__prefLabel,
    alternativeLabels:s.skos__altLabel,
    type:case st.skos__prefLabel
            when 'skill' then 'competency'
            when 'knowledge' then 'knowledge'
        end,
    relationshipType:'essential',
    skillReusability:srl.skos__prefLabel,
    lastModified:s.dct__modified
}) as MatchingEssentialSkills, MatchingOptionalSkills, JobCategories, JobProfile, JobProfileUri, JobProfileDescription
OPTIONAL MATCH (j:JobProfile)--(o)<-[:esco__isEssentialSkillFor]-(sa:esco__Skill)-[orrl:esco__skillReuseLevel]-(osrl:skos__Concept),(sa)-[ocrst:esco__skillType]-(ost:skos__Concept) 
WHERE size(MatchingOptionalSkills) + size(MatchingEssentialSkills) >= $minimumMatchingSkills
WITH soc, o, collect(distinct
{
    uri:sa.uri,
    skill:sa.skos__prefLabel,
    alternativeLabels:sa.skos__altLabel,
    type:case ost.skos__prefLabel
            when 'skill' then 'competency'
            when 'knowledge' then 'knowledge'
        end,
    relationshipType:'essential',
    skillReusability:ost.skos__prefLabel,
    lastModified:sa.dct__modified
}) as AllEssentialSkills,MatchingEssentialSkills,MatchingOptionalSkills, JobCategories, JobProfile, JobProfileUri, JobProfileDescription
OPTIONAL MATCH (j:JobProfile)--(o)<-[:esco__isOptionalSkillFor]-(sa:esco__Skill)-[orrl:esco__skillReuseLevel]-(osrl:skos__Concept),(sa)-[ocrst:esco__skillType]-(ost:skos__Concept) 
WITH soc, o, collect(distinct
{
    uri:sa.uri,
    skill:sa.skos__prefLabel,
    alternativeLabels:sa.skos__altLabel,
    type:case ost.skos__prefLabel
            when 'skill' then 'competency'
            when 'knowledge' then 'knowledge'
        end,
    relationshipType:'optional',
    skillReusability:ost.skos__prefLabel,
    lastModified:sa.dct__modified
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
MATCH (soc:SOCCode)-[:hasSocCode]-(j:JobProfile)--(o:esco__Occupation )<-[r:esco__isEssentialSkillFor|:esco__isOptionalSkillFor]- (s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
MATCH(jc:JobCategory)-[:hasJobProfile]-(j)
WITH COLLECT({Uri:'https://nationalcareers.service.gov.uk/' + jc.WebsiteURI, Name:jc.skos__prefLabel}) as JobCategories, soc, o, s, st, srl, rrl
OPTIONAL MATCH (soc:SOCCode)-[:hasSocCode]-(j:JobProfile)--(o:esco__Occupation )<-[r:esco__isOptionalSkillFor]- (s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
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
OPTIONAL MATCH (soc:SOCCode)-[:hasSocCode]-(j:JobProfile)--(o:esco__Occupation )<-[r:esco__isEssentialSkillFor]- (s:esco__Skill)-[rrl:esco__skillReuseLevel]-(srl:skos__Concept),(s)-[rst:esco__skillType]-(st:skos__Concept) WHERE s.uri in ['http://data.europa.eu/esco/skill/f8180a0a-fba3-43de-bab2-f25cb9d64ad7']
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
MATCH (j:JobProfile)--(o)<-[:esco__isEssentialSkillFor]-(sa)-[orrl:esco__skillReuseLevel]-(osrl:skos__Concept),(s)-[ocrst:esco__skillType]-(ost:skos__Concept) WHERE size(MatchingOptionalSkills) + size(MatchingEssentialSkills) >= 1
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
}) as AllEssentialSkills,MatchingEssentialSkills,MatchingOptionalSkills, JobCategories, j.skos__prefLabel as JobProfile, j.uri as JobProfileUri, j.Description as JobProfileDescription
OPTIONAL MATCH (j:JobProfile)--(o)<-[:esco__isOptionalSkillFor]-(sa)-[orrl:esco__skillReuseLevel]-(osrl:skos__Concept),(s)-[ocrst:esco__skillType]-(ost:skos__Concept) WITH soc, o, collect(distinct
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
```

##Questions
