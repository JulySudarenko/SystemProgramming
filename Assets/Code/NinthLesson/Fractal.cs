using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Code.NinthLesson
{
    public class Fractal : MonoBehaviour
    {
        public struct FractalPart
        {
            public Vector3 Direction;
            public Quaternion Rotation;
            public Vector3 WorldPosition;
            public Quaternion WorldRotation;
            public float SpinAngle;
        }

        private static Quaternion _deltaRotation;
        private static float _spinAngelDelta;
        private static float _scale;

        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;

        [SerializeField, Range(1, 8)] private int _depth = 4;
        [SerializeField, Range(0, 360)] private int _speedRotation = 80;

        private const float POSITION_OFFSET = 1.5f;
        private const float SCALE_BIAS = .5f;
        private const int CHILD_COUNT = 5;


        public FractalPart[][] _parts;
        public Matrix4x4[][] _matrices;
        private ComputeBuffer[] _matricesBuffers;

        private static readonly int MatricesId = Shader.PropertyToID("_Matrices");
        private static MaterialPropertyBlock _propertyBlock;

        private static readonly Vector3[] _directions =
        {
            Vector3.up,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        private static readonly Quaternion[] _rotations =
        {
            Quaternion.identity,
            Quaternion.Euler(.0f, .0f, 90.0f),
            Quaternion.Euler(.0f, .0f, -90.0f),
            Quaternion.Euler(90.0f, .0f, .0f),
            Quaternion.Euler(-90.0f, .0f, .0f)
        };


        private void OnEnable()
        {
            _parts = new FractalPart[_depth][];
            _matrices = new Matrix4x4[_depth][];
            _matricesBuffers = new ComputeBuffer[_depth];
            var stride = 16 * 4;
            for (int i = 0, length = 1; i < _parts.Length; i++, length *= CHILD_COUNT)
            {
                _parts[i] = new FractalPart[length];
                _matrices[i] = new Matrix4x4[length];
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
            Direction = _directions[childIndex],
            Rotation = _rotations[childIndex],
        };

        private void FixedUpdate()
        {
            _spinAngelDelta = _speedRotation * Time.deltaTime;
            var rootPart = _parts[0][0];
            rootPart.SpinAngle += _spinAngelDelta;
            _deltaRotation = Quaternion.Euler(.0f, rootPart.SpinAngle, .0f);
            rootPart.WorldRotation = rootPart.Rotation * _deltaRotation;
            _parts[0][0] = rootPart;
            _matrices[0][0] = Matrix4x4.TRS(rootPart.WorldPosition, rootPart.WorldRotation, Vector3.one);
            _scale = 1.0f;

            ParallelCalculate();
            Bounds(rootPart);
        }

        private void ParallelCalculate()
        {
            for (var li = 1; li < _parts.Length; li++)
            {
                _scale *= SCALE_BIAS;
                var parentParts = _parts[li - 1];
                var levelParts = _parts[li];
                var levelMatrices = _matrices[li];

                JobStructParallel parallelThread = new JobStructParallel
                {
                    ParentParts = CreateFractalPartArray(parentParts, parentParts.Length),
                    LevelParts = CreateFractalPartArray(levelParts, levelParts.Length),
                    LevelMatrices = CreateMatricesArray(levelMatrices, levelMatrices.Length)
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

        private NativeArray<Matrix4x4> CreateMatricesArray(Matrix4x4[] matrix, int length)
        {
            NativeArray<Matrix4x4> matrices = new NativeArray<Matrix4x4>(length, Allocator.Persistent);

            for (int i = 0; i < length; i++)
            {
                matrices[i] = new Matrix4x4
                {
                    m00 = matrix[i].m00,
                    m01 = matrix[i].m01,
                    m02 = matrix[i].m02,
                    m03 = matrix[i].m03,
                    m10 = matrix[i].m10,
                    m11 = matrix[i].m11,
                    m12 = matrix[i].m12,
                    m13 = matrix[i].m13,
                    m20 = matrix[i].m20,
                    m21 = matrix[i].m21,
                    m22 = matrix[i].m22,
                    m23 = matrix[i].m23,
                    m30 = matrix[i].m30,
                    m31 = matrix[i].m31,
                    m32 = matrix[i].m32,
                    m33 = matrix[i].m33,
                };
            }

            return matrices;
        }

        [BurstCompile]
        public struct JobStructParallel : IJobParallelFor
        {
            [ReadOnly] public NativeArray<FractalPart> ParentParts;
            public NativeArray<FractalPart> LevelParts;
            public NativeArray<Matrix4x4> LevelMatrices;

            public void Execute(int fpi)
            {
                var parent = ParentParts[fpi / CHILD_COUNT];
                var part = LevelParts[fpi];
                part.SpinAngle += _spinAngelDelta;
                _deltaRotation = Quaternion.Euler(.0f, part.SpinAngle, .0f);
                part.WorldRotation = parent.WorldRotation * part.Rotation * _deltaRotation;
                part.WorldPosition = parent.WorldPosition +
                                     parent.WorldRotation * (POSITION_OFFSET * _scale * part.Direction);
                LevelParts[fpi] = part;
                LevelMatrices[fpi] = Matrix4x4.TRS(part.WorldPosition, part.WorldRotation, _scale * Vector3.one);
            }
        }
    }
}

