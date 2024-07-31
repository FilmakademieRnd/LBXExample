// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FairyGUI/BlurFilter" {
    Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
    Pass {
          ZTest Always 
        ZWrite Off

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        struct appdata_t {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
    
            UNITY_VERTEX_INPUT_INSTANCE_ID // insert
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            half2 texcoord : TEXCOORD0;
            half2 taps[4] : TEXCOORD1; 
    
            UNITY_VERTEX_OUTPUT_STEREO // insert
        };

        sampler2D _MainTex;
        half4 _MainTex_TexelSize;
        half4 _BlurOffsets;
        
        v2f vert (appdata_t v)
        {
            v2f o;
    
            UNITY_SETUP_INSTANCE_ID(v); //Insert
            UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
    
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.texcoord = v.texcoord - _BlurOffsets.xy * _MainTex_TexelSize.xy;
            o.taps[0] = o.texcoord + _MainTex_TexelSize * _BlurOffsets.xy;
            o.taps[1] = o.texcoord - _MainTex_TexelSize * _BlurOffsets.xy;
            o.taps[2] = o.texcoord + _MainTex_TexelSize * _BlurOffsets.xy * half2(1,-1);
            o.taps[3] = o.texcoord - _MainTex_TexelSize * _BlurOffsets.xy * half2(1,-1);
            return o;
        }
        
        fixed4 frag (v2f i) : SV_Target
        {
            half4 color = tex2D(_MainTex, i.taps[0]);
            color += tex2D(_MainTex, i.taps[1]);
            color += tex2D(_MainTex, i.taps[2]);
            color += tex2D(_MainTex, i.taps[3]); 
            return color * 0.25;
        }
    ENDCG
    }
}
Fallback off
}
