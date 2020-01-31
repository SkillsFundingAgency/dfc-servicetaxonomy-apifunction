param
(
    [string]$baseDir="F:\dredd\servicetaxonomy",
    [string]$SubscriptionKey="############################",
    [string]$SourceFile="C:\Users\esfa\Downloads\Service Taxonomy Get Skills By Label.openapi_origin.yaml",
    [string]$httpProtocol="https",
    #[string]$ApiBaseUrl="localhost:7071",
    [string[]] $httpStatusCodeExclusions = ( '204','400','422','500')
)

#clear
Write-Output "baseDir:" + $baseDir;
Write-Output "ApiBaseUri:" + $ApiBaseUri;

cd $baseDir

if ( Test-Path -path convertedfile.yaml )
{
    Remove-Item convertedfile.yaml
}

$converted = $( api-spec-converter -f openapi_3 -t swagger_2 -s yaml $SourceFile)

$outputLine = $true
$excludeThisLine = $false
$lastLineTagged = $false
$checkForExtraLines = $false
$suppressText = $false

ForEach ($line in $($converted -split "`r`n"))
{
    foreach ( $item in $httpStatusCodeExclusions )
    {
        If ( $Line.contains($item) -And $Line.trim().EndsWith(":") ) 
        {
            # current line is start of session we want to skip
            $excludeThisLine = $true
            $suppressText = $true
        }
    } 
    If ( $excludeThisLine  )
    {
        # reset flags for skip section
        $lastLineTagged = $true
        $excludeThisLine = $false
    }
    ElseIf ( $lastLineTagged  )
    {
        # processing line after start of skip section, prep for checking for additional lines
        $suppressText =$true
        $lastLineTagged = $false
        $checkForExtraLines = $true
    }
    ElseIf ( $checkForExtraLines )
    {
        #keep skipping until hit next section
        if ( $Line.trim().EndsWith(":") )
        {
            $checkForExtraLines = $false
            $suppressText = $false
        }
        else
        {
            $suppressText = $true
        }
    }
    Else
    {
        $suppressText = $false
    }
    
    if ( -not($suppressText ) )
    {
        $Line | Out-File -Append -Encoding ascii convertedfile.yaml
        #Write-host $Line
    }
}

$baseUrlLine = $converted -match '^host:\s[a-zA-Z.]+'
$baseUrl = ($baseUrlLine -split ":")[1]

if ($baseUrl.length -lt 1)
{
    $baseUrl="http://localhost:7071"
}

if ( ! $baseUrl.Contains("http") )
{
   #add http protocol
   $baseUrl = $httpProtocol + "://" + $baseUrl.Trim()
}
#$baseUrl="http://localhost:7071"
$header="Ocp-Apim-Subscription-Key:" + $SubscriptionKey

Write-Host "Run dredd against converted file: "$baseUrl 
dredd convertedfile.yaml --header $header $baseUrl

#Remove-Item convertedfile.yaml