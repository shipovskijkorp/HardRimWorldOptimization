using Verse;

#nullable disable
namespace MyRimWorldMod;

public static class Flag
{
  public static bool VFEEmpire = ModLister.HasActiveModWithName("Vanilla Factions Expanded - Empire");
  public static bool VREArchon = ModLister.HasActiveModWithName("Vanilla Races Expanded - Archon");
  public static bool LTSTenant = ModLister.HasActiveModWithName("[LTS]Tenants");
}
