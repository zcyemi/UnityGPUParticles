// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/shaderParticles"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_tcol("Color",Color) = (1,0,0,1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent+500" }
		LOD 100
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			StructuredBuffer<float4> _cbufferPosition;
			StructuredBuffer<float4> _cbufferParams;



			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed4 _tcol;


			StructuredBuffer<float> _CBuffer;
			
			v2f vert (appdata v,uint instanceID : SV_InstanceID)
			{
				v2f o;
				float4 position =  _cbufferPosition[instanceID];
				float4 params = _cbufferParams[instanceID];

				float3 localpos = v.vertex.xyz * float3(params.xy,1);

				

				float4 pos = float4(localpos + position.xyz,1.0);
				float dist = (_Time.y - params.w) * params.z;
				
				float newp = pos.y - dist;

				if(newp < position.w){
					pos.y += 2000;
				}
				else{
					 pos.y -=dist;
				}
				

				o.vertex = mul(UNITY_MATRIX_VP,pos);
				

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{

				return _tcol;
				return 1;
			}
			ENDCG
		}
	}
}
