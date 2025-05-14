using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SnakeController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public GameObject bodyPrefab;
    public float bodySpacing = 0.5f;

    private List<Transform> bodyParts = new List<Transform>();
    private List<Vector3> positionsHistory = new List<Vector3>();

    private Vector3 moveDirection = Vector3.forward; // Стартовое направление (вперёд)

    void Start()
    {
        //ставит змейку на пол
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f))
        {
            float yOffset = GetComponent<Renderer>().bounds.extents.y;
            transform.position = new Vector3(transform.position.x, hit.point.y + yOffset, transform.position.z);
        }
        
        //изначально удлинняет хвост
        AddBodyPart();
        AddBodyPart();
        AddBodyPart();
        
        positionsHistory.Add(transform.position);
    }

    void Update()
    {
        HandleInput();

        // Двигаем голову
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Запоминаем позицию головы
        if (Vector3.Distance(transform.position, positionsHistory[0]) > bodySpacing)
        {
            positionsHistory.Insert(0, transform.position);
        }

        // Двигаем тело
        for (int i = 0; i < bodyParts.Count; i++)
        {
            Vector3 targetPos = positionsHistory[Mathf.Min(i * Mathf.RoundToInt(bodySpacing / 0.1f), positionsHistory.Count - 1)];
            bodyParts[i].position = Vector3.Lerp(bodyParts[i].position, targetPos, Time.deltaTime * moveSpeed);
        }

        // Добавление тела
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddBodyPart();
        }
        
        // Проверка: врезалась ли голова в тело
        foreach (Transform part in bodyParts)
        {
            if (Vector3.Distance(transform.position, part.position) < 0.4f)
            {
                Die();
                return;
            }
            
            // Проверка: есть ли земля под головой
            if (!Physics.Raycast(transform.position, Vector3.down, 1f))
            {
                Die();
                return;
            }
        }
    }

    void HandleInput()
    {
        // Меняем направление только по одной оси, чтобы избежать диагоналей
        if (Input.GetKeyDown(KeyCode.W) && moveDirection != Vector3.back)
            moveDirection = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S) && moveDirection != Vector3.forward)
            moveDirection = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.A) && moveDirection != Vector3.right)
            moveDirection = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D) && moveDirection != Vector3.left)
            moveDirection = Vector3.right;

        // ↑ эта проверка запрещает разворот назад (как в классической змейке)
    }

    void AddBodyPart()
    {
        Vector3 spawnPos = bodyParts.Count > 0 ?
            bodyParts[bodyParts.Count - 1].position :
            transform.position - moveDirection * bodySpacing;

        GameObject newPart = Instantiate(bodyPrefab, spawnPos, Quaternion.identity);
        bodyParts.Add(newPart.transform);
    }

    void Die()
    {
        SceneManager.LoadScene("menu");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Apple"))
        {
            Destroy(other.gameObject); // съесть яблоко
            AddBodyPart();             // удлиниться

            FindObjectOfType<AppleSpawner>().SpawnApple(); // заспавнить новое
        }
    }

}
