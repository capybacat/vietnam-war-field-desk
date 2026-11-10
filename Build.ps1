$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$target = Join-Path $PSScriptRoot 'VietnamWarLiveEditor.exe'
$sourceFiles = @('LiveEditor.cs','FieldForm.cs','FieldTheme.cs','ResourceEditor.cs','ResourcePanel.cs','AssemblyInfo.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /unsafe /codepage:65001 "/out:$target" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/win32manifest:$PSScriptRoot\app.manifest" "/win32icon:$PSScriptRoot\Assets\app.ico" "/resource:$PSScriptRoot\fields.tsv,fields.tsv" "/resource:$PSScriptRoot\Assets\app-emblem.png,fielddesk.emblem" "/resource:$PSScriptRoot\Assets\app.ico,fielddesk.icon" $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Write-Output $target
