using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class GGTerrainDef: TerrainDef
{
    public string texturePathFade;
    public string texturePathRough;
    public string texturePathWater;

    public ShaderTypeDef terrainShader;

    public string TexturePath
    {
        get
        {
            string path = edgeType switch
            {
                TerrainEdgeType.Hard => texturePath,
                TerrainEdgeType.Fade => texturePathFade,
                TerrainEdgeType.FadeRough => texturePathRough,
                TerrainEdgeType.Water => texturePathWater,
                _ => null
            };
            return path;
        }
    }

    public override void PostLoad()
    {
        LongEventHandler.ExecuteWhenFinished((Action) (() =>
        {
            if (graphic == BaseContent.BadGraphic)
            {
                Shader shader = Shader;
                graphic = GraphicDatabase.Get<Graphic_Terrain>(texturePath, terrainShader.Shader, Vector2.one, DrawColor, 2000 + renderPrecedence);
                if (shader == ShaderDatabase.TerrainFadeRough || shader == ShaderDatabase.TerrainWater)
                    graphic.MatSingle.SetTexture("_AlphaAddTex", TexGame.AlphaAddTex);
            }

            if (!uiIconPath.NullOrEmpty())
                uiIcon = ContentFinder<Texture2D>.Get(uiIconPath);
            else
                ResolveIcon();
            if (uiIconPathsStuff == null)
                return;
            stuffUiIcons = new Dictionary<StuffAppearanceDef, Texture2D>();
            foreach (IconForStuffAppearance forStuffAppearance in uiIconPathsStuff)
                stuffUiIcons.Add(forStuffAppearance.Appearance, ContentFinder<Texture2D>.Get(forStuffAppearance.IconPath));
        }));
    }
}
