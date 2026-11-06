using System;
using System.Collections.Generic;
using UnityEngine;
using EnemyOperation = OperationEnemySpawner.EnemyOperation;
using SpecialEnemyType = OperationEnemySpawner.SpecialEnemyType;

// Runtime identity only. The serialized operation list is always supplied by the caller.
internal sealed class EnemyRegistry
{
    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly HashSet<GameObject> answeredEnemies = new HashSet<GameObject>();
    private readonly List<GameObject> destroyedEnemyBuffer = new List<GameObject>();
    private int spawnReservations;
    internal int SpawnReservations => spawnReservations;
    internal int ActiveCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                GameObject enemy = activeEnemies[i];
                if (enemy != null && !answeredEnemies.Contains(enemy) && activeEnemies.IndexOf(enemy) == i) count++;
            }
            return count;
        }
    }
    internal bool HasSpawnCapacity(int limit) => ActiveCount + spawnReservations < limit;
    internal bool TryReserveSpawnSlot(int limit)
    {
        if (!HasSpawnCapacity(limit)) return false;
        spawnReservations++;
        return true;
    }
    internal bool CanCommitSpawnSlot(int limit) => spawnReservations > 0 && ActiveCount + spawnReservations <= limit;
    internal void ReleaseSpawnSlot() { spawnReservations--; }
    internal IReadOnlyList<GameObject> ActiveEnemies => activeEnemies;
    internal IEnumerable<GameObject> AnsweredEnemies => answeredEnemies;
    internal void AddActive(GameObject enemy) { activeEnemies.Add(enemy); }
    internal bool RemoveActive(GameObject enemy) { return activeEnemies.Remove(enemy); }
    internal bool IsActive(GameObject enemy) { return activeEnemies.Contains(enemy); }
    internal bool IsAnswered(GameObject enemy) { return answeredEnemies.Contains(enemy); }
    internal void ClearActive() { activeEnemies.Clear(); }
    internal void ClearConfirmationTracking() { answeredEnemies.Clear(); destroyedEnemyBuffer.Clear(); }

    // Read-only snapshot: pressing Z without an eligible enemy must change no state.
    internal List<Transform> CollectCollectiveTargets(List<EnemyOperation> operations)
    {
        var targets = new List<Transform>();
        var seen = new HashSet<GameObject>();
        foreach (EnemyOperation operation in operations)
        {
            GameObject enemy = operation.enemy;
            if (enemy != null && enemy.transform.position.y < 3f && !IsAnswered(enemy) && seen.Add(enemy))
                targets.Add(enemy.transform);
        }
        return targets;
    }

    internal void SelectCollectiveTargets(Func<List<EnemyOperation>> readOperations, Func<GameObject, bool> confirm,
        List<Transform> targets, List<int> indices)
    {
        // Confirmation can synchronously replace the public metadata list.
        for (int i = 0; i < readOperations().Count; i++)
        {
            if (readOperations()[i].enemy != null && readOperations()[i].enemy.transform.position.y < 3f && !IsAnswered(readOperations()[i].enemy))
            {
                confirm(readOperations()[i].enemy);
                targets.Add(readOperations()[i].enemy.transform);
                indices.Add(i);
            }
        }
    }

    internal void RemoveCollectiveTargets(Func<List<EnemyOperation>> readOperations, List<int> indices)
    {
        // Animation/projectile callbacks run before this step; never retain their old list.
        for (int i = indices.Count - 1; i >= 0; i--)
        {
            int index = indices[i];
            GameObject enemy = readOperations()[index].enemy;
            if (IsActive(enemy))
                RemoveActive(enemy);
            readOperations().RemoveAt(index);
        }
    }

    internal bool HasEnemyBelow(float threshold)
    {
        foreach (GameObject enemy in activeEnemies)
            if (enemy != null && enemy.transform.position.y < threshold) return true;
        return false;
    }

    internal bool TryConfirm(GameObject enemy)
    {
        if (enemy == null || !answeredEnemies.Add(enemy)) return false;
        foreach (Collider2D collider in enemy.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        return true;
    }

    internal int FindCandidateIndex(List<EnemyOperation> operations, int answer)
    {
        int index = -1;
        float lowestY = Mathf.Infinity;
        for (int i = 0; i < operations.Count; i++)
        {
            if (operations[i].result == answer && operations[i].enemy != null && !answeredEnemies.Contains(operations[i].enemy))
            {
                float y = operations[i].enemy.transform.position.y;
                if (MathAnswerEvaluator.IsPreferredCandidate(y, lowestY)) { lowestY = y; index = i; }
            }
        }
        return index;
    }

    internal SpecialEnemyType OriginalType(List<EnemyOperation> operations, GameObject enemy)
    {
        for (int i = 0; i < operations.Count; i++)
            if (operations[i].enemy == enemy) return operations[i].specialType;
        return SpecialEnemyType.None;
    }

    internal void RemoveEnemy(GameObject enemy, List<EnemyOperation> operations)
    {
        activeEnemies.Remove(enemy);
        for (int i = operations.Count - 1; i >= 0; i--)
            if (operations[i].enemy == enemy) operations.RemoveAt(i);
    }

    internal void PruneDestroyed(List<EnemyOperation> enemyOperations)
    {
        foreach (GameObject enemy in answeredEnemies)
        {
            if (enemy == null)
                destroyedEnemyBuffer.Add(enemy);
        }
        foreach (GameObject enemy in destroyedEnemyBuffer)
            answeredEnemies.Remove(enemy);
        destroyedEnemyBuffer.Clear();
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
                activeEnemies.RemoveAt(i);
        }
        for (int i = enemyOperations.Count - 1; i >= 0; i--)
        {
            if (enemyOperations[i].enemy == null)
                enemyOperations.RemoveAt(i);
        }
    }

    internal void MoveActive(bool gravityReversed, float fallSpeed, float reversedTargetY, float targetY, float deltaTime)
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                if (gravityReversed)
                {
                    // Gravity is reversed: enemies move up if spacing allows.
                    float step = fallSpeed * deltaTime;
                    float tentativeY = Mathf.MoveTowards(enemy.transform.position.y, reversedTargetY, step);
                    if (CanMoveUp(enemy, tentativeY))
                    {
                        enemy.transform.position = new Vector3(enemy.transform.position.x, tentativeY, enemy.transform.position.z);
                    }

                }
                else
                {
                    // Normal gravity: enemy falls down (if above targetY)
                    if (enemy.transform.position.y > targetY)
                    {
                        float step = fallSpeed * deltaTime;
                        float newY = Mathf.MoveTowards(enemy.transform.position.y, targetY, step);
                        enemy.transform.position = new Vector3(enemy.transform.position.x, newY, enemy.transform.position.z);
                    }
                }
            }
        }
    }

    internal bool CanMoveUp(GameObject currentEnemy, float newY)
    {
        foreach (GameObject otherEnemy in activeEnemies)
        {
            if (otherEnemy != null && otherEnemy != currentEnemy)
            {
                if (Mathf.Abs(otherEnemy.transform.position.y - newY) < 0.6f)
                    return false;
            }
        }
        return true;
    }

    internal bool CanSpawnEnemy(float spawnY)
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null && Mathf.Abs(enemy.transform.position.y - spawnY) < 0.6f)
                return false;
        }
        return true;
    }

    internal bool CanSpawnSpecialEnemyAt(float spawnX, float spawnY)
    {
        Vector2 candidatePos = new Vector2(spawnX, spawnY);
        float minDistance = 1.0f; // Adjust as needed

        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Vector2 enemyPos = new Vector2(enemy.transform.position.x, enemy.transform.position.y);
                if (Vector2.Distance(candidatePos, enemyPos) < minDistance)
                    return false;
            }
        }
        return true;
    }
}
