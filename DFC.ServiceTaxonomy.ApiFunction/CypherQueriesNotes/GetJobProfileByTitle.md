
# GetJobProfileByTitle Query Notes

## Future Query
```
MATCH (jp:ncs__JobProfile{skos__prefLabel:REPLACE($canonicalName,'-',' ')})-[:ncs__hasSocCode]->(soc:ncs__SOCCode), (jp)-[:ncs__relatedOccupation]-(oc:esco__Occupation) 
OPTIONAL MATCH (dtd:ncs__DayToDayTask)<-[:ncs__hasDayToDayTask]-(jp) 
WITH jp,soc,oc,{tasks:collect(dtd.skos__prefLabel)} as dayToDayTasks
OPTIONAL MATCH (jp)-[:ncs__hasWorkingUniform]-(wu:ncs__WorkingUniform) 
OPTIONAL MATCH (jp)-[:ncs__hasWorkingEnvironment]-(we:ncs__WorkingEnvironment) 
OPTIONAL MATCH (jp)-[:ncs__hasWorkingLocation]-(wl:ncs__WorkingLocation)
OPTIONAL MATCH (jp)-[:ncs__requiresHtbRegistration]-(re:ncs__Registration)
WITH wu,we,wl,jp,soc,oc,dayToDayTasks, {values:collect(ncs.servicetaxonomy.userfunctions.formatLinks(re.ncs__Description)), modifiedDate:max(re.ncs__ModifiedDate)} as registration 
OPTIONAL MATCH (jp)-[:ncs__hasHtbUniversityRoute]-(ur:ncs__UniversityRoute), (ur)-[:ncs__hasRequirementsPrefix]-(upr:ncs__RequirementsPrefix)
OPTIONAL MATCH (ur)-[:ncs__hasUniversityRequirement]-(urq:ncs__UniversityRequirement)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration, {values:collect(urq.skos__prefLabel), modifiedDate:max(urq.ncs__ModifiedDate)} as universityRequirement
OPTIONAL MATCH (ur)-[:ncs__hasUniversityLink]-(ul:ncs__UniversityLink)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,{values:collect('[' + ul.ncs__Link_text + ' | ' + ul.ncs__Link_url + ']'), modifiedDate:max(ul.ncs__ModifiedDate)} as universityLinks
OPTIONAL MATCH (jp)-[:ncs__hasHtbCollegeRoute]-(cr:ncs__CollegeRoute), (cr)-[:ncs__hasRequirementsPrefix]-(cpr:ncs__RequirementsPrefix)
OPTIONAL MATCH (cr)-[:ncs__hasCollegeRequirement]-(crq:ncs__CollegeRequirement)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,{values:collect(crq.skos__prefLabel), modifiedDate:max(crq.ncs__ModifiedDate)} as collegeRequirement
OPTIONAL MATCH (cr)-[:ncs__hasCollegeLink]-(cl:ncs__CollegeLink)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,{values:collect('[' + cl.ncs__Link_text + ' | ' + cl.ncs__Link_url + ']'), modifiedDate:max(cl.ncs__ModifiedDate)} as collegeLinks
OPTIONAL MATCH (jp)-[:ncs__hasHtbApprenticeshipRoute]-(ar:ncs__ApprenticeshipRoute), (ar)-[:ncs__hasRequirementsPrefix]-(apr:ncs__RequirementsPrefix)
OPTIONAL MATCH (ar)-[:ncs__hasApprenticeshipRequirement]-(arq:ncs__ApprenticeshipRequirement)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,{values:collect(arq.skos__prefLabel), modifiedDate:max(arq.ncs__ModifiedDate)} as apprenticeshipRequirement
OPTIONAL MATCH (ar)-[:ncs__hasApprenticeshipLink]-(al:ncs__ApprenticeshipLink)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,{values:collect('[' + al.ncs__Link_text + ' | ' + al.ncs__Link_url + ']'), modifiedDate:max(al.ncs__ModifiedDate)} as apprenticeshipLinks
OPTIONAL MATCH (jp)-[:ncs__hasHtbWorkRoute]-(wr:ncs__WorkRoute)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,{values:collect(wr.ncs__Description), modifiedDate:max(wr.ncs__ModifiedDate)} as workRoute
OPTIONAL MATCH (jp)-[:ncs__hasHtbVolunteeringRoute]-(vr:ncs__VolunteeringRoute)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,{values:collect(vr.ncs__Description), modifiedDate:max(vr.ncs__ModifiedDate)} as volunteeringRoute
OPTIONAL MATCH (jp)-[:ncs__hasHtbDirectRoute]-(dr:ncs__DirectRoute)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,volunteeringRoute, {values:collect(dr.ncs__Description), modifiedDate:max(dr.ncs__ModifiedDate)} as directRoute
OPTIONAL MATCH (jp)-[:ncs__hasHtbOtherRoute]-(or:ncs__OtherRoute)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,volunteeringRoute,directRoute,{values:collect(or.ncs__Description),modifiedDate:max(or.ncs__ModifiedDate)} as otherRoute
OPTIONAL MATCH (jp)-[:ncs__hasWitOtherRequirement]-(oreq:ncs__OtherRequirement)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,volunteeringRoute,directRoute,otherRoute,{values:collect(oreq.ncs__Description),modifiedDate:max(oreq.ncs__ModifiedDate)} as otherRequirements
OPTIONAL MATCH (jp)-[:ncs__hasWitRestriction]-(res:ncs__Restriction)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,volunteeringRoute,directRoute,otherRoute,otherRequirements,{values:collect(res.skos__prefLabel),modifiedDate:max(res.ncs__ModifiedDate)} as restrictions
WITH wu,we,wl,jp,soc,oc,
	{
    	lastModified:[jp.ncs__ModifiedDate, we.ncs__ModifiedDate, oc.ncs__ModifiedDate, wl.ncs__ModifiedDate, wu.ncs__ModifiedDate,ur.ncs__ModifiedDate,upr.ncs__ModifiedDate,universityLinks.ncs__ModifiedDate,universityRequirement.modifiedDate,collegeLinks.ncs__ModifiedDate,collegeRequirement.modifiedDate,apprenticeshipLinks.ncs__ModifiedDate,apprenticeshipRequirement.modifiedDate,workRoute.modifiedDate,volunteeringRoute.modifiedDate,directRoute.modifiedDate,otherRoute.modifiedDate,registration.modifiedDate, restrictions.modifiedDate, otherRequirements.modifiedDate],
		title: jp.skos__prefLabel,
        universityRoute:{
          requirements:universityRequirement,
          requirementsPreface:upr.skos__prefLabel,
          relevantSubjects: CASE ur.ncs__RelevantSubjects WHEN '' THEN [] WHEN NULL THEN [] ELSE [ur.ncs__RelevantSubjects] END,
          furtherInfo:CASE ur.ncs__FurtherInfo WHEN '' THEN [] WHEN NULL THEN [] ELSE [ur.ncs__FurtherInfo] END,
          links:universityLinks.values
        },
        collegeRoute:{
        	requirements:collegeRequirement,
            requirementsPreface:cpr.skos__prefLabel,
            relevantSubjects: CASE cr.ncs__RelevantSubjects WHEN '' THEN [] WHEN NULL THEN [] ELSE [cr.ncs__RelevantSubjects] END,
            furtherInfo: CASE cr.ncs__FurtherInfo WHEN '' THEN [] WHEN NULL THEN [] ELSE [cr.ncs__FurtherInfo] END,
            links:collegeLinks.values
        },
        apprenticeshipRoute:{
        	requirements:apprenticeshipRequirement,
            requirementsPreface:apr.skos__prefLabel,
            relevantSubjects:CASE ar.ncs__RelevantSubjects WHEN '' THEN [] WHEN NULL THEN [] ELSE [ar.ncs__RelevantSubjects] END,
            furtherInfo:CASE ar.ncs__FurtherInfo WHEN '' THEN [] WHEN NULL THEN [] ELSE [ar.ncs__FurtherInfo] END,
            links:apprenticeshipLinks.values
        },
        workRoute:{
        	values:workRoute.values
        },
        volunteeringRoute:{
        	values:volunteeringRoute.values
        },
        directRoute:{
        	values:directRoute.values
        },
        otherRoute:{
        	values:otherRoute.values
        },
		requirementsAndRestrictions:{
			otherRequirements:otherRequirements.values,
			restrictions:restrictions.values
		}
		,
        dayToDayTasks:dayToDayTasks,
        registration:registration.values
	} as combinedProfiles 
    UNWIND combinedProfiles.lastModified as lastModifiedDatesAsRows
    WITH wu, we, wl, jp, soc, oc, combinedProfiles, lastModifiedDatesAsRows
RETURN 
{
	Title:combinedProfiles.title, 
	LastUpdatedDate:MAX(lastModifiedDatesAsRows), 
	Url:$apiHost + 'getjobprofilebytitle/execute/' +  jp.ncs__JobProfileWebsiteUrl, 
	Soc:soc.skos__prefLabel, 
	ONetOccupationalCode:'ToDo', 
	AlternativeTitle:REDUCE(s = HEAD(oc.skos__altLabel), n IN TAIL( oc.skos__altLabel) | s + ', ' + n), 
	Overview:jp.ncs__Description, 
	SalaryStarter:jp.ncs__SalaryStarter, 
	SalaryExperienced:jp.ncs__SalaryExperienced, 
	MinimumHours:jp.ncs__MinimumHours, 
	MaximumHours:jp.ncs__MaximumHours, 
	WorkingHoursDetails:jp.ncs__WorkingHoursDetails, 
	WorkingPattern:jp.ncs__WorkingPattern, 
	WorkingPatternDetails:jp.ncs__WorkingPatternDetails, 
	HowToBecome:
	{
		EntryRouteSummary:'ToDo', 
		EntryRoutes:
		{
			University:
				{
					RelevantSubjects:COALESCE(combinedProfiles.universityRoute.relevantSubjects,[]),
					FurtherInformation:COALESCE(combinedProfiles.universityRoute.furtherInfo,[]), 
					EntryRequirementPreface:combinedProfiles.universityRoute.requirementsPreface, 
					EntryRequirements:COALESCE(combinedProfiles.universityRoute.requirements.values,[]), 
					AdditionalInformation:COALESCE(combinedProfiles.universityRoute.links,[])
				},
                College:
				{
					RelevantSubjects:COALESCE(combinedProfiles.collegeRoute.relevantSubjects,[]),
					FurtherInformation:COALESCE(combinedProfiles.collegeRoute.furtherInfo,[]), 
					EntryRequirementPreface:combinedProfiles.collegeRoute.requirementsPreface, 
					EntryRequirements:COALESCE(combinedProfiles.collegeRoute.requirements.values,[]), 
					AdditionalInformation:COALESCE(combinedProfiles.collegeRoute.links,[])
				},
                Apprenticeship:
                {
                	RelevantSubjects:COALESCE(combinedProfiles.apprenticeshipRoute.relevantSubjects,[]),
					FurtherInformation:COALESCE(combinedProfiles.apprenticeshipRoute.furtherInfo,[]), 
					EntryRequirementPreface:combinedProfiles.apprenticeshipRoute.requirementsPreface, 
					EntryRequirements:COALESCE(combinedProfiles.apprenticeshipRoute.requirements.values,[]), 
					AdditionalInformation:COALESCE(combinedProfiles.apprenticeshipRoute.links,[])
                },
				Work:combinedProfiles.workRoute.values,
				Volunteering:combinedProfiles.volunteeringRoute.values,
				DirectApplication:combinedProfiles.directRoute.values,
				OtherRoutes:combinedProfiles.otherRoute.values
		}, 
		MoreInformation:
		{
			Registrations:COALESCE(combinedProfiles.registration,[]), 
			CareerTips:CASE jp.ncs__HtbCareerTips WHEN '' THEN [] ELSE [jp.ncs__HtbCareerTips] END,
			ProfessionalAndIndustryBodies:CASE jp.ncs__HtbBodies WHEN '' THEN [] ELSE [jp.ncs__HtbBodies] END,
			FurtherInformation:CASE jp.ncs__HtbFurtherInformation WHEN '' THEN [] ELSE [jp.ncs__HtbFurtherInformation] END
		}
	}, 
	WhatItTakes:
	{
		DigitalSkillsLevel:jp.ncs__WitDigitalSkillsLevel, 
		Skills:['ToDo'],
		RestrictionsAndRequirements: {
        RelatedRestrictions: combinedProfiles.requirementsAndRestrictions.restrictions,
        OtherRequirements: combinedProfiles.requirementsAndRestrictions.otherRequirements
		}
	}, 
	WhatYouWillDo:
	{ 
		WYDDayToDayTasks:combinedProfiles.dayToDayTasks.tasks, 
		WorkingEnvironment:
		{
			Location:COALESCE(wl.skos_prefLabel, ''), 
			Environment:COALESCE(we.skos_prefLabel, ''), 
			Uniform:COALESCE(wu.skos_prefLabel, '') 
		}
	}, 
	CareerPathAndProgression:
	{
		CareerPathAndProgression:[jp.ncs__CareerPathAndProgression]
	}, 
	RelatedCareers:['ToDo']
}
```
## Current Query

