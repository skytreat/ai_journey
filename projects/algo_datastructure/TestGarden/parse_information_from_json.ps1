# powershell script to parse IP from the given json file
$inputJsonFile = "D:\workspace\vscode\console\TestGarden\Input.json"

$targetTagName = "office"

$jsonLines = Get-Content $inputJsonFile

$jsonContent = "";
foreach($line in $jsonLines)
{
    $jsonContent += $line;
}

$jsonObjectList = ConvertFrom-Json $jsonContent

$resultList = @()

foreach($object in $jsonObjectList.results)
{
    write-host ("hello " + $object.tags)
    if ($object.tags -and ($object.tags.display -eq $targetTagName))
    {
        $resultList += $object
    }
}