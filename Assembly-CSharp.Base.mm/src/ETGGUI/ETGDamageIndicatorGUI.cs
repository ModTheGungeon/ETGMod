using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ETGDamageIndicatorGUI : MonoBehaviour {

    private static Dictionary<HealthHaver, HealthHaverTracker> _AllBars = new Dictionary<HealthHaver, HealthHaverTracker>();

    public static bool RenderHealthBars = true;
    public static bool RenderIndicators = true;

    public static void Create() {
        new GameObject("Damage Indicators").AddComponent<ETGDamageIndicatorGUI>().transform.SetParent(ETGModGUI.MenuObject.transform);
    }

    public void Start() {
        DontDestroyOnLoad(gameObject);
    }

    public void Update() {
        for (int i = 0; i < DamageIndicator.AllIndicators.Count; i++)
            DamageIndicator.AllIndicators[0].Update();

        if (DamageIndicator.RemoveIndicators.Count != 0) {
            for (int i = 0; i < DamageIndicator.RemoveIndicators.Count; i++)
                DamageIndicator.AllIndicators.Remove(DamageIndicator.RemoveIndicators[i]);

            DamageIndicator.RemoveIndicators.Clear();
        }
    }

    public void OnGUI() {
        for (int i = 0; i < DamageIndicator.AllIndicators.Count; i++)
            DamageIndicator.AllIndicators[0].OnGUI();
    }

    public static void HealthHaverTookDamage(HealthHaver dmg, float damage) {
        if (!_AllBars.ContainsKey(dmg)) {
            HealthHaverTracker tracker = new HealthHaverTracker();
            tracker.thisHaver = dmg;
            _AllBars.Add(dmg,tracker);
        }

        if (RenderIndicators) {
            DamageIndicator.AllIndicators.Add(new DamageIndicator(dmg.specRigidbody.UnitCenter, damage));
        }
    }

    public class HealthHaverTracker {
        public HealthHaver thisHaver;

    }

    public class DamageIndicator {
        private readonly static Vector2 _Vector2_250 = new Vector2(250f, 250f);

        public Vector3 Position;
        public float DamageTaken;
        public float WiggleAmount;
        public float Lifetime;
        public float Speed;

        public static List<DamageIndicator> AllIndicators = new List<DamageIndicator>();
        public static List<DamageIndicator> RemoveIndicators = new List<DamageIndicator>();

        public DamageIndicator(Vector3 wPosStart, float damageTaken = 0, float wiggleAmount = 0, float lifetime = 1, float speed = 1) {
            Position = wPosStart;
            DamageTaken = damageTaken;
            WiggleAmount = wiggleAmount;
            Lifetime = lifetime;
            Speed = speed;
        }

        public void Update() {
            Position += Vector3.up * Speed * Time.deltaTime;

            Lifetime -= Time.deltaTime;
            if (Lifetime <= 0)
                Destroy();
        }

        public void OnGUI() {
            Vector2 worldToScreenPoint = Camera.main.WorldToScreenPoint(Position);
            worldToScreenPoint = new Vector2(worldToScreenPoint.x, Screen.height - worldToScreenPoint.y);

            GUI.Label(new Rect(worldToScreenPoint, _Vector2_250), "<size=30> " + DamageTaken + " </size>");
        }

        public void Destroy() {
            RemoveIndicators.Add(this);
        }
    }

}

