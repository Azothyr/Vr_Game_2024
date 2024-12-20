using System.Collections;
using UnityEngine;

public class ObjectMotion : MonoBehaviour
{
    public enum WaveType { Sine, Cosine }
    [System.Flags] public enum MotionType {
        None = 0, // 0b0000, decimal 0: Default value
        Position = 1 << 0, // 0b0001, decimal 1
        Rotation = 1 << 1, // 0b0010, decimal 2
        Scale = 1 << 2 // 0b0100, decimal 3
    }
    [System.Flags] public enum Axis
    {
        None = 0, // 0b0000, decimal 0: Default value
        X = 1 << 0, // 0b0001, decimal 1
        Y = 1 << 1, // 0b0010, decimal 2
        Z = 1 << 2,  // 0b0100, decimal 3
    }
    
    [Header("Motion Settings")]
    public bool startOnLoad;
    [Tooltip("Select the types of motion to apply: Position - translation along Position Axis, Rotation - constant rotation along Rotation Axis, or Scale - scaling along Scale Axis.")]
    public MotionType motionType;
    
    [Header("Position Settings")]
    [Tooltip("Choose whether position wave is based on a sine or cosine function - sine starts at vector3(0, 0, 0), cosine starts at vector3(axis-max, axis-max, axis-max) axis-max is the value of the positionAmplitude or 0 depending on the Position Axis Selected.")]
    public WaveType positionWaveType;
    [Tooltip("Select the axis for positional motion.")]
    public Axis positionAxis;
    [Tooltip("positionAmplitude of the wave - the height of the wave's peak.")]
    public float positionAmplitude = 1.0f;
    [Tooltip("positionFrequency of the wave - how fast the wave oscillates.")]
    public float positionFrequency = 1.0f;
    [Tooltip("Phase offset of the wave - shifts the wave along the time axis.")]
    public float positionOffset;
    
    [Header("Rotation Settings")]
    [Tooltip("Select the axis for rotational motion.")]
    public Axis rotationAxis;
    [Range(-359, 359), Tooltip("Speed of rotation around the rotation-axis in degrees per second.")]
    public float rotationSpeed;
    public Vector3 rotationOffset;
    
    [Header("Scale Settings")]
    [Tooltip("Choose whether position wave is based on a sine or cosine function - sine starts at vector3(0, 0, 0), cosine starts at vector3(axis-max, axis-max, axis-max) axis-max is the value of the positionAmplitude or 0 depending on the Scale Axis Selected.")]
    public WaveType scaleWaveType;
    [Tooltip("Select the axis for scaling motion.")]
    public Axis scaleAxis;
    [Tooltip("positionAmplitude of the scale wave - the height of the wave's peak.")]
    public float pulseAmplitude = 0.1f;
    [Tooltip("positionFrequency of the scale wave - how fast the wave oscillates.")]
    public float pulseFrequency = 1.0f;
    [Tooltip("Pulse offset of the wave - shifts the wave along the time axis.")]
    [Range(-1, 1)] public float pulseOffset;

    private Vector3 _startPosition, _startScale;
    private Coroutine _motionRoutine, _rotationRoutine, _scalingRoutine;

    private void OnEnable()
    {
        if (startOnLoad) StartAllMotion();
    }
    private void OnDisable() => StopAllMotion();
    private void OnDestroy() => StopAllMotion();

    public void StartMotion()
    {
        if (_motionRoutine != null) return;
        _startPosition = transform.position;
        
        _motionRoutine = StartCoroutine(PositionMotion());
    }

    public void StopMotion()
    {
        if (_motionRoutine == null) return;
        
        StopCoroutine(_motionRoutine);
        _motionRoutine = null;
        transform.position = _startPosition;
    }

    public void StartRotation() { _rotationRoutine ??= StartCoroutine(RotateMotion()); }

    public void StopRotation()
    {
        if (_rotationRoutine == null) return;
        
        StopCoroutine(_rotationRoutine);
        _rotationRoutine = null;
    }

    public void StartScaling()
    {
        if (_scalingRoutine != null) return;
        _startScale = transform.localScale;
        
        _scalingRoutine = StartCoroutine(ScaleMotion());
    }

    public void StopScaling()
    {
        if (_scalingRoutine == null) return;
        
        StopCoroutine(_scalingRoutine);
        _scalingRoutine = null;
        transform.localScale = _startScale;
    }
    
    public void StartAllMotion()
    {
        if (motionType.HasFlag(MotionType.Position)) StartMotion();
        if (motionType.HasFlag(MotionType.Rotation)) StartRotation();
        if (motionType.HasFlag(MotionType.Scale)) StartScaling();
    }
    
    public void StopAllMotion()
    {
        StopMotion();
        StopRotation();
        StopScaling();
    }
    
    private IEnumerator PositionMotion()
    {
        while (true)
        {
            float baseEquation = GetBaseEquation(Time.time, positionFrequency, positionOffset);
            float waveFunction = InvokePositionWaveTypeFunction(baseEquation);
            float displacement = GetDisplacement(waveFunction, positionAmplitude);
            var newPosition = _startPosition + GetVectorFromAxes(positionAxis) * displacement;
            transform.position = newPosition;
            yield return null;
        }
    }

    private IEnumerator RotateMotion()
    {
        while (true)
        {
            transform.Rotate(GetVectorFromAxes(rotationAxis) + rotationOffset, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator ScaleMotion()
    {
        while (true)
        {
            float baseEquation = GetBaseEquation(Time.time, pulseFrequency, pulseOffset);
            float waveFunction = scaleWaveType == WaveType.Sine ? Mathf.Sin(baseEquation) : Mathf.Cos(baseEquation);
            float scaleAmount = GetDisplacement(waveFunction, pulseAmplitude);
            transform.localScale = _startScale + GetVectorFromAxes(scaleAxis) * scaleAmount;
            yield return null;
        }
    }

    private static float GetBaseEquation(float time, float freq, float offset)
    {
        return time * freq * 2 * Mathf.PI + offset;
    }

    private float InvokePositionWaveTypeFunction(float baseEquation)
    {
        return positionWaveType == WaveType.Sine ? Mathf.Sin(baseEquation) : Mathf.Cos(baseEquation);
    }

    private static float GetDisplacement(float waveFunction, float amp)
    {
        return waveFunction * amp;
    }

    private static Vector3 GetVectorFromAxes(Axis axes)
    {
        var result = Vector3.zero;
        
        if (axes.HasFlag(Axis.X))
            result += Vector3.right;
            
        if (axes.HasFlag(Axis.Y))
            result += Vector3.up;
            
        if (axes.HasFlag(Axis.Z))
            result += Vector3.forward;

        return result;
    }
}