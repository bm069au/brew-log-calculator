Brew Log & Calculator 1.0 — Windows x64

Run BrewLogCalculator-Setup.exe. Windows requests administrator permission once
to install to C:\Program Files\Brew Log Calculator. Start the app from Start or
the desktop. No account, server, internet connection or AI service is used.
Requires the Windows .NET Framework 4.x runtime (present on this Windows PC).

1. Enter a batch name, drink, volume, recipe quantities, yeast and start date.
2. Enter the initial specific gravity taken before fermentation, AFTER initial
   ingredients were mixed. 98 = 1.098; 62 = 1.062; 104 = 1.104 (not 1.004).
3. Enter sample temperature if known; blank means no correction is possible.
   The 60 F shown on Brett's meter is its calibration: 15.56 C. Change this
   field for a different hydrometer. This is NOT a thermometer reading.
4. Record the OTHER side separately; it is not automatically interpreted.
   The label/units of the second pictured scale have not been confirmed.
5. Add dated current readings on the second tab. Use a degassed sample and
   read the bottom of the meniscus at eye level, with the float clear of the
   container's bottom and sides. Recalculate after changing batch settings.
6. Save. Copy recipe creates a fresh batch with no old gravity readings.

DATA
Readable XML text is stored in BrewLog.txt BESIDE THE PROGRAM; the previous
save is BrewLog.txt.bak. Open TXT displays it in Notepad. Copy these files
elsewhere for independent backups. Avoid editing while the program is open.
The installer grants ordinary Windows users read/write to those two files
ONLY, not the app executable or install directory. Thus all local users can
read/edit this shared log. It is not an encrypted or per-user private store.
No hidden AppData storage or silent alternate save location is used. Reinstall
keeps existing text files. Windows Installed Apps uninstall keeps the data.
If the log cannot be parsed, saving is disabled so the original is protected.
Keep both files before attempting manual repair or restoring the backup.

CALCULATIONS AND LIMITS
Approximate alcohol so far = (corrected original SG - corrected current SG)
times 131.25. Example: 1.098 -> 1.062 at equal calibration temperatures gives
4.725% ABV. This is an estimate; accuracy is limited, especially for strong brews.
Temperature correction uses the published water-density ratio polynomial with
Celsius converted to Fahrenheit internally. Supported sample/calibration range:
0–40 C. Unknown temperatures are explicitly left uncorrected.

Later sugar/honey/juice/water changes the baseline. Tick Later additions and
record amounts/dates in Notes. The app withholds a misleading ABV rather than
guessing an adjustment. Stevia products may contain other ingredients; log them.
An increasing SG is flagged, never reported as negative alcohol.

The editable 7–21 day window is a starter PLANNING ALLOWANCE, not a validated
prediction for every recipe, drink, yeast or temperature. Replace it with your
recipe/yeast instructions. It does not automatically infer fermentability from
recipe prose. Two readings at least 24 h apart can show a straight-line estimate
to your chosen target SG; fermentation may slow or stall, so this is not a
deadline. No target is inferred from stevia/honey or the type of drink.
Bottle conditioning is extra time (rough planning allowance 1–3 weeks for fizz,
sometimes longer). Clearing/ageing can add further time.

There is NO automatic "safe to bottle" verdict. Active fermentation must not
be sealed into bottles. Stable readings can indicate stalled fermentation;
compare with the expected final gravity and recipe. Use measured priming sugar
and pressure-rated bottles. Sweetness from fermentable sugar can restart it.

REFERENCES
https://www.brewersfriend.com/abv-calculator/
https://www.vinolab.hr/calculator/hydrometer-temperature-correction-en31
https://www.brewersfriend.com/hydrometer-temp/
https://www.diybeer.com/pages/faq

SOURCE / BUILD
BrewLog.cs, Installer.cs, manifests and build.ps1 are retained with the project.
Uses the installed .NET Framework C# compiler and Windows Forms; no packages
or new runtime download. Build results are in dist. This is an unsigned local
tool: Windows may show an unknown-publisher warning for the installer.

SOFTWARE LICENCE
MIT License

Copyright (c) 2026 Brett Moore

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


Created by Brett Martin Moore. This program is free. If you can afford to help
with development, voluntary donations are welcome. Use Help > About > Donations
or contact bm069au@gmail.com to ask how to contribute.
