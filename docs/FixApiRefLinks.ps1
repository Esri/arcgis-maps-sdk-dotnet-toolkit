﻿[cmdletbinding()]
param([string]$Path)

function FixApiLinks([string]$path)
{
    $files = Get-ChildItem -Path $path -Recurse -Include *.html
    foreach ($file in $files)
    {
        $content = Get-Content -Path $file
        $newContent = $content -replace "../(android|ios|netwin)/", ''
        $newContent | Set-Content -Path $file
    }
}
FixApiLinks -path $Path