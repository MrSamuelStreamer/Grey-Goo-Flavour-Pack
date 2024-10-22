using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Grey_Goo;

[StaticConstructorOnStartup]
public class BossBar_MapComponent(Map map) : MapComponent(map)
{
    public static Texture2D BossBarTex = ContentFinder<Texture2D>.Get("UI/MSS_GG_BossBar");
    public static Texture2D BossBar_BarTex = ContentFinder<Texture2D>.Get("UI/MSS_GG_BossBar_Bar");

    public static GUIStyle _fontStyle;
    public static GUIStyle fontStyle
    {
        get
        {
            if (_fontStyle == null)
            {
                _fontStyle = new GUIStyle();
                _fontStyle.font =  GG_Fonts.OptimusPrincepsSemiBold;
                _fontStyle.alignment = TextAnchor.MiddleLeft;
                _fontStyle.fontSize = 40;
                _fontStyle.richText = true;
            }
            return _fontStyle;
        }
    }

    public override void MapComponentOnGUI()
    {
        List<Pawn> bossBarPawns = map.mapPawns.AllPawns.Where(pawn => !pawn.Dead && pawn.health.hediffSet.HasHediff(Grey_GooDefOf.MSS_GG_BossBar)).ToList();

        int barWidth = Mathf.CeilToInt(UI.screenWidth * 0.6f);
        int barStartX = Mathf.CeilToInt((UI.screenWidth - barWidth)/2f);

        int barHeight = Mathf.CeilToInt(UI.screenHeight * 0.0138f);
        int barStartY = Mathf.CeilToInt(UI.screenHeight * 0.75f);

        foreach (Pawn pawn in bossBarPawns)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs.Where(h=>h.def == Grey_GooDefOf.MSS_GG_BossBar).ToList();

            List<Hediff> partHediffs = hediffs.Where(h => h.Part != null).ToList();

            List<Hediff> nonPartHediffs = hediffs.Except(partHediffs).ToList();

            // foreach (Hediff partHediff in partHediffs)
            // {
            //     float healthAsFloat = pawn.health.RestorePart();
            //     Rect healthRect = new Rect(barStartX, barStartY, Mathf.CeilToInt(barWidth*healthAsFloat), barHeight);
            //     Rect barRect = new Rect(barStartX, barStartY, barWidth, barHeight);
            //     Rect nameRect = new Rect(barStartX, barStartY-40, barWidth, barHeight);
            //
            //     GUI.DrawTexture(healthRect, BossBar_BarTex, ScaleMode.StretchToFill);
            //     GUI.DrawTexture(barRect, BossBarTex, ScaleMode.StretchToFill);
            //     GUI.Label(nameRect, $"<color=#ffffff>{pawn.Name.ToStringFull}</color>" , fontStyle);
            //
            //     barStartY -= 80;
            // }

            if (nonPartHediffs.Any())
            {
                float healthAsFloat = pawn.health.summaryHealth.SummaryHealthPercent;
                Rect healthRect = new Rect(barStartX, barStartY, Mathf.CeilToInt(barWidth*healthAsFloat), barHeight);
                Rect barRect = new Rect(barStartX, barStartY, barWidth, barHeight);
                Rect nameRect = new Rect(barStartX, barStartY-40, barWidth, barHeight);

                GUI.DrawTexture(healthRect, BossBar_BarTex, ScaleMode.StretchToFill);
                GUI.DrawTexture(barRect, BossBarTex, ScaleMode.StretchToFill);
                GUI.Label(nameRect, $"<color=#ffffff>{pawn.Name.ToStringFull}</color>" , fontStyle);

                barStartY -= 80;
            }
        }
    }
}
