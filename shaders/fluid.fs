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

// Noise function
float noise(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

// Fluid flow field
vec2 flowField(vec2 pos, float time) {
    float x = pos.x;
    float y = pos.y;
    
    // Create swirling motion
    vec2 flow = vec2(
        sin(y * 3.0 + time) + cos(x * 2.0 + time * 0.7),
        cos(x * 3.0 + time * 0.8) + sin(y * 2.0 + time * 1.2)
    );
    
    return flow * 0.1;
}

// Particle simulation
float particle(vec2 uv, vec2 startPos, float time, float id) {
    vec2 pos = startPos;
    
    // Simulate particle movement through flow field
    float dt = 0.01;
    for (int i = 0; i < 50; i++) {
        vec2 flow = flowField(pos, time + float(i) * dt);
        pos += flow * dt;
    }
    
    // Add some randomness
    pos += vec2(sin(id * 2.4 + time), cos(id * 3.7 + time)) * 0.1;
    
    float dist = distance(uv, pos);
    return exp(-dist * 100.0);
}

// Fluid density field
float fluidDensity(vec2 uv, float time) {
    float density = 0.0;
    
    // Multiple fluid sources
    vec2 source1 = vec2(0.3, 0.7) + 0.1 * vec2(sin(time), cos(time));
    vec2 source2 = vec2(0.7, 0.3) + 0.1 * vec2(cos(time * 0.7), sin(time * 1.3));
    
    // Distance-based density
    density += exp(-distance(uv, source1) * 5.0);
    density += exp(-distance(uv, source2) * 4.0);
    
    // Add turbulence
    float turbulence = sin(uv.x * 10.0 + time) * sin(uv.y * 10.0 + time * 1.1);
    density += turbulence * 0.1;
    
    return density;
}

// Velocity field visualization
vec2 velocity(vec2 uv, float time) {
    vec2 vel = flowField(uv, time);
    
    // Add vortices
    vec2 vortex1 = vec2(0.25, 0.25);
    vec2 vortex2 = vec2(0.75, 0.75);
    
    vec2 r1 = uv - vortex1;
    vec2 r2 = uv - vortex2;
    
    vel += vec2(-r1.y, r1.x) * exp(-length(r1) * 3.0) * 0.5;
    vel += vec2(r2.y, -r2.x) * exp(-length(r2) * 4.0) * 0.3;
    
    return vel;
}

// Color based on fluid properties
vec3 fluidColor(float density, vec2 vel, float time) {
    float speed = length(vel);
    
    // Base fluid color (blue-cyan)
    vec3 color = vec3(0.1, 0.4, 0.8);
    
    // Color based on velocity
    color += speed * vec3(0.5, 0.3, 0.1);
    
    // Color based on density
    color = mix(color, vec3(0.8, 0.9, 1.0), density);
    
    // Add some shimmer
    float shimmer = sin(time * 5.0 + density * 10.0) * 0.1 + 0.9;
    color *= shimmer;
    
    return color;
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Calculate fluid properties
    float density = fluidDensity(uv, time);
    vec2 vel = velocity(uv, time);
    
    // Mouse interaction
    vec2 mouse = mousePos / resolution;
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float mouseInfluence = exp(-mouseDist * radius * 0.3) * power;
        
        // Mouse creates disturbance in fluid
        vec2 mouseForce = (uv - mouse) * mouseInfluence * 0.5;
        vel += mouseForce;
        
        // Mouse increases density locally
        density += mouseInfluence * 2.0;
        
        // Add bubbles around mouse
        float bubbles = sin(mouseDist * 50.0 - time * 10.0) * exp(-mouseDist * 10.0);
        density += bubbles * mouseInfluence;
    }
    
    // Generate particles
    float particles = 0.0;
    for (int i = 0; i < 20; i++) {
        float id = float(i);
        vec2 startPos = vec2(
            noise(vec2(id, 0.0)),
            noise(vec2(id, 1.0))
        );
        particles += particle(uv, startPos, time, id);
    }
    
    // Combine density and particles
    density += particles * 0.3;
    
    // Get fluid color
    vec3 color = fluidColor(density, vel, time);
    
    // Add flow visualization
    float flowVis = sin(dot(uv, normalize(vel)) * 20.0 - time * 3.0) * 0.1;
    color += flowVis * vec3(1.0, 1.0, 0.5) * length(vel);
    
    // Add caustics effect
    float caustics = sin(uv.x * 15.0 + vel.x * 10.0 + time) * 
                    sin(uv.y * 15.0 + vel.y * 10.0 + time * 1.1);
    color += caustics * density * vec3(0.3, 0.6, 1.0) * 0.2;
    
    // Surface tension effects
    float surface = smoothstep(0.3, 0.7, density);
    color = mix(color, vec3(0.9, 0.95, 1.0), surface * 0.3);
    
    // Final adjustments
    color = pow(color, vec3(0.9));
    color *= 1.1;
    
    fragColor = vec4(color, 1.0);
}
