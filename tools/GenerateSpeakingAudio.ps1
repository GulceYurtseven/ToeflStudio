$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$root = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $root 'src/ToeflExam.Api/Content/Speaking'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$source = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src/ToeflExam.Infrastructure/Persistence/SpeakingSeed.cs')
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $voice = $synth.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -eq 'en-US' } | Select-Object -First 1
    if (-not $voice) { throw 'An installed en-US Windows speech voice is required.' }
    $synth.SelectVoice($voice.VoiceInfo.Name)
    $synth.Rate = -1
    $format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(16000, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
    $groups = [regex]::Matches($source, '(?s)\["(?:Welcome|Think).*?\]')
    if ($groups.Count -ne 2) { throw 'Expected two seed prompt arrays.' }
    $kind = 0
    foreach ($group in $groups) {
        $kind++
        $position = 0
        foreach ($match in [regex]::Matches($group.Value, '"([^"]+)"')) {
            $position++
            $file = Join-Path $destination "$kind-$position-v1.wav"
            # v1 assets are immutable; intentional edits require a new content/audio version.
            if (Test-Path -LiteralPath $file) { continue }
            $synth.SetOutputToWaveFile($file, $format)
            $synth.Speak($match.Groups[1].Value)
            $synth.SetOutputToNull()
            Write-Output "Created $kind-$position-v1.wav"
        }
    }
    Write-Output ('Voice: ' + $voice.VoiceInfo.Name)
} finally { $synth.Dispose() }
