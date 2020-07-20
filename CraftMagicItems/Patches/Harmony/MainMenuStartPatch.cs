using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CraftMagicItems.Constants;
using CraftMagicItems.Localization;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using UnityEngine;

namespace CraftMagicItems.Patches.Harmony
{

    [Harmony12.HarmonyPatch(typeof(MainMenu), "Start")]
    public static class MainMenuStartPatch
    {
        private static bool mainMenuStarted;

        private static void InitialiseCraftingData()
        {
            // Read the crafting data now that ResourcesLibrary is loaded.
            Main.LoadedData.ItemCraftingData = Main.ReadJsonFile<ItemCraftingData[]>($"{Main.ModEntry.Path}/Data/ItemTypes.json", new CraftingTypeConverter());
            // Initialise lookup tables.
            foreach (var itemData in Main.LoadedData.ItemCraftingData)
            {
                if (itemData is RecipeBasedItemCraftingData recipeBased)
                {
                    recipeBased.Recipes = recipeBased.RecipeFileNames.Aggregate(Enumerable.Empty<RecipeData>(),
                        (all, fileName) => all.Concat(Main.ReadJsonFile<RecipeData[]>($"{Main.ModEntry.Path}/Data/{fileName}"))
                    ).Where(recipe => {
                        return (recipe.ResultItem != null)
                            || (recipe.Enchantments.Length > 0)
                            || (recipe.NoResultItem && recipe.NoEnchantments);
                    }).ToArray();

                    foreach (var recipe in recipeBased.Recipes)
                    {
                        if (recipe.ResultItem != null)
                        {
                            if (recipe.NameId == null)
                            {
                                recipe.NameId = recipe.ResultItem.Name;
                            }
                            else
                            {
                                recipe.NameId = new L10NString(recipe.NameId).ToString();
                            }
                        }
                        else if (recipe.NameId != null)
                        {
                            recipe.NameId = new L10NString(recipe.NameId).ToString();
                        }
                        if (recipe.ParentNameId != null)
                        {
                            recipe.ParentNameId = new L10NString(recipe.ParentNameId).ToString();
                        }
                        recipe.Enchantments.ForEach(enchantment => Main.AddRecipeForEnchantment(enchantment.AssetGuid, recipe));
                        if (recipe.Material != 0)
                        {
                            Main.AddRecipeForMaterial(recipe.Material, recipe);
                        }

                        if (recipe.ParentNameId != null)
                        {
                            recipeBased.SubRecipes = recipeBased.SubRecipes ?? new Dictionary<string, List<RecipeData>>();
                            if (!recipeBased.SubRecipes.ContainsKey(recipe.ParentNameId))
                            {
                                recipeBased.SubRecipes[recipe.ParentNameId] = new List<RecipeData>();
                            }

                            recipeBased.SubRecipes[recipe.ParentNameId].Add(recipe);
                        }
                    }

                    if (recipeBased.Name.StartsWith("CraftMundane"))
                    {
                        foreach (var blueprint in recipeBased.NewItemBaseIDs)
                        {
                            if (!blueprint.AssetGuid.Contains("#CraftMagicItems"))
                            {
                                Main.AddItemForType(blueprint);
                            }
                        }
                    }
                }

                if (itemData.ParentNameId != null)
                {
                    if (!Main.LoadedData.SubCraftingData.ContainsKey(itemData.ParentNameId))
                    {
                        Main.LoadedData.SubCraftingData[itemData.ParentNameId] = new List<ItemCraftingData>();
                    }

                    Main.LoadedData.SubCraftingData[itemData.ParentNameId].Add(itemData);
                }
            }

            var allUsableItems = Resources.FindObjectsOfTypeAll<BlueprintItemEquipment>();
            foreach (var item in allUsableItems)
            {
                Main.AddItemIdForEnchantment(item);
            }

            var allNonRecipeEnchantmentsInItems = Resources.FindObjectsOfTypeAll<BlueprintEquipmentEnchantment>()
                .Where(enchantment => !Main.LoadedData.EnchantmentIdToRecipe.ContainsKey(enchantment.AssetGuid) && Main.LoadedData.EnchantmentIdToItem.ContainsKey(enchantment.AssetGuid))
                .ToArray();
            // BlueprintEnchantment.EnchantmentCost seems to be full of nonsense values - attempt to set cost of each enchantment by using the prices of
            // items with enchantments.
            foreach (var enchantment in allNonRecipeEnchantmentsInItems)
            {
                var itemsWithEnchantment = Main.LoadedData.EnchantmentIdToItem[enchantment.AssetGuid];
                foreach (var item in itemsWithEnchantment)
                {
                    if (Main.DoesItemMatchAllEnchantments(item, enchantment.AssetGuid))
                    {
                        Main.LoadedData.EnchantmentIdToCost[enchantment.AssetGuid] = item.Cost;
                        break;
                    }
                }
            }

            foreach (var enchantment in allNonRecipeEnchantmentsInItems)
            {
                if (!Main.LoadedData.EnchantmentIdToCost.ContainsKey(enchantment.AssetGuid))
                {
                    var itemsWithEnchantment = Main.LoadedData.EnchantmentIdToItem[enchantment.AssetGuid];
                    foreach (var item in itemsWithEnchantment)
                    {
                        if (ReverseEngineerEnchantmentCost(item, enchantment.AssetGuid))
                        {
                            break;
                        }
                    }
                }
            }

            Main.LoadedData.CustomLootItems = Main.ReadJsonFile<CustomLootItem[]>($"{Main.ModEntry.Path}/Data/LootItems.json");
        }

