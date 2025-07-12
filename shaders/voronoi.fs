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

// Voronoi cell calculation
vec3 voronoi(vec2 uv) {
    vec2 g = floor(uv);
    vec2 f = fract(uv);
    
    float minDist1 = 1.0;
    float minDist2 = 1.0;
    vec2 minPoint;
    vec2 minCell;
    
    // Search neighboring cells
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 neighbor = vec2(float(x), float(y));
            vec2 cell = g + neighbor;
            
            // Animated point within cell
            vec2 point = neighbor + 0.5 + 0.3 * sin(time * 0.5 + 6.2831 * cell);
            vec2 diff = neighbor + point - f;
            float dist = length(diff);
            
            if (dist < minDist1) {
                minDist2 = minDist1;
                minDist1 = dist;
                minPoint = point;
                minCell = cell;
            } else if (dist < minDist2) {
                minDist2 = dist;
            }
        }
    }
    
    return vec3(minDist1, minDist2 - minDist1, minCell.x + minCell.y);
}

// Generate cell colors based on cell ID
vec3 cellColor(float cellId, float time) {
    float r = sin(cellId * 2.4 + time * 0.3) * 0.5 + 0.5;
    float g = sin(cellId * 3.7 + time * 0.4) * 0.5 + 0.5;
    float b = sin(cellId * 5.1 + time * 0.2) * 0.5 + 0.5;
    return vec3(r, g, b);
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Mouse interaction - affect the scale
    vec2 mouse = mousePos / resolution;
    float mouseInfluence = 0.0;
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        mouseInfluence = exp(-mouseDist * radius * 0.2) * power;
    }
    
    // Animated scale
    float scale = 8.0 + sin(time * 0.3) * 2.0 + mouseInfluence * 5.0;
    
    // Calculate Voronoi
    vec3 vor = voronoi(uv * scale + time * 0.1);
    float dist1 = vor.x;
    float edgeDistance = vor.y;
    float cellId = vor.z;
    
    // Cell colors
    vec3 color = cellColor(cellId, time);
    
    // Edge highlighting
    float edge = smoothstep(0.0, 0.1, edgeDistance);
    color = mix(vec3(1.0), color, edge);
    
    // Distance field effect
    float distanceField = 1.0 - smoothstep(0.0, 0.3, dist1);
    color += distanceField * vec3(0.2, 0.4, 0.8) * 0.5;
    
    // Add pulsing effect to cells
    float pulse = sin(cellId * 2.0 + time * 2.0) * 0.5 + 0.5;
    color *= 0.7 + pulse * 0.3;
    
    // Mouse glow effect
    if (mouseInfluence > 0.0) {
        color += mouseInfluence * vec3(1.0, 0.8, 0.2) * 0.5;
    }
    
    // Add some noise to break uniformity
    float noise = sin(uv.x * 100.0 + time) * sin(uv.y * 100.0 + time * 1.1) * 0.02;
    color += noise;
    
    // Contrast and brightness
    color = pow(color, vec3(0.8));
    color *= 1.1;
    
    fragColor = vec4(color, 1.0);
}
