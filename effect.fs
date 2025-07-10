#version 330

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D texture0;
uniform vec2 mousePos;
uniform float radius;
uniform vec2 resolution;

void main() {
    vec2 uv = fragTexCoord;
    vec2 pixelPos = uv * resolution;
    
    // Distance du pixel au point de clic
    float dist = distance(pixelPos, mousePos);
    
    // Couleur originale
    vec4 originalColor = texture(texture0, uv);
    
    if (dist < radius) {
       fragColor = vec4(1.0-originalColor.rgb, originalColor.a);
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
