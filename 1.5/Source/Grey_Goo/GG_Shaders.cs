using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Grey_Goo;

[StaticConstructorOnStartup]
public static class GG_Shaders
{
    private static AssetBundle bundleInt;
    private static Dictionary<string, Shader> lookupShaders;

    public static readonly Shader LiquidMetal = LoadShader(Path.Combine("Assets", "Shaders", "LiquidMetal.shader"));
    public static readonly Shader LiquidMetalSimplex = LoadShader(Path.Combine("Assets", "Shaders", "LiquidMetalSimplex.shader"));
    public static readonly Shader LiquidMetalCracked = LoadShader(Path.Combine("Assets", "Shaders", "LiquidMetalCracked.shader"));

    public static AssetBundle AssetBundle
    {
        get
        {
            if (bundleInt != null)
            {
                return bundleInt;
            }

            bundleInt = Grey_GooMod.mod.MainBundle;
            ModLog.Debug($"bundleInt: {bundleInt.name}");

            return bundleInt;
        }
    }

    public static Shader LoadShader(string shaderName)
    {
        lookupShaders ??= new Dictionary<string, Shader>();
        if (!lookupShaders.ContainsKey(shaderName))
        {
            ModLog.Debug($"lookupShaders: {lookupShaders.ToList().Count}");
            lookupShaders[shaderName] = AssetBundle.LoadAsset<Shader>(shaderName);
        }

        Shader shader = lookupShaders[shaderName];
        if (shader == null)
        {
            ModLog.Warn($"Could not load shader: {shaderName}");
            return ShaderDatabase.DefaultShader;
        }

        if (shader != null)
        {
            ModLog.Debug($"Loaded shaders: {lookupShaders.Count}");
        }

        return shader;
    }
}
