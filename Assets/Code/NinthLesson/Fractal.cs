using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

namespace Code.NinthLesson
{
    public class Fractal : MonoBehaviour
    {
        public struct FractalPart
        {
            public float3 Direction;
            public quaternion Rotation;
            public float3 WorldPosition;
            public quaternion WorldRotation;
            public float SpinAngle;
        }

        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;

        [SerializeField, Range(1, 8)] private int _depth = 4;
        [SerializeField, Range(0, 360)] private int _speedRotation = 80;

        private const float POSITION_OFFSET = 1.5f;
        private const float SCALE_BIAS = .5f;
        private const int CHILD_COUNT = 5;

        private FractalPart[][] _parts;
        private float4x4[][] _matrices;
        private ComputeBuffer[] _matricesBuffers;

        private static readonly int MatricesId = Shader.PropertyToID("_Matrices");
        private static MaterialPropertyBlock _propertyBlock;

        private static readonly float3[] Directions =
        {
            up(),
            left(),
            right(),
            forward(),
            back()
        };

        private static readonly quaternion[] Rotations =
        {
            quaternion.identity,
            quaternion.RotateZ(-0.5f * PI),
            quaternion.RotateZ(0.5f * PI),
            quaternion.RotateX(0.5f * PI),
            quaternion.RotateX(-0.5f * PI)
        };

        private void OnEnable()
        {
            _parts = new FractalPart[_depth][];
            _matrices = new float4x4[_depth][];
            _matricesBuffers = new ComputeBuffer[_depth];
            var stride = 16 * 4;
            for (int i = 0, length = 1; i < _parts.Length; i++, length *= CHILD_COUNT)
            {
                _parts[i] = new FractalPart[length];
                _matrices[i] = new float4x4[length];
                _matricesBuffers[i] = new ComputeBuffer(length, stride);
            }

            _parts[0][0] = CreatePart(0);

            for (var li = 1; li < _parts.Length; li++)
            {
                var levelParts = _parts[li];

                for (var fpi = 0; fpi < levelParts.Length; fpi += CHILD_COUNT)
                {
                    for (var ci = 0; ci < CHILD_COUNT; ci++)
                    {
                        levelParts[fpi + ci] = CreatePart(ci);
                    }
                }
            }

            _propertyBlock ??= new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            for (var i = 0; i < _matricesBuffers.Length; i++)
            {
                _matricesBuffers[i].Release();
            }

            _parts = null;
            _matrices = null;
            _matricesBuffers = null;
        }

        private void OnValidate()
        {
            if (_parts is null || !enabled)
            {
                return;
            }

            OnDisable();
            OnEnable();
        }

        private FractalPart CreatePart(int childIndex) => new FractalPart
        {
            Direction = Directions[childIndex],
            Rotation = Rotations[childIndex],
        };

        private void FixedUpdate()
        {
            var spinAngelDelta = _speedRotation * PI * Time.deltaTime;
            var rootPart = _parts[0][0];
            rootPart.SpinAngle += spinAngelDelta;
            rootPart.WorldRotation = mul(rootPart.Rotation, quaternion.RotateY(rootPart.SpinAngle));
            _parts[0][0] = rootPart;
            _matrices[0][0] = float4x4.TRS(rootPart.WorldPosition, rootPart.WorldRotation, float3(Vector3.one));

            ParallelCalculate(spinAngelDelta);
            Bounds(rootPart);
        }

        private void ParallelCalculate(float spinAngleDelta)
        {
            for (var li = 1; li < _parts.Length; li++)
            {
                var scale = 1.0f;
                scale *= SCALE_BIAS;
                var parentParts = _parts[li - 1];
                var levelParts = _parts[li];
                var levelMatrices = _matrices[li];

                JobStructParallel parallelThread = new JobStructParallel
                {
                    ParentParts = CreateFractalPartArray(parentParts, parentParts.Length),
                    LevelParts = CreateFractalPartArray(levelParts, levelParts.Length),
                    LevelMatrices = CreateMatricesArray(levelMatrices, levelMatrices.Length),
                    SpinAngleDelta = spinAngleDelta,
                    Scale = scale
                };
                JobHandle newJobHandle = parallelThread.Schedule(levelParts.Length, 0);
                newJobHandle.Complete();

                parallelThread.LevelParts.Dispose();
                parallelThread.ParentParts.Dispose();
                parallelThread.LevelMatrices.Dispose();
            }
        }

        private void Bounds(FractalPart rootPart)
        {
            var bounds = new Bounds(rootPart.WorldPosition, 3f * Vector3.one);
            for (var i = 0; i < _matricesBuffers.Length; i++)
            {
                var buffer = _matricesBuffers[i];
                buffer.SetData(_matrices[i]);
                _propertyBlock.SetBuffer(MatricesId, buffer);
                _material.SetBuffer(MatricesId, buffer);
                Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds,
                    buffer.count, _propertyBlock);
            }
        }

        private NativeArray<FractalPart> CreateFractalPartArray(FractalPart[] fractalParts, int length)
        {
            NativeArray<FractalPart> fractalPartArray = new NativeArray<FractalPart>(length, Allocator.Persistent);

            for (int i = 0; i < length; i++)
            {
                fractalPartArray[i] = new FractalPart
                {
                    Direction = fractalParts[i].Direction,
                    Rotation = fractalParts[i].Rotation,
                    WorldPosition = fractalParts[i].WorldPosition,
                    WorldRotation = fractalParts[i].WorldRotation,
                    SpinAngle = fractalParts[i].SpinAngle,
                };
            }

            return fractalPartArray;
        }

        private NativeArray<float4x4> CreateMatricesArray(float4x4[] matrix, int length)
        {
            NativeArray<float4x4> matrices = new NativeArray<float4x4>(length, Allocator.Persistent);

            for (int i = 0; i < length; i++)
            {
                matrices[i] = matrix[i];
            }

            return matrices;
        }

        [BurstCompile]
        public struct JobStructParallel : IJobParallelFor
        {
            [ReadOnly] public NativeArray<FractalPart> ParentParts;
            public NativeArray<FractalPart> LevelParts;
            [WriteOnly] public NativeArray<float4x4> LevelMatrices;
            [ReadOnly] public float SpinAngleDelta;
            [ReadOnly] public float Scale;

            public void Execute(int fpi)
            {
                var parent = ParentParts[fpi / CHILD_COUNT];
                var part = LevelParts[fpi];
                part.SpinAngle += SpinAngleDelta;
                part.WorldRotation = mul(parent.WorldRotation, mul(part.Rotation, quaternion.RotateY(part.SpinAngle)));
                part.WorldPosition = parent.WorldPosition +
                                     mul(parent.WorldRotation, POSITION_OFFSET * Scale * part.Direction);
                LevelParts[fpi] = part;
                LevelMatrices[fpi] = float4x4.TRS(part.WorldPosition, part.WorldRotation, float3(Scale));
            }
        }
    }
}
