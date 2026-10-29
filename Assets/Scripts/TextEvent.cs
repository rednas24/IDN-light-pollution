using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TextEvent : MonoBehaviour
{
    [SerializeField, TextArea(2, 4)] private string[] pages;
    [SerializeField] private bool onlyOnce = true;
    [SerializeField, Range(0f, 1f)] private float minSkyBrightness;

    private LightPollutionController sky;
    private bool triggered;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Awake()
    {
        sky = FindAnyObjectByType<LightPollutionController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((onlyOnce && triggered) || !other.TryGetComponent(out FirstPersonController _))
            return;
        if (sky != null && sky.Pollution < minSkyBrightness)
            return;

        triggered = true;
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
