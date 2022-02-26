using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Code.FirstLesson
{
    internal class FirstLessonThirdTask
    {
        public async Task WhatTaskFasterAsync(CancellationToken token, Task task1, Task task2)
        {
            using (CancellationTokenSource tokenLinks = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                CancellationToken tokenLink = tokenLinks.Token;
                Task finishedTask = await Task.WhenAny(task1, task2);
                
                if (token.IsCancellationRequested)
                {
                    Debug.Log(false);
                    tokenLinks.Cancel();
                    return;
                }

                if (finishedTask == task1)
                {
                    Debug.Log(true);
                }
                else
                {
                    Debug.Log(false);
                }

                tokenLinks.Cancel();
            }
        }
    }
}
