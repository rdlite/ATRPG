using UnityEngine;

public class KeyboardCheats : MonoBehaviour
{
    [SerializeField] private float _gameTimeScale = 5f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = Time.timeScale == _gameTimeScale ? 1f : _gameTimeScale;
        }
    }
}