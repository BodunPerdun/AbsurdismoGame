using UnityEngine;
using UnityEngine.Events;
using DG.Tweening; // Используем наш DOTween для сочности

public class threeDButton : MonoBehaviour
{
    public UnityEvent onClick;
    public Vector3 pressDepth = new Vector3(0, -0.2f, 0); // Куда вдавливается кнопка

    private Vector3 originalPos;

    void Start() => originalPos = transform.localPosition;

   
    void OnMouseDown()
    {
       
    }


    void OnMouseUp()
    {
        onClick.Invoke(); // Запускаем действие (например, переход в игру)
    }



}