// ReSharper disable once CppInconsistentNaming
// sampler3D SolverTex;

// ReSharper disable once CppInconsistentNaming
void SetPositionsOnGrid(inout VFXAttributes attributes, in uint size, in float density, in uint particle_id) {
    uint a = attributes.particleId;
    uint x = particle_id % size;
    uint y = particle_id / (size * size);
    uint z = particle_id % (size * size) / size;

    attributes.position = (float3(x, y, z) / size - 0.5f) / density;
}

// ReSharper disable once CppInconsistentNaming
void SetTargetPositionsByLBMSolver(inout VFXAttributes attributes, in float density) {
    // float3 uvw = attributes.position * density + 0.5f;
    // float3 velocity = tex3D(SolverTex, uvw).xyz;
    // attributes.targetPosition = attributes.position + velocity;
}
