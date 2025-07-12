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

// Hash function for randomness
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Smooth noise
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// Fractal Brownian Motion for complex nebula patterns
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for (int i = 0; i < 8; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
        p = p * mat2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5));
    }
    return value;
}

// Turbulence for gas flow
float turbulence(vec2 p) {
    float turb = 0.0;
    float amplitude = 1.0;
    
    for (int i = 0; i < 4; i++) {
        turb += amplitude * abs(noise(p) - 0.5);
        amplitude *= 0.5;
        p *= 2.0;
    }
    return turb;
}

// Star field
float stars(vec2 uv) {
    vec2 starGrid = floor(uv * 200.0);
    float starNoise = hash(starGrid);
    
    if (starNoise > 0.98) {
        vec2 starPos = starGrid / 200.0 + hash(starGrid + 1.0) / 200.0;
        float dist = distance(uv, starPos);
        float star = exp(-dist * 2000.0);
        
        // Twinkling effect
        float twinkle = sin(time * 5.0 + starNoise * 20.0) * 0.3 + 0.7;
        return star * twinkle;
    }
    
    return 0.0;
}

// Distant galaxies
float galaxies(vec2 uv) {
    float galactic = 0.0;
    
    for (int i = 0; i < 5; i++) {
        vec2 galaxyCenter = vec2(
            hash(vec2(float(i))) * 0.8 + 0.1,
            hash(vec2(float(i), 1.0)) * 0.8 + 0.1
        );
        
        float dist = distance(uv, galaxyCenter);
        float galaxy = exp(-dist * 8.0) * (sin(dist * 50.0 + time + float(i)) * 0.5 + 0.5);
        galactic += galaxy * 0.3;
    }
    
    return galactic;
}

// Nebula gas clouds
vec3 nebulaGas(vec2 uv, float time) {
    // Multiple gas layers with different colors and movements
    vec2 gasUV1 = uv * 2.0 + vec2(time * 0.01, time * 0.02);
    vec2 gasUV2 = uv * 3.0 + vec2(-time * 0.015, time * 0.01);
    vec2 gasUV3 = uv * 1.5 + vec2(time * 0.005, -time * 0.008);
    
    float gas1 = fbm(gasUV1);
    float gas2 = fbm(gasUV2);
    float gas3 = fbm(gasUV3);
    
    // Add turbulence for more realistic gas flow
    float turb1 = turbulence(gasUV1 * 0.5);
    float turb2 = turbulence(gasUV2 * 0.3);
    
    gas1 += turb1 * 0.3;
    gas2 += turb2 * 0.2;
    
    // Different nebula regions with different colors
    vec3 hotGas = vec3(1.0, 0.3, 0.1); // Orange-red hot gas
    vec3 coolGas = vec3(0.1, 0.3, 1.0); // Blue cool gas
    vec3 ionizedGas = vec3(0.8, 0.1, 0.8); // Purple ionized gas
    vec3 hydrogenGas = vec3(1.0, 0.1, 0.3); // Pink hydrogen
    
    // Blend gas colors based on density and type
    vec3 gasColor = vec3(0.0);
    gasColor += hotGas * smoothstep(0.3, 0.8, gas1) * 0.7;
    gasColor += coolGas * smoothstep(0.2, 0.7, gas2) * 0.6;
    gasColor += ionizedGas * smoothstep(0.4, 0.9, gas3) * 0.5;
    gasColor += hydrogenGas * smoothstep(0.5, 1.0, (gas1 + gas2) * 0.5) * 0.4;
    
    return gasColor;
}

// Stellar nursery (bright star formation regions)
float stellarNursery(vec2 uv, float time) {
    vec2 nurseryUV = uv * 1.5 + vec2(sin(time * 0.1), cos(time * 0.08)) * 0.1;
    
    float nursery = fbm(nurseryUV * 3.0);
    nursery = pow(nursery, 2.0); // Make more concentrated
    
    // Add bright spots for newly formed stars
    float brightSpots = 0.0;
    for (int i = 0; i < 8; i++) {
        vec2 spotPos = vec2(
            hash(vec2(float(i), 10.0)) * 0.6 + 0.2,
            hash(vec2(float(i), 11.0)) * 0.6 + 0.2
        );
        
        float spotDist = distance(uv, spotPos);
        float spot = exp(-spotDist * 5.0) * (sin(time * 2.0 + float(i)) * 0.5 + 0.5);
        brightSpots += spot;
    }
    
    return nursery + brightSpots * 0.5;
}

// Cosmic dust lanes
float dustLanes(vec2 uv, float time) {
    vec2 dustUV = uv * 4.0 + vec2(time * 0.002, -time * 0.003);
    
    float dust = fbm(dustUV);
    dust = 1.0 - smoothstep(0.3, 0.7, dust); // Invert for dark dust lanes
    
    // Add wispy structure
    float wispy = sin(uv.x * 20.0 + time * 0.5) * sin(uv.y * 15.0 + time * 0.3);
    dust += wispy * 0.1;
    
    return dust;
}

