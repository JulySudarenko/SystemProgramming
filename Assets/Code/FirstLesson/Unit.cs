using System.Collections;
using UnityEngine;

namespace Code.FirstLesson
{
    internal class Unit : MonoBehaviour
    {
        private float _health = 80.0f;
        private float _maxHealth = 100.0f;
        private float _duration;
        private bool _heal;


        public void ReceiveHealing(float lifePoints, float frequency, float maxDuration)
        {
            if (_duration == 0.0f)
            {
                _duration = 0.5f;
                StartCoroutine(StartHealingProcess(lifePoints, frequency, maxDuration));
            }
        }

        private IEnumerator StartHealingProcess(float lifePoints, float frequency, float maxDuration)
        {
            for (_duration = 0.5f; _duration < maxDuration; _duration += frequency)
            {
                if (_duration < maxDuration && _health < _maxHealth)
                {
                    yield return new WaitForSeconds(frequency);
                    Heal(lifePoints);
                }
            }

            _duration = 0.0f;
        }

        private void Heal(float lifePoints)
        {
            _health += lifePoints;
            if (_health > _maxHealth)
                _health = _maxHealth;
            Debug.Log(_health);
            Debug.Log(_duration);
        }
    }
}
