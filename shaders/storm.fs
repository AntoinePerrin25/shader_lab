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

// Noise function
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

// Fractal Brownian Motion for complex cloud patterns
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for (int i = 0; i < 6; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
        p = p * mat2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5));
    }
    return value;
}

// Lightning bolt generation
float lightning(vec2 uv, vec2 start, vec2 end, float time, float seed) {
    vec2 dir = normalize(end - start);
    vec2 perp = vec2(-dir.y, dir.x);
    
    float t = dot(uv - start, dir) / length(end - start);
    t = clamp(t, 0.0, 1.0);
    
    // Jagged lightning path
    float zigzag = sin(t * 20.0 + seed * 10.0 + time * 5.0) * 0.05;
    zigzag += sin(t * 40.0 + seed * 15.0 + time * 8.0) * 0.02;
    zigzag += sin(t * 80.0 + seed * 20.0 + time * 12.0) * 0.01;
    
    vec2 lightningPos = mix(start, end, t) + perp * zigzag;
    float dist = distance(uv, lightningPos);
    
    // Main bolt
    float bolt = exp(-dist * 300.0);
    
    // Glow around bolt
    float glow = exp(-dist * 50.0) * 0.3;
    
    // Flickering effect
    float flicker = sin(time * 20.0 + seed * 30.0) * 0.3 + 0.7;
    
    return (bolt + glow) * flicker;
}

// Cloud formation
float clouds(vec2 uv, float time) {
    vec2 cloudUV = uv * 3.0 + vec2(time * 0.1, 0.0);
    
    float cloud1 = fbm(cloudUV);
    float cloud2 = fbm(cloudUV * 2.0 + vec2(time * 0.05, 0.0));
    float cloud3 = fbm(cloudUV * 4.0 - vec2(time * 0.08, 0.0));
    
    float clouds = cloud1 * 0.6 + cloud2 * 0.3 + cloud3 * 0.1;
    
    // Make clouds more dramatic
    clouds = pow(clouds, 1.5);
    
    return clouds;
}

// Rain effect
float rain(vec2 uv, float intensity) {
    float rainEffect = 0.0;
    
    for (int i = 0; i < 50; i++) {
        float x = hash(vec2(float(i))) * 1.2 - 0.1;
        float speed = 2.0 + hash(vec2(float(i), 1.0)) * 3.0;
        float y = mod(time * speed + hash(vec2(float(i), 2.0)), 1.2) - 0.1;
        
        vec2 dropPos = vec2(x, y);
        float dist = distance(uv, dropPos);
        
        // Rain drop
        float drop = exp(-dist * 1000.0);
        
        // Rain streak
        float streak = exp(-abs(uv.x - dropPos.x) * 200.0) * 
                      smoothstep(dropPos.y, dropPos.y + 0.1, uv.y) * 
                      smoothstep(dropPos.y + 0.2, dropPos.y + 0.1, uv.y);
        
        rainEffect += (drop + streak * 0.3) * intensity;
    }
    
    return rainEffect;
}

// Thunder/lightning flash
float thunderFlash(float time) {
    float flashTime = sin(time * 0.3) * 0.5 + 0.5;
    
    // Random lightning strikes
    if (flashTime > 0.95) {
        return sin(time * 100.0) * 0.5 + 0.5;
    } else if (flashTime > 0.9) {
        return sin(time * 50.0) * 0.3 + 0.3;
    }
    
    return 0.0;
}

