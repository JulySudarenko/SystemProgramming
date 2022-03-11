using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.SecondLesson
{
    internal class SecondLessonMain : MonoBehaviour
    {
        private int _arrayLength = 10;

        private void Start()
        {
            JobStruct jobFirstTask = new JobStruct
            {
                FirstTaskArray = CreateRandomIntArray(_arrayLength)
            };

            JobHandle jobHandle = jobFirstTask.Schedule(); //задача отправляется в пул потоков юнити
            //jobFirstTask.Execute(); //задача выполнится синхронно

            jobHandle.Complete();
            jobFirstTask.FirstTaskArray.Dispose();

            NativeArray<Vector3> finalPositions = new NativeArray<Vector3>(_arrayLength, Allocator.Persistent);

            var positions = CreateRandomVector3Array(_arrayLength);
            var velocities = CreateRandomVector3Array(_arrayLength);

            JobStructParallel jobSecondTask = new JobStructParallel
            {
                Positions = positions,
                Velocities = velocities,
                FinalPositions = finalPositions
            };

            JobHandle jobHandleSecondTask = jobSecondTask.Schedule(finalPositions.Length, 0);
            jobHandleSecondTask.Complete();

            jobSecondTask.Positions.Dispose();
            jobSecondTask.Velocities.Dispose();
            jobSecondTask.FinalPositions.Dispose();
        }

        private NativeArray<Vector3> CreateRandomVector3Array(int arrayLength)
        {
            NativeArray<Vector3> arrayVector3 = new NativeArray<Vector3>(arrayLength, Allocator.Persistent);

            for (int i = 0; i < arrayLength; i++)
            {
                arrayVector3[i] = new Vector3(Random.Range(1, 10), Random.Range(1, 10), Random.Range(1, 10));
            }

            return arrayVector3;
        }

        private NativeArray<int> CreateRandomIntArray(int arrayLength)
        {
            var newIntArray = new NativeArray<int>(arrayLength, Allocator.Persistent);
            for (int i = 0; i < arrayLength; i++)
            {
                newIntArray[i] = Random.Range(1, 20);
            }

            return newIntArray;
        }
    }
    
    public struct JobStruct : IJob
    {
        public NativeArray<int> FirstTaskArray;

        public void Execute()
        {
            for (int i = 0; i < FirstTaskArray.Length; i++)
            {
                if (FirstTaskArray[i] >= 10)
                {
                    FirstTaskArray[i] = 0;
                }

                Debug.Log(FirstTaskArray[i]);
            }
        }
    }

    public struct JobStructParallel : IJobParallelFor
    {
        public NativeArray<Vector3> Positions;

        public NativeArray<Vector3> Velocities;

        public NativeArray<Vector3> FinalPositions;

        public void Execute(int i)
        {
            FinalPositions[i] = Positions[i] + Velocities[i];
            Debug.Log(FinalPositions[i]);
        }
    }
}
