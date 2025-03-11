# Addressables Generator
This library provides utilities for programatically generating Addressable Groups based on other assets in your project, such as scriptable objects. Generating Addressable Groups is often much easier than manually configuring them, especially when you want to enforce consistent configuration over a large class of assets in your project.

It also provides the ability to automatically generate dependency asset bundles containing assets shared by one of more Groups that would otherwise be duplicated in each of those Groups.

## Installation
We recommend you install this package via [OpenUPM](https://openupm.com/packages/com.jonagill.addressablesgenerator/). Per OpenUPM's documentation:

1. Open `Edit/Project Settings/Package Manager`
2. Add a new Scoped Registry (or edit the existing OpenUPM entry) to read:
    * Name: `package.openupm.com`
    * URL: `https://package.openupm.com`
    * Scope(s): `com.jonagill.addressablesgenerator`
3. Click Save (or Apply)
4. Open Window/Package Manager
5. Click the + button
6. Select `Add package by name...` or `Add package from git URL...` 
7. Enter `com.jonagill.addressablesgenerator` and click Add

# Usage
## Create AssetRequests
All Addressable Group generation is handled by the class `AddressableGroupGenerator`. You register `AssetEntryRequests` with this class via a static API, specifying how you want those particular assets to be grouped into Addressable Groups during the build. You can add asset requests directly via the `AddEntries()` method, or you can register a type-specific callback via the `RegisterGeneratorForType()` method that will automatically generate entry requests from assets of a given type. This has the benefit of automatically re-running the generator every time an asset of that type changes on-disk.

`ScriptableObjectAddressableGroupGenerator` provides a base class and some guidance on using type-specific generators. One thing to note is that, if your source asset references Addressable assets directly (e.g. a prefab library with an array of direct prefab references), you must make sure that these direct references do not themselves affect the Addressable dependency chain. This can be done by making sure your source asset doesn't exist in the build itself (by not including it in Addressables and not referencing it via any other Addressable asset), or by making it so your direct source references are not compiled at runtime by wrapping them in an `#if UNITY_EDITOR` directive.

An alternative method could be to create a `BuildProcessor` class that calls `AddEntries()` right before we build Addressables. This class should have a callback order less than or equal to `AddressablesGeneratorBuildProcessor.CallbackOrder` to ensure that your entries get added before we generate dependency groups and build asset bundles.

## Run a build
By default, registered generators will run during a build if that build is also set to generate Addressables. The build can also be configured to automatically generate dependency bundles as part of the build process (see **Settings** below).

Neither of these steps will happen automatically when manually building Addressables from the Addressable Groups window. You can trigger this behavior via the `Tools/Addressables Generator` menu item if you wish to manually generate groups outside of a build.

## Settings
All of the behavior of this package can be configured via the **Addressables Generator** section of your project's **Project Settings**. These allow you to enable and disable the automatic generation of generated groups and dependency bundles as part of the build process.