using UnityEngine;

namespace Pulsar.Utilities
{

//Tuning values
public struct PIDParams
{
    /*
     * Proportional output of the current error, AKA where its going,
     * higher means faster response time
     * Expected range, 0.1 to 100
     * too low it doesnt reach its point, too passes the point and oscillates
     */
    private float _kP;
    /*
     * Derivative output of the current error,
     * how much we need to break to get to the point
     * Expected range, 0.001 to 10
     * Too low it doesn't break hard enough, too high it slows down too much
     */
    private float _kD;
    /*
     * Integral output of the error from external factors to account for
     * Expected range, 0.001 to 1.0 (how sharp it accounts in seconds)
     * higher eliminates outside forces faster but oscillates like kP
     */
    private float _kI;

    /*
     * The integral can accumulate and get crazy values and needs to be clamped
     * allows for a bit more dynamic control of the value.
     * 1f should normally work tho, but its included everywhere so better safe
     */
    private float _integralWindup;

    public PIDParams(float kP, float kD, float kI, float integralWindup = 1f)
    {
        _kP = kP;
        _kD = kD;
        _kI = kI;
        _integralWindup = integralWindup;
    }
}

    public class PIDController : MonoBehaviour
{
    private float _integral;
    private float _error;
    private bool _hasError;

    public float CalculateDir(in PIDParams p)
    {
        //TODO
        Debug.Log("NOT DONE");
        return -1f;
    }

    public float CalculateAngle(in PIDParams p)
    {
        //TODO
        Debug.Log("NOT DONE");
        return -1f;
    }
}
}
