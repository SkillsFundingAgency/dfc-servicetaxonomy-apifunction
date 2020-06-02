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
    $Context = New-AzApiManagementContext -ResourceGroupName $ApimResourceGroup -ServiceName $InstanceName

    # --- get a version set
    Write-Host "Getting version set for $ApiPath"
    $versionSets = Get-AzApiManagementApiVersionSet -Context $Context | Where-Object { $_.DisplayName -eq $ApiName }
    if (!$versionSets) {
        New-AzApiManagementApiVersionSet -Context $Context -Scheme Header -HeaderName "version" -Description $ApiName -Name $ApiName
    }
    $versionSet = Get-AzApiManagementApiVersionSet -Context $Context | Where-Object { $_.DisplayName -eq $ApiName }

    # --- Import openapi definition
    Write-Host "Updating API $InstanceName\$($ApiName) from definition $($OutputFile.FullName)"
    Import-AzApiManagementApi -Context $Context -SpecificationFormat OpenApi -SpecificationPath $OpenApiSpecificationFile -ApiId $ApiName -Path $ApiPath -ApiVersion $ApiVersion -ApiVersionSetId $versionSet.ApiVersionSetId -ErrorAction Stop -Verbose:$VerbosePreference
}
catch {
   throw $_
}