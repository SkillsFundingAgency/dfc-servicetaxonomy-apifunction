
# GetSkillsByLabel Query Notes

## Current Query (V2)

```
with toLower('toxic') as lowerlabel
   
call db.index.fulltext.queryNodes("SkillLabels", "skos__prefLabel:"+ "*" + lowerlabel + "*") yield node, score
match (node:SkillLabel)-[:hasPrefLabel|:hasAltLabel|:hasHiddenLabel]-(s:esco__Skill)-[:hasAltLabel|:hasHiddenLabel]-(AltLabels),(skillreuselevel)-[:esco__skillReuseLevel]-(s)-[:esco__skillType]->(skilltype)
with collect(distinct s) as skills, avg(score) as averageScore, AltLabels, node, lowerlabel, skilltype, skillreuselevel
with collect(distinct AltLabels.skos__prefLabel) as allAltLabels, skills, lowerlabel, averageScore, skilltype, skillreuselevel
unwind skills as sk
optional match (altLabel)-[:hasAltLabel]-(sk)
with collect(distinct altLabel.skos__prefLabel) as matchingAltLabels, allAltLabels, lowerlabel, skills, skilltype,
sk, averageScore, skillreuselevel
optional match(prefLabel)-[:hasPrefLabel]-(sk)
with collect(distinct prefLabel.skos__prefLabel) as matchingPrefLabels, matchingAltLabels, allAltLabels, lowerlabel, skills, sk, averageScore, skilltype, skillreuselevel
optional match(hiddenLabel)-[:hasHiddenLabel]-(sk)
with collect(distinct hiddenLabel.skos__prefLabel) as matchingHiddenLabels, matchingPrefLabels, matchingAltLabels, allAltLabels, lowerlabel, skills, sk, averageScore, skilltype, skillreuselevel
with {Value: [prefLab in matchingPrefLabels where toLower(prefLab) contains lowerlabel]} as matchingPrefLabelCount, {Value: [altLab in matchingAltLabels where toLower(altLab) contains lowerlabel]} as matchingAltLabelCount, {Value: [hiddenLab in matchingHiddenLabels where toLower(hiddenLab) contains lowerlabel]} as matchingHiddenLabelCount, sk, allAltLabels, matchingAltLabels, matchingPrefLabels, matchingHiddenLabels, lowerlabel, skills, averageScore, skilltype, skillreuselevel
with {Skills: case 'true' when 'true' then sk when 'false' then [val in skills where size(matchingPrefLabelCount.Value) > 0] end } as filteredResults, matchingPrefLabels, matchingAltLabels, matchingPrefLabelCount, allAltLabels, lowerlabel, {Value: averageScore + (size(matchingPrefLabelCount.Value) * 10) + ((size(matchingAltLabelCount.Value) - size(matchingHiddenLabelCount.Value))/size(allAltLabels) * size(matchingAltLabelCount.Value)) } as boostedScore, skilltype, skillreuselevel, matchingHiddenLabels
unwind filteredResults.Skills as s
with {skills:collect(
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
  alternativeLabels:allAltLabels,
  lastModified:s.dct__modified,
  matches:
  {
    skill:[prefLab in matchingPrefLabels where toLower(prefLab) contains lowerlabel],
    alternativeLabels:[altLab in matchingAltLabels where toLower(altLab) contains lowerlabel],
    hiddenLabels:[hiddenLab in matchingHiddenLabels where toLower(hiddenLab) contains lowerlabel]
  },
  Score:boostedScore.Value
}
)} as skills
return skills
```


## Previous Query (V1)

Here's the current query, formatted and with params replaced by literals...

This version includes skill pref label in matches - it the one in use currently

```
with toLower('processing') as lowerlabel
match (ca:skos__Concept) <-[:skos__broader]- (s:esco__Skill) where ca.skos__notation starts with 'S'
and ( toLower(ca.skos__prefLabel) contains lowerlabel or
case toLower('true')
  when 'true' then
    toLower(s.skos__prefLabel) contains lowerlabel
  else
    false
  end )
with distinct ca as c, collect(s.skos__prefLabel) as skillLabels, lowerlabel
with { skills:collect(
{
  uri:c.uri,
  skill:c.skos__prefLabel,
  skillType: '',
  skillReusability: '',
  alternativeLabels:[],
  lastModified:'0001-01-01T00:00:00Z',
  matches:
  {
    skill:[preflab in c.skos__prefLabel where toLower(preflab) contains lowerlabel],
    alternativeLabels:coalesce([altlab in skillLabels where toLower(altlab) contains lowerlabel],[]),
    hiddenLabels:[]
  }
}
)} as skills 
return skills
```

This version includes skill pref and alt labels in matches - needs some work to collect all relevant labels

```
with toLower('processing') as lowerlabel
match (ca:skos__Concept) <-[:skos__broader]- (s:esco__Skill) where ca.skos__notation starts with 'S'
and ( toLower(ca.skos__prefLabel) contains lowerlabel or
case toLower('true')
  when 'true' then
    toLower(s.skos__prefLabel) contains lowerlabel
    or any(alt in s.skos__altLabel where toLower(alt) contains lowerlabel)
    or any(hidden in s.skos__hiddenLabel where toLower(hidden) contains lowerlabel)
  else
    false
  end )
with distinct ca as c, s, [s.skos__prefLabel]+s.skos__altLabel as skillLabels, lowerlabel
with { skills:collect(
{
  uri:c.uri,
  skill:c.skos__prefLabel,
  skillType: '',
  skillReusability: '',
  alternativeLabels:[],
  lastModified:'0001-01-01T00:00:00Z',
  matches:
  {
    skill:[preflab in c.skos__prefLabel where toLower(preflab) contains lowerlabel],
    alternativeLabels:coalesce([altlab in skillLabels where toLower(altlab) contains lowerlabel],[]),
    hiddenLabels:coalesce([hidlab in c.skos__hiddenLabel where toLower(hidlab) contains lowerlabel],[])
  }
}
)} as skills 
return skills
```


This is the prev version of V1 (using low level skills)

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
