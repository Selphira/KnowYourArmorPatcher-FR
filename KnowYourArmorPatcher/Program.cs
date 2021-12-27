using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;

namespace KnowYourArmorPatcher
{
    public class Program
    {
        static ModKey elementalDestruction = ModKey.FromNameAndExtension("Elemental Destruction.esp");
        static ModKey shadowSpellPackage = ModKey.FromNameAndExtension("ShadowSpellPackage.esp");
        private static Lazy<Settings> _settings = null!;
        public static Task<int> Main(string[] args)
        {
            return SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings("Settings", "settings.json", out _settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "know_your_armor_patcher.esp")
                .Run(args);
        }

        private static float AdjustEffectMagnitude(float magnitude, float scale)
        {
            if (magnitude.EqualsWithin(0))
                return magnitude;
            if (magnitude > 1)
                return (magnitude - 1) * scale + 1;
            return 1 / AdjustEffectMagnitude(1 / magnitude, scale);
        }

        private static IEnumerable<string> GetFromJson(string key, JObject jObject)
        {
            return jObject.ContainsKey(key) ? jObject[key]!.Select(x => (string?)x).Where(x => x != null).Select(x => x!).ToList() : new List<string>();
        }

        private static readonly Dictionary<string, IFormLinkGetter<IKeywordGetter>> armorKeywords = new()
        {
            //{ "full", KnowYourEnemy.Keyword.kye_armor_full },
            { "warm", KnowYourEnemy.Keyword.kye_armor_warm },
            { "leathery", KnowYourEnemy.Keyword.kye_armor_leathery },
            //{ "brittle", KnowYourEnemy.Keyword.kye_armor_brittle },
            { "nonconductive", KnowYourEnemy.Keyword.kye_armor_nonconductive },
            { "thick", KnowYourEnemy.Keyword.kye_armor_thick },
            { "metal", KnowYourEnemy.Keyword.kye_armor_metal },
            { "layered", KnowYourEnemy.Keyword.kye_armor_layered },
            { "deep", KnowYourEnemy.Keyword.kye_armor_deep },
        };

        private static void QuickAppend(StringBuilder description, string name, float num)
        {
            // The en-US is to make the whole numbers and decimals split with a . instead of a ,
            if (num != 1) description.Append(" " + name + " x" + Math.Round(num, 2).ToString(new CultureInfo("en-US")) + ",");
        }

        /*private static string GenerateDescription(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string recordEDID, JObject armorRulesJson, float effectIntensity)
        {
            StringBuilder description = new StringBuilder();
            if (armorRulesJson[recordEDID] != null)
            {
                if (armorRulesJson[recordEDID]!["material"] != null)
                {
                    description.Append("Material: " + armorRulesJson[recordEDID]!["material"]!.ToString());
                }
                if (armorRulesJson[recordEDID]!["construction"] != null)
                {
                    if (description.Length != 0) description.Append("; ");
                    description.Append("Construction: " + armorRulesJson[recordEDID]!["construction"]);
                }
                if (description.Length != 0) description.Append(".");

                if (armorRulesJson[recordEDID]!["keywords"] != null)
                {
                    float fire = 1,
                        frost = 1,
                        shock = 1,
                        blade = 1,
                        axe = 1,
                        blunt = 1,
                        arrows = 1,
                        earth = 1,
                        water = 1,
                        wind = 1;

                    string[] keywords = ((JArray)(armorRulesJson[recordEDID]!)).ToObject<string[]>()!;
                    if (keywords.Contains("warm"))
                    {
                        arrows *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        frost *= AdjustEffectMagnitude(0.5f, effectIntensity);
                        water *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        wind *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }

                    if (keywords.Contains("leathery"))
                    {
                        arrows *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        fire *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        wind *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        water *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    if (keywords.Contains("brittle"))
                    {
                        blunt *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        water *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        earth *= AdjustEffectMagnitude(1.25f, effectIntensity);
                    }
                    if (keywords.Contains("nonconductive"))
                    {
                        shock *= AdjustEffectMagnitude(0.25f, effectIntensity);
                        fire *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        frost *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        water *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    if (keywords.Contains("thick"))
                    {
                        arrows *= AdjustEffectMagnitude(0.5f, effectIntensity);
                        blade *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        wind *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    if (keywords.Contains("metal"))
                    {
                        arrows *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        blade *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        shock *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        earth *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        water *= AdjustEffectMagnitude(1.25f, effectIntensity);
                    }
                    if (keywords.Contains("layered"))
                    {
                        arrows *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        wind *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    if (keywords.Contains("deep"))
                    {
                        blunt *= AdjustEffectMagnitude(0.5f, effectIntensity);
                        axe *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        earth *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    QuickAppend(description, "Fire", fire);
                    QuickAppend(description, "Frost", frost);
                    QuickAppend(description, "Shock", shock);
                    QuickAppend(description, "Blade", blade);
                    QuickAppend(description, "Axe", axe);
                    QuickAppend(description, "Blunt", blunt);
                    QuickAppend(description, "Arrows", arrows);

                    // If load order contains Know Your Elements, write descriptions for water + wind + earth
                    if (state.LoadOrder.ContainsKey(ModKey.FromNameAndExtension("Know Your Elements.esp")))
                    {
                        QuickAppend(description, "Water", water);
                        QuickAppend(description, "Wind", wind);
                        QuickAppend(description, "Earth", earth);
                    }

                    // Remove last char if ending with ,
                    if (description[description.Length - 1] == ',') description[description.Length - 1] = '.';
                }
            }
            return description.ToString();
        }*/

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!state.LoadOrder.ContainsKey(KnowYourEnemy.ModKey))
                throw new Exception("ERROR: Know Your Enemy not detected in load order. You need to install KYE prior to running this patcher!");

