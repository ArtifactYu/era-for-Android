# era for Android — third-party notices

era for Android is a modified distribution integrating the Emuera interpreter and the andEmuera compatibility port. Its Android application, library, storage module and player interface were created for this project. It is not an original implementation of the Emuera interpreter.

## Licensing

- Original code created for era for Android (the Android launcher, library and storage modules, probe, player interface additions, and QA scripts/fixtures) is licensed under the **PolyForm Noncommercial License 1.0.0** — noncommercial use only. See the repository root `LICENSE`. The English license text is authoritative.
- Emuera-derived portions (the interpreter core, the WebHost port, the andEmuera Android port and the upstream sources) remain under the **Emuera license**, a zlib-style license that permits any use, including commercial use, provided that the origin is not misrepresented, modifications are stated, and the notice is preserved. See `Emuera.LICENSE.txt` and `EmueraDotNet.LICENSE.md`.
- This repository is a **modified version** of Emuera / Emuera.EM+EE / andEmuera. Upstream changes are kept in `patches/`; the modification statement is also repeated in `README-andEmuera.md`.

## Attributions

- Emuera / Emuera.EM+EE: MinorShift, 妊）|дﾟ)の中の人, VVII, EvilMask, Enter and contributors. https://gitlab.com/EvilMask/emuera.em — revision `7b69ebd27378c03c32b6477b74901bfc3d33223c`. See Emuera.LICENSE.txt and EmueraDotNet.LICENSE.md.
- andEmuera compatibility port: BluishCat and contributors. https://github.com/BluishCat/andEmuera — revision `e4ed157ec2712e2103d58834a519a421b7f919bf`. Original source notices and attribution are preserved.
- .NET, System.CommandLine, System.IO.Hashing, Microsoft.Data.Sqlite: Microsoft / .NET Foundation and contributors, MIT. See dotnet.LICENSE.txt and EmueraDotNet.LICENSE.md.
- SkiaSharp 3.119.0: Microsoft / Xamarin and contributors, MIT. See SkiaSharp.LICENSE.txt and Skia.THIRD-PARTY-NOTICES.txt.
- Enums.NET 4.0.2: Tyler Brinkley, MIT. See EnumsNET.LICENSE.txt.
- SQLitePCLRaw 3.0.5: Copyright 2014–2026 SourceGear, LLC. Apache License 2.0. See Apache-2.0.txt. SQLite is public domain.
- BIZ UDGothic: SIL Open Font License 1.1. See BIZUDGothic.OFL.txt.

era for Android modifications include a new Android library and landscape player; scoped directory and ZIP import; save backup/restore; authenticated local transport; Windows-style filename resolution on Android; display image controls; legacy function-name compatibility; serialized timed input execution with an empty-buffer guard; and a collapsible player sidebar.

Version 1.1 also adds legacy Chinese configuration aliases, strict mixed-encoding detection, corrected CSV diagnostics, single-key input, and independently written support for the PuchiEmuera FONTEDGE/FADE/SHAKE and FIXED-image dialect. No PuchiEmuera executable or third-party game resource is distributed with era for Android.

Material 3 Expressive design references: https://m3.material.io/ and https://matraic.github.io/m3e/. No Material website images or game artwork are bundled by era for Android.

Third-party games used for compatibility testing are not included in this application or its source distribution, and download addresses for them are intentionally omitted from the published source.
