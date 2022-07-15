﻿using System;
using Glamourer.Customization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer;

public static unsafe class CustomizeExtensions
{
    // In languages other than english the actual clan name may depend on gender.
    public static string ClanName(SubRace race, Gender gender)
    {
        if (gender == Gender.FemaleNpc)
            gender = Gender.Female;
        if (gender == Gender.MaleNpc)
            gender = Gender.Male;
        return (gender, race) switch
        {
            (Gender.Male, SubRace.Midlander)         => Glamourer.Customization.GetName(CustomName.MidlanderM),
            (Gender.Male, SubRace.Highlander)        => Glamourer.Customization.GetName(CustomName.HighlanderM),
            (Gender.Male, SubRace.Wildwood)          => Glamourer.Customization.GetName(CustomName.WildwoodM),
            (Gender.Male, SubRace.Duskwight)         => Glamourer.Customization.GetName(CustomName.DuskwightM),
            (Gender.Male, SubRace.Plainsfolk)        => Glamourer.Customization.GetName(CustomName.PlainsfolkM),
            (Gender.Male, SubRace.Dunesfolk)         => Glamourer.Customization.GetName(CustomName.DunesfolkM),
            (Gender.Male, SubRace.SeekerOfTheSun)    => Glamourer.Customization.GetName(CustomName.SeekerOfTheSunM),
            (Gender.Male, SubRace.KeeperOfTheMoon)   => Glamourer.Customization.GetName(CustomName.KeeperOfTheMoonM),
            (Gender.Male, SubRace.Seawolf)           => Glamourer.Customization.GetName(CustomName.SeawolfM),
            (Gender.Male, SubRace.Hellsguard)        => Glamourer.Customization.GetName(CustomName.HellsguardM),
            (Gender.Male, SubRace.Raen)              => Glamourer.Customization.GetName(CustomName.RaenM),
            (Gender.Male, SubRace.Xaela)             => Glamourer.Customization.GetName(CustomName.XaelaM),
            (Gender.Male, SubRace.Helion)            => Glamourer.Customization.GetName(CustomName.HelionM),
            (Gender.Male, SubRace.Lost)              => Glamourer.Customization.GetName(CustomName.LostM),
            (Gender.Male, SubRace.Rava)              => Glamourer.Customization.GetName(CustomName.RavaM),
            (Gender.Male, SubRace.Veena)             => Glamourer.Customization.GetName(CustomName.VeenaM),
            (Gender.Female, SubRace.Midlander)       => Glamourer.Customization.GetName(CustomName.MidlanderF),
            (Gender.Female, SubRace.Highlander)      => Glamourer.Customization.GetName(CustomName.HighlanderF),
            (Gender.Female, SubRace.Wildwood)        => Glamourer.Customization.GetName(CustomName.WildwoodF),
            (Gender.Female, SubRace.Duskwight)       => Glamourer.Customization.GetName(CustomName.DuskwightF),
            (Gender.Female, SubRace.Plainsfolk)      => Glamourer.Customization.GetName(CustomName.PlainsfolkF),
            (Gender.Female, SubRace.Dunesfolk)       => Glamourer.Customization.GetName(CustomName.DunesfolkF),
            (Gender.Female, SubRace.SeekerOfTheSun)  => Glamourer.Customization.GetName(CustomName.SeekerOfTheSunF),
            (Gender.Female, SubRace.KeeperOfTheMoon) => Glamourer.Customization.GetName(CustomName.KeeperOfTheMoonF),
            (Gender.Female, SubRace.Seawolf)         => Glamourer.Customization.GetName(CustomName.SeawolfF),
            (Gender.Female, SubRace.Hellsguard)      => Glamourer.Customization.GetName(CustomName.HellsguardF),
            (Gender.Female, SubRace.Raen)            => Glamourer.Customization.GetName(CustomName.RaenF),
            (Gender.Female, SubRace.Xaela)           => Glamourer.Customization.GetName(CustomName.XaelaF),
            (Gender.Female, SubRace.Helion)          => Glamourer.Customization.GetName(CustomName.HelionM),
            (Gender.Female, SubRace.Lost)            => Glamourer.Customization.GetName(CustomName.LostM),
            (Gender.Female, SubRace.Rava)            => Glamourer.Customization.GetName(CustomName.RavaF),
            (Gender.Female, SubRace.Veena)           => Glamourer.Customization.GetName(CustomName.VeenaF),
            _                                        => throw new ArgumentOutOfRangeException(nameof(race), race, null),
        };
    }

    public static string ClanName(this CharacterCustomization customize)
        => ClanName(customize.Clan, customize.Gender);

    // Change a gender and fix up all required customizations afterwards.
    public static bool ChangeGender(this CharacterCustomization customize, Gender gender, CharacterEquip equip)
    {
        if (customize.Gender == gender)
            return false;

        customize.Gender = gender;
        FixUpAttributes(customize, equip);

        return true;
    }

    // Change a race and fix up all required customizations afterwards.
    public static bool ChangeRace(this CharacterCustomization customize, SubRace clan, CharacterEquip equip)
    {
        if (customize.Clan == clan)
            return false;

        var race = clan.ToRace();
        customize.Race = race;
        customize.Clan = clan;

        // TODO: Female Hrothgar
        if (race == Race.Hrothgar)
            customize.Gender = Gender.Male;

        FixUpAttributes(customize, equip);

        return true;
    }

    // Go through a whole customization struct and fix up all settings that need fixing.
    private static void FixUpAttributes(CharacterCustomization customize, CharacterEquip equip)
    {
        var set = Glamourer.Customization.GetList(customize.Clan, customize.Gender);
        foreach (CustomizationId id in Enum.GetValues(typeof(CustomizationId)))
        {
            switch (id)
            {
                case CustomizationId.Race:                  break;
                case CustomizationId.Clan:                  break;
                case CustomizationId.BodyType:              break;
                case CustomizationId.Gender:                break;
                case CustomizationId.FacialFeaturesTattoos: break;
                case CustomizationId.HighlightsOnFlag:      break;
                case CustomizationId.Face:                  break;
                default:
                    var count = set.Count(id);
                    if (set.DataByValue(id, customize[id], out _) < 0)
                        customize[id] = count == 0 ? (byte)0 : set.Data(id, 0).Value;
                    break;
            }
        }

        if (!equip)
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var item = equip[slot];
            var (replaced, newSet, newVariant) =
                Glamourer.RestrictedGear.ResolveRestricted(item.Set, item.Variant, slot, customize.Race, customize.Gender);
            if (replaced)
                equip[slot] = new CharacterArmor(newSet, newVariant, item.Stain);
        }
    }
}
