using UnityEngine;

public class Sensors : MonoBehaviour
{
    public bool IsGrounded => CheckIfGrounded();
    
    [SerializeField] private Transform _groundSensor;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckRadius;

    private bool CheckIfGrounded()
    {
        return Physics2D.OverlapCircle(_groundSensor.position, _groundCheckRadius, _groundLayer);
    }
}
