﻿using BepInEx.Configuration;
using LethalLevelLoader;
using LethalLib.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CentralConfig
{
    public class ConfigAider : MonoBehaviour
    {
        private static ConfigAider _instance;
        public static ConfigAider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("ConfigAider").AddComponent<ConfigAider>();
                }
                return _instance;
            }
        }
        // Safety box of LLL config stuff renamed + some other random methods
        public const string DaComma = ",";
        public const string DaColon = ":";
        public const string DaDash = "-";
        public static List<Item> Items { get; internal set; } = new List<Item>();
        public static string CauterizeString(string inputString)
        {
            return (inputString.SkipToLetters().RemoveWhitespace().ToLower());
        }

        internal static List<Item> GrabFullItemList()
        {
            List<Item> allItems = StartOfRound.Instance.allItemsList.itemsList;
            foreach (Item item in allItems)
            {
                if (!Items.Contains(item))
                {
                    Items.Add(item);
                }
            }
            return Items.ToList();
        }

        public static List<EnemyType> GrabFullEnemyList()
        {
            List<EnemyType> allEnemies = new List<EnemyType>();

            foreach (EnemyType enemy in OriginalContent.Enemies)
            {
                if (!allEnemies.Contains(enemy))
                {
                    allEnemies.Add(enemy);
                }
            }
            foreach (ExtendedEnemyType extendedEnemy in PatchedContent.ExtendedEnemyTypes)
            {
                if (!allEnemies.Contains(extendedEnemy.EnemyType))
                {
                    allEnemies.Add(extendedEnemy.EnemyType);
                }
            }
            foreach (Enemies.SpawnableEnemy spawnableEnemy in Enemies.spawnableEnemies)
            {
                if (!allEnemies.Contains(spawnableEnemy.enemy))
                {
                    allEnemies.Add(spawnableEnemy.enemy);
                }
            }
            return allEnemies;
        }
        public static List<ContentTag> GrabFullTagList()
        {
            List<ContentTag> allContentTagsList = new List<ContentTag>();
            List<ExtendedLevel> allExtendedLevels = PatchedContent.ExtendedLevels;

            foreach (ExtendedMod extendedMod in PatchedContent.ExtendedMods.Concat(new List<ExtendedMod>() { PatchedContent.VanillaMod }))
            {
                foreach (ExtendedContent extendedContent in extendedMod.ExtendedContents)
                {
                    foreach (ContentTag contentTag in extendedContent.ContentTags)
                    {
                        if (allExtendedLevels.Any(level => level.ContentTags.Contains(contentTag)) && !allContentTagsList.Contains(contentTag))
                        {
                            allContentTagsList.Add(contentTag);
                        }
                    }
                }
            }
            return allContentTagsList;
        }

        public static List<string> SplitStringsByDaComma(string newInputString)
        {
            List<string> stringList = new List<string>();

            string inputString = newInputString;

            while (inputString.Contains(DaComma))
            {
                string inputStringWithoutTextBeforeFirstComma = inputString.Substring(inputString.IndexOf(DaComma));
                stringList.Add(inputString.Replace(inputStringWithoutTextBeforeFirstComma, ""));
                if (inputStringWithoutTextBeforeFirstComma.Contains(DaComma))
                    inputString = inputStringWithoutTextBeforeFirstComma.Substring(inputStringWithoutTextBeforeFirstComma.IndexOf(DaComma) + 1);

            }
            stringList.Add(inputString);

            return stringList;
        }
        public static (string, string) SplitStringsByDaColon(string inputString)
        {
            return SplitStringByChars(inputString, DaColon);
        }
        public static (string, string) SplitStringByDaDash(string inputString)
        {
            return SplitStringByChars(inputString, DaDash);
        }
        public static (string, string) SplitStringByChars(string newInputString, string splitValue)
        {
            if (!newInputString.Contains(splitValue))
                return ((newInputString, string.Empty));
            else
            {
                string firstValue = string.Empty;
                string secondValue = string.Empty;
                firstValue = newInputString.Replace(newInputString.Substring(newInputString.IndexOf(splitValue)), "");
                secondValue = newInputString.Substring(newInputString.IndexOf(splitValue) + 1);
                return ((firstValue, secondValue));
            }
        }

        public static List<StringWithRarity> ConvertStringToList(string newInputString, Vector2 clampRarity)
        {
            List<StringWithRarity> returnList = new List<StringWithRarity>();

            List<string> stringList = SplitStringsByDaComma(newInputString);

            foreach (string stringString in stringList)
            {
                (string, string) splitStringData = SplitStringsByDaColon(stringString);
                string levelName = splitStringData.Item1;
                int rarity = 0;
                if (int.TryParse(splitStringData.Item2, out int value))
                    rarity = value;

                if (clampRarity != Vector2.zero)
                    rarity = Mathf.Clamp(rarity, Mathf.RoundToInt(clampRarity.x), Mathf.RoundToInt(clampRarity.y));

                returnList.Add(new StringWithRarity(levelName, rarity));
            }
            return returnList;
        }

        // Enemies

        public static string ConvertEnemyListToString(List<SpawnableEnemyWithRarity> spawnableEnemiesList)
        {
            string returnString = string.Empty;

            foreach (SpawnableEnemyWithRarity spawnableEnemyWithRarity in spawnableEnemiesList)
            {
                if (spawnableEnemyWithRarity.enemyType.enemyName != "Lasso" && spawnableEnemyWithRarity.rarity > 0)
                {
                    returnString += spawnableEnemyWithRarity.enemyType.enemyName + DaColon + spawnableEnemyWithRarity.rarity.ToString() + DaComma;
                }
            }

            if (returnString.Contains(",") && returnString.LastIndexOf(",") == (returnString.Length - 1))
                returnString = returnString.Remove(returnString.LastIndexOf(","), 1);

            if (returnString == string.Empty)
                returnString = "Default Values Were Empty";

            return returnString;
        }
        public static List<SpawnableEnemyWithRarity> ConvertStringToEnemyList(string newInputString, Vector2 clampRarity)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty")
            {
                return new List<SpawnableEnemyWithRarity>();
            }
            List<StringWithRarity> stringList = ConvertStringToList(newInputString, clampRarity);
            List<SpawnableEnemyWithRarity> returnList = new List<SpawnableEnemyWithRarity>();

            List<EnemyType> AllEnemies = GrabFullEnemyList();

            foreach (EnemyType enemyType in AllEnemies)
            {
                foreach (StringWithRarity stringString in new List<StringWithRarity>(stringList))
                {
                    if (enemyType.enemyName.ToLower().Contains(stringString.Name.ToLower()))
                    {
                        SpawnableEnemyWithRarity newEnemy = new SpawnableEnemyWithRarity();
                        newEnemy.enemyType = enemyType;
                        newEnemy.rarity = stringString.Rarity;
                        returnList.Add(newEnemy);
                        stringList.Remove(stringString);
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Return List:");
            foreach (SpawnableEnemyWithRarity enemyType in returnList)
            {
                sb.AppendLine($"Item Name: {enemyType.enemyType.enemyName}, Rarity: {enemyType.rarity}");
            }
            // CentralConfig.instance.mls.LogInfo(sb.ToString());

            return returnList;
        }
        public static List<SpawnableEnemyWithRarity> RemoveLowerRarityDuplicateEnemies(List<SpawnableEnemyWithRarity> enemies)
        {
            return enemies.GroupBy(e => e.enemyType.enemyName).Select(g => g.OrderByDescending(e => e.rarity).First()).ToList();
        }
        public static List<SpawnableEnemyWithRarity> ReplaceEnemies(List<SpawnableEnemyWithRarity> EnemyList, string ReplaceConfig) // takes in the original enemy list and the config for replacing enemies
        {
            if (string.IsNullOrEmpty(ReplaceConfig) || ReplaceConfig == "Default Values Were Empty")
            {
                return EnemyList; // if the config is unused just returns the list untouched
            }
            List<SpawnableEnemyWithRarity> returnList = new List<SpawnableEnemyWithRarity>(); // makes a new list of enemies
            List<string> replacedEnemies = new List<string>(); // To remember what was replaced
            var pairs = ReplaceConfig.Split(','); // This is expected to be like EnemyName:EnemyName,EnemyName:EnemyName etc

            foreach (var pair in pairs) // so for each pair of enemy names
            {
                var parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    var originalName = parts[0].Trim(); // first enemy name is the original
                    var replacementName = parts[1].Trim(); // second enemy name is the replacement

                    foreach (var enemy in EnemyList) // goes through the enemy list
                    {
                        if (enemy.enemyType.enemyName.Equals(originalName)) // if the entry matches an original name
                        {
                            SpawnableEnemyWithRarity newEnemy = new SpawnableEnemyWithRarity();
                            newEnemy.enemyType = GetEnemyTypeByName(replacementName); // method to check all enemyTypes and get the one with the name of the replacement, then sets the new enemies' enemyType to the replacements
                            newEnemy.rarity = enemy.rarity; // uses the same rarity
                            returnList.Add(newEnemy); // adds this new enemy to the returnlist
                            replacedEnemies.Add(originalName); // adds the replacement to the string list I made
                            CentralConfig.instance.mls.LogInfo($"Replaced enemy: {originalName} -> {replacementName}");
                        }
                    }
                }
            }
            foreach (var enemy in EnemyList) // goes through the enemy list again
            {
                if (!replacedEnemies.Contains(enemy.enemyType.enemyName)) // if it wasn't replaced by another enemy
                {
                    SpawnableEnemyWithRarity oldEnemy = new SpawnableEnemyWithRarity();
                    oldEnemy.enemyType = enemy.enemyType; // add it as is
                    oldEnemy.rarity = enemy.rarity; // same
                    returnList.Add(oldEnemy); // yeah its added to the return list untouched
                    CentralConfig.instance.mls.LogInfo("Added non replaced enemy: " + oldEnemy.enemyType.enemyName);
                }
            }
            return returnList; // and gets the list back
        }
        public static EnemyType GetEnemyTypeByName(string enemyName)
        {
            List<EnemyType> AllEnemies = GrabFullEnemyList();
            foreach (var enemyType in AllEnemies)
            {
                if (enemyType.enemyName == enemyName)
                {
                    return enemyType;
                }
            }
            return null;
        }

        // Scrap

        public static string ConvertItemListToString(List<SpawnableItemWithRarity> spawnableItemsList)
        {
            string returnString = string.Empty;

            foreach (SpawnableItemWithRarity spawnableItemWithRarity in spawnableItemsList)
            {
                if (spawnableItemWithRarity.rarity > 0)
                {
                    returnString += spawnableItemWithRarity.spawnableItem.itemName + DaColon + spawnableItemWithRarity.rarity.ToString() + DaComma;
                }
            }
            if (returnString.Contains(",") && returnString.LastIndexOf(",") == (returnString.Length - 1))
                returnString = returnString.Remove(returnString.LastIndexOf(","), 1);

            if (returnString == string.Empty)
                returnString = "Default Values Were Empty";
            return returnString;
        }
        public static List<SpawnableItemWithRarity> ConvertStringToItemList(string newInputString, Vector2 clampRarity)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty")
            {
                return new List<SpawnableItemWithRarity>();
            }
            List<StringWithRarity> stringList = ConvertStringToList(newInputString, clampRarity);
            List<SpawnableItemWithRarity> returnList = new List<SpawnableItemWithRarity>();

            List<Item> allItems = GrabFullItemList();

            foreach (Item item in allItems)
            {
                foreach (StringWithRarity stringString in new List<StringWithRarity>(stringList))
                {
                    if (CauterizeString(item.itemName).Contains(CauterizeString(stringString.Name)) || CauterizeString(stringString.Name).Contains(CauterizeString(item.itemName)))
                    {
                        SpawnableItemWithRarity newItem = new SpawnableItemWithRarity();
                        newItem.spawnableItem = item;
                        newItem.rarity = stringString.Rarity;
                        returnList.Add(newItem);
                        stringList.Remove(stringString);
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Return List:");
            foreach (SpawnableItemWithRarity item in returnList)
            {
                sb.AppendLine($"Item Name: {item.spawnableItem.itemName}, Rarity: {item.rarity}");
            }
            // CentralConfig.instance.mls.LogInfo(sb.ToString());

            return returnList;
        }
        public static List<SpawnableItemWithRarity> RemoveLowerRarityDuplicateItems(List<SpawnableItemWithRarity> items)
        {
            return items.GroupBy(e => e.spawnableItem.itemName).Select(g => g.OrderByDescending(e => e.rarity).First()).ToList();
        }

        // Weather + Tags

        public static string ConvertWeatherArrayToString(RandomWeatherWithVariables[] randomWeatherArray)
        {
            string returnString = "";
            bool noneExists = false;

            foreach (RandomWeatherWithVariables randomWeather in randomWeatherArray)
            {
                if (randomWeather.weatherType.ToString() == "None")
                {
                    noneExists = true;
                }
                returnString += randomWeather.weatherType + DaComma;
            }

            if (!noneExists)
            {
                returnString = "None" + DaComma + returnString;
            }

            if (returnString.Contains(",") && returnString.LastIndexOf(",") == (returnString.Length - 1))
            {
                returnString = returnString.Remove(returnString.LastIndexOf(","), 1);
            }

            return returnString;
        }
        public static RandomWeatherWithVariables[] ConvertStringToWeatherArray(string newInputString)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty")
            {
                return new RandomWeatherWithVariables[] { new RandomWeatherWithVariables { weatherType = LevelWeatherType.None } };
            }

            string[] weatherStrings = newInputString.Split(',');

            RandomWeatherWithVariables[] returnArray = new RandomWeatherWithVariables[weatherStrings.Length];

            for (int i = 0; i < weatherStrings.Length; i++)
            {
                if (Enum.TryParse(weatherStrings[i], out LevelWeatherType weatherType))
                {
                    returnArray[i] = new RandomWeatherWithVariables { weatherType = weatherType };
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Return Array:");
            foreach (RandomWeatherWithVariables weatherType in returnArray)
            {
                sb.AppendLine($"{weatherType.weatherType}");
            }
            // CentralConfig.instance.mls.LogInfo(sb.ToString());
            return returnArray;
        }

        public static string ConvertTagsToString(List<ContentTag> contentTags)
        {
            string returnString = string.Empty;

            foreach (ContentTag contentTag in contentTags)
                returnString += contentTag.contentTagName + DaComma;

            if (returnString.EndsWith(","))
                returnString = returnString.Remove(returnString.LastIndexOf(","), 1);

            if (string.IsNullOrEmpty(returnString))
                returnString = "Default Values Were Empty";

            return returnString;
        }
        public static List<ContentTag> ConvertStringToTagList(string newInputString)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty")
            {
                return new List<ContentTag>();
            }
            List<string> stringList = newInputString.Split(',').ToList();
            List<ContentTag> returnList = new List<ContentTag>();

            List<ContentTag> allContentTagsList = GrabFullTagList();

            foreach (ContentTag contentTag in allContentTagsList)
            {
                foreach (string stringString in new List<string>(stringList))
                {
                    if (CauterizeString(contentTag.contentTagName).Contains(CauterizeString(stringString)) || CauterizeString(stringString).Contains(CauterizeString(contentTag.contentTagName)))
                    {
                        returnList.Add(contentTag);
                        stringList.Remove(stringString);
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Return List:");
            foreach (ContentTag tag in returnList)
            {
                sb.AppendLine($"{tag.contentTagName}");
            }
            // CentralConfig.instance.mls.LogInfo(sb.ToString());
            return returnList;
        }

        // Misc String

        public static string ConvertStringWithRarityToString(List<StringWithRarity> names)
        {
            string returnString = string.Empty;

            foreach (StringWithRarity name in names)
            {
                if (name.Rarity > 0)
                {
                    returnString += name.Name + DaColon + name.Rarity.ToString() + DaComma;
                }
            }

            if (returnString.Contains(",") && returnString.LastIndexOf(",") == (returnString.Length - 1))
                returnString = returnString.Remove(returnString.LastIndexOf(","), 1);

            if (returnString == string.Empty)
                returnString = "Default Values Were Empty";

            return (returnString);
        }
        public static List<StringWithRarity> ConvertModStringToStringWithRarityList(string newInputString, Vector2 clampRarity)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty" || newInputString == "Default Values Were Empty:0" || newInputString == ":0")
            {
                return new List<StringWithRarity>();
            }
            List<StringWithRarity> returnList = new List<StringWithRarity>();

            List<string> stringList = SplitStringsByDaComma(newInputString);

            foreach (string stringString in stringList)
            {
                (string, string) splitStringData = SplitStringsByDaColon(stringString);
                string modName = splitStringData.Item1;
                int rarity = 0;
                if (int.TryParse(splitStringData.Item2, out int value))
                    rarity = value;

                if (clampRarity != Vector2.zero)
                    Mathf.Clamp(rarity, Mathf.RoundToInt(clampRarity.x), Mathf.RoundToInt(clampRarity.y));

                returnList.Add(new StringWithRarity(modName, rarity));
            }
            return (returnList);
        }
        public static List<StringWithRarity> ConvertTagStringToStringWithRarityList(string newInputString, Vector2 clampRarity)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty" || newInputString == "Default Values Were Empty:0" || newInputString == ":0")
            {
                return new List<StringWithRarity>();
            }
            List<StringWithRarity> returnList = new List<StringWithRarity>();

            List<string> stringList = SplitStringsByDaComma(newInputString);

            foreach (string stringString in stringList)
            {
                (string, string) splitStringData = SplitStringsByDaColon(stringString);
                string tagName = splitStringData.Item1;
                int rarity = 0;
                if (int.TryParse(splitStringData.Item2, out int value))
                    rarity = value;

                if (clampRarity != Vector2.zero)
                    Mathf.Clamp(rarity, Mathf.RoundToInt(clampRarity.x), Mathf.RoundToInt(clampRarity.y));

                returnList.Add(new StringWithRarity(tagName, rarity));
            }
            return (returnList);
        }

        // This method should make the planet name list have to be the exact name

        public static List<StringWithRarity> ConvertPlanetNameStringToStringWithRarityList(string newInputString, Vector2 clampRarity)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty" || newInputString == "Default Values Were Empty:0" || newInputString == ":0")
            {
                return new List<StringWithRarity>();
            }
            List<StringWithRarity> returnList = new List<StringWithRarity>();

            List<string> stringList = SplitStringsByDaComma(newInputString);

            HashSet<string> allPlanetNames = new HashSet<string>(PatchedContent.ExtendedLevels.Select(level => level.NumberlessPlanetName));

            foreach (string stringString in stringList)
            {
                (string planetName, string rarityString) = SplitStringsByDaColon(stringString);
                int rarity = 0;
                if (int.TryParse(rarityString, out int value))
                    rarity = value;

                if (clampRarity != Vector2.zero)
                    Mathf.Clamp(rarity, Mathf.RoundToInt(clampRarity.x), Mathf.RoundToInt(clampRarity.y));

                if (allPlanetNames.Contains(planetName))
                {
                    returnList.Add(new StringWithRarity(planetName, rarity));
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Return List:");
            foreach (StringWithRarity planetName in returnList)
            {
                sb.AppendLine($"{planetName.Name}: {planetName.Rarity}");
            }
            // CentralConfig.instance.mls.LogInfo(sb.ToString());
            return (returnList);
        }

        // For Route Price settings

        public static string ConvertVector2WithRaritiesToString(List<Vector2WithRarity> values)
        {
            string returnString = string.Empty;

            foreach (Vector2WithRarity vector2withRarity in values)
                returnString += vector2withRarity.Min + DaDash + vector2withRarity.Max + DaColon + vector2withRarity.Rarity + DaComma;

            if (returnString.Contains(",") && returnString.LastIndexOf(",") == (returnString.Length - 1))
                returnString = returnString.Remove(returnString.LastIndexOf(","), 1);

            if (returnString == string.Empty)
                returnString = "Default Values Were Empty";

            return (returnString);
        }
        public static List<Vector2WithRarity> ConvertStringToVector2WithRarityList(string newInputString, Vector2 clampRarity)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty" || newInputString == "Default Values Were Empty:0" || newInputString == ":0" || newInputString == "0-0:0")
            {
                return new List<Vector2WithRarity>();
            }
            List<Vector2WithRarity> returnList = new List<Vector2WithRarity>();

            List<string> stringList = SplitStringsByDaComma(newInputString);

            foreach (string stringString in stringList)
            {
                (string, string) splitStringData = SplitStringsByDaColon(stringString);
                (string, string) vector2Strings = SplitStringByDaDash(splitStringData.Item1);

                float x = 0f;
                float y = 0f;
                int rarity = 0;
                if (float.TryParse(vector2Strings.Item1, out float xValue))
                    x = xValue;
                if (float.TryParse(vector2Strings.Item2, out float yValue))
                    y = yValue;
                if (int.TryParse(splitStringData.Item2, out int value))
                    rarity = value;

                if (clampRarity != Vector2.zero)
                    Mathf.Clamp(rarity, Mathf.RoundToInt(clampRarity.x), Mathf.RoundToInt(clampRarity.y));

                returnList.Add(new Vector2WithRarity(new Vector2(x, y), rarity));
            }
            return (returnList);
        }

        // Dungeons

        public static List<ExtendedDungeonFlowWithRarity> ConvertStringToDungeonFlowWithRarityList(string newInputString, Vector2 clampRarity)
        {
            if (string.IsNullOrEmpty(newInputString) || newInputString == "Default Values Were Empty" || newInputString == "Default Values Were Empty:0" || newInputString == ":0")
            {
                return new List<ExtendedDungeonFlowWithRarity>();
            }
            List<StringWithRarity> stringList = ConvertStringToList(newInputString, clampRarity);
            List<ExtendedDungeonFlowWithRarity> returnList = new List<ExtendedDungeonFlowWithRarity>();

            List<ExtendedDungeonFlow> alldungeonFlows = PatchedContent.ExtendedDungeonFlows;

            foreach (ExtendedDungeonFlow dungeon in alldungeonFlows)
            {
                foreach (StringWithRarity stringString in new List<StringWithRarity>(stringList))
                {
                    if (CauterizeString(dungeon.DungeonName).Contains(CauterizeString(stringString.Name)) || CauterizeString(stringString.Name).Contains(CauterizeString(dungeon.DungeonName)))
                    {
                        ExtendedDungeonFlowWithRarity newDungeon = new ExtendedDungeonFlowWithRarity(dungeon, stringString.Rarity);
                        newDungeon.extendedDungeonFlow = dungeon;
                        newDungeon.rarity = stringString.Rarity;
                        returnList.Add(newDungeon);
                        stringList.Remove(stringString);
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Return List:");
            foreach (ExtendedDungeonFlowWithRarity dungeon in returnList)
            {
                sb.AppendLine($"Item Name: {dungeon.extendedDungeonFlow.DungeonName}, Rarity: {dungeon.rarity}");
            }
            // CentralConfig.instance.mls.LogInfo(sb.ToString());

            return returnList;
        }

        // For updating enemy spawn curves
        public static AnimationCurve MultiplyYValues(AnimationCurve curve, float multiplier, string LevelName, string TypeOf)
        {
            // CentralConfig.instance.mls.LogInfo(LevelName + " " +TypeOf + " Y-Values multiplied by: " + multiplier);
            Keyframe[] keyframes = new Keyframe[curve.length];

            for (int i = 0; i < curve.length; i++)
            {
                Keyframe key = curve[i];
                key.value = key.value * multiplier;
                keyframes[i] = key;
            }

            return new AnimationCurve(keyframes);
        }

        // Modified cfg cleaner from Kitten :3

        public void CleanConfig(ConfigFile cfg)
        {
            StartCoroutine(CQueen(cfg));
        }
        private IEnumerator CQueen(ConfigFile cfg)
        {
            while (!ApplyMoonConfig.Ready || !ApplyDungeonConfig.Ready || !ApplyTagConfig.Ready || !ApplyWeatherConfig.Ready)
            {
                yield return null;
            }
            ConfigCleaner(cfg);
        }
        private void ConfigCleaner(ConfigFile cfg)
        {
            PropertyInfo orphanedEntriesProp = cfg.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg, null);
            orphanedEntries.Clear();
            cfg.Save();
            CentralConfig.instance.mls.LogInfo("Config Cleaned");
        }
    }
}