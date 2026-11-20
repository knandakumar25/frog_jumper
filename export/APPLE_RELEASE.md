# macOS and iOS release preparation

These are setup instructions for future, explicitly requested exports. Nothing
in normal startup, help, or the check commands produces a game build. GitHub
Actions remains prohibited. Rebuild the export image after these tooling changes.

## Outputs and commands

Run one export at a time, after edits/imports/other builds have finished:

```powershell
export.bat macos
export.bat ios
```

Bash equivalents are `./export.sh macos` and `./export.sh ios`. Direct Compose
equivalents use `docker compose run --rm export macos` or `ios` after rebuilding
the image with `docker compose --profile export build export`.

| Target | Docker output | Remaining work |
|---|---|---|
| macos | build/macos/AxiomForce-unsigned.zip | Sign, notarize, staple and test on a Mac |
| ios | build/ios/AxiomForce-iOS-source.zip | Generate the native iOS project on a Mac, configure signing, archive and distribute |
| all | Existing platform attempts plus both Apple outputs | Requires a real macOS bundle ID before any exports; Apple steps remain manual |

The iOS ZIP is **source**, not a native iOS project, simulator build or installable IPA.
It contains the game, required add-ons, presets, and these completion tools.
Development environment files, backend/website, build caches, signing files and
signing passwords are excluded. Do not add credentials to source files.

## Before a macOS Docker export

1. In Godot's macOS export preset, replace `com.example.axiomforce` with a unique
   reverse-DNS bundle ID you control. The helper refuses the placeholder.
2. Keep architecture `universal`, code signing disabled, and notarization disabled
   for the Linux Docker stage. The output is a release-mode **unsigned intermediate**,
   never a silent debug fallback. The .NET runtime/add-on architecture compatibility
   still needs real Intel and Apple Silicon testing.
3. Review the version/build numbers (initially 1.0 / 1) and the application icon.
4. Only then explicitly request the Docker export. It does not sign or upload.

Read-only configuration check, with no game export:

```text
python export/apple_release.py check-macos .
```

## Finish macOS distribution on a Mac

1. Transfer and unzip the Docker output on a Mac. For public direct downloads,
   obtain an Apple Developer account and a Developer ID Application identity.
   Keep certificates/private keys/passwords in the Mac's Keychain, not this repo.
2. Follow Godot's code-signing/notarization instructions linked below. Sign embedded
   executable libraries/frameworks from the inside out, then the outer app, using
   hardened runtime and the entitlements required by Godot .NET. This project uses
   add-ons, so audit their native libraries and entitlements too. Do not apply a
   generic unsigned-app bypass as a release procedure.
3. Verify the signature, create the submission ZIP with Apple's `ditto`, and
   submit it with `xcrun notarytool submit --keychain-profile <your-profile> --wait`.
   Create that Keychain profile interactively using your own Apple credentials.
4. After acceptance, run `xcrun stapler staple <app-path>`, verify with
   `codesign --verify --deep --strict --verbose=2 <app-path>` and
   `spctl --assess --type execute --verbose=2 <app-path>`, then recreate the
   distributable ZIP with `ditto -c -k --sequesterRsrc --keepParent <app-path> <zip-path>`.
5. Test the downloaded package on clean Intel and Apple Silicon machines.
   A Mac App Store release instead needs its own sandbox/provisioning/distribution
   configuration and review; this unsigned Docker preset does not claim to supply it.

The native Apple signing/distribution steps above are the explicit exception to
the normal Docker-only policy. They are manual, local Mac operations, never Actions.

## Finish iOS on a Mac

1. Extract the source handoff into its own project directory. Install **Godot
   4.7.2 .NET**, matching export templates, the **.NET 11 SDK**, Apple build tools, and
   Python 3. Accept the build tools license, install the iOS platform/device
   support, and configure the developer command-line tools.
2. Install the .NET iOS workload compatible with your .NET SDK and developer toolchain:
   `dotnet workload install ios`. Follow Microsoft's compatibility guidance if
   the installed workload requires a different toolchain version.
3. Open the extracted project in Godot. In its iOS preset set your real bundle ID,
   ten-character Apple Developer Team ID, versions, distribution identity and
   provisioning settings. Keep **Export Project Only** enabled. ARM64 and
   iPhone+iPad are configured; review minimum iOS support and all icon/privacy
   requirements in the installed editor before release.
4. Run the preflight from that project directory:

   ```bash
   export GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
   bash export/finish_ios.sh --check
   ```

   Adjust GODOT_BIN to the actual .NET app executable. The check does not export
   and cannot verify installed signing certificates, templates or device behavior.
5. When ready to explicitly generate the native iOS project:

   ```bash
   bash export/finish_ios.sh project
   ```

   Output is under build/ios/xcode. Open the generated project (unzip the
   project archive first if needed). Select your Team, matching bundle ID, signing
   certificate and provisioning profile. Resolve iOS capability/privacy/icon
   validation and third-party native plug-in compatibility before archiving.
6. Test on a physical iPhone and iPad, then use the Apple developer toolchain to archive and
   distribute the app for TestFlight/App Store Connect. This setup does
   not create/upload an IPA or choose your signing credentials automatically.

iOS C# support is experimental. Device/.NET AOT and third-party library
compatibility remain unverified until an actual Mac/device build. Configure
reachable production API/database endpoints; the development localhost addresses
are not the game server on a phone. The handoff intentionally contains no .env.

## Verification and sources

`python export/apple_release.py check .` inventories the allowed source inputs
without writing an archive or invoking Godot. Helper tests use a tiny synthetic
project in a temporary directory; they do not export this game.

- [Godot macOS export and signing](https://docs.godotengine.org/en/4.7/tutorials/export/exporting_for_macos.html)
- [Godot iOS requirements](https://docs.godotengine.org/en/4.7/tutorials/export/exporting_for_ios.html)
- [Microsoft .NET for iOS installation](https://learn.microsoft.com/en-us/dotnet/maui/ios/deployment/)
- [Apple notarization workflow](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution)
