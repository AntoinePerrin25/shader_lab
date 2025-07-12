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

// Distance to line segment
float distanceToLine(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

// DNA helix strand
float dnaStrand(vec2 uv, float helixRadius, float phase, float twist) {
    float y = uv.y;
    float helixPhase = y * twist + phase + time * 2.0;
    
    vec2 helixCenter = vec2(0.5 + cos(helixPhase) * helixRadius, y);
    float dist = distance(uv, helixCenter);
    
    return 1.0 - smoothstep(0.0, 0.02, dist);
}

// DNA base pairs (connecting bars)
float basePairs(vec2 uv, float helixRadius, float twist) {
    float y = uv.y;
    float spacing = 0.08; // Distance between base pairs
    float pairY = floor(y / spacing) * spacing + spacing * 0.5;
    
    if (abs(y - pairY) < 0.01) {
        float helixPhase = pairY * twist + time * 2.0;
        
        vec2 strand1 = vec2(0.5 + cos(helixPhase) * helixRadius, pairY);
        vec2 strand2 = vec2(0.5 + cos(helixPhase + PI) * helixRadius, pairY);
        
        float dist = distanceToLine(uv, strand1, strand2);
        return 1.0 - smoothstep(0.0, 0.008, dist);
    }
    
    return 0.0;
}

// Nucleotide bases (A, T, G, C)
float nucleotides(vec2 uv, float helixRadius, float twist) {
    float y = uv.y;
    float spacing = 0.08;
    float pairY = floor(y / spacing) * spacing + spacing * 0.5;
    
    if (abs(y - pairY) < 0.02) {
        float helixPhase = pairY * twist + time * 2.0;
        
        vec2 strand1 = vec2(0.5 + cos(helixPhase) * helixRadius, pairY);
        vec2 strand2 = vec2(0.5 + cos(helixPhase + PI) * helixRadius, pairY);
        
        // Nucleotide positions
        vec2 base1 = mix(strand1, strand2, 0.25);
        vec2 base2 = mix(strand1, strand2, 0.75);
        
        float dist1 = distance(uv, base1);
        float dist2 = distance(uv, base2);
        
        float nucleotide1 = 1.0 - smoothstep(0.0, 0.015, dist1);
        float nucleotide2 = 1.0 - smoothstep(0.0, 0.015, dist2);
        
        return max(nucleotide1, nucleotide2);
    }
    
    return 0.0;
}

// Particle effects around DNA
float particles(vec2 uv) {
    float particles = 0.0;
    
    for (int i = 0; i < 20; i++) {
        float seed = float(i);
        vec2 particlePos = vec2(
            sin(time * 0.5 + seed * 2.0) * 0.3 + 0.5,
            mod(time * 0.2 + seed, 1.0)
        );
        
        float dist = distance(uv, particlePos);
        float particle = exp(-dist * 100.0) * (sin(time * 3.0 + seed * 10.0) * 0.5 + 0.5);
        particles += particle;
    }
    
    return particles;
}

// Electric field visualization
float electricField(vec2 uv, float helixRadius, float twist) {
    float field = 0.0;
    float y = uv.y;
    
    for (float i = 0.0; i <= 1.0; i += 0.1) {
        float helixPhase = i * twist + time * 2.0;
        vec2 chargePos = vec2(0.5 + cos(helixPhase) * helixRadius, i);
        
        float dist = distance(uv, chargePos);
        field += 1.0 / (1.0 + dist * 20.0);
    }
    
    return field;
}

// DNA color scheme
vec3 getDNAColor(float strand, float bases, float nucleotides, float particles, float field, vec2 uv) {
    vec3 color = vec3(0.0);
    
    // Background (cellular environment)
    vec3 background = vec3(0.05, 0.1, 0.15) * (1.0 + sin(uv.x * 10.0 + time) * 0.1);
    color = background;
    
    // DNA strands (phosphate backbone)
    if (strand > 0.0) {
        vec3 strandColor = vec3(0.2, 0.6, 1.0); // Blue phosphate groups
        color = mix(color, strandColor, strand);
    }
    
    // Base pairs
    if (bases > 0.0) {
        vec3 baseColor = vec3(0.8, 0.8, 0.2); // Yellow-green base pairs
        color = mix(color, baseColor, bases);
    }
    
    // Nucleotides (A, T, G, C)
    if (nucleotides > 0.0) {
        // Different colors for different bases
        float baseType = sin(uv.y * 50.0 + time * 5.0) * 0.5 + 0.5;
        vec3 nucleotideColor;
        
        if (baseType < 0.25) {
            nucleotideColor = vec3(1.0, 0.2, 0.2); // Adenine - Red
        } else if (baseType < 0.5) {
            nucleotideColor = vec3(0.2, 1.0, 0.2); // Thymine - Green
        } else if (baseType < 0.75) {
            nucleotideColor = vec3(1.0, 1.0, 0.2); // Guanine - Yellow
        } else {
            nucleotideColor = vec3(0.2, 0.2, 1.0); // Cytosine - Blue
        }
        
        color = mix(color, nucleotideColor, nucleotides);
    }
    
    // Particles (proteins and enzymes)
    color += particles * vec3(0.8, 0.4, 1.0) * 0.5;
    
    // Electric field
    color += field * vec3(0.3, 0.8, 1.0) * 0.1;
    
    return color;
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Mouse interaction affects DNA structure
    vec2 mouse = mousePos / resolution;
    float mouseInfluence = 0.0;
    float helixRadius = 0.15;
    float twist = 15.0;
    
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        mouseInfluence = exp(-mouseDist * radius * 0.5) * power;
        
        // Mouse affects helix parameters
        helixRadius += mouseInfluence * 0.1;
        twist += mouseInfluence * 10.0;
    }
    
    // Scroll the DNA upward
    uv.y += time * 0.1;
    uv.y = mod(uv.y, 1.0);
    
    // Calculate DNA components
    float strand1 = dnaStrand(uv, helixRadius, 0.0, twist);
    float strand2 = dnaStrand(uv, helixRadius, PI, twist);
    float totalStrand = max(strand1, strand2);
    
    float bases = basePairs(uv, helixRadius, twist);
    float nucleotides = nucleotides(uv, helixRadius, twist);
    float particleEffect = particles(uv);
    float field = electricField(uv, helixRadius, twist);
    
    // Generate final color
    vec3 color = getDNAColor(totalStrand, bases, nucleotides, particleEffect, field, uv);
    
    // Add double helix glow
    float helixGlow = max(strand1, strand2) * 0.5;
    vec2 center = vec2(0.5, uv.y);
    float distFromCenter = distance(uv, center);
    float glow = exp(-distFromCenter * 3.0) * helixGlow;
    color += glow * vec3(0.3, 0.6, 1.0);
    
    // Add replication fork effect
    float replicationY = mod(time * 0.05, 1.0);
    if (abs(uv.y - replicationY) < 0.1) {
        float forkIntensity = 1.0 - abs(uv.y - replicationY) * 10.0;
        color += forkIntensity * vec3(1.0, 0.8, 0.2) * 0.5;
        
        // Split effect
        float splitWidth = (abs(uv.y - replicationY) / 0.1) * helixRadius;
        helixRadius += splitWidth;
    }
    
    // Mouse glow effect
    if (mouseInfluence > 0.0) {
        color += mouseInfluence * vec3(1.0, 0.5, 0.8) * 0.8;
    }
    
    // Add chromatin packaging effect (zoom out view)
    float packaging = sin(uv.x * 20.0 + time * 2.0) * sin(uv.y * 30.0 + time * 1.5) * 0.05;
    color += packaging * vec3(0.5, 0.3, 0.8);
    
    // Histone proteins
    float histone = step(0.8, sin(uv.x * 40.0 + time) * sin(uv.y * 25.0 + time * 0.7));
    color += histone * vec3(0.8, 0.2, 0.6) * 0.3;
    
    // Final adjustments
    color = pow(color, vec3(0.8)); // Gamma correction
    color *= 1.2; // Brightness boost
    
    // Add subtle film grain for realism
    float grain = sin(uv.x * 1000.0 + time) * sin(uv.y * 1000.0 + time * 1.1) * 0.02;
    color += grain;
    
    fragColor = vec4(color, 1.0);
}
