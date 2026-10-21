using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainShape
{
    public bool ApplyShape
    {
        get => _applyShape;
        set => _applyShape = value;
    }
    [Tooltip("If false, the shape will be ignored.")]
    [SerializeField] private bool _applyShape = true;
    
    public AudioShape Shape { get => _shape; set => _shape = value; }
    [Tooltip("Specifies the shape that the bands should be cast onto.")]
    [SerializeField] private AudioShape _shape;
    public enum AudioShape { Circle, Square, Diamond, AlignmentX, AlignmentZ }

    public float Strength { get => _strength; set => _strength = value; }
    [Tooltip("TODO")]
    [SerializeField]
    private float _strength;

    public float Zoom { get => _shapeZoom; set => _shapeZoom = value; }
    [Tooltip("Zoom of the shape. Will show more or less effect depending on the shape.")]
    [SerializeField] private float _shapeZoom = 1;
    
    public bool Invert { get => _invertShape; set => _invertShape = value; }
    [Tooltip("If true, the bands position on the shape will be inverted. If for example using a shape of AlignmentX, the first band will be located to the most right and the last band to the most left. " +
        "If inverted, the first band will be located to the most left and the last band to the most right.")]
    [SerializeField] private bool _invertShape;
    
    public AudioShapeOperation Operation { get => _operation; set => _operation = value; }
    [Tooltip("If more than one ShapeSetting is specified (for example in the TerrainGenerator), this variable will dictate what operation to perform from a current shape to its previous one. If only one shape" +
        " is specified, no operation will be applied and the values will be used rawly.")]
    [SerializeField] private AudioShapeOperation _operation;
    public enum AudioShapeOperation { Override, /*Multiply,*/ Add, Subtract, Maximum, Minimum, Average }
    

    public bool ApplyCustomAudioBandWidth { get => _applyCustomAudioBandWidth; set => _applyCustomAudioBandWidth = value; }
    [Header("- Band Widths -")]
    [Tooltip("If true, the width of the bands will be altered according to the specified _audioBandWidthCurve.")]
    [SerializeField] private bool _applyCustomAudioBandWidth = true;
    [Tooltip("Curve that dictates the general width of the bands. f(0) is the factor for the first band, f(1) is the factor for the last band. Make sure that the curve does not go beneath f(x)=1. This feature" +
        " was implemented quickly, so its usage might be a bit quirky.")]
    [SerializeField] private AnimationCurve _audioBandWidthCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 2), new Keyframe(1, 1) });
    

    /// <summary>
    /// Changes whenever a setting that has an effect on <see cref="GetNormalizedBandIndexByShape"/> changes, so results can be cached.
    /// Calculates without allocating, it is meant to be called every frame.
    /// </summary>
    public int GetIndexSettingsHash()
    {
        unchecked
        {
            int hash = _applyShape ? 1 : 0;
            hash = hash * 31 + (int)_shape;
            hash = hash * 31 + _shapeZoom.GetHashCode();
            hash = hash * 31 + (_invertShape ? 1 : 0);
            hash = hash * 31 + (_applyCustomAudioBandWidth ? 1 : 0);
            if (_applyCustomAudioBandWidth && _audioBandWidthCurve != null)
            {
                hash = hash * 31 + _audioBandWidthCurve.length;
                for (int i = 0; i < _audioBandWidthCurve.length; i++)
                {
                    Keyframe key = _audioBandWidthCurve[i];
                    hash = hash * 31 + key.time.GetHashCode();
                    hash = hash * 31 + key.value.GetHashCode();
                    hash = hash * 31 + key.inTangent.GetHashCode();
                    hash = hash * 31 + key.outTangent.GetHashCode();
                }
            }
            return hash;
        }
    }

    public float GetNormalizedBandIndexByShape(float x, float z)
    {
        float indexNormalized = GetIndexByShape(x, z);

        if (_applyCustomAudioBandWidth)
            indexNormalized *= 1 / _audioBandWidthCurve.Evaluate(indexNormalized);

        if (indexNormalized < 0 || indexNormalized > 1)
            indexNormalized = -1;
        else if (_invertShape) // apply _invertShape only if not index out of range
            indexNormalized = 1 - indexNormalized;

        return indexNormalized;
    }

    private float GetIndexByShape(float x, float z)
    {
        float indexNormalized = 0;

        if (_shape == AudioShape.AlignmentX)
            indexNormalized = x;
        else if (_shape == AudioShape.Circle)
        {
            float distanceXNormalized = Mathf.Abs(0.5f - x);
            float distanceZNormalized = Mathf.Abs(0.5f - z);
            float distanceNormalized = Mathf.Sqrt(distanceXNormalized * distanceXNormalized + distanceZNormalized * distanceZNormalized);
            indexNormalized = distanceNormalized / 0.7071067f * _shapeZoom;
        }
        else if (_shape == AudioShape.AlignmentZ)
            indexNormalized = z;
        else if (_shape == AudioShape.Diamond)
        {
            float distanceXNormalized = Mathf.Abs(0.5f - x);
            float distanceZNormalized = Mathf.Abs(0.5f - z);
            float distanceNormalized = (distanceXNormalized + distanceZNormalized) * 2;
            indexNormalized = distanceNormalized * _shapeZoom;
        }
        else if (_shape == AudioShape.Square)
            indexNormalized = _shapeZoom * Mathf.Max(Mathf.Abs(0.5f - x), Mathf.Abs(0.5f - z)) * 2;

        return indexNormalized;
    }
}
