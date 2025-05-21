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

# Example
Here is a minimal example of a `ScriptableObjectAddressableGroupGenerator` that takes a ScriptableObject containing an editor-assigned array of prefabs, adds those prefabs to an Addressable Group named "Enemy Prefabs", and then creates AssetReferences to each of those prefabs for use in gameplay. By configuring the settings of your generator and the `AssetEntryRequests` it constructs, you can modify the settings of the generated group and assets to add labels, build as single or individual bundles, and so on.

```
public class EnemyPrefabs : ScriptableObject
{
  // Used in gameplay
  [SerializeField, HideInInspector] public AssetReferenceGameObject[] enemyPrefabReferences;
  
  #if UNITY_EDITOR

  // Assigned via the inspector. Marked as #if UNITY_EDITOR so that we don't contain these direct references
  // in the build, which would cause us to load all of our prefab data as soon as anyone referenced this asset
  // An alternative might be to split your editor and runtime data across two separate ScriptableObjects,
  // only one of which is referenced at runtime.
  [SerializeField] public GameObject[] editorSourcePrefabs;

  public void EditorCopySourcePrefabsToReferences()
  {
      enemyPrefabReferences = editorSourcePrefabs.Select(prefab =>
      {
          string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
          return new AssetReferenceGameObject(guid);
      }).ToArray();
  }

  #endif
}

public class EnemyPrefabAddressablesGroupGenerator : ScriptableObjectAddressableGroupGenerator<EnemyPrefabs, EnemyPrefabAddressablesGroupGenerator>
{
  [InitializeOnLoadMethod]
  private static void InitializeOnLoad()
  {
      // Must be called by all implementers of ScriptableObjectAddressableGroupGenerator to add them to the system
      RegisterGenerator();
  }
  
  protected override BundledAssetGroupSchema.BundlePackingMode BundlePackingMode => BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
  protected override bool ClearGroup => true;
  
  protected override string GenerateGroupNameForAsset(EnemyPrefabs asset)
  {
      return "Enemy Prefabs";
  }

  protected override IReadOnlyList<AddressableGroupGenerator.AssetEntryRequest> GenerateAssetRequestsForAsset(EnemyPrefabs asset)
  {
      List<AddressableGroupGenerator.AssetEntryRequest> requests = new();
      requests.AddRange(asset.editorSourcePrefabs.Select(prefab => new AddressableGroupGenerator.AssetEntryRequest()
      {
          asset = prefab
      }));
      
      return requests;
  }

  protected override void OnAddressablesGeneratedForAsset(EnemyPrefabs asset)
  {
      asset.EditorCopySourcePrefabsToReferences();
      EditorUtility.SetDirty(asset);
  }
}

```

## Run a build
By default, registered generators will run during a build if that build is also set to generate Addressables. The build can also be configured to automatically generate dependency bundles as part of the build process (see **Settings** below).

Neither of these steps will happen automatically when manually building Addressables from the Addressable Groups window. You can trigger this behavior via the `Tools/Addressables Generator` menu item if you wish to manually generate groups outside of a build.

## Settings
All of the behavior of this package can be configured via the **Addressables Generator** section of your project's **Project Settings**. These allow you to enable and disable the automatic generation of generated groups and dependency bundles as part of the build process.
