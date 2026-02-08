using UnityEngine;

public class AnimationSound : MonoBehaviour
{
    private AudioSource audioSource;

    void Start() {
        audioSource = GetComponent<AudioSource>();
    }

    // Эту функцию мы выберем в анимационном событии
    public void PlayAnimationSound() {
        audioSource.Play();
    }
}