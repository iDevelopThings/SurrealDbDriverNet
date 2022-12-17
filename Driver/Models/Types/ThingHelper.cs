
namespace Driver.Models.Types;

public static class ThingExtensions {
    /// <summary>Creates a <see cref="Thing"/> from a table and key.</summary>
    public static Thing ToThing<T>(in this (string Table, T Key) thing) {
        return Thing.From(thing.Table, thing.Key);
    }
}


