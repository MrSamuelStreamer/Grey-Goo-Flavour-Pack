using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class GG_ShaderTypeDef : ShaderTypeDef
{
    public static Lazy<FieldInfo> ShaderIntFI = new Lazy<FieldInfo>(() => AccessTools.Field(typeof(ShaderTypeDef), "shaderInt"));

    public override void PostLoad()
    {
        base.PostLoad();

        LongEventHandler.ExecuteWhenFinished((Action) (() =>
        {
            Shader shader = GG_Shaders.LoadShader(shaderPath);
            ShaderIntFI.Value.SetValue(this, shader);
        }));
    }
}