// Wind effect on rain
vec2 windDistortion(vec2 uv, float windStrength) {
    float windX = sin(uv.y * 10.0 + time * 2.0) * windStrength;
    float windY = sin(uv.x * 8.0 + time * 1.5) * windStrength * 0.5;
    return uv + vec2(windX, windY) * 0.02;
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Mouse interaction affects storm intensity
    vec2 mouse = mousePos / resolution;
    float stormIntensity = 1.0;
    float windStrength = 0.5;
    
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float mouseInfluence = exp(-mouseDist * radius * 0.3) * power;
        stormIntensity += mouseInfluence * 2.0;
        windStrength += mouseInfluence * 0.5;
    }
    
    // Apply wind distortion
    vec2 windUV = windDistortion(uv, windStrength);
    
    // Sky gradient
    vec3 skyColor = mix(vec3(0.1, 0.1, 0.2), vec3(0.2, 0.2, 0.3), 1.0 - uv.y);
    vec3 color = skyColor;
    
    // Storm clouds
    float cloudLayer = clouds(windUV, time);
    vec3 cloudColor = mix(vec3(0.3, 0.3, 0.4), vec3(0.15, 0.15, 0.25), cloudLayer);
    cloudColor = mix(cloudColor, vec3(0.05, 0.05, 0.1), pow(cloudLayer, 2.0));
    
    // Apply clouds to sky
    color = mix(color, cloudColor, smoothstep(0.3, 0.8, cloudLayer));
    
    // Lightning bolts
    float lightningEffect = 0.0;
    
    // Multiple lightning bolts
    for (int i = 0; i < 5; i++) {
        float seed = float(i);
        vec2 start = vec2(0.2 + seed * 0.15, 0.8 + sin(time * 0.5 + seed) * 0.1);
        vec2 end = vec2(0.3 + seed * 0.1, 0.2 + sin(time * 0.3 + seed * 2.0) * 0.3);
        
        // Only show lightning occasionally
        float lightningTime = sin(time * 0.2 + seed * 5.0);
        if (lightningTime > 0.7) {
            lightningEffect += lightning(windUV, start, end, time, seed) * stormIntensity;
        }
    }
    
    // Apply lightning
    color += lightningEffect * vec3(0.8, 0.9, 1.0);
    
    // Rain
    float rainIntensity = 0.3 + stormIntensity * 0.4;
    float rainEffect = rain(windUV, rainIntensity);
    color += rainEffect * vec3(0.6, 0.7, 0.8) * 0.5;
    
    // Thunder flash (illuminates entire scene)
    float flash = thunderFlash(time) * stormIntensity;
    color += flash * vec3(0.8, 0.85, 1.0) * 0.3;
    
    // Ground/horizon
    if (uv.y < 0.15) {
        vec3 groundColor = vec3(0.05, 0.1, 0.05); // Dark ground
        float groundMix = smoothstep(0.1, 0.15, uv.y);
        color = mix(groundColor, color, groundMix);
        
        // Lightning reflections in puddles
        if (uv.y < 0.1 && lightningEffect > 0.0) {
            float puddleReflection = lightningEffect * 0.3;
            color += puddleReflection * vec3(0.6, 0.7, 0.9);
        }
    }
    
    // Atmospheric fog
    float fog = fbm(windUV * 2.0 + time * 0.05) * 0.3;
    fog *= (1.0 - uv.y) * 0.5;
    color = mix(color, vec3(0.4, 0.4, 0.5), fog);
    
    // Add dramatic lighting from lightning
    if (lightningEffect > 0.0) {
        // Volumetric lighting effect
        float volumetric = lightningEffect * fbm(windUV * 5.0) * 0.4;
        color += volumetric * vec3(0.7, 0.8, 1.0);
    }
    
    // Mouse storm center effect
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float stormCenter = exp(-mouseDist * radius) * power;
        
        // Eye of the storm (calm center)
        if (mouseDist < 0.1 * radius) {
            color = mix(color, vec3(0.6, 0.6, 0.8), stormCenter * 0.3);
        } else {
            // Swirling effect around mouse
            float swirl = sin(atan(uv.y - mouse.y, uv.x - mouse.x) * 8.0 + time * 5.0) * stormCenter;
            color += swirl * vec3(0.3, 0.4, 0.6) * 0.2;
        }
    }
    
    // Hail effect (small white dots)
    if (stormIntensity > 1.5) {
        float hail = 0.0;
        for (int i = 0; i < 20; i++) {
            float hailX = hash(vec2(float(i), 0.0)) * 1.2 - 0.1;
            float hailSpeed = 3.0 + hash(vec2(float(i), 1.0)) * 2.0;
            float hailY = mod(time * hailSpeed + hash(vec2(float(i), 2.0)), 1.2) - 0.1;
            
            vec2 hailPos = vec2(hailX, hailY) + windDistortion(vec2(hailX, hailY), windStrength * 2.0);
            float hailDist = distance(uv, hailPos);
            hail += exp(-hailDist * 500.0);
        }
        color += hail * vec3(1.0, 1.0, 1.0) * 0.8;
    }
    
    // Final color adjustments
    color = pow(color, vec3(0.9)); // Gamma correction (darker for storm)
    color *= 0.8; // Reduce overall brightness for storm atmosphere
    
    // Add film grain for realism
    float grain = hash(uv + time) * 0.03;
    color += grain;
    
    fragColor = vec4(color, 1.0);
}
