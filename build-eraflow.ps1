param(
 [Parameter(Mandatory=$true)][string]$AndroidSdk,
 [Parameter(Mandatory=$true)][string]$JavaSdk,
 [string]$KeyStore,
 [string]$KeyAlias='eraflow'
)
$ErrorActionPreference='Stop'
$sourceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'src'))
$project=Join-Path $sourceRoot 'EraFlow.Android'
# Rebuild every referenced assembly so an earlier diagnostic build cannot be reused.
foreach($name in @('EraFlow.Android','Emuera.Core','Emuera.WebHost','EraFlow.Storage')){
 foreach($folder in @('bin','obj')){
  $target=[IO.Path]::GetFullPath((Join-Path (Join-Path $sourceRoot $name) $folder))
  if(!$target.StartsWith($sourceRoot+[IO.Path]::DirectorySeparatorChar)){throw 'Unsafe build output directory'}
  if(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target -Recurse -Force}
 }
}
$arguments=@('build',$project,'-c','Release',"-p:AndroidSdkDirectory=$AndroidSdk","-p:JavaSdkDirectory=$JavaSdk")
if($KeyStore){
 if(!$env:ERAFLOW_SIGNPASS){throw 'Set ERAFLOW_SIGNPASS before signing.'}
 $arguments+=@('-p:AndroidKeyStore=true',"-p:AndroidSigningKeyStore=$KeyStore","-p:AndroidSigningKeyAlias=$KeyAlias",'-p:AndroidSigningKeyPass=env:ERAFLOW_SIGNPASS','-p:AndroidSigningStorePass=env:ERAFLOW_SIGNPASS')
}
& dotnet @arguments
exit $LASTEXITCODE
