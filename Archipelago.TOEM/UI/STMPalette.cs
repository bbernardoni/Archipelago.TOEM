
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Colors;

namespace Archipelago.TOEM.UI;

public static class STMPalette
{
    public const string White = "<c=white>";
    public const string Black = "<c=black>";
    public const string Red = "<c=red>";
    public const string Green = "<c=green>";
    public const string Blue = "<c=blue>";
    public const string Cyan = "<c=00EEEE>";
    public const string Magenta = "<c=DE0A4F>";
    public const string Yellow  = "<c=yellow>";
    public const string SlateBlue = "<c=6D8BE8>";
    public const string Salmon = "<c=FA8072>";
    public const string Plum = "<c=AF99EF>";
    public const string Rainbow = "<c=rainbow>";
    public const string End = "</c>";
    
    public static readonly Palette<string> Default = new("", new Dictionary<PaletteColor, string>
    {
        [PaletteColor.White] = White,
        [PaletteColor.Black] = Black,
        [PaletteColor.Red] = Red,
        [PaletteColor.Green] = Green,
        [PaletteColor.Blue] = Blue,
        [PaletteColor.Cyan] = Cyan,
        [PaletteColor.Magenta] = Magenta,
        [PaletteColor.Yellow] = Yellow,
        [PaletteColor.SlateBlue] = SlateBlue,
        [PaletteColor.Salmon] = Salmon,
        [PaletteColor.Plum] = Plum
    });
}