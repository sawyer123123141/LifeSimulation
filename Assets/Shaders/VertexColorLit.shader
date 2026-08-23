// Lit shading driven by per-vertex colour.
//
// Unity's Standard shader ignores mesh.colors, so the icosphere planet - which carries its biome
// colour per vertex rather than in a texture - renders white under it. This is the smallest shader
// that keeps real lighting while reading that colour.
//
// Per-vertex rather than a texture because an icosphere has no natural UV mapping: any
// equirectangular projection onto it reintroduces the seam and the polar stretch that moving off the
// lat/lon sphere was meant to remove.
Shader "LifeSimulation/VertexColorLit"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.05
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float4 color : COLOR;
        };

        half _Glossiness;
        half _Metallic;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
