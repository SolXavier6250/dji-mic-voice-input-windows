$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '需要 Windows .NET Framework 4.x C# 编译器。' }
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$target = Join-Path $dist 'DJI-Voice-Input-Windows.exe'
& $compiler /nologo /codepage:65001 /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll ('/out:' + $target) (Join-Path $PSScriptRoot 'src\Program.cs')
if ($LASTEXITCODE -ne 0) { throw '编译失败。' }
Get-Item -LiteralPath $target | Select-Object Name,Length
Get-FileHash -Algorithm SHA256 -LiteralPath $target
