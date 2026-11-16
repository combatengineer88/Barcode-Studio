# Barcode Studio
Native C# WinForms barcode generator for **QR codes (2D)** and **Code 128, Code 39, EAN-13, UPC-A, EAN-8, ITF (1D)**. Live preview, PNG export, copy and print.

## Download the Windows installer
Open the **Actions** tab, choose **Build Windows Installer**, open the latest successful run, and download the **BarcodeStudio-Windows-Installer** artifact. Extract it and run the Setup EXE.

For a public GitHub Release, create a version tag such as `v1.0.0`; the workflow builds the installer and attaches it to the release.

## Development
Requires .NET 8 SDK. Run `dotnet run` on Windows. The build workflow uses .NET publish self-contained win-x64 and Inno Setup.

**Retail note:** EAN/UPC barcode images do not provide GS1 licensing or product registration.
