using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TextEvent : MonoBehaviour
{
    [SerializeField, TextArea(2, 4)] private string[] pages;
    [SerializeField] private bool onlyOnce = true;
    [SerializeField, Range(0f, 1f)] private float minSkyBrightness;
    [SerializeField] private bool lookUp;
    [SerializeField] private AudioClip sound;

    private float lookUpPitch = -55f;
    private float lookUpTime = 6f;

    private LightPollutionController sky;
    private AudioSource audioSource;
    private bool triggered;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Awake()
    {
        sky = FindAnyObjectByType<LightPollutionController>();
        if (sound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((onlyOnce && triggered) || !other.TryGetComponent(out FirstPersonController player))
            return;
        if (sky != null && 1f - sky.Pollution < minSkyBrightness)
            return;

        triggered = true;
        if (audioSource != null)
            audioSource.PlayOneShot(sound);

        if (lookUp)
            StartCoroutine(LookUp(player));
        else
            TextBox.Show(pages);
    }

    private IEnumerator LookUp(FirstPersonController player)
    {
        player.CanMove = false;
        player.CanLook = false;

        float start = player.Pitch;
        for (float time = 0f; time < lookUpTime; time += Time.deltaTime)
        {
            player.Pitch = Mathf.Lerp(start, lookUpPitch, Mathf.SmoothStep(0f, 1f, time / lookUpTime));
            yield return null;
        }
        player.Pitch = lookUpPitch;

        player.CanLook = true;
        TextBox.Show(pages);
    }

    private void OnDrawGizmos()
    {
        BoxCollider zone = GetComponent<BoxCollider>();
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.2f);
        Gizmos.DrawCube(zone.center, zone.size);
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(zone.center, zone.size);
    }
}
