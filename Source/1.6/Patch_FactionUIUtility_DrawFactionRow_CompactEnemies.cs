using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace MyRimWorldMod
{
    [HarmonyPatch(typeof(FactionUIUtility), "DrawFactionRow")]
    internal static class Patch_FactionUIUtility_DrawFactionRow_CompactEnemies
    {
        private const float RowHeight = 80f;
        private const float BaseIconSize = 18f;
        private const float BaseStepY = 19f;
        private const float TopPadding = 1f;
        private const float MinMargin = 2f;
        private const float FirstOffsetX = 17f;
        private const float IconGapX = 4f;

        private static readonly FieldInfo ShowAllField =
            AccessTools.Field(typeof(FactionUIUtility), "showAll");

        private static readonly MethodInfo GetOngoingEventsMI =
            AccessTools.Method(typeof(FactionUIUtility), "GetOngoingEvents", new[] { typeof(Faction) });

        private static readonly MethodInfo GetRecentEventsMI =
            AccessTools.Method(typeof(FactionUIUtility), "GetRecentEvents", new[] { typeof(Faction) });

        private static readonly MethodInfo GetRelationKindForGoodwillMI =
            AccessTools.Method(typeof(FactionUIUtility), "GetRelationKindForGoodwill", new[] { typeof(int) });

        private static readonly MethodInfo GetNaturalGoodwillExplanationMI =
            AccessTools.Method(typeof(FactionUIUtility), "GetNaturalGoodwillExplanation", new[] { typeof(Faction) });

        private static readonly MethodInfo DrawFactionIconWithTooltipMI =
            AccessTools.Method(typeof(FactionUIUtility), "DrawFactionIconWithTooltip", new[] { typeof(Rect), typeof(Faction) });

        public static bool Prefix(Faction faction, float rowY, Rect fillRect, ref float __result)
        {
            var settings = HardRimWorldOptimizationMod.Settings;
            if (settings == null || !settings.compactEnemyIconsInFactionRow)
                return true;

            // if something critical is missing -> do vanilla to avoid breaking the UI
            if (DrawFactionIconWithTooltipMI == null)
                return true;

            try
            {
                bool showAll = false;
                if (ShowAllField != null && ShowAllField.GetValue(null) is bool b)
                    showAll = b;

                // This number is basically "free horizontal space" after vanilla columns.
                // It's a bit magic, but it matches the original mod's intent.
                float extraW = fillRect.width - 300f - 40f - 70f - 54f - 16f - 120f;

                // hostiles to this faction (in practice: enemies shown in row)
                Faction[] hostiles = Find.FactionManager.AllFactionsInViewOrder
                    .Where(f =>
                        f != faction &&
                        FactionUtility.HostileTo(f, faction) &&
                        (f.IsPlayer || !f.Hidden || showAll))
                    .ToArray();

                // --- Left main info block (icon + name/desc) ---
                var infoRect = new Rect(90f, rowY, 300f, RowHeight);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                var factionIconRect = new Rect(24f, rowY + (infoRect.height - 42f) / 2f, 42f, 42f);
                GUI.color = faction.Color;
                GUI.DrawTexture(factionIconRect, faction.def.FactionIcon);
                GUI.color = Color.white;

                string leaderLine = faction.leader != null
                    ? $"{GenText.CapitalizeFirst(faction.LeaderTitle)}: {faction.leader.Name.ToStringFull}"
                    : "";

                string label =
                    GenText.CapitalizeFirst(faction.Name) + "\n" +
                    faction.def.LabelCap + "\n" +
                    leaderLine;

                Widgets.Label(infoRect, label);

                var clickRect = new Rect(0f, rowY, infoRect.xMax, RowHeight);
                if (Mouse.IsOver(clickRect))
                {
                    TooltipHandler.TipRegion(clickRect, new TipSignal(() =>
                        ColoredText.Colorize(faction.Name, ColoredText.TipSectionTitleColor)
                        + "\n" + faction.def.LabelCap.Resolve()
                        + "\n\n" + faction.def.Description,
                        faction.loadID ^ 1938473043));

                    Widgets.DrawHighlight(clickRect);
                }

                if (Widgets.ButtonInvisible(clickRect))
                    Find.WindowStack.Add(new Dialog_InfoCard(faction));

                // --- Info card button ---
                var infoBtnRect = new Rect(infoRect.xMax, rowY, 40f, RowHeight);
                Widgets.InfoCardButtonCentered(infoBtnRect, faction);

                // --- Relationship / goodwill area (mostly vanilla) ---
                var relRect = new Rect(infoBtnRect.xMax, rowY, 70f, RowHeight);

                if (!faction.IsPlayer)
                {
                    string relLabel = FactionRelationKindUtility.GetLabelCap(faction.PlayerRelationKind);
                    if (faction.defeated)
                        relLabel = ColoredText.Colorize(relLabel, ColorLibrary.Grey);

                    GUI.color = FactionRelationKindUtility.GetColor(faction.PlayerRelationKind);
                    Text.Anchor = TextAnchor.MiddleCenter;

                    if (faction.HasGoodwill && !faction.def.permanentEnemy)
                    {
                        Widgets.Label(new Rect(relRect.x, relRect.y - 10f, relRect.width, relRect.height), relLabel);
                        Text.Font = GameFont.Tiny;
                        Widgets.Label(new Rect(relRect.x, relRect.y + 10f, relRect.width, relRect.height), GenText.ToStringWithSign(faction.PlayerGoodwill));
                        Text.Font = GameFont.Small;
                    }
                    else
                    {
                        Widgets.Label(relRect, relLabel);
                    }

                    GenUI.ResetLabelAlign();
                    GUI.color = Color.white;

                    if (Mouse.IsOver(relRect))
                    {
                        TaggedString tip = "";
                        if (faction.def.permanentEnemy)
                        {
                            tip = "CurrentGoodwillTip_PermanentEnemy".Translate();
                        }
                        else if (faction.HasGoodwill)
                        {
                            TaggedString baseTip =
                                $"{ColoredText.Colorize("Goodwill".Translate(), ColoredText.TipSectionTitleColor)}: " +
                                $"{ColoredText.Colorize($"{GenText.ToStringWithSign(faction.PlayerGoodwill)}, {FactionRelationKindUtility.GetLabel(faction.PlayerRelationKind)}",
                                FactionRelationKindUtility.GetColor(faction.PlayerRelationKind))}";

                            TaggedString ongoing = TaggedString.Empty;
                            if (GetOngoingEventsMI != null)
                                ongoing = (TaggedString)GetOngoingEventsMI.Invoke(null, new object[] { faction });

                            if (!ongoing.NullOrEmpty())
                                baseTip += $"\n\n{ColoredText.Colorize("OngoingEvents".Translate(), ColoredText.TipSectionTitleColor)}:\n" + ongoing;

                            TaggedString recent = TaggedString.Empty;
                            if (GetRecentEventsMI != null)
                                recent = (TaggedString)GetRecentEventsMI.Invoke(null, new object[] { faction });

                            if (!recent.NullOrEmpty())
                                baseTip += $"\n\n{ColoredText.Colorize("RecentEvents".Translate(), ColoredText.TipSectionTitleColor)}:\n" + recent;

                            tip = baseTip;
                        }

                        if (!tip.NullOrEmpty())
                            TooltipHandler.TipRegion(relRect, (TipSignal)tip);

                        Widgets.DrawHighlight(relRect);
                    }
                }

                // --- Natural goodwill mini box (optional, vanilla-ish) ---
                var naturalRect = new Rect(relRect.xMax, rowY, 54f, RowHeight);

                if (!faction.IsPlayer && faction.HasGoodwill && !faction.def.permanentEnemy && GetRelationKindForGoodwillMI != null)
                {
                    var natKind = (FactionRelationKind)GetRelationKindForGoodwillMI.Invoke(null, new object[] { faction.NaturalGoodwill });
                    GUI.color = FactionRelationKindUtility.GetColor(natKind);

                    Rect box = GenUI.ContractedBy(naturalRect, 7f);
                    box.y = rowY + 30f;
                    box.height = 20f;

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.DrawRectFast(box, Color.black);
                    Widgets.Label(box, GenText.ToStringWithSign(faction.NaturalGoodwill));
                    GenUI.ResetLabelAlign();
                    GUI.color = Color.white;

                    if (Mouse.IsOver(naturalRect))
                    {
                        TaggedString expl = TaggedString.Empty;
                        if (GetNaturalGoodwillExplanationMI != null)
                            expl = (TaggedString)GetNaturalGoodwillExplanationMI.Invoke(null, new object[] { faction });

                        TaggedString tip =
                            $"{ColoredText.Colorize("NaturalGoodwill".Translate(), ColoredText.TipSectionTitleColor)}: " +
                            $"{ColoredText.Colorize(GenText.ToStringWithSign(faction.NaturalGoodwill), FactionRelationKindUtility.GetColor(natKind))}";

                        if (!expl.NullOrEmpty())
                            tip += $"\n\n{ColoredText.Colorize("AffectedBy".Translate(), ColoredText.TipSectionTitleColor)}\n" + expl;

                        TooltipHandler.TipRegion(naturalRect, (TipSignal)tip);
                        Widgets.DrawHighlight(naturalRect);
                    }
                }

                // --- Compact hostile icons grid ---
                float xStart = naturalRect.xMax;
                float xRight = naturalRect.xMax + extraW;

                float iconSize = BaseIconSize;
                float stepY = BaseStepY;
                float stepX = iconSize + IconGapX;

                float availableW = Mathf.Max(iconSize, xRight - xStart - FirstOffsetX - MinMargin);
                int perRow = Mathf.Max(1, Mathf.FloorToInt((availableW + IconGapX) / (iconSize + IconGapX)));
                int rows = hostiles.Length > 0 ? (hostiles.Length + perRow - 1) / perRow : 0;

                int maxRowsNoScale = Mathf.Clamp(settings.compactEnemyIconsMaxRowsWithoutScaling, 1, 100);

                if (rows > maxRowsNoScale && rows > 0)
                {
                    // scale down so it fits into row height
                    float scale = Mathf.Clamp(78f / (iconSize + (rows - 1) * stepY), 0.5f, 1f);
                    iconSize *= scale;
                    stepY *= scale;
                    stepX = iconSize + IconGapX;

                    perRow = Mathf.Max(1, Mathf.FloorToInt((availableW - iconSize) / stepX));
                    rows = hostiles.Length > 0 ? (hostiles.Length + perRow - 1) / perRow : 0;
                }

                float centerPad = Mathf.Max(1f, (RowHeight - (rows == 0 ? 0f : (iconSize + (rows - 1) * stepY))) / 2f);
                float yStart = rowY + Mathf.Max(centerPad, TopPadding);

                int row = 0;
                int col = 0;

                for (int i = 0; i < hostiles.Length; i++)
                {
                    float x = xStart + FirstOffsetX + col * stepX;
                    float y = yStart + row * stepY;

                    DrawFactionIconWithTooltipMI.Invoke(null, new object[]
                    {
                        new Rect(x, y, iconSize, iconSize),
                        hostiles[i]
                    });

                    col++;
                    if (col >= perRow)
                    {
                        col = 0;
                        row++;
                    }
                }

                Text.Anchor = TextAnchor.UpperLeft;
                __result = RowHeight;
                return false;
            }
            catch (Exception e)
            {
                if (HardRimWorldOptimizationMod.Settings != null &&
                    HardRimWorldOptimizationMod.Settings.compactEnemyIconsVerboseLogging)
                {
                    Log.Warning("[HardRimWorldOptimization] Faction row compact patch failed, falling back to vanilla.\n" + e);
                }
                return true; // fallback to vanilla if anything went wrong
            }
        }
    }
}
