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

        static readonly Option[] AdditionalOptionValues =
        {
            new Option(50, "50 tiles",
                $"Continental Territories have 50 land tiles on average."),
            new Option(60, "60 tiles",
                $"Continental Territories have 60 land tiles on average."),
            new Option(70, "70 tiles",
                $"Continental Territories have 70 land tiles on average."),
        };

        [MenuItem("Humankind MOD/Generate Territory Size Options")]
        public static void CreateRuntimeModule()
        {
            var gameOption = CreateGameOption();
            CreateLocalizedStrings(gameOption);

            AssetDatabase.SaveAssets();
        }

        static GameOptionDefinition CreateGameOption()
        {
            var collection = ScriptableObject.CreateInstance<GameOptionDefinitionCollection>();
            AssetDatabase.CreateAsset(collection,
                "Assets/Databases/TerritorySizeGameOptions.asset");

            var originalGameOptionDefinition = GetOriginalTerritorySizeGameOption();

            var gameOptionDefinition = collection.CreateDatatableElement<GameOptionDefinition>();
            gameOptionDefinition.Reset(originalGameOptionDefinition);
            var originalOptions = gameOptionDefinition.States.ToList();

            var insertAt = originalOptions.FindIndex(x => string.Equals("Random", x.Value));
            if (insertAt < 0)
            {
                // let's just inject at the end
                insertAt = originalOptions.Count;
            }
            
            originalOptions.InsertRange(insertAt, AdditionalOptionValues.Select(x =>
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
            }));
            
            gameOptionDefinition.States = originalOptions.ToArray();

            EditorUtility.SetDirty(collection);
            return gameOptionDefinition;
        }

        static void CreateLocalizedStrings(GameOptionDefinition gameOption)
        {
            var collection1 = ScriptableObject.CreateInstance<LocalizedStringTranslationCollection>();
            collection1.LanguageCode = "en-US";
            var gameOptionName = gameOption.name;

            foreach (var option in AdditionalOptionValues)
            {
                collection1.Translations.Add(new LocalizedStringTranslation
                {
                    LocalizationLine = new NonProcessedLocalizationString
                    {
                        Id = $"%{gameOptionName}{option.Value}Title",
                        Body = option.Name
                    },
                    LocalizationState = LocalizationState.Translated
                });
                collection1.Translations.Add(new LocalizedStringTranslation
                {
                    LocalizationLine = new NonProcessedLocalizationString
                    {
                        Id = $"%{gameOptionName}{option.Value}Description",
                        Body = option.Description
                    },
                    LocalizationState = LocalizationState.Translated
                });
            }

            AssetDatabase.CreateAsset(collection1, "Assets/Localization/Translations/Translations.asset");

            collection1.Initialize();

            LocalizationUtils.GenerateRuntime();
        }

        static GameOptionDefinition GetOriginalTerritorySizeGameOption()
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
            if (!assetBundle.TryGetGuidFromAssetPath("Assets/Databases/Option/GameOptionDefinition.asset",
                    out var assetGuid))
            {
                Debug.LogError("Could not find a GUID for GameOptionDefinition.asset");
            }

            var gameOptionDefinitions = assetBundle.FetchAllSubAssetsOfType<GameOptionDefinition>(assetGuid);

            var territorySizeGameOption = (GameOptionDefinition)gameOptionDefinitions
                .FirstOrDefault(x => x.name == "GameOption_TerritoryLandSize");

            if (territorySizeGameOption == null)
            {
                Debug.LogError("Could not find GameOption_TerritoryLandSize GameOptionDefinition");
                return null;
            }

            return territorySizeGameOption;
        }
    }
}