            if (state.LoadOrder.ContainsKey(elementalDestruction) && !state.LoadOrder.ContainsKey(KnowYourElements.ModKey))
                Console.WriteLine("WARNING: Elemental Destruction Magic detected. For full compatibility with Know Your Enemy please install Know Your Elements!");

            if (!state.LoadOrder.ContainsKey(elementalDestruction) && state.LoadOrder.ContainsKey(KnowYourElements.ModKey))
                Console.WriteLine("WARNING: Know Your Elements detected, but Elemental Destruction Magic was not found!");

            if (state.LoadOrder.ContainsKey(shadowSpellPackage) && !state.LoadOrder.ContainsKey(LightAndShadow.ModKey))
                Console.WriteLine("WARNING: Shadow Spells Package detected. For full compatibility with Know Your Enemy please install Know Your Enemy Light and Shadow!");

            if (!state.LoadOrder.ContainsKey(shadowSpellPackage) && state.LoadOrder.ContainsKey(LightAndShadow.ModKey))
                Console.WriteLine("WARNING: Know Your Enemy Light and Shadow detected, but Shadow Spells Package was not found!");

            /*
            string[] requiredFiles = { , , state.ExtraSettingsDataPath + @"\settings.json" };
            string[] foundFiles = Directory.GetFiles(state.ExtraSettingsDataPath);
            if (!requiredFiles.SequenceEqual(foundFiles))
                throw new Exception("Missing required files! Make sure to copy all files over when installing the patcher, and don't run it from within an archive.");
            */

