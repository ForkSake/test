﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

// Each Subrace and Gender combo has a customization set.
// This describes the available customizations, their types and their names.
public class CustomizationSet
{
    internal CustomizationSet(SubRace clan, Gender gender)
    {
        Gender = gender;
        Clan   = clan;
        _settingAvailable = clan.ToRace() == Race.Hrothgar && gender == Gender.Female
            ? 0u
            : DefaultAvailable;
    }

    public Gender  Gender { get; }
    public SubRace Clan   { get; }

    public Race Race
        => Clan.ToRace();

    private uint _settingAvailable;

    internal void SetAvailable(CustomizationId id)
        => _settingAvailable |= 1u << (int)id;

    public bool IsAvailable(CustomizationId id)
        => (_settingAvailable & (1u << (int)id)) != 0;

    public int NumEyebrows    { get; internal init; }
    public int NumEyeShapes   { get; internal init; }
    public int NumNoseShapes  { get; internal init; }
    public int NumJawShapes   { get; internal init; }
    public int NumMouthShapes { get; internal init; }

    public string ToHumanReadable(CustomizationData customizationData)
    {
        var sb = new StringBuilder();
        foreach (var id in Enum.GetValues<CustomizationId>().Where(IsAvailable))
            sb.AppendFormat("{0,-20}", Option(id)).Append(customizationData[id]);

        return sb.ToString();
    }


    public IReadOnlyList<string>                       OptionName      { get; internal set; }  = null!;
    public IReadOnlyList<Customization>                Faces           { get; internal init; } = null!;
    public IReadOnlyList<Customization>                HairStyles      { get; internal init; } = null!;
    public IReadOnlyList<Customization>                TailEarShapes   { get; internal init; } = null!;
    public IReadOnlyList<IReadOnlyList<Customization>> FeaturesTattoos { get; internal set; }  = null!;
    public IReadOnlyList<Customization>                FacePaints      { get; internal init; } = null!;

    public IReadOnlyList<Customization> SkinColors           { get; internal init; } = null!;
    public IReadOnlyList<Customization> HairColors           { get; internal init; } = null!;
    public IReadOnlyList<Customization> HighlightColors      { get; internal init; } = null!;
    public IReadOnlyList<Customization> EyeColors            { get; internal init; } = null!;
    public IReadOnlyList<Customization> TattooColors         { get; internal init; } = null!;
    public IReadOnlyList<Customization> FacePaintColorsLight { get; internal init; } = null!;
    public IReadOnlyList<Customization> FacePaintColorsDark  { get; internal init; } = null!;
    public IReadOnlyList<Customization> LipColorsLight       { get; internal init; } = null!;
    public IReadOnlyList<Customization> LipColorsDark        { get; internal init; } = null!;

    public IReadOnlyList<CharaMakeParams.MenuType>                          Types { get; internal set; } = null!;
    public IReadOnlyDictionary<CharaMakeParams.MenuType, CustomizationId[]> Order { get; internal set; } = null!;


    public string Option(CustomizationId id)
        => OptionName[(int)id];

    public Customization FacialFeature(int faceIdx, int idx)
        => FeaturesTattoos[faceIdx - 1][idx];

    public int DataByValue(CustomizationId id, byte value, out Customization? custom)
    {
        var type = id.ToType();
        custom = null;
        if (type == CharaMakeParams.MenuType.Percentage || type == CharaMakeParams.MenuType.ListSelector)
        {
            if (value < Count(id))
            {
                custom = new Customization(id, value, 0, value);
                return value;
            }

            return -1;
        }

        int Get(IEnumerable<Customization> list, ref Customization? output)
        {
            var (val, idx) = list.Cast<Customization?>().Select((c, i) => (c, i)).FirstOrDefault(c => c.c!.Value.Value == value);
            if (val == null)
                return -1;

            output = val;
            return idx;
        }

        return id switch
        {
            CustomizationId.SkinColor      => Get(SkinColors,                                       ref custom),
            CustomizationId.EyeColorL      => Get(EyeColors,                                        ref custom),
            CustomizationId.EyeColorR      => Get(EyeColors,                                        ref custom),
            CustomizationId.HairColor      => Get(HairColors,                                       ref custom),
            CustomizationId.HighlightColor => Get(HighlightColors,                                  ref custom),
            CustomizationId.TattooColor    => Get(TattooColors,                                     ref custom),
            CustomizationId.LipColor       => Get(LipColorsDark.Concat(LipColorsLight),             ref custom),
            CustomizationId.FacePaintColor => Get(FacePaintColorsDark.Concat(FacePaintColorsLight), ref custom),

            CustomizationId.Face                  => Get(Faces,              ref custom),
            CustomizationId.Hairstyle             => Get(HairStyles,         ref custom),
            CustomizationId.TailEarShape          => Get(TailEarShapes,      ref custom),
            CustomizationId.FacePaint             => Get(FacePaints,         ref custom),
            CustomizationId.FacialFeaturesTattoos => Get(FeaturesTattoos[0], ref custom),
            _                                     => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };
    }

