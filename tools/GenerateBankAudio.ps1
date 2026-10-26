$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$repoRoot = Split-Path $PSScriptRoot -Parent
$catalogRoot = Join-Path $repoRoot 'src/ToeflExam.Infrastructure/ContentBank'
$bankAudioRoot = Join-Path $repoRoot 'src/ToeflExam.Api/Content/Bank'
$speakingAudioRoot = Join-Path $repoRoot 'src/ToeflExam.Api/Content/Speaking'
New-Item -ItemType Directory -Force -Path $bankAudioRoot | Out-Null
New-Item -ItemType Directory -Force -Path $speakingAudioRoot | Out-Null
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $voices = @($synth.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -like 'en-*' })
    if ($voices.Count -lt 1) { throw 'An English Windows speech voice must be installed.' }
    $synth.Rate = -1
    $format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(16000, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
    $count = 0
    foreach ($catalog in Get-ChildItem -LiteralPath $catalogRoot -Filter '*-v1.json') {
        $rows = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalog.FullName | ConvertFrom-Json
        foreach ($entry in $rows) {
            if ($entry.section -notin @('speaking','listening')) { continue }
            if ($entry.audioFile -notmatch '^[a-f0-9]{32}\.wav$') { throw 'Invalid asset filename.' }
            $destination = if ($entry.section -eq 'speaking' -and $entry.pool -eq 'practice') { $speakingAudioRoot } else { $bankAudioRoot }
            $file = Join-Path $destination $entry.audioFile
            if (Test-Path -LiteralPath $file) { continue }
            $spoken = if ($entry.section -eq 'speaking') { $entry.prompt } else { $entry.text }
            $synth.SetOutputToWaveFile($file, $format)
            $builder = New-Object System.Speech.Synthesis.PromptBuilder
            foreach ($line in ($spoken -split "`n")) {
                if ([string]::IsNullOrWhiteSpace($line)) { continue }
                $voiceIndex = if ($line.StartsWith('B:') -and $voices.Count -gt 1) { 1 } else { 0 }
                $builder.StartVoice($voices[$voiceIndex].VoiceInfo.Name)
                $builder.AppendText(($line -replace '^[AB]:\s*',''))
                $builder.EndVoice()
                $builder.AppendBreak([TimeSpan]::FromMilliseconds(250))
            }
            $synth.Speak($builder)
            $synth.SetOutputToNull()
            $count++
        }
    }
    Write-Output "Generated $count original synthetic question recordings."
    Write-Output ('Voices: ' + (($voices | ForEach-Object { $_.VoiceInfo.Name }) -join ', '))
} finally { $synth.Dispose() }
