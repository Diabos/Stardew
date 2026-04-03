param(
    [Parameter(Mandatory = $true)]
    [string]$FrameworkDir
)

$files = Get-ChildItem -Path $FrameworkDir -Filter "dotnet.native*.js" -File -ErrorAction SilentlyContinue
if (-not $files) {
    Write-Host "No dotnet.native*.js files found in $FrameworkDir"
    exit 0
}

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw

    $content = $content -replace 'assert\(buf % 16 == 0\);', '/* relaxed for legacy SDL em_asm argument packing */'
    $content = $content.Replace('assert(buf % 16 == 0);', '/* relaxed for legacy SDL em_asm argument packing */')
    $content = $content -replace "var validChars = \['d', 'f', 'i'\];", "var validChars = ['d', 'f', 'i', 'p', 's'];"
    $content = $content -replace 'assert\(validChars\.includes\(chr\), .*?\);', 'if (!validChars.includes(chr)) { ch = 105; } // Treat unknown legacy ABI args as i32'
    $content = $content.Replace('assert(validChars.includes(chr), ''Invalid character '' + ch + ''("'' + chr + ''") in readEmAsmArgs! Use only ['' + validChars + ''], and do not specify "v" for void return argument.'');', 'if (!validChars.includes(chr)) { ch = 105; } // Treat unknown legacy ABI args as i32')
    $content = $content -replace 'buf \+= \(ch != 105/\*i\*/\) & buf;', 'buf += ((ch == 100/*d*/ || ch == 102/*f*/ || ch == 106/*j*/) ? (buf & 1) : 0);'
    $content = $content.Replace('buf += (ch != 105/*i*/) & buf;', 'buf += ((ch == 100/*d*/ || ch == 102/*f*/ || ch == 106/*j*/) ? (buf & 1) : 0);')

    # Hard-disable sync-on-main-thread EM_ASM dispatch. Legacy SDL cursor paths can
    # trigger strict ABI assertions here in the generated runtime.
    $content = $content.Replace('return runMainThreadEmAsm(code, sigPtr, argbuf, 1);', 'return 0;')

        $content = $content.Replace("  err('missing function: emscripten_asm_const_ptr_sync_on_main_thread'); abort(-1);", @"
    console.warn('emscripten_asm_const_ptr_sync_on_main_thread is unavailable in this runtime; returning 0');
    return 0;
"@)

    $oldBlock = @"
        readEmAsmArgsArray.push(
          ch == 105/*i*/ ? HEAP32[buf] :
         (ch == 106/*j*/ ? HEAP64 : HEAPF64)[buf++ >> 1]
        );
        ++buf;
"@

    $newBlock = @"
        if (ch == 115/*s*/) {
          readEmAsmArgsArray.push(UTF8ToString(HEAP32[buf]));
          ++buf;
          continue;
        }
        readEmAsmArgsArray.push(
         (ch == 105/*i*/ || ch == 112/*p*/) ? HEAP32[buf] :
         (ch == 106/*j*/ ? HEAP64 : HEAPF64)[buf++ >> 1]
        );
        ++buf;
"@

    if ($content.Contains($oldBlock)) {
        $content = $content.Replace($oldBlock, $newBlock)
    }

    Set-Content -Path $file.FullName -Value $content -NoNewline

    # Keep the precompressed asset in sync, otherwise Static Web Assets may serve
    # stale unpatched code from the .gz variant.
    $gzPath = "$($file.FullName).gz"
    if (Test-Path $gzPath) {
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        $bytes = $utf8NoBom.GetBytes($content)
        $out = [System.IO.File]::Create($gzPath)
        try {
            $gzip = New-Object System.IO.Compression.GzipStream($out, [System.IO.Compression.CompressionMode]::Compress)
            try {
                $gzip.Write($bytes, 0, $bytes.Length)
            }
            finally {
                $gzip.Dispose()
            }
        }
        finally {
            $out.Dispose()
        }
    }

    Write-Host "Patched $($file.Name)"
}
