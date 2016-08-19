using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ETGDamageIndicatorGUI : MonoBehaviour {

    private static Dictionary<HealthHaver, HealthHaverTracker> _AllBars = new Dictionary<HealthHaver, HealthHaverTracker>();

    public static bool RenderHealthBars = true;
    public static bool RenderIndicators = true;

    public static void Create() {
        GameObject newObject = new GameObject();
        newObject.name="Damage Indicators";
        newObject.AddComponent<ETGDamageIndicatorGUI>();
        newObject.transform.SetParent(ETGModGUI.MenuObject.transform);
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

    class HealthHaverTracker {
        public HealthHaver thisHaver;



    }

    class HealthBar {
        public HealthHaverTracker tracker;


    }

    class DamageIndicator {

        Vector3 position = Vector3.zero;
        float damageTaken = 0;
        float wiggleAmount = 0;
        float lifetime = 1;
        float speed = 1;

        public static List<DamageIndicator> allIndicators = new List<DamageIndicator>();
        public static List<DamageIndicator> removeIndicators = new List<DamageIndicator>();

        public DamageIndicator(Vector3 wPosStart, float damageTaken = 0, float wiggleAmount = 0, float lifetime = 1, float speed = 1) {
            this.position=wPosStart;
            this.damageTaken=damageTaken;
            this.wiggleAmount=wiggleAmount;
            this.lifetime=lifetime;
            this.speed=speed;

            allIndicators.Add(this);
        }

        public void Update() {

            position+=Vector3.up*speed*Time.deltaTime;

            lifetime-=Time.deltaTime;
            if (lifetime<=0)
                Destroy();
        }

        public void OnGUI() {

            Vector2 worldToScreenPoint = Camera.main.WorldToScreenPoint(position);
            worldToScreenPoint=new Vector2(worldToScreenPoint.x, Screen.height-worldToScreenPoint.y);

            GUI.Label(new Rect(worldToScreenPoint,Vector2.one*250), "<size=30> " + damageTaken.ToString() + " </size>");

        }

        public void Destroy() {
            removeIndicators.Add(this);
        }

    }

}

