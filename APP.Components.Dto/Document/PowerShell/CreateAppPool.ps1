param (
    [string]$newAppName = "UserAppTest1"  
 )


Import-Module WebAdministration
$iisAppName = $newAppName
$siteName = "TEST"
$iisAppPoolName = $newAppName
$iisAppPoolDotNetVersion = "v4.0"
$directoryPath = "I:\DevTest\App\PlmApplication"



#navigate to the app pools root
cd IIS:\AppPools\

#check if the app pool exists
if (!(Test-Path $iisAppPoolName -pathType container))
{
    #create the app pool
    $appPool = New-Item $iisAppPoolName
    $appPool | Set-ItemProperty -Name "managedRuntimeVersion" -Value $iisAppPoolDotNetVersion
    $appPool | Set-ItemProperty -Name processModel.identityType -Value 0
}

#navigate to the sites root
cd IIS:\Sites\TEST

#check if Web Application exists
if (Test-Path $iisAppName -pathType container)
{
    return
}

#create Website
#$iisApp = New-Item $iisAppName -bindings @{protocol="http";bindingInformation=":80:" + $iisAppName} -physicalPath $directoryPath
#$iisApp | Set-ItemProperty -Name "applicationPool" -Value $iisAppPoolName

#create Web Application
New-WebApplication -Name $iisAppName -Site $SiteName -PhysicalPath $directoryPath -ApplicationPool $iisAppPoolName
