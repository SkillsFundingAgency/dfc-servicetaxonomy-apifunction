
# GetSkillsByLabel Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
match (skillreuselevel)<-[esco__skillReuseLevel]-(s:esco__Skill)-[:esco__skillType]->(skilltype)
where head(s.skos__prefLabel) contains 'toxic' or 
case toLower('true')
  when 'true' then
    any(alt in s.skos__altLabel where alt contains 'toxic')
    or any(hidden in s.skos__hiddenLabel where hidden contains 'toxic')
  else
    false
  end
with { skills:collect(
{
  uri:s.uri,
  skill:head(s.skos__prefLabel),
  skillType: case head(skilltype.skos__prefLabel)
    when 'skill' then 'competency'
    when 'knowledge' then 'knowledge' end,
  skillReusability: case head(skillreuselevel.skos__prefLabel)
    when 'cross-sector skills and competences' then 'cross-sectoral'
    when 'sector specific skills and competences' then 'sector-specific'
    when 'occupation specific skills and competences' then 'occupation-specific'
    when 'transversal skills and competences' then 'transversal' end,
  alternativeLabels:coalesce(s.skos__altLabel,[]),
  lastModified:head(s.dct__modified),
  matches:
  {
    occupation:[preflab in s.skos__prefLabel where preflab contains 'toxic'],
    alternativeLabels:coalesce([altlab in s.skos__altLabel where altlab contains 'toxic'],[]),
    hiddenLabels:coalesce([hidlab in s.skos__hiddenLabel where hidlab contains 'toxic'],[])
  }
}
)} as skills 
return skills
```

##Questions

All skills, or skills with a relationship to an ESCO occupation that has a mapping to a job profile?
return hidden labels?