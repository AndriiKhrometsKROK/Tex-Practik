using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum Sphere { Q, W, E }

public class HeroController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public TextMeshProUGUI spheresText;

    [Header("Spells")]
    public GameObject damagePrefab; // Засунь сюда синий квадрат для теста во все слоты!

    private Vector2 _targetPos;
    private bool _isMoving;
    private List<Sphere> _spheres = new List<Sphere>();

    void Start()
    {
        _targetPos = transform.position;
        UpdateUI();
    }

    void Update()
    {
        // Ходьба
        if (Input.GetMouseButtonDown(1))
        {
            _targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _isMoving = true;
        }

        // Ввод сфер
        if (Input.GetKeyDown(KeyCode.Q)) AddSphere(Sphere.Q);
        if (Input.GetKeyDown(KeyCode.W)) AddSphere(Sphere.W);
        if (Input.GetKeyDown(KeyCode.E)) AddSphere(Sphere.E);

        // Каст
        if (Input.GetKeyDown(KeyCode.R)) CastSpell();

        if (_isMoving) Move();
    }

    void Move()
    {
        transform.position = Vector2.MoveTowards(transform.position, _targetPos, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, _targetPos) < 0.1f) _isMoving = false;
    }

    void AddSphere(Sphere type)
    {
        _spheres.Add(type);
        if (_spheres.Count > 3) _spheres.RemoveAt(0);
        Debug.Log("Добавлена сфера: " + type); // СМОТРИ В КОНСОЛЬ
        UpdateUI();
    }

    void UpdateUI()
    {
        if (spheresText != null)
        {
            string s = "";
            foreach (var sphere in _spheres) s += sphere.ToString() + " ";
            spheresText.text = "Сферы: " + (s == "" ? "пусто" : s);
        }
    }

    void CastSpell()
    {
        if (_spheres.Count < 3)
        {
            Debug.Log("Мало сфер для каста!");
            return;
        }

        Debug.Log("Пытаюсь кастануть!");

        // Для теста просто спавним префаб в сторону мышки
        if (damagePrefab != null)
        {
            GameObject go = Instantiate(damagePrefab, transform.position, Quaternion.identity);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            Vector3 dir = mousePos - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        _spheres.Clear();
        UpdateUI();
    }
}