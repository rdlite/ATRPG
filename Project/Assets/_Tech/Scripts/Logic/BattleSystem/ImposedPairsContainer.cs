using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImposedPairsContainer
{
    private ICoroutineService _coroutineService;
    private List<ImposedPair> _pairs;

    public ImposedPairsContainer(ICoroutineService coroutineService)
    {
        _coroutineService = coroutineService;
        _pairs = new List<ImposedPair>();
    }

    public bool HasPairWith(UnitBase unitToCheck)
    {
        for (int i = 0; i < _pairs.Count; i++)
        {
            if (_pairs[i].Unit1 == unitToCheck || _pairs[i].Unit2 == unitToCheck)
            {
                return true;
            }
        }

        return false;
    }

    public void TryCreateNewPair(UnitBase unit1, UnitBase unit2)
    {
        if (!HasPairWith(unit1) && !HasPairWith(unit2) && !unit1.IsDeadOnBattleField && !unit2.IsDeadOnBattleField)
        {
            _pairs.Add(new ImposedPair(unit1, unit2));
            _pairs[^1].AttakingRoutine = _coroutineService.StartCoroutine(AttakingRoutine(_pairs[^1]));
        }
    }

    public void TryRemovePair(UnitBase unitToCheck)
    {
        for (int i = 0; i < _pairs.Count; i++)
        {
            if (_pairs[i].Unit1 == unitToCheck || _pairs[i].Unit2 == unitToCheck)
            {
                if (_pairs[i].AttakingRoutine != null)
                {
                    _coroutineService.StopCoroutine(_pairs[i].AttakingRoutine);
                }

                _pairs.RemoveAt(i);
                return;
            }
        }
    }

    public UnitBase GetPairFor(UnitBase unitToCheck)
    {
        for (int i = 0; i < _pairs.Count; i++)
        {
            if (_pairs[i].Unit1 == unitToCheck || _pairs[i].Unit2 == unitToCheck)
            {
                return _pairs[i].Unit1 == unitToCheck ? _pairs[i].Unit2 : _pairs[i].Unit1;
            }
        }

        return null;
    }

    public Vector2 GetAttackDirectionFor(UnitBase unitToCheck)
    {
        if (HasPairWith(unitToCheck))
        {
            return new Vector2(unitToCheck.transform.forward.x, unitToCheck.transform.forward.z);
        }
        else
        {
            return Vector2.zero;
        }
    }

    private IEnumerator AttakingRoutine(ImposedPair pair)
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            if (!pair.Unit1.IsDeadOnBattleField && !pair.Unit2.IsDeadOnBattleField && !pair.Unit1.IsBusy && !pair.Unit2.IsBusy)
            {
                if (Random.Range(0, 2) == 0)
                {
                    pair.Unit1.PlayImposedAttackAnimation();
                    pair.Unit2.PlayImposedImpactAnimation();
                }
                else
                {
                    pair.Unit2.PlayImposedAttackAnimation();
                    pair.Unit1.PlayImposedImpactAnimation();
                }
            }

            yield return new WaitForSeconds(Random.Range(2.5f, 4f));
        }
    }

    private class ImposedPair
    {
        public UnitBase Unit1, Unit2;
        public Coroutine AttakingRoutine;

        public ImposedPair(UnitBase unit1, UnitBase unit2)
        {
            Unit1 = unit1;
            Unit2 = unit2;
        }
    }
}