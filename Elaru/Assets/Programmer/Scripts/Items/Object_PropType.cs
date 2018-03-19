using System;
using System.Linq;
using UnityEngine;

public enum PropType
{
    None,
    Concrete,
    Paper,
    Rubber,
    Glass,
    Metal,
}

[CreateAssetMenu(fileName = "PropTypes", menuName = "ELARU/Prop Types", order = 1)]
public class Object_PropType : ScriptableObject
{
    private const string Path = "PropTypes";

    static Object_PropType _instance = null;
    public static Object_PropType Instance
    {
        get
        {
            if (!_instance)
                _instance = Resources.Load(Path) as Object_PropType;
            return _instance;
        }
    }

    [Serializable]
    public struct Prop
    {
        public PropType Type;
        [Header("Sounds")]
        public string ImpactSFX;
        public string BreakSFX;
        [Header("Physics")]
        [Tooltip("A low Drag value makes an object seem heavy. " +
            "A high one makes it seem light. Typical values for Drag are between" +
            " .001 (solid block of metal) and 10 (feather).")]
        [Range(0f, 10f)]
        public float Drag;
        [Header("Particles")]
        [Tooltip("10 is nearly impossible to break by hand, 8 is difficult, 6 is doable and 1 is very flimsy.")]
        [Range(0, 100)]
        public int DamageThreshold;
        public ParticleSystem Debris;

        public Prop(PropType type)
        {
            Type = type;
            ImpactSFX = "";
            BreakSFX = "";
            Drag = 1f;
            DamageThreshold = -1;
            Debris = null;
        }
    }

    public Prop[] PropTypes = new Prop[]
    {
        new Prop(PropType.Concrete),
        new Prop(PropType.Paper),
        new Prop(PropType.Rubber),
        new Prop(PropType.Glass),
        new Prop(PropType.Metal),
    };

    public Prop GetPropByType(PropType type)
    {
        return PropTypes.FirstOrDefault(prop => prop.Type == type);
    }
}
