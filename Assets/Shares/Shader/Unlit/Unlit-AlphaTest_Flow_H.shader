///////////////////////////////////////////////////////////////////////////////////////////
//作者：沙新佳 qq：287490904
//最后修改时间：2015/9/2
//脚本描述：基本色，cutout，实时阴影，模拟光，高光，x光,流光 。（临时提供给武器 和 坐骑）
//////////////////////////////////
// Unlit alpha-cutout shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Unlit/Transparent Cutout Flow_H" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	
	[HideInInspector]
	[Space(50)]_MatCap ("Xray Dec  MatCap (RGB)", 2D) = "white" {}

	//高级效果
	 _XrayColor("XrayColor", Color) = (1,1,1,0.5)
	 //_BumpMap ("Normalmap", 2D) = "bump" {}
	 _BRDFTex ("NdotL NdotH (RGB)", 2D) = "white" {}
	 _FakeProbeTopColor("Fake light probe top color", Color) = (0.4,0.4,0.4,1)
	_FakeProbeBotColor("Fake light probe bottom color", Color) = (0.1,0.1,0.1,1)
	_LightProbesLightingAmount("Light probes lighting amount (set 0)", Range(0,1)) = 0.0
	 _SpecularStrength("Specular strength weights", Vector) = (0.5,0.5,0.5,1)
	 
	 //滚动纹理
	 _DetailTex1 ("Blend_Texture_01(RGB)", 2D) = "white" {}
	 //滚动颜色
	 _OverlayColor ("Color (RGB)", Color) = (1,1,1,1)
	 //
	 _ColorMask("Color Mask (A)", 2D) = "white" {}
	//滚动速度
	 _Scroll1 ("Blend_Texture_x speed Scroll1", Float) = 1.0
	 _Scroll2 ("Blend_Texture_y speed Scroll2", Float) = 1.0
}
SubShader {
	//Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="TransparentCutout"} 
	LOD 100
	Lighting Off
	UsePass "Hidden/XRayBase/XRAY_VF_R"	//UsePass "Hidden/XRayBase/XRAY_VF_R"
	Pass { 
		Tags {"Queue"="AlphaTest-50" "IgnoreProjector"="True" "RenderType"="TransparentCutout"} 
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest		
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

//			struct appdata_t {
//				float4 vertex : POSITION;
//				float4 normal : NORMAL;
//				float2 texcoord : TEXCOORD0;
//				float3  texcoord1 : TEXCOORD1;
//			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				float3 normal : TEXCOORD5;
				fixed3 ldir	: TEXCOORD1;
				fixed3 hdir : TEXCOORD2;
				fixed4 color: COLOR;
				half2 texcoord3 : TEXCOORD3;
				UNITY_FOG_COORDS(4)
			};

			half3 GetDominantDirLightFromSH()
		 	{ 		
		 		half3 res = unity_SHAr.xyz * 0.3 + unity_SHAg.xyz * 0.59 + unity_SHAb.xyz * 0.11;
		 		
		 		normalize(res);
		 	
		 		return res;
		 	}

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed _Cutoff;
			fixed _Reflection;
			uniform fixed4 _ReflectColor;
			sampler2D _Spec_Gloss_Reflec_Masks;

			//sampler2D 		_BumpMap;
			sampler2D		_BRDFTex;
			float4 			_FakeProbeTopColor;
			float4 			_FakeProbeBotColor;
			float			_LightProbesLightingAmount;
			float4			_SpecularStrength;
			
			sampler2D _DetailTex1;
			sampler2D _ColorMask;
			float4 _DetailTex1_ST;
			float4 _OverlayColor;
			float _Scroll1;
			float _Scroll2;
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);  
				       		
				float3 worldNormal = UnityObjectToWorldNormal( v.normal );

				o.texcoord3 = TRANSFORM_TEX(v.texcoord.xy,_DetailTex1) + frac(float2(_Scroll1, _Scroll2) * _Time);

				TANGENT_SPACE_ROTATION;
				#if 0
				half3	ldir = mul((float3x3)_World2Object,GetDominantDirLightFromSH());
				o.ldir = normalize(mul(rotation, ldir));
				#else
				o.ldir = mul(rotation,ObjSpaceLightDir(v.vertex));
				#endif
				float3 vdir = normalize(mul(rotation,ObjSpaceViewDir(v.vertex)));
				o.hdir = normalize(vdir + o.ldir);
				float3 SHLighting	= ShadeSH9(float4(worldNormal,1));
				o.color =  lerp(saturate(SHLighting+(1 - _LightProbesLightingAmount)).xyzz,lerp(_FakeProbeBotColor.xyz,_FakeProbeTopColor.xyz,worldNormal.y*0.5+0.5).xyzz,step(_LightProbesLightingAmount,0.001f));

				o.normal = worldNormal;

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			samplerCUBE _Cube;

			half4 frag (v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.texcoord);
				
				 fixed3	normal	=i.normal*2.0-1.0; // tex2D(_MainTex, i.texcoord.xy).rgb * 2.0 - 1.0;
				 half4	gloss	= dot(_SpecularStrength,col);

				 col.xyz *= i.color.xyz;

				fixed	nl		= dot (normal, i.ldir);
				fixed	nh		= dot (normal, i.hdir);
				fixed4	l		= tex2D(_BRDFTex, fixed2(nl * 0.5 + 0.5, nh));
				 col.rgb *= (l.rgb + gloss * l.a)*2.0;
				 //2st flow
				half4 tex1 = tex2D (_DetailTex1, i.texcoord3);
				//蒙版
				half4 msk = tex2D(_ColorMask, i.texcoord);
				
				col.rgb =col.rgb + tex1.rgb*_OverlayColor.a*_OverlayColor.rgb*msk.a;
				
				clip(col.a - _Cutoff);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
		ENDCG
	}
	UsePass "Hidden/ShadowCasterBase/MV_SHADOWCASTER"
}
	Fallback  Off
}