        private static void AddCraftingFeats(ObjectIDGenerator idGenerator, BlueprintProgression progression)
        {
            foreach (var levelEntry in progression.LevelEntries)
            {
                foreach (var featureBase in levelEntry.Features)
                {
                    var selection = featureBase as BlueprintFeatureSelection;
                    if (selection != null && (Features.CraftingFeatGroups.Contains(selection.Group) || Features.CraftingFeatGroups.Contains(selection.Group2)))
                    {
                        // Use ObjectIDGenerator to detect which shared lists we've added the feats to.
                        idGenerator.GetId(selection.AllFeatures, out var firstTime);
                        if (firstTime)
                        {
                            foreach (var data in Main.LoadedData.ItemCraftingData)
                            {
                                if (data.FeatGuid != null)
                                {
                                    var featBlueprint = ResourcesLibrary.TryGetBlueprint(data.FeatGuid) as BlueprintFeature;
                                    var list = selection.AllFeatures.ToList();
                                    list.Add(featBlueprint);
                                    selection.AllFeatures = list.ToArray();
                                    idGenerator.GetId(selection.AllFeatures, out firstTime);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AddAllCraftingFeats()
        {
            var idGenerator = new ObjectIDGenerator();
            // Add crafting feats to general feat selection
            AddCraftingFeats(idGenerator, Game.Instance.BlueprintRoot.Progression.FeatsProgression);
            // ... and to relevant class feat selections.
            foreach (var characterClass in Game.Instance.BlueprintRoot.Progression.CharacterClasses)
            {
                AddCraftingFeats(idGenerator, characterClass.Progression);
            }

            // Alchemists get Brew Potion as a bonus 1st level feat, except for Grenadier archetype alchemists.
            var brewPotionData = Main.LoadedData.ItemCraftingData.First(data => data.Name == "Potion");
            var brewPotion = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(brewPotionData.FeatGuid);
            var alchemistProgression = ResourcesLibrary.TryGetBlueprint<BlueprintProgression>(ClassBlueprints.AlchemistProgressionGuid);
            var grenadierArchetype = ResourcesLibrary.TryGetBlueprint<BlueprintArchetype>(ClassBlueprints.AlchemistGrenadierArchetypeGuid);
            if (brewPotion != null && alchemistProgression != null && grenadierArchetype != null)
            {
                var firstLevelIndex = alchemistProgression.LevelEntries.FindIndex((levelEntry) => (levelEntry.Level == 1));
                alchemistProgression.LevelEntries[firstLevelIndex].Features.Add(brewPotion);
                alchemistProgression.UIDeterminatorsGroup = alchemistProgression.UIDeterminatorsGroup.Concat(new[] { brewPotion }).ToArray();
                // Vanilla Grenadier has no level 1 RemoveFeatures, but a mod may have changed that, so search for it as well.
                var firstLevelGrenadierRemoveIndex = grenadierArchetype.RemoveFeatures.FindIndex((levelEntry) => (levelEntry.Level == 1));
                if (firstLevelGrenadierRemoveIndex < 0)
                {
                    var removeFeatures = new[] { new LevelEntry { Level = 1 } };
                    grenadierArchetype.RemoveFeatures = removeFeatures.Concat(grenadierArchetype.RemoveFeatures).ToArray();
                    firstLevelGrenadierRemoveIndex = 0;
                }
                grenadierArchetype.RemoveFeatures[firstLevelGrenadierRemoveIndex].Features.Add(brewPotion);
            }
            else
            {
                Main.ModEntry.Logger.Warning("Failed to locate Alchemist progression, Grenadier archetype or Brew Potion feat!");
            }

            // Scroll Savant should get Scribe Scroll as a bonus 1st level feat.
            var scribeScrollData = Main.LoadedData.ItemCraftingData.First(data => data.Name == "Scroll");
            var scribeScroll = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(scribeScrollData.FeatGuid);
            var scrollSavantArchetype = ResourcesLibrary.TryGetBlueprint<BlueprintArchetype>(ClassBlueprints.ScrollSavantArchetypeGuid);
            if (scribeScroll != null && scrollSavantArchetype != null)
            {
                var firstLevelAdd = scrollSavantArchetype.AddFeatures.First((levelEntry) => (levelEntry.Level == 1));
                firstLevelAdd.Features.Add(scribeScroll);
            }
            else
            {
                Main.ModEntry.Logger.Warning("Failed to locate Scroll Savant archetype or Scribe Scroll feat!");
            }
        }

        [AllowMultipleComponents]
        public class TwoWeaponFightingAttackPenaltyPatch : RuleInitiatorLogicComponent<RuleCalculateAttackBonusWithoutTarget>, IInitiatorRulebookHandler<RuleAttackWithWeapon>
        {
            public BlueprintFeature shieldMaster;
            public BlueprintFeature prodigiousTwoWeaponFighting;
            private int penalty = 0;

            public override void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
            {
                penalty = 0;
                ItemEntityWeapon maybeWeapon = evt.Initiator.Body.PrimaryHand.MaybeWeapon;
                ItemEntityWeapon maybeWeapon2 = evt.Initiator.Body.SecondaryHand.MaybeWeapon;
                bool flag = maybeWeapon2 != null && evt.Weapon == maybeWeapon2 && maybeWeapon2.IsShield && base.Owner.Progression.Features.HasFact(shieldMaster);
                if (evt.Weapon == null || maybeWeapon == null || maybeWeapon2 == null || maybeWeapon.Blueprint.IsNatural || maybeWeapon2.Blueprint.IsNatural ||
                    maybeWeapon == evt.Initiator.Body.EmptyHandWeapon || maybeWeapon2 == evt.Initiator.Body.EmptyHandWeapon ||
                    (maybeWeapon != evt.Weapon && maybeWeapon2 != evt.Weapon) || flag)
                {
                    return;
                }
                int rank = base.Fact.GetRank();
                int num = (rank <= 1) ? -4 : -2;
                int num2 = (rank <= 1) ? -8 : -2;
                penalty = (evt.Weapon != maybeWeapon) ? num2 : num;
                UnitPartWeaponTraining unitPartWeaponTraining = base.Owner.Get<UnitPartWeaponTraining>();
                bool flag2 = (base.Owner.State.Features.EffortlessDualWielding && unitPartWeaponTraining != null && unitPartWeaponTraining.IsSuitableWeapon(maybeWeapon2))
                    || (prodigiousTwoWeaponFighting != null && base.Owner.Progression.Features.HasFact(prodigiousTwoWeaponFighting));
                if (!maybeWeapon2.Blueprint.IsLight && !maybeWeapon.Blueprint.Double && !flag2)
                {
                    penalty += -2;
                }
                evt.AddBonus(penalty, base.Fact);
            }
            public override void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }

            public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
            {
                if (!evt.IsFullAttack && penalty != 0)
                {
                    evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus.AddModifier(-penalty, this, ModifierDescriptor.UntypedStackable));
                }
            }
            public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
        }

        [AllowMultipleComponents]
        public class ShieldMasterPatch : GameLogicComponent, IInitiatorRulebookHandler<RuleCalculateDamage>, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>
        {
            public void OnEventAboutToTrigger(RuleCalculateDamage evt)
            {
                if (!evt.Initiator.Body.SecondaryHand.HasShield || evt.DamageBundle.Weapon == null || !evt.DamageBundle.Weapon.IsShield)
                {
                    return;
                }
                var armorEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.ArmorComponent);
                var weaponEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.WeaponComponent);
                var itemEnhancementBonus = armorEnhancementBonus - weaponEnhancementBonus;
                PhysicalDamage physicalDamage = evt.DamageBundle.WeaponDamage as PhysicalDamage;
                if (physicalDamage != null && itemEnhancementBonus > 0)
                {
                    physicalDamage.Enchantment += itemEnhancementBonus;
                    physicalDamage.EnchantmentTotal += itemEnhancementBonus;
                }
            }
            public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

            public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
            {
                if (!evt.Initiator.Body.SecondaryHand.HasShield || evt.Weapon == null || !evt.Weapon.IsShield)
                {
                    return;
                }
                var armorEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.ArmorComponent);
                var weaponEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.WeaponComponent);
                var itemEnhancementBonus = armorEnhancementBonus - weaponEnhancementBonus;
                if (itemEnhancementBonus > 0)
                {
                    evt.AddBonusDamage(itemEnhancementBonus);
                }
            }
            public void OnEventDidTrigger(RuleCalculateDamage evt) { }

            public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
            {
                if (!evt.Initiator.Body.SecondaryHand.HasShield || evt.Weapon == null || !evt.Weapon.IsShield)
                {
                    return;
                }
                var armorEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.ArmorComponent);
                var weaponEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.WeaponComponent);
                var num = armorEnhancementBonus - weaponEnhancementBonus;
                if (num > 0)
                {
                    evt.AddBonus(num, base.Fact);
                }
            }
            public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
        }