// Electromagnetic radiation visualization
vec3 cosmicRadiation(vec2 uv, float time) {
    vec3 radiation = vec3(0.0);
    
    // X-ray emissions (blue-white)
    float xray = fbm(uv * 10.0 + time * 0.1) * 0.3;
    radiation += vec3(0.5, 0.8, 1.0) * xray;
    
    // Radio waves (visualized as red)
    float radio = sin(uv.x * 50.0 + time * 2.0) * sin(uv.y * 30.0 + time * 1.5) * 0.1;
    radiation += vec3(1.0, 0.2, 0.2) * radio;
    
    return radiation;
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Mouse interaction affects nebula dynamics
    vec2 mouse = mousePos / resolution;
    vec2 nebulaFlow = vec2(0.0);
    float energyBoost = 1.0;
    
    if (length(mousePos) > 0.0) {
        vec2 mouseDir = mouse - uv;
        float mouseDist = length(mouseDir);
        float mouseInfluence = exp(-mouseDist * radius * 0.5) * power;
        
        nebulaFlow = normalize(mouseDir) * mouseInfluence * 0.1;
        energyBoost += mouseInfluence * 2.0;
    }
    
    // Apply nebula flow distortion
    vec2 flowUV = uv + nebulaFlow;
    
    // Start with deep space background
    vec3 spaceColor = vec3(0.02, 0.02, 0.08);
    vec3 color = spaceColor;
    
    // Add distant galaxies
    float galaxyLayer = galaxies(flowUV);
    color += galaxyLayer * vec3(0.6, 0.4, 0.8);
    
    // Generate nebula gas
    vec3 gasColor = nebulaGas(flowUV, time);
    color += gasColor * energyBoost;
    
    // Add stellar nursery regions
    float nursery = stellarNursery(flowUV, time);
    vec3 nurseryColor = vec3(1.0, 0.8, 0.6) * nursery * 0.8;
    color += nurseryColor * energyBoost;
    
    // Add cosmic dust lanes (darken regions)
    float dust = dustLanes(flowUV, time);
    color *= (1.0 - dust * 0.6);
    
    // Add electromagnetic radiation
    vec3 radiation = cosmicRadiation(flowUV, time);
    color += radiation * 0.5;
    
    // Add star field
    float starField = stars(uv);
    color += starField * vec3(1.0, 0.9, 0.8);
    
    // Add pulsars (rapidly rotating neutron stars)
    for (int i = 0; i < 3; i++) {
        vec2 pulsarPos = vec2(
            hash(vec2(float(i), 20.0)) * 0.8 + 0.1,
            hash(vec2(float(i), 21.0)) * 0.8 + 0.1
        );
        
        float pulsarDist = distance(uv, pulsarPos);
        float pulseRate = 10.0 + float(i) * 5.0;
        float pulse = sin(time * pulseRate) * 0.5 + 0.5;
        float pulsar = exp(-pulsarDist * 20.0) * pulse;
        
        color += pulsar * vec3(0.8, 1.0, 0.9) * 2.0;
        
        // Pulsar beam
        float beamAngle = time * pulseRate * 0.1 + float(i) * 2.0;
        vec2 beamDir = vec2(cos(beamAngle), sin(beamAngle));
        float beamDist = abs(dot(normalize(uv - pulsarPos), beamDir));
        float beam = exp(-beamDist * 50.0) * exp(-pulsarDist * 2.0) * pulse;
        color += beam * vec3(0.0, 1.0, 0.8) * 0.5;
    }
    
    // Add supernova remnant (expanding shock wave)
    vec2 supernovaCenter = vec2(0.7, 0.3);
    float supernovaDist = distance(uv, supernovaCenter);
    float shockRadius = mod(time * 0.1, 0.4);
    float shockWave = exp(-abs(supernovaDist - shockRadius) * 30.0) * (0.4 - shockRadius);
    color += shockWave * vec3(1.0, 0.5, 0.2) * 3.0;
    
    // Mouse creates gravitational lensing effect
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        if (mouseDist < 0.2) {
            // Lensing distortion
            vec2 lensedUV = uv + (mouse - uv) * (0.2 - mouseDist) * 0.5;
            vec3 lensedColor = nebulaGas(lensedUV, time);
            color += lensedColor * power * 0.3;
        }
        
        // Accretion disk around mouse (black hole effect)
        float accretionDisk = exp(-mouseDist * 20.0) * sin(atan(uv.y - mouse.y, uv.x - mouse.x) * 8.0 + time * 10.0);
        color += accretionDisk * vec3(1.0, 0.6, 0.1) * power;
    }
    
    // Add cosmic microwave background radiation (very subtle)
    float cmb = noise(uv * 1000.0) * 0.01;
    color += cmb * vec3(0.1, 0.05, 0.15);
    
    // Dark matter visualization (invisible but affects structure)
    float darkMatter = fbm(uv * 0.5 + time * 0.001);
    color *= (1.0 + darkMatter * 0.1);
    
    // Add cosmic void regions (darker areas)
    float voidRegion = smoothstep(0.4, 0.6, fbm(uv * 0.3));
    color *= (1.0 - voidRegion * 0.4);
    
    // Redshift effect for distant objects
    float redshift = length(uv - vec2(0.5)) * 0.3;
    color.r += redshift * 0.1;
    color.gb *= (1.0 - redshift * 0.05);
    
    // Final cosmic enhancement
    color = pow(color, vec3(0.8)); // Gamma correction
    color *= 1.2; // Brightness boost
    
    // Add cosmic ray hits (random bright pixels)
    if (hash(uv + time) > 0.999) {
        color += vec3(1.0, 1.0, 0.8) * 2.0;
    }
    
    fragColor = vec4(color, 1.0);
}
