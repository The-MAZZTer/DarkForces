Shader "Unlit/Plane"
{
	Properties
	{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
		_Parallax("Parallax", Vector) = (0, 0, 0, 0)
		_ScreenSize("_ScreenSize", Vector) = (1920, 1080, 0, 0)
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _Parallax;
			float4 _ScreenSize;

			float4 vert(float4 vertex : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}
			
			fixed4 frag(float4 sp : WPOS) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, sp.xy / _ScreenSize.xy + _Parallax.xy);
				return col;
			}
			ENDCG
		}
	}
}
