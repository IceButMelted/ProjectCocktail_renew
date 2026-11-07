// ============================================================
//  GarnishCategory.cs — Which of the 3 garnish groups an item
//  belongs to. Tags SO_GarnishItemOption for UI filtering only —
//  it does not restrict which of the 2 item slots an item can go
//  in, since Fresh and Novelty share both slots.
//
//  Rim garnishes are a separate type entirely (SO_GarnishRimOption,
//  one whole-rim treatment per glass, not a slot pick) so there is
//  no "Rim" member here.
// ============================================================

/// <summary>Fresh = Citrus Garnishes + Herbs &amp; Fresh Produce (one merged group, per design).</summary>
public enum GarnishCategory : byte
{
    Fresh,
    Novelty
}
