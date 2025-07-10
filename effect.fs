#version 460

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D texture0;
uniform vec2 mousePos;
uniform float radius;
uniform float power;
uniform vec2 resolution;
uniform float time;

void main() {
    vec2 uv = fragTexCoord;
    vec2 pixelPos = uv * resolution;
    
    // Distance du pixel au point de clic
    float dist = distance(pixelPos, mousePos);
    
    // Couleur originale
    vec4 originalColor = texture(texture0, uv);
    
    if (dist < radius) {
       // Animation de couleur basée sur le temps et la distance
       float animatedBlue = 0.5 + 0.3 * cos(time * 2.0 + dist * 0.1);
       fragColor = vec4(0.6 - originalColor.rg, animatedBlue, originalColor.a);
    }
    else if (dist <= min(radius*1.05, radius+5.0))
    {
        fragColor = vec4(0.0, 0.0, 0.0, 1.0); // Pixel affecté devient noir
    } 
    else {
        // Pixel non affecté
        fragColor = originalColor;
    }
}
