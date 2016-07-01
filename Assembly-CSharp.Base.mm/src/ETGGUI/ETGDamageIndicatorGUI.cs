using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ETGDamageIndicatorGUI : MonoBehaviour{

    private static List<DamageIndicator> indicators = new List<DamageIndicator>();
    private static List<DamageIndicator> usedPool = new List<DamageIndicator>();
    private static List<DamageIndicator> toRemove = new List<DamageIndicator>();

    public static List<HealthHaver> allHealthHavers = new List<HealthHaver>();
    public static Dictionary<HealthHaver, float> maxHP = new Dictionary<HealthHaver, float>();
    public static Dictionary<HealthHaver, float> currentHP = new Dictionary<HealthHaver, float>();

    public static bool RenderHealthBars = true;

    public static void Create() {
        GameObject newObject = new GameObject();
        newObject.name="Damage Indicators";
        newObject.AddComponent<ETGDamageIndicatorGUI>();
        newObject.transform.SetParent(ETGModGUI.menuObj.transform);
    }

    public void Start() {
        DontDestroyOnLoad(gameObject);
    }

    public void Update() {
        foreach (DamageIndicator i in indicators)
            i.Update();

        foreach (DamageIndicator i in toRemove)
            indicators.Remove(i);
        toRemove.Clear();
    }

    public void OnGUI() {
        foreach (DamageIndicator i in indicators)
            i.OnGUI();
        //Disabled until next patch
        //foreach (HealthHaver HH in allHealthHavers)
            //RenderHealthBar(HH);
    }

    public static void CreateIndicator(Vector3 worldPosOrigin, object content) {

        //We need to make a new object
        if (usedPool.Count==0) {
            DamageIndicator newIndicator = new DamageIndicator();

            newIndicator.wPosOrigin=worldPosOrigin;
            newIndicator.content="<size=35>"+content+"</size>";

            indicators.Add(newIndicator);
        } else {
            //We have a pooled object, use this instead

            DamageIndicator pickedIndicator = usedPool[0];
            usedPool.RemoveAt(0);

            pickedIndicator.content="<size=35>"+content+"</size>";
            pickedIndicator.wPosOrigin=worldPosOrigin;

            indicators.Add(pickedIndicator);
        }
    }

    public void RenderHealthBar(HealthHaver hh) {
        GUILayout.Label(currentHP[hh].ToString());
        Vector3 wPos = (Vector3)hh.SpeculativeRigidbody_0.Vector2_4+(hh.transform.up);
        Vector2 screenPos = Camera.main.WorldToScreenPoint(wPos);
        screenPos=new Vector2(screenPos.x,Screen.height-screenPos.y);

        int hpMaxBar = (int)Mathf.Max(0, 15-(maxHP[hh]))+100;

        Rect totalBarRect = new Rect(screenPos.x-(hpMaxBar/2),screenPos.y-10,hpMaxBar,20);
        Rect filledBarRect = new Rect(screenPos.x-(hpMaxBar/2),screenPos.y-10,hpMaxBar*((float)currentHP[hh]/(float)maxHP[hh]),20);

        GUI.Box(totalBarRect,"");
        GUI.color=Color.red;
        GUI.Box(filledBarRect, "");
        GUI.color=Color.white;
    }

    private class DamageIndicator{
        public Vector2 offset;
        public Vector3 wPosOrigin;
        public object content;

        private float time;

        public void Update() {
            offset-=Vector2.up*Time.deltaTime*5;
            offset=new Vector2(Mathf.Sin(time*15)-0.5f,offset.y);
            time+=Time.deltaTime;

            if (time>=2) {
                usedPool.Add(this);
                toRemove.Add(this);
                time=0;
                offset=Vector2.zero;
            }
        }

        public void OnGUI() {
            Vector2 worldToScreenPoint = Camera.main.WorldToScreenPoint(wPosOrigin);
            worldToScreenPoint=new Vector2(worldToScreenPoint.x,Screen.height-worldToScreenPoint.y);
            GUI.color=Color.red;
            GUI.Label(new Rect(worldToScreenPoint+(offset*15)+(new Vector2(25,25)), new Vector2(50, 50)),content.ToStringIfNoString());
            GUI.color=Color.white;
        }
    }

}

