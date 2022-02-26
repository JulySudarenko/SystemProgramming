using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Code.FirstLesson
{
    internal class FirstLessonSecondTask
    {
        private string _cancelFirstMessage = "Task #1 was canceled";
        private string _cancelSecondMessage = "Task #2 was canceled";

        public async Task Task1Async(CancellationToken token, string message, int time)
        {
            using (CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                if (token.IsCancellationRequested)
                {
                    Debug.Log(_cancelFirstMessage);
                    return;
                }

                await Task.Delay(time);
                // if (token.IsCancellationRequested)
                // {
                //     Debug.Log(_cancelFirstMessage);
                //     return;
                // }
                Debug.Log(message);
                linkedToken.Cancel();
            }
        }

        public async Task Task2Async(CancellationToken token, string message, int frames)
        {
            using (CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                if (token.IsCancellationRequested)
                {
                    Debug.Log(_cancelSecondMessage);
                    return;
                }

                while (frames > 0)
                {
                    // if (token.IsCancellationRequested)
                    // {
                    //     Debug.Log(_cancelSecondMessage);
                    //     return;
                    // }
                    
                    frames--;
                    await Task.Yield();
                }

                Debug.Log(message);
                linkedToken.Cancel();
            }
        }
    }
}
