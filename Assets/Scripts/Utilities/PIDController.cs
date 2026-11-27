using System;
using UnityEngine;

namespace Pulsar.Utilities
{
//Tuning values
[Serializable]
public struct PIDParams
{
    /*
     * Proportional output of the current error, AKA where its going,
     * higher means faster response time
     * Expected range, 0.1 to 100
     * too low it doesnt reach its point, too passes the point and oscillates
     */
    [Tooltip("Acceleration intensity")]
    [Min(0.1f)]
    public float kP;

    /*
     * Derivative output of the current error,
     * how much we need to break to get to the point
     * Expected range, 0.001 to 10
     * Too low it doesn't break hard enough, too high it slows down too much
     */
    [Tooltip("Braking against velocity")]
    [Min(0.001f)]
    public float kD;

    /*
     * Integral output of the error from external factors to account for
     * Expected range, 0.001 to 1.0 (how sharp it accounts in seconds)
     * higher eliminates outside forces faster but oscillates like kP
     */
    [Tooltip("Account for external forces")]
    [Min(0.001f)]
    public float kI;

    /*
     * The integral can accumulate and get crazy values and needs to be clamped
     * allows for a bit more dynamic control of the value.
     * 1f should normally work tho, but its included everywhere so better safe
     */
    [Tooltip("Prevent integral accumulation")]
    [Min(1f)]
    public float IntegralWindup;

    public PIDParams(float kP, float kD, float kI, float integralWindup = 1f)
    {
        this.kP = kP;
        this.kD = kD;
        this.kI = kI;
        this.IntegralWindup = integralWindup;
    }
}

public class PIDController
{
    private float _integral;
    private float _oldError;
    private bool _hasOldError;

    public void Reset()
    {
        _integral = 0f;
        _oldError = 0f;
        _hasOldError = false;
    }

    //Unit Vector
    public float CalculateDir(in PIDParams p, float deltaTime, float error)
    {
        if (deltaTime <= 0f)
        {
            return 0f;
        }

        CalcIntegral(error * deltaTime, p.IntegralWindup);

        float deriv = _hasOldError ? (error - _oldError) / deltaTime : 0f;
        _oldError = error;
        _hasOldError = true;

        return Mathf.Clamp(p.kP * error + p.kI * _integral + p.kD * deriv,
            -1f, 1f);
    }

    public float CalculateVelocity(
        in PIDParams p,
        float deltaTime,
        float error,
        float currentVelocity)
    {
        if (deltaTime <= 0f)
        {
            return 0f;
        }

        CalcIntegral(error * deltaTime, p.IntegralWindup);

        float deriv = -currentVelocity;
        _oldError = error;
        _hasOldError = true;

        return Mathf.Clamp(
            p.kP * error + p.kI * _integral + p.kD * deriv, -1f, 1f);
    }

    private void CalcIntegral(float errorOverTime, float windUp)
    {
        _integral = Mathf.Clamp(_integral + errorOverTime,
            -windUp, windUp);
    }
}
}
