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

// Smooth noise function
float noise(vec2 p) {
    return sin(p.x * 10.0 + time * 0.5) * sin(p.y * 10.0 + time * 0.7) * 0.5 + 0.5;
}

// Polar coordinates conversion
vec2 toPolar(vec2 uv) {
    vec2 centered = uv - 0.5;
    float r = length(centered);
    float theta = atan(centered.y, centered.x);
    return vec2(r, theta);
}

// Create symmetrical pattern with n-fold symmetry
float nFoldSymmetry(float angle, int n) {
    float segmentAngle = 2.0 * PI / float(n);
    float normalizedAngle = mod(angle + PI, segmentAngle) - segmentAngle * 0.5;
    return abs(normalizedAngle);
}

// Lotus petal shape
float lotusPetal(vec2 polar, float petalSize, float phase) {
    float r = polar.x;
    float theta = polar.y + phase;
    
    // Create petal shape using sine waves
    float petalShape = sin(theta * 8.0) * 0.3 + 0.7;
    petalShape *= sin(theta * 4.0) * 0.1 + 0.9;
    
    // Radial fade
    float radialFade = smoothstep(petalSize * 1.2, petalSize * 0.8, r);
    
    return petalShape * radialFade;
}

// Sacred geometry patterns
float sacredGeometry(vec2 uv, float time) {
    vec2 polar = toPolar(uv);
    float r = polar.x;
    float theta = polar.y;
    
    float pattern = 0.0;
    
    // Flower of life pattern
    for (int i = 0; i < 6; i++) {
        float angle = float(i) * PI / 3.0 + time * 0.2;
        vec2 center = vec2(cos(angle), sin(angle)) * 0.15 + 0.5;
        float dist = distance(uv, center);
        pattern += sin(dist * 20.0 - time * 2.0) * exp(-dist * 8.0) * 0.5;
    }
    
    // Concentric circles
    for (int i = 1; i <= 8; i++) {
        float ringRadius = float(i) * 0.06;
        float ringPattern = sin((r - ringRadius) * 100.0) * exp(-abs(r - ringRadius) * 20.0);
        pattern += ringPattern * 0.3;
    }
    
    // Radial symmetry lines
    for (int i = 0; i < 12; i++) {
        float lineAngle = float(i) * PI / 6.0;
        float angleDiff = abs(theta - lineAngle);
        angleDiff = min(angleDiff, abs(angleDiff - 2.0 * PI));
        pattern += exp(-angleDiff * 50.0) * (1.0 - r) * 0.4;
    }
    
    return pattern;
}

// Chakra color wheel
vec3 chakraColors(float angle) {
    angle = mod(angle + PI, 2.0 * PI) / (2.0 * PI);
    
    if (angle < 1.0/7.0) return mix(vec3(1.0, 0.0, 0.0), vec3(1.0, 0.5, 0.0), angle * 7.0); // Root to Sacral
    if (angle < 2.0/7.0) return mix(vec3(1.0, 0.5, 0.0), vec3(1.0, 1.0, 0.0), (angle - 1.0/7.0) * 7.0); // Sacral to Solar
    if (angle < 3.0/7.0) return mix(vec3(1.0, 1.0, 0.0), vec3(0.0, 1.0, 0.0), (angle - 2.0/7.0) * 7.0); // Solar to Heart
    if (angle < 4.0/7.0) return mix(vec3(0.0, 1.0, 0.0), vec3(0.0, 1.0, 1.0), (angle - 3.0/7.0) * 7.0); // Heart to Throat
    if (angle < 5.0/7.0) return mix(vec3(0.0, 1.0, 1.0), vec3(0.0, 0.0, 1.0), (angle - 4.0/7.0) * 7.0); // Throat to Third Eye
    if (angle < 6.0/7.0) return mix(vec3(0.0, 0.0, 1.0), vec3(0.5, 0.0, 1.0), (angle - 5.0/7.0) * 7.0); // Third Eye to Crown
    return mix(vec3(0.5, 0.0, 1.0), vec3(1.0, 0.0, 0.0), (angle - 6.0/7.0) * 7.0); // Crown to Root
}

// Breathing animation
float breathingPulse(float time) {
    return sin(time * 0.8) * 0.5 + 0.5;
}

