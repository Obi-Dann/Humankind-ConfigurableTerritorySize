using System.Linq;
using System.Reflection;
using Amplitude.Framework.Editor.Localization;
using Amplitude.Framework.Localization;
using Amplitude.Framework.Options;
using Amplitude.Mercury.Data.GameOptions;
using Amplitude.Mercury.Production.Modification;
using Amplitude.Mercury.UI;
using Amplitude.UI;
using UnityEditor;
using UnityEngine;
using AssetBundle = UnityEngine.AssetBundle;
using AssetDatabase = UnityEditor.AssetDatabase;

namespace Editor
{
    public class CreateAssetMenus
    {
        const int defaultValue = 40;

        record Option(int Value, string Name, string Description)
        {
            public int Value { get; } = Value;
            public string Name { get; } = Name;
            public string Description { get; } = Description;
        }

        static readonly Option[] OptionValues =
        {
            new Option(30, "Extra Small (30 tiles)",
                "The average number of land tiles per territory will be set to 30 tiles. It may be difficult to find large enough territories for some of the end-game projects"),
            new Option(35, "Small (35 tiles)",
                "The average number of land tiles per territory will be set to 35 tiles. It may be difficult to find large enough territories for some of the end-game projects"),
            new Option(40, "Standard (40 tiles)", "The default territory size in Humankind"),
            new Option(50, "Large (50 tiles)",
                $"The average number of land tiles per territory will be set to 50 tiles"),
            new Option(60, "Extra Large (60 tiles)",
                $"The average number of land tiles per territory will be set to 60 tiles"),
            new Option(70, "Huge (70 tiles)",
                $"The average number of land tiles per territory will be set to 70 tiles"),
        };

        [MenuItem("Humankind MOD/Generate Territory Size Options")]
        public static void CreateRuntimeModule()
        {
            CreateGameOption();
            CreateUIMapper();
            CreateLocalizedStrings();

            AssetDatabase.SaveAssets();
        }

        static void CreateGameOption()
        {
            var collection = ScriptableObject.CreateInstance<GameOptionDefinitionCollection>();
            AssetDatabase.CreateAsset(collection,
                "Assets/Databases/TerritorySizeGameOptions.asset");

            var gameOptionDefinition =
                collection.CreateDatatableElement<GameOptionDefinition>("GameOption_TerritorySize");

            gameOptionDefinition.Default = $"{defaultValue}";
            gameOptionDefinition.States = OptionValues.Select(x =>
            {
                var state = new OptionState
                {
                    Value = x.Value.ToString(),
                    KeyValuePairs = new[]
                    {
                        new KeyValuePair
                        {
                            Key = "ExpectedRegionArea",
                            Value = x.Value.ToString()
                        }
                    }
                };

                return state;
            }).ToArray();

            EditorUtility.SetDirty(collection);
        }

        static void CreateUIMapper()
        {
            var collection = ScriptableObject.CreateInstance<UIMappersCollection>();
            AssetDatabase.CreateAsset(collection,
                "Assets/Databases/TerritorySizeUIMappers.asset");

            var gameOptionUIMapper = collection.CreateDatatableElement<OptionUIMapper>("GameOption_TerritorySize");

            gameOptionUIMapper.Title = "%OptionRegionSizeTitle";
            gameOptionUIMapper.Description = "%OptionRegionSizeDescription";
            gameOptionUIMapper.ControlType = UIControlType.DropList;

            var originalOptionsGroupUIMapper = GetOriginalOptionsGroupUIMapper();
            if (originalOptionsGroupUIMapper != null)
            {
                var modifiedOptionsGroupUIMapper = (OptionsGroupUIMapper)originalOptionsGroupUIMapper.Clone();
                InjectGameOption(modifiedOptionsGroupUIMapper);
                AssetDatabase.AddObjectToAsset(modifiedOptionsGroupUIMapper, collection);
                EditorUtility.SetDirty(modifiedOptionsGroupUIMapper);
            }

            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(gameOptionUIMapper);
        }

        static void CreateLocalizedStrings()
        {
            var collection1 = ScriptableObject.CreateInstance<LocalizedStringTranslationCollection>();
            collection1.LanguageCode = "en-US";

            foreach (var option in OptionValues)
            {
                collection1.Translations.Add(new LocalizedStringTranslation
                {
                    LocalizationLine = new NonProcessedLocalizationString
                    {
                        Id = $"%GameOption_TerritorySize{option.Value}Title",
                        Body = option.Name
                    },
                    LocalizationState = LocalizationState.Translated
                }); collection1.Translations.Add(new LocalizedStringTranslation
                {
                    LocalizationLine = new NonProcessedLocalizationString
                    {
                        Id = $"%GameOption_TerritorySize{option.Value}Description",
                        Body = option.Description
                    },
                    LocalizationState = LocalizationState.Translated
                });
            }

            AssetDatabase.CreateAsset(collection1, "Assets/Localization/Translations/Translations.asset");
        
            collection1.Initialize();
            
            LocalizationUtils.GenerateRuntime();
        }

        static OptionsGroupUIMapper GetOriginalOptionsGroupUIMapper()
        {
            var allLoaded = AssetBundle.GetAllLoadedAssetBundles();

            var assetBundleFolderName = "MercuryDatabases.AvatarPresentation";
            var assetBundleFileName = assetBundleFolderName.ToLowerInvariant() + ".assetbundle";
            var assetBundleFilePath = System.IO.Path.Combine(ModuleEditor.MercuryFolderPath, "AssetBundles",
                assetBundleFolderName, assetBundleFileName);

            if (!Amplitude.Framework.Asset.AssetDatabase.TryMountAssetBundle(assetBundleFileName, assetBundleFilePath,
                    uint.MaxValue, out var assetProvider, Amplitude.Framework.Asset.AssetBundle.Options.None))
            {
                Debug.LogError("Failed to mount asset bundle");
                return null;
            }

            var assetBundle = (Amplitude.Framework.Asset.AssetBundle)assetProvider;
            if (!assetBundle.TryGetGuidFromAssetPath("Assets/Databases/Option/GameOptionGroupUIMappers.asset",
                    out var assetGuid))
            {
                Debug.LogError("Could not find a GUID for GameOptionGroupUIMappers.asset");
            }

            var optionsGroupUIMappers = assetBundle.FetchAllSubAssetsOfType<OptionsGroupUIMapper>(assetGuid);

            var lobbyWorldOptionsMapper = (OptionsGroupUIMapper)optionsGroupUIMappers
                .FirstOrDefault(x => x.name == "GameOptionGroup_LobbyWorldOptions");

            if (lobbyWorldOptionsMapper == null)
            {
                Debug.LogError("Could not find GameOptionGroup_LobbyWorldOptions OptionsGRoupUIMapper");
                return null;
            }

            return lobbyWorldOptionsMapper;
        }

        static void InjectGameOption(OptionsGroupUIMapper optionsGroupUIMapper)
        {
            var optionNamesField =
                typeof(OptionsGroupUIMapper).GetField("optionsName", BindingFlags.Instance | BindingFlags.NonPublic);

            var optionsList = ((string[])optionNamesField.GetValue(optionsGroupUIMapper)).ToList();

            optionsList.Insert(optionsList.IndexOf("GameOption_Elevation") + 1, "GameOption_TerritorySize");
            optionNamesField.SetValue(optionsGroupUIMapper, optionsList.ToArray());
            optionsGroupUIMapper.Initialize();
        }
    }
}