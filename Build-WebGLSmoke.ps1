$ErrorActionPreference = 'Stop'
$env:PYTHONUTF8 = '1'
$env:PYTHONIOENCODING = 'utf-8'
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\2022.3.7f1c1\Editor\Unity.exe'
$buildLog = Join-Path $PSScriptRoot 'Logs\webgl-smoke.log'
New-Item -ItemType Directory -Force -Path (Split-Path $buildLog) | Out-Null
$unityArgs = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $PSScriptRoot + '"'), '-buildTarget', 'WebGL', '-executeMethod', 'SmokeBuild.BuildWebGL', '-logFile', ('"' + $buildLog + '"'))
$buildProcess = Start-Process -FilePath $unityEditor -ArgumentList $unityArgs -WindowStyle Hidden -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) { throw "Unity build failed. See $buildLog" }
Write-Output "WebGL smoke build completed: $PSScriptRoot\Builds\WebGLSmoke"
