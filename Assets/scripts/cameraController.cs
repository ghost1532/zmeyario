using UnityEngine;

/// <summary>
/// Контролирует камеру, следующую за целью (головой змейки).
/// Позволяет настраивать высоту, плавность и переключаться в режим, подобный первому лицу.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Настройки Цели")]
    [Tooltip("Объект, за которым будет следовать камера (голова змейки).")]
    public Transform target; // Голова змейки (цель камеры)

    [Tooltip("Ссылка на скрипт контроллера змейки для получения направления движения.")]
    public SnakeController snakeController; // Ссылка на скрипт контроллера змейки

    [Header("Позиционирование Камеры")]
    [Tooltip("Желаемая высота камеры над целью в режиме вида сверху.")]
    public float height = 5f; // Желаемая высота камеры над целью

    [Tooltip("Смещение от точки опоры цели в режиме от первого лица (локальные координаты). X: вбок, Y: вверх/вниз, Z: вперед/назад.")]
    public Vector3 firstPersonOffset = new Vector3(0f, 0.2f, 0.3f); // например, немного выше и впереди точки опоры

    [Tooltip("Высота, на уровне или ниже которой камера переключается в режим, подобный первому лицу.")]
    public float firstPersonThreshold = 0.5f;

    [Header("Плавность Движения")]
    [Tooltip("Скорость плавного изменения позиции камеры.")]
    public float positionSmoothSpeed = 8f; // Скорость плавного изменения позиции (для Lerp)

    [Tooltip("Скорость плавного изменения поворота камеры.")]
    public float rotationSmoothSpeed = 8f; // Скорость плавного изменения поворота (для Slerp)

    void Start()
    {
        // Проверка наличия цели
        if (target == null)
        {
            Debug.LogError("Цель для камеры (Target) не назначена в инспекторе!");
            enabled = false; // Отключаем скрипт, если цель не назначена
            return;
        }

        // Проверка наличия контроллера змейки
        if (snakeController == null)
        {
            // Пытаемся получить компонент SnakeController с объекта цели, если он не назначен явно
            snakeController = target.GetComponent<SnakeController>();
            if (snakeController == null)
            {
                Debug.LogError("SnakeController не назначен в инспекторе и не найден на объекте цели!");
                enabled = false; // Отключаем скрипт, если контроллер змейки не найден
                return;
            }
        }
        // Начальная установка позиции и поворота камеры без сглаживания
        UpdateCameraState(true);
    }

    // LateUpdate вызывается после всех вызовов Update, что хорошо для логики камеры
    void LateUpdate()
    {
        // Если цель или контроллер отсутствуют, ничего не делаем
        if (target == null || snakeController == null) return;

        // Обновляем состояние камеры с плавным переходом
        UpdateCameraState(false);
    }

    /// <summary>
    /// Обновляет позицию и поворот камеры.
    /// </summary>
    /// <param name="instant">Если true, обновление происходит мгновенно, без плавности.</param>
    void UpdateCameraState(bool instant = false)
    {
        // Получаем нормализованное направление движения змейки
        Vector3 snakeWorldForward = snakeController.moveDirection.normalized;

        // Запасной вариант, если moveDirection равен нулю (например, на старте или если змейка не движется)
        if (snakeWorldForward == Vector3.zero)
        {
            snakeWorldForward = target.forward; // Используем фактическое направление вперед объекта цели
        }
        // Дополнительный запасной вариант, если и target.forward нулевой (маловероятно для движущегося объекта)
        if (snakeWorldForward == Vector3.zero)
        {
            snakeWorldForward = Vector3.forward; // По умолчанию используем мировое направление вперед (ось Z)
        }

        Vector3 desiredPosition;
        Quaternion desiredRotation;
// Определяем режим камеры (от первого лица или вид сверху)
        if (height <= firstPersonThreshold)
        {
            // --- Режим от первого лица или очень близкий вид ---
            // Позиционируем камеру относительно локального пространства цели, используя firstPersonOffset
            desiredPosition = target.TransformPoint(firstPersonOffset);

            // Камера смотрит в направлении движения змейки (или ее текущей ориентации вперед)
            // target.up используется как подсказка для вертикальной ориентации камеры,
            // чтобы 'верх' камеры совпадал с 'верхом' змейки.
            desiredRotation = Quaternion.LookRotation(snakeWorldForward, target.up);
        }
        else
        {
            // --- Режим вида сверху ---
            // Камера располагается прямо над целью на заданной высоте
            desiredPosition = target.position + Vector3.up * height;

            // Камера смотрит вниз на цель.
            // Ее 'верх' (локальная ось Y камеры) ориентируется по направлению движения змейки (snakeWorldForward).
            // Это позволяет камере поворачиваться вместе со змейкой, сохраняя вид сверху.
            Vector3 directionToTarget = (target.position - desiredPosition).normalized;
            if (directionToTarget == Vector3.zero)
            {
                directionToTarget = -Vector3.up; // Избегаем нулевого вектора, если камера уже в позиции цели
            }
            desiredRotation = Quaternion.LookRotation(directionToTarget, snakeWorldForward);
        }

        // Применяем позицию и поворот с плавностью или мгновенно
        if (instant)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
        }
        else
        {
            // Плавное изменение позиции
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * Time.deltaTime);
            // Плавное изменение поворота
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }
}