using System.Threading;
using UnityEngine;

namespace Code.FirstLesson
{
    public class FirstLessonMain : MonoBehaviour
    {
        //FirstTask
        [SerializeField] private Unit _unit;
        private float _healLifePoints = 5.0f;
        private float _frequency = 0.5f;
        private float _maxDuration = 3.0f;

        //SecondTask
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private string _firstMessage = "Task #1 is done";
        private string _secondMessage = "Task #2 is done";
        private int _time = 1000;
        private int _frames = 60;


        void Start()
        {
            //FirstTask

            _unit.ReceiveHealing(_healLifePoints, _frequency, _maxDuration);
            _unit.ReceiveHealing(_healLifePoints, _frequency, _maxDuration);
            _unit.ReceiveHealing(_healLifePoints, _frequency, _maxDuration);
            _unit.ReceiveHealing(_healLifePoints, _frequency, _maxDuration);
            _unit.ReceiveHealing(_healLifePoints, _frequency, _maxDuration);

            //SecondTask

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            var taskSecond = new FirstLessonSecondTask();
            var task1 = taskSecond.Task1Async(_cancellationToken, _firstMessage, _time);
            var task2 = taskSecond.Task2Async(_cancellationToken, _secondMessage, _frames);

            //_cancellationTokenSource.Cancel();

            // var task3 = taskSecond.Task1Async(_cancellationToken, _firstMessage, _time);
            // var task4 = taskSecond.Task2Async(_cancellationToken, _secondMessage, _frames);

            //ThirdTask

            var taskThird = new FirstLessonThirdTask();
            taskThird.WhatTaskFasterAsync(_cancellationToken, task1, task2);
            //taskThird.WhatTaskFasterAsync(_cancellationToken, task3, task4);

            _cancellationTokenSource.Dispose();
        }
    }
}