```
MATCH (jp:ncs__JobProfile{skos__prefLabel:REPLACE($canonicalName,'-',' ')})-[:ncs__hasSocCode]->(soc:ncs__SOCCode), (jp)-[:ncs__relatedOccupation]-(oc:esco__Occupation) 
OPTIONAL MATCH (dtd:ncs__DayToDayTask)<-[:ncs__hasDayToDayTask]-(jp) 
WITH jp,soc,oc,{tasks:collect(dtd.skos__prefLabel)} as dayToDayTasks
OPTIONAL MATCH (jp)-[:ncs__hasWorkingUniform]-(wu:ncs__WorkingUniform) 
OPTIONAL MATCH (jp)-[:ncs__hasWorkingEnvironment]-(we:ncs__WorkingEnvironment) 
OPTIONAL MATCH (jp)-[:ncs__hasWorkingLocation]-(wl:ncs__WorkingLocation)
OPTIONAL MATCH (jp)-[:ncs__requiresHtbRegistration]-(re:ncs__Registration)
WITH wu,we,wl,jp,soc,oc,dayToDayTasks, {values:collect('[' + re.skos__prefLabel + ' | ' + re.ncs__Description + ']'), modifiedDate:max(re.ncs__ModifiedDate)} as registration 
OPTIONAL MATCH (jp)-[:ncs__hasHtbUniversityRoute]-(ur:ncs__UniversityRoute), (ur)-[:ncs__hasRequirementsPrefix]-(upr:ncs__RequirementsPrefix)
OPTIONAL MATCH (ur)-[:ncs__hasUniversityRequirement]-(urq:ncs__UniversityRequirement)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration, {values:collect(urq.skos__prefLabel), modifiedDate:max(urq.ncs__ModifiedDate)} as universityRequirement
OPTIONAL MATCH (ur)-[:ncs__hasUniversityLink]-(ul:ncs__UniversityLink)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,{values:collect('[' + ul.ncs__Link_text + ' | ' + ul.ncs__Link_url + ']'), modifiedDate:max(ul.ncs__ModifiedDate)} as universityLinks
OPTIONAL MATCH (jp)-[:ncs__hasHtbCollegeRoute]-(cr:ncs__CollegeRoute), (cr)-[:ncs__hasRequirementsPrefix]-(cpr:ncs__RequirementsPrefix)
OPTIONAL MATCH (cr)-[:ncs__hasCollegeRequirement]-(crq:ncs__CollegeRequirement)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,{values:collect(crq.skos__prefLabel), modifiedDate:max(crq.ncs__ModifiedDate)} as collegeRequirement
OPTIONAL MATCH (cr)-[:ncs__hasCollegeLink]-(cl:ncs__CollegeLink)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,{values:collect('[' + cl.ncs__Link_text + ' | ' + cl.ncs__Link_url + ']'), modifiedDate:max(cl.ncs__ModifiedDate)} as collegeLinks
OPTIONAL MATCH (jp)-[:ncs__hasHtbApprenticeshipRoute]-(ar:ncs__ApprenticeshipRoute), (ar)-[:ncs__hasRequirementsPrefix]-(apr:ncs__RequirementsPrefix)
OPTIONAL MATCH (ar)-[:ncs__hasApprenticeshipRequirement]-(arq:ncs__ApprenticeshipRequirement)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,{values:collect(arq.skos__prefLabel), modifiedDate:max(arq.ncs__ModifiedDate)} as apprenticeshipRequirement
OPTIONAL MATCH (ar)-[:ncs__hasApprenticeshipLink]-(al:ncs__ApprenticeshipLink)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,{values:collect('[' + al.ncs__Link_text + ' | ' + al.ncs__Link_url + ']'), modifiedDate:max(al.ncs__ModifiedDate)} as apprenticeshipLinks
OPTIONAL MATCH (jp)-[:ncs__hasHtbWorkRoute]-(wr:ncs__WorkRoute)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,{values:collect(wr.ncs__Description), modifiedDate:max(wr.ncs__ModifiedDate)} as workRoute
OPTIONAL MATCH (jp)-[:ncs__hasHtbVolunteeringRoute]-(vr:ncs__VolunteeringRoute)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,{values:collect(vr.ncs__Description), modifiedDate:max(vr.ncs__ModifiedDate)} as volunteeringRoute
OPTIONAL MATCH (jp)-[:ncs__hasHtbDirectRoute]-(dr:ncs__DirectRoute)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,volunteeringRoute, {values:collect(dr.ncs__Description), modifiedDate:max(dr.ncs__ModifiedDate)} as directRoute
OPTIONAL MATCH (jp)-[:ncs__hasHtbOtherRoute]-(or:ncs__OtherRoute)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,volunteeringRoute,directRoute,{values:collect(or.ncs__Description),modifiedDate:max(or.ncs__ModifiedDate)} as otherRoute
OPTIONAL MATCH (jp)-[:ncs__hasWitOtherRequirement]-(oreq:ncs__OtherRequirement)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,volunteeringRoute,directRoute,otherRoute,{values:collect(oreq.ncs__Description),modifiedDate:max(oreq.ncs__ModifiedDate)} as otherRequirements
OPTIONAL MATCH (jp)-[:ncs__hasWitRestriction]-(res:ncs__Restriction)
WITH wu,we,wl,jp,soc,oc,ur,upr,dayToDayTasks,registration,universityRequirement,universityLinks,cr,cpr,collegeRequirement,collegeLinks,ar,apr,apprenticeshipRequirement,apprenticeshipLinks,workRoute,volunteeringRoute,directRoute,otherRoute,otherRequirements,{values:collect(res.skos__prefLabel),modifiedDate:max(res.ncs__ModifiedDate)} as restrictions
WITH wu,we,wl,jp,soc,oc,
	{
    	lastModified:[jp.ncs__ModifiedDate, we.ncs__ModifiedDate, oc.ncs__ModifiedDate, wl.ncs__ModifiedDate, wu.ncs__ModifiedDate,ur.ncs__ModifiedDate,upr.ncs__ModifiedDate,universityLinks.ncs__ModifiedDate,universityRequirement.modifiedDate,collegeLinks.ncs__ModifiedDate,collegeRequirement.modifiedDate,apprenticeshipLinks.ncs__ModifiedDate,apprenticeshipRequirement.modifiedDate,workRoute.modifiedDate,volunteeringRoute.modifiedDate,directRoute.modifiedDate,otherRoute.modifiedDate,registration.modifiedDate, restrictions.modifiedDate, otherRequirements.modifiedDate],
		title: jp.skos__prefLabel,
        universityRoute:{
          requirements:universityRequirement,
          requirementsPreface:upr.skos__prefLabel,
          relevantSubjects: CASE ur.ncs__RelevantSubjects WHEN '' THEN [] WHEN NULL THEN [] ELSE [ur.ncs__RelevantSubjects] END,
          furtherInfo:CASE ur.ncs__FurtherInfo WHEN '' THEN [] WHEN NULL THEN [] ELSE [ur.ncs__FurtherInfo] END,
          links:universityLinks.values
        },
        collegeRoute:{
        	requirements:collegeRequirement,
            requirementsPreface:cpr.skos__prefLabel,
            relevantSubjects: CASE cr.ncs__RelevantSubjects WHEN '' THEN [] WHEN NULL THEN [] ELSE [cr.ncs__RelevantSubjects] END,
            furtherInfo: CASE cr.ncs__FurtherInfo WHEN '' THEN [] WHEN NULL THEN [] ELSE [cr.ncs__FurtherInfo] END,
            links:collegeLinks.values
        },
        apprenticeshipRoute:{
        	requirements:apprenticeshipRequirement,
            requirementsPreface:apr.skos__prefLabel,
            relevantSubjects:CASE ar.ncs__RelevantSubjects WHEN '' THEN [] WHEN NULL THEN [] ELSE [ar.ncs__RelevantSubjects] END,
            furtherInfo:CASE ar.ncs__FurtherInfo WHEN '' THEN [] WHEN NULL THEN [] ELSE [ar.ncs__FurtherInfo] END,
            links:apprenticeshipLinks.values
        },
        workRoute:{
        	values:workRoute.values
        },
        volunteeringRoute:{
        	values:volunteeringRoute.values
        },
        directRoute:{
        	values:directRoute.values
        },
        otherRoute:{
        	values:otherRoute.values
        },
		requirementsAndRestrictions:{
			otherRequirements:otherRequirements.values,
			restrictions:restrictions.values
		}
		,
        dayToDayTasks:dayToDayTasks,
        registration:registration.values
	} as combinedProfiles 
    UNWIND combinedProfiles.lastModified as lastModifiedDatesAsRows
    WITH wu, we, wl, jp, soc, oc, combinedProfiles, lastModifiedDatesAsRows
RETURN 
{
	Title:combinedProfiles.title, 
	LastUpdatedDate:MAX(lastModifiedDatesAsRows), 
	Url:$apiHost + 'getjobprofilebytitle/execute/' +  jp.ncs__JobProfileWebsiteUrl, 
	Soc:soc.skos__prefLabel, 
	ONetOccupationalCode:'ToDo', 
	AlternativeTitle:REDUCE(s = HEAD(oc.skos__altLabel), n IN TAIL( oc.skos__altLabel) | s + ', ' + n), 
	Overview:jp.ncs__Description, 
	SalaryStarter:jp.ncs__SalaryStarter, 
	SalaryExperienced:jp.ncs__SalaryExperienced, 
	MinimumHours:jp.ncs__MinimumHours, 
	MaximumHours:jp.ncs__MaximumHours, 
	WorkingHoursDetails:jp.ncs__WorkingHoursDetails, 
	WorkingPattern:jp.ncs__WorkingPattern, 
	WorkingPatternDetails:jp.ncs__WorkingPatternDetails, 
	HowToBecome:
	{
		EntryRouteSummary:'ToDo', 
		EntryRoutes:
		{
			University:
				{
					RelevantSubjects:COALESCE(combinedProfiles.universityRoute.relevantSubjects,[]),
					FurtherInformation:COALESCE(combinedProfiles.universityRoute.furtherInfo,[]), 
					EntryRequirementPreface:combinedProfiles.universityRoute.requirementsPreface, 
					EntryRequirements:COALESCE(combinedProfiles.universityRoute.requirements.values,[]), 
					AdditionalInformation:COALESCE(combinedProfiles.universityRoute.links,[])
				},
                College:
				{
					RelevantSubjects:COALESCE(combinedProfiles.collegeRoute.relevantSubjects,[]),
					FurtherInformation:COALESCE(combinedProfiles.collegeRoute.furtherInfo,[]), 
					EntryRequirementPreface:combinedProfiles.collegeRoute.requirementsPreface, 
					EntryRequirements:COALESCE(combinedProfiles.collegeRoute.requirements.values,[]), 
					AdditionalInformation:COALESCE(combinedProfiles.collegeRoute.links,[])
				},
                Apprenticeship:
                {
                	RelevantSubjects:COALESCE(combinedProfiles.apprenticeshipRoute.relevantSubjects,[]),
					FurtherInformation:COALESCE(combinedProfiles.apprenticeshipRoute.furtherInfo,[]), 
					EntryRequirementPreface:combinedProfiles.apprenticeshipRoute.requirementsPreface, 
					EntryRequirements:COALESCE(combinedProfiles.apprenticeshipRoute.requirements.values,[]), 
					AdditionalInformation:COALESCE(combinedProfiles.apprenticeshipRoute.links,[])
                },
				Work:combinedProfiles.workRoute.values,
				Volunteering:combinedProfiles.volunteeringRoute.values,
				DirectApplication:combinedProfiles.directRoute.values,
				OtherRoutes:combinedProfiles.otherRoute.values
		}, 
		MoreInformation:
		{
			Registrations:COALESCE(combinedProfiles.registration,[]), 
			CareerTips:CASE jp.ncs__HtbCareerTips WHEN '' THEN [] ELSE [jp.ncs__HtbCareerTips] END,
			ProfessionalAndIndustryBodies:CASE jp.ncs__HtbBodies WHEN '' THEN [] ELSE [jp.ncs__HtbBodies] END,
			FurtherInformation:CASE jp.ncs__HtbFurtherInformation WHEN '' THEN [] ELSE [jp.ncs__HtbFurtherInformation] END
		}
	}, 
	WhatItTakes:
	{
		DigitalSkillsLevel:jp.ncs__WitDigitalSkillsLevel, 
		Skills:['ToDo'],
		RestrictionsAndRequirements: {
        RelatedRestrictions: combinedProfiles.requirementsAndRestrictions.restrictions,
        OtherRequirements: combinedProfiles.requirementsAndRestrictions.otherRequirements
		}
	}, 
	WhatYouWillDo:
	{ 
		WYDDayToDayTasks:combinedProfiles.dayToDayTasks.tasks, 
		WorkingEnvironment:
		{
			Location:COALESCE(wl.skos_prefLabel, ''), 
			Environment:COALESCE(we.skos_prefLabel, ''), 
			Uniform:COALESCE(wu.skos_prefLabel, '') 
		}
	}, 
	CareerPathAndProgression:
	{
		CareerPathAndProgression:[jp.ncs__CareerPathAndProgression]
	}, 
	RelatedCareers:['ToDo']
}
```