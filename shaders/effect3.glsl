#version 460

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D texture0;
uniform vec2 mousePos;
uniform float radius;
uniform float power;
uniform vec2 resolution;
uniform float time;
#define PI 3.1415926535897932384626433832795

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0)+cos(time);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(vec3(1.0), clamp(p - vec3(1.0), 0.0, 1.0), abs(cos(time))*c.y);
}


void main()
{
    vec2 uv = fragTexCoord;
    
    float hue = uv.x; // 0 à gauche, 1 à droite (équivalent à 0-360°)
    
    float saturation = uv.y; // Moins saturé en haut (0), plus saturé en bas (1)
    float value = 1.0; // Luminosité constante

    vec3 hsvColor = vec3(hue, (1+cos(time)/2)*saturation, value);
    vec3 rgbColor = hsv2rgb(hsvColor);
    
    fragColor = vec4(rgbColor, 1.0);
}