
# GetSkillsByLabel Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
with toLower('toxic') as lowerlabel
match (skillreuselevel)<-[:esco__skillReuseLevel]-(s:esco__Skill)-[:esco__skillType]->(skilltype)
where toLower(s.skos__prefLabel) contains lowerlabel or 
case toLower('true')
  when 'true' then
    any(alt in s.skos__altLabel where toLower(alt) contains lowerlabel)
    or any(hidden in s.skos__hiddenLabel where toLower(hidden) contains lowerlabel)
  else
    false
  end
with { skills:collect(
{
  uri:s.uri,
  skill:s.skos__prefLabel,
  skillType: case skilltype.skos__prefLabel
    when 'skill' then 'competency'
    when 'knowledge' then 'knowledge' end,
  skillReusability: case skillreuselevel.skos__prefLabel
    when 'cross-sector skills and competences' then 'cross-sectoral'
    when 'sector specific skills and competences' then 'sector-specific'
    when 'occupation specific skills and competences' then 'occupation-specific'
    when 'transversal skills and competences' then 'transversal' end,
  alternativeLabels:coalesce(s.skos__altLabel,[]),
  lastModified:s.dct__modified,
  matches:
  {
    skill:[preflab in s.skos__prefLabel where toLower(preflab) contains lowerlabel],
    alternativeLabels:coalesce([altlab in s.skos__altLabel where toLower(altlab) contains lowerlabel],[]),
    hiddenLabels:coalesce([hidlab in s.skos__hiddenLabel where toLower(hidlab) contains lowerlabel],[])
  }
}
)} as skills 
return skills
```

##Questions
