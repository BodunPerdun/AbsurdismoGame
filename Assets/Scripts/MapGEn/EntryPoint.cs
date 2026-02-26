using UnityEngine;

public enum SocketType { Road, Building }

public class EntryPoint : MonoBehaviour {
    public SocketType type;
    public bool isOccupied = false;

    private void OnDrawGizmos() {
        Gizmos.color = (type == SocketType.Road) ? Color.blue : Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
}