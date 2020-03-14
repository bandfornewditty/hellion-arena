﻿using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

/// <summary>
/// InputSystem
/// </summary>
[AlwaysSynchronizeSystem]
public class MouseInputSystem : JobComponentSystem
{
    private const float RAYCAST_DISTANCE = 100f;
    private float3 destination;

    protected override void OnCreate() => RequireSingletonForUpdate<NavMap>();

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var result = new RaycastHit();

        if (Input.GetMouseButton(0) && MouseRaycast(RAYCAST_DISTANCE, out result))
        {
            destination = result.Position;
            DebugExtension.DebugPoint(destination, 1);
        }
        else
        {
            return default;
        }

        var dst = destination;
        var dt = Time.fixedDeltaTime;

        Entities
            .WithAll<NavAgent>()
            .WithNone<PathRequest, Waypoint>()
            .WithStructuralChanges() // Use CmdBuffer instead
            .ForEach((Entity entity) =>
            {
                EntityManager.AddComponentData(entity, new PathRequest { To = dst });
                //var waypoints = EntityManager.AddBuffer<Waypoint>(entt);
                //waypoints.Add(new Waypoint() { Value = new float3(dst.x, translation.Value.y, dst.z) });
            })
            .WithoutBurst()
            .Run();

        return default;
    }

    // Raycast from mouse position to world
    public bool MouseRaycast(float distance, out RaycastHit hit)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastInput input = new RaycastInput()
        {
            Start = ray.origin,
            End = ray.direction * distance,
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            }
        };

        var physicsWorld = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>().PhysicsWorld;
        return physicsWorld.CollisionWorld.CastRay(input, out hit);
    }
}