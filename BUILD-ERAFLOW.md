# Build era for Android

This source snapshot contains the full patched interpreter; cloning the upstream repositories or applying the historical patches again is not required.

## Requirements

- .NET SDK 10.0.401 or compatible .NET 10 SDK
- .NET Android workload (`dotnet workload install android`)
- JDK 21
- Android SDK platform 36 and build-tools 36.0.0

The validated desktop build host was Windows. The app targets Android 8.0+ and includes ARM64 and x86_64 runtimes. No separate .NET installation is needed on the Android device.

## Build

```powershell
./build-eraflow.ps1 -AndroidSdk 'C:/Android/sdk' -JavaSdk 'C:/Java/jdk-21'
```

Output: `src/EraFlow.Android/bin/Release/net10.0-android/app.eraflow.android-Signed.apk`. Without signing parameters, the Android SDK's development certificate is used. To make a release build with your own certificate:

```powershell
$env:ERAFLOW_SIGNPASS = '<your keystore password>'
./build-eraflow.ps1 -AndroidSdk 'C:/Android/sdk' -JavaSdk 'C:/Java/jdk-21' -KeyStore 'C:/keys/release.jks' -KeyAlias 'eraflow'
```

The delivered APK was separately aligned and signed with this project's release certificate. The private signing key is deliberately excluded from this source archive. A build signed with a different certificate cannot update the delivered installation in place.

## Checks

```powershell
dotnet run --project src/EraFlow.Storage.Tests -c Release
dotnet run --project src/EraFlow.Probe -c Release -- import '<library>' '<directory of ZIPs>'
dotnet run --project src/EraFlow.Probe -c Release -- probe '<game directory>' 0 '<enter>'
```

`auto` runs a bounded, heuristic input sequence and records engine status and rendered-frame hashes. It is a compatibility probe, not proof that every storyline can be completed.

The Android UI checks used a separate build with `ERAFLOW_TEST` defined. Only that build enables the explicit WebView debugging call and the authenticated local `/diagnostics` and `/choices` endpoints. The delivered APK omits these test interfaces. A userdebug Android system can independently force WebView debugging; the AOSP test emulator did so, despite the app having no debuggable flag. The UI test scripts and neutral ERB fixtures are included in `qa`; the downloaded third-party games are excluded. The build script cleans referenced assemblies to prevent reusing an earlier diagnostic build.

## Structure and changes

- `src/EraFlow.Android`: new Android launcher, scoped document import, per-game process, orientation and platform bridge.
- `src/EraFlow.Storage`: bounded ZIP import, safe paths, library persistence and save backup/restore.
- `src/Emuera.WebHost/wwwroot/player.html`: new landscape numeric keypad and Expressive settings sidebar.
- `src/Emuera.Core`: Android-compatible Emuera runtime, including case-insensitive filename resolution, display options, legacy Chinese configuration names and the supported PuchiEmuera fixed-image/effect dialect.
- `upstream/emuera.em`: patched upstream source with serialized timer callbacks, image visibility and empty-print-buffer handling.
- `licenses`: upstream attribution and license notices. See `era-for-Android.NOTICES.md` for revisions and modifications.

The UI is implemented locally in HTML/CSS within Android WebViews. The C# interpreter runs on-device and supplies the original game rendering through a token-protected loopback WebSocket. No remote game service is required.

Version 1.1 adds strict text-encoding fallback for mixed legacy files, a corrected CSV diagnostic path, single-key input support in the player, and independently implemented FONTEDGE/FADE/SHAKE and FIXED image handling. Test routes use public menu input; no third-party executables are loaded. The test report distinguishes bounded compatibility runs from the complete eraRPG graduation route.

The landscape player temporarily hides its numeric keypad while the soft keyboard reduces the available height. Buttons retain text focus until their click finishes, keeping confirmation visible and preventing keyboard dismissal from moving it before submission.

Disabling mouse input in a game's configuration no longer overrides its keyboard prompt type. WAIT, INPUTS and INPUT retain their continuation, text and integer controls. The KeyboardOnly regression fixture covers this setting.
