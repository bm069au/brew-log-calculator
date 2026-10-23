# Brew Log & Calculator

A small, free, offline Windows app for ginger beer, cider, beer, wine, mead and
other brews. No accounts, subscriptions, advertisements or paid APIs.

## Download

**[Download BrewLogCalculator-Setup.exe](https://github.com/bm069au/brew-log-calculator/releases/latest/download/BrewLogCalculator-Setup.exe)**

Double-click the setup file, approve Windows' permission prompt and click
**Install**. Then open **Brew Log & Calculator** from Start or the desktop.
No extraction, command line or build tools are needed to install it.
The installer defaults to `C:\Program Files\Brew Log Calculator` and creates a
Start menu shortcut, with an optional desktop shortcut. It needs administrator
permission once; the application itself runs as an ordinary user.

## Features

- Save multiple batches, recipes, quantities, yeast and notes; copy a recipe.
- Enter initial and dated current hydrometer readings (`62` or `1.062`).
- Record the other side of the float separately without guessing its units.
- Temperature-correct readings when sample and calibration temperatures are known.
- Estimate alcohol from original/current gravity; flag rising gravity or a
  changed baseline after later sugar, honey, juice or water additions.
- Show an editable fermentation planning allowance and an observed-rate trend
  towards the recipe's target gravity, with bottle conditioning kept separate.

## Data

Batch data is readable XML **text** in `BrewLog.txt` beside the executable;
`BrewLog.txt.bak` contains the previous save. The installer gives ordinary users
write access to **those two files only**, not the program or directory. It is a
shared local log, not encrypted per-user storage. Copy both files elsewhere for
independent backups. Reinstall/uninstall retains them.

## Limits

Alcohol is approximate, not a laboratory measurement. Later additions require
a revised baseline; this version does not guess that adjustment. The second
scale's units must be identified before interpreting it. It is recorded only.

The default 7–21 day window is an **editable planning allowance**, not a
recipe/yeast/temperature simulation. A trend is straight-line extrapolation and
may slow or stall. No automatic safe-to-bottle verdict is provided.

See `README.txt` for usage, calculation references and detailed limits.

## Build

Windows x64, installed .NET Framework 4.x compiler. Run `build.ps1`; outputs go
to `dist`. No NuGet or runtime download. `verify.ps1` checks math, persistence,
exception cases and the embedded installer payload using synthetic data only.

Compilation, synthetic checks and both UI-tab renders passed. The installed
Program Files / non-admin write / uninstall flow still needs a human check;
no administrator-level installation was performed during development.

## Licence

MIT. Free to use, modify and redistribute. The supplied installer is unsigned.

## Creator and support

Created by **Brett Martin Moore**. This program is free. Development funds are
often limited; voluntary donations are welcome if you can afford to help.
Open **Help > About** for Contact and Donations buttons, or email
[bm069au@gmail.com](mailto:bm069au@gmail.com) to get in touch or ask how to donate.
