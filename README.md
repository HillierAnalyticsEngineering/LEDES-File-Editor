# LEDES-File-Editor
C#.NET Framework Windows Forms Application for loading, editing, previewing, and writing .LED LEDES 98B files for legal billing

This is a new free-for-commercial-use and open-source data processing application I've written. Since I've started at Harpst Becker LLC, I've had to start working with specially delimited file formats for Legal Billing called LEDES 1998B files. This file format (.LED) is a 24-field, pipe-delimited ASCII file. Our legal billing software didn't allow us to take posted bills and generate LEDES files with ease, so I created this windows forms application (Free and Open-Source under GNU_GPLv3.0) in order to upload, edit, add new lines to, preview, and write .LED files with ease. If you work in law or legal billing, feel free to download the application to use for free (the .exe file in the repository), or share it to your lawyer, paralegal, and legal billing friends.

According to LEDES.org (Ledes Oversight Committee):
"LEDES 1998 was the first standard issued by the cross-industry consortium led by PricewaterhouseCoopers (PwC) for ebilling. The format documentation is no longer available. The standard was quickly updated and replaced by LEDES 1998B.

LEDES 1998B is an ASCII, pipe delimited format containing 24 fields. It is currently the most-widely used ebilling standard in the legal industry in the US.

It is not intended that the LEDES 1998B format will be modified in the future."

## Supported formats

| Format | Layout | Support |
|---|---|---|
| LEDES 1998B | Pipe-delimited, 24 fields, `LEDES1998B[]` | Open, edit, validate, save, convert |
| LEDES 1998BI V2 | Pipe-delimited, 52 fields, `LEDES98BI V2[]` | Open, edit, validate, save, convert |
| LEDES 1998BI and other delimited variants | Pipe-delimited, columns from the file header | Open, edit, validate by field name, save |
| LEDES 2000, LEDES XML 2.0, 2.1, 2.2 | XML | Edit `<fee>` and `<expense>` line items in place; the rest of the XML is preserved |

The format is detected from the first line or the XML root. Delimited files can be converted between 1998B and 1998BI V2 from the Format box.

## Features

- Starts with a bundled example 1998B invoice. A 1998BI V2 example is under **Example**.
- **Validate** (F5) checks dates (CCYYMMDD), numbers, line types (F, E, IF, IE), required fields, stray `|` or `[]` characters, line totals (units x unit cost + adjustment, plus tax for BI V2), invoice totals, and invoice-level fields that disagree across lines. Problems show in the Issues tab and on the cells. Double-click an issue to jump to it.
- **Preview Output** shows exactly what will be saved, including the `[]` record terminators.
- **Save As** validates first, warns about errors, and writes through a temporary file so a failed save never damages the original.
- **Help** (F1) explains every button and rule.
- Errors never close the app. File problems (missing, locked, read-only, too large, not LEDES) get a plain-language message. Unexpected errors are logged to `%LOCALAPPDATA%\HillierLEDES\error.log`.
- Drag a file onto the window or the .exe to open it. Shortcuts: Ctrl+O, Ctrl+S, Ctrl+P, F5, F1.

## Building

Open `Hillier LEDES Reader.sln` in Visual Studio 2019 or later and build (targets .NET Framework 4.7.2). The example files and help text are embedded resources from `Samples\` and `Resources\`.

#data #work #law #legal #gnu #pwc #software #lawyer #windows #csharp #csharpdotnet #opensource #opensourcesoftware #winforms #share
