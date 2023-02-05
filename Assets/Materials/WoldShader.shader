// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Bumped shader. Differences from regular Bumped one:
// - no Main Color
// - Normalmap uses Tiling/Offset of the Base texture
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "World Shader" 
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _TextureScale("Texture Scale", float) = 10
        [NoScaleOffset] _BumpMap("Normalmap", 2D) = "bump" {}
    }

        SubShader
    {
            Tags { "RenderType" = "Opaque" }
            LOD 250

        CGPROGRAM
        #pragma surface surf Lambert noforwardadd

        sampler2D _MainTex;
        float _TextureScale;
        // sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        float my_fmod(float a, float b)
        {
            float c = frac(abs(a / b)) * abs(b);
            return c;
        }


        void surf(Input IN, inout SurfaceOutput o) 
        {
            float x = IN.worldPos.x * _TextureScale/2;
            float y = IN.worldPos.y * _TextureScale/2;
            float z = IN.worldPos.z * _TextureScale/2;

            float isUp = abs(IN.worldNormal.y);

            float2 offset = float2(my_fmod(z + x * (1 - isUp), 0.0625), my_fmod(y + x * isUp, 0.0625));

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex + offset);
            o.Albedo = c.rgb;
            o.Alpha = 1;
            // o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
        }
        ENDCG
    }

        FallBack "Mobile/Diffuse"
}