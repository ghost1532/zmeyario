// Файл: SnakeController.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SnakeController : MonoBehaviour
{
    [Header("Настройки Движения")]
    public float moveSpeed = 5f;
    public float headRotationSpeed = 10f; // Скорость поворота головы

    [Header("Настройки Тела")]
    public GameObject bodyPrefab;
    public float bodySpacing = 0.5f; // Желаемое расстояние между сегментами

    [Header("Публичные Состояния (для других скриптов)")]
    public Vector3 moveDirection = Vector3.forward; // Стартовое и текущее целевое направление движения

    private List<Transform> bodyParts = new List<Transform>();
    private List<Vector3> positionsHistory = new List<Vector3>();
    private float distanceToRecordPosition;

    void Start()
    {
        // 1. Позиционируем змейку на полу
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f))
        {
            Renderer headRenderer = GetComponent<Renderer>();
            float yOffset = 0.1f; // Значение по умолчанию, если нет Renderer или Collider
            if (headRenderer != null) {
                yOffset = headRenderer.bounds.extents.y;
            } else {
                Collider headCollider = GetComponent<Collider>();
                if (headCollider != null) {
                    yOffset = headCollider.bounds.extents.y;
                }
            }
            transform.position = new Vector3(transform.position.x, hit.point.y + yOffset, transform.position.z);
        }

        // 2. Ориентируем голову в начальном направлении движения *перед* созданием тела
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
        else // На случай, если moveDirection не инициализирован (хотя он имеет значение по умолчанию)
        {
            moveDirection = Vector3.forward; // Устанавливаем значение по умолчанию, если его нет
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
        
        // 3. Добавляем начальные сегменты тела. Теперь они будут использовать корректное transform.forward головы.
        AddBodyPart();
        AddBodyPart();
        AddBodyPart();
        
        // 4. Записываем начальную позицию головы в историю
        positionsHistory.Insert(0, transform.position);

        // 5. Настраиваем дистанцию для записи точек в историю
        distanceToRecordPosition = bodySpacing / 2f;
        if (distanceToRecordPosition < 0.01f) distanceToRecordPosition = 0.01f; // Минимальная дистанция
    }

    void Update()
    {
        HandleInput(); // Обработка ввода для изменения moveDirection

        // Поворачиваем голову змейки в сторону целевого moveDirection
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetHeadRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetHeadRotation, headRotationSpeed * Time.deltaTime);
        }

        // Двигаем голову всегда вперед относительно ее текущей ориентации
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // Запоминаем позицию головы, если она достаточно сместилась
        if (positionsHistory.Count == 0 || Vector3.Distance(transform.position, positionsHistory[0]) >= distanceToRecordPosition)
        {
            positionsHistory.Insert(0, transform.position);
            int maxHistoryPoints = Mathf.CeilToInt((bodyParts.Count + 5) * bodySpacing / distanceToRecordPosition) * 2;
            if (positionsHistory.Count > Mathf.Max(maxHistoryPoints, 50))
            {
                positionsHistory.RemoveAt(positionsHistory.Count - 1);
            }
        }

        // Двигаем и поворачиваем тело
        for (int i = 0; i < bodyParts.Count; i++)
        {
            int historyIndex = Mathf.FloorToInt((i + 1) * bodySpacing / distanceToRecordPosition);
            historyIndex = Mathf.Clamp(historyIndex, 0, positionsHistory.Count - 1);

            if (historyIndex < positionsHistory.Count)
            {
                Vector3 targetPos = positionsHistory[historyIndex];
                bodyParts[i].position = Vector3.Lerp(bodyParts[i].position, targetPos, moveSpeed * 1.5f * Time.deltaTime);

                Vector3 lookAtTarget;
                if (historyIndex > 0 && historyIndex -1 < positionsHistory.Count) {
                     lookAtTarget = positionsHistory[historyIndex-1];
                } else {
                     lookAtTarget = transform.position; 
                }
                if (Vector3.Distance(bodyParts[i].position, lookAtTarget) > 0.01f) {
                    Quaternion targetSegmentRotation = Quaternion.LookRotation(lookAtTarget - bodyParts[i].position);
                    bodyParts[i].rotation = Quaternion.Slerp(bodyParts[i].rotation, targetSegmentRotation, headRotationSpeed * Time.deltaTime);
                }
            }
        }
        
        // Проверка столкновений и падения (логика остается прежней)
        for (int i = 2; i < bodyParts.Count; i++) 
        {
            if (Vector3.Distance(transform.position, bodyParts[i].position) < bodySpacing * 0.8f)
            {
                Die();
                return;
            }
        }
        if (!Physics.Raycast(transform.position, Vector3.down, 1.5f))
        {
            Die();
            return;
        }
    }

    void HandleInput()
    {
        // Получаем текущее фактическое направление "вперед" и "вправо" для головы змейки
        // Проецируем на плоскость XZ и нормализуем, чтобы избежать влияния наклона головы
        Vector3 headForward = transform.forward;
        headForward.y = 0;
        headForward.Normalize();

        Vector3 headRight = transform.right;
        headRight.y = 0;
        headRight.Normalize();

        // Если headForward или headRight стали нулевыми (например, если змейка смотрит строго вверх/вниз),
        // используем предыдущее moveDirection как базу для относительного поворота.
        if (headForward == Vector3.zero) headForward = moveDirection; // Используем последнее известное хорошее направление
        // headRight можно пересчитать из headForward: headRight = Vector3.Cross(Vector3.up, headForward).normalized;
        // Но для простоты, если headForward валиден, headRight тоже должен быть валиден после проекции.

        Vector3 newDesiredDirection = Vector3.zero; // Направление, выбранное игроком

        if (Input.GetKeyDown(KeyCode.W))
        {
            newDesiredDirection = headForward; // Двигаться вперед относительно текущего направления головы
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            newDesiredDirection = -headForward; // Двигаться назад относительно текущего направления головы
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            newDesiredDirection = -headRight; // Двигаться влево относительно текущего направления головы
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            newDesiredDirection = headRight;  // Двигаться вправо относительно текущего направления головы
        }

        if (newDesiredDirection != Vector3.zero)
        {
            // Привязываем выбранное направление к ближайшей мировой оси (кардинальное направление)
            // Это сохраняет "змеиный" характер движения по сетке или основным направлениям.
            float dotForward = Vector3.Dot(newDesiredDirection, Vector3.forward);
            float dotBack = Vector3.Dot(newDesiredDirection, Vector3.back);
            float dotLeft = Vector3.Dot(newDesiredDirection, Vector3.left);
            float dotRight = Vector3.Dot(newDesiredDirection, Vector3.right);

            float maxDot = Mathf.Max(Mathf.Abs(dotForward), Mathf.Abs(dotBack), Mathf.Abs(dotLeft), Mathf.Abs(dotRight));

            Vector3 snappedDirection = moveDirection; // По умолчанию сохраняем текущее направление

            if (Mathf.Approximately(maxDot, Mathf.Abs(dotForward))) snappedDirection = (dotForward > 0) ? Vector3.forward : Vector3.back;
            else if (Mathf.Approximately(maxDot, Mathf.Abs(dotLeft))) snappedDirection = (dotLeft > 0) ? Vector3.left : Vector3.right;
            // Замечание: Логика выше немного упрощена. Более точное определение:
            // if (Mathf.Abs(dotForward) == maxDot) snappedDirection = (dotForward > 0) ? Vector3.forward : Vector3.back;
            // else if (Mathf.Abs(dotBack) == maxDot) snappedDirection = (dotBack > 0) ? Vector3.back : Vector3.forward; // Это условие избыточно из-за Abs
            // else if (Mathf.Abs(dotLeft) == maxDot) snappedDirection = (dotLeft > 0) ? Vector3.left : Vector3.right;
            // else if (Mathf.Abs(dotRight) == maxDot) snappedDirection = (dotRight > 0) ? Vector3.right : Vector3.left;

            // Более простой и надежный способ привязки:
            if (Mathf.Abs(newDesiredDirection.x) > Mathf.Abs(newDesiredDirection.z)) { // Движение больше по X
                snappedDirection = new Vector3(Mathf.Sign(newDesiredDirection.x), 0, 0);
            } else { // Движение больше по Z
                snappedDirection = new Vector3(0, 0, Mathf.Sign(newDesiredDirection.z));
            }


            // Правило против разворота на 180 градусов:
            // Новое направление не должно быть противоположно текущему moveDirection.
            // (moveDirection - это то, куда голова УЖЕ стремится или движется)
            // Допускаем разворот, если тела еще нет.
            if (snappedDirection != -moveDirection || bodyParts.Count == 0)
            {
                moveDirection = snappedDirection;
            }
        }
    }

    void AddBodyPart()
    {
        Transform parentTransform = (bodyParts.Count > 0) ? bodyParts[bodyParts.Count - 1] : transform;
        Vector3 spawnPos = parentTransform.position - parentTransform.forward * bodySpacing;
        Quaternion spawnRot = parentTransform.rotation;
        GameObject newPart = Instantiate(bodyPrefab, spawnPos, spawnRot);
        bodyParts.Add(newPart.transform);
    }

    void Die()
    {
        Debug.Log("Игра окончена!"); 
        SceneManager.LoadScene("menu");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Apple"))
        {
            Destroy(other.gameObject); 
            AddBodyPart();            
            AppleSpawner spawner = FindObjectOfType<AppleSpawner>();
            if (spawner != null) spawner.SpawnApple(); 
            else Debug.LogWarning("AppleSpawner не найден на сцене!");
        }
    }
}
