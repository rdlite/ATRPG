using System.Collections;
using UnityEngine;

public interface ICoroutineService : IService
{
    Coroutine StartCoroutine(IEnumerator coroutine);
    void StopCoroutine(Coroutine routine);
}