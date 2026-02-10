using Verse;

namespace MyRimWorldMod
{
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        static ModInit()
        {
            Log.Message("[MyRimWorldMod] Mod loaded.");
        }
    }
}
