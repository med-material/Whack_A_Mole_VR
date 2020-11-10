  Shader "Custom/EyePatch" {
    Properties {
      _Cube ("Cubemap", CUBE) = "" {}
      _FogMaxHeight ("Fog Max Height", Float) = 0.0
      _FogMinHeight ("Fog Min Height", Float) = -1.0
      _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
    }
    SubShader {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent" } 
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert alpha:fade
        struct Input {
            float2 uv_MainTex;
            float3 worldRefl;
            float3 viewDir;
            float4 pos;
        };
        sampler2D _MainTex;
        samplerCUBE _Cube;
        float _FogMaxHeight;
        float _FogMinHeight;
        float4 _FogColor;

        void vert (inout appdata_full v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            // The shader operates in local space at the moment, but change to world space using the below:
            //o.pos = (unity_ObjectToWorld, v.vertex);
            o.pos = v.vertex;    
        }

        void surf (Input IN, inout SurfaceOutput o) {
            o.Albedo = 0;
            o.Emission = texCUBE (_Cube, -IN.viewDir).rgb;
            float lerpValue = clamp((IN.pos.x - _FogMinHeight) / (_FogMaxHeight - _FogMinHeight), 0, 1);
            o.Alpha = lerp (0, 1, lerpValue);
        }
        
        ENDCG
    } 
    Fallback "Diffuse"
  }