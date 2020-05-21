
# GetJobProfilesBySearchTerm Query Notes

## Current Query

```
with 'baker' as lowerlabel
call db.index.fulltext.queryNodes("JobProfiles", "skos__prefLabel:"+ "*" + lowerlabel + "*") yield node, score
MATCH (node)-[:relatedOccupation]-(oc:esco__Occupation)
WITH node,oc
{
	ResultItemTitle:node.skos__prefLabel,
	ResultItemAlternativeTitle:REDUCE(s = HEAD(oc.skos__altLabel), n IN TAIL( oc.skos__altLabel) | s + ', ' + n), 
	ResultItemOverview:node.Description, 
	ResultItemSalaryRange:'£' + node.SalaryStarter + ' to ' + '£' + node.SalaryExperienced, 
	ResultItemUrlName:'somedomain.com' + 'getjobprofilesbysearchterm/execute/' + node.JobProfileWebsiteUrl, 
	JobProfileCategories:['ToDo']
} as jobProfiles 
WITH 
{ 
	TotalResults:size(collect(jobProfiles)), 
	Profiles:collect(jobProfiles)
} AS aggregatedResults 
UNWIND aggregatedResults.Profiles AS unwoundProfiles 
WITH unwoundProfiles, aggregatedResults 
SKIP (1-1)*10
LIMIT 10 
WITH aggregatedResults
{
	PageSize:10, 
	CurrentPage:1, 
	Count:aggregatedResults.TotalResults,
	Results:collect(unwoundProfiles)
} AS finalResult 
RETURN finalResult
```