        private static void PatchBlueprints()
        {
            var shieldMaster = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Features.ShieldMasterGuid);
            var twoWeaponFighting = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(MechanicsBlueprints.TwoWeaponFightingBasicMechanicsGuid);
            for (int i = 0; i < twoWeaponFighting.ComponentsArray.Length; i++)
            {
                if (twoWeaponFighting.ComponentsArray[i] is TwoWeaponFightingAttackPenalty component)
                {
                    twoWeaponFighting.ComponentsArray[i] = CraftMagicItems.Accessors.Create<TwoWeaponFightingAttackPenaltyPatch>(a => {
                        a.name = component.name.Replace("TwoWeaponFightingAttackPenalty", "TwoWeaponFightingAttackPenaltyPatch");
                        a.shieldMaster = shieldMaster;
                        a.prodigiousTwoWeaponFighting = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Features.ProdigiousTwoWeaponFightingGuid);
                    });
                }
                Main.Accessors.SetBlueprintUnitFactDisplayName(twoWeaponFighting, new L10NString("e32ce256-78dc-4fd0-bf15-21f9ebdf9921"));
            }

            for (int i = 0; i < shieldMaster.ComponentsArray.Length; i++)
            {
                if (shieldMaster.ComponentsArray[i] is ShieldMaster component)
                {
                    shieldMaster.ComponentsArray[i] = CraftMagicItems.Accessors.Create<ShieldMasterPatch>(a => {
                        a.name = component.name.Replace("ShieldMaster", "ShieldMasterPatch");
                    });
                }
            }

            var lightShield = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponType>(ItemQualityBlueprints.WeaponLightShieldGuid);
            Main.Accessors.SetBlueprintItemBaseDamage(lightShield, new DiceFormula(1, DiceType.D3));
            var heavyShield = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponType>(ItemQualityBlueprints.WeaponHeavyShieldGuid);
            Main.Accessors.SetBlueprintItemBaseDamage(heavyShield, new DiceFormula(1, DiceType.D4));

            for (int i = 0; i < EnchantmentBlueprints.ItemEnchantmentGuids.Length; i++)
            {
                var source = ResourcesLibrary.TryGetBlueprint<BlueprintItemEnchantment>(EnchantmentBlueprints.ItemEnchantmentGuids[i].WeaponEnchantmentGuid);
                var dest = ResourcesLibrary.TryGetBlueprint<BlueprintItemEnchantment>(EnchantmentBlueprints.ItemEnchantmentGuids[i].UnarmedEnchantmentGuid);
                Main.Accessors.SetBlueprintItemEnchantmentEnchantName(dest, Main.Accessors.GetBlueprintItemEnchantmentEnchantName(source));
                Main.Accessors.SetBlueprintItemEnchantmentDescription(dest, Main.Accessors.GetBlueprintItemEnchantmentDescription(source));
            }

            var longshankBane = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponEnchantment>(EnchantmentBlueprints.LongshankBaneGuid);
            if (longshankBane.ComponentsArray.Length >= 2 && longshankBane.ComponentsArray[1] is WeaponConditionalDamageDice conditional)
            {
                for (int i = 0; i < conditional.Conditions.Conditions.Length; i++)
                {
                    if (conditional.Conditions.Conditions[i] is Kingmaker.Designers.EventConditionActionSystem.Conditions.HasFact condition)
                    {
                        var replace = ScriptableObject.CreateInstance<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact>();
                        replace.Fact = condition.Fact;
                        replace.name = condition.name.Replace("HasFact", "ContextConditionHasFact");
                        conditional.Conditions.Conditions[i] = replace;
                    }
                }
            }
        }

        private static void InitialiseMod()
        {
            if (Main.modEnabled)
            {
                PatchBlueprints();
                LeftHandVisualDisplayPatcher.PatchLeftHandedWeaponModels();
                InitialiseCraftingData();
                AddAllCraftingFeats();
            }
        }

        [Harmony12.HarmonyPriority(Harmony12.Priority.Last)]
        // ReSharper disable once UnusedMember.Local
        private static void Postfix()
        {
            if (!mainMenuStarted)
            {
                mainMenuStarted = true;
                InitialiseMod();
            }
        }

        public static void ModEnabledChanged()
        {
            if (!Main.modEnabled)
            {
                // Reset everything InitialiseMod initialises
                Main.LoadedData.ItemCraftingData = null;
                Main.LoadedData.SubCraftingData.Clear();
                Main.LoadedData.SpellIdToItem.Clear();
                Main.LoadedData.TypeToItem.Clear();
                Main.LoadedData.EnchantmentIdToItem.Clear();
                Main.LoadedData.EnchantmentIdToCost.Clear();
                Main.LoadedData.EnchantmentIdToRecipe.Clear();
            }
            else if (mainMenuStarted)
            {
                // If the mod is enabled and we're past the Start of main menu, (re-)initialise.
                InitialiseMod();
            }
            L10N.SetEnabled(Main.modEnabled);
        }

        // Attempt to work out the cost of enchantments which aren't in recipes by checking if blueprint, which contains the enchantment, contains only other
        // enchantments whose cost is known.
        private static bool ReverseEngineerEnchantmentCost(BlueprintItemEquipment blueprint, string enchantmentId)
        {
            if (blueprint == null || blueprint.IsNotable || blueprint.Ability != null || blueprint.ActivatableAbility != null)
            {
                return false;
            }

            if (blueprint is BlueprintItemShield || blueprint is BlueprintItemWeapon || blueprint is BlueprintItemArmor)
            {
                // Cost of enchantments on arms and armor is different, and can be treated as a straight delta.
                return true;
            }

            var mostExpensiveEnchantmentCost = 0;
            var costSum = 0;
            foreach (var enchantment in blueprint.Enchantments)
            {
                if (enchantment.AssetGuid == enchantmentId)
                {
                    continue;
                }

                if (!Main.LoadedData.EnchantmentIdToRecipe.ContainsKey(enchantment.AssetGuid) && !Main.LoadedData.EnchantmentIdToCost.ContainsKey(enchantment.AssetGuid))
                {
                    return false;
                }

                var enchantmentCost = Main.GetEnchantmentCost(enchantment.AssetGuid, blueprint);
                costSum += enchantmentCost;
                if (mostExpensiveEnchantmentCost < enchantmentCost)
                {
                    mostExpensiveEnchantmentCost = enchantmentCost;
                }
            }

            var remainder = blueprint.Cost - 3 * costSum / 2;
            if (remainder >= mostExpensiveEnchantmentCost)
            {
                // enchantmentId is the most expensive enchantment
                Main.LoadedData.EnchantmentIdToCost[enchantmentId] = remainder;
            }
            else
            {
                // mostExpensiveEnchantmentCost is the most expensive enchantment
                Main.LoadedData.EnchantmentIdToCost[enchantmentId] = (2 * remainder + mostExpensiveEnchantmentCost) / 3;
            }

            return true;
        }
    }
}