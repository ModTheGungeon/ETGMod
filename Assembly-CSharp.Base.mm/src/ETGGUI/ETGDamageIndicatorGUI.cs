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
        foreach (DamageIndicator d in DamageIndicator.allIndicators)
            d.Update();

        foreach (DamageIndicator d in DamageIndicator.removeIndicators)
            DamageIndicator.allIndicators.Remove(d);

        DamageIndicator.removeIndicators.Clear();
    }

    public void OnGUI() {
        foreach (DamageIndicator d in DamageIndicator.allIndicators)
            d.OnGUI();
    }

    public static void HealthHaverTookDamage(HealthHaver dmg, float damage) {
        if (!_AllBars.ContainsKey(dmg)) {
            HealthHaverTracker tracker = new HealthHaverTracker();

            tracker.thisHaver=dmg;
            _AllBars.Add(dmg,tracker);
        }

        if (RenderIndicators) {
            DamageIndicator newIndicator = new DamageIndicator(dmg.specRigidbody.UnitCenter,damage);
        }
    }

    public class HealthHaverTracker {
        public HealthHaver thisHaver;

    }

    public class DamageIndicator {
        private readonly static Vector2 _Vector2_250 = new Vector2(250f, 250f);

        Vector3 Position = Vector3.zero;
        float DamageTaken = 0;
        float WiggleAmount = 0;
        float Lifetime = 1;
        float Speed = 1;

        public static List<DamageIndicator> allIndicators = new List<DamageIndicator>();
        public static List<DamageIndicator> removeIndicators = new List<DamageIndicator>();

        public DamageIndicator(Vector3 wPosStart, float damageTaken = 0, float wiggleAmount = 0, float lifetime = 1, float speed = 1) {
            Position = wPosStart;
            DamageTaken = damageTaken;
            WiggleAmount = wiggleAmount;
            Lifetime = lifetime;
            Speed = speed;

            allIndicators.Add(this);
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
            removeIndicators.Add(this);
        }
    }

}

