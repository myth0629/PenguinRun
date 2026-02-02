using UnityEngine;

public class GameSpeedController : MonoBehaviour
{
    [SerializeField] private float defaultSpeed = 6.5f;

    public static float Speed { get; private set; } = 6.5f;

    private void Awake()
    {
        if (Speed <= 0f)
        {
            Speed = defaultSpeed;
        }
    }

    private void OnValidate()
    {
        if (defaultSpeed > 0f)
        {
            Speed = defaultSpeed;
        }
    }

    public static void SetSpeed(float newSpeed)
    {
        Speed = Mathf.Max(0f, newSpeed);
    }
}
