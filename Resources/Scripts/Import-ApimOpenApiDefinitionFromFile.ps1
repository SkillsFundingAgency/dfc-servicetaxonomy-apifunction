<#
.SYNOPSIS
Update an APIM API with an openapi definition

.DESCRIPTION
Update an APIM API with a openapi definition

.PARAMETER ApimResourceGroup
The name of the resource group that contains the APIM instnace

.PARAMETER InstanceName
The name of the APIM instance

.PARAMETER ApiName
The name of the API to update

.PARAMETER OpenApiSpecificationFile
The path to save the openapi specification file to update the APIM instance with.

.PARAMETER ApiVersion
The path to save the openapi specification file to update the APIM instance with.

.EXAMPLE
Import-ApimOpenApiDefinitionFromFile -ApimResourceGroup dfc-foo-bar-rg -InstanceName dfc-foo-bar-apim -ApiName bar -OpenApiSpecificationFile some-file.yaml -Verbose

#>
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [String]$ApimResourceGroup,
    [Parameter(Mandatory=$true)]
    [String]$InstanceName,
    [Parameter(Mandatory=$true)]
    [String]$ApiName,
    [Parameter(Mandatory=$true)]
    [String]$ApiPath,
    [Parameter(Mandatory=$true)]
    [String]$OpenApiSpecificationFile,
    [Parameter(Mandatory=$true)]
    [String]$ApiVersion

)

try {
    # --- Build context and retrieve apiid
    Write-Host "Building APIM context for $ApimResourceGroup\$InstanceName"
    $context = New-AzApiManagementContext -ResourceGroupName $ApimResourceGroup -ServiceName $InstanceName


    # Check if it's already existing, if not a new versionset needs to be created, else use the existing one
    # Version Set is unique by API, if there's an API with 2 versions there will be 1 version set with 2 APIs
    Write-Host "[VERSION SET] Performing lookup. "
    $versionSetLookup = Get-AzApiManagementApiVersionSet -Context $context | Where-Object { $_.DisplayName -eq "$ApiName" }  | Sort-Object -Property ApiVersionSetId -Descending | Select-Object -first 1
    if($null -eq $versionSetLookup)
    {
        Write-Host "[VERSION SET] Version set NOT FOUND for: $ApiName, creating a new one. "
        $versionSet = New-AzApiManagementApiVersionSet -Context $context -Name "$ApiName" -Scheme Header -HeaderName "version" -Description "$ApiName"
        $versionSetId = $versionSet.Id
        Write-Host "[VERSION SET] Created new version set, id: $versionSetId"
    }
    else
    {
        Write-Host "[VERSION SET] Version set FOUND for: $ApiName, using existing one. "
        $versionSetId = $versionSetLookup.ApiVersionSetId
        Write-Host "[VERSION SET] Reusing existing versionset , id: $versionSetId"
    }
    
    # import api from OpenAPI Specs
    Write-Host  "[IMPORT] Importing OpenAPI: $openapiSpecs "
    $api = Import-AzApiManagementApi -Context $context -SpecificationPath $OpenApiSpecificationFile -SpecificationFormat OpenApi -Path $ApiPath -ApiId "$ApiName$ApiVersion" -ApiVersion $ApiVersion -ApiVersionSetId $versionSetId -ErrorAction Stop -Verbose:$VerbosePreference
    Write-Host  "[IMPORT] Imported API: $api.ApiId " 
    

    # # --- Import openapi definition
    # Write-Host "Updating API $InstanceName\$($ApiName) from definition $($OutputFile.FullName)"
    # Import-AzApiManagementApi -Context $Context -SpecificationFormat OpenApi -SpecificationPath $OpenApiSpecificationFile -ApiId $ApiName -Path $ApiPath -ApiVersion $ApiVersion -ApiVersionSetId $versionSet.ApiVersionSetId -ErrorAction Stop -Verbose:$VerbosePreference
}
catch {
   throw $_
}