
# GetSkillsByLabel Query Notes

## Current Query

Here's the current query, formatted and with params replaced by literals...

```
match (o:esco__Skill)
where head(o.skos__prefLabel) contains 'toxic' or 
case toLower('true')
  when 'true' then
    any(alt in o.skos__altLabel where alt contains 'toxic')
    or any(hidden in o.skos__hiddenLabel where hidden contains 'toxic')
  else
    false
  end
with { skills:collect(
{
  uri:o.uri,
  skill:head(o.skos__prefLabel),
  alternativeLabels:coalesce(o.skos__altLabel,[]),
  hiddenLabels:coalesce(o.skos__hiddenLabel,[]),
  lastModified:head(o.dct__modified),
  matches:
  {
    occupation:[preflab in o.skos__prefLabel where preflab contains 'toxic'],
    alternativeLabels:coalesce([altlab in o.skos__altLabel where altlab contains 'toxic'],[]),
    hiddenLabels:coalesce([hidlab in o.skos__hiddenLabel where hidlab contains 'toxic'],[])
  }
}
)} as skills 
return skills
```
