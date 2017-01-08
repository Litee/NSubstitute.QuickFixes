Simple Roslyn-based Visual Studio extension to speed up tests development when using NSubstitute mocking library. Inspired by https://github.com/ycherkes/MockIt

## Supported features:
* Generate mocks from constructor calls:

![](https://github.com/Litee/NSubstitute.QuickFixes/blob/master/media/nsubstitute-generate-mocks-as-fields.gif)

Note: If you will add more parameters into existing constructor call then extension will offer you to generate mocks for new parameters

## How to use:
* Install "NSubstitute.QuickFixes" extension into Visual Studio
* Refer NSubstitute library from your test project - analyzer ignores code if NSubstitute is not referenced!