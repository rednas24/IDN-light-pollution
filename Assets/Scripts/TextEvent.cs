using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TextEvent : MonoBehaviour
{
    [SerializeField, TextArea(2, 4)] private string[] pages;
    [SerializeField] private bool onlyOnce = true;

    private bool triggered;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((onlyOnce && triggered) || !other.TryGetComponent(out FirstPersonController _))
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
