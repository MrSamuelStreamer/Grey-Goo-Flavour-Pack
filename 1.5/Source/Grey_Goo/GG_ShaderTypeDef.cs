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
    }

    public new Shader Shader
    {
        get
        {
            if (ShaderIntFI.Value.GetValue(this) == null)
                ShaderIntFI.Value.SetValue(this, GG_Shaders.LoadShader(shaderPath));
            return (Shader)ShaderIntFI.Value.GetValue(this);
        }
    }
}