// Mandala layers
float mandalaLayer(vec2 uv, int symmetry, float radius, float detail, float phase) {
    vec2 polar = toPolar(uv);
    float r = polar.x;
    float theta = polar.y;
    
    // Apply symmetry
    float symTheta = nFoldSymmetry(theta + phase, symmetry);
    
    // Create pattern
    float pattern = sin(symTheta * detail * 10.0) * sin(r * detail * 20.0);
    pattern *= smoothstep(radius + 0.1, radius - 0.05, r);
    pattern *= smoothstep(0.0, 0.05, r);
    
    return pattern * 0.5 + 0.5;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 polar = toPolar(uv);
    float r = polar.x;
    float theta = polar.y;
    
    // Mouse interaction affects the mandala
    vec2 mouse = mousePos / resolution;
    float mouseInfluence = 0.0;
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        mouseInfluence = exp(-mouseDist * radius * 0.5) * power;
    }
    
    // Breathing animation
    float breathPhase = breathingPulse(time);
    float scale = 1.0 + breathPhase * 0.1 + mouseInfluence * 0.2;
    uv = (uv - 0.5) / scale + 0.5;
    polar = toPolar(uv);
    r = polar.x;
    theta = polar.y;
    
    // Initialize color
    vec3 color = vec3(0.0);
    
    // Background with subtle gradient
    vec3 bgColor = mix(vec3(0.05, 0.02, 0.1), vec3(0.1, 0.05, 0.15), r);
    color = bgColor;
    
    // Sacred geometry base
    float geometry = sacredGeometry(uv, time);
    
    // Multiple mandala layers with different symmetries
    float layer1 = mandalaLayer(uv, 8, 0.4, 1.0 + sin(time * 0.3) * 0.2, time * 0.1);
    float layer2 = mandalaLayer(uv, 12, 0.3, 1.5 + cos(time * 0.4) * 0.3, -time * 0.15);
    float layer3 = mandalaLayer(uv, 6, 0.25, 2.0 + sin(time * 0.2) * 0.4, time * 0.08);
    float layer4 = mandalaLayer(uv, 16, 0.15, 3.0 + cos(time * 0.5) * 0.5, -time * 0.12);
    
    // Combine layers
    float totalPattern = geometry * 0.3 + layer1 * 0.4 + layer2 * 0.3 + layer3 * 0.2 + layer4 * 0.1;
    
    // Lotus petals
    float lotusPattern = 0.0;
    for (int i = 0; i < 8; i++) {
        float petalPhase = float(i) * PI / 4.0 + time * 0.1;
        lotusPattern += lotusPetal(polar, 0.35, petalPhase) * 0.1;
    }
    totalPattern += lotusPattern;
    
    // Apply chakra colors based on angle and radius
    vec3 chakraColor = chakraColors(theta + time * 0.1);
    
    // Radial color variation
    vec3 innerColor = vec3(1.0, 0.8, 0.2); // Golden center
    vec3 outerColor = chakraColor;
    vec3 baseColor = mix(innerColor, outerColor, smoothstep(0.0, 0.5, r));
    
    // Apply pattern intensity
    color = mix(color, baseColor, totalPattern * (0.7 + breathPhase * 0.3));
    
    // Add energy lines
    float energyLines = 0.0;
    for (int i = 0; i < 24; i++) {
        float lineAngle = float(i) * PI / 12.0 + time * 0.05;
        float angleDiff = abs(theta - lineAngle);
        angleDiff = min(angleDiff, abs(angleDiff - 2.0 * PI));
        energyLines += exp(-angleDiff * 80.0) * sin(r * 30.0 - time * 3.0) * 0.1;
    }
    color += energyLines * chakraColors(theta + time * 0.2) * 0.5;
    
    // Central eye/dot
    float centerDot = exp(-r * 20.0);
    color += centerDot * vec3(1.0, 1.0, 1.0) * (0.8 + breathPhase * 0.4);
    
    // Om symbol area (simplified as a glow)
    if (r < 0.08) {
        float omGlow = (0.08 - r) / 0.08;
        color += omGlow * vec3(1.0, 0.9, 0.3) * 0.6;
    }
    
    // Mouse interaction glow
    if (mouseInfluence > 0.0) {
        vec3 mouseColor = chakraColors(atan(mouse.y - 0.5, mouse.x - 0.5));
        color += mouseInfluence * mouseColor * 0.8;
    }
    
    // Meditation particles
    float particles = 0.0;
    for (int i = 0; i < 15; i++) {
        float particleAngle = float(i) * 2.0 * PI / 15.0 + time * 0.3;
        float particleR = 0.3 + sin(time * 0.5 + float(i)) * 0.1;
        vec2 particlePos = vec2(cos(particleAngle), sin(particleAngle)) * particleR + 0.5;
        
        float particleDist = distance(uv, particlePos);
        particles += exp(-particleDist * 100.0) * (sin(time * 2.0 + float(i)) * 0.5 + 0.5);
    }
    color += particles * vec3(1.0, 0.8, 0.9) * 0.3;
    
    // Soft glow around entire mandala
    float mandalaGlow = exp(-r * 1.5) * 0.2;
    color += mandalaGlow * chakraColors(time * 0.1);
    
    // Final adjustments
    color = pow(color, vec3(0.8)); // Gamma correction
    color *= 1.1; // Brightness boost
    
    // Add subtle noise for texture
    float noiseValue = noise(uv * 20.0) * 0.02;
    color += noiseValue;
    
    fragColor = vec4(color, 1.0);
}
