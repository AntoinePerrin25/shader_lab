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


float angleBetween(vec2 a, vec2 b) {
    return atan(b.y - a.y, b.x - a.x);
}
vec4 gaussianBlur(sampler2D tex, vec2 uv, vec2 resolution, float radius) {
    float sigma = radius / 2.0;
    float twoSigmaSq = 2.0 * sigma * sigma;
    float blurRadius = ceil(radius);
    vec4 color = vec4(0.0);
    float total = 0.0;

    for (float x = -blurRadius; x <= blurRadius; x++) {
        for (float y = -blurRadius; y <= blurRadius; y++) {
            vec2 offset = vec2(x, y) / resolution;
            float weight = exp(-(x * x + y * y) / twoSigmaSq);
            color += texture(tex, uv + offset) * weight;
            total += weight;
        }
    }
    return color / total;
}
void main() {
    vec2 uv = fragTexCoord;
    vec2 pixelPos = uv * resolution;
    float dist = distance(pixelPos, mousePos);
    vec4 originalColor = texture(texture0, uv);
    float inner = radius * 0.2;
    float outer = radius;
    float center = radius * 0.6;


    if (dist < outer) {
        // Netteté globale à l'intérieur du cercle
        mat3 kernel = mat3(
            0, -1,  0,
           -1,  5, -1,
            0, -1,  0
        );
        vec2 texel = 1.0 / resolution;
        vec4 sharpColor = 
            texture(texture0, uv + texel * vec2(-1, -1)) * kernel[0][0] +
            texture(texture0, uv + texel * vec2( 0, -1)) * kernel[0][1] +
            texture(texture0, uv + texel * vec2( 1, -1)) * kernel[0][2] +
            texture(texture0, uv + texel * vec2(-1,  0)) * kernel[1][0] +
            texture(texture0, uv + texel * vec2( 0,  0)) * kernel[1][1] +
            texture(texture0, uv + texel * vec2( 1,  0)) * kernel[1][2] +
            texture(texture0, uv + texel * vec2(-1,  1)) * kernel[2][0] +
            texture(texture0, uv + texel * vec2( 0,  1)) * kernel[2][1] +
            texture(texture0, uv + texel * vec2( 1,  1)) * kernel[2][2];
        fragColor = sharpColor;
    }
    else if (dist <= min(radius*1.05, radius+5.0)) {
        fragColor = vec4(0.0, 0.0, 0.0, 1.0);
    } 
    else {
        fragColor = originalColor;
    }
}