            // Get all the files in the KYA settings folder and trim the paths.
            List<string> allFiles = Directory.EnumerateFiles(state.ExtraSettingsDataPath).ToList();
            if (!allFiles.Any()) throw new Exception("Settings folder for Know Your Armor is empty. Run the patcher again so Synthesis can populate it.");
            for (int i = 0; i < allFiles.Count; ++i) allFiles[i] = allFiles[i].Substring(allFiles[i].LastIndexOf(@"\") + 1);
            
            // Check if the required settings are present or not.
            if (!allFiles.Contains("armor_rules.json") || !allFiles.Contains("misc.json") || !allFiles.Contains("settings.json"))
                throw new Exception("Core settings files are missing. Delete the settings folder for Know Your Armor and then run the patcher again so Synthesis can populate it.");
            allFiles.Remove("armor_rules.json"); allFiles.Remove("misc.json"); allFiles.Remove("settings.json");

            // If there are any additional files, read the armor rules into the rules list while respecting the load order.
            List<JObject?> armorRules = new();
            if(allFiles.Any())
            {
                Console.WriteLine("\nAdditional Settings Files: ");
                foreach (var modListing in state.LoadOrder.PriorityOrder)
                {
                    string modName = modListing.ModKey.Name + ".json";
                    if (allFiles.Contains(modName))
                    {
                        armorRules.Add(JObject.Parse(File.ReadAllText(state.ExtraSettingsDataPath + @"\" + modName)));
                        Console.WriteLine(" - " + modName + " - PLUGIN PRESENT");
                        allFiles.Remove(modName);
                    }
                }

                foreach (var missingMod in allFiles)
                    Console.WriteLine(" - " + missingMod + " - PLUGIN MISSING");
                Console.WriteLine();
            }

            // Add the base armor rules file to the end of the rules list.
            armorRules.Add(JObject.Parse(File.ReadAllText(state.ExtraSettingsDataPath + @"\armor_rules.json")));
            var miscJson = JObject.Parse(File.ReadAllText(state.ExtraSettingsDataPath + @"\misc.json"));
            //var armorRulesJson = JObject.Parse(File.ReadAllText(requiredFiles[0]));
            //var settingsJson = JObject.Parse(File.ReadAllText(requiredFiles[2]));

            // Converting to list because .Contains in Newtonsoft.JSON has weird behavior
            List<string> armorRaces = GetFromJson("armor_races", miscJson).ToList();
            List<string> ignoredArmors = GetFromJson("ignored_armors", miscJson).ToList();

            float effectIntensity = _settings.Value.EffectIntensity;
            //bool patchArmorDescriptions = _settings.Value.PatchArmorDescriptions;

            Console.WriteLine("Settings Used: ");
            //Console.WriteLine("patch_armor_descriptions: " + patchArmorDescriptions);
            Console.WriteLine("effect_intensity: " + effectIntensity + "\n");

            if (!KnowYourEnemy.Perk.kye_perk_armors2.TryResolve(state.LinkCache, out var perkLink))
                throw new Exception($"Unable to find required perk: {KnowYourEnemy.Perk.kye_perk_armors2}");

            // Returns all keywords from an armor that are found in armor rules json 
            List<string> GetRecognizedKeywords(IArmorGetter armor, JObject armorRuleSet)
            {
                List<string> foundEDIDs = new List<string>();
                if (armor.Keywords == null) return foundEDIDs;
                foreach (var keyword in armor.Keywords)
                {
                    if (keyword.TryResolve(state.LinkCache, out var kw))
                    {
                        if (kw.EditorID != null && armorRuleSet![kw.EditorID] != null)
                        {
                            // Make sure ArmorMaterialIron comes first - fixes weird edge case generating descriptions when ArmorMaterialIronBanded is also in there
                            if (kw.Equals(Skyrim.Keyword.ArmorMaterialIronBanded))
                            {
                                foundEDIDs.Insert(0, kw.EditorID);
                            }
                            else
                            {
                                foundEDIDs.Add(kw.EditorID);
                            }
                        }
                    }
                }
                return foundEDIDs;
            }

            // Part 1
            // Add the armor perk to all relevant NPCs
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList)) continue;

                if (npc.Keywords != null && npc.Keywords.Contains(Skyrim.Keyword.ActorTypeGhost)) continue;

                if (npc.Race.TryResolve(state.LinkCache, out var race) && race.EditorID != null && armorRaces.Contains(race.EditorID))
                {
                    var npcCopy = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                    
                    if (npcCopy.Name != null && npcCopy.Name.TryLookup(Language.French, out string i18nNpcName)) {
                        npcCopy.Name = i18nNpcName;
                    }
                    if (npcCopy.ShortName != null && npcCopy.ShortName.TryLookup(Language.French, out string i18nNpcShortName)) {
                        npcCopy.ShortName = i18nNpcShortName;
                    }
                    
                    if (npcCopy.Perks == null) npcCopy.Perks = new ExtendedList<PerkPlacement>();
                    PerkPlacement p = new PerkPlacement
                    {
                        Rank = 1,
                        Perk = perkLink.AsLink()
                    };
                    npcCopy.Perks.Add(p);
                }
            }

            // Part 2
            // Adjust the magnitude of KYE's effects according to the effectIntensity settings
            if (!effectIntensity.EqualsWithin(1))
            {
                var perk = state.PatchMod.Perks.GetOrAddAsOverride(perkLink);
                foreach (var eff in perk.Effects)
                {

                    if (eff is not PerkEntryPointModifyValue epValue) continue;
                    if (epValue.EntryPoint == APerkEntryPointEffect.EntryType.ModIncomingDamage || epValue.EntryPoint == APerkEntryPointEffect.EntryType.ModIncomingSpellMagnitude)
                        epValue.Value = AdjustEffectMagnitude(epValue.Value ?? 0.0f, effectIntensity);
                }
            }

            // Part 3
            // Add the keywords to each armor (and optionally add descriptions)
            foreach (var armor in state.LoadOrder.PriorityOrder.Armor().WinningOverrides())
            {
                if (armor.EditorID == null || ignoredArmors.Contains(armor.EditorID)) continue;
                if (armor.Keywords == null || !armor.Keywords.Contains(Skyrim.Keyword.ArmorCuirass)) continue;
                if (!armor.TemplateArmor.IsNull) continue;

                foreach (var armorRuleSet in armorRules)
                {
                    if (armorRuleSet is null) continue;
                    List<string> foundEDIDs = GetRecognizedKeywords(armor, armorRuleSet);
                    if (armorRuleSet[armor.EditorID] == null && !foundEDIDs.Any()) continue;

                    List<string> armorKeywordsToAdd = new List<string>();

                    var armorCopy = state.PatchMod.Armors.GetOrAddAsOverride(armor);
                    
                    if (armorCopy.Name != null && armorCopy.Name.TryLookup(Language.French, out string i18nArmorName)) {
                        armorCopy.Name = i18nArmorName;
                    }
                    //var origDescription = armorCopy.Description;
                    foreach (string foundEDID in foundEDIDs)
                    {
                        // Get KYE keywords connected to recognized armor keyword
                        foreach (string keywordToAdd in ((JArray)armorRuleSet[foundEDID]!).ToObject<string[]>()!)
                        {
                            if (!armorKeywordsToAdd.Contains(keywordToAdd))
                                armorKeywordsToAdd.Add(keywordToAdd);
                        }
                        /*if (patchArmorDescriptions)
                        {
                            string desc = GenerateDescription(state, foundEDID, armorRulesJson, effectIntensity);
                            if (!String.IsNullOrEmpty(desc))
                            {
                                if (armorCopy.Description?.String.IsNullOrEmpty() ?? true)
                                {
                                    armorCopy.Description = desc;
                                }
                                else
                                {
                                    if (armorCopy.Description?.String?.EndsWith(".") ?? false)
                                    {
                                        armorCopy.Description = origDescription?.String + " " + desc;
                                    }
                                    else
                                    {
                                        armorCopy.Description = origDescription?.String + ". " + desc;
                                    }
                                }
                            }
                        }*/
                    }

                    if (armorRuleSet[armor.EditorID] != null)
                    {
                        foreach (string? keywordToAdd in ((JArray)armorRuleSet[armor.EditorID]!).ToObject<string[]>()!)
                        {
                            if (keywordToAdd != null && !armorKeywordsToAdd.Contains(keywordToAdd))
                            {
                                armorKeywordsToAdd.Add(keywordToAdd);
                            }

                        }

                        /*if (patchArmorDescriptions)
                        {
                            var desc = GenerateDescription(state, armor.EditorID, armorRulesJson, effectIntensity);
                            if (!String.IsNullOrEmpty(desc))
                            {
                                if (armorCopy.Description?.String.IsNullOrEmpty() ?? true)
                                {
                                    armorCopy.Description = desc;
                                }
                                else
                                {
                                    if (armorCopy.Description?.String?.EndsWith(".") ?? false)
                                    {
                                        armorCopy.Description = origDescription?.String + " " + desc;
                                    }
                                    else
                                    {
                                        armorCopy.Description = origDescription?.String + ". " + desc;
                                    }
                                }
                            }
                        }*/
                    }

                    // Add keywords that are to be added to armor
                    foreach (var keyword in armorKeywordsToAdd)
                    {
                        armorCopy.Keywords!.Add(armorKeywords[keyword]);
                    }
                    break;
                }
            }
        }
    }
}
