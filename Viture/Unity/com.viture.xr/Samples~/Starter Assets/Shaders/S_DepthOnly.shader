Shader "Viture/DepthOnly"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType" = "Transparent"}


        Pass
        {
            ZWrite On

            ColorMask 0
        }

    }
}