    public Customization Data(CustomizationId id, int idx)
    {
        if (idx > Count(id))
            throw new IndexOutOfRangeException();

        switch (id.ToType())
        {
            case CharaMakeParams.MenuType.Percentage:   return new Customization(id, (byte)idx, 0, (ushort)idx);
            case CharaMakeParams.MenuType.ListSelector: return new Customization(id, (byte)idx, 0, (ushort)idx);
        }

        return id switch
        {
            CustomizationId.Face                  => Faces[idx],
            CustomizationId.Hairstyle             => HairStyles[idx],
            CustomizationId.TailEarShape          => TailEarShapes[idx],
            CustomizationId.FacePaint             => FacePaints[idx],
            CustomizationId.FacialFeaturesTattoos => FeaturesTattoos[0][idx],

            CustomizationId.SkinColor      => SkinColors[idx],
            CustomizationId.EyeColorL      => EyeColors[idx],
            CustomizationId.EyeColorR      => EyeColors[idx],
            CustomizationId.HairColor      => HairColors[idx],
            CustomizationId.HighlightColor => HighlightColors[idx],
            CustomizationId.TattooColor    => TattooColors[idx],
            CustomizationId.LipColor       => idx < 96 ? LipColorsDark[idx] : LipColorsLight[idx - 96],
            CustomizationId.FacePaintColor => idx < 96 ? FacePaintColorsDark[idx] : FacePaintColorsLight[idx - 96],
            _                              => new Customization(0, 0),
        };
    }

    public CharaMakeParams.MenuType Type(CustomizationId id)
        => Types[(int)id];

    internal static IReadOnlyDictionary<CharaMakeParams.MenuType, CustomizationId[]> ComputeOrder(CustomizationSet set)
    {
        var ret = (CustomizationId[])Enum.GetValues(typeof(CustomizationId));
        ret[(int)CustomizationId.TattooColor] = CustomizationId.EyeColorL;
        ret[(int)CustomizationId.EyeColorL]   = CustomizationId.EyeColorR;
        ret[(int)CustomizationId.EyeColorR]   = CustomizationId.TattooColor;

        return ret.Skip(2).Where(set.IsAvailable).GroupBy(set.Type).ToDictionary(k => k.Key, k => k.ToArray());
    }

    public int Count(CustomizationId id)
    {
        if (!IsAvailable(id))
            return 0;

        if (id.ToType() == CharaMakeParams.MenuType.Percentage)
            return 101;

        return id switch
        {
            CustomizationId.Face                  => Faces.Count,
            CustomizationId.Hairstyle             => HairStyles.Count,
            CustomizationId.HighlightsOnFlag      => 2,
            CustomizationId.SkinColor             => SkinColors.Count,
            CustomizationId.EyeColorR             => EyeColors.Count,
            CustomizationId.HairColor             => HairColors.Count,
            CustomizationId.HighlightColor        => HighlightColors.Count,
            CustomizationId.FacialFeaturesTattoos => 8,
            CustomizationId.TattooColor           => TattooColors.Count,
            CustomizationId.Eyebrows              => NumEyebrows,
            CustomizationId.EyeColorL             => EyeColors.Count,
            CustomizationId.EyeShape              => NumEyeShapes,
            CustomizationId.Nose                  => NumNoseShapes,
            CustomizationId.Jaw                   => NumJawShapes,
            CustomizationId.Mouth                 => NumMouthShapes,
            CustomizationId.LipColor              => LipColorsLight.Count + LipColorsDark.Count,
            CustomizationId.TailEarShape          => TailEarShapes.Count,
            CustomizationId.FacePaint             => FacePaints.Count,
            CustomizationId.FacePaintColor        => FacePaintColorsLight.Count + FacePaintColorsDark.Count,
            _                                     => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };
    }

    private const uint DefaultAvailable =
        (1u << (int)CustomizationId.Height)
      | (1u << (int)CustomizationId.Hairstyle)
      | (1u << (int)CustomizationId.SkinColor)
      | (1u << (int)CustomizationId.EyeColorR)
      | (1u << (int)CustomizationId.EyeColorL)
      | (1u << (int)CustomizationId.HairColor)
      | (1u << (int)CustomizationId.HighlightColor)
      | (1u << (int)CustomizationId.FacialFeaturesTattoos)
      | (1u << (int)CustomizationId.TattooColor)
      | (1u << (int)CustomizationId.LipColor)
      | (1u << (int)CustomizationId.Height);
}
