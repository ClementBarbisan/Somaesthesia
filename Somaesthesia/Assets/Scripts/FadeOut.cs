using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FadeOut : MonoBehaviour
{
    [SerializeField] private float _speed = 1f;

    private TextMeshProUGUI _text;
    // Start is called before the first frame update
    void OnEnable()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_text.color.a <= 0)
        {
            return;
        }

        Color col = _text.color;
        col.a -= Time.deltaTime * (1 / _speed);
        _text.color = col;
    }
}
