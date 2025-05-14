using System.Collections.Generic;
using UnityEngine;

public class AppleSpawner : MonoBehaviour
{
    public GameObject applePrefab;
    public Vector2Int gridSize = new Vector2Int(20, 20);
    public float cellSize = 1f;
    public float appleYOffset = 0.25f; // на сколько приподнять яблоко над полом
    public int maxAttempts = 50; // попытки найти свободное место

    void Start()
    {
        SpawnApple();
    }

    public void SpawnApple()
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-gridSize.x / 2, gridSize.x / 2) * cellSize,
                10f, // высота, откуда бросаем луч
                Random.Range(-gridSize.y / 2, gridSize.y / 2) * cellSize
            );

            // Raycast вниз, чтобы найти пол
            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 20f))
            {
                Vector3 spawnPos = hit.point + Vector3.up * appleYOffset;

                // Проверка — не внутри ли змейки
                if (!IsInsideSnake(spawnPos))
                {
                    Instantiate(applePrefab, spawnPos, Quaternion.identity);
                    return;
                }
            }
        }

        Debug.LogWarning("Не удалось найти свободное место для яблока.");
    }

    private bool IsInsideSnake(Vector3 pos)
    {
        // Ищем все объекты тела змейки по тегу
        GameObject[] snakeParts = GameObject.FindGameObjectsWithTag("SnakeBody");

        foreach (GameObject part in snakeParts)
        {
            if (Vector3.Distance(part.transform.position, pos) < cellSize * 0.5f)
                return true;
        }

        return false;
    }
}